using System;
using Windows.UI.Core;

namespace XboxGamingBar.ViewModels
{
    /// <summary>
    /// ViewModel for the HUD cards, Quick Access toggle buttons (OSD, FPS limit, HDR),
    /// live telemetry summary, and controller status.
    /// </summary>
    public class QuickAccessViewModel : ViewModelBase
    {
        // OSD Quick Toggle
        private bool _isOsdButtonEnabled = true;
        private string _osdSubtitle = "Disabled";
        private string _osdBadgeText = "OFF";
        private bool _isOsdActive = false;

        // FPS Limit Quick Toggle
        private bool _isFpsLimitButtonEnabled = true;
        private string _fpsLimitSubtitle = "Disabled";
        private string _fpsLimitBadgeText = "OFF";
        private bool _isFpsLimitActive = false;
        private bool _isFpsLimitMismatch = false;

        // HDR Quick Toggle
        private bool _isHdrButtonEnabled = false;
        private string _hdrSubtitle = "Not supported";
        private string _hdrBadgeText = "SDR";
        private bool _isHdrActive = false;

        // Display & System Summary
        private string _displaySummary = "Display";
        private string _rtssStatusSummary = "RTSS Offline";

        // Live Telemetry
        private string _cpuUsage = "--%";
        private string _cpuWattage = "--W";
        private string _gpuUsage = "--%";
        private string _gpuWattage = "--W";
        private string _ramUsage = "--%";
        private string _ramUsed = "-- GB";
        private string _vramUsage = "--%";
        private string _vramUsed = "-- GB";

        public bool IsOsdButtonEnabled
        {
            get => _isOsdButtonEnabled;
            set => SetProperty(ref _isOsdButtonEnabled, value);
        }

        public string OsdSubtitle
        {
            get => _osdSubtitle;
            set => SetProperty(ref _osdSubtitle, value);
        }

        public string OsdBadgeText
        {
            get => _osdBadgeText;
            set => SetProperty(ref _osdBadgeText, value);
        }

        public bool IsOsdActive
        {
            get => _isOsdActive;
            set => SetProperty(ref _isOsdActive, value);
        }

        public bool IsFpsLimitButtonEnabled
        {
            get => _isFpsLimitButtonEnabled;
            set => SetProperty(ref _isFpsLimitButtonEnabled, value);
        }

        public string FpsLimitSubtitle
        {
            get => _fpsLimitSubtitle;
            set => SetProperty(ref _fpsLimitSubtitle, value);
        }

        public string FpsLimitBadgeText
        {
            get => _fpsLimitBadgeText;
            set => SetProperty(ref _fpsLimitBadgeText, value);
        }

        public bool IsFpsLimitActive
        {
            get => _isFpsLimitActive;
            set => SetProperty(ref _isFpsLimitActive, value);
        }

        public bool IsFpsLimitMismatch
        {
            get => _isFpsLimitMismatch;
            set => SetProperty(ref _isFpsLimitMismatch, value);
        }

        public bool IsHdrButtonEnabled
        {
            get => _isHdrButtonEnabled;
            set => SetProperty(ref _isHdrButtonEnabled, value);
        }

        public string HdrSubtitle
        {
            get => _hdrSubtitle;
            set => SetProperty(ref _hdrSubtitle, value);
        }

        public string HdrBadgeText
        {
            get => _hdrBadgeText;
            set => SetProperty(ref _hdrBadgeText, value);
        }

        public bool IsHdrActive
        {
            get => _isHdrActive;
            set => SetProperty(ref _isHdrActive, value);
        }

        public string DisplaySummary
        {
            get => _displaySummary;
            set => SetProperty(ref _displaySummary, value);
        }

        public string RtssStatusSummary
        {
            get => _rtssStatusSummary;
            set => SetProperty(ref _rtssStatusSummary, value);
        }

        public string CpuUsage
        {
            get => _cpuUsage;
            set => SetProperty(ref _cpuUsage, value);
        }

        public string CpuWattage
        {
            get => _cpuWattage;
            set => SetProperty(ref _cpuWattage, value);
        }

        public string GpuUsage
        {
            get => _gpuUsage;
            set => SetProperty(ref _gpuUsage, value);
        }

        public string GpuWattage
        {
            get => _gpuWattage;
            set => SetProperty(ref _gpuWattage, value);
        }

        public string RamUsage
        {
            get => _ramUsage;
            set => SetProperty(ref _ramUsage, value);
        }

        public string RamUsed
        {
            get => _ramUsed;
            set => SetProperty(ref _ramUsed, value);
        }

        public string VramUsage
        {
            get => _vramUsage;
            set => SetProperty(ref _vramUsage, value);
        }

        public string VramUsed
        {
            get => _vramUsed;
            set => SetProperty(ref _vramUsed, value);
        }

        public QuickAccessViewModel(CoreDispatcher dispatcher = null) : base(dispatcher)
        {
        }

