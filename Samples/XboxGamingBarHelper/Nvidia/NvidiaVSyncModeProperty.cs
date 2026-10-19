using NLog;
using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Nvidia
{
    internal class NvidiaVSyncModeProperty : HelperProperty<int, NvidiaManager>
    {
        public NvidiaVSyncModeProperty(int inValue, NvidiaManager inManager)
            : base(inValue, null, Function.NvidiaVSyncMode, inManager)
        {
        }

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            if (newValue is int modeInt)
            {
                Logger.Info($"NvidiaVSyncModeProperty SetValue called with mode: {(NvidiaVSyncMode)modeInt}");
                if (Manager != null)
                {
                    Manager.SetVSyncMode((NvidiaVSyncMode)modeInt);
                }
                return base.SetValue(modeInt, updatedTime);
            }
            return false;
        }
    }
}
