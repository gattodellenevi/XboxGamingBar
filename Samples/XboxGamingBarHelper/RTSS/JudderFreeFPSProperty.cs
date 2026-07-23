using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.RTSS
{
    internal class JudderFreeFPSProperty : HelperProperty<bool, RTSSManager>
    {
        public JudderFreeFPSProperty(RTSSManager inManager) : base(true, null, Function.JudderFreeFPS, inManager)
        {
        }
    }
}
