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
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using EmguExtensions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


public partial class OperationPattern : Operation
{
    #region Members
    #endregion

    #region Overrides
    public override bool CanMask => false;
    public override string IconClass => "TableLarge";
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
    [ObservableProperty]
    public partial Anchor Anchor { get; set; } = Anchor.None;

    public uint ImageWidth { get; private set; }
    public uint ImageHeight { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InfoCols))]
    [NotifyPropertyChangedFor(nameof(InfoWidthString))]
    [NotifyPropertyChangedFor(nameof(InfoModelWithinBoundaryString))]
    public partial ushort Cols { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InfoRows))]
    [NotifyPropertyChangedFor(nameof(InfoHeightString))]
    [NotifyPropertyChangedFor(nameof(InfoModelWithinBoundaryString))]
    public partial ushort Rows { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InfoCols))]
    public partial ushort MaxCols { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InfoRows))]
    public partial ushort MaxRows { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InfoWidthString))]
    [NotifyPropertyChangedFor(nameof(InfoModelWithinBoundaryString))]
    public partial ushort ColSpacing { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InfoHeightString))]
    [NotifyPropertyChangedFor(nameof(InfoModelWithinBoundaryString))]
    public partial ushort RowSpacing { get; set; }

    partial void OnColsChanged(ushort value) => ValidateBounds();
    partial void OnRowsChanged(ushort value) => ValidateBounds();
    partial void OnMaxColsChanged(ushort value) => ValidateBounds();
    partial void OnMaxRowsChanged(ushort value) => ValidateBounds();
    partial void OnColSpacingChanged(ushort value) => ValidateBounds();
    partial void OnRowSpacingChanged(ushort value) => ValidateBounds();

    [ObservableProperty]
    public partial ushort MaxColSpacing { get; set; }

    [ObservableProperty]
    public partial ushort MaxRowSpacing { get; set; }

    public string InfoCols => $"Columns: {Cols} / {MaxCols}";
    public string InfoRows => $"Rows: {Rows} / {MaxRows}";

    public string InfoWidthString =>
        $"Width: {GetPatternVolume.Width} (Min: {ROI.Width}, Max: {ImageWidth})";

    public string InfoHeightString =>
        $"Width: {GetPatternVolume.Height} (Min: {ROI.Height}, Max: {ImageHeight})";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InfoModelWithinBoundaryString))]
    public partial bool IsWithinBoundary { get; set; } = true;

    public string InfoModelWithinBoundaryString => "Model within boundary: " + (IsWithinBoundary ? "Yes" : "No");

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

    public void SetAnchor(object value)
    {
        Anchor = (Anchor)Convert.ToByte(value);
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
        ColSpacing = CalculateAutoColSpacing(Cols);
    }

    public void FillRowSpacing()
    {
        RowSpacing = CalculateAutoRowSpacing(Rows);
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
            progress.PauseIfRequested();
            using var mat = SlicerFile[layerIndex].LayerMat;
            using var layerRoi = new Mat(mat, ROI);
            using var dstLayer = mat.NewZeros();
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
