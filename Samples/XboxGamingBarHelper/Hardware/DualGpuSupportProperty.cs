using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Hardware
{
    internal class DualGpuSupportProperty : HelperProperty<string, HardwareManager>
    {
        public DualGpuSupportProperty(string inValue, HardwareManager inManager) : base(inValue, null, Function.Support_DualGpu, inManager)
        {
        }
    }
}
