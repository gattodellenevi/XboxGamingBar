using System;
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
        public static readonly HDRColorFormatter Instance = new HDRColorFormatter(0.38f, 0.45f);

        private readonly float dimFactor;
        private readonly float saturationFactor;

        public HDRColorFormatter(float dimFactor = 0.38f, float saturationFactor = 0.45f)
        {
            this.dimFactor = dimFactor;
            this.saturationFactor = saturationFactor;
        }

        public string Format(Color baseColor)
        {
            // 1. Calculate perceived luminance (grayscale value using standard Rec. 709 coefficients)
            float lum = 0.2126f * baseColor.R + 0.7152f * baseColor.G + 0.0722f * baseColor.B;

            // 2. Desaturate towards luminance to eliminate wide-gamut / HDR oversaturation
            float r = lum + saturationFactor * (baseColor.R - lum);
            float g = lum + saturationFactor * (baseColor.G - lum);
            float b = lum + saturationFactor * (baseColor.B - lum);

            // 3. Apply HDR brightness dimming
            byte rOut = (byte)Math.Max(0, Math.Min(255, (int)(r * dimFactor)));
            byte gOut = (byte)Math.Max(0, Math.Min(255, (int)(g * dimFactor)));
            byte bOut = (byte)Math.Max(0, Math.Min(255, (int)(b * dimFactor)));

            return $"{rOut:X2}{gOut:X2}{bOut:X2}";
        }
    }
}
