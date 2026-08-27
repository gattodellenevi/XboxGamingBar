using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class OnScreenDisplayProviderInstalledProperty : WidgetControlEnabledProperty<Slider>
    {
        public OnScreenDisplayProviderInstalledProperty(Slider inUI, Page inOwner) : base(Function.Settings_OnScreenDisplayProviderInstalled, inUI, inOwner)
        {
        }
    }
}
