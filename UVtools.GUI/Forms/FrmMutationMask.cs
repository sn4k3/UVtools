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
using UVtools.GUI.Extensions;

namespace UVtools.GUI.Forms
{
    public partial class FrmMutationMask : Form
    {
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

        public Mat Mask { get; private set; }
        #endregion

        #region Constructors
        public FrmMutationMask(Mutation mutation)
        {
            InitializeComponent();
            Mutation = mutation;

            Text = $"Mutate: {mutation.MenuName}";
            lbDescription.Text = Mutation.Description;

            nmLayerRangeEnd.Value = Program.SlicerFile.LayerCount-1;
            lbPrinterResolution.Text = $"Printer Resolution: {Program.FrmMain.ActualLayerImage.Size}";
        }
        #endregion

        #region Overrides
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Enter)
            {
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

                    Mask = CvInvoke.Imread(fileOpen.FileName, ImreadModes.Grayscale);

                    if (Mask.Size != Program.FrmMain.ActualLayerImage.Size)
                    {
                        CvInvoke.Resize(Mask, Mask, Program.FrmMain.ActualLayerImage.Size);
                    }

                    if (cbInvertMask.Checked)
                    {
                        CvInvoke.BitwiseNot(Mask, Mask);
                    }

                    lbMaskResolution.Text = $"Mask Resolution: {Mask.Size}";

                    pbMask.Image = Mask.ToBitmap();
                    btnMutate.Enabled = true;
                }
                return;
            }

            if (ReferenceEquals(sender, cbInvertMask))
            {
                CvInvoke.BitwiseNot(Mask, Mask);
                pbMask.Image = Mask.ToBitmap();
                return;
            }

            if (ReferenceEquals(sender, btnMaskGenerate))
            {
                Mask = Program.FrmMain.ActualLayerImage.CloneBlank();
                lbMaskResolution.Text = $"Mask Resolution: {Mask.Size}";

                int radius = (int) nmGeneratorDiameter.Value;
                if (radius == 0)
                {
                    radius = Math.Min(Mask.Width, Mask.Height) / 2;
                }
                else
                {
                    radius = radius.Clamp(2, Math.Min(Mask.Width, Mask.Height)) / 2;
                }

                var maxScalar = new MCvScalar((double)nmGeneratorMaxBrightness.Value);
                Mask.SetTo(maxScalar);

                var center = new Point(Mask.Width / 2, Mask.Height / 2);
                var colorDifference = nmGeneratorMinBrightness.Value - nmGeneratorMaxBrightness.Value;
                //CvInvoke.Circle(Mask, center, radius, minScalar, -1);

                for (decimal i = 1; i < radius; i++)
                {
                    int color = (int) (nmGeneratorMinBrightness.Value - i / radius * colorDifference); //or some another color calculation
                    CvInvoke.Circle(Mask, center, (int) i, new MCvScalar(color), 2);
                }

                if (cbInvertMask.Checked)
                    CvInvoke.BitwiseNot(Mask, Mask);
                pbMask.Image = Mask.ToBitmap();
                btnMutate.Enabled = true;
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

                if (MessageBox.Show($"Are you sure you want to {Mutation.Mutate}?", Text, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes) return;

                DialogResult = DialogResult.OK;
                Close();

                return;
            }

            if (ReferenceEquals(sender, btnCancel))
            {
                Mask?.Dispose();
                DialogResult = DialogResult.Cancel;
                return;
            }
        }


        #endregion
    }
}
