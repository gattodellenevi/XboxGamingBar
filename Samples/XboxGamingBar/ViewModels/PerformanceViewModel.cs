using Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Core;

namespace XboxGamingBar.ViewModels
{
    public class JudderFreeMarker
    {
        public int Value { get; set; }
        public double XPercent { get; set; }

        public JudderFreeMarker(int value, double xPercent)
        {
            Value = value;
            XPercent = xPercent;
        }
    }

    /// <summary>
    /// ViewModel for Performance tuning: RTSS FPS Limiter, Judder-free mode,
    /// OSD levels & sizing, and RTSS elevation/privilege status.
    /// </summary>
    public class PerformanceViewModel : ViewModelBase
    {
        private bool _isLimitFpsEnabled = false;
        private int _fpsLimit = 60;
        private int _fpsLimitMode = 0;
        private bool _isJudderFreeEnabled = false;
        private int _osdLevel = 0;
        private int _osdTextSize = 100;
        private int _rtssElevationStatus = 0; // -1: not installed, 0: not running, 1: standard user, 2: elevated
        private bool _isElevationMismatch = false;
        private bool _isRtssRunning = false;
        private bool _isElevationWarningVisible = false;
        private string _elevationWarningText = string.Empty;

        public bool IsLimitFpsEnabled
        {
            get => _isLimitFpsEnabled;
            set => SetProperty(ref _isLimitFpsEnabled, value);
        }

        public int FpsLimit
        {
            get => _fpsLimit;
            set => SetProperty(ref _fpsLimit, value);
        }

        public int FpsLimitMode
        {
            get => _fpsLimitMode;
            set => SetProperty(ref _fpsLimitMode, value);
        }

        public bool IsJudderFreeEnabled
        {
            get => _isJudderFreeEnabled;
            set => SetProperty(ref _isJudderFreeEnabled, value);
        }

        public int OsdLevel
        {
            get => _osdLevel;
            set => SetProperty(ref _osdLevel, value);
        }

        public int OsdTextSize
        {
            get => _osdTextSize;
            set => SetProperty(ref _osdTextSize, value);
        }

        public int RtssElevationStatus
        {
            get => _rtssElevationStatus;
            set
            {
                if (SetProperty(ref _rtssElevationStatus, value))
                {
                    IsRtssRunning = value > 0;
                }
            }
        }

        public bool IsElevationMismatch
        {
            get => _isElevationMismatch;
            set => SetProperty(ref _isElevationMismatch, value);
        }

        public bool IsRtssRunning
        {
            get => _isRtssRunning;
            set => SetProperty(ref _isRtssRunning, value);
        }

        public bool IsElevationWarningVisible
        {
            get => _isElevationWarningVisible;
            set => SetProperty(ref _isElevationWarningVisible, value);
        }

        public string ElevationWarningText
        {
            get => _elevationWarningText;
            set => SetProperty(ref _elevationWarningText, value);
        }

        public PerformanceViewModel(CoreDispatcher dispatcher = null) : base(dispatcher)
        {
        }

        /// <summary>
        /// Calculates judder-free divisors for a given display refresh rate.
        /// </summary>
        public List<int> GetJudderFreeValues(int activeRefreshRate, int minFps = 30)
        {
            if (activeRefreshRate <= 0) return new List<int>();
            return RTSSHelper.GetJudderFreeFPSValues(activeRefreshRate, minFps);
        }

        /// <summary>
        /// Calculates markers for the canvas overlay in either judder-free or standard linear mode.
        /// </summary>
        public List<JudderFreeMarker> CalculateMarkers(int activeRefreshRate, bool isJudderFreeMode, double sliderMin, double sliderMax)
        {
            var result = new List<JudderFreeMarker>();
            var judderFreeValues = GetJudderFreeValues(activeRefreshRate, 30);
            if (judderFreeValues.Count == 0) return result;

            if (isJudderFreeMode)
            {
                int count = judderFreeValues.Count;
                for (int i = 0; i < count; i++)
                {
                    double percent = count > 1 ? (double)i / (count - 1) : 0;
                    result.Add(new JudderFreeMarker(judderFreeValues[i], percent));
                }
            }
            else
            {
                if (sliderMax <= sliderMin) return result;
                foreach (int val in judderFreeValues)
                {
                    if (val < sliderMin || val > sliderMax) continue;
                    double percent = (val - sliderMin) / (sliderMax - sliderMin);
                    result.Add(new JudderFreeMarker(val, percent));
                }
            }

            return result;
        }

        /// <summary>
        /// Evaluates elevation synchronization between RTSS and the helper service.
        /// </summary>
        public void EvaluateElevationStatus(bool isConnected, int rtssElevation, bool isHelperElevated)
        {
            RtssElevationStatus = rtssElevation;
            IsRtssRunning = isConnected && rtssElevation > 0;
            IsElevationMismatch = isConnected && rtssElevation == 2 && !isHelperElevated;

            if (IsElevationMismatch)
            {
                IsElevationWarningVisible = true;
                ElevationWarningText = "RTSS is running as Administrator, but the Helper service is running as a standard user. Restart the Helper as Administrator to regain control.";
            }
            else if (!IsRtssRunning && isConnected)
            {
                IsElevationWarningVisible = false;
                ElevationWarningText = rtssElevation == -1 ? "RTSS is not installed." : "RTSS is not running.";
            }
            else
            {
                IsElevationWarningVisible = false;
                ElevationWarningText = string.Empty;
            }
        }
    }
}
