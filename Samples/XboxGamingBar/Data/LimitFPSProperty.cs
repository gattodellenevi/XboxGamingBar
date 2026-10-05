using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class LimitFPSProperty : WidgetToggleProperty
    {
        public LimitFPSProperty(ToggleSwitch inUI, Page inOwner) : base(false, Function.LimitFPS, inUI, inOwner)
        {
        }

        protected override void SetControlEnabled(bool isEnabled)
        {
            if (Owner is GamingWidget widget && widget.IsElevationMismatch)
            {
                base.SetControlEnabled(false);
                return;
            }
            base.SetControlEnabled(isEnabled);
        }
    }
}
