using System;
using System.IO;
using System.Diagnostics;
using RTSSSharedMemoryNET;
using Shared.Enums;
using Shared.Utilities;
using Windows.ApplicationModel.AppService;
using XboxGamingBarHelper.OnScreenDisplay;
using XboxGamingBarHelper.Hardware;
using XboxGamingBarHelper.RTSS.OSDItems;
using XboxGamingBarHelper.Windows;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.Win32;

namespace XboxGamingBarHelper.RTSS
{
    internal class RTSSManager : OnScreenDisplayManager
    {
        // START IOnScreenDisplayProvider implementation
        public override bool IsInstalled => RTSSHelper.IsInstalled(out _);
        // END IOnScreenDisplayProvider implementation

        private readonly FPSLimitProperty fpsLimit;
        public FPSLimitProperty FPSLimit => fpsLimit;

        private readonly FPSLimitModeProperty fpsLimitMode;
        public FPSLimitModeProperty FPSLimitMode => fpsLimitMode;

        private readonly LimitFPSProperty limitFPS;
        public LimitFPSProperty LimitFPS => limitFPS;

        private readonly JudderFreeFPSProperty judderFreeFPS;
        public JudderFreeFPSProperty JudderFreeFPS => judderFreeFPS;

        private readonly OSDTextSizeProperty osdTextSize;
        public OSDTextSizeProperty OSDTextSize => osdTextSize;

        private const string OSDNewLine = "\n";
        private const string OSDNewLinePadding = " ";
        private const string OSDSingleLineShortBackground = "<M=8,4,8,4><P=0,0><L0><C=80000000><B=0,0>\b<C>";
        private const string OSDSingleLineFullwidthBackground = "<M=8,4,-3000,4><P=0,0><L0><C=80000000><B=0,0>\b<C>";
        private const string OSDMultipleLinesBackground = "<M=8,4,8,4><P=0,0><L0><C=80000000><B=0,0>\b<C><A0=4><A1=10>";
        private const string OSDAppName = "Gaming Bar OSD";

        private OSD rtssOSD;
        private readonly OSDItem[] osdItems;
        private readonly OSDItemFrametimeStats frametimeStatsItem;

        private IColorFormatter activeColorFormatter = SDRColorFormatter.Instance;
        private bool isHDRActive = false;

