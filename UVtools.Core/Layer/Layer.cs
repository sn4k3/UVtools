/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Compression;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;
using Stream = System.IO.Stream;

namespace UVtools.Core
{
    /// <summary>
    /// Represent a Layer
    /// </summary>
    public class Layer : IEquatable<Layer>, IEquatable<uint>
    {
        #region Properties

        public object Mutex = new object();

        /// <summary>
        /// Gets the parent layer manager
        /// </summary>
        public LayerManager ParentLayerManager { get; set; }

        /// <summary>
        /// Gets the number of non zero pixels on this layer image
        /// </summary>
        public uint NonZeroPixelCount { get; internal set; }

        /// <summary>
        /// Gets the bounding rectangle for the image area
        /// </summary>
        public Rectangle BoundingRectangle { get; internal set; } = Rectangle.Empty;

        /// <summary>
        /// Gets the layer index
        /// </summary>
        public uint Index { get; set; }

        /// <summary>
        /// Gets or sets the exposure time in seconds
        /// </summary>
        public float ExposureTime { get; set; }

        /// <summary>
        /// Gets or sets the layer position on Z in mm
        /// </summary>
        public float PositionZ { get; set; }

        private byte[] _compressedBytes;
        /// <summary>
        /// Gets or sets layer image compressed data
        /// </summary>
        public byte[] CompressedBytes
        {
            get => LayerManager.DecompressLayer(_compressedBytes);
            set
            {
                _compressedBytes = LayerManager.CompressLayer(value);
                IsModified = true;
                if (!ReferenceEquals(ParentLayerManager, null))
                    ParentLayerManager.BoundingRectangle = Rectangle.Empty;
            }
        }

        /// <summary>
        /// Gets the original filename, null if no filename attached with layer
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets if layer has been modified
        /// </summary>
        public bool IsModified { get; set; }

        /// <summary>
        /// Gets or sets a new image instance
        /// </summary>
        public Mat LayerMat
        {
            get
            {
                Mat mat = new Mat();
                CvInvoke.Imdecode(CompressedBytes, ImreadModes.Grayscale, mat);
                return mat;
            }
            set
            {
                using (var vector = new VectorOfByte())
                {
                    CvInvoke.Imencode(".png", value, vector);
                    CompressedBytes = vector.ToArray();

                    GetBoundingRectangle(value, true);
                }
            }
        }

        /// <summary>
        /// Gets a new Brg image instance
        /// </summary>
        public Mat BrgMat
        {
            get
            {
                Mat mat = LayerMat;
                CvInvoke.CvtColor(mat, mat, ColorConversion.Gray2Bgr);
                return mat;
            }
        }

        #endregion

        #region Constructor
        public Layer(uint index, byte[] compressedBytes, string filename = null, LayerManager pararentLayerManager = null)
        {
            ParentLayerManager = pararentLayerManager;
            Index = index;
            Filename = filename ?? $"Layer{index}.png";
            CompressedBytes = compressedBytes;
            IsModified = false;
            /*if (compressedBytes.Length > 0)
            {
                GetBoundingRectangle();
            }*/
        }

        public Layer(uint index, Mat layerMat, string filename = null, LayerManager pararentLayerManager = null) : this(index, new byte[0], filename, pararentLayerManager)
        {
            LayerMat = layerMat;
            IsModified = false;
        }


        public Layer(uint index, Stream stream, string filename = null, LayerManager pararentLayerManager = null) : this(index, stream.ToArray(), filename, pararentLayerManager)
        { }
        #endregion

        #region Equatables

        public static bool operator ==(Layer obj1, Layer obj2)
        {
            return obj1.Equals(obj2);
        }

        public static bool operator !=(Layer obj1, Layer obj2)
        {
            return !obj1.Equals(obj2);
        }

        public static bool operator >(Layer obj1, Layer obj2)
        {
            return obj1.Index > obj2.Index;
        }

        public static bool operator <(Layer obj1, Layer obj2)
        {
            return obj1.Index < obj2.Index;
        }

        public static bool operator >=(Layer obj1, Layer obj2)
        {
            return obj1.Index >= obj2.Index;
        }

