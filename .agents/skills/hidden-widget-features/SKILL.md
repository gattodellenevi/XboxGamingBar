---
name: hidden-widget-features
description: Explains hidden and conditionally loaded features in the XboxGamingBar widget, including AMD ADLX checks, TDP/RyzenAdj checks, Store build restrictions, and OSD providers.
---

# Hidden and Conditionally Loaded Features in XboxGamingBar

This document collates all hidden UI components and features in the XboxGamingBar widget that remain collapsed (`Visibility="Collapsed"`) until specific hardware, driver, or build checks pass at runtime.

## 1. AMD Radeon Settings Tab & ADLX Features
* **XAML Element**: `AMDPivotItem`, `AMDPivotItemStackPanel` in [GamingWidget.xaml](file:///c:/Users/andre/git/XboxGamingBar/Samples/XboxGamingBar/GamingWidget.xaml#L240)
* **Backend Handler**: [AMDManager.cs](file:///c:/Users/andre/git/XboxGamingBar/Samples/XboxGamingBarHelper/AMD/AMDManager.cs#L250-L261)
* **Property Controller**: [AMDSettingsSupportedProperty.cs](file:///c:/Users/andre/git/XboxGamingBar/Samples/XboxGamingBar/Data/AMDSettingsSupportedProperty.cs)
* **Check Logic**:
  * On launch, the helper calls `ADLXHelper.Initialize()` to query the AMD Display Library Extra (ADLX).
  * If ADLX fails (e.g. non-AMD GPU or outdated driver), `AMDPivotItem` stays hidden.
  * If ADLX succeeds, `AMDSettingsSupportedProperty` (`Function.Support_AMDSettings`) sets `AMDPivotItem.Visibility = Visible`.
* **Individual Feature Checks**:
  * Sub-features (Radeon Super Resolution, Fluid Motion Frames, Anti-Lag, Radeon Boost, Radeon Chill) are checked individually via driver support APIs (`AMDRadeonSuperResolutionSupported`, etc.).
  * Fine-tuning controls (e.g. Sharpness slider, Boost resolution slider, Chill min/max FPS) become visible only when their parent feature toggle is switched ON (`IsOn="True"`).

## 2. TDP (Thermal Design Power) Tuning Controls
* **XAML Element**: `TDPHeaderGrid`, `TDPSlider` in [GamingWidget.xaml](file:///c:/Users/andre/git/XboxGamingBar/Samples/XboxGamingBar/GamingWidget.xaml#L103)
* **Backend Handler**: [HardwareManager.cs](file:///c:/Users/andre/git/XboxGamingBar/Samples/XboxGamingBarHelper/Hardware/HardwareManager.cs#L207-L225)
* **Property Controller**: [TDPControlSupportProperty.cs](file:///c:/Users/andre/git/XboxGamingBar/Samples/XboxGamingBar/Data/TDPControlSupportProperty.cs)
* **Check Logic**:
  * **Compile-Time Check**: `#if !STORE` preprocessor directive. If built with the `STORE` flag for Microsoft Store release, `RyzenAdj` is disabled completely due to Microsoft Store policy restrictions.
  * **Runtime Check**: If `#if !STORE`, `RyzenAdj.init_ryzenadj()` is called. If successful, `tdpControlSupport` sets `TDPHeaderGrid.Visibility = Visible`. Otherwise, it remains hidden.

## 3. On-Screen Display (OSD) / Performance Overlay
* **XAML Element**: `PerformanceOverlaySlider` in [GamingWidget.xaml](file:///c:/Users/andre/git/XboxGamingBar/Samples/XboxGamingBar/GamingWidget.xaml#L108)
* **Property Controller**: `OnScreenDisplayProviderInstalledProperty` in [GamingWidget.xaml.cs](file:///c:/Users/andre/git/XboxGamingBar/Samples/XboxGamingBar/GamingWidget.xaml.cs#L68)
* **Check Logic**: Verifies whether a compatible OSD provider (such as RTSS or internal provider) is installed on the system (`Function.Settings_OnScreenDisplayProviderInstalled`).

## 4. Conditional Controls & Dynamic Limiting
* **XAML Elements**: [GamingWidget.xaml](file:///c:/Users/andre/git/XboxGamingBar/Samples/XboxGamingBar/GamingWidget.xaml#L120-L190)
* **Handlers**: [GamingWidget.xaml.cs](file:///c:/Users/andre/git/XboxGamingBar/Samples/XboxGamingBar/GamingWidget.xaml.cs#L646-L696)
* **Behavior**:
  * **CPU EPP & CPU Clock Limit**: Sliders (`CPUEPPSlider`, `CPUClockMaxSlider`) remain collapsed until their parent toggles (`SetCPUEPPToggle`, `LimitCPUClockToggle`) are turned on.
  * **FPS Limiters**: Toggle switches between standard `FPSLimitSlider` and `FPSLimitJudderFreeSlider` / `FPSLimitJudderFreeCanvas` based on `LimitFPSToggle.IsOn` and `JudderFreeFPSToggle.IsOn`.
