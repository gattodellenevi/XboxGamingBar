using NLog;
using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.AMD.Properties
{
    internal class LaunchAmdAppProperty : HelperProperty<bool, AMDManager>
    {
        public LaunchAmdAppProperty(bool inValue, AMDManager inManager) 
            : base(inValue, null, Function.LaunchAmdApp, inManager)
        {
        }

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            Logger.Info($"LaunchAmdAppProperty SetValue called with newValue: {newValue}");
            if (Manager != null)
            {
                Manager.StartAmdApp();
            }
            return base.SetValue(false, updatedTime);
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            if (Value && Manager != null)
            {
                Manager.StartAmdApp();
                value = false;
            }
        }
    }
}
