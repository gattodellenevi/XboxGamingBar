using NLog;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows.Forms;

namespace XboxGamingBarHelper.Systems
{
    public static class ElevationManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static bool IsElevated()
        {
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to check process elevation.");
                return false;
            }
        }

        public static void RestartElevated()
        {
            if (IsElevated())
            {
                Logger.Info("Helper process is already running with elevated Administrator privileges.");
                return;
            }

            Logger.Info("Requesting helper process to restart with elevated Administrator privileges.");

            string exePath = null;
            try
            {
                exePath = Process.GetCurrentProcess().MainModule?.FileName;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not determine MainModule filename for RestartElevated.");
            }

            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CouchGamingBarHelper.exe");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Verb = "runas",
                UseShellExecute = true
            };

            try
            {
                Logger.Info($"Launching elevated instance of '{exePath}'...");
                Process.Start(startInfo);
                Logger.Info("Elevated instance launched successfully. Exiting current standard-privilege helper instance...");

                // Terminate current process so the newly spawned elevated instance acquires the single-instance mutex cleanly
                Application.Exit();
                Environment.Exit(0);
            }
            catch (Win32Exception ex)
            {
                // NativeErrorCode 1223 = ERROR_CANCELLED (user clicked 'No' or Cancel on UAC)
                Logger.Warn(ex, "User cancelled or denied the UAC elevation prompt (error code 1223). Remaining in standard-privilege mode.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to start elevated helper process.");
            }
        }
    }
}
