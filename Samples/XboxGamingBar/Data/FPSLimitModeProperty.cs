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

        protected override void SetControlEnabled(bool isEnabled)
        {
            if (Owner is GamingWidget widget && !widget.IsRTSSRunning)
            {
                base.SetControlEnabled(false);
                return;
            }
            if (Owner is GamingWidget widget2 && widget2.IsElevationMismatch)
            {
                return;
            }
            base.SetControlEnabled(isEnabled);
        }

        protected override bool ShouldSendNotifyMessage()
        {
            if (Owner is GamingWidget widget && widget.IsElevationMismatch)
            {
                return false;
            }
            return base.ShouldSendNotifyMessage();
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

        private void UpdateUI()
        {
            if (UI != null && UI.SelectedIndex != Value)
            {
                isUpdatingFromBackend = true;
                try
                {
                    Logger.Info($"Update {Function} combo box selected index to {Value}.");
                    UI.SelectedIndex = Value;
                }
                finally
                {
                    isUpdatingFromBackend = false;
                }
            }
        }

        public override async System.Threading.Tasks.Task Sync()
        {
            await base.Sync();

            if (UI != null && Owner != null)
            {
                if (Owner.Dispatcher.HasThreadAccess)
                {
                    UpdateUI();
                }
                else
                {
                    await Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateUI);
                }
            }
        }

        protected override async void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            if (UI != null && Owner != null)
            {
                if (Owner.Dispatcher.HasThreadAccess)
                {
                    UpdateUI();
                }
                else
                {
                    await Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateUI);
                }
            }
        }
    }
}

