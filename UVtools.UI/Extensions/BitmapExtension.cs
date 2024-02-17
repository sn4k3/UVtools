/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Emgu.CV;
using Emgu.CV.CvEnum;
using SkiaSharp;
using System;
using System.Threading.Tasks;
using UVtools.Core.Extensions;

namespace UVtools.UI.Extensions;

/// <summary>
/// Provide extension method to convert IInputArray to and from Bitmap
/// </summary>
public static class BitmapExtension
{
    public static int GetByteStep(this ILockedFramebuffer buffer)
        => buffer.RowBytes;

    public static int GetLength(this ILockedFramebuffer buffer)
        => buffer.Size.Width * buffer.Size.Height;

    public static int GetPixelPos(this ILockedFramebuffer buffer, int x, int y)
        => buffer.RowBytes * y + x;

    public static int GetPixelPos(this ILockedFramebuffer buffer, System.Drawing.Point location)
        => buffer.GetPixelPos(location.X, location.Y);

    public static int GetByteStep(this WriteableBitmap bitmap)
        => (int)bitmap.Size.Width * 4;

    /// <summary>
    /// Gets the total length of this <see cref="WriteableBitmap"/> as uint
    /// </summary>
    /// <param name="bitmap"></param>
    /// <returns>The total length of this <see cref="WriteableBitmap"/></returns>
    public static int GetLength(this WriteableBitmap bitmap)
        => (int) (bitmap.Size.Width * bitmap.Size.Height);

    public static int GetPixelPos(this WriteableBitmap bitmap, int x, int y)
        => bitmap.GetByteStep() * y + x;

    public static int GetPixelPos(this WriteableBitmap bitmap, System.Drawing.Point location)
        => bitmap.GetPixelPos(location.X, location.Y);

    public static unsafe Span<byte> GetPixelByteSpan(this ILockedFramebuffer buffer, int length = 0, int offset = 0)
    {
        return new(IntPtr.Add(buffer.Address, offset).ToPointer(), length > 0 ? length : buffer.RowBytes * buffer.Size.Height);
    }

    public static unsafe Span<uint> GetPixelSpan(this ILockedFramebuffer buffer, int length = 0, int offset = 0) 
        => new(IntPtr.Add(buffer.Address, offset).ToPointer(), length > 0 ? length : buffer.GetLength());

    public static Span<uint> GetSinglePixelSpan(this ILockedFramebuffer buffer, int x, int y, int length = 1) 
        => buffer.GetPixelSpan(length, buffer.GetPixelPos(x, y));

    public static Span<uint> GetSinglePixelPosSpan(this ILockedFramebuffer buffer, int pos, int length = 1)
        => buffer.GetPixelSpan(length, pos);

    public static Span<uint> GetPixelRowSpan(this ILockedFramebuffer buffer, int y, int length = 0, int offset = 0)
        => buffer.GetPixelSpan(length == 0 ? buffer.Size.Width : length, buffer.GetPixelPos(offset, y));

    /// <summary>
    /// Gets a single pixel span to manipulate or read pixels
    /// </summary>
    /// <returns>A <see cref="Span{T}"/> containing all pixels in data memory</returns>
    public static Span<uint> GetPixelSpan(this WriteableBitmap bitmap, int length = 0, int offset = 0)
    {
        using var l = bitmap.Lock();
        return l.GetPixelSpan(length, offset);
    }

    public static Span<uint> GetSinglePixelSpan(this WriteableBitmap bitmap, int x, int y, int length = 1) 
        => bitmap.GetPixelSpan(length, bitmap.GetPixelPos(x, y));

    public static Span<uint> GetSinglePixelPosSpan(this WriteableBitmap bitmap, int pos, int length = 1)
        => bitmap.GetPixelSpan(length, pos);


    public static Span<uint> GetPixelRowSpan(this WriteableBitmap bitmap, int y, int length = 0, int offset = 0)
        => bitmap.GetPixelSpan(length == 0 ? (int)bitmap.Size.Width : length, bitmap.GetPixelPos(offset, y));

