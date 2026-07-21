using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.RTSS
{
    internal class FPSLimitModeProperty : HelperProperty<int, RTSSManager>
    {
        public FPSLimitModeProperty(RTSSManager inManager) : base(0, null, Function.FPSLimitMode, inManager)
        {
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            RTSSFPSLimiter.SetFPSLimitMode(Value);
        }
    }
}
