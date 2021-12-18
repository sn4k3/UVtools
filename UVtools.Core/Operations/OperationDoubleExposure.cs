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
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationDoubleExposure : Operation
    {
        #region Members
        private decimal _firstBottomExposure;
        private decimal _firstNormalExposure;
        private decimal _secondBottomExposure;
        private decimal _secondNormalExposure;
        private byte _firstBottomErodeIterations = 4;
        private byte _secondBottomErodeIterations;
        private byte _firstNormalErodeIterations = 1;
        private byte _secondNormalErodeIterations;
        private bool _secondLayerDifference = true;
        private byte _secondLayerDifferenceOverlapErodeIterations = 10;
        private bool _differentSettingsForSecondLayer;
        private bool _secondLayerLiftHeightEnabled = true;
        private decimal _secondLayerLiftHeight;
        private bool _secondLayerWaitTimeBeforeCureEnabled = true;
        private decimal _secondLayerWaitTimeBeforeCure;

        #endregion

        #region Overrides
        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.Bottom;
        public override string Title => "Double exposure";
        public override string Description =>
            "The double exposure method clones the selected layer range and print the same layer twice with different exposure times and strategies.\n" +
            "Can be used to eliminate the elephant foot effect or to harden a layer in two steps.\n" +
            "After this, do not apply any modification which reconstruct the z positions of the layers.\n" +
            "Note: To eliminate the elephant foot effect, the use of wall dimming method is recommended.";

        public override string ConfirmationText =>
            $"double exposure model layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressTitle =>
            $"Double exposure from layers {LayerIndexStart} to {LayerIndexEnd}";

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
            var result = $"[1º exp: {_firstBottomExposure}/{_firstNormalExposure}s erode: {_firstBottomErodeIterations}/{_firstNormalErodeIterations}px] " +
                         $"[2º exp: {_secondBottomExposure}/{_secondNormalExposure}s erode: {_secondBottomErodeIterations}/{_secondNormalErodeIterations}px] " +
                         $"[Diff: {_secondLayerDifference} Overlap: {_secondLayerDifferenceOverlapErodeIterations}px]" + LayerRangeString;
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

        public decimal SecondBottomExposure
        {
            get => _secondBottomExposure;
            set => RaiseAndSetIfChanged(ref _secondBottomExposure, Math.Round(value, 2));
        }

        public decimal SecondNormalExposure
        {
            get => _secondNormalExposure;
            set => RaiseAndSetIfChanged(ref _secondNormalExposure, Math.Round(value, 2));
        }

        public byte FirstBottomErodeIterations
        {
            get => _firstBottomErodeIterations;
            set => RaiseAndSetIfChanged(ref _firstBottomErodeIterations, value);
        }

        public byte SecondBottomErodeIterations
        {
            get => _secondBottomErodeIterations;
            set => RaiseAndSetIfChanged(ref _secondBottomErodeIterations, value);
        }

        public byte FirstNormalErodeIterations
        {
            get => _firstNormalErodeIterations;
            set => RaiseAndSetIfChanged(ref _firstNormalErodeIterations, value);
        }

        public byte SecondNormalErodeIterations
        {
            get => _secondNormalErodeIterations;
            set => RaiseAndSetIfChanged(ref _secondNormalErodeIterations, value);
        }

        public bool SecondLayerDifference
        {
            get => _secondLayerDifference;
            set => RaiseAndSetIfChanged(ref _secondLayerDifference, value);
        }

        public byte SecondLayerDifferenceOverlapErodeIterations
        {
            get => _secondLayerDifferenceOverlapErodeIterations;
            set => RaiseAndSetIfChanged(ref _secondLayerDifferenceOverlapErodeIterations, value);
        }

        public bool DifferentSettingsForSecondLayer
        {
            get => _differentSettingsForSecondLayer;
            set => RaiseAndSetIfChanged(ref _differentSettingsForSecondLayer, value);
        }

        public bool SecondLayerLiftHeightEnabled
        {
            get => _secondLayerLiftHeightEnabled;
            set => RaiseAndSetIfChanged(ref _secondLayerLiftHeightEnabled, value);
        }

        public decimal SecondLayerLiftHeight
        {
            get => _secondLayerLiftHeight;
            set => RaiseAndSetIfChanged(ref _secondLayerLiftHeight, value);
        }

        public bool SecondLayerWaitTimeBeforeCureEnabled
        {
            get => _secondLayerWaitTimeBeforeCureEnabled;
            set => RaiseAndSetIfChanged(ref _secondLayerWaitTimeBeforeCureEnabled, value);
        }

        public decimal SecondLayerWaitTimeBeforeCure
        {
            get => _secondLayerWaitTimeBeforeCure;
            set => RaiseAndSetIfChanged(ref _secondLayerWaitTimeBeforeCure, value);
        }


        public KernelConfiguration Kernel { get; set; } = new();

        #endregion

        #region Constructor

        public OperationDoubleExposure() { }

        public OperationDoubleExposure(FileFormat slicerFile) : base(slicerFile)
        {
            if (SlicerFile.SupportPerLayerSettings)
            {
                _differentSettingsForSecondLayer = true;
                if (SlicerFile.SupportsGCode)
                {
                    _secondLayerLiftHeight = 0;
                    _secondLayerWaitTimeBeforeCure = 2;
                }
                else
                {
                    _secondLayerLiftHeight = 0.1m;
                    _secondLayerWaitTimeBeforeCure = 0;
                }
            }
        }

        public override void InitWithSlicerFile()
        {
            base.InitWithSlicerFile();
            if (_firstBottomExposure <= 0) _firstBottomExposure = (decimal)SlicerFile.BottomExposureTime;
            if (_firstNormalExposure <= 0) _firstNormalExposure = (decimal)SlicerFile.ExposureTime;
            if (_secondBottomExposure <= 0) _secondBottomExposure = (decimal)SlicerFile.ExposureTime;
            if (_secondNormalExposure <= 0) _secondNormalExposure = (decimal)SlicerFile.ExposureTime;
        }

        #endregion

        #region Equality

        protected bool Equals(OperationDoubleExposure other)
        {
            return _firstBottomExposure == other._firstBottomExposure && _firstNormalExposure == other._firstNormalExposure && _secondBottomExposure == other._secondBottomExposure && _secondNormalExposure == other._secondNormalExposure && _firstBottomErodeIterations == other._firstBottomErodeIterations && _secondBottomErodeIterations == other._secondBottomErodeIterations && _firstNormalErodeIterations == other._firstNormalErodeIterations && _secondNormalErodeIterations == other._secondNormalErodeIterations && _secondLayerDifference == other._secondLayerDifference && _secondLayerDifferenceOverlapErodeIterations == other._secondLayerDifferenceOverlapErodeIterations && _differentSettingsForSecondLayer == other._differentSettingsForSecondLayer && _secondLayerLiftHeightEnabled == other._secondLayerLiftHeightEnabled && _secondLayerLiftHeight == other._secondLayerLiftHeight && _secondLayerWaitTimeBeforeCureEnabled == other._secondLayerWaitTimeBeforeCureEnabled && _secondLayerWaitTimeBeforeCure == other._secondLayerWaitTimeBeforeCure;
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
            hashCode.Add(_secondBottomExposure);
            hashCode.Add(_secondNormalExposure);
            hashCode.Add(_firstBottomErodeIterations);
            hashCode.Add(_secondBottomErodeIterations);
            hashCode.Add(_firstNormalErodeIterations);
            hashCode.Add(_secondNormalErodeIterations);
            hashCode.Add(_secondLayerDifference);
            hashCode.Add(_secondLayerDifferenceOverlapErodeIterations);
            hashCode.Add(_differentSettingsForSecondLayer);
            hashCode.Add(_secondLayerLiftHeightEnabled);
            hashCode.Add(_secondLayerLiftHeight);
            hashCode.Add(_secondLayerWaitTimeBeforeCureEnabled);
            hashCode.Add(_secondLayerWaitTimeBeforeCure);
            return hashCode.ToHashCode();
        }

        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            var anchor = new Point(-1, -1);
            
            var layers = new Layer[SlicerFile.LayerCount+LayerRangeCount];

            // Untouched
            for (uint i = 0; i < LayerIndexStart; i++)
            {
                layers[i] = SlicerFile[i];
            }

            Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.ParallelOptions, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;

                var firstLayer = SlicerFile[layerIndex];
                var secondLayer = firstLayer.Clone();
                var isBottomLayer = firstLayer.IsBottomLayer;

                firstLayer.ExposureTime = (float)( isBottomLayer ? _firstBottomExposure : _firstNormalExposure);
                secondLayer.ExposureTime = (float)(isBottomLayer ? _secondBottomExposure : _secondNormalExposure);

                if (_differentSettingsForSecondLayer)
                {
                    if (_secondLayerLiftHeightEnabled) secondLayer.LiftHeightTotal = (float)_secondLayerLiftHeight;
                    if (_secondLayerWaitTimeBeforeCureEnabled) secondLayer.SetWaitTimeBeforeCureOrLightOffDelay((float)_secondLayerWaitTimeBeforeCure);
                }

                byte firstErodeIterations = isBottomLayer ? _firstBottomErodeIterations : _firstNormalErodeIterations;
                byte secondErodeIterations = isBottomLayer ? _secondBottomErodeIterations : _secondNormalErodeIterations;

                using (var mat = firstLayer.LayerMat)
                {
                    //using Mat matOriginal = _secondExposureLayerDifference ? mat.Clone() : null;
                    if (firstErodeIterations > 0 && firstErodeIterations == secondErodeIterations)
                    {
                        int tempIterations = firstErodeIterations;
                        var kernel = Kernel.GetKernel(ref tempIterations);
                        CvInvoke.Erode(mat, mat, kernel, anchor, tempIterations, BorderType.Reflect101, default);
                        firstLayer.LayerMat = mat;
                        firstLayer.CopyImageTo(secondLayer);

                        if (_secondLayerDifference && _secondLayerDifferenceOverlapErodeIterations > 0)
                        {
                            tempIterations = _secondLayerDifferenceOverlapErodeIterations;
                            kernel = Kernel.GetKernel(ref tempIterations);
                            using var matErode = new Mat();
                            CvInvoke.Erode(mat, matErode, kernel, anchor, tempIterations, BorderType.Reflect101, default);
                            //CvInvoke.Threshold(matErode, matErode, 127, 255, ThresholdType.Binary);
                            CvInvoke.Subtract(mat, matErode, mat);
                            secondLayer.LayerMat = mat;
                        }
                        else
                        {
                            firstLayer.CopyImageTo(secondLayer);
                        }
                    }
                    else
                    {
                        Mat firstMat = null;
                        Mat secondMat = null;
                        if (firstErodeIterations > 0)
                        {
                            int tempIterations = firstErodeIterations;
                            var kernel = Kernel.GetKernel(ref tempIterations);
                            firstMat = new Mat();
                            CvInvoke.Erode(mat, firstMat, kernel, anchor, tempIterations, BorderType.Reflect101, default);
                            firstLayer.LayerMat = firstMat;
                        }

                        if (secondErodeIterations > 0)
                        {
                            int tempIterations = secondErodeIterations;
                            var kernel = Kernel.GetKernel(ref tempIterations);
                            secondMat = new Mat();
                            CvInvoke.Erode(mat, secondMat, kernel, anchor, tempIterations, BorderType.Reflect101, default);
                        }

                        if(firstMat is not null && _secondLayerDifference)
                        {
                            if (firstErodeIterations + _secondLayerDifferenceOverlapErodeIterations != secondErodeIterations)
                            {
                                if (_secondLayerDifferenceOverlapErodeIterations > 0 &&
                                    firstErodeIterations + _secondLayerDifferenceOverlapErodeIterations != secondErodeIterations)
                                {
                                    int tempIterations = _secondLayerDifferenceOverlapErodeIterations;
                                    var kernel = Kernel.GetKernel(ref tempIterations);
                                    CvInvoke.Erode(firstMat, firstMat, kernel, anchor, tempIterations, BorderType.Reflect101, default);
                                    //CvInvoke.Threshold(firstMat, firstMat, 127, 255, ThresholdType.Binary);
                                }

                                CvInvoke.AbsDiff(firstMat, secondMat ?? mat, mat);
                                secondLayer.LayerMat = mat;
                            }
                        }
                        else if (secondMat is not null)
                        {
                            secondLayer.LayerMat = secondMat;
                        }

                        firstMat?.Dispose();
                        secondMat?.Dispose();
                    }
                }

                uint index = LayerIndexStart + (uint)(layerIndex - LayerIndexStart) * 2; 
                
                layers[index] = firstLayer;
                layers[index + 1] = secondLayer;

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

            return !progress.Token.IsCancellationRequested;
        }

        #endregion
    }
}
