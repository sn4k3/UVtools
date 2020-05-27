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
using PrusaSL1Reader.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PrusaSL1Reader
{
    public class LayerManager : IEnumerable<LayerManager.Layer>
    {
        #region Layer Class
        /// <summary>
        /// Represent a Layer
        /// </summary>
        public class Layer : IEquatable<Layer>, IEquatable<uint>
        {
            #region Properties
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
                get => DecompressLayer(_rawData);
                set { _rawData = CompressLayer(value); IsModified = true; }
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

            #endregion

            #region Constructor
            public Layer(uint index, byte[] rawData, string filename = null)
            {
                Index = index;
                RawData = rawData;
                Filename = filename ?? $"Layer{index}.png";
                IsModified = false;
            }

            public Layer(uint index, Image<L8> image, string filename = null) : this(index, new byte[0], filename)
            {
                Image = image;
                IsModified = false;
            }


            public Layer(uint index, Stream stream, string filename = null) : this(index, stream.ToArray(), filename)
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
                return Equals((Layer) obj);
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
            public Layer Clone()
            {
                return new Layer(Index, RawData, Filename);
            }
            #endregion
        }
        #endregion

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
            set => Layers[index] = value;
        }
        public Layer this[int index]
        {
            get => Layers[index];
            set => Layers[index] = value;
        }
        public Layer this[long index]
        {
            get => Layers[index];
            set => Layers[index] = value;
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
}
