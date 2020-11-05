/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Text;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationPattern : Operation
    {
        private Enumerations.Anchor _anchor = Enumerations.Anchor.None;
        private ushort _colSpacing;
        private ushort _rowSpacing;
        private ushort _maxColSpacing;
        private ushort _maxRowSpacing;
        private ushort _cols = 1;
        private ushort _rows = 1;
        private ushort _maxCols;
        private ushort _maxRows;
        private bool _isWithinBoundary = true;

        #region Overrides

        public override string Title => "Pattern";
        public override string Description =>
            "Duplicates the model in a rectangular pattern around the build plate.";
        public override string ConfirmationText =>
            $"pattern the object across {Cols} columns and {Rows} rows?";

        public override string ProgressTitle =>
            $"Patterning the object across {Cols} columns and {Rows} rows";

        public override string ProgressAction => "Patterned layers";

        public override bool CanHaveProfiles => false;

        public override StringTag Validate(params object[] parameters)
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

            return new StringTag(sb.ToString());
        }

        #endregion

        public Enumerations.Anchor Anchor
        {
            get => _anchor;
            set => RaiseAndSetIfChanged(ref _anchor, value);
        }

        public uint ImageWidth { get; }
        public uint ImageHeight { get; }

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

        public Size GetPatternVolume => new Size(Cols * ROI.Width + (Cols - 1) * ColSpacing, Rows * ROI.Height + (Rows - 1) * RowSpacing);

        public OperationPattern()
        {
        }

        public OperationPattern(Rectangle srcRoi, Size resolution)
        {
            ImageWidth = (uint) resolution.Width;
            ImageHeight = (uint) resolution.Height;

            SetRoi(srcRoi);
            Fill();
        }

        public void SetAnchor(byte value)
        {
            Anchor = (Enumerations.Anchor)value;
        }

        public void SetRoi(Rectangle srcRoi)
        {
            ROI = srcRoi;

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
    }
}
