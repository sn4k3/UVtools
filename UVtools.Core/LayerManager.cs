/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core.Extensions;

namespace UVtools.Core
{
    #region LayerIssue Class

    public class LayerIssue : IEnumerable<Point>
    {
        public enum IssueType : byte
        {
            Island,
            ResinTrap,
            TouchingBound,
            //HoleSandwich,
        }

        /// <summary>
        /// Gets the parent layer
        /// </summary>
        public Layer Layer { get; }

        /// <summary>
        /// Gets the issue type associated
        /// </summary>
        public IssueType Type { get; }

        /// <summary>
        /// Gets the pixels containing the issue
        /// </summary>
        public Point[] Pixels { get; }

        /// <summary>
        /// Gets the bounding rectangle of the pixel area
        /// </summary>
        public Rectangle BoundingRectangle { get; }

        /// <summary>
        /// Gets the X coordinate for the first point, -1 if doesn't exists
        /// </summary>
        public int X => HaveValidPoint ? Pixels[0].X : -1;

        /// <summary>
        /// Gets the Y coordinate for the first point, -1 if doesn't exists
        /// </summary>
        public int Y => HaveValidPoint ? Pixels[0].Y : -1;

        /// <summary>
        /// Gets the XY point for first point
        /// </summary>
        public Point Point => HaveValidPoint ? Pixels[0] : new Point(-1, -1);

        /// <summary>
        /// Gets the number of pixels on this issue
        /// </summary>
        public uint Size {
            get
            {
                if (Type == IssueType.ResinTrap && !BoundingRectangle.IsEmpty)
                {
                    return (uint) (BoundingRectangle.Width * BoundingRectangle.Height);
                }

                if (ReferenceEquals(Pixels, null)) return 0;
                return (uint) Pixels.Length;
            }
        }

        /// <summary>
        /// Check if this issue have a valid start point to show
        /// </summary>
        public bool HaveValidPoint => !ReferenceEquals(Pixels, null) && Pixels.Length > 0;

        public LayerIssue(Layer layer, IssueType type, Point[] pixels = null, Rectangle boundingRectangle = new Rectangle())
        {
            Layer = layer;
            Type = type;
            Pixels = pixels;
            BoundingRectangle = boundingRectangle;
        }

        public Point this[uint index] => Pixels[index];

        public Point this[int index] => Pixels[index];

        public IEnumerator<Point> GetEnumerator()
        {
            return ((IEnumerable<Point>)Pixels).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return $"{nameof(Type)}: {Type}, Layer: {Layer.Index}, {nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Size)}: {Size}";
        }
    }
    #endregion

    #region LayerHollowArea

    public class LayerHollowArea : IEnumerable<Point>
    {
        public enum AreaType : byte
        {
            Unknown = 0,
            Trap,
            Drain
        }
        /// <summary>
        /// Gets area pixels
        /// </summary>
        public Point[] Contour { get; }

        public System.Drawing.Rectangle BoundingRectangle { get; }

        public AreaType Type { get; set; } = AreaType.Unknown;

        public bool Processed { get; set; }

        #region Indexers
        public Point this[uint index]
        {
            get => index < Contour.Length ? Contour[index] : Point.Empty;
            set => Contour[index] = value;
        }

        public Point this[int index]
        {
            get => index < Contour.Length ? Contour[index] : Point.Empty;
            set => Contour[index] = value;
        }

        public Point this[uint x, uint y]
        {
            get
            {
                for (uint i = 0; i < Contour.Length; i++)
                {
                    if (Contour[i].X == x && Contour[i].Y == y) return Contour[i];
                }
                return Point.Empty;
            }
        }

        public Point this[int x, int y] => this[(uint) x, (uint)y];

        public Point this[Point point] => this[point.X, point.Y];

        #endregion

        public IEnumerator<Point> GetEnumerator()
        {
            return ((IEnumerable<Point>)Contour).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public LayerHollowArea()
        {
        }

        public LayerHollowArea(Point[] contour, System.Drawing.Rectangle boundingRectangle, AreaType type = AreaType.Unknown)
        {
            Contour = contour;
            BoundingRectangle = boundingRectangle;
            Type = type;
        }
    }
    #endregion

