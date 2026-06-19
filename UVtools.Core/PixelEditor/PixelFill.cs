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

public partial class PixelFill : PixelOperation
{
    public const byte Diameter = 4;

    public override PixelOperationType OperationType => PixelOperationType.Fill;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ErasePixelBrightnessPercent))]
    public partial byte ErasePixelBrightness { get; set; }

    public decimal ErasePixelBrightnessPercent => Math.Round(ErasePixelBrightness * 100M / 255M, 2);

    [XmlIgnore]
    public bool IsAdd { get; private set; }

    public byte Brightness => IsAdd ? PixelBrightness : ErasePixelBrightness;

    public PixelFill()
    {
    }

    public PixelFill(uint layerIndex, Point location, byte erasePixelBrightness, byte pixelBrightness, bool isAdd) : base(layerIndex, location, LineType.AntiAlias, pixelBrightness)
    {
        Size = new Size(Diameter, Diameter);
        ErasePixelBrightness = erasePixelBrightness;
        IsAdd = isAdd;
    }

    public override void CopyTo(PixelOperation operation)
    {
        base.CopyTo(operation);
        if (operation is not PixelFill drawing) throw new TypeAccessException($"Expecting PixelFill but got {operation.GetType().Name}");
        drawing.ErasePixelBrightness = ErasePixelBrightness;
        drawing.IsAdd = IsAdd;
    }

    public override string ToString()
    {
        return $"{PixelBrightness}☼/{ErasePixelBrightness}☼, Layers: {LayersBelow}/{LayersAbove}";
    }
}
