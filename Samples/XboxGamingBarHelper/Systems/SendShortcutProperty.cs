using NLog;
using Shared.Data;
using Shared.Enums;
using System;
using XboxGamingBarHelper.Core;
using XboxGamingBarHelper.Windows;

namespace XboxGamingBarHelper.Systems
{
    internal class SendShortcutProperty : HelperProperty<string, SystemManager>
    {
        public SendShortcutProperty(SystemManager inManager) : base(string.Empty, null, Function.SendShortcut, inManager)
        {
        }

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            var payload = newValue?.ToString();
            Logger.Info($"SendShortcutProperty SetValue received payload: {payload}");

            if (!string.IsNullOrWhiteSpace(payload))
            {
                if (ShortcutItem.TryParsePayload(payload, out bool ctrl, out bool alt, out bool shift, out bool win, out int virtualKey))
                {
                    InputSimulator.SendKeyCombo(ctrl, alt, shift, win, virtualKey);
                }
                else
                {
                    Logger.Warn($"Failed to parse shortcut payload: {payload}");
                }
            }

            return base.SetValue(string.Empty, updatedTime);
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);
            if (!string.IsNullOrWhiteSpace(Value))
            {
                if (ShortcutItem.TryParsePayload(Value, out bool ctrl, out bool alt, out bool shift, out bool win, out int virtualKey))
                {
                    InputSimulator.SendKeyCombo(ctrl, alt, shift, win, virtualKey);
                }
                value = string.Empty;
            }
        }
    }
}
