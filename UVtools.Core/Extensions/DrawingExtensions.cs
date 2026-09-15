using System;
using System.Drawing;
using Emgu.CV.Structure;

namespace UVtools.Core.Extensions;

public static class DrawingExtensions
{
    public static Color FactorColor(this Color color, byte pixelColor, byte min = 0, byte max = byte.MaxValue) => color.FactorColor(pixelColor / 255.0, min, max);

    public static Color FactorColor(this Color color, double factor, byte min = 0, byte max = byte.MaxValue)
    {
        byte r = (byte)(color.R == 0 ? 0 :
            Math.Min(Math.Max(min, color.R * factor), max));

        byte g = (byte)(color.G == 0 ? 0 :
            Math.Min(Math.Max(min, color.G * factor), max));

        byte b = (byte)(color.B == 0 ? 0 :
            Math.Min(Math.Max(min, color.B * factor), max));
        return Color.FromArgb(r, g, b);
    }

    public static double CalculatePolygonSideLengthFromRadius(double radius, int sides)
    {
        return 2 * radius * Math.Sin(Math.PI / sides);
    }

    public static double CalculatePolygonVerticalLengthFromRadius(double radius, int sides)
    {
        return radius * Math.Cos(Math.PI / sides);
    }

    public static double CalculatePolygonRadiusFromSideLength(double length, int sides)
    {
        var theta = 360.0 / sides;
        return length / (2 * Math.Cos((90 - theta / 2) * Math.PI / 180.0));
    }

    public static Point[] GetPolygonVertices(int sides, SizeF diameter, PointF center, double startingAngle = 0, bool flipHorizontally = false, bool flipVertically = false, MidpointRounding midpointRounding = MidpointRounding.AwayFromZero)
    {
        if (sides < 3)
            throw new ArgumentException("Polygons can't have less than 3 sides...", nameof(sides));

        var vertices = new Point[sides];
        var radiusX = diameter.Width / 2; // X radius for pixel pitch
        var radiusY = diameter.Height / 2; // Y radius for pixel pitch

        if (sides == 4)
        {
            var rotatedRect = new RotatedRect(center, new SizeF(diameter.Width - 1, diameter.Height - 1), (float)startingAngle);
            var verticesF = rotatedRect.GetVertices();
            for (var i = 0; i < verticesF.Length; i++)
            {
                vertices[i] = verticesF[i].ToPoint(midpointRounding);
            }
        }
        else
        {
            var angleIncrement = 2 * Math.PI / sides;
            var startRotationAngleRadians = startingAngle * Math.PI / 180;

            for (int i = 0; i < sides; i++)
            {
                var angle = startRotationAngleRadians + i * angleIncrement;

                // Scale the X and Y coordinates independently for pixel pitch
                var x = (int)Math.Round(center.X + radiusX * Math.Cos(angle), midpointRounding);
                var y = (int)Math.Round(center.Y + radiusY * Math.Sin(angle), midpointRounding);

                vertices[i] = new Point(x, y);
            }
        }

        if (flipHorizontally)
        {
            var startX = center.X - radiusX;
            var endX = center.X + radiusX;
            for (int i = 0; i < sides; i++)
            {
                vertices[i].X = (int)Math.Round(endX - (vertices[i].X - startX), midpointRounding);
            }
        }

        if (flipVertically)
        {
            var startY = center.Y - radiusY;
            var endY = center.Y + radiusY;
            for (int i = 0; i < sides; i++)
            {
                vertices[i].Y = (int)Math.Round(endY - (vertices[i].Y - startY), midpointRounding);
            }
        }

        return vertices;
    }


