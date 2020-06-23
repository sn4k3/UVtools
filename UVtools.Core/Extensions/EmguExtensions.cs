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
using Emgu.CV.Structure;

namespace UVtools.Core.Extensions
{
    public static class EmguExtensions
    {
        public static unsafe Span<T> GetPixelSpan<T>(this Mat mat)
        {
            return new Span<T>(mat.DataPointer.ToPointer(), mat.GetLength());
        }

        public static Span<T> GetPixelRowSpan<T>(this Mat mat, int y)
        {
            int offset = y * mat.Step;
            //IntPtr ptr = IntPtr.Add(mat.DataPointer, offset);
            return mat.GetPixelSpan<T>().Slice(offset, mat.Step);
        }

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

        public static int GetLength(this Mat mat)
        {
            return mat.Step * mat.Height;
        }

        public static int GetPixelPos(this Mat mat, int x, int y)
        {
            return y * mat.Step + x * mat.NumberOfChannels;
        }

        public static int GetPixelPos(this Mat mat, Point point)
        {
            return point.Y * mat.Step + point.X * mat.NumberOfChannels;
        }

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

        public static byte[] GetBytesBlank(this Mat mat)
        {
            return new byte[mat.GetLength()];
        }

        public static Mat CloneBlank(this Mat mat)
        {
            return new Mat(new Size(mat.Width, mat.Height), mat.Depth, mat.NumberOfChannels);
        }

        public static byte GetByte(this Mat mat, int pos)
        {
            var value = new byte[1];
            Marshal.Copy(mat.DataPointer + pos * mat.ElementSize, value, 0, value.Length);
            return value[0];
        }

        public static byte GetByte(this Mat mat, int x, int y) => GetByte(mat, y * mat.Cols + x);


        public static void SetByte(this Mat mat, int pixel, byte value) => SetByte(mat, pixel, new[] {value});

        public static void SetByte(this Mat mat, int pixel, byte[] value)
        {
            Marshal.Copy(value, 0, mat.DataPointer + pixel * mat.ElementSize, value.Length);
        }

        public static void SetByte(this Mat mat, int x, int y, byte value) =>
            SetByte(mat, x, y, new[] {value});

        public static void SetByte(this Mat mat, int x, int y, byte[] value) =>
            SetByte(mat, y * mat.Cols + x, value);

        public static void SetBytes(this Mat mat, byte[] value)
        {
            Marshal.Copy(value, 0, mat.DataPointer, value.Length);
        }

    }
}
