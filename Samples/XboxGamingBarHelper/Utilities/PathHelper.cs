using NLog;
using System;
using System.IO;
using Windows.Storage;

namespace XboxGamingBarHelper.Utilities
{
    public static class PathHelper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static string _cachedLocalFolderPath;

        public static string GetLocalFolderPath()
        {
            if (!string.IsNullOrEmpty(_cachedLocalFolderPath))
            {
                return _cachedLocalFolderPath;
            }

            try
            {
                var path = ApplicationData.Current.LocalFolder.Path;
                Logger.Info($"Obtained LocalFolder path from ApplicationData.Current: {path}");
                _cachedLocalFolderPath = path;
                return path;
            }
            catch (Exception ex)
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                try
                {
                    string packageFamilyName = null;
                    var packageManager = new global::Windows.Management.Deployment.PackageManager();
                    var packages = packageManager.FindPackagesForUser(string.Empty);
                    foreach (var package in packages)
                    {
                        if (package.Id.Name.Equals("CouchGamingBar", StringComparison.OrdinalIgnoreCase) ||
                            package.Id.Name.Equals("Zanchisoft.CouchGamingBar", StringComparison.OrdinalIgnoreCase))
                        {
                            packageFamilyName = package.Id.FamilyName;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(packageFamilyName))
                    {
                        string localState = Path.Combine(localAppData, "Packages", packageFamilyName, "LocalState");
                        if (Directory.Exists(localState))
                        {
                            Logger.Info($"ApplicationData.Current not available ({ex.Message}); resolved active package LocalState directory: {localState}");
                            _cachedLocalFolderPath = localState;
                            return localState;
                        }
                    }
                }
                catch (Exception pkgEx)
                {
                    Logger.Warn(pkgEx, "Failed to resolve active package LocalState directory.");
                }

                Logger.Info($"ApplicationData.Current not available ({ex.Message}), falling back to LocalApplicationData directory.");
                string fallbackFolder = Path.Combine(localAppData, "CouchGamingBar");
                Directory.CreateDirectory(fallbackFolder);
                _cachedLocalFolderPath = fallbackFolder;
                return fallbackFolder;
            }
        }
    }
}
