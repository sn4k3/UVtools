/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Windows.Forms;
using UVtools.Core;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolMove : CtrlToolWindowContent
    {
        public OperationMove Operation { get; }


        public CtrlToolMove()
        {
            InitializeComponent();

            var roi = Program.FrmMain.ROI;

            Operation = new OperationMove(roi.IsEmpty ? Program.SlicerFile.LayerManager.BoundingRectangle : roi, (uint)Program.FrmMain.ActualLayerImage.Width,
                (uint)Program.FrmMain.ActualLayerImage.Height);
            SetOperation(Operation);

            cbMoveType.SelectedIndex = 0;
            ExtraActionCall(this);
        }

        public override void ExtraActionCall(object sender)
        {
            if (ReferenceEquals(sender, this) || ReferenceEquals(sender, ParentToolWindow.btnActionExtra))
            {
                lbVolumeWidth.Text = $"Width: {Operation.ROI.Width} / {Operation.ImageWidth}";
                lbVolumeHeight.Text = $"Height: {Operation.ROI.Height} / {Operation.ImageHeight}";
                
                nmMarginLeft.Value = 0;
                nmMarginTop.Value = 0;
                nmMarginRight.Value = 0;
                nmMarginBottom.Value = 0;
                rbAnchorMiddleCenter.Checked = true;
                EventValueChanged(this, EventArgs.Empty);

                return;
            }

            if (ReferenceEquals(sender, ParentToolWindow.btnClearRoi))
            {
                Operation.ROI = Program.SlicerFile.LayerManager.BoundingRectangle;
                ExtraActionCall(this);
                return;
            }
        }

        private void EventValueChanged(object sender, EventArgs e)
        {
            UpdateOperation();
            var insideBounds = ButtonOkEnabled = Operation.ValidateBounds();
            lbInsideBounds.Text = "Model within boundary: " + (insideBounds ? "Yes" : "No");
            lbPlacementX.Text = $"X: {Operation.DstRoi.X} / {Operation.ImageWidth - Operation.ROI.Width}";
            lbPlacementY.Text = $"Y: {Operation.DstRoi.Y} / {Operation.ImageHeight - Operation.ROI.Height}";
        }

        public override bool UpdateOperation()
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
                    Operation.Anchor = (Enumerations.Anchor)i;
                    break;
                }

                i++;
            }

            Operation.MarginLeft = (int)nmMarginLeft.Value;
            Operation.MarginTop = (int)nmMarginTop.Value;
            Operation.MarginRight = (int)nmMarginRight.Value;
            Operation.MarginBottom = (int)nmMarginBottom.Value;
            Operation.IsCutMove = cbMoveType.SelectedIndex == 0;
            return true;
        }
    }
}
