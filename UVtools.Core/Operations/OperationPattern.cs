/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


public class OperationPattern : Operation
{
    #region Members
    private Anchor _anchor = Anchor.None;
    private ushort _colSpacing;
    private ushort _rowSpacing;
    private ushort _maxColSpacing;
    private ushort _maxRowSpacing;
    private ushort _cols = 1;
    private ushort _rows = 1;
    private ushort _maxCols;
    private ushort _maxRows;
    private bool _isWithinBoundary = true;
    #endregion

    #region Overrides
    public override bool CanMask => false;
    public override string IconClass => "fa-solid fa-th-large";
    public override string Title => "Pattern";
    public override string Description =>
        "Duplicates the model in a rectangular pattern around the build plate.\n" +
        "Note: Before perform this operation, un-rotate the layer preview to see the real orientation.";
    public override string ConfirmationText =>
        $"pattern the object across {Cols} columns and {Rows} rows?";

    public override string ProgressTitle =>
        $"Patterning the object across {Cols} columns and {Rows} rows";

    public override string ProgressAction => "Patterned layers";

    public override bool CanHaveProfiles => false;

    public override string? ValidateSpawn()
    {
        if (MaxRows < 2 && MaxCols < 2)
        {
            return "The available free volume is not enough to pattern this object.\n" +
                   "To run this tool the free space must allow at least 1 copy.";
        }

        return null;
    }

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (Cols <= 1 && Rows <= 1)
        {
            sb.AppendLine("Either columns or rows must be greater than 1.");
        }
            
        if (!ValidateBounds())
        {
            sb.AppendLine("Your parameters will put the object outside of the build plate, please adjust the margins.");
        }