    public static SKBitmap ToSkBitmap(this Mat mat)
    {
        var target = mat;
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

        var bitmap = new SKBitmap(new SKImageInfo(target.Width, target.Height, colorType));
        bitmap.SetPixels(target.DataPointer);
        return bitmap;
    }

    public static SKImage ToSkImage(this Mat mat)
    {
        var bitmap = mat.ToSkBitmap();
        return SKImage.FromBitmap(bitmap);
    }

    public static WriteableBitmap ToBitmap(this Mat mat)
    {
        var writableBitmap = new WriteableBitmap(new PixelSize(mat.Width, mat.Height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        using var lockBuffer = writableBitmap.Lock();

        switch (mat.NumberOfChannels)
        {
            case 1:
            {
                using var convertMat = new Mat();
                CvInvoke.CvtColor(mat, convertMat, ColorConversion.Gray2Bgra);
                convertMat.CopyTo(lockBuffer.Address);
                break;
            }
            case 3:
            {
                using var convertMat = new Mat();
                CvInvoke.CvtColor(mat, convertMat, ColorConversion.Bgr2Bgra);
                convertMat.CopyTo(lockBuffer.Address);
                break;
            }
            case 4:
            {
                if (mat.IsContinuous)
                {
                    mat.CopyTo(lockBuffer.Address);
                }
                else
                {
                    var srcSpan = mat.GetDataSpan2D<byte>();
                    var dstSpan = lockBuffer.GetPixelByteSpan();
                    srcSpan.CopyTo(dstSpan);
                }

                break;
            }
        }

        return writableBitmap;
    }

    public static WriteableBitmap ToBitmapParallel(this Mat mat)
    {
        var width = mat.Width;
        var height = mat.Height;
        var dataCount = width * height;

        var writableBitmap = new WriteableBitmap(new PixelSize(mat.Width, mat.Height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        using var lockBuffer = writableBitmap.Lock();

        unsafe
        {
            switch (mat.NumberOfChannels)
            {
                // Method 1 (Span copy)
                case 1:
                    Parallel.For(0, height, y =>
                    {
                        var spanBitmap = lockBuffer.GetPixelRowSpan(y);
                        var spanMat = mat.GetRowByteSpan(y);
                        for (var x = 0; x < width; x++)
                        {
                            var color = spanMat[x];
                            spanBitmap[x] = (uint)(color | color << 8 | color << 16 | 0xff << 24);
                        }
                    });
                    

                    break;
                case 3:
                    Parallel.For(0, height, y =>
                    {
                        var spanBitmap = lockBuffer.GetPixelRowSpan(y);
                        var spanMat = mat.GetRowByteSpan(y);
                        int pixel = 0;
                        for (var x = 0; x < width; x++)
                        {
                            spanBitmap[x] = (uint)(spanMat[pixel++] | spanMat[pixel++] << 8 | spanMat[pixel++] << 16 | 0xff << 24);
                        }
                    });

                    break;
                case 4:
                    if (mat.Depth == DepthType.Cv8U)
                    {
                        Parallel.For(0, height, y =>
                        {
                            var spanBitmap = lockBuffer.GetPixelRowSpan(y);
                            var spanMat = mat.GetRowByteSpan(y);
                            int pixel = 0;
                            for (var x = 0; x < width; x++)
                            {
                                spanBitmap[x] = (uint)(spanMat[pixel++] | spanMat[pixel++] << 8 | spanMat[pixel++] << 16 | spanMat[pixel++] << 24);
                            }
                        });
                    }
                    else if (mat.Depth == DepthType.Cv32S)
                    {
                        mat.GetDataReadOnlySpan<uint>().CopyTo(new Span<uint>(lockBuffer.Address.ToPointer(), dataCount));
                    }

                    break;
            }
        }

        return writableBitmap;
    }
}