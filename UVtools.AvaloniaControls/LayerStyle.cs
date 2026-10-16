/*
 *                               The MIT License (MIT)
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so.
 */

using System;
using Avalonia.Media;

namespace UVtools.AvaloniaControls;

/// <summary>
/// How an image is drawn over what is under it. The default value, <see cref="HasStyle"/> being <see langword="false"/>,
/// is the plain draw of the main image.
/// </summary>
internal readonly struct LayerStyle : IEquatable<LayerStyle>
{
    public LayerStyle(Color tint, double opacity, ImageBoxCompositeBlend blend, bool alphaFromLuminance)
    {
        HasStyle = true;
        Tint = tint.ToUInt32();
        Opacity = opacity;
        Blend = blend;
        AlphaFromLuminance = alphaFromLuminance;
    }

    /// <summary>Gets if the image is drawn with a style, otherwise it is drawn as the main image.</summary>
    public bool HasStyle { get; }

    /// <summary>Gets the tint as <c>0xAARRGGBB</c>.</summary>
    public uint Tint { get; }

    /// <summary>Gets the opacity, from 0 to 1.</summary>
    public double Opacity { get; }

    /// <summary>Gets how the image is blended with what is under it.</summary>
    public ImageBoxCompositeBlend Blend { get; }

    /// <summary>Gets if the luminance of the pixels is the alpha of the image.</summary>
    public bool AlphaFromLuminance { get; }

    public bool Equals(LayerStyle other)
    {
        return HasStyle == other.HasStyle && Tint == other.Tint && Opacity.Equals(other.Opacity)
               && Blend == other.Blend && AlphaFromLuminance == other.AlphaFromLuminance;
    }

    public override bool Equals(object? obj) => obj is LayerStyle other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(HasStyle, Tint, Opacity, Blend, AlphaFromLuminance);
}
