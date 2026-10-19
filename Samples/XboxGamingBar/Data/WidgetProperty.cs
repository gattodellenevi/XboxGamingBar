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
            bool wasBatchUpdating = IsBatchUpdating;
            try
            {
                var dispatcher = CoreApplication.MainView?.CoreWindow?.Dispatcher;
                if (dispatcher != null && !dispatcher.HasThreadAccess)
                {
                    _ = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (wasBatchUpdating)
                        {
                            using (BeginBatchUpdate())
                            {
                                base.NotifyPropertyChanged(propertyName);
                            }
                        }
                        else
                        {
                            base.NotifyPropertyChanged(propertyName);
                        }
                    });
                    return;
                }
            }
            catch
            {
            }

            base.NotifyPropertyChanged(propertyName);
        }

        public async Task PushCurrentValueToHelperAsync()
        {
            if (App.Connection == null) return;

            try
            {
                var request = new WidgetValueSet
                {
                    { nameof(Command), (int)Command.Set },
                    { nameof(Function), (int)function },
                };
                request = AddValueSetContent(request);
                await SendMessageAsync(request);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Failed to push current value of {function} to helper.");
            }
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
