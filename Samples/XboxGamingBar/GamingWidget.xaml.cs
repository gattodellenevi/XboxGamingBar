using Microsoft.Gaming.XboxGameBar;
using NLog;
using Shared.Data;
using Shared.Enums;
using Shared.Utilities;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
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
        public XboxGameBarWidgetActivity WidgetActivity { get { return widgetActivity; } }
        private XboxGameBarAppTargetTracker appTargetTracker = null;

        private SolidColorBrush widgetDarkThemeBrush = null;
        private SolidColorBrush widgetLightThemeBrush = null;

        // Properties
        private readonly OSDProperty osd;
        private readonly RunningGameProperty runningGame;
        private readonly PerGameProfileProperty perGameProfile;
        private readonly CPUBoostProperty cpuBoost;
        private readonly CPUEPPProperty cpuEPP;
        private readonly SetCPUEPPProperty setCPUEPP;
        private readonly LimitCPUClockProperty limitCPUClock;
        private readonly CPUClockMaxProperty cpuClockMax;
        private readonly LimitFPSProperty limitFPS;
        private readonly RefreshRatesProperty refreshRates;
        private readonly RefreshRateProperty refreshRate;
        private readonly ResolutionProperty resolution;
        private readonly ResolutionsProperty resolutions;
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
        private bool isElevationFlyoutOpen = false;

        private readonly WidgetProperties properties;

        public GamingWidget()
        {
            InitializeComponent();
            osd = new OSDProperty(0, PerformanceOverlaySlider, this);
            osdTextSize = new OSDTextSizeProperty(100, OverlayTextSizeSlider, this);
            runningGame = new RunningGameProperty(RunningGameText, PerGameProfileToggle, this);
            perGameProfile = new PerGameProfileProperty(PerGameProfileToggle, this);
            cpuBoost = new CPUBoostProperty(CPUBoostToggle, this);
            cpuEPP = new CPUEPPProperty(80, CPUEPPSlider, this);
            setCPUEPP = new SetCPUEPPProperty(SetCPUEPPToggle, this);
            limitCPUClock = new LimitCPUClockProperty(LimitCPUClockToggle, this);
            cpuClockMax = new CPUClockMaxProperty(CPUClockMaxSlider, this);
            refreshRates = new RefreshRatesProperty(RefreshRatesComboBox, this);
            refreshRate = new RefreshRateProperty(RefreshRatesComboBox, this, FPSLimitSlider);
            resolutions = new ResolutionsProperty(ResolutionsComboBox, this);
            resolution = new ResolutionProperty(ResolutionsComboBox, this);
            helperElevation = new HelperElevationProperty(HelperElevationBadge, HelperElevationBadgeText, HelperStatusSubtitleText, HeroElevationWarningButton, HeroElevationWarningIcon, HeroElevationWarningText, this);
            rtssElevation = new RTSSElevationProperty(RTSSElevationBadge, RTSSElevationBadgeText, RTSSStatusSubtitleText, this);
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

            JudderFreeFPSToggle.Toggled += JudderFreeFPSToggle_Toggled;
            LimitFPSToggle.Toggled += (s, e) => UpdateFPSLimitSliderJudderFree();
            FPSLimitJudderFreeSlider.ValueChanged += FPSLimitJudderFreeSlider_ValueChanged;
            WidgetSliderProperty.AttachEngagementHandler(FPSLimitJudderFreeSlider);
            FPSLimitJudderFreeCanvas.SizeChanged += (s, e) => RenderJudderFreeMarkers();
            PerformanceOverlaySlider.ValueChanged += (s, e) => UpdateOSDSegmentedButtons((int)e.NewValue);
            UpdateOSDSegmentedButtons((int)PerformanceOverlaySlider.Value);
            judderFreeFPS.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(judderFreeFPS.Value))
                {
                    UpdateFPSLimitSliderJudderFree();
                }
            };
            refreshRate.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(refreshRate.Value))
                {
                    UpdateFPSLimitSliderJudderFree();
                }
            };

            properties = new WidgetProperties(
                osd,
                osdTextSize,
                runningGame,
                perGameProfile,
                cpuBoost,
                cpuEPP,
                setCPUEPP,
                limitCPUClock,
                cpuClockMax,
                refreshRates,
                refreshRate,
                resolutions,
                resolution,
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
                rtssElevation
            );

            if (HelperElevationFlyout != null)
            {
                HelperElevationFlyout.Opened += (s, e) =>
                {
                    isElevationFlyoutOpen = true;
                    if (lastElevationFlyoutInvoker == null)
                    {
                        lastElevationFlyoutInvoker = HelperElevationHelpButton;
                    }
                    HelperElevationWikiButton?.Focus(FocusState.Programmatic);
                };
                HelperElevationFlyout.Closed += (s, e) =>
                {
                    isElevationFlyoutOpen = false;
                };
            }
            if (HelperElevationFlyoutContent != null)
            {
                HelperElevationFlyoutContent.PreviewKeyDown += HelperElevationFlyoutContent_PreviewKeyDown;
            }

            this.KeyDown += GamingWidget_KeyDown;
            InitializeAppVersion();
        }

        private Control lastElevationFlyoutInvoker;

        private void HelperElevationFlyoutContent_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.GamepadB || e.Key == VirtualKey.Escape)
            {
                HelperElevationFlyout?.Hide();
                (lastElevationFlyoutInvoker ?? HelperElevationHelpButton)?.Focus(FocusState.Programmatic);
                e.Handled = true;
            }
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
            if (App.Connection != null)
            {
                try
                {
                    if (RestartElevatedButtonText != null)
                    {
                        RestartElevatedButtonText.Text = "Restarting...";
                    }
                    if (RestartElevatedButton != null)
                    {
                        RestartElevatedButton.IsEnabled = false;
                    }

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
                }
                finally
                {
                    await Task.Delay(1500);
                    if (RestartElevatedButtonText != null)
                    {
                        RestartElevatedButtonText.Text = "Restart as Administrator";
                    }
                    if (RestartElevatedButton != null)
                    {
                        RestartElevatedButton.IsEnabled = true;
                    }
                    HelperElevationFlyout?.Hide();
                }
            }
            else
            {
                Logger.Warn("App.Connection is null when attempting to restart elevated.");
                await EnsureHelperConnectionOrLaunchAsync();
            }
        }

        private async void HelperElevationWikiButton_Click(object sender, RoutedEventArgs e)
        {
            await LaunchExternalUriAsync("https://github.com/gattodellenevi/XboxGamingBar/wiki/Elevate-Process-Permissions");
        }

        private void CloseElevationHelpButton_Click(object sender, RoutedEventArgs e)
        {
            HelperElevationFlyout?.Hide();
            (lastElevationFlyoutInvoker ?? HelperElevationHelpButton)?.Focus(FocusState.Programmatic);
        }

        private void HeroElevationWarningButton_Click(object sender, RoutedEventArgs e)
        {
            lastElevationFlyoutInvoker = HeroElevationWarningButton;
            HelperElevationFlyout?.ShowAt(HeroElevationWarningButton);
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
            if (isElevationFlyoutOpen && (e.Key == VirtualKey.GamepadB || e.Key == VirtualKey.Escape))
            {
                HelperElevationFlyout?.Hide();
                (lastElevationFlyoutInvoker ?? HelperElevationHelpButton)?.Focus(FocusState.Programmatic);
                e.Handled = true;
                return;
            }

            if (e.Key == VirtualKey.GamepadLeftTrigger || e.Key == VirtualKey.GamepadLeftShoulder || e.Key == VirtualKey.PageUp)
            {
                NavigatePivot(-1);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.GamepadRightTrigger || e.Key == VirtualKey.GamepadRightShoulder || e.Key == VirtualKey.PageDown)
            {
                NavigatePivot(1);
                e.Handled = true;
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
                PerformanceOverlaySlider.Value = level;
                UpdateOSDSegmentedButtons(level);
            }
        }

        private async void UpdateOSDSegmentedButtons(int level)
        {
            if (Dispatcher != null && !Dispatcher.HasThreadAccess)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateOSDSegmentedButtons(level));
                return;
            }

            Button[] buttons = new Button[] { OSDLevel0Button, OSDLevel1Button, OSDLevel2Button, OSDLevel3Button, OSDLevel4Button };
            var accentBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 124, 65)); // Xbox Green
            var transparentBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);
            var whiteBrush = new SolidColorBrush(Windows.UI.Colors.White);
            var textMutedBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 200, 200, 200));

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    if (i == level)
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

        private void NavigatePivot(int direction)
        {
            int count = MainPivot.Items.Count;
            if (count <= 1) return;

            int currentIndex = MainPivot.SelectedIndex;
            int targetIndex = currentIndex + direction;

            // Linear navigation: Search in the given direction without circular wrapping
            while (targetIndex >= 0 && targetIndex < count)
            {
                if (MainPivot.Items[targetIndex] is PivotItem item && item.Visibility == Visibility.Visible)
                {
                    MainPivot.SelectedIndex = targetIndex;
                    break;
                }

                targetIndex += direction;
            }
        }

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
                ShowLoadingModal();
                await EnsureHelperConnectionOrLaunchAsync();
            }
            Logger.Info("GamingWidget OnNavigatedTo finished.");
        }

        private async void ShowLoadingModal(string title = "Connecting to Helper...", string subtitle = "Initializing hardware sensors & power controls...")
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
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

        private async void HideLoadingModal()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
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

        private void EnsureWidgetActivity()
        {
            if (widget != null && widgetActivity == null)
            {
                try
                {
                    widgetActivity = new XboxGameBarWidgetActivity(widget, "XboxGamingBarActivity");
                    Logger.Info("Created widget activity to keep Game Bar active.");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Could not create widget activity: {ex.Message}");
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
                    ShowLoadingModal("Connecting to Helper...", "Establishing communication channel...");
                }
                else
                {
                    EnsureWidgetActivity();
                    ShowLoadingModal("Starting CouchGamingBar Helper...", "Initializing hardware sensors & power controls...");
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
            HideLoadingModal();

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
        }

        /// <summary>
        /// When the desktop process is disconnected, reconnect if needed
        /// </summary>
        private async void GamingWidget_AppServiceDisconnected(object sender, EventArgs e)
        {
            helperElevation?.SetDisconnectedState();
            rtssElevation?.SetDisconnectedState();

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
                if (widgetActivity != null)
                {
                    widgetActivity.Complete();
                    widgetActivity = null;
                    Logger.Info("Stopped widget activity on terminating disconnect.");
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
                var judderFreeValues = RTSSHelper.GetJudderFreeFPSValues(maxRefresh, 30);
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

            int maxRefresh = refreshRate.Value;
            bool isLimitFPSOn = LimitFPSToggle != null && LimitFPSToggle.IsOn;
            bool isJudderFree = JudderFreeFPSToggle != null && JudderFreeFPSToggle.IsOn;

            if (!isLimitFPSOn)
            {
                FPSLimitSlider.Visibility = Visibility.Collapsed;
                FPSLimitJudderFreeSlider.Visibility = Visibility.Collapsed;
                FPSLimitJudderFreeCanvas.Visibility = Visibility.Collapsed;
                return;
            }

            FPSLimitJudderFreeCanvas.Visibility = Visibility.Visible;

            if (isJudderFree)
            {
                var judderFreeValues = RTSSHelper.GetJudderFreeFPSValues(maxRefresh, 30);
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

                    double currentVal = FPSLimitSlider.Value;
                    int closestVal = judderFreeValues.OrderBy(v => Math.Abs(v - currentVal)).First();
                    int idx = judderFreeValues.IndexOf(closestVal);
                    if (idx < 0) idx = 0;

                    FPSLimitJudderFreeSlider.Value = idx;
                    FPSLimitSlider.Value = judderFreeValues[idx];
                    isUpdatingJudderFreeSlider = false;
                }
                else
                {
                    ShowStandardFPSLimitSlider(maxRefresh);
                }
            }
            else
            {
                ShowStandardFPSLimitSlider(maxRefresh);
            }

            RenderJudderFreeMarkers();
        }

        private void ShowStandardFPSLimitSlider(int maxRefresh)
        {
            FPSLimitSlider.Visibility = Visibility.Visible;
            FPSLimitJudderFreeSlider.Visibility = Visibility.Collapsed;
            FPSLimitSlider.Minimum = 30;
            FPSLimitSlider.Maximum = maxRefresh;
            FPSLimitSlider.TickPlacement = TickPlacement.BottomRight;
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

            var judderFreeValues = RTSSHelper.GetJudderFreeFPSValues(maxRefresh, 30);
            if (judderFreeValues == null || judderFreeValues.Count == 0)
                return;

            bool isJudderFree = JudderFreeFPSToggle != null && JudderFreeFPSToggle.IsOn;

            if (isJudderFree)
            {
                int count = judderFreeValues.Count;
                for (int i = 0; i < count; i++)
                {
                    int val = judderFreeValues[i];
                    double percent = count > 1 ? (double)i / (count - 1) : 0;
                    double xPos = percent * canvasWidth;

                    AddCanvasLabel(val.ToString(), xPos, canvasWidth);
                }
            }
            else
            {
                double min = FPSLimitSlider.Minimum;
                double max = FPSLimitSlider.Maximum;
                if (max <= min) return;

                foreach (int val in judderFreeValues)
                {
                    if (val < min || val > max) continue;

                    double percent = (val - min) / (max - min);
                    double xPos = percent * canvasWidth;

                    AddCanvasLabel(val.ToString(), xPos, canvasWidth);
                }
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
    }
}
