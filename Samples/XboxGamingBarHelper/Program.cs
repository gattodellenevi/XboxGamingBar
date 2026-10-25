using NLog;
using Shared.Constants;
using Shared.Data;
using Shared.Utilities;
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using XboxGamingBarHelper.Utilities;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using System.Windows.Forms;
using XboxGamingBarHelper.AMD;
using XboxGamingBarHelper.Core;
using XboxGamingBarHelper.Hardware;
using XboxGamingBarHelper.Nvidia;
using XboxGamingBarHelper.OnScreenDisplay;

using XboxGamingBarHelper.Profile;
using XboxGamingBarHelper.RTSS;
using XboxGamingBarHelper.Settings;
using XboxGamingBarHelper.Systems;
using XboxGamingBarHelper.Windows;

namespace XboxGamingBarHelper
{
    internal class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static AppServiceConnection connection = null;

        // Managers
        private static HardwareManager hardwareManager;
        private static RTSSManager rtssManager;
        private static ProfileManager profileManager;
        private static SystemManager systemManager;
        private static AMDManager amdManager;
        private static NvidiaManager nvidiaManager;
        private static SettingsManager settingsManager;
        private static List<IManager> Managers;
        private static AppServiceConnectionStatus appServiceConnectionStatus = AppServiceConnectionStatus.AppServiceUnavailable;
        public static bool IsConnected => appServiceConnectionStatus == AppServiceConnectionStatus.Success;

        public static OnScreenDisplayProperty onScreenDisplay;
        public static List<OnScreenDisplayManager> onScreenDisplayProviders;
        private static TrayIconManager trayIconManager;

        // Properties
        private static HelperProperties properties;
        public static volatile bool HasServedInitialSnapshot = false;
        public static bool IsAmdInitialized => amdManager != null && amdManager.IsInitialized;

        private static System.Threading.Mutex _singleInstanceMutex;
        private static System.Threading.EventWaitHandle _connectEvent;
        private static System.Threading.RegisteredWaitHandle _registeredConnectWaitHandle;

