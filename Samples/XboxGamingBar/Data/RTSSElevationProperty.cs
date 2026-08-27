using Shared.Enums;
using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace XboxGamingBar.Data
{
    internal class RTSSElevationProperty : WidgetPropertyWithAdditionalUI<int, Border, TextBlock>
    {
        private readonly TextBlock subtitleText;
        private static readonly SolidColorBrush ElevatedBrush = new SolidColorBrush(Color.FromArgb(255, 16, 124, 65)); // Xbox Green #107C41
        private static readonly SolidColorBrush StandardBrush = new SolidColorBrush(Color.FromArgb(255, 202, 138, 4)); // Amber #CA8A04
        private static readonly SolidColorBrush OfflineBrush = new SolidColorBrush(Color.FromArgb(255, 90, 90, 90));   // Muted Gray #5A5A5A
        private static readonly SolidColorBrush WhiteBrush = new SolidColorBrush(Colors.White);

        public RTSSElevationProperty(Border inBadgeBorder, TextBlock inBadgeText, TextBlock inSubtitleText, Page inOwner)
            : base(0, Function.RTSSElevation, inBadgeBorder, inBadgeText, inOwner)
        {
            subtitleText = inSubtitleText;
        }

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            var result = base.SetValue(newValue, updatedTime);

            int status = Value;
            if (newValue is int i)
            {
                status = i;
            }
            else if (newValue is string s && int.TryParse(s, out var parsed))
            {
                status = parsed;
            }

            if (UI != null && AdditionalUI != null && Owner != null)
            {
                var _ = Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI(status, true);
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
                    UpdateUI(0, false);
                });
            }
        }

        public void UpdateUI(int status, bool isConnected)
        {
            if (!isConnected)
            {
                UI.Background = OfflineBrush;
                UI.BorderBrush = OfflineBrush;
                AdditionalUI.Text = "OFFLINE";
                AdditionalUI.Foreground = WhiteBrush;
                if (subtitleText != null)
                {
                    subtitleText.Text = "Helper process is not running or disconnected";
                }
            }
            else
            {
                switch (status)
                {
                    case 2: // Elevated
                        UI.Background = ElevatedBrush;
                        UI.BorderBrush = ElevatedBrush;
                        AdditionalUI.Text = "ELEVATED";
                        AdditionalUI.Foreground = WhiteBrush;
                        if (subtitleText != null)
                        {
                            subtitleText.Text = "Running with Administrator privileges";
                        }
                        break;
                    case 1: // Standard
                        UI.Background = StandardBrush;
                        UI.BorderBrush = StandardBrush;
                        AdditionalUI.Text = "STANDARD";
                        AdditionalUI.Foreground = WhiteBrush;
                        if (subtitleText != null)
                        {
                            subtitleText.Text = "Running without elevation (limited hook access)";
                        }
                        break;
                    case -1: // Not installed
                        UI.Background = OfflineBrush;
                        UI.BorderBrush = OfflineBrush;
                        AdditionalUI.Text = "NOT INSTALLED";
                        AdditionalUI.Foreground = WhiteBrush;
                        if (subtitleText != null)
                        {
                            subtitleText.Text = "RivaTuner Statistics Server is not installed";
                        }
                        break;
                    case 0: // Offline
                    default:
                        UI.Background = OfflineBrush;
                        UI.BorderBrush = OfflineBrush;
                        AdditionalUI.Text = "OFFLINE";
                        AdditionalUI.Foreground = WhiteBrush;
                        if (subtitleText != null)
                        {
                            subtitleText.Text = "RivaTuner Statistics Server is not running";
                        }
                        break;
                }
            }
        }

        protected override bool ShouldSendNotifyMessage()
        {
            return false;
        }
    }
}
