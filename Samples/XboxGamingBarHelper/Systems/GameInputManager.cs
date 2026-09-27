using NLog;
using System;
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

        public static string GetInstalledServiceName()
        {
            try
            {
                using (var sc = new ServiceController(RedistServiceName))
                {
                    var _ = sc.Status;
                    return RedistServiceName;
                }
            }
            catch
            {
                try
                {
                    using (var sc = new ServiceController(NativeServiceName))
                    {
                        var _ = sc.Status;
                        return NativeServiceName;
                    }
                }
                catch
                {
                    return null;
                }
            }
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

        public static bool RestartService()
        {
            string serviceName = GetInstalledServiceName();
            if (serviceName == null)
            {
                Logger.Warn("Cannot restart GameInput service: no GameInput service found.");
                return false;
            }

            Logger.Info($"Initiating restart for GameInput service '{serviceName}'...");

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
            try
            {
                int currentSessionId = Process.GetCurrentProcess().SessionId;
                string targetProcName = (serviceName == RedistServiceName) ? RedistProcessName : NativeProcessName;

                foreach (var p in Process.GetProcessesByName(targetProcName))
                {
                    if (p.SessionId == currentSessionId)
                    {
                        try
                        {
                            Logger.Info($"Killing lingering session worker process {p.ProcessName} (PID {p.Id})...");
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
            catch (Exception ex)
            {
                Logger.Warn(ex, "Error while checking for lingering session worker processes.");
            }

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

                // Allow 500ms for Session 0 service to launch the interactive /session agent
                Task.Delay(500).Wait();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to start {serviceName}. Ensure helper is running with administrative privileges.");
                return false;
            }
        }
    }
}
