# Test results

Build 0.8.0.1, Windows, November 2, 2026.

| Check | Result |
| --- | --- |
| Windows app and installer build | Passed |
| Full Windows smoke suite | 784 passed, 0 failed, 0 skipped |
| Focused task regressions | 15 passed, 0 failed |
| Gemma 4 E2B text fixtures | 3 passed |
| Installer verification | Passed |
| Embedded app matches the tested executable | SHA-256 matches |
| PowerShell build script syntax | Passed |
| Git whitespace check | Passed |

The full suite reports 39 manual validation limits. These include physical input, tray interactions, real games, audio devices and live provider access. They are not counted as automated passes. Installation and uninstallation were not performed against the existing install.

Gemma checks used the installed E2B Q4_K_M model through a temporary local llama.cpp server, the app's task prompt and tool listing, and its completion parser. Cases: a completed note, the next unfinished typing step, and a completed search. These were text fixtures, not a live desktop benchmark. The temporary server was stopped after testing.

The full suite passed from a short scratch path. Earlier runs in the long checkout path hit Windows' legacy path limit. One earlier combined run also failed a window-wander animation assertion; subsequent integration runs and the final full run passed it. The character animation code was not changed.

The mini-board monitor-positioning failure was reproduced in the untouched upstream build before fixing the deferred rescale.

Mac and Linux were not built locally because the .NET 10 SDK is not installed. The desktop automation changes are Windows-specific.

See [task execution changes](task-execution.md) for commands, implementation details and limits.
