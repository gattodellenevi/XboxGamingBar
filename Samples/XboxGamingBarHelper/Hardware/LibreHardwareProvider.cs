#if !STORE
using LibreHardwareMonitor.Hardware;
using NLog;
using System;
using System.Management;

namespace XboxGamingBarHelper.Hardware
{
    internal class LibreHardwareProvider : IHardwareProvider
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Computer computer;
        private readonly IVisitor updateVisitor;
        private string cpuName = string.Empty;
        private string motherboardName = string.Empty;
        private int cpuCoreCount = 0;

        // CPU Sensors
        private ISensor cpuUsageSensor;
        private ISensor cpuWattageSensor;
        private ISensor cpuTemperatureSensor;
        private ISensor[] cpuCoreUsageSensors = Array.Empty<ISensor>();
        private ISensor[] cpuCoreClockSensors = Array.Empty<ISensor>();

        // GPU Sensors (AMD, Nvidia, Intel)
        private ISensor gpuClockSensor;
        private ISensor gpuMemoryUsedSensor;
        private ISensor gpuMemoryTotalSensor;
        private ISensor gpuMemoryClockSensor;
        private ISensor gpuUsageSensor;
        private ISensor gpuWattageSensor;
        private ISensor gpuTemperatureSensor;

        // Memory Sensors
        private ISensor memoryUsageSensor;
        private ISensor memoryUsedSensor;

        // Battery Sensors
        private ISensor batteryLevelSensor;
        private ISensor batteryRemainingTimeSensor;
        private ISensor batteryDischargeRateSensor;
        private ISensor batteryChargeRateSensor;

        public LibreHardwareProvider()
        {
            motherboardName = GetMotherboardNameFromWmi();
            Logger.Info($"Detected Motherboard via WMI: \"{motherboardName}\"");

            computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = false,
                IsControllerEnabled = false,
                IsNetworkEnabled = false,
                IsStorageEnabled = false,
                IsBatteryEnabled = true,
            };
            updateVisitor = new UpdateVisitor();
            computer.Open();

