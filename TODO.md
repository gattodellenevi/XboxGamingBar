# Project Backlog & TODOs

## Performance & Optimization

- [x] **Optimize Window Enumeration & Process Path Query in Helper**
  - **File:** `Samples/XboxGamingBarHelper/Windows/User32.cs` (`GetOpenWindows`)
  - **Goal:** Eliminate heavy `Process.GetProcessById(processId)` allocations and `process.MainModule.FileName` Win32 handle queries running every loop tick.
  - **Status:** Completed using hybrid in-memory caching (`PID -> CachedProcessInfo`) combined with native Win32 `QueryFullProcessImageName` (`PROCESS_QUERY_LIMITED_INFORMATION`) and periodic cache pruning.

- [ ] **Unified Startup Snapshot DTO / Batch State Sync**
  - **Files:** `Samples/Shared/Data/`, `Samples/XboxGamingBar/Data/WidgetProperties.cs`, `Samples/XboxGamingBarHelper/Program.cs`
  - **Goal:** Replace the sequential 25+ IPC `properties.Sync()` round-trips over the AppService pipe with a single `GetInitialState` / `StateSnapshotDTO` IPC request on widget connect.
  - **Details:** The widget sends a single snapshot request on connection, the helper returns a unified struct/JSON containing all current settings, hardware stats, and available resolutions in one payload, and the widget populates all properties in a single batch on the UI thread before entering reactive delta-update mode.
  - **Status:** Backlog / Planned.
