using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class LimitFPSProperty : WidgetToggleProperty
    {
        public LimitFPSProperty(ToggleSwitch inUI, Page inOwner) : base(false, Function.LimitFPS, inUI, inOwner)
        {
        }
    }
}
