
using NLog;
using System;
//using System.Collections;
using System.Collections.Generic;
using Windows.ApplicationModel.AppService;
using XboxGamingBarHelper.Core;
using XboxGamingBarHelper.Hardware.Devices;
using XboxGamingBarHelper.Hardware.Sensors;

namespace XboxGamingBarHelper.Hardware
{
   

    internal class HardwareManager : Manager
    {
        private readonly CPU cpu;
        private readonly Device device;

        private readonly IHardwareProvider hardwareProvider;

        public HardwareProviderProperty HardwareProvider { get; }

        public CPUUsageSensor CPUUsage { get; }
        public CPUClockSensor CPUClock { get; }
        public CPUWattageSensor CPUWattage { get; }
        public CPUTemperatureSensor CPUTemperature { get; }
        public CPUCoreUsageSensor[] CPUCoreUsages { get; }
        public CPUCoreClockSensor[] CPUCoreClocks { get; }

        public GPUUsageSensor GPUUsage { get; }
        public GPUClockSensor GPUClock { get; }
        public GPUMemoryUsedSensor GPUMemoryUsed { get; }
        public GPUMemoryTotalSensor GPUMemoryTotal { get; }
        public GPUMemoryClockSensor GPUMemoryClock { get; }
        public GPUWattageSensor GPUWattage { get; }
        public GPUTemperatureSensor GPUTemperature { get; }

        public MemoryUsageSensor MemoryUsage { get; }
        public MemoryUsedSensor MemoryUsed { get; }

        public BatteryLevelSensor BatteryLevel { get; }
        public BatteryRemainingTimeSensor BatteryRemainingTime { get; }
        public BatteryDischargeRateSensor BatteryDischargeRate { get; }
        public BatteryChargeRateSensor BatteryChargeRate { get; }

        private readonly List<HardwareSensor> hardwareSensors;

        internal HardwareManager(AppServiceConnection connection) : base(connection)
        {
#if STORE
            hardwareProvider = new WindowsHardwareProvider();
#else
            hardwareProvider = new LibreHardwareProvider();
#endif
            HardwareProvider = new HardwareProviderProperty(hardwareProvider.ProviderName, this);

            var cpuId = hardwareProvider.GetCpuName();
            var mainboardId = hardwareProvider.GetMotherboardName();

            cpu = CPUFactory.Create(cpuId);
            Logger.Info($"Initialized CPU: {cpu.Name} (\"{cpuId}\")");
            device = DeviceFactory.Create(mainboardId, cpu);
            Logger.Info($"Initialized Device: {device.Name} (\"{mainboardId}\")");

            // Initialize hardware sensors
            CPUClock = new CPUClockSensor();
            CPUUsage = new CPUUsageSensor();
            CPUWattage = new CPUWattageSensor();
            CPUTemperature = new CPUTemperatureSensor();

            int coreCount = hardwareProvider.GetCpuCoreCount();
            CPUCoreUsages = new CPUCoreUsageSensor[coreCount];
            CPUCoreClocks = new CPUCoreClockSensor[coreCount];
            for (int i = 0; i < coreCount; i++)
            {
                CPUCoreUsages[i] = new CPUCoreUsageSensor(i);
                CPUCoreClocks[i] = new CPUCoreClockSensor(i);
            }

            GPUUsage = new GPUUsageSensor();
            GPUClock = new GPUClockSensor();
            GPUMemoryUsed = new GPUMemoryUsedSensor();
            GPUMemoryTotal = new GPUMemoryTotalSensor();
            GPUMemoryClock = new GPUMemoryClockSensor();
            GPUTemperature = new GPUTemperatureSensor();
            GPUWattage = new GPUWattageSensor();
            MemoryUsage = new MemoryUsageSensor();
            MemoryUsed = new MemoryUsedSensor();
            BatteryLevel = new BatteryLevelSensor();
            BatteryRemainingTime = new BatteryRemainingTimeSensor();
            BatteryDischargeRate = new BatteryDischargeRateSensor();
            BatteryChargeRate = new BatteryChargeRateSensor();
            hardwareSensors = new List<HardwareSensor>()
            {
                CPUClock,
                CPUUsage,
                CPUWattage,
                CPUTemperature,
                GPUUsage,
                GPUClock,
                GPUMemoryUsed,
                GPUMemoryTotal,
                GPUMemoryClock,
                GPUTemperature,
                GPUWattage,
                MemoryUsage,
                MemoryUsed,
                BatteryLevel,
                BatteryRemainingTime,
                BatteryDischargeRate,
                BatteryChargeRate,
            };

            foreach (var sensor in CPUCoreUsages) hardwareSensors.Add(sensor);
            foreach (var sensor in CPUCoreClocks) hardwareSensors.Add(sensor);
        }

        public override void Update()
        {
            base.Update();

            hardwareProvider.Update();

            CPUClock.Value = hardwareProvider.GetCpuClock();
            CPUUsage.Value = hardwareProvider.GetCpuUsage();
            CPUWattage.Value = hardwareProvider.GetCpuWattage();
            CPUTemperature.Value = hardwareProvider.GetCpuTemperature();

            for (int i = 0; i < CPUCoreUsages.Length; i++)
            {
                CPUCoreUsages[i].Value = hardwareProvider.GetCpuCoreUsage(i);
                CPUCoreClocks[i].Value = hardwareProvider.GetCpuCoreClock(i);
            }

            GPUClock.Value = hardwareProvider.GetGpuClock();
            GPUMemoryUsed.Value = hardwareProvider.GetGpuMemoryUsed();
            GPUMemoryTotal.Value = hardwareProvider.GetGpuMemoryTotal();
            GPUMemoryClock.Value = hardwareProvider.GetGpuMemoryClock();
            GPUUsage.Value = hardwareProvider.GetGpuUsage();
            GPUWattage.Value = hardwareProvider.GetGpuWattage();
            GPUTemperature.Value = hardwareProvider.GetGpuTemperature();

            MemoryUsage.Value = hardwareProvider.GetMemoryUsage();
            MemoryUsed.Value = hardwareProvider.GetMemoryUsed();

            BatteryLevel.Value = hardwareProvider.GetBatteryLevel();
            BatteryRemainingTime.Value = hardwareProvider.GetBatteryRemainingTime();
            BatteryDischargeRate.Value = hardwareProvider.GetBatteryDischargeRate();
            BatteryChargeRate.Value = hardwareProvider.GetBatteryChargeRate();
        }
    }
}
