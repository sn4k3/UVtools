/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

namespace UVtools.WPF.Extensions
{
    public static class DrawingExtensions
    {
        public static Avalonia.Media.Color ToAvalonia(this System.Drawing.Color color)
        {
            return new Avalonia.Media.Color(color.A, color.R, color.G, color.B);
        }

        public static System.Drawing.Color ToDotNet(this Avalonia.Media.Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