    #region Layer Class
    /// <summary>
    /// Represent a Layer
    /// </summary>
    public class Layer : IEquatable<Layer>, IEquatable<uint>
    {
        #region Properties

        /// <summary>
        /// Gets the parent layer manager
        /// </summary>
        public LayerManager ParentLayerManager { get; set; }

        /// <summary>
        /// Gets the layer index
        /// </summary>
        public uint Index { get; }

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
            Index = index;
            CompressedBytes = compressedBytes;
            Filename = filename ?? $"Layer{index}.png";
            IsModified = false;
            ParentLayerManager = pararentLayerManager;
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
                            result.Add(new LayerIssue(this, LayerIssue.IssueType.Island, pixels.ToArray(), new Rectangle(minX, minY, maxX-minX, maxY-minY)));
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
                    pixels.Add(new Point(x, mat.Height-1));
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
                    pixels.Add(new Point(mat.Width-1, y));
                }
            }

            if (pixels.Count > 0)
            {
                result.Add(new LayerIssue(this, LayerIssue.IssueType.TouchingBound, pixels.ToArray()));
            }

            pixels.Clear();

            return result;
        }


        public void MutateResize(double xScale, double yScale)
        {
            using (var mat = LayerMat)
            {
                mat.ScaleFromCenter(xScale, yScale);
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
                                if ((int) arr.GetValue(0, i, 2) != -1 || (int) arr.GetValue(0, i, 3) == -1) continue;
                                CvInvoke.DrawContours(mat, contours, i, new MCvScalar(255), -1);
                            }
                        }
                    }
                }

                LayerMat = mat;
            }
        }

        public void MutateErode(int iterations = 1, IInputArray kernel = null, Point anchor = default, BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            if(anchor.IsEmpty) anchor = new Point(-1, -1);
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
            if(size.IsEmpty) size = new Size(5, 5);

            using (Mat dst = LayerMat)
            {
                CvInvoke.GaussianBlur(dst, dst, size, sigmaX, sigmaY, borderType);
                LayerMat = dst;
            }
        }



        public Layer Clone()
        {
            return new Layer(Index, CompressedBytes, Filename, ParentLayerManager);
        }
       

        
        #endregion
    }
    #endregion

    #region LayerManager Class
    public class LayerManager : IEnumerable<Layer>
    {
       #region Properties
        /// <summary>
        /// Layers List
        /// </summary>
        public Layer[] Layers { get; }

        /// <summary>
        /// Gets the layers count
        /// </summary>
        public uint Count => (uint) Layers.Length;

        /// <summary>
        /// Gets if any layer got modified, otherwise false
        /// </summary>
        public bool IsModified
        {
            get
            {
                for (uint i = 0; i < Count; i++)
                {
                    if (Layers[i].IsModified) return true;
                }
                return false;
            }
        }


        #endregion

        #region Constructors
        public LayerManager(uint layerCount)
        {
            Layers = new Layer[layerCount];
        }
        #endregion

        #region Indexers
        public Layer this[uint index]
        {
            get => Layers[index];
            set => AddLayer(index, value);
        }

        public Layer this[int index]
        {
            get => Layers[index];
            set => AddLayer((uint) index, value);
        }

        public Layer this[long index]
        {
            get => Layers[index];
            set => AddLayer((uint) index, value);
        }

        #endregion

        #region Numerators
        public IEnumerator<Layer> GetEnumerator()
        {
            return ((IEnumerable<Layer>)Layers).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Compress a layer from a <see cref="Stream"/>
        /// </summary>
        /// <param name="input"><see cref="Stream"/> to compress</param>
        /// <returns>Compressed byte array</returns>
        public static byte[] CompressLayer(Stream input)
        {
            return CompressLayer(input.ToArray());
        }

        /// <summary>
        /// Compress a layer from a byte array
        /// </summary>
        /// <param name="input">byte array to compress</param>
        /// <returns>Compressed byte array</returns>
        public static byte[] CompressLayer(byte[] input)
        {
            return input;
            /*using (MemoryStream output = new MemoryStream())
            {
                using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
                {
                    dstream.Write(input, 0, input.Length);
                }
                return output.ToArray();
            }*/
        }

        /// <summary>
        /// Decompress a layer from a byte array
        /// </summary>
        /// <param name="input">byte array to decompress</param>
        /// <returns>Decompressed byte array</returns>
        public static byte[] DecompressLayer(byte[] input)
        {
            return input;
            /*using (MemoryStream ms = new MemoryStream(input))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    using (DeflateStream dstream = new DeflateStream(ms, CompressionMode.Decompress))
                    {
                        dstream.CopyTo(output);
                    }
                    return output.ToArray();
                }
            }*/
        }
        #endregion

        #region Methods

        /// <summary>
        /// Add a layer
        /// </summary>
        /// <param name="index">Layer index</param>
        /// <param name="layer">Layer to add</param>
        public void AddLayer(uint index, Layer layer)
        {
            Layers[index] = layer;
            layer.ParentLayerManager = this;
        }

        /// <summary>
        /// Get layer given index
        /// </summary>
        /// <param name="index">Layer index</param>
        /// <returns></returns>
        public Layer GetLayer(uint index)
        {
            return Layers[index];
        }

        /// <summary>
        /// Resizes layer images in x and y factor, starting at 1 = 100%
        /// </summary>
        /// <param name="startLayerIndex">Layer index to start</param>
        /// <param name="endLayerIndex">Layer index to end</param>
        /// <param name="x">X factor, starts at 1</param>
        /// <param name="y">Y factor, starts at 1</param>
        /// <param name="isFade">Fade X/Y towards 100%</param>
        public void MutateResize(uint startLayerIndex, uint endLayerIndex, double x, double y, bool isFade)
        {
            if (x == 1.0 && y == 1.0) return;

            double xSteps = Math.Abs(x - 1.0) / (endLayerIndex - startLayerIndex);
            double ySteps = Math.Abs(y - 1.0) / (endLayerIndex - startLayerIndex);

            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                var newX = x;
                var newY = y;
                if (isFade)
                {
                    if (newX != 1.0)
                    {
                        
                        //maxIteration = Math.Max(iterationsStart, iterationsEnd);

                        newX = (float)(newX < 1.0
                            ? newX + (layerIndex - startLayerIndex) * xSteps
                            : newX - (layerIndex - startLayerIndex) * xSteps);

                        // constrain
                        //iterations = Math.Min(Math.Max(1, iterations), maxIteration);
                    }

                    if (y != 1.0)
                    {
                        
                        //maxIteration = Math.Max(iterationsStart, iterationsEnd);

                        newY = (float)(newY < 1.0
                            ? newY + (layerIndex - startLayerIndex) * ySteps
                            : newY - (layerIndex - startLayerIndex) * ySteps);

                        // constrain
                        //iterations = Math.Min(Math.Max(1, iterations), maxIteration);
                    }
                }

                if (newX == 1.0 && newY == 1.0) return;

                this[layerIndex].MutateResize(newX, newY);
            });
        }

        public void MutateSolidify(uint startLayerIndex, uint endLayerIndex)
        {
            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                this[layerIndex].MutateSolidify();
            });
        }

        private void MutateGetVarsIterationFade(uint startLayerIndex, uint endLayerIndex, int iterationsStart, int iterationsEnd, ref bool isFade, out int iterationSteps, out int maxIteration)
        {
            iterationSteps = 0;
            maxIteration = 0;
            isFade = isFade && startLayerIndex != endLayerIndex && iterationsStart != iterationsEnd;
            if (!isFade) return;
            iterationSteps = (int)Math.Abs((double)(iterationsStart - iterationsEnd) / (endLayerIndex - startLayerIndex));
            maxIteration = Math.Max(iterationsStart, iterationsEnd);
        }

        private int MutateGetIterationVar(bool isFade, int iterationsStart, int iterationsEnd, int iterationSteps, int maxIteration, uint startLayerIndex, uint layerIndex)
        {
            if(!isFade) return iterationsStart;
            // calculate iterations based on range
            int iterations = (int)(iterationsStart < iterationsEnd
                ? iterationsStart + (layerIndex - startLayerIndex) * iterationSteps
                : iterationsStart - (layerIndex - startLayerIndex) * iterationSteps);

            // constrain
            return Math.Min(Math.Max(1, iterations), maxIteration);
        }

        public void MutateErode(uint startLayerIndex, uint endLayerIndex, int iterationsStart = 1, int iterationsEnd = 1, bool isFade = false,
            IInputArray kernel = null, Point anchor = default,
            BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            MutateGetVarsIterationFade(
                startLayerIndex, 
                endLayerIndex, 
                iterationsStart,
                iterationsEnd, 
                ref isFade,
                out var iterationSteps, 
                out var maxIteration
                );

            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                int iterations = MutateGetIterationVar(isFade, iterationsStart, iterationsEnd, iterationSteps, maxIteration, startLayerIndex, (uint) layerIndex);
                this[layerIndex].MutateErode(iterations, kernel, anchor, borderType, borderValue);
            });
        }

        public void MutateDilate(uint startLayerIndex, uint endLayerIndex, int iterationsStart = 1, int iterationsEnd = 1, bool isFade = false,
            IInputArray kernel = null, Point anchor = default,
            BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            MutateGetVarsIterationFade(
                startLayerIndex,
                endLayerIndex,
                iterationsStart,
                iterationsEnd,
                ref isFade,
                out var iterationSteps,
                out var maxIteration
            );

            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                int iterations = MutateGetIterationVar(isFade, iterationsStart, iterationsEnd, iterationSteps, maxIteration, startLayerIndex, (uint)layerIndex);
                this[layerIndex].MutateDilate(iterations, kernel, anchor, borderType, borderValue);
            });
        }

        public void MutateOpen(uint startLayerIndex, uint endLayerIndex, int iterationsStart = 1, int iterationsEnd = 1, bool isFade = false,
            IInputArray kernel = null, Point anchor = default,
            BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            MutateGetVarsIterationFade(
                startLayerIndex,
                endLayerIndex,
                iterationsStart,
                iterationsEnd,
                ref isFade,
                out var iterationSteps,
                out var maxIteration
            );

            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                int iterations = MutateGetIterationVar(isFade, iterationsStart, iterationsEnd, iterationSteps, maxIteration, startLayerIndex, (uint)layerIndex);
                this[layerIndex].MutateOpen(iterations, kernel, anchor, borderType, borderValue);
            });
        }

        public void MutateClose(uint startLayerIndex, uint endLayerIndex, int iterationsStart = 1, int iterationsEnd = 1, bool isFade = false,
            IInputArray kernel = null, Point anchor = default,
            BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            MutateGetVarsIterationFade(
                startLayerIndex,
                endLayerIndex,
                iterationsStart,
                iterationsEnd,
                ref isFade,
                out var iterationSteps,
                out var maxIteration
            );

            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                int iterations = MutateGetIterationVar(isFade, iterationsStart, iterationsEnd, iterationSteps, maxIteration, startLayerIndex, (uint)layerIndex);
                this[layerIndex].MutateClose(iterations, kernel, anchor, borderType, borderValue);
            });
        }

        public void MutateGradient(uint startLayerIndex, uint endLayerIndex, int iterationsStart = 1, int iterationsEnd = 1, bool isFade = false,
            IInputArray kernel = null, Point anchor = default,
            BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            MutateGetVarsIterationFade(
                startLayerIndex,
                endLayerIndex,
                iterationsStart,
                iterationsEnd,
                ref isFade,
                out var iterationSteps,
                out var maxIteration
            );

            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                int iterations = MutateGetIterationVar(isFade, iterationsStart, iterationsEnd, iterationSteps, maxIteration, startLayerIndex, (uint)layerIndex);
                this[layerIndex].MutateGradient(iterations, kernel, anchor, borderType, borderValue);
            });
        }

        public void MutatePyrDownUp(uint startLayerIndex, uint endLayerIndex, BorderType borderType = BorderType.Default)
        {
            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                this[layerIndex].MutatePyrDownUp(borderType);
            });
        }

        public void MutateMedianBlur(uint startLayerIndex, uint endLayerIndex, int aperture = 1)
        {
            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                this[layerIndex].MutateMedianBlur(aperture);
            });
        }

        public void MutateGaussianBlur(uint startLayerIndex, uint endLayerIndex, Size size = default, int sigmaX = 0, int sigmaY = 0, BorderType borderType = BorderType.Reflect101)
        {
            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                this[layerIndex].MutateGaussianBlur(size, sigmaX, sigmaY, borderType);
            });
        }

        public ConcurrentDictionary<uint, List<LayerIssue>> GetAllIssues()
        {
            const byte requiredPixelsToSupportIsland = 10;
            const byte minIslandPixelToCheck = 10;
            const byte minIslandSupportPixelColor = 150;
            const byte minTouchingBondsPixelColor = 200;

            var result = new ConcurrentDictionary<uint, List<LayerIssue>>();

            /*Parallel.ForEach(this, layer =>
            {
                var issues = layer.GetIssues();
                if (issues.Count > 0)
                {
                    if (!result.TryAdd(layer.Index, issues))
                    {
                        throw new AccessViolationException("Error while trying to add an issue to the dictionary, please try again.");
                    }
                }
            });*/

            var task1 = new TaskFactory().StartNew(() =>
            {
                // Detect contours
                Parallel.ForEach(this,
                    //new ParallelOptions{MaxDegreeOfParallelism = 1},
                    layer =>
                    {
                        using (var image = layer.LayerMat)
                        {
                            int step = image.Step;
                            var span = image.GetPixelSpan<byte>();

                            // TouchingBounds Checker
                            List<Point> pixels = new List<Point>();
                            for (int x = 0; x < image.Width; x++) // Check Top and Bottom bounds
                            {
                                if (span[x] >= minTouchingBondsPixelColor) // Top
                                {
                                    pixels.Add(new Point(x, 0));
                                }

                                if (span[step * image.Height - step + x] >= minTouchingBondsPixelColor) // Bottom
                                {
                                    pixels.Add(new Point(x, image.Height - 1));
                                }
                            }

                            for (int y = 0; y < image.Height; y++) // Check Left and Right bounds
                            {
                                if (span[y * step] >= minTouchingBondsPixelColor) // Left
                                {
                                    pixels.Add(new Point(0, y));
                                }

                                if (span[y * step + step - 1] >= minTouchingBondsPixelColor) // Right
                                {
                                    pixels.Add(new Point(step - 1, y));
                                }
                            }

                            if (pixels.Count > 0)
                            {
                                result.TryAdd(layer.Index, new List<LayerIssue>
                                {
                                    new LayerIssue(layer, LayerIssue.IssueType.TouchingBound, pixels.ToArray())
                                });
                            }

                            if (layer.Index == 0) return; // No inslands for layer 0

                            CvInvoke.Threshold(image, image, 1, 255, ThresholdType.Binary);
                            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                            Mat hierarchy = new Mat();

                            CvInvoke.FindContours(image, contours, hierarchy, RetrType.Ccomp,
                                ChainApproxMethod.ChainApproxSimple);

                            var arr = hierarchy.GetData();
                            //
                            //hierarchy[i][0]: the index of the next contour of the same level
                            //hierarchy[i][1]: the index of the previous contour of the same level
                            //hierarchy[i][2]: the index of the first child
                            //hierarchy[i][3]: the index of the parent
                            //

                            Mat previousImage = null;
                            Span<byte> previousSpan = null;
                            Span<byte> imageSpan = null;
                            for (int i = 0; i < contours.Size; i++)
                            {
                                if ((int) arr.GetValue(0, i, 2) == -1 && (int) arr.GetValue(0, i, 3) != -1) continue;
                                if (ReferenceEquals(previousImage, null))
                                {
                                    previousImage = this[layer.Index - 1].LayerMat;
                                    previousSpan = previousImage.GetPixelSpan<byte>();
                                    imageSpan = image.GetPixelSpan<byte>();
                                }

                                var rect = CvInvoke.BoundingRectangle(contours[i]);
                                List<Point> points = new List<Point>();
                                uint pixelsSupportingIsland = 0;

                                using (Mat contourImage = image.CloneBlank())
                                {
                                    CvInvoke.DrawContours(contourImage, contours, i, new MCvScalar(255), -1);
                                    var contourImageSpan = contourImage.GetPixelSpan<byte>();

                                    for (int y = rect.Y; y < rect.Bottom; y++)
                                    {
                                        for (int x = rect.X; x < rect.Right; x++)
                                        {
                                            //int pixel = image.GetPixelPos(x, y);
                                            int pixel = step * y + x;
                                            if (imageSpan[pixel] < minIslandPixelToCheck)
                                                continue; // Low brightness, ignore
                                            if (contourImageSpan[pixel] != 255)
                                                continue; // Not inside contour, ignore

                                            //if (CvInvoke.PointPolygonTest(contours[i], new PointF(x, y), false) < 0) continue; // Out of contour SLOW!
                                            //Debug.WriteLine($"Layer: {layer.Index}, Coutour: {i},  X:{x} Y:{y}");
                                            points.Add(new Point(x, y));

                                            if (previousSpan[pixel] >= minIslandSupportPixelColor)
                                            {
                                                pixelsSupportingIsland++;
                                            }
                                        }
                                    }
                                }

                                if (points.Count == 0) continue;
                                if (pixelsSupportingIsland >= requiredPixelsToSupportIsland)
                                    continue; // Not a island, bounding is strong, i think...
                                if (pixelsSupportingIsland > 0 && points.Count < requiredPixelsToSupportIsland &&
                                    pixelsSupportingIsland >= Math.Max(1, points.Count / 2))
                                    continue; // Not a island, but maybe weak bounding...


                                var issue = new LayerIssue(layer, LayerIssue.IssueType.Island, points.ToArray(), rect);
                                result.AddOrUpdate(layer.Index, new List<LayerIssue> {issue},
                                    (layerIndex, list) =>
                                    {
                                        list.Add(issue);
                                        return list;
                                    });
                            }

                            contours.Dispose();
                            hierarchy.Dispose();
                            previousImage?.Dispose();
                        }
                    });
            });

            var layerHollowAreas = new ConcurrentDictionary<uint, List<LayerHollowArea>>();
            var task2 = new TaskFactory().StartNew(() =>
            {
                // Detect contours
                Parallel.ForEach(this,
                    //new ParallelOptions{MaxDegreeOfParallelism = 1},
                    layer =>
                    {
                        using (var image = layer.LayerMat)
                        {
                            CvInvoke.Threshold(image, image, 254, 255, ThresholdType.Binary);

                            var listHollowArea = new List<LayerHollowArea>();

                            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                            {
                                using (Mat hierarchy = new Mat())
                                {

                                    CvInvoke.FindContours(image, contours, hierarchy, RetrType.Ccomp,
                                        ChainApproxMethod.ChainApproxSimple);

                                    var arr = hierarchy.GetData();
                                    //
                                    //hierarchy[i][0]: the index of the next contour of the same level
                                    //hierarchy[i][1]: the index of the previous contour of the same level
                                    //hierarchy[i][2]: the index of the first child
                                    //hierarchy[i][3]: the index of the parent
                                    //

                                    for (int i = 0; i < contours.Size; i++)
                                    {
                                        if ((int) arr.GetValue(0, i, 2) != -1 || (int) arr.GetValue(0, i, 3) == -1)
                                            continue;
                                        listHollowArea.Add(new LayerHollowArea(contours[i].ToArray(),
                                            CvInvoke.BoundingRectangle(contours[i]),
                                            layer.Index == 0 || layer.Index == Count - 1
                                                ? LayerHollowArea.AreaType.Drain
                                                : LayerHollowArea.AreaType.Unknown));

                                        if (listHollowArea.Count > 0)
                                            layerHollowAreas.TryAdd(layer.Index, listHollowArea);
                                    }
                                }
                            }
                        }
                    });


                for (uint layerIndex = 1; layerIndex < Count - 1; layerIndex++) // Ignore first and last layers, always drains
                {
                    if (!layerHollowAreas.TryGetValue(layerIndex, out var areas))
                        continue; // No hollow areas in this layer, ignore

                    byte areaCount = 0;
                    //foreach (var area in areas)
                    
                    Parallel.ForEach(from t in areas where t.Type == LayerHollowArea.AreaType.Unknown select t, area =>
                    {
                        //if (area.Type != LayerHollowArea.AreaType.Unknown) return; // processed, ignore
                        area.Type = LayerHollowArea.AreaType.Trap;

                        areaCount++;

                        List<LayerHollowArea> linkedAreas = new List<LayerHollowArea>();

                        for (sbyte dir = 1; dir >= -1 && area.Type != LayerHollowArea.AreaType.Drain; dir -= 2) 
                        //Parallel.ForEach(new sbyte[] {1, -1}, new ParallelOptions {MaxDegreeOfParallelism = 2}, dir =>
                        {
                            Queue<LayerHollowArea> queue = new Queue<LayerHollowArea>();
                            queue.Enqueue(area);
                            area.Processed = false;
                            int nextLayerIndex = (int) layerIndex;
                            while (queue.Count > 0 && area.Type != LayerHollowArea.AreaType.Drain)
                            {
                                LayerHollowArea checkArea = queue.Dequeue();
                                if (checkArea.Processed) continue;
                                checkArea.Processed = true;
                                nextLayerIndex += dir;
                                Debug.WriteLine($"Area Count: {areaCount} | Layer: {layerIndex} | Next Layer: {nextLayerIndex} | Dir: {dir}");
                                if (nextLayerIndex < 0 && nextLayerIndex >= Count)
                                    break; // Exhausted layers
                                bool haveNextAreas = layerHollowAreas.TryGetValue((uint) nextLayerIndex, out var nextAreas);
                                List<LayerHollowArea> intersectingAreas = new List<LayerHollowArea>();

                                using (var image = this[nextLayerIndex].LayerMat)
                                {
                                    var span = image.GetPixelSpan<byte>();
                                    using (var emguImage = image.CloneBlank())
                                    {
                                        using(var vec = new VectorOfVectorOfPoint(new VectorOfPoint(checkArea.Contour)))
                                        {
                                            CvInvoke.DrawContours(emguImage, vec, -1, new MCvScalar(255), -1);
                                        }

                                        if (haveNextAreas)
                                        {
                                            foreach (var nextArea in nextAreas)
                                            {
                                                if (!checkArea.BoundingRectangle.IntersectsWith(nextArea.BoundingRectangle)) continue;

                                                intersectingAreas.Add(nextArea);
                                                /*CvInvoke.DrawContours(emguImage,
                                                    new VectorOfVectorOfPoint(new VectorOfPoint(nextArea.Contour)),
                                                    -1,
                                                    new MCvScalar(intersectingAreas.Count), -1);*/
                                            }
                                            if (intersectingAreas.Count == 0)
                                            {
                                                haveNextAreas = false;
                                            }
                                        }

                                        bool exitPixelLoop = false;
                                        uint blackCount = 0;

                                        var spanContour = emguImage.GetPixelSpan<byte>();
                                        for (int y = checkArea.BoundingRectangle.Y;
                                            y <= checkArea.BoundingRectangle.Bottom &&
                                            area.Type != LayerHollowArea.AreaType.Drain && !exitPixelLoop;
                                            y++)
                                        {
                                            int pixelPos = image.GetPixelPos(checkArea.BoundingRectangle.X, y)-1;
                                            for (int x = checkArea.BoundingRectangle.X;
                                                x <= checkArea.BoundingRectangle.Right &&
                                                area.Type != LayerHollowArea.AreaType.Drain && !exitPixelLoop;
                                                x++)
                                            {
                                                pixelPos++;

                                                if (spanContour[pixelPos] == 0) continue; // No contour
                                                if (span[pixelPos] > 30) continue; // Threshold to ignore white area
                                                blackCount++;

                                                if (haveNextAreas) // Have areas, can be on same area path or not
                                                {
                                                    /*int i = spanContour[pixelPos] - 1;
                                                    if (i == -1 || i >= 254)
                                                        continue;

                                                    //if(queue.Contains(intersectingAreas[i])) continue;
                                                    //Debug.WriteLine($"BlackCount: {blackCount}, pixel color: {i}, layerindex: {layerIndex}");
                                                    
                                                    if (intersectingAreas[i].Type == LayerHollowArea.AreaType.Drain) // Found a drain, stop query
                                                    {
                                                        area.Type = LayerHollowArea.AreaType.Drain;
                                                    }
                                                    else
                                                    {
                                                        queue.Enqueue(intersectingAreas[i]);
                                                    }

                                                    linkedAreas.Add(intersectingAreas[i]);

                                                    exitPixelLoop = true;
                                                    break;*/
                                                    foreach (var nextAreaCheck in intersectingAreas)
                                                    {
                                                        using (var vec = new VectorOfPoint(nextAreaCheck.Contour))
                                                        {
                                                            //Debug.WriteLine(CvInvoke.PointPolygonTest(vec, new PointF(x, y), false));
                                                            if (CvInvoke.PointPolygonTest(vec, new PointF(x, y), false) < 0) continue;
                                                        }

                                                        if (nextAreaCheck.Type == LayerHollowArea.AreaType.Drain
                                                        ) // Found a drain, stop query
                                                        {
                                                            area.Type = LayerHollowArea.AreaType.Drain;
                                                            exitPixelLoop = true;
                                                        }
                                                        else
                                                        {
                                                            queue.Enqueue(area);
                                                        }

                                                        linkedAreas.Add(nextAreaCheck);
                                                        intersectingAreas.Remove(nextAreaCheck);
                                                        haveNextAreas = intersectingAreas.Count > 0;
                                                        break;
                                                        //exitPixelLoop = true;
                                                        //break;

                                                    }
                                                }
                                                else if (blackCount > Math.Min(checkArea.Contour.Length / 2, 10)) // Black pixel without next areas = Drain
                                                {
                                                    area.Type = LayerHollowArea.AreaType.Drain;
                                                    exitPixelLoop = true;
                                                    break;
                                                }
                                            } // X loop
                                        } // Y loop

                                        if (queue.Count == 0 && blackCount > Math.Min(checkArea.Contour.Length / 2, 10))
                                        {

                                            area.Type = LayerHollowArea.AreaType.Drain;
                                        }

                                    } // Dispose emgu image
                                } // Dispose image
                            } // Areas loop
                        } // Dir layer loop

                        foreach (var linkedArea in linkedAreas) // Update linked areas
                        {
                            linkedArea.Type = area.Type;
                        }
                    });
                }
            });

            task1.Wait(); // Islands & bounds
            task2.Wait(); // Resin trap

            for (uint layerIndex = 0; layerIndex < Count; layerIndex++)
            {
                if (!layerHollowAreas.TryGetValue(layerIndex, out var list)) continue;
                if (list.Count == 0) continue;
                foreach (var issue in 
                        from area 
                        in list 
                        where area.Type == LayerHollowArea.AreaType.Trap 
                        select new LayerIssue(this[layerIndex], LayerIssue.IssueType.ResinTrap, area.Contour, area.BoundingRectangle))
                {
                    result.AddOrUpdate(layerIndex, new List<LayerIssue> {issue}, (u, listIssues) =>
                    {
                        listIssues.Add(issue);
                        return listIssues;
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Desmodify all layers
        /// </summary>
        public void Desmodify()
        {
            for (uint i = 0; i < Count; i++)
            {
                Layers[i].IsModified = false;
            }
        }

        /// <summary>
        /// Clone this object
        /// </summary>
        /// <returns></returns>
        public LayerManager Clone()
        {
            LayerManager layerManager = new LayerManager(Count);
            foreach (var layer in this)
            {
                layerManager[layer.Index] = layer.Clone();
            }

            return layerManager;
        }


        #endregion

    }
    #endregion
}
