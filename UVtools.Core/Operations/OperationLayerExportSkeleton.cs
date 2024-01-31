/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using System;
using System.Drawing;
using System.Threading.Tasks;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


public sealed class OperationLayerExportSkeleton : Operation
{
    #region Members
    private string _filePath = null!;
    private bool _cropByRoi = true;

    #endregion

    #region Overrides

    public override bool CanHaveProfiles => false;

    public override string IconClass => "fa-solid fa-file-image";
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
        var result = $"[Crop by ROI: {_cropByRoi}]" +
                     LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Properties

    public string FilePath
    {
        get => _filePath;
        set => RaiseAndSetIfChanged(ref _filePath, value);
    }

    public bool CropByROI
    {
        get => _cropByRoi;
        set => RaiseAndSetIfChanged(ref _cropByRoi, value);
    }

    #endregion

    #region Constructor

    public OperationLayerExportSkeleton()
    { }

    public OperationLayerExportSkeleton(FileFormat slicerFile) : base(slicerFile)
    { }
        
    public override void InitWithSlicerFile()
    {
        _filePath = SlicerFile.FileFullPathNoExt + "_skeleton.png";
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
            using var skeletonRoi = matRoi.Skeletonize(new Size(3, 3), ElementShape.Rectangle, progress.Token);
            lock (progress.Mutex)
            {
                CvInvoke.Add(skeletonSumRoi, skeletonRoi, skeletonSumRoi, mask);
                progress++;
            }
        });
        
        if (_cropByRoi && HaveROI)
        {
            skeletonSumRoi.Save(_filePath);
        }
        else
        {
            skeletonSum.Save(_filePath);
        }

        return !progress.Token.IsCancellationRequested;
    }

    #endregion

    #region Equality

    private bool Equals(OperationLayerExportSkeleton other)
    {
        return _filePath == other._filePath && _cropByRoi == other._cropByRoi;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationLayerExportSkeleton other && Equals(other);
    }


    #endregion
}