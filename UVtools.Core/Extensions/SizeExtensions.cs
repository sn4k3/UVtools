/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace UVtools.Core.Extensions
{
    public static class SizeExtensions
    {
        public static readonly string[] SizeSuffixes =
            { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string SizeSuffix(long value, byte decimalPlaces = 2)
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
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }

        public static Size Inflate(this Size size, Size otherSize) => new (size.Width + otherSize.Width, size.Height + otherSize.Height);
        public static Size Inflate(this Size size) => size.Inflate(size);
        public static Size Inflate(this Size size, int pixels) => new (size.Width + pixels, size.Height + pixels);
        public static Size Inflate(this Size size, int width, int height) => new (size.Width + width, size.Height + height);

        /// <summary>
        /// Gets if this size have a zero value on width or height
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static bool HaveZero(this Size size) => size.Width <= 0 && size.Height <= 0;

        /// <summary>
        /// Exchange width with height
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Size Invert(this Size size) => new(size.Height, size.Width);

        public static int Area(this Rectangle rect) => rect.Width * rect.Height;

        public static int Area(this Size size) => size.Width * size.Height;

        public static int Max(this Size size) => Math.Max(size.Width, size.Height);


        public static float Area(this RectangleF rect, int round = -1) => round >= 0 ? (float) Math.Round(rect.Width * rect.Height, round) : rect.Width * rect.Height;

        /// <summary>
        /// Gets if this size have a zero value on width or height
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static bool HaveZero(this SizeF size) => size.Width <= 0 && size.Height <= 0;

        public static float Area(this SizeF size, int round = -1) => round >= 0 ? (float)Math.Round(size.Width * size.Height, round) : size.Width * size.Height;

        public static float Max(this SizeF size) => Math.Max(size.Width, size.Height);

        public static Size Half(this Size size) => new(size.Width / 2, size.Height / 2);

        public static SizeF Half(this SizeF size) => new(size.Width / 2, size.Height / 2);

        public static Point ToPoint(this Size size) => new(size.Width, size.Height);
        public static PointF ToPoint(this SizeF size) => new(size.Width, size.Height);

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
}
