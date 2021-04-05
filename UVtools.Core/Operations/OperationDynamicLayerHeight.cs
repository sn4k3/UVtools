/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationDynamicLayerHeight : Operation
    {
        #region Sub Classes
        public sealed class Report
        {
            public uint OldLayerCount { get; set; }
            public uint NewLayerCount { get; set; }
            public uint StackedLayers { get; set; }
            public uint ReusedLayers => OldLayerCount - StackedLayers;
            public float MaximumLayerHeight { get; set; }
            public float OldPrintTime { get; set; }
            public float NewPrintTime { get; set; }
            public double SparedPrintTime => Math.Round(OldPrintTime - NewPrintTime, 2);

            public double CompressionRatio => Math.Round((double)OldLayerCount / NewLayerCount * 100.0, 2);

            public override string ToString()
            {
                var oldTime = TimeSpan.FromSeconds(OldPrintTime);
                var newTime = TimeSpan.FromSeconds(NewPrintTime);
                var sparedTime = TimeSpan.FromSeconds(SparedPrintTime);
                return
                    $"From {OldLayerCount} layers, {ReusedLayers} got reused, {StackedLayers} got stacked and optimized with dynamic layer height's\n" +
                    $"Resultant layers: {NewLayerCount}\n" +
                    $"Compression ratio: {CompressionRatio}%\n" +
                    $"Maximum layer height reached: {MaximumLayerHeight}mm\n" +
                    $"Print time: {oldTime.Hours}h{oldTime.Minutes}m{oldTime.Seconds}s -> {newTime.Hours}h{newTime.Minutes}m{newTime.Seconds}s (- {sparedTime.Hours}h{sparedTime.Minutes}m{sparedTime.Seconds}s)";
            }
        }

        #endregion

        #region Constants
        public const byte ObjectsPerCache = 2;
        #endregion

        #region Members

        //private decimal _displayWidth;
        //private decimal _displayHeight;
        private decimal _minimumLayerHeight = 0.03m;
        private decimal _maximumLayerHeight = 0.10m;
        private decimal _cacheRamSize = 1.5m;
        private ExposureSetTypes _exposureSetType = ExposureSetTypes.Linear;
        private decimal _bottomExposureStep = 0.5m;
        private decimal _exposureStep = 0.2m;
        private ObservableCollection<ExposureItem> _automaticExposureTable = new();
        private ObservableCollection<ExposureItem> _manualExposureTable = new();

        #endregion

        #region Overrides

        public override bool CanROI => false;

        public override string Title => "Dynamic layer height";

        public override string Description =>
            "Analyze and optimize the model with dynamic layer heights, larger angles will slice at lower layer height" +
            " while more straight angles will slice larger layer height.\n" +
            "Note: The model should be sliced at the lowest layer height possible (0.01mm).\n" +
            "After this, do not apply any modification which reconstruct the z positions of the layers. " +
            "Only few printers support this, make sure your is supported or else it will print a malformed model.";

        public override string ConfirmationText =>
            $"dynamic layers from layers {LayerIndexStart} through {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Analyzing and optimizing layers height from layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Processed layers";

        public override string ValidateInternally()
        {
            var sb = new StringBuilder();

            /*if (XYResolutionUm <= 0)
            {
                sb.AppendLine($"Display width and height must be a positive value.");
            }*/

            decimal layerHeight = (decimal) SlicerFile.LayerHeight;
            if (_minimumLayerHeight < layerHeight)
            {
                sb.AppendLine(
                    $"Minimum layer height ({_minimumLayerHeight}mm) must be equal or higher than file layer height ({layerHeight}mm)");
            }
            if (_minimumLayerHeight > _maximumLayerHeight)
            {
                sb.AppendLine(
                    $"Minimum layer height ({_minimumLayerHeight}mm) can't be higher than maximum layer height ({_maximumLayerHeight}mm)");
            }
            if (layerHeight >= _maximumLayerHeight)
            {
                sb.AppendLine(
                    $"Maximum layer height ({_maximumLayerHeight}mm) can't be the same or less than current file layer height ({SlicerFile.LayerHeight}mm)");
            }

            var exposureTable = ExposureTableDictionary;

            for (layerHeight = (decimal) SlicerFile.LayerHeight;
                layerHeight <= _maximumLayerHeight;
                layerHeight = layerHeight + (decimal) SlicerFile.LayerHeight)
            {
                layerHeight = Layer.RoundHeight(layerHeight);
                if (exposureTable.TryGetValue(layerHeight, out var exposure))
                {
                    if (exposure.BottomExposure <= 0 || exposure.Exposure <= 0)
                    {
                        sb.AppendLine($"Layer height {layerHeight}mm exposures must be a positive value, current: {exposure.BottomExposure}s/{exposure.Exposure}s");
                    }
                }
                else
                {
                    sb.AppendLine($"Layer height {layerHeight}mm exposures are missing.");
                }
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            var result = $"[Layer Height: Min: {_minimumLayerHeight}mm Max: {_maximumLayerHeight}mm] [RAM: {_cacheRamSize}Gb] [Exposure type: {_exposureSetType}, Steps: {_bottomExposureStep}s/{_exposureStep}s]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }


        #endregion

        #region Enums

        public enum ExposureSetTypes: byte
        {
            Linear,
            Multiplier,
            Manual
        }

        public static Array ExposureSetTypeItems => Enum.GetValues(typeof(ExposureSetTypes));

        #endregion

        #region Properties

        /*public decimal DisplayWidth
        {
            get => _displayWidth;
            set
            {
                if (!RaiseAndSetIfChanged(ref _displayWidth, Math.Round(value, 2))) return;
                //RaisePropertyChanged(nameof(XYResolutionUm));
            }
        }

        public decimal DisplayHeight
        {
            get => _displayHeight;
            set
            {
                if (!RaiseAndSetIfChanged(ref _displayHeight, Math.Round(value, 2))) return;
                //RaisePropertyChanged(nameof(XYResolutionUm));
            }
        }

        public decimal XYResolutionUm => DisplayWidth > 0 || DisplayHeight > 0 ?
            Math.Round(Math.Max(
                DisplayWidth / SlicerFile.ResolutionX,
                DisplayHeight / SlicerFile.ResolutionY
            ), 3) * 1000
            : 0;
        */

        public decimal MinimumLayerHeight
        {
            get => _minimumLayerHeight;
            set
            {
                if (!RaiseAndSetIfChanged(ref _minimumLayerHeight, Layer.RoundHeight(value))) return;
                //RaisePropertyChanged(nameof(ExposureData));
                //if (!IsExposureSetTypeManual) RebuildAutoExposureTable();
            }
        }

        public decimal MaximumLayerHeight
        {
            get => _maximumLayerHeight;
            set
            {
                if(!RaiseAndSetIfChanged(ref _maximumLayerHeight, Layer.RoundHeight(value))) return;
                //RaisePropertyChanged(nameof(ExposureData));
                if(!IsExposureSetTypeManual) RebuildAutoExposureTable();
            }
        }

        public uint CacheObjectCount => (uint) (_cacheRamSize * 1000000000L / SlicerFile.Resolution.Area() / ObjectsPerCache);

        public decimal CacheRAMSize
        {
            get => _cacheRamSize;
            set
            {
                if(!RaiseAndSetIfChanged(ref _cacheRamSize, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(CacheObjectCount));
            }
        }

        public ExposureSetTypes ExposureSetType
        {
            get => _exposureSetType;
            set
            {
                if(!RaiseAndSetIfChanged(ref _exposureSetType, value)) return;
                RaisePropertyChanged(nameof(IsExposureSetTypeManual));
                RaisePropertyChanged(nameof(ExposureTable));
                RaisePropertyChanged(nameof(ExposureTableDictionary));
                //RaisePropertyChanged(nameof(ExposureData));
                if (!IsExposureSetTypeManual) RebuildAutoExposureTable();
            }
        }

        public bool IsExposureSetTypeManual => _exposureSetType == ExposureSetTypes.Manual;

        public decimal BottomExposureStep
        {
            get => _bottomExposureStep;
            set
            {
                if(!RaiseAndSetIfChanged(ref _bottomExposureStep, value)) return;
                //RaisePropertyChanged(nameof(ExposureData));
                if (!IsExposureSetTypeManual) RebuildAutoExposureTable();
            }
        }

        public decimal ExposureStep
        {
            get => _exposureStep;
            set
            {
                if(!RaiseAndSetIfChanged(ref _exposureStep, value)) return;
                //RaisePropertyChanged(nameof(ExposureData));
                if (!IsExposureSetTypeManual) RebuildAutoExposureTable();
            }
        }

        [XmlIgnore]
        public ObservableCollection<ExposureItem> AutomaticExposureTable
        {
            get
            {
                if(_automaticExposureTable.Count == 0) RebuildAutoExposureTable();
                return _automaticExposureTable;
            }
            set => RaiseAndSetIfChanged(ref _automaticExposureTable, value);
        }

        public ObservableCollection<ExposureItem> ManualExposureTable
        {
            get => _manualExposureTable;
            set => RaiseAndSetIfChanged(ref _manualExposureTable, value);
        }

        [XmlIgnore]
        public ObservableCollection<ExposureItem> ExposureTable => IsExposureSetTypeManual ? _manualExposureTable : AutomaticExposureTable;

        /// <summary>
        /// Gets the exposure table into a dictionary where key is the layer height
        /// </summary>
        [XmlIgnore]
        public Dictionary<decimal, ExposureItem> ExposureTableDictionary
        {
            get
            {
                Dictionary<decimal, ExposureItem> dictionary = new();
                foreach (var exposure in ExposureTable)
                {
                    dictionary.TryAdd(exposure.LayerHeight, exposure);
                }

                return dictionary;
            }
        }

        public string ExposureData
        {
            get
            {
                StringBuilder sb = new();
                byte count = 0;
                for (decimal layerHeight = (decimal) SlicerFile.LayerHeight; layerHeight <= _maximumLayerHeight; layerHeight+= (decimal)SlicerFile.LayerHeight)
                {
                    decimal bottomExposure = 0;
                    decimal exposure = 0;
                    switch (_exposureSetType)
                    {
                        case ExposureSetTypes.Linear:
                            bottomExposure = (decimal)SlicerFile.BottomExposureTime + count * _bottomExposureStep;
                            exposure = (decimal)SlicerFile.ExposureTime + count * _exposureStep;
                            break;
                        case ExposureSetTypes.Multiplier:
                            bottomExposure = (decimal)SlicerFile.BottomExposureTime + (decimal)SlicerFile.BottomExposureTime * count * layerHeight * _bottomExposureStep;
                            exposure = (decimal)SlicerFile.BottomExposureTime + (decimal)SlicerFile.ExposureTime * count * layerHeight * _exposureStep;
                            break;
                        case ExposureSetTypes.Manual:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    sb.AppendLine($"{layerHeight:F2}mm: {bottomExposure:F2}s / {exposure:F2}s");
                    count++;
                }
                return sb.ToString();
            }
        }

        #endregion

        #region Constructor

        public OperationDynamicLayerHeight()
        {
            //InitManualTable();
        }

        public OperationDynamicLayerHeight(FileFormat slicerFile) : base(slicerFile)
        {
            InitManualTable();
        }

        public override void InitWithSlicerFile()
        {
            base.InitWithSlicerFile();

            /*if (SlicerFile.DisplayWidth > 0)
                _displayWidth = (decimal)SlicerFile.DisplayWidth;
            if (SlicerFile.DisplayHeight > 0)
                _displayHeight = (decimal)SlicerFile.DisplayHeight;*/

            var layerHeight = (decimal)SlicerFile.LayerHeight;
            if (_minimumLayerHeight < layerHeight)
            {
                _minimumLayerHeight = layerHeight;
            }
            if (layerHeight * 2 > _maximumLayerHeight)
            {
                _maximumLayerHeight = Math.Min((decimal) FileFormat.MaximumLayerHeight, _maximumLayerHeight*2);
            }
        }

        public void InitManualTable()
        {
            for (decimal layerHeight = (decimal)FileFormat.MinimumLayerHeight;
                layerHeight <= (decimal) FileFormat.MaximumLayerHeight;
                layerHeight += (decimal)FileFormat.MinimumLayerHeight)
            {
                var item = new ExposureItem(layerHeight);
                if (layerHeight == (decimal) SlicerFile.LayerHeight)
                {
                    item.BottomExposure = (decimal) SlicerFile.BottomExposureTime;
                    item.Exposure = (decimal) SlicerFile.ExposureTime;
                }
                _manualExposureTable.Add(item);
            }
        }

        #endregion

        #region Equality

        private bool Equals(OperationDynamicLayerHeight other)
        {
            return _minimumLayerHeight == other._minimumLayerHeight && _maximumLayerHeight == other._maximumLayerHeight && _cacheRamSize == other._cacheRamSize && _exposureSetType == other._exposureSetType && _bottomExposureStep == other._bottomExposureStep && _exposureStep == other._exposureStep && Equals(_manualExposureTable, other._manualExposureTable);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is OperationDynamicLayerHeight other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_minimumLayerHeight, _maximumLayerHeight, _cacheRamSize, (int) _exposureSetType, _bottomExposureStep, _exposureStep, _manualExposureTable);
        }

        #endregion

        #region Methods

        public void RebuildAutoExposureTable()
        {
            if (SlicerFile is null) return;
            _automaticExposureTable.Clear();
            byte count = 0;
            for (decimal layerHeight = (decimal)SlicerFile.LayerHeight; layerHeight <= _maximumLayerHeight; layerHeight += (decimal)SlicerFile.LayerHeight)
            {
                decimal bottomExposure = 0;
                decimal exposure = 0;
                switch (_exposureSetType)
                {
                    case ExposureSetTypes.Linear:
                        bottomExposure = (decimal)SlicerFile.BottomExposureTime + count * _bottomExposureStep;
                        exposure = (decimal)SlicerFile.ExposureTime + count * _exposureStep;
                        break;
                    case ExposureSetTypes.Multiplier:
                        bottomExposure = (decimal)SlicerFile.BottomExposureTime + (decimal)SlicerFile.BottomExposureTime * count * layerHeight * _bottomExposureStep;
                        exposure = (decimal)SlicerFile.BottomExposureTime + (decimal)SlicerFile.ExposureTime * count * layerHeight * _exposureStep;
                        break;
                    case ExposureSetTypes.Manual:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                _automaticExposureTable.Add(new ExposureItem(layerHeight, Math.Round(bottomExposure, 2), Math.Round(exposure, 2)));
                count++;
            }
        }

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            Report report = new()
            {
                OldLayerCount = SlicerFile.LayerCount,
                OldPrintTime = SlicerFile.PrintTime
            };

            var anchor = new Point(-1, -1);
            using var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);

            var matCache = new Mat[SlicerFile.LayerCount];
            var matThresholdCache = new Mat[SlicerFile.LayerCount];

            List<Layer> layers = new();

            using Mat matXor = new();
            Mat matXorSum = null;
            Mat matSum = null;

            const byte _maximumErodes = 10;
            //decimal xyResolutionUm = XYResolutionUm;

            //const double xyRes = 35;
            //var stepAngle = Math.Atan(SlicerFile.LayerHeight*1000 / xyRes) * (180 / Math.PI);
            //byte maximumErodes = (byte) (_maximumLayerHeight * 100 - (decimal) (slicerFile.LayerHeight * 100f));

            void CacheLayers(uint fromLayerIndex)
            {
                for (int layerIndex = (int) fromLayerIndex-2; layerIndex >= 0; layerIndex--) // clean up only the required layers
                {
                    if (matCache[layerIndex] is null) break;
                    matCache[layerIndex].Dispose();
                    matCache[layerIndex] = null;
                    matThresholdCache[layerIndex].Dispose();
                    matThresholdCache[layerIndex] = null;
                }
                Parallel.For(fromLayerIndex, Math.Min(fromLayerIndex + CacheObjectCount, SlicerFile.LayerCount), layerIndex =>
                {
                    if (matCache[layerIndex] is not null) return; // Already cached
                    matCache[layerIndex] = SlicerFile[layerIndex].LayerMat;
                    matThresholdCache[layerIndex] = new Mat();

                    // Clean AA
                    CvInvoke.Threshold(matCache[layerIndex], matThresholdCache[layerIndex], 128, 255, ThresholdType.Binary);
                });
            }

            float GetLastPositionZ(float layerHeight) => layers.Count > 0 ? Layer.RoundHeight(layers[^1].PositionZ + layerHeight) : layerHeight;

            (Mat, Mat) GetLayer(uint layerIndex)
            {
                if (matCache[layerIndex] is null)
                {
                    CacheLayers(layerIndex);
                }

                return (matCache[layerIndex], matThresholdCache[layerIndex]);
            }

            void AddNewLayer(Mat mat, float layerHeight)
            {
                report.MaximumLayerHeight = Math.Max(report.MaximumLayerHeight, layerHeight);
                var positionZ = GetLastPositionZ(layerHeight);
                var layer = new Layer((uint) layers.Count, mat, SlicerFile)
                {
                    IsModified = true,
                    PositionZ = positionZ

                };
                layers.Add(layer);
            }

            void ReUseLayer(uint layerIndex)
            {
                SlicerFile[layerIndex].PositionZ = GetLastPositionZ(SlicerFile.LayerHeight);
                SlicerFile[layerIndex].Index = (uint) layers.Count;
                SlicerFile[layerIndex].IsModified = true;
                layers.Add(SlicerFile[layerIndex]);
            }

            for (uint layerIndex = 0; layerIndex < LayerIndexStart; layerIndex++) // Skip layers and re-use layers
            {
                ReUseLayer(layerIndex);
            }

            for (uint layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; )
            {
                Debug.WriteLine($"Head Layer: {layerIndex} ({SlicerFile.LayerHeight}mm)");

                if (layerIndex == LayerIndexEnd)
                {
                    ReUseLayer(layerIndex);
                    break;
                }

                var currentLayerHeight = SlicerFile.LayerHeight;
                byte layerSum = 1;
                byte erodeCount = 0;
                //byte maxErodeCount = 0;
                //double maxSlop = 90;
                matSum?.Dispose();
                matSum = null;
                matXorSum?.Dispose();
                matXorSum = null;

                while (true)
                {
                    progress.Token.ThrowIfCancellationRequested();
                    progress.ProcessedItems = layerIndex;

                    if (currentLayerHeight >= (float)_maximumLayerHeight || layerIndex == LayerIndexEnd)
                    {
                        // Cant perform any additional stack. Maximum layer height reached!
                        // Break this cycle and restart from same layer
                        matSum ??= GetLayer(layerIndex).Item1.Clone(); // This only happen when layer height is already the maximum supported layer height
                        layerIndex++;
                        break;
                    }

                    var previousLayerHeight = currentLayerHeight;
                    currentLayerHeight = Layer.RoundHeight(currentLayerHeight + SlicerFile.LayerHeight);

                    var (mat1, mat1Threshold) = GetLayer(layerIndex);
                    var (mat2, mat2Threshold) = GetLayer(++layerIndex);

                    Debug.Write($"  Stacking layer: {layerIndex} ({currentLayerHeight}mm)");

                    matSum ??= mat1.Clone();
                    
                    CvInvoke.BitwiseXor(mat1Threshold, mat2Threshold, matXor);
                    if (matXorSum is null)
                    {
                        matXorSum = matXor.Clone();
                    }
                    else
                    {
                        CvInvoke.Add(matXorSum, matXor, matXorSum);
                    }

                    var currentLayerHeigthUm = currentLayerHeight * 1000.0;

                    if (CvInvoke.CountNonZero(matXorSum) > 0) // Layers are different
                    {
                        bool meetRequirement = false;
                        for (; erodeCount <= _maximumErodes; )
                        {
                            erodeCount++;
                            //maxErodeCount = Math.Max(maxErodeCount, erodeCount);

                            /*var slope = Math.Atan(currentLayerHeigthUm / (double) (xyResolutionUm * erodeCount)) * (180 / Math.PI);
                            var stepover = Math.Round(currentLayerHeigthUm / Math.Tan(slope * (Math.PI / 180)));
                            Debug.Write($" [Slope: {slope:F2} Stepover: {stepover} <= {xyResolutionUm} = {stepover <= (double) xyResolutionUm}]");

                            if (stepover > (double) xyResolutionUm)
                            {
                                meetRequirement = false;
                                break;
                            }*/
                            
                            CvInvoke.Erode(matXorSum, matXor, kernel, anchor, 1, BorderType.Reflect101, default);
                            if (CvInvoke.CountNonZero(matXor) == 0) // Image pixels exhausted and got empty image, can pack and go next
                            {
                                meetRequirement = true;
                                break;
                            } 
                        }

                        //if ((!meetRequirement || erodeCount >= _maximumErodes) && _minimumLayerHeight < (decimal) currentLayerHeight
                        // To many pixels, image still not blank, pack the previous group and start again from current height
                        if (!meetRequirement && _minimumLayerHeight < (decimal) currentLayerHeight)
                        {
                            currentLayerHeight = previousLayerHeight;
                            Debug.WriteLine(string.Empty);
                            break;
                        }
                    }
                    else
                    {
                        //erodeCount++; // Safe check
                        Debug.Write(" [Equal layer]");
                    }

                    layerSum++;
                    CvInvoke.Add(matSum, mat2, matSum);
                    Debug.WriteLine(string.Empty);
                }

                if (layerSum > 1) report.StackedLayers += layerSum;
                Debug.WriteLine($" Packing {layerSum} layers with {currentLayerHeight}mm");
                // Add the result


                var positionZ = GetLastPositionZ(currentLayerHeight);
                if ((decimal)positionZ != (decimal)SlicerFile[layerIndex-1].PositionZ)
                {
                    Debug.WriteLine($"{layerIndex}: ({positionZ}mm != {SlicerFile[layerIndex-1].PositionZ}mm) Height mismatch!!");
                    throw new InvalidOperationException($"Model height integrity has been violated at layer {layerIndex}/{layers.Count} ({positionZ}mm != {SlicerFile[layerIndex - 1].PositionZ}mm), this operation will not proceed.");
                }
                AddNewLayer(matSum, currentLayerHeight);
            }

            for (int i = (int) LayerIndexEnd; i >= 0; i--)
            {
                if(matCache[i] is null) break;
                matCache[i].Dispose();
                matThresholdCache[i].Dispose();
            }

            for (uint layerIndex = LayerIndexEnd+1; layerIndex < SlicerFile.LayerCount; layerIndex++) // Add left-overs
            {
                ReUseLayer(layerIndex);
            }

            SlicerFile.SuppressRebuildPropertiesWork(() =>
            {
                SlicerFile.LayerManager.Layers = layers.ToArray();
            }, true, false);

            // Set exposures times per layer
            var exposureDictionary = ExposureTableDictionary;
            for (uint layerIndex = 0; layerIndex < SlicerFile.LayerCount; layerIndex++)
            {
                float bottomExposure = SlicerFile.BottomExposureTime;
                float exposure = SlicerFile.ExposureTime;
                if(exposureDictionary.TryGetValue((decimal)SlicerFile[layerIndex].LayerHeight, out var item))
                {
                    bottomExposure = (float) item.BottomExposure;
                    exposure = (float) item.Exposure;
                }

                SlicerFile[layerIndex].ExposureTime = SlicerFile.GetInitialLayerValueOrNormal(layerIndex, bottomExposure, exposure);
            }
            //var layer = slicerFile.LayerManager.Layers[^1];

            /*Debug.WriteLine(layer.ExposureTime);
            Debug.WriteLine(layer.Index);
            Debug.WriteLine(layer.Filename);
            Debug.WriteLine(layer.IsNormalLayer);
            Debug.WriteLine(layer.IsBottomLayer);
            Debug.WriteLine(layer.LiftHeight);
            Debug.WriteLine(layer.LiftSpeed);
            Debug.WriteLine(layer.LightOffDelay);
            Debug.WriteLine(layer.LightPWM);
            Debug.WriteLine(layer.BoundingRectangle);*/
            //Debug.WriteLine(layer.LayerHeight);
            //Debug.WriteLine(layer);
            /*Debug.WriteLine(slicerFile.LayerManager);
            foreach (var layer in slicerFile)
            {
                Debug.WriteLine(layer.Index);
            }*/

            report.NewLayerCount = SlicerFile.LayerCount;
            report.NewPrintTime = SlicerFile.PrintTime;
            Tag = report;

            return true;
        }

        #endregion
    }
}