    public static Point[] GetAlignedPolygonVertices(int sides, SizeF diameter, PointF center, double startingAngle = 0, bool flipHorizontally = false, bool flipVertically = false, MidpointRounding midpointRounding = MidpointRounding.AwayFromZero)
    {
        if (sides != 4) startingAngle += (180 - (360.0 / sides)) / 2;
        return GetPolygonVertices(sides, diameter, center, startingAngle, flipHorizontally, flipVertically, midpointRounding);

        /*if (sides < 3)
            throw new ArgumentException("Polygons can't have less than 3 sides...", nameof(sides));

        var vertices = new Point[sides];
        var radius = diameter / 2;

        if (sides == 4)
        {
            var rotatedRect = new RotatedRect(center, new SizeF((float)diameter - 1, (float)diameter - 1), (float)startingAngle);
            var verticesF = rotatedRect.GetVertices();
            for (var i = 0; i < verticesF.Length; i++)
            {
                vertices[i] = verticesF[i].ToPoint(midpointRounding);
            }
        }
        else
        {
            // Aligned version
            var angleIncrement = 360.0 / sides; //calculate the rotation angle
            var rad = Math.PI / 180.0;

            var x0 = center.X + radius * Math.Cos(-(((180 - angleIncrement) / 2) + startingAngle) * rad);
            var y0 = center.Y - radius * Math.Sin(-(((180 - angleIncrement) / 2) + startingAngle) * rad);

            var x1 = center.X + radius * Math.Cos(-(((180 - angleIncrement) / 2) + startingAngle + angleIncrement) * rad);
            var y1 = center.Y - radius * Math.Sin(-(((180 - angleIncrement) / 2) + startingAngle + angleIncrement) * rad);

            vertices[0] = new(
                (int)Math.Round(x0, midpointRounding),
                (int)Math.Round(y0, midpointRounding)
            );

            vertices[1] = new(
                (int)Math.Round(x1, midpointRounding),
                (int)Math.Round(y1, midpointRounding)
            );

            for (int i = 0; i < sides - 2; i++)
            {
                var dsinrot = Math.Sin(angleIncrement * (i + 1) * rad);
                var dcosrot = Math.Cos(angleIncrement * (i + 1) * rad);

                vertices[i + 2] = new(
                    (int)Math.Round(center.X + dcosrot * (x1 - center.X) - dsinrot * (y1 - center.Y), midpointRounding),
                    (int)Math.Round(center.Y + dsinrot * (x1 - center.X) + dcosrot * (y1 - center.Y), midpointRounding)
                );
            }
        }

        if (flipHorizontally)
        {
            var startX = center.X - radius;
            var endX = center.X + radius;
            for (int i = 0; i < sides; i++)
            {
                vertices[i].X = (int)Math.Round(endX - (vertices[i].X - startX), midpointRounding);
            }
        }

        if (flipVertically)
        {
            var startY = center.Y - radius;
            var endY = center.Y + radius;
            for (int i = 0; i < sides; i++)
            {
                vertices[i].Y = (int)Math.Round(endY - (vertices[i].Y - startY), midpointRounding);
            }
        }

        return vertices;
    */
    }

    public static Point[] GetAlignedPolygonVertices(int sides, float diameter, PointF center, double startingAngle = 0, bool flipHorizontally = false, bool flipVertically = false, MidpointRounding midpointRounding = MidpointRounding.AwayFromZero)
    {
        if (sides != 4) startingAngle += (180 - (360.0 / sides)) / 2;
        return GetPolygonVertices(sides, new SizeF(diameter, diameter), center, startingAngle, flipHorizontally, flipVertically, midpointRounding);
    }

    /// <summary>
    /// Interpolates the pixels of a line between two points (inclusive of both endpoints) using
    /// Bresenham's algorithm.
    /// </summary>
    /// <param name="start">Start point.</param>
    /// <param name="end">End point.</param>
    /// <returns>An allocation-free enumerator over the ordered pixels from <paramref name="start"/> to
    /// <paramref name="end"/>, both inclusive.</returns>
    public static LineEnumerator InterpolateLine(this Point start, Point end) => new(start, end);

    public struct LineEnumerator
    {
        private readonly Point _end;
        private readonly int _deltaX;
        private readonly int _deltaY;
        private readonly int _stepX;
        private readonly int _stepY;
        private int _error;
        private bool _started;
        private bool _complete;

        public Point Current { get; private set; }

        internal LineEnumerator(Point start, Point end)
        {
            _end = end;
            _deltaX = Math.Abs(end.X - start.X);
            _deltaY = Math.Abs(end.Y - start.Y);
            _stepX = start.X < end.X ? 1 : -1;
            _stepY = start.Y < end.Y ? 1 : -1;
            _error = _deltaX - _deltaY;
            Current = start;
        }

        public readonly LineEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (_complete) return false;
            if (!_started)
            {
                _started = true;
                _complete = Current == _end;
                return true;
            }

            var error = 2 * _error;
            if (error > -_deltaY)
            {
                _error -= _deltaY;
                Current = Current with { X = Current.X + _stepX };
            }

            if (error < _deltaX)
            {
                _error += _deltaX;
                Current = Current with { Y = Current.Y + _stepY };
            }

            _complete = Current == _end;
            return true;
        }
    }
}