        public static bool operator <=(Layer obj1, Layer obj2)
        {
            return obj1.Index <= obj2.Index;
        }

        public bool Equals(uint other)
        {
            return Index == other;
        }

        public bool Equals(Layer other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(_compressedBytes, other._compressedBytes);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Layer)obj);
        }

        public override int GetHashCode()
        {
            return (_compressedBytes != null ? _compressedBytes.GetHashCode() : 0);
        }

        private sealed class IndexRelationalComparer : IComparer<Layer>
        {
            public int Compare(Layer x, Layer y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return 1;
                if (ReferenceEquals(null, x)) return -1;
                return x.Index.CompareTo(y.Index);
            }
        }

        public static IComparer<Layer> IndexComparer { get; } = new IndexRelationalComparer();
        #endregion

        #region Formaters
        public override string ToString()
        {
            return $"{nameof(Index)}: {Index}, {nameof(Filename)}: {Filename}, {nameof(IsModified)}: {IsModified}";
        }
        #endregion

        #region Methods

        public Rectangle GetBoundingRectangle(Mat mat = null, bool reCalculate = false)
        {
            if (NonZeroPixelCount > 0 && !reCalculate)
            {
                return BoundingRectangle;
            }
            bool needDispose = false;
            if (ReferenceEquals(mat, null))
            {
                mat = LayerMat;
                needDispose = true;
            }

            using (var nonZeroMat = new Mat())
            {
                CvInvoke.FindNonZero(mat, nonZeroMat);
                NonZeroPixelCount = (uint)nonZeroMat.Height;
                BoundingRectangle = CvInvoke.BoundingRectangle(nonZeroMat);
            }


            if (needDispose) mat.Dispose();

            return BoundingRectangle;
        }

        public Layer PreviousLayer()
        {
            if (ReferenceEquals(ParentLayerManager, null) || Index == 0)
                return null;

            return ParentLayerManager[Index - 1];
        }

        public Layer NextLayer()
        {
            if (ReferenceEquals(ParentLayerManager, null) || Index >= ParentLayerManager.Count - 1)
                return null;

            return ParentLayerManager[Index + 1];
        }

        /// <summary>
        /// Gets all islands start pixel location for this layer
        /// https://www.geeksforgeeks.org/find-number-of-islands/
        /// </summary>
        /// <returns><see cref="List{T}"/> holding all islands coordinates</returns>
        public List<LayerIssue> GetIssues(uint requiredPixelsToSupportIsland = 5)
        {
            if (requiredPixelsToSupportIsland == 0)
                requiredPixelsToSupportIsland = 1;

            // These arrays are used to 
            // get row and column numbers 
            // of 8 neighbors of a given cell 
            List<LayerIssue> result = new List<LayerIssue>();
            List<Point> pixels = new List<Point>();



            var mat = LayerMat;
            var bytes = mat.GetPixelSpan<byte>();



            var previousLayerImage = PreviousLayer()?.LayerMat;
            var previousBytes = previousLayerImage?.GetBytes();


            /*var nextLayerImage = NextLayer()?.Image;
            byte[] nextBytes = null;
            if (!ReferenceEquals(nextLayerImage, null))
            {
                if (nextLayerImage.TryGetSinglePixelSpan(out var nextPixelSpan))
                {
                    nextBytes = MemoryMarshal.AsBytes(nextPixelSpan).ToArray();
                }
            }*/

            // Make a bool array to
            // mark visited cells. 
            // Initially all cells 
            // are unvisited 
            bool[,] visited = new bool[mat.Width, mat.Height];

            // Initialize count as 0 and 
            // traverse through the all 
            // cells of given matrix 
            //uint count = 0;

            // Island checker
            sbyte[] rowNbr = { -1, -1, -1, 0, 0, 1, 1, 1 };
            sbyte[] colNbr = { -1, 0, 1, -1, 1, -1, 0, 1 };
            const uint minPixel = 10;
            const uint minPixelForSupportIsland = 200;
            int pixelIndex;
            uint islandSupportingPixels;
            if (Index > 0)
            {
                for (int y = 0; y < mat.Height; y++)
                {
                    for (int x = 0; x < mat.Width; x++)
                    {
                        pixelIndex = y * mat.Width + x;

                        /*if (bytes[pixelIndex] == 0 && previousBytes?[pixelIndex] == byte.MaxValue &&
                            nextBytes?[pixelIndex] == byte.MaxValue)
                        {
                            result.Add(new LayerIssue(this, LayerIssue.IssueType.HoleSandwich, new []{new Point(x, y)}));
                        }*/

                        if (bytes[pixelIndex] > minPixel && !visited[x, y])
                        {
                            // If a cell with value 1 is not 
                            // visited yet, then new island 
                            // found, Visit all cells in this 
                            // island and increment island count 
                            pixels.Clear();
                            pixels.Add(new Point(x, y));
                            islandSupportingPixels = previousBytes[pixelIndex] >= minPixelForSupportIsland ? 1u : 0;

                            int minX = x;
                            int maxX = x;
                            int minY = y;
                            int maxY = y;

                            int x2;
                            int y2;


                            Queue<Point> queue = new Queue<Point>();
                            queue.Enqueue(new Point(x, y));
                            // Mark this cell as visited 
                            visited[x, y] = true;

                            while (queue.Count > 0)
                            {
                                var point = queue.Dequeue();
                                y2 = point.Y;
                                x2 = point.X;
                                for (byte k = 0; k < 8; k++)
                                {
                                    //if (isSafe(y2 + rowNbr[k], x2 + colNbr[k]))
                                    var tempy2 = y2 + rowNbr[k];
                                    var tempx2 = x2 + colNbr[k];
                                    pixelIndex = tempy2 * mat.Width + tempx2;
                                    if (tempy2 >= 0 &&
                                        tempy2 < mat.Height &&
                                        tempx2 >= 0 && tempx2 < mat.Width &&
                                        bytes[pixelIndex] >= minPixel &&
                                        !visited[tempx2, tempy2])
                                    {
                                        visited[tempx2, tempy2] = true;
                                        point = new Point(tempx2, tempy2);
                                        pixels.Add(point);
                                        queue.Enqueue(point);

                                        minX = Math.Min(minX, tempx2);
                                        maxX = Math.Max(maxX, tempx2);
                                        minY = Math.Min(minY, tempy2);
                                        maxY = Math.Max(maxY, tempy2);

                                        islandSupportingPixels += previousBytes[pixelIndex] >= minPixelForSupportIsland ? 1u : 0;
                                    }
                                }
                            }
                            //count++;

                            if (islandSupportingPixels >= requiredPixelsToSupportIsland)
                                continue; // Not a island, bounding is strong
                            if (islandSupportingPixels > 0 && pixels.Count < requiredPixelsToSupportIsland &&
                                islandSupportingPixels >= Math.Max(1, pixels.Count / 2)) continue; // Not a island
                            result.Add(new LayerIssue(this, LayerIssue.IssueType.Island, pixels.ToArray(), new Rectangle(minX, minY, maxX - minX, maxY - minY)));
                        }
                    }
                }
            }

            pixels.Clear();

            // TouchingBounds Checker
            for (int x = 0; x < mat.Width; x++) // Check Top and Bottom bounds
            {
                if (bytes[x] >= 200) // Top
                {
                    pixels.Add(new Point(x, 0));
                }

                if (bytes[mat.Width * mat.Height - mat.Width + x] >= 200) // Bottom
                {
                    pixels.Add(new Point(x, mat.Height - 1));
                }
            }

            for (int y = 0; y < mat.Height; y++) // Check Left and Right bounds
            {
                if (bytes[y * mat.Width] >= 200) // Left
                {
                    pixels.Add(new Point(0, y));
                }

                if (bytes[y * mat.Width + mat.Width - 1] >= 200) // Right
                {
                    pixels.Add(new Point(mat.Width - 1, y));
                }
            }

            if (pixels.Count > 0)
            {
                result.Add(new LayerIssue(this, LayerIssue.IssueType.TouchingBound, pixels.ToArray()));
            }

            pixels.Clear();

            return result;
        }

        public void MutateMove(OperationMove move)
        {
            using (var layer = LayerMat)
            {
                if (move.ImageWidth == 0) move.ImageWidth = (uint)layer.Width;
                if (move.ImageHeight == 0) move.ImageHeight = (uint)layer.Height;

                /*layer.Transform(1.0, 1.0, move.MarginLeft - move.MarginRight, move.MarginTop-move.MarginBottom);
                LayerMat = layer;*/
                using (var layerRoi = new Mat(layer, move.SrcRoi))
                {
                    using (var dstLayer = layer.CloneBlank())
                    {
                        using (var dstRoi = new Mat(dstLayer, move.DstRoi))
                        {
                            layerRoi.CopyTo(dstRoi);
                            LayerMat = dstLayer;
                        }
                    }
                }
            }
        }


        public void MutateResize(double xScale, double yScale)
        {
            using (var mat = LayerMat)
            {
                mat.TransformFromCenter(xScale, yScale);
                LayerMat = mat;
            }
        }

        public void MutateFlip(FlipType flipType, bool makeCopy = true)
        {
            using (var mat = LayerMat)
            {
                if (makeCopy)
                {
                    using (Mat dst = new Mat())
                    {
                        CvInvoke.Flip(mat, dst, flipType);
                        var spanSrc = mat.GetPixelSpan<byte>();
                        var spanDst = dst.GetPixelSpan<byte>();
                        for (int i = 0; i < spanSrc.Length; i++)
                        {
                            if (spanDst[i] == 0) continue;
                            spanSrc[i] = spanDst[i];
                        }

                        LayerMat = mat;

                    }
                }
                else
                {
                    CvInvoke.Flip(mat, mat, flipType);
                    /*GpuMat gpumat = new GpuMat();
                    gpumat.Upload(mat);
                    CudaInvoke.Flip(gpumat, gpumat, flipType);
                    gpumat.Download(mat);*/
                }

                LayerMat = mat;
            }
        }

        public void MutateRotate(double angle = 90.0, Inter interpolation = Inter.Linear)
        {
            using (var mat = LayerMat)
            {
                var halfWidth = mat.Width / 2.0f;
                var halfHeight = mat.Height / 2.0f;
                using (var translateTransform = new Matrix<double>(2, 3))
                {
                    CvInvoke.GetRotationMatrix2D(new PointF(halfWidth, halfHeight), angle, 1.0, translateTransform);
                    /*var rect = new RotatedRect(PointF.Empty, mat.Size, (float) angle).MinAreaRect();
                    translateTransform[0, 2] += rect.Width / 2.0 - mat.Cols / 2.0;
                    translateTransform[0, 2] += rect.Height / 2.0 - mat.Rows / 2.0;*/

                    /*   var abs_cos = Math.Abs(translateTransform[0, 0]);
                       var abs_sin = Math.Abs(translateTransform[0, 1]);

                       var bound_w = mat.Height * abs_sin + mat.Width * abs_cos;
                       var bound_h = mat.Height * abs_cos + mat.Width * abs_sin;

                       translateTransform[0, 2] += bound_w / 2 - halfWidth;
                       translateTransform[1, 2] += bound_h / 2 - halfHeight;*/


                    CvInvoke.WarpAffine(mat, mat, translateTransform, mat.Size, interpolation);
                }

                LayerMat = mat;
            }
        }

        public void MutateSolidify()
        {
            using (Mat mat = LayerMat)
            {
                using (Mat filteredMat = new Mat())
                {
                    CvInvoke.Threshold(mat, filteredMat, 254, 255, ThresholdType.Binary); // Clean AA

                    using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                    {
                        using (Mat hierarchy = new Mat())
                        {
                            CvInvoke.FindContours(filteredMat, contours, hierarchy, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);
                            var arr = hierarchy.GetData();
                            for (int i = 0; i < contours.Size; i++)
                            {
                                if ((int)arr.GetValue(0, i, 2) != -1 || (int)arr.GetValue(0, i, 3) == -1) continue;
                                CvInvoke.DrawContours(mat, contours, i, new MCvScalar(255), -1);
                            }
                        }
                    }
                }

                LayerMat = mat;
            }
        }

        public void MutatePixelDimming(Matrix<byte> evenPattern = null, Matrix<byte> oddPattern = null, ushort borderSize = 5)
        {
            var anchor = new Point(-1, -1);
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);
            if (ReferenceEquals(evenPattern, null))
            {
                evenPattern = new Matrix<byte>(2, 2)
                {
                    [0, 0] = 127,
                    [0, 1] = 255,
                    [1, 0] = 255,
                    [1, 1] = 127,
                };

                if (ReferenceEquals(oddPattern, null))
                {
                    oddPattern = new Matrix<byte>(2, 2)
                    {
                        [0, 0] = 255,
                        [0, 1] = 127,
                        [1, 0] = 127,
                        [1, 1] = 255,
                    };
                }
            }

            using (Mat dst = LayerMat)
            {
                using (Mat erode = new Mat())
                {
                    using (Mat diff = new Mat())
                    {
                        using (Mat mask = dst.CloneBlank())
                        {
                            CvInvoke.Erode(dst, erode, kernel, anchor, borderSize, BorderType.Default, default);
                            CvInvoke.Subtract(dst, erode, diff);

                            if (Index % 2 == 0)
                            {
                                CvInvoke.Repeat(evenPattern, dst.Rows / evenPattern.Rows + 1, dst.Cols / evenPattern.Cols + 1, mask);
                            }
                            else
                            {
                                CvInvoke.Repeat(oddPattern, dst.Rows / oddPattern.Rows + 1, dst.Cols / oddPattern.Cols + 1, mask);
                            }

                            using (var maskReshape = new Mat(mask, new Rectangle(0, 0, dst.Width, dst.Height)))
                            {
                                CvInvoke.BitwiseAnd(erode, maskReshape, dst);
                            }

                            CvInvoke.Add(dst, diff, dst);
                            LayerMat = dst;
                        }
                    }
                }
            }
        }

        public void MutatePixelDimming(Mat evenPatternMask, Mat oddPatternMask = null, ushort borderSize = 5)
        {
            var anchor = new Point(-1, -1);
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);
            
            if (ReferenceEquals(oddPatternMask, null))
            {
                oddPatternMask = evenPatternMask;
            }

            using (Mat dst = LayerMat)
            {
                using (Mat erode = new Mat())
                {
                    using (Mat diff = new Mat())
                    {
                        CvInvoke.Erode(dst, erode, kernel, anchor, borderSize, BorderType.Default, default);
                        CvInvoke.Subtract(dst, erode, diff);
                        CvInvoke.BitwiseAnd(erode, Index % 2 == 0 ? evenPatternMask : oddPatternMask, dst);
                        CvInvoke.Add(dst, diff, dst);
                        LayerMat = dst;
                    }
                }
            }
        }

        public void MutateErode(int iterations = 1, IInputArray kernel = null, Point anchor = default, BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            if (anchor.IsEmpty) anchor = new Point(-1, -1);
            if (ReferenceEquals(kernel, null))
            {
                kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);
            }
            using (Mat dst = LayerMat)
            {
                CvInvoke.Erode(dst, dst, kernel, anchor, iterations, borderType, borderValue);
                LayerMat = dst;
            }
        }

        public void MutateDilate(int iterations = 1, IInputArray kernel = null, Point anchor = default, BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            if (anchor.IsEmpty) anchor = new Point(-1, -1);
            if (ReferenceEquals(kernel, null))
            {
                kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);
            }
            using (Mat dst = LayerMat)
            {
                CvInvoke.Dilate(dst, dst, kernel, anchor, iterations, borderType, borderValue);
                LayerMat = dst;
            }
        }

        public void MutateOpen(int iterations = 1, IInputArray kernel = null, Point anchor = default, BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            if (anchor.IsEmpty) anchor = new Point(-1, -1);
            if (ReferenceEquals(kernel, null))
            {
                kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);
            }
            using (Mat dst = LayerMat)
            {
                CvInvoke.MorphologyEx(dst, dst, MorphOp.Open, kernel, anchor, iterations, borderType, borderValue);
                LayerMat = dst;
            }
        }

        public void MutateClose(int iterations = 1, IInputArray kernel = null, Point anchor = default, BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            if (anchor.IsEmpty) anchor = new Point(-1, -1);
            if (ReferenceEquals(kernel, null))
            {
                kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);
            }
            using (Mat dst = LayerMat)
            {
                CvInvoke.MorphologyEx(dst, dst, MorphOp.Close, kernel, anchor, iterations, borderType, borderValue);
                LayerMat = dst;
            }
        }

        public void MutateGradient(int iterations = 1, IInputArray kernel = null, Point anchor = default, BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            if (anchor.IsEmpty) anchor = new Point(-1, -1);
            if (ReferenceEquals(kernel, null))
            {
                kernel = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(3, 3), anchor);
            }
            using (Mat dst = LayerMat)
            {
                CvInvoke.MorphologyEx(dst, dst, MorphOp.Gradient, kernel, anchor, iterations, borderType, borderValue);
                LayerMat = dst;
            }
        }

        public void MutateThresholdPixels(byte threshold, byte maximum, ThresholdType thresholdType)
        {
            using (Mat dst = LayerMat)
            {
                CvInvoke.Threshold(dst, dst, threshold, maximum, thresholdType);
                LayerMat = dst;
            }
        }

        public void MutatePyrDownUp(BorderType borderType = BorderType.Reflect101)
        {
            using (Mat dst = LayerMat)
            {
                CvInvoke.PyrDown(dst, dst, borderType);
                CvInvoke.PyrUp(dst, dst, borderType);
                LayerMat = dst;
            }
        }

        public void MutateMedianBlur(int aperture = 1)
        {
            using (Mat dst = LayerMat)
            {
                CvInvoke.MedianBlur(dst, dst, aperture);
                LayerMat = dst;
            }
        }

        public void MutateGaussianBlur(Size size = default, int sigmaX = 0, int sigmaY = 0, BorderType borderType = BorderType.Reflect101)
        {
            if (size.IsEmpty) size = new Size(5, 5);

            using (Mat dst = LayerMat)
            {
                CvInvoke.GaussianBlur(dst, dst, size, sigmaX, sigmaY, borderType);
                LayerMat = dst;
            }
        }

        public void ChangeResolution(uint newResolutionX, uint newResolutionY, Rectangle roi)
        {
            using (var mat = LayerMat)
            {
                //mat.Transform(1, 1, newResolutionX-mat.Width+roi.Width, newResolutionY-mat.Height - roi.Height/2, new Size((int) newResolutionX, (int) newResolutionY));
                using (var matRoi = new Mat(mat, roi))
                {
                    using (var matDst = new Mat(new Size((int) newResolutionX, (int) newResolutionY), mat.Depth,
                        mat.NumberOfChannels))
                    {
                        using (var matDstRoi = new Mat(matDst,
                            new Rectangle((int) (newResolutionX / 2 - roi.Width / 2), (int) newResolutionY / 2 - roi.Height / 2, roi.Width, roi.Height)))
                        {
                            matRoi.CopyTo(matDstRoi);
                            LayerMat = matDst;
                        }
                    }
                }
            }
        }

        public void ToolPattern(OperationPattern settings)
        {
            using (var layer = LayerMat)
            {
                using (var layerRoi = new Mat(layer, settings.SrcRoi))
                {
                    using (var dstLayer = layer.CloneBlank())
                    {
                        for (ushort col = 0; col < settings.Cols; col++)
                        {
                            for (ushort row = 0; row < settings.Rows; row++)
                            {
                                using (var dstRoi = new Mat(dstLayer, settings.GetRoi(col, row)))
                                {
                                    layerRoi.CopyTo(dstRoi);
                                }
                            }
                        }

                        LayerMat = dstLayer;
                    }
                }
            }
        }



        public Layer Clone()
        {
            return new Layer(Index, CompressedBytes, Filename, ParentLayerManager)
            {
                PositionZ = PositionZ,
                ExposureTime = ExposureTime,
                BoundingRectangle = BoundingRectangle,
                NonZeroPixelCount = NonZeroPixelCount,
            };
        }

        #endregion

        
    }
}
