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
using UVtools.Core.Objects;

namespace UVtools.Core.Extensions
{
    public static class EmguExtensions
    {
        #region Constants
        public static readonly MCvScalar WhiteColor = new(255, 255, 255, 255);
        public static readonly MCvScalar BlackColor = new(0, 0, 0, 255);
        //public static readonly MCvScalar TransparentColor = new();
        #endregion

        #region Initializers methods
        /// <summary>
        /// Create a byte array of size of this <see cref="Mat"/>
        /// </summary>
        /// <param name="mat"></param>
        /// <returns>Blank byte array</returns>
        public static byte[] CreateBlankByteArray(this Mat mat)
        {
            return new byte[mat.GetLength()];
        }

        /// <summary>
        /// Creates a new empty <see cref="Mat"/> with same size and type of the source
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static Mat New(this Mat mat)
        {
            return new(mat.Rows, mat.Cols, mat.Depth, mat.NumberOfChannels);
        }

        /// <summary>
        /// Creates a new empty <see cref="Mat"/> with same size and type of the source
        /// </summary>
        /// <param name="src"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Mat New(this Mat src, MCvScalar color)
        {
            Mat mat = new(src.Rows, src.Cols, src.Depth, src.NumberOfChannels);
            mat.SetTo(color);
            return mat;
        }

        /// <summary>
        /// Creates a new blanked (All zeros) <see cref="Mat"/> with same size and type of the source
        /// </summary>
        /// <param name="mat"></param>
        /// <returns>Blanked <see cref="Mat"/></returns>
        public static Mat NewBlank(this Mat mat)
        {
            return InitMat(mat.Size, mat.NumberOfChannels, mat.Depth);
        }

        /// <summary>
        /// Creates a <see cref="Mat"/> with same size and type of the source and set it to a color
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Mat NewSetTo(this Mat mat, MCvScalar color)
        {
            return InitMat(mat.Size, color, mat.NumberOfChannels, mat.Depth);
        }


        public static Mat InitMat(Size size, int channels = 1, DepthType depthType = DepthType.Cv8U)
        {
            return size.IsEmpty ? new() : Mat.Zeros(size.Height, size.Width, depthType, channels);
        }

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
        #endregion

        #region Memory accessors
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
        public static unsafe Span<T> GetDataSpan<T>(this Mat mat)
        {
            return new(mat.DataPointer.ToPointer(), mat.GetLength());
        }

        public static unsafe Span<byte> GetDataByteSpan(this Mat mat)
        {
            return new(mat.DataPointer.ToPointer(), mat.GetLength());
        }

        public static unsafe Span<T> GetDataSpan<T>(this Mat mat, int length, int offset = 0)
        {
            return new(IntPtr.Add(mat.DataPointer, offset).ToPointer(), length);
        }

        public static Span<T> GetPixelSpan<T>(this Mat mat, int x, int y)
        {
            return mat.GetDataSpan<T>(mat.NumberOfChannels, mat.GetPixelPos(x, y));
        }

        public static Span<T> GetPixelSpan<T>(this Mat mat, int pos)
        {
            return mat.GetDataSpan<T>(mat.NumberOfChannels, pos);
        }


        public static unsafe Span<T> GetRowSpan<T>(this Mat mat, int y, int length = 0, int offset = 0)
        {
            return new(IntPtr.Add(mat.DataPointer, y * mat.Step + offset).ToPointer(), length <= 0 ? mat.Step : length);
        }

        public static unsafe Span<T> GetColSpan<T>(this Mat mat, int x, int length = 0, int offset = 0)
        {
            var colMat = mat.Col(x);
            return new(IntPtr.Add(colMat.DataPointer, offset).ToPointer(), length <= 0 ? mat.Height : length);
        }
        #endregion

        #region Get/Set methods
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
            var data = new byte[mat.GetLength()];
            Marshal.Copy(mat.DataPointer, data, 0, data.Length);
            return data;
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


        public static void SetByte(this Mat mat, int pixel, byte value) => SetByte(mat, pixel, new[] { value });

        public static void SetByte(this Mat mat, int pixel, byte[] value) =>
            Marshal.Copy(value, 0, mat.DataPointer + pixel, value.Length);

