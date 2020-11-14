using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace UVtools.Core.Extensions
{
    public static class PointExtensions
    {
        public static Point Rotate(this Point point, double angleDegree, Point pivot = default)
        {
            double angle = angleDegree * Math.PI / 180;
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            int dx = point.X - pivot.X;
            int dy = point.Y - pivot.Y;
            double x = cos * dx - sin * dy + pivot.X;
            double y = sin * dx + cos * dy + pivot.X;

            Point rotated = new Point((int)Math.Round(x), (int)Math.Round(y));
            return rotated;
        }
    }
}
