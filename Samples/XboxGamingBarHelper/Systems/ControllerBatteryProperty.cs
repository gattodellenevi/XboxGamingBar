using Shared.Data;
using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Systems
{
    internal class ControllerBatteryProperty : HelperProperty<ControllerBattery, SystemManager>
    {
        public ControllerBatteryProperty(ControllerBattery inValue, SystemManager inManager)
            : base(inValue, null, Function.ControllerBattery, inManager)
        {
        }
    }
}
