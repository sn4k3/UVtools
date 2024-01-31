/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using System;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


public class OperationResize : Operation
{
    #region Members
    private decimal _x = 100;
    private decimal _y = 100;
    private bool _constrainXy;
    private bool _isFade;
    #endregion

    #region Overrides
    public override string IconClass => "fa-solid fa-expand-alt";
    public override string Title => "Resize";
    public override string Description =>
        "Resize the model by a percentage in the X/Y plane.\n\n" +
        "NOTE: This operation is applied based on the original orientation of the layers in the model file. " +
        "If the image is rotated 90° in the Layer Preview, you will need to compensate by inverting " +
        "the X and Y values.  The print bounds will also not be validated as part of this operation, so please " +
        "ensure that the scaling factor does not result in the model being clipped.";

    public override string ConfirmationText =>
        $"resize model layers {LayerIndexStart} through {LayerIndexEnd} by " +
        $"X={X}% and Y={Y}%";

    public override string ProgressTitle =>
        $"Resizing from layers {LayerIndexStart} to {LayerIndexEnd} at X:{X}%  Y:{Y}% ";

    public override string ProgressAction => "Resized layers";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (X == 100m && Y == 100m)
        {
            sb.AppendLine("X and Y can't both be 100%.");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[X: {_x}%, Y: {_y}%] [Fade: {_isFade}] [Constrain: {_constrainXy}]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Properties
    public decimal X
    {
        get => _x;
        set
        {
            RaiseAndSetIfChanged(ref _x, value);
            if (_constrainXy)
                Y = value;
            RaisePropertyChanged(nameof(XScale));
        }
    }

    public decimal XScale => _x / 100m;
    public decimal YScale => _y / 100m;

    public decimal Y
    {
        get => _y;
        set
        {
            if(!RaiseAndSetIfChanged(ref _y, value)) return;
            RaisePropertyChanged(nameof(YScale));
        }
    }

    public bool ConstrainXY
    {
        get => _constrainXy;
        set
        {
            if (!RaiseAndSetIfChanged(ref _constrainXy, value)) return;
            if (_constrainXy)
            {
                Y = _x;
            }
        }
    }

    public bool IsFade
    {
        get => _isFade;
        set => RaiseAndSetIfChanged(ref _isFade, value);
    }
    #endregion

    #region Constructor

    public OperationResize() { }

    public OperationResize(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Equality

    protected bool Equals(OperationResize other)
    {
        return _x == other._x && _y == other._y && _constrainXy == other._constrainXy && _isFade == other._isFade;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((OperationResize) obj);
    }
    
    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        if (_x <= 0 || _y <= 0) return false;
        if (_x == 100 && _y == 100) return false;

        decimal xSteps = Math.Abs(100 - _x) / (LayerIndexEnd - LayerIndexStart + 1);
        decimal ySteps = Math.Abs(100 - _y) / (LayerIndexEnd - LayerIndexStart + 1);

        Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            var newX = _x;
            var newY = _y;
            if (IsFade)
            {
                if (newX != 100)
                {
                    newX = newX < 100
                        ? newX + (layerIndex - LayerIndexStart) * xSteps
                        : newX - (layerIndex - LayerIndexStart) * xSteps;
                }

                if (newY != 100)
                {
                    newY = newY < 100
                        ? newY + (layerIndex - LayerIndexStart) * ySteps
                        : newY - (layerIndex - LayerIndexStart) * ySteps;
                }
            }

            progress.LockAndIncrement();

            if (newX == 100 && newY == 100) return;

            using var mat = SlicerFile[layerIndex].LayerMat;
            Execute(mat, newX  / 100, newY / 100);
            SlicerFile[layerIndex].LayerMat = mat;
        });

        return !progress.Token.IsCancellationRequested;
    }

    public override bool Execute(Mat mat, params object[]? arguments)
    {
        var xScale = XScale;
        var yScale = YScale;
        if (arguments is not null && arguments.Length >= 2)
        {
            xScale = (decimal) arguments[0];
            yScale = (decimal) arguments[1];
        }

        using var original = mat.Clone();
        using var target = GetRoiOrDefault(mat);
        target.TransformFromCenter((double) xScale, (double) yScale);
        ApplyMask(original, target);
        return true;
    }

    #endregion
}