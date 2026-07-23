using NLog;
using Shared.Utilities;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace XboxGamingBarHelper.RTSS
{
    internal static class RTSSFPSLimiter
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static bool _isInitialized = false;
        private static bool _isAvailable = false;
        private static bool _profileLoaded = false;

        private const string GLOBAL_PROFILE = "";

        #region P/Invoke Declarations

        [DllImport("RTSSHooks64.dll", CharSet = CharSet.Ansi)]
        private static extern bool GetProfileProperty(string propertyName, IntPtr value, uint size);

        [DllImport("RTSSHooks64.dll", CharSet = CharSet.Ansi)]
        private static extern bool SetProfileProperty(string propertyName, IntPtr value, uint size);

        [DllImport("RTSSHooks64.dll", CharSet = CharSet.Ansi)]
        private static extern void LoadProfile(string profile = GLOBAL_PROFILE);

        [DllImport("RTSSHooks64.dll", CharSet = CharSet.Ansi)]
        private static extern void SaveProfile(string profile = GLOBAL_PROFILE);

        [DllImport("RTSSHooks64.dll", CharSet = CharSet.Ansi)]
        private static extern void UpdateProfiles();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        #endregion

        private static IntPtr _hooksModule = IntPtr.Zero;

        public static bool Initialize()
        {
            if (_isInitialized)
                return _isAvailable;

            _isInitialized = true;

            try
            {
                if (!RTSSHelper.IsInstalled(out string rtssPath))
                {
                    Logger.Info("RTSSFPSLimiter: RTSS is not installed");
                    _isAvailable = false;
                    return false;
                }

                string hooksDllPath = Path.Combine(rtssPath, "RTSSHooks64.dll");
                if (!File.Exists(hooksDllPath))
                {
                    Logger.Warn($"RTSSFPSLimiter: RTSSHooks64.dll not found at {hooksDllPath}");
                    _isAvailable = false;
                    return false;
                }

                _hooksModule = LoadLibrary(hooksDllPath);
                if (_hooksModule == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    Logger.Error($"RTSSFPSLimiter: Failed to load RTSSHooks64.dll, error code: {error}");
                    _isAvailable = false;
                    return false;
                }

                Logger.Info($"RTSSFPSLimiter: Successfully loaded RTSSHooks64.dll from {hooksDllPath}");
                _isAvailable = true;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"RTSSFPSLimiter: Initialization failed: {ex.Message}");
                _isAvailable = false;
                return false;
            }
        }

        public static bool IsAvailable => _isAvailable;

        public static bool SetFPSLimit(int fpsLimit)
        {
            if (!_isAvailable)
            {
                Logger.Debug("RTSSFPSLimiter: Not available, cannot set FPS limit");
                return false;
            }

            if (!RTSSHelper.IsRunning())
            {
                Logger.Debug("RTSSFPSLimiter: RTSS is not running");
                return false;
            }

            try
            {
                LoadProfile(GLOBAL_PROFILE);

                if (SetProfileProperty("FramerateLimit", fpsLimit))
                {
                    SaveProfile(GLOBAL_PROFILE);
                    UpdateProfiles();

                    Logger.Info($"RTSSFPSLimiter: Set FramerateLimit to {fpsLimit}");
                    return true;
                }
                else
                {
                    Logger.Warn($"RTSSFPSLimiter: SetProfileProperty returned false for FramerateLimit={fpsLimit}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"RTSSFPSLimiter: Failed to set FPS limit: {ex.Message}");
            }

            return false;
        }

        public static int GetFPSLimit()
        {
            if (!_isAvailable)
                return -1;

            if (!RTSSHelper.IsRunning())
                return -1;

            try
            {
                if (!_profileLoaded)
                {
                    LoadProfile(GLOBAL_PROFILE);
                    _profileLoaded = true;
                }

                if (GetProfileProperty("FramerateLimit", out int fpsLimit))
                {
                    return fpsLimit;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"RTSSFPSLimiter: Failed to get FPS limit: {ex.Message}");
            }

            return -1;
        }

        public static bool SetFPSLimitMode(int mode)
        {
            if (!_isAvailable)
            {
                Logger.Debug("RTSSFPSLimiter: Not available, cannot set FPS limit mode");
                return false;
            }

            if (!RTSSHelper.IsRunning())
            {
                Logger.Debug("RTSSFPSLimiter: RTSS is not running");
                return false;
            }

            try
            {
                LoadProfile(GLOBAL_PROFILE);

                if (SetProfileProperty("SyncLimiter", mode))
                {
                    SaveProfile(GLOBAL_PROFILE);
                    UpdateProfiles();

                    Logger.Info($"RTSSFPSLimiter: Set SyncLimiter to {mode}");
                    return true;
                }
                else
                {
                    Logger.Warn($"RTSSFPSLimiter: SetProfileProperty returned false for SyncLimiter={mode}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"RTSSFPSLimiter: Failed to set FPS limit mode: {ex.Message}");
            }

            return false;
        }

        public static int GetFPSLimitMode()
        {
            if (!_isAvailable)
                return 0;

            if (!RTSSHelper.IsRunning())
                return 0;

            try
            {
                if (!_profileLoaded)
                {
                    LoadProfile(GLOBAL_PROFILE);
                    _profileLoaded = true;
                }

                if (GetProfileProperty("SyncLimiter", out int mode))
                {
                    return mode;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"RTSSFPSLimiter: Failed to get FPS limit mode: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Calculates the Judder Free FPS values (divisors of refreshRate starting from minFPS, e.g. 30).
        /// </summary>
        /// <param name="refreshRate">The display's refresh rate (e.g. 120)</param>
        /// <param name="minFPS">The minimum threshold for calculated FPS values (defaults to 30)</param>
        /// <returns>A sorted list of judder free FPS values</returns>
        public static System.Collections.Generic.List<int> GetJudderFreeFPSValues(int refreshRate, int minFPS = 30)
        {
            return RTSSHelper.GetJudderFreeFPSValues(refreshRate, minFPS);
        }

        #region Generic Property Helpers

        private static bool GetProfileProperty<T>(string propertyName, out T value) where T : struct
        {
            var bytes = new byte[Marshal.SizeOf<T>()];
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            value = default;

            try
            {
                if (!GetProfileProperty(propertyName, handle.AddrOfPinnedObject(), (uint)bytes.Length))
                    return false;

                value = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"RTSSFPSLimiter: GetProfileProperty failed: {ex.Message}");
                return false;
            }
            finally
            {
                handle.Free();
            }
        }

        private static bool SetProfileProperty<T>(string propertyName, T value) where T : struct
        {
            var bytes = new byte[Marshal.SizeOf<T>()];
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            try
            {
                Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
                return SetProfileProperty(propertyName, handle.AddrOfPinnedObject(), (uint)bytes.Length);
            }
            catch (Exception ex)
            {
                Logger.Error($"RTSSFPSLimiter: SetProfileProperty failed: {ex.Message}");
                return false;
            }
            finally
            {
                handle.Free();
            }
        }

        #endregion

        public static void Shutdown()
        {
            if (_hooksModule != IntPtr.Zero)
            {
                FreeLibrary(_hooksModule);
                _hooksModule = IntPtr.Zero;
                Logger.Info("RTSSFPSLimiter: Unloaded RTSSHooks64.dll");
            }
            _isAvailable = false;
            _isInitialized = false;
            _profileLoaded = false;
        }
    }
}
