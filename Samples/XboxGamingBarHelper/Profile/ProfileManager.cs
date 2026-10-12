using NLog;
using Shared.Data;
using Shared.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.ApplicationModel.AppService;
using Windows.Storage;
using XboxGamingBarHelper.Core;
using XboxGamingBarHelper.Utilities;

namespace XboxGamingBarHelper.Profile
{
    internal class ProfileManager : Manager
    {
        private const string PROFILE_FOLDER_NAME = "profiles";
        private const string XML_EXTENSION = ".xml";

        public static string GetGameProfilesFolder()
        {
            return Path.Combine(PathHelper.GetLocalFolderPath(), PROFILE_FOLDER_NAME);
        }

        public static string GetGlobalProfilePath()
        {
            return Path.Combine(PathHelper.GetLocalFolderPath(), $"{GameProfile.GLOBAL_PROFILE_NAME}{XML_EXTENSION}");
        }

        private readonly ProfileStore store;
        public ProfileStore Store => store;

        public GameProfile GlobalProfile => store.GlobalProfile;

        public IReadOnlyDictionary<GameId, GameProfile> GameProfiles => store.GameProfiles;

        private readonly PerGameProfileProperty perGameProfile;
        public PerGameProfileProperty PerGameProfile
        {
            get { return perGameProfile; }
        }
        
        private readonly GameProfileProperty currentProfile;
        public GameProfileProperty CurrentProfile
        {
            get { return currentProfile; }
        }

        public ProfileManager(AppServiceConnection connection, ProfileStore existingStore = null) : base(connection)
        {
            Logger.Info("Initialize ProfileStore engine.");
            store = existingStore ?? new ProfileStore();

            Logger.Info("Initialize game profile properties.");
            perGameProfile = new PerGameProfileProperty(null, this);
            currentProfile = new GameProfileProperty(store.GlobalProfile, this);
        }

        public bool TryGetProfile(GameId gameId, out GameProfile gameProfile)
        {
            return store.TryGetProfile(gameId, out gameProfile);
        }

        public GameProfile AddNewProfile(GameId gameId)
        {
            return store.AddNewProfile(gameId);
        }
    }
}
