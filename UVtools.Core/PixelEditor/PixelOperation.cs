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
using UVtools.Core.Objects;

namespace UVtools.Core.PixelEditor;

public abstract class PixelOperation : BindableBase
{
    private protected uint _index;
    private protected LineType _lineType = LineType.AntiAlias;
    private protected byte _pixelBrightness = 255;
    private protected uint _layersBelow;
    private protected uint _layersAbove;

    public enum PixelOperationType : byte
    {
        Drawing,
        Text,
        Eraser,
        Supports,
        DrainHole,
    }

    /// <summary>
    /// Gets or sets the index number to show on GUI
    /// </summary>
    public uint Index
    {
        get => _index;
        set => RaiseAndSetIfChanged(ref _index, value);
    }

    /// <summary>
    /// Gets the <see cref="PixelOperationType"/>
    /// </summary>
    public abstract PixelOperationType OperationType { get; }

    /// <summary>
    /// Gets the layer index
    /// </summary>
    public uint LayerIndex { get; }

    /// <summary>
    /// Gets the location of the operation
    /// </summary>
    public Point Location { get; }

    /// <summary>
    /// Gets the <see cref="LineType"/> for the draw operation
    /// </summary>
    public LineType LineType
    {
        get => _lineType;
        set => RaiseAndSetIfChanged(ref _lineType, value);
    }

    public LineType[] LineTypes => new[]
    {
        LineType.FourConnected,
        LineType.EightConnected,
        LineType.AntiAlias
    };

    public byte PixelBrightness
    {
        get => _pixelBrightness;
        set
        {
            if(!RaiseAndSetIfChanged(ref _pixelBrightness, value)) return;
            RaisePropertyChanged(nameof(PixelBrightnessPercent));
        }
    }

    public decimal PixelBrightnessPercent => Math.Round(_pixelBrightness * 100M / 255M, 2);

    public uint LayersBelow
    {
        get => _layersBelow;
        set => RaiseAndSetIfChanged(ref _layersBelow, value);
    }

    public uint LayersAbove
    {
        get => _layersAbove;
        set => RaiseAndSetIfChanged(ref _layersAbove, value);
    }

    /// <summary>
    /// Gets the total size of the operation
    /// </summary>
    public Size Size { get; private protected set; } = Size.Empty;

    protected PixelOperation() { }

    protected PixelOperation(uint layerIndex, Point location, LineType lineType = LineType.AntiAlias, int pixelBrightness = -1)
    {
        Location = location;
        LayerIndex = layerIndex;
        LineType = lineType;
        if (pixelBrightness > -1)
            _pixelBrightness = (byte) pixelBrightness;
    }

    public PixelOperation Clone()
    {
        return (PixelOperation) MemberwiseClone();
    }
}