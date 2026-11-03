using System;
using System.Collections.Generic;
using System.Drawing;

namespace CapriDesktop
{
    internal partial class Companion
    {
        internal static void SmokeRequestReceipts(List<string> checks, string folder)
        {
            string path = Brain.TranscriptPath; bool keeping = Brain.Keeping;
            try
            {
                Brain.TranscriptPath = folder; Brain.Keeping = true;
                string request = "open Apple Music and play something random";
                try { new Brain().Ask(request); } catch (BrainException) { }
                Brain.Received("use api_key=fixture-secret-token");
                string day = System.IO.Path.Combine(folder, DateTime.Now.ToString("yyyy-MM-dd") + ".md");
                string receipt = System.IO.File.ReadAllText(day);
                if (!receipt.Contains("Request received: " + request) || receipt.Contains("fixture-secret-token") || !receipt.Contains("api_key=[redacted]") || System.IO.File.Exists(System.IO.Path.Combine(folder, "unfolded.txt"))) throw new Exception("A failed request lost its receipt, exposed a key or entered long-term memory.");
                Brain.Received("(You glanced at their screen)");
                Brain.Keeping = false; Brain.Received("not retained");
                if (System.IO.File.ReadAllText(day) != receipt) throw new Exception("Receipt logging ignored transcript settings or retained an ambient glance.");
                checks.Add("PASS request text is saved before an offline failure, respects transcript settings and stays out of the memory-fold queue");
            }
            finally { Brain.TranscriptPath = path; Brain.Keeping = keeping; }
        }

