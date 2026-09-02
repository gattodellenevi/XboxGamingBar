using NLog;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using Windows.System;

namespace XboxGamingBar.Data
{
    public class ShortcutPreset
    {
        public string Title { get; set; }
        public string Name { get; set; }
        public string IconGlyph { get; set; }
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public bool Win { get; set; }
        public int VirtualKey { get; set; }

        public override string ToString() => Title;
    }

    public class ShortcutIconOption
    {
        public string Name { get; set; }
        public string Glyph { get; set; }

        public override string ToString() => $"{Name}";
    }

    public class ShortcutKeyOption
    {
        public string Name { get; set; }
        public int VirtualKey { get; set; }

        public override string ToString() => Name;
    }

    public static class ShortcutManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private const string SETTINGS_KEY = "GlobalShortcuts_v1";

        public static List<ShortcutPreset> Presets { get; } = new List<ShortcutPreset>
        {
            new ShortcutPreset { Title = "(Custom)", Name = "", IconGlyph = "\uE765", Ctrl = false, Alt = false, Shift = false, Win = false, VirtualKey = 0 },
            new ShortcutPreset { Title = "Steam Screenshot (F12)", Name = "Screenshot", IconGlyph = "\uE722", Ctrl = false, Alt = false, Shift = false, Win = false, VirtualKey = (int)VirtualKey.F12 },
            new ShortcutPreset { Title = "Toggle Fullscreen (Alt + Enter)", Name = "Fullscreen", IconGlyph = "\uE740", Ctrl = false, Alt = true, Shift = false, Win = false, VirtualKey = (int)VirtualKey.Enter },
            new ShortcutPreset { Title = "Xbox Game Bar Record (Win + Alt + R)", Name = "Record", IconGlyph = "\uE714", Ctrl = false, Alt = true, Shift = false, Win = true, VirtualKey = (int)VirtualKey.R },
            new ShortcutPreset { Title = "Xbox Game Bar Screenshot (Win + Alt + PrtScn)", Name = "Capture", IconGlyph = "\uE722", Ctrl = false, Alt = true, Shift = false, Win = true, VirtualKey = (int)VirtualKey.Snapshot },
            new ShortcutPreset { Title = "Show Desktop (Win + D)", Name = "Desktop", IconGlyph = "\uE770", Ctrl = false, Alt = false, Shift = false, Win = true, VirtualKey = (int)VirtualKey.D },
            new ShortcutPreset { Title = "Task Manager (Ctrl + Shift + Esc)", Name = "Task Mgr", IconGlyph = "\uE9D2", Ctrl = true, Alt = false, Shift = true, Win = false, VirtualKey = (int)VirtualKey.Escape },
            new ShortcutPreset { Title = "NVIDIA ShadowPlay Overlay (Alt + Z)", Name = "GeForce", IconGlyph = "\uE7FC", Ctrl = false, Alt = true, Shift = false, Win = false, VirtualKey = (int)VirtualKey.Z },
            new ShortcutPreset { Title = "NVIDIA Save Replay (Alt + F10)", Name = "Save Clip", IconGlyph = "\uE714", Ctrl = false, Alt = true, Shift = false, Win = false, VirtualKey = (int)VirtualKey.F10 },
            new ShortcutPreset { Title = "AMD Adrenalin Overlay (Alt + R)", Name = "AMD Overlay", IconGlyph = "\uE7FC", Ctrl = false, Alt = true, Shift = false, Win = false, VirtualKey = (int)VirtualKey.R },
            new ShortcutPreset { Title = "AMD Instant Replay (Ctrl + Shift + S)", Name = "AMD Replay", IconGlyph = "\uE714", Ctrl = true, Alt = false, Shift = true, Win = false, VirtualKey = (int)VirtualKey.S },
            new ShortcutPreset { Title = "Discord Mute (Ctrl + Shift + M)", Name = "Mute Mic", IconGlyph = "\uE720", Ctrl = true, Alt = false, Shift = true, Win = false, VirtualKey = (int)VirtualKey.M },
            new ShortcutPreset { Title = "Mute Audio (Volume Mute)", Name = "Mute Audio", IconGlyph = "\uE74F", Ctrl = false, Alt = false, Shift = false, Win = false, VirtualKey = 173 },
        };

        public static List<ShortcutIconOption> Icons { get; } = new List<ShortcutIconOption>
        {
            new ShortcutIconOption { Name = "Camera (Screenshot)", Glyph = "\uE722" },
            new ShortcutIconOption { Name = "Video (Record)", Glyph = "\uE714" },
            new ShortcutIconOption { Name = "Gamepad (Overlay)", Glyph = "\uE7FC" },
            new ShortcutIconOption { Name = "Microphone (Mute)", Glyph = "\uE720" },
            new ShortcutIconOption { Name = "Screen (Fullscreen)", Glyph = "\uE740" },
            new ShortcutIconOption { Name = "Desktop (Show Desktop)", Glyph = "\uE770" },
            new ShortcutIconOption { Name = "Performance (Task Mgr)", Glyph = "\uE9D2" },
            new ShortcutIconOption { Name = "Key (Shortcut)", Glyph = "\uE765" },
            new ShortcutIconOption { Name = "Action (Flash)", Glyph = "\uE945" },
            new ShortcutIconOption { Name = "Star (Favorite)", Glyph = "\uE734" },
            new ShortcutIconOption { Name = "Gear (Settings)", Glyph = "\uE713" },
            new ShortcutIconOption { Name = "Speaker (Audio)", Glyph = "\uE767" },
            new ShortcutIconOption { Name = "Play (Media)", Glyph = "\uE768" },
            new ShortcutIconOption { Name = "Refresh (Reload)", Glyph = "\uE72C" }
        };

