using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NLog;
using XboxGamingBarHelper.Hardware.Abstractions;
using XboxGamingBarHelper.Windows;

namespace XboxGamingBarHelper.Hardware.Providers
{
    internal class CommonSystemMetricsProvider : ISystemMetricsProvider
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private PerformanceCounter cpuCounter;
        private PerformanceCounter cpuFreqCounter;
        private PerformanceCounter[] cpuCoreCounters;
        private PerformanceCounter[] cpuCoreFreqCounters;

        private float cpuUsage = -1.0f;
        private float cpuClock = -1.0f;
        private float[] cpuCoreUsages;
        private float[] cpuCoreClocks;
        private float maxCpuMhz = 0f;
        private int coreCount = 0;

        private float memoryUsage = -1.0f;
        private float memoryUsed = -1.0f;

        private float batteryLevel = -1.0f;
        private float batteryRemainingTime = -1.0f;
        private float batteryDischargeRate = -1.0f;
        private float batteryChargeRate = -1.0f;

        public CommonSystemMetricsProvider()
        {
            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuCounter.NextValue();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize CPU usage counter");
            }

            try
            {
                cpuFreqCounter = new PerformanceCounter("Processor Information", "% of Maximum Frequency", "_Total");
                cpuFreqCounter.NextValue();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize CPU frequency counter");
            }

            coreCount = Math.Min(Environment.ProcessorCount, 8);
            cpuCoreCounters = new PerformanceCounter[coreCount];
            cpuCoreFreqCounters = new PerformanceCounter[coreCount];
            cpuCoreUsages = new float[coreCount];
            cpuCoreClocks = new float[coreCount];

            for (int i = 0; i < coreCount; i++)
            {
                cpuCoreUsages[i] = -1.0f;
                cpuCoreClocks[i] = -1.0f;

                try
                {
                    cpuCoreCounters[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
                    cpuCoreCounters[i].NextValue();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to initialize CPU usage counter for core {i}");
                }

                try
                {
                    cpuCoreFreqCounters[i] = new PerformanceCounter("Processor Information", "% of Maximum Frequency", $"0,{i}");
                    cpuCoreFreqCounters[i].NextValue();
                }
                catch
                {
                    try
                    {
                        cpuCoreFreqCounters[i] = new PerformanceCounter("Processor Information", "% of Maximum Frequency", i.ToString());
                        cpuCoreFreqCounters[i].NextValue();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Failed to initialize CPU frequency counter for core {i}");
                    }
                }
            }
        }

