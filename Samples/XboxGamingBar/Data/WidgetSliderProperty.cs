using System;
using Shared.Enums;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace XboxGamingBar.Data
{
    internal class WidgetSliderProperty : WidgetControlProperty<int, Slider>
    {
        public WidgetSliderProperty(int inValue, Function inFunction, Slider inControl, Page inOwner) : base(inValue, inFunction, inControl, inOwner)
        {
            if (UI != null)
            {
                UI.ValueChanged += Slider_ValueChanged;
                AttachEngagementHandler(UI);
                UI.Value = inValue;
            }
        }

        internal static void AttachNavigationHandler(Slider slider)
        {
            if (slider == null) return;

            slider.PreviewKeyDown += (sender, e) =>
            {
                if (sender is Slider s)
                {
                    switch (e.Key)
                    {
                        case VirtualKey.Up:
                        case VirtualKey.GamepadDPadUp:
                        case VirtualKey.GamepadLeftThumbstickUp:
                            e.Handled = true;
                            FocusManager.TryMoveFocus(FocusNavigationDirection.Up);
                            break;

                        case VirtualKey.Down:
                        case VirtualKey.GamepadDPadDown:
                        case VirtualKey.GamepadLeftThumbstickDown:
                            e.Handled = true;
                            FocusManager.TryMoveFocus(FocusNavigationDirection.Down);
                            break;

                        case VirtualKey.Left:
                        case VirtualKey.GamepadDPadLeft:
                        case VirtualKey.GamepadLeftThumbstickLeft:
                            e.Handled = true;
                            double stepLeft = s.StepFrequency > 0 ? s.StepFrequency : 1;
                            s.Value = Math.Max(s.Minimum, s.Value - stepLeft);
                            break;

                        case VirtualKey.Right:
                        case VirtualKey.GamepadDPadRight:
                        case VirtualKey.GamepadLeftThumbstickRight:
                            e.Handled = true;
                            double stepRight = s.StepFrequency > 0 ? s.StepFrequency : 1;
                            s.Value = Math.Min(s.Maximum, s.Value + stepRight);
                            break;
                    }
                }
            };
        }

        internal static void AttachEngagementHandler(Slider slider) => AttachNavigationHandler(slider);

       

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var newValue = (int)e.NewValue;
            if (newValue != Value)
            {
                Logger.Info($"{Function} Slider value changed from {e.OldValue} to {e.NewValue}, update property.");
                SetValue(newValue);
            }
        }

        protected override async void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            if (UI != null && Owner != null)
            {
                Logger.Info($"Update {Function} slider value {Value}.");
                await Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { UI.Value = Value; });
            }
        }
    }
}
