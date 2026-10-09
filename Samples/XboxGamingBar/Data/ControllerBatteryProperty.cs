using NLog;
using Shared.Data;
using Shared.Enums;
using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace XboxGamingBar.Data
{
    internal class QuickGamepadButtonUI
    {
        public Button Button;
        public FontIcon Icon;
        public TextBlock Name;
        public TextBlock Subtitle;
        public Border Badge;
        public TextBlock BadgeText;
        public FontIcon FlyoutIcon;
        public TextBlock FlyoutName;
        public TextBlock FlyoutSubtitle;
        public TextBlock FlyoutPercent;
        public TextBlock FlyoutType;
        public TextBlock FlyoutStatus;
    }

    internal class QuickGamepadUIElements
    {
        public StackPanel Section;
        public QuickGamepadButtonUI[] Buttons;
    }

    internal class ControllerBatteryProperty : WidgetProperty<ControllerBattery>
    {
        private static new readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Hero Card elements
        private readonly Button heroBadgeButton;
        private readonly FontIcon heroBadgeIcon;
        private readonly TextBlock heroBadgeText;

        // Hero Flyout elements
        private readonly FontIcon flyoutIcon;
        private readonly TextBlock flyoutName;
        private readonly TextBlock flyoutSubtitle;
        private readonly TextBlock flyoutPercent;
        private readonly TextBlock flyoutType;
        private readonly TextBlock flyoutStatus;

        // System Tab elements
        private readonly Border systemBadgeBorder;
        private readonly TextBlock systemBadgeText;
        private readonly TextBlock systemSubtitleText;

        // Quick Access elements
        private readonly QuickGamepadUIElements quickGamepadUI;

        private readonly Page owner;

        private static readonly Color GreenColor = Color.FromArgb(255, 16, 124, 65);   // Xbox Green #107C41
        private static readonly Color AmberColor = Color.FromArgb(255, 245, 158, 11);  // Amber #F59E0B
        private static readonly Color RedColor = Color.FromArgb(255, 239, 68, 68);     // Red #EF4444
        private static readonly Color GrayColor = Color.FromArgb(255, 90, 90, 90);     // Muted Gray #5A5A5A
        private static readonly Color WhiteColor = Colors.White;

        public ControllerBatteryProperty(
            Button inHeroBadgeButton,
            FontIcon inHeroBadgeIcon,
            TextBlock inHeroBadgeText,
            FontIcon inFlyoutIcon,
            TextBlock inFlyoutName,
            TextBlock inFlyoutSubtitle,
            TextBlock inFlyoutPercent,
            TextBlock inFlyoutType,
            TextBlock inFlyoutStatus,
            Border inSystemBadgeBorder,
            TextBlock inSystemBadgeText,
            TextBlock inSystemSubtitleText,
            QuickGamepadUIElements inQuickGamepadUI,
            Page inOwner)
            : base(ControllerBattery.Disconnected, null, Function.ControllerBattery)
        {
            heroBadgeButton = inHeroBadgeButton;
            heroBadgeIcon = inHeroBadgeIcon;
            heroBadgeText = inHeroBadgeText;
            flyoutIcon = inFlyoutIcon;
            flyoutName = inFlyoutName;
            flyoutSubtitle = inFlyoutSubtitle;
            flyoutPercent = inFlyoutPercent;
            flyoutType = inFlyoutType;
            flyoutStatus = inFlyoutStatus;
            systemBadgeBorder = inSystemBadgeBorder;
            systemBadgeText = inSystemBadgeText;
            systemSubtitleText = inSystemSubtitleText;
            quickGamepadUI = inQuickGamepadUI;
            owner = inOwner;

            // Listen for local gamepad events as a fallback if helper is not yet connected
            try
            {
                global::Windows.Gaming.Input.Gamepad.GamepadAdded += OnGamepadChanged;
                global::Windows.Gaming.Input.Gamepad.GamepadRemoved += OnGamepadChanged;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to register Windows.Gaming.Input event listeners.");
            }

            // Initial UI state
            UpdateUI(Value);
        }

        private void OnGamepadChanged(object sender, global::Windows.Gaming.Input.Gamepad e)
        {
            if (App.Connection == null)
            {
                var localBattery = QueryLocalGamepad();
                SetValue(localBattery);
            }
        }

        private static void DetermineGamepadDetails(
            global::Windows.Gaming.Input.Gamepad gp,
            int index,
            out string deviceName,
            out string connectionType,
            out bool isWireless)
        {
            deviceName = $"Xbox Controller {index + 1}";
            connectionType = "Xbox Wireless";
            isWireless = true;

            try
            {
                var raw = global::Windows.Gaming.Input.RawGameController.FromGameController(gp);
                if (raw != null)
                {
                    isWireless = raw.IsWireless;
                    if (!isWireless)
                    {
                        connectionType = "Wired";
                    }

                    ushort vid = raw.HardwareVendorId;
                    ushort pid = raw.HardwareProductId;
                    string nonRoamableId = raw.NonRoamableId ?? string.Empty;

                    bool hasBluetoothMarker =
                        nonRoamableId.IndexOf("BTH", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        nonRoamableId.IndexOf("BLUETOOTH", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        nonRoamableId.IndexOf("{00001124-0000-1000-8000-00805F9B34FB}", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (vid == 0x045E) // Microsoft
                    {
                        // Known Microsoft Xbox Bluetooth PIDs:
                        // 0x02E0, 0x02FD: Xbox One Wireless Controller Bluetooth
                        // 0x0B05: Xbox Elite Wireless Controller Series 2 Bluetooth
                        // 0x0B13: Xbox Series X|S Wireless Controller Bluetooth (BLE)
                        // 0x0B21, 0x0B22: Xbox Adaptive Controller Bluetooth
                        bool isKnownBluetoothPid = pid == 0x02E0 || pid == 0x02FD || pid == 0x0B05 || pid == 0x0B13 || pid == 0x0B21 || pid == 0x0B22;

                        if (isWireless)
                        {
                            connectionType = (isKnownBluetoothPid || hasBluetoothMarker) ? "Bluetooth" : "Xbox Wireless";
                        }

                        // Determine friendly device name
                        if (!string.IsNullOrWhiteSpace(raw.DisplayName) &&
                            !raw.DisplayName.Equals("HID-compliant game controller", StringComparison.OrdinalIgnoreCase) &&
                            !raw.DisplayName.Equals("Game Controller", StringComparison.OrdinalIgnoreCase))
                        {
                            deviceName = raw.DisplayName;
                        }
                        else
                        {
                            switch (pid)
                            {
                                case 0x0B12:
                                case 0x0B13:
                                    deviceName = "Xbox Wireless Controller"; // Xbox Series X|S
                                    break;
                                case 0x0B00:
                                case 0x0B05:
                                    deviceName = "Xbox Elite Wireless Controller Series 2";
                                    break;
                                case 0x02E3:
                                    deviceName = "Xbox Elite Wireless Controller";
                                    break;
                                case 0x0B20:
                                case 0x0B21:
                                case 0x0B22:
                                    deviceName = "Xbox Adaptive Controller";
                                    break;
                                case 0x028E:
                                case 0x028F:
                                case 0x02A1:
                                    deviceName = "Xbox 360 Controller";
                                    break;
                                default:
                                    deviceName = "Xbox Wireless Controller";
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (isWireless)
                        {
                            connectionType = hasBluetoothMarker ? "Bluetooth" : "Wireless";
                        }

                        if (!string.IsNullOrWhiteSpace(raw.DisplayName))
                        {
                            deviceName = raw.DisplayName;
                        }
                        else
                        {
                            deviceName = $"Gamepad {index + 1}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to resolve RawGameController connection details.");
            }
        }

        private ControllerBattery QueryLocalGamepad()
        {
            try
            {
                var gamepads = global::Windows.Gaming.Input.Gamepad.Gamepads;
                if (gamepads != null && gamepads.Count > 0)
                {
                    var list = new System.Collections.Generic.List<ControllerBatteryInfo>();
                    for (int i = 0; i < gamepads.Count && list.Count < 4; i++)
                    {
                        var gp = gamepads[i];
                        DetermineGamepadDetails(gp, i, out string deviceName, out string connectionType, out bool isWireless);

                        var report = gp.TryGetBatteryReport();
                        if (report != null && report.Status != global::Windows.System.Power.BatteryStatus.NotPresent)
                        {
                            if (!isWireless)
                            {
                                list.Add(new ControllerBatteryInfo
                                {
                                    Index = i,
                                    IsConnected = true,
                                    BatteryLevel = 100,
                                    LevelCategory = "Wired",
                                    BatteryType = "Wired",
                                    IsCharging = true,
                                    DeviceName = deviceName
                                });
                                continue;
                            }

                            if (report.RemainingCapacityInMilliwattHours.HasValue &&
                                report.FullChargeCapacityInMilliwattHours.HasValue &&
                                report.FullChargeCapacityInMilliwattHours.Value > 0)
                            {
                                int pct = (int)Math.Clamp(Math.Round((double)report.RemainingCapacityInMilliwattHours.Value * 100.0 / report.FullChargeCapacityInMilliwattHours.Value), 0, 100);
                                bool isCharging = report.Status == global::Windows.System.Power.BatteryStatus.Charging;
                                string cat = pct >= 70 ? "Full" : (pct >= 35 ? "Medium" : (pct >= 15 ? "Low" : "Empty"));
                                list.Add(new ControllerBatteryInfo
                                {
                                    Index = i,
                                    IsConnected = true,
                                    BatteryLevel = pct,
                                    LevelCategory = cat,
                                    BatteryType = isCharging ? "Wired" : connectionType,
                                    IsCharging = isCharging,
                                    DeviceName = deviceName
                                });
                                continue;
                            }
                            else if (report.Status == global::Windows.System.Power.BatteryStatus.Charging)
                            {
                                list.Add(new ControllerBatteryInfo
                                {
                                    Index = i,
                                    IsConnected = true,
                                    BatteryLevel = 100,
                                    LevelCategory = "Wired",
                                    BatteryType = "Wired",
                                    IsCharging = true,
                                    DeviceName = deviceName
                                });
                                continue;
                            }
                        }

                        list.Add(new ControllerBatteryInfo
                        {
                            Index = i,
                            IsConnected = true,
                            BatteryLevel = 100,
                            LevelCategory = "Connected",
                            BatteryType = isWireless ? connectionType : "Wired",
                            IsCharging = false,
                            DeviceName = deviceName
                        });
                    }

                    if (list.Count > 0)
                    {
                        var primary = list[0];
                        return new ControllerBattery
                        {
                            IsConnected = true,
                            BatteryLevel = primary.BatteryLevel,
                            LevelCategory = primary.LevelCategory,
                            BatteryType = primary.BatteryType,
                            IsCharging = primary.IsCharging,
                            DeviceName = primary.DeviceName,
                            Controllers = list.ToArray()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to query local gamepad.");
            }

            return ControllerBattery.Disconnected;
        }

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            var result = base.SetValue(newValue, updatedTime);
            UpdateUI(Value);
            return result;
        }

        public override async Task Sync()
        {
            await base.Sync();
            UpdateUI(Value);
        }

        protected override bool ShouldSendNotifyMessage() => false;

        private void UpdateUI(ControllerBattery battery)
        {
            if (owner == null) return;

            var _ = owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (battery.IsConnected)
                {
                    // Select glyph and color
                    string glyph;
                    Color accentColor;
                    string badgeText;

                    if (battery.IsCharging || string.Equals(battery.BatteryType, "Wired", StringComparison.OrdinalIgnoreCase))
                    {
                        glyph = "\uEA93"; // Lightning bolt / plugged
                        accentColor = GreenColor;
                        badgeText = "Wired";
                    }
                    else if (battery.BatteryLevel >= 70 || string.Equals(battery.LevelCategory, "Full", StringComparison.OrdinalIgnoreCase))
                    {
                        glyph = "\uEBB5"; // Battery 10 (Full)
                        accentColor = GreenColor;
                        badgeText = battery.BatteryLevel >= 0 ? $"{battery.BatteryLevel}%" : "Full";
                    }
                    else if (battery.BatteryLevel >= 35 || string.Equals(battery.LevelCategory, "Medium", StringComparison.OrdinalIgnoreCase))
                    {
                        glyph = "\uEBAE"; // Battery 4 (Medium)
                        accentColor = GreenColor;
                        badgeText = battery.BatteryLevel >= 0 ? $"{battery.BatteryLevel}%" : "Med";
                    }
                    else if (battery.BatteryLevel >= 15 || string.Equals(battery.LevelCategory, "Low", StringComparison.OrdinalIgnoreCase))
                    {
                        glyph = "\uEBAB"; // Battery 2 (Low)
                        accentColor = AmberColor;
                        badgeText = battery.BatteryLevel >= 0 ? $"{battery.BatteryLevel}%" : "Low";
                    }
                    else
                    {
                        glyph = "\uEBAA"; // Battery 1 (Critical)
                        accentColor = RedColor;
                        badgeText = battery.BatteryLevel >= 0 ? $"{battery.BatteryLevel}%" : "Crit";
                    }

                    // 1. Hero Card Badge
                    if (heroBadgeButton != null)
                    {
                        heroBadgeButton.Visibility = Visibility.Visible;
                    }
                    if (heroBadgeIcon != null)
                    {
                        heroBadgeIcon.Glyph = glyph;
                        heroBadgeIcon.Foreground = new SolidColorBrush(accentColor);
                    }
                    if (heroBadgeText != null)
                    {
                        heroBadgeText.Text = badgeText;
                    }

                    // 2. Hero Card Flyout
                    if (flyoutIcon != null)
                    {
                        flyoutIcon.Glyph = glyph;
                        flyoutIcon.Foreground = new SolidColorBrush(accentColor);
                    }
                    if (flyoutName != null)
                    {
                        flyoutName.Text = string.IsNullOrEmpty(battery.DeviceName) ? "Controller" : battery.DeviceName;
                    }
                    if (flyoutSubtitle != null)
                    {
                        flyoutSubtitle.Text = battery.IsCharging ? "Charging / Wired" : $"{battery.BatteryType} Connection";
                    }
                    if (flyoutPercent != null)
                    {
                        flyoutPercent.Text = battery.BatteryLevel >= 0 ? $"{battery.BatteryLevel}%" : battery.LevelCategory;
                    }
                    if (flyoutType != null)
                    {
                        flyoutType.Text = battery.BatteryType;
                    }
                    if (flyoutStatus != null)
                    {
                        flyoutStatus.Text = battery.IsCharging ? "Charging" : (string.Equals(battery.BatteryType, "Wired", StringComparison.OrdinalIgnoreCase) ? "Wired" : "Discharging");
                    }

                    // 3. System Tab Status Row
                    if (systemBadgeBorder != null)
                    {
                        systemBadgeBorder.Background = new SolidColorBrush(accentColor);
                        systemBadgeBorder.BorderBrush = new SolidColorBrush(accentColor);
                    }
                    if (systemBadgeText != null)
                    {
                        systemBadgeText.Text = string.Equals(battery.BatteryType, "Wired", StringComparison.OrdinalIgnoreCase)
                            ? "WIRED"
                            : (battery.BatteryLevel >= 0 ? $"{battery.BatteryLevel}% {battery.LevelCategory.ToUpper()}" : battery.LevelCategory.ToUpper());
                        systemBadgeText.Foreground = new SolidColorBrush(WhiteColor);
                    }
                    if (systemSubtitleText != null)
                    {
                        systemSubtitleText.Text = $"{battery.DeviceName} ({battery.BatteryType})";
                    }

                    // 4. Quick Access Gamepad Buttons
                    if (quickGamepadUI != null)
                    {
                        var controllers = battery.Controllers;
                        if ((controllers == null || controllers.Length == 0) && battery.IsConnected)
                        {
                            controllers = new[]
                            {
                                new ControllerBatteryInfo
                                {
                                    Index = 0,
                                    IsConnected = true,
                                    BatteryLevel = battery.BatteryLevel,
                                    LevelCategory = battery.LevelCategory,
                                    BatteryType = battery.BatteryType,
                                    IsCharging = battery.IsCharging,
                                    DeviceName = battery.DeviceName
                                }
                            };
                        }

                        int connectedCount = (controllers != null) ? controllers.Length : 0;
                        if (connectedCount > 0)
                        {
                            if (quickGamepadUI.Section != null)
                            {
                                quickGamepadUI.Section.Visibility = Visibility.Visible;
                            }

                            for (int i = 0; i < quickGamepadUI.Buttons.Length; i++)
                            {
                                var ui = quickGamepadUI.Buttons[i];
                                if (ui == null) continue;

                                if (i < connectedCount && controllers[i].IsConnected)
                                {
                                    var info = controllers[i];
                                    ui.Button.Visibility = Visibility.Visible;

                                    string btnGlyph;
                                    Color btnColor;
                                    string btnBadgeText;

                                    if (info.IsCharging || string.Equals(info.BatteryType, "Wired", StringComparison.OrdinalIgnoreCase))
                                    {
                                        btnGlyph = "\uEA93"; // Lightning bolt / plugged
                                        btnColor = GreenColor;
                                        btnBadgeText = "Wired";
                                    }
                                    else if (info.BatteryLevel >= 70 || string.Equals(info.LevelCategory, "Full", StringComparison.OrdinalIgnoreCase))
                                    {
                                        btnGlyph = "\uEBB5"; // Battery 10 (Full)
                                        btnColor = GreenColor;
                                        btnBadgeText = info.BatteryLevel >= 0 ? $"{info.BatteryLevel}%" : "Full";
                                    }
                                    else if (info.BatteryLevel >= 35 || string.Equals(info.LevelCategory, "Medium", StringComparison.OrdinalIgnoreCase))
                                    {
                                        btnGlyph = "\uEBAE"; // Battery 4 (Medium)
                                        btnColor = GreenColor;
                                        btnBadgeText = info.BatteryLevel >= 0 ? $"{info.BatteryLevel}%" : "Med";
                                    }
                                    else if (info.BatteryLevel >= 15 || string.Equals(info.LevelCategory, "Low", StringComparison.OrdinalIgnoreCase))
                                    {
                                        btnGlyph = "\uEBAB"; // Battery 2 (Low)
                                        btnColor = AmberColor;
                                        btnBadgeText = info.BatteryLevel >= 0 ? $"{info.BatteryLevel}%" : "Low";
                                    }
                                    else
                                    {
                                        btnGlyph = "\uEBAA"; // Battery 1 (Critical)
                                        btnColor = RedColor;
                                        btnBadgeText = info.BatteryLevel >= 0 ? $"{info.BatteryLevel}%" : "Crit";
                                    }

                                    string displayName = string.IsNullOrEmpty(info.DeviceName)
                                        ? $"Controller {i + 1}"
                                        : (connectedCount > 1 && !info.DeviceName.EndsWith((i + 1).ToString()) ? $"{info.DeviceName} {i + 1}" : info.DeviceName);

                                    // Button UI
                                    if (ui.Icon != null)
                                    {
                                        ui.Icon.Glyph = btnGlyph;
                                        ui.Icon.Foreground = new SolidColorBrush(btnColor);
                                    }
                                    if (ui.Name != null)
                                    {
                                        ui.Name.Text = displayName;
                                    }
                                    if (ui.Subtitle != null)
                                    {
                                        ui.Subtitle.Text = info.IsCharging ? "Charging" : (string.Equals(info.BatteryType, "Wired", StringComparison.OrdinalIgnoreCase) ? "Wired" : $"{info.BatteryType}");
                                    }
                                    if (ui.Badge != null)
                                    {
                                        ui.Badge.Background = new SolidColorBrush(btnColor);
                                        ui.Badge.BorderBrush = new SolidColorBrush(btnColor);
                                    }
                                    if (ui.BadgeText != null)
                                    {
                                        ui.BadgeText.Text = btnBadgeText;
                                        ui.BadgeText.Foreground = new SolidColorBrush(WhiteColor);
                                    }

                                    // Flyout UI
                                    if (ui.FlyoutIcon != null)
                                    {
                                        ui.FlyoutIcon.Glyph = btnGlyph;
                                        ui.FlyoutIcon.Foreground = new SolidColorBrush(btnColor);
                                    }
                                    if (ui.FlyoutName != null)
                                    {
                                        ui.FlyoutName.Text = displayName;
                                    }
                                    if (ui.FlyoutSubtitle != null)
                                    {
                                        ui.FlyoutSubtitle.Text = info.IsCharging ? "Charging / Wired" : $"{info.BatteryType} Connection";
                                    }
                                    if (ui.FlyoutPercent != null)
                                    {
                                        ui.FlyoutPercent.Text = info.BatteryLevel >= 0 ? $"{info.BatteryLevel}%" : info.LevelCategory;
                                    }
                                    if (ui.FlyoutType != null)
                                    {
                                        ui.FlyoutType.Text = info.BatteryType;
                                    }
                                    if (ui.FlyoutStatus != null)
                                    {
                                        ui.FlyoutStatus.Text = info.IsCharging ? "Charging" : (string.Equals(info.BatteryType, "Wired", StringComparison.OrdinalIgnoreCase) ? "Wired" : "Discharging");
                                    }
                                }
                                else
                                {
                                    ui.Button.Visibility = Visibility.Collapsed;
                                }
                            }
                        }
                        else
                        {
                            if (quickGamepadUI.Section != null)
                            {
                                quickGamepadUI.Section.Visibility = Visibility.Collapsed;
                            }
                            for (int i = 0; i < quickGamepadUI.Buttons.Length; i++)
                            {
                                if (quickGamepadUI.Buttons[i] != null)
                                {
                                    quickGamepadUI.Buttons[i].Button.Visibility = Visibility.Collapsed;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Disconnected state
                    if (heroBadgeButton != null)
                    {
                        heroBadgeButton.Visibility = Visibility.Collapsed;
                    }
                    if (systemBadgeBorder != null)
                    {
                        systemBadgeBorder.Background = new SolidColorBrush(GrayColor);
                        systemBadgeBorder.BorderBrush = new SolidColorBrush(GrayColor);
                    }
                    if (systemBadgeText != null)
                    {
                        systemBadgeText.Text = "DISCONNECTED";
                        systemBadgeText.Foreground = new SolidColorBrush(WhiteColor);
                    }
                    if (systemSubtitleText != null)
                    {
                        systemSubtitleText.Text = "No game controller detected";
                    }

                    // 4. Quick Access Gamepad Buttons (Disconnected)
                    if (quickGamepadUI != null)
                    {
                        if (quickGamepadUI.Section != null)
                        {
                            quickGamepadUI.Section.Visibility = Visibility.Collapsed;
                        }
                        for (int i = 0; i < quickGamepadUI.Buttons.Length; i++)
                        {
                            if (quickGamepadUI.Buttons[i] != null)
                            {
                                quickGamepadUI.Buttons[i].Button.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                }
            });
        }
    }
}
