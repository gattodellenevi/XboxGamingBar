using System;
using System.Diagnostics;
using NLog;
using XboxGamingBarHelper.Hardware.Abstractions;
using XboxGamingBarHelper.Hardware.Dxgi;

namespace XboxGamingBarHelper.Hardware.Providers
{
    internal class WindowsFallbackGpuMetricsProvider : IGpuMetricsProvider
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly string name;
        private readonly GpuVendor vendor;
        private readonly uint vendorId;

        private float gpuUsage = -1.0f;
        private float gpuMemoryUsedMb = -1.0f;
        private float gpuMemoryTotalMb = -1.0f;

        private PerformanceCounter gpuUsageCounter;

        public string Name => name;
        public GpuVendor Vendor => vendor;
        public bool IsAvailable => true;

        public WindowsFallbackGpuMetricsProvider(string adapterName = "Windows GPU", GpuVendor vendor = GpuVendor.Intel, uint vendorId = DxgiHelper.VENDOR_INTEL)
        {
            this.name = string.IsNullOrEmpty(adapterName) ? "Windows GPU" : adapterName;
            this.vendor = vendor;
            this.vendorId = vendorId;

            InitializeGpuUsageCounter();
        }

        private void InitializeGpuUsageCounter()
        {
            try
            {
                // Try to find an active GPU 3D Engine counter
                var category = new PerformanceCounterCategory("GPU Engine");
                var instanceNames = category.GetInstanceNames();
                foreach (var instance in instanceNames)
                {
                    if (instance.IndexOf("engtype_3D", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        gpuUsageCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance);
                        gpuUsageCounter.NextValue();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Windows GPU Engine performance counter not available");
            }
        }

        public void Update()
        {
            // 1. VRAM via DXGI 1.4
            if (DxgiHelper.TryGetVramInfoByVendor(vendorId, out ulong usedBytes, out ulong totalBytes))
            {
                gpuMemoryUsedMb = usedBytes / (1024.0f * 1024.0f);
                gpuMemoryTotalMb = totalBytes / (1024.0f * 1024.0f);
            }
            else if (DxgiHelper.TryGetVramInfo(0, out usedBytes, out totalBytes))
            {
                gpuMemoryUsedMb = usedBytes / (1024.0f * 1024.0f);
                gpuMemoryTotalMb = totalBytes / (1024.0f * 1024.0f);
            }

            // 2. GPU Usage
            if (gpuUsageCounter != null)
            {
                try
                {
                    gpuUsage = gpuUsageCounter.NextValue();
                }
                catch
                {
                    gpuUsage = -1.0f;
                }
            }
        }

        public float GetGpuUsage() => gpuUsage;
        public float GetGpuWattage() => -1.0f; // Wattage not available in standard Windows metrics without kernel driver
        public float GetGpuClock() => -1.0f;
        public float GetGpuMemoryUsed() => gpuMemoryUsedMb;
        public float GetGpuMemoryTotal() => gpuMemoryTotalMb;
        public float GetGpuMemoryClock() => -1.0f;
        public float GetGpuTemperature() => -1.0f;

        public void Dispose()
        {
            gpuUsageCounter?.Dispose();
        }
    }
}
