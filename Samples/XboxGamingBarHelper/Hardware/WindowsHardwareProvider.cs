using System;
using NLog;
using XboxGamingBarHelper.Hardware.Abstractions;
using XboxGamingBarHelper.Hardware.Providers;

namespace XboxGamingBarHelper.Hardware
{
    internal class WindowsHardwareProvider : IHardwareProvider, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly object _syncLock = new object();
        private readonly ISystemMetricsProvider systemMetrics;

        private IGpuMetricsProvider _discreteProvider;
        private IGpuMetricsProvider _integratedProvider;
        private IGpuMetricsProvider _activeGpuMetrics;
        private int currentGpuTarget = 0;

        public string ProviderName
        {
            get
            {
                lock (_syncLock)
                {
                    return $"Windows ({_activeGpuMetrics?.Name ?? "GPU"})";
                }
            }
        }

        public WindowsHardwareProvider(int initialGpuTarget = 0)
        {
            currentGpuTarget = initialGpuTarget;
            systemMetrics = new CommonSystemMetricsProvider();

            lock (_syncLock)
            {
                if (currentGpuTarget == 1)
                {
                    _integratedProvider = GpuMetricsProviderFactory.Create(1);
                    _activeGpuMetrics = _integratedProvider;
                }
                else
                {
                    _discreteProvider = GpuMetricsProviderFactory.Create(0);
                    _activeGpuMetrics = _discreteProvider;
                }
            }

            Logger.Info($"WindowsHardwareProvider initialized with GPU Provider: {_activeGpuMetrics?.Name} ({_activeGpuMetrics?.Vendor})");
        }

        public void SetGpuTarget(int target)
        {
            lock (_syncLock)
            {
                if (currentGpuTarget == target && _activeGpuMetrics != null) return;

                currentGpuTarget = target;
                try
                {
                    if (target == 1)
                    {
                        if (_integratedProvider == null)
                        {
                            Logger.Info("WindowsHardwareProvider: Initializing integrated GPU provider...");
                            _integratedProvider = GpuMetricsProviderFactory.Create(1);
                        }
                        _activeGpuMetrics = _integratedProvider;
                    }
                    else
                    {
                        if (_discreteProvider == null)
                        {
                            Logger.Info("WindowsHardwareProvider: Initializing discrete GPU provider...");
                            _discreteProvider = GpuMetricsProviderFactory.Create(0);
                        }
                        _activeGpuMetrics = _discreteProvider;
                    }

                    Logger.Info($"WindowsHardwareProvider switched to GPU target {target}: {_activeGpuMetrics?.Name} ({_activeGpuMetrics?.Vendor})");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to switch GPU target to {target}");
                }
            }
        }

        public void Update()
        {
            systemMetrics.Update();
            lock (_syncLock)
            {
                _activeGpuMetrics?.Update();
            }
        }

        // CPU & System
        public float GetCpuClock() => systemMetrics.GetCpuClock();
        public float GetCpuUsage() => systemMetrics.GetCpuUsage();
        public float GetCpuWattage() => systemMetrics.GetCpuWattage();
        public float GetCpuTemperature() => systemMetrics.GetCpuTemperature();
        public int GetCpuCoreCount() => systemMetrics.GetCpuCoreCount();
        public float GetCpuCoreUsage(int coreIndex) => systemMetrics.GetCpuCoreUsage(coreIndex);
        public float GetCpuCoreClock(int coreIndex) => systemMetrics.GetCpuCoreClock(coreIndex);
        public string GetCpuName() => systemMetrics.GetCpuName();
        public string GetMotherboardName() => systemMetrics.GetMotherboardName();

        // Memory
        public float GetMemoryUsage() => systemMetrics.GetMemoryUsage();
        public float GetMemoryUsed() => systemMetrics.GetMemoryUsed();

        // Battery
        public float GetBatteryLevel() => systemMetrics.GetBatteryLevel();
        public float GetBatteryRemainingTime() => systemMetrics.GetBatteryRemainingTime();
        public float GetBatteryDischargeRate() => systemMetrics.GetBatteryDischargeRate();
        public float GetBatteryChargeRate() => systemMetrics.GetBatteryChargeRate();

        // GPU
        public float GetGpuClock()
        {
            lock (_syncLock) return _activeGpuMetrics?.GetGpuClock() ?? -1.0f;
        }

        public float GetGpuUsage()
        {
            lock (_syncLock) return _activeGpuMetrics?.GetGpuUsage() ?? -1.0f;
        }

        public float GetGpuMemoryUsed()
        {
            lock (_syncLock) return _activeGpuMetrics?.GetGpuMemoryUsed() ?? -1.0f;
        }

        public float GetGpuMemoryTotal()
        {
            lock (_syncLock) return _activeGpuMetrics?.GetGpuMemoryTotal() ?? -1.0f;
        }

        public float GetGpuMemoryClock()
        {
            lock (_syncLock) return _activeGpuMetrics?.GetGpuMemoryClock() ?? -1.0f;
        }

        public float GetGpuWattage()
        {
            lock (_syncLock) return _activeGpuMetrics?.GetGpuWattage() ?? -1.0f;
        }

        public float GetGpuTemperature()
        {
            lock (_syncLock) return _activeGpuMetrics?.GetGpuTemperature() ?? -1.0f;
        }

        public void Dispose()
        {
            lock (_syncLock)
            {
                systemMetrics?.Dispose();
                _discreteProvider?.Dispose();
                _integratedProvider?.Dispose();
                _activeGpuMetrics = null;
            }
        }
    }
}
