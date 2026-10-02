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

            // If running inside WindowsApps, stage to LocalAppData to bypass TrustedInstaller ACL execution isolation
            if (exePath.IndexOf("WindowsApps", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                try
                {
                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string targetDir = Path.Combine(localAppData, "CouchGamingBarHelper");
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    string sourceDir = Path.GetDirectoryName(exePath);
                    foreach (var file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
                    {
                        string rel = Path.GetRelativePath(sourceDir, file);
                        string dest = Path.Combine(targetDir, rel);
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        File.Copy(file, dest, true);
                    }
                    string stagedExe = Path.Combine(targetDir, Path.GetFileName(exePath));
                    if (File.Exists(stagedExe))
                    {
                        exePath = stagedExe;
                        Logger.Info($"Staged helper from WindowsApps to local folder: {exePath}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to stage helper to local folder, attempting to launch from source path.");
                }
            }

            // Release single-instance mutex so newly spawned elevated process can immediately acquire it without conflict
            Program.ReleaseSingleInstanceMutex();

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

                // Terminate current process so the newly spawned elevated instance takes over cleanly
                Application.Exit();
                Environment.Exit(0);
            }
            catch (Win32Exception ex)
            {
                // NativeErrorCode 1223 = ERROR_CANCELLED (user clicked 'No' or Cancel on UAC)
                Logger.Warn(ex, "User cancelled or denied the UAC elevation prompt (error code 1223). Remaining in standard-privilege mode.");
                // Re-acquire single-instance mutex to maintain standard helper operation
                bool createdNew;
                Program.CreateSingleInstanceMutex(out createdNew);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to start elevated helper process.");
                bool createdNew;
                Program.CreateSingleInstanceMutex(out createdNew);
            }
        }
    }
}
