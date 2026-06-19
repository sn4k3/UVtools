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
using System.Threading.Tasks;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public partial class OperationFlip : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Members
    #endregion

    #region Overrides
    public override string IconClass => "FlipHorizontal";
    public override string Title => "Flip";
    public override string Description =>
        "Flip the layers of the model vertically and/or horizontally.\n" +
        "Note: Before perform this operation, un-rotate the layer preview to see the real orientation.";

    public override string ConfirmationText =>
        FlipDirection == FlipType.Both
            ? $"flip {(MakeCopy ? "and blend ":"")}layers {LayerIndexStart} through {LayerIndexEnd} Horizontally and Vertically?"
            : $"flip {(MakeCopy ? "and blend " : "")}layers {LayerIndexStart} through {LayerIndexEnd} {FlipDirection}?";

    public override string ProgressTitle =>
        FlipDirection == FlipType.Both
            ? $"Flipping {(MakeCopy ? "and blending " : "")}layers {LayerIndexStart} through {LayerIndexEnd} Horizontally and Vertically"
            : $"Flipping {(MakeCopy ? "and blending " : "")}layers {LayerIndexStart} through {LayerIndexEnd} {FlipDirection}";

    public override string ProgressAction => "Flipped layers";

    public override string ToString()
    {
        var result = $"[{FlipDirection}] [Blend: {MakeCopy}]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Properties

    [ObservableProperty]
    public partial FlipType FlipDirection { get; set; } = FlipType.Horizontal;

    [ObservableProperty]
    public partial bool MakeCopy { get; set; }

    #endregion

    #region Constructor

    public OperationFlip() { }

    public OperationFlip(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Equality

    protected bool Equals(OperationFlip other)
    {
        return MakeCopy == other.MakeCopy && FlipDirection == other.FlipDirection;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((OperationFlip) obj);
    }

    #endregion

    #region Methods
    protected override bool ExecuteInternally(OperationProgress progress)
    {
        Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            using var mat = SlicerFile[layerIndex].LayerMat;
            Execute(mat);
            SlicerFile[layerIndex].LayerMat = mat;

            progress.LockAndIncrement();
        });

        return !progress.Token.IsCancellationRequested;
    }

    public override bool Execute(Mat mat, params object[]? arguments)
    {
        using var original = mat.Clone();
        using var target = GetRoiOrDefault(mat);

        if (MakeCopy)
        {
            using Mat dst = new();
            CvInvoke.Flip(target, dst, FlipDirection);
            CvInvoke.Add(target, dst, target);
        }
        else
        {
            CvInvoke.Flip(target, target, FlipDirection);
        }

        ApplyMask(original, target);

        return true;
    }
    #endregion
}