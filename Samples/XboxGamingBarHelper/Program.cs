using NLog;
using Shared.Constants;
using Shared.Data;
using Shared.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using System.Windows.Forms;
using XboxGamingBarHelper.AMD;
using XboxGamingBarHelper.Core;
using XboxGamingBarHelper.Hardware;
using XboxGamingBarHelper.Nvidia;
using XboxGamingBarHelper.OnScreenDisplay;
using XboxGamingBarHelper.Power;
using XboxGamingBarHelper.Profile;
using XboxGamingBarHelper.RTSS;
using XboxGamingBarHelper.Settings;
using XboxGamingBarHelper.Systems;

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
        private static PowerManager powerManager;
        private static AMDManager amdManager;
        private static NvidiaManager nvidiaManager;
        private static SettingsManager settingsManager;
        private static List<IManager> Managers;
        private static AppServiceConnectionStatus appServiceConnectionStatus;

        public static OnScreenDisplayProperty onScreenDisplay;
        public static List<OnScreenDisplayManager> onScreenDisplayProviders;
        private static TrayIconManager trayIconManager;

        // Properties
        private static HelperProperties properties;

        private static System.Threading.Mutex _singleInstanceMutex;

        static void Main(string[] args)
        {
            bool createdNew = false;
            try
            {
                var sid = new System.Security.Principal.SecurityIdentifier("S-1-15-2-1");
                var mutexSecurity = new System.Security.AccessControl.MutexSecurity();
                mutexSecurity.AddAccessRule(new System.Security.AccessControl.MutexAccessRule(
                    sid,
                    System.Security.AccessControl.MutexRights.Synchronize | System.Security.AccessControl.MutexRights.Modify,
                    System.Security.AccessControl.AccessControlType.Allow));

                _singleInstanceMutex = System.Threading.MutexAcl.Create(true, @"Global\CouchGamingBarHelper_SingleInstance_Mutex", out createdNew, mutexSecurity);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create single-instance mutex with ACL, falling back to standard Mutex constructor.");
                _singleInstanceMutex = new System.Threading.Mutex(true, @"Global\CouchGamingBarHelper_SingleInstance_Mutex", out createdNew);
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
            _ = MainLoopAsync(args);
            Application.Run();
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
                Logger.Info("Initialize Settings Manager.");
                settingsManager = SettingsManager.CreateInstance(connection);

                Logger.Info("Initialize Hardware Manager.");
                hardwareManager = new HardwareManager(connection);
                Logger.Info("Initialize RTSS Manager.");
                rtssManager = new RTSSManager(hardwareManager, connection);
                Logger.Info("Initialize Profile Manager.");
                profileManager = new ProfileManager(connection);
                Logger.Info("Initialize System Manager.");
                systemManager = new SystemManager(connection, profileManager.GameProfiles);
                Logger.Info("Initialize Power Manager.");
                powerManager = new PowerManager(connection);
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
                    powerManager,
                    amdManager,
                    nvidiaManager,
                    settingsManager
                };

                Logger.Info("Initialize properties.");
                onScreenDisplayProviders = new List<OnScreenDisplayManager>() { rtssManager, amdManager };
                onScreenDisplay = new OnScreenDisplayProperty(settingsManager.Setting.OnScreenDisplay, null, onScreenDisplayProviders[settingsManager.OnScreenDisplayProvider]);
                settingsManager.SyncOnScreenDisplaySettings(onScreenDisplay);
                //onScreenDisplay = new OnScreenDisplayProperty(0, null, amdManager);

                // Initialize properties.
                properties = new HelperProperties(
                    systemManager.RunningGame,
                    onScreenDisplay,
                    profileManager.PerGameProfile,
                    powerManager.CPUBoost,
                    powerManager.CPUEPP,
                    powerManager.SetCPUEPP,
                    powerManager.LimitCPUClock,
                    powerManager.CPUClockMax,
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
                    rtssManager.LimitFPS,
                    rtssManager.FPSLimit,
                    rtssManager.FPSLimitMode,
                    rtssManager.JudderFreeFPS);

                Logger.Info("Initialize callbacks.");
                systemManager.RunningGame.PropertyChanged += RunningGame_PropertyChanged;
                systemManager.ResumeFromSleep += SystemManager_ResumeFromSleep;
                profileManager.PerGameProfile.PropertyChanged += PerGameProfile_PropertyChanged;
                powerManager.CPUBoost.PropertyChanged += CPUBoost_PropertyChanged;
                powerManager.CPUEPP.PropertyChanged += CPUEPP_PropertyChanged;
                powerManager.SetCPUEPP.PropertyChanged += CPUEPP_PropertyChanged;
                powerManager.LimitCPUClock.PropertyChanged += CPUClock_PropertyChanged;
                powerManager.CPUClockMax.PropertyChanged += CPUClock_PropertyChanged;
                rtssManager.LimitFPS.PropertyChanged += LimitFPS_PropertyChanged;
                rtssManager.FPSLimit.PropertyChanged += FPSLimit_PropertyChanged;
                rtssManager.FPSLimitMode.PropertyChanged += FPSLimitMode_PropertyChanged;
                rtssManager.JudderFreeFPS.PropertyChanged += JudderFreeFPS_PropertyChanged;
                profileManager.CurrentProfile.PropertyChanged += CurrentProfile_PropertyChanged;

                await ConnectToWidget(false);

                Logger.Info($"Widget connection status: {appServiceConnectionStatus}");
                DateTime lastReconnectAttempt = DateTime.MinValue;

                while (true)
                {
                    if (appServiceConnectionStatus != AppServiceConnectionStatus.Success && connection != null && !string.IsNullOrEmpty(connection?.PackageFamilyName) && (DateTime.UtcNow - lastReconnectAttempt).TotalMilliseconds >= 2000)
                    {
                        lastReconnectAttempt = DateTime.UtcNow;
                        Logger.Info("Try to reconnect to the widget.");
                        await ConnectToWidget(false);
                    }

                    await Task.Delay(500);

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

            if (string.IsNullOrEmpty(packageFamilyName))
            {
                try
                {
                    var packageManager = new global::Windows.Management.Deployment.PackageManager();
                    var packages = packageManager.FindPackagesForUser(string.Empty);
                    foreach (var package in packages)
                    {
                        if (package.Id.Name.Equals("CouchGameBar", StringComparison.OrdinalIgnoreCase))
                        {
                            packageFamilyName = package.Id.FamilyName;
                            Logger.Info($"Found installed package family name via PackageManager: {packageFamilyName}");
                            break;
                        }
                    }
                }
                catch (Exception pmEx)
                {
                    Logger.Error(pmEx, "Failed to query PackageManager for CouchGameBar package family name.");
                }
            }

            if (!string.IsNullOrEmpty(packageFamilyName))
            {
                connection.PackageFamilyName = packageFamilyName;
            }
            else
            {
                Logger.Warn("PackageFamilyName could not be determined. AppService connection to widget will not be possible until package is registered.");
            }
        }

        private static void RecreateConnection()
        {
            Logger.Info("Re-creating AppServiceConnection object for next connection attempt.");
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

        private static async Task ConnectToWidget(bool blocking)
        {
            if (connection == null || string.IsNullOrEmpty(connection?.PackageFamilyName))
            {
                Logger.Info("Cannot connect to widget AppService: connection is null or PackageFamilyName is empty.");
                appServiceConnectionStatus = AppServiceConnectionStatus.AppServiceUnavailable;
                return;
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
                Logger.Info("Connected to the widget.");
            }
            else
            {
                Logger.Info("Start trying to connect to the widget.");
                try
                {
                    appServiceConnectionStatus = await connection.OpenAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Exception occurred when trying to connect to the widget.");
                    appServiceConnectionStatus = AppServiceConnectionStatus.AppServiceUnavailable;
                }

                Logger.Info($"Try to connect to the widget {appServiceConnectionStatus}.");
                if (appServiceConnectionStatus != AppServiceConnectionStatus.Success)
                {
                    RecreateConnection();
                }
            }
        }

        private static void CPUClock_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var cpuClock = powerManager.LimitCPUClock ? powerManager.CPUClockMax : 0;
            Logger.Info($"Set current profile {profileManager.CurrentProfile.GameId.Name}'s CPU Clock from {profileManager.CurrentProfile.CPUClock} to {cpuClock}.");
            profileManager.CurrentProfile.CPUClock = cpuClock;
        }

        private static void CPUBoost_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Logger.Info($"Set current profile {profileManager.CurrentProfile.GameId.Name}'s CPU Boost from {profileManager.CurrentProfile.CPUBoost} to {powerManager.CPUBoost}.");
            profileManager.CurrentProfile.CPUBoost = powerManager.CPUBoost;
        }

        private static void CPUEPP_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Logger.Info($"Set current profile {profileManager.CurrentProfile.GameId.Name}'s CPU EPP from {profileManager.CurrentProfile.CPUEPP} to {powerManager.CPUEPP} (Enabled: {powerManager.SetCPUEPP.Value}).");
            profileManager.CurrentProfile.CPUEPP = powerManager.CPUEPP;
            profileManager.CurrentProfile.SetCPUEPP = powerManager.SetCPUEPP.Value;
            if (!profileManager.CurrentProfile.IsGlobalProfile)
            {
                profileManager.GlobalProfile.SetCPUEPP = powerManager.SetCPUEPP.Value;
            }
        }

        private static void CurrentProfile_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (profileManager.CurrentProfile.Use || profileManager.CurrentProfile.IsGlobalProfile)
            {
                Logger.Info($"Profile changed to {profileManager.CurrentProfile.GameId.Name}, apply it.");
                powerManager.CPUBoost.SetValue(profileManager.CurrentProfile.CPUBoost);
                powerManager.SetCPUEPP.SetValue(profileManager.CurrentProfile.SetCPUEPP);
                powerManager.CPUEPP.SetValue(profileManager.CurrentProfile.CPUEPP);
                powerManager.LimitCPUClock.SetValue(profileManager.CurrentProfile.CPUClock > 0);
                powerManager.CPUClockMax.SetValue(profileManager.CurrentProfile.CPUClock > 0 ? profileManager.CurrentProfile.CPUClock : CPUConstants.DEFAULT_CPU_CLOCK);
                rtssManager.LimitFPS.SetValue(profileManager.CurrentProfile.FPSLimit > 0);
                rtssManager.FPSLimit.SetValue(profileManager.CurrentProfile.FPSLimit > 0 ? profileManager.CurrentProfile.FPSLimit : 60);
                rtssManager.FPSLimitMode.SetValue(profileManager.CurrentProfile.FPSLimitMode);
                rtssManager.JudderFreeFPS.SetValue(profileManager.CurrentProfile.JudderFreeFPS);
                profileManager.PerGameProfile.SetValue(profileManager.CurrentProfile.Use);
            }
            else
            {
                Logger.Info($"Profile changed to {profileManager.CurrentProfile.GameId.Name} is not used.");
            }
        }

        private static void PerGameProfile_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            GameProfile gameProfile;
            if (profileManager.PerGameProfile)
            {
                if (!profileManager.TryGetProfile(systemManager.RunningGame.Value.GameId, out gameProfile))
                {
                    gameProfile = profileManager.AddNewProfile(systemManager.RunningGame.Value.GameId);
                }
                Logger.Info($"Enable per-game profile for {systemManager.RunningGame.Value.GameId}");
                gameProfile.Use = true;
            }
            else
            {
                if (profileManager.TryGetProfile(systemManager.RunningGame.Value.GameId, out gameProfile))
                {
                    gameProfile.Use = false;
                }
                gameProfile = profileManager.GlobalProfile;
            }
            profileManager.CurrentProfile.SetValue(gameProfile);
        }

        private static void UpdateCurrentProfileFPSLimit()
        {
            var newFPSLimit = rtssManager.LimitFPS ? (rtssManager.FPSLimit > 0 ? rtssManager.FPSLimit.Value : 60) : 0;
            Logger.Info($"Update current profile {profileManager.CurrentProfile.GameId.Name}'s FPS Limit to {newFPSLimit}.");
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
            Logger.Info($"Set current profile {profileManager.CurrentProfile.GameId.Name}'s FPS Limit Mode from {profileManager.CurrentProfile.FPSLimitMode} to {rtssManager.FPSLimitMode}.");
            profileManager.CurrentProfile.FPSLimitMode = rtssManager.FPSLimitMode;
        }

        private static void JudderFreeFPS_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Logger.Info($"Set current profile {profileManager.CurrentProfile.GameId.Name}'s JudderFreeFPS from {profileManager.CurrentProfile.JudderFreeFPS} to {rtssManager.JudderFreeFPS.Value}.");
            profileManager.CurrentProfile.JudderFreeFPS = rtssManager.JudderFreeFPS.Value;
        }

        private static void RunningGame_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (systemManager.RunningGame.Value.IsValid())
            {
                if (profileManager.TryGetProfile(systemManager.RunningGame.Value.GameId, out var runningGameProfile))
                {
                    if (runningGameProfile.Use)
                    {
                        Logger.Info($"Game {systemManager.RunningGame.GameId} has per-game profile in use.");
                        profileManager.CurrentProfile.SetValue(runningGameProfile);
                    }
                    else
                    {
                        Logger.Info($"Game {systemManager.RunningGame.GameId} has per-game profile but not in use.");
                    }
                }
                else
                {
                    Logger.Info($"Game {systemManager.RunningGame.GameId} doesn't have per-game profile.");
                }
            }
            else
            {
                Logger.Info($"Stopped playing game, use global profile instead.");
                profileManager.CurrentProfile.SetValue(profileManager.GlobalProfile);
            }
        }

        /// <summary>
        /// Handles the event when the desktop process receives a request from the UWP app
        /// </summary>
        private static async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            Logger.Info($"Helper received message {args.Request.Message.ToDebugString()} from widget.");
            await properties.OnRequestReceived(new HelperAppServiceRequest(args.Request));
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
    }
}
