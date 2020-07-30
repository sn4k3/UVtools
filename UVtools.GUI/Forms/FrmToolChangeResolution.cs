/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using UVtools.Core.Operations;


namespace UVtools.GUI.Forms
{
    public partial class FrmToolChangeResolution : Form
    {
        readonly struct ResolutionItem
        {
            public uint ResolutionX { get; }
            public uint ResolutionY { get; }
            public string Name { get; }

            public ResolutionItem(uint resolutionX, uint resolutionY, string name = null)
            {
                ResolutionX = resolutionX;
                ResolutionY = resolutionY;
                Name = name;
            }

            public override string ToString()
            {
                var str = $"{ResolutionX} x {ResolutionY}";
                if (!string.IsNullOrEmpty(Name))
                {
                    str += $" ({Name})";
                }
                return str;
            }
        }
        public uint OldResolutionX { get; }
        public uint OldResolutionY { get; }
        public Rectangle VolumeBonds { get; }

        public uint NewResolutionX => (uint) nmNewX.Value;
        public uint NewResolutionY => (uint)nmNewY.Value;

        #region Constructors
        public FrmToolChangeResolution(uint oldResolutionX, uint oldResolutionY, Rectangle volumeBonds)
        {
            InitializeComponent();
            DialogResult = DialogResult.Cancel;

            OldResolutionX = oldResolutionX;
            OldResolutionY = oldResolutionY;
            VolumeBonds = volumeBonds;


            lbCurrent.Text = $"Current resolution (X/Y): {oldResolutionX} x {oldResolutionY}";
            lbObjectVolume.Text = $"Object volume (X/Y): {volumeBonds.Width} x {volumeBonds.Height}";
            nmNewX.Value = oldResolutionX;
            nmNewY.Value = oldResolutionY;

            ResolutionItem[] resolutions = {
                new ResolutionItem(854, 480, "FWVGA"),
                new ResolutionItem(960, 1708),
                new ResolutionItem(1080, 1920, "FHD"),
                new ResolutionItem(1440, 2560, "QHD"),
                new ResolutionItem(1600, 2560, "WQXGA"),
                new ResolutionItem(1620, 2560, "WQXGA"),
                new ResolutionItem(1920, 1080, "FHD"),
                new ResolutionItem(2160, 3840, "4K UHD"),
                new ResolutionItem(2531, 1410, "QHD"),
                new ResolutionItem(2560, 1440, "QHD"),
                new ResolutionItem(2560, 1600, "WQXGA"),
                new ResolutionItem(2560, 1620, "WQXGA"),
                new ResolutionItem(3840, 2160, "4K UHD"),
                new ResolutionItem(3840, 2400, "WQUXGA"),
                
            };

            foreach (var resolution in resolutions)
            {
                cbPreset.Items.Add(resolution);
            }
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
        }

        #endregion

        #region Events
        private void EventClick(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, btnOk))
            {
                if (!btnOk.Enabled) return;

                if (OldResolutionX == NewResolutionX && OldResolutionY == NewResolutionY)
                {
                    MessageBox.Show("The new resolution must be different from current resolution.", Text, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                if (NewResolutionX < VolumeBonds.Width || NewResolutionY < VolumeBonds.Height)
                {
                    MessageBox.Show("The new resolution is not enough to accommodate the object volume, continue with operation will cut the object.\n" +
                                    "Please fix this before, try to rotate object and/or resize to fit on new resolution.", Text, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }


                if (MessageBox.Show("Are you sure you want change change layer height?\n" +
                                    $"X/Y = {nmNewX.Value} x {nmNewY.Value}", Text, MessageBoxButtons.YesNo,
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
            if (ReferenceEquals(sender, nmNewX) || ReferenceEquals(sender, nmNewY))
            {
                btnOk.Enabled = OldResolutionX != NewResolutionX || OldResolutionY != NewResolutionY;
                return;
            }

            if (ReferenceEquals(sender, cbPreset))
            {
                if (cbPreset.SelectedIndex >= 0)
                {
                    ResolutionItem resolution = (ResolutionItem)cbPreset.SelectedItem;
                    nmNewX.Value = resolution.ResolutionX;
                    nmNewY.Value = resolution.ResolutionY;
                    cbPreset.SelectedIndex = -1;
                }
                
                return;
            }
        }



        #endregion

        #region Methods


        #endregion

        
    }
}
