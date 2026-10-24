using System;

namespace Shared.Data
{
    public enum TrayAction
    {
        PrimaryClick = 0,    // Left-click / Invoke (Default action)
        SecondaryClick = 1,  // Right-click (Context menu)
        BringToFront = 2,    // SetForegroundWindow on main application window
        Terminate = 3,       // Kill process
        DoubleClick = 4      // Double-click (Open main UI)
    }

    public class TrayItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Tooltip { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public string IconBase64 { get; set; } = string.Empty;
        public bool IsHidden { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class TrayActionRequest
    {
        public string ItemId { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public TrayAction Action { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}
