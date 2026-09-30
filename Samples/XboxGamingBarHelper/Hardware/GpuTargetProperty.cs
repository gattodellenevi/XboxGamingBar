using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Hardware
{
    internal class GpuTargetProperty : HelperProperty<int, HardwareManager>
    {
        public GpuTargetProperty(int inValue, HardwareManager inManager) : base(inValue, null, Function.Settings_GpuTarget, inManager)
        {
        }
    }
}
