using Shared.Enums;
using System;
using System.Threading.Tasks;
using XboxGamingBarHelper.Core;
using XboxGamingBarHelper.Windows;

namespace XboxGamingBarHelper.Systems
{
    internal class VirtualKeyboardProperty : HelperProperty<bool, SystemManager>
    {
        public VirtualKeyboardProperty(SystemManager inManager) : base(false, null, Function.VirtualKeyboard, inManager)
        {
        }

        public override bool ShouldSync() => false;

        protected override bool ShouldSendNotifyMessage() => false;

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            Logger.Info($"VirtualKeyboardProperty SetValue called with: {newValue}");

            bool shouldTrigger = false;
            if (newValue is bool b)
            {
                shouldTrigger = b;
            }
            else if (newValue is string s && bool.TryParse(s, out var parsed))
            {
                shouldTrigger = parsed;
            }

            if (shouldTrigger)
            {
                Task.Run(() =>
                {
                    try
                    {
                        bool success = VirtualKeyboardHelper.Toggle();
                        Logger.Info($"VirtualKeyboard toggle completed. Success: {success}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to toggle virtual keyboard.");
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
                        bool success = VirtualKeyboardHelper.Toggle();
                        Logger.Info($"VirtualKeyboard toggle from NotifyPropertyChanged completed. Success: {success}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to toggle virtual keyboard from NotifyPropertyChanged.");
                    }
                });
                value = false;
            }
        }
    }
}
