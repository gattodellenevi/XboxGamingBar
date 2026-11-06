using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class CursorSizeProperty : WidgetSliderProperty
    {
        public CursorSizeProperty(int inValue, Slider inControl, Page inOwner) : base(inValue, Function.CursorSize, inControl, inOwner)
        {
        }
    }
}
