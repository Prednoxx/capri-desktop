using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CapriDesktop
{
    internal static partial class Computer
    {
        internal static bool FinishedReply(string reply, out string summary)
        {
            summary = null;
            string visible = Brain.Visible(reply ?? "").Trim().Trim('`').Trim();
            if (StepText.IsMatch(visible) || BareClick.IsMatch(visible)) return false;
            Match finished = Regex.Match(visible, @"^(?:done|finished|complete|completed)(?:\s*[.!:]\s*|\s+|$)(.*)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!finished.Success) return false;
            summary = finished.Groups[1].Value.Trim();
            return true;
        }

        internal static string History(IList<string> steps, int recent)
        {
            int older = Math.Max(0, steps.Count - recent);
            List<string> milestones = new List<string>();
            for (int i = 0; i < older && milestones.Count < 8; i++)
            {
                string step = steps[i];
                int split = ResultAt(step);
                string result = split < 0 ? step : step.Substring(split + 2);
                if (Failed(result) || step.StartsWith("wait", StringComparison.Ordinal) || step.StartsWith("note", StringComparison.Ordinal)) continue;
                string brief = step.Length > 120 ? step.Substring(0, 117) + "..." : step;
                if (!milestones.Contains(brief)) milestones.Add(brief);
            }
            return milestones.Count == 0 ? "" : "\nEarlier steps already run (do not restart them):\n- " + string.Join("\n- ", milestones.ToArray());
        }
    }

    internal sealed class JobGuard
    {
        internal const int TurnLimit = 120;
        internal const string Repeated = "That didn't work: I blocked a repeated action on a screen already visited. Check whether the job is finished; otherwise try a different step.";
        internal const string Stopped = "I stopped because the job wasn't making reliable progress. Some steps may have run; I couldn't verify the whole job finished.";
        private readonly Dictionary<string, int> attempts = new Dictionary<string, int>();
        private readonly HashSet<string> opened = new HashSet<string>();
        private string observation = "";
        private bool limited;
        internal int Turns, Blocked;
        internal bool Exhausted { get { return limited || Blocked >= 3; } }
        internal void Observe(string state) { observation = Digest(Encoding.UTF8.GetBytes(state ?? "")); }
        internal bool Turn(int limit = TurnLimit)
        {
            if (Turns >= limit) limited = true;
            if (Exhausted) return false;
            Turns++;
            return true;
        }

        internal bool Allow(string action)
        {
            string key = observation + "|" + action;
            int count;
            attempts.TryGetValue(key, out count);
            if (count >= 2 || opened.Contains(action)) { Blocked++; return false; }
            attempts[key] = count + 1;
            return true;
        }

        internal void Opened(string action) { opened.Add(action); }
        internal void Redirect() { attempts.Clear(); opened.Clear(); Blocked = 0; }

        internal static string Digest(byte[] data)
        {
            using (SHA256 hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(data));
        }

        internal static string Action(string tool, ToolArguments args, Sight sight)
        {
            ToolArguments stable = (args ?? new ToolArguments()).Copy();
            Target target = sight == null ? null : sight.Find(stable.Target);
            if (target != null) stable.Target = target.Role + "|" + target.Name + "|" + target.Box;
            if (tool == "open_app") stable.Name = (stable.Name ?? "").Trim().ToLowerInvariant();
            using (MemoryStream data = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(ToolArguments)).WriteObject(data, stable);
                return tool + "|" + Digest(data.ToArray());
            }
        }
    }

    internal sealed partial class Job
    {
        private readonly JobGuard guard = new JobGuard();
        private readonly object execution = new object();
        internal int TurnBudget = JobGuard.TurnLimit;
        private string ended;
        private bool terminal;
        private readonly List<byte[]> views = new List<byte[]>();
        private readonly List<string> viewNames = new List<string>();
        private bool Bounded { get { return !Playing && !Computer.Streaming; } }

        private bool NextTurn() { return !Bounded || guard.Turn(TurnBudget); }

        private string Guarded(string tool, ToolArguments args, Sight sight)
        {
            if (!Bounded) return null;
            string action = JobGuard.Action(tool, args, sight);
            if (guard.Allow(action)) return action;
            Mark(new JobStep { Tool = tool, Label = JobGuard.Repeated, Finished = true, Failed = true });
            return null;
        }

        private void Observed(Picture picture)
        {
            if (!Bounded || picture == null) return;
            byte[] gray = null;
            try
            {
                using (MemoryStream data = new MemoryStream(picture.Jpeg))
                using (Bitmap bitmap = new Bitmap(data)) gray = Computer.Gray(bitmap);
            }
            catch (ArgumentException) { }
            if (gray != null)
            {
                for (int i = 0; i < views.Count; i++)
                    if (Computer.Changed(views[i], gray) <= 0.002) { guard.Observe(viewNames[i]); return; }
            }
            string key = JobGuard.Digest(picture.Jpeg);
            if (gray != null)
            {
                if (views.Count >= 16) { views.RemoveAt(0); viewNames.RemoveAt(0); }
                views.Add(gray); viewNames.Add(key);
            }
            guard.Observe(key);
        }
    }
}
