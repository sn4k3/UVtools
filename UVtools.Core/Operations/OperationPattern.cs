/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.Drawing;

namespace UVtools.Core.Operations
{
    public class OperationPattern
    {
        public Anchor Anchor { get; set; }
        public Rectangle SrcRoi { get; }

        public uint ImageWidth { get; }
        public uint ImageHeight { get; }

        public ushort MarginCol { get; set; }
        public ushort MarginRow { get; set; }

        public ushort MaxMarginCol { get; }
        public ushort MaxMarginRow { get; }

        public ushort Cols { get; set; } = 1;
        public ushort Rows { get; set; } = 1;

        public ushort MaxCols { get; }
        public ushort MaxRows { get; }

        public Size GetPatternVolume => new Size(Cols * SrcRoi.Width + (Cols - 1) * MarginCol, Rows * SrcRoi.Height + (Rows - 1) * MarginRow);


        public OperationPattern(Rectangle srcRoi, uint imageWidth, uint imageHeight)
        {
            SrcRoi = srcRoi;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;

            MaxCols = (ushort) (imageWidth / srcRoi.Width);
            MaxRows = (ushort) (imageHeight / srcRoi.Height);

            MaxMarginCol = CalculateMarginCol(MaxCols);
            MaxMarginRow = CalculateMarginRow(MaxRows);
        }


        /*public void CalculateDstRoi()
        {
            _dstRoi.Size = SrcRoi.Size;

            switch (Anchor)
            {
                case Anchor.TopLeft:
                    _dstRoi.Location = new Point(0, 0);
                    break;
                case Anchor.TopCenter:
                    _dstRoi.Location = new Point((int)(ImageWidth / 2 - SrcRoi.Width / 2), 0);
                    break;
                case Anchor.TopRight:
                    _dstRoi.Location = new Point((int)(ImageWidth - SrcRoi.Width), 0);
                    break;
                case Anchor.MiddleLeft:
                    _dstRoi.Location = new Point(0, (int)(ImageHeight / 2 - SrcRoi.Height / 2));
                    break;
                case Anchor.MiddleCenter:
                    _dstRoi.Location = new Point((int)(ImageWidth / 2 - SrcRoi.Width / 2), (int)(ImageHeight / 2 - SrcRoi.Height / 2));
                    break;
                case Anchor.MiddleRight:
                    _dstRoi.Location = new Point((int)(ImageWidth - SrcRoi.Width), (int)(ImageHeight / 2 - SrcRoi.Height / 2));
                    break;
                case Anchor.BottomLeft:
                    _dstRoi.Location = new Point(0, (int)(ImageHeight - SrcRoi.Height));
                    break;
                case Anchor.BottomCenter:
                    _dstRoi.Location = new Point((int)(ImageWidth / 2 - SrcRoi.Width / 2), (int)(ImageHeight - SrcRoi.Height));
                    break;
                case Anchor.BottomRight:
                    _dstRoi.Location = new Point((int)(ImageWidth - SrcRoi.Width), (int)(ImageHeight - SrcRoi.Height));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _dstRoi.X += MarginLeft;
            _dstRoi.X -= MarginRight;
            _dstRoi.Y += MarginTop;
            _dstRoi.Y -= MarginBottom;
        }*/




        /// <summary>
        /// Fills the plate with maximum cols and rows
        /// </summary>
        public void Fill()
        {
            Cols = MaxCols;
            MarginCol = MaxMarginCol;

            Rows = MaxRows;
            MarginRow = MaxMarginRow;
        }

        public ushort CalculateMarginCol(ushort cols)
        {
            if (cols <= 1) return 0;
            return (ushort)((ImageWidth - SrcRoi.Width * cols) / cols);
        }

        public ushort CalculateMarginRow(ushort rows)
        {
            if (rows <= 1) return 0;
            return (ushort)((ImageHeight - SrcRoi.Height * rows) / rows);
        }

        public Rectangle GetRoi(ushort col, ushort row)
        {
            var patternVolume = GetPatternVolume;

            return new Rectangle(new Point(
                (int) (col * SrcRoi.Width + col * MarginCol + (ImageWidth - patternVolume.Width) / 2), 
                (int) (row * SrcRoi.Height + row * MarginRow + (ImageHeight - patternVolume.Height) / 2)), SrcRoi.Size);
        }
    }
}
