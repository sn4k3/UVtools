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
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolPattern : CtrlToolWindowContent
    {
        public OperationPattern Operation { get; }


        public CtrlToolPattern()
        {
            InitializeComponent();

            var roi = Program.FrmMain.ROI;
            Operation = new OperationPattern(roi.IsEmpty ? Program.SlicerFile.LayerManager.BoundingRectangle : roi, Program.FrmMain.ActualLayerImage.Size);
            SetOperation(Operation);

            if (Operation.MaxRows < 2 && Operation.MaxCols < 2)
            {
                GUIExtensions.MessageBoxInformation("Unable to pattern", 
                    "The available free volume is not enough to pattern this object.\n" +
                           "To run this tool the free space must allow at least 1 copy.");
                CanRun = false;
                return;
            }

            ExtraActionCall(this);
        }

        public override void ExtraActionCall(object sender)
        {
            if (ReferenceEquals(sender, this) || ReferenceEquals(sender, ParentToolWindow.btnActionExtra))
            {
                nmCols.Maximum = Operation.MaxCols;
                nmRows.Maximum = Operation.MaxRows;

                nmMarginCol.Value = Operation.MaxMarginCol;
                nmMarginRow.Value = Operation.MaxMarginRow;
                nmCols.Value = Operation.MaxCols;
                nmRows.Value = Operation.MaxRows;
                Operation.Fill();
                EventValueChanged(this, EventArgs.Empty);

                return;
            }

            if (ReferenceEquals(sender, ParentToolWindow.btnClearRoi))
            {
                Operation.SetRoi(Program.SlicerFile.LayerManager.BoundingRectangle);
                ExtraActionCall(this);
                return;
            }
        }

        public override bool UpdateOperation()
        {
            base.UpdateOperation();

            byte i = 0;
            foreach (var radioButton in new[]
            {
                rbAnchorTopLeft,    rbAnchorTopCenter, rbAnchorTopRight,
                rbAnchorMiddleLeft, rbAnchorMiddleCenter, rbAnchorMiddleRight,
                rbAnchorBottomLeft, rbAnchorBottomCenter, rbAnchorBottomRight,
                rbAnchorNone
            })
            {
                if (radioButton.Checked)
                {
                    Operation.Anchor = (Enumerations.Anchor)i;
                    break;
                }

                i++;
            }

            Operation.MarginCol = (ushort)nmMarginCol.Value;
            Operation.MarginRow = (ushort)nmMarginRow.Value;

            Operation.Cols = (ushort)nmCols.Value;
            Operation.Rows = (ushort)nmRows.Value;
            return true;
        }

        private void EventValueChanged(object sender, EventArgs e)
        {
            UpdateOperation();

            var insideBounds = Operation.ValidateBounds();
            ButtonOkEnabled = insideBounds && (Operation.Cols > 1 || Operation.Rows > 1);
            lbInsideBounds.Text = "Model within boundary: " + (insideBounds ? "Yes" : "No");

            lbVolumeWidth.Text = $"Width: {Operation.GetPatternVolume.Width} (Min:{Operation.ROI.Width}, Max:{Operation.ImageWidth})";
            lbVolumeHeight.Text = $"Height: {Operation.GetPatternVolume.Height} (Min:{Operation.ROI.Height}, Max:{Operation.ImageHeight})";

            lbCols.Text = $"Columns: {nmCols.Value} / {Operation.MaxCols}";
            lbRows.Text = $"Rows: {nmRows.Value} / {Operation.MaxRows}";
        }

        private void EventClick(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, btnAutoMarginCol))
            {
                UpdateOperation();
                nmMarginCol.Value = Operation.CalculateMarginCol((ushort)nmCols.Value).Clamp(0, (ushort) nmMarginCol.Maximum);
                return;
            }

            if (ReferenceEquals(sender, btnAutoMarginRow))
            {
                UpdateOperation();
                nmMarginRow.Value = Operation.CalculateMarginRow((ushort)nmRows.Value).Clamp(0, (ushort) nmMarginRow.Maximum);
                return;
            }
        }
    }
}
