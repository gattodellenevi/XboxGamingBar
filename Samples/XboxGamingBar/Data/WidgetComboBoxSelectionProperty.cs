using Shared.Enums;
using System;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace XboxGamingBar.Data
{
    internal class WidgetComboBoxSelectionProperty<ValueType> : WidgetControlProperty<ValueType, ComboBox> where ValueType : IEquatable<ValueType>
    {
        private bool isUpdatingFromBackend = false;

        public WidgetComboBoxSelectionProperty(ValueType inValue, Function inFunction, ComboBox inUI, Page inOwner) : base(inValue, inFunction, inUI, inOwner)
        {
            if (UI != null)
            {
                UI.SelectionChanged += ComboBox_SelectionChanged;
                UI.DropDownClosed += ComboBox_DropDownClosed;
                AttachNavigationHandler(UI);
            }
        }

        internal static void AttachNavigationHandler(ComboBox comboBox)
        {
            if (comboBox == null) return;

            comboBox.PreviewKeyDown += (sender, e) =>
            {
                if (sender is ComboBox cb && !cb.IsDropDownOpen)
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
                            FocusManager.TryMoveFocus(FocusNavigationDirection.Left);
                            break;

                        case VirtualKey.Right:
                        case VirtualKey.GamepadDPadRight:
                        case VirtualKey.GamepadLeftThumbstickRight:
                            e.Handled = true;
                            FocusManager.TryMoveFocus(FocusNavigationDirection.Right);
                            break;
                    }
                }
            };
        }

        protected virtual void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingFromBackend) return;

            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ValueType selectedValue)
            {
                Logger.Info($"{Function} combo box selection changed to {selectedValue}.");
            }
        }

        private void ComboBox_DropDownClosed(object sender, object e)
        {
            if (isUpdatingFromBackend) return;

            if (UI != null && UI.SelectedItem is ValueType selectedValue && !selectedValue.Equals(Value))
            {
                Logger.Info($"{Function} combo box dropdown closed, applying new selection {selectedValue}.");
                SetValue(selectedValue);
            }
        }

        protected override async void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            if (UI != null && Owner != null)
            {
                await Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    isUpdatingFromBackend = true;
                    try
                    {
                        for (var i = 0; i < UI.Items.Count; i++)
                        {
                            if (UI.Items[i] is ValueType selectedValue && selectedValue.Equals(Value))
                            {
                                Logger.Info($"{Function} combo box selected index {i}.");
                                UI.SelectedIndex = i;
                                break;
                            }
                        }
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
