using NLog;
using Shared.Data;
using Shared.Enums;
using Shared.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using XboxGamingBarHelper.Utilities;

namespace XboxGamingBarHelper.Profile
{
    internal class ProfileStore
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const string SETTINGS_JSON_NAME = "settings.json";
        private const string SETTINGS_XML_NAME = "settings.xml";
        private const string GLOBAL_PROFILE_JSON = "global.json";
        private const string GLOBAL_PROFILE_XML = "global.xml";
        private const string PROFILES_FOLDER_NAME = "profiles";
        private const string JSON_EXTENSION = ".json";
        private const string XML_EXTENSION = ".xml";

        private readonly string baseFolderPath;
        private readonly string profilesFolderPath;
        private readonly string settingsJsonPath;
        private readonly string globalProfileJsonPath;

        private readonly Dictionary<string, object> globalSettings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyDictionary<string, object> GlobalSettings => globalSettings;

        private GameProfile globalProfile;
        public GameProfile GlobalProfile => globalProfile;

        private readonly Dictionary<GameId, GameProfile> gameProfiles = new Dictionary<GameId, GameProfile>();
        public IReadOnlyDictionary<GameId, GameProfile> GameProfiles => gameProfiles;

        private GameProfile activeProfile;
        public GameProfile ActiveProfile => activeProfile;

        private bool isPerGameProfileActive = false;
        public bool IsPerGameProfileActive => isPerGameProfileActive;

        private GameId activeGameId = new GameId();
        public GameId ActiveGameId => activeGameId;

        public ProfileStore()
        {
            baseFolderPath = PathHelper.GetLocalFolderPath();
            profilesFolderPath = Path.Combine(baseFolderPath, PROFILES_FOLDER_NAME);
            settingsJsonPath = Path.Combine(baseFolderPath, SETTINGS_JSON_NAME);
            globalProfileJsonPath = Path.Combine(baseFolderPath, GLOBAL_PROFILE_JSON);

            if (!Directory.Exists(profilesFolderPath))
            {
                Directory.CreateDirectory(profilesFolderPath);
            }

            LoadGlobalSettings();
            LoadGlobalProfile();
            LoadGameProfiles();

            activeProfile = globalProfile;
            isPerGameProfileActive = false;
        }

        #region Loading & Migration

