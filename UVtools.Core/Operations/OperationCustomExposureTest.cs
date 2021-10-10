/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Extensions;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationCustomExposureTest : Operation
    {
        #region Members
        private decimal _firstBottomExposure;
        private decimal _firstNormalExposure;
        private int _numberOfCopies = 5;
        private decimal _exposureStep = 0.25M;
        private int _margin = 100;

        private bool _differentSettingsForAdditionalLayers;
        private bool _additionalLayersLiftHeightEnabled = true;
        private decimal _additionalLayersLiftHeight;
        private bool _additionalLayersWaitTimeBeforeCureEnabled = true;
        private decimal _additionalLayersWaitTimeBeforeCure;

        private bool _overrideRaft = true; // True means replace all flat layers with a rectangular raft (to ake sure there is a base for the label)
        private int _flatLayers = 10; // 0 Means use the base layers as the flat layers
        private int _labelLayers = 10;

        private FontFace _font = Emgu.CV.CvEnum.FontFace.HersheyDuplex;
        private double _fontScale = 1.5;

        #endregion

        #region Overrides
        // public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.Bottom;
        public override string Title => "Custom exposure test";
        public override string Description =>
            "The custom exposure test operator makes multiple copies of the existing model, shifting them over "+
            "and adjusting their exposure. Can be used to generate exposure test prints for external STLs (such as Ameralabs' Town).\n" +
            "Only works on printers that support multiple exposures at the same z positions.\n"+
            "After this, do not apply any modification which reconstruct the z positions of the layers.";

        public override string ConfirmationText =>
            $"Create exposure test for layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressTitle =>
            $"Create exposure test for layers {LayerIndexStart} to {LayerIndexEnd}";

        public override string ProgressAction => "Cloned layers";

        public override string ValidateSpawn()
        {
            if (!SlicerFile.CanUseLayerLiftHeight || !SlicerFile.CanUseLayerExposureTime)
            {
                return NotSupportedMessage;
            }

            return null;
        }

        public override string ValidateInternally()
        {
            var sb = new StringBuilder();

            //if (LayerRangeHaveBottoms && _firstBottomExposure == _secondBottomExposure && _firstBottomErodeIterations == _secondBottomErodeIterations)
            //    sb.AppendLine("The settings for bottoms layers will produce exactly to equal layers");


            float lastPositionZ = SlicerFile[LayerIndexStart].PositionZ;
            for (uint layerIndex = LayerIndexStart + 1; layerIndex <= LayerIndexEnd; layerIndex++)
            {
                if (lastPositionZ == SlicerFile[layerIndex].PositionZ)
                {
                    sb.AppendLine($"The selected layer range already have modified layers with same z position, starting at layer {layerIndex}. Not safe to continue.");
                    break;
                }
                lastPositionZ = SlicerFile[layerIndex].PositionZ;
            }


            return sb.ToString();
        }

        public override string ToString()
        {
            var result = $"[Base exposure: {_firstBottomExposure}/{_firstNormalExposure}s] " +
                         $"[Copies: {_numberOfCopies} Exposure Step: {_exposureStep}, Margin: {_margin}] " +
                         $"[Override raft?: {_overrideRaft} Flat Layers: {_flatLayers} Label Layers: {_labelLayers}] " +
                         LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }
        #endregion

        #region Properties

        public decimal FirstBottomExposure
        {
            get => _firstBottomExposure;
            set => RaiseAndSetIfChanged(ref _firstBottomExposure, Math.Round(value, 2));
        }

        public decimal FirstNormalExposure
        {
            get => _firstNormalExposure;
            set => RaiseAndSetIfChanged(ref _firstNormalExposure, Math.Round(value, 2));
        }

        public int NumberOfCopies
        {
            get => _numberOfCopies;
            set => RaiseAndSetIfChanged(ref _numberOfCopies, value);
        }

        public decimal ExposureStep
        {
            get => _exposureStep;
            set => RaiseAndSetIfChanged(ref _exposureStep, value);
        }

        public int Margin
        {
            get => _margin;
            set => RaiseAndSetIfChanged(ref _margin, value);
        }

        public bool DifferentSettingsForAdditionalLayers
        {
            get => _differentSettingsForAdditionalLayers;
            set => RaiseAndSetIfChanged(ref _differentSettingsForAdditionalLayers, value);
        }

        public bool AdditionalLayersLiftHeightEnabled
        {
            get => _additionalLayersLiftHeightEnabled;
            set => RaiseAndSetIfChanged(ref _additionalLayersLiftHeightEnabled, value);
        }

        public decimal AdditionalLayersLiftHeight
        {
            get => _additionalLayersLiftHeight;
            set => RaiseAndSetIfChanged(ref _additionalLayersLiftHeight, value);
        }

        public bool AdditionalLayersWaitTimeBeforeCureEnabled
        {
            get => _additionalLayersWaitTimeBeforeCureEnabled;
            set => RaiseAndSetIfChanged(ref _additionalLayersWaitTimeBeforeCureEnabled, value);
        }

        public decimal AdditionalLayersWaitTimeBeforeCure
        {
            get => _additionalLayersWaitTimeBeforeCure;
            set => RaiseAndSetIfChanged(ref _additionalLayersWaitTimeBeforeCure, value);
        }

        public bool OverrideRaft
        {
            get => _overrideRaft;
            set => RaiseAndSetIfChanged(ref _overrideRaft, value);
        }

        public int FlatLayers
        {
            get => _flatLayers;
            set => RaiseAndSetIfChanged(ref _flatLayers, value);
        }

        public int LabelLayers
        {
            get => _labelLayers;
            set => RaiseAndSetIfChanged(ref _labelLayers, value);
        }

        #endregion

        #region Constructor

        public OperationCustomExposureTest() { }

        public OperationCustomExposureTest(FileFormat slicerFile) : base(slicerFile)
        {
            if (SlicerFile.SupportPerLayerSettings)
            {
                _differentSettingsForAdditionalLayers = true;
                if (SlicerFile.SupportsGCode)
                {
                    _additionalLayersLiftHeight = 0;
                    _additionalLayersWaitTimeBeforeCure = 2;
                }
                else
                {
                    _additionalLayersLiftHeight = 0.1m;
                    _additionalLayersWaitTimeBeforeCure = 0;
                }
            }
        }

        public override void InitWithSlicerFile()
        {
            base.InitWithSlicerFile();
            ROI = SlicerFile.BoundingRectangle;
            if (_firstBottomExposure <= 0) _firstBottomExposure = (decimal)SlicerFile.BottomExposureTime;
            if (_firstNormalExposure <= 0) _firstNormalExposure = (decimal)SlicerFile.ExposureTime;
        }

        #endregion

        #region Equality

        protected bool Equals(OperationCustomExposureTest other)
        {
            return _firstBottomExposure == other._firstBottomExposure && _firstNormalExposure == other._firstNormalExposure &&
                   _numberOfCopies == other._numberOfCopies && _exposureStep == other._exposureStep && _margin == other._margin &&
                   _differentSettingsForAdditionalLayers == other._differentSettingsForAdditionalLayers &&
                   _additionalLayersLiftHeightEnabled == other._additionalLayersLiftHeightEnabled && _additionalLayersLiftHeight == other._additionalLayersLiftHeight &&
                   _additionalLayersWaitTimeBeforeCureEnabled == other._additionalLayersWaitTimeBeforeCureEnabled && _additionalLayersWaitTimeBeforeCure == other._additionalLayersWaitTimeBeforeCure &&
                   _overrideRaft == other._overrideRaft && _flatLayers == other._flatLayers && _labelLayers == other._labelLayers &&
                   _font == other._font && _fontScale == other._fontScale;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OperationDoubleExposure)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(_firstBottomExposure);
            hashCode.Add(_firstNormalExposure);
            hashCode.Add(_numberOfCopies);
            hashCode.Add(_exposureStep);
            hashCode.Add(_margin);
            hashCode.Add(_differentSettingsForAdditionalLayers);
            hashCode.Add(_additionalLayersLiftHeightEnabled);
            hashCode.Add(_additionalLayersLiftHeight);
            hashCode.Add(_additionalLayersWaitTimeBeforeCureEnabled);
            hashCode.Add(_additionalLayersWaitTimeBeforeCure);
            hashCode.Add(_overrideRaft);
            hashCode.Add(_flatLayers);
            hashCode.Add(_labelLayers);
            hashCode.Add(_font);
            hashCode.Add(_fontScale);
            return hashCode.ToHashCode();
        }

        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            // What Layer Index do we change from flat to test layers?
            uint bottomIndexEnd = _flatLayers > 0 ? (uint)_flatLayers : (SlicerFile.FirstNormalLayer?.Index ?? 0);

            // How many "flat" layers (single exposure per Z) and "test" layers (multiple exposures per Z) do we have?
            var flatLayerCount = Math.Clamp(bottomIndexEnd - LayerIndexStart, 0, LayerRangeCount);
            var testLayerCount = LayerRangeCount - flatLayerCount;

            var layers = new Layer[SlicerFile.LayerCount+testLayerCount*_numberOfCopies];

            int baseOffset = -(ROI.Width+_margin)*_numberOfCopies/2;

            // Untouched
            for (uint i = 0; i < LayerIndexStart; i++)
            {
                layers[i] = SlicerFile[i];
            }

            Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.ParallelOptions, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;

                var firstLayer = SlicerFile[layerIndex];

                uint flatIndex = (uint)layerIndex - LayerIndexStart;
                uint testIndex = 0;
                bool isFlatLayer = true;

                if (flatIndex >= flatLayerCount) {
                    testIndex = flatIndex - flatLayerCount;
                    flatIndex = flatLayerCount;
                    isFlatLayer = false;
                }

                uint baseLayerIndex = LayerIndexStart + flatIndex + testIndex * ((uint)_numberOfCopies + 1); 

                if (isFlatLayer) {
                    firstLayer.ExposureTime = firstLayer.IsBottomLayer ? (float)_firstBottomExposure : (float)_firstNormalExposure;

                    using (var mat = firstLayer.LayerMat) 
                    using (var srcRoi = new Mat(mat, ROI))
                    using (var orgRoi = srcRoi.Clone())
                    {
                        srcRoi.SetTo(new MCvScalar(0));

                        for (var objectIdx = 0; objectIdx < _numberOfCopies + 1 ; objectIdx++) {
                            var DstRoi = new Rectangle(ROI.Location, ROI.Size);
                            DstRoi.X += baseOffset + (DstRoi.Width + _margin)*(objectIdx);

                            using (var dstRoi = new Mat(mat, DstRoi))
                            if (_overrideRaft) dstRoi.SetTo(new MCvScalar(255));
                            else orgRoi.CopyTo(dstRoi);
                        }

                        firstLayer.LayerMat = mat;
                    }

                    layers[baseLayerIndex] = firstLayer;
                }
                else {
                    Parallel.For(0, _numberOfCopies + 1, CoreSettings.ParallelOptions, objIndex =>
                    {
                        if (progress.Token.IsCancellationRequested) return;

                        var layer = firstLayer.Clone();
                        layer.ExposureTime = (float)(_firstNormalExposure + _exposureStep * objIndex);

                        if (objIndex != 0 && _differentSettingsForAdditionalLayers)
                        {
                            if (_additionalLayersLiftHeightEnabled) layer.LiftHeightTotal = (float)_additionalLayersLiftHeight;
                            if (_additionalLayersWaitTimeBeforeCureEnabled) layer.SetWaitTimeBeforeCureOrLightOffDelay((float)_additionalLayersWaitTimeBeforeCure);
                        }

                        using (var mat = layer.LayerMat)
                        {
                            var DstRoi = new Rectangle(ROI.Location, ROI.Size);
                            DstRoi.X += baseOffset + (DstRoi.Width + _margin) * objIndex;

                            using var srcRoi = new Mat(mat, ROI);
                            using var tgtRoi = srcRoi.Clone();
                            using var dstRoi = new Mat(mat, DstRoi);

                            System.Console.WriteLine($"Layer {baseLayerIndex} Destination {DstRoi}");

                            srcRoi.SetTo(new MCvScalar(0));
                            tgtRoi.CopyTo(dstRoi);

                            if (testIndex < _labelLayers) {
                                var label = $"{layer.ExposureTime}s";
                                int labelBaseline = 0;
                                var labelSize = CvInvoke.GetTextSize(label, _font, _fontScale, 3, ref labelBaseline);
                                labelSize.Height += labelBaseline;

                                using var txtMat = new Mat(labelSize, DepthType.Cv8U, 1);
                                txtMat.SetTo(new MCvScalar(0));

                                CvInvoke.PutText(
                                    txtMat, 
                                    label, 
                                    new Point(0, labelSize.Height-labelBaseline),
                                    _font, _fontScale, 
                                    new MCvScalar(255), 
                                    3, 
                                    LineType.EightConnected, 
                                    false
                                );

                                var TxtRoi = new Rectangle(DstRoi.Location, labelSize);
                                TxtRoi.X += (DstRoi.Width - labelSize.Width)/2;
                                TxtRoi.Y += DstRoi.Height - labelSize.Height - 20;

                                using var txtDstMat = new Mat(mat, TxtRoi);

                                CvInvoke.Flip(txtMat, txtDstMat, FlipType.Horizontal);
                                //txtMat.CopyTo(txtDstMat);
                            }

                            layer.LayerMat = mat;
                        }

                        uint index = baseLayerIndex + (uint)objIndex;
                        layers[index] = layer;
                    });
                }

                progress.LockAndIncrement();
            });

            // Untouched
            for (uint i = LayerIndexEnd+1; i < SlicerFile.LayerCount; i++)
            {
                layers[i + LayerRangeCount] = SlicerFile[i];
            }

            SlicerFile.SuppressRebuildPropertiesWork(() =>
            {
                SlicerFile.LayerManager.Layers = layers;
            });

            // Middle-Center everything
            // new OperationMove(SlicerFile).Execute(progress);                

            return !progress.Token.IsCancellationRequested;
        }

        #endregion
    }
}
