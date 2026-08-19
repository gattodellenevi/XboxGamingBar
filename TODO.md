# Project Backlog & TODOs

## Performance & Optimization

- [x] **Optimize Window Enumeration & Process Path Query in Helper**
  - **File:** `Samples/XboxGamingBarHelper/Windows/User32.cs` (`GetOpenWindows`)
  - **Goal:** Eliminate heavy `Process.GetProcessById(processId)` allocations and `process.MainModule.FileName` Win32 handle queries running every loop tick.
  - **Status:** Completed using hybrid in-memory caching (`PID -> CachedProcessInfo`) combined with native Win32 `QueryFullProcessImageName` (`PROCESS_QUERY_LIMITED_INFORMATION`) and periodic cache pruning.
