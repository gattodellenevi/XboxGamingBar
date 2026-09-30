using System.Collections.Generic;
using System.Drawing;
using XboxGamingBarHelper.Hardware;

namespace XboxGamingBarHelper.RTSS.OSDItems
{
    internal class OSDItemGPU : OSDItem
    {
        private HardwareSensor gpuUsageSensor;
        private HardwareSensor gpuClockSensor;
        private HardwareSensor gpuWattageSensor;
        private HardwareSensor gpuTemperatureSensor;

        public OSDItemGPU(HardwareSensor gpuUsageSensor, HardwareSensor gpuClockSensor, HardwareSensor gpuWattageSensor, HardwareSensor gpuTemperatureSensor) : base("GPU", Color.LawnGreen)
        {
            this.gpuWattageSensor = gpuWattageSensor;
            this.gpuUsageSensor = gpuUsageSensor;
            this.gpuClockSensor = gpuClockSensor;
            this.gpuTemperatureSensor = gpuTemperatureSensor;
        }

        protected override List<OSDItemValue> GetValues(int osdLevel)
        {
            var osdItems = base.GetValues(osdLevel);

            // In DETAIL mode (level 2), show GPU load (percentage).
            if (osdLevel >= 2)
            {
                osdItems.Add(new OSDItemValue(gpuUsageSensor.Value, "%"));
            }

            // In FULL / ALL mode (level 3+), show wattage, temperature, and clock speed.
            if (osdLevel >= 3)
            {
                osdItems.Add(new OSDItemValue(gpuWattageSensor.Value, "W"));
                osdItems.Add(new OSDItemValue(gpuTemperatureSensor.Value, "°C"));
                osdItems.Add(new OSDItemValue(gpuClockSensor.Value, "MHz"));
            }

            return osdItems;
        }
    }
}
