using Shared.Enums;
using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace XboxGamingBar.Data
{
    internal class HardwareProviderProperty : WidgetPropertyWithAdditionalUI<string, Border, TextBlock>
    {
        private readonly TextBlock subtitleText;

        private static readonly Color LibreHardwareColor = Color.FromArgb(255, 16, 124, 65); // Xbox Green #107C41
        private static readonly Color WindowsApiColor = Color.FromArgb(255, 2, 132, 199);   // Sky/Steel Blue #0284C7
        private static readonly Color OfflineColor = Color.FromArgb(255, 90, 90, 90);       // Muted Gray #5A5A5A
        private static readonly Color WhiteColor = Colors.White;

        public HardwareProviderProperty(Border inBadgeBorder, TextBlock inBadgeText, TextBlock inSubtitleText, Page inOwner)
            : base(string.Empty, Function.HardwareProvider, inBadgeBorder, inBadgeText, inOwner)
        {
            subtitleText = inSubtitleText;
        }

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            var result = base.SetValue(newValue, updatedTime);

            string provider = Value ?? string.Empty;
            if (newValue is string s)
            {
                provider = s;
            }

            if (UI != null && AdditionalUI != null && Owner != null)
            {
                var _ = Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI(provider, true);
                });
            }

            return result;
        }

        public override async Task Sync()
        {
            await base.Sync();

            if (UI != null && AdditionalUI != null && Owner != null)
            {
                bool isConnected = App.Connection != null;
                await Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI(Value, isConnected);
                });
            }
        }

        protected override async void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            if (UI != null && AdditionalUI != null && Owner != null)
            {
                await Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI(Value, true);
                });
            }
        }

        public void SetDisconnectedState()
        {
            if (UI != null && AdditionalUI != null && Owner != null)
            {
                var _ = Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI(string.Empty, false);
                });
            }
        }

        public void UpdateUI(string provider, bool isConnected)
        {
            if (!isConnected || string.IsNullOrEmpty(provider))
            {
                UI.Background = new SolidColorBrush(OfflineColor);
                UI.BorderBrush = new SolidColorBrush(OfflineColor);
                AdditionalUI.Text = "OFFLINE";
                AdditionalUI.Foreground = new SolidColorBrush(WhiteColor);
                if (subtitleText != null)
                {
                    subtitleText.Text = "Helper process is not running or disconnected";
                }
            }
            else if (provider.IndexOf("Libre", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                UI.Background = new SolidColorBrush(LibreHardwareColor);
                UI.BorderBrush = new SolidColorBrush(LibreHardwareColor);
                AdditionalUI.Text = "LIBREHARDWARE 0.9.6";
                AdditionalUI.Foreground = new SolidColorBrush(WhiteColor);
                if (subtitleText != null)
                {
                    subtitleText.Text = "LibreHardwareMonitorLib (Hardware sensors: CPU/GPU temps, wattage, clocks)";
                }
            }
            else if (provider.IndexOf("Windows", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                UI.Background = new SolidColorBrush(WindowsApiColor);
                UI.BorderBrush = new SolidColorBrush(WindowsApiColor);
                AdditionalUI.Text = "WINDOWS API";
                AdditionalUI.Foreground = new SolidColorBrush(WhiteColor);
                if (subtitleText != null)
                {
                    subtitleText.Text = "Windows Performance Counters (Store sandbox mode)";
                }
            }
            else
            {
                UI.Background = new SolidColorBrush(LibreHardwareColor);
                UI.BorderBrush = new SolidColorBrush(LibreHardwareColor);
                AdditionalUI.Text = provider.ToUpperInvariant();
                AdditionalUI.Foreground = new SolidColorBrush(WhiteColor);
                if (subtitleText != null)
                {
                    subtitleText.Text = $"Active provider: {provider}";
                }
            }
        }

        protected override bool ShouldSendNotifyMessage()
        {
            return false;
        }
    }
}
