using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class FPSLimitProperty : WidgetSliderProperty
    {
        public FPSLimitProperty(int inValue, Slider inControl, Page inOwner) : base(inValue, Function.FPSLimit, inControl, inOwner)
        {
        }

        protected override void SetControlEnabled(bool isEnabled)
        {
            if (Owner is GamingWidget widget && !widget.IsRTSSRunning)
            {
                base.SetControlEnabled(false);
                return;
            }
            if (Owner is GamingWidget widget2 && widget2.IsElevationMismatch)
            {
                return;
            }
            base.SetControlEnabled(isEnabled);
        }

        protected override bool ShouldSendNotifyMessage()
        {
            if (Owner is GamingWidget widget && widget.IsElevationMismatch)
            {
                return false;
            }
            return base.ShouldSendNotifyMessage();
        }
    }
}
