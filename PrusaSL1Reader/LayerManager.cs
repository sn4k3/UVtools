/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PrusaSL1Reader.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Point = System.Drawing.Point;

namespace PrusaSL1Reader
{
    #region LayerIsland Class
    public class LayerIsland : IEnumerable<Point>
    {
        /// <summary>
        /// Gets the layer who own this island
        /// </summary>
        public Layer Owner { get; }

        public uint X => (uint) Pixels[0].X;

        public uint Y => (uint)Pixels[0].Y;

        public Point Point => new Point((int) X, (int) Y);

        /// <summary>
        /// Gets pixels locations
        /// </summary>
        public Point[] Pixels { get; }

        /// <summary>
        /// Gets the number of pixels on this island
        /// </summary>
        public uint Size => (uint) Pixels.Length;

        public LayerIsland(Layer owner, Point[] pixels)
        {
            Owner = owner;
            Pixels = pixels;
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

        private byte[] _rawData;
        /// <summary>
        /// Gets or sets layer image compressed data
        /// </summary>
        public byte[] RawData
        {
            get => LayerManager.DecompressLayer(_rawData);
            set
            {
                _rawData = LayerManager.CompressLayer(value);
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
        public Image<L8> Image
        {
            get => SixLabors.ImageSharp.Image.Load<L8>(RawData);
            set
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    value.Save(stream, Helpers.PngEncoder);
                    RawData = stream.ToArray();
                }
            }
        }

        /// <summary>
        /// Gets a new RGBA image instance
        /// </summary>
        public Image<Rgba32> ImageRbga32 => Image.CloneAs<Rgba32>();

        #endregion

        #region Constructor
        public Layer(uint index, byte[] rawData, string filename = null, LayerManager pararentLayerManager = null)
        {
            Index = index;
            RawData = rawData;
            Filename = filename ?? $"Layer{index}.png";
            IsModified = false;
            ParentLayerManager = pararentLayerManager;
        }

        public Layer(uint index, Image<L8> image, string filename = null, LayerManager pararentLayerManager = null) : this(index, new byte[0], filename, pararentLayerManager)
        {
            Image = image;
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
            return Equals(_rawData, other._rawData);
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
            return (_rawData != null ? _rawData.GetHashCode() : 0);
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
            return $"{nameof(Filename)}: {Filename}, {nameof(IsModified)}: {IsModified}";
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
        public List<LayerIsland> GetIslandsLocation(uint requiredPixelsToSupportIsland = 5)
        {
            if (requiredPixelsToSupportIsland == 0)
                requiredPixelsToSupportIsland = 1;

            // These arrays are used to 
            // get row and column numbers 
            // of 8 neighbors of a given cell 
            List<LayerIsland> result = new List<LayerIsland>();
            List<Point> pixels = new List<Point>();
            if (Index == 0) return result;

            sbyte[] rowNbr = { -1, -1, -1, 0, 0, 1, 1, 1 };
            sbyte[] colNbr = { -1, 0, 1, -1, 1, -1, 0, 1 };
            const uint minPixel = 10;
            const uint minPixelForSupportIsland = 200;
            int pixelIndex;
            uint islandSupportingPixels;

            var image = Image;
            byte[] bytes = null;
            if (image.TryGetSinglePixelSpan(out var pixelSpan))
            {
                bytes = MemoryMarshal.AsBytes(pixelSpan).ToArray();
            }

            var previousLayerImage = PreviousLayer()?.Image;
            byte[] previousBytes = null;
            if (!ReferenceEquals(previousLayerImage, null))
            {
                if (previousLayerImage.TryGetSinglePixelSpan(out var previousPixelSpan))
                {
                    previousBytes = MemoryMarshal.AsBytes(previousPixelSpan).ToArray();
                }
            }

            // Make a bool array to
            // mark visited cells. 
            // Initially all cells 
            // are unvisited 
            bool[,] visited = new bool[image.Height, image.Width];

            /*bool isSafe()
            {
                // row number is in range, 
                // column number is in range 
                // and value is 1 and not 
                // yet visited 
                pixelIndex = y2 * image.Width + x2;
                return (y2 >= 0) && (y2 < image.Height) && (x2 >= 0) && (x2 < image.Width) && (bytes[pixelIndex] >= minPixel && !visited[y2, x2]);
            }*/


            void DFS(int y2, int x2)
            {
                Queue<System.Drawing.Point> queue = new Queue<System.Drawing.Point>();
                queue.Enqueue(new System.Drawing.Point(x2, y2));
                // Mark this cell as visited 
                visited[y2, x2] = true;

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
                            !visited[tempy2, tempx2])
                        {
                            visited[tempy2, tempx2] = true;
                            point = new Point(tempx2, tempy2);
                            pixels.Add(point);
                            queue.Enqueue(point);

                            islandSupportingPixels += previousBytes[pixelIndex] >= minPixelForSupportIsland ? 1u : 0;
                            /*if (!presetOnPrevious)
                            {
                                if (previousBytes[pixelIndex] >= minPixelForSupportIsland) presetOnPrevious = true;
                            }*/
                        }
                    }
                }
            }

            // Initialize count as 0 and 
            // travese through the all 
            // cells of given matrix 
            //uint count = 0;
            for (int y = 0; y < image.Height; ++y)
            {
                for (int x = 0; x < image.Width; ++x)
                {
                    pixelIndex = y * image.Width + x;
                    if (bytes[pixelIndex] > minPixel && !visited[y, x])
                    {
                        // If a cell with value 1 is not 
                        // visited yet, then new island 
                        // found, Visit all cells in this 
                        // island and increment island count 
                        pixels.Clear();
                        pixels.Add(new Point(x, y));
                        islandSupportingPixels = previousBytes[pixelIndex] >= minPixelForSupportIsland ? 1u : 0;
                        DFS(y, x);
                        //count++;

                        if (islandSupportingPixels >= requiredPixelsToSupportIsland) continue; // Not a island, bounding is strong
                        if (islandSupportingPixels > 0 && pixels.Count < requiredPixelsToSupportIsland && islandSupportingPixels >= Math.Max(1, pixels.Count / 2)) continue; // Not a island
                        result.Add(new LayerIsland(this, pixels.ToArray()));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Count the number of islands on this layer
        /// </summary>
        /// <returns>Number of islands</returns>
        public uint CountIslands => (uint)GetIslandsLocation().Count;


        public Layer Clone()
        {
            return new Layer(Index, RawData, Filename, ParentLayerManager);
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

        public Dictionary<uint, List<LayerIsland>> GetAllIslands()
        {
            var result = new Dictionary<uint, List<LayerIsland>>((int) Count);

            Parallel.ForEach(this, (layer) =>
            {
                result[layer.Index] = layer.GetIslandsLocation();
            });

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
