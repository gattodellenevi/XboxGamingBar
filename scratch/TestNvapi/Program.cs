using System;
using System.Runtime.InteropServices;
using System.Text;

class Program
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr NvAPI_QueryInterfaceDelegate(uint id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvAPI_InitializeDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvAPI_EnumPhysicalGPUsDelegate([Out] IntPtr[] handles, out int count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvAPI_GPU_GetFullNameDelegate(IntPtr handle, StringBuilder name);

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

    static void Main()
    {
        IntPtr hModule = LoadLibrary("nvapi64.dll");
        if (hModule == IntPtr.Zero)
        {
            Console.WriteLine("nvapi64.dll not found");
            return;
        }

        IntPtr pQuery = GetProcAddress(hModule, "nvapi_QueryInterface");
        var query = Marshal.GetDelegateForFunctionPointer<NvAPI_QueryInterfaceDelegate>(pQuery);

        IntPtr pInit = query(0x0150E828);
        var fnInit = Marshal.GetDelegateForFunctionPointer<NvAPI_InitializeDelegate>(pInit);
        int resInit = fnInit();
        Console.WriteLine($"NvAPI_Initialize: {resInit}");

        IntPtr pEnum = query(0xE5AC921F);
        var fnEnum = Marshal.GetDelegateForFunctionPointer<NvAPI_EnumPhysicalGPUsDelegate>(pEnum);
        IntPtr[] gpus = new IntPtr[64];
        int count = 0;
        int resEnum = fnEnum(gpus, out count);
        Console.WriteLine($"NvAPI_EnumPhysicalGPUs: {resEnum}, count={count}");

        if (count == 0) return;
        IntPtr gpu = gpus[0];

        IntPtr pName = query(0xCEEE8E9F);
        var fnName = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_GetFullNameDelegate>(pName);
        var sb = new StringBuilder(64);
        fnName(gpu, sb);
        Console.WriteLine($"GPU Name: '{sb}'");

        // 1. Pstates (Usage)
        IntPtr pPstates = query(0x60DED2ED);
        Console.WriteLine($"pPstates ptr: 0x{pPstates.ToInt64():X}");
        if (pPstates != IntPtr.Zero)
        {
            var fnPstates = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_GetDynamicPstatesInfoExDelegate>(pPstates);
            var pstates = new NV_GPU_DYNAMIC_PSTATES_INFO_EX();
            pstates.Version = (uint)Marshal.SizeOf(typeof(NV_GPU_DYNAMIC_PSTATES_INFO_EX)) | 0x10000;
            pstates.Utilization = new NV_GPU_DYNAMIC_PSTATES_UTILIZATION[8];
            int resPstates = fnPstates(gpu, ref pstates);
            Console.WriteLine($"GetDynamicPstatesInfoEx res={resPstates}, IsPercentage={pstates.Utilization[0].IsPercentage}, Percentage={pstates.Utilization[0].Percentage}");
        }

        // 2. Clocks
        IntPtr pClock = query(0xDCB616C3);
        Console.WriteLine($"pClock ptr: 0x{pClock.ToInt64():X}");
        if (pClock != IntPtr.Zero)
        {
            var fnClock = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_GetAllClockFrequenciesDelegate>(pClock);
            var clocks = new NV_GPU_CLOCK_FREQUENCIES_V2();
            clocks.Version = (uint)Marshal.SizeOf(typeof(NV_GPU_CLOCK_FREQUENCIES_V2)) | 0x20000;
            clocks.ClockType = 0;
            clocks.Clocks = new NV_GPU_CLOCK_DOMAIN[32];
            int resClock = fnClock(gpu, ref clocks);
            Console.WriteLine($"GetAllClockFrequencies res={resClock}");
            for (int i = 0; i < 8; i++)
            {
                if (clocks.Clocks[i].IsPresent != 0)
                {
                    Console.WriteLine($"  Clock[{i}]: {clocks.Clocks[i].Frequency} (freq/1000 = {clocks.Clocks[i].Frequency / 1000.0f})");
                }
            }
        }

        // 3. Thermal
        IntPtr pThermal = query(0xE3640A56);
        Console.WriteLine($"pThermal ptr: 0x{pThermal.ToInt64():X}");
        if (pThermal != IntPtr.Zero)
        {
            var fnThermal = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_GetThermalSettingsDelegate>(pThermal);
            var therm = new NV_GPU_THERMAL_SETTINGS_V2();
            therm.Version = (uint)Marshal.SizeOf(typeof(NV_GPU_THERMAL_SETTINGS_V2)) | 0x20000;
            therm.Sensors = new NV_SENSOR[3];
            int resThermal = fnThermal(gpu, 0, ref therm);
            Console.WriteLine($"GetThermalSettings res={resThermal}, Count={therm.Count}, Temp={therm.Sensors[0].CurrentTemp}");
        }

        // 4. Power Topology
        IntPtr pPower = query(0xEDCF624E);
        Console.WriteLine($"pPower ptr: 0x{pPower.ToInt64():X}");
        if (pPower != IntPtr.Zero)
        {
            var fnPower = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_ClientPowerTopologyGetStatusDelegate>(pPower);
            var topo = new NVAPI_GPU_POWER_TOPO();
            topo.Version = (uint)Marshal.SizeOf(typeof(NVAPI_GPU_POWER_TOPO)) | 0x10000;
            topo.Entries = new NV_POWER_TOPO_ENTRY[4];
            int resPower = fnPower(gpu, ref topo);
            Console.WriteLine($"GetPowerTopology res={resPower}, Count={topo.Count}, Power={(topo.Count > 0 ? topo.Entries[0].Power : 0)} mW");
        }

        // 4b. Power Policies (0x70916171)
        IntPtr pPolicy = query(0x70916171);
        Console.WriteLine($"pPolicy ptr: 0x{pPolicy.ToInt64():X}");
        if (pPolicy != IntPtr.Zero)
        {
            var fnPolicy = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_ClientPowerTopologyGetStatusDelegate>(pPolicy);
            var topo2 = new NVAPI_GPU_POWER_TOPO();
            topo2.Version = (uint)Marshal.SizeOf(typeof(NVAPI_GPU_POWER_TOPO)) | 0x10000;
            topo2.Entries = new NV_POWER_TOPO_ENTRY[4];
            int resPolicy = fnPolicy(gpu, ref topo2);
            Console.WriteLine($"ClientPowerPoliciesGetStatus res={resPolicy}, Count={topo2.Count}, Power={(topo2.Count > 0 ? topo2.Entries[0].Power : 0)}");
        }

        IntPtr pMemEx = query(0xC0599498);

        // Loop 5 times like Update()
        for (int step = 0; step < 5; step++)
        {
            Console.WriteLine($"--- Loop Step {step} ---");
            if (pPstates != IntPtr.Zero)
            {
                var fnPstates = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_GetDynamicPstatesInfoExDelegate>(pPstates);
                var pstates = new NV_GPU_DYNAMIC_PSTATES_INFO_EX();
                pstates.Version = (uint)Marshal.SizeOf(typeof(NV_GPU_DYNAMIC_PSTATES_INFO_EX)) | 0x10000;
                pstates.Utilization = new NV_GPU_DYNAMIC_PSTATES_UTILIZATION[8];
                int res = fnPstates(gpu, ref pstates);
                Console.WriteLine($"  Pstates: res={res}, Load={pstates.Utilization[0].Percentage}%");
            }

            if (pClock != IntPtr.Zero)
            {
                var fnClock = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_GetAllClockFrequenciesDelegate>(pClock);
                var clocks = new NV_GPU_CLOCK_FREQUENCIES_V2();
                clocks.Version = (uint)Marshal.SizeOf(typeof(NV_GPU_CLOCK_FREQUENCIES_V2)) | 0x20000;
                clocks.ClockType = 0;
                clocks.Clocks = new NV_GPU_CLOCK_DOMAIN[32];
                int res = fnClock(gpu, ref clocks);
                Console.WriteLine($"  Clock: res={res}, Core={clocks.Clocks[0].Frequency/1000}MHz");
            }

            if (pThermal != IntPtr.Zero)
            {
                var fnThermal = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_GetThermalSettingsDelegate>(pThermal);
                var therm = new NV_GPU_THERMAL_SETTINGS_V2();
                therm.Version = (uint)Marshal.SizeOf(typeof(NV_GPU_THERMAL_SETTINGS_V2)) | 0x20000;
                therm.Sensors = new NV_SENSOR[3];
                int res = fnThermal(gpu, 0, ref therm);
                Console.WriteLine($"  Thermal: res={res}, Temp={therm.Sensors[0].CurrentTemp}C");
            }

            if (pPower != IntPtr.Zero)
            {
                var fnPower = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_ClientPowerTopologyGetStatusDelegate>(pPower);
                var topo = new NVAPI_GPU_POWER_TOPO();
                topo.Version = (uint)Marshal.SizeOf(typeof(NVAPI_GPU_POWER_TOPO)) | 0x10000;
                topo.Entries = new NV_POWER_TOPO_ENTRY[4];
                int res = fnPower(gpu, ref topo);
                Console.WriteLine($"  Power: res={res}, Count={topo.Count}");
            }

            if (pMemEx != IntPtr.Zero)
            {
                var fnMemEx = Marshal.GetDelegateForFunctionPointer<NvAPI_GPU_GetMemoryInfoExDelegate>(pMemEx);
                var memEx = new NV_GPU_MEMORY_INFO_EX();
                memEx.Version = (uint)Marshal.SizeOf(typeof(NV_GPU_MEMORY_INFO_EX)) | 0x10000;
                int res = fnMemEx(gpu, ref memEx);
                Console.WriteLine($"  Memory: res={res}, Used={(memEx.DedicatedVideoMemory - memEx.CurAvailableDedicatedVideoMemory)/(1024*1024)}MB");
            }

            System.Threading.Thread.Sleep(500);
        }

        TestDxgi();
    }

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

    [DllImport("dxgi.dll")]
    private static extern int CreateDXGIFactory1(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppFactory);

    [ComImport]
    [Guid("770aae78-f26f-4dba-a829-253c83d1b387")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDXGIFactory1
    {
        [PreserveSig] int SetPrivateData(ref Guid Name, uint DataSize, IntPtr pData);
        [PreserveSig] int SetPrivateDataInterface(ref Guid Name, [MarshalAs(UnmanagedType.IUnknown)] object pUnknown);
        [PreserveSig] int GetPrivateData(ref Guid Name, ref uint pDataSize, IntPtr pData);
        [PreserveSig] int GetParent(ref Guid riid, out IntPtr ppParent);
        [PreserveSig] int EnumAdapters(uint Adapter, out IntPtr ppAdapter);
        [PreserveSig] int MakeWindowAssociation(IntPtr WindowHandle, uint Flags);
        [PreserveSig] int GetWindowAssociation(out IntPtr pWindowHandle);
        [PreserveSig] int CreateSwapChain([MarshalAs(UnmanagedType.IUnknown)] object pDevice, IntPtr pDesc, out IntPtr ppSwapChain);
        [PreserveSig] int CreateSoftwareAdapter(IntPtr Module, out IntPtr ppAdapter);
        [PreserveSig] int EnumAdapters1(uint Adapter, [MarshalAs(UnmanagedType.IUnknown)] out object ppAdapter);
        [PreserveSig] int IsCurrent();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct DXGI_ADAPTER_DESC1
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Description;
        public uint VendorId;
        public uint DeviceId;
        public uint SubSysId;
        public uint Revision;
        public UIntPtr DedicatedVideoMemory;
        public UIntPtr DedicatedSystemMemory;
        public UIntPtr SharedSystemMemory;
        public long AdapterLuid;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DXGI_QUERY_VIDEO_MEMORY_INFO
    {
        public ulong Budget;
        public ulong CurrentUsage;
        public ulong AvailableForReservation;
        public ulong CurrentReservation;
    }

    [ComImport]
    [Guid("29038f61-3839-4626-91fd-086879011a05")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDXGIAdapter1
    {
        [PreserveSig] int SetPrivateData(ref Guid Name, uint DataSize, IntPtr pData);
        [PreserveSig] int SetPrivateDataInterface(ref Guid Name, [MarshalAs(UnmanagedType.IUnknown)] object pUnknown);
        [PreserveSig] int GetPrivateData(ref Guid Name, ref uint pDataSize, IntPtr pData);
        [PreserveSig] int GetParent(ref Guid riid, out IntPtr ppParent);
        [PreserveSig] int EnumOutputs(uint Output, out IntPtr ppOutput);
        [PreserveSig] int GetDesc(IntPtr pDesc);
        [PreserveSig] int CheckInterfaceSupport(ref Guid InterfaceName, out long pUMDVersion);
        [PreserveSig] int GetDesc1(out DXGI_ADAPTER_DESC1 pDesc);
    }

    [ComImport]
    [Guid("645967a4-1392-4310-a798-8053ce3e93fd")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDXGIAdapter3 : IDXGIAdapter1
    {
        [PreserveSig] new int SetPrivateData(ref Guid Name, uint DataSize, IntPtr pData);
        [PreserveSig] new int SetPrivateDataInterface(ref Guid Name, [MarshalAs(UnmanagedType.IUnknown)] object pUnknown);
        [PreserveSig] new int GetPrivateData(ref Guid Name, ref uint pDataSize, IntPtr pData);
        [PreserveSig] new int GetParent(ref Guid riid, out IntPtr ppParent);
        [PreserveSig] new int EnumOutputs(uint Output, out IntPtr ppOutput);
        [PreserveSig] new int GetDesc(IntPtr pDesc);
        [PreserveSig] new int CheckInterfaceSupport(ref Guid InterfaceName, out long pUMDVersion);
        [PreserveSig] new int GetDesc1(out DXGI_ADAPTER_DESC1 pDesc);
        [PreserveSig] int GetDesc2(IntPtr pDesc);
        [PreserveSig] int RegisterHardwareContentProtectionTeardownStatusEvent(IntPtr hEvent, out uint pdwCookie);
        void UnregisterHardwareContentProtectionTeardownStatus(uint dwCookie);
        [PreserveSig] int QueryVideoMemoryInfo(uint NodeIndex, int MemorySegmentGroup, out DXGI_QUERY_VIDEO_MEMORY_INFO pVideoMemoryInfo);
    }

    [DllImport("dxgi.dll")]
    private static extern int CreateDXGIFactory2(uint flags, ref Guid riid, out IntPtr ppFactory);

    static void TestDxgi()
    {
        Guid iidFactory4 = new Guid("1bc6ea02-ef36-464f-bf0c-21ca39e5168a"); // IDXGIFactory4
        int hrF = CreateDXGIFactory2(0, ref iidFactory4, out IntPtr pFactory);
        Console.WriteLine($"CreateDXGIFactory2 hr=0x{hrF:X8}, ptr=0x{pFactory.ToInt64():X}");
        if (hrF == 0 && pFactory != IntPtr.Zero)
        {
            var factory4 = (IDXGIFactory4)Marshal.GetObjectForIUnknown(pFactory);
            for (uint i = 0; ; i++)
            {
                int res = factory4.EnumAdapters1(i, out object adapterObj);
                if (res != 0 || adapterObj == null) break;
                var adapter1 = (IDXGIAdapter1)adapterObj;
                adapter1.GetDesc1(out DXGI_ADAPTER_DESC1 desc);
                Console.WriteLine($"Factory4 Adapter {i}: {desc.Description} (Dedicated={desc.DedicatedVideoMemory.ToUInt64()/(1024*1024)} MB)");
                Guid iidAd3 = new Guid("645967a4-1392-4310-a798-8053ce3e93fd");
                int hrLuid = factory4.EnumAdapterByLuid(desc.AdapterLuid, ref iidAd3, out IntPtr pAd3);
                Console.WriteLine($"  EnumAdapterByLuid: hr=0x{hrLuid:X8}");
                if (hrLuid == 0)
                {
                    var adapter3 = (IDXGIAdapter3)Marshal.GetObjectForIUnknown(pAd3);
                    adapter3.QueryVideoMemoryInfo(0, 0, out DXGI_QUERY_VIDEO_MEMORY_INFO info0);
                    Console.WriteLine($"  Local: Budget={info0.Budget/(1024*1024)} MB, Usage={info0.CurrentUsage/(1024*1024)} MB");
                    Marshal.Release(pAd3);
                }
            }
            Marshal.Release(pFactory);
        }
    }

    [ComImport]
    [Guid("1bc6ea02-ef36-464f-bf0c-21ca39e5168a")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDXGIFactory4
    {
        [PreserveSig] int SetPrivateData(ref Guid Name, uint DataSize, IntPtr pData);
        [PreserveSig] int SetPrivateDataInterface(ref Guid Name, [MarshalAs(UnmanagedType.IUnknown)] object pUnknown);
        [PreserveSig] int GetPrivateData(ref Guid Name, ref uint pDataSize, IntPtr pData);
        [PreserveSig] int GetParent(ref Guid riid, out IntPtr ppParent);
        [PreserveSig] int EnumAdapters(uint Adapter, out IntPtr ppAdapter);
        [PreserveSig] int MakeWindowAssociation(IntPtr WindowHandle, uint Flags);
        [PreserveSig] int GetWindowAssociation(out IntPtr pWindowHandle);
        [PreserveSig] int CreateSwapChain([MarshalAs(UnmanagedType.IUnknown)] object pDevice, IntPtr pDesc, out IntPtr ppSwapChain);
        [PreserveSig] int CreateSoftwareAdapter(IntPtr Module, out IntPtr ppAdapter);
        [PreserveSig] int EnumAdapters1(uint Adapter, [MarshalAs(UnmanagedType.IUnknown)] out object ppAdapter);
        [PreserveSig] int IsCurrent();
        [PreserveSig] int IsWindowedStereoEnabled();
        [PreserveSig] int CreateSwapChainForHwnd([MarshalAs(UnmanagedType.IUnknown)] object pDevice, IntPtr hWnd, IntPtr pDesc, IntPtr pFullscreenDesc, [MarshalAs(UnmanagedType.IUnknown)] object pRestrictToOutput, out IntPtr ppSwapChain);
        [PreserveSig] int CreateSwapChainForCoreWindow([MarshalAs(UnmanagedType.IUnknown)] object pDevice, [MarshalAs(UnmanagedType.IUnknown)] object pWindow, IntPtr pDesc, [MarshalAs(UnmanagedType.IUnknown)] object pRestrictToOutput, out IntPtr ppSwapChain);
        [PreserveSig] int GetSharedResourceAdapterLuid(IntPtr hResource, out long pLuid);
        [PreserveSig] int RegisterStereoStatusWindow(IntPtr WindowHandle, uint wMsg, out uint pdwCookie);
        [PreserveSig] int RegisterStereoStatusEvent(IntPtr hEvent, out uint pdwCookie);
        void UnregisterStereoStatus(uint dwCookie);
        [PreserveSig] int RegisterOcclusionStatusWindow(IntPtr WindowHandle, uint wMsg, out uint pdwCookie);
        [PreserveSig] int RegisterOcclusionStatusEvent(IntPtr hEvent, out uint pdwCookie);
        void UnregisterOcclusionStatus(uint dwCookie);
        [PreserveSig] int CreateSwapChainForComposition([MarshalAs(UnmanagedType.IUnknown)] object pDevice, IntPtr pDesc, [MarshalAs(UnmanagedType.IUnknown)] object pRestrictToOutput, out IntPtr ppSwapChain);
        [PreserveSig] int GetCreationFlags();
        [PreserveSig] int EnumAdapterByLuid(long AdapterLuid, ref Guid riid, out IntPtr ppvAdapter);
        [PreserveSig] int EnumWarpAdapter(ref Guid riid, out IntPtr ppvAdapter);
    }
}
