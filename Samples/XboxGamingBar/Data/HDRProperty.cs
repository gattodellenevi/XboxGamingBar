using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class HDRProperty : WidgetToggleProperty
    {
        public HDRProperty(ToggleSwitch inUI, Page inOwner) : base(false, Function.HDR, inUI, inOwner)
        {
        }
    }
}
