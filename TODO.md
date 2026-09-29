# Project Backlog & TODOs

## Performance & Optimization

- [x] **Optimize Window Enumeration & Process Path Query in Helper**
  - **Files:** [`Samples/XboxGamingBarHelper/Windows/User32.cs`](file:///Users/zanchia/gitpe/XboxGamingBar/Samples/XboxGamingBarHelper/Windows/User32.cs) (`GetOpenWindows`)
  - **Goal:** Eliminate heavy `Process.GetProcessById(processId)` allocations and `process.MainModule.FileName` Win32 handle queries running every loop tick.
  - **Status:** Completed using hybrid in-memory caching (`PID -> CachedProcessInfo`) combined with native Win32 `QueryFullProcessImageName` (`PROCESS_QUERY_LIMITED_INFORMATION`) and periodic cache pruning.

---

## Architectural Pitfalls & Structural Refactoring

- [x] **1. Inverted Lifecycle & Fragile IPC Architecture (Event-Driven AppService Handshake — Option B)**
  - **Files:**
    - [`Samples/Shared/Constants/StringConstants.cs`](file:///Users/zanchia/gitpe/XboxGamingBar/Samples/Shared/Constants/StringConstants.cs)
    - [`Samples/Shared/Utilities/WidgetSignalHelper.cs`](file:///Users/zanchia/gitpe/XboxGamingBar/Samples/Shared/Utilities/WidgetSignalHelper.cs)
    - [`Samples/XboxGamingBarHelper/Program.cs`](file:///Users/zanchia/gitpe/XboxGamingBar/Samples/XboxGamingBarHelper/Program.cs)
    - [`Samples/XboxGamingBar/GamingWidget.xaml.cs`](file:///Users/zanchia/gitpe/XboxGamingBar/Samples/XboxGamingBar/GamingWidget.xaml.cs)
    - [`Samples/Shared/Data/FunctionalProperty.cs`](file:///Users/zanchia/gitpe/XboxGamingBar/Samples/Shared/Data/FunctionalProperty.cs)
    - [`Samples/XboxGamingBar/App.xaml.cs`](file:///Users/zanchia/gitpe/XboxGamingBar/Samples/XboxGamingBar/App.xaml.cs)
  - **Remediation Completed (Option B):**
    - Created cross-process `Global\CouchGamingBar_WidgetActive_Event` with `ALL APPLICATION PACKAGES` (`S-1-15-2-1`) DACL.
    - Helper registers non-blocking `ThreadPool.RegisterWaitForSingleObject` to listen passively for widget activation signals.
    - Completely removed the perpetual 2,000ms polling loop and `RecreateConnection()` churn from `Program.MainLoopAsync` when Game Bar is idle.
    - Widget reactively signals `WidgetSignalHelper.SignalWidgetActive()` when entering foreground or upon disconnect while active.
    - Wrapped all IPC messaging and synchronization in `FunctionalProperty.cs` in robust try-catch blocks to prevent unhandled `async void` exceptions.
    - Fixed memory leak / reference clearing bug in `App.xaml.cs` (`gamingWidgetSettings`).
  - **Status:** Completed (Option B).

- [ ] **2. Shattered State Synchronization, N+1 Roundtrip Latency & Split-Brain Game Detection**
  - **Files:**
    - [`Samples/Shared/Data/FunctionalProperties.cs`](file:///Users/zanchia/gitpe/XboxGamingBar/Samples/Shared/Data/FunctionalProperties.cs#L40-L96)
    - [`Samples/Shared/Data/GenericProperty.cs`](file:///Users/zanchia/gitpe/XboxGamingBar/Samples/Shared/Data/GenericProperty.cs#L150-L201)
    - [`Samples/XboxGamingBar/Data/WidgetProperties.cs`](file:///Users/zanchia/gitpe/XboxGamingBar/Samples/XboxGamingBar/Data/WidgetProperties.cs#L20-L27)
    - [`Samples/XboxGamingBar/GamingWidget.xaml.cs`](file:///Users/zanchia/gitpe/XboxGamingBar/Samples/XboxGamingBar/GamingWidget.xaml.cs#L94-L194)
    - [`Samples/XboxGamingBarHelper/Systems/SystemManager.cs`](file:///Users/zanchia/gitpe/XboxGamingBar/Samples/XboxGamingBarHelper/Systems/SystemManager.cs#L205-L347)
  - **Problem:** State is fragmented across ~35 individual `FunctionalProperty` instances. On widget wake-up (`LeavingBackground`), `properties.Sync()` executes 35 sequential IPC round-trips over the AppService pipe, creating noticeable latency on overlay open. Setting properties triggers bidirectional echo loops guarded only by fragile timestamp comparisons. Game detection is split between UWP's `XboxGameBarAppTargetTracker` and the Helper's `User32.GetOpenWindows` + RTSS hooks, causing profile conflicts. UI controls are directly coupled to IPC transport, bypassing MVVM.
  - **Action Items:**
    - Implement a unified **Startup Snapshot DTO / Batch State Sync** (`GetInitialState` payload) returning all settings, resolutions, and hardware stats in a single IPC request when the widget opens.
    - Implement unidirectional delta streams (Helper -> Widget) to eliminate bidirectional echo loops.
    - Unify game detection authority: establish the helper as the authoritative source of truth, taking tracker hints from the widget without race conditions or overwriting.
    - Decouple XAML UI controls from IPC properties by adopting clean MVVM ViewModels with data bindings.
  - **Status:** Backlog / High Priority.

- [ ] **3. Execution Context & Storage Root Schism (MSIX Isolation vs. Staged Autostart)**
  - **Files:**
    - [`Samples/XboxGamingBarHelper/Utilities/PathHelper.cs`](file:///Users/zanchia/gitpe/XboxGamingBar/Samples/XboxGamingBarHelper/Utilities/PathHelper.cs#L12-L28)
    - [`Samples/Register-AutostartTask.ps1`](file:///Users/zanchia/gitpe/XboxGamingBar/Samples/Register-AutostartTask.ps1#L40-L60)
    - [`Samples/XboxGamingBarHelper/Settings/SettingsManager.cs`](file:///Users/zanchia/gitpe/XboxGamingBar/Samples/XboxGamingBarHelper/Settings/SettingsManager.cs#L57-L76)
    - [`Samples/XboxGamingBarHelper/Profile/ProfileManager.cs`](file:///Users/zanchia/gitpe/XboxGamingBar/Samples/XboxGamingBarHelper/Profile/ProfileManager.cs#L100-L108)
    - [`STORE_VS_GITHUB.md`](file:///Users/zanchia/gitpe/XboxGamingBar/STORE_VS_GITHUB.md)
  - **Problem:** To enable elevated logon autostart, `Register-AutostartTask.ps1` stages the helper into `%LOCALAPPDATA%\CouchGamingBarHelper` outside `WindowsApps`. `PathHelper.GetLocalFolderPath()` resolves to `ApplicationData.Current.LocalFolder` when run packaged (via `FullTrustProcessLauncher`), but falls back to `%LOCALAPPDATA%\CouchGamingBar` when run unpackaged (via Task Scheduler). As a result, settings and per-game profiles are saved to two different disk locations depending on how the helper was launched. Furthermore, Store sandbox restrictions (`#if STORE`, `asInvoker`, no `LibreHardwareMonitorLib`) maintain a bifurcated architecture in a single codebase.
  - **Action Items:**
    - Standardize configuration and profile persistence unconditionally to `%LOCALAPPDATA%\CouchGamingBar` regardless of whether `ApplicationData.Current` is available, ensuring identical state across both launch mechanisms.
    - Cleanly encapsulate Store vs. GitHub differences behind abstract provider factories rather than widespread `#if STORE` preprocessor directives.
  - **Status:** Backlog / Medium Priority.

