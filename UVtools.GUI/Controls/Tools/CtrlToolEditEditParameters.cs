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
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;
using UVtools.GUI.Extensions;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolEditParameters : CtrlToolWindowContent
    {
        public OperationEditParameters Operation { get; }
        public RowControl[] RowControls;

        public sealed class RowControl
        {
            public FileFormat.PrintParameterModifier Modifier { get; }

            public Label Name { get; }
            public Label OldValue { get; }
            public NumericUpDown NewValue { get; }
            public Label Unit { get; }
            public Button ResetButton { get; }

            public RowControl(FileFormat.PrintParameterModifier modifier)
            {
                Modifier = modifier;
                modifier.OldValue = decimal.Parse(Program.SlicerFile.GetValueFromPrintParameterModifier(modifier).ToString());

                Name = new Label
                {
                    Text = $"{modifier.Name}:",
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    Tag = this,
                };

                OldValue = new Label
                {
                    Text = modifier.OldValue.ToString(CultureInfo.InvariantCulture),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    Tag = this
                };

                NewValue = new NumericUpDown
                {
                    DecimalPlaces = modifier.DecimalPlates,
                    Minimum = modifier.Minimum,
                    Maximum = modifier.Maximum,
                    Value = modifier.OldValue.Clamp(modifier.Minimum, modifier.Maximum),
                    Tag = this,
                    Width = 100,
                    Dock = DockStyle.Fill,
                    //AutoSize = true
                };
                NewValue.ValueChanged += NewValue_ValueChanged;

                Unit = new Label
                {
                    Text = modifier.ValueUnit,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    Tag = this
                };

                ResetButton = new Button
                {
                    Image = Properties.Resources.undo_16x16,
                    Dock = DockStyle.Fill,
                    BackColor = Color.LightGray,
                    Enabled = false,
                    Tag = this
                };
                ResetButton.Click += ResetButton_Clicked;
            }

            private void NewValue_ValueChanged(object sender, EventArgs e)
            {
                Modifier.NewValue = NewValue.Value;
                ResetButton.Enabled = Modifier.HasChanged;
            }

            private void ResetButton_Clicked(object sender, EventArgs e)
            {
                /*if (!(sender is Button button)) return;
                if (!(button.Tag is NumericUpDown numericUpDown)) return;
                if (!(numericUpDown.Tag is FileFormat.PrintParameterModifier modifier)) return;
                numericUpDown.Value = modifier.OldValue;*/

                NewValue.Value = Modifier.OldValue;
                NewValue.Select();
                return;

            }
        }
        
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

            table.SetDoubleBuffered();

            int rowIndex = 1;
            RowControls = new RowControl[Operation.Modifiers.Length];
            //table.RowCount = Operation.Modifiers.Length+1;
            foreach (var modifier in Operation.Modifiers)
            {
                byte column = 0;
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                var rowControl = new RowControl(modifier);
                table.Controls.Add(rowControl.Name, column++, rowIndex);
                table.Controls.Add(rowControl.OldValue, column++, rowIndex);
                table.Controls.Add(rowControl.NewValue, column++, rowIndex);
                table.Controls.Add(rowControl.Unit, column++, rowIndex);
                table.Controls.Add(rowControl.ResetButton, column++, rowIndex);

                RowControls[rowIndex-1] = rowControl;

                rowIndex++;
            }
        }

        public override bool UpdateOperation()
        {
            base.UpdateOperation();

            foreach (var rowControl in RowControls)
            {
                rowControl.Modifier.NewValue = rowControl.NewValue.Value;
                //if(!modifier.HasChanged) continue;
                //Program.SlicerFile.SetValueFromPrintParameterModifier(modifier, modifier.NewValue);
            }

            return true;
        }

        public override void ExtraActionCall(object sender)
        {
            if (sender is Button button)
            {
                foreach (var rowControl in RowControls)
                {
                    rowControl.NewValue.Value = rowControl.Modifier.OldValue;
                }
                return;
            }
        }
    }
}
