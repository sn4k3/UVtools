/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Windows.Forms;
using Emgu.CV.CvEnum;

namespace UVtools.GUI.Forms
{
    public partial class FrmMutationThreshold : Form
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

        public byte Threshold
        {
            get => (byte)nmThreshold.Value;
            set => nmThreshold.Value = value;
        }

        public byte Maximum
        {
            get => (byte) nmMaximum.Value;
            set => nmMaximum.Value = value;
        }

        public ThresholdType ThresholdTypeValue
        {
            get => cbThresholdType.SelectedItem is ThresholdType item ? item : ThresholdType.Binary;
            set => cbThresholdType.SelectedItem = value;
        }
        #endregion

        #region Constructors
        public FrmMutationThreshold(Mutation mutation)
        {
            InitializeComponent();
            Mutation = mutation;
            DialogResult = DialogResult.Cancel;

            Text = $"Mutate: {mutation.MenuName}";
            lbDescription.Text = Mutation.Description;

            nmLayerRangeEnd.Value = Program.SlicerFile.LayerCount-1;

            foreach (var thresholdType in (ThresholdType[])Enum.GetValues(typeof(ThresholdType)))
            {
                cbThresholdType.Items.Add(thresholdType);
            }

            cbThresholdType.SelectedItem = ThresholdType.Binary;
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

            if (ReferenceEquals(sender, btnPresetFreeUse))
            {
                nmThreshold.Enabled =
                nmMaximum.Enabled =
                cbThresholdType.Enabled = true;
                return;
            }

            if (ReferenceEquals(sender, btnPresetStripAntiAliasing))
            {
                ItemClicked(btnPresetFreeUse, e);
                nmMaximum.Enabled = cbThresholdType.Enabled = false;

                nmThreshold.Value = 127;
                nmMaximum.Value = 255;
                cbThresholdType.SelectedItem = ThresholdType.Binary;
                nmThreshold.Focus();
                return;
            }

            if (ReferenceEquals(sender, btnPresetSetPixelsBrightness))
            {
                ItemClicked(btnPresetFreeUse, e);
                nmThreshold.Enabled = cbThresholdType.Enabled = false;

                nmThreshold.Value = 254;
                nmMaximum.Value = 254;
                cbThresholdType.SelectedItem = ThresholdType.Binary;
                nmMaximum.Focus();
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

        #endregion


    }
}
