using Shared.Enums;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace XboxGamingBar.Data
{
    internal class HardwareTelemetryProperty : WidgetProperty<string>
    {
        private readonly Page owner;
        private readonly TextBlock cpuUsageText;
        private readonly TextBlock cpuWattageText;
        private readonly TextBlock gpuUsageText;
        private readonly TextBlock gpuWattageText;
        private readonly TextBlock vramUsageText;
        private readonly TextBlock vramUsedText;
        private readonly TextBlock ramUsageText;
        private readonly TextBlock ramUsedText;

        private readonly TextBlock quickCpuUsageText;
        private readonly TextBlock quickCpuWattageText;
        private readonly TextBlock quickGpuUsageText;
        private readonly TextBlock quickGpuWattageText;
        private readonly TextBlock quickVramUsageText;
        private readonly TextBlock quickVramUsedText;
        private readonly TextBlock quickRamUsageText;
        private readonly TextBlock quickRamUsedText;

        public HardwareTelemetryProperty(
            TextBlock inCpuUsageText,
            TextBlock inCpuWattageText,
            TextBlock inGpuUsageText,
            TextBlock inGpuWattageText,
            TextBlock inVramUsageText,
            TextBlock inVramUsedText,
            TextBlock inRamUsageText,
            TextBlock inRamUsedText,
            Page inOwner,
            TextBlock inQuickCpuUsageText = null,
            TextBlock inQuickCpuWattageText = null,
            TextBlock inQuickGpuUsageText = null,
            TextBlock inQuickGpuWattageText = null,
            TextBlock inQuickVramUsageText = null,
            TextBlock inQuickVramUsedText = null,
            TextBlock inQuickRamUsageText = null,
            TextBlock inQuickRamUsedText = null)
            : base(string.Empty, null, Function.HardwareTelemetry)
        {
            owner = inOwner;
            cpuUsageText = inCpuUsageText;
            cpuWattageText = inCpuWattageText;
            gpuUsageText = inGpuUsageText;
            gpuWattageText = inGpuWattageText;
            vramUsageText = inVramUsageText;
            vramUsedText = inVramUsedText;
            ramUsageText = inRamUsageText;
            ramUsedText = inRamUsedText;

            quickCpuUsageText = inQuickCpuUsageText;
            quickCpuWattageText = inQuickCpuWattageText;
            quickGpuUsageText = inQuickGpuUsageText;
            quickGpuWattageText = inQuickGpuWattageText;
            quickVramUsageText = inQuickVramUsageText;
            quickVramUsedText = inQuickVramUsedText;
            quickRamUsageText = inQuickRamUsageText;
            quickRamUsedText = inQuickRamUsedText;
        }

        public override bool SetValue(object newValue, long updatedTime = 0)
        {
            var result = base.SetValue(newValue, updatedTime);

            if (newValue is string telemetry && !string.IsNullOrEmpty(telemetry))
            {
                UpdateUI(telemetry);
            }

            return result;
        }

        public override async Task Sync()
        {
            await base.Sync();
            if (!string.IsNullOrEmpty(Value))
            {
                UpdateUI(Value);
            }
        }

        protected override async void NotifyPropertyChanged(string propertyName = "")
        {
            base.NotifyPropertyChanged(propertyName);
            if (!string.IsNullOrEmpty(Value))
            {
                UpdateUI(Value);
            }
        }

        public void SetDisconnectedState()
        {
            if (owner == null) return;
            var _ = owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (cpuUsageText != null) cpuUsageText.Text = "--%";
                if (cpuWattageText != null) cpuWattageText.Text = "--W";
                if (gpuUsageText != null) gpuUsageText.Text = "--%";
                if (gpuWattageText != null) gpuWattageText.Text = "--W";
                if (vramUsageText != null) vramUsageText.Text = "--%";
                if (vramUsedText != null) vramUsedText.Text = "-- GB";
                if (ramUsageText != null) ramUsageText.Text = "--%";
                if (ramUsedText != null) ramUsedText.Text = "-- GB";

                if (quickCpuUsageText != null) quickCpuUsageText.Text = "--%";
                if (quickCpuWattageText != null) quickCpuWattageText.Text = "--W";
                if (quickGpuUsageText != null) quickGpuUsageText.Text = "--%";
                if (quickGpuWattageText != null) quickGpuWattageText.Text = "--W";
                if (quickVramUsageText != null) quickVramUsageText.Text = "--%";
                if (quickVramUsedText != null) quickVramUsedText.Text = "-- GB";
                if (quickRamUsageText != null) quickRamUsageText.Text = "--%";
                if (quickRamUsedText != null) quickRamUsedText.Text = "-- GB";
            });
        }

        private void UpdateUI(string telemetry)
        {
            if (owner == null) return;

            var parts = telemetry.Split(';');
            if (parts.Length < 10) return;

            float.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out float cpuUsage);
            float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float cpuWatt);
            float.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float cpuClock);
            float.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out float gpuUsage);
            float.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out float gpuWatt);
            float.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out float gpuClock);
            float.TryParse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture, out float vramPercent);
            float.TryParse(parts[7], NumberStyles.Any, CultureInfo.InvariantCulture, out float vramUsedGb);
            float.TryParse(parts[8], NumberStyles.Any, CultureInfo.InvariantCulture, out float ramPercent);
            float.TryParse(parts[9], NumberStyles.Any, CultureInfo.InvariantCulture, out float ramUsedGb);

            var _ = owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                string cpuUsageStr = (cpuUsage >= 0) ? $"{Math.Round(cpuUsage)}%" : "--%";
                if (cpuUsageText != null) cpuUsageText.Text = cpuUsageStr;
                if (quickCpuUsageText != null) quickCpuUsageText.Text = cpuUsageStr;

                string cpuWattStr;
                if (cpuWatt > 0)
                    cpuWattStr = $"{Math.Round(cpuWatt)}W";
                else if (cpuClock > 0)
                    cpuWattStr = $"{Math.Round(cpuClock)} MHz";
                else
                    cpuWattStr = "--W";
                if (cpuWattageText != null) cpuWattageText.Text = cpuWattStr;
                if (quickCpuWattageText != null) quickCpuWattageText.Text = cpuWattStr;

                string gpuUsageStr = (gpuUsage >= 0) ? $"{Math.Round(gpuUsage)}%" : "--%";
                if (gpuUsageText != null) gpuUsageText.Text = gpuUsageStr;
                if (quickGpuUsageText != null) quickGpuUsageText.Text = gpuUsageStr;

                string gpuWattStr;
                if (gpuWatt >= 0)
                    gpuWattStr = $"{Math.Round(gpuWatt)}W";
                else if (gpuClock > 0)
                    gpuWattStr = $"{Math.Round(gpuClock)} MHz";
                else
                    gpuWattStr = "--W";
                if (gpuWattageText != null) gpuWattageText.Text = gpuWattStr;
                if (quickGpuWattageText != null) quickGpuWattageText.Text = gpuWattStr;

                string vramPercentStr = (vramPercent >= 0) ? $"{Math.Round(vramPercent)}%" : "--%";
                if (vramUsageText != null) vramUsageText.Text = vramPercentStr;
                if (quickVramUsageText != null) quickVramUsageText.Text = vramPercentStr;

                string vramUsedStr = (vramUsedGb >= 0) ? $"{vramUsedGb:F1} GB" : "-- GB";
                if (vramUsedText != null) vramUsedText.Text = vramUsedStr;
                if (quickVramUsedText != null) quickVramUsedText.Text = vramUsedStr;

                string ramPercentStr = (ramPercent >= 0) ? $"{Math.Round(ramPercent)}%" : "--%";
                if (ramUsageText != null) ramUsageText.Text = ramPercentStr;
                if (quickRamUsageText != null) quickRamUsageText.Text = ramPercentStr;

                string ramUsedStr = (ramUsedGb >= 0) ? $"{ramUsedGb:F1} GB" : "-- GB";
                if (ramUsedText != null) ramUsedText.Text = ramUsedStr;
                if (quickRamUsedText != null) quickRamUsedText.Text = ramUsedStr;
            });
        }

        protected override bool ShouldSendNotifyMessage()
        {
            return false;
        }
    }
}
