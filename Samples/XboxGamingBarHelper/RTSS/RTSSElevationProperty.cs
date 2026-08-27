using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.RTSS
{
    internal class RTSSElevationProperty : HelperProperty<int, RTSSManager>
    {
        public RTSSElevationProperty(int inValue, RTSSManager inManager) : base(inValue, null, Function.RTSSElevation, inManager)
        {
        }
    }
}
