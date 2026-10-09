using Shared.Enums;
using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace XboxGamingBar.Data
{
    internal class HelperElevationProperty : WidgetPropertyWithAdditionalUI<bool, Border, TextBlock>
    {
        private readonly TextBlock subtitleText;

        private static readonly Color ElevatedColor = Color.FromArgb(255, 16, 124, 65); // Xbox Green #107C41
        private static readonly Color StandardColor = Color.FromArgb(255, 202, 138, 4); // Amber #CA8A04
        private static readonly Color OfflineColor = Color.FromArgb(255, 90, 90, 90);   // Muted Gray #5A5A5A
        private static readonly Color WhiteColor = Colors.White;

        public HelperElevationProperty(Border inBadgeBorder, TextBlock inBadgeText, TextBlock inSubtitleText, Page inOwner)
            : base(false, Function.HelperElevation, inBadgeBorder, inBadgeText, inOwner)
        {
            subtitleText = inSubtitleText;
        }

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            var result = base.SetValue(newValue, updatedTime);

            bool isElevated = Value;
            if (newValue is bool b)
            {
                isElevated = b;
            }
            else if (newValue is string s && bool.TryParse(s, out var parsed))
            {
                isElevated = parsed;
            }

            if (UI != null && AdditionalUI != null && Owner != null)
            {
                var _ = Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateUI(isElevated, true);
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
                    UpdateUI(false, false);
                });
            }
        }

        public void UpdateUI(bool isElevated, bool isConnected)
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
            else if (isElevated)
            {
                UI.Background = new SolidColorBrush(ElevatedColor);
                UI.BorderBrush = new SolidColorBrush(ElevatedColor);
                AdditionalUI.Text = "ELEVATED";
                AdditionalUI.Foreground = new SolidColorBrush(WhiteColor);
                if (subtitleText != null)
                {
                    subtitleText.Text = "Running with Administrator privileges";
                }
            }
            else
            {
                UI.Background = new SolidColorBrush(StandardColor);
                UI.BorderBrush = new SolidColorBrush(StandardColor);
                AdditionalUI.Text = "STANDARD";
                AdditionalUI.Foreground = new SolidColorBrush(WhiteColor);
                if (subtitleText != null)
                {
                    subtitleText.Text = "Running without elevation (limited hardware control)";
                }
            }
        }

        // Helper elevation is only changed/reported by the helper, not changed by widget UI.
        protected override bool ShouldSendNotifyMessage()
        {
            return false;
        }
    }
}
