/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Drawing;

namespace UVtools.Core.Extensions;

public static class SizeExtensions
{
    public static readonly string[] SizeSuffixes =
        ["bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];

    public static string SizeSuffix(long value, byte decimalPlaces = 2, bool suffixSpaced = true)
    {
        //if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
        if (value < 0) { return "-" + SizeSuffix(-value); }
        if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

        // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
        int mag = (int)Math.Log(value, 1024);

        // 1L << (mag * 10) == 2 ^ (10 * mag) 
        // [i.e. the number of bytes in the unit corresponding to mag]
        decimal adjustedSize = (decimal)value / (1L << (mag * 10));

        // make adjustment when the value is large enough that
        // it would round up to 1000 or more
        if (Math.Round(adjustedSize, decimalPlaces, MidpointRounding.AwayFromZero) >= 1000)
        {
            mag += 1;
            adjustedSize /= 1024;
        }

        return string.Format($"{{0:n{decimalPlaces}}}{(suffixSpaced ? ' ' : string.Empty)}{SizeSuffixes[mag]}", adjustedSize);
    }

    public static string SizeSuffix(long value, bool suffixSpaced) => SizeSuffix(value, 2, suffixSpaced);

    public static Size Add(this Size size, Size otherSize) => new (size.Width + otherSize.Width, size.Height + otherSize.Height);
    public static Size Add(this Size size) => size.Add(size);
    public static Size Add(this Size size, int pixels) => new (size.Width + pixels, size.Height + pixels);
    public static Size Add(this Size size, int width, int height) => new (size.Width + width, size.Height + height);

    public static Size Subtract(this Size size, Size otherSize) => new(size.Width - otherSize.Width, size.Height - otherSize.Height);
    public static Size Subtract(this Size size) => size.Subtract(size);
    public static Size Subtract(this Size size, int pixels) => new(size.Width - pixels, size.Height - pixels);
    public static Size Subtract(this Size size, int width, int height) => new(size.Width - width, size.Height - height);

    public static Size Multiply(this Size size, SizeF otherSize) => new((int)(size.Width * otherSize.Width), (int)(size.Height * otherSize.Height));
    public static Size Multiply(this Size size) => size.Multiply(size);
    public static Size Multiply(this Size size, double dxy) => new((int)(size.Width * dxy), (int)(size.Height * dxy));
    public static Size Multiply(this Size size, double dx, double dy) => new((int)(size.Width * dx), (int)(size.Height * dy));

    public static Size Divide(this Size size, Size otherSize) => new(otherSize.Width == 0 ? 0 : size.Width / otherSize.Width, otherSize.Height == 0 ? 0 : size.Height / otherSize.Height);
    public static Size Divide(this Size size, SizeF otherSize) => new((int)(otherSize.Width == 0 ? 0 : size.Width / otherSize.Width), (int)(otherSize.Height == 0 ? 0 : size.Height / otherSize.Height));
    public static Size Divide(this Size size) => size.Divide(size);
    public static Size Divide(this Size size, double dxy) => dxy == 0 ? Size.Empty : new((int)(size.Width / dxy), (int)(size.Height / dxy));
    public static Size Divide(this Size size, double dx, double dy) => new((int)(dx == 0 ? 0 : size.Width / dx), (int)(dy == 0 ? 0 : size.Height / dy));

    public static SizeF Divide(this SizeF size, Size otherSize) => new((int)(otherSize.Width == 0 ? 0 : size.Width / otherSize.Width), (int)(otherSize.Height == 0 ? 0 : size.Height / otherSize.Height));
    public static SizeF Divide(this SizeF size, SizeF otherSize) => new((int)(otherSize.Width == 0 ? 0 : size.Width / otherSize.Width), (int)(otherSize.Height == 0 ? 0 : size.Height / otherSize.Height));
    public static SizeF Divide(this SizeF size) => size.Divide(size);
    public static SizeF Divide(this SizeF size, double dxy) => dxy == 0 ? Size.Empty : new((int)(size.Width / dxy), (int)(size.Height / dxy));
    public static SizeF Divide(this SizeF size, double dx, double dy) => new((int)(dx == 0 ? 0 : size.Width / dx), (int)(dy == 0 ? 0 : size.Height / dy));

    /// <summary>
    /// Gets if this size have a zero value on width or height
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public static bool HaveZero(this Size size) => size.Width <= 0 || size.Height <= 0;

    /// <summary>
    /// Exchange width with height
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public static Size Exchange(this Size size) => new(size.Height, size.Width);

    public static int Area(this Size size) => size.Width * size.Height;

    public static int Max(this Size size) => Math.Max(size.Width, size.Height);

    /// <summary>
    /// Gets a new <see cref="Size"/> with the maximum of all sizes Width and Height
    /// </summary>
    /// <param name="sizes"></param>
    /// <returns></returns>
    public static Size Max(params Size[] sizes)
    {
        var maxSize = new Size();
        foreach (var size in sizes)
        {
            maxSize.Width = Math.Max(maxSize.Width, size.Width);
            maxSize.Height = Math.Max(maxSize.Height, size.Height);
        }

        return maxSize;
    }


    /// <summary>
    /// Gets if this size have a zero value on width or height
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public static bool HaveZero(this SizeF size) => size.Width <= 0 || size.Height <= 0;

    public static float Area(this SizeF size) => size.Width * size.Height;
    public static float Area(this SizeF size, int round) => MathF.Round(size.Area(), round);

    public static float Max(this SizeF size) => Math.Max(size.Width, size.Height);

    /// <summary>
    /// Gets a new <see cref="Size"/> with the maximum of all sizes Width and Height
    /// </summary>
    /// <param name="sizes"></param>
    /// <returns></returns>
    public static SizeF Max(params SizeF[] sizes)
    {
        var maxSize = new SizeF();
        foreach (var size in sizes)
        {
            maxSize.Width = Math.Max(maxSize.Width, size.Width);
            maxSize.Height = Math.Max(maxSize.Height, size.Height);
        }

        return maxSize;
    }

    public static Size Half(this Size size) => new(size.Width / 2, size.Height / 2);

    public static SizeF Half(this SizeF size) => new(size.Width / 2f, size.Height / 2f);

    public static Point ToPoint(this Size size) => new(size.Width, size.Height);
    public static PointF ToPointF(this Size size) => new(size.Width, size.Height);
    public static Point ToPoint(this SizeF size) => new((int)size.Width, (int)size.Height);
    public static PointF ToPointF(this SizeF size) => new(size.Width, size.Height);

    public static Size Rotate(this Size size, double angleDegree)
    {
        if (angleDegree % 360 == 0) return size;
        var sizeHalf = size.Half();
        double angle = angleDegree * Math.PI / 180;
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        // var newImgWidth  = + (float)(x0 + Math.Abs((x - x0) * Math.Cos(rad)) + Math.Abs((y - y0) * Math.Sin(rad)));
        //var newImgHeight = -(float)(y0 + Math.Abs((x - x0) * Math.Sin(rad)) + Math.Abs((y - y0) * Math.Cos(rad)));
        int dx = size.Width - sizeHalf.Width;
        int dy = size.Height - sizeHalf.Height;
        //double width = sizeHalf.Width + Math.Abs(dx * cos) + Math.Abs(dy * sin);
        //double height = sizeHalf.Height + Math.Abs(dx * sin) + Math.Abs(dy * cos);
        double width = Math.Abs(cos * dx) - Math.Abs(sin * dy) + sizeHalf.Width;
        double height = Math.Abs(sin * dx) + Math.Abs(cos * dy) + sizeHalf.Height;

        return new((int)Math.Round(width), (int)Math.Round(height));
    }

}