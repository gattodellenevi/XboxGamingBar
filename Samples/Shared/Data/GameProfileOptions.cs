using System.Collections.Generic;

namespace Shared.Data
{
    public struct GameProfileOptions
    {
        public string GameName { get; set; }
        public string GamePath { get; set; }
        public bool InUse { get; set; }
        public bool CPUBoost { get; set; }
        public int CPUEPP { get; set; }
        public bool SetCPUEPP { get; set; }
        public int CPUClock { get; set; }
        public int FPSLimit { get; set; }
        public int FPSLimitMode { get; set; }
        public bool JudderFreeFPS { get; set; }
        public string ProfilePath { get; set; }
        public IDictionary<GameId, GameProfile> Cache { get; set; }

        public static GameProfileOptions CreateDefault()
        {
            return new GameProfileOptions
            {
                GameName = string.Empty,
                GamePath = string.Empty,
                InUse = true,
                CPUBoost = true,
                CPUEPP = 80,
                SetCPUEPP = false,
                CPUClock = 0,
                FPSLimit = 0,
                FPSLimitMode = 0,
                JudderFreeFPS = true,
                ProfilePath = string.Empty,
                Cache = null
            };
        }
    }
}
