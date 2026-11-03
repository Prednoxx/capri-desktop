using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace CapriDesktop
{
    internal static partial class Hands
    {
        internal static bool AppleMusicId(string id)
        {
            return Regex.IsMatch(id ?? "", @"^(?:AppleInc\.AppleMusicWin_[a-z0-9]+![a-z0-9.]+|AppleMusic(?:\.exe)?)$", RegexOptions.IgnoreCase);
        }

#if !CROSS
        internal static Func<Tune> AppleMusicSession = ReadAppleMusic;
        internal static Func<Tune, string, bool> AppleMusicSend = delegate(Tune tune, string command) { return Tunes.SendOnly(tune, command); };
        internal static Action<int> MusicWait = Thread.Sleep;

        private static Tune ReadAppleMusic()
        {
            List<Tune> sessions = Tunes.Sessions();
            return sessions == null ? null : sessions.Find(tune => tune != null && AppleMusicId(tune.App) && !tune.Video);
        }
#endif

        private static string PlayAppleMusic(string song, string kind)
        {
#if CROSS
            return "Couldn't play through Apple Music: this tool supports it only on Windows. I didn't open another music service.";
#else
            InstalledApp app = Apps().Find(item => AppleMusicId(item.Id));
            if (app == null) return "No app called Apple Music is installed on this PC. I didn't open another music service.";
            Run(app.Id);
            bool random = (string.IsNullOrEmpty(kind) || kind == "song") && Regex.IsMatch(song.Trim(), @"^(?:(?:play|shuffle)\s+)?(?:(?:me|some|any|a|random)\s+){0,2}(?:music|songs?|tracks?|tunes?|something|anything|my library)(?:\s+(?:random|for me))?$", RegexOptions.IgnoreCase);
            if (!random) return "Couldn't select that named song or playlist in Apple Music. The app is open; nothing else was opened or played.";
            Tune before = null;
            for (int attempt = 0; attempt < 16; attempt++)
            {
                before = AppleMusicSession();
                if (before != null && AppleMusicId(before.App) && !before.Video) break;
                before = null; MusicWait(250);
            }
            if (before == null) return "Couldn't confirm playback: Apple Music is open but has no music queue available to Windows. Start a library or playlist there first.";
            string oldKey = before.Key;
            if (!AppleMusicSend(before, "shuffle")) return "Couldn't enable shuffle in Apple Music. The app is open; other players were left alone.";
            if (!before.CanSkip || !AppleMusicSend(before, "next")) return "Couldn't select another track in Apple Music. Shuffle was accepted, but a random song wasn't confirmed.";
            // Send Play to this session only. Global media keys can start an unrelated video player.
            if (!before.Playing && !AppleMusicSend(before, "play")) return "Couldn't start playback in Apple Music. Shuffle was accepted, but Play wasn't.";
            for (int attempt = 0; attempt < 20; attempt++)
            {
                Tune after = AppleMusicSession();
                if (after != null && AppleMusicId(after.App) && !after.Video && !after.Changing && after.Playing && after.Shuffling == true && !string.IsNullOrWhiteSpace(after.Title) && after.Key != oldKey)
                    return "Playing " + after.Title + (string.IsNullOrWhiteSpace(after.Artist) ? "" : " by " + after.Artist) + " in Apple Music with shuffle on.";
                MusicWait(250);
            }
            return "Couldn't verify a different song playing in Apple Music after shuffle. Check the app; I didn't switch to another player.";
#endif
        }

        internal static void SmokeMusicApps(List<string> checks)
        {
            string tool; ToolArguments args;
            foreach (string request in new[] { "open Apple Music and play something random", "play something random on Apple Music", "Capri, please open Apple Music and play a random song", "open applemusic then play something random", "play a random song in Apple Music", "open my Apple Music app and play something random" })
            {
                if (!Heard(request, out tool, out args) || tool != "play_music" || args.App != "apple_music" || args.Song != "something random" || !Instant(tool, args)) throw new Exception("Apple Music request lost its app: " + request);
#if !CROSS
                if (Companion.CursorJob(tool, args) != null) throw new Exception("Apple Music took a cursor or YouTube detour.");
#endif
                List<KeyValuePair<string, ToolArguments>> calls = new List<KeyValuePair<string, ToolArguments>> { new KeyValuePair<string, ToolArguments>("open_app", new ToolArguments { Name = "Photos" }) };
                Rescue(request, calls);
                if (calls.Count != 1 || calls[0].Key != "play_music" || calls[0].Value.App != "apple_music") throw new Exception("A wrong-app plan survived the exact music request.");
            }
            if (!Valid("play_music", new ToolArguments { Song = "something random", App = "apple_music" }) || !AppleMusicId("AppleInc.AppleMusicWin_nzyj5cx40ttqa!App") || AppleMusicId("Microsoft.Windows.Photos_8wekyb3d8bbwe!App") || AppleMusicId("AppleMusicVideo.exe")) throw new Exception("Music app identity checks were wrong.");
            checks.Add("PASS Apple Music: exact open-and-play requests, random-song suffixes and rescued plans preserve the named app and bypass cursor jobs");
#if !CROSS
            Func<List<InstalledApp>> catalog = Catalog; Action<string> run = Run; Func<string, string> opened = Opened;
            Func<Tune> session = AppleMusicSession; Func<Tune, string, bool> send = AppleMusicSend; Action<int> wait = MusicWait;
            Func<List<Tune>> sessions = Tunes.Sessions; Action<byte> press = Press;
            try
            {
                const string apple = "AppleInc.AppleMusicWin_nzyj5cx40ttqa!App";
                List<string> launches = new List<string>(), commands = new List<string>();
                Catalog = delegate { return new List<InstalledApp> { new InstalledApp("Apple Music", apple), new InstalledApp("Photos", "Microsoft.Windows.Photos_8wekyb3d8bbwe!App") }; }; Forget();
                Run = launches.Add; Opened = delegate { return ""; }; MusicWait = delegate { };
                Tune current = new Tune { App = apple, Title = "Old song", Playing = false, CanSkip = true };
                AppleMusicSession = delegate { return current; };
                AppleMusicSend = delegate(Tune tune, string command)
                {
                    if (!AppleMusicId(tune.App)) throw new Exception("Command targeted a different app.");
                    commands.Add(command);
                    if (command == "shuffle") current.Shuffling = true;
                    if (command == "next") current.Title = "New song";
                    if (command == "play") current.Playing = true;
                    return true;
                };
                string result = Do("play_music", new ToolArguments { Song = "something random", App = "apple_music" });
                if (launches.Count != 1 || launches[0] != apple || string.Join(",", commands.ToArray()) != "shuffle,next,play" || !result.StartsWith("Playing New song", StringComparison.Ordinal)) throw new Exception("Apple shuffle did not verify the matching session: " + result);
                launches.Clear(); commands.Clear(); current.Title = "Previous song"; current.Playing = false;
                result = Do("use_computer", new ToolArguments { Task = "open Apple Music and play something random" });
                if (launches.Count != 1 || launches[0] != apple || string.Join(",", commands.ToArray()) != "shuffle,next,play" || !result.StartsWith("Playing New song", StringComparison.Ordinal)) throw new Exception("A computer-task route lost Apple Music.");
                commands.Clear(); AppleMusicSession = delegate { return new Tune { App = "Microsoft.Windows.Photos_8wekyb3d8bbwe!App", Title = "Video", Playing = true }; };
                result = Do("play_music", new ToolArguments { Song = "something random", App = "apple_music" });
                if (commands.Count != 0 || !Failed(result)) throw new Exception("A video session counted as Apple Music.");
                AppleMusicSession = delegate { return current; }; AppleMusicSend = delegate { return false; };
                if (!Failed(Do("play_music", new ToolArguments { Song = "something random", App = "apple_music" }))) throw new Exception("Rejected shuffle reported success.");
                AppleMusicSend = delegate { return true; };
                if (!Failed(Do("play_music", new ToolArguments { Song = "something random", App = "apple_music" }))) throw new Exception("An unchanged song reported success.");
                foreach (Tune invalid in new[]
                {
                    new Tune { App = "Microsoft.Windows.Photos_8wekyb3d8bbwe!App", Title = "New", Playing = true, Shuffling = true },
                    new Tune { App = apple, Title = "New", Playing = false, Shuffling = true },
                    new Tune { App = apple, Title = "New", Playing = true, Changing = true, Shuffling = true },
                    new Tune { App = apple, Title = "New", Playing = true, Shuffling = false },
                    new Tune { App = apple, Title = "New", Playing = true, Video = true, Shuffling = true }
                })
                {
                    bool moved = false;
                    AppleMusicSession = delegate { return moved ? invalid : current; };
                    AppleMusicSend = delegate(Tune tune, string command) { moved = true; return true; };
                    if (!Failed(Do("play_music", new ToolArguments { Song = "something random", App = "apple_music" }))) throw new Exception("Unverified playback reported success.");
                }
                commands.Clear(); AppleMusicSend = delegate(Tune tune, string command) { commands.Add(command); return true; };
                if (!Failed(Do("play_music", new ToolArguments { Song = "A named song", App = "apple_music" })) || commands.Count != 0) throw new Exception("A named song was replaced with random playback.");
                if (!Failed(Do("play_music", new ToolArguments { Song = "Something", Kind = "playlist", App = "apple_music" })) || commands.Count != 0) throw new Exception("A named playlist was replaced with random playback.");
                Catalog = delegate { return new List<InstalledApp> { new InstalledApp("Apple Music", "Microsoft.Windows.Photos_8wekyb3d8bbwe!App") }; }; Forget(); launches.Clear();
                if (!Failed(Do("play_music", new ToolArguments { Song = "something random", App = "apple_music" })) || launches.Count != 0) throw new Exception("An app-name match bypassed the identity check.");
                if (!Failed(Do("play_music", new ToolArguments { Song = "something random", App = "gallery" })) || launches.Count != 0) throw new Exception("An unsupported app fell back to another service.");
                int keys = 0; Press = delegate { keys++; };
                if (Tunes.SendOnly(new Tune { App = apple }, "play") || Tunes.SendOnly(new Tune { App = apple }, "like") || keys != 0) throw new Exception("A named session fell back to global media keys or another player's controls.");
                checks.Add("PASS Apple Music: verified shuffle/next/play, wrong-player rejection, missing app, rejected controls, unchanged tracks and named-song limits never claim unverified playback");
                checks.Add("PASS named-app media commands never fall back to global media keys");
                checks.Add("LIMIT live Apple Music sign-in, library and shuffle availability: session controls use stand-ins; Capri remains stopped");
            }
            finally { Catalog = catalog; Run = run; Opened = opened; AppleMusicSession = session; AppleMusicSend = send; MusicWait = wait; Tunes.Sessions = sessions; Press = press; Forget(); }
#endif
        }
    }
}
