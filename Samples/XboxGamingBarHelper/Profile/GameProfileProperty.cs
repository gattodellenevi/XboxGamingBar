using Shared.Data;
using Shared.Enums;
using XboxGamingBarHelper.Core;

namespace XboxGamingBarHelper.Profile
{
    internal class GameProfileProperty : HelperProperty<GameProfile, ProfileManager>
    {
        public GameProfileProperty(GameProfile inValue, ProfileManager inManager) : base(inValue, null, Function.None, inManager)
        {
        }

        public int TDP
        {
            get { return value.TDP; }
            set
            {
                if (this.value.TDP != value)
                {
                    this.value.TDP = value;
                }
            }
        }

        public bool CPUBoost
        {
            get { return value.CPUBoost; }
            set
            {
                if (this.value.CPUBoost != value)
                {
                    this.value.CPUBoost = value;
                }
            }
        }

        public int CPUEPP
        {
            get { return value.CPUEPP; }
            set
            {
                if (this.value.CPUEPP != value)
                {
                    this.value.CPUEPP = value;
                }
            }
        }

        public bool SetCPUEPP
        {
            get { return value.SetCPUEPP; }
            set
            {
                if (this.value.SetCPUEPP != value)
                {
                    this.value.SetCPUEPP = value;
                }
            }
        }

        public int CPUClock
        {
            get { return value.CPUClock; }
            set
            {
                if (this.Value.CPUClock != value)
                {
                    this.value.CPUClock = value;
                }
            }
        }

        public int FPSLimit
        {
            get { return value.FPSLimit; }
            set
            {
                if (this.value.FPSLimit != value)
                {
                    this.value.FPSLimit = value;
                }
            }
        }

        public int FPSLimitMode
        {
            get { return value.FPSLimitMode; }
            set
            {
                if (this.value.FPSLimitMode != value)
                {
                    this.value.FPSLimitMode = value;
                }
            }
        }

        public bool JudderFreeFPS
        {
            get { return value.JudderFreeFPS; }
            set
            {
                if (this.value.JudderFreeFPS != value)
                {
                    this.value.JudderFreeFPS = value;
                }
            }
        }

        public GameId GameId
        {
            get { return value.GameId; }
        }

        public bool Use
        {
            get { return value.Use; }
            set
            {
                if (this.value.Use != value)
                {
                    this.value.Use = value;
                }
            }
        }

        public bool IsGlobalProfile
        {
            get { return value.IsGlobalProfile; }
        }
    }
}
