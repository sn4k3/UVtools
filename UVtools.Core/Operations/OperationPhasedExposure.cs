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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Objects;
using ZLinq;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
public partial class OperationPhasedExposure : Operation, IEquatable<OperationPhasedExposure>
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region SubClasses

    public sealed partial class PhasedExposure : ObservableObject
    {
        private decimal _bottomExposureTime;
        private decimal _exposureTime;

        public decimal BottomExposureTime
        {
            get => _bottomExposureTime;
            set => SetProperty(ref _bottomExposureTime, Math.Round(Math.Clamp(value, 0, 1000), 2));
        }

        [ObservableProperty]
        public partial ushort BottomIterations { get; set; }

        public decimal ExposureTime
        {
            get => _exposureTime;
            set => SetProperty(ref _exposureTime, Math.Round(Math.Clamp(value, 0, 1000), 2));
        }

        [ObservableProperty]
        public partial ushort Iterations { get; set; }

        public PhasedExposure()
        {
        }

        public override string ToString()
        {
            return $"{nameof(BottomExposureTime)}: {BottomExposureTime}s, {nameof(BottomIterations)}: {BottomIterations}px, {nameof(ExposureTime)}: {ExposureTime}s, {nameof(Iterations)}: {Iterations}px";
        }

        public PhasedExposure(decimal bottomExposureTime, ushort bottomIterations, decimal exposureTime, ushort iterations)
        {
            _bottomExposureTime = bottomExposureTime;
            BottomIterations = bottomIterations;
            _exposureTime = exposureTime;
            Iterations = iterations;
        }



        public PhasedExposure Clone()
        {
            return (PhasedExposure)MemberwiseClone();
        }
    }

    #endregion

    #region Members


    #endregion

    #region Overrides
    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.Bottom;
    public override bool CanROI => false;
    public override bool CanMask => false;

    public override string IconClass => "SunClock ";
    public override string Title => "Phased exposure";
    public override string Description =>
        "The phased exposure method clones the selected layer range and print the same layer with different exposure times and strategies.\n" +
        "Can be used to eliminate the elephant foot effect or to harden a layer in multiple steps.\n" +
        "After this, do not apply any modification which reconstruct the z positions of the layers.\n" +
        "Note: To eliminate the elephant foot effect, the use of wall dimming method is recommended.";

    public override string ConfirmationText =>
        $"phased exposure model layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressTitle =>
        $"Phased exposure from layers {LayerIndexStart} to {LayerIndexEnd}";

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

        if (Count < 2)
        {
            sb.AppendLine("This tool requires at least two phased exposures in order to run.");
        }

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

        for (var i = 0; i < PhasedExposures.Count-1; i++)
        {
            if (PhasedExposures[i].BottomExposureTime == PhasedExposures[i + 1].ExposureTime &&
                PhasedExposures[i].BottomIterations == PhasedExposures[i + 1].BottomIterations &&
                PhasedExposures[i].ExposureTime == PhasedExposures[i + 1].ExposureTime &&
                PhasedExposures[i].Iterations == PhasedExposures[i + 1].Iterations)
            {
                sb.AppendLine($"Duplicated exposure sequence #{i+1} == #{i+2}. Group of entries can't be duplicated.");
            }
        }


        return sb.ToString();
    }

    public IEnumerator<PhasedExposure> GetEnumerator()
    {
        return PhasedExposures.GetEnumerator();
    }

    public override string ToString()
    {
        var result = $"[Phases: {Count}] " +
                     $"[Diff: {ExposureDifferenceOnly} Overlap: {ExposureDifferenceOnlyOverlapIterations}px]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    public int Count => PhasedExposures.Count;

    public PhasedExposure this[int index] => PhasedExposures[index];

    public override Operation Clone()
    {
        var clone = (OperationPhasedExposure)base.Clone();
        clone.PhasedExposures = PhasedExposures.CloneByXmlSerialization();
        return clone;
    }

    #endregion

    #region Properties

    [ObservableProperty]
    public partial RangeObservableCollection<PhasedExposure> PhasedExposures { get; set; } = [];

    [ObservableProperty]
    public partial bool ExposureDifferenceOnly { get; set; } = true;

    [ObservableProperty]
    public partial ushort ExposureDifferenceOnlyOverlapIterations { get; set; } = 10;

    [ObservableProperty]
    public partial bool DifferentSettingsForSequentialLayers { get; set; }

    [ObservableProperty]
    public partial bool SequentialLiftHeightEnabled { get; set; } = true;

    [ObservableProperty]
    public partial decimal SequentialLiftHeight { get; set; }

    [ObservableProperty]
    public partial bool SequentialWaitTimeBeforeCureEnabled { get; set; } = true;

    [ObservableProperty]
    public partial decimal SequentialWaitTimeBeforeCure { get; set; }


    public KernelConfiguration Kernel { get; set; } = new();

    #endregion

    #region Constructor

    public OperationPhasedExposure() { }

    public OperationPhasedExposure(FileFormat slicerFile) : base(slicerFile)
    {
        if (SlicerFile.SupportPerLayerSettings)
        {
            DifferentSettingsForSequentialLayers = true;
            if (SlicerFile.SupportGCode)
            {
                SequentialLiftHeight = 0;
                SequentialWaitTimeBeforeCure = 2;
            }
            else
            {
                SequentialLiftHeight = 0.1m;
                SequentialWaitTimeBeforeCure = 0;
            }
        }
    }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();
        if (Count == 0)
        {
            PhasedExposures.AddRange([
                new PhasedExposure((decimal)SlicerFile.BottomExposureTime, 10, (decimal)SlicerFile.ExposureTime, 4),
                new PhasedExposure((decimal)SlicerFile.ExposureTime, 0, (decimal)SlicerFile.ExposureTime, 0)
            ]);
        }
    }

    #endregion

    #region Equality

    public bool Equals(OperationPhasedExposure? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return PhasedExposures.Equals(other.PhasedExposures) && ExposureDifferenceOnly == other.ExposureDifferenceOnly && ExposureDifferenceOnlyOverlapIterations == other.ExposureDifferenceOnlyOverlapIterations && DifferentSettingsForSequentialLayers == other.DifferentSettingsForSequentialLayers && SequentialLiftHeightEnabled == other.SequentialLiftHeightEnabled && SequentialLiftHeight == other.SequentialLiftHeight && SequentialWaitTimeBeforeCureEnabled == other.SequentialWaitTimeBeforeCureEnabled && SequentialWaitTimeBeforeCure == other.SequentialWaitTimeBeforeCure;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OperationPhasedExposure)obj);
    }

    public static bool operator ==(OperationPhasedExposure? left, OperationPhasedExposure? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(OperationPhasedExposure? left, OperationPhasedExposure? right)
    {
        return !Equals(left, right);
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        var layers = new Layer?[SlicerFile.LayerCount + LayerRangeCount * (PhasedExposures.Count - 1)];

        // Untouched
        for (uint i = 0; i < LayerIndexStart; i++)
        {
            layers[i] = SlicerFile[i];
        }

        int bottomLayers = SlicerFile.BottomLayerCount;

        Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            var layer = SlicerFile[layerIndex];

            if (layer.IsEmpty)
            {
                progress.LockAndIncrement();
                return;
            }

            var isBottomLayer = layer.IsBottomLayer;

            uint newLayerIndex = (uint)(LayerIndexStart + (layerIndex - LayerIndexStart) * PhasedExposures.Count);

            if (isBottomLayer) Interlocked.Increment(ref bottomLayers);

            using var matRoi = layer.LayerMatBoundingRectangle;
            uint affectedLayers = 0;

            var lastPhasedExposure = PhasedExposures[0];
            foreach (var phasedExposure in PhasedExposures)
            {
                int iterations = isBottomLayer ? phasedExposure.BottomIterations : phasedExposure.Iterations;
                bool setNewMat = false;
                using var newMatRoi = matRoi.Clone();

                if (iterations > 0)
                {
                    int tempIterations = iterations;
                    var kernel = Kernel.GetKernel(ref tempIterations);
                    CvInvoke.Erode(newMatRoi.RoiMat, newMatRoi.RoiMat, kernel, EmguCvExtensions.AnchorCenter, tempIterations, BorderType.Reflect101, default);
                    if (!CvInvoke.HasNonZero(newMatRoi.RoiMat)) continue; // Produce all black layer, ignoring
                    setNewMat = true;
                }

                if (affectedLayers > 0 && ExposureDifferenceOnly)
                {
                    var overlapIterations = ExposureDifferenceOnlyOverlapIterations + (isBottomLayer ? lastPhasedExposure.BottomIterations : lastPhasedExposure.Iterations);
                    if (overlapIterations > iterations)
                    {
                        using var overlapMat = new Mat();
                        int tempIterations = overlapIterations;
                        var kernel = Kernel.GetKernel(ref tempIterations);
                        CvInvoke.Erode(matRoi.RoiMat, overlapMat, kernel, EmguCvExtensions.AnchorCenter, tempIterations, BorderType.Reflect101, default);
                        if (CvInvoke.HasNonZero(overlapMat))
                        {
                            CvInvoke.Subtract(newMatRoi.RoiMat, overlapMat, newMatRoi.RoiMat);
                            setNewMat = CvInvoke.HasNonZero(newMatRoi.RoiMat);
                        }
                    }
                }

                var newLayer = layer.Clone();
                newLayer.ExposureTime = (float)(isBottomLayer ? phasedExposure.BottomExposureTime : phasedExposure.ExposureTime);

                if (DifferentSettingsForSequentialLayers && affectedLayers > 0)
                {
                    if (SequentialLiftHeightEnabled) newLayer.LiftHeightTotal = (float)SequentialLiftHeight;
                    if (SequentialWaitTimeBeforeCureEnabled) newLayer.SetWaitTimeBeforeCureOrLightOffDelay((float)SequentialWaitTimeBeforeCure);
                }

                layers[newLayerIndex] = newLayer;
                if (setNewMat) layers[newLayerIndex]!.LayerMat = newMatRoi.SourceMat;

                affectedLayers++;
                newLayerIndex++;
                lastPhasedExposure = phasedExposure;
            }

            // Prevent layer loss due erode settings on low pixel layer, keep the original instead
            if (affectedLayers == 0) layers[newLayerIndex] = layer;


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
            SlicerFile.Layers = layers.AsValueEnumerable().Where(layer => layer is not null).ToArray()!;
        });

        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}