            foreach (IHardware hardware in computer.Hardware)
            {
                var properties = string.Empty;
                if (hardware.Properties.Count > 0)
                {
                    foreach (var property in hardware.Properties)
                    {
                        properties = properties.Length == 0 ? $"{property.Key}:{property.Value}" : $"{properties}, {property.Key}:{property.Value}";
                    }
                }

                Logger.Info($"Found hardware {hardware.HardwareType}: Name={hardware.Name}, Type={hardware.HardwareType}, Id={hardware.Identifier}, Properties={properties}");

                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    cpuName = hardware.Name;

                    // Detect core count (max 8)
                    int maxCores = 0;
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Clock && sensor.Name.StartsWith("Core #"))
                        {
                            if (int.TryParse(sensor.Name.Replace("Core #", ""), out int coreNum))
                            {
                                if (coreNum > maxCores) maxCores = coreNum;
                            }
                        }
                    }
                    cpuCoreCount = Math.Min(maxCores, 8);
                    cpuCoreUsageSensors = new ISensor[cpuCoreCount];
                    cpuCoreClockSensors = new ISensor[cpuCoreCount];

                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load && sensor.Name == "CPU Total")
                            cpuUsageSensor = sensor;
                        else if (sensor.SensorType == SensorType.Power && (sensor.Name == "Package" || sensor.Name == "CPU Package" || sensor.Name == "Total"))
                            cpuWattageSensor = sensor;
                        else if (sensor.SensorType == SensorType.Temperature && (sensor.Name == "Core (Tctl/Tdie)" || sensor.Name == "CPU Package" || sensor.Name == "Core Average"))
                            cpuTemperatureSensor = sensor;

                        if (sensor.SensorType == SensorType.Load && sensor.Name.StartsWith("CPU Core #"))
                        {
                            if (int.TryParse(sensor.Name.Replace("CPU Core #", ""), out int coreIdx) && coreIdx >= 1 && coreIdx <= cpuCoreCount)
                            {
                                cpuCoreUsageSensors[coreIdx - 1] = sensor;
                            }
                        }
                        else if (sensor.SensorType == SensorType.Clock && sensor.Name.StartsWith("Core #"))
                        {
                            if (int.TryParse(sensor.Name.Replace("Core #", ""), out int coreIdx) && coreIdx >= 1 && coreIdx <= cpuCoreCount)
                            {
                                cpuCoreClockSensors[coreIdx - 1] = sensor;
                            }
                        }
                    }

                    // Fallback temperature sensor if specific name not matched
                    if (cpuTemperatureSensor == null)
                    {
                        foreach (var sensor in hardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Temperature)
                            {
                                cpuTemperatureSensor = sensor;
                                break;
                            }
                        }
                    }
                }
                else if (hardware.HardwareType == HardwareType.GpuAmd || hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuIntel)
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Clock && (sensor.Name == "GPU Core" || (gpuClockSensor == null && sensor.Name.Contains("Core"))))
                            gpuClockSensor = sensor;
                        else if (sensor.SensorType == SensorType.SmallData && sensor.Name == "GPU Memory Used")
                            gpuMemoryUsedSensor = sensor;
                        else if (sensor.SensorType == SensorType.SmallData && sensor.Name == "GPU Memory Total")
                            gpuMemoryTotalSensor = sensor;
                        else if (sensor.SensorType == SensorType.Clock && (sensor.Name == "GPU Memory" || (gpuMemoryClockSensor == null && sensor.Name.Contains("Memory"))))
                            gpuMemoryClockSensor = sensor;
                        else if (sensor.SensorType == SensorType.Load && (sensor.Name == "GPU Core" || (gpuUsageSensor == null && sensor.Name.Contains("Core"))))
                            gpuUsageSensor = sensor;
                        else if (sensor.SensorType == SensorType.Power && (sensor.Name == "GPU Core" || sensor.Name == "GPU Package" || (gpuWattageSensor == null && sensor.Name.Contains("GPU"))))
                            gpuWattageSensor = sensor;
                        else if (sensor.SensorType == SensorType.Temperature && (sensor.Name == "GPU VR SoC" || sensor.Name == "GPU Core" || (gpuTemperatureSensor == null && sensor.Name.Contains("GPU"))))
                            gpuTemperatureSensor = sensor;
                    }
                }
                else if (hardware.HardwareType == HardwareType.Memory)
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load && sensor.Name == "Memory")
                            memoryUsageSensor = sensor;
                        else if (sensor.SensorType == SensorType.Data && sensor.Name == "Memory Used")
                            memoryUsedSensor = sensor;
                    }
                }
                else if (hardware.HardwareType == HardwareType.Battery)
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Level && sensor.Name == "Charge Level")
                            batteryLevelSensor = sensor;
                        else if (sensor.SensorType == SensorType.TimeSpan && sensor.Name == "Remaining Time (Estimated)")
                            batteryRemainingTimeSensor = sensor;
                        else if (sensor.SensorType == SensorType.Power && sensor.Name == "Discharge Rate")
                            batteryDischargeRateSensor = sensor;
                        else if (sensor.SensorType == SensorType.Power && sensor.Name == "Charge Rate")
                            batteryChargeRateSensor = sensor;
                    }
                }
            }
        }

        private static string GetMotherboardNameFromWmi()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Product FROM Win32_BaseBoard"))
                {
                    foreach (var item in searcher.Get())
                    {
                        var prod = item["Product"]?.ToString();
                        if (!string.IsNullOrEmpty(prod))
                        {
                            return prod;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to get motherboard name from WMI.");
            }
            return string.Empty;
        }

        public void Update()
        {
            computer.Accept(updateVisitor);
        }

        public float GetCpuClock()
        {
            if (cpuCoreClockSensors == null || cpuCoreClockSensors.Length == 0) return -1.0f;
            float totalClock = 0;
            int count = 0;
            for (int i = 0; i < cpuCoreClockSensors.Length; i++)
            {
                var sensor = cpuCoreClockSensors[i];
                if (sensor?.Value.HasValue == true)
                {
                    totalClock += sensor.Value.Value;
                    count++;
                }
            }
            return count > 0 ? totalClock / count : -1.0f;
        }

        public float GetCpuUsage() => cpuUsageSensor?.Value ?? -1.0f;
        public float GetCpuWattage() => cpuWattageSensor?.Value ?? -1.0f;
        public float GetCpuTemperature() => cpuTemperatureSensor?.Value ?? -1.0f;

        public int GetCpuCoreCount() => cpuCoreCount;
        public float GetCpuCoreUsage(int coreIndex) => (coreIndex >= 0 && coreIndex < cpuCoreUsageSensors.Length) ? (cpuCoreUsageSensors[coreIndex]?.Value ?? -1.0f) : -1.0f;
        public float GetCpuCoreClock(int coreIndex) => (coreIndex >= 0 && coreIndex < cpuCoreClockSensors.Length) ? (cpuCoreClockSensors[coreIndex]?.Value ?? -1.0f) : -1.0f;

        public float GetGpuClock() => gpuClockSensor?.Value ?? -1.0f;
        public float GetGpuMemoryUsed() => gpuMemoryUsedSensor?.Value ?? -1.0f;
        public float GetGpuMemoryTotal() => gpuMemoryTotalSensor?.Value ?? -1.0f;
        public float GetGpuMemoryClock() => gpuMemoryClockSensor?.Value ?? -1.0f;
        public float GetGpuUsage() => gpuUsageSensor?.Value ?? -1.0f;
        public float GetGpuWattage() => gpuWattageSensor?.Value ?? -1.0f;
        public float GetGpuTemperature() => gpuTemperatureSensor?.Value ?? -1.0f;

        public float GetMemoryUsage() => memoryUsageSensor?.Value ?? -1.0f;
        public float GetMemoryUsed() => memoryUsedSensor?.Value ?? -1.0f;

        public float GetBatteryLevel() => batteryLevelSensor?.Value ?? -1.0f;
        public float GetBatteryRemainingTime() => batteryRemainingTimeSensor?.Value ?? -1.0f;
        public float GetBatteryDischargeRate() => batteryDischargeRateSensor?.Value ?? -1.0f;
        public float GetBatteryChargeRate() => batteryChargeRateSensor?.Value ?? -1.0f;

        public string GetCpuName() => cpuName;
        public string GetMotherboardName() => motherboardName;
    }
}
#endif
