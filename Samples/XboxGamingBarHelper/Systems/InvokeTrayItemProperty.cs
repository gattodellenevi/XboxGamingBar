using Shared.Data;
using Shared.Enums;
using System;
using System.Text.Json;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Systems
{
    internal class InvokeTrayItemProperty : HelperProperty<string, SystemManager>
    {
        public InvokeTrayItemProperty(SystemManager inManager) : base(string.Empty, null, Function.InvokeTrayItem, inManager)
        {
        }

        public override bool ShouldSync() => false;
        protected override bool ShouldSendNotifyMessage() => false;

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            var payload = newValue?.ToString();
            Logger.Info($"InvokeTrayItemProperty received action payload: {payload}");

            if (!string.IsNullOrWhiteSpace(payload))
            {
                try
                {
                    var request = JsonSerializer.Deserialize<TrayActionRequest>(payload);
                    if (request != null)
                    {
                        TrayInspectionManager.ExecuteAction(request);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to deserialize and execute tray action: {payload}");
                }
            }

            return base.SetValue(string.Empty, updatedTime);
        }
    }
}
