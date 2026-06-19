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
using System.ComponentModel;
using System.Threading.Tasks;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed partial class OperationMorph : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Enums
    public enum MorphOperations
    {
        [Description("Erode: Contracts the boundaries within the object")]
        Erode = MorphOp.Erode,

        [Description("Dilate: Expands the boundaries within the object")]
        Dilate = MorphOp.Dilate,

        [Description("Noise removal: Removes small isolated pixels (Erode -> Dilate)")]
        Open = MorphOp.Open,

        [Description("Gap closing: Closes small holes inside the objects (Dilate -> Erode)")]
        Close = MorphOp.Close,

        [Description("Gradient: Removes the interior areas of objects and expand the boundaries by half (Dilate - Erode)")]
        Gradient = MorphOp.Gradient,

        // White top-hat transform: This is the difference between the image and its opening.
        [Description("White tophat: Removes small isolated pixels and only return its affected pixels (Image - Noise removal)")]
        WhiteTopHat = MorphOp.Tophat,

        // Black top-hat transform: Difference between the closing and the input image.
        [Description("Black tophat: Closes small holes inside the objects and only return its affected pixels (Gap closing - Image)")]
        BlackTopHat = MorphOp.Blackhat,

        [Description("Hit or miss: Finds pixels in a given kernel pattern")]
        HitMiss = MorphOp.HitMiss,

        [Description("Offset crop: Like erode but discards the outer pixels")]
        OffsetCrop,

        //[Description("Isolate features: Isolates thin features and discards other pixels (Opening - )")]
        //IsolateFeatures,
    }
    #endregion

    #region Members
    #endregion

    #region Overrides
    public override string IconClass => "Dharmachakra";
    public override string Title => "Morph";
    public override string Description =>
        $"Morph Model - " +
        $"Various operations that can be used to change the physical structure of the model or individual layers.";
    public override string ConfirmationText =>
        $"morph model layers {LayerIndexStart} through {LayerIndexEnd}?";

    public override string ProgressTitle =>
        $"Morphing layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressAction => "Morphed layers";

    #endregion

    #region Properties

    public MorphOp MorphOperationOpenCV
    {
        get
        {
            return MorphOperation switch
            {
                MorphOperations.OffsetCrop => MorphOp.Erode,
                _ => (MorphOp) MorphOperation
            };
        }
    }

    [ObservableProperty]
    public partial MorphOperations MorphOperation { get; set; } = MorphOperations.Erode;

    public uint Iterations
    {
        get => IterationsStart;
        set => IterationsStart = IterationsEnd = value;
    }

    [ObservableProperty]
    public partial uint IterationsStart { get; set; } = 1;

    [ObservableProperty]
    public partial uint IterationsEnd { get; set; } = 1;

    [ObservableProperty]
    public partial bool Chamfer { get; set; }

    public KernelConfiguration Kernel { get; set; } = new();

    public override string ToString()
    {
        var result = $"[{MorphOperation}] [Iterations: {IterationsStart}/{IterationsEnd}] [Chamfer: {Chamfer}]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Constructor

    public OperationMorph() { }

    public OperationMorph(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Equality

    private bool Equals(OperationMorph other)
    {
        return MorphOperation == other.MorphOperation && IterationsStart == other.IterationsStart && IterationsEnd == other.IterationsEnd && Chamfer == other.Chamfer;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationMorph other && Equals(other);
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        var isFade = Chamfer;
        FileFormat.MutateGetVarsIterationChamfer(
            LayerIndexStart,
            LayerIndexEnd,
            (int)IterationsStart,
            (int)IterationsEnd,
            ref isFade,
            out var iterationSteps,
            out var maxIteration
        );

        Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            int iterations = FileFormat.MutateGetIterationVar(isFade, (int)IterationsStart, (int)IterationsEnd, iterationSteps, maxIteration, LayerIndexStart, (uint)layerIndex);

            using var mat = SlicerFile[layerIndex].LayerMat;
            Execute(mat, iterations);
            SlicerFile[layerIndex].LayerMat = mat;

            progress.LockAndIncrement();
        });

        return !progress.Token.IsCancellationRequested;
    }

    public override bool Execute(Mat mat, params object[]? arguments)
    {
        int iterations = (int) IterationsStart;
        if (arguments is not null && arguments.Length >= 1)
        {
            iterations = (int) arguments[0];
        }

        using var original = mat.Clone();
        using var target = GetRoiOrDefault(mat);

        /*if (CoreSettings.CanUseCuda)
        {
            var gpuMat = target.ToGpuMat();
            using var morph = new CudaMorphologyFilter(MorphOperationOpenCV, target.Depth, target.NumberOfChannels, Kernel.Matrix, Kernel.Anchor, iterations);
            morph.Apply(gpuMat, gpuMat);
            gpuMat.Download(target);
        }
        else
        {*/

        var kernel = Kernel.GetKernel(ref iterations);
        CvInvoke.MorphologyEx(target, target, MorphOperationOpenCV, kernel, Kernel.Anchor, iterations, BorderType.Reflect101, default);

        if (MorphOperation == MorphOperations.OffsetCrop)
        {
            using var originalRoi = GetRoiOrDefault(original);
            originalRoi.CopyTo(target, target);
        }
        /*else if (MorphOperation == MorphOperations.IsolateFeatures)
        {
            var originalRoi = GetRoiOrDefault(original);
            CvInvoke.Subtract(originalRoi, target, target);
        }*/
        //}


        ApplyMask(original, target);
        return true;
    }

    #endregion
}