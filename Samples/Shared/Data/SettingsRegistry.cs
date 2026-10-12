using Shared.Enums;
using System;
using System.Collections.Generic;

namespace Shared.Data
{
    public static class SettingsRegistry
    {
        private static readonly Dictionary<Function, ISettingDefinition> definitionsByFunction = new Dictionary<Function, ISettingDefinition>();
        private static readonly Dictionary<string, ISettingDefinition> definitionsByKey = new Dictionary<string, ISettingDefinition>(StringComparer.OrdinalIgnoreCase);

        // Core Global Settings
        public static readonly ISettingDefinition<int> OnScreenDisplayProvider = Register(
            new SettingDefinition<int>(Function.Settings_OnScreenDisplayProvider, "OnScreenDisplayProvider", SettingScope.GlobalOnly, 0));

        public static readonly ISettingDefinition<int> OnScreenDisplay = Register(
            new SettingDefinition<int>(Function.OSD, "OnScreenDisplay", SettingScope.GlobalOnly, 0));

        public static readonly ISettingDefinition<int> OnScreenDisplayTextSize = Register(
            new SettingDefinition<int>(Function.OSDTextSize, "OnScreenDisplayTextSize", SettingScope.GlobalOnly, 100, v => Math.Max(50, Math.Min(200, v))));

        public static readonly ISettingDefinition<int> GpuTarget = Register(
            new SettingDefinition<int>(Function.Settings_GpuTarget, "GpuTarget", SettingScope.GlobalOnly, 0));

        // Core Game / Profile Settings
        public static readonly ISettingDefinition<int> FPSLimit = Register(
            new SettingDefinition<int>(Function.FPSLimit, "FPSLimit", SettingScope.ProfileOrGlobal, 0, v => Math.Max(0, v)));

        public static readonly ISettingDefinition<int> FPSLimitMode = Register(
            new SettingDefinition<int>(Function.FPSLimitMode, "FPSLimitMode", SettingScope.ProfileOrGlobal, 0));

        public static readonly ISettingDefinition<bool> JudderFreeFPS = Register(
            new SettingDefinition<bool>(Function.JudderFreeFPS, "JudderFreeFPS", SettingScope.ProfileOrGlobal, true));

        public static readonly ISettingDefinition<bool> LimitFPS = Register(
            new SettingDefinition<bool>(Function.LimitFPS, "LimitFPS", SettingScope.ProfileOrGlobal, false));

        public static readonly ISettingDefinition<int> RefreshRate = Register(
            new SettingDefinition<int>(Function.RefreshRate, "RefreshRate", SettingScope.ProfileOrGlobal, 0));

        public static readonly ISettingDefinition<bool> HDR = Register(
            new SettingDefinition<bool>(Function.HDR, "HDR", SettingScope.ProfileOrGlobal, false));

        // System & Mouse Settings
        public static readonly ISettingDefinition<int> MouseSpeed = Register(
            new SettingDefinition<int>(Function.MouseSpeed, "MouseSpeed", SettingScope.GlobalOnly, 10, v => Math.Max(1, Math.Min(20, v))));

        public static readonly ISettingDefinition<bool> MouseEnhancePointerPrecision = Register(
            new SettingDefinition<bool>(Function.MouseEnhancePointerPrecision, "MouseEnhancePointerPrecision", SettingScope.GlobalOnly, false));

        public static readonly ISettingDefinition<int> MouseScrollSpeed = Register(
            new SettingDefinition<int>(Function.MouseScrollSpeed, "MouseScrollSpeed", SettingScope.GlobalOnly, 3, v => Math.Max(1, Math.Min(100, v))));

        public static ISettingDefinition<T> Register<T>(SettingDefinition<T> definition)
        {
            definitionsByFunction[definition.Function] = definition;
            definitionsByKey[definition.Key] = definition;
            return definition;
        }

        public static bool TryGet(Function function, out ISettingDefinition definition)
        {
            return definitionsByFunction.TryGetValue(function, out definition);
        }

        public static bool TryGet(string key, out ISettingDefinition definition)
        {
            return definitionsByKey.TryGetValue(key, out definition);
        }

        public static IEnumerable<ISettingDefinition> AllDefinitions => definitionsByFunction.Values;
    }
}
