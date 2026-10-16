/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia;
using Emgu.CV;
using Emgu.CV.CvEnum;
using EmguExtensions;
using EmguExtensions.Avalonia;
using SkiaSharp;
using UVtools.AvaloniaControls;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;

namespace UVtools.UI;

public sealed class LayerCache : IDisposable
{
    private Layer? _layer;
    private Mat? _image;
    private WriteableBitmap? _bitmap;
    private Mat _imageBgra = new();
    private bool _imageBgraValid;
    private GrayscaleImage? _grayscaleImage;
    private bool disposedValue;

    public bool IsCached => _layer is not null;

    public unsafe Layer? Layer
    {
        get => _layer;
        set
        {
            //if (ReferenceEquals(_layer, value)) return;
            _layer = value;
            Image = _layer?.LayerMat;
            _imageBgraValid = false;
            if (_image is null)
            {
                Bitmap = null;
                return;
            }

            ImageSpan = _image.BytePointer;
        }
    }

    public Mat? Image
    {
        get => _image;
        private set
        {
            _image?.Dispose();
            _image = value;
        }
    }

    /// <summary>
    /// Gets the layer image as BGRA, which the layer preview draws on. It is created from <see cref="Image"/> the first
    /// time it is needed for each layer, because layers shown as grayscale never need it.
    /// </summary>
    public Mat ImageBgra
    {
        get
        {
            EnsureBgra();
            return _imageBgra;
        }
    }

    public unsafe byte *ImageSpan { get; private set; }

    public unsafe byte *ImageBgraSpan
    {
        get
        {
            EnsureBgra();
            return ImageBgraSpanUnchecked;
        }
    }

    private unsafe byte *ImageBgraSpanUnchecked { get; set; }

    /// <summary>
    /// Gets the number of layers shown in a row as <see cref="GrayscaleImage"/>.
    /// </summary>
    public int GrayscaleStreak { get; set; }

    /// <summary>
    /// Creates <see cref="ImageBgra"/> from <see cref="Image"/> when it was not yet for this layer.
    /// </summary>
    public unsafe void EnsureBgra()
    {
        if (_imageBgraValid || _image is null) return;
        CvInvoke.CvtColor(_image, _imageBgra, ColorConversion.Gray2Bgra);
        ImageBgraSpanUnchecked = _imageBgra.BytePointer;
        _imageBgraValid = true;
    }

    /// <summary>
    /// Sets <see cref="ImageBgra"/> to the BGRA conversion of another grayscale image of the layer size.
    /// </summary>
    public unsafe void SetBgraFrom(Mat grayscale)
    {
        CvInvoke.CvtColor(grayscale, _imageBgra, ColorConversion.Gray2Bgra);
        ImageBgraSpanUnchecked = _imageBgra.BytePointer;
        _imageBgraValid = true;
    }

    /// <summary>
    /// Gets the 8-bit image displayed by the layer preview when the layer has nothing to color, which takes a quarter of
    /// the memory of <see cref="ImageBgra"/> and its bitmap.
    /// </summary>
    public GrayscaleImage? GrayscaleImage => _grayscaleImage;

    /// <summary>
    /// Copies an 8-bit image of the layer size to <see cref="GrayscaleImage"/>, which is only allocated again when the
    /// size of the layer changes.
    /// </summary>
    /// <param name="grayscale">The continuous single channel 8-bit image to display.</param>
    /// <param name="previousImage">
    /// The image that was replaced because the size changed, or <see langword="null"/>. The caller owns it and must
    /// dispose it after nothing displays it anymore.
    /// </param>
    /// <returns>The image with the pixels.</returns>
    public GrayscaleImage UpdateGrayscaleImage(Mat grayscale, out GrayscaleImage? previousImage)
    {
        previousImage = null;
        var image = _grayscaleImage;
        if (image is null || image.Width != grayscale.Width || image.Height != grayscale.Height)
        {
            previousImage = image;
            image = new GrayscaleImage(grayscale.Width, grayscale.Height);
            _grayscaleImage = image;
        }

        image.CopyFrom(grayscale.DataPointer, grayscale.Step);
        return image;
    }

