using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class LimitCPUEPPProperty : WidgetToggleProperty
    {
        public LimitCPUEPPProperty(ToggleSwitch inUI, Page inOwner) : base(false, Function.LimitCPUEPP, inUI, inOwner)
        {
        }
    }
}
