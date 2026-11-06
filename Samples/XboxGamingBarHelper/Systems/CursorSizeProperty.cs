using Shared.Enums;
using XboxGamingBarHelper.Core;
using XboxGamingBarHelper.Windows;

namespace XboxGamingBarHelper.Systems
{
    internal class CursorSizeProperty : HelperProperty<int, SystemManager>
    {
        public CursorSizeProperty(int inValue, SystemManager inManager) : base(inValue, null, Function.CursorSize, inManager)
        {
        }

        public override (object Content, long UpdatedTime) GetSnapshotData()
        {
            var liveSize = User32.GetCursorSize();
            if (liveSize != value)
            {
                value = liveSize;
            }
            return base.GetSnapshotData();
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            var currentSize = User32.GetCursorSize();
            if (currentSize != Value)
            {
                User32.SetCursorSize(Value);
            }
        }
    }
}
