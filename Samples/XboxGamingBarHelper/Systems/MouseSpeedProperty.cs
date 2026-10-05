using Shared.Enums;
using XboxGamingBarHelper.Core;
using XboxGamingBarHelper.Windows;

namespace XboxGamingBarHelper.Systems
{
    internal class MouseSpeedProperty : HelperProperty<int, SystemManager>
    {
        public MouseSpeedProperty(int inValue, SystemManager inManager) : base(inValue, null, Function.MouseSpeed, inManager)
        {
        }

        public override (object Content, long UpdatedTime) GetSnapshotData()
        {
            var liveSpeed = User32.GetMouseSpeed();
            if (liveSpeed != value)
            {
                value = liveSpeed;
            }
            return base.GetSnapshotData();
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            var currentSpeed = User32.GetMouseSpeed();
            if (currentSpeed != Value)
            {
                User32.SetMouseSpeed(Value);
            }
        }
    }
}