        public static List<ShortcutKeyOption> AvailableKeys { get; } = BuildAvailableKeys();

        private static List<ShortcutKeyOption> BuildAvailableKeys()
        {
            var list = new List<ShortcutKeyOption>
            {
                new ShortcutKeyOption { Name = "-- Select Key --", VirtualKey = 0 }
            };

            // Function Keys (F1 - F12)
            for (int i = 1; i <= 12; i++)
            {
                list.Add(new ShortcutKeyOption { Name = $"F{i}", VirtualKey = (int)VirtualKey.F1 + (i - 1) });
            }

            // Letters (A - Z)
            for (char c = 'A'; c <= 'Z'; c++)
            {
                list.Add(new ShortcutKeyOption { Name = c.ToString(), VirtualKey = (int)Enum.Parse(typeof(VirtualKey), c.ToString()) });
            }

            // Numbers (0 - 9)
            for (int i = 0; i <= 9; i++)
            {
                list.Add(new ShortcutKeyOption { Name = i.ToString(), VirtualKey = (int)VirtualKey.Number0 + i });
            }

            // Common action keys
            list.Add(new ShortcutKeyOption { Name = "Enter", VirtualKey = (int)VirtualKey.Enter });
            list.Add(new ShortcutKeyOption { Name = "Esc", VirtualKey = (int)VirtualKey.Escape });
            list.Add(new ShortcutKeyOption { Name = "Space", VirtualKey = (int)VirtualKey.Space });
            list.Add(new ShortcutKeyOption { Name = "Tab", VirtualKey = (int)VirtualKey.Tab });
            list.Add(new ShortcutKeyOption { Name = "PrtScn", VirtualKey = (int)VirtualKey.Snapshot });
            list.Add(new ShortcutKeyOption { Name = "Backspace", VirtualKey = (int)VirtualKey.Back });
            list.Add(new ShortcutKeyOption { Name = "Delete", VirtualKey = (int)VirtualKey.Delete });
            list.Add(new ShortcutKeyOption { Name = "Insert", VirtualKey = (int)VirtualKey.Insert });
            list.Add(new ShortcutKeyOption { Name = "Home", VirtualKey = (int)VirtualKey.Home });
            list.Add(new ShortcutKeyOption { Name = "End", VirtualKey = (int)VirtualKey.End });
            list.Add(new ShortcutKeyOption { Name = "PageUp", VirtualKey = (int)VirtualKey.PageUp });
            list.Add(new ShortcutKeyOption { Name = "PageDown", VirtualKey = (int)VirtualKey.PageDown });
            list.Add(new ShortcutKeyOption { Name = "Up", VirtualKey = (int)VirtualKey.Up });
            list.Add(new ShortcutKeyOption { Name = "Down", VirtualKey = (int)VirtualKey.Down });
            list.Add(new ShortcutKeyOption { Name = "Left", VirtualKey = (int)VirtualKey.Left });
            list.Add(new ShortcutKeyOption { Name = "Right", VirtualKey = (int)VirtualKey.Right });

            // Media keys
            list.Add(new ShortcutKeyOption { Name = "Volume Mute", VirtualKey = 173 });
            list.Add(new ShortcutKeyOption { Name = "Volume Down", VirtualKey = 174 });
            list.Add(new ShortcutKeyOption { Name = "Volume Up", VirtualKey = 175 });
            list.Add(new ShortcutKeyOption { Name = "Play / Pause", VirtualKey = 179 });
            list.Add(new ShortcutKeyOption { Name = "Next Track", VirtualKey = 176 });
            list.Add(new ShortcutKeyOption { Name = "Previous Track", VirtualKey = 177 });

            return list;
        }

        private static List<ShortcutItem> GetDefaultShortcuts()
        {
            return new List<ShortcutItem>
            {
                new ShortcutItem("Screenshot", "\uE722", false, false, false, false, (int)VirtualKey.F12),
                new ShortcutItem("Fullscreen", "\uE740", false, true, false, false, (int)VirtualKey.Enter),
                new ShortcutItem("Record", "\uE714", false, true, false, true, (int)VirtualKey.R),
                new ShortcutItem("Desktop", "\uE770", false, false, false, true, (int)VirtualKey.D),
                new ShortcutItem("Task Mgr", "\uE9D2", true, false, true, false, (int)VirtualKey.Escape),
            };
        }

        public static List<ShortcutItem> LoadShortcuts()
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings.Values.TryGetValue(SETTINGS_KEY, out object raw) && raw is string data && !string.IsNullOrWhiteSpace(data))
                {
                    var lines = data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var list = new List<ShortcutItem>();
                    foreach (var line in lines)
                    {
                        var item = ShortcutItem.FromStorageString(line);
                        if (item != null)
                        {
                            list.Add(item);
                        }
                    }

                    if (list.Count > 0)
                    {
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load shortcuts from LocalSettings.");
            }

            // Defaults for first run
            var defaults = GetDefaultShortcuts();
            SaveShortcuts(defaults);
            return defaults;
        }

        public static void SaveShortcuts(IEnumerable<ShortcutItem> shortcuts)
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                if (shortcuts == null)
                {
                    localSettings.Values[SETTINGS_KEY] = string.Empty;
                    return;
                }

                var lines = shortcuts.Select(s => s.ToStorageString());
                localSettings.Values[SETTINGS_KEY] = string.Join("\n", lines);
                Logger.Info("Saved shortcuts to LocalSettings.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save shortcuts to LocalSettings.");
            }
        }
    }
}
