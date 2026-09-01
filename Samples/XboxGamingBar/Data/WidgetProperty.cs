using System;
using Shared.Data;
using Shared.Enums;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace XboxGamingBar.Data
{
    internal class WidgetProperty<ValueType> : GenericProperty<ValueType, WidgetValueSet, WidgetAppServiceResponse>
    {
        public WidgetProperty(ValueType inValue, IProperty inParentProperty, Function inFunction) : base(inValue, inParentProperty, inFunction)
        {
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            try
            {
                var dispatcher = CoreApplication.MainView?.CoreWindow?.Dispatcher;
                if (dispatcher != null && !dispatcher.HasThreadAccess)
                {
                    _ = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        base.NotifyPropertyChanged(propertyName);
                    });
                    return;
                }
            }
            catch
            {
            }

            base.NotifyPropertyChanged(propertyName);
        }

        protected override Task<WidgetAppServiceResponse> SendMessageAsync(WidgetValueSet request)
        {
            if (App.Connection == null)
            {
                Logger.Warn($"Widget property {function} doesn't have connection.");
                return null;
            }

            Logger.Info($"Sending message for widget property {function}: {request.ToDebugString()}.");
            return App.Connection.SendMessageAsync(request.ValueSet).AsTask().ContinueWith(antecedentTask =>
            {
                return new WidgetAppServiceResponse(antecedentTask.Result);
            }, TaskScheduler.Default);
        }
    }
}
