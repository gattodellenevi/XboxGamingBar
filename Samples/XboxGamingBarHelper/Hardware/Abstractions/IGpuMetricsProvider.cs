using System;

namespace XboxGamingBarHelper.Hardware.Abstractions
{
    public interface IGpuMetricsProvider : IDisposable
    {
        string Name { get; }
        GpuVendor Vendor { get; }
        bool IsAvailable { get; }

        void Update();

        float GetGpuUsage();
        float GetGpuWattage();
        float GetGpuClock();
        float GetGpuMemoryUsed();
        float GetGpuMemoryTotal();
        float GetGpuMemoryClock();
        float GetGpuTemperature();
    }
}
