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
using System.Linq;
using System.Xml.Serialization;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;

namespace UVtools.Core.PixelEditor;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
public partial class PixelStroke : PixelOperation, IEquatable<PixelStroke>
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    public override PixelOperationType OperationType => PixelOperationType.Stroke;

    [XmlIgnore]
    public List<Point> Points { get; } = [];

    [XmlIgnore]
    public bool IsEmpty => Points.Count == 0;

    public PixelDrawing.BrushShapeType BrushShape { get; set; } = PixelDrawing.BrushShapeType.Square;

    public double RotationAngle { get; set; }

    public ushort BrushSize { get; set; } = 1;

    public short Thickness { get; set; } = -1;

    public byte RemovePixelBrightness { get; set; }

    [XmlIgnore]
    public bool IsAdd { get; set; } = true;

    public byte Brightness => IsAdd ? PixelBrightness : RemovePixelBrightness;

    public PixelStroke()
    {
    }

    public PixelStroke(uint layerIndex, IEnumerable<Point> points, LineType lineType, PixelDrawing.BrushShapeType brushShape,
        double rotationAngle, ushort brushSize, short thickness, byte removePixelBrightness, byte pixelBrightness,
        bool isAdd) : base(layerIndex, Point.Empty, lineType, pixelBrightness)
    {
        BrushShape = brushShape;
        RotationAngle = rotationAngle;
        BrushSize = brushSize;
        Thickness = thickness;
        RemovePixelBrightness = removePixelBrightness;
        IsAdd = isAdd;
        foreach (var point in points)
        {
            AddPoint(point);
        }

        Location = Points.Count > 0 ? Points[0] : Point.Empty;
        if (Points.Count > 0)
        {
            Size = new Size(GetBounds().Width + 1, GetBounds().Height + 1);
        }
    }

    /// <summary>
    /// Adds a point to the stroke, skipping it if it equals the last stored point (time-window dedup).
    /// </summary>
    /// <param name="point">The image-space point.</param>
    /// <returns>True if the point was appended, false if it was a duplicate of the last stored point.</returns>
    public bool AddPoint(Point point)
    {
        if (Points.Count > 0 && Points[^1] == point) return false;
        Points.Add(point);
        return true;
    }

    /// <summary>
    /// Gets the bounding rectangle of the stored points.
    /// </summary>
    public Rectangle GetBounds()
    {
        if (IsEmpty) return Rectangle.Empty;
        var min = new Point(int.MaxValue, int.MaxValue);
        var max = new Point(int.MinValue, int.MinValue);
        foreach (var point in Points)
        {
            if (point.X < min.X) min.X = point.X;
            if (point.Y < min.Y) min.Y = point.Y;
            if (point.X > max.X) max.X = point.X;
            if (point.Y > max.Y) max.Y = point.Y;
        }

        return new Rectangle(min, new Size(max.X - min.X, max.Y - min.Y));
    }

    public override void CopyTo(PixelOperation operation)
    {
        base.CopyTo(operation);
        if (operation is not PixelStroke stroke) throw new TypeAccessException($"Expecting PixelStroke but got {operation.GetType().Name}");
        stroke.Points.Clear();
        stroke.Points.AddRange(Points);
        stroke.BrushShape = BrushShape;
        stroke.RotationAngle = RotationAngle;
        stroke.BrushSize = BrushSize;
        stroke.Thickness = Thickness;
        stroke.RemovePixelBrightness = RemovePixelBrightness;
        stroke.IsAdd = IsAdd;
    }

    public override string ToString()
    {
        return $"{LineType} {BrushShape}, {BrushSize}px/{Thickness}px, {RotationAngle}º, {Points.Count} points, {PixelBrightness}☼/{RemovePixelBrightness}☼, Layers: {LayersBelow}/{LayersAbove}";
    }

    public bool Equals(PixelStroke? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return base.Equals(other) && BrushShape == other.BrushShape && BrushSize == other.BrushSize &&
               Thickness == other.Thickness && RemovePixelBrightness == other.RemovePixelBrightness &&
               RotationAngle.Equals(other.RotationAngle) && IsAdd == other.IsAdd &&
               Points.Count == other.Points.Count && Points.SequenceEqual(other.Points);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((PixelStroke)obj);
    }

    public static bool operator ==(PixelStroke? left, PixelStroke? right) => Equals(left, right);

    public static bool operator !=(PixelStroke? left, PixelStroke? right) => !Equals(left, right);
}