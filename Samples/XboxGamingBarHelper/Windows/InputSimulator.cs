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

        private const ushort VK_SHIFT = 0x10;
        private const ushort VK_CONTROL = 0x11;
        private const ushort VK_MENU = 0x12; // Alt
        private const ushort VK_LWIN = 0x5B;

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

            return new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vk,
                        wScan = 0,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
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
