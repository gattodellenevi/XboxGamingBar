using NLog;
using System;
using System.IO;
using Windows.Storage;

namespace XboxGamingBarHelper.Utilities
{
    public static class PathHelper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static string GetLocalFolderPath()
        {
            try
            {
                var path = ApplicationData.Current.LocalFolder.Path;
                Logger.Info($"Obtained LocalFolder path from ApplicationData.Current: {path}");
                return path;
            }
            catch (Exception ex)
            {
                Logger.Info($"ApplicationData.Current not available ({ex.Message}), falling back to LocalApplicationData directory.");
                string fallbackFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CouchGameBar");
                Directory.CreateDirectory(fallbackFolder);
                return fallbackFolder;
            }
        }
    }
}
