using NLog;
using System;
using System.Diagnostics;
using System.IO;
using Windows.ApplicationModel.AppService;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Nvidia
{
    internal class NvidiaManager : Manager
    {
        public LaunchNvidiaAppProperty LaunchNvidiaApp { get; }

        public NvidiaManager(AppServiceConnection connection) : base(connection)
        {
            LaunchNvidiaApp = new LaunchNvidiaAppProperty(false, this);
        }

        public void StartNvidiaApp()
        {
            Logger.Info("Attempting to launch NVIDIA App / GeForce Experience...");

            string[] possiblePaths = new string[]
            {
                // NVIDIA App (New unified CEF build)
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"NVIDIA Corporation\NVIDIA app\CEF\NVIDIA app.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"NVIDIA Corporation\NVIDIA app\NVIDIA app.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"NVIDIA Corporation\NVIDIA app\CEF\NVIDIA app.exe"),

                // Start Menu Shortcuts
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), @"NVIDIA Corporation\NVIDIA.lnk"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), @"NVIDIA Corporation\NVIDIA App.lnk"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), @"NVIDIA Corporation\NVIDIA GeForce Experience.lnk"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), @"NVIDIA Corporation\NVIDIA.lnk"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), @"NVIDIA Corporation\NVIDIA App.lnk"),

                // NVIDIA GeForce Experience (Legacy)
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"NVIDIA Corporation\NVIDIA GeForce Experience\NVIDIA GeForce Experience.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"NVIDIA Corporation\NVIDIA GeForce Experience\NVIDIA GeForce Experience.exe"),

                // NVIDIA Control Panel
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"NVIDIA Corporation\Control Panel Client\nvcplui.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "nvcplui.exe"),
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        Logger.Info($"Found NVIDIA executable at: {path}. Launching...");
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = path,
                            UseShellExecute = true
                        });
                        return;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Failed to launch executable at {path}");
                    }
                }
            }

            // Fallback: Open NVIDIA App download page in default browser
            try
            {
                Logger.Info("No local NVIDIA software found. Opening official NVIDIA App download page in web browser...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.nvidia.com/en-us/software/nvidia-app/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to open NVIDIA App URL in browser.");
            }
        }
    }
}
