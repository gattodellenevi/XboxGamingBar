using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class OSDTextSizeProperty : WidgetSliderProperty
    {
        public OSDTextSizeProperty(int inValue, Slider inControl, Page inOwner) : base(inValue, Function.OSDTextSize, inControl, inOwner)
        {
        }
    }
}
