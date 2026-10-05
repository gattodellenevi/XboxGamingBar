using Shared.Enums;
using XboxGamingBarHelper.Core;
using XboxGamingBarHelper.Windows;

namespace XboxGamingBarHelper.Systems
{
    internal class MouseScrollSpeedProperty : HelperProperty<int, SystemManager>
    {
        public MouseScrollSpeedProperty(int inValue, SystemManager inManager) : base(inValue, null, Function.MouseScrollSpeed, inManager)
        {
        }

        public override (object Content, long UpdatedTime) GetSnapshotData()
        {
            var liveSpeed = User32.GetMouseScrollSpeed();
            if (liveSpeed != value)
            {
                value = liveSpeed;
            }
            return base.GetSnapshotData();
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            var currentSpeed = User32.GetMouseScrollSpeed();
            if (currentSpeed != Value)
            {
                User32.SetMouseScrollSpeed(Value);
            }
        }
    }
}
