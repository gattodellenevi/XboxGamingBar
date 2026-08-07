# Project Rules & Guidelines for CouchGameBar / XboxGamingBar

## Task Scheduler & Autostart
* When setting up autostart for `CouchGamingBarHelper.exe` via Windows Task Scheduler:
  * Trigger must be set to **"At log on"** (not "At system startup").
  * Option **"Run only when user is logged on"** must be selected so that the system tray icon (`NotifyIcon`) renders in Session 1+.
  * Option **"Run with highest privileges"** must be enabled for hardware access (RyzenAdj/TDP/Power control).
  * See full details in `task-scheduler-autostart` skill: [.agents/skills/task-scheduler-autostart/SKILL.md](file:///c:/Users/andre/git/XboxGamingBar/.agents/skills/task-scheduler-autostart/SKILL.md).

## Helper & UWP AppService Resilience
* `CouchGamingBarHelper.exe` can be run standalone or packaged.
* Never assume `Package.Current` is available without `try-catch` exception handling, as accessing `Package.Current` unpackaged throws `InvalidOperationException`.
* Never block the helper main startup loop waiting for UWP AppService connections (`ConnectToWidget(false)` should be non-blocking).
* **No Arbitrary Delays in Widget**: `GamingWidget` must not use fixed delay timers (such as `Task.Delay(1000)`) to wait for helper reconnection. It must reactively subscribe to `App.AppServiceConnected` and `App.AppServiceDisconnected` events.
* **Helper Object Re-instantiation on Retry**: Windows UWP `AppServiceConnection` instances cannot be reused after `OpenAsync()` fails. `CouchGamingBarHelper` must dispose old connection handles and initialize a fresh `AppServiceConnection` object (`RecreateConnection()`) before retrying, pacing retries (e.g. 2000ms) to avoid CPU/log churn.

