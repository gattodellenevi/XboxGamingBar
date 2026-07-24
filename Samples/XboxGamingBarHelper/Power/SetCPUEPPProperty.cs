using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Power
{
    internal class SetCPUEPPProperty : HelperProperty<bool, PowerManager>
    {
        public SetCPUEPPProperty(bool inValue, PowerManager inManager) : base(inValue, null, Function.SetCPUEPP, inManager)
        {
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            Logger.Info($"{(Value ? "Enable" : "Disable")} CPU EPP value override.");
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
