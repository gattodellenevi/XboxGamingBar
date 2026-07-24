using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class SetCPUEPPProperty : WidgetToggleProperty
    {
        public SetCPUEPPProperty(ToggleSwitch inUI, Page inOwner) : base(false, Function.SetCPUEPP, inUI, inOwner)
        {
        }
    }
}
