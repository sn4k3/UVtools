/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using UVtools.Core.Layers;

namespace UVtools.Core.Objects;


public sealed class ExposureItem : BindableBase, IComparable<ExposureItem>
{
    private decimal _layerHeight;
    private decimal _bottomExposure;
    private decimal _exposure;
    private byte _brightness = byte.MaxValue;

    /// <summary>
    /// Gets or sets the layer height in millimeters
    /// </summary>
    public decimal LayerHeight
    {
        get => _layerHeight;
        set => RaiseAndSetIfChanged(ref _layerHeight, Layer.RoundHeight(value));
    }


    /// <summary>
    /// Gets or sets the bottom exposure in seconds
    /// </summary>
    public decimal BottomExposure
    {
        get => _bottomExposure;
        set => RaiseAndSetIfChanged(ref _bottomExposure, Math.Round(value, 2));
    }

    /// <summary>
    /// Gets or sets the bottom exposure in seconds
    /// </summary>
    public decimal Exposure
    {
        get => _exposure;
        set => RaiseAndSetIfChanged(ref _exposure, Math.Round(value, 2));
    }

    /// <summary>
    /// Gets or sets the brightness level
    /// </summary>
    public byte Brightness
    {
        get => _brightness;
        set
        {
            if(!RaiseAndSetIfChanged(ref _brightness, value)) return;
            RaisePropertyChanged(nameof(BrightnessPercent));
        }
    }

    public decimal BrightnessPercent => Math.Round(_brightness * 100m / byte.MaxValue, 2);

    public bool IsValid => _layerHeight > 0 && _bottomExposure > 0 && _exposure > 0 && _brightness > 0;

    public ExposureItem() { }

    public ExposureItem(decimal layerHeight, decimal bottomExposure = 0, decimal exposure = 0, byte brightness = 255)
    {
        _layerHeight = Layer.RoundHeight(layerHeight);
        _bottomExposure = Math.Round(bottomExposure, 2);
        _exposure = Math.Round(exposure, 2);
        _brightness = brightness;
    }

    public override string ToString()
    {
        return $"{nameof(LayerHeight)}: {_layerHeight}mm, {nameof(BottomExposure)}: {_bottomExposure}s, {nameof(Exposure)}: {_exposure}s, {nameof(Brightness)}: {_brightness} ({BrightnessPercent} %)";
    }

    private bool Equals(ExposureItem other)
    {
        return _layerHeight == other._layerHeight && _bottomExposure == other._bottomExposure && _exposure == other._exposure && _brightness == other._brightness;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ExposureItem other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_layerHeight, _bottomExposure, _exposure, _brightness);
    }

    public int CompareTo(ExposureItem? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        var layerHeightComparison = _layerHeight.CompareTo(other._layerHeight);
        if (layerHeightComparison != 0) return layerHeightComparison;
        var bottomExposureComparison = _bottomExposure.CompareTo(other._bottomExposure);
        if (bottomExposureComparison != 0) return bottomExposureComparison;
        var exposureComparison = _exposure.CompareTo(other._exposure);
        if (exposureComparison != 0) return exposureComparison;
        return _brightness.CompareTo(other._brightness);
    }

    public ExposureItem Clone()
    {
        return (MemberwiseClone() as ExposureItem)!;
    }
}