        public void Update()
        {
            // Battery Update
            try
            {
                var powerStatus = SystemInformation.PowerStatus;
                batteryLevel = powerStatus.BatteryLifePercent * 100;
                batteryRemainingTime = powerStatus.BatteryLifeRemaining;

                if (TryGetBatteryState(out var battery))
                {
                    if (battery.Charging)
                    {
                        batteryDischargeRate = -1.0f;
                        batteryChargeRate = battery.Rate / 1000.0f;
                    }
                    else
                    {
                        batteryDischargeRate = battery.Rate / 1000.0f;
                        batteryChargeRate = -1.0f;
                    }
                }
            }
            catch { }

            // CPU Usage
            if (cpuCounter != null)
            {
                try
                {
                    cpuUsage = cpuCounter.NextValue();
                }
                catch { }
            }

            // CPU Clock
            if (maxCpuMhz <= 0)
            {
                maxCpuMhz = GetMaxCpuFrequency();
            }

            if (maxCpuMhz > 0 && cpuFreqCounter != null)
            {
                try
                {
                    float percent = cpuFreqCounter.NextValue();
                    cpuClock = maxCpuMhz * (percent / 100.0f);
                }
                catch
                {
                    cpuClock = -1.0f;
                }
            }
            else
            {
                cpuClock = maxCpuMhz;
            }

            // Per-Core CPU
            for (int i = 0; i < coreCount; i++)
            {
                if (cpuCoreCounters[i] != null)
                {
                    try { cpuCoreUsages[i] = cpuCoreCounters[i].NextValue(); } catch { }
                }

                if (maxCpuMhz > 0 && cpuCoreFreqCounters[i] != null)
                {
                    try
                    {
                        float percent = cpuCoreFreqCounters[i].NextValue();
                        cpuCoreClocks[i] = maxCpuMhz * (percent / 100.0f);
                    }
                    catch { cpuCoreClocks[i] = maxCpuMhz; }
                }
                else
                {
                    cpuCoreClocks[i] = maxCpuMhz;
                }
            }

            // Memory Status
            try
            {
                var memStatus = new Kernel32.MEMORYSTATUSEX();
                if (Kernel32.GlobalMemoryStatusEx(memStatus))
                {
                    memoryUsage = memStatus.dwMemoryLoad;
                    double usedBytes = (double)(memStatus.ullTotalPhys - memStatus.ullAvailPhys);
                    memoryUsed = (float)(usedBytes / (1024.0 * 1024.0 * 1024.0));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get memory status");
            }
        }

        private float GetMaxCpuFrequency()
        {
            try
            {
                int processorCount = Environment.ProcessorCount;
                int size = Marshal.SizeOf(typeof(PROCESSOR_POWER_INFORMATION)) * processorCount;
                IntPtr buffer = Marshal.AllocHGlobal(size);

                try
                {
                    uint ret = PowrProf.CallNtPowerInformation(
                        (int)POWER_INFORMATION_LEVEL.ProcessorInformation,
                        IntPtr.Zero,
                        0,
                        buffer,
                        size);

                    if (ret == 0)
                    {
                        float maxMhz = 0;
                        long stride = Marshal.SizeOf(typeof(PROCESSOR_POWER_INFORMATION));
                        for (int i = 0; i < processorCount; i++)
                        {
                            IntPtr ptr = (IntPtr)((long)buffer + (i * stride));
                            var info = (PROCESSOR_POWER_INFORMATION)Marshal.PtrToStructure(ptr, typeof(PROCESSOR_POWER_INFORMATION));
                            if (info.MaxMhz > maxMhz) maxMhz = info.MaxMhz;
                        }
                        return maxMhz;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get CPU max frequency");
            }

            return -1.0f;
        }

        private static bool TryGetBatteryState(out SYSTEM_BATTERY_STATE state)
        {
            const int BUFFER_SIZE = 128;
            IntPtr buffer = Marshal.AllocHGlobal(BUFFER_SIZE);
            try
            {
                uint status = PowrProf.CallNtPowerInformation(
                    5, // SystemBatteryState
                    IntPtr.Zero,
                    0,
                    buffer,
                    BUFFER_SIZE);

                if (status != 0)
                {
                    state = default;
                    return false;
                }

                state = Marshal.PtrToStructure<SYSTEM_BATTERY_STATE>(buffer);
                return state.BatteryPresent;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public float GetCpuClock() => cpuClock;
        public float GetCpuUsage() => cpuUsage;
        public float GetCpuWattage() => -1.0f;
        public float GetCpuTemperature() => -1.0f;

        public int GetCpuCoreCount() => coreCount;
        public float GetCpuCoreUsage(int coreIndex) => (coreIndex >= 0 && coreIndex < coreCount) ? cpuCoreUsages[coreIndex] : -1.0f;
        public float GetCpuCoreClock(int coreIndex) => (coreIndex >= 0 && coreIndex < coreCount) ? cpuCoreClocks[coreIndex] : -1.0f;

        public float GetMemoryUsage() => memoryUsage;
        public float GetMemoryUsed() => memoryUsed;

        public float GetBatteryLevel() => batteryLevel;
        public float GetBatteryRemainingTime() => batteryRemainingTime;
        public float GetBatteryDischargeRate() => batteryDischargeRate;
        public float GetBatteryChargeRate() => batteryChargeRate;

        public string GetCpuName() => "Unknown CPU";
        public string GetMotherboardName() => string.Empty;

        public void Dispose()
        {
            cpuCounter?.Dispose();
            cpuFreqCounter?.Dispose();
            if (cpuCoreCounters != null)
            {
                foreach (var c in cpuCoreCounters) c?.Dispose();
            }
            if (cpuCoreFreqCounters != null)
            {
                foreach (var c in cpuCoreFreqCounters) c?.Dispose();
            }
        }
    }
}
