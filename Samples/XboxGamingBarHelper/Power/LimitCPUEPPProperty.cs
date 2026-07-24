using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Power
{
    internal class LimitCPUEPPProperty : HelperProperty<bool, PowerManager>
    {
        public LimitCPUEPPProperty(bool inValue, PowerManager inManager) : base(inValue, null, Function.LimitCPUEPP, inManager)
        {
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            Logger.Info($"{(Value ? "Enable" : "Disable")} CPU EPP limit.");
            if (Value)
            {
                PowerManager.SetEppValue(false, (uint)Manager.CPUEPP.Value);
                PowerManager.SetEppValue(true, (uint)Manager.CPUEPP.Value);
            }
            else
            {
                PowerManager.RestoreDefaultEpp();
            }
        }
    }
}