        private static void SmokeJobSafety(List<string> checks)
        {
            Func<Sight> lookWas = Computer.Look, glimpseWas = Computer.Glimpse;
            Func<IntPtr> frontWas = Computer.Front;
            Func<string, ToolArguments, Sight, string> actWas = Computer.Act;
            Func<Rectangle?, Picture> shootWas = Computer.Shoot;
            Func<string, ToolArguments, Picture, string> useWas = Computer.Use;
            Func<Target> focusWas = Computer.Focus;
            bool streamingWas = Computer.Streaming;
            try
            {
                Computer.Streaming = false;
                Sight screen = new Sight { Window = new IntPtr(7), Title = "Notes", Program = "notepad" };
                screen.Targets.Add(new Target { Number = 1, Role = "button", Name = "Next" });
                Computer.Look = Computer.Glimpse = delegate { return screen; };
                Computer.Front = delegate { return screen.Window; };
                int actions = 0;
                Computer.Act = delegate { actions++; return "Clicked Next."; };

                Job repeats = new Job("go through the notes");
                repeats.Think = delegate { return "click{\"target\":\"1\"}"; };
                repeats.Run();
                if (actions != 2 || repeats.Outcome != "failed" || repeats.Report().Blocked != 3) throw new Exception("Repeated clicks were not stopped.");
                int before = actions;
                string stopped = repeats.Run();
                if (actions != before || stopped != JobGuard.Stopped) throw new Exception("A failed job restarted.");
                checks.Add("PASS task loops: repeated actions stop after two attempts, with three blocked retries and a failed outcome");

                actions = 0;
                int turn = 0;
                Job sequence = new Job("open notes, type hello, and save");
                string[] script = { "open_app{\"name\":\"notepad\"}", "type{\"target\":\"1\",\"text\":\"hello\"}", "press{\"keys\":\"ctrl+s\"}", "done{\"text\":\"Saved the note.\"}" };
                sequence.Think = delegate { return script[turn++]; };
                string result = sequence.Run();
                int thoughts = turn;
                if (result != "Saved the note." || sequence.Outcome != "done" || actions != 3 || sequence.Run() != result || turn != thoughts || actions != 3) throw new Exception("A completed multi-step job restarted.");
                if (sequence.Report().Done != 3 || sequence.Report().Outcome != "done") throw new Exception("Task report lost the final state.");
                checks.Add("PASS task completion: multi-step jobs run once, retain their result and report their final state");

                string summary;
                if (!Computer.FinishedReply("done\nI saved the note.", out summary) || summary != "I saved the note." || !Computer.FinishedReply("done", out summary)
                    || Computer.FinishedReply("done\nclick{\"target\":\"1\"}", out summary) || Computer.FinishedReply("not done", out summary)) throw new Exception("Plain completion replies were parsed incorrectly.");
                actions = turn = 0;
                Job plainDone = new Job("select the note");
                plainDone.Think = delegate { return turn++ == 0 ? "click{\"target\":\"1\"}" : "done\nI selected the note."; };
                if (plainDone.Run() != "I selected the note." || actions != 1 || plainDone.Outcome != "done") throw new Exception("Gemma's plain done reply restarted a task.");
                checks.Add("PASS small-model completion: bare done and done with a summary end the job without another action");

                actions = turn = 0;
                Job reopen = new Job("open notes");
                reopen.Think = delegate { screen.Title = "Notes " + turn++; return "open_app{\"name\":\"notepad\"}"; };
                reopen.Run();
                if (actions != 1 || reopen.Outcome != "failed") throw new Exception("A finished open action repeated on a changing screen.");
                checks.Add("PASS task replay: a successful app launch is not repeated when the screen changes");

                actions = turn = 0;
                Job drifting = new Job("finish the note");
                drifting.Think = delegate { screen.Title = "Notes " + turn; return "press{\"keys\":\"key" + turn++ + "\"}"; };
                drifting.Run();
                if (actions != JobGuard.TurnLimit || drifting.Report().Turns != JobGuard.TurnLimit || drifting.Outcome != "failed") throw new Exception("Changing screens escaped the task budget.");
                checks.Add("PASS task budget: changing screens and different actions cannot run a desktop job forever");

                actions = turn = 0;
                Computer.Act = delegate { actions++; return "That didn't work: blocked."; };
                Job falseDone = new Job("finish the note");
                falseDone.Think = delegate { return turn++ == 0 ? "click{\"target\":\"1\"}" : "done{\"text\":\"Done.\"}"; };
                falseDone.Run();
                if (actions != 1 || falseDone.Outcome != "failed") throw new Exception("Failed steps were reported as completion.");
                Job broken = new Job("finish the note");
                int brokenCalls = 0;
                broken.Think = delegate { brokenCalls++; throw new InvalidOperationException("test brain failure"); };
                try { broken.Run(); throw new Exception("The brain error disappeared."); }
                catch (InvalidOperationException) { }
                broken.Run();
                if (brokenCalls != 1 || broken.Outcome != "failed") throw new Exception("An errored job restarted.");
                checks.Add("PASS task failures: failed actions and brain errors cannot turn into success or restart the job");

                actions = turn = 0;
                Computer.Act = delegate { actions++; return "Clicked Next."; };
                Job cancelled = new Job("finish the note");
                cancelled.Think = delegate { turn++; Computer.Interrupt("Stopped test task."); return "click{\"target\":\"1\"}"; };
                cancelled.Run(); cancelled.Run();
                if (actions != 0 || turn != 1 || cancelled.Outcome != "stopped") throw new Exception("A cancelled job acted or restarted.");
                checks.Add("PASS task cancellation: a stop during model generation prevents the action and stays stopped");

                JobGuard cycle = new JobGuard();
                for (int i = 0; i < 4; i++) { cycle.Observe((i % 2).ToString()); if (!cycle.Allow("click")) throw new Exception("An initial cycle step was refused."); }
                cycle.Observe("0");
                if (cycle.Allow("click")) throw new Exception("An alternating screen loop escaped the guard.");
                cycle.Redirect();
                if (!cycle.Allow("click")) throw new Exception("An explicit change did not clear stale attempts.");
                JobGuard budget = new JobGuard();
                for (int i = 0; i < JobGuard.TurnLimit; i++) if (!budget.Turn()) throw new Exception("The turn budget ended early.");
                if (budget.Turn()) throw new Exception("The turn budget did not end.");
                checks.Add("PASS task bounds: alternating screen loops are caught, explicit changes reset retries, and desktop jobs have a turn limit");

                List<string> steps = new List<string> { "open_app: Opened Notes." };
                for (int i = 0; i < 20; i++) steps.Add("click: Clicked step " + i + ".");
                string brief = Computer.Brief("finish the note", steps, screen);
                if (!brief.Contains("Earlier steps already run") || !brief.Contains("Opened Notes") || Computer.History(steps, 6).Length > 1100) throw new Exception("Earlier task progress was lost or unbounded.");
                checks.Add("PASS task memory: earlier milestones stay in the small-model brief within a fixed size");

                using (Bitmap bitmap = new Bitmap(320, 200))
                {
                    using (Graphics draw = Graphics.FromImage(bitmap)) draw.Clear(Color.White);
                    Picture picture = Picture.Of(bitmap, new Rectangle(0, 0, 320, 200));
                    Computer.Shoot = delegate { return picture; };
                    Computer.Focus = delegate { return null; };
                    Computer.Use = delegate { actions++; return "Pressed Return."; };
                    screen.Targets.Clear();
                    actions = turn = 0;
                    Job vision = new Job("select the note");
                    vision.Seer = delegate { return ++turn == 1 ? "press{\"keys\":\"ctrl+a\"}\ndone{\"text\":\"Selected it.\"}" : "done{\"text\":\"Selected it.\"}"; };
                    vision.Run();
                    if (actions != 1 || turn != 2 || vision.Outcome != "done") throw new Exception("A vision batch finished before a fresh observation.");
                    actions = turn = 0;
                    Job unreadable = new Job("select the note");
                    unreadable.Seer = delegate { return ++turn == 1 ? "press{\"keys\":\"ctrl+a\"}" : "I cannot produce a move"; };
                    unreadable.Run();
                    if (actions != 1 || unreadable.Outcome != "failed") throw new Exception("Invalid model replies were reported as success.");

                    int shots = 0;
                    Computer.Shoot = delegate
                    {
                        using (Bitmap noisy = new Bitmap(320, 200))
                        {
                            using (Graphics draw = Graphics.FromImage(noisy)) draw.Clear(Color.White);
                            noisy.SetPixel(++shots % 300, 10, Color.Black);
                            return Picture.Of(noisy, new Rectangle(0, 0, 320, 200));
                        }
                    };
                    actions = 0;
                    Job visualLoop = new Job("select the note");
                    visualLoop.Seer = delegate { return "press{\"keys\":\"ctrl+a\"}"; };
                    visualLoop.Run();
                    if (actions != 2 || visualLoop.Outcome != "failed" || visualLoop.Report().Blocked != 3) throw new Exception("Minor screenshot movement hid a repeated-action loop.");
                }
                checks.Add("PASS vision completion: desktop actions get a fresh observation before done, and invalid replies never become success");
                checks.Add("PASS vision loops: minor cursor-sized changes do not reset repeated-action tracking");
            }
            finally
            {
                Computer.Look = lookWas; Computer.Glimpse = glimpseWas; Computer.Front = frontWas;
                Computer.Act = actWas; Computer.Shoot = shootWas; Computer.Use = useWas;
                Computer.Focus = focusWas; Computer.Streaming = streamingWas;
                Computer.Finish();
            }
        }
    }
}