        public static void SetByte(this Mat mat, int x, int y, byte value) =>
            SetByte(mat, x, y, new[] { value });

        public static void SetByte(this Mat mat, int x, int y, byte[] value) =>
            SetByte(mat, y * mat.Step + x * mat.NumberOfChannels, value);

        public static void SetBytes(this Mat mat, byte[] value) =>
            Marshal.Copy(value, 0, mat.DataPointer, value.Length);

        public static byte[] GetPngByes(this Mat mat)
        {
            using var vector = new VectorOfByte();
            CvInvoke.Imencode(".png", mat, vector);
            return vector.ToArray();
        }
        #endregion

        #region Copy methods
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
        #endregion

        #region Roi methods

        /// <summary>
        /// Gets a Roi, but return source when roi is empty or have same size as source
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="roi"></param>
        /// <returns></returns>
        public static Mat Roi(this Mat mat, Rectangle roi)
        {
            return roi.IsEmpty || roi.Size == mat.Size ? mat : new Mat(mat, roi);
        }

        /// <summary>
        /// Gets a new mat obtained from it center at a target size and roi
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="targetSize"></param>
        /// <param name="roi"></param>
        /// <returns></returns>
        public static Mat NewRoiFromCenter(this Mat mat, Size targetSize, Rectangle roi)
        {
            if (targetSize == mat.Size) return mat.Clone();
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
        #endregion

        #region Is methods
        /// <summary>
        /// Gets if a <see cref="Mat"/> is all zeroed by a threshold
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="threshold">Pixel brightness threshold</param>
        /// <returns></returns>
        public static unsafe bool IsZeroed(this Mat mat, byte threshold = 0)
        {
            var ptr = mat.GetBytePointer();
            var length = mat.GetLength();
            for (var i = 0; i < length; i++)
            {
                if (ptr[i] > threshold) return false;
            }
            return true;
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
        {
            text = text.TrimEnd('\n', '\r', ' ');
            var lines = text.Split(StaticObjects.LineBreakCharacters, StringSplitOptions.None);
            var textSize = CvInvoke.GetTextSize(text, fontFace, fontScale, thickness, ref baseLine);

            if (lines.Length is 0 or 1) return textSize;

            var lineGap = textSize.Height / 3;
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

        /// <summary>
        /// Extended OpenCV PutText to accepting line breaks and line alignment
        /// </summary>
        public static void PutTextExtended(this Mat src, string text, Point org, FontFace fontFace, double fontScale,
            MCvScalar color, int thickness = 1, LineType lineType = LineType.EightConnected, bool bottomLeftOrigin = false, PutTextLineAlignment lineAlignment = default)
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
            var lineGap = textSize.Height / 3;
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
        public static void PutTextRotated(this Mat src, string text, Point org, FontFace fontFace, double fontScale, MCvScalar color,
        int thickness = 1, LineType lineType = LineType.EightConnected, bool bottomLeftOrigin = false, PutTextLineAlignment lineAlignment = default, double angle = 0)
        {
            if (angle % 360 == 0) // No rotation needed, cheaper cycle
            {
                src.PutTextExtended(text, org, fontFace, fontScale, color, thickness, lineType, bottomLeftOrigin, lineAlignment);
                return;
            }

            using var rotatedSrc = src.Clone();
            rotatedSrc.RotateAdjustBounds(-angle);
            var sizeDifference = (rotatedSrc.Size - src.Size).Half();
            org.Offset(sizeDifference.ToPoint());
            org = org.Rotate(-angle, new Point(rotatedSrc.Size.Width / 2, rotatedSrc.Size.Height / 2));
            rotatedSrc.PutTextExtended(text, org, fontFace, fontScale, color, thickness, lineType, bottomLeftOrigin, lineAlignment);
   
            using var mask = rotatedSrc.NewBlank();
            mask.PutTextExtended(text, org, fontFace, fontScale, WhiteColor, thickness, lineType, bottomLeftOrigin, lineAlignment);

            rotatedSrc.Rotate(angle, src.Size);
            mask.Rotate(angle, src.Size);

            rotatedSrc.CopyTo(src, mask);
        }
        #endregion

        #region Utilities methods
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
            var skeleton = src.NewBlank();
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
        #endregion
    }
}
