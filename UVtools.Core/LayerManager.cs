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
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core.Extensions;

namespace UVtools.Parser
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
            return $"{nameof(Type)}: {Type}";
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
            get {
                using (Mat image = LayerMat)
                {
                    Mat mat = new Mat();
                    CvInvoke.CvtColor(image, mat, ColorConversion.Gray2Bgr);
                    return mat;
                }
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
            var bytes = mat.GetBytes();



            var previousLayerImage = PreviousLayer()?.LayerMat;
            byte[] previousBytes = previousLayerImage?.GetBytes();
            

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

        public Layer Clone()
        {
            return new Layer(Index, CompressedBytes, Filename, ParentLayerManager);
        }
        #endregion

        public void Resize(float newWidth, float newHeight)
        {
            using (var mat = LayerMat)
            {
                using (var resizedMat = new Mat())
                {
                    int width = (int)(mat.Width * newWidth);
                    int height = (int)(mat.Height * newHeight);
                    Point location = new Point(mat.Width / 2 - width / 2, mat.Height / 2 - height / 2);

                    CvInvoke.Resize(mat, resizedMat, new Size(width, height));
                    mat.SetTo(new MCvScalar(0));
                    resizedMat.CopyTo(mat);
                    LayerMat = mat;
                }
            }
        }
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

        public ConcurrentDictionary<uint, List<LayerIssue>> GetAllIssues()
        {
            var result = new ConcurrentDictionary<uint, List<LayerIssue>>();

            Parallel.ForEach(this, layer =>
            {
                var issues = layer.GetIssues();
                if (issues.Count > 0)
                {
                    if (!result.TryAdd(layer.Index, issues))
                    {
                        throw new AccessViolationException("Error while trying to add an issue to the dictionary, please try again.");
                    }
                }
            });

            /*const byte minPixel = 50;
            sbyte[] rowNbr = { -1, -1, -1, 0, 0, 1, 1, 1 };
            sbyte[] colNbr = { -1, 0, 1, -1, 1, -1, 0, 1 };

            int pixelIndex;
            for (uint layerindex = 0; layerindex < Count; layerindex++)
            {
                var image = this[layerindex].Image;
                byte[] bytes = null;
                if (image.TryGetSinglePixelSpan(out var pixelSpan))
                {
                    bytes = MemoryMarshal.AsBytes(pixelSpan).ToArray();
                }

                bool[,] visited = new bool[image.Width, image.Height];

                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        pixelIndex = y * image.Width + x;
                        if (bytes[pixelIndex] > minPixel && !visited[y, x])
                        {
                            Queue<Point> queue = new Queue<Point>();
                            queue.Enqueue(new Point(x, y));
                            // Mark this cell as visited 
                            visited[x, y] = true;

                            var x2 = x;
                            var y2 = y;

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
                                    pixelIndex = tempy2 * image.Width + tempx2;
                                    if (tempy2 >= 0 &&
                                        tempy2 < image.Height &&
                                        tempx2 >= 0 && tempx2 < image.Width &&
                                        bytes[pixelIndex] >= minPixel &&
                                        !visited[tempx2, tempy2])
                                    {
                                        visited[tempx2, tempy2] = true;
                                        point = new Point(tempx2, tempy2);
                                        pixels.Add(point);
                                        queue.Enqueue(point);

                                        islandSupportingPixels += previousBytes[pixelIndex] >= minPixelForSupportIsland ? 1u : 0;
                                    }
                                }
                            }
                        }
                    }
                }
            }*/

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
