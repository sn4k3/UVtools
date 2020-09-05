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
    public partial class FrmToolLayerImport : Form
    {
        #region Properties

        public uint InsertAfterLayer
        {
            get => (uint) nmInsertAfterLayer.Value;
            set => nmInsertAfterLayer.Value = value;
        }

        #endregion

        #region Constructors
        public FrmToolLayerImport(int layerIndex = -1)
        {
            InitializeComponent();

            //nmLayerRangeEnd.Value = Program.SlicerFile.LayerCount - 1;

            if (layerIndex > 0)
            {
                nmInsertAfterLayer.Value = layerIndex;
            }

            EventValueChanged(nmInsertAfterLayer, null);
        }
        #endregion

        #region Overrides
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Enter)
            {
                btnOk.PerformClick();
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
            /*if (ReferenceEquals(sender, btnLayerRangeAllLayers))
            {
                nmInsertAfterLayer.Value = 0;
                nmLayerRangeEnd.Value = Program.SlicerFile.LayerCount-1;
                return;
            }

            if (ReferenceEquals(sender, btnLayerRangeCurrentLayer))
            {
                nmInsertAfterLayer.Value = Program.FrmMain.ActualLayer;
                nmLayerRangeEnd.Value = Program.FrmMain.ActualLayer;
                return;
            }

            if (ReferenceEquals(sender, btnLayerRangeBottomLayers))
            {
                nmInsertAfterLayer.Value = 0;
                nmLayerRangeEnd.Value = Program.SlicerFile.InitialLayerCount-1;
                return;
            }

            if (ReferenceEquals(sender, btnLayerRangeNormalLayers))
            {
                nmInsertAfterLayer.Value = Program.SlicerFile.InitialLayerCount - 1;
                nmLayerRangeEnd.Value = Program.SlicerFile.LayerCount - 1;
                return;
            }

            if (ReferenceEquals(sender, btnOk))
            {
                if (!btnOk.Enabled) return;
                if (LayerRangeStart > LayerRangeEnd)
                {
                    MessageBox.Show("Layer range start can't be higher than layer end.\nPlease fix and try again.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    nmInsertAfterLayer.Select();
                    return;
                }

                if (MessageBox.Show($"Are you sure you want to clone the selected layers times {nmClones.Value}?", Text, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }

                return;
            }*/

            if (ReferenceEquals(sender, btnCancel))
            {
                DialogResult = DialogResult.Cancel;
                return;
            }
        }

        #endregion

        private void EventValueChanged(object sender, EventArgs e)
        {
            /*uint extraLayers = (uint) Math.Max(0, (nmLayerRangeEnd.Value - nmInsertAfterLayer.Value + 1) * nmClones.Value);
            float extraHeight = (float) Math.Round(extraLayers * Program.SlicerFile.LayerHeight, 2);
            lbLayersCount.Text = $"Layers: {Program.SlicerFile.TotalHeight} → {Program.SlicerFile.TotalHeight + extraLayers} (+ {extraLayers})";
            lbHeights.Text = $"Heights: {Program.SlicerFile.TotalHeight}mm → {Program.SlicerFile.TotalHeight + extraHeight}mm (+ {extraHeight}mm)";*/
        }
    }
}
