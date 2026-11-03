# Desktop task fixes

Build: 0.8.0.1. Base: lachydotmcg/capri-desktop at 873e931.

## What changed

- Finished, failed and cancelled jobs keep their result. Calling the same job again returns that result without running its actions again. Permission waits can still resume.
- An action gets two attempts on the same observed screen. A third attempt is blocked. Three blocked attempts stop the job with a failure message instead of leaving it running.
- Returning to an earlier screen does not reset its action history. Screenshot jobs compare small grayscale frames, allowing a small amount of cursor or animation movement. The last 16 views are retained.
- A successful app or website launch runs once per job. New instructions from the user clear that history.
- Local desktop jobs get 120 model turns. Cloud desktop jobs get 600. Games and live streams retain their existing continuous behavior and Esc stop control.
- The screenshot path used by local models runs one desktop action, then takes a fresh screenshot. Typing and its requested Enter remain one operation. A model cannot click through a whole batch and declare success before looking again.
- Plain `done`, or `done` followed by a summary, works alongside the existing tool format. A response containing another tool call is not treated as plain completion.
- Unreadable replies after partial execution return failure. If every attempted step failed, the job cannot report success.
- Payment-page handoffs report that user input is needed, rather than marking the task complete. Existing payment and credential protections stay in place.
- Task reports include the outcome, model turns and blocked repeats. They still list individual actions and failures. Successful input delivery is not treated as proof that the whole task finished.
- Earlier successful steps remain in a compact history: up to eight entries of 120 characters, in addition to the recent steps. This keeps the start of a job from disappearing from a small model's context.
- Task execution and routing use temperature 0.2 on Ollama and local OpenAI-compatible servers. Conversation stays at 0.85.
- Preset task, wake and status lines were refreshed. Stop lines no longer say “done,” and progress lines avoid guessed finish times.
- Mini-game boards rescale after Windows finishes moving the window. This fixes a monitor-positioning failure also reproduced in the unmodified upstream build.
- Windows updates now come from this fork. Upstream releases cannot replace this build through its updater.
- The Windows smoke runner gets 15 minutes by default. The old one-minute wrapper could stop the suite while its own checks were still running.
- The build script accepts `-SmokeDirectory` for a shorter test path when a long checkout path hits Windows' legacy path limit.

The sprites, animation code, personality, voice models, recorded startup greetings and permission settings are unchanged. Recorded greetings keep their matching text. Updated preset text is spoken through the selected voice.

## Code map

| File | Role |
| --- | --- |
| `app/Brain.cs` | Chat, provider requests, tool routing, local sampling and shared preset lines |
| `app/Hands.cs` | High-level tools, arguments, permissions and action dispatch |
| `app/Computer.cs` | Windows observations, input, task lifecycle, permission waits and execution paths |
| `app/Play.cs` | Screenshot action parsing, game controls and visual task briefs |
| `app/JobSafety.cs` | Retry history, turn budgets, terminal results, completion parsing and compact progress history |
| `app/JobChecks.cs` | Scripted task regressions with fake observations and input |
| `app/Asides.cs` | Task status used during chat and voice calls |
| `app/Capri.cs` | App integration, task panel, model wiring, smoke checks and updates |
| `app/Minis.cs` | Local mini-game boards |

The second repository, [mochiiwuff/capri-desktop](https://github.com/mochiiwuff/capri-desktop), was reviewed at cb33e87. It is an Electron app focused on coding-tool events and mission logs. Its code was not copied into the C# app.

## Tests

Windows build and full smoke suite:

```powershell
./ci/build-windows.ps1 -SmokeTest -SmokeSeconds 1800
```

For a long checkout path:

```powershell
$smoke = Join-Path $env:TEMP ('capri-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
./ci/build-windows.ps1 -SmokeTest -SmokeSeconds 1800 -SmokeDirectory $smoke
```

Focused task regressions after building:

```powershell
./out/windows/Capri.exe --smoke-test out/windows/task-checks --only SmokeJobSafety,SmokeLongJob,SmokeOpensOnce
```

The regressions cover repeated clicks, reopening apps, alternating screens, changing-screen loops, terminal replay, multi-step execution, plain completion replies, cancellation, model errors, false success, compact history, screenshot verification and long tasks with forward progress. Existing long-task fixtures now advance their fake page or target instead of simulating the same action on an unchanged screen indefinitely.

Gemma 4 E2B Q4_K_M was found in Capri's existing local brain folder, with its vision projector. The installed runtime is llama.cpp; no running Ollama service was found. A temporary localhost server ran three text-only fixtures using the app's actual prompt, tool listing and completion parser: completed note, next unfinished step and completed search. All three passed. This checks model output handling, not real desktop accuracy.

## Limits

The guard reduces repeated input; it cannot prove every possible app-specific result. A changed screenshot is not proof of success. Desktop jobs may stop when an app provides too little visible feedback. The user can give a new instruction or start a new job. The turn budget bounds model rounds, not wall-clock time; existing request timeouts and Esc still apply.

The task state lasts for the job's lifetime. Jobs do not resume automatically after an app restart. Automatic scheduling, crash recovery and new file permissions were not added.

## Preset wording sources

The line edits keep this project's existing playful fan interpretation. They are original wording, not dialogue from the show. [CD PROJEKT RED's series page](https://www.cyberpunk.net/en/edgerunners2), [Netflix's announcement](https://www.netflix.com/tudum/articles/cyberpunk-edgerunners-2-release-date-news) and [Capri's official merchandise listing](https://eu.gear.cdprojektred.com/it/products/cyberpunk-edgerunners-2-capri-hoodie) were checked. Those pages do not provide a detailed Capri speech guide. No new backstory or supposed canon catchphrases were added.

## Installing

Use this fork's Windows installer. It uses Capri's existing data folder, so the installed brain, voice, settings and chat stay in place. Mac and Linux downloads linked in the original README remain upstream builds; the Windows automation changes described here do not add computer control to them.

The original noncommercial licence and required notices still apply.
