/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using Avalonia.Media.Imaging;
using Avalonia.Skia;
using Emgu.CV;
using Emgu.CV.CvEnum;
using SkiaSharp;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;

namespace UVtools.UI;

public sealed class LayerCache : IDisposable
{
    private Layer? _layer;
    private Mat? _image;
    private WriteableBitmap? _bitmap;
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
            if (_image is null)
            {
                Bitmap = null;
                return;
            }
            CvInvoke.CvtColor(_image, ImageBgra, ColorConversion.Gray2Bgra);

            ImageSpan = _image.GetBytePointer();
            ImageBgraSpan = ImageBgra.GetBytePointer();
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

    public Mat ImageBgra { get; } = new();

    public unsafe byte *ImageSpan { get; private set; }
    public unsafe byte *ImageBgraSpan { get; private set; }

    public WriteableBitmap? Bitmap
    {
        get => _bitmap;
        set
        {
            _bitmap?.Dispose();
            _bitmap = value;
        }
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
    /// Clears the cache
    /// </summary>
    public void Clear()
    {
        _image?.Dispose();
        _bitmap?.Dispose();

        _layer = null;
        _image = null;
        _bitmap = null;
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Clear();
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