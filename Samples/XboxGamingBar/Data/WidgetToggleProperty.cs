using Shared.Enums;
using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class WidgetToggleProperty : WidgetControlProperty<bool, ToggleSwitch>
    {
        private bool isUpdatingFromBackend = false;

        public WidgetToggleProperty(bool inValue, Function inFunction, ToggleSwitch inUI, Page inOwner) : base(inValue, inFunction, inUI, inOwner)
        {
            if (UI != null)
            {
                UI.Toggled += ToggleSwitch_ValueChanged;
                UI.IsOn = inValue;
            }
        }

        protected virtual void ToggleSwitch_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (isUpdatingFromBackend) return;

            SetValue(UI.IsOn, DateTime.UtcNow.Ticks);
        }

        protected override async void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            if (UI != null && Owner != null)
            {
                Logger.Info($"Update {Function} value {Value}.");
                void UpdateToggle()
                {
                    isUpdatingFromBackend = true;
                    try
                    {
                        UI.IsOn = Value;
                    }
                    finally
                    {
                        isUpdatingFromBackend = false;
                    }
                }

                if (Owner.Dispatcher.HasThreadAccess)
                {
                    UpdateToggle();
                }
                else
                {
                    await Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateToggle);
                }
            }
        }
    }
}
