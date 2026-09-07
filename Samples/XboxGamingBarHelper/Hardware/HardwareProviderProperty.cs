using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Hardware
{
    internal class HardwareProviderProperty : HelperProperty<string, HardwareManager>
    {
        public HardwareProviderProperty(string inValue, HardwareManager inManager) : base(inValue, null, Function.HardwareProvider, inManager)
        {
        }
    }
}
