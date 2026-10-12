using Shared.Data;
using Shared.Utilities;
using System.ComponentModel;
using System.IO;
using Windows.ApplicationModel.AppService;
using Windows.Storage;
using XboxGamingBarHelper.Core;
using XboxGamingBarHelper.OnScreenDisplay;
using XboxGamingBarHelper.RTSS;
using XboxGamingBarHelper.Utilities;

using XboxGamingBarHelper.Profile;

namespace XboxGamingBarHelper.Settings
{
    internal class SettingsManager : Manager
    {
        private static SettingsManager instance;
        public static SettingsManager CreateInstance(AppServiceConnection connection, ProfileStore store = null)
        {
            if (instance == null)
            {
                instance = new SettingsManager(connection, store);
            }
            return instance;
        }

        public static SettingsManager GetInstance()
        {
            return instance;
        }

        private readonly ProfileStore store;
        public ProfileStore Store => store;

        private readonly OnScreenDisplayProviderProperty onScreenDisplayProvider;
        public OnScreenDisplayProviderProperty OnScreenDisplayProvider
        {
            get { return onScreenDisplayProvider; }
        }

        private readonly OnScreenDisplayProviderInstalledProperty onScreenDisplayProviderInstalled;
        public OnScreenDisplayProviderInstalledProperty OnScreenDisplayProviderInstalled
        {
            get { return onScreenDisplayProviderInstalled; }
        }

        private readonly IsForegroundProperty isForeground;
        public IsForegroundProperty IsForeground
        {
            get { return isForeground; }
        }

        private HelperProperty<int, OnScreenDisplayManager> onScreenDisplayPropertySettings;

        protected SettingsManager(AppServiceConnection connection, ProfileStore store = null) : base(connection)
        {
            this.store = store ?? new ProfileStore();

            int initialOsdProvider = this.store.GetEffectiveValue(SettingsRegistry.OnScreenDisplayProvider);
            onScreenDisplayProvider = new OnScreenDisplayProviderProperty(initialOsdProvider, this);
            onScreenDisplayProvider.PropertyChanged += OnScreenDisplayProviderChanged;
            onScreenDisplayProviderInstalled = new OnScreenDisplayProviderInstalledProperty(this);
            isForeground = new IsForegroundProperty(this);
        }

        public void SyncOnScreenDisplaySettings(HelperProperty<int, OnScreenDisplayManager> onScreenDisplayProperty)
        {
            if (onScreenDisplayPropertySettings != null)
                onScreenDisplayPropertySettings.PropertyChanged -= PerformanceOverlayLevelChanged;
            onScreenDisplayPropertySettings = onScreenDisplayProperty;
            onScreenDisplayProperty.PropertyChanged += PerformanceOverlayLevelChanged;
        }

        private OSDTextSizeProperty osdTextSizePropertySettings;
        public void SyncOnScreenDisplayTextSizeSettings(OSDTextSizeProperty osdTextSizeProperty)
        {
            if (osdTextSizePropertySettings != null)
                osdTextSizePropertySettings.PropertyChanged -= PerformanceOverlayTextSizeChanged;
            osdTextSizePropertySettings = osdTextSizeProperty;
            osdTextSizeProperty.SetValue(store.GetEffectiveValue(SettingsRegistry.OnScreenDisplayTextSize));
            osdTextSizeProperty.PropertyChanged += PerformanceOverlayTextSizeChanged;
        }

        private XboxGamingBarHelper.Hardware.GpuTargetProperty gpuTargetPropertySettings;
        public void SyncGpuTargetSettings(XboxGamingBarHelper.Hardware.GpuTargetProperty gpuTargetProperty)
        {
            if (gpuTargetPropertySettings != null)
                gpuTargetPropertySettings.PropertyChanged -= GpuTargetChanged;
            gpuTargetPropertySettings = gpuTargetProperty;
            gpuTargetProperty.SetValue(store.GetEffectiveValue(SettingsRegistry.GpuTarget));
            gpuTargetProperty.PropertyChanged += GpuTargetChanged;
        }

        private void GpuTargetChanged(object sender, PropertyChangedEventArgs e)
        {
            if (gpuTargetPropertySettings != null)
            {
                Logger.Info($"Save setting GPU Target {gpuTargetPropertySettings.Value}.");
                store.SetEffectiveValue(SettingsRegistry.GpuTarget, gpuTargetPropertySettings.Value);
            }
        }

        private void OnScreenDisplayProviderChanged(object sender, PropertyChangedEventArgs e)
        {
            Logger.Info($"Save setting On-Screen Display Provider {onScreenDisplayProvider.Value}.");
            store.SetEffectiveValue(SettingsRegistry.OnScreenDisplayProvider, onScreenDisplayProvider.Value);
        }

        public void PerformanceOverlayLevelChanged(object sender, PropertyChangedEventArgs e)
        {
            if (onScreenDisplayPropertySettings == null)
            {
                Logger.Error($"On-Screen Display Property for settings not found.");
                return;
            }
            else
            {
                Logger.Info($"Save setting On-Screen Display {onScreenDisplayPropertySettings.Value}.");
                store.SetEffectiveValue(SettingsRegistry.OnScreenDisplay, onScreenDisplayPropertySettings.Value);
            }
        }

        private void PerformanceOverlayTextSizeChanged(object sender, PropertyChangedEventArgs e)
        {
            if (osdTextSizePropertySettings != null)
            {
                Logger.Info($"Save setting On-Screen Display Text Size {osdTextSizePropertySettings.Value}.");
                store.SetEffectiveValue(SettingsRegistry.OnScreenDisplayTextSize, osdTextSizePropertySettings.Value);
            }
        }

        public override void Update()
        {
            base.Update();

            Logger.Debug($"On-Screen Display provider {Program.onScreenDisplay.Manager.GetType().Name} is {(Program.onScreenDisplay.Manager.IsInstalled ? "installed" : "not installed")}.");
            onScreenDisplayProviderInstalled.SetValue(Program.onScreenDisplay.Manager.IsInstalled);
        }
    }
}
