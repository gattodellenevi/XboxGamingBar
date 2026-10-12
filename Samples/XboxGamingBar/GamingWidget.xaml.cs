using Microsoft.Gaming.XboxGameBar;
using NLog;
using Shared.Data;
using Shared.Enums;
using Shared.Utilities;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Input;
using Windows.System;
using XboxGamingBar.Data;
using XboxGamingBar.Event;
using XboxGamingBar.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace XboxGamingBar
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GamingWidget : Page
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly List<string> BlackListAppTrackerNames = new List<string>()
        {
            "App Installer", //Somehow App Installer shows up as a game sometimes
        };

        // Xbox Game Bar logic
        private XboxGameBarWidget widget = null;
        private XboxGameBarWidgetActivity widgetActivity = null;
        private readonly object widgetActivityLock = new object();
        public XboxGameBarWidgetActivity WidgetActivity { get { return widgetActivity; } }
        private XboxGameBarAppTargetTracker appTargetTracker = null;

        private SolidColorBrush widgetDarkThemeBrush = null;
        private SolidColorBrush widgetLightThemeBrush = null;

        // Properties
        private readonly OSDProperty osd;
        private readonly RunningGameProperty runningGame;
        private readonly PerGameProfileProperty perGameProfile;
        private readonly LimitFPSProperty limitFPS;
        private readonly RefreshRatesProperty refreshRates;
        private readonly RefreshRateProperty refreshRate;
        private readonly ResolutionProperty resolution;
        private readonly ResolutionsProperty resolutions;
        private readonly HDRProperty hdr;
        private readonly HDRSupportedProperty hdrSupported;
        private readonly TrackedGameProperty trackedGame;
        private readonly OnScreenDisplayProviderInstalledProperty onScreenDisplayProviderInstalled;
        private readonly IsForegroundProperty isForeground;
        private readonly FocusingOnOSDSliderProperty focusingOnOSDSlider;

        // AMD properties
        private readonly AMDSettingsSupportedProperty amdSettingsSupported;
        private readonly AMDRadeonSuperResolutionEnabledProperty amdRadeonSuperResolutionEnabled;
        private readonly AMDRadeonSuperResolutionSupportedProperty amdRadeonSuperResolutionSupported;
        private readonly AMDRadeonSuperResolutionSharpnessProperty amdRadeonSuperResolutionSharpness;
        private readonly AMDFluidMotionFrameEnabledProperty amdFluidMotionFrameEnabled;
        private readonly AMDFluidMotionFrameSupportedProperty amdFluidMotionFrameSupported;
        private readonly AMDRadeonAntiLagEnabledProperty amdRadeonAntiLagEnabled;
        private readonly AMDRadeonAntiLagSupportedProperty amdRadeonAntiLagSupported;
        private readonly AMDRadeonBoostEnabledProperty amdRadeonBoostEnabled;
        private readonly AMDRadeonBoostSupportedProperty amdRadeonBoostSupported;
        private readonly AMDRadeonBoostResolutionProperty amdRadeonBoostResolution;
        private readonly AMDRadeonChillEnabledProperty amdRadeonChillEnabled;
        private readonly AMDRadeonChillSupportedProperty amdRadeonChillSupported;
        private readonly AMDRadeonChillMinFPSProperty amdRadeonChillMinFPSProperty;
        private readonly AMDRadeonChillMaxFPSProperty amdRadeonChillMaxFPSProperty;

        private readonly FPSLimitProperty fpsLimit;
        private readonly FPSLimitModeProperty fpsLimitMode;
        private readonly JudderFreeFPSProperty judderFreeFPS;
        private readonly OSDTextSizeProperty osdTextSize;
        private readonly HelperElevationProperty helperElevation;
        private readonly RTSSElevationProperty rtssElevation;
        private readonly HardwareProviderProperty hardwareProviderProperty;
        private readonly HardwareTelemetryProperty hardwareTelemetry;
        private bool isRestartingElevated = false;
        private CancellationTokenSource elevationFallbackCts = null;
        private CancellationTokenSource modalTimeoutCts = null;
        private readonly GameInputStatusProperty gameInputStatus;
        private readonly GpuTargetProperty gpuTarget;
        private readonly DualGpuSupportProperty dualGpuSupport;
        private readonly ControllerBatteryProperty controllerBattery;
        private readonly MouseSpeedProperty mouseSpeed;
        private readonly MouseScrollSpeedProperty mouseScrollSpeed;
        private readonly MouseEnhancePointerPrecisionProperty mouseEnhancePointerPrecision;

        public bool IsElevationMismatch => App.Connection != null &&
                                           rtssElevation != null &&
                                           rtssElevation.Value == 2 &&
                                           helperElevation != null &&
                                           !helperElevation.Value;

        public bool IsRTSSRunning => App.Connection != null &&
                                     rtssElevation != null &&
                                     rtssElevation.Value > 0;

        public MainViewModel ViewModel { get; }

        private readonly WidgetProperties properties;
        private const string SETTING_LAST_ACTIVE_OSD_LEVEL = "LastActiveOSDLevel";
        private int _lastActiveOsdLevel = 1;

        public GamingWidget()
        {
            InitializeComponent();
            ViewModel = new MainViewModel(Dispatcher);
            LoadLastActiveOsdLevel();
            osd = new OSDProperty(0, PerformanceOverlaySlider, this);
            osdTextSize = new OSDTextSizeProperty(100, OverlayTextSizeSlider, this);
            runningGame = new RunningGameProperty(RunningGameText, PerGameProfileToggle, this);
            perGameProfile = new PerGameProfileProperty(PerGameProfileToggle, this);
            refreshRates = new RefreshRatesProperty(RefreshRatesComboBox, this);
            refreshRate = new RefreshRateProperty(RefreshRatesComboBox, this);
            resolutions = new ResolutionsProperty(ResolutionsComboBox, this);
            resolution = new ResolutionProperty(ResolutionsComboBox, this);
            hdr = new HDRProperty(HDRToggle, this);
            hdrSupported = new HDRSupportedProperty(HDRToggle, this);
            helperElevation = new HelperElevationProperty(HelperElevationBadge, HelperElevationBadgeText, HelperStatusSubtitleText, this);
            rtssElevation = new RTSSElevationProperty(RTSSElevationBadge, RTSSElevationBadgeText, RTSSStatusSubtitleText, this);
            hardwareProviderProperty = new HardwareProviderProperty(HardwareProviderBadge, HardwareProviderBadgeText, HardwareProviderSubtitleText, this);
            hardwareTelemetry = new HardwareTelemetryProperty(
                LiveCPUUsageText,
                LiveCPUWattageText,
                LiveGPUUsageText,
                LiveGPUWattageText,
                LiveVRAMUsageText,
                LiveVRAMUsedText,
                LiveRAMUsageText,
                LiveRAMUsedText,
                this,
                QuickCPUUsageText,
                QuickCPUWattageText,
                QuickGPUUsageText,
                QuickGPUWattageText,
                QuickVRAMUsageText,
                QuickVRAMUsedText,
                QuickRAMUsageText,
                QuickRAMUsedText);
            gameInputStatus = new GameInputStatusProperty(GameInputStatusBadge, GameInputStatusBadgeText, GameInputStatusSubtitleText, this);
            mouseSpeed = new MouseSpeedProperty(10, MouseSpeedSlider, this);
            mouseScrollSpeed = new MouseScrollSpeedProperty(3, MouseScrollSpeedSlider, this);
            mouseEnhancePointerPrecision = new MouseEnhancePointerPrecisionProperty(EnhancePointerPrecisionToggle, this);
            trackedGame = new TrackedGameProperty(new TrackedGame());
            onScreenDisplayProviderInstalled = new OnScreenDisplayProviderInstalledProperty(PerformanceOverlaySlider, this);
            isForeground = new IsForegroundProperty();
            amdSettingsSupported = new AMDSettingsSupportedProperty(AMDPivotItem, this, AMDPivotItemStackPanel, AMDRadeonSuperResolutionToggle,
                AMDRadeonSuperResolutionText, AMDFluidMotionFrameToggle, AMDFluidMotionFrameText, AMDRadeonAntiLagToggle, AMDRadeonAntiLagText,
                AMDRadeonBoostToggle, AMDRadeonBoostText, AMDRadeonChillToggle, AMDRadeonChillText);
            amdRadeonSuperResolutionEnabled = new AMDRadeonSuperResolutionEnabledProperty(AMDRadeonSuperResolutionToggle, this);
            amdRadeonSuperResolutionSupported = new AMDRadeonSuperResolutionSupportedProperty(AMDRadeonSuperResolutionToggle, this);
            amdRadeonSuperResolutionSharpness = new AMDRadeonSuperResolutionSharpnessProperty(AMDRadeonSuperResolutionSharpnessSlider, this);
            amdFluidMotionFrameEnabled = new AMDFluidMotionFrameEnabledProperty(AMDFluidMotionFrameToggle, this);
            amdFluidMotionFrameSupported = new AMDFluidMotionFrameSupportedProperty(AMDFluidMotionFrameToggle, this);
            amdRadeonAntiLagEnabled = new AMDRadeonAntiLagEnabledProperty(AMDRadeonAntiLagToggle, this);
            amdRadeonAntiLagSupported = new AMDRadeonAntiLagSupportedProperty(AMDRadeonAntiLagToggle, this);
            amdRadeonBoostEnabled = new AMDRadeonBoostEnabledProperty(AMDRadeonBoostToggle, this);
            amdRadeonBoostSupported = new AMDRadeonBoostSupportedProperty(AMDRadeonBoostToggle, this);
            amdRadeonBoostResolution = new AMDRadeonBoostResolutionProperty(AMDRadeonBoostResolutionSlider, this);
            amdRadeonChillEnabled = new AMDRadeonChillEnabledProperty(AMDRadeonChillToggle, this);
            amdRadeonChillSupported = new AMDRadeonChillSupportedProperty(AMDRadeonChillToggle, this);
            amdRadeonChillMinFPSProperty = new AMDRadeonChillMinFPSProperty(AMDRadeonChillMinFPSSlider, this);
            amdRadeonChillMaxFPSProperty = new AMDRadeonChillMaxFPSProperty(AMDRadeonChillMaxFPSSlider, this);

            focusingOnOSDSlider = new FocusingOnOSDSliderProperty(PerformanceOverlaySlider, this);
            limitFPS = new LimitFPSProperty(LimitFPSToggle, this);
            fpsLimit = new FPSLimitProperty(60, FPSLimitSlider, this);
            fpsLimitMode = new FPSLimitModeProperty(FPSLimitModeComboBox, this);
            judderFreeFPS = new JudderFreeFPSProperty(JudderFreeFPSToggle, this);
            gpuTarget = new GpuTargetProperty(DiscreteGpuButton, IntegratedGpuButton, this);
            dualGpuSupport = new DualGpuSupportProperty(GpuSelectionCard, GpuSelectionSubtitleText, DiscreteGpuButton, IntegratedGpuButton, this);
            var quickGamepadUI = new QuickGamepadUIElements
            {
                Section = QuickGamepadSection,
                Buttons = new[]
                {
                    new QuickGamepadButtonUI
                    {
                        Button = QuickGamepadButton1,
                        Icon = QuickGamepadIcon1,
                        Name = QuickGamepadName1,
                        Subtitle = QuickGamepadSubtitle1,
                        Badge = QuickGamepadBadge1,
                        BadgeText = QuickGamepadBadgeText1,
                        FlyoutIcon = QuickGamepadFlyoutIcon1,
                        FlyoutName = QuickGamepadFlyoutName1,
                        FlyoutSubtitle = QuickGamepadFlyoutSubtitle1,
                        FlyoutPercent = QuickGamepadFlyoutPercent1,
                        FlyoutType = QuickGamepadFlyoutType1,
                        FlyoutStatus = QuickGamepadFlyoutStatus1,
                    },
                    new QuickGamepadButtonUI
                    {
                        Button = QuickGamepadButton2,
                        Icon = QuickGamepadIcon2,
                        Name = QuickGamepadName2,
                        Subtitle = QuickGamepadSubtitle2,
                        Badge = QuickGamepadBadge2,
                        BadgeText = QuickGamepadBadgeText2,
                        FlyoutIcon = QuickGamepadFlyoutIcon2,
                        FlyoutName = QuickGamepadFlyoutName2,
                        FlyoutSubtitle = QuickGamepadFlyoutSubtitle2,
                        FlyoutPercent = QuickGamepadFlyoutPercent2,
                        FlyoutType = QuickGamepadFlyoutType2,
                        FlyoutStatus = QuickGamepadFlyoutStatus2,
                    },
                    new QuickGamepadButtonUI
                    {
                        Button = QuickGamepadButton3,
                        Icon = QuickGamepadIcon3,
                        Name = QuickGamepadName3,
                        Subtitle = QuickGamepadSubtitle3,
                        Badge = QuickGamepadBadge3,
                        BadgeText = QuickGamepadBadgeText3,
                        FlyoutIcon = QuickGamepadFlyoutIcon3,
                        FlyoutName = QuickGamepadFlyoutName3,
                        FlyoutSubtitle = QuickGamepadFlyoutSubtitle3,
                        FlyoutPercent = QuickGamepadFlyoutPercent3,
                        FlyoutType = QuickGamepadFlyoutType3,
                        FlyoutStatus = QuickGamepadFlyoutStatus3,
                    },
                    new QuickGamepadButtonUI
                    {
                        Button = QuickGamepadButton4,
                        Icon = QuickGamepadIcon4,
                        Name = QuickGamepadName4,
                        Subtitle = QuickGamepadSubtitle4,
                        Badge = QuickGamepadBadge4,
                        BadgeText = QuickGamepadBadgeText4,
                        FlyoutIcon = QuickGamepadFlyoutIcon4,
                        FlyoutName = QuickGamepadFlyoutName4,
                        FlyoutSubtitle = QuickGamepadFlyoutSubtitle4,
                        FlyoutPercent = QuickGamepadFlyoutPercent4,
                        FlyoutType = QuickGamepadFlyoutType4,
                        FlyoutStatus = QuickGamepadFlyoutStatus4,
                    }
                }
            };

            controllerBattery = new ControllerBatteryProperty(
                ControllerBatteryBadge,
                ControllerBatteryIcon,
                ControllerBatteryText,
                FlyoutBatteryIcon,
                FlyoutControllerNameText,
                FlyoutBatterySubtitleText,
                FlyoutBatteryPercentText,
                FlyoutBatteryTypeText,
                FlyoutBatteryStatusText,
                ControllerBatteryStatusBadge,
                ControllerBatteryStatusBadgeText,
                ControllerBatteryStatusSubtitleText,
                quickGamepadUI,
                this);

            JudderFreeFPSToggle.Toggled += JudderFreeFPSToggle_Toggled;
            LimitFPSToggle.Toggled += (s, e) =>
            {
                UpdateFPSLimitSliderJudderFree();
                UpdateFPSLimiterElevationUI();
                UpdateQuickFPSLimitButtonUI();
            };
            FPSLimitSlider.ValueChanged += (s, e) =>
            {
                UpdateQuickFPSLimitButtonUI();
            };
            FPSLimitJudderFreeSlider.ValueChanged += FPSLimitJudderFreeSlider_ValueChanged;
            WidgetSliderProperty.AttachEngagementHandler(FPSLimitJudderFreeSlider);
            FPSLimitJudderFreeCanvas.SizeChanged += (s, e) => RenderJudderFreeMarkers();
            PerformanceOverlaySlider.ValueChanged += (s, e) =>
            {
                int newLevel = (int)e.NewValue;
                if (newLevel > 0)
                {
                    _lastActiveOsdLevel = newLevel;
                    SaveLastActiveOsdLevel(newLevel);
                }
                UpdateOSDSegmentedButtons(newLevel);
                UpdateQuickOSDButtonUI(newLevel);
            };
            UpdateOSDSegmentedButtons((int)PerformanceOverlaySlider.Value);
            UpdateQuickOSDButtonUI((int)PerformanceOverlaySlider.Value);
            UpdateQuickFPSLimitButtonUI();
            judderFreeFPS.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(judderFreeFPS.Value))
                {
                    UpdateFPSLimitSliderJudderFree();
                }
            };
            refreshRates.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(refreshRates.Value))
                {
                    UpdateFPSLimitSliderJudderFree();
                }
            };
            refreshRate.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(refreshRate.Value))
                {
                    UpdateFPSLimitSliderJudderFree();
                    var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateQuickDisplayInfo());
                }
            };
            resolution.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(resolution.Value))
                {
                    var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateQuickDisplayInfo());
                }
            };
            hdr.PropertyChanged += (s, e) =>
            {
                var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateQuickDisplayInfo();
                    UpdateQuickHDRButtonUI(hdr != null && hdr.Value, hdrSupported != null && hdrSupported.Value);
                });
            };
            hdrSupported.PropertyChanged += (s, e) =>
            {
                var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (HDRSubtitleText != null)
                    {
                        HDRSubtitleText.Text = hdrSupported.Value ? "High Dynamic Range display" : "Not supported by display";
                    }
                    UpdateQuickDisplayInfo();
                    UpdateQuickHDRButtonUI(hdr != null && hdr.Value, hdrSupported.Value);
                });
            };
            fpsLimit.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(fpsLimit.Value))
                {
                    UpdateFPSLimitSliderJudderFree();
                    var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateQuickFPSLimitButtonUI());
                }
            };
            helperElevation.PropertyChanged += (s, e) =>
            {
                var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateFPSLimiterElevationUI();
                    UpdateOSDStatusUI();
                    UpdateQuickOSDButtonUI((int)PerformanceOverlaySlider.Value);
                    UpdateQuickFPSLimitButtonUI();
                });
            };
            rtssElevation.PropertyChanged += (s, e) =>
            {
                var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateFPSLimiterElevationUI();
                    UpdateOSDStatusUI();
                    UpdateQuickOSDButtonUI((int)PerformanceOverlaySlider.Value);
                    UpdateQuickFPSLimitButtonUI();
                });
            };
            onScreenDisplayProviderInstalled.PropertyChanged += (s, e) =>
            {
                var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateOSDStatusUI();
                    UpdateQuickOSDButtonUI((int)PerformanceOverlaySlider.Value);
                });
            };
            limitFPS.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(limitFPS.Value))
                {
                    var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        UpdateFPSLimiterElevationUI();
                        UpdateQuickFPSLimitButtonUI();
                    });
                }
            };

            properties = new WidgetProperties(
                this.Dispatcher,
                osd,
                osdTextSize,
                runningGame,
                perGameProfile,
                refreshRates,
                refreshRate,
                resolutions,
                resolution,
                hdr,
                hdrSupported,
                trackedGame,
                onScreenDisplayProviderInstalled,
                isForeground,
                amdSettingsSupported,
                amdRadeonSuperResolutionEnabled,
                amdRadeonSuperResolutionSupported,
                amdRadeonSuperResolutionSharpness,
                amdFluidMotionFrameEnabled,
                amdFluidMotionFrameSupported,
                amdRadeonAntiLagEnabled,
                amdRadeonAntiLagSupported,
                amdRadeonBoostEnabled,
                amdRadeonBoostSupported,
                amdRadeonBoostResolution,
                amdRadeonChillEnabled,
                amdRadeonChillSupported,
                amdRadeonChillMinFPSProperty,
                amdRadeonChillMaxFPSProperty,
                focusingOnOSDSlider,
                limitFPS,
                fpsLimit,
                fpsLimitMode,
                judderFreeFPS,
                helperElevation,
                rtssElevation,
                hardwareProviderProperty,
                hardwareTelemetry,
                gameInputStatus,
                gpuTarget,
                dualGpuSupport,
                controllerBattery,
                mouseSpeed,
                mouseScrollSpeed,
                mouseEnhancePointerPrecision
            );

            // Optimistic Local Cache Rendering: Instantly populate UI from last known state
            properties.TryApplyCachedSnapshot();
            InitializeViewModelBindings();

            this.PreviewKeyDown += GamingWidget_KeyDown;
            InitializeAppVersion();
            InitializeShortcuts();
            InitializeTabNavigation();
            UpdateFPSLimiterElevationUI();
            UpdateOSDStatusUI();
            UpdateQuickDisplayInfo();
            UpdateQuickRTSSStatus();
            UpdateQuickHDRButtonUI(hdr != null && hdr.Value, hdrSupported != null && hdrSupported.Value);
        }

        private async Task LaunchExternalUriAsync(string uriString)
        {
            Logger.Info($"LaunchExternalUriAsync called with: {uriString}");

            if (App.Connection != null)
            {
                try
                {
                    var valueSet = new ValueSet();
                    valueSet.Add(nameof(Command), (int)Command.Set);
                    valueSet.Add(nameof(Function), (int)Function.OpenUri);
                    valueSet.Add(nameof(Content), uriString);
                    valueSet.Add(nameof(UpdatedTime), DateTime.UtcNow.Ticks);

                    var response = await App.Connection.SendMessageAsync(valueSet);
                    Logger.Info($"SendMessageAsync OpenUri status: {response?.Status}");
                    if (response?.Status == AppServiceResponseStatus.Success)
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to send OpenUri message to helper.");
                }
            }
            else
            {
                Logger.Warn("App.Connection is null when attempting to launch URI via helper.");
            }

            // Fallback: Attempt UWP Launcher directly
            try
            {
                if (Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
                {
                    bool launched = await Launcher.LaunchUriAsync(uri);
                    Logger.Info($"Fallback Launcher.LaunchUriAsync result: {launched}");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Fallback Launcher.LaunchUriAsync failed for {uriString}.");
            }
        }

        private async void RestartElevatedButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("RestartElevatedButton clicked.");
            var clickedButton = sender as Button;
            if (App.Connection != null)
            {
                try
                {
                    isRestartingElevated = true;
                    if (clickedButton != null)
                    {
                        clickedButton.IsEnabled = false;
                    }

                    ShowLoadingModal("Restarting Helper as Administrator...", "Awaiting elevated background service connection...");

                    elevationFallbackCts?.Cancel();
                    elevationFallbackCts?.Dispose();
                    elevationFallbackCts = new CancellationTokenSource();
                    var token = elevationFallbackCts.Token;

                    // Fallback timeout only if user cancels UAC prompt or process fails to spawn
                    _ = Task.Delay(25000, token).ContinueWith(t =>
                    {
                        if (!t.IsCanceled)
                        {
                            var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                if (isRestartingElevated)
                                {
                                    Logger.Warn("Elevated restart timed out without reconnection. Resetting isRestartingElevated flag.");
                                    isRestartingElevated = false;
                                    if (clickedButton != null)
                                    {
                                        clickedButton.IsEnabled = true;
                                    }
                                    HideLoadingModal();
                                }
                            });
                        }
                    }, TaskScheduler.Default);

                    var valueSet = new ValueSet();
                    valueSet.Add(nameof(Command), (int)Command.Set);
                    valueSet.Add(nameof(Function), (int)Function.RestartElevated);
                    valueSet.Add(nameof(Content), true);
                    valueSet.Add(nameof(UpdatedTime), DateTime.UtcNow.Ticks);

                    var response = await App.Connection.SendMessageAsync(valueSet);
                    Logger.Info($"SendMessageAsync RestartElevated status: {response?.Status}");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to send RestartElevated message to helper.");
                    isRestartingElevated = false;
                    elevationFallbackCts?.Cancel();
                    elevationFallbackCts?.Dispose();
                    elevationFallbackCts = null;
                    if (clickedButton != null)
                    {
                        clickedButton.IsEnabled = true;
                    }
                    HideLoadingModal();
                }
            }
            else
            {
                Logger.Warn("App.Connection is null when attempting to restart elevated.");
                await EnsureHelperConnectionOrLaunchAsync();
            }
        }

        private async void RestartGameInputServiceButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("RestartGameInputServiceButton clicked.");
            if (App.Connection != null)
            {
                try
                {
                    if (RestartGameInputServiceButtonText != null)
                    {
                        RestartGameInputServiceButtonText.Text = "Restarting...";
                    }
                    if (RestartGameInputServiceButton != null)
                    {
                        RestartGameInputServiceButton.IsEnabled = false;
                    }

                    var valueSet = new ValueSet();
                    valueSet.Add(nameof(Command), (int)Command.Set);
                    valueSet.Add(nameof(Function), (int)Function.RestartGameInputService);
                    valueSet.Add(nameof(Content), true);
                    valueSet.Add(nameof(UpdatedTime), DateTime.UtcNow.Ticks);

                    var response = await App.Connection.SendMessageAsync(valueSet);
                    Logger.Info($"SendMessageAsync RestartGameInputService status: {response?.Status}");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to send RestartGameInputService message to helper.");
                }
                finally
                {
                    await Task.Delay(2000);
                    if (RestartGameInputServiceButtonText != null)
                    {
                        RestartGameInputServiceButtonText.Text = "Restart";
                    }
                    if (RestartGameInputServiceButton != null)
                    {
                        RestartGameInputServiceButton.IsEnabled = true;
                    }
                }
            }
            else
            {
                Logger.Warn("App.Connection is null when attempting to restart GameInput service.");
                await EnsureHelperConnectionOrLaunchAsync();
            }
        }

        private async void OpenVirtualKeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("OpenVirtualKeyboardButton clicked.");
            if (App.Connection != null)
            {
                try
                {
                    if (OpenVirtualKeyboardButtonText != null)
                    {
                        OpenVirtualKeyboardButtonText.Text = "Opening...";
                    }
                    if (OpenVirtualKeyboardButton != null)
                    {
                        OpenVirtualKeyboardButton.IsEnabled = false;
                    }

                    var valueSet = new ValueSet();
                    valueSet.Add(nameof(Command), (int)Command.Set);
                    valueSet.Add(nameof(Function), (int)Function.VirtualKeyboard);
                    valueSet.Add(nameof(Content), true);
                    valueSet.Add(nameof(UpdatedTime), DateTime.UtcNow.Ticks);

                    var response = await App.Connection.SendMessageAsync(valueSet);
                    Logger.Info($"SendMessageAsync VirtualKeyboard status: {response?.Status}");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to send VirtualKeyboard message to helper.");
                }
                finally
                {
                    await Task.Delay(1000);
                    if (OpenVirtualKeyboardButtonText != null)
                    {
                        OpenVirtualKeyboardButtonText.Text = "Open";
                    }
                    if (OpenVirtualKeyboardButton != null)
                    {
                        OpenVirtualKeyboardButton.IsEnabled = true;
                    }
                }
            }
            else
            {
                Logger.Warn("App.Connection is null when attempting to open virtual keyboard.");
                await EnsureHelperConnectionOrLaunchAsync();
            }
        }

        private async void OpenGamepadCursorSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            await OpenGamepadCursorSettingsAsync();
        }

        private async Task OpenGamepadCursorSettingsAsync()
        {
            Logger.Info("OpenGamepadCursorSettingsAsync called.");
            var uri = new Uri("ms-gamebar://launchforeground/activate/SettingsWidget");

            // 1. Try Xbox Game Bar SDK widget control if hosted inside Game Bar
            if (widget != null)
            {
                try
                {
                    var widgetControl = new XboxGameBarWidgetControl(widget);
                    await widgetControl.ActivateWithUriAsync(uri);
                    Logger.Info("Successfully activated SettingsWidget via XboxGameBarWidgetControl.");
                    return;
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "XboxGameBarWidgetControl.ActivateWithUriAsync failed, trying Launcher fallback.");
                }
            }

            // 2. Fall back to UWP Launcher.LaunchUriAsync
            try
            {
                bool launched = await Launcher.LaunchUriAsync(uri);
                Logger.Info($"Launcher.LaunchUriAsync result for {uri}: {launched}");
                if (launched)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Launcher.LaunchUriAsync failed for {uri}.");
            }

            // 3. Fall back to helper Process.Start via LaunchExternalUriAsync
            await LaunchExternalUriAsync(uri.AbsoluteUri);
        }

        private void DiscreteGpuButton_Click(object sender, RoutedEventArgs e)
        {
            gpuTarget?.SetValue(0);
            gpuTarget?.UpdateUI(0);
        }

        private void IntegratedGpuButton_Click(object sender, RoutedEventArgs e)
        {
            gpuTarget?.SetValue(1);
            gpuTarget?.UpdateUI(1);
        }

        private void InitializeAppVersion()
        {
            string versionString;
            try
            {
                var v = Package.Current.Id.Version;
                versionString = $"v{v.Major}.{v.Minor}.{v.Build}";
            }
            catch
            {
                var v = typeof(GamingWidget).Assembly.GetName().Version;
                versionString = $"v{v.Major}.{v.Minor}.{v.Build}";
            }

            if (AppVersionText != null)
            {
                AppVersionText.Text = $"CouchGamingBar {versionString}";
            }
        }

        private void GamingWidget_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (ShortcutEditorOverlay != null && ShortcutEditorOverlay.Visibility == Visibility.Visible)
            {
                if (isRecordingKeys)
                {
                    if (e.Key == VirtualKey.Control)
                    {
                        if (EditorCtrlToggle != null) EditorCtrlToggle.IsChecked = true;
                        UpdateEditorPreview();
                        e.Handled = true;
                        return;
                    }
                    if (e.Key == VirtualKey.Menu)
                    {
                        if (EditorAltToggle != null) EditorAltToggle.IsChecked = true;
                        UpdateEditorPreview();
                        e.Handled = true;
                        return;
                    }
                    if (e.Key == VirtualKey.Shift)
                    {
                        if (EditorShiftToggle != null) EditorShiftToggle.IsChecked = true;
                        UpdateEditorPreview();
                        e.Handled = true;
                        return;
                    }
                    if (e.Key == VirtualKey.LeftWindows || e.Key == VirtualKey.RightWindows)
                    {
                        if (EditorWinToggle != null) EditorWinToggle.IsChecked = true;
                        UpdateEditorPreview();
                        e.Handled = true;
                        return;
                    }

                    int vk = (int)e.Key;
                    if (EditorKeyComboBox != null)
                    {
                        int keyIdx = ShortcutManager.AvailableKeys.FindIndex(k => k.VirtualKey == vk);
                        if (keyIdx >= 0)
                        {
                            EditorKeyComboBox.SelectedIndex = keyIdx;
                        }
                        else
                        {
                            var dynamicKey = new ShortcutKeyOption { Name = ShortcutItem.GetKeyDisplayName(vk), VirtualKey = vk };
                            ShortcutManager.AvailableKeys.Add(dynamicKey);
                            EditorKeyComboBox.ItemsSource = null;
                            EditorKeyComboBox.ItemsSource = ShortcutManager.AvailableKeys;
                            EditorKeyComboBox.SelectedItem = dynamicKey;
                        }
                    }

                    isRecordingKeys = false;
                    UpdateRecordButtonState(false);
                    UpdateEditorPreview();
                    e.Handled = true;
                    return;
                }

                if (e.Key == VirtualKey.GamepadB || e.Key == VirtualKey.Escape)
                {
                    EditorCancelButton_Click(this, null);
                    e.Handled = true;
                    return;
                }
            }

            if (e.Key == VirtualKey.GamepadLeftTrigger || e.Key == VirtualKey.PageUp)
            {
                NavigatePivot(-1);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.GamepadRightTrigger || e.Key == VirtualKey.PageDown)
            {
                NavigatePivot(1);
                e.Handled = true;
            }
            else if (Window.Current?.CoreWindow != null)
            {
                var ctrlState = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
                bool isCtrlPressed = (ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
                var shiftState = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
                bool isShiftPressed = (shiftState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

                if (isCtrlPressed && e.Key == VirtualKey.Tab)
                {
                    NavigatePivot(isShiftPressed ? -1 : 1);
                    e.Handled = true;
                }
                else if (isCtrlPressed && e.Key >= VirtualKey.Number1 && e.Key <= VirtualKey.Number9)
                {
                    int num = (int)e.Key - (int)VirtualKey.Number1;
                    JumpToVisibleTab(num);
                    e.Handled = true;
                }
            }
        }

        private async void LaunchNvidiaAppButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Launch NVIDIA App button clicked.");
            if (App.Connection != null)
            {
                var valueSet = new ValueSet();
                valueSet.Add(nameof(Command), (int)Command.Set);
                valueSet.Add(nameof(Function), (int)Function.LaunchNvidiaApp);
                valueSet.Add(nameof(Content), true);
                valueSet.Add(nameof(UpdatedTime), DateTime.UtcNow.Ticks);
                try
                {
                    var response = await App.Connection.SendMessageAsync(valueSet);
                    Logger.Info($"SendMessageAsync LaunchNvidiaApp status: {response?.Status}");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to send LaunchNvidiaApp message to helper.");
                }
            }
            else
            {
                Logger.Warn("App.Connection is null, helper is not connected.");
            }
        }

        private void OSDLevelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int level))
            {
                Logger.Info($"OSD level button clicked: {level}");
                if (level > 0)
                {
                    _lastActiveOsdLevel = level;
                    SaveLastActiveOsdLevel(level);
                }
                PerformanceOverlaySlider.Value = level;
                UpdateOSDSegmentedButtons(level);
                UpdateQuickOSDButtonUI(level);
            }
        }

        private async void UpdateOSDSegmentedButtons(int level)
        {
            if (Dispatcher != null && !Dispatcher.HasThreadAccess)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateOSDSegmentedButtons(level));
                return;
            }

            Button[] buttons = new Button[] {
                OSDLevel0Button, OSDLevel1Button, OSDLevel2Button, OSDLevel3Button, OSDLevel4Button
            };
            var accentBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 124, 65)); // Xbox Green
            var transparentBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);
            var whiteBrush = new SolidColorBrush(Windows.UI.Colors.White);
            var textMutedBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 200, 200, 200));

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    int btnLevel = i % 5;
                    if (btnLevel == level)
                    {
                        buttons[i].Background = accentBrush;
                        buttons[i].Foreground = whiteBrush;
                        buttons[i].FontWeight = Windows.UI.Text.FontWeights.Bold;
                    }
                    else
                    {
                        buttons[i].Background = transparentBrush;
                        buttons[i].Foreground = textMutedBrush;
                        buttons[i].FontWeight = Windows.UI.Text.FontWeights.SemiBold;
                    }
                }
            }
        }

        #region Couch Essentials Quick Toggles

        private void LoadLastActiveOsdLevel()
        {
            try
            {
                if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.TryGetValue(SETTING_LAST_ACTIVE_OSD_LEVEL, out object saved) && saved is int lvl && lvl > 0)
                {
                    _lastActiveOsdLevel = lvl;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to load last active OSD level: {ex.Message}");
            }
        }

        private void SaveLastActiveOsdLevel(int level)
        {
            if (level <= 0) return;
            try
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values[SETTING_LAST_ACTIVE_OSD_LEVEL] = level;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to save last active OSD level: {ex.Message}");
            }
        }

        private static string GetOsdLevelName(int level)
        {
            switch (level)
            {
                case 1: return "FPS";
                case 2: return "DETAIL";
                case 3: return "FULL";
                case 4: return "ALL";
                default: return "OFF";
            }
        }

        private void QuickOSDToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsRTSSRunning || PerformanceOverlaySlider == null) return;

            int currentLevel = (int)PerformanceOverlaySlider.Value;
            if (currentLevel > 0)
            {
                _lastActiveOsdLevel = currentLevel;
                SaveLastActiveOsdLevel(currentLevel);
                Logger.Info($"QuickOSDToggleButton clicked: turning OFF RTSS overlay (saved active level: {currentLevel}).");
                PerformanceOverlaySlider.Value = 0;
            }
            else
            {
                int targetLevel = _lastActiveOsdLevel > 0 ? _lastActiveOsdLevel : 1;
                Logger.Info($"QuickOSDToggleButton clicked: turning ON RTSS overlay to restored level: {targetLevel}.");
                PerformanceOverlaySlider.Value = targetLevel;
            }
        }

        private void QuickFPSLimitToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsElevationMismatch || !IsRTSSRunning || LimitFPSToggle == null) return;

            bool newState = !LimitFPSToggle.IsOn;
            Logger.Info($"QuickFPSLimitToggleButton clicked: toggling frame limiter to {newState} (target limit: {(int)FPSLimitSlider.Value} FPS).");
            LimitFPSToggle.IsOn = newState;
        }

        private async void UpdateQuickOSDButtonUI(int level)
        {
            if (Dispatcher != null && !Dispatcher.HasThreadAccess)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateQuickOSDButtonUI(level));
                return;
            }

            if (QuickOSDToggleButton == null || QuickOSDSubtitleText == null || QuickOSDBadge == null || QuickOSDBadgeText == null)
                return;

            var greenBrush = Resources.ContainsKey("XboxGreenBrush")
                ? (SolidColorBrush)Resources["XboxGreenBrush"]
                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 124, 65));
            var mutedBrush = (Brush)Resources["SystemControlForegroundBaseMediumBrush"];
            var offBgBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 58, 58)); // #3A3A3A
            var offTextBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 204, 204, 204)); // #CCCCCC
            var whiteBrush = new SolidColorBrush(Windows.UI.Colors.White);

            ViewModel.QuickAccess.UpdateOsdUI(level, App.Connection != null, IsRTSSRunning, rtssElevation != null ? rtssElevation.Value : -1, _lastActiveOsdLevel);

            QuickOSDToggleButton.IsEnabled = ViewModel.QuickAccess.IsOsdButtonEnabled;
            QuickOSDSubtitleText.Text = ViewModel.QuickAccess.OsdSubtitle;
            QuickOSDBadgeText.Text = ViewModel.QuickAccess.OsdBadgeText;

            if (ViewModel.QuickAccess.IsOsdActive)
            {
                if (QuickOSDIcon != null) QuickOSDIcon.Foreground = greenBrush;
                QuickOSDBadgeText.Foreground = whiteBrush;
                QuickOSDBadge.Background = greenBrush;
                QuickOSDBadge.BorderBrush = greenBrush;
            }
            else
            {
                if (QuickOSDIcon != null) QuickOSDIcon.Foreground = mutedBrush;
                QuickOSDBadgeText.Foreground = offTextBrush;
                QuickOSDBadge.Background = offBgBrush;
                QuickOSDBadge.BorderBrush = offBgBrush;
            }
        }

        private async void UpdateQuickFPSLimitButtonUI()
        {
            if (Dispatcher != null && !Dispatcher.HasThreadAccess)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateQuickFPSLimitButtonUI());
                return;
            }

            if (QuickFPSLimitToggleButton == null || QuickFPSLimitSubtitleText == null || QuickFPSLimitBadge == null || QuickFPSLimitBadgeText == null)
                return;

            var greenBrush = Resources.ContainsKey("XboxGreenBrush")
                ? (SolidColorBrush)Resources["XboxGreenBrush"]
                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 124, 65));
            var mutedBrush = (Brush)Resources["SystemControlForegroundBaseMediumBrush"];
            var offBgBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 58, 58)); // #3A3A3A
            var offTextBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 204, 204, 204)); // #CCCCCC
            var whiteBrush = new SolidColorBrush(Windows.UI.Colors.White);

            bool isLimitOn = LimitFPSToggle != null && LimitFPSToggle.IsOn;
            int fpsValue = (FPSLimitSlider != null && FPSLimitSlider.Value > 0) ? (int)FPSLimitSlider.Value : 60;

            ViewModel.QuickAccess.UpdateFpsLimitUI(isLimitOn, fpsValue, App.Connection != null, IsRTSSRunning, IsElevationMismatch, rtssElevation != null ? rtssElevation.Value : -1);

            QuickFPSLimitToggleButton.IsEnabled = ViewModel.QuickAccess.IsFpsLimitButtonEnabled;
            QuickFPSLimitSubtitleText.Text = ViewModel.QuickAccess.FpsLimitSubtitle;
            QuickFPSLimitBadgeText.Text = ViewModel.QuickAccess.FpsLimitBadgeText;

            if (ViewModel.QuickAccess.IsFpsLimitMismatch)
            {
                if (QuickFPSLimitIcon != null) QuickFPSLimitIcon.Foreground = mutedBrush;
                QuickFPSLimitBadgeText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)); // Amber
                var amberBg = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 35, 5));
                QuickFPSLimitBadge.Background = amberBg;
                QuickFPSLimitBadge.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 202, 138, 4));
            }
            else if (ViewModel.QuickAccess.IsFpsLimitActive)
            {
                if (QuickFPSLimitIcon != null) QuickFPSLimitIcon.Foreground = greenBrush;
                QuickFPSLimitBadgeText.Foreground = whiteBrush;
                QuickFPSLimitBadge.Background = greenBrush;
                QuickFPSLimitBadge.BorderBrush = greenBrush;
            }
            else
            {
                if (QuickFPSLimitIcon != null) QuickFPSLimitIcon.Foreground = mutedBrush;
                QuickFPSLimitBadgeText.Foreground = offTextBrush;
                QuickFPSLimitBadge.Background = offBgBrush;
                QuickFPSLimitBadge.BorderBrush = offBgBrush;
            }
        }

        private void QuickHDRToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (hdrSupported == null || !hdrSupported.Value) return;

            bool newState = hdr != null ? !hdr.Value : true;
            Logger.Info($"QuickHDRToggleButton clicked: switching HDR to {newState}.");
            if (hdr != null)
            {
                hdr.SetValue(newState);
            }
            if (HDRToggle != null)
            {
                HDRToggle.IsOn = newState;
            }
            UpdateQuickHDRButtonUI(newState, true);
            UpdateQuickDisplayInfo();
        }

        private async void UpdateQuickHDRButtonUI(bool isEnabled, bool isSupported)
        {
            if (Dispatcher != null && !Dispatcher.HasThreadAccess)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateQuickHDRButtonUI(isEnabled, isSupported));
                return;
            }

            if (QuickHDRToggleButton == null || QuickHDRSubtitleText == null || QuickHDRBadge == null || QuickHDRBadgeText == null)
                return;

            var greenBrush = Resources.ContainsKey("XboxGreenBrush")
                ? (SolidColorBrush)Resources["XboxGreenBrush"]
                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 124, 65));
            var mutedBrush = (Brush)Resources["SystemControlForegroundBaseMediumBrush"];
            var offBgBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 58, 58)); // #3A3A3A
            var offTextBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 204, 204, 204)); // #CCCCCC
            var whiteBrush = new SolidColorBrush(Windows.UI.Colors.White);

            ViewModel.QuickAccess.UpdateHdrUI(isEnabled, isSupported);

            QuickHDRToggleButton.IsEnabled = ViewModel.QuickAccess.IsHdrButtonEnabled;
            QuickHDRSubtitleText.Text = ViewModel.QuickAccess.HdrSubtitle;
            QuickHDRBadgeText.Text = ViewModel.QuickAccess.HdrBadgeText;

            if (ViewModel.QuickAccess.IsHdrActive)
            {
                if (QuickHDRIcon != null) QuickHDRIcon.Foreground = greenBrush;
                QuickHDRBadgeText.Foreground = whiteBrush;
                QuickHDRBadge.Background = greenBrush;
                QuickHDRBadge.BorderBrush = greenBrush;
            }
            else
            {
                if (QuickHDRIcon != null) QuickHDRIcon.Foreground = mutedBrush;
                QuickHDRBadgeText.Foreground = offTextBrush;
                QuickHDRBadge.Background = offBgBrush;
                QuickHDRBadge.BorderBrush = offBgBrush;
            }
        }

        private void InitializeViewModelBindings()
        {
            if (refreshRate != null) ViewModel.Display.SelectedRefreshRate = refreshRate.Value;
            if (refreshRates != null) ViewModel.Display.RefreshRates = refreshRates.Value;
            if (resolution != null) ViewModel.Display.SelectedResolution = resolution.Value.ToString();
            if (resolutions != null && resolutions.Value.AvailableResolutions != null) ViewModel.Display.Resolutions = resolutions.Value.AvailableResolutions.Select(r => r.ToString()).ToList();
            if (hdr != null) ViewModel.Display.IsHdrEnabled = hdr.Value;
            if (hdrSupported != null) ViewModel.Display.IsHdrSupported = hdrSupported.Value;

            if (limitFPS != null) ViewModel.Performance.IsLimitFpsEnabled = limitFPS.Value;
            if (fpsLimit != null) ViewModel.Performance.FpsLimit = fpsLimit.Value;
            if (fpsLimitMode != null) ViewModel.Performance.FpsLimitMode = fpsLimitMode.Value;
            if (judderFreeFPS != null) ViewModel.Performance.IsJudderFreeEnabled = judderFreeFPS.Value;
            if (osd != null) ViewModel.Performance.OsdLevel = osd.Value;
            if (osdTextSize != null) ViewModel.Performance.OsdTextSize = osdTextSize.Value;
            if (rtssElevation != null) ViewModel.Performance.RtssElevationStatus = rtssElevation.Value;

            if (amdSettingsSupported != null) ViewModel.AMD.IsAmdSettingsSupported = amdSettingsSupported.Value;
            if (amdRadeonSuperResolutionSupported != null) ViewModel.AMD.IsRsrSupported = amdRadeonSuperResolutionSupported.Value;
            if (amdRadeonSuperResolutionEnabled != null) ViewModel.AMD.IsRsrEnabled = amdRadeonSuperResolutionEnabled.Value;
            if (amdRadeonSuperResolutionSharpness != null) ViewModel.AMD.RsrSharpness = amdRadeonSuperResolutionSharpness.Value;
            if (amdFluidMotionFrameSupported != null) ViewModel.AMD.IsAfmfSupported = amdFluidMotionFrameSupported.Value;
            if (amdFluidMotionFrameEnabled != null) ViewModel.AMD.IsAfmfEnabled = amdFluidMotionFrameEnabled.Value;
            if (amdRadeonAntiLagSupported != null) ViewModel.AMD.IsAntiLagSupported = amdRadeonAntiLagSupported.Value;
            if (amdRadeonAntiLagEnabled != null) ViewModel.AMD.IsAntiLagEnabled = amdRadeonAntiLagEnabled.Value;
            if (amdRadeonBoostSupported != null) ViewModel.AMD.IsBoostSupported = amdRadeonBoostSupported.Value;
            if (amdRadeonBoostEnabled != null) ViewModel.AMD.IsBoostEnabled = amdRadeonBoostEnabled.Value;
            if (amdRadeonBoostResolution != null) ViewModel.AMD.BoostResolution = amdRadeonBoostResolution.Value;
            if (amdRadeonChillSupported != null) ViewModel.AMD.IsChillSupported = amdRadeonChillSupported.Value;
            if (amdRadeonChillEnabled != null) ViewModel.AMD.IsChillEnabled = amdRadeonChillEnabled.Value;
            if (amdRadeonChillMinFPSProperty != null) ViewModel.AMD.ChillMinFps = amdRadeonChillMinFPSProperty.Value;
            if (amdRadeonChillMaxFPSProperty != null) ViewModel.AMD.ChillMaxFps = amdRadeonChillMaxFPSProperty.Value;

            if (onScreenDisplayProviderInstalled != null) ViewModel.Settings.IsOsdProviderInstalled = onScreenDisplayProviderInstalled.Value;
            if (dualGpuSupport != null) ViewModel.Settings.IsDualGpuSupported = !string.IsNullOrEmpty(dualGpuSupport.Value);
            if (gpuTarget != null) ViewModel.Settings.GpuTarget = gpuTarget.Value;
            if (helperElevation != null) ViewModel.Settings.IsHelperElevated = helperElevation.Value;
            if (gameInputStatus != null) ViewModel.Settings.GameInputStatus = gameInputStatus.Value;
            if (mouseSpeed != null) ViewModel.Settings.MouseSpeed = mouseSpeed.Value;
            if (mouseScrollSpeed != null) ViewModel.Settings.MouseScrollSpeed = mouseScrollSpeed.Value;
            if (mouseEnhancePointerPrecision != null) ViewModel.Settings.IsMouseEnhancePointerPrecision = mouseEnhancePointerPrecision.Value;

            // Wire up event updates
            if (refreshRate != null) refreshRate.PropertyChanged += (s, e) => { ViewModel.Display.SelectedRefreshRate = refreshRate.Value; UpdateQuickDisplayInfo(); };
            if (refreshRates != null) refreshRates.PropertyChanged += (s, e) => { ViewModel.Display.RefreshRates = refreshRates.Value; UpdateQuickDisplayInfo(); };
            if (resolution != null) resolution.PropertyChanged += (s, e) => { ViewModel.Display.SelectedResolution = resolution.Value.ToString(); UpdateQuickDisplayInfo(); };
            if (resolutions != null) resolutions.PropertyChanged += (s, e) => { if (resolutions.Value.AvailableResolutions != null) ViewModel.Display.Resolutions = resolutions.Value.AvailableResolutions.Select(r => r.ToString()).ToList(); };
            if (hdr != null) hdr.PropertyChanged += (s, e) => { ViewModel.Display.IsHdrEnabled = hdr.Value; UpdateQuickHDRButtonUI(hdr.Value, hdrSupported != null && hdrSupported.Value); };
            if (hdrSupported != null) hdrSupported.PropertyChanged += (s, e) => { ViewModel.Display.IsHdrSupported = hdrSupported.Value; UpdateQuickHDRButtonUI(hdr != null && hdr.Value, hdrSupported.Value); };

            if (limitFPS != null) limitFPS.PropertyChanged += (s, e) => { ViewModel.Performance.IsLimitFpsEnabled = limitFPS.Value; UpdateFPSLimitSliderJudderFree(); UpdateFPSLimiterElevationUI(); UpdateQuickFPSLimitButtonUI(); };
            if (fpsLimit != null) fpsLimit.PropertyChanged += (s, e) => { ViewModel.Performance.FpsLimit = fpsLimit.Value; UpdateQuickFPSLimitButtonUI(); };
            if (fpsLimitMode != null) fpsLimitMode.PropertyChanged += (s, e) => { ViewModel.Performance.FpsLimitMode = fpsLimitMode.Value; };
            if (judderFreeFPS != null) judderFreeFPS.PropertyChanged += (s, e) => { ViewModel.Performance.IsJudderFreeEnabled = judderFreeFPS.Value; UpdateFPSLimitSliderJudderFree(); };
            if (osd != null) osd.PropertyChanged += (s, e) => { ViewModel.Performance.OsdLevel = osd.Value; UpdateOSDSegmentedButtons(osd.Value); UpdateQuickOSDButtonUI(osd.Value); };
            if (osdTextSize != null) osdTextSize.PropertyChanged += (s, e) => { ViewModel.Performance.OsdTextSize = osdTextSize.Value; };

            if (amdSettingsSupported != null) amdSettingsSupported.PropertyChanged += (s, e) => { ViewModel.AMD.IsAmdSettingsSupported = amdSettingsSupported.Value; };
            if (amdRadeonSuperResolutionSupported != null) amdRadeonSuperResolutionSupported.PropertyChanged += (s, e) => { ViewModel.AMD.IsRsrSupported = amdRadeonSuperResolutionSupported.Value; };
            if (amdRadeonSuperResolutionEnabled != null) amdRadeonSuperResolutionEnabled.PropertyChanged += (s, e) => { ViewModel.AMD.IsRsrEnabled = amdRadeonSuperResolutionEnabled.Value; };
            if (amdRadeonSuperResolutionSharpness != null) amdRadeonSuperResolutionSharpness.PropertyChanged += (s, e) => { ViewModel.AMD.RsrSharpness = amdRadeonSuperResolutionSharpness.Value; };
            if (amdFluidMotionFrameSupported != null) amdFluidMotionFrameSupported.PropertyChanged += (s, e) => { ViewModel.AMD.IsAfmfSupported = amdFluidMotionFrameSupported.Value; };
            if (amdFluidMotionFrameEnabled != null) amdFluidMotionFrameEnabled.PropertyChanged += (s, e) => { ViewModel.AMD.IsAfmfEnabled = amdFluidMotionFrameEnabled.Value; };
            if (amdRadeonAntiLagSupported != null) amdRadeonAntiLagSupported.PropertyChanged += (s, e) => { ViewModel.AMD.IsAntiLagSupported = amdRadeonAntiLagSupported.Value; };
            if (amdRadeonAntiLagEnabled != null) amdRadeonAntiLagEnabled.PropertyChanged += (s, e) => { ViewModel.AMD.IsAntiLagEnabled = amdRadeonAntiLagEnabled.Value; };
            if (amdRadeonBoostSupported != null) amdRadeonBoostSupported.PropertyChanged += (s, e) => { ViewModel.AMD.IsBoostSupported = amdRadeonBoostSupported.Value; };
            if (amdRadeonBoostEnabled != null) amdRadeonBoostEnabled.PropertyChanged += (s, e) => { ViewModel.AMD.IsBoostEnabled = amdRadeonBoostEnabled.Value; };
            if (amdRadeonBoostResolution != null) amdRadeonBoostResolution.PropertyChanged += (s, e) => { ViewModel.AMD.BoostResolution = amdRadeonBoostResolution.Value; };
            if (amdRadeonChillSupported != null) amdRadeonChillSupported.PropertyChanged += (s, e) => { ViewModel.AMD.IsChillSupported = amdRadeonChillSupported.Value; };
            if (amdRadeonChillEnabled != null) amdRadeonChillEnabled.PropertyChanged += (s, e) => { ViewModel.AMD.IsChillEnabled = amdRadeonChillEnabled.Value; };
            if (amdRadeonChillMinFPSProperty != null) amdRadeonChillMinFPSProperty.PropertyChanged += (s, e) => { ViewModel.AMD.ChillMinFps = amdRadeonChillMinFPSProperty.Value; };
            if (amdRadeonChillMaxFPSProperty != null) amdRadeonChillMaxFPSProperty.PropertyChanged += (s, e) => { ViewModel.AMD.ChillMaxFps = amdRadeonChillMaxFPSProperty.Value; };

            if (onScreenDisplayProviderInstalled != null) onScreenDisplayProviderInstalled.PropertyChanged += (s, e) => { ViewModel.Settings.IsOsdProviderInstalled = onScreenDisplayProviderInstalled.Value; };
            if (dualGpuSupport != null) dualGpuSupport.PropertyChanged += (s, e) => { ViewModel.Settings.IsDualGpuSupported = !string.IsNullOrEmpty(dualGpuSupport.Value); };
            if (gpuTarget != null) gpuTarget.PropertyChanged += (s, e) => { ViewModel.Settings.GpuTarget = gpuTarget.Value; };
            if (helperElevation != null) helperElevation.PropertyChanged += (s, e) => { ViewModel.Settings.IsHelperElevated = helperElevation.Value; UpdateFPSLimiterElevationUI(); UpdateOSDStatusUI(); UpdateQuickFPSLimitButtonUI(); UpdateQuickRTSSStatus(); };
            if (rtssElevation != null) rtssElevation.PropertyChanged += (s, e) => { ViewModel.Performance.EvaluateElevationStatus(App.Connection != null, rtssElevation.Value, helperElevation != null && helperElevation.Value); UpdateFPSLimiterElevationUI(); UpdateOSDStatusUI(); UpdateQuickFPSLimitButtonUI(); UpdateQuickOSDButtonUI((int)PerformanceOverlaySlider.Value); UpdateQuickRTSSStatus(); };
            if (gameInputStatus != null) gameInputStatus.PropertyChanged += (s, e) => { ViewModel.Settings.GameInputStatus = gameInputStatus.Value; };
            if (mouseSpeed != null) mouseSpeed.PropertyChanged += (s, e) => { ViewModel.Settings.MouseSpeed = mouseSpeed.Value; };
            if (mouseScrollSpeed != null) mouseScrollSpeed.PropertyChanged += (s, e) => { ViewModel.Settings.MouseScrollSpeed = mouseScrollSpeed.Value; };
            if (mouseEnhancePointerPrecision != null) mouseEnhancePointerPrecision.PropertyChanged += (s, e) => { ViewModel.Settings.IsMouseEnhancePointerPrecision = mouseEnhancePointerPrecision.Value; };

            if (runningGame != null)
            {
                ViewModel.RunningGameName = runningGame.Value.IsValid() ? runningGame.Value.GameId.Name : "No game detected";
                ViewModel.IsGameRunning = runningGame.Value.IsValid();
                runningGame.PropertyChanged += (s, e) =>
                {
                    ViewModel.RunningGameName = runningGame.Value.IsValid() ? runningGame.Value.GameId.Name : "No game detected";
                    ViewModel.IsGameRunning = runningGame.Value.IsValid();
                };
            }
            if (perGameProfile != null)
            {
                ViewModel.IsPerGameProfile = perGameProfile.Value;
                perGameProfile.PropertyChanged += (s, e) =>
                {
                    ViewModel.IsPerGameProfile = perGameProfile.Value;
                };
            }

            if (hardwareTelemetry != null)
            {
                hardwareTelemetry.OnTelemetryUpdated = (cpuU, cpuW, gpuU, gpuW, ramU, ramUsed, vramU, vramUsed) =>
                {
                    ViewModel.QuickAccess.UpdateTelemetry(cpuU, cpuW, gpuU, gpuW, ramU, ramUsed, vramU, vramUsed);
                };
            }
        }

        #endregion

        #region Option C Tab Navigation & Living Room HUD

        private class TabNavigationInfo
        {
            public PivotItem Item { get; set; }
            public string Title { get; set; }
            public string ShortName { get; set; }
            public string Glyph { get; set; }
            public SolidColorBrush AccentBrush { get; set; }
        }

        private List<TabNavigationInfo> tabInfos;

        private void InitializeTabNavigation()
        {
            var greenBrush = Resources.ContainsKey("XboxGreenBrush") ? (SolidColorBrush)Resources["XboxGreenBrush"] : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 124, 65));
            var rubyBrush = Resources.ContainsKey("RadeonRubyBrush") ? (SolidColorBrush)Resources["RadeonRubyBrush"] : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 237, 28, 36));
            var geforceBrush = Resources.ContainsKey("GeForceGreenBrush") ? (SolidColorBrush)Resources["GeForceGreenBrush"] : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 118, 185, 0));

            tabInfos = new List<TabNavigationInfo>
            {
                new TabNavigationInfo { Item = QuickAccessPivotItem, Title = "QUICK ACCESS", ShortName = "Quick", Glyph = "\uEC7A", AccentBrush = greenBrush },
                new TabNavigationInfo { Item = PerformancePivotItem, Title = "PERFORMANCE", ShortName = "Perf", Glyph = "\uE9D2", AccentBrush = greenBrush },
                new TabNavigationInfo { Item = ShortcutsPivotItem, Title = "SHORTCUTS", ShortName = "Shortcuts", Glyph = "\uE765", AccentBrush = greenBrush },
                new TabNavigationInfo { Item = CursorPivotItem, Title = "CURSOR", ShortName = "Cursor", Glyph = "\uE962", AccentBrush = greenBrush },
                new TabNavigationInfo { Item = SystemPivotItem, Title = "SYSTEM", ShortName = "System", Glyph = "\uE770", AccentBrush = greenBrush },
                new TabNavigationInfo { Item = AMDPivotItem, Title = "AMD RADEON", ShortName = "AMD", Glyph = "\uE945", AccentBrush = rubyBrush },
                new TabNavigationInfo { Item = NvidiaPivotItem, Title = "NVIDIA", ShortName = "NVIDIA", Glyph = "\uE9D9", AccentBrush = geforceBrush }
            };

            if (AMDPivotItem != null)
            {
                AMDPivotItem.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, (s, e) => UpdateTabNavigationHUD());
            }
            if (NvidiaPivotItem != null)
            {
                NvidiaPivotItem.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, (s, e) => UpdateTabNavigationHUD());
            }

            UpdateTabNavigationHUD();
        }

        private void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTabNavigationHUD();
        }

        private void UpdateTabNavigationHUD()
        {
            if (tabInfos == null || MainPivot == null) return;

            var visibleTabs = tabInfos.Where(t => t.Item != null && t.Item.Visibility == Visibility.Visible).ToList();
            if (visibleTabs.Count == 0) return;

            int activeIndex = visibleTabs.FindIndex(t => t.Item == MainPivot.SelectedItem);
            if (activeIndex == -1) activeIndex = 0;

            var current = visibleTabs[activeIndex];
            int prevIndex = (activeIndex - 1 + visibleTabs.Count) % visibleTabs.Count;
            int nextIndex = (activeIndex + 1) % visibleTabs.Count;

            var prev = visibleTabs[prevIndex];
            var next = visibleTabs[nextIndex];

            if (NavActiveHeroText != null) NavActiveHeroText.Text = current.Title;
            if (NavActiveHeroIcon != null) NavActiveHeroIcon.Glyph = current.Glyph;
            if (NavActiveHeroBadge != null && current.AccentBrush != null) NavActiveHeroBadge.Background = current.AccentBrush;

            if (NavPrevTabText != null) NavPrevTabText.Text = prev.ShortName;
            if (NavPrevTabIcon != null) NavPrevTabIcon.Glyph = prev.Glyph;

            if (NavNextTabText != null) NavNextTabText.Text = next.ShortName;
            if (NavNextTabIcon != null) NavNextTabIcon.Glyph = next.Glyph;

            if (NavTabCounterText != null) NavTabCounterText.Text = $"{activeIndex + 1} of {visibleTabs.Count}";

            if (NavPagerDotsPanel != null)
            {
                NavPagerDotsPanel.Children.Clear();
                for (int i = 0; i < visibleTabs.Count; i++)
                {
                    int targetPos = i;
                    var dotButton = new Button
                    {
                        Background = new SolidColorBrush(Windows.UI.Colors.Transparent),
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(0),
                        Margin = new Thickness(2, 0, 2, 0),
                        Width = 16,
                        Height = 12,
                        IsTabStop = false
                    };

                    var dotBorder = new Border
                    {
                        CornerRadius = new CornerRadius(10),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    if (i == activeIndex)
                    {
                        dotBorder.Width = 10;
                        dotBorder.Height = 10;
                        dotBorder.Background = current.AccentBrush ?? (SolidColorBrush)Resources["XboxGreenBrush"];
                        dotBorder.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 52, 211, 153));
                        dotBorder.BorderThickness = new Thickness(1.5);
                    }
                    else
                    {
                        dotBorder.Width = 7;
                        dotBorder.Height = 7;
                        dotBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 75, 85, 99));
                    }

                    dotButton.Content = dotBorder;
                    dotButton.Click += (s, e) =>
                    {
                        if (targetPos >= 0 && targetPos < visibleTabs.Count)
                        {
                            MainPivot.SelectedItem = visibleTabs[targetPos].Item;
                        }
                    };
                    NavPagerDotsPanel.Children.Add(dotButton);
                }
            }
        }

        private void NavTriggerLeftButton_Click(object sender, RoutedEventArgs e)
        {
            NavigatePivot(-1);
        }

        private void NavTriggerRightButton_Click(object sender, RoutedEventArgs e)
        {
            NavigatePivot(1);
        }

        private void NavPrevTabGhostButton_Click(object sender, RoutedEventArgs e)
        {
            NavigatePivot(-1);
        }

        private void NavNextTabGhostButton_Click(object sender, RoutedEventArgs e)
        {
            NavigatePivot(1);
        }

        private void NavTabHUD_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var props = e.GetCurrentPoint(this).Properties;
            if (props.MouseWheelDelta > 0)
            {
                NavigatePivot(-1);
                e.Handled = true;
            }
            else if (props.MouseWheelDelta < 0)
            {
                NavigatePivot(1);
                e.Handled = true;
            }
        }

        private void JumpToVisibleTab(int targetIndex)
        {
            if (tabInfos == null || MainPivot == null) return;
            var visibleTabs = tabInfos.Where(t => t.Item != null && t.Item.Visibility == Visibility.Visible).ToList();
            if (targetIndex >= 0 && targetIndex < visibleTabs.Count)
            {
                MainPivot.SelectedItem = visibleTabs[targetIndex].Item;
            }
        }

        private void NavigatePivot(int direction)
        {
            if (tabInfos == null || MainPivot == null) return;

            var visibleTabs = tabInfos.Where(t => t.Item != null && t.Item.Visibility == Visibility.Visible).ToList();
            if (visibleTabs.Count <= 1) return;

            int currentPos = visibleTabs.FindIndex(t => t.Item == MainPivot.SelectedItem);
            if (currentPos == -1) currentPos = 0;

            // Circular navigation across all visible tabs
            int targetPos = (currentPos + direction + visibleTabs.Count) % visibleTabs.Count;
            MainPivot.SelectedItem = visibleTabs[targetPos].Item;
        }

        #endregion

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            Logger.Info($"GamingWidget OnNavigatedTo. NavigationMode: {e.NavigationMode}, Parameter: {e.Parameter}");
            base.OnNavigatedTo(e);

            //while (!System.Diagnostics.Debugger.IsAttached)
            //{
            //    await Task.Delay(500);
            //}

            widgetDarkThemeBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 40, 44));
            widgetLightThemeBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));

            widget = e.Parameter as XboxGameBarWidget;
            if (widget != null)
            {
                Logger.Info("Running as a Xbox Game Bar widget.");
                await widget.CenterWindowAsync();
                widget.RequestedThemeChanged += GamingWidget_RequestedThemeChanged;
                widget.SettingsClicked += GamingWidget_SettingsClicked;
                EnsureWidgetActivity();
            }
            else
            {
                Logger.Info("XboxGameBarWidget not available, probably running as an app instead of widget.");
            }

            Logger.Info($"App.Connection:{(App.Connection == null ? "NULL" : "NOT_NULL")} FullTrustAppContract:{(ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0) ? "PRESENT" : "NOT_PRESENT")}");
            if (App.Connection != null)
            {
                HideLoadingModal();
                ReconnectAppService();
            }
            else
            {
                if (!properties.HasCachedState)
                {
                    ShowLoadingModal();
                }
                await EnsureHelperConnectionOrLaunchAsync();
            }
            Logger.Info("GamingWidget OnNavigatedTo finished.");
        }

        private async void ShowLoadingModal(string title = "Connecting to Helper...", string subtitle = "Initializing hardware sensors & power controls...")
        {
            try
            {
                modalTimeoutCts?.Cancel();
                modalTimeoutCts?.Dispose();
                modalTimeoutCts = new CancellationTokenSource();
                var token = modalTimeoutCts.Token;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(5000, token);
                        if (!token.IsCancellationRequested)
                        {
                            Logger.Warn("Loading modal safety timeout reached (5s). Dismissing modal so UI remains accessible.");
                            HideLoadingModal();
                        }
                    }
                    catch (TaskCanceledException) { }
                });
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to start modal timeout timer.");
            }

            var disp = Dispatcher ?? CoreApplication.MainView?.CoreWindow?.Dispatcher ?? Windows.UI.Xaml.Window.Current?.Dispatcher;
            if (disp != null)
            {
                if (!disp.HasThreadAccess)
                {
                    await disp.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (LoadingModalTitleText != null)
                        {
                            LoadingModalTitleText.Text = title;
                        }
                        if (LoadingModalSubtitleText != null)
                        {
                            LoadingModalSubtitleText.Text = subtitle;
                        }
                        if (LoadingModalProgressRing != null)
                        {
                            LoadingModalProgressRing.IsActive = true;
                        }
                        if (LoadingModalOverlay != null)
                        {
                            LoadingModalOverlay.Visibility = Visibility.Visible;
                        }
                    });
                }
                else
                {
                    if (LoadingModalTitleText != null)
                    {
                        LoadingModalTitleText.Text = title;
                    }
                    if (LoadingModalSubtitleText != null)
                    {
                        LoadingModalSubtitleText.Text = subtitle;
                    }
                    if (LoadingModalProgressRing != null)
                    {
                        LoadingModalProgressRing.IsActive = true;
                    }
                    if (LoadingModalOverlay != null)
                    {
                        LoadingModalOverlay.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private async void HideLoadingModal()
        {
            try
            {
                modalTimeoutCts?.Cancel();
                modalTimeoutCts?.Dispose();
                modalTimeoutCts = null;
            }
            catch { }

            var disp = Dispatcher ?? CoreApplication.MainView?.CoreWindow?.Dispatcher ?? Windows.UI.Xaml.Window.Current?.Dispatcher;
            if (disp != null)
            {
                if (!disp.HasThreadAccess)
                {
                    await disp.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (LoadingModalProgressRing != null)
                        {
                            LoadingModalProgressRing.IsActive = false;
                        }
                        if (LoadingModalOverlay != null)
                        {
                            LoadingModalOverlay.Visibility = Visibility.Collapsed;
                        }
                    });
                }
                else
                {
                    if (LoadingModalProgressRing != null)
                    {
                        LoadingModalProgressRing.IsActive = false;
                    }
                    if (LoadingModalOverlay != null)
                    {
                        LoadingModalOverlay.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void EnsureWidgetActivity()
        {
            if (widget == null)
            {
                return;
            }

            lock (widgetActivityLock)
            {
                if (widgetActivity == null)
                {
                    try
                    {
                        widgetActivity = new XboxGameBarWidgetActivity(widget, $"XboxGamingBarActivity_{Guid.NewGuid():N}");
                        Logger.Info("Created widget activity to keep Game Bar active.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Could not create widget activity: {ex.Message}");
                    }
                }
            }
        }

        private async Task EnsureHelperConnectionOrLaunchAsync()
        {
            if (App.Connection != null)
            {
                HideLoadingModal();
                return;
            }

            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
            {
                Logger.Info("App.Connection is NULL. Registering listeners.");
                App.AppServiceConnected -= GamingWidget_AppServiceConnected;
                App.AppServiceConnected += GamingWidget_AppServiceConnected;
                App.AppServiceDisconnected -= GamingWidget_AppServiceDisconnected;
                App.AppServiceDisconnected += GamingWidget_AppServiceDisconnected;

                if (IsHelperProcessRunning())
                {
                    Logger.Info("Helper process is already running. Skipping launch and waiting for AppService connection.");
                    if (!properties.HasCachedState)
                    {
                        ShowLoadingModal("Connecting to Helper...", "Establishing communication channel...");
                    }

                    // Reactively signal the running helper process to connect immediately (Task 4.4)
                    try
                    {
                        if (System.Threading.EventWaitHandle.TryOpenExisting(@"Global\CouchGamingBarHelper_ConnectEvent", out var connectEvent))
                        {
                            Logger.Info("Signaled running helper via CouchGamingBarHelper_ConnectEvent.");
                            connectEvent.Set();
                            connectEvent.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Could not signal CouchGamingBarHelper_ConnectEvent.");
                    }
                }
                else
                {
                    EnsureWidgetActivity();
                    if (!properties.HasCachedState)
                    {
                        ShowLoadingModal("Starting CouchGamingBar Helper...", "Initializing hardware sensors & power controls...");
                    }
                    try
                    {
                        Logger.Info("Helper process is not running. Launching full trust process (helper).");
                        await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                        Logger.Info("FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync() completed.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to launch full trust helper process.");
                        HideLoadingModal();
                    }
                }
            }
            else
            {
                Logger.Info("FullTrustAppContract not present. Cannot launch full trust helper process.");
                HideLoadingModal();
            }
        }

        private bool IsHelperProcessRunning()
        {
            try
            {
                if (System.Threading.Mutex.TryOpenExisting(@"Global\CouchGamingBarHelper_SingleInstance_Mutex", out var mutex))
                {
                    mutex?.Dispose();
                    return true;
                }
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                // Access denied means the mutex exists but belongs to an elevated process (e.g. Task Scheduler logon task)
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Exception checking helper single-instance mutex: {ex.Message}");
                return false;
            }
        }

        private void ReconnectAppService()
        {
            Logger.Info("Reconnect to existing AppServiceConnection.");
            App.AppServiceConnected -= GamingWidget_AppServiceConnected;
            App.AppServiceConnected += GamingWidget_AppServiceConnected;
            App.AppServiceDisconnected -= GamingWidget_AppServiceDisconnected;
            App.AppServiceDisconnected += GamingWidget_AppServiceDisconnected;
            GamingWidget_AppServiceConnected(null, null);
        }

        public async Task GamingWidget_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            if (widget != null)
            {
                await widget.CenterWindowAsync();
            }

            if (App.Connection != null)
            {
                Logger.Info("GamingWidget LeavingBackground, sync UI now.");
                await properties.Sync();
            }
            else
            {
                Logger.Info("GamingWidget LeavingBackground but not connected to full trust process. Checking helper status...");
                await EnsureHelperConnectionOrLaunchAsync();
            }

            isForeground.SetValue(true);
        }

        public void GamingWidget_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            Logger.Info("GamingWidget EnterBackground.");
            isForeground.SetValue(false);
        }

        private void AppTargetTracker_TargetChanged(XboxGameBarAppTargetTracker sender, object args)
        {
            var settingEnabled = appTargetTracker.Setting == XboxGameBarAppTargetSetting.Enabled;

            XboxGameBarAppTarget target = null;
            if (settingEnabled)
            {
                target = appTargetTracker.GetTarget();
            }

            if (target == null)
            {
                Logger.Debug("Found no target.");
                trackedGame.SetValue(new TrackedGame());
            }
            else
            {
                if (target.IsGame && !BlackListAppTrackerNames.Contains(target.DisplayName))
                {
                    Logger.Debug($"Tracked game DisplayName={target.DisplayName} AumId={target.AumId} TitleId={target.TitleId} IsFullscreen={target.IsFullscreen}");
                    trackedGame.SetValue(new TrackedGame(target.AumId, target.DisplayName, StringHelper.CleanStringForSerialization(target.TitleId), target.IsFullscreen));
                }
                else
                {
                    Logger.Debug($"Tracked non-game DisplayName={target.DisplayName} AumId={target.AumId} TitleId={target.TitleId} IsFullscreen={target.IsFullscreen}");
                    trackedGame.SetValue(new TrackedGame());
                }
            }
        }

        /// <summary>
        /// When the desktop process is connected, get ready to send/receive requests
        /// </summary>
        private async void GamingWidget_AppServiceConnected(object sender, AppServiceTriggerDetails _)
        {
            Logger.Info("GamingWidget AppService connected.");
            try
            {
                elevationFallbackCts?.Cancel();
                elevationFallbackCts?.Dispose();
                elevationFallbackCts = null;
                isRestartingElevated = false;

                var disp = Dispatcher ?? CoreApplication.MainView?.CoreWindow?.Dispatcher ?? Windows.UI.Xaml.Window.Current?.Dispatcher;
                if (disp != null && !disp.HasThreadAccess)
                {
                    await disp.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (OSDRestartElevatedButton != null) OSDRestartElevatedButton.IsEnabled = true;
                        if (FPSLimitRestartElevatedButton != null) FPSLimitRestartElevatedButton.IsEnabled = true;
                        HideLoadingModal();
                    });
                }
                else
                {
                    if (OSDRestartElevatedButton != null) OSDRestartElevatedButton.IsEnabled = true;
                    if (FPSLimitRestartElevatedButton != null) FPSLimitRestartElevatedButton.IsEnabled = true;
                    HideLoadingModal();
                }

                if (widget != null)
                {
                    EnsureWidgetActivity();

                    if (appTargetTracker == null)
                    {
                        appTargetTracker = new XboxGameBarAppTargetTracker(widget);
                        appTargetTracker.SettingChanged += AppTargetTracker_TargetChanged;

                        if (appTargetTracker.Setting == XboxGameBarAppTargetSetting.Enabled)
                        {
                            Logger.Info("Created new app target tracker to track current game.");
                            var initialTarget = appTargetTracker.GetTarget();
                            if (initialTarget.IsGame)
                            {
                                Logger.Info($"Initial tracked game DisplayName={initialTarget.DisplayName} AumId={initialTarget.AumId} TitleId={initialTarget.TitleId} IsFullscreen={initialTarget.IsFullscreen}");
                                trackedGame.SetValue(new TrackedGame(initialTarget.AumId, initialTarget.DisplayName, StringHelper.CleanStringForSerialization(initialTarget.TitleId), initialTarget.IsFullscreen));
                            }
                            else
                            {
                                trackedGame.SetValue(new TrackedGame());
                                Logger.Info("No initial game target found.");
                            }
                            appTargetTracker.TargetChanged += AppTargetTracker_TargetChanged;
                        }
                        else
                        {
                            Logger.Info("Created new app target tracker but not enabled.");
                        }
                    }
                    else
                    {
                        Logger.Info("App target tracker already created.");
                    }
                }
                else
                {
                    Logger.Info("No widget found, probably running as an app instead of Xbox Game Bar widget.");
                }

                await properties.Sync();
                UpdateFPSLimiterElevationUI();
                UpdateOSDStatusUI();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during GamingWidget_AppServiceConnected.");
            }
            finally
            {
                HideLoadingModal();
            }
        }

        /// <summary>
        /// When the desktop process is disconnected, reconnect if needed
        /// </summary>
        private async void GamingWidget_AppServiceDisconnected(object sender, EventArgs e)
        {
            helperElevation?.SetDisconnectedState();
            rtssElevation?.SetDisconnectedState();
            hardwareProviderProperty?.SetDisconnectedState();
            hardwareTelemetry?.SetDisconnectedState();
            gameInputStatus?.SetDisconnectedState();
            dualGpuSupport?.SetDisconnectedState();
            UpdateFPSLimiterElevationUI();
            UpdateOSDStatusUI();

            if (isRestartingElevated)
            {
                Logger.Info("AppService disconnected during elevated restart. Awaiting elevated helper connection without relaunching standard process.");
                ShowLoadingModal("Restarting Helper as Administrator...", "Awaiting elevated background service connection...");
                return;
            }

            var eventArgs = e as BackgroundTaskCancellationEventArgs;
            if (eventArgs != null && eventArgs.Reason != BackgroundTaskCancellationReason.Terminating)
            {
                ShowLoadingModal("Reconnecting to Helper...", "Waiting for background service...");
                if (IsHelperProcessRunning())
                {
                    Logger.Info($"AppService disconnected due to {eventArgs.Reason}, but helper process is already running. Skipping relaunch.");
                }
                else
                {
                    EnsureWidgetActivity();
                    try
                    {
                        Logger.Info($"AppService disconnected due to {eventArgs.Reason}, trying to relaunch.");
                        await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to relaunch full trust helper process on disconnect.");
                        HideLoadingModal();
                    }
                }
            }
            else
            {
                Logger.Info($"AppService disconnected due to {eventArgs.Reason}, not relaunching.");
                HideLoadingModal();
                lock (widgetActivityLock)
                {
                    if (widgetActivity != null)
                    {
                        try
                        {
                            widgetActivity.Complete();
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"Could not complete widget activity: {ex.Message}");
                        }
                        widgetActivity = null;
                        Logger.Info("Stopped widget activity on terminating disconnect.");
                    }
                }
            }
        }

        private async void GamingWidget_RequestedThemeChanged(XboxGameBarWidget sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SetBackgroundColor();
            });
        }

        private async void GamingWidget_SettingsClicked(XboxGameBarWidget sender, object args)
        {
            await widget.ActivateSettingsAsync();
        }

        private void SetBackgroundColor()
        {
            this.RequestedTheme = widget.RequestedTheme;
            var themeBrush = (widget.RequestedTheme == ElementTheme.Dark) ? widgetDarkThemeBrush : widgetLightThemeBrush;
            RootGrid.Background = themeBrush;
            if (LoadingModalOverlay != null)
            {
                LoadingModalOverlay.Background = themeBrush;
            }
        }

        /// <summary>
        /// Handle calculation request from desktop process
        /// (dummy scenario to show that connection is bi-directional)
        /// </summary>
        public async Task RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            //Logger.Info($"GamingWidget received message {args.Request.Message.ToDebugString()} from helper.");
            await properties.OnRequestReceived(new WidgetAppServiceRequest(args.Request));
        }

        private bool isUpdatingJudderFreeSlider = false;

        private void JudderFreeFPSToggle_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateFPSLimitSliderJudderFree();
        }

        private void FPSLimitJudderFreeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (isUpdatingJudderFreeSlider) return;

            if (refreshRate != null && refreshRate.Value > 0)
            {
                int maxRefresh = refreshRate.Value;
                var judderFreeValues = ViewModel.Performance.GetJudderFreeValues(maxRefresh, 30);
                int idx = (int)Math.Round(e.NewValue);
                if (idx >= 0 && idx < judderFreeValues.Count)
                {
                    FPSLimitSlider.Value = judderFreeValues[idx];
                }
            }
        }

        private async void UpdateFPSLimitSliderJudderFree()
        {
            if (Dispatcher != null && !Dispatcher.HasThreadAccess)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateFPSLimitSliderJudderFree());
                return;
            }

            if (FPSLimitSlider == null || FPSLimitJudderFreeSlider == null || refreshRate == null || refreshRate.Value <= 0)
                return;

            int activeRefresh = refreshRate.Value;
            int maxScreenRefresh = GetMaxScreenRefreshRate();
            bool isLimitFPSOn = LimitFPSToggle != null && LimitFPSToggle.IsOn;
            bool isJudderFree = JudderFreeFPSToggle != null && JudderFreeFPSToggle.IsOn;

            if (IsElevationMismatch || !IsRTSSRunning || !isLimitFPSOn)
            {
                FPSLimitSlider.Visibility = Visibility.Collapsed;
                FPSLimitJudderFreeSlider.Visibility = Visibility.Collapsed;
                FPSLimitJudderFreeCanvas.Visibility = Visibility.Collapsed;
                return;
            }

            FPSLimitJudderFreeCanvas.Visibility = Visibility.Visible;

            if (isJudderFree)
            {
                var judderFreeValues = ViewModel.Performance.GetJudderFreeValues(activeRefresh, 30);
                if (judderFreeValues.Count > 0)
                {
                    FPSLimitSlider.Visibility = Visibility.Collapsed;
                    FPSLimitJudderFreeSlider.Visibility = Visibility.Visible;

                    isUpdatingJudderFreeSlider = true;
                    FPSLimitJudderFreeSlider.Minimum = 0;
                    FPSLimitJudderFreeSlider.Maximum = judderFreeValues.Count - 1;
                    FPSLimitJudderFreeSlider.StepFrequency = 1;
                    FPSLimitJudderFreeSlider.TickFrequency = 1;
                    FPSLimitJudderFreeSlider.SnapsTo = SliderSnapsTo.StepValues;
                    FPSLimitJudderFreeSlider.TickPlacement = TickPlacement.None;

                    double currentVal = fpsLimit.Value > 0 ? fpsLimit.Value : (FPSLimitSlider.Value > 0 && FPSLimitSlider.Value != 60 ? FPSLimitSlider.Value : activeRefresh);
                    int closestVal = judderFreeValues.OrderBy(v => Math.Abs(v - currentVal)).First();
                    int idx = judderFreeValues.IndexOf(closestVal);
                    if (idx < 0) idx = 0;

                    FPSLimitJudderFreeSlider.Value = idx;
                    fpsLimit.SetUIValueSilently(judderFreeValues[idx]);
                    isUpdatingJudderFreeSlider = false;
                }
                else
                {
                    ShowStandardFPSLimitSlider(maxScreenRefresh);
                }
            }
            else
            {
                ShowStandardFPSLimitSlider(maxScreenRefresh);
            }

            RenderJudderFreeMarkers();
        }

        private int GetMaxScreenRefreshRate()
        {
            int maxRate = refreshRate != null && refreshRate.Value > 0 ? refreshRate.Value : 60;
            if (refreshRates != null && refreshRates.Value != null && refreshRates.Value.Count > 0)
            {
                maxRate = Math.Max(maxRate, refreshRates.Value.Max());
            }
            return Math.Max(30, maxRate);
        }

        private void UpdateQuickDisplayInfo()
        {
            if (QuickDisplayInfoText == null) return;
            string res = (resolution != null && resolution.Value.Width > 0) ? resolution.Value.ToString() : "Display";
            int hz = refreshRate != null ? refreshRate.Value : 0;
            string baseDisplay = hz > 0 ? $"{res} @ {hz} Hz" : res;

            bool isSupported = hdrSupported != null && hdrSupported.Value;
            bool isEnabled = hdr != null && hdr.Value;

            if (isSupported)
            {
                QuickDisplayInfoText.Text = $"{baseDisplay} • {(isEnabled ? "HDR On" : "HDR Off")}";
            }
            else
            {
                QuickDisplayInfoText.Text = $"{baseDisplay} • SDR";
            }

            if (QuickHDRStatusBadge != null && QuickHDRStatusBadgeText != null)
            {
                var greenBrush = Resources.ContainsKey("XboxGreenBrush")
                    ? (SolidColorBrush)Resources["XboxGreenBrush"]
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 124, 65));
                var offBgBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 58, 58)); // #3A3A3A
                var offTextBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 204, 204, 204)); // #CCCCCC
                var whiteBrush = new SolidColorBrush(Windows.UI.Colors.White);

                if (isSupported)
                {
                    if (isEnabled)
                    {
                        QuickHDRStatusBadgeText.Text = "HDR ON";
                        QuickHDRStatusBadgeText.Foreground = whiteBrush;
                        QuickHDRStatusBadge.Background = greenBrush;
                        QuickHDRStatusBadge.BorderBrush = greenBrush;
                    }
                    else
                    {
                        QuickHDRStatusBadgeText.Text = "HDR OFF";
                        QuickHDRStatusBadgeText.Foreground = offTextBrush;
                        QuickHDRStatusBadge.Background = offBgBrush;
                        QuickHDRStatusBadge.BorderBrush = offBgBrush;
                    }
                }
                else
                {
                    QuickHDRStatusBadgeText.Text = "SDR";
                    QuickHDRStatusBadgeText.Foreground = offTextBrush;
                    QuickHDRStatusBadge.Background = offBgBrush;
                    QuickHDRStatusBadge.BorderBrush = offBgBrush;
                }
            }
        }

        private void UpdateQuickRTSSStatus()
        {
            if (QuickRTSSStatusText == null || QuickRTSSStatusDot == null) return;
            bool isRunning = IsRTSSRunning;
            ViewModel.QuickAccess.UpdateRtssSummary(App.Connection != null, isRunning, rtssElevation != null ? rtssElevation.Value : 0, IsElevationMismatch);
            QuickRTSSStatusText.Text = isRunning ? "RTSS Active" : "RTSS Offline";
            var greenBrush = Resources.ContainsKey("XboxGreenBrush") ? (SolidColorBrush)Resources["XboxGreenBrush"] : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 124, 65));
            var grayBrush = new SolidColorBrush(Windows.UI.Colors.Gray);
            QuickRTSSStatusDot.Fill = isRunning ? greenBrush : grayBrush;
        }

        private void ShowStandardFPSLimitSlider(int maxRefresh)
        {
            FPSLimitSlider.Visibility = Visibility.Visible;
            FPSLimitJudderFreeSlider.Visibility = Visibility.Collapsed;
            FPSLimitSlider.Minimum = 30;
            FPSLimitSlider.Maximum = maxRefresh;
            if (FPSLimitSlider.Value < 30) FPSLimitSlider.Value = 30;
            if (FPSLimitSlider.Value > maxRefresh) FPSLimitSlider.Value = maxRefresh;
            FPSLimitSlider.TickPlacement = TickPlacement.BottomRight;
        }

        public void UpdateFPSLimiterElevationUI()
        {
            if (Dispatcher != null && !Dispatcher.HasThreadAccess)
            {
                var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateFPSLimiterElevationUI());
                return;
            }

            if (FPSLimitElevationWarningCard == null || FPSLimitElevationWarningBadge == null) return;

            bool isRtssRunning = IsRTSSRunning;
            bool isMismatch = IsElevationMismatch;

            if (App.Connection != null && !isRtssRunning)
            {
                limitFPS?.EnableControl(false);
                if (FPSLimitValueBadge != null)
                {
                    FPSLimitValueBadge.Visibility = Visibility.Collapsed;
                }
                if (FPSLimitElevationWarningBadge != null)
                {
                    FPSLimitElevationWarningBadge.Visibility = Visibility.Collapsed;
                }
                if (FPSLimitElevationWarningCard != null)
                {
                    FPSLimitElevationWarningCard.Visibility = Visibility.Collapsed;
                }

                if (FPSLimitRTSSWarningBadge != null)
                {
                    if (FPSLimitRTSSWarningBadgeText != null)
                    {
                        FPSLimitRTSSWarningBadgeText.Text = (rtssElevation != null && rtssElevation.Value == -1)
                            ? "NOT INSTALLED"
                            : "RTSS NOT RUNNING";
                    }
                    FPSLimitRTSSWarningBadge.Visibility = Visibility.Visible;
                }

                if (FPSLimitRTSSWarningCard != null)
                {
                    if (rtssElevation != null && rtssElevation.Value == -1)
                    {
                        if (FPSLimitRTSSWarningTitleText != null)
                        {
                            FPSLimitRTSSWarningTitleText.Text = "RTSS Not Installed";
                        }
                        if (FPSLimitRTSSWarningDescText != null)
                        {
                            FPSLimitRTSSWarningDescText.Text = "RivaTuner Statistics Server was not detected on this system. Install RTSS to enable framerate limiting and frame pacing.";
                        }
                    }
                    else
                    {
                        if (FPSLimitRTSSWarningTitleText != null)
                        {
                            FPSLimitRTSSWarningTitleText.Text = "RTSS is Not Running";
                        }
                        if (FPSLimitRTSSWarningDescText != null)
                        {
                            FPSLimitRTSSWarningDescText.Text = "RivaTuner Statistics Server is required for framerate limiting and frame pacing. Start RTSS to enable framerate controls.";
                        }
                    }
                    FPSLimitRTSSWarningCard.Visibility = Visibility.Visible;
                }

                if (FPSLimitSubtitleText != null)
                {
                    FPSLimitSubtitleText.Text = (rtssElevation != null && rtssElevation.Value == -1)
                        ? "RivaTuner Statistics Server is not installed"
                        : "Requires RivaTuner Statistics Server to be running";
                }

                if (FPSLimitSettingsContainer != null)
                {
                    FPSLimitSettingsContainer.Visibility = Visibility.Collapsed;
                }
                if (FPSLimitSlider != null)
                {
                    FPSLimitSlider.IsEnabled = false;
                }
            }
            else if (isMismatch)
            {
                limitFPS?.EnableControl(false);
                if (FPSLimitRTSSWarningBadge != null)
                {
                    FPSLimitRTSSWarningBadge.Visibility = Visibility.Collapsed;
                }
                if (FPSLimitRTSSWarningCard != null)
                {
                    FPSLimitRTSSWarningCard.Visibility = Visibility.Collapsed;
                }

                FPSLimitElevationWarningBadge.Visibility = Visibility.Visible;
                if (FPSLimitValueBadge != null)
                {
                    FPSLimitValueBadge.Visibility = Visibility.Collapsed;
                }
                FPSLimitElevationWarningCard.Visibility = Visibility.Visible;
                if (FPSLimitSubtitleText != null)
                {
                    FPSLimitSubtitleText.Text = "Requires Administrator privileges to modify RTSS";
                }
                if (FPSLimitSettingsContainer != null)
                {
                    FPSLimitSettingsContainer.Visibility = Visibility.Collapsed;
                }
                if (FPSLimitSlider != null)
                {
                    FPSLimitSlider.IsEnabled = false;
                }
            }
            else
            {
                if (FPSLimitRTSSWarningBadge != null)
                {
                    FPSLimitRTSSWarningBadge.Visibility = Visibility.Collapsed;
                }
                if (FPSLimitRTSSWarningCard != null)
                {
                    FPSLimitRTSSWarningCard.Visibility = Visibility.Collapsed;
                }
                FPSLimitElevationWarningBadge.Visibility = Visibility.Collapsed;
                FPSLimitElevationWarningCard.Visibility = Visibility.Collapsed;

                if (FPSLimitSubtitleText != null)
                {
                    FPSLimitSubtitleText.Text = "Cap framerate via RTSS to stabilize frametimes";
                }
                if (App.Connection != null)
                {
                    limitFPS?.EnableControl(true);
                }
                if (FPSLimitSettingsContainer != null)
                {
                    FPSLimitSettingsContainer.Visibility = (LimitFPSToggle != null && LimitFPSToggle.IsOn) 
                        ? Visibility.Visible 
                        : Visibility.Collapsed;
                }
                if (FPSLimitValueBadge != null)
                {
                    FPSLimitValueBadge.Visibility = (LimitFPSToggle != null && LimitFPSToggle.IsOn) 
                        ? Visibility.Visible 
                        : Visibility.Collapsed;
                }
            }
            UpdateFPSLimitSliderJudderFree();
            UpdateQuickFPSLimitButtonUI();
        }

        public void UpdateOSDStatusUI()
        {
            if (Dispatcher != null && !Dispatcher.HasThreadAccess)
            {
                var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateOSDStatusUI());
                return;
            }

            if (OSDRTSSWarningCard == null || OSDRTSSWarningBadge == null) return;

            UpdateQuickRTSSStatus();

            bool isRtssRunning = IsRTSSRunning;

            if (App.Connection != null && !isRtssRunning)
            {
                onScreenDisplayProviderInstalled?.EnableControl(false);
                osdTextSize?.EnableControl(false);

                if (OSDElevationWarningBadge != null)
                {
                    OSDElevationWarningBadge.Visibility = Visibility.Collapsed;
                }
                if (OSDElevationWarningCard != null)
                {
                    OSDElevationWarningCard.Visibility = Visibility.Collapsed;
                }

                if (OSDRTSSWarningBadge != null)
                {
                    if (OSDRTSSWarningBadgeText != null)
                    {
                        OSDRTSSWarningBadgeText.Text = (rtssElevation != null && rtssElevation.Value == -1)
                            ? "NOT INSTALLED"
                            : "RTSS NOT RUNNING";
                    }
                    OSDRTSSWarningBadge.Visibility = Visibility.Visible;
                }

                if (OSDRTSSWarningCard != null)
                {
                    if (rtssElevation != null && rtssElevation.Value == -1)
                    {
                        if (OSDRTSSWarningTitleText != null)
                        {
                            OSDRTSSWarningTitleText.Text = "RTSS Not Installed";
                        }
                        if (OSDRTSSWarningDescText != null)
                        {
                            OSDRTSSWarningDescText.Text = "RivaTuner Statistics Server was not detected on this system. Install RTSS to enable the overlay.";
                        }
                    }
                    else
                    {
                        if (OSDRTSSWarningTitleText != null)
                        {
                            OSDRTSSWarningTitleText.Text = "RTSS is Not Running";
                        }
                        if (OSDRTSSWarningDescText != null)
                        {
                            OSDRTSSWarningDescText.Text = "RivaTuner Statistics Server is required for on-screen metrics display. Start RTSS to enable the overlay.";
                        }
                    }
                    OSDRTSSWarningCard.Visibility = Visibility.Visible;
                }

                if (OSDSubtitleText != null)
                {
                    OSDSubtitleText.Text = (rtssElevation != null && rtssElevation.Value == -1)
                        ? "RivaTuner Statistics Server is not installed"
                        : "Requires RivaTuner Statistics Server to be running";
                }

                if (OSDControlsContainer != null)
                {
                    OSDControlsContainer.Visibility = Visibility.Collapsed;
                }
                if (PerformanceOverlaySlider != null)
                {
                    PerformanceOverlaySlider.IsEnabled = false;
                }
                if (OverlayTextSizeSlider != null)
                {
                    OverlayTextSizeSlider.IsEnabled = false;
                }
            }
            else
            {
                if (OSDRTSSWarningBadge != null)
                {
                    OSDRTSSWarningBadge.Visibility = Visibility.Collapsed;
                }
                if (OSDRTSSWarningCard != null)
                {
                    OSDRTSSWarningCard.Visibility = Visibility.Collapsed;
                }
                if (OSDElevationWarningBadge != null)
                {
                    OSDElevationWarningBadge.Visibility = Visibility.Collapsed;
                }
                if (OSDElevationWarningCard != null)
                {
                    OSDElevationWarningCard.Visibility = Visibility.Collapsed;
                }

                if (OSDSubtitleText != null)
                {
                    OSDSubtitleText.Text = "RTSS / ADLX in-game metrics overlay";
                }

                if (OSDControlsContainer != null)
                {
                    OSDControlsContainer.Visibility = Visibility.Visible;
                }

                if (App.Connection != null)
                {
                    onScreenDisplayProviderInstalled?.EnableControl(true);
                    osdTextSize?.EnableControl(true);
                }
            }
            UpdateQuickOSDButtonUI((int)PerformanceOverlaySlider.Value);
        }

        private async void RenderJudderFreeMarkers()
        {
            if (Dispatcher != null && !Dispatcher.HasThreadAccess)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => RenderJudderFreeMarkers());
                return;
            }

            if (FPSLimitJudderFreeCanvas == null || refreshRate == null || refreshRate.Value <= 0)
                return;

            FPSLimitJudderFreeCanvas.Children.Clear();

            int maxRefresh = refreshRate.Value;
            double canvasWidth = FPSLimitJudderFreeCanvas.ActualWidth;
            if (canvasWidth <= 0)
                return;

            bool isJudderFree = JudderFreeFPSToggle != null && JudderFreeFPSToggle.IsOn;
            double min = FPSLimitSlider != null ? FPSLimitSlider.Minimum : 30;
            double max = FPSLimitSlider != null ? FPSLimitSlider.Maximum : maxRefresh;

            var markers = ViewModel.Performance.CalculateMarkers(maxRefresh, isJudderFree, min, max);
            foreach (var marker in markers)
            {
                double xPos = marker.XPercent * canvasWidth;
                AddCanvasLabel(marker.Value.ToString(), xPos, canvasWidth);
            }
        }

        private void AddCanvasLabel(string text, double xPos, double canvasWidth)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = 16,
                Foreground = (Windows.UI.Xaml.Media.Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"]
            };

            textBlock.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            double textWidth = textBlock.DesiredSize.Width;

            double leftPos = xPos - (textWidth / 2.0);
            if (leftPos < 0) leftPos = 0;
            if (leftPos + textWidth > canvasWidth) leftPos = canvasWidth - textWidth;

            Canvas.SetLeft(textBlock, leftPos);
            Canvas.SetTop(textBlock, 0);

            FPSLimitJudderFreeCanvas.Children.Add(textBlock);
        }

        #region Shortcuts Logic

        private List<ShortcutItem> shortcuts = new List<ShortcutItem>();
        private ShortcutItem editingShortcut = null;
        private bool isRecordingKeys = false;

        private void InitializeShortcuts()
        {
            try
            {
                if (EditorPresetComboBox != null)
                {
                    EditorPresetComboBox.ItemsSource = ShortcutManager.Presets;
                    EditorPresetComboBox.SelectedIndex = 0;
                    WidgetComboBoxSelectionProperty<int>.AttachNavigationHandler(EditorPresetComboBox);
                }

                if (EditorIconComboBox != null)
                {
                    EditorIconComboBox.ItemsSource = ShortcutManager.Icons;
                    EditorIconComboBox.SelectedIndex = 0;
                    WidgetComboBoxSelectionProperty<int>.AttachNavigationHandler(EditorIconComboBox);
                }

                if (EditorKeyComboBox != null)
                {
                    EditorKeyComboBox.ItemsSource = ShortcutManager.AvailableKeys;
                    EditorKeyComboBox.SelectedIndex = 0;
                    WidgetComboBoxSelectionProperty<int>.AttachNavigationHandler(EditorKeyComboBox);
                }

                shortcuts = ShortcutManager.LoadShortcuts();
                RefreshShortcutsGrid();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize shortcuts.");
            }
        }

        private void RefreshShortcutsGrid()
        {
            if (ShortcutsGridView != null)
            {
                ShortcutsGridView.ItemsSource = null;
                ShortcutsGridView.ItemsSource = shortcuts;
            }

            if (NoShortcutsPlaceholder != null)
            {
                NoShortcutsPlaceholder.Visibility = (shortcuts == null || shortcuts.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async void ShortcutsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ShortcutItem shortcut)
            {
                Logger.Info($"Shortcut clicked via ItemClick (gamepad/grid): '{shortcut.Name}' ({shortcut.DisplayKeyCombo})");
                await SendShortcutAsync(shortcut);
            }
        }

        private void ShortcutsGridView_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.GamepadMenu || e.Key == VirtualKey.GamepadX || e.Key == VirtualKey.Application)
            {
                var focused = FocusManager.GetFocusedElement() as DependencyObject;
                DependencyObject current = focused;
                while (current != null && !(current is GridViewItem) && current != ShortcutsGridView)
                {
                    current = VisualTreeHelper.GetParent(current);
                }

                if (current is GridViewItem gvi)
                {
                    var btn = FindVisualChild<Button>(gvi);
                    if (btn?.ContextFlyout is MenuFlyout flyout)
                    {
                        flyout.ShowAt(btn);
                        e.Handled = true;
                    }
                }
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private async void ShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ShortcutItem shortcut)
            {
                Logger.Info($"Shortcut clicked: '{shortcut.Name}' ({shortcut.DisplayKeyCombo})");
                await SendShortcutAsync(shortcut);
            }
        }

        private void ShortcutButton_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.ContextFlyout is MenuFlyout flyout)
            {
                flyout.ShowAt(fe);
                e.Handled = true;
            }
        }

        private void ShortcutButton_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == Windows.UI.Input.HoldingState.Started && sender is FrameworkElement fe && fe.ContextFlyout is MenuFlyout flyout)
            {
                flyout.ShowAt(fe);
                e.Handled = true;
            }
        }

        private async void MenuExecute_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is ShortcutItem shortcut)
            {
                await SendShortcutAsync(shortcut);
            }
        }

        private void MenuEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is ShortcutItem shortcut)
            {
                OpenShortcutEditor(shortcut);
            }
        }

        private void MenuDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is ShortcutItem shortcut)
            {
                shortcuts.RemoveAll(s => s.Id == shortcut.Id);
                ShortcutManager.SaveShortcuts(shortcuts);
                RefreshShortcutsGrid();
            }
        }

        private void AddShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenShortcutEditor(null);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in AddShortcutButton_Click.");
            }
        }

        private void OpenShortcutEditor(ShortcutItem itemToEdit)
        {
            try
            {
                editingShortcut = itemToEdit;
                isRecordingKeys = false;
                UpdateRecordButtonState(false);

                if (itemToEdit == null)
                {
                    if (EditorModalTitleText != null) EditorModalTitleText.Text = "Add Shortcut";
                    if (EditorPresetComboBox != null) EditorPresetComboBox.SelectedIndex = 0;
                    if (EditorNameTextBox != null) EditorNameTextBox.Text = "";
                    if (EditorIconComboBox != null) EditorIconComboBox.SelectedIndex = 0;
                    if (EditorCtrlToggle != null) EditorCtrlToggle.IsChecked = false;
                    if (EditorAltToggle != null) EditorAltToggle.IsChecked = false;
                    if (EditorShiftToggle != null) EditorShiftToggle.IsChecked = false;
                    if (EditorWinToggle != null) EditorWinToggle.IsChecked = false;
                    if (EditorKeyComboBox != null) EditorKeyComboBox.SelectedIndex = 0;
                }
                else
                {
                    if (EditorModalTitleText != null) EditorModalTitleText.Text = "Edit Shortcut";
                    if (EditorPresetComboBox != null) EditorPresetComboBox.SelectedIndex = 0;
                    if (EditorNameTextBox != null) EditorNameTextBox.Text = itemToEdit.Name ?? "";

                    if (EditorIconComboBox != null)
                    {
                        int iconIdx = ShortcutManager.Icons.FindIndex(ic => ic.Glyph == itemToEdit.IconGlyph);
                        EditorIconComboBox.SelectedIndex = iconIdx >= 0 ? iconIdx : 0;
                    }

                    if (EditorCtrlToggle != null) EditorCtrlToggle.IsChecked = itemToEdit.Ctrl;
                    if (EditorAltToggle != null) EditorAltToggle.IsChecked = itemToEdit.Alt;
                    if (EditorShiftToggle != null) EditorShiftToggle.IsChecked = itemToEdit.Shift;
                    if (EditorWinToggle != null) EditorWinToggle.IsChecked = itemToEdit.Win;

                    if (EditorKeyComboBox != null)
                    {
                        int keyIdx = ShortcutManager.AvailableKeys.FindIndex(k => k.VirtualKey == itemToEdit.VirtualKey);
                        EditorKeyComboBox.SelectedIndex = keyIdx >= 0 ? keyIdx : 0;
                    }
                }

                UpdateEditorPreview();
                if (ShortcutEditorOverlay != null)
                {
                    ShortcutEditorOverlay.Visibility = Visibility.Visible;
                    try
                    {
                        EditorNameTextBox?.Focus(FocusState.Programmatic);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Failed to focus EditorNameTextBox.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in OpenShortcutEditor.");
            }
        }

        private void EditorPresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (EditorPresetComboBox?.SelectedItem is ShortcutPreset preset && preset.VirtualKey > 0)
                {
                    if (EditorNameTextBox != null) EditorNameTextBox.Text = preset.Name;
                    if (EditorCtrlToggle != null) EditorCtrlToggle.IsChecked = preset.Ctrl;
                    if (EditorAltToggle != null) EditorAltToggle.IsChecked = preset.Alt;
                    if (EditorShiftToggle != null) EditorShiftToggle.IsChecked = preset.Shift;
                    if (EditorWinToggle != null) EditorWinToggle.IsChecked = preset.Win;

                    if (EditorIconComboBox != null)
                    {
                        int iconIdx = ShortcutManager.Icons.FindIndex(ic => ic.Glyph == preset.IconGlyph);
                        if (iconIdx >= 0) EditorIconComboBox.SelectedIndex = iconIdx;
                    }

                    if (EditorKeyComboBox != null)
                    {
                        int keyIdx = ShortcutManager.AvailableKeys.FindIndex(k => k.VirtualKey == preset.VirtualKey);
                        if (keyIdx >= 0) EditorKeyComboBox.SelectedIndex = keyIdx;
                    }

                    UpdateEditorPreview();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in EditorPresetComboBox_SelectionChanged.");
            }
        }

        private void EditorInput_Changed(object sender, object e)
        {
            try
            {
                UpdateEditorPreview();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in EditorInput_Changed.");
            }
        }

        private void EditorModifier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateEditorPreview();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in EditorModifier_Click.");
            }
        }

        private void EditorKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                UpdateEditorPreview();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in EditorKeyComboBox_SelectionChanged.");
            }
        }

        private void UpdateEditorPreview()
        {
            try
            {
                string name = string.IsNullOrWhiteSpace(EditorNameTextBox?.Text) ? "Shortcut Name" : EditorNameTextBox.Text;
                string iconGlyph = (EditorIconComboBox?.SelectedItem is ShortcutIconOption ic) ? ic.Glyph : "\uE765";
                bool ctrl = EditorCtrlToggle?.IsChecked == true;
                bool alt = EditorAltToggle?.IsChecked == true;
                bool shift = EditorShiftToggle?.IsChecked == true;
                bool win = EditorWinToggle?.IsChecked == true;
                int vk = (EditorKeyComboBox?.SelectedItem is ShortcutKeyOption ko) ? ko.VirtualKey : 0;

                var tempItem = new ShortcutItem(name, iconGlyph, ctrl, alt, shift, win, vk);

                if (EditorPreviewName != null) EditorPreviewName.Text = name;
                if (EditorPreviewIcon != null) EditorPreviewIcon.Glyph = iconGlyph;
                if (EditorHeaderIcon != null) EditorHeaderIcon.Glyph = iconGlyph;
                if (EditorPreviewComboText != null) EditorPreviewComboText.Text = tempItem.DisplayKeyCombo;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in UpdateEditorPreview.");
            }
        }

        private void EditorRecordKeysButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                isRecordingKeys = !isRecordingKeys;
                UpdateRecordButtonState(isRecordingKeys);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in EditorRecordKeysButton_Click.");
            }
        }

        private void UpdateRecordButtonState(bool recording)
        {
            try
            {
                if (EditorRecordText != null)
                {
                    EditorRecordText.Text = recording ? "Listening... Press keys on keyboard" : "Press Keys to Record";
                }
                if (EditorRecordKeysButton != null)
                {
                    if (recording)
                    {
                        EditorRecordKeysButton.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 124, 65));
                    }
                    else
                    {
                        EditorRecordKeysButton.ClearValue(Button.BackgroundProperty);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in UpdateRecordButtonState.");
            }
        }

        private async void EditorTestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool ctrl = EditorCtrlToggle?.IsChecked == true;
                bool alt = EditorAltToggle?.IsChecked == true;
                bool shift = EditorShiftToggle?.IsChecked == true;
                bool win = EditorWinToggle?.IsChecked == true;
                int vk = (EditorKeyComboBox?.SelectedItem is ShortcutKeyOption ko) ? ko.VirtualKey : 0;

                if (vk > 0 || ctrl || alt || shift || win)
                {
                    var tempItem = new ShortcutItem("Test", "", ctrl, alt, shift, win, vk);
                    await SendShortcutAsync(tempItem);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in EditorTestButton_Click.");
            }
        }

        private void EditorCancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                isRecordingKeys = false;
                UpdateRecordButtonState(false);
                if (ShortcutEditorOverlay != null)
                {
                    ShortcutEditorOverlay.Visibility = Visibility.Collapsed;
                }
                AddShortcutButton?.Focus(FocusState.Programmatic);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in EditorCancelButton_Click.");
            }
        }

        private void EditorSaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = EditorNameTextBox?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = "Shortcut";
                }

                string iconGlyph = (EditorIconComboBox?.SelectedItem is ShortcutIconOption ic) ? ic.Glyph : "\uE765";
                bool ctrl = EditorCtrlToggle?.IsChecked == true;
                bool alt = EditorAltToggle?.IsChecked == true;
                bool shift = EditorShiftToggle?.IsChecked == true;
                bool win = EditorWinToggle?.IsChecked == true;
                int vk = (EditorKeyComboBox?.SelectedItem is ShortcutKeyOption ko) ? ko.VirtualKey : 0;

                if (editingShortcut != null)
                {
                    editingShortcut.Name = name;
                    editingShortcut.IconGlyph = iconGlyph;
                    editingShortcut.Ctrl = ctrl;
                    editingShortcut.Alt = alt;
                    editingShortcut.Shift = shift;
                    editingShortcut.Win = win;
                    editingShortcut.VirtualKey = vk;
                }
                else
                {
                    var newItem = new ShortcutItem(name, iconGlyph, ctrl, alt, shift, win, vk);
                    shortcuts.Add(newItem);
                }

                ShortcutManager.SaveShortcuts(shortcuts);
                RefreshShortcutsGrid();

                if (ShortcutEditorOverlay != null)
                {
                    ShortcutEditorOverlay.Visibility = Visibility.Collapsed;
                }
                AddShortcutButton?.Focus(FocusState.Programmatic);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in EditorSaveButton_Click.");
            }
        }

        private string lastExecutedShortcutId = null;
        private DateTime lastExecutedShortcutTime = DateTime.MinValue;

        private async Task SendShortcutAsync(ShortcutItem shortcut)
        {
            if (shortcut == null) return;

            if (shortcut.Id == lastExecutedShortcutId && (DateTime.UtcNow - lastExecutedShortcutTime).TotalMilliseconds < 300)
            {
                Logger.Debug($"Ignoring rapid duplicate shortcut execution for '{shortcut.Name}'.");
                return;
            }
            lastExecutedShortcutId = shortcut.Id;
            lastExecutedShortcutTime = DateTime.UtcNow;

            Logger.Info($"SendShortcutAsync: Sending payload '{shortcut.ToPayloadString()}'");

            if (App.Connection != null)
            {
                try
                {
                    var valueSet = new ValueSet();
                    valueSet.Add(nameof(Command), (int)Command.Set);
                    valueSet.Add(nameof(Function), (int)Function.SendShortcut);
                    valueSet.Add(nameof(Content), shortcut.ToPayloadString());
                    valueSet.Add(nameof(UpdatedTime), DateTime.UtcNow.Ticks);

                    var response = await App.Connection.SendMessageAsync(valueSet);
                    Logger.Info($"SendMessageAsync SendShortcut status: {response?.Status}");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to send shortcut payload to helper.");
                }
            }
            else
            {
                Logger.Warn("App.Connection is null when attempting to execute shortcut.");
            }
        }

        #endregion
    }
}
