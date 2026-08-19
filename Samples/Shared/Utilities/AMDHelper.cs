using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace Shared.Utilities
{
    public class AMDHelper
    {
        public const string AMD_SOFTWARE_ADRENALINE_EDITION_FILE_NAME = "RadeonSoftware";

        private static Process _cachedAmdProcess;
        private static DateTime _lastProcessCheckTime = DateTime.MinValue;
        private static string _cachedInstallDir;
        private static DateTime _lastInstallCheckTime = DateTime.MinValue;

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

            installDir = RegistryHelper.ReadStringValue(Registry.LocalMachine, @"SOFTWARE\AMD\CN", "InstallDir");
            _cachedInstallDir = installDir;
            return !string.IsNullOrEmpty(installDir);
        }

        public static bool IsRunning(out Process process)
        {
            if (_cachedAmdProcess != null)
            {
                try
                {
                    if (!_cachedAmdProcess.HasExited)
                    {
                        process = _cachedAmdProcess;
                        try
                        {
                            return (DateTime.Now - process.StartTime).TotalSeconds >= 3.0f;
                        }
                        catch
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                }

                _cachedAmdProcess.Dispose();
                _cachedAmdProcess = null;
            }

            var now = DateTime.UtcNow;
            if ((now - _lastProcessCheckTime).TotalMilliseconds < 1500)
            {
                process = null;
                return false;
            }
            _lastProcessCheckTime = now;

            try
            {
                var amdProcesses = Process.GetProcessesByName(AMD_SOFTWARE_ADRENALINE_EDITION_FILE_NAME);
                if (amdProcesses.Length > 0)
                {
                    _cachedAmdProcess = amdProcesses[0];
                    for (int i = 1; i < amdProcesses.Length; i++)
                    {
                        amdProcesses[i].Dispose();
                    }
                    process = _cachedAmdProcess;
                    try
                    {
                        return (DateTime.Now - process.StartTime).TotalSeconds >= 3.0f;
                    }
                    catch
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }

            process = null;
            return false;
        }
    }
}
