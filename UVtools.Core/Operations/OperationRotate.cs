/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using System;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


public class OperationRotate : Operation
{
    #region Members
    private decimal _angleDegrees = 90;
    #endregion

    #region Overrides
    public override string IconClass => "fa-solid fa-sync-alt";
    public override string Title => "Rotate";
    public override string Description =>
        "Rotate the layers of the model.\n";

    public override string ConfirmationText =>
        $"rotate layers {LayerIndexStart} through {LayerIndexEnd} {(AngleDegrees < 0?"counter-clockwise":"clockwise")} by {Math.Abs(AngleDegrees)}°?";

    public override string ProgressTitle =>
        $"Rotating layers {LayerIndexStart} through {LayerIndexEnd} {(AngleDegrees < 0 ? "counter-clockwise" : "clockwise")} by {Math.Abs(AngleDegrees)}°";

    public override string ProgressAction => "Rotated layers";

    public override string? ValidateInternally()
    {
        if (_angleDegrees == 0)
        {
            return "An angle of 0 will have no effect";
        }

        return null;
    }

    public override string ToString()
    {
        var result = $"[Angle: {Math.Abs(_angleDegrees)}º {(AngleDegrees < 0 ? "CCW" : "CW")}]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Constructor

    public OperationRotate() { }

    public OperationRotate(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Properties
    public decimal AngleDegrees
    {
        get => _angleDegrees;
        set => RaiseAndSetIfChanged(ref _angleDegrees, value);
    }
    #endregion

    #region Equality

    protected bool Equals(OperationRotate other)
    {
        return _angleDegrees == other._angleDegrees;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((OperationRotate) obj);
    }

    public override int GetHashCode()
    {
        return _angleDegrees.GetHashCode();
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
        target.Rotate((double)AngleDegrees);
        ApplyMask(original, target);
        return true;
    }

    #endregion
}