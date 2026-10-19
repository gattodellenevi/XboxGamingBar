using NLog;
using Shared.Enums;
using System;
using System.Runtime.InteropServices;

namespace XboxGamingBarHelper.Nvidia
{
    internal class NvidiaDrsService : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const string NVAPI64_DLL = "nvapi64.dll";
        private const string NVAPI_DLL = "nvapi.dll";

        // Setting IDs
        private const uint VSYNCMODE_ID = 0x00A879CF;
        private const uint VSYNCTEARCONTROL_ID = 0x005A375C;

        // VSync Mode constants
        private const uint VSYNCMODE_PASSIVE = 0x60925292;
        private const uint VSYNCMODE_FORCEOFF = 0x08416747;
        private const uint VSYNCMODE_FORCEON = 0x47814940;
        private const uint VSYNCMODE_VIRTUAL = 0x18888888;
        private const uint VSYNCMODE_FLIPINTERVAL2 = 0x32610244;

        // Tear Control constants
        private const uint VSYNCTEARCONTROL_DISABLE = 0x96861077;
        private const uint VSYNCTEARCONTROL_ENABLE = 0x99941284;

        // Structure sizes and offsets for NVDRS_SETTING
        private const int SETTING_STRUCT_SIZE = 12320;
        private const uint SETTING_VERSION = (uint)(SETTING_STRUCT_SIZE | (1 << 16));
        private const int ID_OFFSET = 4100;
        private const int TYPE_OFFSET = 4104;
        private const int VALUE_OFFSET = 8220;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr NvAPI_QueryInterfaceDelegate(uint id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_InitializeDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_UnloadDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_DRS_CreateSessionDelegate(out IntPtr phSession);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_DRS_DestroySessionDelegate(IntPtr hSession);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_DRS_LoadSettingsDelegate(IntPtr hSession);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_DRS_SaveSettingsDelegate(IntPtr hSession);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_DRS_GetBaseProfileDelegate(IntPtr hSession, out IntPtr phProfile);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_DRS_GetSettingDelegate(IntPtr hSession, IntPtr hProfile, uint settingId, IntPtr pSetting);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_DRS_SetSettingDelegate(IntPtr hSession, IntPtr hProfile, IntPtr pSetting);

        private IntPtr hModule = IntPtr.Zero;
        private bool isAvailable = false;
        private bool isInitialized = false;

        private NvAPI_UnloadDelegate fnUnload;
        private NvAPI_DRS_CreateSessionDelegate fnCreateSession;
        private NvAPI_DRS_DestroySessionDelegate fnDestroySession;
        private NvAPI_DRS_LoadSettingsDelegate fnLoadSettings;
        private NvAPI_DRS_SaveSettingsDelegate fnSaveSettings;
        private NvAPI_DRS_GetBaseProfileDelegate fnGetBaseProfile;
        private NvAPI_DRS_GetSettingDelegate fnGetSetting;
        private NvAPI_DRS_SetSettingDelegate fnSetSetting;

        public bool IsAvailable => isAvailable;

        public NvidiaDrsService()
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                string dllName = Environment.Is64BitProcess ? NVAPI64_DLL : NVAPI_DLL;
                hModule = LoadLibrary(dllName);
                if (hModule == IntPtr.Zero)
                {
                    Logger.Info($"NVIDIA DRS: {dllName} not found on this system.");
                    return;
                }

                IntPtr pQuery = GetProcAddress(hModule, "nvapi_QueryInterface");
                if (pQuery == IntPtr.Zero)
                {
                    Logger.Warn("NVIDIA DRS: nvapi_QueryInterface export not found.");
                    return;
                }

                var query = Marshal.GetDelegateForFunctionPointer<NvAPI_QueryInterfaceDelegate>(pQuery);

                IntPtr pInit = query(0x0150E828);
                if (pInit == IntPtr.Zero)
                {
                    Logger.Warn("NVIDIA DRS: NvAPI_Initialize not found.");
                    return;
                }
                var fnInit = Marshal.GetDelegateForFunctionPointer<NvAPI_InitializeDelegate>(pInit);
                int initRes = fnInit();
                if (initRes != 0)
                {
                    Logger.Info($"NVIDIA DRS: NvAPI_Initialize returned {initRes} (no NVIDIA GPU or not supported).");
                    return;
                }
                isInitialized = true;

                IntPtr pUnload = query(0xD22BDD7E);
                if (pUnload != IntPtr.Zero)
                {
                    fnUnload = Marshal.GetDelegateForFunctionPointer<NvAPI_UnloadDelegate>(pUnload);
                }

                IntPtr pCreateSession = query(0x0694D52E);
                IntPtr pDestroySession = query(0xDAD9CFF8);
                IntPtr pLoadSettings = query(0x375DBD6B);
                IntPtr pSaveSettings = query(0xFCBC7E14);
                IntPtr pGetBaseProfile = query(0xDA8466A0);
                IntPtr pGetSetting = query(0x73BF8338);
                IntPtr pSetSetting = query(0x577DD202);

                if (pCreateSession == IntPtr.Zero || pDestroySession == IntPtr.Zero ||
                    pLoadSettings == IntPtr.Zero || pSaveSettings == IntPtr.Zero ||
                    pGetBaseProfile == IntPtr.Zero || pGetSetting == IntPtr.Zero ||
                    pSetSetting == IntPtr.Zero)
                {
                    Logger.Warn("NVIDIA DRS: One or more required DRS entry points were not found.");
                    return;
                }

                fnCreateSession = Marshal.GetDelegateForFunctionPointer<NvAPI_DRS_CreateSessionDelegate>(pCreateSession);
                fnDestroySession = Marshal.GetDelegateForFunctionPointer<NvAPI_DRS_DestroySessionDelegate>(pDestroySession);
                fnLoadSettings = Marshal.GetDelegateForFunctionPointer<NvAPI_DRS_LoadSettingsDelegate>(pLoadSettings);
                fnSaveSettings = Marshal.GetDelegateForFunctionPointer<NvAPI_DRS_SaveSettingsDelegate>(pSaveSettings);
                fnGetBaseProfile = Marshal.GetDelegateForFunctionPointer<NvAPI_DRS_GetBaseProfileDelegate>(pGetBaseProfile);
                fnGetSetting = Marshal.GetDelegateForFunctionPointer<NvAPI_DRS_GetSettingDelegate>(pGetSetting);
                fnSetSetting = Marshal.GetDelegateForFunctionPointer<NvAPI_DRS_SetSettingDelegate>(pSetSetting);

                // Verify session creation and base profile access
                if (fnCreateSession(out IntPtr testSession) == 0)
                {
                    try
                    {
                        if (fnLoadSettings(testSession) == 0 && fnGetBaseProfile(testSession, out _) == 0)
                        {
                            isAvailable = true;
                            Logger.Info("NVIDIA DRS: Successfully initialized and verified base profile access.");
                        }
                    }
                    finally
                    {
                        fnDestroySession(testSession);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "NVIDIA DRS: Error initializing NVAPI DRS service.");
                isAvailable = false;
            }
        }

