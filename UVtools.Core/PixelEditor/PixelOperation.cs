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
public abstract partial class PixelOperation : ObservableObject, IEquatable<PixelOperation>
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    public enum PixelOperationType : byte
    {
        Drawing,
        Text,
        Fill,
        Supports,
        DrainHole,
    }

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    [ObservableProperty]
    public partial bool ProfileIsDefault { get; set; }

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
    [ObservableProperty]
    public partial LineType LineType { get; set; } = LineType.AntiAlias;

    public LineType[] LineTypes =>
    [
        LineType.FourConnected,
        LineType.EightConnected,
        LineType.AntiAlias
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PixelBrightnessPercent))]
    public partial byte PixelBrightness { get; set; } = byte.MaxValue;

    public decimal PixelBrightnessPercent => Math.Round(PixelBrightness * 100M / 255M, 2, MidpointRounding.AwayFromZero);

    [ObservableProperty]
    public partial uint LayersBelow { get; set; }

    [ObservableProperty]
    public partial uint LayersAbove { get; set; }

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
        if (pixelBrightness > -1) PixelBrightness = (byte) pixelBrightness;
    }

    public virtual void CopyTo(PixelOperation operation)
    {
        operation.LineType = LineType;
        operation.PixelBrightness = PixelBrightness;
        operation.LayersBelow = LayersBelow;
        operation.LayersAbove = LayersAbove;

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
        return LineType == other.LineType && PixelBrightness == other.PixelBrightness && LayersBelow == other.LayersBelow && LayersAbove == other.LayersAbove && OperationType == other.OperationType && LayerIndex == other.LayerIndex && Location.Equals(other.Location) && Size.Equals(other.Size);
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
