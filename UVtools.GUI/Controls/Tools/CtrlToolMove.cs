/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolMove : CtrlToolWindowContent
    {
        public OperationMove Operation { get; }


        public CtrlToolMove()
        {
            InitializeComponent();
            Operation = new OperationMove(Program.SlicerFile.LayerManager.BoundingRectangle, (uint)Program.FrmMain.ActualLayerImage.Width,
                (uint)Program.FrmMain.ActualLayerImage.Height);
            SetOperation(Operation);

            lbVolumeWidth.Text = $"Volume Width: {Operation.SrcRoi.Width} / {Operation.ImageWidth}";
            lbVolumeHeight.Text = $"Volume Height: {Operation.SrcRoi.Height} / {Operation.ImageHeight}";

            ResetDefaults();
        }

        public override void ResetDefaults()
        {
            nmMarginLeft.Value = 0;
            nmMarginTop.Value = 0;
            nmMarginRight.Value = 0;
            nmMarginBottom.Value = 0;
            rbAnchorMiddleCenter.Checked = true;
            EventValueChanged(this, EventArgs.Empty);
        }

        private void EventValueChanged(object sender, EventArgs e)
        {
            UpdateOperation();
            var insideBounds = ButtonOkEnabled = Operation.ValidateBounds();
            lbInsideBounds.Text = "Inside Bounds: " + (insideBounds ? "Yes" : "No");
            lbPlacementX.Text = $"Placement X: {Operation.DstRoi.X} / {Operation.ImageWidth - Operation.SrcRoi.Width}";
            lbPlacementY.Text = $"Placement Y: {Operation.DstRoi.Y} / {Operation.ImageHeight - Operation.SrcRoi.Height}";
        }

        public override void UpdateOperation()
        {
            base.UpdateOperation();
            byte i = 0;
            foreach (var radioButton in new[]
            {
                rbAnchorTopLeft,    rbAnchorTopCenter, rbAnchorTopRight,
                rbAnchorMiddleLeft, rbAnchorMiddleCenter, rbAnchorMiddleRight,
                rbAnchorBottomLeft, rbAnchorBottomCenter, rbAnchorBottomRight
            })
            {
                if (radioButton.Checked)
                {
                    Operation.Anchor = (Anchor)i;
                    break;
                }

                i++;
            }

            Operation.MarginLeft = (int)nmMarginLeft.Value;
            Operation.MarginTop = (int)nmMarginTop.Value;
            Operation.MarginRight = (int)nmMarginRight.Value;
            Operation.MarginBottom = (int)nmMarginBottom.Value;
        }
    }
}
