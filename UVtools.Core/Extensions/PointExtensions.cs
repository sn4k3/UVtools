/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Drawing;

namespace UVtools.Core.Extensions
{
    public static class PointExtensions
    {
        public static double FindLength(Point start, Point end) => Math.Sqrt(Math.Pow(end.Y - start.Y, 2) + Math.Pow(end.X - start.X, 2));
        
        public static Point Rotate(this Point point, double angleDegree, Point pivot = default)
        {
            if (angleDegree is 0 or 360) return point;
            double angle = angleDegree * Math.PI / 180;
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            int dx = point.X - pivot.X;
            int dy = point.Y - pivot.Y;
            double x = cos * dx - sin * dy + pivot.X;
            double y = sin * dx + cos * dy + pivot.Y;

            Point rotated = new((int)Math.Round(x), (int)Math.Round(y));
            return rotated;
        }

    }
}
