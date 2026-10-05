using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class MouseScrollSpeedProperty : WidgetSliderProperty
    {
        public MouseScrollSpeedProperty(int inValue, Slider inControl, Page inOwner) : base(inValue, Function.MouseScrollSpeed, inControl, inOwner)
        {
        }
    }
}
