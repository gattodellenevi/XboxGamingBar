using Shared.Enums;
using System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class NvidiaVSyncModeProperty : WidgetControlProperty<int, ComboBox>
    {
        private bool isUpdatingFromBackend = false;

        public NvidiaVSyncModeProperty(ComboBox inUI, Page inOwner) : base(0, Function.NvidiaVSyncMode, inUI, inOwner)
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

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            var result = base.SetValue(newValue, updatedTime);

            if (UI != null && Owner != null)
            {
                if (Owner.Dispatcher.HasThreadAccess)
                {
                    UpdateUI();
                }
                else
                {
                    var _ = Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateUI);
                }
            }

            return result;
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
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
                    var _ = Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateUI);
                }
            }
        }
    }
}
