using Shared.Enums;
using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal interface IWidgetControlProperty
    {
        void EnableControl(bool isEnabled = true);
    }

    internal class WidgetControlProperty<ValueType, UIType> : WidgetUIProperty<ValueType, UIType>, IWidgetControlProperty where UIType : Control
    {
        public WidgetControlProperty(ValueType inValue, Function inFunction, UIType inUI, Page inOwner) : base(inValue, inFunction, inUI, inOwner)
        {
        }

        public override async Task Sync()
        {
            await base.Sync();

            if (UI != null && Owner != null)
            {
                await Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SetControlEnabled(true));
            }
        }

        public virtual void EnableControl(bool isEnabled = true)
        {
            SetControlEnabled(isEnabled);
        }

        protected virtual void SetControlEnabled(bool isEnabled)
        {
            Logger.Debug($"{GetType().Name} Set {UI?.Name} {(isEnabled ? "enabled" : "disabled")}.");
            if (UI != null)
            {
                UI.IsEnabled = isEnabled;
            }
        }
    }
}
