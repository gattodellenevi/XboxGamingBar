using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Data
{
    public class ShortcutItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string IconGlyph { get; set; } = "\uE765"; // Default key glyph
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public bool Win { get; set; }
        public int VirtualKey { get; set; } // Virtual Key code (e.g. 123 for F12)

        public ShortcutItem()
        {
        }

        public ShortcutItem(string name, string iconGlyph, bool ctrl, bool alt, bool shift, bool win, int virtualKey)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            IconGlyph = string.IsNullOrWhiteSpace(iconGlyph) ? "\uE765" : iconGlyph;
            Ctrl = ctrl;
            Alt = alt;
            Shift = shift;
            Win = win;
            VirtualKey = virtualKey;
        }

        public string KeyName => GetKeyDisplayName(VirtualKey);

        public string DisplayKeyCombo
        {
            get
            {
                var parts = new List<string>();
                if (Win) parts.Add("Win");
                if (Ctrl) parts.Add("Ctrl");
                if (Alt) parts.Add("Alt");
                if (Shift) parts.Add("Shift");

                string keyName = KeyName;
                if (!string.IsNullOrEmpty(keyName))
                {
                    parts.Add(keyName);
                }

                return parts.Count > 0 ? string.Join(" + ", parts) : "None";
            }
        }

        /// <summary>
        /// Payload formatted as "Ctrl|Alt|Shift|Win|VirtualKey" sent via AppService
        /// </summary>
        public string ToPayloadString()
        {
            return $"{(Ctrl ? 1 : 0)}|{(Alt ? 1 : 0)}|{(Shift ? 1 : 0)}|{(Win ? 1 : 0)}|{VirtualKey}";
        }

        public static bool TryParsePayload(string payload, out bool ctrl, out bool alt, out bool shift, out bool win, out int virtualKey)
        {
            ctrl = false;
            alt = false;
            shift = false;
            win = false;
            virtualKey = 0;

            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            var parts = payload.Split('|');
            if (parts.Length >= 5)
            {
                ctrl = parts[0] == "1";
                alt = parts[1] == "1";
                shift = parts[2] == "1";
                win = parts[3] == "1";
                if (int.TryParse(parts[4], out int vk))
                {
                    virtualKey = vk;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Serializes the item to a pipe-delimited string for easy storage in LocalSettings.
        /// Format: Id|Name|IconGlyph|Ctrl|Alt|Shift|Win|VirtualKey
        /// </summary>
        public string ToStorageString()
        {
            string safeName = (Name ?? string.Empty).Replace("|", "/");
            string safeIcon = (IconGlyph ?? "\uE765").Replace("|", "/");
            return $"{Id}|{safeName}|{safeIcon}|{(Ctrl ? 1 : 0)}|{(Alt ? 1 : 0)}|{(Shift ? 1 : 0)}|{(Win ? 1 : 0)}|{VirtualKey}";
        }

        public static ShortcutItem FromStorageString(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            var parts = line.Split('|');
            if (parts.Length >= 8)
            {
                if (int.TryParse(parts[7], out int vk))
                {
                    return new ShortcutItem
                    {
                        Id = parts[0],
                        Name = parts[1],
                        IconGlyph = parts[2],
                        Ctrl = parts[3] == "1",
                        Alt = parts[4] == "1",
                        Shift = parts[5] == "1",
                        Win = parts[6] == "1",
                        VirtualKey = vk
                    };
                }
            }

            return null;
        }

        public static string GetKeyDisplayName(int vk)
        {
            if (vk >= 112 && vk <= 135) // F1 - F24
            {
                return $"F{vk - 111}";
            }
            if (vk >= 65 && vk <= 90) // A - Z
            {
                return ((char)vk).ToString();
            }
            if (vk >= 48 && vk <= 57) // 0 - 9
            {
                return ((char)vk).ToString();
            }
            if (vk >= 96 && vk <= 105) // Numpad 0 - 9
            {
                return $"Num {vk - 96}";
            }

            switch (vk)
            {
                case 8: return "Backspace";
                case 9: return "Tab";
                case 13: return "Enter";
                case 19: return "Pause";
                case 20: return "CapsLock";
                case 27: return "Esc";
                case 32: return "Space";
                case 33: return "PageUp";
                case 34: return "PageDown";
                case 35: return "End";
                case 36: return "Home";
                case 37: return "Left";
                case 38: return "Up";
                case 39: return "Right";
                case 40: return "Down";
                case 44: return "PrtScn";
                case 45: return "Insert";
                case 46: return "Delete";
                case 106: return "Num *";
                case 107: return "Num +";
                case 109: return "Num -";
                case 110: return "Num .";
                case 111: return "Num /";
                case 144: return "NumLock";
                case 145: return "ScrollLock";
                case 173: return "Mute";
                case 174: return "Vol-";
                case 175: return "Vol+";
                case 176: return "Next";
                case 177: return "Prev";
                case 178: return "Stop";
                case 179: return "Play/Pause";
                case 186: return ";";
                case 187: return "=";
                case 188: return ",";
                case 189: return "-";
                case 190: return ".";
                case 191: return "/";
                case 192: return "`";
                case 219: return "[";
                case 220: return "\\";
                case 221: return "]";
                case 222: return "'";
                default:
                    return vk > 0 ? $"Key({vk})" : "";
            }
        }
    }
}
