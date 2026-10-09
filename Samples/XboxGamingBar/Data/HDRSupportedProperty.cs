using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class HDRSupportedProperty : WidgetControlEnabledProperty<ToggleSwitch>
    {
        public HDRSupportedProperty(ToggleSwitch inUI, Page inOwner) : base(Function.HDRSupported, inUI, inOwner)
        {
        }
    }
}
