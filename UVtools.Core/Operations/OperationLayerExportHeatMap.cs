/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed class OperationLayerExportHeatMap : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Members
    private string _filePath = null!;
    private RotateDirection _rotateDirection = RotateDirection.None;
    private FlipDirection _flipDirection = FlipDirection.None;
    private bool _mergeSamePositionedLayers = true;
    private bool _cropByRoi = true;

    #endregion

    #region Overrides

    public override bool CanHaveProfiles => false;

    public override string IconClass => "fa-solid fa-file-image";
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

    public RotateDirection RotateDirection
    {
        get => _rotateDirection;
        set => RaiseAndSetIfChanged(ref _rotateDirection, value);
    }

    public FlipDirection FlipDirection
    {
        get => _flipDirection;
        set => RaiseAndSetIfChanged(ref _flipDirection, value);
    }

    public bool MergeSamePositionedLayers
    {
        get => _mergeSamePositionedLayers;
        set => RaiseAndSetIfChanged(ref _mergeSamePositionedLayers, value);
    }

    public bool CropByROI
    {
        get => _cropByRoi;
        set => RaiseAndSetIfChanged(ref _cropByRoi, value);
    }

    #endregion

    #region Constructor

    public OperationLayerExportHeatMap()
    { }

    public OperationLayerExportHeatMap(FileFormat slicerFile) : base(slicerFile)
    {
        _flipDirection = SlicerFile.DisplayMirror;
    }

    public override void InitWithSlicerFile()
    {
        _filePath = SlicerFile.FileFullPathNoExt + "_heatmap.png";
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        using var resultMat = EmguExtensions.InitMat(SlicerFile.Resolution, 1, DepthType.Cv32S);
        using var resultMatRoi = GetRoiOrDefault(resultMat);
        using var mask = GetMask(resultMat, HaveROI ? ROI.Location.Invert() : default);

        var layerRange = _mergeSamePositionedLayers
            ? SlicerFile.GetDistinctLayersByPositionZ(LayerIndexStart, LayerIndexEnd).ToArray()
            : GetSelectedLayerRange().ToArray();

        progress.ItemCount = (uint)layerRange.Length;


       Parallel.ForEach(layerRange, CoreSettings.GetParallelOptions(progress), layer =>
        {
            progress.PauseIfRequested();
            using var mat = _mergeSamePositionedLayers
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

        if (_flipDirection != FlipDirection.None)
        {
            CvInvoke.Flip(resultMat, resultMat, (FlipType)_flipDirection);
        }

        if (_rotateDirection != RotateDirection.None)
        {
            CvInvoke.Rotate(resultMat, resultMat, (RotateFlags)_rotateDirection);
        }

        if (_cropByRoi && HaveROI)
        {
            using var sumMatRoi = GetRoiOrDefault(resultMat);
            sumMatRoi.Save(_filePath);
        }
        else
        {
            resultMat.Save(_filePath);
        }


        return !progress.Token.IsCancellationRequested;
    }

    #endregion

    #region Equality

    private bool Equals(OperationLayerExportHeatMap other)
    {
        return _filePath == other._filePath && _rotateDirection == other._rotateDirection && _flipDirection == other._flipDirection && _mergeSamePositionedLayers == other._mergeSamePositionedLayers && _cropByRoi == other._cropByRoi;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationLayerExportHeatMap other && Equals(other);
    }


    #endregion
}