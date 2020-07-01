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
    public partial class FrmMutationResize : Form
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

        public float X
        {
            get => (float)nmX.Value;
            set => nmX.Value = (decimal) value;
        }

        public float Y
        {
            get => (float)nmY.Value;
            set => nmY.Value = (decimal) value;
        }

        public bool ConstrainXY
        {
            get => cbConstrainXY.Checked;
            set => cbConstrainXY.Checked = value;
        }

        public bool Fade
        {
            get => cbFade.Checked;
            set => cbFade.Checked = value;
        }
        #endregion

        #region Constructors
        public FrmMutationResize(Mutation mutation)
        {
            InitializeComponent();
            Mutation = mutation;
            DialogResult = DialogResult.Cancel;

            nmX.Select();

            Text = $"Mutate: {mutation.MenuName}";
            lbDescription.Text = Mutation.Description;

            nmLayerRangeEnd.Value = Program.SlicerFile.LayerCount-1;

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

            if (ReferenceEquals(sender, btnMutate))
            {
                if (!btnMutate.Enabled) return;
                if (LayerRangeStart > LayerRangeEnd)
                {
                    MessageBox.Show("Layer range start can't be higher than layer end.\nPlease fix and try again.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    nmLayerRangeStart.Select();
                    return;
                }

                if (X == 100 && Y == 100)
                {
                    MessageBox.Show($"X and Y cant be 100% together.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (MessageBox.Show($"Are you sure you want to {Mutation.Mutate}?\nX={X}%  Y={Y}%", Text, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    DialogResult = DialogResult.OK;
                    if (X <= 0 || Y <= 0) // Should never happen!
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

        private void EventCheckedChanged(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, cbConstrainXY))
            {
                lbY.Enabled = nmY.Enabled = !cbConstrainXY.Checked;
                EventValueChanged(nmX, null);

                return;
            }
        }

        private void EventValueChanged(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, nmX))
            {
                if (cbConstrainXY.Checked)
                {
                    nmY.Value = nmX.Value;
                }

                return;
            }
        }

        #endregion


    }
}
