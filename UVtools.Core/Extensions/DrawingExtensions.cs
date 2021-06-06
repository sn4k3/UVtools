using System;
using System.Drawing;

namespace UVtools.Core.Extensions
{
    public static class DrawingExtensions
    {
        public static Color FactorColor(this Color color, byte pixelColor, byte min = 0, byte max = byte.MaxValue) =>
            FactorColor(color, pixelColor / 255f, min, max);

        public static Color FactorColor(this Color color, float factor, byte min = 0, byte max = byte.MaxValue)
        {
            byte r = (byte)(color.R == 0 ? 0 :
                Math.Min(Math.Max(min, color.R * factor), max));

            byte g = (byte)(color.G == 0 ? 0 :
                Math.Min(Math.Max(min, color.G * factor), max));
            
            byte b = (byte)(color.B == 0 ? 0 :
                Math.Min(Math.Max(min, color.B * factor), max));
            return Color.FromArgb(r, g, b);
        }
    }
}
