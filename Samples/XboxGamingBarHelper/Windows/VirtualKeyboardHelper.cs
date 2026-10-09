using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace XboxGamingBarHelper.Windows
{
    internal static class VirtualKeyboardHelper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [ComImport, Guid("4ce576fa-83dc-4F88-951c-9d0782b4e376")]
        private class UIHostNoLaunch { }

        [ComImport, Guid("37c994e7-432b-4834-a2f7-dce1f13b834b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITipInvocation
        {
            void Toggle(IntPtr hwnd);
        }

        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr GetDesktopWindow();

        public static bool Toggle()
        {
            Logger.Info("Attempting to toggle Windows Virtual Keyboard...");

            // Method 1: Try modern Windows Touch Keyboard via COM ITipInvocation (Windows 10 / 11)
            try
            {
                var uiHostNoLaunch = new UIHostNoLaunch();
                var tipInvocation = (ITipInvocation)uiHostNoLaunch;
                tipInvocation.Toggle(GetDesktopWindow());
                Marshal.ReleaseComObject(uiHostNoLaunch);
                Logger.Info("Successfully invoked ITipInvocation.Toggle.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "ITipInvocation COM approach failed, attempting TabTip.exe launch fallback.");
            }

            // Method 2: Launch TabTip.exe directly
            try
            {
                string tabTipPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
                    @"microsoft shared\ink\TabTip.exe");

                if (File.Exists(tabTipPath))
                {
                    Logger.Info($"Found TabTip.exe at '{tabTipPath}'. Starting process...");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = tabTipPath,
                        UseShellExecute = true
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to launch TabTip.exe, attempting Win+Ctrl+O shortcut fallback.");
            }

            // Method 3: Simulate Win + Ctrl + O hotkey (Windows native toggle for On-Screen Keyboard)
            try
            {
                Logger.Info("Simulating Win + Ctrl + O hotkey for On-Screen Keyboard...");
                InputSimulator.SendKeyCombo(ctrl: true, alt: false, shift: false, win: true, virtualKey: 0x4F); // 0x4F = 'O'
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to send Win + Ctrl + O hotkey, attempting osk.exe process fallback.");
            }

            // Method 4: Launch osk.exe directly
            try
            {
                Logger.Info("Attempting to launch osk.exe...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = "osk.exe",
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "All virtual keyboard invocation methods failed.");
                return false;
            }
        }
    }
}
