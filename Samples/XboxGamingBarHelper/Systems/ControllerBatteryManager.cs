using NLog;
using Shared.Data;
using System;
using System.Runtime.InteropServices;

namespace XboxGamingBarHelper.Systems
{
    public static class ControllerBatteryManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const byte BATTERY_DEVTYPE_GAMEPAD = 0x00;
        private const byte BATTERY_TYPE_DISCONNECTED = 0x00;
        private const byte BATTERY_TYPE_WIRED = 0x01;
        private const byte BATTERY_TYPE_ALKALINE = 0x02;
        private const byte BATTERY_TYPE_NIMH = 0x03;
        private const byte BATTERY_TYPE_UNKNOWN = 0xFF;

        private const byte BATTERY_LEVEL_EMPTY = 0x00;
        private const byte BATTERY_LEVEL_LOW = 0x01;
        private const byte BATTERY_LEVEL_MEDIUM = 0x02;
        private const byte BATTERY_LEVEL_FULL = 0x03;

        [StructLayout(LayoutKind.Sequential)]
        public struct XINPUT_BATTERY_INFORMATION
        {
            public byte BatteryType;
            public byte BatteryLevel;
        }

        private static class NativeMethods
        {
            [DllImport("xinput1_4.dll", EntryPoint = "XInputGetBatteryInformation", SetLastError = false)]
            public static extern uint XInputGetBatteryInformation14(uint dwUserIndex, byte devType, ref XINPUT_BATTERY_INFORMATION pBatteryInformation);

            [DllImport("xinput1_3.dll", EntryPoint = "XInputGetBatteryInformation", SetLastError = false)]
            public static extern uint XInputGetBatteryInformation13(uint dwUserIndex, byte devType, ref XINPUT_BATTERY_INFORMATION pBatteryInformation);
        }

        private static bool isXInput14Available = true;

        private static uint SafeXInputGetBattery(uint userIndex, byte devType, ref XINPUT_BATTERY_INFORMATION info)
        {
            if (isXInput14Available)
            {
                try
                {
                    return NativeMethods.XInputGetBatteryInformation14(userIndex, devType, ref info);
                }
                catch (DllNotFoundException)
                {
                    isXInput14Available = false;
                }
                catch (EntryPointNotFoundException)
                {
                    isXInput14Available = false;
                }
            }

            try
            {
                return NativeMethods.XInputGetBatteryInformation13(userIndex, devType, ref info);
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to call XInputGetBatteryInformation.");
                return 1167; // ERROR_DEVICE_NOT_CONNECTED
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

        public static ControllerBattery GetCurrentBatteryStatus()
        {
            // 1. Try Windows.Gaming.Input for detailed battery percentages
            try
            {
                var gamepads = global::Windows.Gaming.Input.Gamepad.Gamepads;
                if (gamepads != null && gamepads.Count > 0)
                {
                    for (int i = 0; i < gamepads.Count; i++)
                    {
                        var gp = gamepads[i];
                        DetermineGamepadDetails(gp, i, out string deviceName, out string connectionType, out bool isWireless);

                        var report = gp.TryGetBatteryReport();
                        if (report != null && report.Status != global::Windows.System.Power.BatteryStatus.NotPresent)
                        {
                            if (!isWireless)
                            {
                                return new ControllerBattery
                                {
                                    IsConnected = true,
                                    BatteryLevel = 100,
                                    LevelCategory = "Wired",
                                    BatteryType = "Wired",
                                    IsCharging = true,
                                    DeviceName = deviceName
                                };
                            }

                            if (report.RemainingCapacityInMilliwattHours.HasValue &&
                                report.FullChargeCapacityInMilliwattHours.HasValue &&
                                report.FullChargeCapacityInMilliwattHours.Value > 0)
                            {
                                int pct = (int)Math.Clamp(Math.Round((double)report.RemainingCapacityInMilliwattHours.Value * 100.0 / report.FullChargeCapacityInMilliwattHours.Value), 0, 100);
                                bool isCharging = report.Status == global::Windows.System.Power.BatteryStatus.Charging;
                                string cat = pct >= 70 ? "Full" : (pct >= 35 ? "Medium" : (pct >= 15 ? "Low" : "Empty"));
                                return new ControllerBattery
                                {
                                    IsConnected = true,
                                    BatteryLevel = pct,
                                    LevelCategory = cat,
                                    BatteryType = isCharging ? "Wired" : connectionType,
                                    IsCharging = isCharging,
                                    DeviceName = deviceName
                                };
                            }
                            else if (report.Status == global::Windows.System.Power.BatteryStatus.Charging)
                            {
                                return new ControllerBattery
                                {
                                    IsConnected = true,
                                    BatteryLevel = 100,
                                    LevelCategory = "Wired",
                                    BatteryType = "Wired",
                                    IsCharging = true,
                                    DeviceName = deviceName
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Windows.Gaming.Input battery query exception.");
            }

            // 2. Query XInput for wireless or wired controllers (User index 0 - 3)
            for (uint userIndex = 0; userIndex < 4; userIndex++)
            {
                var xinfo = new XINPUT_BATTERY_INFORMATION();
                uint res = SafeXInputGetBattery(userIndex, BATTERY_DEVTYPE_GAMEPAD, ref xinfo);

                if (res == 0 && xinfo.BatteryType != BATTERY_TYPE_DISCONNECTED)
                {
                    if (xinfo.BatteryType == BATTERY_TYPE_WIRED)
                    {
                        return new ControllerBattery
                        {
                            IsConnected = true,
                            BatteryLevel = 100,
                            LevelCategory = "Wired",
                            BatteryType = "Wired",
                            IsCharging = true,
                            DeviceName = $"Xbox Controller {userIndex + 1}"
                        };
                    }

                    int level = 95;
                    string cat = "Full";
                    switch (xinfo.BatteryLevel)
                    {
                        case BATTERY_LEVEL_EMPTY:
                            level = 10;
                            cat = "Empty";
                            break;
                        case BATTERY_LEVEL_LOW:
                            level = 25;
                            cat = "Low";
                            break;
                        case BATTERY_LEVEL_MEDIUM:
                            level = 60;
                            cat = "Medium";
                            break;
                        case BATTERY_LEVEL_FULL:
                        default:
                            level = 95;
                            cat = "Full";
                            break;
                    }

                    return new ControllerBattery
                    {
                        IsConnected = true,
                        BatteryLevel = level,
                        LevelCategory = cat,
                        BatteryType = "Xbox Wireless",
                        IsCharging = false,
                        DeviceName = $"Xbox Controller {userIndex + 1}"
                    };
                }
            }

            return ControllerBattery.Disconnected;
        }
    }
}
