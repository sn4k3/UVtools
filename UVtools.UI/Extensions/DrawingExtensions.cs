/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Avalonia;
using System.Drawing;

namespace UVtools.UI.Extensions;

public static class DrawingExtensions
{
    public static Avalonia.Media.Color ToAvalonia(this System.Drawing.Color color)
    {
        return new(color.A, color.R, color.G, color.B);
    }

    public static System.Drawing.Color ToDotNet(this Avalonia.Media.Color color)
    {
        return Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    public static Rect ToAvalonia(this Rectangle rectangle)
    {
        return new(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height); 
    }

    public static Rectangle ToDotNet(this Rect rectangle)
    {
        return new((int) rectangle.X, (int) rectangle.Y, (int) rectangle.Width, (int) rectangle.Height);
    }
}