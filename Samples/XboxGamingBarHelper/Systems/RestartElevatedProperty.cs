using Shared.Enums;
using System;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Systems
{
    internal class RestartElevatedProperty : HelperProperty<bool, SystemManager>
    {
        public RestartElevatedProperty(SystemManager inManager) : base(false, null, Function.RestartElevated, inManager)
        {
        }

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            Logger.Info($"RestartElevatedProperty SetValue called with: {newValue}");
            try
            {
                ElevationManager.RestartElevated();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to execute RestartElevated from SetValue.");
            }
            return base.SetValue(false, updatedTime);
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);
            if (Value)
            {
                try
                {
                    ElevationManager.RestartElevated();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to execute RestartElevated from NotifyPropertyChanged.");
                }
                value = false;
            }
        }
    }
}
