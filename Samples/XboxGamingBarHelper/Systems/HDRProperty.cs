using Shared.Enums;
using XboxGamingBarHelper.Core;
using XboxGamingBarHelper.Windows;

namespace XboxGamingBarHelper.Systems
{
    internal class HDRProperty : HelperProperty<bool, SystemManager>
    {
        public HDRProperty(bool inValue, SystemManager inManager) : base(inValue, null, Function.HDR, inManager)
        {
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            var (supported, enabled) = User32.GetHDRStatus();
            if (supported && enabled != Value)
            {
                User32.SetHDR(Value);
            }
        }
    }
}
