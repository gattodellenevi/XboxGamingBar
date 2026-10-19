using NLog;
using Shared.Utilities;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Shared.Data
{
    [XmlRoot("GameProfile")]
    public struct GameProfile
    {
        public const string GLOBAL_PROFILE_NAME = "global";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [XmlElement("GameId")]
        public GameId GameId;

        [XmlElement("Use")]
        private bool use;
        public bool Use
        {
            get
            {
                if (IsGlobalProfile)
                {
                    // Logger.Warn("Per-game profile is preferred over global profile.");
                    return false;
                }

                return use;
            }
            set
            {
                if (IsGlobalProfile)
                {
                    Logger.Warn("Can't change \"Use\" property of global profile.");
                    return;
                }

                if (use != value)
                {
                    use = value;
                    Save();
                }
            }
        }


        [XmlElement("FPSLimit")]
        private int fpsLimit;
        public int FPSLimit
        {
            get { return fpsLimit; }
            set
            {
                if (fpsLimit != value)
                {
                    fpsLimit = value;
                    Save();
                }
            }
        }

        [XmlElement("FPSLimitMode")]
        private int fpsLimitMode;
        public int FPSLimitMode
        {
            get { return fpsLimitMode; }
            set
            {
                if (fpsLimitMode != value)
                {
                    fpsLimitMode = value;
                    Save();
                }
            }
        }

        [XmlElement("JudderFreeFPS")]
        private bool judderFreeFPS;
        public bool JudderFreeFPS
        {
            get { return judderFreeFPS; }
            set
            {
                if (judderFreeFPS != value)
                {
                    judderFreeFPS = value;
                    Save();
                }
            }
        }

        [XmlIgnore]
        private Dictionary<string, object> settings;

        [XmlIgnore]
        public Dictionary<string, object> Settings
        {
            get
            {
                if (settings == null)
                {
                    settings = new Dictionary<string, object>();
                    settings[SettingsRegistry.FPSLimit.Key] = fpsLimit;
                    settings[SettingsRegistry.FPSLimitMode.Key] = fpsLimitMode;
                    settings[SettingsRegistry.JudderFreeFPS.Key] = judderFreeFPS;
                }
                return settings;
            }
            set
            {
                settings = value;
            }
        }

        public bool TryGetSetting<T>(ISettingDefinition<T> def, out T val)
        {
            if (Settings != null && Settings.TryGetValue(def.Key, out var rawVal))
            {
                val = (T)def.Sanitize(rawVal);
                return true;
            }
            if (def.Function == Enums.Function.FPSLimit) { val = (T)(object)FPSLimit; return true; }
            if (def.Function == Enums.Function.FPSLimitMode) { val = (T)(object)FPSLimitMode; return true; }
            if (def.Function == Enums.Function.JudderFreeFPS) { val = (T)(object)JudderFreeFPS; return true; }
            val = def.DefaultValue;
            return false;
        }

        public void SetSetting<T>(ISettingDefinition<T> def, T val)
        {
            T sanitized = def.Sanitize(val);
            Settings[def.Key] = sanitized;
            if (def.Function == Enums.Function.FPSLimit && sanitized is int fl) fpsLimit = fl;
            else if (def.Function == Enums.Function.FPSLimitMode && sanitized is int flm) fpsLimitMode = flm;
            else if (def.Function == Enums.Function.JudderFreeFPS && sanitized is bool jf) judderFreeFPS = jf;
            Save();
        }

        [XmlIgnore]
        public string Path;

        public bool IsGlobalProfile { get { return string.Compare(GameId.Name, GLOBAL_PROFILE_NAME) == 0; } }

        [XmlIgnore]
        private IDictionary<GameId, GameProfile> cache;
        [XmlIgnore]
        public IDictionary<GameId, GameProfile> Cache
        {
            get { return cache; }
            set { cache = value; }
        }

        public GameProfile(GameProfileOptions options)
        {
            GameId = new GameId(options.GameName, options.GamePath);
            use = options.InUse;

            fpsLimit = options.FPSLimit;
            fpsLimitMode = options.FPSLimitMode;
            judderFreeFPS = options.JudderFreeFPS;
            Path = options.ProfilePath;
            cache = options.Cache;
            settings = new Dictionary<string, object>();
            settings[SettingsRegistry.FPSLimit.Key] = fpsLimit;
            settings[SettingsRegistry.FPSLimitMode.Key] = fpsLimitMode;
            settings[SettingsRegistry.JudderFreeFPS.Key] = judderFreeFPS;
        }

        public bool IsValid()
        {
            return GameId.IsValid();
        }

        public static bool operator ==(GameProfile g1, GameProfile g2)
        {
            return g1.GameId == g2.GameId &&
                   g1.use == g2.use &&
                   g1.fpsLimit == g2.fpsLimit &&
                   g1.fpsLimitMode == g2.fpsLimitMode &&
                   g1.judderFreeFPS == g2.judderFreeFPS;
        }

        public static bool operator !=(GameProfile p1, GameProfile p2)
        {
            return !(p1 == p2);
        }

        public override bool Equals(object obj)
        {
            if (obj is GameProfile other)
            {
                return this == other;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return GameId.GetHashCode();
        }

        // Export to xml string.
        public override string ToString()
        {
            return XmlHelper.ToXMLString(this, true);
        }

        public void Save()
        {
            if (cache != null)
            {
                cache[GameId] = this;
            }
        }
    }
}
