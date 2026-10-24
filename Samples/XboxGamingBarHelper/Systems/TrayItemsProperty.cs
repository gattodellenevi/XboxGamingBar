using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Systems
{
    internal class TrayItemsProperty : HelperProperty<string, SystemManager>
    {
        public TrayItemsProperty(SystemManager inManager) : base("[]", null, Function.TrayItems, inManager)
        {
        }

        public override bool ShouldSync() => false;
        protected override bool ShouldSendNotifyMessage() => false;

        public override HelperValueSet AddValueSetContent(in HelperValueSet inValueSet)
        {
            // On-demand fetch whenever requested by widget
            value = TrayInspectionManager.GetTrayItemsJson();
            return base.AddValueSetContent(inValueSet);
        }

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            // Trigger refresh on demand
            string json = TrayInspectionManager.GetTrayItemsJson();
            return base.SetValue(json, updatedTime);
        }
    }
}
