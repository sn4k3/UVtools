/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;

namespace UVtools.GUI.Forms
{
    public partial class FrmToolWindow : Form
    {
        #region Properties

        public string Description
        {
            get => lbDescription.Text;
            set => lbDescription.Text = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", typeof(UITypeEditor))]
        [SettingsBindable(true)]
        public string ButtonOkText
        {
            get => btnOk.Text;
            set => btnOk.Text = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", typeof(UITypeEditor))]
        [SettingsBindable(true)]
        public string ButtonCancelText
        {
            get => btnOk.Text;
            set => btnOk.Text = value;
        }

        [SettingsBindable(true)]
        public bool LayerRangeVisible
        {
            get => pnLayerRange.Visible;
            set => pnLayerRange.Visible = value;
        }

        [SettingsBindable(true)]
        public bool LayerRangeEndVisible
        {
            get => nmLayerRangeEnd.Visible;
            set =>
                nmLayerRangeEnd.Visible = 
                lbLayerRangeTo.Visible = 
                lbLayerRangeToMM.Visible =
                btnLayerRangeSelect.Visible = value;
        }

        [SettingsBindable(true)]
        public uint LayerRangeStart
        {
            get => (uint)nmLayerRangeStart.Value;
            set => nmLayerRangeStart.Value = value;
        }

        [SettingsBindable(true)]
        public uint LayerRangeEnd
        {
            get => (uint)Math.Min(nmLayerRangeEnd.Value, Program.SlicerFile.LayerCount - 1);
            set => nmLayerRangeEnd.Value = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", typeof(UITypeEditor))]
        [SettingsBindable(true)]
        public virtual string ConfirmationText { get; } = "do this action?";


        #endregion

        #region Constructors

        public FrmToolWindow()
        {
            InitializeComponent();
        }

        public FrmToolWindow(string description, string buttonOkText, bool layerRangeVisible = true) : this()
        {
            Description = description;
            ButtonOkText = buttonOkText;
            LayerRangeVisible = layerRangeVisible;
            LayerRangeEnd = Program.SlicerFile.LayerCount - 1;

            EventValueChanged(nmLayerRangeStart, EventArgs.Empty);
            EventValueChanged(nmLayerRangeEnd, EventArgs.Empty);
        }

        public FrmToolWindow(string description, string buttonOkText, uint layerIndex) : this(description, buttonOkText)
        {
            LayerRangeStart = LayerRangeEnd = layerIndex;
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

            if (ReferenceEquals(sender, btnOk))
            {
                if (!btnOk.Enabled) return;
                if (!ValidateForm()) return;

                if (MessageBox.Show($"Are you sure you want to {ConfirmationText}", Text, MessageBoxButtons.YesNo,
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
            if (ReferenceEquals(sender, nmLayerRangeStart) || ReferenceEquals(sender, nmLayerRangeEnd))
            {
                uint layerIndex = (uint) ((NumericUpDown) sender).Value;
                if (layerIndex >= Program.SlicerFile.LayerCount) return;
                var layer = Program.SlicerFile[layerIndex];
                var text = $"({layer.PositionZ}mm)";

                if (ReferenceEquals(sender, nmLayerRangeStart))
                {
                    lbLayerRangeFromMM.Text = text;
                    return;
                }
                if (ReferenceEquals(sender, nmLayerRangeEnd))
                {
                    lbLayerRangeToMM.Text = text;
                    return;
                }
                return;
            }
        }


        #endregion

        #region Methods

        public virtual bool ValidateForm()
        {
            if (LayerRangeStart > LayerRangeEnd)
            {
                MessageBox.Show("Layer range start can't be higher than layer end.\nPlease fix and try again.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                nmLayerRangeStart.Select();
                return false;
            }
            return true;
        }

        #endregion

    }
}
