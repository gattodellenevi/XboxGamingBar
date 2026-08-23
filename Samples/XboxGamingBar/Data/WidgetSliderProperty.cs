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

        //private void Slider_NoFocusCandidateFound(Windows.UI.Xaml.UIElement sender, Windows.UI.Xaml.Input.NoFocusCandidateFoundEventArgs args)
        //{
        //    Logger.Info($"Slider {UI.Name} of function {function} NoFocusCandidateFound");
        //}

        //private void Slider_LostFocus(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        //{
        //    Logger.Info($"Slider {UI.Name} of function {function} LostFocus");
        //}

        //private void Slider_LosingFocus(Windows.UI.Xaml.UIElement sender, Windows.UI.Xaml.Input.LosingFocusEventArgs args)
        //{
        //    Logger.Info($"Slider {UI.Name} of function {function} LosingFocus");
        //}

        //private void Slider_GotFocus(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        //{
        //    Logger.Info($"Slider {UI.Name} of function {function} GotFocus");
        //}

        //private void Slider_GettingFocus(Windows.UI.Xaml.UIElement sender, Windows.UI.Xaml.Input.GettingFocusEventArgs args)
        //{
        //    Logger.Info($"Slider {UI.Name} of function {function} GettingFocus");
        //}

        //private void Slider_FocusEngaged(Control sender, FocusEngagedEventArgs args)
        //{
        //    Logger.Info($"Slider {UI.Name} of function {function} FocusEngaged");
        //}

        //private void Slider_FocusDisengaged(Control sender, FocusDisengagedEventArgs args)
        //{
        //    Logger.Info($"Slider {UI.Name} of function {function} FocusDisengaged");
        //}

        //private void Slider_DragLeave(object sender, Windows.UI.Xaml.DragEventArgs e)
        //{
        //    Logger.Info($"{Function} Slider drag leave {e.Data.ToString()}.");
        //}

        //private void Slider_DragOver(object sender, Windows.UI.Xaml.DragEventArgs e)
        //{
        //    Logger.Info($"{Function} Slider drag over {e.Data.ToString()}.");
        //}

        //private void Slider_DragStarting(Windows.UI.Xaml.UIElement sender, Windows.UI.Xaml.DragStartingEventArgs args)
        //{
        //    Logger.Info($"{Function} Slider drag starting {args.Data.ToString()}.");
        //}

        //private void Slider_DragEnter(object sender, Windows.UI.Xaml.DragEventArgs e)
        //{
        //    Logger.Info($"{Function} Slider drag enter {e.Data.ToString()}.");
        //}

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
