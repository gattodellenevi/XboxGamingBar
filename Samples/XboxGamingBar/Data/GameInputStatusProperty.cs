using Shared.Enums;
using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace XboxGamingBar.Data
{
    internal class GameInputStatusProperty : WidgetPropertyWithAdditionalUI<int, Border, TextBlock>
    {
        private readonly TextBlock subtitleText;
        private static readonly Color RunningColor = Color.FromArgb(255, 16, 124, 65); // Xbox Green #107C41
        private static readonly Color DesyncColor = Color.FromArgb(255, 202, 138, 4);  // Amber #CA8A04
        private static readonly Color OfflineColor = Color.FromArgb(255, 90, 90, 90);   // Muted Gray #5A5A5A
        private static readonly Color WhiteColor = Colors.White;

        public GameInputStatusProperty(Border inBadgeBorder, TextBlock inBadgeText, TextBlock inSubtitleText, Page inOwner)
            : base(0, Function.GameInputStatus, inBadgeBorder, inBadgeText, inOwner)
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
                UI.Background = new SolidColorBrush(OfflineColor);
                UI.BorderBrush = new SolidColorBrush(OfflineColor);
                AdditionalUI.Text = "OFFLINE";
                AdditionalUI.Foreground = new SolidColorBrush(WhiteColor);
                if (subtitleText != null)
                {
                    subtitleText.Text = "Helper process is not running or disconnected";
                }
            }
            else
            {
                switch (status)
                {
                    case 1: // Running & Healthy
                        UI.Background = new SolidColorBrush(RunningColor);
                        UI.BorderBrush = new SolidColorBrush(RunningColor);
                        AdditionalUI.Text = "RUNNING";
                        AdditionalUI.Foreground = new SolidColorBrush(WhiteColor);
                        if (subtitleText != null)
                        {
                            subtitleText.Text = "Powers Game Bar gamepad cursor and controller navigation";
                        }
                        break;
                    case 2: // Desync / Degraded
                        UI.Background = new SolidColorBrush(DesyncColor);
                        UI.BorderBrush = new SolidColorBrush(DesyncColor);
                        AdditionalUI.Text = "DESYNC";
                        AdditionalUI.Foreground = new SolidColorBrush(WhiteColor);
                        if (subtitleText != null)
                        {
                            subtitleText.Text = "Service running but session worker is unresponsive (cursor will freeze)";
                        }
                        break;
                    case -1: // Not installed
                        UI.Background = new SolidColorBrush(OfflineColor);
                        UI.BorderBrush = new SolidColorBrush(OfflineColor);
                        AdditionalUI.Text = "NOT INSTALLED";
                        AdditionalUI.Foreground = new SolidColorBrush(WhiteColor);
                        if (subtitleText != null)
                        {
                            subtitleText.Text = "Microsoft GameInput service is not installed";
                        }
                        break;
                    case 0: // Stopped
                    default:
                        UI.Background = new SolidColorBrush(OfflineColor);
                        UI.BorderBrush = new SolidColorBrush(OfflineColor);
                        AdditionalUI.Text = "STOPPED";
                        AdditionalUI.Foreground = new SolidColorBrush(WhiteColor);
                        if (subtitleText != null)
                        {
                            subtitleText.Text = "GameInput service is stopped";
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
