using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Hardware
{
    internal class ActiveGpuProperty : HelperProperty<string, HardwareManager>
    {
        public ActiveGpuProperty(string inValue, HardwareManager inManager) : base(inValue, null, Function.ActiveGpu, inManager)
        {
        }
    }
}
