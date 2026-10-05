using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class MouseSpeedProperty : WidgetSliderProperty
    {
        public MouseSpeedProperty(int inValue, Slider inControl, Page inOwner) : base(inValue, Function.MouseSpeed, inControl, inOwner)
        {
        }
    }
}
