using System.Drawing;

namespace UVtools.Core.Operations
{
    public class OperationPattern
    {
        public Anchor Anchor { get; set; }
        public Rectangle SrcRoi { get; }

        public uint ImageWidth { get; }
        public uint ImageHeight { get; }

        public ushort MarginCol { get; set; } = 0;
        public ushort MarginRow { get; set; } = 0;

        public ushort MaxMarginCol => CalculateMarginCol(MaxCols);
        public ushort MaxMarginRow => CalculateMarginRow(MaxRows);

        public ushort Cols { get; set; } = 1;
        public ushort Rows { get; set; } = 1;

        public ushort MaxCols { get; }
        public ushort MaxRows { get; }
    

        public OperationPattern(Rectangle srcRoi, uint imageWidth, uint imageHeight)
        {
            SrcRoi = srcRoi;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;

            MaxCols = (ushort) (imageWidth / srcRoi.Width);
            MaxRows = (ushort) (imageHeight / srcRoi.Height);
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
            return (ushort)((ImageWidth - SrcRoi.Width * cols) / cols);
        }

        public ushort CalculateMarginRow(ushort rows)
        {
            return (ushort)((ImageHeight - SrcRoi.Height * rows) / rows);
        }

        public Size CalculatePatternVolume => new Size(Cols * SrcRoi.Width + Cols * MarginCol, Rows * SrcRoi.Height + Rows * MarginRow);

        public Rectangle GetRoi(ushort col, ushort row)
        {
            var patternVolume = CalculatePatternVolume;

            return new Rectangle(new Point(
                (int) (col * SrcRoi.Width + col * MarginCol + (ImageWidth - patternVolume.Width) / 2), 
                (int) (row * SrcRoi.Height + row * MarginRow + (ImageHeight - patternVolume.Height) / 2)), SrcRoi.Size);
        }
    }
}
