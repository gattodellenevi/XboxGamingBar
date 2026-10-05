using Shared.Enums;
using XboxGamingBarHelper.Core;
using XboxGamingBarHelper.Windows;

namespace XboxGamingBarHelper.Systems
{
    internal class MouseEnhancePointerPrecisionProperty : HelperProperty<bool, SystemManager>
    {
        public MouseEnhancePointerPrecisionProperty(bool inValue, SystemManager inManager) : base(inValue, null, Function.MouseEnhancePointerPrecision, inManager)
        {
        }

        public override (object Content, long UpdatedTime) GetSnapshotData()
        {
            var live = User32.GetEnhancePointerPrecision();
            if (live != value)
            {
                value = live;
            }
            return base.GetSnapshotData();
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);

            var current = User32.GetEnhancePointerPrecision();
            if (current != Value)
            {
                User32.SetEnhancePointerPrecision(Value);
            }
        }
    }
}
