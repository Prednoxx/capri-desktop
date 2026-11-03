# Music app routing

Build 0.8.0.2 fixes `open Apple Music and play something random` losing its named app. The combined command previously missed the direct route, while `play something random on Apple Music` could lose the app and fall back to Spotify or YouTube.

## Apple Music on Windows

- Combined open/play commands and `on`, `in`, `using` or `through Apple Music` keep `apple_music` as the tool's app.
- This route runs directly, including on planning models. It does not ask the model to click through the desktop or turn into a YouTube job.
- The installed app must match Apple Music's Windows identity. A similarly named photo/video app is not enough.
- Random playback uses Apple Music's existing queue: request shuffle, select the next track, and play through that session. No global media keys are used. A failed session command cannot start whichever other player Windows currently prefers.
- Success requires a different nonempty track title, active playback and shuffle reported by Apple Music's own session. Video, paused, changing and other-app sessions do not establish success.
- If Apple Music has no available queue, refuses shuffle, or does not confirm playback, the result says so. There is no switch to another service.
- Named-song and named-playlist selection in Apple Music is not supported by this tool yet. It can open the app, but reports that selection is incomplete rather than choosing unrelated music.

Windows controls use [the session API](https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssession?view=winrt-26100), [shuffle requests](https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssession.trychangeshuffleactiveasync?view=winrt-26100) and [reported shuffle state](https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionplaybackinfo.isshuffleactive?view=winrt-26100). Live support depends on what Apple Music exposes for its current queue.

## Interrupted requests

With transcript saving enabled, incoming chat requests now get a local receipt before model processing. Tool starts and task-step results are also recorded. Receipt/step lines stay out of the memory-fold queue, and API credentials are redacted. This preserves useful evidence when a model stalls or the app must be stopped before an answer finishes. Turning transcript saving off also disables these lines.

The repetitive/invalid-text fallback now describes the model output check without claiming it diagnosed a GPU fault. The CPU fallback behavior is unchanged.

## Checks

The focused Windows checks run by loading the compiled assembly in a separate PowerShell process. They do not start Capri's UI. App launches, music sessions, controls and desktop input are replaced by stand-ins.

- Exact command and phrasing variants preserve Apple Music, including a wrong-app plan corrected by the direct route.
- Direct `play_music` and music filed under `use_computer` both target the same app.
- Rejected shuffle, missing sessions, unchanged songs, named-song limits and misleading app names cannot report verified playback.
- A named session without native control support cannot send global media keys.
- An offline request retains its text before failing, respects transcript settings and does not enter the memory-fold queue.
- Existing request routing and bounded-task regressions still pass.

Live Apple Music playback was not replayed during this check: the desktop app had been stopped at the user's request. Mac/Linux builds were not run because the .NET 10 SDK is not installed. Their tool returns an unsupported result instead of opening another music service.
