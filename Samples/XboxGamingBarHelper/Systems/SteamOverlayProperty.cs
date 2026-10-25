using Shared.Enums;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using XboxGamingBarHelper.Core;
using XboxGamingBarHelper.Windows;

namespace XboxGamingBarHelper.Systems
{
    internal class SteamOverlayProperty : HelperProperty<bool, SystemManager>
    {
        public SteamOverlayProperty(SystemManager inManager) : base(false, null, Function.SteamOverlay, inManager)
        {
        }

        public override bool ShouldSync() => false;

        protected override bool ShouldSendNotifyMessage() => false;

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            Logger.Info($"SteamOverlayProperty SetValue called with: {newValue}");

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
                TriggerSteamOverlay();
            }

            return base.SetValue(false, updatedTime);
        }

        protected override void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);
            if (Value)
            {
                TriggerSteamOverlay();
                value = false;
            }
        }

        private void TriggerSteamOverlay()
        {
            Task.Run(async () =>
            {
                try
                {
                    // 1. Check if Steam is running
                    var steamProcesses = Process.GetProcessesByName("steam");
                    if (steamProcesses.Length == 0)
                    {
                        Logger.Warn("Cannot trigger Steam Overlay: steam.exe is not running.");
                        return;
                    }

                    // 2. Find target game window
                    IntPtr targetHwnd = IntPtr.Zero;
                    if (Manager != null && Manager.RunningGame.Value.IsValid())
                    {
                        int pid = Manager.RunningGame.Value.ProcessId;
                        if (Manager.ProcessWindows.TryGetValue(pid, out var procWin) && procWin.WindowHandle != IntPtr.Zero)
                        {
                            targetHwnd = procWin.WindowHandle;
                        }
                        else
                        {
                            try
                            {
                                var proc = Process.GetProcessById(pid);
                                targetHwnd = proc.MainWindowHandle;
                            }
                            catch { }
                        }
                    }

                    // If not found in RunningGame, scan ProcessWindows for a game process from Steam
                    if (targetHwnd == IntPtr.Zero && Manager != null && Manager.ProcessWindows != null)
                    {
                        foreach (var kvp in Manager.ProcessWindows)
                        {
                            var win = kvp.Value;
                            if (win.WindowHandle != IntPtr.Zero &&
                                !string.IsNullOrEmpty(win.Path) &&
                                win.Path.IndexOf("steamapps", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                !win.ProcessName.Equals("steam", StringComparison.OrdinalIgnoreCase) &&
                                !win.ProcessName.StartsWith("gameoverlay", StringComparison.OrdinalIgnoreCase))
                            {
                                targetHwnd = win.WindowHandle;
                                Logger.Info($"Resolved Steam game window from open windows: \"{win.Title}\" (PID {win.ProcessId})");
                                break;
                            }
                        }
                    }

                    // 3. Send Steam Overlay shortcut (Shift+Tab with hardware scan codes)
                    InputSimulator.SendSteamOverlayShortcut(targetHwnd);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to trigger Steam Overlay.");
                }
            });
        }
    }
}
