using System.Collections.Generic;
using System.Drawing;
using XboxGamingBarHelper.Hardware;

namespace XboxGamingBarHelper.RTSS.OSDItems
{
    internal class OSDItemCPUPerCore : OSDItem
    {
        private readonly int coreIndex;
        private HardwareSensor cpuCoreUsageSensor;
        private HardwareSensor cpuCoreClockSensor;

        public OSDItemCPUPerCore(int coreIndex, HardwareSensor cpuCoreUsageSensor, HardwareSensor cpuCoreClockSensor) : base("CPU", Color.Turquoise)
        {
            this.coreIndex = coreIndex;
            this.cpuCoreUsageSensor = cpuCoreUsageSensor;
            this.cpuCoreClockSensor = cpuCoreClockSensor;
        }

        protected override string GetNameString(IColorFormatter formatter = null)
        {
            formatter = formatter ?? SDRColorFormatter.Instance;
            return $"<C={formatter.Format(baseColor)}>CPU<S={RTSSManager.SubscriptFontScale}>{coreIndex}<S={RTSSManager.CurrentFontScale}><C>";
        }

        protected override List<OSDItemValue> GetValues(int osdLevel)
        {
            var osdItems = new List<OSDItemValue>();

            if (osdLevel < 4)
            {
                return osdItems;
            }

            osdItems.Add(new OSDItemValue(cpuCoreUsageSensor.Value, "%"));
            osdItems.Add(new OSDItemValue(cpuCoreClockSensor.Value, "MHz"));

            return osdItems;
        }
    }
}
