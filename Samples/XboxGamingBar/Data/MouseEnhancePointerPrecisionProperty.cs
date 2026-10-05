using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class MouseEnhancePointerPrecisionProperty : WidgetToggleProperty
    {
        public MouseEnhancePointerPrecisionProperty(ToggleSwitch inUI, Page inOwner) : base(false, Function.MouseEnhancePointerPrecision, inUI, inOwner)
        {
        }
    }
}
