using Shared.Data;
using Shared.Enums;

namespace XboxGamingBar.Data
{
    internal class TrackedGameProperty : WidgetProperty<TrackedGame>
    {
        public TrackedGameProperty(TrackedGame inValue) : base(inValue, null, Function.TrackedGame)
        {
        }

        // Controlled by the widget only, no need to sync from helper.
        public override bool ShouldSync()
        {
            return false;
        }
    }
}
