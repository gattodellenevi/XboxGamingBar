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
    - [x] **Optimistic Local Cache Rendering**: Instantly render the widget using cached last-known values from `ApplicationData.Current.LocalSettings` without blocking modal dialogs. Seamlessly reconcile live state when `StateSnapshotDTO` arrives over AppService. Synchronous inline UI updating on UI thread avoids dispatcher lag.

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

- [ ] **4.1. Event-Driven Active Game & Window Detection**
  - **Files:** `Samples/XboxGamingBarHelper/Systems/SystemManager.cs`, `Samples/XboxGamingBarHelper/Windows/User32.cs`
  - **Task:**
    - Replace the 1,000ms `GetRunningGame()` window scanning loop with native Win32 `SetWinEventHook` listening for `EVENT_SYSTEM_FOREGROUND`.
    - Evaluate game process profiles only when the foreground window changes (0% idle CPU overhead).

- [ ] **4.2. Native OS Event Handlers for Display & Power**
  - **Files:** `Samples/XboxGamingBarHelper/Systems/SystemManager.cs`, `Samples/XboxGamingBarHelper/RTSS/RTSSManager.cs`
  - **Task:**
    - Rely entirely on `SystemEvents.DisplaySettingsChanged` for display resolution, refresh rate, and HDR mode updates.
    - Rely on `SystemEvents.PowerModeChanged` to re-apply profiles immediately upon resume without polling.

- [ ] **4.3. On-Demand Telemetry Subscription Model**
  - **Files:** `Samples/XboxGamingBarHelper/Hardware/HardwareManager.cs`, `Samples/XboxGamingBarHelper/Program.cs`
  - **Task:**
    - Turn hardware telemetry into an on-demand subscription push model.
    - Run hardware sensor timers (1,000ms) only when the widget is in the foreground OR when OnScreenDisplay level > 0.
    - Completely stop sensor polling loops when both the widget and OSD are idle.

- [ ] **4.4. Reactive Connection & Elevation State Machine**
  - **Files:** `Samples/XboxGamingBar/GamingWidget.xaml.cs`, `Samples/XboxGamingBarHelper/Program.cs`
  - **Task:**
    - Remove the helper's 2,000ms reconnect polling loop; handle disconnections purely via `Connection_ServiceClosed`.
    - Replace the widget's `Task.Delay(15000)` arbitrary elevation timeout with a reactive state machine listening to `AppServiceConnected`. Unlock UI the exact millisecond the elevated process connects.

---

## 5. UI Modernization & MVVM Separation

- [ ] **5.1. Decouple Monolithic `GamingWidget.xaml.cs` into ViewModels**
  - **Files:** `Samples/XboxGamingBar/GamingWidget.xaml.cs`, `Samples/XboxGamingBar/ViewModels/`
  - **Problem:** `GamingWidget.xaml.cs` currently has 2,785 lines mixing navigation, gamepad input, theme changes, judder-free math, and direct UI manipulation.
  - **Task:**
    - Break logic into modular ViewModels:
      - `QuickAccessViewModel`: HUD cards, battery, quick toggles.
      - `DisplayViewModel`: Resolution, refresh rates, HDR.
      - `PerformanceViewModel`: RTSS limits, judder-free calculations, OSD levels and text size.
      - `AMDFeaturesViewModel`: RSR, Anti-Lag, Boost, Chill, AFMF.
      - `SettingsViewModel`: OSD provider, GPU target, elevation status.

- [ ] **5.2. Compiled `{x:Bind}` Data Binding**
  - **Files:** `Samples/XboxGamingBar/GamingWidget.xaml`, `Samples/XboxGamingBar/Data/`
  - **Task:**
    - Replace manual event wireups (`ToggleSwitch.Toggled += ...`, `Dispatcher.RunAsync(...)`) with compiled `{x:Bind Mode=OneWay}` and `{x:Bind Mode=TwoWay}` bindings.
    - Eliminate boilerplate wrapper property classes in `Data/` in favor of standard `INotifyPropertyChanged` ViewModel properties.
