using System;
using System.Drawing;

namespace UVtools.Core.Operations
{
    public enum Anchor : byte
    {
        TopLeft,    TopCenter,    TopRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        BottomLeft, BottomCenter, BottomRight,
        None
    }

    public class OperationMove
    {
        public Rectangle SrcRoi { get; set; }

        private Rectangle _dstRoi = Rectangle.Empty;
        public Rectangle DstRoi
        {
            get
            {
                if(!_dstRoi.IsEmpty) return _dstRoi;
                CalculateDstRoi();

                return _dstRoi;
            }
        }

        public void CalculateDstRoi()
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
        }


        public uint ImageWidth { get; set; }
        public uint ImageHeight { get; set; }

        public Anchor Anchor { get; set; }

        public int MarginLeft { get; set; } = 0;
        public int MarginTop { get; set; } = 0;
        public int MarginRight { get; set; } = 0;
        public int MarginBottom { get; set; } = 0;

        public OperationMove(Rectangle srcRoi, uint imageWidth = 0, uint imageHeight = 0, Anchor anchor = Anchor.MiddleCenter)
        {
            SrcRoi = srcRoi;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
            Anchor = anchor;
        }
    }
}
