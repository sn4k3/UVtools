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

namespace UVtools.GUI.Forms
{
    public partial class FrmMutationMove : Form
    {
        #region Properties

        private Mutation Mutation { get; }

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

        private readonly OperationMove _operationMove;
        
        public OperationMove OperationMove
        {
            get
            {
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
                        _operationMove.Anchor = (Anchor) i;
                        break;
                    }

                    i++;
                }

                _operationMove.MarginLeft = (int) nmMarginLeft.Value;
                _operationMove.MarginTop = (int)nmMarginTop.Value;
                _operationMove.MarginRight = (int)nmMarginRight.Value;
                _operationMove.MarginBottom = (int)nmMarginBottom.Value;

                return _operationMove;
            }
        }

        #endregion

        #region Constructors
        public FrmMutationMove(Mutation mutation, Rectangle srcRoi, uint imageWidth = 0, uint imageHeight = 0)
        {
            InitializeComponent();
            _operationMove = new OperationMove(srcRoi, imageWidth, imageHeight);
            Mutation = mutation;
            DialogResult = DialogResult.Cancel;

            Text = $"Mutate: {mutation.MenuName}";
            lbDescription.Text = Mutation.Description;

            nmLayerRangeEnd.Value = Program.SlicerFile.LayerCount-1;

            OperationMove.SrcRoi = srcRoi;
            OperationMove.ImageWidth = imageWidth;
            OperationMove.ImageHeight = imageHeight;

            lbVolumeWidth.Text = $"Volume Width: {srcRoi.Width} / {imageWidth}";
            lbVolumeHeight.Text = $"Volume Height: {srcRoi.Height} / {imageHeight}";
            EventValueChanged(this, null);
        }
        #endregion

        #region Overrides
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Enter)
            {
                btnMutate.PerformClick();
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

            if (ReferenceEquals(sender, btnResetDefaults))
            {
                nmMarginLeft.Value = 0;
                nmMarginTop.Value = 0;
                nmMarginRight.Value = 0;
                nmMarginBottom.Value = 0;
                rbAnchorMiddleCenter.Checked = true;
                return;
            }

            if (ReferenceEquals(sender, btnMutate))
            {
                if (!btnMutate.Enabled) return;
                if (LayerRangeStart > LayerRangeEnd)
                {
                    MessageBox.Show("Layer range start can't be higher than layer end.\nPlease fix and try again.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    nmLayerRangeStart.Select();
                    return;
                }

                if (!ValidateBounds())
                {
                    MessageBox.Show("Your parameters will put the object out of build plate, please adjust the margins", Text, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }


                if (MessageBox.Show($"Are you sure you want to {Mutation.Mutate}?", Text, MessageBoxButtons.YesNo,
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
            var insideBounds = btnMutate.Enabled = ValidateBounds();
            lbInsideBounds.Text = "Inside Bounds: "+(insideBounds ? "Yes" : "No");
            lbPlacementX.Text = $"Placement X: {OperationMove.DstRoi.X} / {OperationMove.ImageWidth - OperationMove.SrcRoi.Width}";
            lbPlacementY.Text = $"Placement Y: {OperationMove.DstRoi.Y} / {OperationMove.ImageHeight - OperationMove.SrcRoi.Height}";
        }

        #endregion

        #region Methods

        public bool ValidateBounds()
        {
            OperationMove.CalculateDstRoi();
            if (OperationMove.DstRoi.X < 0) return false;
            if (OperationMove.DstRoi.Y < 0) return false;
            if (OperationMove.DstRoi.Right > OperationMove.ImageWidth) return false;
            if (OperationMove.DstRoi.Bottom > OperationMove.ImageHeight) return false;

            return true;
        }
        #endregion

        
    }
}
