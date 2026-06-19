/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV.CvEnum;
using System;
using System.Drawing;
using System.Xml.Serialization;

namespace UVtools.Core.PixelEditor;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
public partial class PixelDrawing : PixelOperation, IEquatable<PixelDrawing>
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    public const byte MinRectangleBrush = 1;
    public const byte MinCircleBrush = 7;
    public enum BrushShapeType : byte
    {
        //Mask = 0,
        Line = 1,
        Triangle = 3,
        Square = 4,
        Pentagon = 5,
        Hexagon = 6,
        Heptagon = 7,
        Octagon = 8,
        Nonagon = 9,
        Decagon = 10,
        Hendecagon = 11,
        Dodecagon = 12,
        Circle = 100,
    }

    public override PixelOperationType OperationType => PixelOperationType.Drawing;

    [ObservableProperty]
    public partial BrushShapeType BrushShape { get; set; } = BrushShapeType.Square;

    partial void OnBrushShapeChanged(BrushShapeType value)
    {
        if (value == BrushShapeType.Circle)
        {
            BrushSize = Math.Max(MinCircleBrush, BrushSize);
        }
    }

    public double RotationAngle
    {
        get;
        set => SetProperty(ref field, Math.Round(value, 2));
    }

    [ObservableProperty]
    public partial ushort BrushSize { get; set; } = 1;

    [ObservableProperty]
    public partial short Thickness { get; set; } = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemovePixelBrightnessPercent))]
    public partial byte RemovePixelBrightness { get; set; }

    public decimal RemovePixelBrightnessPercent => Math.Round(RemovePixelBrightness * 100M / 255M, 2);

    [XmlIgnore]
    public bool IsAdd { get; private set; }

    public byte Brightness => IsAdd ? PixelBrightness : RemovePixelBrightness;

    [XmlIgnore]
    public Rectangle Rectangle { get; private set; }

    public PixelDrawing() 
    {

    }

    public PixelDrawing(uint layerIndex, Point location, LineType lineType, BrushShapeType brushShape, double rotationAngle, ushort brushSize, short thickness, byte removePixelBrightness, byte pixelBrightness, bool isAdd) : base(layerIndex, location, lineType, pixelBrightness)
    {
        BrushShape = brushShape;
        RotationAngle = rotationAngle;
        BrushSize = brushSize;
        Thickness = thickness;
        RemovePixelBrightness = removePixelBrightness;
        IsAdd = isAdd;

        int shiftPos = brushSize / 2;
        Rectangle = new Rectangle(Math.Max(0, location.X - shiftPos), Math.Max(0, location.Y - shiftPos), brushSize-1, brushSize-1);
        Size = new Size(BrushSize, BrushSize);
    }

    public override void CopyTo(PixelOperation operation)
    {
        base.CopyTo(operation);
        if (operation is not PixelDrawing drawing) throw new TypeAccessException($"Expecting PixelDrawing but got {operation.GetType().Name}");
        drawing.BrushShape = BrushShape;
        drawing.RotationAngle = RotationAngle;
        drawing.BrushSize = BrushSize;
        drawing.Thickness = Thickness;
        drawing.RemovePixelBrightness = RemovePixelBrightness;
        drawing.IsAdd = IsAdd;
        drawing.Rectangle = Rectangle;
    }

    public override string ToString()
    {
        return $"{LineType} {BrushShape}, {BrushSize}px/{Thickness}px, {RotationAngle}º, {PixelBrightness}☼/{RemovePixelBrightness}☼, Layers: {LayersBelow}/{LayersAbove}";
    }


    public bool Equals(PixelDrawing? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return base.Equals(other) && BrushShape == other.BrushShape && BrushSize == other.BrushSize && Thickness == other.Thickness && RemovePixelBrightness == other.RemovePixelBrightness && RotationAngle.Equals(other.RotationAngle) && IsAdd == other.IsAdd && Rectangle.Equals(other.Rectangle);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((PixelDrawing)obj);
    }

    public static bool operator ==(PixelDrawing? left, PixelDrawing? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(PixelDrawing? left, PixelDrawing? right)
    {
        return !Equals(left, right);
    }
}
