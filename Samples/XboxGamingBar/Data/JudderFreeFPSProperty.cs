using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class JudderFreeFPSProperty : WidgetToggleProperty
    {
        public JudderFreeFPSProperty(ToggleSwitch inUI, Page inOwner) : base(true, Function.JudderFreeFPS, inUI, inOwner)
        {
        }
    }
}
