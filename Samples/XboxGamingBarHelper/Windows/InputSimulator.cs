using NLog;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace XboxGamingBarHelper.Windows
{
    internal static class InputSimulator
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;

        private const ushort VK_SHIFT = 0x10;
        private const ushort VK_CONTROL = 0x11;
        private const ushort VK_MENU = 0x12; // Alt
        private const ushort VK_LWIN = 0x5B;

        private const ushort SCANCODE_LSHIFT = 0x2A;
        private const ushort SCANCODE_TAB = 0x0F;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);

        private static INPUT CreateKeyInput(ushort vk, bool isKeyUp)
        {
            uint flags = 0;
            if (isKeyUp)
            {
                flags |= KEYEVENTF_KEYUP;
            }

            // Extended keys (Windows key, Right Alt, navigation keys, etc.)
            if (vk == VK_LWIN || vk == 0x5C || (vk >= 33 && vk <= 46))
            {
                flags |= KEYEVENTF_EXTENDEDKEY;
            }

            ushort scan = (ushort)User32.MapVirtualKey((uint)vk, User32.MAPVK_VK_TO_VSC);

            return new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vk,
                        wScan = scan,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        private static INPUT CreateScanCodeInput(ushort scanCode, bool isKeyUp, bool isExtended = false)
        {
            uint flags = KEYEVENTF_SCANCODE;
            if (isKeyUp)
            {
                flags |= KEYEVENTF_KEYUP;
            }
            if (isExtended)
            {
                flags |= KEYEVENTF_EXTENDEDKEY;
            }

            return new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = scanCode,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        public static void SendSteamOverlayShortcut(IntPtr targetHwnd = default)
        {
            Task.Run(async () =>
            {
                try
                {
                    Logger.Info($"Sending Steam Overlay shortcut: Dismissing Game Bar and targeting HWND: {targetHwnd}");

                    // 1. Dismiss Game Bar by toggling Win + G so the game window regains native foreground focus
                    User32.keybd_event(0x5B, 0x5B, 0, UIntPtr.Zero); // Win down
                    User32.keybd_event(0x47, 0x22, 0, UIntPtr.Zero); // G down
                    await Task.Delay(35);
                    User32.keybd_event(0x47, 0x22, 0x0002, UIntPtr.Zero); // G up
                    User32.keybd_event(0x5B, 0x5B, 0x0002, UIntPtr.Zero); // Win up

                    // 2. Allow Windows shell time to dismiss Game Bar and transfer focus
                    await Task.Delay(350);

                    // 3. Ensure target game window is brought to foreground
                    if (targetHwnd != IntPtr.Zero)
                    {
                        User32.ForceSetForegroundWindow(targetHwnd);
                        await Task.Delay(100);
                    }

                    // 4. Send Shift + Tab using keybd_event with BOTH VirtualKey and Hardware ScanCode
                    // Down: Shift (VK_SHIFT = 0x10, ScanCode = 0x2A), then Tab (VK_TAB = 0x09, ScanCode = 0x0F)
                    User32.keybd_event(0x10, 0x2A, 0, UIntPtr.Zero);
                    await Task.Delay(30);
                    User32.keybd_event(0x09, 0x0F, 0, UIntPtr.Zero);

                    // Hold for 75ms so game hook and frame pump register both keys
                    await Task.Delay(75);

                    // Up: Tab, then Shift
                    User32.keybd_event(0x09, 0x0F, 0x0002, UIntPtr.Zero);
                    await Task.Delay(30);
                    User32.keybd_event(0x10, 0x2A, 0x0002, UIntPtr.Zero);

                    Logger.Info("Steam Overlay shortcut (Win+G dismiss + Shift+Tab keybd_event) sent successfully.");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to send Steam Overlay shortcut.");
                }
            });
        }

        public static void SendKeyCombo(bool ctrl, bool alt, bool shift, bool win, int virtualKey)
        {
            Task.Run(async () =>
            {
                try
                {
                    Logger.Info($"Sending key combo: Ctrl={ctrl}, Alt={alt}, Shift={shift}, Win={win}, Vk={virtualKey}");

                    var downInputs = new List<INPUT>();

                    // 1. Modifiers DOWN
                    if (ctrl) downInputs.Add(CreateKeyInput(VK_CONTROL, false));
                    if (alt) downInputs.Add(CreateKeyInput(VK_MENU, false));
                    if (shift) downInputs.Add(CreateKeyInput(VK_SHIFT, false));
                    if (win) downInputs.Add(CreateKeyInput(VK_LWIN, false));

                    // 2. Primary key DOWN
                    if (virtualKey > 0)
                    {
                        downInputs.Add(CreateKeyInput((ushort)virtualKey, false));
                    }

                    if (downInputs.Count > 0)
                    {
                        uint sentDown = SendInput((uint)downInputs.Count, downInputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
                        Logger.Debug($"Sent {sentDown}/{downInputs.Count} key-down inputs.");
                    }

                    // Hold for 35ms so game / OS polling loops register the key state
                    await Task.Delay(35);

                    var upInputs = new List<INPUT>();

                    // 3. Primary key UP
                    if (virtualKey > 0)
                    {
                        upInputs.Add(CreateKeyInput((ushort)virtualKey, true));
                    }

                    // 4. Modifiers UP in reverse order
                    if (win) upInputs.Add(CreateKeyInput(VK_LWIN, true));
                    if (shift) upInputs.Add(CreateKeyInput(VK_SHIFT, true));
                    if (alt) upInputs.Add(CreateKeyInput(VK_MENU, true));
                    if (ctrl) upInputs.Add(CreateKeyInput(VK_CONTROL, true));

                    if (upInputs.Count > 0)
                    {
                        uint sentUp = SendInput((uint)upInputs.Count, upInputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
                        Logger.Debug($"Sent {sentUp}/{upInputs.Count} key-up inputs.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to simulate key combo.");
                }
            });
        }
    }
}
