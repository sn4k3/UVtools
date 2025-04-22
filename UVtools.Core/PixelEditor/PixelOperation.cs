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
using UVtools.Core.Objects;

namespace UVtools.Core.PixelEditor;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
public abstract class PixelOperation : BindableBase, IEquatable<PixelOperation>
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    private string? _profileName;
    private bool _profileIsDefault;

    private protected LineType _lineType = LineType.AntiAlias;
    private protected byte _pixelBrightness = 255;
    private protected uint _layersBelow;
    private protected uint _layersAbove;

    public enum PixelOperationType : byte
    {
        Drawing,
        Text,
        Fill,
        Supports,
        DrainHole,
    }

    public string? ProfileName
    {
        get => _profileName;
        set => RaiseAndSetIfChanged(ref _profileName, value);
    }

    public bool ProfileIsDefault
    {
        get => _profileIsDefault;
        set => RaiseAndSetIfChanged(ref _profileIsDefault, value);
    }

    /// <summary>
    /// Gets the <see cref="PixelOperationType"/>
    /// </summary>
    public abstract PixelOperationType OperationType { get; }

    /// <summary>
    /// Gets the layer index
    /// </summary>
    [XmlIgnore]
    public uint LayerIndex { get; private set; }

    /// <summary>
    /// Gets the location of the operation
    /// </summary>
    [XmlIgnore]
    public Point Location { get; private set; }

    /// <summary>
    /// Gets the <see cref="LineType"/> for the draw operation
    /// </summary>
    public LineType LineType
    {
        get => _lineType;
        set => RaiseAndSetIfChanged(ref _lineType, value);
    }

    public LineType[] LineTypes =>
    [
        LineType.FourConnected,
        LineType.EightConnected,
        LineType.AntiAlias
    ];

    public byte PixelBrightness
    {
        get => _pixelBrightness;
        set
        {
            if(!RaiseAndSetIfChanged(ref _pixelBrightness, value)) return;
            RaisePropertyChanged(nameof(PixelBrightnessPercent));
        }
    }

    public decimal PixelBrightnessPercent => Math.Round(_pixelBrightness * 100M / 255M, 2, MidpointRounding.AwayFromZero);

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
    [XmlIgnore]
    public Size Size { get; private protected set; } = Size.Empty;

    protected PixelOperation() { }

    protected PixelOperation(uint layerIndex, Point location, LineType lineType = LineType.AntiAlias, int pixelBrightness = -1)
    {
        Location = location;
        LayerIndex = layerIndex;
        LineType = lineType;
        if (pixelBrightness > -1) _pixelBrightness = (byte) pixelBrightness;
    }

    public virtual void CopyTo(PixelOperation operation)
    {
        operation.LineType = _lineType;
        operation.PixelBrightness = _pixelBrightness;
        operation.LayersBelow = _layersBelow;
        operation.LayersAbove = _layersAbove;

        operation.LayerIndex = LayerIndex;
        operation.Location = Location;
        operation.Size = Size;
    }

    public PixelOperation Clone()
    {
        return (PixelOperation) MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{nameof(OperationType)}: {OperationType}, {nameof(LineType)}: {LineType}, {nameof(PixelBrightness)}: {PixelBrightness}, {nameof(LayersBelow)}: {LayersBelow}, {nameof(LayersAbove)}: {LayersAbove}";
    }

    public bool Equals(PixelOperation? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _lineType == other._lineType && _pixelBrightness == other._pixelBrightness && _layersBelow == other._layersBelow && _layersAbove == other._layersAbove && OperationType == other.OperationType && LayerIndex == other.LayerIndex && Location.Equals(other.Location) && Size.Equals(other.Size);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((PixelOperation)obj);
    }

    public static bool operator ==(PixelOperation? left, PixelOperation? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(PixelOperation? left, PixelOperation? right)
    {
        return !Equals(left, right);
    }
}