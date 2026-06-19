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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed partial class OperationLayerExportHeatMap : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Members

    #endregion

    #region Overrides

    public override bool CanHaveProfiles => false;

    public override string IconClass => "Image360";
    public override string Title => "Export layers to heat map";

    public override string Description =>
        "Export a layer range to a grayscale heat map image that represents the median of the mass in the Z depth/perception.\n" +
        "The pixel brightness/intensity shows where the most mass are concentrated.";

    public override string ConfirmationText =>
        $"generate a heatmap from layers {LayerIndexStart} through {LayerIndexEnd}?";

    public override string ProgressTitle =>
        $"Generating a heatmap from layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressAction => "Packed layers";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (LayerRangeCount < 2)
        {
            sb.AppendLine("To generate a heat map at least two layers are required.");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[Crop by ROI: {CropByROI}]" +
                     LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Properties

    [ObservableProperty]
    public partial string FilePath { get; set; } = null!;

    [ObservableProperty]
    public partial RotateDirection RotateDirection { get; set; } = RotateDirection.None;

    [ObservableProperty]
    public partial FlipDirection FlipDirection { get; set; } = FlipDirection.None;

    [ObservableProperty]
    public partial bool MergeSamePositionedLayers { get; set; } = true;

    [ObservableProperty]
    public partial bool CropByROI { get; set; } = true;

    #endregion

    #region Constructor

    public OperationLayerExportHeatMap()
    { }

    public OperationLayerExportHeatMap(FileFormat slicerFile) : base(slicerFile)
    {
        FlipDirection = SlicerFile.DisplayMirror;
    }

    public override void InitWithSlicerFile()
    {
        FilePath = SlicerFile.FileFullPathNoExt + "_heatmap.png";
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        using var resultMat = EmguCvExtensions.InitMat(SlicerFile.Resolution, 1, DepthType.Cv32S);
        using var resultMatRoi = GetRoiOrDefault(resultMat);
        using var mask = GetMask(resultMat, HaveROI ? ROI.Location.Invert() : default);

        var layerRange = MergeSamePositionedLayers
            ? SlicerFile.GetDistinctLayersByPositionZ(LayerIndexStart, LayerIndexEnd).ToArray()
            : GetSelectedLayerRange().ToArray();

        progress.ItemCount = (uint)layerRange.Length;


       Parallel.ForEach(layerRange, CoreSettings.GetParallelOptions(progress), layer =>
        {
            progress.PauseIfRequested();
            using var mat = MergeSamePositionedLayers
                ? SlicerFile.GetMergedMatForSequentialPositionedLayers(layer.Index)
                : layer.LayerMat;
            using var matRoi = GetRoiOrDefault(mat);

            matRoi.ConvertTo(matRoi, DepthType.Cv32S);

            lock (progress.Mutex)
            {
                CvInvoke.Add(resultMatRoi, matRoi, resultMatRoi, mask);
                progress++;
            }
        });


        resultMat.ConvertTo(resultMat, DepthType.Cv8U, 1.0 / layerRange.Length);

        if (FlipDirection != FlipDirection.None)
        {
            CvInvoke.Flip(resultMat, resultMat, (FlipType)FlipDirection);
        }

        if (RotateDirection != RotateDirection.None)
        {
            CvInvoke.Rotate(resultMat, resultMat, (RotateFlags)RotateDirection);
        }

        if (CropByROI && HaveROI)
        {
            using var sumMatRoi = GetRoiOrDefault(resultMat);
            sumMatRoi.Save(FilePath);
        }
        else
        {
            resultMat.Save(FilePath);
        }


        return !progress.Token.IsCancellationRequested;
    }

    #endregion

    #region Equality

    private bool Equals(OperationLayerExportHeatMap other)
    {
        return FilePath == other.FilePath && RotateDirection == other.RotateDirection && FlipDirection == other.FlipDirection && MergeSamePositionedLayers == other.MergeSamePositionedLayers && CropByROI == other.CropByROI;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationLayerExportHeatMap other && Equals(other);
    }


    #endregion
}