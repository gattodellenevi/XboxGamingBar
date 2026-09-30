using System.Collections.Generic;
using System.Drawing;
using XboxGamingBarHelper.Hardware;

namespace XboxGamingBarHelper.RTSS.OSDItems
{
    internal class OSDItemGPUWattage : OSDItem
    {
        private readonly HardwareSensor gpuWattageSensor;

        public OSDItemGPUWattage(HardwareSensor gpuWattageSensor) : base("GPU", Color.LawnGreen)
        {
            this.gpuWattageSensor = gpuWattageSensor;
        }

        protected override List<OSDItemValue> GetValues(int osdLevel)
        {
            var osdItems = base.GetValues(osdLevel);

            // In DETAIL mode (level 2), GPU wattage is shown as its own element after VRAM.
            // In FULL / ALL mode (level 3+), wattage is already included in the multi-line OSDItemGPU.
            if (osdLevel == 2)
            {
                osdItems.Add(new OSDItemValue(gpuWattageSensor.Value, "W"));
            }

            return osdItems;
        }
    }
}
