/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Globalization;
using System.Windows.Forms;


namespace UVtools.GUI.Forms
{
    public partial class FrmToolLayerReHeight : Form
    {
        private uint LayerCount { get; }
        private decimal LayerHeight { get; }

        #region Constructors
        public FrmToolLayerReHeight(uint layerCount, float layerHeight)
        {
            InitializeComponent();
            DialogResult = DialogResult.Cancel;

            LayerCount = layerCount;
            LayerHeight = (decimal) layerHeight;

            lbCurrent.Text = $"Current layers: {LayerCount} at {layerHeight}mm";

            for (byte i = 2; i < 255; i++)
            {
                var countStr = (LayerCount / (decimal)i).ToString();
                if (countStr.IndexOf(".") >= 0) continue; // Cant multiply layers
                countStr = (LayerHeight / i).ToString();
                int decimalCount = countStr.Substring(countStr.IndexOf(".")).Length;
                if (decimalCount > 2) continue; // Cant multiply height

                cbMultiplier.Items.Add($"/ {i}");
            }

            for (byte i = 2; i < 255; i++)
            {
                var countStr = (LayerCount / (decimal)i).ToString(CultureInfo.InvariantCulture);
                if (countStr.IndexOf(".", StringComparison.Ordinal) >= 0) continue; // Cant multiply layers
                if(LayerHeight * i > 0.2m) break;

                countStr = (LayerHeight * i).ToString(CultureInfo.InvariantCulture);
                int decimalCount = countStr.Substring(countStr.IndexOf(".", StringComparison.Ordinal)).Length;
                if (decimalCount > 2) continue; // Cant multiply height

                cbMultiplier.Items.Add($"x {i}");
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


                if (MessageBox.Show($"Are you sure you want change layer height?", Text, MessageBoxButtons.YesNo,
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

        #region Methods


        #endregion

        
    }
}
