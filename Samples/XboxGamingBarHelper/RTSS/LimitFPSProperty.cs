using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.RTSS
{
    internal class LimitFPSProperty : HelperProperty<bool, RTSSManager>
    {
        public LimitFPSProperty(bool inValue, RTSSManager inManager) : base(inValue, null, Function.LimitFPS, inManager)
        {
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            Logger.Info($"{(Value ? "Enable" : "Disable")} FPS limit.");
            var fpsLimit = Value ? Manager.FPSLimit.Value : 0;
            RTSSFPSLimiter.SetFPSLimit(fpsLimit);
            if (Value)
            {
                RTSSFPSLimiter.SetFPSLimitMode(Manager.FPSLimitMode.Value);
            }
        }
    }
}
