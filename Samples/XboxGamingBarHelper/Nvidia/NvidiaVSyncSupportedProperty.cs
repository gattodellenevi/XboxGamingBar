using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Nvidia
{
    internal class NvidiaVSyncSupportedProperty : HelperProperty<bool, NvidiaManager>
    {
        public NvidiaVSyncSupportedProperty(bool inValue, NvidiaManager inManager)
            : base(inValue, null, Function.NvidiaVSyncSupported, inManager)
        {
        }
    }
}
