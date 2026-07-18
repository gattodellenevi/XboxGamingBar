using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class FPSLimitProperty : WidgetSliderProperty
    {
        public FPSLimitProperty(int inValue, Slider inControl, Page inOwner) : base(inValue, Function.FPSLimit, inControl, inOwner)
        {
        }
    }
}
