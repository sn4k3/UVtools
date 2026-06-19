/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using Emgu.CV.CvEnum;
using EmguExtensions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed partial class OperationLayerExportSkeleton : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Members

    #endregion

    #region Overrides

    public override bool CanHaveProfiles => false;

    public override string IconClass => "RadiologyBox";
    public override string Title => "Export layers to skeleton";

    public override string Description =>
        "Export a layer range to a skeletonized image that is the sum of each layer skeleton.";

    public override string ConfirmationText =>
        $"skeletonize from layers {LayerIndexStart} through {LayerIndexEnd}?";

    public override string ProgressTitle =>
        $"Skeletonizing from layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressAction => "Skeletonized layers";

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
    public partial bool CropByROI { get; set; } = true;

    #endregion

    #region Constructor

    public OperationLayerExportSkeleton()
    { }

    public OperationLayerExportSkeleton(FileFormat slicerFile) : base(slicerFile)
    { }

    public override void InitWithSlicerFile()
    {
        FilePath = SlicerFile.FileFullPathNoExt + "_skeleton.png";
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        using var skeletonSum = SlicerFile.CreateMat();
        using var skeletonSumRoi = GetRoiOrDefault(skeletonSum);
        using var mask = GetMask(skeletonSum);


        Parallel.For(LayerIndexStart, LayerIndexEnd+1, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            using var mat = SlicerFile[layerIndex].LayerMat;
            using var matRoi = GetRoiOrDefault(mat);
            using var skeletonRoi = matRoi.Skeletonize();
            lock (progress.Mutex)
            {
                CvInvoke.Add(skeletonSumRoi, skeletonRoi, skeletonSumRoi, mask);
                progress++;
            }
        });

        if (CropByROI && HaveROI)
        {
            skeletonSumRoi.Save(FilePath);
        }
        else
        {
            skeletonSum.Save(FilePath);
        }

        return !progress.Token.IsCancellationRequested;
    }

    #endregion

    #region Equality

    private bool Equals(OperationLayerExportSkeleton other)
    {
        return FilePath == other.FilePath && CropByROI == other.CropByROI;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationLayerExportSkeleton other && Equals(other);
    }


    #endregion
}