using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    /// <summary>
    /// This kind of property decides whether a widget control is enabled or disabled.
    /// </summary>
    internal class WidgetControlEnabledProperty<UIType> : WidgetControlProperty<bool, UIType> where UIType : Control
    {
        public WidgetControlEnabledProperty(Function inFunction, UIType inUI, Page inOwner) : base(false, inFunction, inUI, inOwner)
        {
        }

        protected override bool ShouldSendNotifyMessage()
        {
            return false;
        }

        public override void EnableControl(bool isEnabled = true)
        {
            SetControlEnabled(Value);
        }

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            var result = base.SetValue(newValue, updatedTime);
            if (UI != null && Owner != null)
            {
                if (Owner.Dispatcher.HasThreadAccess)
                {
                    SetControlEnabled(Value);
                }
                else
                {
                    var _ = Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        SetControlEnabled(Value);
                    });
                }
            }
            return result;
        }

        protected override async void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            if (UI != null && Owner != null)
            {
                void UpdateEnabled()
                {
                    Logger.Debug($"{(Value ? "Enabled" : "Disabled")} {UI.Name} property changed.");
                    SetControlEnabled(Value);
                }

                if (Owner.Dispatcher.HasThreadAccess)
                {
                    UpdateEnabled();
                }
                else
                {
                    await Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateEnabled);
                }
            }
        }

        public override async Task Sync()
        {
            await base.Sync();

            if (UI != null && Owner != null)
            {
                await Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Logger.Debug($"{(Value ? "Enabled" : "Disabled")} {UI.Name} at sync.");
                    SetControlEnabled(Value);
                });
            }
        }
    }
}
