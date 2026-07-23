using Shared.Constants;
using Shared.Enums;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class RefreshRateProperty : WidgetComboBoxSelectionProperty<int>
    {
        private readonly Slider fpsLimitSlider;

        public RefreshRateProperty(ComboBox inUI, Page inOwner, Slider fpsLimitSlider = null) : base(SystemConstants.DEFAULT_REFRESH_RATE, Function.RefreshRate, inUI, inOwner)
        {
            this.fpsLimitSlider = fpsLimitSlider;

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Value))
                {
                    UpdateFPSLimitMax();
                }
            };

            UpdateFPSLimitMax();
        }

        private void UpdateFPSLimitMax()
        {
            if (fpsLimitSlider != null && Value > 0 && Owner != null)
            {
                _ = Owner.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    fpsLimitSlider.Maximum = Value;
                });
            }
        }
    }
}
