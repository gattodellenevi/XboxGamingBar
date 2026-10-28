using NLog;
using Microsoft.Win32;
using Shared.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Xml;
using XboxGamingBarHelper.Windows;

namespace XboxGamingBarHelper.Systems
{
    public class TrayInspectionManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly ConcurrentDictionary<string, string> IconCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, TrayItem> LastKnownTrayItems = new ConcurrentDictionary<string, TrayItem>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<Guid, string> KnownFolderCache = new ConcurrentDictionary<Guid, string>();

        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int SHGetKnownFolderPath(
            [MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
            uint dwFlags,
            IntPtr hToken,
            out IntPtr pszPath);

        private static readonly HashSet<string> IgnoredButtonNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Start",
            "Search",
            "Task View",
            "Copilot",
            "Widgets",
            "Show hidden icons",
            "Show Hidden Icons",
            "Notification chevron",
            "System Clock",
            "Clock",
            "Action Center",
            "Notification Center",
            "Quick Settings",
            "Windows Shell Experience Host",
            "Show Desktop"
        };

        public static string GetTrayItemsJson()
        {
            try
            {
                var items = EnumerateTrayItems();
                return JsonSerializer.Serialize(items);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get system tray items JSON.");
                return "[]";
            }
        }

        public static List<TrayItem> EnumerateTrayItems()
        {
            var results = new List<TrayItem>();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenPids = new HashSet<int>();

            try
            {
                // Snapshot current processes for fast path-and-PID resolution
                var runningProcesses = Process.GetProcesses();

                // 1. Inspect Taskbar Notification Area (Primary and Secondary monitors)
                IntPtr taskbarHwnd = User32.FindWindow("Shell_TrayWnd", null);
                if (taskbarHwnd != IntPtr.Zero)
                {
                    EnumerateContainerButtons(taskbarHwnd, false, runningProcesses, seenNames, seenPids, results);
                }

                IntPtr secondaryTaskbarHwnd = User32.FindWindow("Shell_SecondaryTrayWnd", null);
                if (secondaryTaskbarHwnd != IntPtr.Zero)
                {
                    EnumerateContainerButtons(secondaryTaskbarHwnd, false, runningProcesses, seenNames, seenPids, results);
                }

                // 2. Inspect Hidden / Overflow Icons Window (modern Win11 and classic Win10)
                IntPtr overflowXamlHwnd = User32.FindWindow("TopLevelWindowForOverflowXamlIsland", null);
                if (overflowXamlHwnd != IntPtr.Zero)
                {
                    EnumerateContainerButtons(overflowXamlHwnd, true, runningProcesses, seenNames, seenPids, results);
                }

                IntPtr overflowClassicHwnd = User32.FindWindow("NotifyIconOverflowWindow", null);
                if (overflowClassicHwnd != IntPtr.Zero)
                {
                    EnumerateContainerButtons(overflowClassicHwnd, true, runningProcesses, seenNames, seenPids, results);
                }

                // 3. Inspect Registry NotifyIconSettings (Fallback and supplement for missing/hidden icons in FSE)
                EnumerateRegistryTrayItems(runningProcesses, seenNames, seenPids, results);

                // 4. Merge with Last-Known-Good In-Memory Cache and validate process liveness
                results = MergeWithLastKnownCache(results, runningProcesses);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception while enumerating system tray items via UIAutomation.");
            }

            Logger.Info($"Discovered {results.Count} system tray applications.");
            return results;
        }

        private static List<AutomationElement> FindTrayButtons(IntPtr containerHwnd, bool isOverflow)
        {
            var buttons = new List<AutomationElement>();
            if (containerHwnd == IntPtr.Zero) return buttons;

            var buttonCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);