        return sb.ToString();
    }

    #endregion

    #region Properties
    public Anchor Anchor
    {
        get => _anchor;
        set => RaiseAndSetIfChanged(ref _anchor, value);
    }

    public uint ImageWidth { get; private set; }
    public uint ImageHeight { get; private set; }

    public ushort Cols
    {
        get => _cols;
        set
        {
            if (!RaiseAndSetIfChanged(ref _cols, value)) return;
            RaisePropertyChanged(nameof(InfoCols));
            RaisePropertyChanged(nameof(InfoWidthStr));
            RaisePropertyChanged(nameof(InfoModelWithinBoundaryStr));
            ValidateBounds();
        }
    }

    public ushort Rows
    {
        get => _rows;
        set
        {
            if (!RaiseAndSetIfChanged(ref _rows, value)) return;
            RaisePropertyChanged(nameof(InfoRows));
            RaisePropertyChanged(nameof(InfoHeightStr));
            RaisePropertyChanged(nameof(InfoModelWithinBoundaryStr));
            ValidateBounds();
        }
    }

    public ushort MaxCols
    {
        get => _maxCols;
        set
        {
            if(!RaiseAndSetIfChanged(ref _maxCols, value)) return;
            RaisePropertyChanged(nameof(InfoCols));
            ValidateBounds();
        }
    }

    public ushort MaxRows
    {
        get => _maxRows;
        set
        {
            if (!RaiseAndSetIfChanged(ref _maxRows, value)) return;
            RaisePropertyChanged(nameof(InfoRows));
            ValidateBounds();
        }
    }

    public ushort ColSpacing
    {
        get => _colSpacing;
        set
        {
            if(!RaiseAndSetIfChanged(ref _colSpacing, value)) return;
            RaisePropertyChanged(nameof(InfoWidthStr));
            RaisePropertyChanged(nameof(InfoModelWithinBoundaryStr));
            ValidateBounds();
        }
    }

    public ushort RowSpacing
    {
        get => _rowSpacing;
        set
        {
            if (!RaiseAndSetIfChanged(ref _rowSpacing, value)) return;
            RaisePropertyChanged(nameof(InfoHeightStr));
            RaisePropertyChanged(nameof(InfoModelWithinBoundaryStr));
            ValidateBounds();
        }
    }

    public ushort MaxColSpacing
    {
        get => _maxColSpacing;
        set => RaiseAndSetIfChanged(ref _maxColSpacing, value);
    }

    public ushort MaxRowSpacing
    {
        get => _maxRowSpacing;
        set => RaiseAndSetIfChanged(ref _maxRowSpacing, value);
    }

    public string InfoCols => $"Columns: {Cols} / {MaxCols}";
    public string InfoRows => $"Rows: {Rows} / {MaxRows}";

    public string InfoWidthStr =>
        $"Width: {GetPatternVolume.Width} (Min: {ROI.Width}, Max: {ImageWidth})";

    public string InfoHeightStr =>
        $"Width: {GetPatternVolume.Height} (Min: {ROI.Height}, Max: {ImageHeight})";

    public bool IsWithinBoundary
    {
        get => _isWithinBoundary;
        set
        {
            if (!RaiseAndSetIfChanged(ref _isWithinBoundary, value)) return;
            RaisePropertyChanged(nameof(InfoModelWithinBoundaryStr));
        }
    }

    public string InfoModelWithinBoundaryStr => "Model within boundary: " + (_isWithinBoundary ? "Yes" : "No");

    public Size GetPatternVolume => new(Cols * ROI.Width + (Cols - 1) * ColSpacing, Rows * ROI.Height + (Rows - 1) * RowSpacing);
    #endregion

    #region Constructor

    public OperationPattern() { }

    public OperationPattern(FileFormat slicerFile) : base(slicerFile)
    {
        SetRoi(SlicerFile.BoundingRectangle);
    }

    public OperationPattern(FileFormat slicerFile, Rectangle srcRoi) : base(slicerFile)
    {
        SetRoi(srcRoi.IsEmpty ? SlicerFile.BoundingRectangle : srcRoi);
    }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();

        ImageWidth = SlicerFile.ResolutionX;
        ImageHeight = SlicerFile.ResolutionY;

        SetRoi(SlicerFile.BoundingRectangle);
        Fill();
    }

    #endregion

    #region Methods
    public void SetAnchor(byte value)
    {
        Anchor = (Anchor)value;
    }

    public void SetRoi(Rectangle srcRoi)
    {
        ROI = srcRoi;

        if (srcRoi.IsEmpty) return;
        MaxCols = (ushort)(ImageWidth / srcRoi.Width);
        MaxRows = (ushort)(ImageHeight / srcRoi.Height);

        MaxColSpacing = CalculateAutoColSpacing(MaxCols);
        MaxRowSpacing = CalculateAutoRowSpacing(MaxRows);
    }

    /// <summary>
    /// Fills the plate with maximum cols and rows
    /// </summary>
    public void Fill()
    {
        Cols = MaxCols;
        ColSpacing = MaxColSpacing;

        Rows = MaxRows;
        RowSpacing = MaxRowSpacing;
    }

    public ushort CalculateAutoColSpacing(ushort cols)
    {
        if (cols <= 1) return 0;
        return (ushort)((ImageWidth - ROI.Width * cols) / cols);
    }

    public ushort CalculateAutoRowSpacing(ushort rows)
    {
        if (rows <= 1) return 0;
        return (ushort)((ImageHeight - ROI.Height * rows) / rows);
    }

    public Rectangle GetRoi(ushort col, ushort row)
    {
        var patternVolume = GetPatternVolume;

        return new Rectangle(new Point(
            (int) (col * ROI.Width + col * ColSpacing + (ImageWidth - patternVolume.Width) / 2), 
            (int) (row * ROI.Height + row * RowSpacing + (ImageHeight - patternVolume.Height) / 2)), ROI.Size);
    }

    public void FillColumnSpacing()
    {
        ColSpacing = CalculateAutoColSpacing(_cols);
    }

    public void FillRowSpacing()
    {
        RowSpacing = CalculateAutoRowSpacing(_rows);
    }

    public bool ValidateBounds()
    {
        var volume = GetPatternVolume;
        return IsWithinBoundary = volume.Width <= ImageWidth && volume.Height <= ImageHeight;
    }

    public override string ToString()
    {
        var result = $"[Rows: {Rows}] [Cols: {Cols}]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            using var mat = SlicerFile[layerIndex].LayerMat;
            using var layerRoi = new Mat(mat, ROI);
            using var dstLayer = mat.NewBlank();
            for (ushort col = 0; col < Cols; col++)
            for (ushort row = 0; row < Rows; row++)
            {
                var roi = GetRoi(col, row);
                using var dstRoi = new Mat(dstLayer, roi);
                layerRoi.CopyTo(dstRoi);
            }
            //Execute(mat);
            SlicerFile[layerIndex].LayerMat = dstLayer;

            progress.LockAndIncrement();
        });

        SlicerFile.BoundingRectangle = Rectangle.Empty;

        if (Anchor == Anchor.None) return true;
        var operationMove = new OperationMove(SlicerFile, Anchor)
        {
            LayerIndexStart = LayerIndexStart,
            LayerIndexEnd = LayerIndexEnd
        };
        operationMove.Execute(progress);

        return !progress.Token.IsCancellationRequested;
    }

    /*public override bool Execute(Mat mat, params object[] arguments)
    {
        using var layerRoi = new Mat(mat, ROI);
        using var dstLayer = mat.CloneBlank();
        for (ushort col = 0; col < Cols; col++)
        for (ushort row = 0; row < Rows; row++)
        {
            var dstRoi = new Mat(dstLayer, GetRoi(col, row));
            layerRoi.CopyTo(dstRoi);
        }

        return true;
    }*/

    #endregion
}