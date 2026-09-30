using Shared.Enums;
using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class DualGpuSupportProperty : WidgetPropertyWithAdditionalUI<string, Border, TextBlock>
    {
        private readonly Button discreteButton;
        private readonly Button integratedButton;

        public DualGpuSupportProperty(Border inCard, TextBlock inSubtitleText, Button inDiscreteButton, Button inIntegratedButton, Page inOwner)
            : base(string.Empty, Function.Support_DualGpu, inCard, inSubtitleText, inOwner)
        {
            discreteButton = inDiscreteButton;
            integratedButton = inIntegratedButton;
        }

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            var result = base.SetValue(newValue, updatedTime);

            string data = Value ?? string.Empty;
            if (newValue is string s) data = s;

            if (UI != null && Owner != null)
            {
                var _ = Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI(data);
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
                    UpdateUI(Value ?? string.Empty);
                });
            }
        }

        public void SetDisconnectedState()
        {
            if (Owner == null || UI == null) return;
            var _ = Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UI.Visibility = Visibility.Collapsed;
            });
        }

        private void UpdateUI(string data)
        {
            if (Owner == null || UI == null) return;

            if (!Owner.Dispatcher.HasThreadAccess)
            {
                var _ = Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateUI(data));
                return;
            }

            if (string.IsNullOrEmpty(data))
            {
                UI.Visibility = Visibility.Collapsed;
            }
            else
            {
                UI.Visibility = Visibility.Visible;
                var parts = data.Split(';');
                if (parts.Length >= 2)
                {
                    string discrete = parts[0];
                    string integrated = parts[1];

                    if (discreteButton != null)
                    {
                        ToolTipService.SetToolTip(discreteButton, $"Monitor Discrete GPU: {discrete}");
                    }
                    if (integratedButton != null)
                    {
                        ToolTipService.SetToolTip(integratedButton, $"Monitor Integrated GPU: {integrated}");
                    }
                }
            }
        }
    }
}
