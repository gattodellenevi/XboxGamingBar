using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Systems
{
    internal class GameInputStatusProperty : HelperProperty<int, SystemManager>
    {
        public GameInputStatusProperty(int inValue, SystemManager inManager) : base(inValue, null, Function.GameInputStatus, inManager)
        {
        }
    }
}
