using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using XboxGamingBarHelper.Hardware.Abstractions;
using XboxGamingBarHelper.Hardware.Dxgi;

namespace XboxGamingBarHelper.Hardware.Providers
{
    internal static class GpuMetricsProviderFactory
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static bool HasDualGpus(out string discreteName, out string integratedName)
        {
            discreteName = string.Empty;
            integratedName = string.Empty;

            try
            {
                var adapters = DxgiHelper.EnumerateAdapters().Where(a => !a.IsSoftware).ToList();
                var uniqueAdapters = adapters.GroupBy(a => a.Description).Select(g => g.First()).ToList();

                if (uniqueAdapters.Count < 2)
                {
                    return false;
                }

                var dgpu = uniqueAdapters.FirstOrDefault(a => a.Vendor == GpuVendor.Nvidia || a.DedicatedVideoMemoryBytes > 2L * 1024 * 1024 * 1024);
                var igpu = uniqueAdapters.FirstOrDefault(a => a != dgpu);

                if (dgpu != null && igpu != null)
                {
                    discreteName = dgpu.Description;
                    integratedName = igpu.Description;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to check dual GPU capability");
            }

            return false;
        }

        public static IGpuMetricsProvider Create(int gpuTarget = 0)
        {
            var adapters = DxgiHelper.EnumerateAdapters();
            Logger.Info($"GPU Factory: Found {adapters.Count} DXGI adapter(s), target={gpuTarget}:");
            foreach (var a in adapters)
            {
                Logger.Info($"  Adapter {a.Index}: '{a.Description}' (Vendor=0x{a.VendorId:X4}, DedicatedVRAM={a.DedicatedVideoMemoryBytes / (1024 * 1024)} MB, Software={a.IsSoftware})");
            }

            var hardwareAdapters = adapters.Where(a => !a.IsSoftware).ToList();
            if (hardwareAdapters.Count == 0)
            {
                hardwareAdapters = adapters;
            }

            // Target 1: Force Integrated GPU
            if (gpuTarget == 1)
            {
                var igpuAdapter = hardwareAdapters.FirstOrDefault(a => a.Vendor != GpuVendor.Nvidia && a.DedicatedVideoMemoryBytes <= 2L * 1024 * 1024 * 1024)
                               ?? hardwareAdapters.FirstOrDefault(a => a.Vendor == GpuVendor.Amd || a.Vendor == GpuVendor.Intel);

                if (igpuAdapter != null)
                {
                    if (igpuAdapter.Vendor == GpuVendor.Amd)
                    {
                        Logger.Info($"GPU Factory: Loading AMD ADLX provider for integrated GPU: {igpuAdapter.Description}");
                        return new AmdAdlxGpuMetricsProvider(igpuAdapter.Description, true);
                    }
                    else if (igpuAdapter.Vendor == GpuVendor.Intel)
                    {
                        Logger.Info($"GPU Factory: Loading Windows fallback provider for integrated Intel GPU: {igpuAdapter.Description}");
                        return new WindowsFallbackGpuMetricsProvider(igpuAdapter.Description, GpuVendor.Intel, DxgiHelper.VENDOR_INTEL);
                    }
                }
            }

            // Target 0 (Default): Discrete GPU
            // 1. Check for NVIDIA GPU
            var nvidiaAdapter = hardwareAdapters.FirstOrDefault(a => a.Vendor == GpuVendor.Nvidia);
            if (nvidiaAdapter != null)
            {
                try
                {
                    Logger.Info($"GPU Factory: Detected NVIDIA GPU ({nvidiaAdapter.Description}). Attempting NVAPI initialization...");
                    var nvidiaProvider = new NvidiaNvapiGpuMetricsProvider(nvidiaAdapter.Description);
                    if (nvidiaProvider.IsAvailable)
                    {
                        Logger.Info($"GPU Factory: Successfully loaded NVIDIA NVAPI provider ({nvidiaProvider.Name}).");
                        return nvidiaProvider;
                    }
                    else
                    {
                        Logger.Warn("GPU Factory: NVIDIA adapter present but NVAPI failed to initialize. Falling back...");
                        nvidiaProvider.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "GPU Factory: Error initializing NVIDIA provider.");
                }
            }

            // 2. Check for AMD GPU
            var amdAdapter = hardwareAdapters.FirstOrDefault(a => a.Vendor == GpuVendor.Amd);
            if (amdAdapter != null)
            {
                try
                {
                    Logger.Info($"GPU Factory: Detected AMD GPU ({amdAdapter.Description}). Loading AMD ADLX provider...");
                    var amdProvider = new AmdAdlxGpuMetricsProvider(amdAdapter.Description, false);
                    Logger.Info("GPU Factory: Successfully loaded AMD ADLX provider.");
                    return amdProvider;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "GPU Factory: Error initializing AMD ADLX provider.");
                }
            }

            // 3. Check for Intel GPU
            var intelAdapter = hardwareAdapters.FirstOrDefault(a => a.Vendor == GpuVendor.Intel);
            if (intelAdapter != null)
            {
                Logger.Info($"GPU Factory: Detected Intel GPU ({intelAdapter.Description}). Loading Windows metrics provider...");
                return new WindowsFallbackGpuMetricsProvider(intelAdapter.Description, GpuVendor.Intel, DxgiHelper.VENDOR_INTEL);
            }

            // 4. Default Universal Fallback
            var defaultAdapter = hardwareAdapters.FirstOrDefault();
            string adapterName = defaultAdapter?.Description ?? "Generic GPU";
            uint vendorId = defaultAdapter?.VendorId ?? 0;
            GpuVendor vendor = defaultAdapter?.Vendor ?? GpuVendor.Unknown;

            Logger.Info($"GPU Factory: Using universal Windows fallback provider for '{adapterName}'.");
            return new WindowsFallbackGpuMetricsProvider(adapterName, vendor, vendorId);
        }
    }
}
