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
        private readonly Button heroWarningButton;
        private readonly FontIcon heroWarningIcon;
        private readonly TextBlock heroWarningText;

        private static readonly SolidColorBrush ElevatedBrush = new SolidColorBrush(Color.FromArgb(255, 16, 124, 65)); // Xbox Green #107C41
        private static readonly SolidColorBrush StandardBrush = new SolidColorBrush(Color.FromArgb(255, 202, 138, 4)); // Amber #CA8A04
        private static readonly SolidColorBrush OfflineBrush = new SolidColorBrush(Color.FromArgb(255, 90, 90, 90));   // Muted Gray #5A5A5A
        private static readonly SolidColorBrush WhiteBrush = new SolidColorBrush(Colors.White);

        // Warning Button Styling
        private static readonly SolidColorBrush StandardWarningBgBrush = new SolidColorBrush(Color.FromArgb(255, 45, 35, 5)); // Dark Amber #2D2305
        private static readonly SolidColorBrush StandardWarningFgBrush = new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)); // Amber Gold #F59E0B
        private static readonly SolidColorBrush OfflineWarningBgBrush = new SolidColorBrush(Color.FromArgb(255, 37, 37, 37)); // Dark Gray #252525
        private static readonly SolidColorBrush OfflineWarningFgBrush = new SolidColorBrush(Color.FromArgb(255, 160, 160, 160)); // Muted Gray #A0A0A0

        public HelperElevationProperty(Border inBadgeBorder, TextBlock inBadgeText, TextBlock inSubtitleText,
            Button inHeroWarningButton, FontIcon inHeroWarningIcon, TextBlock inHeroWarningText, Page inOwner)
            : base(false, Function.HelperElevation, inBadgeBorder, inBadgeText, inOwner)
        {
            subtitleText = inSubtitleText;
            heroWarningButton = inHeroWarningButton;
            heroWarningIcon = inHeroWarningIcon;
            heroWarningText = inHeroWarningText;
        }

        public HelperElevationProperty(Border inBadgeBorder, TextBlock inBadgeText, TextBlock inSubtitleText, Page inOwner)
            : this(inBadgeBorder, inBadgeText, inSubtitleText, null, null, null, inOwner)
        {
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
                UI.Background = OfflineBrush;
                UI.BorderBrush = OfflineBrush;
                AdditionalUI.Text = "OFFLINE";
                AdditionalUI.Foreground = WhiteBrush;
                if (subtitleText != null)
                {
                    subtitleText.Text = "Helper process is not running or disconnected";
                }

                if (heroWarningButton != null)
                {
                    heroWarningButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    heroWarningButton.Background = OfflineWarningBgBrush;
                    heroWarningButton.BorderBrush = OfflineBrush;
                    if (heroWarningIcon != null)
                    {
                        heroWarningIcon.Glyph = "\uE7BA";
                        heroWarningIcon.Foreground = OfflineWarningFgBrush;
                    }
                    if (heroWarningText != null)
                    {
                        heroWarningText.Text = "Offline";
                        heroWarningText.Foreground = OfflineWarningFgBrush;
                    }
                    ToolTipService.SetToolTip(heroWarningButton, "Helper process is not running or disconnected.");
                }
            }
            else if (isElevated)
            {
                UI.Background = ElevatedBrush;
                UI.BorderBrush = ElevatedBrush;
                AdditionalUI.Text = "ELEVATED";
                AdditionalUI.Foreground = WhiteBrush;
                if (subtitleText != null)
                {
                    subtitleText.Text = "Running with Administrator privileges";
                }

                if (heroWarningButton != null)
                {
                    heroWarningButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
            else
            {
                UI.Background = StandardBrush;
                UI.BorderBrush = StandardBrush;
                AdditionalUI.Text = "STANDARD";
                AdditionalUI.Foreground = WhiteBrush;
                if (subtitleText != null)
                {
                    subtitleText.Text = "Running without elevation (limited hardware control)";
                }

                if (heroWarningButton != null)
                {
                    heroWarningButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    heroWarningButton.Background = StandardWarningBgBrush;
                    heroWarningButton.BorderBrush = StandardBrush;
                    if (heroWarningIcon != null)
                    {
                        heroWarningIcon.Glyph = "\uE7BA";
                        heroWarningIcon.Foreground = StandardWarningFgBrush;
                    }
                    if (heroWarningText != null)
                    {
                        heroWarningText.Text = "Admin Needed";
                        heroWarningText.Foreground = StandardWarningFgBrush;
                    }
                    ToolTipService.SetToolTip(heroWarningButton, "Helper running without Administrator privileges. Hardware power controls are limited.");
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
