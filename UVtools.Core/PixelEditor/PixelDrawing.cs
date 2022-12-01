/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Emgu.CV.CvEnum;
using System;
using System.Drawing;

namespace UVtools.Core.PixelEditor;

public class PixelDrawing : PixelOperation
{
    private BrushShapeType _brushShape = BrushShapeType.Square;
    private ushort _brushSize = 1;
    private short _thickness = -1;
    private byte _removePixelBrightness;
    private double _rotationAngle;
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

    public BrushShapeType BrushShape
    {
        get => _brushShape;
        set
        {
            if (!RaiseAndSetIfChanged(ref _brushShape, value)) return;
            if (_brushShape == BrushShapeType.Circle)
            {
                BrushSize = Math.Max(MinCircleBrush, BrushSize);
            }
        }
    }

    public double RotationAngle
    {
        get => _rotationAngle;
        set => RaiseAndSetIfChanged(ref _rotationAngle, Math.Round(value, 2));
    }

    public ushort BrushSize
    {
        get => _brushSize;
        set => RaiseAndSetIfChanged(ref _brushSize, value);
    }

    public short Thickness
    {
        get => _thickness;
        set => RaiseAndSetIfChanged(ref _thickness, value);
    }

    public byte RemovePixelBrightness
    {
        get => _removePixelBrightness;
        set
        {
            if (!RaiseAndSetIfChanged(ref _removePixelBrightness, value)) return;
            RaisePropertyChanged(nameof(RemovePixelBrightnessPercent));
        }
    }

    public decimal RemovePixelBrightnessPercent => Math.Round(_removePixelBrightness * 100M / 255M, 2);

    public bool IsAdd { get; }

    public byte Brightness => IsAdd ? _pixelBrightness : _removePixelBrightness;

    public Rectangle Rectangle { get; }

    public PixelDrawing() 
    {

    }

    public PixelDrawing(uint layerIndex, Point location, LineType lineType, BrushShapeType brushShape, double rotationAngle, ushort brushSize, short thickness, byte removePixelBrightness, byte pixelBrightness, bool isAdd) : base(layerIndex, location, lineType, pixelBrightness)
    {
        _brushShape = brushShape;
        _rotationAngle = rotationAngle;
        _brushSize = brushSize;
        _thickness = thickness;
        _removePixelBrightness = removePixelBrightness;
        IsAdd = isAdd;

        int shiftPos = brushSize / 2;
        Rectangle = new Rectangle(Math.Max(0, location.X - shiftPos), Math.Max(0, location.Y - shiftPos), brushSize-1, brushSize-1);
        Size = new Size(BrushSize, BrushSize);
    }

        
}