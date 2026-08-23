using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.RTSS
{
    internal class OSDTextSizeProperty : HelperProperty<int, RTSSManager>
    {
        public OSDTextSizeProperty(int inValue, RTSSManager inManager) : base(inValue, null, Function.OSDTextSize, inManager)
        {
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);
            Manager?.SetTextSize(Value);
        }
    }
}
