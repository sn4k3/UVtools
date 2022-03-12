/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Avalonia.Media.Imaging;
using Avalonia.Skia;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using SkiaSharp;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;

namespace UVtools.WPF;

public sealed class LayerCache
{
    private Layer _layer;
    private VectorOfVectorOfPoint _layerContours;
    private int[,] _layerContourHierarchy;
    //private SKCanvas _canvas;
    private WriteableBitmap _bitmap;

    public bool IsCached => _layer is not null;

    public unsafe Layer Layer
    {
        get => _layer;
        set
        {
            //if (ReferenceEquals(_layer, value)) return;
            Clear();
            _layer = value;
            Image = _layer.LayerMat;
            if (Image is null) return;
            ImageBgr = new Mat();
            CvInvoke.CvtColor(Image, ImageBgr, ColorConversion.Gray2Bgr);

            ImageSpan = Image.GetBytePointer();
            ImageBgrSpan = ImageBgr.GetBytePointer();
        }
    }

    public Mat Image { get; private set; }

    public Mat ImageBgr { get; private set; }

    public unsafe byte *ImageSpan { get; private set; }
    public unsafe byte *ImageBgrSpan { get; private set; }

    public WriteableBitmap Bitmap
    {
        get => _bitmap;
        set
        {
            _bitmap = value;
            //_canvas?.Dispose();
            //_canvas = null;
        }
    }

    public SKCanvas Canvas
    {
        get
        {
            using var framebuffer = Bitmap.Lock();
            var info = new SKImageInfo(framebuffer.Size.Width, framebuffer.Size.Height,
                framebuffer.Format.ToSkColorType(), SKAlphaType.Premul);
            return SKSurface.Create(info, framebuffer.Address, framebuffer.RowBytes).Canvas;
        }
    }

    public VectorOfVectorOfPoint LayerContours
    {
        get
        {
            if (_layerContours is null) CacheContours();
            return _layerContours;
        }
        private set => _layerContours = value;
    }

    public int[,] LayerContourHierarchy
    {
        get
        {
            if (_layerContourHierarchy is null) CacheContours();
            return _layerContourHierarchy;
        }
        private set => _layerContourHierarchy = value;
    }

    public void CacheContours(bool refresh = false)
    {
        if(refresh) Clear();
        if (_layerContours is not null) return;
        _layerContours = Image.FindContours(out _layerContourHierarchy, RetrType.Tree);
    }
        

    /// <summary>
    /// Clears the cache
    /// </summary>
    public void Clear()
    {
        _layer = null;
        Image?.Dispose();
        ImageBgr?.Dispose();
        _layerContours?.Dispose();
        _layerContours = null;
        _layerContourHierarchy = null;
    }
}