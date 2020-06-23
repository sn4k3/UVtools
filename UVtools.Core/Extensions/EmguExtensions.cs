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
            /*
             * // Create rect representing the image
             *
            auto image_rect = cv::Rect({}, image.size());

            // Find intersection, i.e. valid crop region
            auto intersection = image_rect & roi;

            // Move intersection to the result coordinate space
            auto inter_roi = intersection - roi.tl();

            // Create black image and copy intersection
            cv::Mat crop = cv::Mat::zeros(roi.size(), image.type());
            image(intersection).copyTo(crop(inter_roi));

            */
            //var spanSrc = src.GetPixelSpan<byte>();
            //var spanDst = dst.GetPixelSpan<byte>();

            /*var dx = (dst.Step - src.Step) / 2;
            var dy = (dst.Height - src.Height) / 2;
            // Find intersection, i.e. valid crop region
            var image_rect = new Rectangle(0, 0, src.Width, src.Height);
            var intersection = new Rectangle(0, 0, dst.Width, dst.Height);
            var roi = new Rectangle(dx, dy, src.Width, src.Height);

            intersection.Intersect(roi);

            var inter_roi = new Rectangle(intersection.Location, intersection.Size);
            inter_roi.Offset(new Point(roi.Left, roi.Top));

            //Mat crop = new Mat(roi.Size, dst.Depth, dst.NumberOfChannels);
            //dst = crop
            Mat destRoi = new Mat(dst, inter_roi);
            Mat intersectionRoi = new Mat(src, intersection);
            intersectionRoi.CopyTo(destRoi);

            dst = destRoi;*/

            /*if (dst.Step > src.Step && dst.Height > src.Height)
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
            }*/

            

            // Solution from George Kerwood, need same XY factor
            // https://stackoverflow.com/questions/62524257/c-sharp-emgucv-resize-mat-but-keep-bounds-resolution/
            /*if (spanDst.Length < spanSrc.Length) 
            {
                for (int y = 0; y < dst.Height; y++)
                {
                    for (int x = 0; x < dst.Step; x++)
                    {
                        spanDst[dst.GetPixelPos(x, y)] = spanSrc[src.GetPixelPos(x + dx, y + dy)];
                    }
                }
            }
            else
            {
                for (int y = 0; y < src.Height; y++)
                {
                    for (int x = 0; x < src.Step; x++)
                    {
                        spanDst[dst.GetPixelPos(x + dx, y + dy)] = spanSrc[src.GetPixelPos(x, y)];
                    }
                }
            }*/

            /*int halfSizeSrc = spanSrc.Length / 2;
            int halfSizeDst = spanDst.Length / 2;

            for (int dir = 1; dir >= -1; dir-=2)
            {
                int pixelSrcPos = halfSizeSrc;
                int pixelDstPos = halfSizeDst;

                 do
                 {
                     spanDst[pixelDstPos] = spanSrc[pixelSrcPos];
                     pixelSrcPos += dir;
                     pixelDstPos += dir;
                 } while (pixelSrcPos >= 0 && pixelDstPos >= 0 && pixelSrcPos < spanSrc.Length && pixelDstPos < spanDst.Length);
            }*/
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
