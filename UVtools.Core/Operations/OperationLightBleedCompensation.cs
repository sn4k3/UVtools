/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;

[Serializable]
public class OperationLightBleedCompensation : Operation
{
    #region Enums

    public enum LightBleedCompensationLookupMode : byte
    {
        [Description("Previous: Look for sequential pixels relative to the previous layers")]
        Previous,

        [Description("Next: Look for sequential pixels relative to the next layers")]
        Next,

        [Description("Both: Look for sequential pixels relative to the previous and next layers")]
        Both
    }

    #endregion

    #region Members

    private LightBleedCompensationLookupMode _lookupMode = LightBleedCompensationLookupMode.Next;
    private string _dimBy = "25,15,10,5";

    #endregion

    #region Overrides

    public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.Normal;
    public override string IconClass => "mdi-lightbulb-on";
    public override string Title => "Light bleed compensation";
    public override string Description =>
        "Compensate the over-curing and light bleed from clear resins by dimming the sequential pixels.\n" +
        "Note: You need to find the optimal minimum pixel brightness that such resin can print in order to optimize this process.\n" +
        "With more translucent resins you can go with lower brightness but stick to a limit that can form the layer without loss." +
        " Tiny details can be lost when using low brightness level.\n" +
        "After apply a light bleed compensation, do not apply or re-run this tool over.";

    public override string ConfirmationText =>
        $"compensate layers {LayerIndexStart} through {LayerIndexEnd}?";

    public override string ProgressTitle =>
        $"Compensate layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressAction => "Compensated layers";

    public override string? ValidateInternally()
    {
        StringBuilder sb = new();

        if (DimByArray.Length == 0)
        {
            sb.AppendLine($"The dim levels are invalid or not set.");
        }

        if (MaximumSubtraction >= byte.MaxValue)
        {
            sb.AppendLine($"The sum of dim levels are producing black pixels.");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[Lookup: {_lookupMode}]" +
                     $" [Dim by: {_dimBy}]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Constructor

    public OperationLightBleedCompensation() { }

    public OperationLightBleedCompensation(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Properties

    public LightBleedCompensationLookupMode LookupMode
    {
        get => _lookupMode;
        set => RaiseAndSetIfChanged(ref _lookupMode, value);
    }

    public string DimBy
    {
        get => _dimBy;
        set
        {
            if(!RaiseAndSetIfChanged(ref _dimBy, value)) return;
            RaisePropertyChanged(nameof(MinimumBrightness));
            RaisePropertyChanged(nameof(MaximumSubtraction));
        }
    }

    public int MinimumBrightness => 255 - MaximumSubtraction;
    public int MaximumSubtraction => DimByArray.Aggregate(0, (current, dim) => current + dim);

    public byte[] DimByArray
    {
        get
        {
            List<byte> levels = new();
            var split = _dimBy.Split(',', StringSplitOptions.TrimEntries);
            foreach (var str in split)
            {
                if (!byte.TryParse(str, out var brightness)) continue;
                if (brightness is byte.MinValue or byte.MaxValue) continue;
                levels.Add(brightness);
            }

            return levels.ToArray();
        }
    }

    public MCvScalar[] DimByMCvScalar
    {
        get
        {
            List<MCvScalar> levels = new();
            var split = _dimBy.Split(',', StringSplitOptions.TrimEntries);
            foreach (var str in split)
            {
                if (!byte.TryParse(str, out var brightness)) continue;
                if (brightness is byte.MinValue or byte.MaxValue) continue;
                levels.Add(new MCvScalar(brightness));
            }

            return levels.ToArray();
        }
    }

    #endregion

    #region Equality

    protected bool Equals(OperationLightBleedCompensation other)
    {
        return _lookupMode == other._lookupMode && _dimBy == other._dimBy;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OperationLightBleedCompensation) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int) _lookupMode, _dimBy);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Get the cached dim mat's
    /// </summary>
    /// <returns></returns>
    public Mat[] GetDimMats()
    {
        var dimLevels = DimByMCvScalar;
        if (dimLevels.Length == 0) return Array.Empty<Mat>();
        var mats = new Mat[dimLevels.Length];
        var matSize = GetRoiSizeOrDefault();
        for (var i = 0; i < mats.Length; i++)
        {
            mats[i] = EmguExtensions.InitMat(matSize, dimLevels[i]);
        }

        return mats;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        var dimMats = GetDimMats();
        if (dimMats.Length == 0) return false;
        Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.ParallelOptions, layerIndex =>
        {
            if (progress.Token.IsCancellationRequested) return; // Abort operation, user requested cancellation

            var layer = SlicerFile[layerIndex];
            using var mat = layer.LayerMat;    
            using var original = mat.Clone();    
            var target = GetRoiOrDefault(mat);

            for (byte i = 0; i < dimMats.Length; i++)
            {
                Mat? mask = null;
                Mat? previousMat = null;
                Mat? previousMatRoi = null;
                Mat? nextMat = null;
                Mat? nextMatRoi = null;


                if (_lookupMode is LightBleedCompensationLookupMode.Previous or LightBleedCompensationLookupMode.Both)
                {
                    int layerPreviousIndex = (int)layerIndex - i - 1;
                    if (layerPreviousIndex >= LayerIndexStart)
                    {
                        previousMat = SlicerFile[layerPreviousIndex].LayerMat;
                        mask = previousMatRoi = GetRoiOrDefault(previousMat);
                    }
                }
                if (_lookupMode is LightBleedCompensationLookupMode.Next or LightBleedCompensationLookupMode.Both)
                {
                    uint layerIndexNext = (uint) (layerIndex + i + 1);
                    if (layerIndexNext <= LayerIndexEnd)
                    {
                        nextMat = SlicerFile[layerIndexNext].LayerMat;
                        mask = nextMatRoi = GetRoiOrDefault(nextMat);
                    }
                }
                    
                if (previousMat is null && nextMat is null) break; // Nothing more to do
                if (previousMat is not null && nextMat is not null) // both, need to merge previous with next layer
                {
                    CvInvoke.Add(previousMatRoi, nextMatRoi, previousMatRoi);
                    mask = previousMatRoi;
                }

                CvInvoke.Subtract(target, dimMats[i], target, mask);

                previousMat?.Dispose();
                nextMat?.Dispose();
            }

            // Apply the results only to the selected masked area, if user selected one
            ApplyMask(original, target);

            // Set current layer image with the modified mat we just manipulated
            layer.LayerMat = mat;

            // Increment progress bar by 1
            progress.LockAndIncrement();
        });

        foreach (var dimMat in dimMats)
        {
            dimMat.Dispose();
        }

        // return true if not cancelled by user
        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}