            // 1. Direct descendants of container
            try
            {
                var container = AutomationElement.FromHandle(containerHwnd);
                if (container != null)
                {
                    var directButtons = container.FindAll(TreeScope.Descendants, buttonCondition);
                    for (int i = 0; i < directButtons.Count; i++)
                    {
                        buttons.Add(directButtons[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, $"Could not get direct button descendants for hwnd 0x{containerHwnd:X}");
            }

            // 2. Enumerate child windows (crucial for Windows 11 XAML Island TopLevelWindowForOverflowXamlIsland
            //    whose buttons reside in Windows.UI.Composition.DesktopWindowContentBridge child HWND)
            if (buttons.Count == 0 || isOverflow)
            {
                User32.EnumChildWindows(containerHwnd, (childHwnd, lp) =>
                {
                    try
                    {
                        var childEl = AutomationElement.FromHandle(childHwnd);
                        if (childEl != null)
                        {
                            var childButtons = childEl.FindAll(TreeScope.Descendants, buttonCondition);
                            for (int i = 0; i < childButtons.Count; i++)
                            {
                                buttons.Add(childButtons[i]);
                            }
                        }
                    }
                    catch { }
                    return true;
                }, IntPtr.Zero);
            }

            return buttons;
        }

        private static void EnumerateContainerButtons(
            IntPtr containerHwnd,
            bool isHiddenOverflow,
            Process[] processes,
            HashSet<string> seenNames,
            HashSet<int> seenPids,
            List<TrayItem> results)
        {
            try
            {
                var buttons = FindTrayButtons(containerHwnd, isHiddenOverflow);

                for (int i = 0; i < buttons.Count; i++)
                {
                    try
                    {
                        var btn = buttons[i];
                        string autoId = btn.Current.AutomationId ?? string.Empty;
                        string className = btn.Current.ClassName ?? string.Empty;
                        string rawName = btn.Current.Name?.Trim();
                        string helpText = btn.Current.HelpText?.Trim();

                        if (string.IsNullOrWhiteSpace(rawName) && !string.IsNullOrWhiteSpace(helpText))
                        {
                            rawName = helpText;
                        }

                        if (string.IsNullOrWhiteSpace(rawName)) continue;

                        // Exclude taskbar application pins / running app window tabs
                        if (autoId.StartsWith("Appid:", StringComparison.OrdinalIgnoreCase) ||
                            autoId.Equals("StartButton", StringComparison.OrdinalIgnoreCase) ||
                            autoId.Equals("SearchButton", StringComparison.OrdinalIgnoreCase) ||
                            className.IndexOf("TaskList", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            continue;
                        }

                        // Exclude Windows shell system controls (Clock, Volume, Battery, Network, Chevron, Show Desktop)
                        if (autoId.Equals("SystemTrayIcon", StringComparison.OrdinalIgnoreCase) ||
                            className.StartsWith("SystemTray.AccentButton", StringComparison.OrdinalIgnoreCase) ||
                            className.StartsWith("SystemTray.OmniButton", StringComparison.OrdinalIgnoreCase) ||
                            className.StartsWith("SystemTray.ShowDesktop", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (IgnoredButtonNames.Contains(rawName)) continue;

                        // Clean up multi-line or status-heavy tooltips (e.g. "Steam\n1 download" -> "Steam")
                        string displayName = CleanDisplayName(rawName);
                        if (string.IsNullOrWhiteSpace(displayName)) continue;

                        // Deduplicate identical apps
                        string dedupeKey = $"{displayName}_{rawName}";
                        if (seenNames.Contains(dedupeKey)) continue;
                        seenNames.Add(dedupeKey);

                        var bounds = btn.Current.BoundingRectangle;
                        int centerX = 0;
                        int centerY = 0;
                        if (!bounds.IsEmpty && !double.IsInfinity(bounds.Left) && !double.IsInfinity(bounds.Top) &&
                            bounds.Width > 0 && bounds.Height > 0)
                        {
                            centerX = (int)(bounds.Left + (bounds.Width / 2));
                            centerY = (int)(bounds.Top + (bounds.Height / 2));
                        }

                        // Resolve process information
                        int pid = 0;
                        string procName = string.Empty;
                        string exePath = string.Empty;

                        MatchProcess(displayName, rawName, processes, out pid, out procName, out exePath);

                        // Extract high-resolution base64 PNG icon
                        string iconBase64 = string.Empty;
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            iconBase64 = GetOrExtractIconBase64(exePath);
                        }

                        var item = new TrayItem
                        {
                            Id = $"{displayName}_{i}_{pid}",
                            Name = displayName,
                            Tooltip = !string.IsNullOrEmpty(helpText) ? helpText : rawName,
                            ProcessName = procName,
                            ProcessId = pid,
                            IconBase64 = iconBase64,
                            IsHidden = isHiddenOverflow || (centerX == 0 && centerY == 0),
                            X = centerX,
                            Y = centerY
                        };

                        if (pid > 0)
                        {
                            seenPids.Add(pid);
                        }
                        seenNames.Add(displayName);

                        results.Add(item);
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug(ex, $"Failed to process tray button index {i}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Failed to enumerate buttons from container hwnd {containerHwnd}");
            }
        }

        private static string CleanDisplayName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return string.Empty;

            // Take first line if multi-line
            string firstLine = rawName.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();

            // Strip trailing parenthetical or dash statuses like "Discord - Connected" or "Discord (1 update)"
            int dashIdx = firstLine.IndexOf(" - ", StringComparison.Ordinal);
            if (dashIdx > 0)
            {
                firstLine = firstLine.Substring(0, dashIdx).Trim();
            }

            int parenIdx = firstLine.IndexOf(" (", StringComparison.Ordinal);
            if (parenIdx > 0)
            {
                firstLine = firstLine.Substring(0, parenIdx).Trim();
            }

            return firstLine;
        }

        private static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new StringBuilder();
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
            }
            return sb.ToString();
        }

        private static void MatchProcess(
            string displayName,
            string rawName,
            Process[] processes,
            out int pid,
            out string procName,
            out string exePath)
        {
            pid = 0;
            procName = string.Empty;
            exePath = string.Empty;

            string normDisplay = Normalize(displayName);
            string normRaw = Normalize(rawName);

            var searchTokens = new List<string>();
            if (!string.IsNullOrEmpty(normDisplay)) searchTokens.Add(normDisplay);
            if (!string.IsNullOrEmpty(normRaw) && normRaw != normDisplay) searchTokens.Add(normRaw);

            // Handle common app aliases and acronyms
            if (normDisplay.Contains("rivatuner") || normDisplay.Contains("rtss") || normRaw.Contains("rivatuner"))
            {
                searchTokens.Insert(0, "rtss");
                searchTokens.Add("rivatuner");
            }
            if (normDisplay.Contains("amdsoftware") || normDisplay.Contains("adrenalin") || normDisplay.Contains("radeon"))
            {
                searchTokens.Insert(0, "radeonsoftware");
                searchTokens.Add("cnext");
                searchTokens.Add("amdrsserv");
            }
            if (normDisplay.Contains("quicknote") || normDisplay.Contains("onenote"))
            {
                searchTokens.Insert(0, "onenotem");
                searchTokens.Add("onenote");
            }
            if (normDisplay.Contains("nvidiasettings") || normDisplay.Contains("geforce") || normDisplay.Contains("nvidia"))
            {
                searchTokens.Add("nvcontainer");
                searchTokens.Add("nvcplui");
                searchTokens.Add("nvdisplay");
            }
            if (normDisplay.Contains("couchgamingbar"))
            {
                searchTokens.Insert(0, "couchgamingbarhelper");
            }
            if (normDisplay.Contains("ghelper"))
            {
                searchTokens.Insert(0, "ghelper");
            }
            if (normDisplay.Contains("steam"))
            {
                searchTokens.Insert(0, "steam");
            }
            if (normDisplay.Contains("discord"))
            {
                searchTokens.Insert(0, "discord");
            }

            // Pass 1: Exact ProcessName match
            foreach (var p in processes)
            {
                try
                {
                    string pNorm = Normalize(p.ProcessName);
                    foreach (var token in searchTokens)
                    {
                        if (pNorm == token)
                        {
                            string path = User32.GetProcessExecutablePath(p.Id);
                            if (!string.IsNullOrEmpty(path))
                            {
                                pid = p.Id;
                                procName = p.ProcessName;
                                exePath = path;
                                return;
                            }
                        }
                    }
                }
                catch { }
            }

            // Pass 2: Substring ProcessName match (token length >= 4)
            foreach (var p in processes)
            {
                try
                {
                    string pNorm = Normalize(p.ProcessName);
                    foreach (var token in searchTokens)
                    {
                        if (token.Length >= 4 && (pNorm.Contains(token) || token.Contains(pNorm)))
                        {
                            string path = User32.GetProcessExecutablePath(p.Id);
                            if (!string.IsNullOrEmpty(path))
                            {
                                pid = p.Id;
                                procName = p.ProcessName;
                                exePath = path;
                                return;
                            }
                        }
                    }
                }
                catch { }
            }

            // Pass 3: Check MainWindowTitle if available
            foreach (var p in processes)
            {
                try
                {
                    if (!string.IsNullOrEmpty(p.MainWindowTitle))
                    {
                        string titleNorm = Normalize(p.MainWindowTitle);
                        foreach (var token in searchTokens)
                        {
                            if (titleNorm.Contains(token) || token.Contains(titleNorm))
                            {
                                string path = User32.GetProcessExecutablePath(p.Id);
                                if (!string.IsNullOrEmpty(path))
                                {
                                    pid = p.Id;
                                    procName = p.ProcessName;
                                    exePath = path;
                                    return;
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            // Pass 4: Fallback for Windows system applets like Bluetooth
            if (normDisplay.Contains("bluetooth"))
            {
                string bthPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "fsquirt.exe");
                if (File.Exists(bthPath))
                {
                    procName = "Bluetooth";
                    exePath = bthPath;
                }
            }
        }

        private static string GetOrExtractIconBase64(string exePath)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) return string.Empty;

            if (IconCache.TryGetValue(exePath, out var cached))
            {
                return cached;
            }

            try
            {
                using (Icon icon = Icon.ExtractAssociatedIcon(exePath))
                {
                    if (icon != null)
                    {
                        using (Bitmap bmp = icon.ToBitmap())
                        using (var ms = new MemoryStream())
                        {
                            bmp.Save(ms, ImageFormat.Png);
                            string b64 = Convert.ToBase64String(ms.ToArray());
                            IconCache[exePath] = b64;
                            return b64;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, $"Failed to extract icon from {exePath}");
            }

            return string.Empty;
        }

        #region Registry and Cache Fallback

        public static void RemoveFromCache(int processId)
        {
            if (processId <= 0) return;
            var keysToRemove = LastKnownTrayItems.Where(kvp => kvp.Value.ProcessId == processId).Select(kvp => kvp.Key).ToList();
            foreach (var k in keysToRemove)
            {
                LastKnownTrayItems.TryRemove(k, out _);
            }
        }

        private static List<TrayItem> MergeWithLastKnownCache(List<TrayItem> currentScan, Process[] runningProcesses)
        {
            var merged = new List<TrayItem>(currentScan);
            var seenPids = new HashSet<int>(currentScan.Where(i => i.ProcessId > 0).Select(i => i.ProcessId));
            var seenNames = new HashSet<string>(currentScan.Select(i => i.Name), StringComparer.OrdinalIgnoreCase);

            // 1. Update cache with items discovered in current scan
            foreach (var item in currentScan)
            {
                string key = $"{item.ProcessName}_{item.Name}".ToLowerInvariant();
                LastKnownTrayItems[key] = item;
            }

            // 2. Retain items from cache that were missed (e.g. due to FSE / DWM occlusion) if process is still alive
            foreach (var kvp in LastKnownTrayItems)
            {
                var cached = kvp.Value;
                if (cached.ProcessId > 0 && !seenPids.Contains(cached.ProcessId) && !seenNames.Contains(cached.Name))
                {
                    bool isAlive = false;
                    try
                    {
                        isAlive = User32.IsProcessAlive(cached.ProcessId);
                    }
                    catch
                    {
                        isAlive = runningProcesses != null && runningProcesses.Any(p => p.Id == cached.ProcessId);
                    }

                    if (isAlive)
                    {
                        merged.Add(cached);
                        seenPids.Add(cached.ProcessId);
                        seenNames.Add(cached.Name);
                        Logger.Info($"Retained active tray application from cache: '{cached.Name}' (PID {cached.ProcessId})");
                    }
                    else
                    {
                        // Prune dead process from cache
                        LastKnownTrayItems.TryRemove(kvp.Key, out _);
                    }
                }
            }

            return merged;
        }

        private static void EnumerateRegistryTrayItems(
            Process[] runningProcesses,
            HashSet<string> seenNames,
            HashSet<int> seenPids,
            List<TrayItem> results)
        {
            if (runningProcesses == null || runningProcesses.Length == 0) return;

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings"))
                {
                    if (key == null) return;

                    string[] subKeyNames = key.GetSubKeyNames();
                    for (int i = 0; i < subKeyNames.Length; i++)
                    {
                        string subKeyName = subKeyNames[i];
                        try
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                if (subKey == null) continue;

                                string rawExePath = subKey.GetValue("ExecutablePath") as string;
                                if (string.IsNullOrWhiteSpace(rawExePath)) continue;

                                string resolvedExePath = ResolvePathWithKnownFolder(rawExePath);
                                string exeName = !string.IsNullOrEmpty(resolvedExePath) ? Path.GetFileNameWithoutExtension(resolvedExePath) : string.Empty;
                                if (string.IsNullOrWhiteSpace(exeName)) continue;

                                string tooltip = (subKey.GetValue("InitialTooltip") as string)?.Trim();
                                string displayName = CleanDisplayName(!string.IsNullOrEmpty(tooltip) ? tooltip : exeName);
                                if (string.IsNullOrWhiteSpace(displayName)) displayName = exeName;

                                if (IgnoredButtonNames.Contains(displayName) || IgnoredButtonNames.Contains(exeName))
                                {
                                    continue;
                                }

                                // Match against active running processes
                                MatchRegistryProcess(resolvedExePath, displayName, exeName, runningProcesses, out int pid, out string procName, out string actualExePath);
                                if (pid <= 0) continue;

                                if (seenPids.Contains(pid)) continue;

                                string dedupeKey = $"{displayName}_{pid}";
                                if (seenNames.Contains(dedupeKey)) continue;

                                seenPids.Add(pid);
                                seenNames.Add(dedupeKey);
                                seenNames.Add(displayName);

                                // Prioritize IconSnapshot (pre-rendered PNG bytes directly from registry)
                                string iconBase64 = string.Empty;
                                byte[] iconBytes = subKey.GetValue("IconSnapshot") as byte[];
                                if (iconBytes != null && iconBytes.Length > 0)
                                {
                                    iconBase64 = Convert.ToBase64String(iconBytes);
                                }

                                if (string.IsNullOrEmpty(iconBase64) && !string.IsNullOrEmpty(actualExePath))
                                {
                                    iconBase64 = GetOrExtractIconBase64(actualExePath);
                                }

                                var item = new TrayItem
                                {
                                    Id = $"{displayName}_reg_{pid}",
                                    Name = displayName,
                                    Tooltip = !string.IsNullOrEmpty(tooltip) ? tooltip : displayName,
                                    ProcessName = procName,
                                    ProcessId = pid,
                                    IconBase64 = iconBase64,
                                    IsHidden = true,
                                    X = 0,
                                    Y = 0
                                };

                                results.Add(item);
                                Logger.Info($"Discovered tray application via Registry fallback: '{displayName}' (PID {pid}, {procName})");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Debug(ex, $"Failed to inspect NotifyIconSettings subkey {subKeyName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to read NotifyIconSettings registry key.");
            }
        }

        private static void MatchRegistryProcess(
            string resolvedExePath,
            string displayName,
            string exeName,
            Process[] runningProcesses,
            out int pid,
            out string procName,
            out string actualExePath)
        {
            pid = 0;
            procName = string.Empty;
            actualExePath = resolvedExePath;

            // 1. Exact match on ProcessName with exeName
            foreach (var p in runningProcesses)
            {
                try
                {
                    if (string.Equals(p.ProcessName, exeName, StringComparison.OrdinalIgnoreCase))
                    {
                        pid = p.Id;
                        procName = p.ProcessName;
                        actualExePath = User32.GetProcessExecutablePath(p.Id) ?? resolvedExePath;
                        return;
                    }
                }
                catch { }
            }

            // 2. Full executable path match if available
            if (!string.IsNullOrEmpty(resolvedExePath))
            {
                foreach (var p in runningProcesses)
                {
                    try
                    {
                        string pPath = User32.GetProcessExecutablePath(p.Id);
                        if (!string.IsNullOrEmpty(pPath) && string.Equals(pPath, resolvedExePath, StringComparison.OrdinalIgnoreCase))
                        {
                            pid = p.Id;
                            procName = p.ProcessName;
                            actualExePath = pPath;
                            return;
                        }
                    }
                    catch { }
                }
            }

            // 3. Fallback to general MatchProcess
            MatchProcess(displayName, exeName, runningProcesses, out pid, out procName, out string matchedPath);
            if (!string.IsNullOrEmpty(matchedPath))
            {
                actualExePath = matchedPath;
            }
        }

        private static string ResolvePathWithKnownFolder(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) return string.Empty;

            if (rawPath.StartsWith("{", StringComparison.Ordinal))
            {
                int closingBrace = rawPath.IndexOf('}');
                if (closingBrace > 1)
                {
                    string guidStr = rawPath.Substring(1, closingBrace - 1);
                    string relativePart = rawPath.Substring(closingBrace + 1).TrimStart('\\', '/');

                    if (Guid.TryParse(guidStr, out Guid folderGuid))
                    {
                        string knownFolder = GetKnownFolderPath(folderGuid);
                        if (!string.IsNullOrEmpty(knownFolder))
                        {
                            return Path.Combine(knownFolder, relativePart);
                        }
                    }
                }
            }

            return Environment.ExpandEnvironmentVariables(rawPath);
        }

        private static string GetKnownFolderPath(Guid folderGuid)
        {
            if (KnownFolderCache.TryGetValue(folderGuid, out string cached))
            {
                return cached;
            }

            // Fast static mappings for standard Windows Known Folders
            if (folderGuid == new Guid("6D809377-6AF0-444B-8957-A3773F02200E"))
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (folderGuid == new Guid("7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E"))
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (folderGuid == new Guid("D0384E7D-BAC3-4797-8F14-CBA229B392B5"))
                return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (folderGuid == new Guid("F1B32785-6FBA-4FCF-9D55-7B8E7F157091"))
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (folderGuid == new Guid("3EB685D2-658F-477E-A060-26CC0C35F7E2"))
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (folderGuid == new Guid("1AC14E77-02E7-4E5D-B744-2EB1AE5198B7"))
                return Environment.GetFolderPath(Environment.SpecialFolder.System);
            if (folderGuid == new Guid("F38BF404-1D43-42F2-9305-67DE0B28FC23"))
                return Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            try
            {
                int hr = SHGetKnownFolderPath(folderGuid, 0, IntPtr.Zero, out IntPtr pszPath);
                if (hr == 0 && pszPath != IntPtr.Zero)
                {
                    try
                    {
                        string path = Marshal.PtrToStringUni(pszPath);
                        if (!string.IsNullOrEmpty(path))
                        {
                            KnownFolderCache[folderGuid] = path;
                            return path;
                        }
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(pszPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, $"Failed to resolve KnownFolder GUID {folderGuid}");
            }

            return string.Empty;
        }

        #endregion

        public static bool ExecuteAction(TrayActionRequest request)
        {
            if (request == null) return false;

            Logger.Info($"Executing Tray Action '{request.Action}' for ItemId '{request.ItemId}' (PID {request.ProcessId}) at ({request.X}, {request.Y})");

            try
            {
                switch (request.Action)
                {
                    case TrayAction.BringToFront:
                        bool brought = BringAppToFront(request.ProcessId, request.ItemId);
                        if (!brought)
                        {
                            Logger.Info($"BringAppToFront returned false for PID {request.ProcessId}. Falling back to PrimaryClick on tray icon.");
                            return PrimaryClickTrayItem(request.ItemId, request.X, request.Y);
                        }
                        return true;

                    case TrayAction.PrimaryClick:
                        if (request.X == 0 && request.Y == 0)
                        {
                            Logger.Info($"Headless item clicked for PID {request.ProcessId}. Attempting BringAppToFront first.");
                            if (BringAppToFront(request.ProcessId, request.ItemId))
                            {
                                return true;
                            }
                        }
                        return PrimaryClickTrayItem(request.ItemId, request.X, request.Y);

                    case TrayAction.DoubleClick:
                        return DoubleClickTrayItem(request.ItemId, request.ProcessId, request.X, request.Y);

                    case TrayAction.SecondaryClick:
                        return SecondaryClickTrayItem(request.ItemId, request.X, request.Y);

                    case TrayAction.Terminate:
                        return TerminateProcess(request.ProcessId);

                    default:
                        Logger.Warn($"Unknown TrayAction {request.Action}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to execute tray action.");
                return false;
            }
        }

        private static bool BringAppToFront(int processId, string itemId)
        {
            return TryActivateApp(processId, itemId);
        }

        private static bool TryActivateApp(int processId, string itemId)
        {
            try
            {
                // 1. Try bringing existing window to front if it exists
                if (processId > 0)
                {
                    try
                    {
                        var proc = Process.GetProcessById(processId);
                        if (proc != null)
                        {
                            IntPtr hWnd = proc.MainWindowHandle;
                            if (hWnd != IntPtr.Zero && User32.IsWindow(hWnd))
                            {
                                Logger.Info($"Restoring and bringing window 0x{hWnd:X} to foreground for PID {processId}.");
                                User32.ShowWindow(hWnd, User32.SW_RESTORE);
                                User32.SetForegroundWindow(hWnd);
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug(ex, $"Could not get MainWindowHandle for PID {processId}");
                    }
                }

                // 2. Query executable path
                string exePath = processId > 0 ? User32.GetProcessExecutablePath(processId) : string.Empty;

                // 3. Check if it's a packaged AppX/MSIX app (e.g. OneGameLauncherService -> OneGameLauncher)
                if (!string.IsNullOrEmpty(exePath) && exePath.IndexOf(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (TryActivatePackagedApp(exePath))
                    {
                        return true;
                    }
                }

                // 4. Common gaming launcher URI protocols
                string lowerItem = (itemId ?? string.Empty).ToLowerInvariant();
                if (lowerItem.Contains("steam"))
                {
                    Logger.Info("Activating Steam via steam://open/main");
                    Process.Start(new ProcessStartInfo("steam://open/main") { UseShellExecute = true });
                    return true;
                }

                // 5. Desktop app launch/restore via executable path
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    string fileName = Path.GetFileName(exePath).ToLowerInvariant();
                    if (!fileName.Contains("host") && !fileName.Contains("service") && !fileName.Contains("system"))
                    {
                        Logger.Info($"Activating desktop app by executing '{exePath}'");
                        Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Failed in TryActivateApp for PID {processId}, ItemId '{itemId}'.");
            }

            return false;
        }

        private static bool TryActivatePackagedApp(string exePath)
        {
            try
            {
                if (string.IsNullOrEmpty(exePath)) return false;

                // Find AppxManifest.xml by walking up directory tree
                DirectoryInfo dir = new FileInfo(exePath).Directory;
                string manifestPath = null;
                while (dir != null && dir.Exists)
                {
                    string candidate = Path.Combine(dir.FullName, "AppxManifest.xml");
                    if (File.Exists(candidate))
                    {
                        manifestPath = candidate;
                        break;
                    }
                    dir = dir.Parent;
                }

                if (string.IsNullOrEmpty(manifestPath)) return false;

                var doc = new XmlDocument();
                doc.Load(manifestPath);

                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("m", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
                nsmgr.AddNamespace("uap", "http://schemas.microsoft.com/appx/manifest/uap/windows10");

                // 1. Try protocol extension (e.g. "ogl:" for OneGameLauncher)
                var protoNode = doc.SelectSingleNode("//uap:Protocol/@Name", nsmgr);
                if (protoNode != null && !string.IsNullOrWhiteSpace(protoNode.Value))
                {
                    string uri = protoNode.Value.Trim() + ":";
                    Logger.Info($"Activating packaged app via protocol '{uri}'");
                    Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                    return true;
                }

                // 2. Try AppUserModelId (shell:AppsFolder\PackageFamilyName!AppId)
                string packageDirName = Path.GetFileName(Path.GetDirectoryName(manifestPath));
                var appNode = doc.SelectSingleNode("//m:Application/@Id", nsmgr);
                string appId = appNode?.Value;

                if (!string.IsNullOrEmpty(packageDirName) && !string.IsNullOrEmpty(appId))
                {
                    try
                    {
                        var pkgMgr = new global::Windows.Management.Deployment.PackageManager();
                        var pkg = pkgMgr.FindPackageForUser(string.Empty, packageDirName);
                        if (pkg != null)
                        {
                            string aumid = $"{pkg.Id.FamilyName}!{appId}";
                            Logger.Info($"Activating packaged app via shell:AppsFolder\\{aumid}");
                            Process.Start(new ProcessStartInfo("explorer.exe", $"shell:AppsFolder\\{aumid}") { UseShellExecute = true });
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug(ex, "Failed to resolve package via PackageManager.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Failed to activate packaged app from {exePath}");
            }

            return false;
        }

        private static bool PrimaryClickTrayItem(string itemId, int x, int y)
        {
            // 1. Try UIAutomation InvokePattern directly on the button
            var btn = FindTrayButtonElement(itemId);
            if (btn != null)
            {
                try
                {
                    if (btn.TryGetCurrentPattern(InvokePattern.Pattern, out object p))
                    {
                        Logger.Info($"Invoking tray button via InvokePattern for '{itemId}'.");
                        ((InvokePattern)p).Invoke();
                        return true;
                    }
                    if (btn.TryGetCurrentPattern(TogglePattern.Pattern, out object t))
                    {
                        Logger.Info($"Toggling tray button via TogglePattern for '{itemId}'.");
                        ((TogglePattern)t).Toggle();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, $"InvokePattern failed for '{itemId}'.");
                }
            }

            // 2. Mouse simulation fallback if coordinates valid
            if (x > 0 && y > 0)
            {
                SimulateClick(x, y, false);
                return true;
            }

            return false;
        }

        private static bool SecondaryClickTrayItem(string itemId, int x, int y)
        {
            // If we have valid coordinates (visible icon on taskbar)
            if (x > 0 && y > 0)
            {
                SimulateClick(x, y, true);
                return true;
            }

            // If icon is in hidden overflow, open overflow flyout to obtain coordinates and right-click
            Task.Run(() =>
            {
                try
                {
                    IntPtr overflowXaml = User32.FindWindow("TopLevelWindowForOverflowXamlIsland", null);
                    bool isAlreadyVisible = (overflowXaml != IntPtr.Zero && User32.IsWindowVisible(overflowXaml));

                    if (!isAlreadyVisible)
                    {
                        var chevron = FindShowHiddenIconsButton();
                        if (chevron != null)
                        {
                            Logger.Info("Opening hidden icons flyout to simulate right-click.");
                            if (chevron.TryGetCurrentPattern(InvokePattern.Pattern, out object p))
                            {
                                ((InvokePattern)p).Invoke();
                            }
                            else if (chevron.TryGetCurrentPattern(TogglePattern.Pattern, out p))
                            {
                                ((TogglePattern)p).Toggle();
                            }

                            Thread.Sleep(200);
                        }
                    }

                    var targetBtn = FindTrayButtonElement(itemId);
                    if (targetBtn != null)
                    {
                        var rect = targetBtn.Current.BoundingRectangle;
                        if (!rect.IsEmpty && rect.Width > 0 && rect.Height > 0)
                        {
                            int cx = (int)(rect.Left + (rect.Width / 2));
                            int cy = (int)(rect.Top + (rect.Height / 2));
                            Logger.Info($"Simulating right click on '{itemId}' at ({cx}, {cy}).");
                            SimulateClick(cx, cy, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to perform secondary click on overflow item '{itemId}'.");
                }
            });

            return true;
        }

        private static bool DoubleClickTrayItem(string itemId, int processId, int x, int y)
        {
            // 1. Try direct app activation first (handles packaged services like OneGameLauncherService, minimized desktop apps, etc.)
            if (TryActivateApp(processId, itemId))
            {
                return true;
            }

            // 2. If we have valid coordinates (visible icon on taskbar)
            if (x > 0 && y > 0)
            {
                SimulateDoubleClick(x, y);
                return true;
            }

            // 3. If icon is in hidden overflow, open overflow flyout to obtain coordinates and double-click
            Task.Run(() =>
            {
                try
                {
                    IntPtr overflowXaml = User32.FindWindow("TopLevelWindowForOverflowXamlIsland", null);
                    bool isAlreadyVisible = (overflowXaml != IntPtr.Zero && User32.IsWindowVisible(overflowXaml));

                    if (!isAlreadyVisible)
                    {
                        var chevron = FindShowHiddenIconsButton();
                        if (chevron != null)
                        {
                            Logger.Info("Opening hidden icons flyout to simulate double-click.");
                            if (chevron.TryGetCurrentPattern(InvokePattern.Pattern, out object p))
                            {
                                ((InvokePattern)p).Invoke();
                            }
                            else if (chevron.TryGetCurrentPattern(TogglePattern.Pattern, out p))
                            {
                                ((TogglePattern)p).Toggle();
                            }

                            Thread.Sleep(200);
                        }
                    }

                    var targetBtn = FindTrayButtonElement(itemId);
                    if (targetBtn != null)
                    {
                        var rect = targetBtn.Current.BoundingRectangle;
                        if (!rect.IsEmpty && rect.Width > 0 && rect.Height > 0)
                        {
                            int cx = (int)(rect.Left + (rect.Width / 2));
                            int cy = (int)(rect.Top + (rect.Height / 2));
                            Logger.Info($"Simulating double-click on '{itemId}' at ({cx}, {cy}).");
                            SimulateDoubleClick(cx, cy);
                            return;
                        }
                    }

                    // Fallback to primary click/invoke if double-click coordinates couldn't be obtained
                    PrimaryClickTrayItem(itemId, x, y);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to perform double-click on overflow item '{itemId}'.");
                }
            });

            return true;
        }
        private static void SimulateDoubleClick(int x, int y)
        {
            Task.Run(() =>
            {
                try
                {
                    if (x <= 0 && y <= 0)
                    {
                        Logger.Warn("Cannot simulate double click: coordinates are (0, 0).");
                        return;
                    }

                    Logger.Info($"Simulating double click at ({x}, {y}).");
                    User32.SetCursorPos(x, y);
                    Thread.Sleep(20);

                    // First click
                    User32.mouse_event(User32.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(30);
                    User32.mouse_event(User32.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);

                    Thread.Sleep(60);

                    // Second click
                    User32.mouse_event(User32.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(30);
                    User32.mouse_event(User32.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to simulate double click on tray icon.");
                }
            });
        }

        private static AutomationElement FindTrayButtonElement(string itemId)
        {
            try
            {
                var containers = new List<IntPtr>();
                IntPtr taskbar = User32.FindWindow("Shell_TrayWnd", null);
                if (taskbar != IntPtr.Zero) containers.Add(taskbar);

                IntPtr overflowXaml = User32.FindWindow("TopLevelWindowForOverflowXamlIsland", null);
                if (overflowXaml != IntPtr.Zero) containers.Add(overflowXaml);

                IntPtr overflowClassic = User32.FindWindow("NotifyIconOverflowWindow", null);
                if (overflowClassic != IntPtr.Zero) containers.Add(overflowClassic);

                foreach (var hwnd in containers)
                {
                    var buttons = FindTrayButtons(hwnd, hwnd != taskbar);
                    foreach (var b in buttons)
                    {
                        try
                        {
                            string name = b.Current.Name?.Trim();
                            string help = b.Current.HelpText?.Trim();
                            string clean = CleanDisplayName(!string.IsNullOrEmpty(name) ? name : help);

                            if (!string.IsNullOrEmpty(itemId) &&
                                (itemId.StartsWith(clean, StringComparison.OrdinalIgnoreCase) ||
                                 (!string.IsNullOrEmpty(name) && itemId.Contains(name)) ||
                                 (!string.IsNullOrEmpty(clean) && clean.Equals(itemId, StringComparison.OrdinalIgnoreCase))))
                            {
                                return b;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Error finding tray button element for '{itemId}'.");
            }

            return null;
        }

        private static AutomationElement FindShowHiddenIconsButton()
        {
            try
            {
                IntPtr taskbar = User32.FindWindow("Shell_TrayWnd", null);
                if (taskbar == IntPtr.Zero) return null;

                var el = AutomationElement.FromHandle(taskbar);
                if (el == null) return null;

                var btnCond = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
                var buttons = el.FindAll(TreeScope.Descendants, btnCond);
                for (int i = 0; i < buttons.Count; i++)
                {
                    var b = buttons[i];
                    string name = b.Current.Name ?? string.Empty;
                    string autoId = b.Current.AutomationId ?? string.Empty;
                    if (name.IndexOf("hidden", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        autoId.IndexOf("chevron", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        autoId.IndexOf("overflow", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return b;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Error finding Show Hidden Icons button.");
            }
            return null;
        }

        private static void SimulateClick(int x, int y, bool isRightClick)
        {
            Task.Run(() =>
            {
                try
                {
                    if (x <= 0 && y <= 0)
                    {
                        Logger.Warn("Cannot simulate click: coordinates are (0, 0).");
                        return;
                    }

                    Logger.Info($"Simulating {(isRightClick ? "right" : "left")} click at ({x}, {y}).");
                    User32.SetCursorPos(x, y);
                    Thread.Sleep(20);

                    if (isRightClick)
                    {
                        User32.mouse_event(User32.MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                        Thread.Sleep(30);
                        User32.mouse_event(User32.MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                    }
                    else
                    {
                        User32.mouse_event(User32.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                        Thread.Sleep(30);
                        User32.mouse_event(User32.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to simulate mouse click on tray icon.");
                }
            });
        }

        private static bool TerminateProcess(int processId)
        {
            if (processId <= 0) return false;

            try
            {
                var proc = Process.GetProcessById(processId);
                if (proc != null && !proc.HasExited)
                {
                    Logger.Info($"Terminating process {proc.ProcessName} (PID {processId}).");
                    proc.Kill();
                    RemoveFromCache(processId);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to terminate process PID {processId}.");
            }

            return false;
        }
    }
}
