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
using Emgu.CV;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolPixelDimming : CtrlToolWindowContent
    {
        #region Subclasses
        class TexboxMatrix
        {
            public Matrix<byte> Pattern { get; set; }
            public TextBox Textbox { get; }

            public TexboxMatrix(TextBox textbox)
            {
                Textbox = textbox;
            }
        }
        #endregion

        public OperationPixelDimming Operation { get; }
        
        public CtrlToolPixelDimming()
        {
            InitializeComponent();
            Operation = new OperationPixelDimming();
            SetOperation(Operation);
            EventClick(btnDimPatternChessBoard, null);
        }

        public override bool UpdateOperation()
        {
            base.UpdateOperation();
            Operation.WallThickness = (uint) nmBorderSize.Value;
            Operation.WallsOnly = cbDimsOnlyBorders.Checked;


            var matrixTextbox = new[]
                {
                    new TexboxMatrix(tbEvenPattern),
                    new TexboxMatrix(tbOddPattern),
                };


            foreach (var item in matrixTextbox)
            {
                if (string.IsNullOrWhiteSpace(item.Textbox.Text))
                {
                    item.Pattern = null;
                }
                else
                {
                    for (var row = 0; row < item.Textbox.Lines.Length; row++)
                    {

                        var bytes = item.Textbox.Lines[row].Trim().Split(' ');
                        if (row == 0)
                        {
                            item.Pattern = new Matrix<byte>(item.Textbox.Lines.Length, bytes.Length);
                        }
                        else
                        {
                            if (item.Pattern.Cols != bytes.Length)
                            {
                                MessageBoxError($"Row {row + 1} have invalid number of pixels, the pattern must have equal pixel count per line, per defined on line 1");
                                return false;
                            }
                        }

                        for (int col = 0; col < bytes.Length; col++)
                        {
                            if (byte.TryParse(bytes[col], out var value))
                            {
                                item.Pattern[row, col] = value;
                            }
                            else
                            {
                                MessageBoxError($"{bytes[col]} is a invalid number, use values from 0 to 255");
                                return false;
                            }
                        }
                    }
                }
            }

            Operation.Pattern = matrixTextbox[0].Pattern;
            Operation.AlternatePattern = matrixTextbox[1].Pattern;
            return true;
        }

        private void EventClick(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, btnDimPatternSolid))
            {
                tbEvenPattern.Text = nmPixelDimBrightness.Value.ToString(CultureInfo.InvariantCulture);
                tbOddPattern.Text = string.Empty;
                return;
            }
            if (ReferenceEquals(sender, btnDimPatternChessBoard))
            {
                tbEvenPattern.Text = string.Format(
                    "255 {0}{1}" +
                    "{0} 255"
                    , nmPixelDimBrightness.Value, Environment.NewLine);

                tbOddPattern.Text = string.Format(
                    "{0} 255{1}" +
                    "255 {0}"
                    , nmPixelDimBrightness.Value, Environment.NewLine);
                return;
            }

            if (ReferenceEquals(sender, btnDimPatternSparse))
            {

                tbEvenPattern.Text = string.Format(
                    "{0} 255 255 255{1}" +
                    "255 255 {0} 255"
                    , nmPixelDimBrightness.Value, Environment.NewLine);

                tbOddPattern.Text = string.Format(
                    "255 255 {0} 255{1}" +
                    "{0} 255 255 255"
                    , nmPixelDimBrightness.Value, Environment.NewLine);
                return;
            }

            if (ReferenceEquals(sender, btnDimPatternCrosses))
            {
                tbEvenPattern.Text = string.Format(
                    "{0} 255 {0} 255{1}" +
                    "255 {0} 255 255{1}" +
                    "{0} 255 {0} 255{1}" +
                    "255 255 255 255"
                    , nmPixelDimBrightness.Value, Environment.NewLine);

                tbOddPattern.Text = string.Format(
                    "255 255 255 255{1}" +
                    "{0} 255 {0} 255{1}" +
                    "255 {0} 255 255{1}" +
                    "{0} 255 {0} 255"
                    , nmPixelDimBrightness.Value, Environment.NewLine);

                return;
            }

            if (ReferenceEquals(sender, btnDimPatternStrips))
            {
                tbEvenPattern.Text = string.Format(
                    "{0}{1}" +
                    "255"
                    , nmPixelDimBrightness.Value, Environment.NewLine);

                tbOddPattern.Text = string.Format(
                    "255{1}" +
                    "{0}"
                    , nmPixelDimBrightness.Value, Environment.NewLine);

                return;
            }

            if (ReferenceEquals(sender, btnDimPatternRhombus))
            {

                tbEvenPattern.Text = string.Format(
                    "255 {0} 255 255{1}" +
                    "{0} 255 {0} 255{1}" +
                    "255 {0} 255 255{1}" +
                    "255 255 255 255"
                    , nmPixelDimBrightness.Value, Environment.NewLine);

                tbOddPattern.Text = string.Format(
                    "255 255 255 255{1}" +
                    "255 {0} 255 255{1}" +
                    "{0} 255 {0} 255{1}" +
                    "255 {0} 255 255"
                    , nmPixelDimBrightness.Value, Environment.NewLine);
                return;
            }

            if (ReferenceEquals(sender, btnDimPatternPyramid))
            {

                tbEvenPattern.Text = string.Format(
                    "255 255 {0} 255 255 255{1}" +
                    "255 {0} 255 {0} 255 255{1}" +
                    "{0} 255 {0} 255 {0} 255{1}" +
                    "255 255 255 255 255 255"
                    , nmPixelDimBrightness.Value, Environment.NewLine);

                tbOddPattern.Text = string.Format(
                    "255 {0} 255 {0} 255 {0}{1}" +
                    "255 255 {0} 255 {0} 255{1}" +
                    "255 255 255 {0} 255 255{1}" +
                    "255 255 255 255 255 255"

                    , nmPixelDimBrightness.Value, Environment.NewLine);
                return;
            }

            if (ReferenceEquals(sender, btnDimPatternHearts))
            {

                tbEvenPattern.Text = string.Format(
                    "255 {0} 255 {0} 255 255{1}" +
                    "{0} 255 {0} 255 {0} 255{1}" +
                    "{0} 255 255 255 {0} 255{1}" +
                    "255 {0} 255 {0} 255 255{1}" +
                    "255 255 {0} 255 255 255{1}" +
                    "255 255 255 255 255 255"
                    , nmPixelDimBrightness.Value, Environment.NewLine);

                tbOddPattern.Text = string.Format(
                    "255 255 255 255 255 255{1}" +
                    "255 255 {0} 255 {0} 255{1}" +
                    "255 {0} 255 {0} 255 {0}{1}" +
                    "255 {0} 255 255 255 {0}{1}" +
                    "255 255 {0} 255 {0} 255{1}" +
                    "255 255 255 {0} 255 255"
                    , nmPixelDimBrightness.Value, Environment.NewLine);
                return;
            }

            if (ReferenceEquals(sender, btnDimPatternSlashes))
            {
                tbEvenPattern.Text = string.Format(
                    "{0} 255 255{1}" +
                    "255 {0} 255{1}" +
                    "255 255 {0}"
                    , nmPixelDimBrightness.Value, Environment.NewLine);

                tbOddPattern.Text = string.Format(
                    "255 255 {0}{1}" +
                    "255 {0} 255{1}" +
                    "{0} 255 255"
                    , nmPixelDimBrightness.Value, Environment.NewLine);
                return;
            }

            if (ReferenceEquals(sender, btnDimPatternWaves))
            {
                tbEvenPattern.Text = string.Format(
                    "{0} 255 255{1}" +
                    "255 255 {0}"
                    , nmPixelDimBrightness.Value, Environment.NewLine);

                tbOddPattern.Text = string.Format(
                    "255 255 {0}{1}" +
                    "{0} 255 255"
                    , nmPixelDimBrightness.Value, Environment.NewLine);
                /*tbPattern.Text = string.Format(
                    "{0} {0} 255 {0}{1}" +
                    "255 {0} 255 {0}{1}" +
                    "255 {0} {0} {0}{1}" +
                    "255 255 255 255"
                    , nmPixelDimBrightness.Value, Environment.NewLine);*/
                return;
            }

            if (ReferenceEquals(sender, btnPatternRandom))
            {
                var text = string.Empty;
                byte size = 10;
                //var bytes = new byte[size * size];
                //RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
                //provider.GetBytes(bytes);

                Random rnd = new Random();

                for (byte row = 0; row < size; row++)
                {
                    for (byte col = 0; col < size; col++)
                    {
                        //byte value = bytes[rnd.Next(0, bytes.Length)];
                        byte value = (byte)rnd.Next(0, 2);
                        text += value == 1 ? "255" : nmPixelDimBrightness.Value.ToString(CultureInfo.InvariantCulture);
                        if (col < size - 1)
                            text += " ";
                    }

                    if (row < size - 1) text += Environment.NewLine;
                }

                tbEvenPattern.Text = text;

                return;
            }

            if (ReferenceEquals(sender, btnInfillPatternRectilinear))
            {
                tbEvenPattern.Text = $"0{Environment.NewLine}".Repeat((int)nmInfillSpacing.Value) + $"255{Environment.NewLine}".Repeat((int)nmInfillSpacing.Value);
                tbEvenPattern.Text = tbEvenPattern.Text.Trim('\n', '\r');

                tbOddPattern.Text = string.Empty;
                return;
            }

            if (ReferenceEquals(sender, btnInfillPatternSquareGrid))
            {
                var p1 = "0 ".Repeat((int)nmInfillSpacing.Value) + "255 ".Repeat((int)nmInfillThickness.Value);
                p1 = p1.Trim() + Environment.NewLine;
                p1 += p1.Repeat((int)nmInfillThickness.Value);


                var p2 = "255 ".Repeat((int)nmInfillSpacing.Value) + "255 ".Repeat((int)nmInfillThickness.Value);
                p2 = p2.Trim() + Environment.NewLine;
                p2 += p2.Repeat((int)nmInfillThickness.Value);

                p2 = p2.Trim('\n', '\r');

                tbEvenPattern.Text = p1 + p2;
                tbOddPattern.Text = string.Empty;
                return;
            }

            if (ReferenceEquals(sender, btnInfillPatternWaves))
            {
                var p1 = string.Empty;
                var pos = 0;
                for (sbyte dir = 1; dir >= -1; dir -= 2)
                {
                    while (pos >= 0 && pos <= nmInfillSpacing.Value)
                    {
                        p1 += "0 ".Repeat(pos);
                        p1 += "255 ".Repeat((int)nmInfillThickness.Value);
                        p1 += "0 ".Repeat((int)nmInfillSpacing.Value - pos);
                        p1 = p1.Trim() + Environment.NewLine;

                        pos += dir;
                    }

                    pos--;
                }

                tbEvenPattern.Text = p1.Trim('\n', '\r');
                tbOddPattern.Text = string.Empty;
                return;
            }
        }
    }
}
