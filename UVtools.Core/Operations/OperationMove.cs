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
    public class OperationMove : Operation
    {
        public override string Title => "Move";
        public override string Description =>
            "Change or copy the position of the model on the build plate.";

        public override string ConfirmationText =>
            (IsCutMove ? "move" : "copy") + $" model layers {LayerIndexStart} through {LayerIndexEnd} from " +
            $"location {{X={ROI.X},Y={ROI.Y}}} to " +
            $"location {{X={DstRoi.X},Y={DstRoi.Y}}}?";

        public override string ProgressTitle =>
            (IsCutMove ? "Moving" : "Copying") +$" model to {{X={DstRoi.X},Y={DstRoi.Y}}}";

        public override string ProgressAction => (IsCutMove ? "Moved" : "Copied")+" layers";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();

            if (!ValidateBounds())
            {
                sb.AppendLine("Your parameters will put the model outside of build plate. Please adjust the location and margins.");
            }

            return new StringTag(sb.ToString());
        }

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
            _dstRoi.Size = ROI.Size;

            switch (Anchor)
            {
                case Enumerations.Anchor.TopLeft:
                    _dstRoi.Location = new Point(0, 0);
                    break;
                case Enumerations.Anchor.TopCenter:
                    _dstRoi.Location = new Point((int)(ImageWidth / 2 - ROI.Width / 2), 0);
                    break;
                case Enumerations.Anchor.TopRight:
                    _dstRoi.Location = new Point((int)(ImageWidth - ROI.Width), 0);
                    break;
                case Enumerations.Anchor.MiddleLeft:
                    _dstRoi.Location = new Point(0, (int)(ImageHeight / 2 - ROI.Height / 2));
                    break;
                case Enumerations.Anchor.MiddleCenter:
                //case Anchor.None:
                    _dstRoi.Location = new Point((int)(ImageWidth / 2 - ROI.Width / 2), (int)(ImageHeight / 2 - ROI.Height / 2));
                    break;
                case Enumerations.Anchor.MiddleRight:
                    _dstRoi.Location = new Point((int)(ImageWidth - ROI.Width), (int)(ImageHeight / 2 - ROI.Height / 2));
                    break;
                case Enumerations.Anchor.BottomLeft:
                    _dstRoi.Location = new Point(0, (int)(ImageHeight - ROI.Height));
                    break;
                case Enumerations.Anchor.BottomCenter:
                    _dstRoi.Location = new Point((int)(ImageWidth / 2 - ROI.Width / 2), (int)(ImageHeight - ROI.Height));
                    break;
                case Enumerations.Anchor.BottomRight:
                    _dstRoi.Location = new Point((int)(ImageWidth - ROI.Width), (int)(ImageHeight - ROI.Height));
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

        public Enumerations.Anchor Anchor { get; set; }

        public int MarginLeft { get; set; } = 0;
        public int MarginTop { get; set; } = 0;
        public int MarginRight { get; set; } = 0;
        public int MarginBottom { get; set; } = 0;

        public bool IsCutMove { get; set; } = true;

        public OperationMove()
        {
        }

        public OperationMove(Rectangle srcRoi, uint imageWidth = 0, uint imageHeight = 0, Enumerations.Anchor anchor = Enumerations.Anchor.MiddleCenter)
        {
            ROI = srcRoi;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
            Anchor = anchor;
        }



        public bool ValidateBounds()
        {
            CalculateDstRoi();
            if (DstRoi.X < 0) return false;
            if (DstRoi.Y < 0) return false;
            if (DstRoi.Right > ImageWidth) return false;
            if (DstRoi.Bottom > ImageHeight) return false;

            return true;
        }
    }
}
