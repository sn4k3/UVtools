/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations;


public class OperationPhasedExposure : Operation, IEquatable<OperationPhasedExposure>
{
    #region SubClasses

    public sealed class PhasedExposure : BindableBase
    {
        private decimal _bottomExposureTime;
        private ushort _bottomIterations;
        private decimal _exposureTime;
        private ushort _iterations;

        public decimal BottomExposureTime
        {
            get => _bottomExposureTime;
            set => RaiseAndSetIfChanged(ref _bottomExposureTime, Math.Round(Math.Clamp(value, 0, 1000), 2));
        }

        public ushort BottomIterations
        {
            get => _bottomIterations;
            set => RaiseAndSetIfChanged(ref _bottomIterations, value);
        }

        public decimal ExposureTime
        {
            get => _exposureTime;
            set => RaiseAndSetIfChanged(ref _exposureTime, Math.Round(Math.Clamp(value, 0, 1000), 2));
        }

       public ushort Iterations
        {
            get => _iterations;
            set => RaiseAndSetIfChanged(ref _iterations, value);
        }

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
            _bottomIterations = bottomIterations;
            _exposureTime = exposureTime;
            _iterations = iterations;
        }

        

        public PhasedExposure Clone()
        {
            return (PhasedExposure)MemberwiseClone();
        }
    }

    #endregion

    #region Members

    private RangeObservableCollection<PhasedExposure> _phasedExposures = new();
    private bool _exposureDifferenceOnly = true;
    private ushort _exposureDifferenceOnlyOverlapIterations = 10;
    private bool _differentSettingsForSequentialLayers;
    private bool _sequentialLiftHeightEnabled = true;
    private decimal _sequentialLiftHeight;
    private bool _sequentialWaitTimeBeforeCureEnabled = true;
    private decimal _sequentialWaitTimeBeforeCure;

    #endregion

    #region Overrides
    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.Bottom;
    public override string IconClass => "fa-solid fa-bars-staggered";
    public override string Title => "Phased exposure";
    public override string Description =>
        "The phased exposure method clones the selected layer range and print the same layer with different exposure times and strategies.\n" +
        "Can be used to eliminate the elephant foot effect or to harden a layer in multiple steps.\n" +
        "After this, do not apply any modification which reconstruct the z positions of the layers.\n" +
        "Note: To eliminate the elephant foot effect, the use of wall dimming method is recommended.";

    public override bool CanROI => false;
    public override bool CanMask => false;

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

        for (var i = 0; i < _phasedExposures.Count-1; i++)
        {
            if (_phasedExposures[i].BottomExposureTime == _phasedExposures[i + 1].ExposureTime &&
                _phasedExposures[i].BottomIterations == _phasedExposures[i + 1].BottomIterations &&
                _phasedExposures[i].ExposureTime == _phasedExposures[i + 1].ExposureTime &&
                _phasedExposures[i].Iterations == _phasedExposures[i + 1].Iterations)
            {
                sb.AppendLine($"Duplicated exposure sequence #{i+1} == #{i+2}. Group of entries can't be duplicated.");
            }
        }


        return sb.ToString();
    }

    public IEnumerator<PhasedExposure> GetEnumerator()
    {
        return _phasedExposures.GetEnumerator();
    }

    public override string ToString()
    {
        var result = $"[Phases: {Count}] " +
                     $"[Diff: {_exposureDifferenceOnly} Overlap: {_exposureDifferenceOnlyOverlapIterations}px]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    public int Count => _phasedExposures.Count;

    public PhasedExposure this[int index] => _phasedExposures[index];

    public override Operation Clone()
    {
        var clone = (OperationPhasedExposure)base.Clone();
        clone.PhasedExposures = PhasedExposures.CloneByXmlSerialization();
        return clone;
    }

    #endregion

    #region Properties

    public RangeObservableCollection<PhasedExposure> PhasedExposures
    {
        get => _phasedExposures;
        set => RaiseAndSetIfChanged(ref _phasedExposures, value);
    }

    public bool ExposureDifferenceOnly
    {
        get => _exposureDifferenceOnly;
        set => RaiseAndSetIfChanged(ref _exposureDifferenceOnly, value);
    }

    public ushort ExposureDifferenceOnlyOverlapIterations
    {
        get => _exposureDifferenceOnlyOverlapIterations;
        set => RaiseAndSetIfChanged(ref _exposureDifferenceOnlyOverlapIterations, value);
    }

    public bool DifferentSettingsForSequentialLayers
    {
        get => _differentSettingsForSequentialLayers;
        set => RaiseAndSetIfChanged(ref _differentSettingsForSequentialLayers, value);
    }

    public bool SequentialLiftHeightEnabled
    {
        get => _sequentialLiftHeightEnabled;
        set => RaiseAndSetIfChanged(ref _sequentialLiftHeightEnabled, value);
    }

    public decimal SequentialLiftHeight
    {
        get => _sequentialLiftHeight;
        set => RaiseAndSetIfChanged(ref _sequentialLiftHeight, value);
    }

    public bool SequentialWaitTimeBeforeCureEnabled
    {
        get => _sequentialWaitTimeBeforeCureEnabled;
        set => RaiseAndSetIfChanged(ref _sequentialWaitTimeBeforeCureEnabled, value);
    }

    public decimal SequentialWaitTimeBeforeCure
    {
        get => _sequentialWaitTimeBeforeCure;
        set => RaiseAndSetIfChanged(ref _sequentialWaitTimeBeforeCure, value);
    }


    public KernelConfiguration Kernel { get; set; } = new();

    #endregion

    #region Constructor

    public OperationPhasedExposure() { }

    public OperationPhasedExposure(FileFormat slicerFile) : base(slicerFile)
    {
        if (SlicerFile.SupportPerLayerSettings)
        {
            _differentSettingsForSequentialLayers = true;
            if (SlicerFile.SupportGCode)
            {
                _sequentialLiftHeight = 0;
                _sequentialWaitTimeBeforeCure = 2;
            }
            else
            {
                _sequentialLiftHeight = 0.1m;
                _sequentialWaitTimeBeforeCure = 0;
            }
        }
    }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();
        if (Count == 0)
        {
            _phasedExposures.AddRange(new []
            {
                new PhasedExposure((decimal)SlicerFile.BottomExposureTime, 10, (decimal)SlicerFile.ExposureTime, 4),
                new PhasedExposure((decimal)SlicerFile.ExposureTime, 0, (decimal)SlicerFile.ExposureTime, 0),
            });
        }
    }

    #endregion

    #region Equality

    public bool Equals(OperationPhasedExposure? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _phasedExposures.Equals(other._phasedExposures) && _exposureDifferenceOnly == other._exposureDifferenceOnly && _exposureDifferenceOnlyOverlapIterations == other._exposureDifferenceOnlyOverlapIterations && _differentSettingsForSequentialLayers == other._differentSettingsForSequentialLayers && _sequentialLiftHeightEnabled == other._sequentialLiftHeightEnabled && _sequentialLiftHeight == other._sequentialLiftHeight && _sequentialWaitTimeBeforeCureEnabled == other._sequentialWaitTimeBeforeCureEnabled && _sequentialWaitTimeBeforeCure == other._sequentialWaitTimeBeforeCure;
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
        var layers = new Layer?[SlicerFile.LayerCount + LayerRangeCount * (_phasedExposures.Count - 1)];
        
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

            uint newLayerIndex = (uint)(LayerIndexStart + (layerIndex - LayerIndexStart) * _phasedExposures.Count);
            
            if (isBottomLayer) Interlocked.Increment(ref bottomLayers);

            using var matRoi = layer.LayerMatBoundingRectangle;
            uint affectedLayers = 0;

            var lastPhasedExposure = _phasedExposures[0];
            foreach (var phasedExposure in _phasedExposures)
            {
                int iterations = isBottomLayer ? phasedExposure.BottomIterations : phasedExposure.Iterations;
                bool setNewMat = false;
                using var newMatRoi = matRoi.Clone();

                if (iterations > 0)
                {
                    int tempIterations = iterations;
                    var kernel = Kernel.GetKernel(ref tempIterations);
                    CvInvoke.Erode(newMatRoi.RoiMat, newMatRoi.RoiMat, kernel, EmguExtensions.AnchorCenter, tempIterations, BorderType.Reflect101, default);
                    if (!CvInvoke.HasNonZero(newMatRoi.RoiMat)) continue; // Produce all black layer, ignoring
                    setNewMat = true;
                }

                if (affectedLayers > 0 && _exposureDifferenceOnly)
                {
                    var overlapIterations = _exposureDifferenceOnlyOverlapIterations + (isBottomLayer ? lastPhasedExposure.BottomIterations : lastPhasedExposure.Iterations);
                    if (overlapIterations > iterations)
                    {
                        using var overlapMat = new Mat();
                        int tempIterations = overlapIterations;
                        var kernel = Kernel.GetKernel(ref tempIterations);
                        CvInvoke.Erode(matRoi.RoiMat, overlapMat, kernel, EmguExtensions.AnchorCenter, tempIterations, BorderType.Reflect101, default);
                        if (CvInvoke.HasNonZero(overlapMat))
                        {
                            CvInvoke.Subtract(newMatRoi.RoiMat, overlapMat, newMatRoi.RoiMat);
                            setNewMat = CvInvoke.HasNonZero(newMatRoi.RoiMat);
                        }
                    }
                }

                var newLayer = layer.Clone();
                newLayer.ExposureTime = (float)(isBottomLayer ? phasedExposure.BottomExposureTime : phasedExposure.ExposureTime);

                if (_differentSettingsForSequentialLayers && affectedLayers > 0)
                {
                    if (_sequentialLiftHeightEnabled) newLayer.LiftHeightTotal = (float)_sequentialLiftHeight;
                    if (_sequentialWaitTimeBeforeCureEnabled) newLayer.SetWaitTimeBeforeCureOrLightOffDelay((float)_sequentialWaitTimeBeforeCure);
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
            SlicerFile.Layers = layers.Where(layer => layer is not null).ToArray()!;
        });

        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}