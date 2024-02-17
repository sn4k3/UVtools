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

public static class PointExtensions
{
    public static double FindLength(Point start, Point end) => Math.Sqrt(Math.Pow(end.Y - start.Y, 2) + Math.Pow(end.X - start.X, 2));
    public static double GetAngleRad(Point start, Point end)
    {
        var angle = Math.Atan2(start.Y - end.Y, end.X - start.X);
        return angle < 0 ? Math.PI * 2 + angle : angle;
    }

    public static double GetAngleDeg(Point start, Point end) => (180 / Math.PI) * GetAngleRad(start, end);

    public static bool IsAnyNegative(this Point point) => point.X < 0 || point.Y < 0;
    public static bool IsBothNegative(this Point point) => point is {X: < 0, Y: < 0};
    public static bool IsBothZeroOrPositive(this Point point) => point is {X: >= 0, Y: >= 0};

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

    public static void Rotate(Point[] points, double angleDegree, Point pivot = default)
    {
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = points[i].Rotate(angleDegree, pivot);
        }
    }

    public static void Rotate(PointF[] points, double angleDegree, PointF pivot = default)
    {
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = points[i].Rotate(angleDegree, pivot);
        }
    }

    public static Point Invert(this Point point) => new(-point.X, -point.Y);

    public static PointF Invert(this PointF point) => new(-point.X, -point.Y);

    public static Point OffsetBy(this Point point, int value)=> new(point.X + value, point.Y + value);
    public static Point OffsetBy(this Point point, int x, int y) => new(point.X + x, point.Y + y);
    public static Point OffsetBy(this Point point, Point other) => new(point.X + other.X, point.Y + other.Y);


    public static Point Half(this Point point) => new(point.X / 2, point.Y / 2);
    public static PointF Half(this PointF point) => new(point.X / 2f, point.Y / 2f);
    public static Point ToPoint(this PointF point, MidpointRounding midpointRounding = MidpointRounding.AwayFromZero) => new((int) Math.Round(point.X, midpointRounding), (int) Math.Round(point.Y, midpointRounding));



    public static Size ToSize(this Point point) => new(point.X, point.Y);

    public static SizeF ToSize(this PointF point) => new(point.X, point.Y);

        

}