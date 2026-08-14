using System.Drawing;

namespace XboxGamingBarHelper.RTSS
{
    internal interface IColorFormatter
    {
        string Format(Color baseColor);
    }

    internal class SDRColorFormatter : IColorFormatter
    {
        public static readonly SDRColorFormatter Instance = new SDRColorFormatter();

        public string Format(Color baseColor)
        {
            return $"{baseColor.R:X2}{baseColor.G:X2}{baseColor.B:X2}";
        }
    }

    internal class HDRColorFormatter : IColorFormatter
    {
        public static readonly HDRColorFormatter Instance = new HDRColorFormatter(0.5f);

        private readonly float dimFactor;

        public HDRColorFormatter(float dimFactor = 0.5f)
        {
            this.dimFactor = dimFactor;
        }

        public string Format(Color baseColor)
        {
            byte r = (byte)(baseColor.R * dimFactor);
            byte g = (byte)(baseColor.G * dimFactor);
            byte b = (byte)(baseColor.B * dimFactor);
            return $"{r:X2}{g:X2}{b:X2}";
        }
    }
}
