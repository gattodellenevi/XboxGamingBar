namespace Shared.Data
{
    using Shared.Utilities;
    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// Contains information related to connected game controller battery status.
    /// </summary>
    [XmlRoot("ControllerBattery")]
    public struct ControllerBattery
    {
        [XmlElement("IsConnected")]
        public bool IsConnected;

        [XmlElement("BatteryLevel")]
        public int BatteryLevel;

        [XmlElement("LevelCategory")]
        public string LevelCategory;

        [XmlElement("BatteryType")]
        public string BatteryType;

        [XmlElement("IsCharging")]
        public bool IsCharging;

        [XmlElement("DeviceName")]
        public string DeviceName;

        [XmlArray("Controllers")]
        [XmlArrayItem("ControllerBatteryInfo")]
        public ControllerBatteryInfo[] Controllers;

        public bool IsValid() => IsConnected;

        public static ControllerBattery Disconnected => new ControllerBattery
        {
            IsConnected = false,
            BatteryLevel = -1,
            LevelCategory = "Disconnected",
            BatteryType = "Disconnected",
            IsCharging = false,
            DeviceName = "No Controller",
            Controllers = Array.Empty<ControllerBatteryInfo>()
        };

        public static bool operator ==(ControllerBattery b1, ControllerBattery b2)
        {
            if (b1.IsConnected != b2.IsConnected ||
                b1.BatteryLevel != b2.BatteryLevel ||
                !string.Equals(b1.LevelCategory, b2.LevelCategory, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(b1.BatteryType, b2.BatteryType, StringComparison.OrdinalIgnoreCase) ||
                b1.IsCharging != b2.IsCharging ||
                !string.Equals(b1.DeviceName, b2.DeviceName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var c1 = b1.Controllers;
            var c2 = b2.Controllers;
            if (ReferenceEquals(c1, c2)) return true;
            int len1 = c1?.Length ?? 0;
            int len2 = c2?.Length ?? 0;
            if (len1 != len2) return false;
            if (c1 != null && c2 != null)
            {
                for (int i = 0; i < c1.Length; i++)
                {
                    if (c1[i] != c2[i]) return false;
                }
            }
            return true;
        }

        public static bool operator !=(ControllerBattery b1, ControllerBattery b2)
        {
            return !(b1 == b2);
        }

        public override bool Equals(object obj)
        {
            if (obj is ControllerBattery other)
            {
                return this == other;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + IsConnected.GetHashCode();
                hash = hash * 31 + BatteryLevel.GetHashCode();
                hash = hash * 31 + (LevelCategory != null ? LevelCategory.GetHashCode() : 0);
                hash = hash * 31 + (BatteryType != null ? BatteryType.GetHashCode() : 0);
                hash = hash * 31 + IsCharging.GetHashCode();
                hash = hash * 31 + (DeviceName != null ? DeviceName.GetHashCode() : 0);
                if (Controllers != null)
                {
                    for (int i = 0; i < Controllers.Length; i++)
                    {
                        hash = hash * 31 + Controllers[i].GetHashCode();
                    }
                }
                return hash;
            }
        }

        public override string ToString()
        {
            return XmlHelper.ToXMLString(this, true);
        }
    }

    [XmlRoot("ControllerBatteryInfo")]
    public struct ControllerBatteryInfo
    {
        [XmlElement("Index")]
        public int Index;

        [XmlElement("IsConnected")]
        public bool IsConnected;

        [XmlElement("BatteryLevel")]
        public int BatteryLevel;

        [XmlElement("LevelCategory")]
        public string LevelCategory;

        [XmlElement("BatteryType")]
        public string BatteryType;

        [XmlElement("IsCharging")]
        public bool IsCharging;

        [XmlElement("DeviceName")]
        public string DeviceName;

        public static bool operator ==(ControllerBatteryInfo b1, ControllerBatteryInfo b2)
        {
            return b1.Index == b2.Index &&
                   b1.IsConnected == b2.IsConnected &&
                   b1.BatteryLevel == b2.BatteryLevel &&
                   string.Equals(b1.LevelCategory, b2.LevelCategory, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(b1.BatteryType, b2.BatteryType, StringComparison.OrdinalIgnoreCase) &&
                   b1.IsCharging == b2.IsCharging &&
                   string.Equals(b1.DeviceName, b2.DeviceName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator !=(ControllerBatteryInfo b1, ControllerBatteryInfo b2)
        {
            return !(b1 == b2);
        }

        public override bool Equals(object obj)
        {
            if (obj is ControllerBatteryInfo other)
            {
                return this == other;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Index.GetHashCode();
                hash = hash * 31 + IsConnected.GetHashCode();
                hash = hash * 31 + BatteryLevel.GetHashCode();
                hash = hash * 31 + (LevelCategory != null ? LevelCategory.GetHashCode() : 0);
                hash = hash * 31 + (BatteryType != null ? BatteryType.GetHashCode() : 0);
                hash = hash * 31 + IsCharging.GetHashCode();
                hash = hash * 31 + (DeviceName != null ? DeviceName.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
