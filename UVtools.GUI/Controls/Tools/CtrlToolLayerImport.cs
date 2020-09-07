/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using UVtools.Core.Obects;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolLayerImport : CtrlToolWindowContent
    {
        public OperationLayerImport Operation { get; } = new OperationLayerImport();

        public CtrlToolLayerImport(uint currentLayer = 0)
        {
            InitializeComponent();
            Text = "Import Layer(s)";

            nmInsertAfterLayer.Maximum = Program.SlicerFile.LayerCount;

            nmInsertAfterLayer.Value = currentLayer;
            nmInsertAfterLayer_ValueChanged(nmInsertAfterLayer, EventArgs.Empty);
        }

        public void UpdateOperation()
        {
            Operation.InsertAfterLayerIndex = (uint)nmInsertAfterLayer.Value;
            Operation.ReplaceStartLayer = cbReplaceStartLayer.Checked;
            Operation.ReplaceSubsequentLayers = cbReplaceSubsequentLayers.Checked;
            Operation.DiscardRemainingLayers = cbDiscardRemainingLayers.Checked;
        }


        public override bool ValidateForm()
        {
            UpdateOperation();
            var result = Operation.Validate(Program.FrmMain.ActualLayerImage.Size);
            if (result.Count == 0) return true;

            var message = new StringBuilder();
            message.AppendLine($"The following {result.Count} files mismatched the slice resolution of {Program.FrmMain.ActualLayerImage.Size.Width}x{Program.FrmMain.ActualLayerImage.Size.Height}:");
            message.AppendLine();
            uint count = 0;
            foreach (var file in result)
            {
                count++;
                if (count == 20)
                {
                    message.AppendLine("... To many to show ...");
                    break;
                }
                message.AppendLine(Path.GetFileNameWithoutExtension(file));
                
            }

            message.AppendLine();
            message.AppendLine("Do you want to remove all invalid files from list?");
            if (MessageErrorBox(message.ToString(), MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                foreach (var file in result)
                {
                    Operation.Files.Remove(file);
                }
                UpdateListBox();
            }

            return false;
        }

        private void nmInsertAfterLayer_ValueChanged(object sender, EventArgs e)
        {
            lbHeight.Text = $"({Program.SlicerFile.GetHeightFromLayer((uint) nmInsertAfterLayer.Value)}mm)";
            UpdateResultText();
        }

        private void EventClick(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, cbReplaceStartLayer) ||
                ReferenceEquals(sender, cbReplaceSubsequentLayers) ||
                ReferenceEquals(sender, cbDiscardRemainingLayers))
            {
                if (ReferenceEquals(sender, cbReplaceSubsequentLayers))
                {
                    cbDiscardRemainingLayers.Enabled = cbReplaceSubsequentLayers.Checked;
                    if (!cbReplaceSubsequentLayers.Checked)
                    {
                        cbDiscardRemainingLayers.Checked = false;
                    }
                }

                UpdateResultText();
                return;
            }

            if (ReferenceEquals(sender, btnAdd))
            {
                using (var fileOpen = new OpenFileDialog
                {
                    Multiselect = true,
                    CheckFileExists = true,
                    Filter = "Image Files(*.PNG;*.BMP;*.JPEG;*.JPG;*.GIF)|*.PNG;*.BMP;*.JPEG;*.JPG;*.GIF"
                })
                {
                    if (fileOpen.ShowDialog() != DialogResult.OK) return;

                    Operation.Files.AddRange(fileOpen.FileNames);

                    if (cbAutoSort.Checked)
                    {
                        Operation.Sort();
                    }

                    UpdateListBox();
                }

                return;
            }

            if (ReferenceEquals(sender, btnRemove))
            {
                foreach (StringTag selectedItem in lbFiles.SelectedItems)
                {
                    Operation.Files.Remove(selectedItem.TagString);
                }
                UpdateListBox();
                
                return;
            }

            if (ReferenceEquals(sender, btnSort))
            {
                Operation.Sort();
                UpdateListBox();
                return;
            }

            if (ReferenceEquals(sender, btnClear))
            {
                Operation.Files.Clear();
                UpdateListBox();
                return;
            }
        }

        private void lbFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (btnRemove.Enabled = lbFiles.SelectedIndex >= 0)
            {
                var file = lbFiles.SelectedItem as StringTag;
                pbSelectedImage.Image = new Bitmap(file.TagString);
            }
            else
            {
                pbSelectedImage.Image = null;
            }
        }

        private void lbFiles_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                lbFiles.SelectedIndices.Clear();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Delete)
            {
                btnRemove.PerformClick();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.A && (ModifierKeys & Keys.Control) != 0)
            {
                lbFiles.BeginUpdate();
                for (int i = 0; i < lbFiles.Items.Count; i++)
                    lbFiles.SetSelected(i, true);
                lbFiles.EndUpdate();
                e.Handled = true;
                return;
            }
        }

        public void UpdateListBox()
        {
            lbFiles.Items.Clear();

            foreach (var file in Operation.Files)
            {
                var stringTag = new StringTag(Path.GetFileNameWithoutExtension(file), file);
                lbFiles.Items.Add(stringTag);
            }

            ButtonOkEnabled = btnRemove.Enabled = btnSort.Enabled = btnClear.Enabled = Operation.Files.Count > 0;
            lbCount.Text = $"Layers: {Operation.Files.Count}";
            UpdateResultText();
        }

        private void UpdateResultText()
        {
            if (Operation.Files.Count > 0)
            {
                UpdateOperation();
                uint modelTotalLayers = Operation.CalculateTotalLayers(Program.SlicerFile.LayerCount);
                string textFactor = "grow";
                if (modelTotalLayers < Program.SlicerFile.LayerCount)
                {
                    textFactor = "shrink";
                }
                else if (modelTotalLayers == Program.SlicerFile.LayerCount)
                {
                    textFactor = "keep";
                }
                lbResult.Text = 
                    $"{Operation.Files.Count} layers will be imported into model starting from layer {nmInsertAfterLayer.Value} {lbHeight.Text}.\n" +
                    $"Model will {textFactor} from layers {Program.SlicerFile.LayerCount} ({Program.SlicerFile.TotalHeight}mm) to {modelTotalLayers} ({Program.SlicerFile.GetHeightFromLayer(modelTotalLayers, false)}mm)";
            }
            else
            {
                lbResult.Text = string.Empty;
            }
        }

        private void lbFiles_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (lbFiles.SelectedItem is StringTag file)
            {
                try
                {
                    using (Process.Start(file.TagString))
                    { }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);
                }
                
                return;
            }
        }
    }
}
