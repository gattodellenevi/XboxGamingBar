---
name: hidden-widget-features
description: Explains hidden and conditionally loaded features in the XboxGamingBar widget, including AMD ADLX checks, Store build restrictions, and OSD providers.
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

## 2. On-Screen Display (OSD) / Performance Overlay
* **XAML Element**: `PerformanceOverlaySlider` in [GamingWidget.xaml](file:///c:/Users/andre/git/XboxGamingBar/Samples/XboxGamingBar/GamingWidget.xaml#L108)
* **Property Controller**: `OnScreenDisplayProviderInstalledProperty` in [GamingWidget.xaml.cs](file:///c:/Users/andre/git/XboxGamingBar/Samples/XboxGamingBar/GamingWidget.xaml.cs#L68)
* **Check Logic**: Verifies whether a compatible OSD provider (such as RTSS or internal provider) is installed on the system (`Function.Settings_OnScreenDisplayProviderInstalled`).

## 3. Conditional Controls & Dynamic Limiting
* **XAML Elements**: [GamingWidget.xaml](file:///c:/Users/andre/git/XboxGamingBar/Samples/XboxGamingBar/GamingWidget.xaml#L120-L190)
* **Handlers**: [GamingWidget.xaml.cs](file:///c:/Users/andre/git/XboxGamingBar/Samples/XboxGamingBar/GamingWidget.xaml.cs#L646-L696)
* **Behavior**:
  * **CPU EPP & CPU Clock Limit**: Sliders (`CPUEPPSlider`, `CPUClockMaxSlider`) remain collapsed until their parent toggles (`SetCPUEPPToggle`, `LimitCPUClockToggle`) are turned on.
  * **FPS Limiters**: Toggle switches between standard `FPSLimitSlider` and `FPSLimitJudderFreeSlider` / `FPSLimitJudderFreeCanvas` based on `LimitFPSToggle.IsOn` and `JudderFreeFPSToggle.IsOn`.
