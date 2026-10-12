using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace XboxGamingBarHelper.Windows
{
    internal class User32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;

            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;

            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;

            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly List<string> ProtectedProcesses = new List<string>() { "parsecd", "Taskmgr" };

        private struct CachedProcessInfo
        {
            public string ProcessName;
            public string ExecutablePath;
            public bool IsProtected;

            public CachedProcessInfo(string processName, string executablePath, bool isProtected)
            {
                ProcessName = processName;
                ExecutablePath = executablePath;
                IsProtected = isProtected;
            }
        }

        private static readonly Dictionary<int, CachedProcessInfo> ProcessCache = new Dictionary<int, CachedProcessInfo>();
        private static readonly HashSet<int> CurrentScanPids = new HashSet<int>();

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, int flags, StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        public static bool IsProcessAlive(int processId)
        {
            if (processId <= 0) return false;
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (hProcess != IntPtr.Zero)
            {
                CloseHandle(hProcess);
                return true;
            }
            return false;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out IntPtr lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        // Delegate for the EnumWindows callback function
        private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        // DllImport for the EnumWindows function
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        // DllImport for IsWindowVisible
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        // DllImport for GetShellWindow
        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int ChangeDisplaySettings(ref DEVMODE lpDevMode, int dwFlags);

        private static int GetWindowProcessId(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                Logger.Error("Can't get process id from invalid window handle.");
                return -1;
            }

            GetWindowThreadProcessId(windowHandle, out IntPtr windowProcessId);
            if (windowProcessId == IntPtr.Zero)
            {
                Logger.Error("Can't get window process id.");
                return -1;
            }

            return (int)windowProcessId;
        }

        public static string GetWindowTitle(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                Logger.Error("Can't get window title from invalid window handle.");
                return string.Empty;
            }

            var length = GetWindowTextLength(windowHandle);
            if (length == 0)
            {
                //Logger.Error($"Window doesn't have window title??");
                return string.Empty;
            }

            var sb = new StringBuilder(length + 1);
            GetWindowText(windowHandle, sb, sb.Capacity);

            return sb.ToString();
        }

        private static bool GetProcessInfo(int processId, out CachedProcessInfo info)
        {
            if (ProcessCache.TryGetValue(processId, out info))
            {
                return true;
            }

            string processName = string.Empty;
            string executablePath = string.Empty;
            bool isProtected = false;

            IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (hProcess != IntPtr.Zero)
            {
                try
                {
                    var sb = new StringBuilder(1024);
                    int capacity = sb.Capacity;
                    if (QueryFullProcessImageName(hProcess, 0, sb, ref capacity))
                    {
                        executablePath = sb.ToString();
                        try
                        {
                            processName = Path.GetFileNameWithoutExtension(executablePath);
                        }
                        catch
                        {
                            processName = string.Empty;
                        }
                    }
                }
                finally
                {
                    CloseHandle(hProcess);
                }
            }

            // Fallback to managed Process if native query didn't yield a path or process name (e.g. system kernel processes)
            if (string.IsNullOrEmpty(executablePath) || string.IsNullOrEmpty(processName))
            {
                try
                {
                    using (var process = Process.GetProcessById(processId))
                    {
                        if (string.IsNullOrEmpty(processName))
                        {
                            processName = process.ProcessName;
                        }

                        if (string.IsNullOrEmpty(executablePath))
                        {
                            try
                            {
                                executablePath = process.MainModule?.FileName ?? string.Empty;
                            }
                            catch
                            {
                                // Suppress access denied on protected processes
                            }
                        }
                    }
                }
                catch
                {
                    // Process may have already exited
                }
            }

            if (!string.IsNullOrEmpty(processName))
            {
                isProtected = ProtectedProcesses.Contains(processName);
            }

            info = new CachedProcessInfo(processName, executablePath, isProtected);
            ProcessCache[processId] = info;
            return true;
        }

        public static void GetOpenWindows(IDictionary<int, ProcessWindow> windows)
        {
            var shellWindow = GetShellWindow();
            var foregroundWindowProcessId = GetWindowProcessId(GetForegroundWindow());
            if (windows == null)
            {
                windows = new Dictionary<int, ProcessWindow>();
            }
            else
            {
                windows.Clear();
            }

            CurrentScanPids.Clear();

            EnumWindows(delegate (IntPtr hWnd, int lParam)
            {
                // Exclude the shell window itself
                if (hWnd == shellWindow)
                {
                    Logger.Debug("Skip window because it's the shell window.");
                    return true;
                }

                // Exclude invisible windows
                if (!IsWindowVisible(hWnd))
                {
                    return true;
                }

                var processId = GetWindowProcessId(hWnd);
                var windowTitle = GetWindowTitle(hWnd);
                if (processId == -1)
                {
                    Logger.Warn($"Window doesn't have process id???");
                    return true; // Continue enumeration
                }

                CurrentScanPids.Add(processId);

                if (!GetProcessInfo(processId, out var procInfo))
                {
                    return true;
                }

                if (procInfo.IsProtected)
                {
                    Logger.Debug($"Ignore protected process {procInfo.ProcessName}");
                    return true;
                }

                windows[processId] = new ProcessWindow(processId, hWnd, windowTitle, procInfo.ProcessName, procInfo.ExecutablePath, processId == foregroundWindowProcessId);
                return true; // Continue enumeration
            }, 0);

            // Prune cache if it grows large or contains dead PIDs (keep memory footprint tiny and prevent PID recycling collisions)
            if (ProcessCache.Count > 100)
            {
                var stalePids = new List<int>();
                foreach (var kvp in ProcessCache)
                {
                    if (!CurrentScanPids.Contains(kvp.Key))
                    {
                        stalePids.Add(kvp.Key);
                    }
                }

                foreach (var stalePid in stalePids)
                {
                    ProcessCache.Remove(stalePid);
                }
            }
        }

        private const int ENUM_CURRENT_SETTINGS = -1;
        private const int CDS_UPDATEREGISTRY = 0x01;
        private const int CDS_TEST = 0x02;
        private const int DISP_CHANGE_SUCCESSFUL = 0;
        private const int DM_PELSWIDTH = 0x00080000;
        private const int DM_PELSHEIGHT = 0x00100000;
        private const int DM_DISPLAYFREQUENCY = 0x00400000;
        private const int DM_DISPLAYORIENTATION = 0x00000080;

        public static int GetCurrentRefreshRate()
        {
            DEVMODE vDevMode = new DEVMODE();
            vDevMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

            if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref vDevMode))
            {
                Logger.Info($"Current refresh rate: {vDevMode.dmDisplayFrequency}Hz");
                return vDevMode.dmDisplayFrequency;
            }
            Logger.Error("Failed to get current refresh rate.");
            return 0; // failed
        }

        private static readonly object _displayModesLock = new object();
        private static List<int> _cachedRefreshRates = null;
        private static List<(int width, int height)> _cachedNativeResolutions = null;

        public static void InvalidateDisplayModesCache()
        {
            lock (_displayModesLock)
            {
                _cachedRefreshRates = null;
                _cachedNativeResolutions = null;
            }
        }

        public static List<int> GetSupportedRefreshRates(bool forceRefresh = false)
        {
            lock (_displayModesLock)
            {
                if (!forceRefresh && _cachedRefreshRates != null)
                {
                    return new List<int>(_cachedRefreshRates);
                }
            }

            List<int> refreshRates = new List<int>();
            DEVMODE devMode = new DEVMODE();
            devMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

            int modeIndex = 0;
            (int nativeWidth, int nativeHeight) = GetNativeResolution();
            float aspectRatio = (float)nativeWidth / nativeHeight;
            const float tolerance = 0.001f;

            while (EnumDisplaySettings(null, modeIndex++, ref devMode))
            {
                int rate = devMode.dmDisplayFrequency;
                float refreshRateAspectRatio = (float)devMode.dmPelsWidth / devMode.dmPelsHeight;
                if (rate > 0 && !refreshRates.Contains(rate) && Math.Abs(aspectRatio - refreshRateAspectRatio) <= tolerance)
                {
                    refreshRates.Add(rate);
                }
                else
                {
                    Logger.Debug($"Skipping refresh rate {rate}Hz at resolution {devMode.dmPelsWidth}x{devMode.dmPelsHeight} because it's not compatible with current aspect ratio {refreshRateAspectRatio} !~ {aspectRatio}");
                }
            }

            refreshRates.Sort();
            foreach (var refreshRate in refreshRates)
            {
                Logger.Info($"Found refresh rate {refreshRate}");
            }

            lock (_displayModesLock)
            {
                _cachedRefreshRates = new List<int>(refreshRates);
            }
            return refreshRates;
        }

        /// <summary>
        /// Set monitor refresh rate to a supported value.
        /// </summary>
        public static bool SetRefreshRate(int targetRate)
        {
            /*var supported = GetSupportedRefreshRates();
            if (!supported.Contains(targetRate))
            {
                Console.WriteLine($"Error: {targetRate}Hz is not supported. Supported rates: {string.Join(", ", supported)}");
                return false;
            }*/

            DEVMODE mode = new DEVMODE { dmSize = (short)Marshal.SizeOf(typeof(DEVMODE)) };

            if (!EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref mode))
            {
                Logger.Info("Error: Could not retrieve current display settings.");
                return false;
            }

            mode.dmDisplayFrequency = targetRate;
            mode.dmFields |= DM_DISPLAYFREQUENCY;

            // Test before applying
            int testResult = ChangeDisplaySettings(ref mode, CDS_TEST);
            if (testResult != DISP_CHANGE_SUCCESSFUL)
            {
                Logger.Info($"Test failed: {targetRate}Hz not valid on this mode.");
                return false;
            }

            // Apply permanently
            int result = ChangeDisplaySettings(ref mode, CDS_UPDATEREGISTRY);
            if (result == DISP_CHANGE_SUCCESSFUL)
            {
                Logger.Info($"Sucessfully set refresh rate to {targetRate}Hz.");
                return true;
            }
            else
            {
                Logger.Info($"Failed to set refresh rate to {targetRate}Hz (error code {result}).");
                return false;
            }
        }

        static (int width, int height) GetNativeResolution()
        {
            // Current mode = ENUM_CURRENT_SETTINGS (-1)
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

            if (EnumDisplaySettings(null, -1, ref dm) && dm.dmPelsWidth > 0 && dm.dmPelsHeight > 0)
            {
                return (dm.dmPelsWidth, dm.dmPelsHeight);
            }

            return (1920, 1080);
        }

        public static List<(int width, int height)> GetSupportedResolutions()
        {
            var modes = new List<(int width, int height)>();
            int modeNum = 0;
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

            while (EnumDisplaySettings(null, modeNum, ref dm))
            {
                if (dm.dmPelsWidth > 0 && dm.dmPelsHeight > 0)
                {
                    modes.Add((dm.dmPelsWidth, dm.dmPelsHeight));
                }
                modeNum++;
            }
            return modes;
        }

        public static List<(int width, int height)> GetSupportedNativeResolutions(bool forceRefresh = false)
        {
            lock (_displayModesLock)
            {
                if (!forceRefresh && _cachedNativeResolutions != null)
                {
                    return new List<(int width, int height)>(_cachedNativeResolutions);
                }
            }

            var allModes = GetSupportedResolutions();
            var native = GetNativeResolution();

            double nativeRatio = native.height > 0 ? (double)native.width / native.height : (16.0 / 9.0);

            var filtered = new List<(int width, int height)>();
            var seen = new HashSet<string>();

            foreach (var m in allModes)
            {
                if (m.height <= 0) continue;
                double ratio = (double)m.width / m.height;

                // Accept resolutions very close to the same aspect ratio (tolerance 0.01)
                if (Math.Abs(ratio - nativeRatio) < 0.01)
                {
                    string key = $"{m.width}x{m.height}";
                    if (!seen.Contains(key))
                    {
                        filtered.Add(m);
                        seen.Add(key);
                    }
                }
            }

            // Always ensure the native resolution is present
            if (native.width > 0 && native.height > 0)
            {
                string nativeKey = $"{native.width}x{native.height}";
                if (!seen.Contains(nativeKey))
                {
                    filtered.Add(native);
                    seen.Add(nativeKey);
                }
            }

            // Sort resolutions descending by pixel count (e.g. 3840x2160, 2560x1440, 1920x1080, 1280x720)
            filtered.Sort((a, b) => (b.width * b.height).CompareTo(a.width * a.height));

            lock (_displayModesLock)
            {
                _cachedNativeResolutions = new List<(int width, int height)>(filtered);
            }
            return filtered;
        }

        public static (int width, int height) GetCurrentResolution()
        {
            const int ENUM_CURRENT_SETTINGS = -1;

            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

            if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref dm))
            {
                Logger.Info($"Current resolution: {dm.dmPelsWidth}x{dm.dmPelsHeight}");
                return (dm.dmPelsWidth, dm.dmPelsHeight);
            }

            Logger.Error("Failed to get current resolution.");
            return (0, 0); // fallback
        }

        public static void SetResolution(int width, int height)
        {
            Logger.Info($"Attempting to set resolution to {width}x{height}...");

            DEVMODE currentMode = new DEVMODE { dmSize = (short)Marshal.SizeOf(typeof(DEVMODE)) };
            bool hasCurrent = EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref currentMode);
            int currentFrequency = hasCurrent ? currentMode.dmDisplayFrequency : 0;
            int currentOrientation = hasCurrent ? currentMode.dmDisplayOrientation : 0;

            DEVMODE targetMode = new DEVMODE { dmSize = (short)Marshal.SizeOf(typeof(DEVMODE)) };
            bool found = false;
            int modeNum = 0;
            
            DEVMODE bestMatch = new DEVMODE { dmSize = (short)Marshal.SizeOf(typeof(DEVMODE)) };
            int bestFrequency = -1;

            while (EnumDisplaySettings(null, modeNum++, ref targetMode))
            {
                if (targetMode.dmPelsWidth == width && targetMode.dmPelsHeight == height)
                {
                    // If we find a mode with matching frequency, that's perfect.
                    if (targetMode.dmDisplayFrequency == currentFrequency)
                    {
                        bestMatch = targetMode;
                        found = true;
                        break;
                    }
                    
                    // Otherwise, keep track of the one with highest frequency as a fallback.
                    if (targetMode.dmDisplayFrequency > bestFrequency)
                    {
                        bestFrequency = targetMode.dmDisplayFrequency;
                        bestMatch = targetMode;
                        found = true;
                    }
                }
            }

            if (!found)
            {
                Logger.Error($"Could not find a supported mode for {width}x{height}.");
                return;
            }

            // Important: We want to preserve the current orientation.
            // Many handhelds have internal panels that are physically portrait but used as landscape.
            if (hasCurrent)
            {
                bestMatch.dmDisplayOrientation = currentOrientation;
                bestMatch.dmFields |= DM_DISPLAYORIENTATION;
            }

            // Test before applying
            int testResult = ChangeDisplaySettings(ref bestMatch, CDS_TEST);
            if (testResult != DISP_CHANGE_SUCCESSFUL)
            {
                Logger.Warn($"Test failed for mode {width}x{height} @ {bestMatch.dmDisplayFrequency}Hz with orientation {bestMatch.dmDisplayOrientation}. Error: {testResult}. Trying without orientation flag.");
                bestMatch.dmFields &= ~DM_DISPLAYORIENTATION;
                testResult = ChangeDisplaySettings(ref bestMatch, CDS_TEST);
            }

            if (testResult != DISP_CHANGE_SUCCESSFUL)
            {
                Logger.Error($"Failed to validate resolution {width}x{height}. Error code: {testResult}");
                return;
            }

            // Apply permanently
            int result = ChangeDisplaySettings(ref bestMatch, CDS_UPDATEREGISTRY);
            if (result == DISP_CHANGE_SUCCESSFUL)
            {
                Logger.Info($"Resolution changed successfully to {width}x{height}.");
            }
            else
            {
                Logger.Error($"Failed to apply resolution {width}x{height}. Error code: {result}");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        const uint INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_KEYUP = 0x0002;

        public static void SendKeyboardInput(IEnumerable<ushort> keyCodes)
        {
            var keys = new List<ushort>(keyCodes);
            if (keys.Count == 0) return;

            int len = keys.Count * 2; // Press + Release
            INPUT[] inputs = new INPUT[len];
            int i = 0;

            // Key Downs
            foreach (var key in keys)
            {
                inputs[i].type = INPUT_KEYBOARD;
                inputs[i].u.ki.wVk = key;
                inputs[i].u.ki.dwFlags = 0; // KeyDown
                i++;
            }

            // Key Ups (Reverse order)
            for (int j = keys.Count - 1; j >= 0; j--)
            {
                inputs[i].type = INPUT_KEYBOARD;
                inputs[i].u.ki.wVk = keys[j];
                inputs[i].u.ki.dwFlags = KEYEVENTF_KEYUP;
                i++;
            }

            SendInput((uint)len, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public const UInt32 WM_KEYDOWN = 0x0100;
        public const int VK_CONTROL = 0x11;
        public const int VK_SHIFT = 0x10;
        public const int VK_O = 0x4F; // Virtual key code for 'O'
        public const uint WM_CLOSE = 0x0010;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public const uint SPI_GETMOUSESPEED = 0x0070;
        public const uint SPI_SETMOUSESPEED = 0x0071;
        public const uint SPI_GETMOUSE = 0x0003;
        public const uint SPI_SETMOUSE = 0x0004;

        public const uint SPIF_UPDATEINIFILE = 0x01;
        public const uint SPIF_SENDCHANGE = 0x02;

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfoGet(uint uiAction, uint uiParam, ref uint pvParam, uint fWinIni);

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfoSet(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfoMouse(uint uiAction, uint uiParam, [In, Out] int[] pvParam, uint fWinIni);

        public static int GetMouseSpeed()
        {
            try
            {
                uint speed = 10;
                if (SystemParametersInfoGet(SPI_GETMOUSESPEED, 0, ref speed, 0))
                {
                    return (int)Math.Max(1, Math.Min(20, speed));
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to get mouse speed.");
            }
            return 10;
        }

        public static bool SetMouseSpeed(int speed)
        {
            try
            {
                int clampedSpeed = Math.Max(1, Math.Min(20, speed));
                Logger.Info($"Setting system mouse speed to {clampedSpeed}.");
                return SystemParametersInfoSet(SPI_SETMOUSESPEED, 0, (IntPtr)clampedSpeed, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to set mouse speed.");
                return false;
            }
        }

        public static bool GetEnhancePointerPrecision()
        {
            try
            {
                int[] mouseParams = new int[3];
                if (SystemParametersInfoMouse(SPI_GETMOUSE, 0, mouseParams, 0))
                {
                    return mouseParams[2] != 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to get Enhance Pointer Precision.");
            }
            return false;
        }

        public static bool SetEnhancePointerPrecision(bool enabled)
        {
            try
            {
                int[] mouseParams = new int[3];
                if (SystemParametersInfoMouse(SPI_GETMOUSE, 0, mouseParams, 0))
                {
                    mouseParams[2] = enabled ? 1 : 0;
                    Logger.Info($"Setting Enhance Pointer Precision to {enabled}.");
                    return SystemParametersInfoMouse(SPI_SETMOUSE, 0, mouseParams, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to set Enhance Pointer Precision.");
            }
            return false;
        }

        public const uint SPI_GETWHEELSCROLLLINES = 0x0068;
        public const uint SPI_SETWHEELSCROLLLINES = 0x0069;

        public static int GetMouseScrollSpeed()
        {
            try
            {
                uint lines = 3;
                if (SystemParametersInfoGet(SPI_GETWHEELSCROLLLINES, 0, ref lines, 0))
                {
                    return (int)Math.Max(1, Math.Min(100, lines));
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to get mouse scroll speed.");
            }
            return 3;
        }

        public static bool SetMouseScrollSpeed(int lines)
        {
            try
            {
                uint clampedLines = (uint)Math.Max(1, Math.Min(100, lines));
                Logger.Info($"Setting mouse wheel scroll lines to {clampedLines}.");
                bool ok = SystemParametersInfoSet(SPI_SETWHEELSCROLLLINES, clampedLines, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                try
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                    {
                        if (key != null)
                        {
                            key.SetValue("WheelScrollLines", clampedLines.ToString(), Microsoft.Win32.RegistryValueKind.String);
                        }
                    }
                }
                catch (Exception regEx)
                {
                    Logger.Debug(regEx, "Failed to persist WheelScrollLines to registry.");
                }
                return ok;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to set mouse scroll speed.");
                return false;
            }
        }

        #region CCD / HDR APIs

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_SOURCE_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_RATIONAL
        {
            public uint Numerator;
            public uint Denominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_TARGET_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint outputTechnology;
            public uint rotation;
            public uint scaling;
            public DISPLAYCONFIG_RATIONAL refreshRate;
            public uint scanLineOrdering;
            public bool targetAvailable;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_INFO
        {
            public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
            public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_MODE_INFO
        {
            public uint infoType;
            public uint id;
            public LUID adapterId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] modeInfo;
        }

        public enum DISPLAYCONFIG_DEVICE_INFO_TYPE : uint
        {
            DISPLAYCONFIG_DEVICE_INFO_GET_HEADER_NAME = 1,
            DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO = 9,
            DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE = 10,
            DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO_2 = 15,
            DISPLAYCONFIG_DEVICE_INFO_SET_HDR_STATE = 16
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_DEVICE_INFO_HEADER
        {
            public DISPLAYCONFIG_DEVICE_INFO_TYPE type;
            public uint size;
            public LUID adapterId;
            public uint id;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint value;
            public uint colorEncoding;
            public uint bitsPerColorChannel;

            public bool AdvancedColorSupported => (value & 0x1) != 0;
            public bool AdvancedColorEnabled => (value & 0x2) != 0;
            public bool WideColorEnforced => (value & 0x4) != 0;
            public bool AdvancedColorForceDisabled => (value & 0x8) != 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint value;
            public uint colorEncoding;
            public uint bitsPerColorChannel;
            public uint activeColorMode;

            public bool AdvancedColorSupported => (value & 0x1) != 0;
            public bool AdvancedColorActive => (value & 0x2) != 0;
            public bool HighDynamicRangeSupported => (value & 0x10) != 0;
            public bool HighDynamicRangeUserEnabled => (value & 0x20) != 0;
            public bool WideColorSupported => (value & 0x40) != 0;
            public bool WideColorUserEnabled => (value & 0x80) != 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint value;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_SET_HDR_STATE
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint value;
        }

        private const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;

        [DllImport("user32.dll")]
        public static extern int GetDisplayConfigBufferSizes(
            uint flags,
            out uint numPathArrayElements,
            out uint numModeInfoArrayElements);

        [DllImport("user32.dll")]
        public static extern int QueryDisplayConfig(
            uint flags,
            ref uint numPathArrayElements,
            [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
            ref uint numModeInfoArrayElements,
            [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
            IntPtr currentTopologyId);

        [DllImport("user32.dll")]
        public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO requestPacket);

        [DllImport("user32.dll")]
        public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2 requestPacket);

        [DllImport("user32.dll")]
        public static extern int DisplayConfigSetDeviceInfo(ref DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE setPacket);

        [DllImport("user32.dll")]
        public static extern int DisplayConfigSetDeviceInfo(ref DISPLAYCONFIG_SET_HDR_STATE setPacket);

        public static (bool supported, bool enabled) GetHDRStatus()
        {
            try
            {
                uint pathCount, modeCount;
                int err = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount);
                if (err != 0 || pathCount == 0)
                {
                    return (false, false);
                }

                var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
                var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

                err = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
                if (err != 0)
                {
                    return (false, false);
                }

                for (int i = 0; i < pathCount; i++)
                {
                    var p = paths[i];

                    // Try v2 first (Windows 11 24H2+)
                    var colorPacket2 = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2();
                    colorPacket2.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO_2;
                    colorPacket2.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2));
                    colorPacket2.header.adapterId = p.targetInfo.adapterId;
                    colorPacket2.header.id = p.targetInfo.id;

                    if (DisplayConfigGetDeviceInfo(ref colorPacket2) == 0)
                    {
                        bool supported = colorPacket2.HighDynamicRangeSupported;
                        bool enabled = colorPacket2.HighDynamicRangeUserEnabled || colorPacket2.activeColorMode == 2;
                        if (supported)
                        {
                            return (true, enabled);
                        }
                    }

                    // Fallback to v1 (legacy Windows 10/11)
                    var colorPacket = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO();
                    colorPacket.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO;
                    colorPacket.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO));
                    colorPacket.header.adapterId = p.targetInfo.adapterId;
                    colorPacket.header.id = p.targetInfo.id;

                    if (DisplayConfigGetDeviceInfo(ref colorPacket) == 0)
                    {
                        bool supported = colorPacket.AdvancedColorSupported && !colorPacket.WideColorEnforced;
                        bool enabled = supported && colorPacket.AdvancedColorEnabled;
                        if (supported)
                        {
                            return (true, enabled);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get HDR status.");
            }

            return (false, false);
        }

        public static bool SetHDR(bool enable)
        {
            Logger.Info($"Setting HDR to {enable}...");
            try
            {
                uint pathCount, modeCount;
                int err = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount);
                if (err != 0 || pathCount == 0)
                {
                    Logger.Warn($"GetDisplayConfigBufferSizes failed: {err}. Falling back to Win+Alt+B.");
                    InputSimulator.SendKeyCombo(false, true, false, true, 0x42);
                    return true;
                }

                var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
                var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

                err = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
                if (err != 0)
                {
                    Logger.Warn($"QueryDisplayConfig failed: {err}. Falling back to Win+Alt+B.");
                    InputSimulator.SendKeyCombo(false, true, false, true, 0x42);
                    return true;
                }

                bool appliedAny = false;
                for (int i = 0; i < pathCount; i++)
                {
                    var p = paths[i];

                    // Try Windows 11 24H2+ SET_HDR_STATE (16)
                    var hdrPacket = new DISPLAYCONFIG_SET_HDR_STATE();
                    hdrPacket.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_SET_HDR_STATE;
                    hdrPacket.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_SET_HDR_STATE));
                    hdrPacket.header.adapterId = p.targetInfo.adapterId;
                    hdrPacket.header.id = p.targetInfo.id;
                    hdrPacket.value = enable ? 1u : 0u;

                    int setErr = DisplayConfigSetDeviceInfo(ref hdrPacket);
                    Logger.Info($"DisplayConfigSetDeviceInfo (SET_HDR_STATE 16) returned {setErr} for path {i}");

                    if (setErr != 0)
                    {
                        // Fallback to legacy SET_ADVANCED_COLOR_STATE (10)
                        var advPacket = new DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE();
                        advPacket.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE;
                        advPacket.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE));
                        advPacket.header.adapterId = p.targetInfo.adapterId;
                        advPacket.header.id = p.targetInfo.id;
                        advPacket.value = enable ? 1u : 0u;

                        setErr = DisplayConfigSetDeviceInfo(ref advPacket);
                        Logger.Info($"DisplayConfigSetDeviceInfo (SET_ADVANCED_COLOR_STATE 10) returned {setErr} for path {i}");
                    }

                    if (setErr == 0)
                    {
                        appliedAny = true;
                    }
                }

                if (!appliedAny)
                {
                    Logger.Warn("DisplayConfigSetDeviceInfo failed for all paths. Falling back to Win+Alt+B shortcut.");
                    InputSimulator.SendKeyCombo(false, true, false, true, 0x42);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to set HDR status. Falling back to Win+Alt+B shortcut.");
                InputSimulator.SendKeyCombo(false, true, false, true, 0x42);
                return false;
            }
        }

        #endregion
    }
}
