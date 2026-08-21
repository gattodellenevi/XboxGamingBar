using System.Collections.Generic;
using System.Drawing;
using XboxGamingBarHelper.Hardware;

namespace XboxGamingBarHelper.RTSS.OSDItems
{
    internal class OSDItemBattery : OSDItem
    {
        private HardwareSensor batteryDischargeRateSensor;
        private HardwareSensor batteryChargeRateSensor;

        public OSDItemBattery(HardwareSensor batteryDischargeRateSensor, HardwareSensor batteryChargeRateSensor) : base("BATT", Color.DarkSalmon)
        {
            this.batteryDischargeRateSensor = batteryDischargeRateSensor;
            this.batteryChargeRateSensor = batteryChargeRateSensor;
        }

        protected override List<OSDItemValue> GetValues(int osdLevel)
        {
            var osdItems = base.GetValues(osdLevel);

            if (osdLevel >= 2)
            {
                if (batteryDischargeRateSensor.Value > 0)
                {
                    osdItems.Add(new OSDItemValue(batteryDischargeRateSensor.Value, "W/H", "-"));
                }

                if (batteryChargeRateSensor.Value > 0)
                {
                    osdItems.Add(new OSDItemValue(batteryChargeRateSensor.Value, "W/H", "+"));
                }
            }

            return osdItems;
        }
    }
}
