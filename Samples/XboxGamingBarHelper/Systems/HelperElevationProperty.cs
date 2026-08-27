using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Systems
{
    internal class HelperElevationProperty : HelperProperty<bool, SystemManager>
    {
        public HelperElevationProperty(bool inValue, SystemManager inManager) : base(inValue, null, Function.HelperElevation, inManager)
        {
        }
    }
}
