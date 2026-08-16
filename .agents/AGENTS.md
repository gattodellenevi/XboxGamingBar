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
* **Single-Instance Helper & Widget Launch Prevention**: The helper process must use `@"Global\CouchGamingBarHelper_SingleInstance_Mutex"` for single-instance protection. Before `GamingWidget` invokes `FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync()`, it must check if the global single-instance mutex already exists (`IsHelperProcessRunning()`, catching `UnauthorizedAccessException` for elevated helper processes). If the mutex exists, `GamingWidget` must skip calling `FullTrustProcessLauncher` and instead passively await connection from the running helper.

## Helper CPU & Performance Optimization
* **Expected CPU Baseline**: Normal baseline CPU usage for `CouchGamingBarHelper.exe` is **< 0.5% average** (0.0%–0.2% idle, 0.1%–1.0% with active OSD/telemetry).
* **Main Loop Pacing**: Default main loop delay is 500ms (`Task.Delay(500)` in `Program.cs`).
* **CPU Reduction Strategies**:
  * **Increase Loop Delay**: Increase `Task.Delay(500)` to `1000ms`+ in `Program.cs` to halve telemetry query frequency.
  * **Adaptive Sleep / Dynamic Polling Rate**: Use longer delay (e.g. `3000ms`) when idle (no active game/OSD) and `1000ms` when gaming or OSD is enabled.
  * **Conditional Telemetry Updates**: Skip `hardwareProvider.Update()` in `HardwareManager.cs` if `onScreenDisplayLevel == 0` and no UWP app service is connected.
  * **NLog Disk Logging Verbosity**: Set `NLog.config` to `Info`/`Warn` to avoid disk I/O CPU spikes.
  * **Disable OSD**: Set `OnScreenDisplay` level to 0 in settings when overlays are not needed to halt RTSS/ADLX buffer updates.

## WinRing0 & TDP Control Removal
* **Permanent Removal**: TDP control, `RyzenAdj` (`libryzenadj.dll`), and `WinRing0` (`WinRing0x64.sys` / `WinRing0x64.dll` / `inpoutx64.dll`) have been completely removed from the project to eliminate all Windows Defender vulnerable driver blocklist warnings (CVE-2020-14979).
* CPU power management in CouchGameBar now relies strictly on native Windows Power Scheme APIs (CPU Boost, Energy Performance Preference / EPP, and CPU Max Clock limits).

## Widget UI & Visual Design Guidelines
* **Fluent 2 Card-Based Architecture**: `GamingWidget.xaml` uses controller-first Fluent 2 card layout:
  * Setting groups are enclosed in rounded cards using `SettingsCardStyle` (`CornerRadius="8"`, `CardBackgroundFillColorDefaultBrush`, `CardStrokeColorDefaultBrush`, `Padding="14,12,14,12"`, `Margin="10,0,10,10"`).
  * Group headers use `SectionHeaderTextStyle` (`FontSize="11"`, `FontWeight="Bold"`, `CharacterSpacing="50"`).
  * Control subtitles use `SubtitleTextStyle` (`FontSize="12"`, `SystemControlForegroundBaseMediumBrush`).
  * Hero game card at the top hosts `RunningGameText` and `PerGameProfileToggle` in a `CornerRadius="10"` container with controller icon badge (`&#xE7FC;`).
  * Tab bumper tags (`LT`/`RT`) use rounded pill borders (`CornerRadius="6"`).
* **Code-Behind & Binding Integrity**:
  * Always preserve all exact `x:Name` properties (e.g. `AMDRadeonSuperResolutionText`, `FPSLimitSlider`, `JudderFreeFPSToggle`, etc.) as they are directly referenced by widget property classes in `Data/` and event subscriptions.
  * Maintain XY focus navigation bindings (`XYFocusUp`, `XYFocusDown`, `XYFocusLeft`, `XYFocusRight`) across card elements for controller navigation.



