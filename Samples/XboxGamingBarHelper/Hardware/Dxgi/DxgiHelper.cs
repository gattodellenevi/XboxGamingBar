using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NLog;
using XboxGamingBarHelper.Hardware.Abstractions;

namespace XboxGamingBarHelper.Hardware.Dxgi
{
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

    internal class DxgiAdapterInfo
    {
        public uint Index { get; set; }
        public string Description { get; set; }
        public uint VendorId { get; set; }
        public GpuVendor Vendor { get; set; }
        public ulong DedicatedVideoMemoryBytes { get; set; }
        public ulong SharedSystemMemoryBytes { get; set; }
        public bool IsSoftware { get; set; }
    }

    internal static class DxgiHelper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [DllImport("dxgi.dll")]
        private static extern int CreateDXGIFactory1(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppFactory);

        public const uint VENDOR_AMD = 0x1002;
        public const uint VENDOR_NVIDIA = 0x10DE;
        public const uint VENDOR_INTEL = 0x8086;
        public const uint VENDOR_MICROSOFT = 0x1414;

        public static GpuVendor MapVendorId(uint vendorId)
        {
            switch (vendorId)
            {
                case VENDOR_AMD:
                    return GpuVendor.Amd;
                case VENDOR_NVIDIA:
                    return GpuVendor.Nvidia;
                case VENDOR_INTEL:
                    return GpuVendor.Intel;
                default:
                    return GpuVendor.Other;
            }
        }

        public static List<DxgiAdapterInfo> EnumerateAdapters()
        {
            var adapters = new List<DxgiAdapterInfo>();
            try
            {
                Guid iid = typeof(IDXGIFactory1).GUID;
                int hr = CreateDXGIFactory1(ref iid, out object factoryObj);
                if (hr != 0 || factoryObj == null)
                {
                    Logger.Warn($"CreateDXGIFactory1 failed with HRESULT 0x{hr:X8}");
                    return adapters;
                }

                var factory = (IDXGIFactory1)factoryObj;
                for (uint i = 0; ; i++)
                {
                    int res = factory.EnumAdapters1(i, out object adapterObj);
                    if (res != 0 || adapterObj == null)
                    {
                        break;
                    }

                    try
                    {
                        var adapter1 = (IDXGIAdapter1)adapterObj;
                        if (adapter1.GetDesc1(out DXGI_ADAPTER_DESC1 desc) == 0)
                        {
                            bool isSoftware = (desc.Flags & 2) != 0 || desc.VendorId == VENDOR_MICROSOFT;
                            var info = new DxgiAdapterInfo
                            {
                                Index = i,
                                Description = desc.Description,
                                VendorId = desc.VendorId,
                                Vendor = MapVendorId(desc.VendorId),
                                DedicatedVideoMemoryBytes = (ulong)desc.DedicatedVideoMemory,
                                SharedSystemMemoryBytes = (ulong)desc.SharedSystemMemory,
                                IsSoftware = isSoftware
                            };
                            adapters.Add(info);
                        }
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(adapterObj);
                    }
                }

                Marshal.ReleaseComObject(factoryObj);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to enumerate DXGI adapters");
            }

            return adapters;
        }

        public static bool TryGetVramInfo(uint adapterIndex, out ulong usedBytes, out ulong totalBytes)
        {
            usedBytes = 0;
            totalBytes = 0;

            try
            {
                Guid iid = typeof(IDXGIFactory1).GUID;
                int hr = CreateDXGIFactory1(ref iid, out object factoryObj);
                if (hr != 0 || factoryObj == null) return false;

                var factory = (IDXGIFactory1)factoryObj;
                try
                {
                    if (factory.EnumAdapters1(adapterIndex, out object adapterObj) == 0 && adapterObj != null)
                    {
                        try
                        {
                            var adapter1 = (IDXGIAdapter1)adapterObj;
                            if (adapter1.GetDesc1(out DXGI_ADAPTER_DESC1 desc) == 0)
                            {
                                totalBytes = (ulong)desc.DedicatedVideoMemory;
                            }

                            if (adapterObj is IDXGIAdapter3 adapter3)
                            {
                                // DXGI_MEMORY_SEGMENT_GROUP_LOCAL = 0
                                if (adapter3.QueryVideoMemoryInfo(0, 0, out DXGI_QUERY_VIDEO_MEMORY_INFO memInfo) == 0)
                                {
                                    usedBytes = memInfo.CurrentUsage;
                                    if (totalBytes == 0) totalBytes = memInfo.Budget;
                                    return true;
                                }
                            }
                            return totalBytes > 0;
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(adapterObj);
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(factoryObj);
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, $"Failed to query VRAM for adapter {adapterIndex}");
            }

            return false;
        }

        public static bool TryGetVramInfoByVendor(uint vendorId, out ulong usedBytes, out ulong totalBytes)
        {
            usedBytes = 0;
            totalBytes = 0;

            try
            {
                Guid iid = typeof(IDXGIFactory1).GUID;
                int hr = CreateDXGIFactory1(ref iid, out object factoryObj);
                if (hr != 0 || factoryObj == null) return false;

                var factory = (IDXGIFactory1)factoryObj;
                try
                {
                    for (uint i = 0; ; i++)
                    {
                        if (factory.EnumAdapters1(i, out object adapterObj) != 0 || adapterObj == null) break;

                        try
                        {
                            var adapter1 = (IDXGIAdapter1)adapterObj;
                            if (adapter1.GetDesc1(out DXGI_ADAPTER_DESC1 desc) == 0)
                            {
                                if (desc.VendorId == vendorId)
                                {
                                    totalBytes = (ulong)desc.DedicatedVideoMemory;
                                    if (adapterObj is IDXGIAdapter3 adapter3)
                                    {
                                        if (adapter3.QueryVideoMemoryInfo(0, 0, out DXGI_QUERY_VIDEO_MEMORY_INFO memInfo) == 0)
                                        {
                                            usedBytes = memInfo.CurrentUsage;
                                            if (totalBytes == 0) totalBytes = memInfo.Budget;
                                            return true;
                                        }
                                    }
                                    return totalBytes > 0;
                                }
                            }
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(adapterObj);
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(factoryObj);
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, $"Failed to query VRAM for vendor 0x{vendorId:X4}");
            }

            return false;
        }
    }
}
