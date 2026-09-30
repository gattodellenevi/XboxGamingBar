using System;
using System.Runtime.InteropServices;
using System.Text;
using NLog;
using XboxGamingBarHelper.Hardware.Abstractions;
using XboxGamingBarHelper.Hardware.Dxgi;

namespace XboxGamingBarHelper.Hardware.Providers
{
    internal class NvidiaNvapiGpuMetricsProvider : IGpuMetricsProvider
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
        private delegate int NvAPI_EnumPhysicalGPUsDelegate([Out] IntPtr[] handles, out int count);

        // Dynamic P-States (Load %)
        [StructLayout(LayoutKind.Sequential)]
        private struct NV_GPU_DYNAMIC_PSTATES_UTILIZATION
        {
            public int IsPercentage;
            public uint Percentage;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NV_GPU_DYNAMIC_PSTATES_INFO_EX
        {
            public uint Version;
            public uint Flags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public NV_GPU_DYNAMIC_PSTATES_UTILIZATION[] Utilization;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GPU_GetDynamicPstatesInfoExDelegate(IntPtr handle, ref NV_GPU_DYNAMIC_PSTATES_INFO_EX info);

        // Thermal Settings (Temp °C)
        [StructLayout(LayoutKind.Sequential)]
        private struct NV_SENSOR
        {
            public int Controller;
            public uint DefaultMinTemp;
            public uint DefaultMaxTemp;
            public int CurrentTemp;
            public int Target;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NV_GPU_THERMAL_SETTINGS_V2
        {
            public uint Version;
            public uint Count;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public NV_SENSOR[] Sensors;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GPU_GetThermalSettingsDelegate(IntPtr handle, int sensorIndex, ref NV_GPU_THERMAL_SETTINGS_V2 settings);

        // Clock Frequencies (MHz)
        [StructLayout(LayoutKind.Sequential)]
        private struct NV_GPU_CLOCK_DOMAIN
        {
            public uint IsPresent;
            public uint Frequency;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NV_GPU_CLOCK_FREQUENCIES_V2
        {
            public uint Version;
            public uint ClockType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public NV_GPU_CLOCK_DOMAIN[] Clocks;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GPU_GetAllClockFrequenciesDelegate(IntPtr handle, ref NV_GPU_CLOCK_FREQUENCIES_V2 clocks);

        // Power Topology (Watts)
        [StructLayout(LayoutKind.Sequential)]
        private struct NV_POWER_TOPO_ENTRY
        {
            public uint Domain;
            public uint Unknown2;
            public uint Power; // milliwatts
            public uint Unknown4;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NVAPI_GPU_POWER_TOPO
        {
            public uint Version;
            public uint Count;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public NV_POWER_TOPO_ENTRY[] Entries;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GPU_ClientPowerTopologyGetStatusDelegate(IntPtr handle, ref NVAPI_GPU_POWER_TOPO topo);

        // Memory Info Ex (Total & Available VRAM)
        [StructLayout(LayoutKind.Sequential)]
        private struct NV_GPU_MEMORY_INFO_EX
        {
            public uint Version;
            public ulong DedicatedVideoMemory;
            public ulong AvailableDedicatedVideoMemory;
            public ulong SystemVideoMemory;
            public ulong SharedSystemMemory;
            public ulong CurAvailableDedicatedVideoMemory;
            public ulong DedicatedVideoMemoryEvictionsSize;
            public ulong DedicatedVideoMemoryEvictionCount;
            public ulong DedicatedVideoMemoryPromotionsSize;
            public ulong DedicatedVideoMemoryPromotionCount;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GPU_GetMemoryInfoExDelegate(IntPtr handle, ref NV_GPU_MEMORY_INFO_EX info);

        private IntPtr hModule = IntPtr.Zero;
        private bool isAvailable = false;
        private IntPtr primaryGpuHandle = IntPtr.Zero;
        private string gpuFullName = "NVIDIA GPU";

        // Delegates
        private NvAPI_UnloadDelegate fnUnload;
        private NvAPI_GPU_GetDynamicPstatesInfoExDelegate fnGetDynamicPstatesInfoEx;
        private NvAPI_GPU_GetThermalSettingsDelegate fnGetThermalSettings;
        private NvAPI_GPU_GetAllClockFrequenciesDelegate fnGetAllClockFrequencies;
        private NvAPI_GPU_ClientPowerTopologyGetStatusDelegate fnGetPowerTopology;
        private NvAPI_GPU_GetMemoryInfoExDelegate fnGetMemoryInfoEx;

        // Cached Metrics
        private float gpuUsage = -1.0f;
        private float gpuWattage = -1.0f;
        private float gpuClock = -1.0f;
        private float gpuMemoryUsedMb = -1.0f;
        private float gpuMemoryTotalMb = -1.0f;
        private float gpuMemoryClock = -1.0f;
        private float gpuTemperature = -1.0f;

        public string Name => gpuFullName;
        public GpuVendor Vendor => GpuVendor.Nvidia;
        public bool IsAvailable => isAvailable;

        public NvidiaNvapiGpuMetricsProvider(string name = "NVIDIA GPU")
        {
            if (!string.IsNullOrEmpty(name))
            {
                gpuFullName = name;
            }
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                string dllName = Environment.Is64BitProcess ? "nvapi64.dll" : "nvapi.dll";
                Logger.Info($"NVAPI: Loading {dllName}...");
                hModule = LoadLibrary(dllName);
                if (hModule == IntPtr.Zero)
                {
                    Logger.Info($"NVAPI: {dllName} not found on this system.");
                    return;
                }

                Logger.Info("NVAPI: Getting nvapi_QueryInterface export...");
                IntPtr pQuery = GetProcAddress(hModule, "nvapi_QueryInterface");
                if (pQuery == IntPtr.Zero)
                {
                    Logger.Warn("NVAPI: nvapi_QueryInterface export not found.");
                    return;
                }

                var query = Marshal.GetDelegateForFunctionPointer<NvAPI_QueryInterfaceDelegate>(pQuery);

                // NvAPI_Initialize (0x0150E828)
                Logger.Info("NVAPI: Querying NvAPI_Initialize (0x0150E828)...");
                IntPtr pInit = query(0x0150E828);
                if (pInit == IntPtr.Zero) { Logger.Warn("NVAPI: pInit is Zero"); return; }
                var fnInit = Marshal.GetDelegateForFunctionPointer<NvAPI_InitializeDelegate>(pInit);
                Logger.Info("NVAPI: Invoking NvAPI_Initialize...");
                int initRes = fnInit();
                Logger.Info($"NVAPI: NvAPI_Initialize returned {initRes}");
                if (initRes != 0)
                {
                    Logger.Warn($"NVAPI: NvAPI_Initialize failed with code {initRes}");
                    return;
                }

                // NvAPI_Unload (0xD22BDD7E)
                IntPtr pUnload = query(0xD22BDD7E);
                if (pUnload != IntPtr.Zero)
                {
                    fnUnload = Marshal.GetDelegateForFunctionPointer<NvAPI_UnloadDelegate>(pUnload);
                }

                // NvAPI_EnumPhysicalGPUs (0xE5AC921F)
                Logger.Info("NVAPI: Querying NvAPI_EnumPhysicalGPUs (0xE5AC921F)...");
                IntPtr pEnum = query(0xE5AC921F);
                if (pEnum == IntPtr.Zero) { Logger.Warn("NVAPI: pEnum is Zero"); return; }
                var fnEnum = Marshal.GetDelegateForFunctionPointer<NvAPI_EnumPhysicalGPUsDelegate>(pEnum);

                IntPtr[] gpus = new IntPtr[64];
                int count = 0;
                Logger.Info("NVAPI: Calling NvAPI_EnumPhysicalGPUs...");
                int enumRes = fnEnum(gpus, out count);
                Logger.Info($"NVAPI: NvAPI_EnumPhysicalGPUs returned {enumRes}, count={count}");
                if (enumRes != 0 || count == 0)
                {
                    Logger.Warn($"NVAPI: No physical NVIDIA GPUs found (count={count}, res={enumRes})");
                    return;
                }

                primaryGpuHandle = gpus[0];
                Logger.Info($"NVAPI: Primary GPU handle = 0x{primaryGpuHandle.ToInt64():X}, Name = '{gpuFullName}'");

                // Query Monitoring Functions
                IntPtr pPstates = query(0x60DED2ED);
                if (pPstates != IntPtr.Zero)
                {
                    fnGetDynamicPstatesInfoEx = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_GetDynamicPstatesInfoExDelegate>(pPstates);
                    Logger.Info("NVAPI: Bound NvAPI_GPU_GetDynamicPstatesInfoEx");
                }

                IntPtr pThermal = query(0xE3640A56);
                if (pThermal != IntPtr.Zero)
                {
                    fnGetThermalSettings = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_GetThermalSettingsDelegate>(pThermal);
                    Logger.Info("NVAPI: Bound NvAPI_GPU_GetThermalSettings");
                }

                IntPtr pClock = query(0xDCB616C3);
                if (pClock != IntPtr.Zero)
                {
                    fnGetAllClockFrequencies = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_GetAllClockFrequenciesDelegate>(pClock);
                    Logger.Info("NVAPI: Bound NvAPI_GPU_GetAllClockFrequencies");
                }

                IntPtr pPower = query(0xEDCF624E);
                if (pPower != IntPtr.Zero)
                {
                    fnGetPowerTopology = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_ClientPowerTopologyGetStatusDelegate>(pPower);
                    Logger.Info("NVAPI: Bound NvAPI_GPU_ClientPowerTopologyGetStatus");
                }

                // NvAPI_GPU_GetMemoryInfoEx (0xC0599498)
                IntPtr pMemEx = query(0xC0599498);
                if (pMemEx != IntPtr.Zero)
                {
                    fnGetMemoryInfoEx = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_GetMemoryInfoExDelegate>(pMemEx);
                    Logger.Info("NVAPI: Bound NvAPI_GPU_GetMemoryInfoEx");
                }

                isAvailable = true;
                Logger.Info($"NVAPI initialized successfully for primary GPU: {gpuFullName}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize NVIDIA NVAPI provider");
                isAvailable = false;
            }
        }

        public void Update()
        {
            if (!isAvailable || primaryGpuHandle == IntPtr.Zero) return;

            try
            {
                // 1. GPU Utilization (Load %)
                if (fnGetDynamicPstatesInfoEx != null)
                {
                    var pstates = new NV_GPU_DYNAMIC_PSTATES_INFO_EX();
                    pstates.Version = (uint)Marshal.SizeOf(typeof(NV_GPU_DYNAMIC_PSTATES_INFO_EX)) | 0x10000;
                    pstates.Utilization = new NV_GPU_DYNAMIC_PSTATES_UTILIZATION[8];
                    int res = fnGetDynamicPstatesInfoEx(primaryGpuHandle, ref pstates);
                    if (res == 0)
                    {
                        gpuUsage = pstates.Utilization[0].Percentage;
                    }
                    else if (res == -220) // NVAPI_GPU_NOT_POWERED (GPU sleeping in Optimus)
                    {
                        gpuUsage = 0.0f;
                    }
                }

                // 2. GPU Clocks (Core & Memory MHz)
                if (fnGetAllClockFrequencies != null)
                {
                    var clocks = new NV_GPU_CLOCK_FREQUENCIES_V2();
                    clocks.Version = (uint)Marshal.SizeOf(typeof(NV_GPU_CLOCK_FREQUENCIES_V2)) | 0x20000;
                    clocks.ClockType = 0; // Current
                    clocks.Clocks = new NV_GPU_CLOCK_DOMAIN[32];
                    int res = fnGetAllClockFrequencies(primaryGpuHandle, ref clocks);
                    if (res == 0)
                    {
                        if (clocks.Clocks[0].IsPresent != 0)
                        {
                            gpuClock = clocks.Clocks[0].Frequency / 1000.0f;
                        }
                        if (clocks.Clocks[4].IsPresent != 0)
                        {
                            gpuMemoryClock = clocks.Clocks[4].Frequency / 1000.0f;
                        }
                    }
                    else if (res == -220) // NVAPI_GPU_NOT_POWERED
                    {
                        gpuClock = 0.0f;
                        gpuMemoryClock = 0.0f;
                    }
                }

                // 3. GPU Temperature (°C)
                if (fnGetThermalSettings != null)
                {
                    var therm = new NV_GPU_THERMAL_SETTINGS_V2();
                    therm.Version = (uint)Marshal.SizeOf(typeof(NV_GPU_THERMAL_SETTINGS_V2)) | 0x20000;
                    therm.Sensors = new NV_SENSOR[3];
                    if (fnGetThermalSettings(primaryGpuHandle, 0, ref therm) == 0 && therm.Count > 0)
                    {
                        gpuTemperature = therm.Sensors[0].CurrentTemp;
                    }
                }

                // 4. GPU Power (Watts)
                if (fnGetPowerTopology != null)
                {
                    var topo = new NVAPI_GPU_POWER_TOPO();
                    topo.Version = (uint)Marshal.SizeOf(typeof(NVAPI_GPU_POWER_TOPO)) | 0x10000;
                    topo.Entries = new NV_POWER_TOPO_ENTRY[4];
                    if (fnGetPowerTopology(primaryGpuHandle, ref topo) == 0 && topo.Count > 0)
                    {
                        gpuWattage = topo.Entries[0].Power / 1000.0f;
                    }
                    else
                    {
                        gpuWattage = 0.0f;
                    }
                }

                // 5. VRAM (via NVAPI NvAPI_GPU_GetMemoryInfoEx)
                bool vramQueried = false;
                if (fnGetMemoryInfoEx != null)
                {
                    var memEx = new NV_GPU_MEMORY_INFO_EX();
                    memEx.Version = (uint)Marshal.SizeOf(typeof(NV_GPU_MEMORY_INFO_EX)) | 0x10000;
                    if (fnGetMemoryInfoEx(primaryGpuHandle, ref memEx) == 0 && memEx.DedicatedVideoMemory > 0)
                    {
                        gpuMemoryTotalMb = memEx.DedicatedVideoMemory / (1024.0f * 1024.0f);
                        gpuMemoryUsedMb = (memEx.DedicatedVideoMemory - memEx.CurAvailableDedicatedVideoMemory) / (1024.0f * 1024.0f);
                        vramQueried = true;
                    }
                }

                // Fallback to DXGI if NVAPI memory info failed
                if (!vramQueried && DxgiHelper.TryGetVramInfoByVendor(DxgiHelper.VENDOR_NVIDIA, out ulong usedBytes, out ulong totalBytes))
                {
                    if (usedBytes > 0) gpuMemoryUsedMb = usedBytes / (1024.0f * 1024.0f);
                    if (totalBytes > 0) gpuMemoryTotalMb = totalBytes / (1024.0f * 1024.0f);
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to update NVIDIA GPU telemetry");
            }
        }

        public float GetGpuUsage() => gpuUsage;
        public float GetGpuWattage() => gpuWattage;
        public float GetGpuClock() => gpuClock;
        public float GetGpuMemoryUsed() => gpuMemoryUsedMb;
        public float GetGpuMemoryTotal() => gpuMemoryTotalMb;
        public float GetGpuMemoryClock() => gpuMemoryClock;
        public float GetGpuTemperature() => gpuTemperature;

        public void Dispose()
        {
            try
            {
                if (isAvailable && fnUnload != null)
                {
                    fnUnload();
                }
            }
            catch { }
            isAvailable = false;
        }
    }
}
