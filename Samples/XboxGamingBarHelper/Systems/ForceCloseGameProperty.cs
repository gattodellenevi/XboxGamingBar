using Shared.Enums;
using System;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Systems
{
    internal class ForceCloseGameProperty : HelperProperty<bool, SystemManager>
    {
        public ForceCloseGameProperty(SystemManager inManager) : base(false, null, Function.ForceCloseGame, inManager)
        {
        }

        public override bool ShouldSync() => false;

        protected override bool ShouldSendNotifyMessage() => false;

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            Logger.Info($"ForceCloseGameProperty SetValue called with: {newValue}");

            bool shouldTrigger = false;
            if (newValue is bool b)
            {
                shouldTrigger = b;
            }
            else if (newValue is string s && bool.TryParse(s, out var parsed))
            {
                shouldTrigger = parsed;
            }

            if (shouldTrigger)
            {
                Manager?.ForceCloseRunningGame();
            }

            return base.SetValue(false, updatedTime);
        }
    }
}