        private void LoadGlobalSettings()
        {
            bool loaded = false;
            if (File.Exists(settingsJsonPath))
            {
                try
                {
                    string json = File.ReadAllText(settingsJsonPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    if (dict != null)
                    {
                        foreach (var kvp in dict)
                        {
                            globalSettings[kvp.Key] = kvp.Value;
                        }
                        loaded = true;
                        Logger.Info($"Loaded global settings from JSON: {settingsJsonPath}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to deserialize settings.json. Falling back to default or XML.");
                }
            }

            if (!loaded)
            {
                // Check for legacy settings.xml
                string legacyXml = Path.Combine(baseFolderPath, SETTINGS_XML_NAME);
                if (File.Exists(legacyXml))
                {
                    try
                    {
                        Logger.Info("Migrating legacy settings.xml to settings.json...");
                        var oldSetting = XmlHelper.FromXMLFile<LegacySettingXmlDTO>(legacyXml);
                        globalSettings[SettingsRegistry.OnScreenDisplayProvider.Key] = oldSetting.OnScreenDisplayProvider;
                        globalSettings[SettingsRegistry.OnScreenDisplay.Key] = oldSetting.OnScreenDisplay;
                        globalSettings[SettingsRegistry.OnScreenDisplayTextSize.Key] = oldSetting.OnScreenDisplayTextSize;
                        globalSettings[SettingsRegistry.GpuTarget.Key] = oldSetting.GpuTarget;
                        SaveGlobalSettings();
                        loaded = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Failed to migrate legacy settings.xml.");
                    }
                }
            }

            // Populate defaults if missing
            foreach (var def in SettingsRegistry.AllDefinitions)
            {
                if (def.Scope == SettingScope.GlobalOnly && !globalSettings.ContainsKey(def.Key))
                {
                    globalSettings[def.Key] = def.DefaultRawValue;
                }
            }
        }

        private void LoadGlobalProfile()
        {
            string legacyGlobalXml = Path.Combine(baseFolderPath, GLOBAL_PROFILE_XML);
            bool loaded = false;

            if (File.Exists(globalProfileJsonPath))
            {
                try
                {
                    string json = File.ReadAllText(globalProfileJsonPath);
                    var profile = JsonSerializer.Deserialize<ProfileDataDTO>(json);
                    if (profile != null)
                    {
                        globalProfile = profile.ToGameProfile(globalProfileJsonPath, gameProfiles);
                        loaded = true;
                        Logger.Info($"Loaded global profile from JSON: {globalProfileJsonPath}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to deserialize global.json. Falling back to XML.");
                }
            }

            if (!loaded && File.Exists(legacyGlobalXml))
            {
                try
                {
                    Logger.Info("Migrating legacy global.xml to global.json...");
                    globalProfile = XmlHelper.FromXMLFile<GameProfile>(legacyGlobalXml);
                    globalProfile.Path = legacyGlobalXml;
                    globalProfile.Cache = gameProfiles;
                    SaveGlobalProfile();
                    loaded = true;
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to migrate legacy global.xml.");
                }
            }

            if (!loaded)
            {
                Logger.Info("Initializing new default global profile...");
                globalProfile = new GameProfile(new GameProfileOptions
                {
                    GameName = GameProfile.GLOBAL_PROFILE_NAME,
                    GamePath = GameProfile.GLOBAL_PROFILE_NAME,
                    InUse = true,
                    FPSLimit = 0,
                    FPSLimitMode = 0,
                    ProfilePath = globalProfileJsonPath,
                    Cache = gameProfiles,
                    JudderFreeFPS = true
                });
                SaveGlobalProfile();
            }
        }

        private void LoadGameProfiles()
        {
            gameProfiles.Clear();

            // Load JSON game profiles
            var jsonFiles = Directory.GetFiles(profilesFolderPath, $"*{JSON_EXTENSION}");
            foreach (var filePath in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var dto = JsonSerializer.Deserialize<ProfileDataDTO>(json);
                    if (dto != null)
                    {
                        var profile = dto.ToGameProfile(filePath, gameProfiles);
                        if (profile.IsValid())
                        {
                            gameProfiles[profile.GameId] = profile;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, $"Failed to load JSON game profile from {filePath}.");
                }
            }

            // Check for un-migrated XML game profiles
            var xmlFiles = Directory.GetFiles(profilesFolderPath, $"*{XML_EXTENSION}");
            foreach (var filePath in xmlFiles)
            {
                try
                {
                    var xmlProfile = XmlHelper.FromXMLFile<GameProfile>(filePath);
                    xmlProfile.Path = filePath;
                    xmlProfile.Cache = gameProfiles;
                    if (xmlProfile.IsValid() && !gameProfiles.ContainsKey(xmlProfile.GameId))
                    {
                        Logger.Info($"Migrating legacy XML profile {xmlProfile.GameId.Name} to JSON...");
                        gameProfiles[xmlProfile.GameId] = xmlProfile;
                        SaveGameProfile(xmlProfile);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, $"Failed to load or migrate XML profile from {filePath}.");
                }
            }
        }

        #endregion

        #region Persistence

        public void SaveGlobalSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(globalSettings, options);
                File.WriteAllText(settingsJsonPath, json);
                Logger.Debug("Saved global settings to JSON.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save global settings JSON.");
            }
        }

        public void SaveGlobalProfile()
        {
            try
            {
                var dto = ProfileDataDTO.FromGameProfile(globalProfile);
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(dto, options);
                File.WriteAllText(globalProfileJsonPath, json);
                Logger.Debug("Saved global profile to JSON.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save global profile.");
            }
        }

        public void SaveGameProfile(GameProfile profile)
        {
            if (!profile.IsValid()) return;

            try
            {
                gameProfiles[profile.GameId] = profile;

                string fileName = Path.GetFileNameWithoutExtension(profile.GameId.Path);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = profile.GameId.Name;
                }
                string jsonPath = Path.Combine(profilesFolderPath, $"{fileName}{JSON_EXTENSION}");

                var dto = ProfileDataDTO.FromGameProfile(profile);
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(dto, options);
                File.WriteAllText(jsonPath, json);
                Logger.Info($"Saved game profile {profile.GameId.Name} to {jsonPath}.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to save game profile {profile.GameId.Name}.");
            }
        }

        #endregion

        #region Setting Access & Resolution

