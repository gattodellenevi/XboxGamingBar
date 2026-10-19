using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class NvidiaVSyncSupportedProperty : WidgetControlEnabledProperty<ComboBox>
    {
        public NvidiaVSyncSupportedProperty(ComboBox inUI, Page inOwner) : base(Function.NvidiaVSyncSupported, inUI, inOwner)
        {
        }
    }
}
