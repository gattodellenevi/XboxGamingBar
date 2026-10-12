using Shared.Enums;
using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class ActiveGpuProperty : WidgetPropertyWithAdditionalUI<string, TextBlock, TextBlock>
    {
        private readonly Border badgeBorder;

        public ActiveGpuProperty(TextBlock inGpuNameText, TextBlock inBadgeText, Border inBadgeBorder, Page inOwner)
            : base(string.Empty, Function.ActiveGpu, inGpuNameText, inBadgeText, inOwner)
        {
            badgeBorder = inBadgeBorder;
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
                UI.Text = "ACTIVE GPU: Not Connected";
                if (AdditionalUI != null)
                {
                    AdditionalUI.Text = "GPU";
                }
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

            if (string.IsNullOrWhiteSpace(data))
            {
                UI.Text = "ACTIVE GPU: Detecting...";
                if (AdditionalUI != null)
                {
                    AdditionalUI.Text = "GPU";
                }
                return;
            }

            // Expected format: "{gpuName}|{Discrete/Integrated}|{vendor}"
            var parts = data.Split('|');
            string gpuName = parts[0];
            string type = parts.Length > 1 ? parts[1] : "Discrete";

            UI.Text = $"ACTIVE GPU: {gpuName}";
            if (AdditionalUI != null)
            {
                AdditionalUI.Text = $"{type} GPU";
            }
        }

        protected override bool ShouldSendNotifyMessage()
        {
            return false;
        }
    }
}
