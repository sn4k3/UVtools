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
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core.Objects;

namespace UVtools.Core.Extensions
{
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
        #endregion

        #region Initializers methods
        /// <summary>
        /// Create a byte array of size of this <see cref="Mat"/>
        /// </summary>
        /// <param name="mat"></param>
        /// <returns>Blank byte array</returns>
        public static byte[] CreateBlankByteArray(this Mat mat)
            => new byte[mat.GetLength()];

        /// <summary>
        /// Creates a new empty <see cref="Mat"/> with same size and type of the source
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static Mat New(this Mat mat)
            => new(mat.Size, mat.Depth, mat.NumberOfChannels);

        /// <summary>
        /// Creates a new empty <see cref="Mat"/> with same size and type of the source
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
        public static Mat NewBlank(this Mat mat)
            => InitMat(mat.Size, mat.NumberOfChannels, mat.Depth);

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
        /// Gets the byte pointer of this <see cref="Mat"/>
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static unsafe byte* GetBytePointer(this Mat mat)
        {
            return (byte*)mat.DataPointer.ToPointer();
        }

        /// <summary>
        /// Gets the whole data span to manipulate or read pixels
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static Span<byte> GetDataByteSpan(this Mat mat)
        {
            return mat.GetDataSpan<byte>();
        }

        public static unsafe Span<byte> GetDataByteSpan(this Mat mat, int length, int offset = 0)
        {
            return new(IntPtr.Add(mat.DataPointer, offset).ToPointer(), length <= 0 ? mat.GetLength() : length);
        }

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
            return new(IntPtr.Add(mat.DataPointer, offset).ToPointer(), length <= 0 ? mat.GetLength() : length);
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
        {
            return mat.GetDataSpan<T>(mat.NumberOfChannels, mat.GetPixelPos(x, y));
        }

        /// <summary>
        /// Gets a single pixel span to manipulate or read pixels
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mat"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Span<T> GetPixelSpan<T>(this Mat mat, int pos)
        {
            return mat.GetDataSpan<T>(mat.NumberOfChannels, pos);
        }

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
            return new(IntPtr.Add(mat.DataPointer, y * mat.Step + offset).ToPointer(), length <= 0 ? mat.Step : length);
        }

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
            var colMat = mat.Col(x);
            return new(IntPtr.Add(colMat.DataPointer, offset).ToPointer(), length <= 0 ? mat.Height : length);
        }
        #endregion

        #region Memory Fill

        /// <summary>
        /// Fill a mat span with a color
        /// </summary>
        /// <param name="mat">Mat to fill</param>
        /// <param name="startPosition">Start position, this reference will increment by the <see cref="length"/></param>
        /// <param name="length">Length to fill</param>
        /// <param name="color">Color to fill with</param>
        /// <param name="colorFillMinThreshold">Ignore and sum <see cref="startPosition"/> to <see cref="length"/> if <see cref="color"/> is less than the threshold value</param>
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
        /// <param name="colorFillMinThreshold">Ignore and sum <see cref="startPosition"/> to <see cref="length"/> if <see cref="color"/> is less than the threshold value</param>
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
        /// <param name="colorFillMinThreshold">Ignore and sum <see cref="startPosition"/> to <see cref="length"/> if <see cref="color"/> is less than the threshold value</param>
        public static void FillSpan(this Mat mat, Point position, int length, byte color, byte colorFillMinThreshold = 1)
            => mat.FillSpan(position.X, position.Y, length, color, colorFillMinThreshold);
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
        public static void SetByte(this Mat mat, int pixel, byte[] value) =>
            Marshal.Copy(value, 0, mat.DataPointer + pixel, value.Length);

        /// <summary>
        /// Sets a byte pixel at a position
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        public static void SetByte(this Mat mat, int x, int y, byte value) =>
            SetByte(mat, x, y, new[] { value });

        /// <summary>
        /// Sets a byte pixel at a position
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        public static void SetByte(this Mat mat, int x, int y, byte[] value) =>
            SetByte(mat, y * mat.Step + x * mat.NumberOfChannels, value);

        /// <summary>
        /// Sets bytes
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="value"></param>
        public static void SetBytes(this Mat mat, byte[] value) =>
            Marshal.Copy(value, 0, mat.DataPointer, value.Length);

        /// <summary>
        /// Gets PNG byte array
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static byte[] GetPngByes(this Mat mat)
        {
            return CvInvoke.Imencode(".png", mat);
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

        #region Draw Methods

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
        /// Draw a polygon given number of sides and length
        /// </summary>
        /// <param name="src"></param>
        /// <param name="sides">Number of polygon sides, Special: use 1 to draw a line and >= 100 to draw a native OpenCV circle</param>
        /// <param name="radius">Radius</param>
        /// <param name="center">Center position</param>
        /// <param name="color"></param>
        /// <param name="startingAngle"></param>
        /// <param name="thickness"></param>
        /// <param name="lineType"></param>
        /// <param name="flip"></param>
        public static void DrawPolygon(this Mat src, int sides, int radius, Point center, MCvScalar color, double startingAngle = 0, int thickness = -1, LineType lineType = LineType.EightConnected, FlipType? flip = null)
        {
            if (sides == 1)
            {
                Point point1 = new(center.X - radius, center.Y);
                point1 = point1.Rotate(startingAngle, center);
                Point point2 = new(center.X + radius, center.Y);
                point2 = point2.Rotate(startingAngle, center);
                CvInvoke.Line(src, point1, point2, color, thickness < 1 ? 1 : thickness, lineType);
                return;
            }
            if (sides >= 100)
            {
                CvInvoke.Circle(src, center, radius, color, thickness, lineType);
                return;
            }

            var points = DrawingExtensions.GetPolygonVertices(sides, radius, center, startingAngle,
                flip is FlipType.Horizontal or FlipType.Both,
                flip is FlipType.Vertical or FlipType.Both);
            
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

        public static void PutTextExtended(this Mat src, string text, Point org, FontFace fontFace, double fontScale,
            MCvScalar color, int thickness = 1, LineType lineType = LineType.EightConnected,
            bool bottomLeftOrigin = false, PutTextLineAlignment lineAlignment = default)
            => src.PutTextExtended(text, org, fontFace, fontScale, color, thickness, 0, lineType, bottomLeftOrigin, lineAlignment);

            /// <summary>
            /// Extended OpenCV PutText to accepting line breaks and line alignment
            /// </summary>
            public static void PutTextExtended(this Mat src, string text, Point org, FontFace fontFace, double fontScale,
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
   
            using var mask = rotatedSrc.NewBlank();
            mask.PutTextExtended(text, org, fontFace, fontScale, WhiteColor, thickness, lineGapOffset, lineType, bottomLeftOrigin, lineAlignment);

            rotatedSrc.Rotate(angle, src.Size);
            mask.Rotate(angle, src.Size);

            rotatedSrc.CopyTo(src, mask);
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
        public static VectorOfVectorOfPoint FindContours(this Mat mat, RetrType mode = RetrType.List, ChainApproxMethod method = ChainApproxMethod.ChainApproxSimple, Point offset = default)
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
        public static VectorOfVectorOfPoint FindContours(this Mat mat, out int[,] hierarchy, RetrType mode, ChainApproxMethod method = ChainApproxMethod.ChainApproxSimple, Point offset = default)
        {
            var contours = new VectorOfVectorOfPoint();
            using var hierarchyMat = new Mat();
            CvInvoke.FindContours(mat, contours, hierarchyMat, mode, method, offset);
            hierarchy = new int[hierarchyMat.Cols, 4];
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
