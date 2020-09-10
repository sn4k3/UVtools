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
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolEditParameters : CtrlToolWindowContent
    {
        public OperationEditParameters Operation { get; }
        public NumericUpDown[] NumericUpDownProperties;
        
        public CtrlToolEditParameters()
        {
            InitializeComponent();
            Operation = new OperationEditParameters(Program.SlicerFile.PrintParameterModifiers);
            SetOperation(Operation);

            if (Operation.Modifiers is null || Operation.Modifiers.Length == 0)
            {
                CanRun = false;
                return;
            }

            int rowIndex = 1;
            NumericUpDownProperties = new NumericUpDown[Operation.Modifiers.Length];
            foreach (var modifier in Operation.Modifiers)
            {
                modifier.OldValue = decimal.Parse(Program.SlicerFile.GetValueFromPrintParameterModifier(modifier).ToString());
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                Label nameLabel = new Label
                {
                    Text = $"{modifier.Name}:",
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize = true,
                    Dock = DockStyle.Fill
                };
                table.Controls.Add(nameLabel, 0, rowIndex);
                toolTip.SetToolTip(nameLabel, modifier.Description);

                Label oldValueLabel = new Label
                {
                    Text = modifier.OldValue.ToString(CultureInfo.InvariantCulture),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    Dock = DockStyle.Fill
                };
                table.Controls.Add(oldValueLabel, 1, rowIndex);

                NumericUpDown numericValue = new NumericUpDown
                {
                    Value = modifier.OldValue,
                    DecimalPlaces = modifier.DecimalPlates,
                    Minimum = modifier.Minimum,
                    Maximum = modifier.Maximum,
                    Tag = modifier,
                    Width = 100,
                    Dock = DockStyle.Fill
                    //AutoSize = true
                };
                NumericUpDownProperties[rowIndex - 1] = numericValue;
                table.Controls.Add(numericValue, 2, rowIndex);

                if (!string.IsNullOrEmpty(modifier.ValueUnit))
                {
                    Label unitLabel = new Label
                    {
                        Text = modifier.ValueUnit,
                        TextAlign = ContentAlignment.MiddleLeft,
                        AutoSize = true,
                        Dock = DockStyle.Fill,
                    };
                    table.Controls.Add(unitLabel, 3, rowIndex);
                }

                Button resetButton = new Button
                {
                    Image = Properties.Resources.refresh_16x16,
                    Dock = DockStyle.Fill,
                    BackColor = Color.WhiteSmoke,
                    Tag = numericValue
                };
                resetButton.Click += ResetClicked;
                table.Controls.Add(resetButton, 4, rowIndex);

                rowIndex++;
            }
        }

        public override bool UpdateOperation()
        {
            base.UpdateOperation();

            foreach (var numUpDown in NumericUpDownProperties)
            {
                if (!(numUpDown.Tag is FileFormat.PrintParameterModifier modifier)) continue;
                modifier.NewValue = modifier.NewValue;
                if(!modifier.HasChanged) continue;
                Program.SlicerFile.SetValueFromPrintParameterModifier(modifier, modifier.NewValue);
            }

            return true;
        }

        public override void ExtraActionCall(object sender)
        {
            if (sender is Button button)
            {
                foreach (var numUpDown in NumericUpDownProperties)
                {
                    if (numUpDown.Tag is FileFormat.PrintParameterModifier modifier)
                    {
                        numUpDown.Value = modifier.OldValue;
                    }
                }
                return;
            }
        }

        private void ResetClicked(object sender, EventArgs e)
        {
            if (!(sender is Button button)) return;
            if (!(button.Tag is NumericUpDown numericUpDown)) return;
            if (!(numericUpDown.Tag is FileFormat.PrintParameterModifier modifier)) return;
            numericUpDown.Value = modifier.OldValue;
            return;

        }
    }
}
