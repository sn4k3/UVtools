/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace UVtools.Core.Extensions
{
    public static class EmguExtensions
    {
        public static readonly MCvScalar BlackByte = new MCvScalar(0);

        public static unsafe byte* GetBytePointer(this Mat mat)
        {
            return (byte*)mat.DataPointer.ToPointer();
        }

        /// <summary>
        /// Gets a single pixel span to manipulate or read pixels
        /// </summary>
        /// <typeparam name="T">Pixel type</typeparam>
        /// <param name="mat"><see cref="Mat"/> Input</param>
        /// <returns>A <see cref="Span{T}"/> containing all pixels in data memory</returns>
        public static unsafe Span<T> GetPixelSpan<T>(this Mat mat)
        {
            return new Span<T>(mat.DataPointer.ToPointer(), mat.GetLength());
        }

        public static unsafe Span<T> GetPixelSpan<T>(this Mat mat, int length, int offset = 0)
        {
            return new Span<T>(IntPtr.Add(mat.DataPointer, offset).ToPointer(), length);
        }

        public static Span<T> GetSinglePixelSpan<T>(this Mat mat, int x, int y)
        {
            return mat.GetPixelSpan<T>(mat.NumberOfChannels, mat.GetPixelPos(x, y));
        }

        public static Span<T> GetSinglePixelSpan<T>(this Mat mat, int pos)
        {
            return mat.GetPixelSpan<T>(mat.NumberOfChannels, pos);
        }


        public static unsafe Span<T> GetPixelRowSpan<T>(this Mat mat, int y, int length = 0, int offset = 0)
        {
            return new Span<T>(IntPtr.Add(mat.DataPointer, y * mat.Step + offset).ToPointer(), length == 0 ? mat.Step : length);
            //return mat.GetPixelSpan<T>().Slice(offset, mat.Step);
        }

        public static void Transform(this Mat src, double xScale, double yScale, double xTrans = 0, double yTrans = 0, Size dstSize = default, Inter interpolation = Inter.Linear)
        {
            //var dst = new Mat(src.Size, src.Depth, src.NumberOfChannels);
            using (var translateTransform = new Matrix<double>(2, 3)
            {
                [0, 0] = xScale, // xScale
                [1, 1] = yScale, // yScale
                [0, 2] = xTrans, //x translation + compensation of x scaling
                [1, 2] = yTrans // y translation + compensation of y scaling
            })
            {
                CvInvoke.WarpAffine(src, src, translateTransform, dstSize.IsEmpty ? src.Size : dstSize, interpolation);
            }
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
        /// Copy a <see cref="Mat"/> to center of other <see cref="Mat"/>
        /// </summary>
        /// <param name="src">Source <see cref="Mat"/> to be copied to</param>
        /// <param name="dst">Target <see cref="Mat"/> to paste the <param name="src"></param></param>
        public static void CopyToCenter(this Mat src, Mat dst)
        {
            if (dst.Step > src.Step && dst.Height > src.Height)
            {
                var dx = Math.Abs(dst.Step - src.Step) / 2;
                var dy = Math.Abs(dst.Height - src.Height) / 2;
                Mat m = new Mat(dst, new Rectangle(dx, dy, src.Width, src.Height));
                src.CopyTo(m);
            }
            else if (dst.Step < src.Step && dst.Height < src.Height)
            {
                var dx = Math.Abs(dst.Step - src.Step) / 2;
                var dy = Math.Abs(dst.Height - src.Height) / 2;
                Mat m = new Mat(src, new Rectangle(dx, dy, dst.Width, dst.Height));
                m.CopyTo(dst);
            }
        }

        /// <summary>
        /// Gets the total length of this <see cref="Mat"/></param>
        /// </summary>
        /// <param name="mat"></param>
        /// <returns>The total length of this <see cref="Mat"/></returns>
        public static int GetLength(this Mat mat)
        {
            return mat.Step * mat.Height;
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
            return y * mat.Step + x * mat.NumberOfChannels;
        }

        /// <summary>
        /// Gets a pixel index position on a span given X and Y
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="point">X and Y Location</param>
        /// <returns>The pixel index position</returns>
        public static int GetPixelPos(this Mat mat, Point point)
        {
            return point.Y * mat.Step + point.X * mat.NumberOfChannels;
        }

        /// <summary>
        /// Gets a byte array copy of this <see cref="Mat"/>
        /// </summary>
        /// <param name="mat"></param>
        /// <returns>Byte array </returns>
        public static byte[] GetBytes(this Mat mat)
        {
            byte[] data = new byte[mat.GetLength()];
            Marshal.Copy(mat.DataPointer, data, 0, data.Length);
            return data;

            /*GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            using (Mat m2 = new Mat(mat.Size, mat.Depth, mat.NumberOfChannels, handle.AddrOfPinnedObject(),
                mat.Width * mat.NumberOfChannels))
            {
                mat.CopyTo(m2);
            }
            handle.Free();*/

        }

        /// <summary>
        /// Create a byte array of size of this <see cref="Mat"/>
        /// </summary>
        /// <param name="mat"></param>
        /// <returns>Blank byte array</returns>
        public static byte[] CreateByteArray(this Mat mat)
        {
            return new byte[mat.GetLength()];
        }

        /// <summary>
        /// Clone this <see cref="Mat"/> blanked (All zeros)
        /// </summary>
        /// <param name="mat"></param>
        /// <returns>Blanked <see cref="Mat"/></returns>
        public static Mat CloneBlank(this Mat mat)
        {
            return InitMat(mat.Size, mat.Depth, mat.NumberOfChannels);
        }

        public static byte GetByte(this Mat mat, int pos)
        {
            //return new Span<byte>(IntPtr.Add(mat.DataPointer, pos).ToPointer(), mat.Step)[0];
            var value = new byte[1];
            Marshal.Copy(mat.DataPointer + pos, value, 0, value.Length);
            return value[0];
        }

        public static byte GetByte(this Mat mat, int x, int y) => GetByte(mat, mat.GetPixelPos(x, y));
        public static byte GetByte(this Mat mat, Point pos) => GetByte(mat, mat.GetPixelPos(pos.X, pos.Y));


        public static void SetByte(this Mat mat, int pixel, byte value) => SetByte(mat, pixel, new[] {value});

        public static void SetByte(this Mat mat, int pixel, byte[] value) =>
            Marshal.Copy(value, 0, mat.DataPointer + pixel, value.Length);

        public static void SetByte(this Mat mat, int x, int y, byte value) =>
            SetByte(mat, x, y, new[] {value});

        public static void SetByte(this Mat mat, int x, int y, byte[] value) =>
            SetByte(mat, y * mat.Step + x * mat.NumberOfChannels, value);

        public static void SetBytes(this Mat mat, byte[] value) =>
            Marshal.Copy(value, 0, mat.DataPointer, value.Length);

        public static Mat InitMat(Size size, DepthType depthType = DepthType.Cv8U, int channels = 1)
        {
            var mat = new Mat(size, depthType, channels);
            mat.SetTo(BlackByte);
            return mat;
        }

    }
}
