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
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


public sealed partial class OperationSolidify : Operation
{
    #region Enums
    public enum AreaCheckTypes
    {
        More,
        Less
    }
    #endregion

    #region Members
    private uint _minimumArea = 1;
    #endregion

    #region Overrides

    public override string IconClass => "Rhombus";
    public override string Title => "Solidify";

    public override string Description =>
        "Solidifies the selected layers, closing all interior holes.\n\n" +
        "NOTE: All open areas of the layer that are completely surrounded by pixels will be filled. Please ensure that none of the holes in the layer are required before proceeding.";

    public override string ConfirmationText =>
        $"solidify layers {LayerIndexStart} through {LayerIndexEnd}?";

    public override string ProgressTitle =>
        $"Solidifying layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressAction => "Solidified layers";

    /// <summary>
    /// Gets the minimum required area to solidify it
    /// </summary>
    public uint MinimumArea
    {
        get => _minimumArea;
        set => SetProperty(ref _minimumArea, Math.Max(1, value));
    }

    [ObservableProperty]
    public partial AreaCheckTypes AreaCheckType { get; set; } = AreaCheckTypes.More;

    public static Array AreaCheckTypeItems => Enum.GetValues(typeof(AreaCheckTypes));

    public override string ToString()
    {
        var result = $"[Area: {AreaCheckType} than {_minimumArea}px²]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Constructor

    public OperationSolidify() { }

    public OperationSolidify(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            var layer = SlicerFile[layerIndex];
            if (!layer.IsEmpty) // An empty layer have no holes
            {
                using var mat = layer.LayerMat;
                if (Solidify(mat)) // Only compress the layer again if a hole was filled
                {
                    layer.LayerMat = mat;
                }
            }

            progress.LockAndIncrement();
        });

        return !progress.Token.IsCancellationRequested;
    }

    public override bool Execute(Mat mat, params object[]? arguments)
    {
        Solidify(mat);
        return true;
    }

    /// <summary>
    /// Fills the interior holes of <paramref name="mat"/>.
    /// </summary>
    /// <param name="mat">The layer, which will be modified.</param>
    /// <returns>True if any hole was filled, otherwise false and the layer is untouched.</returns>
    private bool Solidify(Mat mat)
    {
        using var original = CloneIfMasked(mat);
        using var target = GetRoiOrDefault(mat);

        // Holes are always inside of the content, so only the content area needs to be processed
        var bounds = CvInvoke.BoundingRectangle(target);
        if (bounds.IsEmpty) return false;
        using var area = new Mat(target, bounds);

        using Mat filteredMat = new();
        CvInvoke.Threshold(area, filteredMat, 127, 255, ThresholdType.Binary); // Clean AA
        using var contours = filteredMat.FindContours(out var hierarchy, RetrType.Ccomp);
        var changed = false;
        for (int i = 0; i < contours.Size; i++)
        {
            if (hierarchy[i, EmguContour.HierarchyFirstChild] != -1 || hierarchy[i, EmguContour.HierarchyParent] == -1) continue;
            if (MinimumArea >= 1)
            {
                var contourArea = CvInvoke.ContourArea(contours[i]);
                if (AreaCheckType == AreaCheckTypes.More)
                {
                    if (contourArea < MinimumArea) continue;
                }
                else
                {
                    if (contourArea > MinimumArea) continue;
                }

            }

            CvInvoke.DrawContours(area, contours, i, EmguCvExtensions.WhiteColor, -1);
            changed = true;
        }

        if (changed)
        {
            ApplyMask(original, target);
        }

        return changed;
    }
    #endregion
}