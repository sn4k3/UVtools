/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV.CvEnum;
using EmguExtensions;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public partial class OperationDoubleExposure : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Members
    private decimal _firstBottomExposure;
    private decimal _firstNormalExposure;
    private decimal _secondBottomExposure;
    private decimal _secondNormalExposure;

    #endregion

    #region Overrides
    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.Bottom;
    public override string IconClass => "LightbulbGroup";
    public override string Title => "Double exposure";
    public override string Description =>
        "The double exposure method clones the selected layer range and print the same layer twice with different exposure times and strategies.\n" +
        "Can be used to eliminate the elephant foot effect or to harden a layer in two steps.\n" +
        "After this, do not apply any modification which reconstruct the z positions of the layers.\n" +
        "Note: To eliminate the elephant foot effect, the use of wall dimming method is recommended.";

    public override bool CanROI => false;
    public override bool CanMask => false;

    public override string ConfirmationText =>
        $"double exposure model layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressTitle =>
        $"Double exposure from layers {LayerIndexStart} to {LayerIndexEnd}";

    public override string ProgressAction => "Cloned layers";

    public override string? ValidateSpawn()
    {
        if (!SlicerFile.CanUseSameLayerPositionZ || !SlicerFile.CanUseLayerLiftHeight || !SlicerFile.CanUseLayerExposureTime)
        {
            return NotSupportedMessage;
        }

        return null;
    }

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        //if (LayerRangeHaveBottoms && _firstBottomExposure == _secondBottomExposure && FirstBottomErodeIterations == SecondBottomErodeIterations)
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
        var result = $"[1º exp: {_firstBottomExposure}/{_firstNormalExposure}s erode: {FirstBottomErodeIterations}/{FirstNormalErodeIterations}px] " +
                     $"[2º exp: {_secondBottomExposure}/{_secondNormalExposure}s erode: {SecondBottomErodeIterations}/{SecondNormalErodeIterations}px] " +
                     $"[Diff: {SecondLayerDifference} Overlap: {SecondLayerDifferenceOverlapErodeIterations}px]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Properties

    public decimal FirstBottomExposure
    {
        get => _firstBottomExposure;
        set => SetProperty(ref _firstBottomExposure, Math.Round(value, 2));
    }

    public decimal FirstNormalExposure
    {
        get => _firstNormalExposure;
        set => SetProperty(ref _firstNormalExposure, Math.Round(value, 2));
    }

    public decimal SecondBottomExposure
    {
        get => _secondBottomExposure;
        set => SetProperty(ref _secondBottomExposure, Math.Round(value, 2));
    }

    public decimal SecondNormalExposure
    {
        get => _secondNormalExposure;
        set => SetProperty(ref _secondNormalExposure, Math.Round(value, 2));
    }

    [ObservableProperty]
    public partial byte FirstBottomErodeIterations { get; set; } = 4;

    [ObservableProperty]
    public partial byte SecondBottomErodeIterations { get; set; }

    [ObservableProperty]
    public partial byte FirstNormalErodeIterations { get; set; } = 1;

    [ObservableProperty]
    public partial byte SecondNormalErodeIterations { get; set; }

    [ObservableProperty]
    public partial bool SecondLayerDifference { get; set; } = true;

    [ObservableProperty]
    public partial byte SecondLayerDifferenceOverlapErodeIterations { get; set; } = 10;

    [ObservableProperty]
    public partial bool DifferentSettingsForSecondLayer { get; set; }

    [ObservableProperty]
    public partial bool SecondLayerLiftHeightEnabled { get; set; } = true;

    [ObservableProperty]
    public partial decimal SecondLayerLiftHeight { get; set; }

    [ObservableProperty]
    public partial bool SecondLayerWaitTimeBeforeCureEnabled { get; set; } = true;

    [ObservableProperty]
    public partial decimal SecondLayerWaitTimeBeforeCure { get; set; }


    public KernelConfiguration Kernel { get; set; } = new();

    #endregion

    #region Constructor

    public OperationDoubleExposure() { }

    public OperationDoubleExposure(FileFormat slicerFile) : base(slicerFile)
    {
        if (SlicerFile.SupportPerLayerSettings)
        {
            DifferentSettingsForSecondLayer = true;
            if (SlicerFile.SupportGCode)
            {
                SecondLayerLiftHeight = 0;
                SecondLayerWaitTimeBeforeCure = 2;
            }
            else
            {
                SecondLayerLiftHeight = 0.1m;
                SecondLayerWaitTimeBeforeCure = 0;
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
        return _firstBottomExposure == other._firstBottomExposure && _firstNormalExposure == other._firstNormalExposure && _secondBottomExposure == other._secondBottomExposure && _secondNormalExposure == other._secondNormalExposure && FirstBottomErodeIterations == other.FirstBottomErodeIterations && SecondBottomErodeIterations == other.SecondBottomErodeIterations && FirstNormalErodeIterations == other.FirstNormalErodeIterations && SecondNormalErodeIterations == other.SecondNormalErodeIterations && SecondLayerDifference == other.SecondLayerDifference && SecondLayerDifferenceOverlapErodeIterations == other.SecondLayerDifferenceOverlapErodeIterations && DifferentSettingsForSecondLayer == other.DifferentSettingsForSecondLayer && SecondLayerLiftHeightEnabled == other.SecondLayerLiftHeightEnabled && SecondLayerLiftHeight == other.SecondLayerLiftHeight && SecondLayerWaitTimeBeforeCureEnabled == other.SecondLayerWaitTimeBeforeCureEnabled && SecondLayerWaitTimeBeforeCure == other.SecondLayerWaitTimeBeforeCure;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OperationDoubleExposure)obj);
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        var layers = new Layer[SlicerFile.LayerCount+LayerRangeCount];

        // Untouched
        for (uint i = 0; i < LayerIndexStart; i++)
        {
            layers[i] = SlicerFile[i];
        }

        int bottomLayers = SlicerFile.BottomLayerCount;

        Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            var firstLayer = SlicerFile[layerIndex];
            var secondLayer = firstLayer.Clone();
            var isBottomLayer = firstLayer.IsBottomLayer;

            if (isBottomLayer)
            {
                Interlocked.Increment(ref bottomLayers);
            }

            firstLayer.ExposureTime = (float)( isBottomLayer ? _firstBottomExposure : _firstNormalExposure);
            secondLayer.ExposureTime = (float)(isBottomLayer ? _secondBottomExposure : _secondNormalExposure);

            if (DifferentSettingsForSecondLayer)
            {
                if (SecondLayerLiftHeightEnabled) secondLayer.LiftHeightTotal = (float)SecondLayerLiftHeight;
                if (SecondLayerWaitTimeBeforeCureEnabled) secondLayer.SetWaitTimeBeforeCureOrLightOffDelay((float)SecondLayerWaitTimeBeforeCure);
            }

            byte firstErodeIterations = isBottomLayer ? FirstBottomErodeIterations : FirstNormalErodeIterations;
            byte secondErodeIterations = isBottomLayer ? SecondBottomErodeIterations : SecondNormalErodeIterations;

            using (var mat = firstLayer.LayerMat)
            {
                //using Mat matOriginal = _secondExposureLayerDifference ? mat.Clone() : null;
                if (firstErodeIterations > 0 && firstErodeIterations == secondErodeIterations)
                {
                    int tempIterations = firstErodeIterations;
                    var kernel = Kernel.GetKernel(ref tempIterations);
                    CvInvoke.Erode(mat, mat, kernel, EmguCvExtensions.AnchorCenter, tempIterations, BorderType.Reflect101, default);
                    firstLayer.LayerMat = mat;
                    firstLayer.CopyImageTo(secondLayer);

                    if (SecondLayerDifference && SecondLayerDifferenceOverlapErodeIterations > 0)
                    {
                        tempIterations = SecondLayerDifferenceOverlapErodeIterations;
                        kernel = Kernel.GetKernel(ref tempIterations);
                        using var matErode = new Mat();
                        CvInvoke.Erode(mat, matErode, kernel, EmguCvExtensions.AnchorCenter, tempIterations, BorderType.Reflect101, default);
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
                    Mat? firstMat = null;
                    Mat? secondMat = null;
                    if (firstErodeIterations > 0)
                    {
                        int tempIterations = firstErodeIterations;
                        var kernel = Kernel.GetKernel(ref tempIterations);
                        firstMat = new Mat();
                        CvInvoke.Erode(mat, firstMat, kernel, EmguCvExtensions.AnchorCenter, tempIterations, BorderType.Reflect101, default);
                        firstLayer.LayerMat = firstMat;
                    }

                    if (secondErodeIterations > 0)
                    {
                        int tempIterations = secondErodeIterations;
                        var kernel = Kernel.GetKernel(ref tempIterations);
                        secondMat = new Mat();
                        CvInvoke.Erode(mat, secondMat, kernel, EmguCvExtensions.AnchorCenter, tempIterations, BorderType.Reflect101, default);
                    }

                    if(firstMat is not null && SecondLayerDifference)
                    {
                        if (firstErodeIterations + SecondLayerDifferenceOverlapErodeIterations != secondErodeIterations)
                        {
                            if (SecondLayerDifferenceOverlapErodeIterations > 0 &&
                                firstErodeIterations + SecondLayerDifferenceOverlapErodeIterations != secondErodeIterations)
                            {
                                int tempIterations = SecondLayerDifferenceOverlapErodeIterations;
                                var kernel = Kernel.GetKernel(ref tempIterations);
                                CvInvoke.Erode(firstMat, firstMat, kernel, EmguCvExtensions.AnchorCenter, tempIterations, BorderType.Reflect101, default);
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
            SlicerFile.BottomLayerCount = (ushort)bottomLayers;
            SlicerFile.Layers = layers;
        });

        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}