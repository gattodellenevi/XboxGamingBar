using System;
using Shared.Data;
using Shared.Enums;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class ResolutionsProperty : WidgetControlProperty<Resolutions, ComboBox>
    {
        public ResolutionsProperty(ComboBox inUI, Page inOwner) : base(new Resolutions((1920, 1080)), Function.Resolutions, inUI, inOwner)
        {
        }

        protected override bool ShouldSendNotifyMessage()
        {
            return false;
        }

        protected override void SetControlEnabled(bool isEnabled)
        {
            // Resolutions combo box should be enabled/disabled by ResolutionProperty, not this.
        }

        protected override async void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            if (UI != null && Owner != null && Value.AvailableResolutions != null)
            {
                Logger.Info($"Update {Function} combo box value {Value}.");
                await Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var previousSelection = UI.SelectedItem;
                    UI.Items.Clear();
                    int matchedIndex = -1;
                    for (int i = 0; i < Value.AvailableResolutions.Count; i++)
                    {
                        var res = Value.AvailableResolutions[i];
                        UI.Items.Add(res);
                        if (previousSelection != null && res.Equals(previousSelection))
                        {
                            matchedIndex = i;
                        }
                    }
                    if (matchedIndex >= 0)
                    {
                        UI.SelectedIndex = matchedIndex;
                    }
                });
            }
        }
    }
}
