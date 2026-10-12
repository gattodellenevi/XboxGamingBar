using NLog;
using Shared.Data;
using Shared.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;

namespace XboxGamingBar.Data
{
    internal class WidgetProperties : FunctionalProperties<WidgetValueSet, WidgetAppServiceResponse, WidgetAppServiceRequest>
    {
        private const string CACHED_SNAPSHOT_KEY = "CachedStateSnapshot_v1";
        private readonly CoreDispatcher uiDispatcher;

        public bool HasCachedState { get; private set; } = false;

        public WidgetProperties(params FunctionalProperty<WidgetValueSet, WidgetAppServiceResponse>[] inProperties) : this(null, inProperties) { }

        public WidgetProperties(CoreDispatcher inDispatcher, params FunctionalProperty<WidgetValueSet, WidgetAppServiceResponse>[] inProperties) : base(inProperties)
        {
            uiDispatcher = inDispatcher;
        }

        protected override Task<SharedAppServiceResponseStatus> SendResponse(WidgetAppServiceRequest request, WidgetValueSet response)
        {
            if (request.Message == null || !request.Message.TryGetValue(nameof(Shared.Enums.Function), out var funcObj) || !(funcObj is int funcInt) || (Shared.Enums.Function)funcInt != Shared.Enums.Function.HardwareTelemetry)
            {
                Logger.Info($"Sending response request {request.Message?.ToDebugString()}: {response.ToDebugString()}.");
            }
            return request.SendResponseAsync(response).AsTask().ContinueWith(antecedentTask =>
            {
                return (SharedAppServiceResponseStatus)(int)antecedentTask.Result;
            }, TaskScheduler.Default);
        }

        public async Task Sync()
        {
            bool snapshotSuccess = false;
            try
            {
                snapshotSuccess = await SyncSnapshotAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception during snapshot sync; falling back to sequential sync.");
            }

            if (!snapshotSuccess)
            {
                Logger.Warn("Snapshot sync did not succeed; falling back to sequential property sync.");
                foreach (var property in properties)
                {
                    await property.Value.Sync();
                }
            }
        }

        public async Task<bool> SyncSnapshotAsync()
        {
            if (App.Connection == null)
            {
                Logger.Warn("Cannot sync snapshot: App.Connection is null.");
                return false;
            }

            var request = new WidgetValueSet
            {
                { nameof(Command), (int)Command.GetInitialState },
                { nameof(Function), (int)Function.None }
            };

            Logger.Info("Requesting unified initial state snapshot from helper...");
            var response = await App.Connection.SendMessageAsync(request.ValueSet);
            if (response.Status != AppServiceResponseStatus.Success || response.Message == null)
            {
                Logger.Warn($"Snapshot request failed with status: {response.Status}");
                return false;
            }

            if (!response.Message.TryGetValue(nameof(Content), out object snapshotObj))
            {
                Logger.Warn("Snapshot response did not contain content.");
                return false;
            }

            StateSnapshotDTO snapshot = null;
            if (snapshotObj is string snapshotJson && !string.IsNullOrWhiteSpace(snapshotJson))
            {
                snapshot = StateSnapshotDTO.FromJson(snapshotJson);
            }

            if (snapshot == null || snapshot.Properties == null || snapshot.Properties.Count == 0)
            {
                Logger.Warn("Failed to deserialize StateSnapshotDTO or snapshot is empty.");
                return false;
            }

            Logger.Info($"Received StateSnapshotDTO with {snapshot.Properties.Count} properties. Applying batch on UI thread...");
            await ApplySnapshotAsync(snapshot);
            CacheSnapshot(snapshot);
            HasCachedState = true;
            return true;
        }

        public void CacheSnapshot(StateSnapshotDTO snapshot)
        {
            if (snapshot == null) return;
            try
            {
                string json = snapshot.ToJson();
                if (!string.IsNullOrEmpty(json))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    localSettings.Values[CACHED_SNAPSHOT_KEY] = json;
                    Logger.Info($"Cached StateSnapshotDTO with {snapshot.Properties?.Count ?? 0} properties to LocalSettings.");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to cache StateSnapshotDTO to LocalSettings.");
            }
        }

        public StateSnapshotDTO LoadCachedSnapshot()
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings.Values.TryGetValue(CACHED_SNAPSHOT_KEY, out object cachedObj) && cachedObj is string json && !string.IsNullOrWhiteSpace(json))
                {
                    var snapshot = StateSnapshotDTO.FromJson(json);
                    if (snapshot != null && snapshot.Properties != null && snapshot.Properties.Count > 0)
                    {
                        Logger.Info($"Loaded cached StateSnapshotDTO with {snapshot.Properties.Count} properties from LocalSettings.");
                        return snapshot;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to load cached StateSnapshotDTO from LocalSettings.");
            }
            return null;
        }

        public bool TryApplyCachedSnapshot()
        {
            var cached = LoadCachedSnapshot();
            if (cached != null)
            {
                Logger.Info("Applying cached state snapshot for instant UI presentation...");
                ApplySnapshotInternal(cached);
                HasCachedState = true;
                return true;
            }
            return false;
        }

        public async Task ApplySnapshotAsync(StateSnapshotDTO snapshot)
        {
            if (snapshot == null || snapshot.Properties == null) return;

            var dispatcher = uiDispatcher ?? CoreApplication.MainView?.CoreWindow?.Dispatcher ?? Windows.UI.Xaml.Window.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.HasThreadAccess)
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ApplySnapshotInternal(snapshot);
                });
            }
            else
            {
                ApplySnapshotInternal(snapshot);
            }
        }

        private void ApplySnapshotInternal(StateSnapshotDTO snapshot)
        {
            using (BeginBatchUpdate())
            {
                // 1. First apply list/options properties (Resolutions, RefreshRates) so ComboBoxes have items populated
                foreach (var item in snapshot.Properties)
                {
                    if (item.Function == Function.Resolutions || item.Function == Function.RefreshRates)
                    {
                        ApplySingleProperty(item);
                    }
                }

                // 2. Then apply selection and all other properties
                foreach (var item in snapshot.Properties)
                {
                    if (item.Function != Function.Resolutions && item.Function != Function.RefreshRates)
                    {
                        ApplySingleProperty(item);
                    }
                }

                // 3. Enable all control properties
                foreach (var property in properties.Values)
                {
                    if (property is IWidgetControlProperty controlProperty)
                    {
                        controlProperty.EnableControl(true);
                    }
                }
            }

            Logger.Info("Finished applying snapshot properties and enabled controls in batch on UI thread.");
        }

        private void ApplySingleProperty(PropertySnapshotDTO item)
        {
            if (properties.TryGetValue(item.Function, out var property))
            {
                if (property.ShouldSync())
                {
                    try
                    {
                        var clrValue = item.GetClrValue();
                        property.SetValue(clrValue, item.UpdatedTime);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, $"Failed to apply snapshot value for property {item.Function}.");
                    }
                }
            }
        }
    }
}
