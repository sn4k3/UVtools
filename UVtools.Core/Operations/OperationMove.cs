/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationMove : Operation
    {
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
        private uint _imageWidth;
        private uint _imageHeight;
        private Enumerations.Anchor _anchor = Enumerations.Anchor.MiddleCenter;
        private int _marginLeft;
        private int _marginTop;
        private int _marginRight;
        private int _marginBottom;
        private bool _isCutMove = true;
        private bool _isWithinBoundary;

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

            IsWithinBoundary = !(DstRoi.IsEmpty || DstRoi.X < 0 || DstRoi.Y < 0 ||
                                 DstRoi.Width == 0 || DstRoi.Right > ImageWidth ||
                                 DstRoi.Height == 0 || DstRoi.Bottom > ImageHeight);

            RaisePropertyChanged(nameof(DstRoi));
            RaisePropertyChanged(nameof(LocationXStr));
            RaisePropertyChanged(nameof(LocationYStr));
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

        public Enumerations.Anchor Anchor
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

        public OperationMove()
        {
        }

        public OperationMove(Rectangle srcRoi, uint imageWidth, uint imageHeight, Enumerations.Anchor anchor = Enumerations.Anchor.MiddleCenter)
        {
            ROI = srcRoi;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
            Anchor = anchor;
        }

        public OperationMove(Rectangle srcRoi, Size resolution, Enumerations.Anchor anchor = Enumerations.Anchor.MiddleCenter)
        {
            ROI = srcRoi;
            ImageWidth = (uint) resolution.Width;
            ImageHeight = (uint) resolution.Height;
            Anchor = anchor;
        }

        public void Reset()
        {
            MarginLeft = MarginTop = MarginRight = MarginBottom = 0;
            Anchor = Enumerations.Anchor.MiddleCenter;
            IsCutMove = true;
        }


        public void SetAnchor(byte value)
        {
            Anchor = (Enumerations.Anchor)value;
        }



        public bool ValidateBounds()
        {
            CalculateDstRoi();
            return IsWithinBoundary;
        }

        public override string ToString()
        {
            var result = $"[{ROI} -> {DstRoi}] [Cut: {IsCutMove}]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }
    }
}
