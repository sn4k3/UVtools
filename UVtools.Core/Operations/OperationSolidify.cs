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
using System.Threading.Tasks;
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;

[Serializable]
public sealed class OperationSolidify : Operation
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
    private AreaCheckTypes _areaCheckType = AreaCheckTypes.More;
    #endregion

    #region Overrides

    public override string IconClass => "fas fa-circle";
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
        set => RaiseAndSetIfChanged(ref _minimumArea, Math.Max(1, value));
    }

    public AreaCheckTypes AreaCheckType
    {
        get => _areaCheckType;
        set => RaiseAndSetIfChanged(ref _areaCheckType, value);
    }

    public static Array AreaCheckTypeItems => Enum.GetValues(typeof(AreaCheckTypes));

    public override string ToString()
    {
        var result = $"[Area: {_areaCheckType} than {_minimumArea}px²]" + LayerRangeString;
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
            using var mat = SlicerFile[layerIndex].LayerMat;
            Execute(mat);
            SlicerFile[layerIndex].LayerMat = mat;
            progress.LockAndIncrement();
        });

        return !progress.Token.IsCancellationRequested;
    }

    public override bool Execute(Mat mat, params object[]? arguments)
    {
        using Mat filteredMat = new();
        using var original = mat.Clone();
        var target = GetRoiOrDefault(mat);

        CvInvoke.Threshold(target, filteredMat, 127, 255, ThresholdType.Binary); // Clean AA
        using var contours = filteredMat.FindContours(out var hierarchy, RetrType.Ccomp);
        for (int i = 0; i < contours.Size; i++)
        {
            if (hierarchy[i, EmguContour.HierarchyFirstChild] != -1 || hierarchy[i, EmguContour.HierarchyParent] == -1) continue;
            if (MinimumArea >= 1)
            {
                var area = CvInvoke.ContourArea(contours[i]);
                if (AreaCheckType == AreaCheckTypes.More)
                {
                    if (area < MinimumArea) continue;
                }
                else
                {
                    if (area > MinimumArea) continue;
                }

            }

            CvInvoke.DrawContours(target, contours, i, EmguExtensions.WhiteColor, -1);
        }

        ApplyMask(original, target);

        return true;
    }

    #endregion
}