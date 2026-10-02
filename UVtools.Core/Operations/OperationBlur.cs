/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using CommunityToolkit.Mvvm.ComponentModel;
using EmguExtensions;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed partial class OperationBlur : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Overrides

    public override string IconClass => "WaterOpacity";
    public override string Title => "Blur";
    public override string Description =>
        $"Blur layer images by applying a low pass filter.\n\n" +
        "NOTE: Target printer must support AntiAliasing in order to use this function.\n" +
        "See https://docs.opencv.org/master/d4/d13/tutorial_py_filtering.html";

    public override string ConfirmationText =>
        $"blur model with {BlurOperation} from layers {LayerIndexStart} through {LayerIndexEnd}?";

    public override string ProgressTitle =>
        $"Bluring model with {BlurOperation} from layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressAction => "Blured layers";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (BlurOperation is BlurAlgorithm.StackBlur or BlurAlgorithm.GaussianBlur or BlurAlgorithm.MedianBlur)
        {
            if (Size % 2 != 1)
            {
                sb.AppendLine("Size must be a odd number.");
            }
        }

        if (BlurOperation == BlurAlgorithm.Filter2D)
        {
            if (Kernel is null)
            {
                sb.AppendLine("Kernel can not be empty.");
            }
        }

        return sb.ToString();
    }

    #endregion

    #region Enums
    public enum BlurAlgorithm
    {
        [Description("Stack Blur: Normalized stack blur")]
        StackBlur,
        [Description("Box Blur: Normalized box filter")]
        BoxBlur,
        [Description("Pyramid: Down/up-sampling step of Gaussian pyramid decomposition")]
        Pyramid,
        [Description("Median Blur: Each pixel becomes the median of its surrounding pixels")]
        MedianBlur,
        [Description("Gaussian Blur: Each pixel is a sum of fractions of each pixel in its neighborhood")]
        GaussianBlur,
        [Description("Filter 2D: Applies an arbitrary linear filter to an image")]
        Filter2D
    }
    #endregion

    #region Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSizeEnabled))]
    [NotifyPropertyChangedFor(nameof(IsKernelVisible))]
    public partial BlurAlgorithm BlurOperation { get; set; }

    [ObservableProperty]
    public partial uint Size { get; set; } = 1;

    public bool IsSizeEnabled => BlurOperation != BlurAlgorithm.Pyramid &&
                                 BlurOperation != BlurAlgorithm.Filter2D;

    public bool IsKernelVisible => BlurOperation == BlurAlgorithm.Filter2D;

    public KernelConfiguration Kernel { get; set; } = new ();

    public override string ToString()
    {
        var result = $"[{BlurOperation}] [Size: {Size}]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Constructor

    public OperationBlur() { }

    public OperationBlur(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            var layer = SlicerFile[layerIndex];
            if (!layer.IsEmpty) // Blurring nothing is nothing
            {
                using var mat = layer.LayerMat;
                Execute(mat);
                layer.LayerMat = mat;
            }

            progress.LockAndIncrement();
        });

        return !progress.Token.IsCancellationRequested;
    }

    private static readonly System.Threading.Lock StackBlurLock = new();

    /// <summary>
    /// Gets how far from a pixel the blur can reach, or -1 if the blur is not limited to a distance.
    /// </summary>
    private int GetBlurReach()
    {
        return BlurOperation switch
        {
            BlurAlgorithm.BoxBlur or BlurAlgorithm.MedianBlur => (int)Math.Min(Size, int.MaxValue / 4u) + 1,
            // The other blurs can give slightly different values when applied over a smaller area,
            // pyramid also depends on the position of the pixels
            _ => -1
        };
    }

    public override bool Execute(Mat mat, params object[]? arguments)
    {
        Size size = new((int)Size, (int)Size);
        Point anchor = Kernel.Anchor;
        if (anchor.IsEmpty) anchor = EmguCvExtensions.AnchorCenter;
        //if (size.IsEmpty) size = new Size(3, 3);
        //if (anchor.IsEmpty) anchor = EmguCvExtensions.AnchorCenter;
        using var target = GetRoiOrDefault(mat);
        using var original = CloneIfMasked(mat);

        // The blur does not change pixels away from the content, so the blank area is not processed
        // A ROI view reads the pixels around it, which can reach inside of it, so the crop is only safe without a ROI
        var reach = HaveROI ? -1 : GetBlurReach();
        using var croppedTarget = reach >= 0 ? CropToContent(target, reach) : null;
        if (reach >= 0 && croppedTarget is null) return true; // Blank
        var area = croppedTarget ?? target;

        // The stack blur does not handle the border of a ROI view properly, work over a contiguous copy
        using var contiguous = BlurOperation == BlurAlgorithm.StackBlur && !area.IsContinuous ? area.Clone() : null;
        var work = contiguous ?? area;

        switch (BlurOperation)
        {
            case BlurAlgorithm.StackBlur:
                // The OpenCV stack blur is not thread safe, running it from many threads gives wrong pixels at random
                lock (StackBlurLock)
                {
                    CvInvoke.StackBlur(work, work, size);
                }

                break;
            case BlurAlgorithm.BoxBlur:
                CvInvoke.Blur(work, work, size, Kernel.Anchor);
                break;
            case BlurAlgorithm.Pyramid:
            {
                // The result has another size than the layer, so it can not be written in place
                using var down = new Mat();
                using var up = new Mat();
                CvInvoke.PyrDown(work, down);
                CvInvoke.PyrUp(down, up);
                using var upCropped = up.Size == work.Size ? null : new Mat(up, new Rectangle(Point.Empty, work.Size));
                (upCropped ?? up).CopyTo(work);
                break;
            }
            case BlurAlgorithm.MedianBlur:
                CvInvoke.MedianBlur(work, work, (int)Size);
                break;
            case BlurAlgorithm.GaussianBlur:
                CvInvoke.GaussianBlur(work, work, size, 0);
                break;
            case BlurAlgorithm.Filter2D:
                CvInvoke.Filter2D(work, work, Kernel.GetKernel(), anchor);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        contiguous?.CopyTo(area);
        ApplyMask(original, target);

        return true;
    }

    #endregion

    #region Equality
    private bool Equals(OperationBlur other)
    {
        return BlurOperation == other.BlurOperation && Size == other.Size;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationBlur other && Equals(other);
    }

    #endregion
}
