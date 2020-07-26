/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Windows.Forms;

namespace UVtools.GUI.Forms
{
    public partial class FrmRepairLayers : Form
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

        public uint ClosingIterations
        {
            get => (uint) numClosingIterations.Value;
            set => numClosingIterations.Value = value;
        }

        public uint OpeningIterations
        {
            get => (uint)numOpeningIterations.Value;
            set => numOpeningIterations.Value = value;
        }

        public byte RemoveIslandsBelowEqualPixels
        {
            get => (byte)nmRemoveIslandsBelowEqualPixels.Value;
            set => nmRemoveIslandsBelowEqualPixels.Value = value;
        }

        public bool RepairIslands
        {
            get => cbRepairIslands.Checked;
            set => cbRepairIslands.Checked = value;
        }

        public bool RemoveEmptyLayers
        {
            get => cbRemoveEmptyLayers.Checked;
            set => cbRemoveEmptyLayers.Checked = value;
        }

        public bool RepairResinTraps
        {
            get => cbRepairResinTraps.Checked;
            set => cbRepairResinTraps.Checked = value;
        }
        #endregion

        #region Constructors
        public FrmRepairLayers()
        {
            InitializeComponent();
            DialogResult = DialogResult.Cancel;

            
            ClosingIterations = Properties.Settings.Default.LayerRepairDefaultClosingIterations;
            OpeningIterations = Properties.Settings.Default.LayerRepairDefaultOpeningIterations;
            RepairIslands = Properties.Settings.Default.LayerRepairLayersIslands;
            RemoveEmptyLayers = Properties.Settings.Default.LayerRepairRemoveEmptyLayers;
            RepairResinTraps = Properties.Settings.Default.LayerRepairResinTraps;
            nmRemoveIslandsBelowEqualPixels.Value = Properties.Settings.Default.LayerRepairRemoveIslandsBelowEqualPixelsDefault;
            numClosingIterations.Select();

            nmLayerRangeEnd.Value = Program.SlicerFile.LayerCount-1;

        }
        #endregion

        #region Overrides
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Enter)
            {
                btnRepair.PerformClick();
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
        private void ItemClicked(object sender, EventArgs e)
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

            if (ReferenceEquals(sender, btnRepair))
            {
                if (!btnRepair.Enabled) return;
                if (LayerRangeStart > LayerRangeEnd)
                {
                    MessageBox.Show("Layer range start can't be higher than layer end.\nPlease fix and try again.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    nmLayerRangeStart.Select();
                    return;
                }

                if (OpeningIterations == 0 && ClosingIterations == 0)
                {
                    MessageBox.Show("Any of opening and closing iterations must be non 0.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    numClosingIterations.Select();
                    return;
                }

                if (!RepairIslands && !RemoveEmptyLayers && !RepairResinTraps)
                {
                    MessageBox.Show("You must select at least one repair operation.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (MessageBox.Show("Are you sure you want to attempt this repair?", Text, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    DialogResult = DialogResult.OK;
                    if (ClosingIterations <= 0) // Should never happen!
                    {
                        DialogResult = DialogResult.Cancel;
                    }
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

        
        #endregion


    }
}
