using System.Collections.Generic;
using Windows.UI.Core;

namespace XboxGamingBar.ViewModels
{
    /// <summary>
    /// ViewModel for Display settings: Resolution, Refresh Rate, and HDR.
    /// </summary>
    public class DisplayViewModel : ViewModelBase
    {
        private List<int> _refreshRates = new List<int>();
        private int _selectedRefreshRate = 60;
        private List<string> _resolutions = new List<string>();
        private string _selectedResolution = "1920x1080";
        private bool _isHdrSupported = false;
        private bool _isHdrEnabled = false;

        public List<int> RefreshRates
        {
            get => _refreshRates;
            set => SetProperty(ref _refreshRates, value);
        }

        public int SelectedRefreshRate
        {
            get => _selectedRefreshRate;
            set => SetProperty(ref _selectedRefreshRate, value);
        }

        public List<string> Resolutions
        {
            get => _resolutions;
            set => SetProperty(ref _resolutions, value);
        }

        public string SelectedResolution
        {
            get => _selectedResolution;
            set => SetProperty(ref _selectedResolution, value);
        }

        public bool IsHdrSupported
        {
            get => _isHdrSupported;
            set => SetProperty(ref _isHdrSupported, value);
        }

        public bool IsHdrEnabled
        {
            get => _isHdrEnabled;
            set => SetProperty(ref _isHdrEnabled, value);
        }

        public DisplayViewModel(CoreDispatcher dispatcher = null) : base(dispatcher)
        {
        }

        public int GetMaxRefreshRate()
        {
            int max = 60;
            if (RefreshRates != null)
            {
                foreach (int rate in RefreshRates)
                {
                    if (rate > max) max = rate;
                }
            }
            return max;
        }
    }
}
