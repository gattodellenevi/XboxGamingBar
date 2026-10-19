using NLog;
using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shared.Data
{
    public abstract class FunctionalProperties<TSharedValueSet, TSharedAppServiceResponse, TSharedAppServiceRequest>
        where TSharedAppServiceResponse : SharedAppServiceResponse
        where TSharedAppServiceRequest : SharedAppServiceRequest
        where TSharedValueSet : SharedValueSet, new()
    {
        protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected readonly Dictionary<Function, FunctionalProperty<TSharedValueSet, TSharedAppServiceResponse>> properties;

        public FunctionalProperties(params FunctionalProperty<TSharedValueSet, TSharedAppServiceResponse>[] inProperties)
        {
            properties = new Dictionary<Function, FunctionalProperty<TSharedValueSet, TSharedAppServiceResponse>>();
            foreach (var property in inProperties)
            {
                if (property == null)
                {
                    Logger.Warn("Null property found, skip it...");
                    continue;
                }

                if (!properties.ContainsKey(property.Function))
                {
                    properties.Add(property.Function, property);
                }
                else
                {
                    Logger.Warn($"Duplicated property {property.Function}");
                }
            }
        }

        public IDisposable BeginBatchUpdate()
        {
            var tokens = new List<IDisposable>();
            foreach (var prop in properties.Values)
            {
                if (prop != null)
                {
                    tokens.Add(prop.BeginBatchUpdate());
                }
            }
            return new BatchUpdateCollectionToken(tokens);
        }

        private sealed class BatchUpdateCollectionToken : IDisposable
        {
            private List<IDisposable> tokens;
            public BatchUpdateCollectionToken(List<IDisposable> tokens)
            {
                this.tokens = tokens;
            }
            public void Dispose()
            {
                if (tokens != null)
                {
                    foreach (var token in tokens)
                    {
                        token?.Dispose();
                    }
                    tokens = null;
                }
            }
        }

        public virtual StateSnapshotDTO CreateStateSnapshot()
        {
            var snapshot = new StateSnapshotDTO();
            foreach (var kvp in properties)
            {
                var function = kvp.Key;
                var property = kvp.Value;
                if (property == null || !property.ShouldSync())
                {
                    continue;
                }

                try
                {
                    var (content, updatedTime) = property.GetSnapshotData();
                    snapshot.AddProperty(function, content, updatedTime);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, $"Failed to get snapshot data for property {function}.");
                }
            }
            return snapshot;
        }

        protected virtual Task OnBeforeGetInitialStateAsync()
        {
            return Task.CompletedTask;
        }

        public async Task OnRequestReceived(TSharedAppServiceRequest request)
        {
            try
            {
                if (request.Message == null)
                {
                    Logger.Error("Request message is null.");
                    return;
                }

                Command command = Command.Get;
                if (request.Message.TryGetValue(nameof(Command), out var cmdObj))
                {
                    if (cmdObj is int cInt) command = (Command)cInt;
                    else if (cmdObj is Command cmd) command = cmd;
                    else if (cmdObj is string s && Enum.TryParse<Command>(s, out var parsedCmd)) command = parsedCmd;
                }

                if (command == Command.GetInitialState)
                {
                    Logger.Info("Handling GetInitialState request: building StateSnapshotDTO...");
                    await OnBeforeGetInitialStateAsync();
                    var snapshot = CreateStateSnapshot();
                    var snapshotJson = snapshot.ToJson();
                    var snapshotResponse = new TSharedValueSet();
                    snapshotResponse.Add(nameof(Content), snapshotJson);
                    var sendSnapshotResult = await SendResponse(request, snapshotResponse);
                    Logger.Info($"Sent GetInitialState response with {snapshot.Properties.Count} properties: {sendSnapshotResult}.");
                    return;
                }

                var function = (Function)request.Message[nameof(Function)];
                if (function == Function.None)
                {
                    Logger.Error("Invalid function.");
                    return;
                }

                if (!properties.TryGetValue(function, out var property))
                {
                    Logger.Error($"Property {function} not found.");
                    return;
                }

                var response = new TSharedValueSet();
                switch (command)
                {
                    case Command.Get:
                        response = property.AddValueSetContent(response);
                        break;
                    case Command.Set:
                        object content = null;
                        if (request.Message.TryGetValue(nameof(Content), out var reqContent))
                        {
                            content = reqContent;
                        }
                        else if (request.Message.TryGetValue("Value", out var reqValue))
                        {
                            content = reqValue;
                        }

                        long updatedTime = DateTime.UtcNow.Ticks;
                        if (request.Message.TryGetValue(nameof(UpdatedTime), out var reqTime) && reqTime is long timeVal)
                        {
                            updatedTime = timeVal;
                        }

                        bool setSuccess;
                        using (property.BeginBatchUpdate())
                        {
                            setSuccess = property.SetValue(content, updatedTime);
                        }
                        // Return the actual confirmed and sanitized value from the property to enforce unidirectional state consistency
                        response = property.AddValueSetContent(response);
                        response.Add("Success", setSuccess);
                        break;
                    default:
                        Logger.Error($"Can't process command {command}");
                        break;
                }
                if (function != Function.HardwareTelemetry)
                {
                    Logger.Info($"Start sending response {function} {response.ToDebugString()}");
                }
                var sendResponseResult = await SendResponse(request, response);
                if (function != Function.HardwareTelemetry)
                {
                    Logger.Info($"Sent response {function} {sendResponseResult}.");
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error(ex, "Exception in OnRequestReceived.");
            }
        }

        protected abstract Task<SharedAppServiceResponseStatus> SendResponse(TSharedAppServiceRequest request, TSharedValueSet response);
    }
}
