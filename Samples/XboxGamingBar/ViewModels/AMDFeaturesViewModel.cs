using Windows.UI.Core;

namespace XboxGamingBar.ViewModels
{
    /// <summary>
    /// ViewModel for AMD Adrenalin (ADLX) features:
    /// Radeon Super Resolution (RSR), Fluid Motion Frames (AFMF),
    /// Anti-Lag, Radeon Boost, and Radeon Chill.
    /// </summary>
    public class AMDFeaturesViewModel : ViewModelBase
    {
        private bool _isAmdSettingsSupported = false;

        // RSR
        private bool _isRsrSupported = false;
        private bool _isRsrEnabled = false;
        private int _rsrSharpness = 75;

        // AFMF
        private bool _isAfmfSupported = false;
        private bool _isAfmfEnabled = false;

        // Anti-Lag
        private bool _isAntiLagSupported = false;
        private bool _isAntiLagEnabled = false;

        // Boost
        private bool _isBoostSupported = false;
        private bool _isBoostEnabled = false;
        private int _boostResolution = 1;

        // Chill
        private bool _isChillSupported = false;
        private bool _isChillEnabled = false;
        private int _chillMinFps = 30;
        private int _chillMaxFps = 60;

        public bool IsAmdSettingsSupported
        {
            get => _isAmdSettingsSupported;
            set => SetProperty(ref _isAmdSettingsSupported, value);
        }

        public bool IsRsrSupported
        {
            get => _isRsrSupported;
            set => SetProperty(ref _isRsrSupported, value);
        }

        public bool IsRsrEnabled
        {
            get => _isRsrEnabled;
            set => SetProperty(ref _isRsrEnabled, value);
        }

        public int RsrSharpness
        {
            get => _rsrSharpness;
            set => SetProperty(ref _rsrSharpness, value);
        }

        public bool IsAfmfSupported
        {
            get => _isAfmfSupported;
            set => SetProperty(ref _isAfmfSupported, value);
        }

        public bool IsAfmfEnabled
        {
            get => _isAfmfEnabled;
            set => SetProperty(ref _isAfmfEnabled, value);
        }

        public bool IsAntiLagSupported
        {
            get => _isAntiLagSupported;
            set => SetProperty(ref _isAntiLagSupported, value);
        }

        public bool IsAntiLagEnabled
        {
            get => _isAntiLagEnabled;
            set => SetProperty(ref _isAntiLagEnabled, value);
        }

        public bool IsBoostSupported
        {
            get => _isBoostSupported;
            set => SetProperty(ref _isBoostSupported, value);
        }

        public bool IsBoostEnabled
        {
            get => _isBoostEnabled;
            set => SetProperty(ref _isBoostEnabled, value);
        }

        public int BoostResolution
        {
            get => _boostResolution;
            set => SetProperty(ref _boostResolution, value);
        }

        public bool IsChillSupported
        {
            get => _isChillSupported;
            set => SetProperty(ref _isChillSupported, value);
        }

        public bool IsChillEnabled
        {
            get => _isChillEnabled;
            set => SetProperty(ref _isChillEnabled, value);
        }

        public int ChillMinFps
        {
            get => _chillMinFps;
            set => SetProperty(ref _chillMinFps, value);
        }

        public int ChillMaxFps
        {
            get => _chillMaxFps;
            set => SetProperty(ref _chillMaxFps, value);
        }

        public AMDFeaturesViewModel(CoreDispatcher dispatcher = null) : base(dispatcher)
        {
        }
    }
}
