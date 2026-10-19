using Shared.Data;
using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace XboxGamingBarHelper.Core
{
    internal class HelperProperties : FunctionalProperties<HelperValueSet, HelperAppServiceResponse, HelperAppServiceRequest>
    {
        private static readonly HashSet<Function> AmdAsyncFunctions = new HashSet<Function>
        {
            Function.Support_AMDSettings,
            Function.AMDRadeonSuperResolutionSupported,
            Function.AMDRadeonSuperResolutionEnabled,
            Function.AMDRadeonSuperResolutionSharpness,
            Function.AMDFluidMotionFrameSupported,
            Function.AMDFluidMotionFrameEnabled,
            Function.AMDRadeonAntiLagSupported,
            Function.AMDRadeonAntiLagEnabled,
            Function.AMDRadeonBoostSupported,
            Function.AMDRadeonBoostEnabled,
            Function.AMDRadeonBoostResolution,
            Function.AMDRadeonChillSupported,
            Function.AMDRadeonChillEnabled,
            Function.AMDRadeonChillMinFPS,
            Function.AMDRadeonChillMaxFPS
        };

        public HelperProperties(params FunctionalProperty<HelperValueSet, HelperAppServiceResponse>[] inProperties) : base(inProperties) { }

        protected override async Task OnBeforeGetInitialStateAsync()
        {
            await Program.PrepareInitialStateAsync();
        }

        public override StateSnapshotDTO CreateStateSnapshot()
        {
            Program.HasServedInitialSnapshot = true;
            var snapshot = base.CreateStateSnapshot();

            if (!Program.IsAmdInitialized && snapshot?.Properties != null)
            {
                snapshot.Properties.RemoveAll(p => AmdAsyncFunctions.Contains(p.Function));
                Logger.Info("Skipped uninitialized AMD properties in StateSnapshotDTO to preserve widget cached state.");
            }

            return snapshot;
        }

        protected override Task<SharedAppServiceResponseStatus> SendResponse(HelperAppServiceRequest request, HelperValueSet response)
        {
            return request.SendResponseAsync(response).AsTask().ContinueWith(antecedentTask =>
            {
                return (SharedAppServiceResponseStatus)(int)antecedentTask.Result;
            }, TaskScheduler.Default);
        }
    }
}
