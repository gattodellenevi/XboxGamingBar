using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using NLog;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Shared.Enums;
using XboxGamingBarHelper.Utilities;

namespace XboxGamingBarHelper.Systems
{
    public class TrayIconManager : IDisposable
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;
        private AppServiceConnection connection;

        public TrayIconManager(AppServiceConnection connection)
        {
            this.connection = connection;
            InitializeTrayIcon();
        }

        public AppServiceConnection Connection
        {
            get => connection;
            set => connection = value;
        }

        private void InitializeTrayIcon()
        {
            contextMenu = new ContextMenuStrip();
            var openItem = new ToolStripMenuItem("Open Couch Gaming Bar", null, OnOpenClicked)
            {
                Font = new Font(contextMenu.Font, FontStyle.Bold)
            };
            contextMenu.Items.Add(openItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, OnExitClicked);

            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application, // Fallback icon
                ContextMenuStrip = contextMenu,
                Text = "CouchGamingBar Helper",
                Visible = true
            };
            notifyIcon.DoubleClick += OnOpenClicked;

            // Try to load a better icon if possible
            try
            {
                string[] iconPaths = new string[]
                {
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", "tray_icon_32.png"),
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", "tray_icon.png"),
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tray_icon.png"),
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Images", "Square44x44Logo.png")
                };

                foreach (string iconPath in iconPaths)
                {
                    if (System.IO.File.Exists(iconPath))
                    {
                        Logger.Info($"Found tray icon at {iconPath}");
                        using (Bitmap bitmap = new Bitmap(iconPath))
                        {
                            IntPtr hIcon = bitmap.GetHicon();
                            using (Icon tempIcon = Icon.FromHandle(hIcon))
                            {
                                notifyIcon.Icon = (Icon)tempIcon.Clone();
                            }
                            DestroyIcon(hIcon);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load tray icon image. Using default.");
            }
        }

        private void OnOpenClicked(object sender, EventArgs e)
        {
            Logger.Info("Opening CouchGamingBar application from tray icon.");
            try
            {
                string aumid = GetAppUserModelId();
                if (!string.IsNullOrEmpty(aumid))
                {
                    Logger.Info($"Launching CouchGamingBar via shell:AppsFolder with AUMID: {aumid}");
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo($"shell:AppsFolder\\{aumid}")
                    {
                        UseShellExecute = true
                    });
                    return;
                }
                else
                {
                    Logger.Warn("Could not determine AUMID for CouchGamingBar. Cannot launch from tray.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to launch CouchGamingBar via shell:AppsFolder.");
            }
        }

        private string GetAppUserModelId()
        {
            string packageFamilyName = connection?.PackageFamilyName;

            if (string.IsNullOrEmpty(packageFamilyName))
            {
                try
                {
                    packageFamilyName = global::Windows.ApplicationModel.Package.Current?.Id?.FamilyName;
                }
                catch { }
            }

            if (string.IsNullOrEmpty(packageFamilyName))
            {
                try
                {
                    string localFolderPath = PathHelper.GetLocalFolderPath();
                    string primaryCachePath = System.IO.Path.Combine(localFolderPath, "package_identity.txt");
                    string appDirCachePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "package_identity.txt");

                    if (System.IO.File.Exists(primaryCachePath))
                    {
                        packageFamilyName = System.IO.File.ReadAllText(primaryCachePath).Trim();
                    }
                    else if (System.IO.File.Exists(appDirCachePath))
                    {
                        packageFamilyName = System.IO.File.ReadAllText(appDirCachePath).Trim();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to read cached package identity in TrayIconManager.");
                }
            }

            if (string.IsNullOrEmpty(packageFamilyName))
            {
                try
                {
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
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to query PackageManager in TrayIconManager.");
                }
            }

            if (!string.IsNullOrEmpty(packageFamilyName))
            {
                return $"{packageFamilyName}!App";
            }

            return null;
        }

        private async void OnExitClicked(object sender, EventArgs e)
        {
            Logger.Info("Exit clicked from tray icon.");
            
            if (connection != null)
            {
                try
                {
                    Logger.Info("Signaling widget to close...");
                    ValueSet message = new ValueSet();
                    message.Add("Function", (int)Function.AppExit);
                    await connection.SendMessageAsync(message);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to send AppExit message to widget.");
                }
            }

            notifyIcon.Visible = false;
            Application.Exit();
            Environment.Exit(0);
        }

        public void Dispose()
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
            contextMenu?.Dispose();
        }
    }
}
