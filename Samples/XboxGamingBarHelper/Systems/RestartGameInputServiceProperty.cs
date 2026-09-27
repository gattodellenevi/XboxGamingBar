using Shared.Enums;
using System;
using System.Threading.Tasks;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Systems
{
    internal class RestartGameInputServiceProperty : HelperProperty<bool, SystemManager>
    {
        public RestartGameInputServiceProperty(SystemManager inManager) : base(false, null, Function.RestartGameInputService, inManager)
        {
        }

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            Logger.Info($"RestartGameInputServiceProperty SetValue called with: {newValue}");

            bool shouldRestart = false;
            if (newValue is bool b)
            {
                shouldRestart = b;
            }
            else if (newValue is string s && bool.TryParse(s, out var parsed))
            {
                shouldRestart = parsed;
            }

            if (shouldRestart)
            {
                Task.Run(() =>
                {
                    try
                    {
                        bool success = GameInputManager.RestartService();
                        Logger.Info($"GameInput restart completed. Success: {success}");
                        int newStatus = GameInputManager.GetServiceStatus();
                        manager?.GameInputStatus?.SetValue(newStatus, DateTime.UtcNow.Ticks);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to execute GameInput service restart.");
                    }
                });
            }

            return base.SetValue(false, updatedTime);
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);
            if (Value)
            {
                Task.Run(() =>
                {
                    try
                    {
                        bool success = GameInputManager.RestartService();
                        Logger.Info($"GameInput restart from NotifyPropertyChanged completed. Success: {success}");
                        int newStatus = GameInputManager.GetServiceStatus();
                        manager?.GameInputStatus?.SetValue(newStatus, DateTime.UtcNow.Ticks);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to execute GameInput service restart from NotifyPropertyChanged.");
                    }
                });
                value = false;
            }
        }
    }
}
