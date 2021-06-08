using System;
using System.Drawing;

namespace UVtools.Core.Extensions
{
    public static class DrawingExtensions
    {
        public static Color FactorColor(this Color color, byte pixelColor, byte min = 0, byte max = byte.MaxValue) =>
            FactorColor(color, pixelColor / 255f, min, max);

        public static Color FactorColor(this Color color, float factor, byte min = 0, byte max = byte.MaxValue)
        {
            byte r = (byte)(color.R == 0 ? 0 :
                Math.Min(Math.Max(min, color.R * factor), max));

            byte g = (byte)(color.G == 0 ? 0 :
                Math.Min(Math.Max(min, color.G * factor), max));
            
            byte b = (byte)(color.B == 0 ? 0 :
                Math.Min(Math.Max(min, color.B * factor), max));
            return Color.FromArgb(r, g, b);
        }

        public static double CalculateSideLength(int sides, int radius)
        {
            return 2 * radius * Math.Sin(Math.PI / sides);
        }

        /*public static Point[] GetPolygonVertices(int sides, int radius, Point center, double startingAngle = 0)
        {
            if (sides < 3)
                throw new ArgumentException("Polygons can't have less than 3 sides...", nameof(sides));


            // Fix rotation
            switch (sides)
            {
                case 3:
                    startingAngle += 90;
                    break;
                case 4:
                    startingAngle += 45;
                    break;
                case 5:
                    startingAngle += 22.5;
                    break;
            }

            var points = new Point[sides];
            var step = 360.0 / sides;
            int i = 0;
            for (var angle = startingAngle; angle < startingAngle + 360.0; angle += step) //go in a circle
            {
                if (i == sides) break; // Fix floating problem
                double radians = angle * Math.PI / 180.0;
                points[i++] = new(
                    (int) Math.Round(Math.Cos(radians) * radius + center.X),
                    (int) Math.Round(Math.Sin(-radians) * radius + center.Y)
                );
            }

            return points;
        }*/

        /*public static Point[] GetPolygonVertices(int sides, int radius, Point center, double startingAngle = 0)
        {
            startingAngle = -45;
            if (sides < 3)
                throw new ArgumentException("Polygons can't have less than 3 sides...", nameof(sides));

            var vertex = new Point[sides];
            //var deg = (180.0 * (sides - 2)) / sides + startingAngle;
            var deg = ((180.0 * (sides - 2) / sides) - 180) / 2 + startingAngle;
            var step = 360.0 / sides;
            var rad = deg * (Math.PI / 180);

            double nSinDeg = Math.Sin(rad);
            double nCosDeg = Math.Cos(rad);

            vertex[0] = center;
            //vertex[0].X += radius; 
            vertex[0].Y -= radius; 
            //vertex[0].X += (int)Math.Cos(deg) / 2 *radius; 
            //vertex[0].Y -= (int)Math.Sin(deg) / 2 *radius; 
            int length = (int)Math.Round(CalculateSideLength(sides, radius));

            for (int i = 1; i < vertex.Length; i++)
            {
                vertex[i] = new(
                    (int)Math.Round(vertex[i - 1].X - nCosDeg * length),
                    (int)Math.Round(vertex[i - 1].Y - nSinDeg * length));


                //recalculate the degree for the next vertex
                deg -= step;
                rad = deg * (Math.PI / 180);

                nSinDeg = Math.Sin(rad);
                nCosDeg = Math.Cos(rad);

            }
            return vertex;
        }*/

        public static Point[] GetPolygonVertices(int sides, int radius, Point center, double startingAngle = 0)
        {
            if (sides < 3)
                throw new ArgumentException("Polygons can't have less than 3 sides...", nameof(sides));

            var vertices = new Point[sides];

            double deg = 360.0 / sides;//calculate the rotation angle
            //double a = radius * Math.Cos(Math.PI / sides);//calculate vertical length
            //double s = CalculateSideLength(sides, radius);//calculate the side length
            var rad = Math.PI / 180.0;

            vertices[0] = new(
                (int) Math.Round(center.X + radius * Math.Cos(-(((180 - deg) / 2) + startingAngle) * rad)),
                (int) Math.Round(center.Y - radius * Math.Sin(-(((180 - deg) / 2) + startingAngle) * rad)));

            vertices[1] = new(
                (int) Math.Round(center.X + radius * Math.Cos(-(((180 - deg) / 2) + deg + startingAngle) * rad)),
                (int) Math.Round(center.Y - radius * Math.Sin(-(((180 - deg) / 2) + deg + startingAngle) * rad)
                ));

            for (int i = 0; i < sides - 2; i++)
            {
                double dsinrot = Math.Sin((deg * (i + 1)) * rad);
                double dcosrot = Math.Cos((deg * (i + 1)) * rad);

                vertices[i + 2] = new(
                        (int)Math.Round(center.X + dcosrot * (vertices[1].X - center.X) - dsinrot * (vertices[1].Y - center.Y)),
                        (int)Math.Round(center.Y + dsinrot * (vertices[1].X - center.X) + dcosrot * (vertices[1].Y - center.Y))
                );
            }

            return vertices;
        }
    }
}
