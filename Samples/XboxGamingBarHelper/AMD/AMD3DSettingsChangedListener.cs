using NLog;
//using System;

namespace XboxGamingBarHelper.AMD
{
    internal class AMD3DSettingsChangedListener : IADLX3DSettingsChangedListener
    {
        protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected AMDManager amdManager;

        internal AMD3DSettingsChangedListener(AMDManager inAMDManager) : base()
        {
            amdManager = inAMDManager;
        }

        public override bool On3DSettingsChanged(IADLX3DSettingsChangedEvent p3DSettingsChangedEvent)
        {
         

            if (p3DSettingsChangedEvent.IsAntiLagChanged())
            {
                var isEnabled = amdManager.AMDRadeonAntiLagSetting.IsEnabled();
                if (amdManager.AMDRadeonAntiLagEnabled != isEnabled)
                {
                    amdManager.AMDRadeonAntiLagEnabled.SetValue(isEnabled);
                }
            }

            if (p3DSettingsChangedEvent.IsChillChanged())
            {
                var isEnabled = amdManager.AMDRadeonChillSetting.IsEnabled();
                if (amdManager.AMDRadeonChillEnabled != isEnabled)
                {
                    amdManager.AMDRadeonChillEnabled.SetValue(isEnabled);
                }
            }

            if (p3DSettingsChangedEvent.IsRadeonSuperResolutionChanged())
            {
                var isEnabled = amdManager.AMDRadeonSuperResolutionSetting.IsEnabled();
                if (amdManager.AMDRadeonSuperResolutionEnabled != isEnabled)
                {
                    amdManager.AMDRadeonSuperResolutionEnabled.SetValue(isEnabled);
                }
            }

            if (p3DSettingsChangedEvent.IsBoostChanged())
            {
                var isEnabled = amdManager.AMDRadeonBoostSetting.IsEnabled();
                if (amdManager.AMDRadeonBoostEnabled != isEnabled)
                {
                    amdManager.AMDRadeonBoostEnabled.SetValue(isEnabled);
                }
            }

            var isAMDFluidMotionFramesEnabled = amdManager.AMDFluidMotionFrameSetting.IsEnabled();
            if (amdManager.AMDFluidMotionFrameEnabled != isAMDFluidMotionFramesEnabled)
            {
                amdManager.AMDFluidMotionFrameEnabled.SetValue(isAMDFluidMotionFramesEnabled);
            }

            return true;
        }
    }
}
