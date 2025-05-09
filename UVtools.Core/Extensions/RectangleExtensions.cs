/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Drawing;
using ZLinq;

namespace UVtools.Core.Extensions;

public static class RectangleExtensions
{
    public static Point Center(this Rectangle src)
    {
        return new Point(src.Left + src.Width / 2, src.Top + src.Height / 2);
    }

    public static Rectangle OffsetBy(this Rectangle src, int x, int y)
    {
        var rect = src;
        rect.Offset(x, y);
        return rect;
    }

    public static Rectangle OffsetBy(this Rectangle src, Point position) => src.OffsetBy(position.X, position.Y);

    public static int Perimeter(this Rectangle src) => src.Width * 2 + src.Height * 2;
    public static float Perimeter(this RectangleF src) => src.Width * 2 + src.Height * 2;
    public static float Perimeter(this RectangleF src, int round) => MathF.Round(src.Perimeter(), round);
    public static int Area(this Rectangle src) => src.Width * src.Height;
    public static float Area(this RectangleF src) => src.Width * src.Height;
    public static float Area(this RectangleF src, int round) => MathF.Round(src.Area(), round);

    /// <summary>
    /// Gets the smallest rectangle among all rectangles
    /// </summary>
    /// <param name="rectangles"></param>
    /// <returns>The smallest rectangle</returns>
    public static Rectangle SmallestRectangle(params Rectangle[] rectangles) => rectangles.AsValueEnumerable().MinBy(rectangle => rectangle.Perimeter());

    /// <summary>
    /// Gets the smallest rectangle among all rectangles
    /// </summary>
    /// <param name="rectangles"></param>
    /// <returns>The smallest rectangle</returns>
    public static RectangleF SmallestRectangle(params RectangleF[] rectangles) => rectangles.AsValueEnumerable().MinBy(rectangle => rectangle.Perimeter());

    /// <summary>
    /// Gets the largest rectangle among all rectangles
    /// </summary>
    /// <param name="rectangles"></param>
    /// <returns>The largest rectangle</returns>
    public static Rectangle LargestRectangle(params Rectangle[] rectangles) => rectangles.AsValueEnumerable().MaxBy(rectangle => rectangle.Perimeter());

    /// <summary>
    /// Gets the largest rectangle among all rectangles
    /// </summary>
    /// <param name="rectangles"></param>
    /// <returns>The largest rectangle</returns>
    public static RectangleF LargestRectangle(params RectangleF[] rectangles) => rectangles.AsValueEnumerable().MaxBy(rectangle => rectangle.Perimeter());

}