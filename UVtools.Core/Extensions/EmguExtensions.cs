/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using CommunityToolkit.HighPerformance;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using CommunityToolkit.Diagnostics;
using UVtools.Core.EmguCV;
using UVtools.Core.Objects;

namespace UVtools.Core.Extensions;

public static class EmguExtensions
{
    #region Constants
    /// <summary>
    /// White color: 255, 255, 255, 255
    /// </summary>
    public static readonly MCvScalar WhiteColor = new(255, 255, 255, 255);

    /// <summary>
    /// Black color: 0, 0, 0, 255
    /// </summary>
    public static readonly MCvScalar BlackColor = new(0, 0, 0, 255);
    //public static readonly MCvScalar TransparentColor = new();

    public static readonly Point AnchorCenter = new(-1, -1);
    public static readonly Mat Kernel3x3Rectangle = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), AnchorCenter);

    /// <summary>
    /// Gets the scale relation for 0-255 byte value.<br/>
    /// Constant for: 1/255.0 = 0.00392156862745098039
    /// </summary>
    /// <remarks>Last three digits will drop (...039)</remarks>
    public const double ByteScale = 1 / 255.0;
    #endregion

    #region Initializers methods
    /// <summary>
    /// Create a byte array of size of this <see cref="Mat"/>
    /// </summary>
    /// <param name="mat"></param>
    /// <returns>Blank byte array</returns>
    public static byte[] CreateByteArray(this Mat mat)
        => new byte[mat.GetLength()];

    /// <summary>
    /// Creates a new <see cref="Mat"/> with same size and type of the source
    /// </summary>
    /// <param name="mat"></param>
    /// <returns></returns>
    public static Mat New(this Mat mat)
        => new(mat.Size, mat.Depth, mat.NumberOfChannels);

    /// <summary>
    /// Creates a new <see cref="Mat"/> with same size and type of the source
    /// </summary>
    /// <param name="src"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public static Mat New(this Mat src, MCvScalar color)
        => InitMat(src.Size, color, src.NumberOfChannels, src.Depth);

    /// <summary>
    /// Creates a new blanked (All zeros) <see cref="Mat"/> with same size and type of the source
    /// </summary>
    /// <param name="mat"></param>
    /// <returns>Blanked <see cref="Mat"/></returns>
    public static Mat NewZeros(this Mat mat)
        => InitMat(mat.Size, mat.NumberOfChannels, mat.Depth);

    /// <summary>
    /// Creates a new blanked (All zeros) <see cref="UMat"/> with same size and type of the source
    /// </summary>
    /// <param name="mat"></param>
    /// <returns>Blanked <see cref="Mat"/></returns>
    public static UMat NewZeros(this UMat mat)
        => InitUMat(mat.Size, mat.NumberOfChannels, mat.Depth);

    /// <summary>
    /// Creates a <see cref="Mat"/> with same size and type of the source and set it to a color
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public static Mat NewSetTo(this Mat mat, MCvScalar color)
        => InitMat(mat.Size, color, mat.NumberOfChannels, mat.Depth);


    /// <summary>
    /// Creates a new <see cref="Mat"/> and zero it
    /// </summary>
    /// <param name="size"></param>
    /// <param name="channels"></param>
    /// <param name="depthType"></param>
    /// <returns></returns>
    public static Mat InitMat(Size size, int channels = 1, DepthType depthType = DepthType.Cv8U)
        => size.IsEmpty ? new() : Mat.Zeros(size.Height, size.Width, depthType, channels);

    /// <summary>
    /// Creates a new <see cref="UMat"/> and zero it
    /// </summary>
    /// <param name="size"></param>
    /// <param name="channels"></param>
    /// <param name="depthType"></param>
    /// <returns></returns>
    public static UMat InitUMat(Size size, int channels = 1, DepthType depthType = DepthType.Cv8U)
    {
        if (size.IsEmpty) return new();
        var umat = new UMat(size.Height, size.Width, depthType, channels);
        umat.SetTo(BlackColor);
        return umat;
    }

    /// <summary>
    /// Creates a new <see cref="Mat"/> and set it to a <see cref="MCvScalar"/>
    /// </summary>
    /// <param name="size"></param>
    /// <param name="color"></param>
    /// <param name="channels"></param>
    /// <param name="depthType"></param>
    /// <returns></returns>
    public static Mat InitMat(Size size, MCvScalar color, int channels = 1, DepthType depthType = DepthType.Cv8U)
    {
        if (size.IsEmpty) return new();
        var mat = new Mat(size, depthType, channels);
        mat.SetTo(color);
        return mat;
    }

    /// <summary>
    /// Allocates a new array of mat's
    /// </summary>
    /// <param name="count">Array size</param>
    /// <returns></returns>
    public static Mat[] InitMats(uint count) => InitMats(count, Size.Empty);

    /// <summary>
    /// Allocates a new array of mat 's
    /// </summary>
    /// <param name="count">Array size</param>
    /// <param name="size">Image size to create, use <see cref="Size.Empty"/> to create a empty Mat</param>
    /// <returns>New mat array</returns>
    public static Mat[] InitMats(uint count, Size size)
    {
        var layers = new Mat[count];
        for (var i = 0; i < layers.Length; i++)
        {
            layers[i] = InitMat(size);
        }

        return layers;
    }

    /// <summary>
    /// Create a new <see cref="GpuMat"/> from <see cref="Mat"/>
    /// </summary>
    /// <param name="mat"></param>
    /// <returns></returns>
    public static GpuMat ToGpuMat(this Mat mat)
    {
        var gpuMat = new GpuMat(mat.Rows, mat.Cols, mat.Depth, mat.NumberOfChannels);
        gpuMat.Upload(mat);
        return gpuMat;
    }
    #endregion

    #region Memory accessors

    /// <summary>
    /// Gets the mat bytes as <see cref="UnmanagedMemoryStream"/>
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="accessMode"></param>
    /// <returns></returns>
    public static unsafe UnmanagedMemoryStream GetUnmanagedMemoryStream(this Mat mat, FileAccess accessMode)
    {
        var length = mat.GetLength();
        return new UnmanagedMemoryStream(mat.GetBytePointer(), length, length, accessMode);
    }

    /// <summary>
    /// Gets the byte pointer of this <see cref="Mat"/>
    /// </summary>
    /// <param name="mat"></param>
    /// <returns></returns>
    public static unsafe byte* GetBytePointer(this Mat mat)
        => (byte*)mat.DataPointer.ToPointer();

    /// <summary>
    /// Gets the whole data span to manipulate or read pixels, use this when possibly using ROI
    /// </summary>
    /// <returns></returns>
    public static unsafe Span2D<T> GetDataSpan2D<T>(this Mat mat)
    {
        var step = mat.GetRealStep();
        if (mat.IsContinuous) return new(mat.DataPointer.ToPointer(), mat.Height, step, 0);
        return new(mat.DataPointer.ToPointer(), mat.Height, step, mat.Step / mat.DepthToByteCount() - step);
    }

    /// <summary>
    /// Gets the whole data span to manipulate or read pixels, use this when possibly using ROI
    /// </summary>
    /// <returns></returns>
    public static Span2D<byte> GetDataByteSpan2D(this Mat mat) => mat.GetDataSpan2D<byte>();

    /// <summary>
    /// Gets the whole data span to manipulate or read pixels
    /// </summary>
    /// <param name="mat"></param>
    /// <returns></returns>
    public static Span<byte> GetDataByteSpan(this Mat mat) => mat.GetDataSpan<byte>();

    /// <summary>
    /// Gets the whole data span to manipulate or read pixels
    /// </summary>
    /// <returns></returns>
    public static Span<byte> GetDataByteSpan(this Mat mat, int length, int offset = 0) => GetDataSpan<byte>(mat, length, offset);

    /// <summary>
    /// Gets the data span to manipulate or read pixels given a length and offset
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mat"></param>
    /// <param name="length"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static unsafe Span<T> GetDataSpan<T>(this Mat mat, int length = 0, int offset = 0)
    {
        if (length <= 0)
        {
            if (mat.IsContinuous)
            {
                length = mat.GetLength();
            }
            else
            {
                length = mat.Step / mat.DepthToByteCount() * (mat.Height - 1) + mat.GetRealStep();
            }
        }
        return new(IntPtr.Add(mat.DataPointer, offset).ToPointer(), length);
    }

    /// <summary>
    /// Gets a single pixel span to manipulate or read pixels
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mat"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static Span<T> GetPixelSpan<T>(this Mat mat, int x, int y) 
        => mat.GetDataSpan<T>(mat.NumberOfChannels, mat.GetPixelPos(x, y));

    /// <summary>
    /// Gets a single pixel span to manipulate or read pixels
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mat"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static Span<T> GetPixelSpan<T>(this Mat mat, int pos) 
        => mat.GetDataSpan<T>(mat.NumberOfChannels, pos);

    /// <summary>
    /// Gets a row span to manipulate or read pixels
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mat"></param>
    /// <param name="y"></param>
    /// <param name="length"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static unsafe Span<T> GetRowSpan<T>(this Mat mat, int y, int length = 0, int offset = 0)
    {
        var originalStep = mat.Step;
        if(length <= 0) length = mat.GetRealStep();
        return new(IntPtr.Add(mat.DataPointer, y * originalStep + offset).ToPointer(), length);
    }

    /// <summary>
    /// Gets a row span to manipulate or read pixels
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="y"></param>
    /// <param name="length"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static Span<byte> GetRowByteSpan(this Mat mat, int y, int length = 0, int offset = 0) => mat.GetRowSpan<byte>(y, length, offset);

    /*
    /// <summary>
    /// Gets a col span to manipulate or read pixels
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mat"></param>
    /// <param name="x"></param>
    /// <param name="length"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static unsafe Span<T> GetColSpan<T>(this Mat mat, int x, int length = 0, int offset = 0)
    {
        // Fix with Span2D
        var colMat = mat.Col(x);
        return new(IntPtr.Add(colMat.DataPointer, offset).ToPointer(), length <= 0 ? mat.Height : length);
    }*/



    /// <summary>
    /// Gets the data span to read pixels given a length and offset
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mat"></param>
    /// <param name="length"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static unsafe ReadOnlySpan<T> GetDataReadOnlySpan<T>(this Mat mat, int length = 0, int offset = 0)
    {
        if (length <= 0)
        {
            if (mat.IsContinuous)
            {
                length = mat.GetLength();
            }
            else
            {
                length = mat.Step / mat.DepthToByteCount() * (mat.Height - 1) + mat.GetRealStep();
            }
        }
        return new(IntPtr.Add(mat.DataPointer, offset).ToPointer(), length);
    }

    /// <summary>
    /// Gets the whole data span to manipulate or read pixels
    /// </summary>
    /// <param name="mat"></param>
    /// <returns></returns>
    public static ReadOnlySpan<byte> GetDataByteReadOnlySpan(this Mat mat) => mat.GetDataReadOnlySpan<byte>();

    /// <summary>
    /// Gets a row span to read pixels
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mat"></param>
    /// <param name="y"></param>
    /// <param name="length"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static unsafe ReadOnlySpan<T> GetRowReadOnlySpan<T>(this Mat mat, int y, int length = 0, int offset = 0)
    {
        var originalStep = mat.Step;
        if (length <= 0) length = mat.GetRealStep();
        return new(IntPtr.Add(mat.DataPointer, y * originalStep + offset).ToPointer(), length);
    }

    /// <summary>
    /// Gets a row span or read pixels
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="y"></param>
    /// <param name="length"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static ReadOnlySpan<byte> GetRowByteReadOnlySpan(this Mat mat, int y, int length = 0, int offset = 0) => mat.GetRowReadOnlySpan<byte>(y, length, offset);
    #endregion

    #region Memory Fill

    /// <summary>
    /// Fill a mat span with a color
    /// </summary>
    /// <param name="mat">Mat to fill</param>
    /// <param name="startPosition">Start position, this reference will increment by the <paramref name="length"/></param>
    /// <param name="length">Length to fill</param>
    /// <param name="color">Color to fill with</param>
    /// <param name="colorFillMinThreshold">Ignore and fill to <paramref name="length"/> if <paramref name="color"/> is less than the threshold value</param>
    public static void FillSpan(this Mat mat, ref int startPosition, int length, byte color, byte colorFillMinThreshold = 1)
    {
        if (length <= 0) return;
        if (color < colorFillMinThreshold) // Ignore threshold (mostly if blacks), spare cycles
        {
            startPosition += length;
            return;
        }

        mat.GetDataByteSpan(length, startPosition).Fill(color);
        startPosition += length;
    }

    /// <summary>
    /// Fill a mat span with a color
    /// </summary>
    /// <param name="mat">Mat to fill</param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="length">Length to fill</param>
    /// <param name="color">Color to fill with</param>
    /// <param name="colorFillMinThreshold">Ignore and set to <paramref name="length"/> if <paramref name="color"/> is less than the threshold value</param>
    public static void FillSpan(this Mat mat, int x, int y, int length, byte color, byte colorFillMinThreshold = 1)
    {
        if (length <= 0 || color < colorFillMinThreshold) return; // Ignore threshold (mostly if blacks), spare cycles
        mat.GetDataByteSpan(length, mat.GetPixelPos(x, y)).Fill(color);
    }

    /// <summary>
    /// Fill a mat span with a color
    /// </summary>
    /// <param name="mat">Mat to fill</param>
    /// <param name="position"></param>
    /// <param name="length">Length to fill</param>
    /// <param name="color">Color to fill with</param>
    /// <param name="colorFillMinThreshold">Ignore and fill to <paramref name="length"/> if <paramref name="color"/> is less than the threshold value</param>
    public static void FillSpan(this Mat mat, Point position, int length, byte color, byte colorFillMinThreshold = 1)
        => mat.FillSpan(position.X, position.Y, length, color, colorFillMinThreshold);
    #endregion

    #region Get/Set methods

    /// <summary>
    /// Gets the number of bits this <see cref="Mat"/> uses per data type (Depth)
    /// </summary>
    /// <param name="mat"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static byte DepthToBitCount(this Mat mat)
    {
        return mat.Depth switch
        {
            DepthType.Default => 8,
            DepthType.Cv8U => 8,
            DepthType.Cv8S => 8,
            DepthType.Cv16U => 16,
            DepthType.Cv16S => 16,
            DepthType.Cv32S => 32,
            DepthType.Cv32F => 32,
            DepthType.Cv64F => 64,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    /// <summary>
    /// Gets the number of bytes this <see cref="Mat"/> uses per data type (Depth)
    /// </summary>
    /// <param name="mat"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static byte DepthToByteCount(this Mat mat)
    {
        return mat.Depth switch
        {
            DepthType.Default => 1,
            DepthType.Cv8U => 1,
            DepthType.Cv8S => 1,
            DepthType.Cv16U => 2,
            DepthType.Cv16S => 2,
            DepthType.Cv32S => 4,
            DepthType.Cv32F => 4,
            DepthType.Cv64F => 8,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    /// <summary>
    /// Step return the original Mat step, if ROI step still from original matrix which lead to errors.
    /// Use this to get the real step size
    /// </summary>
    /// <param name="mat"></param>
    /// <returns></returns>
    public static int GetRealStep(this Mat mat)
    {
        return mat.Width * mat.NumberOfChannels;
    }

    /// <summary>
    /// Gets the total length of this <see cref="Mat"/>
    /// </summary>
    /// <param name="mat"></param>
    /// <returns>The total length of this <see cref="Mat"/></returns>
    public static int GetLength(this Mat mat)
    {
        return mat.Total.ToInt32() * mat.NumberOfChannels;
    }

    /// <summary>
    /// Gets a pixel index position on a span given X and Y
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <returns>The pixel index position</returns>
    public static int GetPixelPos(this Mat mat, int x, int y)
    {
        return y * mat.GetRealStep() + x * mat.NumberOfChannels;
    }

    /// <summary>
    /// Gets a pixel index position on a span given X and Y
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="point">X and Y Location</param>
    /// <returns>The pixel index position</returns>
    public static int GetPixelPos(this Mat mat, Point point)
    {
        return mat.GetPixelPos(point.X, point.Y);
    }

    /// <summary>
    /// Gets a byte array copy of this <see cref="Mat"/>
    /// </summary>
    /// <param name="mat"></param>
    /// <returns>Byte array </returns>
    public static byte[] GetBytes(this Mat mat)
    {
        return mat.GetRawData();
    }

    /// <summary>
    /// Gets a byte pixel at a position
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static byte GetByte(this Mat mat, int pos)
    {
        //return new Span<byte>(IntPtr.Add(mat.DataPointer, pos).ToPointer(), mat.Step)[0];
        var value = new byte[1];
        Marshal.Copy(mat.DataPointer + pos, value, 0, value.Length);
        return value[0];
    }

    /// <summary>
    /// Gets a byte pixel at a position
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static byte GetByte(this Mat mat, int x, int y) => GetByte(mat, mat.GetPixelPos(x, y));

    /// <summary>
    /// Gets a byte pixel at a position
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static byte GetByte(this Mat mat, Point pos) => GetByte(mat, mat.GetPixelPos(pos.X, pos.Y));

    /// <summary>
    /// Sets a byte pixel at a position
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="pixel"></param>
    /// <param name="value"></param>
    public static void SetByte(this Mat mat, int pixel, byte value) => SetByte(mat, pixel, new[] { value });

    /// <summary>
    /// Sets a byte pixel at a position
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="pixel"></param>
    /// <param name="value"></param>
    public static void SetByte(this Mat mat, int pixel, byte[] value) => Marshal.Copy(value, 0, mat.DataPointer + pixel, value.Length);

    /// <summary>
    /// Sets a byte pixel at a position
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="value"></param>
    public static void SetByte(this Mat mat, int x, int y, byte value) => SetByte(mat, x, y, new[] { value });

    /// <summary>
    /// Sets a byte pixel at a position
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="pos"></param>
    /// <param name="value"></param>
    public static void SetByte(this Mat mat, Point pos, byte value) => SetByte(mat, pos.X, pos.Y, new[] { value });

    /// <summary>
    /// Sets a byte pixel at a position
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="value"></param>
    public static void SetByte(this Mat mat, int x, int y, byte[] value) => SetByte(mat, y * mat.GetRealStep() + x * mat.NumberOfChannels, value);

    /// <summary>
    /// Sets bytes
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="value"></param>
    public static void SetBytes(this Mat mat, byte[] value)
    {
        mat.SetTo(value);
    }

    /// <summary>
    /// Gets PNG byte array
    /// </summary>
    /// <param name="mat"></param>
    /// <returns></returns>
    public static byte[] GetPngByes(this IInputArray mat)
    {
        return CvInvoke.Imencode(".png", mat);
    }

    public static Point GetCenterPoint(this Mat mat) => new(mat.Width / 2, mat.Height / 2);

    #endregion

    #region Create methods

    public static Mat CreateMask(this Mat src, VectorOfVectorOfPoint contours, Point offset = default)
    {
        var mask = src.NewZeros();
        CvInvoke.DrawContours(mask, contours, -1, WhiteColor, -1, LineType.EightConnected, null, int.MaxValue, offset);
        return mask;
    }

    public static Mat CreateMask(this Mat src, Point[][] contours, Point offset = default)
    {
        using var vec = new VectorOfVectorOfPoint(contours);
        return src.CreateMask(vec, offset);
    }

    public static Mat CropByBounds(this Mat src, bool cloneInsteadRoi = false)
    {
        var rect = CvInvoke.BoundingRectangle(src);
        if (rect.Size == Size.Empty) return src.New();
        if (src.Size == rect.Size) return cloneInsteadRoi ? src.Roi(src.Size) : src.Clone();
        var roi = src.Roi(rect);
        
        if (cloneInsteadRoi)
        {
            var clone = roi.Clone();
            roi.Dispose();
            return clone;
        }

        return roi;
    }

    public static Mat CropByBounds(this Mat src, ushort margin) => src.CropByBounds(new Size(margin, margin));

    public static Mat CropByBounds(this Mat src, Size margin)
    {
        var rect = CvInvoke.BoundingRectangle(src);
        if (rect.Size == Size.Empty) return src.New();
        using var roi = src.Size == rect.Size ? src.Roi(src.Size) : src.Roi(rect);

        var numberOfChannels = roi.NumberOfChannels;
        var cropped = InitMat(new Size(roi.Width + margin.Width * 2, roi.Height + margin.Height * 2), numberOfChannels, roi.Depth);
        
        using var dest = new Mat(cropped, new Rectangle(margin.Width, margin.Height, roi.Width, roi.Height));
        roi.CopyTo(dest);

        return cropped;
    }

    public static void CropByBounds(this Mat src, Mat dst)
    {
        using var mat = src.CropByBounds();
        dst.Create(mat.Rows, mat.Cols, mat.Depth, mat.NumberOfChannels);
        src.CopyTo(dst);
    }


    #endregion

    #region Copy methods

    /// <summary>
    /// Copy the whole mat data to another <paramref name="destination"/> reference
    /// </summary>
    /// <remarks>It does not do any safe-check.</remarks>
    /// <param name="mat"></param>
    /// <param name="destination">Destination address to copy data to</param>
    public static void CopyTo(this Mat mat, IntPtr destination)
    {
        unsafe
        {
            if (mat.IsContinuous)
            {
                var totalBytes = (uint)mat.GetLength();
                Buffer.MemoryCopy(mat.DataPointer.ToPointer(), destination.ToPointer(), totalBytes, totalBytes);
            }
            else
            {
                var srcSpan = mat.GetDataSpan2D<byte>();
                var dstSpan = new Span<byte>(destination.ToPointer(), mat.GetLength());
                srcSpan.CopyTo(dstSpan);
            }
        }

    }

    /// <summary>
    /// Copy a region from <see cref="Mat"/> to center of other <see cref="Mat"/>
    /// </summary>
    /// <param name="src">Source <see cref="Mat"/> to be copied to</param>
    /// <param name="size">Size of the center offset</param>
    /// <param name="dst">Target <see cref="Mat"/> to paste the <paramref name="src"/></param>
    public static void CopyCenterToCenter(this Mat src, Size size, Mat dst)
    {
        using var srcRoi = src.RoiFromCenter(size);
        CopyToCenter(srcRoi, dst);
    }

    /// <summary>
    /// Copy a region from <see cref="Mat"/> to center of other <see cref="Mat"/>
    /// </summary>
    /// <param name="src">Source <see cref="Mat"/> to be copied to</param>
    /// <param name="region">Region to copy</param>
    /// <param name="dst">Target <see cref="Mat"/> to paste the <paramref name="src"/></param>
    public static void CopyRegionToCenter(this Mat src, Rectangle region, Mat dst)
    {
        using var srcRoi = src.Roi(region);
        CopyToCenter(srcRoi, dst);
    }

    /// <summary>
    /// Copy a <see cref="Mat"/> to center of other <see cref="Mat"/>
    /// </summary>
    /// <param name="src">Source <see cref="Mat"/> to be copied to</param>
    /// <param name="dst">Target <see cref="Mat"/> to paste the <paramref name="src"/></param>
    public static void CopyToCenter(this Mat src, Mat dst)
    {
        var srcStep = src.GetRealStep();
        var dstStep = dst.GetRealStep();
        var dx = Math.Abs(dstStep - srcStep) / 2;
        var dy = Math.Abs(dst.Height - src.Height) / 2;

        if (src.Size == dst.Size)
        {
            src.CopyTo(dst);
            return;
        }

        if (dstStep > srcStep && dst.Height > src.Height)
        {
            using var dstRoi = dst.Roi(new Rectangle(dx, dy, src.Width, src.Height));
            src.CopyTo(dstRoi);
            return;
        }
            
        if (dstStep < srcStep && dst.Height < src.Height)
        {
            using var srcRoi = src.Roi(new Rectangle(dx, dy, dst.Width, dst.Height));
            srcRoi.CopyTo(dst);
            return;
        }

        throw new InvalidOperationException("Unable to copy, out of bounds");
    }

    public static void CopyAreasSmallerThan(this Mat src, double threshold, Mat dst)
    {
        if (threshold <= 1) return;
        using var contours = src.FindContours(out var hierarchy, RetrType.Tree);
        var contourGroups = EmguContours.GetContoursInGroups(contours, hierarchy);

        var mask = src.NewZeros();
        uint drawContours = 0;
        foreach (var contourGroup in contourGroups)
        {
            using var selectedContours = new VectorOfVectorOfPoint();
            foreach (var group in contourGroup)
            {
                var area = EmguContours.GetContourArea(group);
                if (area >= threshold) continue;
                drawContours++;
                selectedContours.Push(group);
            }

            if (selectedContours.Size == 0) continue;
            CvInvoke.DrawContours(mask, selectedContours, -1, WhiteColor, -1);

        }
        if (drawContours > 0) src.CopyTo(dst, mask);
    }

    public static void CopyAreasLargerThan(this Mat src, double threshold, Mat dst)
    {
        if (threshold <= 0) return;
        using var contours = src.FindContours(out var hierarchy, RetrType.Tree);
        var contourGroups = EmguContours.GetContoursInGroups(contours, hierarchy);

        var mask = src.NewZeros();
        uint drawContours = 0;
        foreach (var contourGroup in contourGroups)
        {
            using var selectedContours = new VectorOfVectorOfPoint();
            foreach (var group in contourGroup)
            {
                var area = EmguContours.GetContourArea(group);
                if (area <= threshold) continue;
                drawContours++;
                selectedContours.Push(group);
            }

            if (selectedContours.Size == 0) continue;
            CvInvoke.DrawContours(mask, selectedContours, -1, WhiteColor, -1);
                
        }
        if(drawContours > 0) src.CopyTo(dst, mask);
    }
    #endregion

    #region Roi methods

    /// <summary>
    /// Gets a Roi
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="roi"></param>
    /// <param name="emptyRoiBehaviour"></param>
    /// <returns></returns>
    public static Mat Roi(this Mat mat, Rectangle roi, EmptyRoiBehaviour emptyRoiBehaviour = EmptyRoiBehaviour.Continue)
    {
        return emptyRoiBehaviour switch
        {
            EmptyRoiBehaviour.Continue => new Mat(mat, roi),
            EmptyRoiBehaviour.CaptureSource => new Mat(mat, roi.IsEmpty ? new Rectangle(Point.Empty, mat.Size) : roi),
            _ => throw new ArgumentOutOfRangeException(nameof(emptyRoiBehaviour), emptyRoiBehaviour, null)
        };
    }

    /// <summary>
    /// Gets a Roi at x=0 and y=0 given a size
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static Mat Roi(this Mat mat, Size size)
    {
        return new Mat(mat, new(Point.Empty, size));
    }

    /// <summary>
    /// Gets a Roi from a mat size
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="fromMat"></param>
    /// <returns></returns>
    public static Mat Roi(this Mat mat, Mat fromMat)
    {
        return new Mat(mat, new(Point.Empty, fromMat.Size));
    }

    /// <summary>
    /// Calculates the bounding rectangle and return a <see cref="Mat"/> object with it
    /// </summary>
    /// <param name="mat"></param>
    /// <returns></returns>
    public static Mat BoundingRectangleRoi(this Mat mat)
    {
        var boundingRectangle = CvInvoke.BoundingRectangle(mat);
        return new Mat(mat, boundingRectangle);
    }

    /// <summary>
    /// Calculates the bounding rectangle and return a <see cref="MatRoi"/> object with it
    /// </summary>
    /// <param name="mat"></param>
    /// <returns></returns>
    public static MatRoi BoundingRectangleMatRoi(this Mat mat)
    {
        var boundingRectangle = CvInvoke.BoundingRectangle(mat);
        return new MatRoi(mat, boundingRectangle);
    }

    /// <summary>
    /// Gets a Roi from center
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static Mat RoiFromCenter(this Mat mat, Size size)
    {
        if(mat.Size == size) return mat.Roi(size);

        var newRoi = mat.Roi(new Rectangle(
            mat.Width / 2 - size.Width / 2,
            mat.Height / 2 - size.Height / 2,
            size.Width,
            size.Height
        ));

        return newRoi;
    }

    /// <summary>
    /// Gets a new mat obtained from it center at a target size and roi
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="targetSize"></param>
    /// <param name="roi"></param>
    /// <returns></returns>
    public static Mat NewMatFromCenterRoi(this Mat mat, Size targetSize, Rectangle roi)
    {
        if (targetSize == mat.Size) return mat.Clone();
        var newMat = InitMat(targetSize);
        using var roiMat = mat.Roi(roi);
            
        //int xStart = mat.Width / 2 - targetSize.Width / 2;
        //int yStart = mat.Height / 2 - targetSize.Height / 2;
        using var newMatRoi = newMat.RoiFromCenter(roi.Size);
        /*var newMatRoi = new Mat(newMat, new Rectangle(
            targetSize.Width / 2 - roi.Width / 2,
            targetSize.Height / 2 - roi.Height / 2,
            roi.Width,
            roi.Height
        ));*/
        roiMat.CopyTo(newMatRoi);
        return newMat;
    }
    #endregion

    #region Is methods

    /// <summary>
    /// Gets if a <see cref="Mat"/> is all zeroed by a threshold
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="threshold">Pixel brightness threshold</param>
    /// <param name="startPos">Start pixel position</param>
    /// <param name="length">Pixel span length</param>
    /// <returns></returns>
    public static bool IsZeroed(this Mat mat, byte threshold = 0, int startPos = 0, int length = 0)
    {
        return mat.FindFirstPixelGreaterThan(threshold, startPos, length) == -1;
    }
    #endregion

    #region Find methods

    /// <summary>
    /// Finds the first negative (Black) pixel
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="startPos">Start pixel position</param>
    /// <param name="length">Pixel span length</param>
    /// <returns>Pixel position in the span, or -1 if not found</returns>
    public static int FindFirstNegativePixel(this Mat mat, int startPos = 0, int length = 0)
    {
        return mat.FindFirstPixelEqualTo(0, startPos, length);
    }

    /// <summary>
    /// Finds the first positive pixel
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="startPos">Start pixel position</param>
    /// <param name="length">Pixel span length</param>
    /// <returns>Pixel position in the span, or -1 if not found</returns>
    public static int FindFirstPositivePixel(this Mat mat, int startPos = 0, int length = 0)
    {
        return mat.FindFirstPixelGreaterThan(0, startPos, length);
    }

    /// <summary>
    /// Finds the first pixel that is <paramref name="value"/>
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="value"></param>
    /// <param name="startPos">Start pixel position</param>
    /// <param name="length">Pixel span length</param>
    /// <returns>Pixel position in the span, or -1 if not found</returns>
    public static int FindFirstPixelEqualTo(this Mat mat, byte value, int startPos = 0, int length = 0)
    {
        var span = mat.GetDataByteReadOnlySpan();
        if (length <= 0) length = span.Length;
        for (var i = startPos; i < length; i++)
        {
            if (span[i] == value) return i;
        }

        return -1;
    }

    /// <summary>
    /// Finds the first pixel that is at less than <paramref name="value"/>
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="value"></param>
    /// <param name="startPos">Start pixel position</param>
    /// <param name="length">Pixel span length</param>
    /// <returns>Pixel position in the span, or -1 if not found</returns>
    public static int FindFirstPixelLessThan(this Mat mat, byte value, int startPos = 0, int length = 0)
    {
        var span = mat.GetDataByteReadOnlySpan();
        if (length <= 0) length = span.Length;
        for (var i = startPos; i < length; i++)
        {
            if (span[i] < value) return i;
        }

        return -1;
    }

    /// <summary>
    /// Finds the first pixel that is at less or equal than <paramref name="value"/>
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="value"></param>
    /// <param name="startPos">Start pixel position</param>
    /// <param name="length">Pixel span length</param>
    /// <returns>Pixel position in the span, or -1 if not found</returns>
    public static int FindFirstPixelEqualOrLessThan(this Mat mat, byte value, int startPos = 0, int length = 0)
    {
        var span = mat.GetDataByteReadOnlySpan();
        if (length <= 0) length = span.Length;
        for (var i = startPos; i < length; i++)
        {
            if (span[i] <= value) return i;
        }

        return -1;
    }

    /// <summary>
    /// Finds the first pixel that is at greater than <paramref name="value"/>
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="value"></param>
    /// <param name="startPos">Start pixel position</param>
    /// <param name="length">Pixel span length</param>
    /// <returns>Pixel position in the span, or -1 if not found</returns>
    public static int FindFirstPixelGreaterThan(this Mat mat, byte value, int startPos = 0, int length = 0)
    {
        var span = mat.GetDataByteReadOnlySpan();
        if (length <= 0) length = span.Length;
        for (var i = startPos; i < length; i++)
        {
            if (span[i] > value) return i;
        }

        return -1;
    }

    /// <summary>
    /// Finds the first pixel that is at equal or greater than <paramref name="value"/>
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="value"></param>
    /// <param name="startPos">Start pixel position</param>
    /// <param name="length">Pixel span length</param>
    /// <returns>Pixel position in the span, or -1 if not found</returns>
    public static int FindFirstPixelEqualOrGreaterThan(this Mat mat, byte value, int startPos = 0, int length = 0)
    {
        var span = mat.GetDataByteReadOnlySpan();
        if (length <= 0) length = span.Length;
        for (var i = startPos; i < length; i++)
        {
            if (span[i] >= value) return i;
        }

        return -1;
    }

    /// <summary>
    /// Scan sequential strides of continuous pixels
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="strideLimit">Size limit of a single stride.</param>
    /// <param name="breakOnRows">True to break the stride sequence on a new row, otherwise false.</param>
    /// <param name="startOnFirstPositivePixel">True to skip the first sequence of black pixels, otherwise false.</param>
    /// <param name="excludeBlacks">True to exclude black strides from returning, otherwise false.</param>
    /// <param name="thresholdGrey">Value to threshold the grey, below or equal to this value will set to 0, otherwise <paramref name="thresholdMaxGrey"/></param>
    /// <param name="thresholdMaxGrey">Grey value to set when the threshold is above the limit.</param>
    /// <returns></returns>
    public static List<GreyStride> ScanStrides(this Mat mat, uint strideLimit = 0, bool breakOnRows = false, bool startOnFirstPositivePixel = false, bool excludeBlacks = false, byte thresholdGrey = 0, byte thresholdMaxGrey = byte.MaxValue)
    {
        Guard.IsEqualTo(mat.NumberOfChannels, 1);
        Guard.IsNotEqualTo(strideLimit, 1);
        //Guard.IsGreaterThan(strideLimit, 1);

        var result = new List<GreyStride>();

        if (mat.IsEmpty) return result;

        int i = 0;
        int x = 0;
        int y = 0;

        int index = 0;
        Point location = default;
        uint stride = 0;
        byte grey = 0;

        var maxWidth = mat.Width;

        var span = mat.GetDataByteReadOnlySpan();

        if (excludeBlacks || startOnFirstPositivePixel)
        {
            for (; i < span.Length; i++)
            {
                grey = span[i];
                if (thresholdGrey is > byte.MinValue and < byte.MaxValue)
                {
                    grey = grey <= thresholdGrey ? byte.MinValue : thresholdMaxGrey;
                }
                if (grey == 0) continue;
                index = i;
                location.X = i % maxWidth;
                location.Y = y = i / maxWidth;
                stride = 1;
                i++;

                x = location.X + 1;

                break;
            }
        }

        for (; i < span.Length; i++)
        {
            // Check for rows
            if (x == maxWidth)
            {
                y++;

                if (breakOnRows && stride > 0)
                {
                    if (!excludeBlacks || (excludeBlacks && grey > 0)) result.Add(new GreyStride(index, location, stride, grey));
                    index = i;
                    location.X = 0;
                    location.Y = y;
                    stride = 1;
                    grey = span[i];
                    if (thresholdGrey is > byte.MinValue and < byte.MaxValue)
                    {
                        grey = grey <= thresholdGrey ? byte.MinValue : thresholdMaxGrey;
                    }

                    x = 1;

                    continue;
                }

                x = 0;
            }
            
            // Check for sequence
            var currentGrey = span[i];
            if (thresholdGrey is > byte.MinValue and < byte.MaxValue)
            {
                currentGrey = currentGrey <= thresholdGrey ? byte.MinValue : thresholdMaxGrey;
            }

            if (currentGrey == grey)
            {
                stride++;
                if (stride == strideLimit)
                {
                    if (!excludeBlacks || (excludeBlacks && grey > 0)) result.Add(new GreyStride(index, location, stride, grey));
                    index = i;
                    location.X = x;
                    location.Y = y;
                    stride = 0;
                }
            }
            else
            {
                if (stride > 0)
                {
                    if (!excludeBlacks || (excludeBlacks && grey > 0)) result.Add(new GreyStride(index, location, stride, grey));
                    index = i;
                    location.X = x;
                    location.Y = y;
                }

                stride = 1;
                grey = currentGrey;
            }
            
            x++;
        }

        // Return the left over
        if (stride > 0 && (!excludeBlacks || (excludeBlacks && grey > 0)))
        {
            result.Add(new GreyStride(index, location, stride, grey));
        }

        return result;
    }

    /// <summary>
    /// Scan sequential strides of continuous pixels
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="greyFunc">Function to filter and process the gray value.</param>
    /// <param name="strideLimit">Size limit of a single stride.</param>
    /// <param name="breakOnRows">True to break the stride sequence on a new row, otherwise false.</param>
    /// <param name="startOnFirstPositivePixel">True to skip the first sequence of black pixels, otherwise false.</param>
    /// <param name="excludeBlacks">True to exclude black strides from returning, otherwise false.</param>
    /// <returns></returns>
    public static List<GreyStride> ScanStrides(this Mat mat, Func<byte, byte> greyFunc, uint strideLimit = 0, bool breakOnRows = false, bool startOnFirstPositivePixel = false, bool excludeBlacks = false)
    {
        Guard.IsEqualTo(mat.NumberOfChannels, 1);
        Guard.IsNotEqualTo(strideLimit, 1);
        //Guard.IsGreaterThan(strideLimit, 1);

        var result = new List<GreyStride>();

        if (mat.IsEmpty) return result;

        int i = 0;
        int x = 0;
        int y = 0;

        int index = 0;
        Point location = default;
        uint stride = 0;
        byte grey = 0;

        var maxWidth = mat.Width;

        var span = mat.GetDataByteReadOnlySpan();

        if (excludeBlacks || startOnFirstPositivePixel)
        {
            for (; i < span.Length; i++)
            {
                grey = greyFunc(span[i]);

                if (grey == 0) continue;
                index = i;
                location.X = i % maxWidth;
                location.Y = y = i / maxWidth;
                stride = 1;
                i++;

                x = location.X + 1;

                break;
            }
        }

        for (; i < span.Length; i++)
        {
            // Check for rows
            if (x == maxWidth)
            {
                y++;

                if (breakOnRows && stride > 0)
                {
                    if (!excludeBlacks || (excludeBlacks && grey > 0)) result.Add(new GreyStride(index, location, stride, grey));
                    index = i;
                    location.X = 0;
                    location.Y = y;
                    stride = 1;
                    grey = greyFunc(span[i]);

                    x = 1;

                    continue;
                }

                x = 0;
            }

            // Check for sequence
            var currentGrey = greyFunc(span[i]);

            if (currentGrey == grey)
            {
                stride++;
                if (stride == strideLimit)
                {
                    if (!excludeBlacks || (excludeBlacks && grey > 0)) result.Add(new GreyStride(index, location, stride, grey));
                    index = i;
                    location.X = x;
                    location.Y = y;
                    stride = 0;
                }
            }
            else
            {
                if (stride > 0)
                {
                    if (!excludeBlacks || (excludeBlacks && grey > 0)) result.Add(new GreyStride(index, location, stride, grey));
                    index = i;
                    location.X = x;
                    location.Y = y;
                }

                stride = 1;
                grey = currentGrey;
            }

            x++;
        }

        // Return the left over
        if (stride > 0 && (!excludeBlacks || (excludeBlacks && grey > 0)))
        {
            result.Add(new GreyStride(index, location, stride, grey));
        }

        return result;
    }

    /*public static List<GreyStride> ScanLines(this Mat mat)
    {
        return mat.ScanStrides(0, true, true, true);
    }*/

    /// <summary>
    /// Scan sequential lines in X or Y direction
    /// </summary>
    /// <param name="mat">Mat to scan</param>
    /// <param name="vertically">True to scan vertically, otherwise horizontally</param>
    /// <param name="thresholdGrey">Value to threshold the grey, less or equal to this value will set to 0, otherwise 255</param>
    /// <param name="offset">Value to offset the coordinates with.</param>
    /// <returns>List of all lines</returns>
    public static List<GreyLine> ScanLines(this Mat mat, bool vertically = false, byte thresholdGrey = 0, Point offset = default)
    {
        Guard.IsEqualTo(mat.NumberOfChannels, 1);

        var lines = new List<GreyLine>();

        if (mat.IsEmpty) return lines;

        var matSize = mat.Size;

        GreyLine line = default;

        byte grey;
        int x;
        int y;

        if (vertically)
        {
            var span = mat.GetDataByteSpan2D();
            for (x = 0; x < matSize.Width; x++)
            {
                line.StartX = x + offset.X;
                line.StartY = 0 + offset.Y;
                line.EndX = x + offset.X;
                line.EndY = 0 + offset.Y;
                line.Grey = 0;

                for (y = 0; y < matSize.Height; y++)
                {
                    grey = span[y, x];
                    if (thresholdGrey is > byte.MinValue and < byte.MaxValue)
                    {
                        grey = grey <= thresholdGrey ? byte.MinValue : byte.MaxValue;
                    }

                    if (line.Grey == 0)
                    {
                        if (grey == 0) continue;
                        line.StartY = y + offset.Y;
                        line.Grey = grey;
                        continue;
                    }

                    if (grey == line.Grey) continue;
                    line.EndY = y - 1 + offset.Y;
                    lines.Add(line);

                    line.Grey = 0;
                    y--;
                }

                if (line.Grey > 0)
                {
                    line.EndY = y - 1 + offset.Y;
                    lines.Add(line);
                }
            }
        }
        else // Horizontal
        {
            for (y = 0; y < matSize.Height; y++)
            {
                var span = mat.GetRowByteSpan(y);
                line.StartX = 0 + offset.X;
                line.StartY = y + offset.Y;
                line.EndX = 0 + offset.X;
                line.EndY = y + offset.Y;
                line.Grey = 0;

                for (x = 0; x < matSize.Width; x++)
                {
                    grey = span[x];
                    if (thresholdGrey is > byte.MinValue and < byte.MaxValue)
                    {
                        grey = grey <= thresholdGrey ? byte.MinValue : byte.MaxValue;
                    }

                    if (line.Grey == 0)
                    {
                        if (grey == 0) continue;
                        line.StartX = x + offset.X;
                        line.Grey = grey;
                        continue;
                    }
                    
                    if (grey == line.Grey) continue;
                    line.EndX = x - 1 + offset.X;
                    lines.Add(line);

                    line.Grey = 0;
                    x--;
                }

                if (line.Grey > 0)
                {
                    line.EndX = x - 1 + offset.X;
                    lines.Add(line);
                }
            }
        }

        return lines;
    }

    /// <summary>
    /// Scan sequential lines in X or Y direction
    /// </summary>
    /// <param name="mat">Mat to scan</param>
    /// <param name="greyFunc">Function to filter and process the gray value</param>
    /// <param name="vertically">True to scan vertically, otherwise horizontally</param>
    /// <param name="offset">Value to offset the coordinates with.</param>
    /// <returns>List of all lines</returns>
    public static List<GreyLine> ScanLines(this Mat mat, Func<byte, byte> greyFunc, bool vertically = false, Point offset = default)
    {
        Guard.IsEqualTo(mat.NumberOfChannels, 1);

        var lines = new List<GreyLine>();

        if (mat.IsEmpty) return lines;

        var matSize = mat.Size;

        GreyLine line = default;

        byte grey;
        int x;
        int y;

        if (vertically)
        {
            var span = mat.GetDataByteSpan2D();
            for (x = 0; x < matSize.Width; x++)
            {
                line.StartX = x + offset.X;
                line.StartY = 0 + offset.Y;
                line.EndX = x + offset.X;
                line.EndY = 0 + offset.Y;
                line.Grey = 0;

                for (y = 0; y < matSize.Height; y++)
                {
                    grey = greyFunc(span[y, x]);

                    if (line.Grey == 0)
                    {
                        if (grey == 0) continue;
                        line.StartY = y + offset.Y;
                        line.Grey = grey;
                        continue;
                    }

                    if (grey == line.Grey) continue;
                    line.EndY = y - 1 + offset.Y;
                    lines.Add(line);

                    line.Grey = 0;
                    y--;
                }

                if (line.Grey > 0)
                {
                    line.EndY = y - 1 + offset.Y;
                    lines.Add(line);
                }
            }
        }
        else
        {
            for (y = 0; y < matSize.Height; y++)
            {
                var span = mat.GetRowByteSpan(y);
                line.StartX = 0 + offset.X;
                line.StartY = y + offset.Y;
                line.EndX = 0 + offset.X;
                line.EndY = y + offset.Y;
                line.Grey = 0;


                for (x = 0; x < matSize.Width; x++)
                {
                    grey = greyFunc(span[x]);

                    if (line.Grey == 0)
                    {
                        if (grey == 0) continue;
                        line.StartX = x + offset.X;
                        line.Grey = grey;
                        continue;
                    }

                    if (grey == line.Grey) continue;
                    line.EndX = x - 1 + offset.X;
                    lines.Add(line);

                    line.Grey = 0;
                    x--;
                }

                if (line.Grey > 0)
                {
                    line.EndX = x - 1 + offset.X;
                    lines.Add(line);
                }
            }
        }

        return lines;
    }
    #endregion

    #region Transform methods
    public static void Transform(this Mat src, double xScale, double yScale, double xTrans = 0, double yTrans = 0, Size dstSize = default, Inter interpolation = Inter.Linear)
    {
        //var dst = new Mat(src.Size, src.Depth, src.NumberOfChannels);
        using var translateTransform = new Matrix<double>(2, 3)
        {
            [0, 0] = xScale, // xScale
            [1, 1] = yScale, // yScale
            [0, 2] = xTrans, //x translation + compensation of x scaling
            [1, 2] = yTrans // y translation + compensation of y scaling
        };
        CvInvoke.WarpAffine(src, src, translateTransform, dstSize.IsEmpty ? src.Size : dstSize, interpolation);
    }

    /// <summary>
    /// Rotates a Mat by an angle while keeping the image size
    /// </summary>
    /// <param name="src"></param>
    /// <param name="angle">Angle in degrees to rotate</param>
    /// <param name="newSize"></param>
    /// <param name="scale"></param>
    public static void Rotate(this Mat src, double angle, Size newSize = default, double scale = 1.0) => Rotate(src, src, angle, newSize, scale);

    /// <summary>
    /// Rotates a Mat by an angle while keeping the image size
    /// </summary>
    /// <param name="src"></param>
    /// <param name="dst"></param>
    /// <param name="angle"></param>
    /// <param name="newSize"></param>
    /// <param name="scale"></param>
    public static void Rotate(this Mat src, Mat dst, double angle, Size newSize = default, double scale = 1.0)
    {
        if (angle % 360 == 0 && Math.Abs(scale - 1.0) < 0.001) return;
        if (newSize.IsEmpty)
        {
            newSize = src.Size;
        }
            
        var halfWidth = src.Width / 2.0f;
        var halfHeight = src.Height / 2.0f;
        using var translateTransform = new Matrix<double>(2, 3);
        CvInvoke.GetRotationMatrix2D(new PointF(halfWidth, halfHeight), -angle, scale, translateTransform);

        if (src.Size != newSize)
        {
            // adjust the rotation matrix to take into account translation
            translateTransform[0, 2] += newSize.Width / 2.0 - halfWidth;
            translateTransform[1, 2] += newSize.Height / 2.0 - halfHeight;
        }

        CvInvoke.WarpAffine(src, dst, translateTransform, newSize);
    }

    /// <summary>
    /// Rotates a Mat by an angle while adjusting bounds to fit the rotated content
    /// </summary>
    /// <param name="src"></param>
    /// <param name="angle"></param>
    /// <param name="scale"></param>
    public static void RotateAdjustBounds(this Mat src, double angle, double scale = 1.0) => RotateAdjustBounds(src, src, angle, scale);

    /// <summary>
    /// Rotates a Mat by an angle while adjusting bounds to fit the rotated content
    /// </summary>
    /// <param name="src"></param>
    /// <param name="dst"></param>
    /// <param name="angle"></param>
    /// <param name="scale"></param>
    public static void RotateAdjustBounds(this Mat src, Mat dst, double angle, double scale = 1.0)
    {
        if (angle % 360 == 0 && Math.Abs(scale - 1.0) < 0.001) return;
        var halfWidth = src.Width / 2.0f;
        var halfHeight = src.Height / 2.0f;
        using var translateTransform = new Matrix<double>(2, 3);
        CvInvoke.GetRotationMatrix2D(new PointF(halfWidth, halfHeight), -angle, scale, translateTransform);
        var cos = Math.Abs(translateTransform[0, 0]);
        var sin = Math.Abs(translateTransform[0, 1]);

        // compute the new bounding dimensions of the image
        int newWidth = (int) (src.Height * sin + src.Width * cos);
        int newHeight = (int) (src.Height * cos + src.Width * sin);

        // adjust the rotation matrix to take into account translation
        translateTransform[0, 2] += newWidth / 2.0 - halfWidth;
        translateTransform[1, 2] += newHeight / 2.0 - halfHeight;


        CvInvoke.WarpAffine(src, dst, translateTransform, new Size(newWidth, newHeight));
    }

    /// <summary>
    /// Scale image from it center, preserving src bounds
    /// https://stackoverflow.com/a/62543674/933976
    /// </summary>
    /// <param name="src"><see cref="Mat"/> to transform</param>
    /// <param name="xScale">X scale factor</param>
    /// <param name="yScale">Y scale factor</param>
    /// <param name="xTrans">X translation</param>
    /// <param name="yTrans">Y translation</param>
    /// <param name="dstSize">Destination size</param>
    /// <param name="interpolation">Interpolation mode</param>
    public static void TransformFromCenter(this Mat src, double xScale, double yScale, double xTrans = 0, double yTrans = 0, Size dstSize = default, Inter interpolation = Inter.Linear)
    {
        src.Transform(xScale, yScale,
            xTrans + (src.Width - src.Width * xScale) / 2.0, 
            yTrans + (src.Height - src.Height * yScale) / 2.0, dstSize, interpolation);
    }

    /// <summary>
    /// Resize source mat proportional to a scale
    /// </summary>
    /// <param name="src"></param>
    /// <param name="scale"></param>
    /// <param name="interpolation"></param>
    public static void Resize(this Mat src, double scale, Inter interpolation = Inter.Linear)
    {
        if (Math.Abs(scale - 1) < 0.001) return;
        CvInvoke.Resize(src, src, new Size((int) (src.Width * scale), (int) (src.Height * scale)), 0, 0, interpolation);
    }
    #endregion

    #region Draw Methods

    /// <summary>
    /// Correct openCV thickness which always results larger than specified
    /// </summary>
    /// <param name="thickness">Thickness to correct</param>
    /// <returns></returns>
    public static int CorrectThickness(int thickness)
    {
        if (thickness < 3) return thickness;
        return thickness - 1;
    }

    public static void DrawLineAccurate(this Mat src, Point pt1, Point pt2, MCvScalar color, int thickness, LineType lineType = LineType.EightConnected)
    {
        /*var deltaX = pt2.X - pt1.X;
        var deltaY = pt2.Y - pt1.Y;
        var deg = Math.Atan2(deltaY, deltaX) * (180 / Math.PI); 
        src.DrawRotatedRectangle(
            new Size(Math.Abs(deltaX), thickness), 
            new Point(pt1.X + deltaX / 2, pt1.Y + deltaY / 2), 
            color, (int)deg, -1, lineType);*/

        if (thickness >= 3)
        {
            thickness--;
            /*var lastNumber = thickness % 10;
            switch (lastNumber)
            {
                case 1:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                    thickness--;
                    break;
            }*/
        }

        CvInvoke.Line(src, pt1, pt2, color, thickness, lineType);
    }

    /// <summary>
    /// Draw a rotated square around a center point
    /// </summary>
    /// <param name="src"></param>
    /// <param name="size"></param>
    /// <param name="center"></param>
    /// <param name="color"></param>
    /// <param name="angle"></param>
    /// <param name="thickness"></param>
    /// <param name="lineType"></param>
    public static void DrawRotatedSquare(this Mat src, int size, Point center, MCvScalar color, int angle = 0, int thickness = -1, LineType lineType = LineType.EightConnected)
        => src.DrawRotatedRectangle(new(size, size), center, color, angle, thickness, lineType);

    /// <summary>
    /// Draw a rotated rectangle around a center point
    /// </summary>
    /// <param name="src"></param>
    /// <param name="size"></param>
    /// <param name="center"></param>
    /// <param name="color"></param>
    /// <param name="angle"></param>
    /// <param name="thickness"></param>
    /// <param name="lineType"></param>
    public static void DrawRotatedRectangle(this Mat src, Size size, Point center, MCvScalar color, int angle = 0, int thickness = -1, LineType lineType = LineType.EightConnected)
    {
        if (angle == 0)
        {
            src.DrawCenteredRectangle(size, center, color, thickness, lineType);
            return;
        }

        var rect = new RotatedRect(center, size, angle);
        var vertices = rect.GetVertices();
        var points = new Point[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            points[i] = new(
                (int)Math.Round(vertices[i].X), 
                (int)Math.Round(vertices[i].Y)
            );
        }

        if (thickness <= 0)
        {
            using var vec = new VectorOfPoint(points);
            CvInvoke.FillConvexPoly(src, vec, color, lineType);
        }
        else
        {
            CvInvoke.Polylines(src, points, true, color, thickness, lineType);
        }
    }

    /// <summary>
    /// Draw a square around a center point
    /// </summary>
    /// <param name="src"></param>
    /// <param name="size"></param>
    /// <param name="center"></param>
    /// <param name="color"></param>
    /// <param name="thickness"></param>
    /// <param name="lineType"></param>
    public static void DrawCenteredSquare(this Mat src, int size, Point center, MCvScalar color, int thickness = -1, LineType lineType = LineType.EightConnected)
        => src.DrawCenteredRectangle(new Size(size, size), center, color, thickness, lineType);

    /// <summary>
    /// Draw a rectangle around a center point
    /// </summary>
    /// <param name="src"></param>
    /// <param name="size"></param>
    /// <param name="center"></param>
    /// <param name="color"></param>
    /// <param name="thickness"></param>
    /// <param name="lineType"></param>
    public static void DrawCenteredRectangle(this Mat src, Size size, Point center, MCvScalar color, int thickness = -1, LineType lineType = LineType.EightConnected)
    {
        CvInvoke.Rectangle(src, new Rectangle(center.OffsetBy(size.Width / -2, size.Height / -2), size), color, thickness, lineType);
    }

    /// <summary>
    /// Draw a polygon given number of sides and diameter
    /// </summary>
    /// <param name="src"></param>
    /// <param name="sides">Number of polygon sides, Special: use 1 to draw a line and >= 100 to draw a native OpenCV circle</param>
    /// <param name="diameter">Diameter</param>
    /// <param name="center">Center position</param>
    /// <param name="color"></param>
    /// <param name="startingAngle"></param>
    /// <param name="thickness"></param>
    /// <param name="lineType"></param>
    /// <param name="flip"></param>
    /// <param name="midpointRounding"></param>
    public static void DrawPolygon(this Mat src, int sides, double diameter, PointF center, MCvScalar color, double startingAngle = 0, int thickness = -1, LineType lineType = LineType.EightConnected, FlipType? flip = null, MidpointRounding midpointRounding = MidpointRounding.AwayFromZero)
    {
        if (sides == 1)
        {
            var point1 = center with { X = (float)Math.Round(center.X - diameter / 2, midpointRounding) };
            var point2 = point1 with { X = (float)(point1.X + diameter - 1) };
            point1 = point1.Rotate(startingAngle, center);
            point2 = point2.Rotate(startingAngle, center);

            if (flip is FlipType.Horizontal or FlipType.Both)
            {
                var newPoint1 = new PointF(point2.X, point1.Y);
                var newPoint2 = new PointF(point1.X, point2.Y);
                point1 = newPoint1;
                point2 = newPoint2;
            }

            if (flip is FlipType.Vertical or FlipType.Both)
            {
                var newPoint1 = new PointF(point1.X, point2.Y);
                var newPoint2 = new PointF(point2.X, point1.Y);
                point1 = newPoint1;
                point2 = newPoint2;
            }

            CvInvoke.Line(src, point1.ToPoint(midpointRounding), point2.ToPoint(midpointRounding), color, thickness < 1 ? 1 : thickness, lineType);
            return;
        }
        if (sides >= 100)
        {
            CvInvoke.Circle(src, center.ToPoint(midpointRounding), (int)Math.Round(diameter / 2, midpointRounding), color, thickness, lineType);
            return;
        }

        var points = DrawingExtensions.GetPolygonVertices(sides, diameter, center, startingAngle,
            flip is FlipType.Horizontal or FlipType.Both,
            flip is FlipType.Vertical or FlipType.Both,
            midpointRounding);
            
        if (thickness <= 0)
        {
            using var vec = new VectorOfPoint(points);
            CvInvoke.FillConvexPoly(src, vec, color, lineType);
        }
        else
        {
            CvInvoke.Polylines(src, points, true, color, thickness, lineType);
        }
            
    }
    #endregion

    #region Text methods
    public enum PutTextLineAlignment : byte
    {
        /// <summary>
        /// Left aligned without trimming, openCV default call
        /// </summary>
        None,

        /// <summary>
        /// Left aligned and trimmed
        /// </summary>
        Left,

        /// <summary>
        /// Center aligned and trimmed
        /// </summary>
        Center,

        /// <summary>
        /// Right aligned and trimmed
        /// </summary>
        Right
    }

    public static string PutTextLineAlignmentTrim(string line, PutTextLineAlignment lineAlignment)
    {
        switch (lineAlignment)
        {
            case PutTextLineAlignment.None:
                return line.TrimEnd();
            case PutTextLineAlignment.Left:
            case PutTextLineAlignment.Center:
            case PutTextLineAlignment.Right:
                return line.Trim();
            default:
                throw new ArgumentOutOfRangeException(nameof(lineAlignment), lineAlignment, null);
        }
    }

    public static Size GetTextSizeExtended(string text, FontFace fontFace, double fontScale, int thickness, ref int baseLine, PutTextLineAlignment lineAlignment = default)
        => GetTextSizeExtended(text, fontFace, fontScale, thickness, 0, ref baseLine, lineAlignment);
    public static Size GetTextSizeExtended(string text, FontFace fontFace, double fontScale, int thickness, int lineGapOffset, ref int baseLine, PutTextLineAlignment lineAlignment = default)
    {
        text = text.TrimEnd('\n', '\r', ' ');
        var lines = text.Split(StaticObjects.LineBreakCharacters, StringSplitOptions.None);
        var textSize = CvInvoke.GetTextSize(text, fontFace, fontScale, thickness, ref baseLine);

        if (lines.Length is 0 or 1) return textSize;

        var lineGap = textSize.Height / 3 + lineGapOffset;
        var width = 0;
        var height = lines.Length * (lineGap + textSize.Height) - lineGap;

        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = PutTextLineAlignmentTrim(lines[i], lineAlignment);

            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            int baseLineRef = 0;
            var lineSize = CvInvoke.GetTextSize(lines[i], fontFace, fontScale, thickness, ref baseLineRef);
            width = Math.Max(width, lineSize.Width);
        }


        return new(width, height);
    }

    public static void PutTextExtended(this IInputOutputArray src, string text, Point org, FontFace fontFace, double fontScale,
        MCvScalar color, int thickness = 1, LineType lineType = LineType.EightConnected,
        bool bottomLeftOrigin = false, PutTextLineAlignment lineAlignment = default)
        => src.PutTextExtended(text, org, fontFace, fontScale, color, thickness, 0, lineType, bottomLeftOrigin, lineAlignment);

    /// <summary>
    /// Extended OpenCV PutText to accepting line breaks and line alignment
    /// </summary>
    public static void PutTextExtended(this IInputOutputArray src, string text, Point org, FontFace fontFace, double fontScale,
        MCvScalar color, int thickness = 1, int lineGapOffset = 0, LineType lineType = LineType.EightConnected, bool bottomLeftOrigin = false, PutTextLineAlignment lineAlignment = default)
    {
        text = text.TrimEnd('\n', '\r', ' ');
        var lines = text.Split(StaticObjects.LineBreakCharacters, StringSplitOptions.None);
            
        switch (lines.Length)
        {
            case 0:
                return;
            case 1:
                CvInvoke.PutText(src, text, org, fontFace, fontScale, color, thickness, lineType, bottomLeftOrigin);
                return;
        }

        // Get height of text lines in pixels (height of all lines is the same)
        int baseLine = 0;
        var textSize = CvInvoke.GetTextSize(text, fontFace, fontScale, thickness, ref baseLine);
        var lineGap = textSize.Height / 3 + lineGapOffset;
        var linesSize = new Size[lines.Length];
        int width = 0;

        // Sanitize lines
        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = PutTextLineAlignmentTrim(lines[i], lineAlignment);
        }

        // If line needs alignment, calculate the size for each line
        if (lineAlignment is not PutTextLineAlignment.Left and not PutTextLineAlignment.None)
        {
            for (var i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                int baseLineRef = 0;
                linesSize[i] = CvInvoke.GetTextSize(lines[i], fontFace, fontScale, thickness, ref baseLineRef);
                width = Math.Max(width, linesSize[i].Width);
            }
        }

        for (var i = 0; i < lines.Length; i++)
        {
            if(string.IsNullOrWhiteSpace(lines[i])) continue;
                
            int x = lineAlignment switch
            {
                PutTextLineAlignment.None or PutTextLineAlignment.Left => org.X,
                PutTextLineAlignment.Center => org.X + (width - linesSize[i].Width) / 2,
                PutTextLineAlignment.Right => org.X + width - linesSize[i].Width,
                _ => throw new ArgumentOutOfRangeException(nameof(lineAlignment), lineAlignment, null)
            };

            // Find total size of text block before this line
            var lineYAdjustment = i * (lineGap + textSize.Height);
            // Move text down from original line based on line number
            int lineY = !bottomLeftOrigin ? org.Y + lineYAdjustment : org.Y - lineYAdjustment;
            CvInvoke.PutText(src, lines[i], new Point(x, lineY), fontFace, fontScale, color, thickness, lineType, bottomLeftOrigin);
        }
    }

    /// <summary>
    /// Extended OpenCV PutText to accepting line breaks, line alignment and rotation
    /// </summary>
    public static void PutTextRotated(this Mat src, string text, Point org, FontFace fontFace, double fontScale,
        MCvScalar color,
        int thickness = 1, LineType lineType = LineType.EightConnected, bool bottomLeftOrigin = false,
        PutTextLineAlignment lineAlignment = default, double angle = 0)
        => src.PutTextRotated(text, org, fontFace, fontScale, color, thickness, 0, lineType, bottomLeftOrigin,
            lineAlignment, angle);

    /// <summary>
    /// Extended OpenCV PutText to accepting line breaks, line alignment and rotation
    /// </summary>
    public static void PutTextRotated(this Mat src, string text, Point org, FontFace fontFace, double fontScale, MCvScalar color,
        int thickness = 1, int lineGapOffset = 0, LineType lineType = LineType.EightConnected, bool bottomLeftOrigin = false, PutTextLineAlignment lineAlignment = default, double angle = 0)
    {
        if (angle % 360 == 0) // No rotation needed, cheaper cycle
        {
            src.PutTextExtended(text, org, fontFace, fontScale, color, thickness, lineGapOffset, lineType, bottomLeftOrigin, lineAlignment);
            return;
        }

        using var rotatedSrc = src.Clone();
        rotatedSrc.RotateAdjustBounds(-angle);
        var sizeDifference = (rotatedSrc.Size - src.Size).Half();
        org.Offset(sizeDifference.ToPoint());
        org = org.Rotate(-angle, new Point(rotatedSrc.Size.Width / 2, rotatedSrc.Size.Height / 2));
        rotatedSrc.PutTextExtended(text, org, fontFace, fontScale, color, thickness, lineGapOffset, lineType, bottomLeftOrigin, lineAlignment);
   
        using var mask = rotatedSrc.NewZeros();
        mask.PutTextExtended(text, org, fontFace, fontScale, WhiteColor, thickness, lineGapOffset, lineType, bottomLeftOrigin, lineAlignment);

        rotatedSrc.Rotate(angle, src.Size);
        mask.Rotate(angle, src.Size);

        rotatedSrc.CopyTo(src, mask);
    }
    #endregion

    #region Other Images Types

    /// <summary>
    /// From <paramref name="src"/> gets the SVG path's. Tags are not included.
    /// </summary>
    /// <param name="src"></param>
    /// <param name="compression">Compression method for the contours</param>
    /// <param name="threshold">True to binary threshold first</param>
    /// <returns>Array of path's</returns>
    public static IEnumerable<string> GetSvgPath(this Mat src, ChainApproxMethod compression = ChainApproxMethod.ChainApproxSimple, bool threshold = true)
    {
        var mat = src;
        if (threshold)
        {
            mat = new();
            CvInvoke.Threshold(src, mat, 127, byte.MaxValue, ThresholdType.Binary);
        }

        using var contours = mat.FindContours(out var hierarchy, RetrType.Tree, compression);

        var sb = new StringBuilder();
        for (int i = 0; i < contours.Size; i++)
        {
            if (hierarchy[i, EmguContour.HierarchyParent] == -1) // Top hierarchy
            {
                if (sb.Length > 0)
                {
                    yield return sb.ToString();
                    sb.Clear();
                }
            }
            else
            {
                sb.Append(" ");
            }

            sb.Append($"M {contours[i][0].X} {contours[i][0].Y} L");
            for (int x = 1; x < contours[i].Size; x++)
            {
                sb.Append($" {contours[i][x].X} {contours[i][x].Y}");
            }
            sb.Append(" Z");
        }

        if (sb.Length > 0)
        {
            yield return sb.ToString();
            sb.Clear();
        }

        if (!ReferenceEquals(src, mat)) mat.Dispose();
    }
    #endregion

    #region Utilities methods

    /// <summary>
    /// Retrieves contours from the binary image as a contour tree. The pointer firstContour is filled by the function. It is provided as a convenient way to obtain the hierarchy value as int[,].
    /// The function modifies the source image content
    /// </summary>
    /// <param name="mat">The source 8-bit single channel image. Non-zero pixels are treated as 1s, zero pixels remain 0s - that is image treated as binary. To get such a binary image from grayscale, one may use cvThreshold, cvAdaptiveThreshold or cvCanny. The function modifies the source image content</param>
    /// <param name="mode">Retrieval mode</param>
    /// <param name="method">Approximation method (for all the modes, except CV_RETR_RUNS, which uses built-in approximation). </param>
    /// <param name="offset">Offset, by which every contour point is shifted. This is useful if the contours are extracted from the image ROI and then they should be analyzed in the whole image context</param>
    /// <returns>The contour hierarchy</returns>
    public static VectorOfVectorOfPoint FindContours(this IInputOutputArray mat, RetrType mode = RetrType.List, ChainApproxMethod method = ChainApproxMethod.ChainApproxSimple, Point offset = default)
    {
        using var hierarchy = new Mat();
        var contours = new VectorOfVectorOfPoint();
        CvInvoke.FindContours(mat, contours, hierarchy, mode, method, offset);
        return contours;
    }

    /*
    /// <summary>
    /// Retrieves contours from the binary image as a contour tree. The pointer firstContour is filled by the function. It is provided as a convenient way to obtain the hierarchy value as int[,].
    /// The function modifies the source image content
    /// </summary>
    /// <param name="mat">The source 8-bit single channel image. Non-zero pixels are treated as 1s, zero pixels remain 0s - that is image treated as binary. To get such a binary image from grayscale, one may use cvThreshold, cvAdaptiveThreshold or cvCanny. The function modifies the source image content</param>
    /// <param name="contours">Detected contours. Each contour is stored as a vector of points.</param>
    /// <param name="mode">Retrieval mode</param>
    /// <param name="method">Approximation method (for all the modes, except CV_RETR_RUNS, which uses built-in approximation). </param>
    /// <param name="offset">Offset, by which every contour point is shifted. This is useful if the contours are extracted from the image ROI and then they should be analyzed in the whole image context</param>
    /// <returns>The contour hierarchy</returns>
    public static int[,] FindContours(this Mat mat, IOutputArray contours, RetrType mode, ChainApproxMethod method = ChainApproxMethod.ChainApproxSimple, Point offset = default)
    {
        using var hierarchy = new Mat();
        CvInvoke.FindContours(mat, contours, hierarchy, mode, method, offset);
        var numArray = new int[hierarchy.Cols, 4];
        var gcHandle = GCHandle.Alloc(numArray, GCHandleType.Pinned);
        using (var mat2 = new Mat(hierarchy.Rows, hierarchy.Cols, hierarchy.Depth, 4, gcHandle.AddrOfPinnedObject(), hierarchy.Step))
            hierarchy.CopyTo(mat2);
        gcHandle.Free();
        return numArray;
    }*/

    /// <summary>
    /// Retrieves contours from the binary image as a contour tree. The pointer firstContour is filled by the function. It is provided as a convenient way to obtain the hierarchy value as int[,].
    /// The function modifies the source image content
    /// </summary>
    /// <param name="mat">The source 8-bit single channel image. Non-zero pixels are treated as 1s, zero pixels remain 0s - that is image treated as binary. To get such a binary image from grayscale, one may use cvThreshold, cvAdaptiveThreshold or cvCanny. The function modifies the source image content</param>
    /// <param name="hierarchy">The contour hierarchy</param>
    /// <param name="mode">Retrieval mode</param>
    /// <param name="method">Approximation method (for all the modes, except CV_RETR_RUNS, which uses built-in approximation). </param>
    /// <param name="offset">Offset, by which every contour point is shifted. This is useful if the contours are extracted from the image ROI and then they should be analyzed in the whole image context</param>
    /// <returns>Detected contours. Each contour is stored as a vector of points.</returns>
    public static VectorOfVectorOfPoint FindContours(this IInputOutputArray mat, out int[,] hierarchy, RetrType mode, ChainApproxMethod method = ChainApproxMethod.ChainApproxSimple, Point offset = default)
    {
        var contours = new VectorOfVectorOfPoint();
        using var hierarchyMat = new Mat();
        
        CvInvoke.FindContours(mat, contours, hierarchyMat, mode, method, offset);
        
        hierarchy = new int[hierarchyMat.Cols, 4];
        if (contours.Size == 0) return contours;
        var gcHandle = GCHandle.Alloc(hierarchy, GCHandleType.Pinned);
        using (var mat2 = new Mat(hierarchyMat.Rows, hierarchyMat.Cols, hierarchyMat.Depth, 4, gcHandle.AddrOfPinnedObject(), hierarchyMat.Step))
            hierarchyMat.CopyTo(mat2);
        gcHandle.Free();
        return contours;
    }


    /// <summary>
    /// Determine the area (i.e. total number of pixels in the image),
    /// initialize the output skeletonized image, and construct the
    /// morphological structuring element
    /// </summary>
    /// <param name="src"></param>
    /// <param name="iterations">Number of iterations required to perform the skeletoize</param>
    /// <param name="ksize"></param>
    /// <param name="elementShape"></param>
    /// <param name="cancellationToken"></param>
    public static Mat Skeletonize(this Mat src, out int iterations, Size ksize = default, ElementShape elementShape = ElementShape.Rectangle, CancellationToken cancellationToken = default)
    {
        if (ksize.IsEmpty) ksize = new Size(3, 3);
        var skeleton = src.NewZeros();
        using var kernel = CvInvoke.GetStructuringElement(elementShape, ksize, AnchorCenter);

        var image = src;
        using var temp = new Mat();
        iterations = 0;
        using var eroded = new Mat();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            iterations++;

            // erode and dilate the image using the structuring element
            CvInvoke.Erode(image, eroded, kernel, AnchorCenter, 1, BorderType.Reflect101, default);
            CvInvoke.Dilate(eroded, temp, kernel, AnchorCenter, 1, BorderType.Reflect101, default);

            // subtract the temporary image from the original, eroded
            // image, then take the bitwise 'or' between the skeleton
            // and the temporary image
            CvInvoke.Subtract(image, temp, temp);
            CvInvoke.BitwiseOr(skeleton, temp, skeleton);

            if (iterations > 1) image.Dispose();

            // if there are no more 'white' pixels in the image, then
            // break from the loop
            if (!CvInvoke.HasNonZero(eroded)) break;

            image = eroded.Clone();
        }

        return skeleton;
    }

    /// <summary>
    /// Determine the area (i.e. total number of pixels in the image),
    /// initialize the output skeletonized image, and construct the
    /// morphological structuring element
    /// </summary>
    /// <param name="src"></param>
    /// <param name="ksize"></param>
    /// <param name="elementShape"></param>
    /// <param name="cancellationToken"></param>
    public static Mat Skeletonize(this Mat src, Size ksize = default, ElementShape elementShape = ElementShape.Rectangle, CancellationToken cancellationToken = default)
        => src.Skeletonize(out _, ksize, elementShape, cancellationToken);
    #endregion

    #region Kernel methods

    /// <summary>
    /// Reduces iterations to 1 and generate a kernel to match the iterations effect
    /// </summary>
    /// <param name="iterations"></param>
    /// <param name="elementShape"></param>
    /// <returns></returns>
    public static Mat GetDynamicKernel(ref int iterations, ElementShape elementShape = ElementShape.Ellipse)
    {
        var size = Math.Max(iterations, 1) * 2 + 1;
        iterations = 1;
        return CvInvoke.GetStructuringElement(elementShape, new Size(size, size), AnchorCenter);
    }
    #endregion

    #region Disposes
    /// <summary>
    /// Dispose this <see cref="Mat"/> if it's a sub matrix / roi
    /// </summary>
    /// <param name="mat">Mat to dispose</param>
    public static void DisposeIfSubMatrix(this Mat mat)
    {
        if(mat.IsSubmatrix) mat.Dispose();
    }

    /// <summary>
    /// Dispose this <see cref="Mat"/> if it's not the same reference as <paramref name="otherMat"/>
    /// </summary>
    /// <param name="mat">Mat to dispose</param>
    /// <param name="otherMat"></param>
    public static void DisposeIfNot(this Mat mat, Mat otherMat)
    {
        if (!ReferenceEquals(mat, otherMat)) mat.Dispose();
    }
    #endregion
}