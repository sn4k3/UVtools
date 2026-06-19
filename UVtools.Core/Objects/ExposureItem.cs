/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using UVtools.Core.Layers;

namespace UVtools.Core.Objects;


public sealed partial class ExposureItem : ObservableObject, IComparable<ExposureItem>
{
    /// <summary>
    /// Gets or sets the layer height in millimeters
    /// </summary>
    public decimal LayerHeight
    {
        get;
        set => SetProperty(ref field, Layer.RoundHeight(value));
    }


    /// <summary>
    /// Gets or sets the bottom exposure in seconds
    /// </summary>
    public decimal BottomExposure
    {
        get;
        set => SetProperty(ref field, Math.Round(value, 2));
    }

    /// <summary>
    /// Gets or sets the bottom exposure in seconds
    /// </summary>
    public decimal Exposure
    {
        get;
        set => SetProperty(ref field, Math.Round(value, 2));
    }

    /// <summary>
    /// Gets or sets the brightness level
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrightnessPercent))]
    public partial byte Brightness { get; set; } = byte.MaxValue;

    public decimal BrightnessPercent => Math.Round(Brightness * 100m / byte.MaxValue, 2);

    public bool IsValid => LayerHeight > 0 && BottomExposure > 0 && Exposure > 0 && Brightness > 0;

    public ExposureItem() { }

    public ExposureItem(decimal layerHeight, decimal bottomExposure = 0, decimal exposure = 0, byte brightness = 255)
    {
        LayerHeight = layerHeight;
        BottomExposure = bottomExposure;
        Exposure = exposure;
        Brightness = brightness;
    }

    public override string ToString()
    {
        return $"{nameof(LayerHeight)}: {LayerHeight}mm, {nameof(BottomExposure)}: {BottomExposure}s, {nameof(Exposure)}: {Exposure}s, {nameof(Brightness)}: {Brightness} ({BrightnessPercent} %)";
    }

    private bool Equals(ExposureItem other)
    {
        return LayerHeight == other.LayerHeight && BottomExposure == other.BottomExposure && Exposure == other.Exposure && Brightness == other.Brightness;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ExposureItem other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LayerHeight, BottomExposure, Exposure, Brightness);
    }

    public int CompareTo(ExposureItem? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        var layerHeightComparison = LayerHeight.CompareTo(other.LayerHeight);
        if (layerHeightComparison != 0) return layerHeightComparison;
        var bottomExposureComparison = BottomExposure.CompareTo(other.BottomExposure);
        if (bottomExposureComparison != 0) return bottomExposureComparison;
        var exposureComparison = Exposure.CompareTo(other.Exposure);
        if (exposureComparison != 0) return exposureComparison;
        return Brightness.CompareTo(other.Brightness);
    }

    public ExposureItem Clone()
    {
        return (MemberwiseClone() as ExposureItem)!;
    }
}
