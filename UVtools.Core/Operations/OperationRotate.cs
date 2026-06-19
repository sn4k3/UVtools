/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;
using EmguExtensions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public partial class OperationRotate : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Members
    #endregion

    #region Overrides
    public override string IconClass => "Rotate3dVariant";
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
        if (AngleDegrees == 0)
        {
            return "An angle of 0 will have no effect";
        }

        return null;
    }

    public override string ToString()
    {
        var result = $"[Angle: {Math.Abs(AngleDegrees)}º {(AngleDegrees < 0 ? "CCW" : "CW")}]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Constructor

    public OperationRotate() { }

    public OperationRotate(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Properties
    [ObservableProperty]
    public partial decimal AngleDegrees { get; set; } = 90;
    #endregion

    #region Equality

    protected bool Equals(OperationRotate other)
    {
        return AngleDegrees == other.AngleDegrees;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((OperationRotate) obj);
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
        target.RotateFromCenter((double)AngleDegrees);
        ApplyMask(original, target);
        return true;
    }

    #endregion
}