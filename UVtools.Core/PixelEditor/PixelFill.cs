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
using System.Xml.Serialization;

namespace UVtools.Core.PixelEditor;

public class PixelFill : PixelOperation
{
    public const byte Diameter = 4;

    private byte _erasePixelBrightness;


    public override PixelOperationType OperationType => PixelOperationType.Fill;

    public byte ErasePixelBrightness
    {
        get => _erasePixelBrightness;
        set
        {
            if (!RaiseAndSetIfChanged(ref _erasePixelBrightness, value)) return;
            RaisePropertyChanged(nameof(ErasePixelBrightnessPercent));
        }
    }

    public decimal ErasePixelBrightnessPercent => Math.Round(_erasePixelBrightness * 100M / 255M, 2);

    [XmlIgnore]
    public bool IsAdd { get; private set; }

    public byte Brightness => IsAdd ? _pixelBrightness : _erasePixelBrightness;

    public PixelFill()
    {
    }

    public PixelFill(uint layerIndex, Point location, byte erasePixelBrightness, byte pixelBrightness, bool isAdd) : base(layerIndex, location, LineType.AntiAlias, pixelBrightness)
    {
        Size = new Size(Diameter, Diameter);
        _erasePixelBrightness = erasePixelBrightness;
        IsAdd = isAdd;
    }

    public override void CopyTo(PixelOperation operation)
    {
        base.CopyTo(operation);
        if (operation is not PixelFill drawing) throw new TypeAccessException($"Expecting PixelFill but got {operation.GetType().Name}");
        drawing.ErasePixelBrightness = _erasePixelBrightness;
        drawing.IsAdd = IsAdd;
    }

    public override string ToString()
    {
        return $"{_pixelBrightness}☼/{_erasePixelBrightness}☼, Layers: {_layersBelow}/{_layersAbove}";
    }
}