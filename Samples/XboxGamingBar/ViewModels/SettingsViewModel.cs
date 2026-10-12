using Windows.UI.Core;

namespace XboxGamingBar.ViewModels
{
    /// <summary>
    /// ViewModel for application and system settings:
    /// OSD provider selection, GPU targeting (dual-GPU), GameInput status,
    /// Helper elevation, and Windows mouse cursor parameters.
    /// </summary>
    public class SettingsViewModel : ViewModelBase
    {
        private int _osdProvider = 0; // 0 = RTSS, 1 = ADLX
        private bool _isOsdProviderInstalled = false;
        private bool _isDualGpuSupported = false;
        private int _gpuTarget = 0; // 0 = Discrete, 1 = Integrated
        private bool _isHelperElevated = false;
        private int _gameInputStatus = 1;
        private int _mouseSpeed = 10;
        private int _mouseScrollSpeed = 3;
        private bool _isMouseEnhancePointerPrecision = true;

        public int OsdProvider
        {
            get => _osdProvider;
            set => SetProperty(ref _osdProvider, value);
        }

        public bool IsOsdProviderInstalled
        {
            get => _isOsdProviderInstalled;
            set => SetProperty(ref _isOsdProviderInstalled, value);
        }

        public bool IsDualGpuSupported
        {
            get => _isDualGpuSupported;
            set => SetProperty(ref _isDualGpuSupported, value);
        }

        public int GpuTarget
        {
            get => _gpuTarget;
            set => SetProperty(ref _gpuTarget, value);
        }

        public bool IsHelperElevated
        {
            get => _isHelperElevated;
            set => SetProperty(ref _isHelperElevated, value);
        }

        public int GameInputStatus
        {
            get => _gameInputStatus;
            set => SetProperty(ref _gameInputStatus, value);
        }

        public int MouseSpeed
        {
            get => _mouseSpeed;
            set => SetProperty(ref _mouseSpeed, value);
        }

        public int MouseScrollSpeed
        {
            get => _mouseScrollSpeed;
            set => SetProperty(ref _mouseScrollSpeed, value);
        }

        public bool IsMouseEnhancePointerPrecision
        {
            get => _isMouseEnhancePointerPrecision;
            set => SetProperty(ref _isMouseEnhancePointerPrecision, value);
        }

        public SettingsViewModel(CoreDispatcher dispatcher = null) : base(dispatcher)
        {
        }
    }
}