    /// <summary>
    /// Frees <see cref="ImageBgra"/> and <see cref="Bitmap"/>, which are about 8 times the size of the layer in bytes
    /// and are created again when needed. The bitmap must not be displayed.
    /// </summary>
    public unsafe void ReleaseBgra()
    {
        Bitmap = null;
        _imageBgra.Dispose();
        _imageBgra = new Mat();
        _imageBgraValid = false;
        ImageBgraSpanUnchecked = null;
    }

    /// <summary>
    /// Gets or sets the bitmap with the pixels of <see cref="ImageBgra"/>, setting it disposes the previous one.
    /// </summary>
    public WriteableBitmap? Bitmap
    {
        get => _bitmap;
        set
        {
            if (ReferenceEquals(_bitmap, value)) return;
            _bitmap?.Dispose();
            _bitmap = value;
        }
    }

    /// <summary>
    /// Copies the pixels of <see cref="ImageBgra"/> to <see cref="Bitmap"/>, the same bitmap is written again for as
    /// long as the size of the image doesn't change, which avoids allocating a full resolution bitmap for each layer.
    /// </summary>
    /// <param name="previousBitmap">
    /// The bitmap that was replaced because the size of the image changed, or <see langword="null"/> when the bitmap
    /// was reused. The caller owns it and must dispose it after nothing displays it anymore.
    /// </param>
    /// <returns>The bitmap with the pixels, a new instance when <paramref name="previousBitmap"/> is not null.</returns>
    public WriteableBitmap UpdateBitmap(out WriteableBitmap? previousBitmap)
    {
        EnsureBgra();
        previousBitmap = null;
        var bitmap = _bitmap;

        if (bitmap is null
            || bitmap.PixelSize.Width != _imageBgra.Width
            || bitmap.PixelSize.Height != _imageBgra.Height
            || bitmap.Format != PixelFormat.Bgra8888
            || bitmap.AlphaFormat != AlphaFormat.Unpremul)
        {
            previousBitmap = bitmap;
            // Same bitmap format as ToBitmap() of EmguExtensions.Avalonia
            bitmap = new WriteableBitmap(new PixelSize(_imageBgra.Width, _imageBgra.Height), new Vector(96, 96),
                PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            _bitmap = bitmap;
        }

        using var framebuffer = bitmap.Lock();
        using var target = framebuffer.ToMat(); // Zero-copy view of the framebuffer, honors its stride
        _imageBgra.CopyTo(target);

        return bitmap;
    }

    public SKCanvas? Canvas
    {
        get
        {
            if (_bitmap is null) return null;
            using var framebuffer = _bitmap.Lock();
            var info = new SKImageInfo(framebuffer.Size.Width, framebuffer.Size.Height,
                framebuffer.Format.ToSkColorType(), SKAlphaType.Premul);
            return SKSurface.Create(info, framebuffer.Address, framebuffer.RowBytes).Canvas;
        }
    }

    /// <summary>
    /// Executes drawing operations on the backing bitmap's Skia surface with proper locking and cleanup.
    /// </summary>
    public void DrawOnCanvas(Action<SKCanvas> drawAction)
    {
        if (_bitmap is null) return;
        using var framebuffer = _bitmap.Lock();
        var info = new SKImageInfo(framebuffer.Size.Width, framebuffer.Size.Height,
            framebuffer.Format.ToSkColorType(), SKAlphaType.Premul);
        using var surface = SKSurface.Create(info, framebuffer.Address, framebuffer.RowBytes);
        if (surface is null) return;
        drawAction(surface.Canvas);
        surface.Canvas.Flush();
    }

    /// <summary>
    /// Clears the cache
    /// </summary>
    public unsafe void Clear()
    {
        _image?.Dispose();
        _bitmap?.Dispose();

        _grayscaleImage?.Dispose();

        _layer = null;
        _image = null;
        _bitmap = null;
        _grayscaleImage = null;
        _imageBgraValid = false;
        GrayscaleStreak = 0;
        ImageSpan = null;
        ImageBgraSpanUnchecked = null;
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Clear();
                _imageBgra.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
