using Windows.UI.Core;

namespace XboxGamingBar.ViewModels
{
    /// <summary>
    /// Master ViewModel for the XboxGamingBar widget aggregating modular domain ViewModels.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private string _runningGameName = "No game detected";
        private bool _isGameRunning = false;
        private bool _isPerGameProfile = false;

        public QuickAccessViewModel QuickAccess { get; }
        public DisplayViewModel Display { get; }
        public PerformanceViewModel Performance { get; }
        public AMDFeaturesViewModel AMD { get; }
        public SettingsViewModel Settings { get; }

        public string RunningGameName
        {
            get => _runningGameName;
            set => SetProperty(ref _runningGameName, value);
        }

        public bool IsGameRunning
        {
            get => _isGameRunning;
            set => SetProperty(ref _isGameRunning, value);
        }

        public bool IsPerGameProfile
        {
            get => _isPerGameProfile;
            set => SetProperty(ref _isPerGameProfile, value);
        }

        public MainViewModel(CoreDispatcher dispatcher = null) : base(dispatcher)
        {
            QuickAccess = new QuickAccessViewModel(dispatcher);
            Display = new DisplayViewModel(dispatcher);
            Performance = new PerformanceViewModel(dispatcher);
            AMD = new AMDFeaturesViewModel(dispatcher);
            Settings = new SettingsViewModel(dispatcher);
        }

        public void SetDispatcher(CoreDispatcher dispatcher)
        {
            Dispatcher = dispatcher;
            QuickAccess.Dispatcher = dispatcher;
            Display.Dispatcher = dispatcher;
            Performance.Dispatcher = dispatcher;
            AMD.Dispatcher = dispatcher;
            Settings.Dispatcher = dispatcher;
        }
    }
}
