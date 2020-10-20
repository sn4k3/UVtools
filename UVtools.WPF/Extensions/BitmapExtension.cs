/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Emgu.CV;
using Emgu.CV.CvEnum;
using SkiaSharp;
using UVtools.Core.Extensions;

namespace UVtools.WPF.Extensions
{
    /// <summary>
    /// Provide extension method to convert IInputArray to and from Bitmap
    /// </summary>
    public static class BitmapExtension
    {
        public static int GetStep(this WriteableBitmap bitmap)
         => (int)bitmap.Size.Width;

        /// <summary>
        /// Gets the total length of this <see cref="WriteableBitmap"/></param>
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns>The total length of this <see cref="WriteableBitmap"/></returns>
        public static int GetLength(this WriteableBitmap bitmap)
         => (int) (bitmap.Size.Width * bitmap.Size.Height);

        public static int GetPixelPos(this WriteableBitmap bitmap, int x, int y)
         => (int)(bitmap.Size.Width * y + x);

        public static int GetPixelPos(this WriteableBitmap bitmap, System.Drawing.Point location) =>
            bitmap.GetPixelPos(location.X, location.Y);

        /// <summary>
        /// Gets a single pixel span to manipulate or read pixels
        /// </summary>
        /// <typeparam name="T">Pixel type</typeparam>
        /// <param name="mat"><see cref="Mat"/> Input</param>
        /// <returns>A <see cref="Span{T}"/> containing all pixels in data memory</returns>
        public static unsafe Span<uint> GetPixelSpan(this WriteableBitmap bitmap)
        {
            using var l = bitmap.Lock();
            return new Span<uint>(l.Address.ToPointer(), bitmap.GetLength());
        }

        
        public static unsafe Span<uint> GetPixelSpan(this WriteableBitmap bitmap, int length, int offset = 0)
        {
            using var l = bitmap.Lock();
            return new Span<uint>(IntPtr.Add(l.Address, offset).ToPointer(), length);
        }

        public static Span<uint> GetSinglePixelSpan(this WriteableBitmap bitmap, int x, int y, int length = 1)
        {
            using var l = bitmap.Lock();
            return bitmap.GetPixelSpan(length, bitmap.GetPixelPos(x, y));
        }

        public static Span<uint> GetSinglePixelPosSpan(this WriteableBitmap bitmap, int pos, int length = 3)
         => bitmap.GetPixelSpan(length, pos);


        public static unsafe Span<uint> GetPixelRowSpan(this WriteableBitmap bitmap, int y, int length = 0, int offset = 0)
        {
            using var l = bitmap.Lock();
            return new Span<uint>(IntPtr.Add(l.Address, (int) (bitmap.Size.Width * y + offset)).ToPointer(), (int) (length == 0 ? bitmap.Size.Width : length));
        }

        public static SKBitmap ToSkBitmap(this Mat mat)
        {
            SKBitmap bitmap;
            Mat target = mat;
            SKColorType colorType;
            switch (mat.NumberOfChannels)
            {
                case 1:
                    colorType = SKColorType.Gray8;
                    break;
                case 2:
                    colorType = SKColorType.Rg1616;
                    break;
                case 3:
                    CvInvoke.CvtColor(mat, target, ColorConversion.Bgr2Bgra);
                    colorType = SKColorType.Bgra8888;
                    break;
                case 4:
                    colorType = SKColorType.Bgra8888;
                    break;
                default:
                    throw new Exception("Unknown color type");
            }

            bitmap = new SKBitmap(new SKImageInfo(target.Width, target.Height, colorType));
            bitmap.SetPixels(target.DataPointer);
            return bitmap;
        }

        public static SKImage ToSkImage(this Mat mat)
        {
            var bitmap = mat.ToSkBitmap();
            if (bitmap is null) return null;
            return SKImage.FromBitmap(bitmap);
        }

        public static WriteableBitmap ToBitmap(this Mat mat)
        {
            var dataCount = mat.Width * mat.Height;

            var writableBitmap = new WriteableBitmap(new PixelSize(mat.Width, mat.Height), new Vector(96, 96),
                PixelFormat.Bgra8888, AlphaFormat.Unpremul);

            

            using var lockBuffer = writableBitmap.Lock();

           

            unsafe
            {

                var targetPixels = (uint*) (void*) lockBuffer.Address;
                switch (mat.NumberOfChannels)
                {
                    //Stopwatch sw = Stopwatch.StartNew();
                    // Method 1 (Span copy)
                    case 1:
                        var srcPixels1 = (byte*) (void*) mat.DataPointer;
                        for (var i = 0; i < dataCount; i++)
                        {
                            var color = srcPixels1[i];
                            targetPixels[i] = (uint) (color | color << 8 | color << 16 | 0xff << 24);
                        }

                        break;
                    case 3:
                        var srcPixels2 = (byte*) (void*) mat.DataPointer;
                        uint pixel = 0;
                        for (uint i = 0; i < dataCount; i++)
                        {
                            targetPixels[i] = (uint)(srcPixels2[pixel++] | srcPixels2[pixel++] << 8 | srcPixels2[pixel++] << 16 | 0xff << 24);
                        }

                        break;
                    case 4:

                        if (mat.Depth == DepthType.Cv8U)
                        {
                            var srcPixels4 = (byte*)(void*)mat.DataPointer;
                            uint pixel4 = 0;
                            for (uint i = 0; i < dataCount; i++)
                            {
                                targetPixels[i] = (uint)(srcPixels4[pixel4++] | srcPixels4[pixel4++] << 8 | srcPixels4[pixel4++] << 16 | srcPixels4[pixel4++] << 24);
                            }
                        }
                        else if (mat.Depth == DepthType.Cv32S)
                        {
                            var srcPixels4 = (uint*) (void*) mat.DataPointer;
                            for (uint i = 0; i < dataCount; i++)
                            {
                                targetPixels[i] = srcPixels4[i];
                            }
                        }

                        break;
                }
            }

            return writableBitmap;
            /*Debug.WriteLine($"Method 1 (Span copy): {sw.ElapsedMilliseconds}ms");
            
            // Method 2 (OpenCV Convertion + Copy Marshal)
            sw.Restart();
            CvInvoke.CvtColor(mat, target, ColorConversion.Bgr2Bgra);
            var buffer = target.GetBytes();
            Marshal.Copy(buffer, 0, lockBuffer.Address, buffer.Length);
            Debug.WriteLine($"Method 2 (OpenCV Convertion + Copy Marshal): {sw.ElapsedMilliseconds}ms");

            
            //sw.Restart();
            CvInvoke.CvtColor(mat, target, ColorConversion.Bgr2Bgra);
            unsafe
            {
                var srcAddress = (uint*)(void*)target.DataPointer;
                var targetAddress = (uint*)(void*)lockBuffer.Address;
                *targetAddress = *srcAddress;
            }
            //Debug.WriteLine($"Method 3 (OpenCV Convertion + Set Address): {sw.ElapsedMilliseconds}ms");

            return writableBitmap;
            */
            /* for (var y = 0; y < mat.Height; y++)
                {
                    Marshal.Copy(buffer, y * lockBuffer.RowBytes, new IntPtr(lockBuffer.Address.ToInt64() + y * lockBuffer.RowBytes), lockBuffer.RowBytes);
                }*/
        }
    }
}