using NLog;
using Shared.Constants;
using System;
using System.Threading;

namespace Shared.Utilities
{
    public static class WidgetSignalHelper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Signals the global cross-process event indicating the Game Bar widget is active and awaiting connection.
        /// </summary>
        /// <returns>True if the event was found and signaled; false otherwise.</returns>
        public static bool SignalWidgetActive()
        {
            try
            {
                if (EventWaitHandle.TryOpenExisting(StringConstants.WIDGET_ACTIVE_EVENT_NAME, out var waitHandle))
                {
                    using (waitHandle)
                    {
                        waitHandle.Set();
                        Logger.Info($"Signaled {StringConstants.WIDGET_ACTIVE_EVENT_NAME} successfully.");
                        return true;
                    }
                }
                else
                {
                    Logger.Debug($"Could not open {StringConstants.WIDGET_ACTIVE_EVENT_NAME}: event does not exist yet.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Failed to signal {StringConstants.WIDGET_ACTIVE_EVENT_NAME}: {ex.Message}");
                return false;
            }
        }
    }
}
