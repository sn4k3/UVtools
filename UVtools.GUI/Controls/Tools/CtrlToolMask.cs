/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;
using UVtools.GUI.Extensions;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolMask : CtrlToolWindowContent
    {
        public OperationMask Operation { get; }
        
        public CtrlToolMask()
        {
            InitializeComponent();
            Operation = new OperationMask();
            SetOperation(Operation);
            lbPrinterResolution.Text = $"Printer Resolution: {Program.FrmMain.ActualLayerImage.Size}";
        }

        private void EventClick(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, btnImportImageMask))
            {
                using (var fileOpen = new OpenFileDialog
                {
                    CheckFileExists = true,
                    Filter = "Image Files(*.PNG;*.BMP;*.JPEG;*.JPG;*.GIF)|*.PNG;*.BMP;*.JPEG;*.JPG;*.GIF"
                })
                {
                    if (fileOpen.ShowDialog() != DialogResult.OK) return;

                    Operation.Mask = CvInvoke.Imread(fileOpen.FileName, ImreadModes.Grayscale);

                    if (Operation.Mask.Size != Program.FrmMain.ActualLayerImage.Size)
                    {
                        CvInvoke.Resize(Operation.Mask, Operation.Mask, Program.FrmMain.ActualLayerImage.Size);
                    }

                    if (cbInvertMask.Checked)
                    {
                        CvInvoke.BitwiseNot(Operation.Mask, Operation.Mask);
                    }

                    lbMaskResolution.Text = $"Mask Resolution: {Operation.Mask.Size}";

                    pbMask.Image = Operation.Mask.ToBitmap();
                    cbInvertMask.Enabled =
                    ButtonOkEnabled = true;
                }
                return;
            }

            if (ReferenceEquals(sender, cbInvertMask))
            {
                CvInvoke.BitwiseNot(Operation.Mask, Operation.Mask);
                pbMask.Image = Operation.Mask.ToBitmap();
                return;
            }

            if (ReferenceEquals(sender, btnMaskGenerate))
            {
                Operation.Mask = Program.FrmMain.ActualLayerImage.CloneBlank();
                lbMaskResolution.Text = $"Mask Resolution: {Operation.Mask.Size}";

                int radius = (int)nmGeneratorDiameter.Value;
                if (radius == 0)
                {
                    radius = Math.Min(Operation.Mask.Width, Operation.Mask.Height) / 2;
                }
                else
                {
                    radius = radius.Clamp(2, Math.Min(Operation.Mask.Width, Operation.Mask.Height)) / 2;
                }

                var maxScalar = new MCvScalar((double)nmGeneratorMaxBrightness.Value);
                Operation.Mask.SetTo(maxScalar);

                var center = new Point(Operation.Mask.Width / 2, Operation.Mask.Height / 2);
                var colorDifference = nmGeneratorMinBrightness.Value - nmGeneratorMaxBrightness.Value;
                //CvInvoke.Circle(Mask, center, radius, minScalar, -1);

                for (decimal i = 1; i < radius; i++)
                {
                    int color = (int)(nmGeneratorMinBrightness.Value - i / radius * colorDifference); //or some another color calculation
                    CvInvoke.Circle(Operation.Mask, center, (int)i, new MCvScalar(color), 2);
                }

                if (cbInvertMask.Checked)
                    CvInvoke.BitwiseNot(Operation.Mask, Operation.Mask);
                pbMask.Image = Operation.Mask.ToBitmap();
                cbInvertMask.Enabled =
                ButtonOkEnabled = true;
                return;
            }
        }
    }
}
