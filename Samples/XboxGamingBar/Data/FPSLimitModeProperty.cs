using Shared.Enums;
using System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class FPSLimitModeProperty : WidgetControlProperty<int, ComboBox>
    {
        private bool isUpdatingFromBackend = false;

        public FPSLimitModeProperty(ComboBox inUI, Page inOwner) : base(0, Function.FPSLimitMode, inUI, inOwner)
        {
            if (UI != null)
            {
                UI.DropDownClosed += ComboBox_DropDownClosed;
                WidgetComboBoxSelectionProperty<int>.AttachNavigationHandler(UI);
                if (UI.SelectedIndex < 0)
                {
                    UI.SelectedIndex = 0;
                }
            }
        }

        private void ComboBox_DropDownClosed(object sender, object e)
        {
            if (isUpdatingFromBackend) return;

            if (UI != null && UI.SelectedIndex >= 0 && UI.SelectedIndex != Value)
            {
                Logger.Info($"{Function} combo box dropdown closed, applying new mode {UI.SelectedIndex}.");
                SetValue(UI.SelectedIndex);
            }
        }

        protected override async void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            if (UI != null && UI.SelectedIndex != Value && Owner != null)
            {
                await Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    isUpdatingFromBackend = true;
                    try
                    {
                        UI.SelectedIndex = Value;
                    }
                    finally
                    {
                        isUpdatingFromBackend = false;
                    }
                });
            }
        }
    }
}
