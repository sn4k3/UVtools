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
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


public class OperationFlip : Operation
{
    #region Members
    private bool _makeCopy;
    private FlipType _flipDirection = FlipType.Horizontal;
    #endregion

    #region Overrides
    public override string IconClass => "mdi-flip-horizontal";
    public override string Title => "Flip";
    public override string Description =>
        "Flip the layers of the model vertically and/or horizontally.\n" +
        "Note: Before perform this operation, un-rotate the layer preview to see the real orientation.";

    public override string ConfirmationText =>
        FlipDirection == FlipType.Both
            ? $"flip {(_makeCopy ? "and blend ":"")}layers {LayerIndexStart} through {LayerIndexEnd} Horizontally and Vertically?"
            : $"flip {(_makeCopy ? "and blend " : "")}layers {LayerIndexStart} through {LayerIndexEnd} {FlipDirection}?";

    public override string ProgressTitle =>
        FlipDirection == FlipType.Both
            ? $"Flipping {(_makeCopy ? "and blending " : "")}layers {LayerIndexStart} through {LayerIndexEnd} Horizontally and Vertically"
            : $"Flipping {(_makeCopy ? "and blending " : "")}layers {LayerIndexStart} through {LayerIndexEnd} {FlipDirection}";

    public override string ProgressAction => "Flipped layers";

    public override string ToString()
    {
        var result = $"[{_flipDirection}] [Blend: {_makeCopy}]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Properties

    public FlipType FlipDirection
    {
        get => _flipDirection;
        set => RaiseAndSetIfChanged(ref _flipDirection, value);
    }

    public bool MakeCopy
    {
        get => _makeCopy;
        set => RaiseAndSetIfChanged(ref _makeCopy, value);
    }

    #endregion

    #region Constructor

    public OperationFlip() { }

    public OperationFlip(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Equality

    protected bool Equals(OperationFlip other)
    {
        return _makeCopy == other._makeCopy && _flipDirection == other._flipDirection;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((OperationFlip) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (_makeCopy.GetHashCode() * 397) ^ (int) _flipDirection;
        }
    }

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
        using var original = mat.Clone();
        var target = GetRoiOrDefault(mat);

        if (MakeCopy)
        {
            using Mat dst = new();
            CvInvoke.Flip(target, dst, _flipDirection);
            CvInvoke.Add(target, dst, target);
        }
        else
        {
            CvInvoke.Flip(target, target, _flipDirection);
        }

        ApplyMask(original, target);

        return true;
    }
    #endregion
}