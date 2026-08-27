using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Shared.Utilities
{
    public static partial class RTSSHelper
    {
        public const string RTSS_FILE_NAME = "RTSS";

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        private const uint TOKEN_QUERY = 0x0008;
        private const int TokenElevation = 20;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(IntPtr TokenHandle, int TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_ELEVATION
        {
            public int TokenIsElevated;
        }

        public static bool? IsProcessElevated(int processId)
        {
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                if (Marshal.GetLastWin32Error() == 5) // ERROR_ACCESS_DENIED: caller is standard, target is elevated
                {
                    return true;
                }
                return null;
            }

            try
            {
                if (!OpenProcessToken(hProcess, TOKEN_QUERY, out IntPtr hToken))
                {
                    if (Marshal.GetLastWin32Error() == 5)
                    {
                        return true;
                    }
                    return null;
                }

                try
                {
                    int size = Marshal.SizeOf<TOKEN_ELEVATION>();
                    IntPtr pElevation = Marshal.AllocHGlobal(size);
                    try
                    {
                        if (GetTokenInformation(hToken, TokenElevation, pElevation, (uint)size, out _))
                        {
                            var elevation = Marshal.PtrToStructure<TOKEN_ELEVATION>(pElevation);
                            return elevation.TokenIsElevated != 0;
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pElevation);
                    }
                }
                finally
                {
                    CloseHandle(hToken);
                }
            }
            finally
            {
                CloseHandle(hProcess);
            }

            return null;
        }

        public static int GetRTSSElevationStatus()
        {
            if (!IsInstalled(out _))
            {
                return -1; // Not installed
            }

            var process = GetProcess();
            if (process == null)
            {
                return 0; // Not running / Offline
            }

            var isElevated = IsProcessElevated(process.Id);
            if (isElevated == true)
            {
                return 2; // Elevated
            }
            else if (isElevated == false)
            {
                return 1; // Standard
            }

            return 0;
        }

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