        public T GetEffectiveValue<T>(ISettingDefinition<T> def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));

            if (def.Scope == SettingScope.GlobalOnly)
            {
                if (globalSettings.TryGetValue(def.Key, out var rawVal))
                {
                    return (T)def.Sanitize(rawVal);
                }
                return def.DefaultValue;
            }

            if (def.Scope == SettingScope.ProfileOrGlobal)
            {
                if (isPerGameProfileActive && activeProfile.IsValid() && !activeProfile.IsGlobalProfile)
                {
                    if (activeProfile.TryGetSetting(def, out var val))
                    {
                        return val;
                    }
                }

                if (globalProfile.TryGetSetting(def, out var gVal))
                {
                    return gVal;
                }

                return def.DefaultValue;
            }

            return def.DefaultValue;
        }

        public void SetEffectiveValue<T>(ISettingDefinition<T> def, T value)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));

            T sanitized = def.Sanitize(value);

            if (def.Scope == SettingScope.GlobalOnly)
            {
                globalSettings[def.Key] = sanitized;
                SaveGlobalSettings();
                return;
            }

            if (def.Scope == SettingScope.ProfileOrGlobal)
            {
                if (isPerGameProfileActive && activeProfile.IsValid() && !activeProfile.IsGlobalProfile)
                {
                    activeProfile.SetSetting(def, sanitized);
                    SaveGameProfile(activeProfile);
                }
                else
                {
                    globalProfile.SetSetting(def, sanitized);
                    SaveGlobalProfile();
                }
            }
        }

        #endregion

        #region Profile & Game State Management

        public bool TryGetProfile(GameId gameId, out GameProfile profile)
        {
            return gameProfiles.TryGetValue(gameId, out profile);
        }

        public GameProfile AddNewProfile(GameId gameId)
        {
            if (TryGetProfile(gameId, out var existing))
            {
                return existing;
            }

            string fileName = Path.GetFileNameWithoutExtension(gameId.Path);
            if (string.IsNullOrWhiteSpace(fileName)) fileName = gameId.Name;
            string newPath = Path.Combine(profilesFolderPath, $"{fileName}{JSON_EXTENSION}");

            var newProfile = new GameProfile(new GameProfileOptions
            {
                GameName = gameId.Name,
                GamePath = gameId.Path,
                InUse = true,
                FPSLimit = globalProfile.FPSLimit,
                FPSLimitMode = globalProfile.FPSLimitMode,
                JudderFreeFPS = globalProfile.JudderFreeFPS,
                ProfilePath = newPath,
                Cache = gameProfiles
            });

            SaveGameProfile(newProfile);
            return newProfile;
        }

        public void SetActiveGame(GameId gameId)
        {
            activeGameId = gameId;
            if (gameId.IsValid() && TryGetProfile(gameId, out var profile) && profile.Use)
            {
                activeProfile = profile;
                isPerGameProfileActive = true;
                Logger.Info($"Activated per-game profile for {gameId.Name}.");
            }
            else
            {
                activeProfile = globalProfile;
                isPerGameProfileActive = false;
                Logger.Info("Activated global profile (no active per-game profile in use).");
            }
        }

        public void SetPerGameProfileEnabled(bool enabled)
        {
            if (enabled && activeGameId.IsValid())
            {
                if (!TryGetProfile(activeGameId, out var profile))
                {
                    profile = AddNewProfile(activeGameId);
                }
                profile.Use = true;
                activeProfile = profile;
                isPerGameProfileActive = true;
                SaveGameProfile(profile);
                Logger.Info($"Enabled per-game profile for {activeGameId.Name}.");
            }
            else
            {
                if (activeGameId.IsValid() && TryGetProfile(activeGameId, out var profile))
                {
                    profile.Use = false;
                    SaveGameProfile(profile);
                }
                activeProfile = globalProfile;
                isPerGameProfileActive = false;
                Logger.Info("Disabled per-game profile; switched to global profile.");
            }
        }

        #endregion
    }

    internal class ProfileDataDTO
    {
        public string GameName { get; set; } = string.Empty;
        public string GamePath { get; set; } = string.Empty;
        public bool InUse { get; set; } = true;
        public int FPSLimit { get; set; } = 0;
        public int FPSLimitMode { get; set; } = 0;
        public bool JudderFreeFPS { get; set; } = true;
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

        public static ProfileDataDTO FromGameProfile(GameProfile profile)
        {
            var dto = new ProfileDataDTO
            {
                GameName = profile.GameId.Name,
                GamePath = profile.GameId.Path,
                InUse = profile.Use,
                FPSLimit = profile.FPSLimit,
                FPSLimitMode = profile.FPSLimitMode,
                JudderFreeFPS = profile.JudderFreeFPS,
                Settings = new Dictionary<string, object>(profile.Settings)
            };
            return dto;
        }

        public GameProfile ToGameProfile(string path, IDictionary<GameId, GameProfile> cache)
        {
            var profile = new GameProfile(new GameProfileOptions
            {
                GameName = GameName,
                GamePath = GamePath,
                InUse = InUse,
                FPSLimit = FPSLimit,
                FPSLimitMode = FPSLimitMode,
                JudderFreeFPS = JudderFreeFPS,
                ProfilePath = path,
                Cache = cache
            });
            if (Settings != null)
            {
                foreach (var kvp in Settings)
                {
                    profile.Settings[kvp.Key] = kvp.Value;
                }
            }
            return profile;
        }
    }

    [System.Xml.Serialization.XmlRoot("Setting")]
    public struct LegacySettingXmlDTO
    {
        [System.Xml.Serialization.XmlElement("OnScreenDisplayProvider")]
        public int OnScreenDisplayProvider;

        [System.Xml.Serialization.XmlElement("OnScreenDisplay")]
        public int OnScreenDisplay;

        [System.Xml.Serialization.XmlElement("OnScreenDisplayTextSize")]
        public int OnScreenDisplayTextSize;

        [System.Xml.Serialization.XmlElement("GpuTarget")]
        public int GpuTarget;
    }
}
