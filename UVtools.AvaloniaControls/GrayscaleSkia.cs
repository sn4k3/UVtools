/*
 *                               The MIT License (MIT)
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so.
 */

using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;

namespace UVtools.AvaloniaControls;

/// <summary>
/// Draws a <see cref="GrayscaleImage"/> with Skia. It is the only type of the assembly that uses SkiaSharp and
/// Avalonia.Skia, they are not required to load the rest of the controls: the references are private to this
/// package, so a missing or incompatible Skia only makes <see cref="GrayscaleImage"/> use its fallback.
/// </summary>
internal static class GrayscaleSkia
{
    /// <summary>
    /// Records the custom draw operation which draws the image with Skia.
    /// </summary>
    /// <remarks>Not inlined, so a failure to load Skia is thrown here and not while compiling the caller.</remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Draw(
        GrayscaleImage image,
        DrawingContext context,
        Rect source,
        Rect dest,
        GrayscaleSampling sampling,
        Action? onSkiaUnavailable,
        in LayerStyle style
    )
    {
        context.Custom(new Operation(image, source, dest, sampling, onSkiaUnavailable, style));
    }

    /// <summary>
    /// The Skia state of an image: the raster image wrapping the pixels, recreated when the pixels change, and the
    /// color filter of the color map. Disposed with the image.
    /// </summary>
    private sealed class Cache : IDisposable
    {
        public SKColorFilter? ColorFilter;
        public SKImage? Image;

        // The color map combined with the tint of the last styled draw, drawing the same image with another style
        // creates it again
        public SKColorFilter? StyleFilter;
        public LayerStyle StyleKey;
        public int StyleVersion = -1;
        public int Version = -1;

        public void Dispose()
        {
            Image?.Dispose();
            ColorFilter?.Dispose();
            StyleFilter?.Dispose();
            Image = null;
            ColorFilter = null;
            StyleFilter = null;
            StyleVersion = -1;
        }
    }

    private sealed unsafe class Operation(
        GrayscaleImage image,
        Rect source,
        Rect dest,
        GrayscaleSampling sampling,
        Action? onSkiaUnavailable,
        LayerStyle style
    ) : ICustomDrawOperation
    {
        public Rect Bounds => dest;

        public bool HitTest(Point p)
        {
            return dest.Contains(p);
        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return false;
        }

        public void Dispose() { }

        public void Render(ImmediateDrawingContext context)
        {
            var feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (feature is null)
            {
                // Not the Skia backend, the control draws the fallback copy instead
                image.MarkSkiaUnavailable();
                if (onSkiaUnavailable is not null)
                    Dispatcher.UIThread.Post(onSkiaUnavailable);
                return;
            }

            if (!image.TryEnterRead(out var pixels, out var version, out var colorMap))
                return;
            try
            {
                var cache = image.SkiaCache as Cache;
                if (cache is null)
                {
                    cache = new Cache();
                    image.SkiaCache = cache;
                }

                if (cache.Version != version || cache.Image is null)
                {
                    // The pixels changed, the Skia image and its mipmaps are tied to the previous content
                    cache.Dispose();
                    cache.Image = SKImage.FromPixels(
                        new SKImageInfo(
                            image.Width,
                            image.Height,
                            SKColorType.Gray8,
                            SKAlphaType.Opaque
                        ),
                        (IntPtr)pixels,
                        image.Width
                    );
                    cache.ColorFilter = colorMap is null ? null : CreateColorFilter(colorMap);
                    cache.Version = version;
                }

                using var lease = feature.Lease();
                var canvas = lease.SkCanvas;

                // Anti-aliased like Avalonia draws its bitmaps, which makes the edges of a fractional destination
                // match the ones of a 32-bit bitmap
                using var paint = new SKPaint
                {
                    IsAntialias = true,
                    ColorFilter = cache.ColorFilter,
                };
                if (style.HasStyle)
                {
                    if (cache.StyleVersion != version || !cache.StyleKey.Equals(style))
                    {
                        cache.StyleFilter?.Dispose();
                        cache.StyleFilter = CreateStyleFilter(cache.ColorFilter, style);
                        cache.StyleKey = style;
                        cache.StyleVersion = version;
                    }

                    paint.ColorFilter = cache.StyleFilter ?? cache.ColorFilter;
                    paint.BlendMode = style.Blend switch
                    {
                        ImageBoxCompositeBlend.Plus => SKBlendMode.Plus,
                        ImageBoxCompositeBlend.Screen => SKBlendMode.Screen,
                        ImageBoxCompositeBlend.Multiply => SKBlendMode.Multiply,
                        ImageBoxCompositeBlend.Lighten => SKBlendMode.Lighten,
                        ImageBoxCompositeBlend.Darken => SKBlendMode.Darken,
                        _ => SKBlendMode.SrcOver,
                    };
                }

                var options = sampling switch
                {
                    GrayscaleSampling.Linear => new SKSamplingOptions(
                        SKFilterMode.Linear,
                        SKMipmapMode.None
                    ),
                    GrayscaleSampling.LinearMipmap => new SKSamplingOptions(
                        SKFilterMode.Linear,
                        SKMipmapMode.Linear
                    ),
                    _ => SKSamplingOptions.Default,
                };

                canvas.DrawImage(
                    cache.Image,
                    new SKRect(
                        (float)source.Left,
                        (float)source.Top,
                        (float)source.Right,
                        (float)source.Bottom
                    ),
                    new SKRect(
                        (float)dest.Left,
                        (float)dest.Top,
                        (float)dest.Right,
                        (float)dest.Bottom
                    ),
                    options,
                    paint
                );
            }
            finally
            {
                image.ExitRead();
            }
        }

        /// <summary>
        /// Creates the color filter of a styled draw: the color map, then the tint. The opacity is in the alpha of the
        /// tint, because the alpha of the paint is applied before the filters.
        /// </summary>
        /// <returns>A new filter which includes the color map, or <see langword="null"/> when the style adds nothing
        /// to the color map.</returns>
        private static SKColorFilter? CreateStyleFilter(
            SKColorFilter? colorMapFilter,
            in LayerStyle style
        )
        {
            var tint = style.Tint;
            var alpha = (byte)Math.Clamp((int)Math.Round((tint >> 24) * style.Opacity), 0, 255);
            var tintColor = new SKColor((byte)(tint >> 16), (byte)(tint >> 8), (byte)tint, alpha);

            // Luminance: the color is the tint and the alpha the luminance of the pixel (the color map applied).
            // Otherwise the pixel colors are multiplied by the tint
            SKColorFilter? stage;
            if (style.AlphaFromLuminance)
            {
                // One matrix: the color is the tint (the bias, 0..1), the alpha the luminance times the opacity
                var opacity = alpha / 255f;
                stage = SKColorFilter.CreateColorMatrix([
                    0,
                    0,
                    0,
                    0,
                    tintColor.Red / 255f,
                    0,
                    0,
                    0,
                    0,
                    tintColor.Green / 255f,
                    0,
                    0,
                    0,
                    0,
                    tintColor.Blue / 255f,
                    0.2126f * opacity,
                    0.7152f * opacity,
                    0.0722f * opacity,
                    0,
                    0,
                ]);
            }
            else if (tintColor == SKColors.White)
            {
                stage = null;
            }
            else
            {
                stage = SKColorFilter.CreateBlendMode(tintColor, SKBlendMode.Modulate);
            }

            if (stage is null || colorMapFilter is null)
                return stage;

            using (stage)
            {
                return SKColorFilter.CreateCompose(stage, colorMapFilter);
            }
        }

        private static SKColorFilter CreateColorFilter(uint[] colorMap)
        {
            // The image is gray, so the red, green and blue channels have the same value: a table for each channel
            // maps it to the channels of the color
            var alpha = new byte[256];
            var red = new byte[256];
            var green = new byte[256];
            var blue = new byte[256];
            for (var i = 0; i < 256; i++)
            {
                var color = colorMap[i];
                alpha[i] = (byte)i; // Identity, the image is opaque
                red[i] = (byte)(color >> 16);
                green[i] = (byte)(color >> 8);
                blue[i] = (byte)color;
            }

            return SKColorFilter.CreateTable(alpha, red, green, blue);
        }
    }
}
