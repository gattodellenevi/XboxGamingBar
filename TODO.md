# Project Backlog & Architectural Refactoring TODOs

## 1. Setting Architecture: Clean Interface & Universal Persistence (Global vs. Per-Profile)

- [x] **1.1. Descriptor & Property-Bag Architecture (`ISettingDefinition<T>`)**
  - **Files:** `Samples/Shared/Data/`, `Samples/Shared/Enums/Function.cs`, `Samples/XboxGamingBarHelper/Settings/`
  - **Status:** **Completed**
  - **Implemented:**
    - `ISettingDefinition` and `ISettingDefinition<T>` generic interface with `SettingScope` (`GlobalOnly`, `ProfileOrGlobal`, `Transient`).
    - `SettingDefinition<T>` with built-in sanitization and type conversion.
    - Centralized `SettingsRegistry` for clean 1-line definition of global and per-profile settings.
    - Dynamic dictionary property bag in `GameProfile.cs` with `TryGetSetting<T>()` and `SetSetting<T>()` preserving 100% backward compatibility.

- [x] **1.2. Dynamic ProfileStore & Automatic Inheritance Engine**
  - **Files:** `Samples/XboxGamingBarHelper/Profile/ProfileStore.cs`, `Samples/XboxGamingBarHelper/Profile/ProfileManager.cs`, `Samples/XboxGamingBarHelper/Settings/SettingsManager.cs`
  - **Status:** **Completed**
  - **Implemented:**
    - `ProfileStore` managing JSON storage (`settings.json`, `global.json`, `profiles/*.json`) with automatic inheritance (game override -> global profile fallback -> setting default).
    - Transparent migration from legacy `settings.xml` and `profiles/*.xml` with continuous legacy XML fallback writing.
    - Integrated with `SettingsManager`, `ProfileManager`, and `Program.cs`.

---

## 2. Fast Startup Optimization (Helper & Widget)

- [x] **2.1. Helper Startup Fast-Path**
  - **Files:** `Samples/XboxGamingBarHelper/Program.cs`, `Samples/XboxGamingBarHelper/Hardware/HardwareManager.cs`, `Samples/XboxGamingBarHelper/AMD/AMDManager.cs`, `Samples/XboxGamingBarHelper/Windows/User32.cs`
  - **Tasks:**
    - [x] **Eliminate `PackageManager.FindPackagesForUser`**: Cached `PackageFamilyName` in `%LOCALAPPDATA%\CouchGamingBarHelper\package_identity.txt` (written by `Register-AutostartTask.ps1` and refreshed by helper) to read directly (0ms) and bypass multi-second package manager scan.
    - [x] **Deferred / Lazy Hardware Provider Initialization**: Moved `LibreHardwareProvider` WMI/ACPI probing and native ADLX DLL binding to a background `Task.Run` so the helper opens the AppService connection immediately.
    - [x] **Background Display Mode Enumeration**: Moved `EnumDisplaySettings` loops in `User32.cs` to a background task with thread-safe caching; updated on display change events.