        public RTSSManager(HardwareManager hardwareManager, AppServiceConnection connection) : base(connection)
        {
            RTSSFPSLimiter.Initialize();
            fpsLimit = new FPSLimitProperty(this);
            fpsLimitMode = new FPSLimitModeProperty(this);
            limitFPS = new LimitFPSProperty(false, this);
            judderFreeFPS = new JudderFreeFPSProperty(this);
            osdTextSize = new OSDTextSizeProperty(100, this);

            var osdItemsList = new List<OSDItem>()
            {
                new OSDItemGPU(hardwareManager.GPUUsage, hardwareManager.GPUClock, hardwareManager.GPUWattage, hardwareManager.GPUTemperature),
                new OSDItemCPU(hardwareManager.CPUUsage, hardwareManager.CPUClock, hardwareManager.CPUWattage, hardwareManager.CPUTemperature),
            };

            for (int i = 0; i < hardwareManager.CPUCoreUsages.Length; i++)
            {
                osdItemsList.Add(new OSDItemCPUPerCore(i, hardwareManager.CPUCoreUsages[i], hardwareManager.CPUCoreClocks[i]));
            }

            osdItemsList.Add(new OSDItemVideoMemory(hardwareManager.GPUMemoryUsed, hardwareManager.GPUMemoryTotal));
            osdItemsList.Add(new OSDItemMemory(hardwareManager.MemoryUsage, hardwareManager.MemoryUsed));
            osdItemsList.Add(new OSDItemFPS());

            frametimeStatsItem = new OSDItemFrametimeStats();
            osdItemsList.Add(frametimeStatsItem);
            osdItemsList.Add(new OSDItemFramtimeGraph());

            osdItems = osdItemsList.ToArray();

            // Initial HDR & display auto-scale detection, plus event subscription for zero-polling updates
            isHDRActive = HDRDetector.IsHDRActive();
            activeColorFormatter = isHDRActive ? (IColorFormatter)HDRColorFormatter.Instance : SDRColorFormatter.Instance;
            UpdateAutoScale();
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        public static int UserTextSizePercent { get; set; } = 100;
        public static int CurrentBaseAutoScale { get; private set; } = 100;
        public static int CurrentFontScale => Math.Max(25, (int)Math.Round(CurrentBaseAutoScale * (UserTextSizePercent / 100.0)));
        public static int SubscriptFontScale => Math.Max(20, (int)Math.Round(CurrentFontScale * 0.5));

        public void SetTextSize(int size)
        {
            int clamped = Math.Max(50, Math.Min(200, size));
            if (UserTextSizePercent != clamped)
            {
                UserTextSizePercent = clamped;
                Logger.Info($"OSD text size set to {UserTextSizePercent}%. Effective font scale: {CurrentFontScale}%.");
            }
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            bool newHDRState = HDRDetector.IsHDRActive();
            if (newHDRState != isHDRActive)
            {
                isHDRActive = newHDRState;
                activeColorFormatter = isHDRActive ? (IColorFormatter)HDRColorFormatter.Instance : SDRColorFormatter.Instance;
                Logger.Info($"HDR state changed. New IsHDRActive: {isHDRActive}. Swapped RTSS color formatter.");
            }

            UpdateAutoScale();
        }

        private void UpdateAutoScale()
        {
            try
            {
                var (width, height) = User32.GetCurrentResolution();
                if (height <= 0)
                {
                    height = 1080;
                }

                // Base resolution: 1080p = 100% scale
                // 800p  -> ~74%
                // 1080p -> 100%
                // 1440p -> 133%
                // 2160p (4K) -> 200%
                int newBaseScale = (int)Math.Round((height / 1080.0) * 100.0);
                newBaseScale = Math.Max(50, Math.Min(300, newBaseScale));

                if (newBaseScale != CurrentBaseAutoScale)
                {
                    CurrentBaseAutoScale = newBaseScale;
                    Logger.Info($"Display resolution changed to {width}x{height}. Base auto-scale: {CurrentBaseAutoScale}%. Effective font scale: {CurrentFontScale}%.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to update OSD auto-scale.");
                CurrentBaseAutoScale = 100;
            }
        }

        private string GetVerticalLineSeparator()
        {
            var purpleColor = activeColorFormatter.Format(Color.FromArgb(0x6E, 0x00, 0x6A));
            return $" <C={purpleColor}>|<C> ";
        }

        public override void Update()
        {
            base.Update();

            if (!RTSSHelper.IsInstalled(out string installDir))
            {
                Logger.Debug("Rivatuner Statistics Server is not installed.");
                return;
            }

            var isRunning = RTSSHelper.IsRunning();
            string executablePath = Path.Combine(installDir, $"{RTSSHelper.RTSS_FILE_NAME}.exe");
            if (!isRunning && !File.Exists(executablePath))
            {
                Logger.Debug("Rivatuner Statistics Server is installed but the exe file is not found.");
                applicationState = ApplicationState.NotInstalled;
                return;
            }

            if (onScreenDisplayLevel == 0)
            {
                if (rtssOSD != null)
                {
                    rtssOSD.Update(string.Empty);
                    rtssOSD.Dispose();
                    rtssOSD = null;
                }
                return;
            }

            if (!isRunning)
            {
#if !STORE
                if (applicationState == ApplicationState.Starting)
                {
                    Logger.Info("Starting Rivatuner Statistics Server..");
                }
                else
                {
                    applicationState = ApplicationState.Starting;
                    try
                    {
                        Logger.Info("Start Rivatuner Statistics Server.");
                        Process.Start(executablePath);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to start Rivatuner Statistics Server.");
                        applicationState = ApplicationState.NotRunning;
                    }
                }
#endif
                return;
            }

            applicationState = ApplicationState.Running;

            if (rtssOSD == null)
            {
                rtssOSD = new OSD(OSDAppName);
            }

            if (onScreenDisplayLevel >= 4)
            {
                var appEntries = OSD.GetAppEntries();
                if (appEntries != null && appEntries.Length > 0)
                {
                    // Find the app that has stats or use the first one with a PID
                    AppEntry activeApp = appEntries.FirstOrDefault(a => a.StatFrameTimeCount > 0)
                                      ?? appEntries.FirstOrDefault(a => a.ProcessId > 0);

                    if (activeApp != null)
                    {
                        frametimeStatsItem.Min = activeApp.StatFrameTimeMin / 1000.0f;
                        frametimeStatsItem.Max = activeApp.StatFrameTimeMax / 1000.0f;
                        frametimeStatsItem.Avg = activeApp.StatFrameTimeAvg / 1000.0f;
                    }
                }
            }

            var osdString = (onScreenDisplayLevel == 1 ? OSDSingleLineShortBackground : (onScreenDisplayLevel >= 3 ? OSDMultipleLinesBackground : OSDSingleLineFullwidthBackground)) + $"<S={CurrentFontScale}>";
            var needSeparator = false;
            var osdPadding = onScreenDisplayLevel >= 3 ? OSDNewLinePadding : string.Empty;
            var osdSeparator = onScreenDisplayLevel >= 3 ? OSDNewLine : GetVerticalLineSeparator();
            for (int i = 0; i < osdItems.Length; i++)
            {
                var osdItemString = osdItems[i].GetOSDString(onScreenDisplayLevel, activeColorFormatter);
                if (string.IsNullOrEmpty(osdItemString))
                    continue;

                if (needSeparator)
                {
                    osdString += osdSeparator;
                }

                osdString += osdPadding + osdItemString;
                needSeparator = true;
            }

            rtssOSD.Update(osdString);
        }
    }
}
