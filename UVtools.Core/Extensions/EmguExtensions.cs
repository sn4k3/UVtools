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
using Emgu.CV.Util;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UVtools.Core.Extensions
{
    public static class EmguExtensions
    {
        public static readonly MCvScalar WhiteColor = new(255, 255, 255, 255);
        public static readonly MCvScalar BlackColor = new(0, 0, 0, 255);
        //public static readonly MCvScalar TransparentColor = new();

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
            return new(mat.DataPointer.ToPointer(), mat.GetLength());
        }

        public static unsafe Span<byte> GetPixelSpanByte(this Mat mat)
        {
            return new(mat.DataPointer.ToPointer(), mat.GetLength());
        }

        public static unsafe Span<T> GetPixelSpan<T>(this Mat mat, int length, int offset = 0)
        {
            return new(IntPtr.Add(mat.DataPointer, offset).ToPointer(), length);
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
            return new(IntPtr.Add(mat.DataPointer, y * mat.Step + offset).ToPointer(), length <= 0 ? mat.Step : length);
            //return mat.GetPixelSpan<T>().Slice(offset, mat.Step);
        }

        public static unsafe Span<T> GetPixelColSpan<T>(this Mat mat, int x, int length = 0, int offset = 0)
        {
            var colMat = mat.Col(x);
            return new(IntPtr.Add(colMat.DataPointer, offset).ToPointer(), length <= 0 ? mat.Height : length);
        }

        /// <summary>
        /// Gets if a <see cref="Mat"/> is all zeroed
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="threshold">Pixel brightness threshold</param>
        /// <returns></returns>
        public static unsafe bool IsZeroed(this Mat mat, byte threshold = 0)
        {
            var ptr = mat.GetBytePointer();
            for (int i = 0; i < mat.GetLength(); i++)
            {
                if (ptr[i] > threshold) return false;
            }
            return true;
        }


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
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        public static void Rotate(this Mat src, double angle, Size newSize = default, double scale = 1.0) => Rotate(src, src, angle, newSize, scale);

        /// <summary>
        /// Rotates a Mat by an angle while keeping the image size
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        public static void Rotate(this Mat src, Mat dst, double angle, Size newSize = default, double scale = 1.0)
        {
            if (angle % 360 == 0 && scale == 1.0) return;
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
            if (angle % 360 == 0 && scale == 1.0) return;
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

        public static void PutTextRotated(this Mat src, string text, Point org, FontFace fontFace, double fontScale, MCvScalar color,
        int thickness = 1, LineType lineType = LineType.EightConnected, bool bottomLeftOrigin = false, double angle = 0)
        {
            if (angle % 360 == 0) // No rotation needed, cheaper cycle
            {
                CvInvoke.PutText(src, text, org, fontFace, fontScale, color, thickness, lineType, bottomLeftOrigin);
                return;
            }

            using var rotatedSrc = src.Clone();
            rotatedSrc.RotateAdjustBounds(-angle);
            var sizeDifference = (rotatedSrc.Size - src.Size).Half();
            org.Offset(sizeDifference.ToPoint());
            org = org.Rotate(-angle, new Point(rotatedSrc.Size.Width / 2, rotatedSrc.Size.Height / 2));
            CvInvoke.PutText(rotatedSrc, text, org, fontFace, fontScale, color, thickness, lineType, bottomLeftOrigin);
   
            using var mask = rotatedSrc.CloneBlank();
            CvInvoke.PutText(mask, text, org, fontFace, fontScale, WhiteColor, thickness, lineType, bottomLeftOrigin);

            rotatedSrc.Rotate(angle, src.Size);
            mask.Rotate(angle, src.Size);

            rotatedSrc.CopyTo(src, mask);
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
        public static Mat Skeletonize(this Mat src, out int iterations, Size ksize = default, ElementShape elementShape = ElementShape.Rectangle)
        {
            if (ksize.IsEmpty) ksize = new Size(3, 3);
            Point anchor = new(-1, -1);
            var skeleton = src.CloneBlank();
            using var kernel = CvInvoke.GetStructuringElement(elementShape, ksize, anchor);

            var image = src;
            using var temp = new Mat();
            iterations = 0;
            while (true)
            {
                iterations++;

                // erode and dilate the image using the structuring element
                using var eroded = new Mat();
                CvInvoke.Erode(image, eroded, kernel, anchor, 1, BorderType.Reflect101, default);
                CvInvoke.Dilate(eroded, temp, kernel, anchor, 1, BorderType.Reflect101, default);

                // subtract the temporary image from the original, eroded
                // image, then take the bitwise 'or' between the skeleton
                // and the temporary image
                CvInvoke.Subtract(image, temp, temp);
                CvInvoke.BitwiseOr(skeleton, temp, skeleton);
                image = eroded.Clone();

                // if there are no more 'white' pixels in the image, then
                // break from the loop
                if (CvInvoke.CountNonZero(image) == 0) break;
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
        public static Mat Skeletonize(this Mat src, Size ksize = default, ElementShape elementShape = ElementShape.Rectangle)
            => src.Skeletonize(out _, ksize, elementShape);

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
                Mat m = new(dst, new Rectangle(dx, dy, src.Width, src.Height));
                src.CopyTo(m);
            }
            else if (dst.Step < src.Step && dst.Height < src.Height)
            {
                var dx = Math.Abs(dst.Step - src.Step) / 2;
                var dy = Math.Abs(dst.Height - src.Height) / 2;
                Mat m = new(src, new Rectangle(dx, dy, dst.Width, dst.Height));
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

        public static Mat New(this Mat mat)
        {
            return new(mat.Rows, mat.Cols, mat.Depth, mat.NumberOfChannels);
        }

        public static Mat New(this Mat src, MCvScalar color)
        {
            Mat mat = new(src.Rows, src.Cols, src.Depth, src.NumberOfChannels);
            mat.SetTo(color);
            return mat;
        }

        /// <summary>
        /// Clone this <see cref="Mat"/> blanked (All zeros)
        /// </summary>
        /// <param name="mat"></param>
        /// <returns>Blanked <see cref="Mat"/></returns>
        public static Mat CloneBlank(this Mat mat)
        {
            return InitMat(mat.Size, mat.NumberOfChannels, mat.Depth);
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

        public static Mat InitMat(Size size, int channels = 1, DepthType depthType = DepthType.Cv8U)
        {
            return Mat.Zeros(size.Height, size.Width, depthType, channels);
            /*var mat = new Mat(size, depthType, channels);
            switch (channels)
            {
                case 1:
                    mat.SetTo(BlackByte);
                    break;
                case 3:
                    mat.SetTo(Black3Byte);
                    break;
                case 4:
                    mat.SetTo(Transparent4Byte);
                    break;
            }
            
            return mat;*/
        }

        public static Mat InitMat(Size size, MCvScalar scalar, int channels = 1, DepthType depthType = DepthType.Cv8U)
        {
            var mat = new Mat(size, depthType, channels);
            mat.SetTo(scalar);
            return mat;
        }

        public static Mat RoiFromCenter(this Mat mat, Size targetSize, Rectangle roi)
        {
            if (targetSize == mat.Size) return mat;
            var newMat = InitMat(targetSize);

            var roiMat = new Mat(mat, roi);


            //int xStart = mat.Width / 2 - targetSize.Width / 2;
            //int yStart = mat.Height / 2 - targetSize.Height / 2;

            var newMatRoi = new Mat(newMat, new Rectangle(
                targetSize.Width / 2 - roi.Width / 2,
                targetSize.Height / 2 - roi.Height / 2,
                roi.Width,
                roi.Height
            ));
            roiMat.CopyTo(newMatRoi);
            return newMat;
        }

        /// <summary>
        /// Allocates a new array of mat 's
        /// </summary>
        /// <param name="count">Array size</param>
        /// <returns></returns>
        public static Mat[] Allocate(uint count) => Allocate(count, Size.Empty);

        /// <summary>
        /// Allocates a new array of mat 's
        /// </summary>
        /// <param name="count">Array size</param>
        /// <param name="size">Image size to create, use <see cref="Size.Empty"/> to create a empty Mat</param>
        /// <returns>New mat array</returns>
        public static Mat[] Allocate(uint count, Size size)
        {
            var layers = new Mat[count];
            for (var i = 0; i < layers.Length; i++)
            {
                layers[i] = size == Size.Empty ? new Mat() : InitMat(size);
            }

            return layers;
        }

        public static byte[] GetPngByes(this Mat mat)
        {
            using var vector = new VectorOfByte();
            CvInvoke.Imencode(".png", mat, vector);
            return vector.ToArray();
        }

    }
}
