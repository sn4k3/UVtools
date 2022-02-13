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
using System.Linq;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using MoreLinq.Extensions;
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Managers;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.Core.PixelEditor;

namespace UVtools.Core
{
    public class LayerManager : BindableBase, IList<Layer>, IDisposable
    {
        #region Properties
        public FileFormat SlicerFile { get; set; }

        private Layer[] _layers;

        /// <summary>
        /// Layers List
        /// </summary>
        public Layer[] Layers
        {
            get => _layers;
            set
            {
                //if (ReferenceEquals(_layers, value)) return;

                var rebuildProperties = false;
                var oldLayerCount = LayerCount;
                var oldLayers = _layers;
                _layers = value;
                BoundingRectangle = Rectangle.Empty;

                if (LayerCount != oldLayerCount)
                {
                    SlicerFile.LayerCount = LayerCount;
                }

                SlicerFile.RequireFullEncode = true;
                SlicerFile.PrintHeight = SlicerFile.PrintHeight;
                SlicerFile.UpdatePrintTime();

                if (value is not null && LayerCount > 0)
                {
                    //SetAllIsModified(true);

                    for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++) // Forced sanitize
                    {
                        if (_layers[layerIndex] is null) continue;
                        _layers[layerIndex].Index = layerIndex;
                        _layers[layerIndex].ParentLayerManager = this;

                        if (layerIndex >= oldLayerCount || layerIndex < oldLayerCount && !_layers[layerIndex].Equals(oldLayers[layerIndex]))
                        {
                            // Marks as modified only if layer image changed on this index
                            _layers[layerIndex].IsModified = true;
                        }
                    }

                    if (LayerCount != oldLayerCount && !SlicerFile.SuppressRebuildProperties && LastLayer is not null)
                    {
                        RebuildLayersProperties();
                        rebuildProperties = true;
                    }
                }

                if (!rebuildProperties)
                {
                    SlicerFile.MaterialMilliliters = -1;
                    SlicerFile.RebuildGCode();
                }

                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets the last layer index
        /// </summary>
        public uint LastLayerIndex => LayerCount - 1;

        /// <summary>
        /// Gets the first layer
        /// </summary>
        public Layer FirstLayer => _layers?[0];

        /// <summary>
        /// Gets the last layer
        /// </summary>
        public Layer LastLayer => _layers?[^1];

        /// <summary>
        /// Gets the smallest bottom layer using the pixel count
        /// </summary>
        public Layer SmallestBottomLayer => _layers?.Where(layer => layer.IsBottomLayer && !layer.IsEmpty).MinBy(layer => layer.NonZeroPixelCount).FirstOrDefault();

        /// <summary>
        /// Gets the largest bottom layer using the pixel count
        /// </summary>
        public Layer LargestBottomLayer => _layers?.Where(layer => layer.IsBottomLayer && !layer.IsEmpty).MaxBy(layer => layer.NonZeroPixelCount).FirstOrDefault();

        /// <summary>
        /// Gets the smallest normal layer using the pixel count
        /// </summary>
        public Layer SmallestNormalLayer => _layers?.Where(layer => layer.IsNormalLayer && !layer.IsEmpty).MinBy(layer => layer.NonZeroPixelCount).FirstOrDefault();

        /// <summary>
        /// Gets the largest layer using the pixel count
        /// </summary>
        public Layer LargestNormalLayer => _layers?.Where(layer => layer.IsNormalLayer && !layer.IsEmpty).MaxBy(layer => layer.NonZeroPixelCount).FirstOrDefault();

        /// <summary>
        /// Gets the smallest normal layer using the pixel count
        /// </summary>
        public Layer SmallestLayer => _layers?.Where(layer => !layer.IsEmpty).MinBy(layer => layer.NonZeroPixelCount).FirstOrDefault();

        /// <summary>
        /// Gets the largest layer using the pixel count
        /// </summary>
        public Layer LargestLayer => _layers?.MaxBy(layer => layer.NonZeroPixelCount).FirstOrDefault();


        /// <summary>
        /// Gets the bounding rectangle of the object
        /// </summary>
        private Rectangle _boundingRectangle = Rectangle.Empty;

        /// <summary>
        /// Gets the bounding rectangle of the object
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get => GetBoundingRectangle();
            set
            {
                RaiseAndSetIfChanged(ref _boundingRectangle, value);
                RaisePropertyChanged(nameof(BoundingRectangleMillimeters));
            }
        }

        /// <summary>
        /// Gets the bounding rectangle of the object in millimeters
        /// </summary>
        public RectangleF BoundingRectangleMillimeters
        {
            get
            {
                if (SlicerFile is null) return RectangleF.Empty;
                var pixelSize = SlicerFile.PixelSize;
                return new RectangleF(
                    (float)Math.Round(_boundingRectangle.X * pixelSize.Width, 2),
                    (float)Math.Round(_boundingRectangle.Y * pixelSize.Height, 2),
                    (float)Math.Round(_boundingRectangle.Width * pixelSize.Width, 2),
                    (float)Math.Round(_boundingRectangle.Height * pixelSize.Height, 2));
            }
        }

        public void Init(Layer[] layers)
        {
            var oldLayerCount = LayerCount;
            _layers = layers;
            if (LayerCount != oldLayerCount)
            {
                SlicerFile.LayerCount = LayerCount;
            }
        }

        public void Init(uint layerCount, bool initializeLayers = false)
        {
            var layers = new Layer[layerCount];
            if (initializeLayers)
            {
                for (uint layerIndex = 0; layerIndex < layerCount; layerIndex++)
                {
                    layers[layerIndex] = new Layer(layerIndex, this);
                }
            }

            Init(layers);
        }

