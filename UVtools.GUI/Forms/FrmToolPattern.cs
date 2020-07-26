/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Windows.Forms;
using UVtools.Core;
using UVtools.Core.Operations;

namespace UVtools.GUI.Forms
{
    public partial class FrmToolPattern : Form
    {
        #region Properties

        public uint LayerRangeStart
        {
            get => (uint) nmLayerRangeStart.Value;
            set => nmLayerRangeStart.Value = value;
        }

        public uint LayerRangeEnd
        {
            get => (uint)Math.Min(nmLayerRangeEnd.Value, Program.SlicerFile.LayerCount-1);
            set => nmLayerRangeEnd.Value = value;
        }

        private readonly OperationPattern _operation;
        
        public OperationPattern OperationPattern
        {
            get
            {
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
                        _operation.Anchor = (Anchor)i;
                        break;
                    }

                    i++;
                }

                _operation.MarginCol = (ushort)nmMarginCol.Value;
                _operation.MarginRow = (ushort)nmMarginRow.Value;

                _operation.Cols = (ushort) nmCols.Value;
                _operation.Rows = (ushort) nmRows.Value;

                return _operation;
            }
        }

        #endregion

        #region Constructors
        public FrmToolPattern(Rectangle srcRoi, uint imageWidth, uint imageHeight) : this(new OperationPattern(srcRoi, imageWidth, imageHeight)) {}

        public FrmToolPattern(OperationPattern operation)
        {
            InitializeComponent();
            _operation = operation;
            DialogResult = DialogResult.Cancel;

            nmLayerRangeEnd.Value = Program.SlicerFile.LayerCount - 1;

            EventClick(btnResetDefaults, null);
            //EventValueChanged(this, null);

            nmCols.Maximum = _operation.MaxCols;
            nmRows.Maximum = _operation.MaxRows;
        }
        #endregion

        #region Overrides
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Enter)
            {
                btnPattern.PerformClick();
                e.Handled = true;
                return;
            }

            if ((ModifierKeys & Keys.Shift) == Keys.Shift && (ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (e.KeyCode == Keys.A)
                {
                    btnLayerRangeAllLayers.PerformClick();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.C)
                {
                    btnLayerRangeCurrentLayer.PerformClick();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.B)
                {
                    btnLayerRangeBottomLayers.PerformClick();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.N)
                {
                    btnLayerRangeNormalLayers.PerformClick();
                    e.Handled = true;
                    return;
                }
            }
        }

        #endregion

        #region Events
        private void EventClick(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, btnLayerRangeAllLayers))
            {
                nmLayerRangeStart.Value = 0;
                nmLayerRangeEnd.Value = Program.SlicerFile.LayerCount-1;
                return;
            }

            if (ReferenceEquals(sender, btnLayerRangeCurrentLayer))
            {
                nmLayerRangeStart.Value = Program.FrmMain.ActualLayer;
                nmLayerRangeEnd.Value = Program.FrmMain.ActualLayer;
                return;
            }

            if (ReferenceEquals(sender, btnLayerRangeBottomLayers))
            {
                nmLayerRangeStart.Value = 0;
                nmLayerRangeEnd.Value = Program.SlicerFile.InitialLayerCount-1;
                return;
            }

            if (ReferenceEquals(sender, btnLayerRangeNormalLayers))
            {
                nmLayerRangeStart.Value = Program.SlicerFile.InitialLayerCount - 1;
                nmLayerRangeEnd.Value = Program.SlicerFile.LayerCount - 1;
                return;
            }

            if (ReferenceEquals(sender, btnAutoMarginCol))
            {
                nmMarginCol.Value = _operation.CalculateMarginCol((ushort) nmCols.Value);
                return;
            }

            if (ReferenceEquals(sender, btnAutoMarginRow))
            {
                nmMarginRow.Value = _operation.CalculateMarginRow((ushort)nmRows.Value);
                return;
            }

            if (ReferenceEquals(sender, btnResetDefaults))
            {
                nmMarginCol.Value = _operation.MaxMarginCol;
                nmMarginRow.Value = _operation.MaxMarginRow;
                nmCols.Value = _operation.MaxCols;
                nmRows.Value = _operation.MaxRows;
                _operation.Fill();
                return;
            }

            if (ReferenceEquals(sender, btnPattern))
            {
                if (!btnPattern.Enabled) return;
                if (LayerRangeStart > LayerRangeEnd)
                {
                    MessageBox.Show("Layer range start can't be higher than layer end.\nPlease fix and try again.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    nmLayerRangeStart.Select();
                    return;
                }

                if (nmCols.Value == 1 && nmRows.Value == 1)
                {
                    MessageBox.Show("Either columns or rows must be greater than 1 to be able to run this tool", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    nmLayerRangeStart.Select();
                    return;
                }

                if (!ValidateBounds())
                {
                    MessageBox.Show("Your parameters will put the object out of build plate, please adjust the margins", Text, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }


                if (MessageBox.Show($"Are you sure you want to Pattern?", Text, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }

                return;
            }

            if (ReferenceEquals(sender, btnCancel))
            {
                DialogResult = DialogResult.Cancel;
                return;
            }
        }

        private void EventValueChanged(object sender, EventArgs e)
        {
            var insideBounds = btnPattern.Enabled = ValidateBounds();
            lbInsideBounds.Text = "Inside Bounds: " + (insideBounds ? "Yes" : "No");

            lbVolumeWidth.Text = $"Volume/Pattern Width: {_operation.SrcRoi.Width} / {_operation.GetPatternVolume.Width} / {_operation.ImageWidth}";
            lbVolumeHeight.Text = $"Volume/Pattern Height: {_operation.SrcRoi.Height} / {_operation.GetPatternVolume.Height} / {_operation.ImageHeight}";
            
            lbCols.Text = $"Columns: {nmCols.Value} / {_operation.MaxCols}";
            lbRows.Text = $"Rows: {nmRows.Value} / {_operation.MaxRows}";
        }

        #endregion

        #region Methods

        public bool ValidateBounds()
        {
            var volume = OperationPattern.GetPatternVolume;
            if (volume.Width > _operation.ImageWidth) return false;
            if (volume.Height > _operation.ImageHeight) return false;

            return true;
        }
        #endregion

        
    }
}