        [STAThread]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Logger.Error(eventArgs.ExceptionObject as Exception, $"Unhandled AppDomain Exception (isTerminating={eventArgs.IsTerminating})");
            };
            Application.ThreadException += (sender, eventArgs) =>
            {
                Logger.Error(eventArgs.Exception, "Unhandled WinForms ThreadException");
            };
            TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
            {
                Logger.Error(eventArgs.Exception, "Unobserved Task Exception");
                eventArgs.SetObserved();
            };

            try
            {
                User32.SetProcessDpiAwarenessContext(User32.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to set DPI awareness context.");
            }

            try
            {
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to set HighDpiMode.");
            }

            // If running elevated, terminate any existing non-elevated duplicate helper instances
            if (ElevationManager.IsElevated())
            {
                int currentPid = Process.GetCurrentProcess().Id;
                foreach (var p in Process.GetProcessesByName("CouchGamingBarHelper"))
                {
                    if (p.Id != currentPid)
                    {
                        try
                        {
                            Logger.Info($"Elevated instance detected existing helper process PID {p.Id}. Terminating to take over.");
                            p.Kill();
                            p.WaitForExit(1000);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(ex, $"Could not terminate existing helper process PID {p.Id}.");
                        }
                    }
                }
            }

            bool createdNew = false;
            try
            {
                _singleInstanceMutex = CreateSingleInstanceMutexWithAcl(out createdNew);
            }
            catch (UnauthorizedAccessException)
            {
                Logger.Info("CouchGamingBarHelper is already running in an elevated context. Exiting duplicate instance.");
                return;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create single-instance mutex with ACL, falling back to standard Mutex constructor.");
                try
                {
                    _singleInstanceMutex = new System.Threading.Mutex(true, @"Global\CouchGamingBarHelper_SingleInstance_Mutex", out createdNew);
                }
                catch (UnauthorizedAccessException)
                {
                    Logger.Info("CouchGamingBarHelper is already running in an elevated context. Exiting duplicate instance.");
                    return;
                }
            }

            if (!createdNew)
            {
                Logger.Info("CouchGamingBarHelper is already running. Exiting duplicate instance.");
                return;
            }

            Run(args);
        }

        static void Run(string[] args)
        {
            Logger.Info("Run: Starting MainLoopAsync...");
            var mainLoopTask = MainLoopAsync(args);
            Logger.Info("Run: Entering Application.Run()...");
            Application.Run();
            Logger.Warn("Run: Application.Run() returned!");
            try
            {
                if (!mainLoopTask.IsCompleted)
                {
                    Logger.Info("Run: Waiting for MainLoopAsync to observe state...");
                    mainLoopTask.Wait(3000);
                }
                else if (mainLoopTask.IsFaulted)
                {
                    Logger.Error(mainLoopTask.Exception, "Run: MainLoopAsync was faulted!");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Run: MainLoopAsync exception on exit.");
            }
            Logger.Warn("Run: Process exiting.");
        }

        //static async Task Main(string[] args)
        //{
        //    await Initialize();
        //}

        /// <summary>
        /// Open connection to UWP app service
        /// </summary>
        private static async Task MainLoopAsync(string[] args)
        {
            try
            {
                // Initialize app service connection.
                InitializeConnection();

                Logger.Info("Initialize Tray Icon Manager.");
                trayIconManager = new TrayIconManager(connection);

                // Initialize managers.
                Logger.Info("Initialize Profile Manager.");
                profileManager = new ProfileManager(connection);

                Logger.Info("Initialize Settings Manager.");
                settingsManager = SettingsManager.CreateInstance(connection, profileManager.Store);

                Logger.Info("Initialize Hardware Manager.");
                hardwareManager = new HardwareManager(connection);
                Logger.Info("Initialize RTSS Manager.");
                rtssManager = new RTSSManager(hardwareManager, connection);
                Logger.Info("Initialize System Manager.");
                systemManager = new SystemManager(connection, profileManager.GameProfiles);

                Logger.Info("Initialize AMD Manager.");
                amdManager = new AMDManager(connection);
                Logger.Info("Initialize NVIDIA Manager.");
                nvidiaManager = new NvidiaManager(connection);
                
                Managers = new List<IManager>
                {
                    hardwareManager,
                    rtssManager,
                    profileManager,
                    systemManager,
                    amdManager,
                    nvidiaManager,
                    settingsManager
                };

                Logger.Info("Initialize properties.");
                onScreenDisplayProviders = new List<OnScreenDisplayManager>() { rtssManager, amdManager };
                int initialOsdLevel = profileManager.Store.GetEffectiveValue(SettingsRegistry.OnScreenDisplay);
                int initialOsdProvider = profileManager.Store.GetEffectiveValue(SettingsRegistry.OnScreenDisplayProvider);
                onScreenDisplay = new OnScreenDisplayProperty(initialOsdLevel, null, onScreenDisplayProviders[initialOsdProvider]);
                settingsManager.SyncOnScreenDisplaySettings(onScreenDisplay);
                settingsManager.SyncOnScreenDisplayTextSizeSettings(rtssManager.OSDTextSize);
                settingsManager.SyncGpuTargetSettings(hardwareManager.GpuTarget);
                settingsManager.Update();
                //onScreenDisplay = new OnScreenDisplayProperty(0, null, amdManager);

                // Initialize properties.
                properties = new HelperProperties(
                    systemManager.RunningGame,
                    onScreenDisplay,
                    profileManager.PerGameProfile,

                    systemManager.RefreshRates,
                    systemManager.RefreshRate,
                    systemManager.Resolutions,
                    systemManager.Resolution,
                    systemManager.TrackedGame,
                    settingsManager.OnScreenDisplayProviderInstalled,
                    settingsManager.IsForeground,
                    amdManager.AMDSettingsSupported,
                    amdManager.AMDRadeonSuperResolutionEnabled,
                    amdManager.AMDRadeonSuperResolutionSupported,
                    amdManager.AMDRadeonSuperResolutionSharpness,
                    amdManager.AMDFluidMotionFrameEnabled,
                    amdManager.AMDFluidMotionFrameSupported,
                    amdManager.AMDRadeonAntiLagEnabled,
                    amdManager.AMDRadeonAntiLagSupported,
                    amdManager.AMDRadeonBoostEnabled,
                    amdManager.AMDRadeonBoostSupported,
                    amdManager.AMDRadeonBoostResolution,
                    amdManager.AMDRadeonChillEnabled,
                    amdManager.AMDRadeonChillSupported,
                    amdManager.AMDRadeonChillMinFPS,
                    amdManager.AMDRadeonChillMaxFPS,
                    amdManager.FocusingOnOSDSlider,
                    settingsManager.OnScreenDisplayProvider,
                    nvidiaManager.LaunchNvidiaApp,
                    nvidiaManager.NvidiaVSyncMode,
                    nvidiaManager.NvidiaVSyncSupported,
                    amdManager.LaunchAmdApp,
                    rtssManager.LimitFPS,
                    rtssManager.FPSLimit,
                    rtssManager.FPSLimitMode,
                    rtssManager.JudderFreeFPS,
                    rtssManager.OSDTextSize,
                    rtssManager.RTSSElevation,
                    systemManager.HelperElevation,
                    hardwareManager.HardwareProvider,
                    hardwareManager.HardwareTelemetry,
                    hardwareManager.GpuTarget,
                    hardwareManager.DualGpuSupport,
                    hardwareManager.ActiveGpu,
                    systemManager.OpenUri,
                    systemManager.RestartElevated,
                    systemManager.SendShortcut,
                    systemManager.GameInputStatus,
                    systemManager.RestartGameInputService,
                    systemManager.VirtualKeyboard,
                    systemManager.SteamOverlay,
                    systemManager.HDR,
                    systemManager.HDRSupported,
                    systemManager.ControllerBattery,
                    systemManager.MouseSpeed,
                    systemManager.MouseEnhancePointerPrecision,
                    systemManager.MouseScrollSpeed,
                    systemManager.TrayItems,
                    systemManager.InvokeTrayItem);

                Logger.Info("Initialize callbacks.");
                systemManager.RunningGame.PropertyChanged += RunningGame_PropertyChanged;
                systemManager.ResumeFromSleep += SystemManager_ResumeFromSleep;
                profileManager.PerGameProfile.PropertyChanged += PerGameProfile_PropertyChanged;

                rtssManager.LimitFPS.PropertyChanged += LimitFPS_PropertyChanged;
                rtssManager.FPSLimit.PropertyChanged += FPSLimit_PropertyChanged;
                rtssManager.FPSLimitMode.PropertyChanged += FPSLimitMode_PropertyChanged;
                rtssManager.JudderFreeFPS.PropertyChanged += JudderFreeFPS_PropertyChanged;
                profileManager.CurrentProfile.PropertyChanged += CurrentProfile_PropertyChanged;

                ApplyCurrentProfile();

                // On-demand telemetry subscription push model (Task 4.3):
                // Hardware sensors only query when OSD is enabled or widget is foreground and connected
                hardwareManager.IsDemandActive = () =>
                    (onScreenDisplay != null && onScreenDisplay.Value > 0) ||
                    (settingsManager != null && settingsManager.IsForeground.Value && appServiceConnectionStatus == AppServiceConnectionStatus.Success);

                // Reactive connection state machine (Task 4.4):
                try
                {
                    InitializeReactiveConnectEvent();
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to initialize reactive connect event; proceeding with standard connection.");
                }

                await ConnectToWidget(false);

                Logger.Info($"Widget connection status: {appServiceConnectionStatus}");

                while (true)
                {
                    await Task.Delay(1000);

                    foreach (var manager in Managers)
                    {
                        try
                        {
                            manager.Update();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "Exception in manager update loop.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Fatal exception in MainLoopAsync.");
            }
        }

        private static void SystemManager_ResumeFromSleep(object sender)
        {
            Logger.Info("System resumed from sleep, re-apply current profile settings.");
            // Re-apply current profile settings.
            CurrentProfile_PropertyChanged(sender, null);
        }

        private static void InitializeConnection()
        {
            Logger.Info("Initialize connection...");
            connection = new AppServiceConnection();
            connection.AppServiceName = "XboxGamingBarService";
            connection.RequestReceived += Connection_RequestReceived;
            connection.ServiceClosed += Connection_ServiceClosed;

            string packageFamilyName = null;
            try
            {
                packageFamilyName = Package.Current.Id.FamilyName;
                Logger.Info($"Obtained PackageFamilyName from Package.Current: {packageFamilyName}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Could not get Package.Current identity (running unpackaged or via Task Scheduler): {ex.Message}");
            }

            string localFolderPath = PathHelper.GetLocalFolderPath();
            string primaryCachePath = Path.Combine(localFolderPath, "package_identity.txt");
            string appDirCachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "package_identity.txt");

            if (string.IsNullOrEmpty(packageFamilyName))
            {
                // Fast-path: Check cached package identity before querying heavy PackageManager
                try
                {
                    if (File.Exists(primaryCachePath))
                    {
                        var cached = File.ReadAllText(primaryCachePath).Trim();
                        if (!string.IsNullOrEmpty(cached))
                        {
                            packageFamilyName = cached;
                            Logger.Info($"Obtained PackageFamilyName from local cache file: {packageFamilyName}");
                        }
                    }
                    else if (File.Exists(appDirCachePath))
                    {
                        var cached = File.ReadAllText(appDirCachePath).Trim();
                        if (!string.IsNullOrEmpty(cached))
                        {
                            packageFamilyName = cached;
                            Logger.Info($"Obtained PackageFamilyName from app directory cache file: {packageFamilyName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to read cached package identity file.");
                }
            }

            if (string.IsNullOrEmpty(packageFamilyName))
            {
                try
                {
                    var packageManager = new global::Windows.Management.Deployment.PackageManager();
                    var packages = packageManager.FindPackagesForUser(string.Empty);
                    foreach (var package in packages)
                    {
                        if (package.Id.Name.Equals("CouchGamingBar", StringComparison.OrdinalIgnoreCase) ||
                            package.Id.Name.Equals("Zanchisoft.CouchGamingBar", StringComparison.OrdinalIgnoreCase))
                        {
                            packageFamilyName = package.Id.FamilyName;
                            Logger.Info($"Found installed package family name via PackageManager: {packageFamilyName}");
                            break;
                        }
                    }
                }
                catch (Exception pmEx)
                {
                    Logger.Error(pmEx, "Failed to query PackageManager for CouchGamingBar package family name.");
                }
            }

            if (!string.IsNullOrEmpty(packageFamilyName))
            {
                connection.PackageFamilyName = packageFamilyName;

                // Persist/refresh cache file for subsequent instant boots
                try
                {
                    Directory.CreateDirectory(localFolderPath);
                    if (!File.Exists(primaryCachePath) || File.ReadAllText(primaryCachePath).Trim() != packageFamilyName)
                    {
                        File.WriteAllText(primaryCachePath, packageFamilyName);
                        Logger.Info($"Cached PackageFamilyName to: {primaryCachePath}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Could not persist package identity cache.");
                }
            }
            else
            {
                Logger.Warn("PackageFamilyName could not be determined. AppService connection to widget will not be possible until package is registered.");
            }
        }

        private static void RecreateConnection()
        {
            Logger.Info("Re-creating AppServiceConnection object for next connection attempt.");
            appServiceConnectionStatus = AppServiceConnectionStatus.AppServiceUnavailable;
            try
            {
                if (connection != null)
                {
                    connection.RequestReceived -= Connection_RequestReceived;
                    connection.ServiceClosed -= Connection_ServiceClosed;
                    connection.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Exception occurred when disposing old connection: {ex.Message}");
            }

            InitializeConnection();

            if (Managers != null)
            {
                foreach (var manager in Managers)
                {
                    manager.Connection = connection;
                }
            }
            if (trayIconManager != null)
            {
                trayIconManager.Connection = connection;
            }
        }

        private static readonly System.Threading.SemaphoreSlim _connectLock = new System.Threading.SemaphoreSlim(1, 1);
        private static DateTime _lastConnectedTimeUtc = DateTime.MinValue;

        public static async Task PrepareInitialStateAsync()
        {
            try
            {
                settingsManager?.IsForeground?.SetValue(true);
                settingsManager?.Update();

                var readinessTasks = new List<Task>();
                if (amdManager?.InitializationTask != null && !amdManager.InitializationTask.IsCompleted)
                {
                    readinessTasks.Add(amdManager.InitializationTask);
                }
                if (systemManager?.DisplayEnumerationTask != null && !systemManager.DisplayEnumerationTask.IsCompleted)
                {
                    readinessTasks.Add(systemManager.DisplayEnumerationTask);
                }

                if (readinessTasks.Count > 0)
                {
                    Logger.Info($"Waiting up to 1500ms for {readinessTasks.Count} background subsystem initialization task(s) before snapshot...");
                    await Task.WhenAny(Task.WhenAll(readinessTasks), Task.Delay(1500));
                }

                if (hardwareManager != null && string.IsNullOrEmpty(hardwareManager.HardwareTelemetry?.Value))
                {
                    hardwareManager.Update();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Exception while preparing initial state snapshot.");
            }
        }

        private static async Task ConnectToWidget(bool blocking, bool forceReconnectOnEvent = false)
        {
            if (connection == null || string.IsNullOrEmpty(connection?.PackageFamilyName))
            {
                Logger.Info("Cannot connect to widget AppService: connection is null or PackageFamilyName is empty.");
                appServiceConnectionStatus = AppServiceConnectionStatus.AppServiceUnavailable;
                return;
            }

            if (!await _connectLock.WaitAsync(0))
            {
                Logger.Info("Connection attempt is already in progress, skipping concurrent request.");
                return;
            }

            try
            {
                if (appServiceConnectionStatus == AppServiceConnectionStatus.Success)
                {
                    double elapsedMs = (DateTime.UtcNow - _lastConnectedTimeUtc).TotalMilliseconds;
                    if (!forceReconnectOnEvent || elapsedMs < 1500)
                    {
                        Logger.Info($"Widget is already connected (elapsed={elapsedMs:F0}ms). Skipping duplicate connect request.");
                        return;
                    }

                    Logger.Info($"Widget requested connection for new session (elapsed={elapsedMs:F0}ms). Recreating connection inside lock.");
                    RecreateConnection();
                }

                if (blocking)
                {
                    do
                    {
                        Logger.Info("Start connecting to the widget.");
                        try
                        {
                            appServiceConnectionStatus = await connection.OpenAsync();
                        }
                        catch (Exception exception)
                        {
                            Logger.Error($"Exception occurred when connecting to the widget: {exception}");
                            appServiceConnectionStatus = AppServiceConnectionStatus.AppServiceUnavailable;
                        }

                        if (appServiceConnectionStatus != AppServiceConnectionStatus.Success)
                        {
                            Logger.Info("Can't connect to the widget. Recreating connection object and trying again in 1 second...");
                            RecreateConnection();
                            await Task.Delay(1000);
                        }
                    } while (appServiceConnectionStatus != AppServiceConnectionStatus.Success);
                    _lastConnectedTimeUtc = DateTime.UtcNow;
                    settingsManager?.IsForeground?.SetValue(true);
                    Logger.Info("Connected to the widget.");
                }
                else
                {
                    // Paced retries: try up to 4 times with 400ms delay to give widget time to activate
                    int maxRetries = 4;
                    for (int attempt = 1; attempt <= maxRetries; attempt++)
                    {
                        Logger.Info($"Start trying to connect to the widget (attempt {attempt}/{maxRetries}).");
                        try
                        {
                            appServiceConnectionStatus = await connection.OpenAsync();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "Exception occurred when trying to connect to the widget.");
                            appServiceConnectionStatus = AppServiceConnectionStatus.AppServiceUnavailable;
                        }

                        Logger.Info($"Try to connect to the widget result: {appServiceConnectionStatus}.");
                        if (appServiceConnectionStatus == AppServiceConnectionStatus.Success)
                        {
                            _lastConnectedTimeUtc = DateTime.UtcNow;
                            settingsManager?.IsForeground?.SetValue(true);
                            break;
                        }

                        RecreateConnection();
                        if (attempt < maxRetries)
                        {
                            await Task.Delay(400);
                        }
                    }
                }
            }
            finally
            {
                _connectLock.Release();
            }
        }

        private static bool isApplyingProfile = false;


        private static void CurrentProfile_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ApplyCurrentProfile();
        }

        private static void ApplyCurrentProfile()
        {
            var profile = profileManager.Store.ActiveProfile;
            if (profile.Use || profile.IsGlobalProfile)
            {
                Logger.Info($"Applying profile {profile.GameId.Name}: FPSLimit={profile.FPSLimit}, FPSLimitMode={profile.FPSLimitMode}, JudderFreeFPS={profile.JudderFreeFPS}");
                isApplyingProfile = true;

                rtssManager.FPSLimitMode.SetValue(profile.FPSLimitMode);
                rtssManager.JudderFreeFPS.SetValue(profile.JudderFreeFPS);
                int fallbackLimit = systemManager.RefreshRate.Value > 0 ? systemManager.RefreshRate.Value : 60;
                rtssManager.FPSLimit.SetValue(profile.FPSLimit > 0 ? profile.FPSLimit : fallbackLimit);
                rtssManager.LimitFPS.SetValue(profile.FPSLimit > 0);
                profileManager.PerGameProfile.SetValue(profile.Use);
                isApplyingProfile = false;
            }
            else
            {
                Logger.Info($"Profile {profile.GameId.Name} is not used.");
            }
        }

        private static void PerGameProfile_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (isApplyingProfile) return;
            profileManager.Store.SetPerGameProfileEnabled(profileManager.PerGameProfile.Value);
            profileManager.CurrentProfile.SetValue(profileManager.Store.ActiveProfile);
        }

        private static void UpdateCurrentProfileFPSLimit()
        {
            if (isApplyingProfile) return;
            int fallbackLimit = systemManager.RefreshRate.Value > 0 ? systemManager.RefreshRate.Value : 60;
            var newFPSLimit = rtssManager.LimitFPS ? (rtssManager.FPSLimit > 0 ? rtssManager.FPSLimit.Value : fallbackLimit) : 0;
            Logger.Info($"Update current profile {profileManager.Store.ActiveProfile.GameId.Name}'s FPS Limit to {newFPSLimit}.");
            profileManager.Store.SetEffectiveValue(SettingsRegistry.FPSLimit, newFPSLimit);
            profileManager.CurrentProfile.FPSLimit = newFPSLimit;
        }

        private static void LimitFPS_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateCurrentProfileFPSLimit();
        }

        private static void FPSLimit_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateCurrentProfileFPSLimit();
        }

        private static void FPSLimitMode_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (isApplyingProfile) return;
            Logger.Info($"Set current profile {profileManager.Store.ActiveProfile.GameId.Name}'s FPS Limit Mode from {profileManager.Store.ActiveProfile.FPSLimitMode} to {rtssManager.FPSLimitMode}.");
            profileManager.Store.SetEffectiveValue(SettingsRegistry.FPSLimitMode, rtssManager.FPSLimitMode.Value);
            profileManager.CurrentProfile.FPSLimitMode = rtssManager.FPSLimitMode.Value;
        }

        private static void JudderFreeFPS_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (isApplyingProfile) return;
            Logger.Info($"Set current profile {profileManager.Store.ActiveProfile.GameId.Name}'s JudderFreeFPS from {profileManager.Store.ActiveProfile.JudderFreeFPS} to {rtssManager.JudderFreeFPS.Value}.");
            profileManager.Store.SetEffectiveValue(SettingsRegistry.JudderFreeFPS, rtssManager.JudderFreeFPS.Value);
            profileManager.CurrentProfile.JudderFreeFPS = rtssManager.JudderFreeFPS.Value;
        }

        private static void RunningGame_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (systemManager.RunningGame.Value.IsValid())
            {
                profileManager.Store.SetActiveGame(systemManager.RunningGame.Value.GameId);
                if (profileManager.Store.IsPerGameProfileActive)
                {
                    Logger.Info($"Game {systemManager.RunningGame.GameId} has per-game profile in use.");
                    profileManager.CurrentProfile.SetValue(profileManager.Store.ActiveProfile);
                }
                else
                {
                    Logger.Info($"Game {systemManager.RunningGame.GameId} using global profile.");
                    profileManager.CurrentProfile.SetValue(profileManager.Store.GlobalProfile);
                }
            }
            else
            {
                Logger.Info($"Stopped playing game, use global profile instead.");
                profileManager.Store.SetActiveGame(new GameId());
                profileManager.CurrentProfile.SetValue(profileManager.Store.GlobalProfile);
            }
        }

        /// <summary>
        /// Handles the event when the desktop process receives a request from the UWP app
        /// </summary>
        private static async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var requestDeferral = args.GetDeferral();
            try
            {
                Logger.Info($"Helper received message {args.Request.Message.ToDebugString()} from widget.");
                await properties.OnRequestReceived(new HelperAppServiceRequest(args.Request));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in Helper Connection_RequestReceived.");
            }
            finally
            {
                requestDeferral.Complete();
            }
        }

        /// <summary>
        /// Handles the event when the app service connection is closed
        /// </summary>
        private static void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            Logger.Info("Lost connection to the widget.");
            appServiceConnectionStatus = AppServiceConnectionStatus.AppServiceUnavailable;

            Logger.Info("Prepare to re-connect to the widget.");
            RecreateConnection();
        }

        public static void ReleaseSingleInstanceMutex()
        {
            try
            {
                if (_singleInstanceMutex != null)
                {
                    _singleInstanceMutex.ReleaseMutex();
                    _singleInstanceMutex.Dispose();
                    _singleInstanceMutex = null;
                    Logger.Info("Released single-instance mutex.");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to release single-instance mutex.");
            }
        }

        public static void CreateSingleInstanceMutex(out bool createdNew)
        {
            try
            {
                _singleInstanceMutex = CreateSingleInstanceMutexWithAcl(out createdNew);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to create single-instance mutex with ACL, falling back to standard constructor.");
                _singleInstanceMutex = new System.Threading.Mutex(true, @"Global\CouchGamingBarHelper_SingleInstance_Mutex", out createdNew);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static System.Threading.Mutex CreateSingleInstanceMutexWithAcl(out bool createdNew)
        {
            var appPackageSid = new System.Security.Principal.SecurityIdentifier("S-1-15-2-1"); // ALL APPLICATION PACKAGES
            var authUsersSid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.AuthenticatedUserSid, null);
            var adminSid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.BuiltinAdministratorsSid, null);

            var mutexSecurity = new System.Security.AccessControl.MutexSecurity();
            mutexSecurity.AddAccessRule(new System.Security.AccessControl.MutexAccessRule(
                appPackageSid,
                System.Security.AccessControl.MutexRights.Synchronize | System.Security.AccessControl.MutexRights.Modify,
                System.Security.AccessControl.AccessControlType.Allow));
            mutexSecurity.AddAccessRule(new System.Security.AccessControl.MutexAccessRule(
                authUsersSid,
                System.Security.AccessControl.MutexRights.Synchronize | System.Security.AccessControl.MutexRights.Modify,
                System.Security.AccessControl.AccessControlType.Allow));
            mutexSecurity.AddAccessRule(new System.Security.AccessControl.MutexAccessRule(
                adminSid,
                System.Security.AccessControl.MutexRights.FullControl,
                System.Security.AccessControl.AccessControlType.Allow));

            return System.Threading.MutexAcl.Create(true, @"Global\CouchGamingBarHelper_SingleInstance_Mutex", out createdNew, mutexSecurity);
        }

        private static void InitializeReactiveConnectEvent()
        {
            try
            {
                CreateAclEventWaitHandle();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to initialize reactive connect event with ACL, falling back to standard EventWaitHandle.");
                try
                {
                    CreateStandardEventWaitHandle();
                }
                catch (Exception fallbackEx)
                {
                    Logger.Error(fallbackEx, "Failed to initialize fallback reactive connect event.");
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void CreateAclEventWaitHandle()
        {
            var appPackageSid = new System.Security.Principal.SecurityIdentifier("S-1-15-2-1"); // ALL APPLICATION PACKAGES
            var authUsersSid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.AuthenticatedUserSid, null);
            var adminSid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.BuiltinAdministratorsSid, null);

            var eventSecurity = new System.Security.AccessControl.EventWaitHandleSecurity();
            eventSecurity.AddAccessRule(new System.Security.AccessControl.EventWaitHandleAccessRule(
                appPackageSid,
                System.Security.AccessControl.EventWaitHandleRights.Synchronize | System.Security.AccessControl.EventWaitHandleRights.Modify,
                System.Security.AccessControl.AccessControlType.Allow));
            eventSecurity.AddAccessRule(new System.Security.AccessControl.EventWaitHandleAccessRule(
                authUsersSid,
                System.Security.AccessControl.EventWaitHandleRights.Synchronize | System.Security.AccessControl.EventWaitHandleRights.Modify,
                System.Security.AccessControl.AccessControlType.Allow));
            eventSecurity.AddAccessRule(new System.Security.AccessControl.EventWaitHandleAccessRule(
                adminSid,
                System.Security.AccessControl.EventWaitHandleRights.FullControl,
                System.Security.AccessControl.AccessControlType.Allow));

            _connectEvent = System.Threading.EventWaitHandleAcl.Create(
                false,
                System.Threading.EventResetMode.AutoReset,
                @"Global\CouchGamingBarHelper_ConnectEvent",
                out bool createdNew,
                eventSecurity);

            RegisterWaitCallback();
            Logger.Info("Initialized reactive connect event wait handle with ACL.");
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void CreateStandardEventWaitHandle()
        {
            _connectEvent = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, @"Global\CouchGamingBarHelper_ConnectEvent");
            RegisterWaitCallback();
            Logger.Info("Initialized standard reactive connect event wait handle.");
        }

        private static void RegisterWaitCallback()
        {
            _registeredConnectWaitHandle = System.Threading.ThreadPool.RegisterWaitForSingleObject(
                _connectEvent,
                async (state, timedOut) =>
                {
                    try
                    {
                        Logger.Info("Reactive connect event received from widget.");
                        await ConnectToWidget(false, forceReconnectOnEvent: true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error handling reactive connect event.");
                    }
                },
                null,
                -1,
                false);
        }
    }
}