        public NvidiaVSyncMode GetVSyncMode()
        {
            if (!isAvailable) return NvidiaVSyncMode.ApplicationControlled;

            if (fnCreateSession(out IntPtr hSession) != 0) return NvidiaVSyncMode.ApplicationControlled;

            try
            {
                if (fnLoadSettings(hSession) != 0) return NvidiaVSyncMode.ApplicationControlled;

                if (fnGetBaseProfile(hSession, out IntPtr hProfile) != 0) return NvidiaVSyncMode.ApplicationControlled;

                IntPtr ptr = Marshal.AllocHGlobal(SETTING_STRUCT_SIZE);
                try
                {
                    // Query VSYNCMODE_ID
                    ZeroMemory(ptr, SETTING_STRUCT_SIZE);
                    Marshal.WriteInt32(ptr, 0, (int)SETTING_VERSION);
                    int resVsync = fnGetSetting(hSession, hProfile, VSYNCMODE_ID, ptr);
                    uint vsyncVal = resVsync == 0 ? (uint)Marshal.ReadInt32(ptr, VALUE_OFFSET) : VSYNCMODE_PASSIVE;

                    // Query VSYNCTEARCONTROL_ID
                    ZeroMemory(ptr, SETTING_STRUCT_SIZE);
                    Marshal.WriteInt32(ptr, 0, (int)SETTING_VERSION);
                    int resTear = fnGetSetting(hSession, hProfile, VSYNCTEARCONTROL_ID, ptr);
                    uint tearVal = resTear == 0 ? (uint)Marshal.ReadInt32(ptr, VALUE_OFFSET) : VSYNCTEARCONTROL_DISABLE;

                    var mode = ParseMode(vsyncVal, tearVal);
                    Logger.Debug($"NVIDIA DRS: GetVSyncMode resolved mode {mode} (vsync=0x{vsyncVal:X8}, tear=0x{tearVal:X8}).");
                    return mode;
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "NVIDIA DRS: Failed to query VSync mode.");
                return NvidiaVSyncMode.ApplicationControlled;
            }
            finally
            {
                fnDestroySession(hSession);
            }
        }