        public void Add(Layer layer)
        {
            Layers = Enumerable.Append(_layers, layer).ToArray();
        }

        public void Add(IEnumerable<Layer> layers)
        {
            var list = _layers.ToList();
            list.AddRange(layers);
            Layers = list.ToArray();
        }

        public void Clear()
        {
            //Layers = Array.Empty<Layer>();
            _layers = null;
        }

        public bool Contains(Layer layer)
        {
            return _layers.Contains(layer);
        }

        public void CopyTo(Layer[] array, int arrayIndex)
        {
            _layers.CopyTo(array, arrayIndex);
        }

        public bool Remove(Layer layer)
        {
            var list = _layers.ToList();
            var result = list.Remove(layer);
            if (result)
            {
                Layers = list.ToArray();
            }

            return result;
        }

        public int IndexOf(Layer layer)
        {
            for (int layerIndex = 0; layerIndex < Count; layerIndex++)
            {
                if (_layers[layerIndex].Equals(layer)) return layerIndex;
            }

            return -1;
        }

        public void Prepend(Layer layer) => Insert(0, layer);
        public void Prepend(IEnumerable<Layer> layers) => InsertRange(0, layers);
        public void Append(Layer layer) => Add(layer);
        public void AppendRange(IEnumerable<Layer> layers) => Add(layers);

        public void Insert(int index, Layer layer)
        {
            if (index < 0) return;
            if (index > Count) 
            {
                Add(layer); // Append
                return;
            }

            var list = _layers.ToList();
            list.Insert(index, layer);
            Layers = list.ToArray();
        }

        public void InsertRange(int index, IEnumerable<Layer> layers)
        {
            if (index < 0) return;
            
            if (index > Count)
            {
                Add(layers);
                return;
            }

            var list = _layers.ToList();
            list.InsertRange(index, layers);
            Layers = list.ToArray();
        }

        public void RemoveAt(int index)
        {
            if (index >= LastLayerIndex) return;
            var list = _layers.ToList();
            list.RemoveAt(index);
            Layers = list.ToArray();
        }

        public void RemoveRange(int index, int count)
        {
            if (count <= 0 || index >= LastLayerIndex) return;
            var list = _layers.ToList();
            list.RemoveRange(index, count);
            Layers = list.ToArray();
        }

        /// <summary>
        /// Removes all null layers in the collection
        /// </summary>
        public void RemoveNulls()
        {
            Layers = _layers.Where(layer => layer is not null).ToArray();
        }

        public int Count => _layers?.Length ?? 0;

        public bool IsReadOnly => false;

        /// <summary>
        /// Gets the layers count
        /// </summary>
        public uint LayerCount => (uint)(_layers?.Length ?? 0);

        public byte LayerDigits => (byte)LayerCount.ToString().Length;

