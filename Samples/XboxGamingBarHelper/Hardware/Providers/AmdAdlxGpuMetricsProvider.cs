using System;
using NLog;
using XboxGamingBarHelper.AMD;
using XboxGamingBarHelper.Hardware.Abstractions;
using XboxGamingBarHelper.Hardware.Dxgi;

namespace XboxGamingBarHelper.Hardware.Providers
{
    internal class AmdAdlxGpuMetricsProvider : IGpuMetricsProvider
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly string name;
        private readonly bool isIntegrated;

        private float gpuUsage = -1.0f;
        private float gpuWattage = -1.0f;
        private float gpuClock = -1.0f;
        private float gpuMemoryUsedMb = -1.0f;
        private float gpuMemoryTotalMb = -1.0f;
        private float gpuMemoryClock = -1.0f;
        private float gpuTemperature = -1.0f;

        public string Name => name;
        public GpuVendor Vendor => GpuVendor.Amd;

        public AmdAdlxGpuMetricsProvider(string name = "AMD ADLX", bool isIntegrated = true)
        {
            this.name = string.IsNullOrEmpty(name) ? "AMD ADLX" : name;
            this.isIntegrated = isIntegrated;
        }

        public bool IsAvailable
        {
            get
            {
                var mgr = AMDManager.Instance;
                if (mgr == null) return false;
                return mgr.AMDSettingsSupported?.Value ?? false;
            }
        }

        public void Update()
        {
            try
            {
                if (AMDManager.Instance != null && AMDManager.Instance.QueryCurrentGpuMetrics(out var metrics, isIntegrated))
                {
                    if (metrics.Usage >= 0) gpuUsage = metrics.Usage;
                    if (metrics.Clock >= 0) gpuClock = metrics.Clock;
                    if (metrics.Wattage >= 0) gpuWattage = metrics.Wattage;
                    if (metrics.Temperature >= 0) gpuTemperature = metrics.Temperature;
                    if (metrics.MemoryUsed >= 0) gpuMemoryUsedMb = metrics.MemoryUsed;
                    if (metrics.MemoryTotal >= 0) gpuMemoryTotalMb = metrics.MemoryTotal;
                    if (metrics.MemoryClock >= 0) gpuMemoryClock = metrics.MemoryClock;
                }

                // If VRAM total or used wasn't reported by ADLX, supplement with DXGI 1.4
                if (gpuMemoryTotalMb <= 0 || gpuMemoryUsedMb < 0)
                {
                    if (DxgiHelper.TryGetVramInfoByVendor(DxgiHelper.VENDOR_AMD, out ulong usedBytes, out ulong totalBytes))
                    {
                        if (usedBytes > 0) gpuMemoryUsedMb = usedBytes / (1024.0f * 1024.0f);
                        if (totalBytes > 0) gpuMemoryTotalMb = totalBytes / (1024.0f * 1024.0f);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to update AMD ADLX metrics");
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
        }
    }
}
