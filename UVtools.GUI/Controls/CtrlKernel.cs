using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;

namespace UVtools.GUI.Controls
{
    public partial class CtrlKernel : UserControl
    {
        public ElementShape AutoShape => (ElementShape)cbShape.SelectedItem;
        public Size AutoKernelSize => new Size((int) nmSizeX.Value, (int) nmSizeY.Value); 
        public Point KernelAnchor => new Point((int) nmAnchorX.Value, (int) nmAnchorY.Value); 
        public CtrlKernel()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            foreach (var element in (ElementShape[])Enum.GetValues(
                typeof(ElementShape)))
            {
                if (element == ElementShape.Custom) continue;
                cbShape.Items.Add(element);
            }

            cbShape.SelectedItem = ElementShape.Rectangle;
            Reset();
        }

        private void EventClick(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, btnGen))
            {
                if (nmSizeX.Value <= nmAnchorX.Value || nmSizeY.Value <= nmAnchorY.Value)
                {
                    MessageBox.Show("Anchor position X/Y can't be higher or equal than size X/Y\nPlease fix the values.",
                        "Invalid anchor position", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using (var kernel = CvInvoke.GetStructuringElement(AutoShape, AutoKernelSize, KernelAnchor))
                {
                    string text = string.Empty;
                    for (int y = 0; y < kernel.Height; y++)
                    {
                        var span = kernel.GetPixelRowSpan<byte>(y);
                        var line = string.Empty;
                        for (int x = 0; x < span.Length; x++)
                        {
                            line += $" {span[x]}";
                        }

                        line = line.Remove(0, 1);
                        text += line;
                        if (y < kernel.Height - 1)
                        {
                            text += Environment.NewLine;
                        }
                    }

                    tbKernel.Text = text;
                }
                return;
            }

            if (ReferenceEquals(sender, btnReset))
            {
                Reset();
            }
        }

        public void Reset()
        {
            nmSizeX.Value = 3;
            nmSizeY.Value = 3;
            nmAnchorX.Value = -1;
            nmAnchorY.Value = -1;
            cbShape.SelectedItem = ElementShape.Rectangle;
            btnGen.PerformClick();
        }

        public Matrix<byte> GetMatrix()
        {
            if (string.IsNullOrEmpty(tbKernel.Text))
            {
                MessageBox.Show("Kernel can't be empty.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            Matrix<byte> matrix = null;
            for (var row = 0; row < tbKernel.Lines.Length; row++)
            {
                var bytes = tbKernel.Lines[row].Trim().Split(' ');
                if (row == 0)
                {
                    matrix = new Matrix<byte>(tbKernel.Lines.Length, bytes.Length);
                }
                else
                {
                    if (matrix.Cols != bytes.Length)
                    {
                        MessageBox.Show($"Row {row + 1} have invalid number of columns, the matrix must have equal columns count per line, per defined on line 1", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        matrix.Dispose();
                        return null;
                    }
                }

                for (int col = 0; col < bytes.Length; col++)
                {
                    if (byte.TryParse(bytes[col], out var value))
                    {
                        matrix[row, col] = value;
                    }
                    else
                    {
                        MessageBox.Show($"{bytes[col]} is a invalid number, use values from 0 to 255", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        matrix.Dispose();
                        return null;
                    }
                }
            }
            if (matrix.Cols <= nmAnchorX.Value || matrix.Rows <= nmAnchorY.Value)
            {
                MessageBox.Show("Anchor position X/Y can't be higher or equal than kernel columns/rows\nPlease fix the values.",
                    "Invalid anchor position", MessageBoxButtons.OK, MessageBoxIcon.Error);
                matrix.Dispose();
                return null;
            }
            return matrix;
        }
    }
}