        public bool SetVSyncMode(NvidiaVSyncMode mode)
        {
            if (!isAvailable) return false;

            if (fnCreateSession(out IntPtr hSession) != 0) return false;

            try
            {
                if (fnLoadSettings(hSession) != 0) return false;

                if (fnGetBaseProfile(hSession, out IntPtr hProfile) != 0) return false;

                var (vsyncVal, tearVal) = ToNvApiValues(mode);

                IntPtr ptr = Marshal.AllocHGlobal(SETTING_STRUCT_SIZE);
                try
                {
                    // Set VSYNCMODE_ID
                    ZeroMemory(ptr, SETTING_STRUCT_SIZE);
                    Marshal.WriteInt32(ptr, 0, (int)SETTING_VERSION);
                    Marshal.WriteInt32(ptr, ID_OFFSET, unchecked((int)VSYNCMODE_ID));
                    Marshal.WriteInt32(ptr, TYPE_OFFSET, 0); // DWORD
                    Marshal.WriteInt32(ptr, VALUE_OFFSET, unchecked((int)vsyncVal));
                    int resVsync = fnSetSetting(hSession, hProfile, ptr);

                    // Set VSYNCTEARCONTROL_ID
                    ZeroMemory(ptr, SETTING_STRUCT_SIZE);
                    Marshal.WriteInt32(ptr, 0, (int)SETTING_VERSION);
                    Marshal.WriteInt32(ptr, ID_OFFSET, unchecked((int)VSYNCTEARCONTROL_ID));
                    Marshal.WriteInt32(ptr, TYPE_OFFSET, 0); // DWORD
                    Marshal.WriteInt32(ptr, VALUE_OFFSET, unchecked((int)tearVal));
                    int resTear = fnSetSetting(hSession, hProfile, ptr);

                    if (resVsync == 0 && resTear == 0)
                    {
                        int saveRes = fnSaveSettings(hSession);
                        Logger.Info($"NVIDIA DRS: Saved VSync mode {mode} (vsync=0x{vsyncVal:X8}, tear=0x{tearVal:X8}) with result {saveRes}.");
                        return saveRes == 0;
                    }
                    else
                    {
                        Logger.Warn($"NVIDIA DRS: Failed to set VSync setting values: resVsync={resVsync}, resTear={resTear}");
                        return false;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"NVIDIA DRS: Failed to set VSync mode {mode}.");
                return false;
            }
            finally
            {
                fnDestroySession(hSession);
            }
        }

        private static (uint vsyncVal, uint tearVal) ToNvApiValues(NvidiaVSyncMode mode)
        {
            switch (mode)
            {
                case NvidiaVSyncMode.Off:
                    return (VSYNCMODE_FORCEOFF, VSYNCTEARCONTROL_DISABLE);
                case NvidiaVSyncMode.On:
                    return (VSYNCMODE_FORCEON, VSYNCTEARCONTROL_DISABLE);
                case NvidiaVSyncMode.Adaptive:
                    return (VSYNCMODE_FORCEON, VSYNCTEARCONTROL_ENABLE);
                case NvidiaVSyncMode.Fast:
                    return (VSYNCMODE_VIRTUAL, VSYNCTEARCONTROL_DISABLE);
                case NvidiaVSyncMode.AdaptiveHalfRefresh:
                    return (VSYNCMODE_FLIPINTERVAL2, VSYNCTEARCONTROL_ENABLE);
                case NvidiaVSyncMode.ApplicationControlled:
                default:
                    return (VSYNCMODE_PASSIVE, VSYNCTEARCONTROL_DISABLE);
            }
        }

        private static NvidiaVSyncMode ParseMode(uint vsyncVal, uint tearVal)
        {
            if (tearVal == VSYNCTEARCONTROL_ENABLE)
            {
                if (vsyncVal == VSYNCMODE_FLIPINTERVAL2)
                {
                    return NvidiaVSyncMode.AdaptiveHalfRefresh;
                }
                return NvidiaVSyncMode.Adaptive;
            }

            switch (vsyncVal)
            {
                case VSYNCMODE_FORCEOFF:
                    return NvidiaVSyncMode.Off;
                case VSYNCMODE_FORCEON:
                    return NvidiaVSyncMode.On;
                case VSYNCMODE_VIRTUAL:
                    return NvidiaVSyncMode.Fast;
                case VSYNCMODE_FLIPINTERVAL2:
                    return NvidiaVSyncMode.AdaptiveHalfRefresh;
                case VSYNCMODE_PASSIVE:
                default:
                    return NvidiaVSyncMode.ApplicationControlled;
            }
        }

        private static void ZeroMemory(IntPtr ptr, int size)
        {
            for (int i = 0; i < size; i++)
            {
                Marshal.WriteByte(ptr, i, 0);
            }
        }

        public void Dispose()
        {
            try
            {
                if (isInitialized && fnUnload != null)
                {
                    fnUnload();
                    isInitialized = false;
                }

                if (hModule != IntPtr.Zero)
                {
                    FreeLibrary(hModule);
                    hModule = IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "NVIDIA DRS: Error disposing service.");
            }
        }
    }
}
