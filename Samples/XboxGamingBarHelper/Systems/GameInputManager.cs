using NLog;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace XboxGamingBarHelper.Systems
{
    public static class GameInputManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public const string RedistServiceName = "GameInputRedistService";
        public const string NativeServiceName = "GameInputSvc";

        public const string RedistProcessName = "GameInputRedistService";
        public const string NativeProcessName = "GameInputSvc";

        /// <summary>
        /// Status codes:
        /// -1 = Not Installed
        ///  0 = Stopped
        ///  1 = Running & Healthy
        ///  2 = Desynchronized / Degraded (Service running in Session 0, but interactive Session worker is missing or hanging)
        /// </summary>
        public const int StatusNotInstalled = -1;
        public const int StatusStopped = 0;
        public const int StatusRunning = 1;
        public const int StatusDesync = 2;

        private static readonly object _restartSyncLock = new object();
        private static bool _isRestartInProgress = false;

        public static string GetInstalledServiceName()
        {
            bool redistExists = false;
            ServiceControllerStatus redistStatus = ServiceControllerStatus.Stopped;
            try
            {
                using (var sc = new ServiceController(RedistServiceName))
                {
                    redistStatus = sc.Status;
                    redistExists = true;
                }
            }
            catch { }

            bool nativeExists = false;
            ServiceControllerStatus nativeStatus = ServiceControllerStatus.Stopped;
            try
            {
                using (var sc = new ServiceController(NativeServiceName))
                {
                    nativeStatus = sc.Status;
                    nativeExists = true;
                }
            }
            catch { }

            if (redistExists && !nativeExists) return RedistServiceName;
            if (nativeExists && !redistExists) return NativeServiceName;
            if (!redistExists && !nativeExists) return null;

            // If both exist: prefer the one that is currently running
            if (redistStatus == ServiceControllerStatus.Running && nativeStatus != ServiceControllerStatus.Running)
                return RedistServiceName;
            if (nativeStatus == ServiceControllerStatus.Running && redistStatus != ServiceControllerStatus.Running)
                return NativeServiceName;

            // If both running or both stopped: default to RedistServiceName
            return RedistServiceName;
        }

        public static int GetServiceStatus()
        {
            string serviceName = GetInstalledServiceName();
            if (serviceName == null)
            {
                return StatusNotInstalled;
            }

            try
            {
                using (var sc = new ServiceController(serviceName))
                {
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        return StatusStopped;
                    }
                }

                // Verify interactive session worker
                int currentSessionId = Process.GetCurrentProcess().SessionId;
                string targetProcName = (serviceName == RedistServiceName) ? RedistProcessName : NativeProcessName;

                var sessionProcesses = Process.GetProcessesByName(targetProcName)
                    .Where(p => p.SessionId == currentSessionId)
                    .ToList();

                if (sessionProcesses.Count == 0)
                {
                    // Service running in Session 0, but no interactive worker running in user session!
                    return StatusDesync;
                }

                foreach (var proc in sessionProcesses)
                {
                    if (!proc.Responding)
                    {
                        return StatusDesync;
                    }
                }

                return StatusRunning;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to query GameInput service status.");
                return StatusStopped;
            }
        }

        public static void TerminateSessionWorkers(string serviceName)
        {
            try
            {
                int currentSessionId = Process.GetCurrentProcess().SessionId;
                string targetProcName = (serviceName == RedistServiceName) ? RedistProcessName : NativeProcessName;

                var targetNames = new[] { targetProcName, RedistProcessName, NativeProcessName }.Distinct();

                foreach (var name in targetNames)
                {
                    foreach (var p in Process.GetProcessesByName(name))
                    {
                        if (p.SessionId == currentSessionId)
                        {
                            try
                            {
                                Logger.Info($"Killing lingering session worker process {p.ProcessName} (PID {p.Id}, Session {p.SessionId})...");
                                p.Kill();
                                p.WaitForExit(2000);
                            }
                            catch (Exception killEx)
                            {
                                Logger.Warn(killEx, $"Could not terminate lingering process PID {p.Id}.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Error while checking for lingering session worker processes.");
            }
        }

        public static bool RestartService()
        {
            lock (_restartSyncLock)
            {
                if (_isRestartInProgress)
                {
                    Logger.Warn("GameInput service restart is already in progress. Ignoring duplicate request.");
                    return false;
                }
                _isRestartInProgress = true;
            }

            try
            {
                if (ElevationManager.IsElevated())
                {
                    Logger.Info("Helper is running elevated. Performing in-process GameInput service restart...");
                    return RestartServiceInternal();
                }
                else
                {
                    Logger.Info("Helper is running without elevation. Requesting elevated GameInput service restart via UAC...");
                    return RestartServiceElevated();
                }
            }
            finally
            {
                lock (_restartSyncLock)
                {
                    _isRestartInProgress = false;
                }
            }
        }

        private static bool RestartServiceInternal()
        {
            string serviceName = GetInstalledServiceName();
            if (serviceName == null)
            {
                Logger.Warn("Cannot restart GameInput service: no GameInput service found.");
                return false;
            }

            Logger.Info($"Initiating in-process restart for GameInput service '{serviceName}'...");

            try
            {
                using (var sc = new ServiceController(serviceName))
                {
                    if (sc.Status != ServiceControllerStatus.Stopped && sc.Status != ServiceControllerStatus.StopPending)
                    {
                        Logger.Info($"Stopping {serviceName}...");
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(6));
                        Logger.Info($"{serviceName} stopped successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Failed to cleanly stop {serviceName} via ServiceController.");
            }

            // Terminate any hanging/lingering interactive session worker processes
            TerminateSessionWorkers(serviceName);

            // Start the service
            try
            {
                using (var sc = new ServiceController(serviceName))
                {
                    Logger.Info($"Starting {serviceName}...");
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(8));
                    Logger.Info($"{serviceName} started successfully.");
                }

                // Allow 1000ms for Session 0 service to launch the interactive /session agent
                Task.Delay(1000).Wait();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to start {serviceName}. Ensure helper is running with administrative privileges.");
                return false;
            }
        }

        private static bool RestartServiceElevated()
        {
            string serviceName = GetInstalledServiceName();
            if (serviceName == null)
            {
                Logger.Warn("Cannot restart GameInput service elevated: no GameInput service found.");
                return false;
            }

            Logger.Info($"Spawning elevated process to restart GameInput service '{serviceName}'...");

            // Terminate any hanging interactive session worker in our current session first
            TerminateSessionWorkers(serviceName);

            // Attempt PowerShell with UAC
            bool success = RestartViaPowerShellElevated(serviceName);

            // Allow 1000ms for Session 0 service to launch the interactive session worker
            Task.Delay(1000).Wait();

            return success;
        }

        private static bool RestartViaPowerShellElevated(string serviceName)
        {
            string script =
                $"Stop-Service -Name '{serviceName}' -Force -ErrorAction SilentlyContinue; " +
                $"Start-Sleep -Milliseconds 500; " +
                $"Get-Process -Name '{RedistProcessName}','{NativeProcessName}' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue; " +
                $"Start-Sleep -Milliseconds 500; " +
                $"Start-Service -Name '{serviceName}'";

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -Command \"{script}\"",
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            try
            {
                Logger.Info($"Spawning elevated PowerShell for '{serviceName}' restart (triggering UAC prompt)...");
                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        bool finished = process.WaitForExit(30000);
                        if (!finished)
                        {
                            Logger.Warn("Elevated PowerShell restart process timed out after 30 seconds.");
                            try { process.Kill(); } catch { }
                            return false;
                        }

                        Logger.Info($"Elevated PowerShell process completed with exit code {process.ExitCode}.");
                        return process.ExitCode == 0;
                    }
                }
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // NativeErrorCode 1223 = ERROR_CANCELLED (User dismissed or clicked 'No' on UAC prompt)
                Logger.Warn("User declined or cancelled the UAC elevation prompt for GameInput service restart.");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error launching elevated PowerShell for GameInput restart. Attempting cmd.exe fallback...");
                return RestartViaCmdElevated(serviceName);
            }

            return false;
        }

        private static bool RestartViaCmdElevated(string serviceName)
        {
            string cmdArgs = $"/c \"net stop \"{serviceName}\" & timeout /t 1 /nobreak >nul & taskkill /F /IM \"{RedistProcessName}.exe\" /IM \"{NativeProcessName}.exe\" >nul 2>&1 & net start \"{serviceName}\"\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = cmdArgs,
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            try
            {
                Logger.Info($"Spawning elevated cmd.exe fallback for '{serviceName}' restart (triggering UAC prompt)...");
                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        bool finished = process.WaitForExit(30000);
                        if (!finished)
                        {
                            Logger.Warn("Elevated cmd.exe process timed out after 30 seconds.");
                            try { process.Kill(); } catch { }
                            return false;
                        }

                        Logger.Info($"Elevated cmd.exe process completed with exit code {process.ExitCode}.");
                        return process.ExitCode == 0;
                    }
                }
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                Logger.Warn("User declined or cancelled the UAC elevation prompt for cmd fallback.");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error launching elevated cmd.exe for GameInput service restart.");
                return false;
            }

            return false;
        }
    }
}
