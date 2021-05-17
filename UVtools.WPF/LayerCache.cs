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
using Emgu.CV.Util;
using SkiaSharp;
using UVtools.Core;
using UVtools.Core.Extensions;

namespace UVtools.WPF
{
    public sealed class LayerCache
    {
        private Layer _layer;
        private Array _layerHierarchyJagged;
        private VectorOfVectorOfPoint _layerContours;
        private Mat _layerHierarchy;
        //private SKCanvas _canvas;
        private WriteableBitmap _bitmap;

        public bool IsCached => !ReferenceEquals(_layer, null);

        public unsafe Layer Layer
        {
            get => _layer;
            set
            {
                //if (ReferenceEquals(_layer, value)) return;
                Clear();
                _layer = value;
                Image = _layer.LayerMat;
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

        public Mat LayerHierarchy
        {
            get
            {
                if (_layerHierarchy is null) CacheContours();
                return _layerHierarchy;
            }
            private set => _layerHierarchy = value;
        }

        public Array LayerHierarchyJagged
        {
            get
            {
                if(_layerHierarchyJagged is null) CacheContours();
                return _layerHierarchyJagged;
            }
            private set => _layerHierarchyJagged = value;
        }

        public void CacheContours(bool refresh = false)
        {
            if(refresh) Clear();
            if (!ReferenceEquals(_layerContours, null)) return;
            _layerContours = new VectorOfVectorOfPoint();
            _layerHierarchy = new Mat();
            CvInvoke.FindContours(Image, _layerContours, _layerHierarchy, RetrType.Ccomp,
                ChainApproxMethod.ChainApproxSimple);
            _layerHierarchyJagged = _layerHierarchy.GetData();
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
            _layerHierarchy?.Dispose();
            _layerHierarchy = null;
            _layerHierarchyJagged = null;
        }
    }
}