- [x] **2.2. Widget Startup Fast-Path**
  - **Files:** `Samples/XboxGamingBar/Data/WidgetProperties.cs`, `Samples/XboxGamingBar/GamingWidget.xaml.cs`, `Samples/XboxGamingBar/Data/*Property.cs`
  - **Tasks:**
    - [x] **Optimistic Local Cache Rendering**: Instantly render the widget using cached last-known values from `cached_snapshot.json` in `LocalFolder` without blocking modal dialogs (bypassing UWP's 8 KB `LocalSettings` limitation). Seamlessly reconcile live state when `StateSnapshotDTO` arrives over AppService. Robust UI thread marshaling (`CoreDispatcher`) and guaranteed modal dismissal on connection.

---

## 3. State Consistency & Synchronization (Widget <-> Helper)

- [x] **3.1. Unidirectional Data Flow (Helper as Single Source of Truth)**
  - **Files:** `Samples/Shared/Data/`, `Samples/XboxGamingBar/Data/WidgetProperties.cs`, `Samples/XboxGamingBarHelper/Core/HelperProperty.cs`
  - **Task:**
    - Helper is the sole authority for persisted settings, active profile state, and hardware capabilities.
    - Widget acts strictly as a presentation and intent layer:
      - Widget sends intent: `Command.Set`, `Function`, `Value`.
      - Helper validates, commits to `ProfileStore`, and notifies subscribers with confirmed value.
      - Widget reconciles UI controls using confirmed backend response values under a batch update scope to eliminate echo loops.

- [x] **3.2. Instance-Level Re-Entrancy Tokens**
  - **Files:** `Samples/Shared/Data/Property.cs`, `Samples/Shared/Data/IProperty.cs`, `Samples/Shared/Data/FunctionalProperty.cs`, `Samples/XboxGamingBar/Data/WidgetProperties.cs`
  - **Task:**
    - Replaced fragile static `Property.SuppressNotification` flag with thread-safe instance-level re-entrancy tokens using `IDisposable BeginBatchUpdate()`.
    - Prevents cross-thread race conditions and echo loops during snapshot applications and multi-property updates.

- [x] **3.3. Decouple Hardware Readiness from Saved Settings**
  - **Files:** `Samples/XboxGamingBarHelper/RTSS/RTSSManager.cs`, `Samples/XboxGamingBarHelper/AMD/AMDManager.cs`
  - **Task:**
    - Decoupled hardware readiness from persisted user intent. Saved user profile settings (FPS limit, RSR, Anti-Lag, etc.) are never overwritten or clamped to `0`/`false` while RTSS or ADLX is offline or initializing.
    - Saved profile intent is automatically applied once the hardware interface completes initialization.

---

## 4. Replacing Wait/Check Loops with an Event-Driven Architecture

- [x] **4.1. Event-Driven Active Game & Window Detection**
  - **Files:** `Samples/XboxGamingBarHelper/Systems/SystemManager.cs`, `Samples/XboxGamingBarHelper/Windows/User32.cs`
  - **Task:**
    - Replaced the 1,000ms `GetRunningGame()` window scanning loop with native Win32 `SetWinEventHook` listening for `EVENT_SYSTEM_FOREGROUND` via a dedicated STA message loop thread.
    - Profiles and active window states are evaluated only upon foreground window changes (0% idle CPU overhead).
    - Lightweight liveness checks (`User32.IsProcessAlive`) ensure exited games are cleaned up without full window enumeration.

- [x] **4.2. Native OS Event Handlers for Display & Power**
  - **Files:** `Samples/XboxGamingBarHelper/Systems/SystemManager.cs`, `Samples/XboxGamingBarHelper/RTSS/RTSSManager.cs`
  - **Task:**
    - Rely entirely on `SystemEvents.DisplaySettingsChanged` for debounced display resolution, refresh rate, and HDR mode updates; eliminated 3,000ms periodic CCD polling.
    - Subscribed `RTSSManager` to `SystemEvents.PowerModeChanged` (`PowerModes.Resume`) to re-apply active FPS limits immediately upon system resume without polling.
    - Optimized RTSS elevation status checks to run only on process state transitions.

- [x] **4.3. On-Demand Telemetry Subscription Model**
  - **Files:** `Samples/XboxGamingBarHelper/Hardware/HardwareManager.cs`, `Samples/XboxGamingBarHelper/Program.cs`
  - **Task:**
    - Converted hardware telemetry to an on-demand subscription model using `hardwareManager.IsDemandActive`.
    - Sensor polling and WMI/LibreHardwareMonitor updates run only when OnScreenDisplay level > 0 OR when the widget is actively in the foreground with an open AppService connection.
    - Hardware sensor polling completely halts (0% CPU) when both the widget and OSD are idle.

- [x] **4.4. Reactive Connection & Elevation State Machine**
  - **Files:** `Samples/XboxGamingBar/GamingWidget.xaml.cs`, `Samples/XboxGamingBarHelper/Program.cs`
  - **Task:**
    - Eliminated the helper's 2,000ms reconnect polling loop. Implemented a native global event handle (`@"Global\CouchGamingBarHelper_ConnectEvent"`) with `ALL APPLICATION PACKAGES` ACL and `ThreadPool.RegisterWaitForSingleObject` so the widget reactively signals the running helper to connect instantly.
    - Replaced the widget's `Task.Delay(15000)` arbitrary timeout with a reactive state machine using a cancelable fallback token; cancels timeout and unlocks UI the exact millisecond the elevated process connects via `AppServiceConnected`.

---

## 5. UI Modernization & MVVM Separation

- [x] **5.1. Decouple Monolithic `GamingWidget.xaml.cs` into ViewModels**
  - **Files:** `Samples/XboxGamingBar/GamingWidget.xaml.cs`, `Samples/XboxGamingBar/ViewModels/`
  - **Task:**
    - Decoupled logic into modular domain ViewModels inheriting `ViewModelBase` with thread-safe UI marshaling via `CoreDispatcher`:
      - `QuickAccessViewModel`: HUD cards, battery, quick toggles (OSD, FPS limit, HDR), live telemetry vitals.
      - `DisplayViewModel`: Resolution, refresh rates, HDR.
      - `PerformanceViewModel`: RTSS limits, judder-free calculations (`CalculateMarkers`), OSD levels and text size, RTSS elevation sync.
      - `AMDFeaturesViewModel`: RSR, Anti-Lag, Boost, Chill, AFMF.
      - `SettingsViewModel`: OSD provider, GPU target, elevation status, mouse settings.
      - `MainViewModel`: Aggregates the domain ViewModels and game profile tracking state.

- [x] **5.2. Compiled `{x:Bind}` Data Binding**
  - **Files:** `Samples/XboxGamingBar/GamingWidget.xaml`, `Samples/XboxGamingBar/Data/HardwareTelemetryProperty.cs`, `Samples/XboxGamingBar/GamingWidget.xaml.cs`
  - **Task:**
    - Replaced manual element string manipulation with compiled `{x:Bind Mode=OneWay}` data bindings across the Quick Access HUD vitals, Performance tab telemetry, active game detection, and display/RTSS summaries.
    - Wired `HardwareTelemetryProperty.OnTelemetryUpdated` directly to `ViewModel.QuickAccess.UpdateTelemetry`, cleanly driving UI updates via standard `INotifyPropertyChanged` notifications.
    - Preserved all required `x:Name` references and container-level `XYFocusKeyboardNavigation` per project guidelines.
