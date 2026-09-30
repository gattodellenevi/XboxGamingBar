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

        public HardwareTelemetryProperty(
            TextBlock inCpuUsageText,
            TextBlock inCpuWattageText,
            TextBlock inGpuUsageText,
            TextBlock inGpuWattageText,
            TextBlock inVramUsageText,
            TextBlock inVramUsedText,
            TextBlock inRamUsageText,
            TextBlock inRamUsedText,
            Page inOwner)
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
                if (cpuUsageText != null)
                    cpuUsageText.Text = (cpuUsage >= 0) ? $"{Math.Round(cpuUsage)}%" : "--%";

                if (cpuWattageText != null)
                {
                    if (cpuWatt > 0)
                        cpuWattageText.Text = $"{Math.Round(cpuWatt)}W";
                    else if (cpuClock > 0)
                        cpuWattageText.Text = $"{Math.Round(cpuClock)} MHz";
                    else
                        cpuWattageText.Text = "--W";
                }

                if (gpuUsageText != null)
                    gpuUsageText.Text = (gpuUsage >= 0) ? $"{Math.Round(gpuUsage)}%" : "--%";

                if (gpuWattageText != null)
                {
                    if (gpuWatt >= 0)
                        gpuWattageText.Text = $"{Math.Round(gpuWatt)}W";
                    else if (gpuClock > 0)
                        gpuWattageText.Text = $"{Math.Round(gpuClock)} MHz";
                    else
                        gpuWattageText.Text = "--W";
                }

                if (vramUsageText != null)
                    vramUsageText.Text = (vramPercent >= 0) ? $"{Math.Round(vramPercent)}%" : "--%";

                if (vramUsedText != null)
                    vramUsedText.Text = (vramUsedGb >= 0) ? $"{vramUsedGb:F1} GB" : "-- GB";

                if (ramUsageText != null)
                    ramUsageText.Text = (ramPercent >= 0) ? $"{Math.Round(ramPercent)}%" : "--%";

                if (ramUsedText != null)
                    ramUsedText.Text = (ramUsedGb >= 0) ? $"{ramUsedGb:F1} GB" : "-- GB";
            });
        }

        protected override bool ShouldSendNotifyMessage()
        {
            return false;
        }
    }
}
