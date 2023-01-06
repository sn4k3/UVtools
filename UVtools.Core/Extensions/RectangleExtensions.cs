/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.Drawing;
using System.Linq;

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

    /// <summary>
    /// Gets the smallest rectangle among all rectangles
    /// </summary>
    /// <param name="rectangles"></param>
    /// <returns>The smallest rectangle</returns>
    public static Rectangle SmallestRectangle(params Rectangle[] rectangles) => rectangles.MinBy(rectangle => rectangle.Area());

    /// <summary>
    /// Gets the smallest rectangle among all rectangles
    /// </summary>
    /// <param name="rectangles"></param>
    /// <returns>The smallest rectangle</returns>
    public static RectangleF SmallestRectangle(params RectangleF[] rectangles) => rectangles.MinBy(rectangle => rectangle.Area());

    /// <summary>
    /// Gets the largest rectangle among all rectangles
    /// </summary>
    /// <param name="rectangles"></param>
    /// <returns>The largest rectangle</returns>
    public static Rectangle LargestRectangle(params Rectangle[] rectangles) => rectangles.MaxBy(rectangle => rectangle.Area());

    /// <summary>
    /// Gets the largest rectangle among all rectangles
    /// </summary>
    /// <param name="rectangles"></param>
    /// <returns>The largest rectangle</returns>
    public static RectangleF LargestRectangle(params RectangleF[] rectangles) => rectangles.MaxBy(rectangle => rectangle.Area());

}