        /// <summary>
        /// Gets if any layer got modified, otherwise false
        /// </summary>
        public bool IsModified
        {
            get
            {
                for (uint i = 0; i < LayerCount; i++)
                {
                    if (_layers[i].IsModified) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// True if all layers are using same value parameters as global settings, otherwise false
        /// </summary>
        public bool AllLayersAreUsingGlobalParameters => _layers.Where(layer => layer is not null).All(layer => layer.IsUsingGlobalParameters);

        /// <summary>
        /// True if any layer is using TSMC, otherwise false when none of layers is using TSMC
        /// </summary>
        public bool AnyLayerIsUsingTSMC => _layers.Where(layer => layer is not null).Any(layer => layer.IsUsingTSMC);

        //public float LayerHeight => Layers[0].PositionZ;

        #endregion

        #region Constructors
        public LayerManager(FileFormat slicerFile)
        {
            SlicerFile = slicerFile;
        }

        public LayerManager(uint layerCount, FileFormat slicerFile) : this(slicerFile)
        {
            SlicerFile = slicerFile;
            Init(layerCount);
        }
        #endregion

        #region Indexers
        public Layer this[uint index]
        {
            get => _layers[index];
            set => SetLayer(index, value);
        }

        public Layer this[int index]
        {
            get => _layers[index];
            set => SetLayer((uint) index, value);
        }

        public Layer this[long index]
        {
            get => _layers[index];
            set => SetLayer((uint) index, value);
        }

        public Layer[] this[System.Range range] => _layers[range];

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
        /// Sanitize layers and thrown exception if a severe problem is found
        /// </summary>
        /// <returns>True if one or more corrections has been applied, otherwise false</returns>
        public bool Sanitize()
        {
            bool appliedCorrections = false;

            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                // Check for null layers
                if (this[layerIndex] is null) throw new InvalidDataException($"Layer {layerIndex} was defined but doesn't contain a valid image.");
                if (layerIndex <= 0) continue;
                // Check for bigger position z than it successor
                if (this[layerIndex - 1].PositionZ > this[layerIndex].PositionZ && this[layerIndex - 1].NonZeroPixelCount > 1)
                    throw new InvalidDataException($"Layer {layerIndex - 1} ({this[layerIndex - 1].PositionZ}mm) have a higher Z position than the successor layer {layerIndex} ({this[layerIndex].PositionZ}mm).\n");
            }

            if (SlicerFile.ResolutionX == 0 || SlicerFile.ResolutionY == 0)
            {
                var layer = FirstLayer;
                if (layer is not null)
                {
                    using var mat = layer.LayerMat;

                    if (mat.Size.HaveZero())
                    {
                        throw new FileLoadException($"File resolution ({SlicerFile.Resolution}) is invalid and can't be auto fixed due invalid layers with same problem ({mat.Size}).", SlicerFile.FileFullPath);
                    }

                    SlicerFile.Resolution = mat.Size;
                    appliedCorrections = true;
                }
            }

            // Fix 0mm positions at layer 0
            if (this[0].PositionZ == 0)
            {
                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    this[layerIndex].PositionZ = Layer.RoundHeight(this[layerIndex].PositionZ + SlicerFile.LayerHeight);
                }

                appliedCorrections = true;
            }

            // Fix LightPWM of 0
            if (SlicerFile.LightPWM == 0)
            {
                SlicerFile.LightPWM = FileFormat.DefaultLightPWM;
                appliedCorrections = true;
            }
            if (SlicerFile.BottomLightPWM == 0)
            {
                SlicerFile.BottomLightPWM = FileFormat.DefaultBottomLightPWM;
                appliedCorrections = true;
            }

            return appliedCorrections;
        }

        /// <summary>
        /// Rebuild layer properties based on slice settings
        /// </summary>
        public void RebuildLayersProperties(bool recalculateZPos = true, string property = null)
        {
            //var layerHeight = SlicerFile.LayerHeight;
            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                var layer = this[layerIndex];
                layer.Index = layerIndex;
                layer.ParentLayerManager = this;

                if (recalculateZPos)
                {
                    layer.PositionZ = SlicerFile.GetHeightFromLayer(layerIndex);
                }

                if (property != string.Empty)
                {
                    if (property is null or nameof(SlicerFile.BottomLayerCount))
                    {
                        layer.LightOffDelay = SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomLightOffDelay, SlicerFile.LightOffDelay);
                        layer.WaitTimeBeforeCure = SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomWaitTimeBeforeCure, SlicerFile.WaitTimeBeforeCure);
                        layer.ExposureTime = SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomExposureTime, SlicerFile.ExposureTime);
                        layer.WaitTimeAfterCure = SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomWaitTimeAfterCure, SlicerFile.WaitTimeAfterCure);
                        layer.LiftHeight = SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomLiftHeight, SlicerFile.LiftHeight);
                        layer.LiftSpeed = SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomLiftSpeed, SlicerFile.LiftSpeed);
                        layer.LiftHeight2 = SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomLiftHeight2, SlicerFile.LiftHeight2);
                        layer.LiftSpeed2 = SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomLiftSpeed2, SlicerFile.LiftSpeed2);
                        layer.WaitTimeAfterLift = SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomWaitTimeAfterLift, SlicerFile.WaitTimeAfterLift);
                        layer.RetractSpeed = SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomRetractSpeed, SlicerFile.RetractSpeed);
                        layer.RetractHeight2 = SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomRetractHeight2, SlicerFile.RetractHeight2);
                        layer.RetractSpeed2 = SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomRetractSpeed2, SlicerFile.RetractSpeed2);
                        layer.LightPWM = SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomLightPWM, SlicerFile.LightPWM);
                    }
                    else
                    {
                        if (layer.IsBottomLayer)
                        {
                            if (property == nameof(SlicerFile.BottomLightOffDelay)) layer.LightOffDelay = SlicerFile.BottomLightOffDelay;
                            else if (property == nameof(SlicerFile.BottomWaitTimeBeforeCure)) layer.WaitTimeBeforeCure = SlicerFile.BottomWaitTimeBeforeCure;
                            else if (property == nameof(SlicerFile.BottomExposureTime)) layer.ExposureTime = SlicerFile.BottomExposureTime;
                            else if (property == nameof(SlicerFile.BottomWaitTimeAfterCure)) layer.WaitTimeAfterCure = SlicerFile.BottomWaitTimeAfterCure;
                            else if (property == nameof(SlicerFile.BottomLiftHeight)) layer.LiftHeight = SlicerFile.BottomLiftHeight;
                            else if (property == nameof(SlicerFile.BottomLiftSpeed)) layer.LiftSpeed = SlicerFile.BottomLiftSpeed;
                            else if (property == nameof(SlicerFile.BottomLiftHeight2)) layer.LiftHeight2 = SlicerFile.BottomLiftHeight2;
                            else if (property == nameof(SlicerFile.BottomLiftSpeed2)) layer.LiftSpeed2 = SlicerFile.BottomLiftSpeed2;
                            else if (property == nameof(SlicerFile.BottomWaitTimeAfterLift)) layer.WaitTimeAfterLift = SlicerFile.BottomWaitTimeAfterLift;
                            else if (property == nameof(SlicerFile.BottomRetractSpeed)) layer.RetractSpeed = SlicerFile.BottomRetractSpeed;
                            else if (property == nameof(SlicerFile.BottomRetractHeight2)) layer.RetractHeight2 = SlicerFile.BottomRetractHeight2;
                            else if (property == nameof(SlicerFile.BottomRetractSpeed2)) layer.RetractSpeed2 = SlicerFile.BottomRetractSpeed2;
                            else if (property == nameof(SlicerFile.BottomLightPWM)) layer.LightPWM = SlicerFile.BottomLightPWM;

                            // Propagate value to layer when bottom property does not exists
                            else if (property == nameof(SlicerFile.LightOffDelay) && !SlicerFile.CanUseBottomLightOffDelay) layer.LightOffDelay = SlicerFile.LightOffDelay;
                            else if (property == nameof(SlicerFile.WaitTimeBeforeCure) && !SlicerFile.CanUseBottomWaitTimeBeforeCure) layer.WaitTimeBeforeCure = SlicerFile.WaitTimeBeforeCure;
                            else if (property == nameof(SlicerFile.ExposureTime) && !SlicerFile.CanUseBottomExposureTime) layer.ExposureTime = SlicerFile.ExposureTime;
                            else if (property == nameof(SlicerFile.WaitTimeAfterCure) && !SlicerFile.CanUseBottomWaitTimeAfterCure) layer.WaitTimeAfterCure = SlicerFile.WaitTimeAfterCure;
                            else if (property == nameof(SlicerFile.LiftHeight) && !SlicerFile.CanUseBottomLiftHeight) layer.LiftHeight = SlicerFile.LiftHeight;
                            else if (property == nameof(SlicerFile.LiftSpeed) && !SlicerFile.CanUseBottomLiftSpeed) layer.LiftSpeed = SlicerFile.LiftSpeed;
                            else if (property == nameof(SlicerFile.LiftHeight2) && !SlicerFile.CanUseBottomLiftHeight2) layer.LiftHeight2 = SlicerFile.LiftHeight2;
                            else if (property == nameof(SlicerFile.LiftSpeed2) && !SlicerFile.CanUseBottomLiftSpeed2) layer.LiftSpeed2 = SlicerFile.LiftSpeed2;
                            else if (property == nameof(SlicerFile.WaitTimeAfterLift) && !SlicerFile.CanUseBottomWaitTimeAfterLift) layer.WaitTimeAfterLift = SlicerFile.WaitTimeAfterLift;
                            else if (property == nameof(SlicerFile.RetractSpeed) && !SlicerFile.CanUseBottomRetractSpeed) layer.RetractSpeed = SlicerFile.RetractSpeed;
                            else if (property == nameof(SlicerFile.RetractHeight2) && !SlicerFile.CanUseBottomRetractHeight2) layer.RetractHeight2 = SlicerFile.RetractHeight2;
                            else if (property == nameof(SlicerFile.RetractSpeed2) && !SlicerFile.CanUseRetractSpeed2) layer.RetractSpeed2 = SlicerFile.RetractSpeed2;
                            else if (property == nameof(SlicerFile.LightPWM) && !SlicerFile.CanUseBottomLightPWM) layer.LightPWM = SlicerFile.LightPWM;
                        }
                        else // Normal layers
                        {
                            if (property == nameof(SlicerFile.LightOffDelay)) layer.LightOffDelay = SlicerFile.LightOffDelay;
                            else if (property == nameof(SlicerFile.WaitTimeBeforeCure)) layer.WaitTimeBeforeCure = SlicerFile.WaitTimeBeforeCure;
                            else if (property == nameof(SlicerFile.ExposureTime)) layer.ExposureTime = SlicerFile.ExposureTime;
                            else if (property == nameof(SlicerFile.WaitTimeAfterCure)) layer.WaitTimeAfterCure = SlicerFile.WaitTimeAfterCure;
                            else if (property == nameof(SlicerFile.LiftHeight)) layer.LiftHeight = SlicerFile.LiftHeight;
                            else if (property == nameof(SlicerFile.LiftSpeed)) layer.LiftSpeed = SlicerFile.LiftSpeed;
                            else if (property == nameof(SlicerFile.LiftHeight2)) layer.LiftHeight2 = SlicerFile.LiftHeight2;
                            else if (property == nameof(SlicerFile.LiftSpeed2)) layer.LiftSpeed2 = SlicerFile.LiftSpeed2;
                            else if (property == nameof(SlicerFile.WaitTimeAfterLift)) layer.WaitTimeAfterLift = SlicerFile.WaitTimeAfterLift;
                            else if (property == nameof(SlicerFile.RetractSpeed)) layer.RetractSpeed = SlicerFile.RetractSpeed;
                            else if (property == nameof(SlicerFile.RetractHeight2)) layer.RetractHeight2 = SlicerFile.RetractHeight2;
                            else if (property == nameof(SlicerFile.RetractSpeed2)) layer.RetractSpeed2 = SlicerFile.RetractSpeed2;
                            else if (property == nameof(SlicerFile.LightPWM)) layer.LightPWM = SlicerFile.LightPWM;
                        }
                    }
                }

                layer.MaterialMilliliters = -1; // Recalculate this value to be sure
            }

            SlicerFile?.RebuildGCode();
        }

        /// <summary>
        /// Set LiftHeight to 0 if previous and current have same PositionZ
        /// <param name="zeroLightOffDelay">If true also set light off to 0, otherwise current value will be kept.</param>
        /// </summary>
        public void SetNoLiftForSamePositionedLayers(bool zeroLightOffDelay = false)
            => SetLiftForSamePositionedLayers(0, zeroLightOffDelay);

        public void SetLiftForSamePositionedLayers(float liftHeight = 0, bool zeroLightOffDelay = false)
        {
            for (int layerIndex = 1; layerIndex < LayerCount; layerIndex++)
            {
                var layer = this[layerIndex];
                if (this[layerIndex - 1].PositionZ != layer.PositionZ) continue;
                layer.LiftHeightTotal = liftHeight;
                layer.WaitTimeAfterLift = 0;
                if (zeroLightOffDelay)
                {
                    layer.LightOffDelay = 0;
                    layer.WaitTimeBeforeCure = 0;
                    layer.WaitTimeAfterCure = 0;
                }
            }
            SlicerFile?.RebuildGCode();
        }

        public IEnumerable<Layer> GetSamePositionedLayers()
        {
            for (int layerIndex = 1; layerIndex < LayerCount; layerIndex++)
            {
                var layer = this[layerIndex];
                if (this[layerIndex - 1].PositionZ != layer.PositionZ) continue;
                yield return layer;
            }
        }

        public IEnumerable<Layer> GetDistinctLayersByPositionZ(uint layerIndexStart = 0) =>
            GetDistinctLayersByPositionZ(layerIndexStart, LastLayerIndex);

        public IEnumerable<Layer> GetDistinctLayersByPositionZ(uint layerIndexStart, uint layerIndexEnd)
        {
            return layerIndexEnd - layerIndexStart >= LastLayerIndex 
                ? SlicerFile.DistinctBy(layer => layer.PositionZ) 
                : SlicerFile.Where((_, layerIndex) => layerIndex >= layerIndexStart && layerIndex <= layerIndexEnd).DistinctBy(layer => layer.PositionZ);
        }

        public Mat GetMergedMatForSequentialPositionedLayers(uint layerIndex, MatCacheManager cacheManager, out uint lastLayerIndex)
        {
            var startLayerPositionZ = this[layerIndex].PositionZ;
            lastLayerIndex = layerIndex;
            var layerMat = cacheManager.Get1(layerIndex).Clone();

            for (var curIndex = layerIndex + 1; curIndex < LayerCount && this[curIndex].PositionZ == startLayerPositionZ; curIndex++)
            {
                CvInvoke.Max(layerMat, cacheManager.Get1(curIndex), layerMat);
                lastLayerIndex = curIndex;
            }

            return layerMat;
        }

        public Mat GetMergedMatForSequentialPositionedLayers(uint layerIndex, MatCacheManager cacheManager) 
            => GetMergedMatForSequentialPositionedLayers(layerIndex, cacheManager, out _);

        public Mat GetMergedMatForSequentialPositionedLayers(uint layerIndex, out uint lastLayerIndex)
        {
            var startLayer = this[layerIndex];
            lastLayerIndex = layerIndex;
            var layerMat = startLayer.LayerMat;

            for (var curIndex = layerIndex + 1; curIndex < LayerCount && this[curIndex].PositionZ == startLayer.PositionZ; curIndex++)
            {
                using var nextLayer = this[curIndex].LayerMat;
                CvInvoke.Max(nextLayer, layerMat, layerMat);
                lastLayerIndex = curIndex;
            }

            return layerMat;
        }

        public Mat GetMergedMatForSequentialPositionedLayers(uint layerIndex) 
            => GetMergedMatForSequentialPositionedLayers(layerIndex, out _);

        public Rectangle GetBoundingRectangle(OperationProgress progress = null)
        {
            var firstLayer = FirstLayer;
            if (!_boundingRectangle.IsEmpty || LayerCount == 0 || firstLayer is null || !firstLayer.HaveImage) return _boundingRectangle;
            progress ??= new OperationProgress(OperationProgress.StatusOptimizingBounds, LayerCount - 1);
            _boundingRectangle = Rectangle.Empty;
            uint firstValidLayerBounds = 0;

            void FindFirstBoundingRectangle()
            {
                for (uint layerIndex = 0; layerIndex < Count; layerIndex++)
                {
                    firstValidLayerBounds = layerIndex;
                    if (this[layerIndex] is null || this[layerIndex].BoundingRectangle == Rectangle.Empty) continue;
                    _boundingRectangle = this[layerIndex].BoundingRectangle;
                    break;
                }
            }

            FindFirstBoundingRectangle();
            //_boundingRectangle = firstLayer.BoundingRectangle;

            if (_boundingRectangle.IsEmpty) // Safe checking, all layers haven't a bounding rectangle
            {
                progress.Reset(OperationProgress.StatusOptimizingBounds, LayerCount-1);
                Parallel.For(0, LayerCount, CoreSettings.ParallelOptions, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    
                    this[layerIndex].GetBoundingRectangle();

                    progress.LockAndIncrement();
                });

                if (progress.Token.IsCancellationRequested)
                {
                    _boundingRectangle = Rectangle.Empty;
                    progress.Token.ThrowIfCancellationRequested();
                }

                FindFirstBoundingRectangle();
            }

            if (firstValidLayerBounds+1 < LayerCount)
            {
                progress.Reset(OperationProgress.StatusCalculatingBounds, LayerCount - firstValidLayerBounds - 1);
                for (var i = firstValidLayerBounds+1; i < LayerCount; i++)
                {
                    if (this[i] is null || this[i].BoundingRectangle.IsEmpty) continue;
                    _boundingRectangle = Rectangle.Union(_boundingRectangle, this[i].BoundingRectangle);
                    progress++;
                }
            }

            RaisePropertyChanged(nameof(BoundingRectangle));
            return _boundingRectangle;
        }

        /// <summary>
        /// Sets a layer
        /// </summary>
        /// <param name="index">Layer index</param>
        /// <param name="layer">Layer to add</param>
        /// <param name="makeClone">True to add a clone of the layer</param>
        public void SetLayer(uint index, Layer layer, bool makeClone = false)
        {
            if (_layers[index] is not null && layer is not null) layer.IsModified = true;
            _layers[index] = makeClone && layer is not null ? layer.Clone() : layer;
            if (layer is null) return;
            layer.Index = index;
            layer.ParentLayerManager = this;
        }

        /// <summary>
        /// Add a list of layers
        /// </summary>
        /// <param name="layers">Layers to add</param>
        /// <param name="makeClone">True to add a clone of layers</param>
        public void SetLayers(IEnumerable<Layer> layers, bool makeClone = false)
        {
            foreach (var layer in layers)
            {
                SetLayer(layer.Index, layer, makeClone);
            }
        }

        /// <summary>
        /// Get layer given index
        /// </summary>
        /// <param name="index">Layer index</param>
        /// <returns></returns>
        public Layer GetLayer(uint index)
        {
            return _layers[index];
        }

        public Layer GetSmallestLayerBetween(uint layerStartIndex, uint layerEndIndex)
        {
            return _layers?.Where((layer, index) => !layer.IsEmpty && index >= layerStartIndex && index <= layerEndIndex).MinBy(layer => layer.NonZeroPixelCount).FirstOrDefault();
        }

        public Layer GetLargestLayerBetween(uint layerStartIndex, uint layerEndIndex)
        {
            return _layers?.Where((layer, index) => !layer.IsEmpty && index >= layerStartIndex && index <= layerEndIndex).MaxBy(layer => layer.NonZeroPixelCount).FirstOrDefault();
        }

        public IEnumerable<Layer> GetLayersFromHeightRange(float startPositionZ, float endPositionZ)
        {
            return this.Where(layer => layer.PositionZ >= startPositionZ && layer.PositionZ <= endPositionZ);
        }

        public IEnumerable<Layer> GetLayersFromHeightRange(float endPositionZ)
        {
            return this.Where(layer => layer.PositionZ <= endPositionZ);
        }

        public static void MutateGetVarsIterationChamfer(uint startLayerIndex, uint endLayerIndex, int iterationsStart, int iterationsEnd, ref bool isFade, out float iterationSteps, out int maxIteration)
        {
            iterationSteps = 0;
            maxIteration = 0;
            isFade = isFade && startLayerIndex != endLayerIndex && iterationsStart != iterationsEnd;
            if (!isFade) return;
            iterationSteps = Math.Abs((iterationsStart - (float)iterationsEnd) / ((float)endLayerIndex - startLayerIndex));
            maxIteration = Math.Max(iterationsStart, iterationsEnd);
        }

        public static int MutateGetIterationVar(bool isFade, int iterationsStart, int iterationsEnd, float iterationSteps, int maxIteration, uint startLayerIndex, uint layerIndex)
        {
            if (!isFade) return iterationsStart;
            // calculate iterations based on range
            int iterations = (int)(iterationsStart < iterationsEnd
                ? iterationsStart + (layerIndex - startLayerIndex) * iterationSteps
                : iterationsStart - (layerIndex - startLayerIndex) * iterationSteps);

            // constrain
            return Math.Min(Math.Max(0, iterations), maxIteration);
        }

        public static int MutateGetIterationChamfer(uint layerIndex, uint startLayerIndex, uint endLayerIndex, int iterationsStart,
            int iterationsEnd, bool isFade)
        {
            MutateGetVarsIterationChamfer(startLayerIndex, endLayerIndex, iterationsStart, iterationsEnd, ref isFade,
                out float iterationSteps, out int maxIteration);
            return MutateGetIterationVar(isFade, iterationsStart, iterationsEnd, iterationSteps, maxIteration, startLayerIndex, layerIndex);
        }

        public void DrawModifications(IList<PixelOperation> drawings, OperationProgress progress = null)
        {
            progress ??= new OperationProgress();
            progress.Reset("Drawings", (uint) drawings.Count);

            ConcurrentDictionary<uint, Mat> modifiedLayers = new();
            for (var i = 0; i < drawings.Count; i++)
            {
                var operation = drawings[i];
                
                if (operation.OperationType == PixelOperation.PixelOperationType.Drawing)
                {
                    var operationDrawing = (PixelDrawing) operation;
                    var mat = modifiedLayers.GetOrAdd(operation.LayerIndex, u => this[operation.LayerIndex].LayerMat);

                    if (operationDrawing.BrushSize == 1)
                    {
                        mat.SetByte(operation.Location.X, operation.Location.Y, operationDrawing.Brightness);
                        continue;
                    }

                    mat.DrawPolygon((byte)operationDrawing.BrushShape, operationDrawing.BrushSize / 2, operationDrawing.Location,
                        new MCvScalar(operationDrawing.Brightness), operationDrawing.RotationAngle, operationDrawing.Thickness, operationDrawing.LineType);
                    /*switch (operationDrawing.BrushShape)
                    {
                        case PixelDrawing.BrushShapeType.Square:
                            CvInvoke.Rectangle(mat, operationDrawing.Rectangle, new MCvScalar(operationDrawing.Brightness), operationDrawing.Thickness, operationDrawing.LineType);
                            break;
                        case PixelDrawing.BrushShapeType.Circle:
                            CvInvoke.Circle(mat, operation.Location, operationDrawing.BrushSize / 2,
                                new MCvScalar(operationDrawing.Brightness), operationDrawing.Thickness, operationDrawing.LineType);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }*/
                }
                else if (operation.OperationType == PixelOperation.PixelOperationType.Text)
                {
                    var operationText = (PixelText)operation;
                    var mat = modifiedLayers.GetOrAdd(operation.LayerIndex, u => this[operation.LayerIndex].LayerMat);

                    mat.PutTextRotated(operationText.Text, operationText.Location, operationText.Font, operationText.FontScale, new MCvScalar(operationText.Brightness), operationText.Thickness, operationText.LineType, operationText.Mirror, operationText.LineAlignment, operationText.Angle);
                }
                else if (operation.OperationType == PixelOperation.PixelOperationType.Eraser)
                {
                    var mat = modifiedLayers.GetOrAdd(operation.LayerIndex, u => this[operation.LayerIndex].LayerMat);

                    using var layerContours  = mat.FindContours(out var hierarchy, RetrType.Tree);
                    
                    if (mat.GetByte(operation.Location) >= 10)
                    {
                        using var vec = EmguContours.GetContoursInside(layerContours, hierarchy, operation.Location);

                        if (vec.Size > 0)
                        {
                            CvInvoke.DrawContours(mat, vec, -1, new MCvScalar(operation.PixelBrightness), -1);
                        }
                    }
                }
                else if (operation.OperationType == PixelOperation.PixelOperationType.Supports)
                {
                    var operationSupport = (PixelSupport)operation;
                    int drawnLayers = 0;
                    for (int operationLayer = (int)operation.LayerIndex-1; operationLayer >= 0; operationLayer--)
                    {
                        var mat = modifiedLayers.GetOrAdd((uint) operationLayer, u => this[operationLayer].LayerMat);
                        int radius = (operationLayer > 10 ? Math.Min(operationSupport.TipDiameter + drawnLayers, operationSupport.PillarDiameter) : operationSupport.BaseDiameter) / 2;
                        uint whitePixels;

                        int yStart = Math.Max(0, operation.Location.Y - operationSupport.TipDiameter / 2);
                        int xStart = Math.Max(0, operation.Location.X - operationSupport.TipDiameter / 2);

                        using (var matCircleRoi = new Mat(mat, new Rectangle(xStart, yStart, operationSupport.TipDiameter, operationSupport.TipDiameter)))
                        {
                            using var matCircleMask = matCircleRoi.NewBlank();
                            CvInvoke.Circle(matCircleMask, new Point(operationSupport.TipDiameter / 2, operationSupport.TipDiameter / 2),
                                operationSupport.TipDiameter / 2, new MCvScalar(operation.PixelBrightness), -1);
                            CvInvoke.BitwiseAnd(matCircleRoi, matCircleMask, matCircleMask);
                            whitePixels = (uint) CvInvoke.CountNonZero(matCircleMask);
                        }

                        if (whitePixels >= Math.Pow(operationSupport.TipDiameter, 2) / 3)
                        {
                            //CvInvoke.Circle(mat, operation.Location, radius, new MCvScalar(255), -1);
                            if (drawnLayers == 0) continue; // Supports nonexistent, keep digging
                            break; // White area end supporting
                        }

                        CvInvoke.Circle(mat, operation.Location, radius, new MCvScalar(operation.PixelBrightness), -1, operationSupport.LineType);
                        drawnLayers++;
                    }
                }
                else if (operation.OperationType == PixelOperation.PixelOperationType.DrainHole)
                {
                    uint drawnLayers = 0;
                    var operationDrainHole = (PixelDrainHole)operation;
                    for (int operationLayer = (int)operation.LayerIndex; operationLayer >= 0; operationLayer--)
                    {
                        var mat = modifiedLayers.GetOrAdd((uint)operationLayer, u => this[operationLayer].LayerMat);
                        int radius =  operationDrainHole.Diameter / 2;
                        uint blackPixels;

                        int yStart = Math.Max(0, operation.Location.Y - radius);
                        int xStart = Math.Max(0, operation.Location.X - radius);

                        using (var matCircleRoi = new Mat(mat, new Rectangle(xStart, yStart, operationDrainHole.Diameter, operationDrainHole.Diameter)))
                        {
                            using var matCircleRoiInv = new Mat();
                            CvInvoke.Threshold(matCircleRoi, matCircleRoiInv, 100, 255, ThresholdType.BinaryInv);
                            using var matCircleMask = matCircleRoi.NewBlank();
                            CvInvoke.Circle(matCircleMask, new Point(radius, radius), radius, EmguExtensions.WhiteColor, -1);
                            CvInvoke.BitwiseAnd(matCircleRoiInv, matCircleMask, matCircleMask);
                            blackPixels = (uint) CvInvoke.CountNonZero(matCircleMask);
                        }

                        if (blackPixels >= Math.Pow(operationDrainHole.Diameter, 2) / 3) // Enough area to drain?
                        {
                            if (drawnLayers == 0) continue; // Drill not found a target yet, keep digging
                            break; // Stop drill drain found!
                        }
                        
                        CvInvoke.Circle(mat, operation.Location, radius, EmguExtensions.BlackColor, -1, operationDrainHole.LineType);
                        drawnLayers++;
                    }
                }
                
                progress++;
            }

            progress.Reset("Saving", (uint) modifiedLayers.Count);
            Parallel.ForEach(modifiedLayers, CoreSettings.ParallelOptions, modifiedLayer =>
            {
                this[modifiedLayer.Key].LayerMat = modifiedLayer.Value;
                modifiedLayer.Value.Dispose();

                progress.LockAndIncrement();
            });

        }

        /// <summary>
        /// Set the IsModified property for all layers
        /// </summary>
        public void SetAllIsModified(bool isModified)
        {
            for (uint i = 0; i < LayerCount; i++)
            {
                if(Layers[i] is null) continue;
                Layers[i].IsModified = isModified;
            }
        }

        /// <summary>
        /// Reallocate with new size
        /// </summary>
        /// <returns></returns>
        public Layer[] ReallocateNew(uint newLayerCount, bool makeClone = false)
        {
            var layers = new Layer[newLayerCount];
            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                if (layerIndex >= newLayerCount) break;
                var layer = this[layerIndex];
                layers[layerIndex] = makeClone && layer is not null ? layer.Clone() : layer;
            }

            return layers;
        }

        /// <summary>
        /// Reallocate layer count with a new size
        /// </summary>
        /// <param name="newLayerCount">New layer count</param>
        /// <param name="initBlack"></param>
        public void Reallocate(uint newLayerCount, bool initBlack = false)
        {
            var oldLayerCount = LayerCount;
            int differenceLayerCount = (int)newLayerCount - Count;
            if (differenceLayerCount == 0) return;
            var newLayers = new Layer[newLayerCount];

            Array.Copy(_layers, 0, newLayers, 0, Math.Min(newLayerCount, newLayers.Length));
            
            if (differenceLayerCount > 0 && initBlack)
            {
                using var blackMat = EmguExtensions.InitMat(SlicerFile.Resolution);
                var pngBytes = blackMat.GetPngByes();
                for (var layerIndex = oldLayerCount; layerIndex < newLayerCount; layerIndex++)
                {
                    newLayers[layerIndex] = new Layer(layerIndex, pngBytes.ToArray(), this);
                }
            }

            SlicerFile.SuppressRebuildPropertiesWork(() =>
            {
                Layers = newLayers;
            });
        }

        /// <summary>
        /// Reallocate at given index
        /// </summary>
        /// <returns></returns>
        public void ReallocateInsert(uint insertAtLayerIndex, uint layerCount, bool initBlack = false)
        {
            if (layerCount == 0) return;
            insertAtLayerIndex = Math.Min(insertAtLayerIndex, LayerCount);
            var newLayers = new Layer[LayerCount + layerCount];

            // Copy from start to insert index
            if(insertAtLayerIndex > 0) 
                Array.Copy(_layers, 0, newLayers, 0, insertAtLayerIndex);

            // Rearrange from last insert to end
            if(insertAtLayerIndex < LayerCount)
                Array.Copy(
                    _layers, insertAtLayerIndex, 
                    newLayers, insertAtLayerIndex + layerCount,
                    LayerCount - insertAtLayerIndex);
            /*for (uint layerIndex = insertAtLayerIndex; layerIndex < _layers.Length; layerIndex++)
            {
                newLayers[layerCount + layerIndex] = _layers[layerIndex];
                newLayers[layerCount + layerIndex].Index = layerCount + layerIndex;
            }*/

            // Allocate new layers in between
            if (initBlack)
            {
                using var blackMat = EmguExtensions.InitMat(SlicerFile.Resolution);
                var pngBytes = blackMat.GetPngByes();
                for (var layerIndex = insertAtLayerIndex; layerIndex < insertAtLayerIndex + layerCount; layerIndex++)
                {
                    newLayers[layerIndex] = new Layer(layerIndex, pngBytes.ToArray(), this);
                }
            }

            SlicerFile.SuppressRebuildPropertiesWork(() =>
            {
                Layers = newLayers;
            });
        }

        /// <summary>
        /// Reallocate at a kept range
        /// </summary>
        /// <param name="startLayerIndex"></param>
        /// <param name="endLayerIndex"></param>
        public void ReallocateKeepRange(uint startLayerIndex, uint endLayerIndex)
        {
            if ((int)(endLayerIndex - startLayerIndex) < 0) return;
            var newLayers = new Layer[1 + endLayerIndex - startLayerIndex];

            Array.Copy(_layers, startLayerIndex, newLayers, 0, newLayers.Length);
            /*uint currentLayerIndex = 0;
            for (uint layerIndex = startLayerIndex; layerIndex <= endLayerIndex; layerIndex++)
            {
                newLayers[currentLayerIndex++] = _layers[layerIndex];
            }*/

            SlicerFile.SuppressRebuildPropertiesWork(() =>
            {
                Layers = newLayers;
            });
        }

        /// <summary>
        /// Reallocate at start
        /// </summary>
        /// <returns></returns>
        public void ReallocateStart(uint layerCount, bool initBlack = false) => ReallocateInsert(0, layerCount, initBlack);

        /// <summary>
        /// Reallocate at end
        /// </summary>
        /// <returns></returns>
        public void ReallocateEnd(uint layerCount, bool initBlack = false) => ReallocateInsert(LayerCount, layerCount, initBlack);

        /// <summary>
        /// Allocate layers from a Mat array
        /// </summary>
        /// <param name="mats"></param>
        /// <returns>The new Layer array</returns>
        public Layer[] AllocateFromMat(Mat[] mats)
        {
            var layers = new Layer[mats.Length];
            Parallel.For(0, mats.Length, CoreSettings.ParallelOptions, i =>
            {
                layers[i] = new Layer((uint)i, mats[i], this);
            });

            return layers;
        }

        /// <summary>
        /// Allocate layers from a Mat array and set them to the current file
        /// </summary>
        /// <param name="mats"></param>
        /// /// <returns>The new Layer array</returns>
        public Layer[] AllocateAndSetFromMat(Mat[] mats)
        {
            var layers = AllocateFromMat(mats);
            Layers = layers;
            return layers;
        }

        /// <summary>
        /// Clone this object
        /// </summary>
        /// <returns></returns>
        public LayerManager Clone()
        {
            LayerManager layerManager = new(SlicerFile);
            layerManager.Init(CloneLayers());
            /*foreach (var layer in this)
            {
                layerManager[layer.Index] = layer.Clone();
            }*/
            layerManager.BoundingRectangle = BoundingRectangle;
            
            return layerManager;
        }

        /// <summary>
        /// Clone layers
        /// </summary>
        /// <returns></returns>
        public Layer[] CloneLayers()
        {
            return Layer.CloneLayers(_layers);
        }

        public void Dispose() { }

        #endregion

        #region Formater

        public override string ToString()
        {
            return $"{nameof(BoundingRectangle)}: {BoundingRectangle}, {nameof(LayerCount)}: {LayerCount}, {nameof(IsModified)}: {IsModified}";
        }

        #endregion
    }
}
