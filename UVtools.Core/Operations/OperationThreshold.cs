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
using System;
using System.Threading.Tasks;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public partial class OperationThreshold : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Overrides
    public override string IconClass => "Opacity";
    public override string Title => "Threshold pixels";
    public override string Description =>
        "Manipulate pixel values based on a threshold.\n\n" +
        "Pixles brighter than the theshold will be set to the Max value, " +
        "all other pixels will be set to 0.\n\n" +
        "See https://docs.opencv.org/master/d7/d4d/tutorial_py_thresholding.html";

    public override string ConfirmationText =>
        $"apply threshold {Threshold} with max {Maximum} from layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressTitle =>
        $"Applying threshold {Threshold} with max {Maximum} from layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressAction => "Thresholded layers";

    public override string ToString()
    {
        var result = $"[{Type} = {Threshold} / {Maximum}]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Constructor

    public OperationThreshold() { }

    public OperationThreshold(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Properties

    [ObservableProperty]
    public partial byte Threshold { get; set; } = 127;

    [ObservableProperty]
    public partial byte Maximum { get; set; } = 255;

    [ObservableProperty]
    public partial ThresholdType Type { get; set; } = ThresholdType.Binary;

    public static Array ThresholdTypes => Enum.GetValues(typeof(ThresholdType));
    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            using (var mat = SlicerFile[layerIndex].LayerMat)
            {
                Execute(mat);
                SlicerFile[layerIndex].LayerMat = mat;
            }

            progress.LockAndIncrement();
        });

        return !progress.Token.IsCancellationRequested;
    }

    public override bool Execute(Mat mat, params object[]? arguments)
    {
        using var original = mat.Clone();
        using var target = GetRoiOrDefault(mat);
        CvInvoke.Threshold(target, target, Threshold, Maximum, Type);
        ApplyMask(original, target);
        return true;
    }

    #endregion

    #region Equality

    protected bool Equals(OperationThreshold other)
    {
        return Threshold == other.Threshold && Maximum == other.Maximum && Type == other.Type;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OperationThreshold) obj);
    }

    #endregion
}
