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
using UVtools.Core;

namespace UVtools.GUI.Forms
{
    public partial class FrmMutationBlur : Form
    {
        public enum BlurAlgorithm
        {
            Blur,
            Pyramid,
            MedianBlur,
            GaussianBlur,
            Filter2D
        }
        public struct BlurAlgorithmDescription
        {
            public BlurAlgorithm BlurAlgorithm;
            public string Description;

            public BlurAlgorithmDescription(BlurAlgorithm blurAlgorithm, string description)
            {
                BlurAlgorithm = blurAlgorithm;
                Description = description;
            }

            public override string ToString()
            {
                return Description;
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

        public BlurAlgorithm BlurAlgorithmType => ((BlurAlgorithmDescription) cbAlgorithm.SelectedItem).BlurAlgorithm;

        public uint KSize
        {
            get => (uint) nmSize.Value;
            set => nmSize.Value = value;
        }

        public Matrix<byte> KernelMatrix { get; private set; }
        public Point KernelAnchor => ctrlKernel.KernelAnchor;
        #endregion

        #region Constructors

        public FrmMutationBlur()
        {
            InitializeComponent();
            nmLayerRangeEnd.Value = Program.SlicerFile.LayerCount - 1;
            cbAlgorithm.Items.AddRange(new object[]
            {
                new BlurAlgorithmDescription(BlurAlgorithm.Blur, "Blur: Normalized box filter"), 
                new BlurAlgorithmDescription(BlurAlgorithm.Pyramid, "Pyramid: Down/up-sampling step of Gaussian pyramid decomposition"), 
                new BlurAlgorithmDescription(BlurAlgorithm.MedianBlur, "Median Blur: Each pixel becomes the median of its surrounding pixels"), 
                new BlurAlgorithmDescription(BlurAlgorithm.GaussianBlur, "Gaussian Blur: Each pixel is a sum of fractions of each pixel in its neighborhood"),
                new BlurAlgorithmDescription(BlurAlgorithm.Filter2D, "Filter 2D: Applies an arbitrary linear filter to an image"), 
            });
            cbAlgorithm.SelectedIndex = 0;
        }

        public FrmMutationBlur(Mutation mutation) : this()
        {
            
            Mutation = mutation;

            Text = $"Mutate: {mutation.MenuName}";
            lbDescription.Text = Mutation.Description;

            if (ReferenceEquals(mutation.Image, null))
            {
                Width = pbInfo.Location.X+25;
            }
            else
            {
                pbInfo.Image = mutation.Image;
                pbInfo.Visible = true;
            }
        }
        #endregion

        #region Overrides
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Enter)
            {
                if (ctrlKernel.tbKernel.ContainsFocus) return;
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

            if (ReferenceEquals(sender, btnMutate))
            {
                if (!btnMutate.Enabled) return;
                if (LayerRangeStart > LayerRangeEnd)
                {
                    MessageBox.Show("Layer range start can't be higher than layer end.\nPlease fix and try again.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    nmLayerRangeStart.Select();
                    return;
                }

                if (BlurAlgorithmType == BlurAlgorithm.GaussianBlur ||
                    BlurAlgorithmType == BlurAlgorithm.MedianBlur)
                {
                    if (KSize % 2 != 1)
                    {
                        MessageBox.Show("Size must be a odd number", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                if (BlurAlgorithmType == BlurAlgorithm.Filter2D)
                {
                    KernelMatrix = ctrlKernel.GetMatrix();
                    if (ReferenceEquals(KernelMatrix, null)) return;
                }


                var operationName = string.IsNullOrEmpty(Mutation.MenuName) ? Mutation.Mutate.ToString() : Mutation.MenuName;
                if (MessageBox.Show($"Are you sure you want to {operationName}?", Text, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    DialogResult = DialogResult.OK;
                    if (KSize <= 0) // Should never happen!
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

        private void EventSelectedIndexChanged(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, cbAlgorithm))
            {
                ctrlKernel.Enabled = BlurAlgorithmType == BlurAlgorithm.Filter2D;
                nmSize.Enabled = BlurAlgorithmType != BlurAlgorithm.Pyramid && BlurAlgorithmType != BlurAlgorithm.Filter2D;
                return;
            }
        }

        #endregion


    }
}
