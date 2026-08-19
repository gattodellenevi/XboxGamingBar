using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace Shared.Utilities
{
    public static partial class RTSSHelper
    {
        public const string RTSS_FILE_NAME = "RTSS";

        private static Process _cachedRtssProcess;
        private static DateTime _lastProcessCheckTime = DateTime.MinValue;
        private static string _cachedInstallDir;
        private static DateTime _lastInstallCheckTime = DateTime.MinValue;

        public static Process GetProcess()
        {
            if (_cachedRtssProcess != null)
            {
                try
                {
                    if (!_cachedRtssProcess.HasExited)
                    {
                        return _cachedRtssProcess;
                    }
                }
                catch
                {
                }

                _cachedRtssProcess.Dispose();
                _cachedRtssProcess = null;
            }

            var now = DateTime.UtcNow;
            if ((now - _lastProcessCheckTime).TotalMilliseconds < 1500)
            {
                return null;
            }
            _lastProcessCheckTime = now;

            try
            {
                var rtssProcesses = Process.GetProcessesByName(RTSS_FILE_NAME);
                if (rtssProcesses.Length > 0)
                {
                    _cachedRtssProcess = rtssProcesses[0];
                    for (int i = 1; i < rtssProcesses.Length; i++)
                    {
                        rtssProcesses[i].Dispose();
                    }
                    return _cachedRtssProcess;
                }
            }
            catch
            {
            }

            return null;
        }

        public static bool IsRunning()
        {
            var rtssProcess = GetProcess();
            if (rtssProcess == null)
            {
                return false;
            }

            try
            {
                return (DateTime.Now - rtssProcess.StartTime).TotalSeconds >= 2.0f;
            }
            catch
            {
                return true;
            }
        }

        public static bool IsInstalled(out string installDir)
        {
            if (!string.IsNullOrEmpty(_cachedInstallDir))
            {
                installDir = _cachedInstallDir;
                return true;
            }

            if ((DateTime.UtcNow - _lastInstallCheckTime).TotalSeconds < 5.0)
            {
                installDir = null;
                return false;
            }
            _lastInstallCheckTime = DateTime.UtcNow;

            installDir = RegistryHelper.ReadStringValue(Registry.LocalMachine, @"Software\WOW6432Node\Unwinder\RTSS", "InstallDir");
            _cachedInstallDir = installDir;
            return !string.IsNullOrEmpty(installDir);
        }

        public static System.Collections.Generic.List<int> GetJudderFreeFPSValues(int refreshRate, int minFPS = 30)
        {
            var values = new System.Collections.Generic.List<int>();
            if (refreshRate <= 0)
                return values;

            for (int i = minFPS; i <= refreshRate; i++)
            {
                if (refreshRate % i == 0)
                {
                    values.Add(i);
                }
            }

            return values;
        }
    }
}
