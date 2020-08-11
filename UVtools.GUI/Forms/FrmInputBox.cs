/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using UVtools.Core;
using UVtools.Core.FileFormats;

namespace UVtools.GUI.Forms
{
    public partial class FrmInputBox : Form
    {
        #region Properties
        private string _description;
        public string Description
        {
            get => _description;
            set { 
                lbDescription.Text = value;
                _description = value;
            }
        }

        public string ValueUint { get; }
        public decimal NewValue
        {
            get => numNewValue.Value;
            private set => numNewValue.Value = value;
        }

        private decimal _currentValue;
        public decimal CurrentValue
        {
            get => _currentValue;
            set { _currentValue = value; tbCurrentValue.Text = value.ToString(CultureInfo.InvariantCulture)+ValueUint; }
        }
        #endregion

        #region Constructors
        public FrmInputBox()
        {
            InitializeComponent();
            DialogResult = DialogResult.Cancel;
            numNewValue.Select();

            
        }

        public FrmInputBox(FileFormat.PrintParameterModifier modifier, decimal currentValue) : this(modifier.Name,
            modifier.Description, currentValue, modifier.ValueUnit, modifier.Minimum, modifier.Maximum, modifier.DecimalPlates)
        { }
        public FrmInputBox(string title, string description, decimal currentValue, string valueUnit = null, decimal minValue = 0, decimal maxValue = 100, byte decimals = 2, string valueLabel = "Value") : this()
        {
            Text = title;
            lbCurrentValue.Text = $"Current {valueLabel}";
            lbNewValue.Text = $"New {valueLabel}";
            Description = description;
            ValueUint = valueUnit ?? string.Empty;
            CurrentValue = currentValue;
            numNewValue.Minimum = minValue;
            numNewValue.Maximum = maxValue;
            numNewValue.DecimalPlaces = decimals;
            NewValue = currentValue;
        }
        #endregion

        #region Overrides
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Enter)
            {
                btnModify.PerformClick();
                e.Handled = true;
            }
        }

        #endregion

        #region Events
        private void ValueChanged(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, numNewValue))
            {
                btnModify.Enabled = numNewValue.Value != CurrentValue;

                return;
            }
        }

        private void ItemClicked(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, btnModify))
            {
                if (!btnModify.Enabled) return;
                if (MessageBox.Show($"Are you sure you want to {Description}?\nFrom {CurrentValue}{ValueUint} to {NewValue}{ValueUint}", Text, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    DialogResult = DialogResult.OK;
                    if (NewValue == CurrentValue) // Should never happen!
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