        public void UpdateTelemetry(string cpuUsage, string cpuWattage, string gpuUsage, string gpuWattage, string ramUsage, string ramUsed, string vramUsage, string vramUsed)
        {
            CpuUsage = cpuUsage;
            CpuWattage = cpuWattage;
            GpuUsage = gpuUsage;
            GpuWattage = gpuWattage;
            RamUsage = ramUsage;
            RamUsed = ramUsed;
            VramUsage = vramUsage;
            VramUsed = vramUsed;
        }

        public static string GetOsdLevelName(int level)
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

        public void UpdateOsdUI(int level, bool isConnected, bool isRtssRunning, int rtssElevationValue, int lastActiveLevel)
        {
            if (isConnected && !isRtssRunning)
            {
                IsOsdButtonEnabled = false;
                OsdSubtitle = (rtssElevationValue == -1) ? "RTSS Not Installed" : "RTSS Offline";
                OsdBadgeText = "OFFLINE";
                IsOsdActive = false;
                return;
            }

            IsOsdButtonEnabled = true;

            if (level <= 0)
            {
                string savedLevelName = GetOsdLevelName(lastActiveLevel > 0 ? lastActiveLevel : 1);
                OsdSubtitle = $"Disabled (saved: {savedLevelName})";
                OsdBadgeText = "OFF";
                IsOsdActive = false;
            }
            else
            {
                string levelName = GetOsdLevelName(level);
                OsdSubtitle = $"Active ({levelName})";
                OsdBadgeText = levelName;
                IsOsdActive = true;
            }
        }

        public void UpdateFpsLimitUI(bool isLimitOn, int fpsValue, bool isConnected, bool isRtssRunning, bool isMismatch, int rtssElevationValue)
        {
            if (isConnected && !isRtssRunning)
            {
                IsFpsLimitButtonEnabled = false;
                FpsLimitSubtitle = (rtssElevationValue == -1) ? "RTSS Not Installed" : "RTSS Offline";
                FpsLimitBadgeText = "OFFLINE";
                IsFpsLimitActive = false;
                IsFpsLimitMismatch = false;
                return;
            }

            if (isConnected && isMismatch)
            {
                IsFpsLimitButtonEnabled = true;
                IsFpsLimitMismatch = true;
                if (!isLimitOn)
                {
                    FpsLimitSubtitle = $"Disabled: {fpsValue} FPS (Elevate to modify)";
                    FpsLimitBadgeText = "OFF • 🔒";
                    IsFpsLimitActive = false;
                }
                else
                {
                    FpsLimitSubtitle = $"Active: {fpsValue} FPS (Elevate to modify)";
                    FpsLimitBadgeText = $"{fpsValue} FPS • 🔒";
                    IsFpsLimitActive = true;
                }
                return;
            }

            IsFpsLimitButtonEnabled = true;
            IsFpsLimitMismatch = false;

            if (!isLimitOn)
            {
                FpsLimitSubtitle = $"Disabled (saved: {fpsValue} FPS)";
                FpsLimitBadgeText = "OFF";
                IsFpsLimitActive = false;
            }
            else
            {
                FpsLimitSubtitle = $"Capped at {fpsValue} FPS";
                FpsLimitBadgeText = $"{fpsValue} FPS";
                IsFpsLimitActive = true;
            }
        }

        public void UpdateHdrUI(bool isEnabled, bool isSupported)
        {
            if (!isSupported)
            {
                IsHdrButtonEnabled = false;
                HdrSubtitle = "Not supported by display";
                HdrBadgeText = "SDR";
                IsHdrActive = false;
                return;
            }

            IsHdrButtonEnabled = true;

            if (isEnabled)
            {
                HdrSubtitle = "High Dynamic Range active";
                HdrBadgeText = "ON";
                IsHdrActive = true;
            }
            else
            {
                HdrSubtitle = "High Dynamic Range disabled";
                HdrBadgeText = "OFF";
                IsHdrActive = false;
            }
        }

        public void UpdateDisplaySummary(string resolution, int refreshRate, bool isHdr)
        {
            string hdrTag = isHdr ? " (HDR)" : "";
            if (!string.IsNullOrEmpty(resolution) && refreshRate > 0)
            {
                DisplaySummary = $"{resolution} @ {refreshRate}Hz{hdrTag}";
            }
            else if (!string.IsNullOrEmpty(resolution))
            {
                DisplaySummary = $"{resolution}{hdrTag}";
            }
            else
            {
                DisplaySummary = $"Display{hdrTag}";
            }
        }

        public void UpdateRtssSummary(bool isConnected, bool isRunning, int elevationValue, bool isMismatch)
        {
            if (!isConnected)
            {
                RtssStatusSummary = "Connecting to Helper...";
            }
            else if (!isRunning)
            {
                RtssStatusSummary = elevationValue == -1 ? "RTSS Not Installed" : "RTSS Offline";
            }
            else if (isMismatch)
            {
                RtssStatusSummary = "Running (Admin, Helper Needs Elevation)";
            }
            else
            {
                RtssStatusSummary = elevationValue == 2 ? "Running (Elevated)" : "Running (Standard)";
            }
        }
    }
}
