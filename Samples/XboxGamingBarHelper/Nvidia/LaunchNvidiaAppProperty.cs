using NLog;
using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Nvidia
{
    internal class LaunchNvidiaAppProperty : HelperProperty<bool, NvidiaManager>
    {
        public LaunchNvidiaAppProperty(bool inValue, NvidiaManager inManager) 
            : base(inValue, null, Function.LaunchNvidiaApp, inManager)
        {
        }

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            Logger.Info($"LaunchNvidiaAppProperty SetValue called with newValue: {newValue}");
            if (Manager != null)
            {
                Manager.StartNvidiaApp();
            }
            return base.SetValue(false, updatedTime);
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            if (Value && Manager != null)
            {
                Manager.StartNvidiaApp();
                value = false;
            }
        }
    }
}
