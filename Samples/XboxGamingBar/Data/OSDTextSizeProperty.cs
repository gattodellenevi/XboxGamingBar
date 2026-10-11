using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class OSDTextSizeProperty : WidgetSliderProperty
    {
        public OSDTextSizeProperty(int inValue, Slider inControl, Page inOwner) : base(inValue, Function.OSDTextSize, inControl, inOwner)
        {
        }

        protected override void SetControlEnabled(bool isEnabled)
        {
            if (Owner is GamingWidget widget && !widget.IsRTSSRunning)
            {
                base.SetControlEnabled(false);
                return;
            }
            base.SetControlEnabled(isEnabled);
        }
    }
}
