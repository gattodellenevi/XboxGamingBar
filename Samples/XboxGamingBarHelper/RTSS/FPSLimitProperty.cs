using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.RTSS
{
    internal class FPSLimitProperty : HelperProperty<int, RTSSManager>
    {
        public FPSLimitProperty(RTSSManager inManager) : base(0, null, Function.FPSLimit, inManager)
        {
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            if (Manager.LimitFPS)
            {
                RTSSFPSLimiter.SetFPSLimit(Value);
                RTSSFPSLimiter.SetFPSLimitMode(Manager.FPSLimitMode.Value);
            }
        }
    }
}
