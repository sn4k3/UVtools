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
            if (angleDegree % 360 == 0) return point;
            double angle = angleDegree * Math.PI / 180;
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            int dx = point.X - pivot.X;
            int dy = point.Y - pivot.Y;
            double x = cos * dx - sin * dy + pivot.X;
            double y = sin * dx + cos * dy + pivot.Y;

            return new((int)Math.Round(x), (int)Math.Round(y));
        }

        public static PointF Rotate(this PointF point, double angleDegree, PointF pivot = default)
        {
            if (angleDegree % 360 == 0) return point;
            double angle = angleDegree * Math.PI / 180;
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            double dx = point.X - pivot.X;
            double dy = point.Y - pivot.Y;
            double x = cos * dx - sin * dy + pivot.X;
            double y = sin * dx + cos * dy + pivot.Y;

            return new((float) x, (float) y);
        }

        public static Point Half(this Point point)=> new(point.X / 2, point.Y / 2);

        public static PointF Half(this PointF point) => new(point.X / 2, point.Y / 2);

        public static Size ToSize(this Point point) => new(point.X, point.Y);

        public static SizeF ToSize(this PointF point) => new(point.X, point.Y);

    }
}
