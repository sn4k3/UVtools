/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;

namespace UVtools.GUI.Forms
{
    public partial class FrmMutationPixelDimming : Form
    {
        class MatrixTexbox
        {
            public Matrix<byte> Pattern;
            public TextBox Textbox;

            public MatrixTexbox(Matrix<byte> pattern, TextBox textbox)
            {
                Pattern = pattern;
                Textbox = textbox;
            }
        }
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

        public uint BorderSize
        {
            get => (uint)nmBorderSize.Value;
            set => nmBorderSize.Value = value;
        }

        public bool DimsOnlyBorders
        {
            get => cbDimsOnlyBorders.Checked;
            set => cbDimsOnlyBorders.Checked = value;
        }

        public Matrix<byte> EvenPattern { get; private set; }
        public Matrix<byte> OddPattern { get; private set; }

        
        #endregion

        #region Constructors
        public FrmMutationPixelDimming(Mutation mutation)
        {
            InitializeComponent();
            Mutation = mutation;
            DialogResult = DialogResult.Cancel;

            nmBorderSize.Select();

            Text = $"Mutate: {mutation.MenuName}";
            lbDescription.Text = Mutation.Description;

            nmLayerRangeEnd.Value = Program.SlicerFile.LayerCount-1;

            ItemClicked(btnDimPatternChessBoard, null);
        }
        #endregion

        #region Overrides
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Enter)
            {
                if (tbEvenPattern.ContainsFocus || tbOddPattern.ContainsFocus) return;
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

            if (ReferenceEquals(sender, btnImportImageMask))
            {
                using (var fileOpen = new OpenFileDialog
                {
                    CheckFileExists = true,
                    Filter = "Image Files(*.PNG;*.BMP;*.JPEG;*.JPG;*.GIF)|*.PNG;*.BMP;*.JPEG;*.JPG;*.GIF"
                })
                {
                    if (fileOpen.ShowDialog() != DialogResult.OK) return;

                    using (var image = CvInvoke.Imread(fileOpen.FileName, ImreadModes.Grayscale))
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int y = 0; y < image.Height; y++)
                        {
                            var span = image.GetPixelRowSpan<byte>(y);
                            string line = string.Empty;
                            for (int x = 0; x < span.Length; x++)
                            {
                                line += $"{span[x]} ";
                            }

                            line = line.Trim();
                            sb.Append(line);

                            if(y < image.Height-1)
                                sb.AppendLine();
                        }

                        tbEvenPattern.Text = sb.ToString();
                        tbOddPattern.Text = string.Empty;
                    }

                }
                return;
            }

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
                        byte value = (byte) rnd.Next(0, 2);
                        text += value == 1 ? "255" : nmPixelDimBrightness.Value.ToString(CultureInfo.InvariantCulture);
                        if (col < size-1)
                            text += " ";
                    }

                    if (row < size-1) text += Environment.NewLine;
                }

                tbEvenPattern.Text = text;

                return;
            }

            if (ReferenceEquals(sender, btnInfillPatternRectilinear))
            {
                tbEvenPattern.Text = $"0{Environment.NewLine}".Repeat((int) nmInfillSpacing.Value) + $"255{Environment.NewLine}".Repeat((int)nmInfillSpacing.Value);
                tbEvenPattern.Text = tbEvenPattern.Text.Trim('\n', '\r');

                tbOddPattern.Text = string.Empty;
                return;
            }

            if (ReferenceEquals(sender, btnInfillPatternSquareGrid))
            {
                var p1 = "0 ".Repeat((int)nmInfillSpacing.Value) + "255 ".Repeat((int)nmInfillThickness.Value);
                p1 = p1.Trim() + Environment.NewLine;
                p1 += p1.Repeat((int)nmInfillThickness.Value);


                var p2 = "255 ".Repeat((int) nmInfillSpacing.Value) + "255 ".Repeat((int) nmInfillThickness.Value);
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
                for (sbyte dir = 1; dir >= -1; dir-=2)
                {
                    while (pos >= 0 && pos <= nmInfillSpacing.Value)
                    {
                        p1 += "0 ".Repeat(pos);
                        p1 += "255 ".Repeat((int) nmInfillThickness.Value);
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

            if (ReferenceEquals(sender, btnMutate))
            {
                if (!btnMutate.Enabled) return;
                if (LayerRangeStart > LayerRangeEnd)
                {
                    MessageBox.Show("Layer range start can't be higher than layer end.\nPlease fix and try again.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    nmLayerRangeStart.Select();
                    return;
                }

                if (BorderSize == 0 && DimsOnlyBorders)
                {
                    MessageBox.Show("Border size must be positive in order to use \"Dims only the borders\" function.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var matrixTextbox = new[]
                {
                    new MatrixTexbox(EvenPattern, tbEvenPattern),
                    new MatrixTexbox(OddPattern, tbOddPattern),
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
                                /*if (tbPattern.Lines.Length % 2 != 0 || bytes.Length % 2 != 0)
                                {
                                    MessageBox.Show($"Rows and columns must be in even numbers", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }*/
                                item.Pattern = new Matrix<byte>(item.Textbox.Lines.Length, bytes.Length);
                            }
                            else
                            {
                                if (item.Pattern.Cols != bytes.Length)
                                {
                                    MessageBox.Show($"Row {row + 1} have invalid number of pixels, the pattern must have equal pixel count per line, per defined on line 1", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
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
                                    MessageBox.Show($"{bytes[col]} is a invalid number, use values from 0 to 255", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                            }
                        }
                    }
                }


                EvenPattern = matrixTextbox[0].Pattern;
                OddPattern = matrixTextbox[1].Pattern;


                /*if (X == 100 && Y == 100)
                {
                    MessageBox.Show($"X and Y cant be 100% together.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }*/

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
