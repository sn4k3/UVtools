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

        public static Bitmap ToBitmap(this Mat mat)
        {
            if (mat.NumberOfChannels == 1)
            {
                var writeableBitmap = new WriteableBitmap(new PixelSize(mat.Width, mat.Height), new Vector(72, 72),
                    PixelFormat.Rgba8888, AlphaFormat.Unpremul);
                var span = mat.GetPixelSpan<byte>();
                var bytes = new[] {0, 0, 0, 255};
                using (var lockBuffer = writeableBitmap.Lock())
                {
                    for (var i = 1; i < span.Length; i++)
                    {
                        bytes[0] = bytes[1] = bytes[2] = span[i];
                        Marshal.Copy(bytes, 0,
                            new IntPtr(lockBuffer.Address.ToInt64() + i * 4), bytes.Length);
                    }
                }

                return writeableBitmap;
            }

            return null;
        }
        /*PixelFormat targetPixelFormat = PixelFormat.Bgra8888;
        
        switch (mat.NumberOfChannels)
        {
            case 3:
                targetPixelFormat = PixelFormat.Rgb565;
                break;
            case 4:
                targetPixelFormat = PixelFormat.Bgra8888;
                break;
            default:
                throw new Exception("Unknown color type");
        }

        
        var writeableBitmap = new WriteableBitmap(new PixelSize(mat.Width, mat.Height), new Vector(72, 72), targetPixelFormat, AlphaFormat.Unpremul);
        using var lockBuffer = writeableBitmap.Lock();
        var buffer = mat.GetBytes();
        for (var y = 0; y < mat.Height; y++)
        {
            Marshal.Copy(buffer, y * lockBuffer.RowBytes, new IntPtr(lockBuffer.Address.ToInt64() + y * lockBuffer.RowBytes), lockBuffer.RowBytes);
        }
        //Marshal.Copy(buffer, 0, lockBuffer.Address, buffer.Length);
        
        SKBitmap bitmap = new SKBitmap(new SKImageInfo(mat.Width, mat.Height, SKColorType.Gray8));
        bitmap.SetPixels(mat.DataPointer);
        Debug.WriteLine(bitmap.Info.ColorType);

        bitmap = SKBitmap.Decode(buffer.AsSpan(), new SKImageInfo
        {
            Width = mat.Width,
            Height = mat.Height,
            ColorType = SKColorType.Gray8,
        });*/

        /*return writeableBitmap;
        }*/
    }
}