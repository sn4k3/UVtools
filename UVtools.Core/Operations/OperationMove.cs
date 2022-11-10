/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


public class OperationMove : Operation
{
    #region Overrides

    public override bool CanMask => false;
    public override string IconClass => "fa-solid fa-arrows-alt";
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
    private uint _imageWidth;
    private uint _imageHeight;
    private Anchor _anchor = Anchor.MiddleCenter;
    private int _marginLeft;
    private int _marginTop;
    private int _marginRight;
    private int _marginBottom;
    private bool _isCutMove = true;
    private bool _isWithinBoundary;
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

    public uint ImageWidth
    {
        get => _imageWidth;
        set => RaiseAndSetIfChanged(ref _imageWidth, value);
    }

    public uint ImageHeight
    {
        get => _imageHeight;
        set => RaiseAndSetIfChanged(ref _imageHeight, value);
    }

    public Anchor Anchor
    {
        get => _anchor;
        set
        {
            RaiseAndSetIfChanged(ref _anchor, value);
            CalculateDstRoi();
        }
    }

    public int MarginLeft
    {
        get => _marginLeft;
        set
        {
            RaiseAndSetIfChanged(ref _marginLeft, value);
            CalculateDstRoi();
        }
    }

    public int MarginTop
    {
        get => _marginTop;
        set
        {
            RaiseAndSetIfChanged(ref _marginTop, value);
            CalculateDstRoi();
        }
    }

    public int MarginRight
    {
        get => _marginRight;
        set
        {
            RaiseAndSetIfChanged(ref _marginRight, value);
            CalculateDstRoi();
        }
    }

    public int MarginBottom
    {
        get => _marginBottom;
        set
        {
            RaiseAndSetIfChanged(ref _marginBottom, value);
            CalculateDstRoi();
        }
    }

    public bool IsCutMove
    {
        get => _isCutMove;
        set => RaiseAndSetIfChanged(ref _isCutMove, value);
    }

    public string LocationXStr => $"X: {DstRoi.X} / {ImageWidth - ROI.Width}";
    public string LocationYStr => $"Y: {DstRoi.Y} / {ImageHeight - ROI.Height}";

    public string LocationWidthStr => $"Width: {ROI.Width} / {ImageWidth}";
    public string LocationHeightStr => $"Height: {ROI.Height} / {ImageHeight}";

    public bool IsWithinBoundary
    {
        get => _isWithinBoundary;
        set
        {
            if (!RaiseAndSetIfChanged(ref _isWithinBoundary, value)) return;
            RaisePropertyChanged(nameof(IsWithinBoundaryStr));
        }
    }

    public string IsWithinBoundaryStr => "Model within boundary: " + (_isWithinBoundary ? "Yes" : "No");

    #endregion

    #region Constructor

    public OperationMove() { }

    public OperationMove(FileFormat slicerFile) : this(slicerFile, Anchor.MiddleCenter)
    { }

    public OperationMove(FileFormat slicerFile, Anchor anchor) : base(slicerFile)
    {
        _anchor = anchor;
    }

    public OperationMove(FileFormat slicerFile, Rectangle srcRoi, Anchor anchor = Anchor.MiddleCenter) : this(slicerFile, anchor)
    {
        if(!srcRoi.IsEmpty) ROI = srcRoi;
    }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();
        ROI = SlicerFile.BoundingRectangle;
        _imageWidth = SlicerFile.ResolutionX;
        _imageHeight = SlicerFile.ResolutionY;
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

        RaisePropertyChanged(nameof(DstRoi));
        RaisePropertyChanged(nameof(LocationXStr));
        RaisePropertyChanged(nameof(LocationYStr));
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
        if (_imageWidth == 0) _imageWidth = (uint) mat.Width;
        if (_imageHeight == 0) _imageHeight = (uint) mat.Height;

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