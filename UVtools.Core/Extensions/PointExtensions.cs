using System;
using System.Drawing;

namespace UVtools.Core.Extensions
{
    public static class PointExtensions
    {
        public static double FindLength(Point start, Point end) => Math.Sqrt(Math.Pow(end.Y - start.Y, 2) + Math.Pow(end.X - start.X, 2));
        
        public static Point Rotate(this Point point, double angleDegree, Point pivot = default)
        {
            if (angleDegree == 0 || angleDegree == 360) return point;
            double angle = angleDegree * Math.PI / 180;
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            int dx = point.X - pivot.X;
            int dy = point.Y - pivot.Y;
            double x = cos * dx - sin * dy + pivot.X;
            double y = sin * dx + cos * dy + pivot.Y;

            Point rotated = new Point((int)Math.Round(x), (int)Math.Round(y));
            return rotated;
        }

    }
}
