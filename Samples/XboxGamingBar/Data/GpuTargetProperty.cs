using Shared.Enums;
using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace XboxGamingBar.Data
{
    internal class GpuTargetProperty : WidgetPropertyWithAdditionalUI<int, Button, Button>
    {
        public GpuTargetProperty(Button inDiscreteButton, Button inIntegratedButton, Page inOwner)
            : base(0, Function.Settings_GpuTarget, inDiscreteButton, inIntegratedButton, inOwner)
        {
        }

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            var result = base.SetValue(newValue, updatedTime);

            int target = Value;
            if (newValue is int i) target = i;

            if (UI != null && AdditionalUI != null && Owner != null)
            {
                var _ = Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI(target);
                });
            }

            return result;
        }

        public override async Task Sync()
        {
            await base.Sync();
            if (Owner != null)
            {
                await Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI(Value);
                });
            }
        }

        public void UpdateUI(int target)
        {
            if (Owner == null || UI == null || AdditionalUI == null) return;

            if (!Owner.Dispatcher.HasThreadAccess)
            {
                var _ = Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateUI(target));
                return;
            }

            var activeBrush = new SolidColorBrush(Color.FromArgb(255, 16, 124, 65)); // Xbox Green #107C41
            var inactiveBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)); // Subtle transparent dark
            var whiteBrush = new SolidColorBrush(Colors.White);

            // UI = Discrete Button, AdditionalUI = Integrated Button
            if (target == 0)
            {
                UI.Background = activeBrush;
                UI.Foreground = whiteBrush;
                AdditionalUI.Background = inactiveBrush;
                AdditionalUI.Foreground = whiteBrush;
            }
            else
            {
                UI.Background = inactiveBrush;
                UI.Foreground = whiteBrush;
                AdditionalUI.Background = activeBrush;
                AdditionalUI.Foreground = whiteBrush;
            }
        }
    }
}
