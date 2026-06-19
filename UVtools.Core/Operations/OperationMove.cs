/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Emgu.CV;
using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


public partial class OperationMove : Operation
{
    #region Overrides

    public override bool CanMask => false;
    public override string IconClass => "CursorMove";
    public override string Title => "Move";
    public override string Description =>
        "Change or copy the position of the model on the build plate.\n" +
        "Note: Before perform this operation, un-rotate the layer preview to see the real orientation.";

    public override string ConfirmationText =>
        (IsCutMove ? "move" : "copy") + $" model layers {LayerIndexStart} through {LayerIndexEnd} from " +
        $"location {{X={ROI.X},Y={ROI.Y}}} to " +
        $"location {{X={DstRoi.X},Y={DstRoi.Y}}}?";

    public override string ProgressTitle =>
        (IsCutMove ? "Moving" : "Copying") +$" model to {{X={DstRoi.X},Y={DstRoi.Y}}}";

    public override string ProgressAction => (IsCutMove ? "Moved" : "Copied")+" layers";

    public override bool CanHaveProfiles => false;

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (!ValidateBounds())
        {
            sb.AppendLine("Your parameters will put the model outside of build plate. Please adjust the location and margins.");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[{ROI} -> {DstRoi}] [Cut: {IsCutMove}]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Members
    private Rectangle _dstRoi = Rectangle.Empty;
    #endregion

    #region Properties

    public Rectangle DstRoi
    {
        get
        {
            if(!_dstRoi.IsEmpty) return _dstRoi;
            CalculateDstRoi();
            return _dstRoi;
        }
    }

    [ObservableProperty]
    public partial uint ImageWidth { get; set; }

    [ObservableProperty]
    public partial uint ImageHeight { get; set; }

    [ObservableProperty]
    public partial Anchor Anchor { get; set; } = Anchor.MiddleCenter;

    [ObservableProperty]
    public partial int MarginLeft { get; set; }

    [ObservableProperty]
    public partial int MarginTop { get; set; }

    [ObservableProperty]
    public partial int MarginRight { get; set; }

    [ObservableProperty]
    public partial int MarginBottom { get; set; }

    partial void OnAnchorChanged(Anchor value) => CalculateDstRoi();
    partial void OnMarginLeftChanged(int value) => CalculateDstRoi();
    partial void OnMarginTopChanged(int value) => CalculateDstRoi();
    partial void OnMarginRightChanged(int value) => CalculateDstRoi();
    partial void OnMarginBottomChanged(int value) => CalculateDstRoi();

    [ObservableProperty]
    public partial bool IsCutMove { get; set; } = true;

    public string LocationXString => $"X: {DstRoi.X} / {ImageWidth - ROI.Width}";
    public string LocationYString => $"Y: {DstRoi.Y} / {ImageHeight - ROI.Height}";

    public string LocationWidthString => $"Width: {ROI.Width} / {ImageWidth}";
    public string LocationHeightString => $"Height: {ROI.Height} / {ImageHeight}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWithinBoundaryString))]
    public partial bool IsWithinBoundary { get; set; }

    public string IsWithinBoundaryString => "Model within boundary: " + (IsWithinBoundary ? "Yes" : "No");

    #endregion

    #region Constructor

    public OperationMove() { }

    public OperationMove(FileFormat slicerFile) : this(slicerFile, Anchor.MiddleCenter)
    { }

    public OperationMove(FileFormat slicerFile, Anchor anchor) : base(slicerFile)
    {
        Anchor = anchor;
    }

    public OperationMove(FileFormat slicerFile, Rectangle srcRoi, Anchor anchor = Anchor.MiddleCenter) : this(slicerFile, anchor)
    {
        if(!srcRoi.IsEmpty) ROI = srcRoi;
    }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();
        ROI = SlicerFile.BoundingRectangle;
        ImageWidth = SlicerFile.ResolutionX;
        ImageHeight = SlicerFile.ResolutionY;
    }

    /*public OperationMove(FileFormat slicerFile, Rectangle srcRoi, Mat mat, Anchor anchor = Anchor.MiddleCenter) : this(slicerFile, srcRoi, anchor)
    {
        ImageWidth = (uint) mat.Width;
        ImageHeight = (uint) mat.Height;
    }*/

    #endregion

    #region Methods
    public void CalculateDstRoi()
    {
        _dstRoi.Size = ROI.Size;

        _dstRoi.Location = Anchor switch
        {
            Anchor.TopLeft => new Point(0, 0),
            Anchor.TopCenter => new Point((int) (ImageWidth / 2 - ROI.Width / 2), 0),
            Anchor.TopRight => new Point((int) (ImageWidth - ROI.Width), 0),
            Anchor.MiddleLeft => new Point(0, (int) (ImageHeight / 2 - ROI.Height / 2)),
            Anchor.MiddleCenter => new Point((int) (ImageWidth / 2 - ROI.Width / 2), (int) (ImageHeight / 2 - ROI.Height / 2)),
            Anchor.MiddleRight => new Point((int) (ImageWidth - ROI.Width), (int) (ImageHeight / 2 - ROI.Height / 2)),
            Anchor.BottomLeft => new Point(0, (int) (ImageHeight - ROI.Height)),
            Anchor.BottomCenter => new Point((int) (ImageWidth / 2 - ROI.Width / 2), (int) (ImageHeight - ROI.Height)),
            Anchor.BottomRight => new Point((int) (ImageWidth - ROI.Width), (int) (ImageHeight - ROI.Height)),
            _ => throw new ArgumentOutOfRangeException()
        };

        _dstRoi.X += MarginLeft;
        _dstRoi.X -= MarginRight;
        _dstRoi.Y += MarginTop;
        _dstRoi.Y -= MarginBottom;

        IsWithinBoundary = !(_dstRoi.IsEmpty || _dstRoi.X < 0 || _dstRoi.Y < 0 ||
                             _dstRoi.Width == 0 || _dstRoi.Right > ImageWidth ||
                             _dstRoi.Height == 0 || _dstRoi.Bottom > ImageHeight);

        OnPropertyChanged(nameof(DstRoi));
        OnPropertyChanged(nameof(LocationXString));
        OnPropertyChanged(nameof(LocationYString));
    }


    public void Reset()
    {
        MarginLeft = MarginTop = MarginRight = MarginBottom = 0;
        Anchor = Anchor.MiddleCenter;
        IsCutMove = true;
    }


    public void SetAnchor(byte value)
    {
        Anchor = (Anchor)value;
    }

    public void SetAnchor(object value)
    {
        Anchor = (Anchor)Convert.ToByte(value);
    }


    public bool ValidateBounds()
    {
        CalculateDstRoi();
        return IsWithinBoundary;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        if (ROI.IsEmpty) ROI = SlicerFile.GetBoundingRectangle(progress);
        CalculateDstRoi();

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

        SlicerFile.BoundingRectangle = Rectangle.Empty;

        return !progress.Token.IsCancellationRequested;
    }

    public override bool Execute(Mat mat, params object[]? arguments)
    {
        if (ImageWidth == 0) ImageWidth = (uint) mat.Width;
        if (ImageHeight == 0) ImageHeight = (uint) mat.Height;

        using var srcRoi = new Mat(mat, ROI);
        using var dstRoi = new Mat(mat, DstRoi);
        if (IsCutMove)
        {
            using var targetRoi = srcRoi.Clone();
            srcRoi.SetTo(new MCvScalar(0));
            targetRoi.CopyTo(dstRoi);
        }
        else
        {
            srcRoi.CopyTo(dstRoi);
        }

        return true;
    }

    #endregion
}
