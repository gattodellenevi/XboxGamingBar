using Shared.Constants;
using Shared.Enums;
using System;
using System.Collections.Generic;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class RefreshRatesProperty : WidgetControlProperty<List<int>, ComboBox>
    {
        public RefreshRatesProperty(ComboBox inUI, Page inOwner) : base(new List<int>() { SystemConstants.DEFAULT_REFRESH_RATE }, Function.RefreshRates, inUI, inOwner)
        {
        }

        protected override bool ShouldSendNotifyMessage()
        {
            return false;
        }

        protected override async void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            if (UI != null && Owner != null && Value != null)
            {
                Logger.Info($"Update {Function} slider value {Value}.");
                void UpdateItems()
                {
                    UI.Items.Clear();
                    foreach (var value in Value)
                    {
                        UI.Items.Add(value);
                    }
                }

                if (Owner.Dispatcher.HasThreadAccess)
                {
                    UpdateItems();
                }
                else
                {
                    await Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateItems);
                }
            }
        }

        protected override void SetControlEnabled(bool isEnabled)
        {
            // Refresh rates combo box should be enabled/disabled by RefreshRateProperty, not this.
            // base.SetControlEnabled(isEnabled);
        }
    }
}
