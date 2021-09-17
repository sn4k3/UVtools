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
using MoreLinq.Extensions;
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
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

        public void Init(uint layerCount)
        {
            _layers = new Layer[layerCount];
        }

        public void Init(Layer[] layers)
        {
            _layers = layers;
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
            _layers = new Layer[layerCount];
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
                if (this[layerIndex - 1].PositionZ > this[layerIndex].PositionZ) throw new InvalidDataException($"Layer {layerIndex - 1} ({this[layerIndex - 1].PositionZ}mm) have a higher Z position than the successor layer {layerIndex} ({this[layerIndex].PositionZ}mm).\n");
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
            var layers = new List<Layer>();
            for (int layerIndex = 1; layerIndex < LayerCount; layerIndex++)
            {
                var layer = this[layerIndex];
                if (this[layerIndex - 1].PositionZ != layer.PositionZ) continue;
                layers.Add(layer);
            }

            return layers;
        }

        public Rectangle GetBoundingRectangle(OperationProgress progress = null)
        {
            if (!_boundingRectangle.IsEmpty || LayerCount == 0 || this[0] is null) return _boundingRectangle;
            progress ??= new OperationProgress(OperationProgress.StatusOptimizingBounds, LayerCount - 1);
            _boundingRectangle = this[0].BoundingRectangle;
            if (_boundingRectangle.IsEmpty) // Safe checking
            {
                progress.Reset(OperationProgress.StatusOptimizingBounds, LayerCount-1);
                Parallel.For(0, LayerCount, CoreSettings.ParallelOptions, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    
                    this[layerIndex].GetBoundingRectangle();

                    if (progress is null) return;
                    progress.LockAndIncrement();
                });
                _boundingRectangle = this[0].BoundingRectangle;

                if (progress is not null && progress.Token.IsCancellationRequested)
                {
                    _boundingRectangle = Rectangle.Empty;
                    progress.Token.ThrowIfCancellationRequested();
                }
            }

            progress.Reset(OperationProgress.StatusCalculatingBounds, LayerCount-1);
            for (int i = 1; i < LayerCount; i++)
            {
                if(this[i] is null || this[i].BoundingRectangle.IsEmpty) continue;
                _boundingRectangle = Rectangle.Union(_boundingRectangle, this[i].BoundingRectangle);
                progress++;
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

        public List<LayerIssue> GetAllIssues(
            IslandDetectionConfiguration islandConfig = null,
            OverhangDetectionConfiguration overhangConfig = null, 
            ResinTrapDetectionConfiguration resinTrapConfig = null,
            TouchingBoundDetectionConfiguration touchBoundConfig = null,
            PrintHeightDetectionConfiguration printHeightConfig = null,
            bool emptyLayersConfig = true,
            List<LayerIssue> ignoredIssues = null,
            OperationProgress progress = null)
        {
            
            islandConfig ??= new IslandDetectionConfiguration();
            overhangConfig ??= new OverhangDetectionConfiguration();
            resinTrapConfig ??= new ResinTrapDetectionConfiguration();
            touchBoundConfig ??= new TouchingBoundDetectionConfiguration();
            printHeightConfig ??= new PrintHeightDetectionConfiguration();
            progress ??= new OperationProgress();
            
            var result = new ConcurrentBag<LayerIssue>();
            //var layerHollowAreas = new ConcurrentDictionary<uint, List<LayerHollowArea>>();
            var resinTraps = new List<VectorOfVectorOfPoint>[LayerCount];
            var suctionTraps = new List<VectorOfVectorOfPoint>[LayerCount];
            var externalContours = new VectorOfVectorOfPoint[LayerCount];
            var hollows = new List<VectorOfVectorOfPoint>[LayerCount];
            var airContours = new List<VectorOfVectorOfPoint>[LayerCount];
            var resinTrapsContoursArea = new double[LayerCount][];
            
            bool IsIgnored(LayerIssue issue) => ignoredIssues is not null && ignoredIssues.Count > 0 && ignoredIssues.Contains(issue);

            bool AddIssue(LayerIssue issue)
            {
                if (IsIgnored(issue)) return false;
                result.Add(issue);
                return true;
            }

            void GenerateAirMap(IInputArray input, IInputOutputArray output, VectorOfVectorOfPoint externals)
            {
                CvInvoke.BitwiseNot(input, output); 
                if (externals is null || externals.Size == 0) return;
                CvInvoke.DrawContours(output, externals, -1, EmguExtensions.BlackColor, -1);
            }

            if (printHeightConfig.Enabled && SlicerFile.MachineZ > 0)
            {
                float printHeightWithOffset = Layer.RoundHeight(SlicerFile.MachineZ + printHeightConfig.Offset);
                if (SlicerFile.PrintHeight > printHeightWithOffset)
                {
                    foreach (var layer in this)
                    {
                        if (layer.PositionZ > printHeightWithOffset)
                        {
                            AddIssue(new LayerIssue(layer, LayerIssue.IssueType.PrintHeight));
                        }
                    }
                }
            }

            if (emptyLayersConfig)
            {
                foreach (var layer in this)
                {
                    if (layer.IsEmpty)
                    {
                        AddIssue(new LayerIssue(layer, LayerIssue.IssueType.EmptyLayer));
                    }
                }
            }

            if (islandConfig.Enabled || overhangConfig.Enabled || resinTrapConfig.Enabled || touchBoundConfig.Enabled)
            {
                progress.Reset(OperationProgress.StatusIslands, LayerCount);

                // Detect contours
                Parallel.For(0, LayerCount, CoreSettings.ParallelOptions, layerIndexInt =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    uint layerIndex = (uint)layerIndexInt;
                    var layer = this[layerIndex];
                    
                    if (layer.IsEmpty)
                    {
                        progress.LockAndIncrement();
                        return;
                    }

                    // Spare a decoding cycle
                    if (!touchBoundConfig.Enabled &&
                        !resinTrapConfig.Enabled &&
                        (!overhangConfig.Enabled || overhangConfig.Enabled && (layerIndex == 0 || overhangConfig.WhiteListLayers is not null && !overhangConfig.WhiteListLayers.Contains(layerIndex))) &&
                        (!islandConfig.Enabled || islandConfig.Enabled && (layerIndex == 0 || islandConfig.WhiteListLayers is not null && !islandConfig.WhiteListLayers.Contains(layerIndex)))
                        )
                    {
                        progress.LockAndIncrement();
                        
                        return;
                    }

                    using (var image = layer.LayerMat)
                    {
                        int step = image.Step;
                        var span = image.GetDataSpan<byte>();

                        if (touchBoundConfig.Enabled)
                        {
                            // TouchingBounds Checker
                            List<Point> pixels = new ();
                            bool touchTop = layer.BoundingRectangle.Top <= touchBoundConfig.MarginTop;
                            bool touchBottom = layer.BoundingRectangle.Bottom >=
                                               image.Height - touchBoundConfig.MarginBottom;
                            bool touchLeft = layer.BoundingRectangle.Left <= touchBoundConfig.MarginLeft;
                            bool touchRight = layer.BoundingRectangle.Right >=
                                              image.Width - touchBoundConfig.MarginRight;
                            if (touchTop || touchBottom)
                            {
                                for (int x = 0; x < image.Width; x++) // Check Top and Bottom bounds
                                {
                                    if (touchTop)
                                    {
                                        for (int y = 0; y < touchBoundConfig.MarginTop; y++) // Top
                                        {
                                            if (span[image.GetPixelPos(x, y)] >=
                                                touchBoundConfig.MinimumPixelBrightness)
                                            {
                                                pixels.Add(new Point(x, y));
                                            }
                                        }
                                    }

                                    if (touchBottom)
                                    {
                                        for (int y = image.Height - touchBoundConfig.MarginBottom;
                                            y < image.Height;
                                            y++) // Bottom
                                        {
                                            if (span[image.GetPixelPos(x, y)] >=
                                                touchBoundConfig.MinimumPixelBrightness)
                                            {
                                                pixels.Add(new Point(x, y));
                                            }
                                        }
                                    }

                                }
                            }

                            if (touchLeft || touchRight)
                            {
                                for (int y = touchBoundConfig.MarginTop;
                                    y < image.Height - touchBoundConfig.MarginBottom;
                                    y++) // Check Left and Right bounds
                                {
                                    if (touchLeft)
                                    {
                                        for (int x = 0; x < touchBoundConfig.MarginLeft; x++) // Left
                                        {
                                            if (span[image.GetPixelPos(x, y)] >=
                                                touchBoundConfig.MinimumPixelBrightness)
                                            {
                                                pixels.Add(new Point(x, y));
                                            }
                                        }
                                    }

                                    if (touchRight)
                                    {
                                        for (int x = image.Width - touchBoundConfig.MarginRight;
                                            x < image.Width;
                                            x++) // Right
                                        {
                                            if (span[image.GetPixelPos(x, y)] >=
                                                touchBoundConfig.MinimumPixelBrightness)
                                            {
                                                pixels.Add(new Point(x, y));
                                            }
                                        }
                                    }
                                }
                            }

                            if (pixels.Count > 0)
                            {
                                AddIssue(new LayerIssue(layer, LayerIssue.IssueType.TouchingBound, pixels.ToArray(), null));
                            }
                        }

                        if (layerIndex > 0) // No islands nor overhangs for layer 0
                        {
                            Mat previousImage = null;
                            Span<byte> previousSpan = null;

                            if (islandConfig.Enabled)
                            {
                                bool canProcessCheck = true;
                                if (islandConfig.WhiteListLayers is not null) // Check white list
                                {
                                    if (!islandConfig.WhiteListLayers.Contains(layerIndex))
                                    {
                                        canProcessCheck = false;
                                    }
                                }

                                if (canProcessCheck)
                                {
                                    bool needDispose = false;
                                    Mat islandImage;
                                    if (islandConfig.BinaryThreshold > 0)
                                    {
                                        needDispose = true;
                                        islandImage = new();
                                        CvInvoke.Threshold(image, islandImage, islandConfig.BinaryThreshold, byte.MaxValue,
                                            ThresholdType.Binary);
                                    }
                                    else
                                    {
                                        islandImage = image;
                                    }

                                    using Mat labels = new();
                                    using Mat stats = new();
                                    using Mat centroids = new();
                                    var numLabels = CvInvoke.ConnectedComponentsWithStats(islandImage, labels, stats,
                                        centroids,
                                        islandConfig.AllowDiagonalBonds
                                            ? LineType.EightConnected
                                            : LineType.FourConnected);

                                    if(needDispose)
                                    {
                                        islandImage.Dispose();
                                    }

                                    // Get array that contains details of each connected component
                                    var ccStats = stats.GetData();
                                    //stats[i][0]: Left Edge of Connected Component
                                    //stats[i][1]: Top Edge of Connected Component 
                                    //stats[i][2]: Width of Connected Component
                                    //stats[i][3]: Height of Connected Component
                                    //stats[i][4]: Total Area (in pixels) in Connected Component

                                    var labelSpan = labels.GetDataSpan<int>();

                                    for (int i = 1; i < numLabels; i++)
                                    {
                                        Rectangle rect = new(
                                            (int) ccStats.GetValue(i, (int) ConnectedComponentsTypes.Left),
                                            (int) ccStats.GetValue(i, (int) ConnectedComponentsTypes.Top),
                                            (int) ccStats.GetValue(i, (int) ConnectedComponentsTypes.Width),
                                            (int) ccStats.GetValue(i, (int) ConnectedComponentsTypes.Height));

                                        if (rect.Area() < islandConfig.RequiredAreaToProcessCheck)
                                            continue;

                                        if (previousImage is null)
                                        {
                                            previousImage = this[layerIndex - 1].LayerMat;
                                            previousSpan = previousImage.GetDataSpan<byte>();
                                        }

                                        List<Point> points = new();
                                        uint pixelsSupportingIsland = 0;

                                        for (int y = rect.Y; y < rect.Bottom; y++)
                                        for (int x = rect.X; x < rect.Right; x++)
                                        {
                                            int pixel = step * y + x;
                                            if (
                                                labelSpan[pixel] !=
                                                i || // Background pixel or a pixel from another component within the bounding rectangle
                                                span[pixel] <
                                                islandConfig
                                                    .RequiredPixelBrightnessToProcessCheck // Low brightness, ignore
                                            ) continue;

                                            points.Add(new Point(x, y));

                                            if (previousSpan[pixel] >=
                                                islandConfig.RequiredPixelBrightnessToSupport)
                                            {
                                                pixelsSupportingIsland++;
                                            }
                                        }

                                        if (points.Count == 0) continue; // Should never happen

                                        var requiredSupportingPixels = Math.Max(1,
                                            points.Count * islandConfig.RequiredPixelsToSupportMultiplier);

                                        /*if (pixelsSupportingIsland >= islandConfig.RequiredPixelsToSupport)
                                                isIsland = false; // Not a island, bounding is strong, i think...
                                            else if (pixelsSupportingIsland > 0 &&
                                                points.Count < islandConfig.RequiredPixelsToSupport &&
                                                pixelsSupportingIsland >= Math.Max(1, points.Count / 2))
                                                isIsland = false; // Not a island, but maybe weak bounding...*/

                                        LayerIssue island = null;
                                        if (pixelsSupportingIsland < requiredSupportingPixels)
                                        {
                                            island = new LayerIssue(layer, LayerIssue.IssueType.Island,
                                                points.ToArray(),
                                                rect);
                                            /*AddIssue(new LayerIssue(layer, LayerIssue.IssueType.Island,
                                                    points.ToArray(),
                                                    rect));*/
                                        }

                                        // Check for overhangs
                                        if (overhangConfig.Enabled && !overhangConfig.IndependentFromIslands &&
                                            island is null
                                            || island is not null && islandConfig.EnhancedDetection &&
                                            pixelsSupportingIsland >= 10
                                        )
                                        {
                                            points.Clear();
                                            using var imageRoi = image.Roi(rect);
                                            using var previousImageRoi = previousImage.Roi(rect);
                                            using var subtractedImage = new Mat();
                                            var anchor = new Point(-1, -1);
                                            CvInvoke.Subtract(imageRoi, previousImageRoi, subtractedImage);
                                            CvInvoke.Threshold(subtractedImage, subtractedImage, 127, 255,
                                                ThresholdType.Binary);

                                            CvInvoke.Erode(subtractedImage, subtractedImage,
                                                CvInvoke.GetStructuringElement(ElementShape.Rectangle,
                                                    new Size(3, 3), anchor),
                                                anchor, overhangConfig.ErodeIterations, BorderType.Default,
                                                new MCvScalar());

                                            var subtractedSpan = subtractedImage.GetDataSpan<byte>();

                                            for (int y = 0; y < subtractedImage.Height; y++)
                                            for (int x = 0; x < subtractedImage.Step; x++)
                                            {
                                                int labelX = rect.X + x;
                                                int labelY = rect.Y + y;
                                                int pixel = subtractedImage.GetPixelPos(x, y);
                                                int pixelLabel = labelY * step + labelX;
                                                if (labelSpan[pixelLabel] != i || subtractedSpan[pixel] == 0)
                                                    continue;

                                                points.Add(new Point(labelX, labelY));
                                            }

                                            if (points.Count >= overhangConfig.RequiredPixelsToConsider
                                            ) // Overhang
                                            {
                                                AddIssue(new LayerIssue(
                                                    layer, LayerIssue.IssueType.Overhang, points.ToArray(), rect
                                                ));
                                            }
                                            else if (islandConfig.EnhancedDetection) // No overhang
                                            {
                                                island = null;
                                            }
                                        }

                                        if (island is not null)
                                            AddIssue(island);
                                    }
                                }
                            }

                            // Overhangs
                            if (!islandConfig.Enabled && overhangConfig.Enabled ||
                                (islandConfig.Enabled && overhangConfig.Enabled &&
                                 overhangConfig.IndependentFromIslands))
                            {
                                bool canProcessCheck = true;
                                if (overhangConfig.WhiteListLayers is not null) // Check white list
                                {
                                    if (!overhangConfig.WhiteListLayers.Contains(layerIndex))
                                    {
                                        canProcessCheck = false;
                                    }
                                }

                                if (canProcessCheck)
                                {
                                    previousImage ??= this[layerIndex - 1].LayerMat;

                                    using var subtractedImage = new Mat();
                                    using var vecPoints = new VectorOfPoint();
                                    var anchor = new Point(-1, -1);


                                    CvInvoke.Subtract(image, previousImage, subtractedImage);
                                    CvInvoke.Threshold(subtractedImage, subtractedImage, 127, 255,
                                        ThresholdType.Binary);

                                    CvInvoke.Erode(subtractedImage, subtractedImage,
                                        CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3),
                                            anchor),
                                        anchor, overhangConfig.ErodeIterations, BorderType.Default,
                                        new MCvScalar());

                                    CvInvoke.FindNonZero(subtractedImage, vecPoints);
                                    if (vecPoints.Size >= overhangConfig.RequiredPixelsToConsider)
                                    {
                                        AddIssue(new LayerIssue(
                                            layer, LayerIssue.IssueType.Overhang, vecPoints.ToArray(),
                                            layer.BoundingRectangle
                                        ));
                                    }
                                }
                            }

                            previousImage?.Dispose();
                        }

                        if (resinTrapConfig.Enabled)
                        {
                            /* this used to calculate all contours for the layers, however new algorithm crops the layers to the overall bounding box
                             * so the contours produced here are not translated properly. We will generate contours during the algorithm itself later */

                            bool needDispose = false;
                            Mat resinTrapImage;
                            if (resinTrapConfig.BinaryThreshold > 0)
                            {
                                resinTrapImage = new Mat();
                                CvInvoke.Threshold(image, resinTrapImage, resinTrapConfig.BinaryThreshold, byte.MaxValue, ThresholdType.Binary);
                            }
                            else
                            {
                                needDispose = true;
                                resinTrapImage = image;
                            }
                            var contourLayer = resinTrapImage.Roi(BoundingRectangle);

                            using var contours = contourLayer.FindContours(out var heirarchy, RetrType.Tree);
                            externalContours[layerIndex] = EmguContours.GetExternalContours(contours, heirarchy);
                            hollows[layerIndex] = EmguContours.GetNegativeContoursInGroups(contours, heirarchy);
                            resinTrapsContoursArea[layerIndex] = EmguContours.GetContoursArea(hollows[layerIndex]);

                            if (needDispose)
                            {
                                resinTrapImage.Dispose();
                            }

                            /*//
                            //hierarchy[i][0]: the index of the next contour of the same level
                            //hierarchy[i][1]: the index of the previous contour of the same level
                            //hierarchy[i][2]: the index of the first child
                            //hierarchy[i][3]: the index of the parent
                            //
                            var listHollowArea = new List<LayerHollowArea>();
                            var hollowGroups = EmguContours.GetNegativeContoursInGroups(contours, hierarchy);
                            var areas = EmguContours.GetContoursArea(hollowGroups);

                            for (var i = 0; i < hollowGroups.Count; i++)
                            {
                                if (areas[i] < resinTrapConfig.RequiredAreaToProcessCheck) continue;
                                var rect = CvInvoke.BoundingRectangle(hollowGroups[i][0]);
                                listHollowArea.Add(new LayerHollowArea(hollowGroups[i].ToArrayOfArray(),
                                    rect,
                                    areas[i],
                                    layer.Index <= resinTrapConfig.StartLayerIndex ||
                                    layer.Index == LayerCount - 1 // First and Last layers, always drains
                                        ? LayerHollowArea.AreaType.Drain
                                        : LayerHollowArea.AreaType.Unknown));
                            }

                            if (listHollowArea.Count > 0) layerHollowAreas.TryAdd(layer.Index, listHollowArea);*/
                        }
                    }

                    progress.LockAndIncrement();
                }); // Parallel end
            }

            var matCache = new Mat[LayerCount];
            var matTargetCache = new Mat[LayerCount];
            void CacheLayers(uint layerIndex, bool direction)
            {
                if (matCache[layerIndex] is not null) return;
                int fromLayerIndex = (int)layerIndex;
                int toLayerIndex = (int)Math.Min(LayerCount, layerIndex + Environment.ProcessorCount * 10 + 1);

                if (!direction)
                {
                    toLayerIndex = fromLayerIndex + 1;
                    fromLayerIndex = (int)Math.Max(resinTrapConfig.StartLayerIndex, fromLayerIndex - Environment.ProcessorCount * 10);
                }

                Parallel.For(fromLayerIndex, toLayerIndex,
                    i =>
                    {
                        matCache[i] = this[i].LayerMat;
                        matTargetCache[i] = matCache[i].Roi(BoundingRectangle);
                        if (resinTrapConfig.MaximumPixelBrightnessToDrain > 0)
                        {
                            CvInvoke.Threshold(matTargetCache[i], matTargetCache[i], resinTrapConfig.MaximumPixelBrightnessToDrain, byte.MaxValue, ThresholdType.Binary);
                        }
                    });
            }

            if (resinTrapConfig.Enabled)
            {
                //progress.Reset("Detecting Air Boundaries (Resin traps)", LayerCount);
                //if (progress.Token.IsCancellationRequested) return result.OrderBy(issue => issue.Type).ThenBy(issue => issue.LayerIndex).ThenBy(issue => issue.Area).ToList();
                progress.Reset("Detection pass 1 of 2 (Resin traps)", LayerCount, resinTrapConfig.StartLayerIndex);

                /* define all mats up front, reducing allocations */
                var currentAirMap = EmguExtensions.InitMat(BoundingRectangle.Size);
                var layerAirMap = currentAirMap.NewBlank();
                /* the first pass does bottom to top, and tracks anything it thinks is a resin trap */
                for (var layerIndex = resinTrapConfig.StartLayerIndex; layerIndex < LayerCount; layerIndex++)
                {
                    if (progress.Token.IsCancellationRequested) return result.OrderBy(issue => issue.Type).ThenBy(issue => issue.LayerIndex).ThenBy(issue => issue.Area).ToList();
                    
                    CacheLayers(layerIndex, true);
                    var curLayer = matTargetCache[layerIndex];

                    //curLayer.Save($"D:\\dump\\{layerIndex}_a.png");

                    /* find hollows of current layer */
                    GenerateAirMap(curLayer, layerAirMap, externalContours[layerIndex]);

                    //layerAirMap.Save($"D:\\dump\\{layerIndex}_b.png");

                    if (layerIndex == resinTrapConfig.StartLayerIndex)
                    {
                        currentAirMap = layerAirMap.Clone();
                    }

                    //currentAirMap.Save($"D:\\dump\\{layerIndex}_c.png");

                    /* remove solid areas of current layer from the air map */
                    CvInvoke.Subtract(currentAirMap, curLayer, currentAirMap);

                    //currentAirMap.Save($"D:\\dump\\{layerIndex}_d.png");

                    /* add in areas of air in current layer to air map */
                    CvInvoke.BitwiseOr(layerAirMap, currentAirMap, currentAirMap);

                    //currentAirMap.Save($"D:\\dump\\{layerIndex}_e.png");

                    if (hollows[layerIndex] is not null)
                    {
                        resinTraps[layerIndex] = new();
                        airContours[layerIndex] = new();
                        Parallel.For(0, hollows[layerIndex].Count, CoreSettings.ParallelOptions, i =>
                        {
                            //for (var i = 0; i < hollows[layerIndex].Count; i++)
                            //{
                            if (progress.Token.IsCancellationRequested) return;
                            if (resinTrapsContoursArea[layerIndex][i] < resinTrapConfig.RequiredAreaToProcessCheck) return;

                            /* intersect current contour, with the current airmap. */
                            using var currentContour = curLayer.NewBlank();
                            using var airOverlap = curLayer.NewBlank();
                            CvInvoke.DrawContours(currentContour, hollows[layerIndex][i], -1, EmguExtensions.WhiteColor, -1);
                            CvInvoke.BitwiseAnd(currentAirMap, currentContour, airOverlap);
                            var overlapCount = CvInvoke.CountNonZero(airOverlap);

                            lock (this[layerIndex].Mutex)
                            {
                                if (overlapCount == 0)
                                {
                                    /* this countour does *not* overlap known air */

                                    /* add a resin trap (for now... will be revisited in part 2) */
                                    resinTraps[layerIndex].Add(hollows[layerIndex][i]);
                                }
                                else
                                {
                                    if (overlapCount >= resinTrapConfig.RequiredBlackPixelsToDrain)
                                    {
                                        /* this contour does overlap air, add it to the current air map and remember this contour was air-connected for 2nd pass */
                                        airContours[layerIndex].Add(hollows[layerIndex][i]);

                                        CvInvoke.BitwiseOr(currentContour, currentAirMap, currentAirMap);
                                    }
                                    else
                                    {
                                        /* it overlapped ,but not by enough, treat as solid */
                                        CvInvoke.Subtract(currentAirMap, currentContour, currentAirMap);
                                    }
                                }
                            }

                        });

                        if (progress.Token.IsCancellationRequested)
                            return result.OrderBy(issue => issue.Type).ThenBy(issue => issue.LayerIndex).ThenBy(issue => issue.Area).ToList();
                    }

                    matCache[layerIndex].Dispose();
                    matCache[layerIndex] = null;
                    matTargetCache[layerIndex] = null;

                    progress++;
                }

                if (progress.Token.IsCancellationRequested) return result.OrderBy(issue => issue.Type).ThenBy(issue => issue.LayerIndex).ThenBy(issue => issue.Area).ToList();
                progress.Reset("Detection pass 2 of 2 (Resin traps)", LayerCount, resinTrapConfig.StartLayerIndex);
                /* starting over again but this time from the top to the bottom */
                if (currentAirMap is not null)
                {
                    currentAirMap.Dispose();
                    currentAirMap = null;
                }

                for (int layerIndex = (int)(LayerCount - 1); layerIndex >= resinTrapConfig.StartLayerIndex; layerIndex--)
                {
                    if (progress.Token.IsCancellationRequested) return result.OrderBy(issue => issue.Type).ThenBy(issue => issue.LayerIndex).ThenBy(issue => issue.Area).ToList();
                    
                    CacheLayers((uint)layerIndex, false);
                    var curLayer = matTargetCache[layerIndex];
                    
                    if (layerIndex == resinTraps.Length - 1)
                    {
                        /* this is subtly different that for the first pass, we don't use GenerateAirMap for the initial airmap */
                        /* instead we use a bitwise not, this way anything that is open/hollow on the top layer is treated as air */
                        currentAirMap = curLayer.Clone();
                        CvInvoke.BitwiseNot(curLayer, currentAirMap);
                    }

                    /* we still modify the airmap like normal, where we account for the air areas of the layer, and any contours that might overlap...*/
                    GenerateAirMap(curLayer, layerAirMap, externalContours[layerIndex]);

                    /* Update air map with any hollows that were found to be air-connected during first pass */
                    if (airContours[layerIndex] is not null)
                    {
                        Parallel.ForEach(airContours[layerIndex], CoreSettings.ParallelOptions, vec =>
                            CvInvoke.DrawContours(layerAirMap, vec, -1, EmguExtensions.WhiteColor, -1)
                        );
                    }

                    /* remove solid areas of current layer from the air map */
                    CvInvoke.Subtract(currentAirMap, curLayer, currentAirMap);

                    /* add in areas of air in current layer to air map */
                    CvInvoke.BitwiseOr(layerAirMap, currentAirMap, currentAirMap);

                    if (resinTraps[layerIndex] is not null)
                    {
                        suctionTraps[layerIndex] = new();
                        /* here we don't worry about finding contours on the layer, the bottom to top pass did that already */
                        /* all we care about is contours the first pass thought were resin traps, since there was no access to air from the bottom */
                        Parallel.For(0, resinTraps[layerIndex].Count, CoreSettings.ParallelOptions, x =>
                        {
                            if (progress.Token.IsCancellationRequested) return;

                            /* check if each contour overlaps known air */
                            using var currentContour = curLayer.NewBlank();
                            using var airOverlap = curLayer.NewBlank();
                            CvInvoke.DrawContours(currentContour, resinTraps[layerIndex][x], -1, EmguExtensions.WhiteColor, -1);

                            CvInvoke.BitwiseAnd(currentAirMap, currentContour, airOverlap);
                            var overlapCount = CvInvoke.CountNonZero(airOverlap);

                            lock (SlicerFile[layerIndex].Mutex)
                            {
                                if (overlapCount >= resinTrapConfig.RequiredBlackPixelsToDrain)
                                {
                                    /* this contour does overlap air, add this it our air map */
                                    CvInvoke.BitwiseOr(currentContour, currentAirMap, currentAirMap);
                                    /* Always add the removed contour to suctionTraps (even if we aren't reporting suction traps)
                                     * This is because contours that are placed on here get removed from resin traps in the next stage
                                     * if you don't put them here, they never get removed even if they should :) */

                                    /* if we haven't defined a suctionTrap list for this layer, do so */


                                    /* since we know it isn't a resin trap, it becomes a suction trap */
                                    suctionTraps[layerIndex].Add(resinTraps[layerIndex][x]);

                                    /* to keep things tidy while we iterate resin traps, it will be left in the list for now, and removed later */
                                }
                                else
                                {
                                    /* doesn't overlap by enough, remove from air map */
                                    CvInvoke.Subtract(currentAirMap, currentContour, currentAirMap);
                                }
                            }
                        });

                        /* anything that converted to a suction trap needs to removed from resinTraps. Loop backwards so indexes don't shift */
                        if (suctionTraps[layerIndex] is not null)
                        {
                            for (var i = suctionTraps[layerIndex].Count - 1; i >= 0; i--)
                            {
                                resinTraps[layerIndex].Remove(suctionTraps[layerIndex][i]);
                                if (resinTraps[layerIndex].Count > 0) continue;
                                resinTraps[layerIndex] = null;
                                break;
                            }
                            
                        }
                    }

                    matCache[layerIndex].Dispose();
                    matCache[layerIndex] = null;
                    matTargetCache[layerIndex] = null;

                    progress++;
                }
                if (progress.Token.IsCancellationRequested) return result.OrderBy(issue => issue.Type).ThenBy(issue => issue.LayerIndex).ThenBy(issue => issue.Area).ToList();
                
                progress.Reset("Translate contours (Resin traps)", 0/*(uint)(resinTraps.Length + suctionTraps.Length)*/);

                /* translate all contour points by ROI x and y */
                var offsetBy = new Point(BoundingRectangle.X, BoundingRectangle.Y);
                foreach (var listOfLayers in new[] { resinTraps, suctionTraps })
                {
                    Parallel.ForEach(listOfLayers, contoursGroups =>
                    {
                        if (contoursGroups is null) return;
                        for (var groupIndex = 0; groupIndex < contoursGroups.Count; groupIndex++)
                        {
                            var contours = contoursGroups[groupIndex];
                            if (contours is null) continue;

                            var arrayOfArrayOfPoints = contours.ToArrayOfArray();

                            foreach (var pointArray in arrayOfArrayOfPoints)
                                for (var i = 0; i < pointArray.Length; i++)
                                    pointArray[i].Offset(offsetBy);

                            contoursGroups[groupIndex].Dispose();
                            contoursGroups[groupIndex] = new VectorOfVectorOfPoint(arrayOfArrayOfPoints);
                        }

                        //progress.LockAndIncrement();
                    });
                }
                
                if (progress.Token.IsCancellationRequested) return result.OrderBy(issue => issue.Type).ThenBy(issue => issue.LayerIndex).ThenBy(issue => issue.Area).ToList();
                
                for (var layerIndex = 0; layerIndex < resinTraps.Length; layerIndex++)
                {
                    if (resinTraps[layerIndex] == null) continue;

                    /* select new LayerIssue(this[layerIndex], LayerIssue.IssueType.ResinTrap, area.Contour, area.BoundingRectangle)) */
                    foreach (var trap in resinTraps[layerIndex])
                    {
                        var rect = CvInvoke.BoundingRectangle(trap[0]);
                        AddIssue(new LayerIssue(this[layerIndex], LayerIssue.IssueType.ResinTrap, trap.ToArrayOfArray(), rect));
                    }
                }

                /* only report suction cup issues if enabled */
                if (resinTrapConfig.DetectSuctionCups)
                {
                    var minimumSuctionArea = resinTrapConfig.RequiredAreaToConsiderSuctionCup;
                    for (var layerIndex = suctionTraps.Length - 1; layerIndex >= 0 ; layerIndex--)
                    {
                        if (suctionTraps[layerIndex] == null) continue;

                        foreach (var trap in suctionTraps[layerIndex])
                        {
                            var suctionTrapArea = EmguContours.GetContourArea(trap);
                            if (suctionTrapArea < minimumSuctionArea) continue;
                            var rect = CvInvoke.BoundingRectangle(trap[0]);
                            AddIssue(new LayerIssue(this[layerIndex], LayerIssue.IssueType.SuctionCup, trap.ToArrayOfArray(), rect) { Area = suctionTrapArea });
                        }
                    }
                }

                // Dispose
                foreach (var listOfVectors in new[]{ resinTraps, suctionTraps, hollows, airContours})
                {
                    foreach (var vectorArray in listOfVectors)
                    {
                        if (vectorArray is null) continue;
                        foreach (var vector in vectorArray)
                        {
                            vector?.Dispose();
                        }
                    }
                }

                foreach (var vector in externalContours)
                {
                    vector?.Dispose();
                }

            }

            return result.OrderBy(issue => issue.Type).ThenBy(issue => issue.LayerIndex).ThenBy(issue => issue.Area).ToList();
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
            Array.Resize(ref _layers, (int) newLayerCount);
            if (differenceLayerCount > 0 && initBlack)
            {
                Parallel.For(oldLayerCount, newLayerCount, CoreSettings.ParallelOptions, layerIndex =>
                {
                    this[layerIndex] = new Layer((uint)layerIndex, EmguExtensions.InitMat(SlicerFile.Resolution), this);
                });
            }
        }

        /// <summary>
        /// Reallocate at given index
        /// </summary>
        /// <returns></returns>
        public void ReallocateInsert(uint insertAtLayerIndex, uint layerCount, bool initBlack = false)
        {
            var newLayers = new Layer[LayerCount + layerCount];

            // Rearrange
            for (uint layerIndex = 0; layerIndex < insertAtLayerIndex; layerIndex++)
            {
                newLayers[layerIndex] = _layers[layerIndex];
            }

            // Rearrange
            for (uint layerIndex = insertAtLayerIndex; layerIndex < _layers.Length; layerIndex++)
            {
                newLayers[layerCount + layerIndex] = _layers[layerIndex];
                newLayers[layerCount + layerIndex].Index = layerCount + layerIndex;
            }

            // Allocate new layers
            if (initBlack)
            {
                Parallel.For(insertAtLayerIndex, insertAtLayerIndex + layerCount, CoreSettings.ParallelOptions, layerIndex =>
                {
                    newLayers[layerIndex] = new Layer((uint) layerIndex, EmguExtensions.InitMat(SlicerFile.Resolution), this);
                });
            }
            /*for (uint layerIndex = insertAtLayerIndex; layerIndex < insertAtLayerIndex + layerCount; layerIndex++)
            {
                Layers[layerIndex] = initBlack ? new Layer(layerIndex, EmguExtensions.InitMat(SlicerFile.Resolution), this) : null;
            }*/
            _layers = newLayers;
        }

        /// <summary>
        /// Reallocate at a kept range
        /// </summary>
        /// <param name="startLayerIndex"></param>
        /// <param name="endLayerIndex"></param>
        public void ReallocateRange(uint startLayerIndex, uint endLayerIndex)
        {
            if ((int)(endLayerIndex - startLayerIndex) < 0) return;
            var newLayers = new Layer[1 + endLayerIndex - startLayerIndex];

            uint currentLayerIndex = 0;
            for (uint layerIndex = startLayerIndex; layerIndex <= endLayerIndex; layerIndex++)
            {
                newLayers[currentLayerIndex++] = _layers[layerIndex];
            }

            _layers = newLayers;
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
            Parallel.For(0L, mats.Length, CoreSettings.ParallelOptions, i =>
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
