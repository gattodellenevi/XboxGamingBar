using System;

namespace XboxGamingBarHelper.Hardware.Abstractions
{
    public interface ISystemMetricsProvider : IDisposable
    {
        void Update();

        float GetCpuClock();
        float GetCpuUsage();
        float GetCpuWattage();
        float GetCpuTemperature();

        int GetCpuCoreCount();
        float GetCpuCoreUsage(int coreIndex);
        float GetCpuCoreClock(int coreIndex);

        float GetMemoryUsage();
        float GetMemoryUsed();

        float GetBatteryLevel();
        float GetBatteryRemainingTime();
        float GetBatteryDischargeRate();
        float GetBatteryChargeRate();

        string GetCpuName();
        string GetMotherboardName();
    }
}
