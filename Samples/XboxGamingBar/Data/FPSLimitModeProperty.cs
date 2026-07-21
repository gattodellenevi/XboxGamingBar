using Shared.Enums;
using System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class FPSLimitModeProperty : WidgetControlProperty<int, ComboBox>
    {
        public FPSLimitModeProperty(ComboBox inUI, Page inOwner) : base(0, Function.FPSLimitMode, inUI, inOwner)
        {
            if (UI != null)
            {
                UI.SelectionChanged += ComboBox_SelectionChanged;
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UI.SelectedIndex >= 0 && Value != UI.SelectedIndex)
            {
                SetValue(UI.SelectedIndex);
            }
        }

        protected override async void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            if (UI != null && UI.SelectedIndex != Value)
            {
                await Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { UI.SelectedIndex = Value; });
            }
        }
    }
}
