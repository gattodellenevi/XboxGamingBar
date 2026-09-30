using System.Collections.Generic;
using System.Drawing;
using XboxGamingBarHelper.Hardware;

namespace XboxGamingBarHelper.RTSS.OSDItems
{
    internal class OSDItemCPU : OSDItem
    {
        private HardwareSensor cpuUsageSensor;
        private HardwareSensor cpuClockSensor;
        private HardwareSensor cpuWattageSensor;
        private HardwareSensor cpuTemperatureSensor;

        public OSDItemCPU(HardwareSensor cpuUsageSensor, HardwareSensor cpuClockSensor, HardwareSensor cpuWattageSensor, HardwareSensor cpuTemperatureSensor) : base("CPU", Color.Turquoise)
        {
            this.cpuWattageSensor = cpuWattageSensor;
            this.cpuUsageSensor = cpuUsageSensor;
            this.cpuClockSensor = cpuClockSensor;
            this.cpuTemperatureSensor = cpuTemperatureSensor;
        }

        protected override List<OSDItemValue> GetValues(int osdLevel)
        {
            var osdItems = base.GetValues(osdLevel);

            // In DETAIL mode (level 2), show CPU load (percentage).
            if (osdLevel >= 2)
            {
                osdItems.Add(new OSDItemValue(cpuUsageSensor.Value, "%"));
            }

            // In FULL / ALL mode (level 3+), show wattage, temperature, and clock speed.
            if (osdLevel >= 3)
            {
                osdItems.Add(new OSDItemValue(cpuWattageSensor.Value, "W"));
                osdItems.Add(new OSDItemValue(cpuTemperatureSensor.Value, "°C"));
                osdItems.Add(new OSDItemValue(cpuClockSensor.Value, "MHz"));
            }

            return osdItems;
        }
    }
}
