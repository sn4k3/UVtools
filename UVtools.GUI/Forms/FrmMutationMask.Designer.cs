using UVtools.GUI.Controls;

namespace UVtools.GUI.Forms
{
    partial class FrmMutationMask
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMutationMask));
            this.lbDescription = new System.Windows.Forms.Label();
            this.lbLayerRange = new System.Windows.Forms.Label();
            this.nmLayerRangeStart = new System.Windows.Forms.NumericUpDown();
            this.nmLayerRangeEnd = new System.Windows.Forms.NumericUpDown();
            this.lbLayerRangeTo = new System.Windows.Forms.Label();
            this.cmLayerRange = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.btnLayerRangeAllLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeCurrentLayer = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeBottomLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeNormalLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnMutate = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.btnLayerRangeSelect = new UVtools.GUI.Controls.SplitButton();
            this.btnImportImageMask = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.pbMask = new System.Windows.Forms.PictureBox();
            this.lbPrinterResolution = new System.Windows.Forms.Label();
            this.lbMaskResolution = new System.Windows.Forms.Label();
            this.cbInvertMask = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.nmGeneratorMinBrightness = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.nmGeneratorMaxBrightness = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.nmGeneratorDiameter = new System.Windows.Forms.NumericUpDown();
            this.btnMaskGenerate = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeStart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeEnd)).BeginInit();
            this.cmLayerRange.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbMask)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmGeneratorMinBrightness)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmGeneratorMaxBrightness)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmGeneratorDiameter)).BeginInit();
            this.SuspendLayout();
            // 
            // lbDescription
            // 
            this.lbDescription.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbDescription.Location = new System.Drawing.Point(13, 14);
            this.lbDescription.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbDescription.Name = "lbDescription";
            this.lbDescription.Size = new System.Drawing.Size(584, 128);
            this.lbDescription.TabIndex = 0;
            this.lbDescription.Text = "Description";
            // 
            // lbLayerRange
            // 
            this.lbLayerRange.AutoSize = true;
            this.lbLayerRange.Location = new System.Drawing.Point(13, 150);
            this.lbLayerRange.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbLayerRange.Name = "lbLayerRange";
            this.lbLayerRange.Size = new System.Drawing.Size(97, 20);
            this.lbLayerRange.TabIndex = 9;
            this.lbLayerRange.Text = "Layer range:";
            this.toolTip.SetToolTip(this.lbLayerRange, resources.GetString("lbLayerRange.ToolTip"));
            // 
            // nmLayerRangeStart
            // 
            this.nmLayerRangeStart.Location = new System.Drawing.Point(118, 147);
            this.nmLayerRangeStart.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmLayerRangeStart.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.nmLayerRangeStart.Name = "nmLayerRangeStart";
            this.nmLayerRangeStart.Size = new System.Drawing.Size(120, 26);
            this.nmLayerRangeStart.TabIndex = 0;
            // 
            // nmLayerRangeEnd
            // 
            this.nmLayerRangeEnd.Location = new System.Drawing.Point(314, 147);
            this.nmLayerRangeEnd.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmLayerRangeEnd.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.nmLayerRangeEnd.Name = "nmLayerRangeEnd";
            this.nmLayerRangeEnd.Size = new System.Drawing.Size(120, 26);
            this.nmLayerRangeEnd.TabIndex = 1;
            // 
            // lbLayerRangeTo
            // 
            this.lbLayerRangeTo.AutoSize = true;
            this.lbLayerRangeTo.Location = new System.Drawing.Point(275, 150);
            this.lbLayerRangeTo.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbLayerRangeTo.Name = "lbLayerRangeTo";
            this.lbLayerRangeTo.Size = new System.Drawing.Size(31, 20);
            this.lbLayerRangeTo.TabIndex = 12;
            this.lbLayerRangeTo.Text = "To:";
            // 
            // cmLayerRange
            // 
            this.cmLayerRange.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnLayerRangeAllLayers,
            this.btnLayerRangeCurrentLayer,
            this.btnLayerRangeBottomLayers,
            this.btnLayerRangeNormalLayers});
            this.cmLayerRange.Name = "cmLayerRange";
            this.cmLayerRange.Size = new System.Drawing.Size(226, 92);
            // 
            // btnLayerRangeAllLayers
            // 
            this.btnLayerRangeAllLayers.Name = "btnLayerRangeAllLayers";
            this.btnLayerRangeAllLayers.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.A)));
            this.btnLayerRangeAllLayers.Size = new System.Drawing.Size(225, 22);
            this.btnLayerRangeAllLayers.Text = "&All Layers";
            this.btnLayerRangeAllLayers.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnLayerRangeCurrentLayer
            // 
            this.btnLayerRangeCurrentLayer.Name = "btnLayerRangeCurrentLayer";
            this.btnLayerRangeCurrentLayer.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.C)));
            this.btnLayerRangeCurrentLayer.Size = new System.Drawing.Size(225, 22);
            this.btnLayerRangeCurrentLayer.Text = "&Current Layer";
            this.btnLayerRangeCurrentLayer.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnLayerRangeBottomLayers
            // 
            this.btnLayerRangeBottomLayers.Name = "btnLayerRangeBottomLayers";
            this.btnLayerRangeBottomLayers.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.B)));
            this.btnLayerRangeBottomLayers.Size = new System.Drawing.Size(225, 22);
            this.btnLayerRangeBottomLayers.Text = "&Bottom Layers";
            this.btnLayerRangeBottomLayers.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnLayerRangeNormalLayers
            // 
            this.btnLayerRangeNormalLayers.Name = "btnLayerRangeNormalLayers";
            this.btnLayerRangeNormalLayers.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.N)));
            this.btnLayerRangeNormalLayers.Size = new System.Drawing.Size(225, 22);
            this.btnLayerRangeNormalLayers.Text = "&Normal Layers";
            this.btnLayerRangeNormalLayers.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Image = global::UVtools.GUI.Properties.Resources.Cancel_24x24;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(434, 733);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(150, 48);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnMutate
            // 
            this.btnMutate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnMutate.Enabled = false;
            this.btnMutate.Image = global::UVtools.GUI.Properties.Resources.Ok_24x24;
            this.btnMutate.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnMutate.Location = new System.Drawing.Point(276, 733);
            this.btnMutate.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnMutate.Name = "btnMutate";
            this.btnMutate.Size = new System.Drawing.Size(150, 48);
            this.btnMutate.TabIndex = 5;
            this.btnMutate.Text = "&Mutate";
            this.btnMutate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnMutate.UseVisualStyleBackColor = true;
            this.btnMutate.Click += new System.EventHandler(this.ItemClicked);
            // 
            // toolTip
            // 
            this.toolTip.AutoPopDelay = 32767;
            this.toolTip.InitialDelay = 500;
            this.toolTip.IsBalloon = true;
            this.toolTip.ReshowDelay = 100;
            this.toolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.toolTip.ToolTipTitle = "Information";
            // 
            // btnLayerRangeSelect
            // 
            this.btnLayerRangeSelect.Location = new System.Drawing.Point(446, 146);
            this.btnLayerRangeSelect.Menu = this.cmLayerRange;
            this.btnLayerRangeSelect.Name = "btnLayerRangeSelect";
            this.btnLayerRangeSelect.Size = new System.Drawing.Size(138, 26);
            this.btnLayerRangeSelect.TabIndex = 2;
            this.btnLayerRangeSelect.Text = "Select";
            this.btnLayerRangeSelect.UseVisualStyleBackColor = true;
            // 
            // btnImportImageMask
            // 
            this.btnImportImageMask.Location = new System.Drawing.Point(17, 181);
            this.btnImportImageMask.Name = "btnImportImageMask";
            this.btnImportImageMask.Size = new System.Drawing.Size(417, 32);
            this.btnImportImageMask.TabIndex = 31;
            this.btnImportImageMask.Text = "Import grayscale mask image from file";
            this.btnImportImageMask.UseVisualStyleBackColor = true;
            this.btnImportImageMask.Click += new System.EventHandler(this.ItemClicked);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.pbMask);
            this.groupBox1.Location = new System.Drawing.Point(14, 387);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(567, 335);
            this.groupBox1.TabIndex = 32;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Mask image";
            // 
            // pbMask
            // 
            this.pbMask.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbMask.Location = new System.Drawing.Point(3, 22);
            this.pbMask.Name = "pbMask";
            this.pbMask.Size = new System.Drawing.Size(561, 310);
            this.pbMask.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbMask.TabIndex = 0;
            this.pbMask.TabStop = false;
            // 
            // lbPrinterResolution
            // 
            this.lbPrinterResolution.AutoSize = true;
            this.lbPrinterResolution.Location = new System.Drawing.Point(13, 322);
            this.lbPrinterResolution.Name = "lbPrinterResolution";
            this.lbPrinterResolution.Size = new System.Drawing.Size(136, 20);
            this.lbPrinterResolution.TabIndex = 33;
            this.lbPrinterResolution.Text = "Printer resolution: ";
            // 
            // lbMaskResolution
            // 
            this.lbMaskResolution.AutoSize = true;
            this.lbMaskResolution.Location = new System.Drawing.Point(13, 352);
            this.lbMaskResolution.Name = "lbMaskResolution";
            this.lbMaskResolution.Size = new System.Drawing.Size(207, 20);
            this.lbMaskResolution.TabIndex = 34;
            this.lbMaskResolution.Text = "Mask resolution: (Unloaded)";
            // 
            // cbInvertMask
            // 
            this.cbInvertMask.AutoSize = true;
            this.cbInvertMask.Location = new System.Drawing.Point(446, 186);
            this.cbInvertMask.Name = "cbInvertMask";
            this.cbInvertMask.Size = new System.Drawing.Size(110, 24);
            this.cbInvertMask.TabIndex = 35;
            this.cbInvertMask.Text = "Invert Mask";
            this.cbInvertMask.UseVisualStyleBackColor = true;
            this.cbInvertMask.CheckedChanged += new System.EventHandler(this.ItemClicked);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnMaskGenerate);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.nmGeneratorDiameter);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.nmGeneratorMaxBrightness);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.nmGeneratorMinBrightness);
            this.groupBox2.Location = new System.Drawing.Point(17, 219);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(564, 96);
            this.groupBox2.TabIndex = 36;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Mask Generator (Round from center)";
            // 
            // nmGeneratorMinBrightness
            // 
            this.nmGeneratorMinBrightness.Location = new System.Drawing.Point(166, 25);
            this.nmGeneratorMinBrightness.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nmGeneratorMinBrightness.Name = "nmGeneratorMinBrightness";
            this.nmGeneratorMinBrightness.Size = new System.Drawing.Size(78, 26);
            this.nmGeneratorMinBrightness.TabIndex = 0;
            this.nmGeneratorMinBrightness.Value = new decimal(new int[] {
            200,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(154, 20);
            this.label1.TabIndex = 35;
            this.label1.Text = "Minimum brightness:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(320, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(158, 20);
            this.label2.TabIndex = 37;
            this.label2.Text = "Maximum brightness:";
            // 
            // nmGeneratorMaxBrightness
            // 
            this.nmGeneratorMaxBrightness.Location = new System.Drawing.Point(480, 25);
            this.nmGeneratorMaxBrightness.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nmGeneratorMaxBrightness.Name = "nmGeneratorMaxBrightness";
            this.nmGeneratorMaxBrightness.Size = new System.Drawing.Size(78, 26);
            this.nmGeneratorMaxBrightness.TabIndex = 36;
            this.nmGeneratorMaxBrightness.Value = new decimal(new int[] {
            255,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(255, 28);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 20);
            this.label3.TabIndex = 38;
            this.label3.Text = "(0-255)";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 60);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(137, 20);
            this.label4.TabIndex = 40;
            this.label4.Text = "Diameter in pixels:";
            // 
            // nmGeneratorDiameter
            // 
            this.nmGeneratorDiameter.Location = new System.Drawing.Point(166, 57);
            this.nmGeneratorDiameter.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmGeneratorDiameter.Name = "nmGeneratorDiameter";
            this.nmGeneratorDiameter.Size = new System.Drawing.Size(78, 26);
            this.nmGeneratorDiameter.TabIndex = 39;
            // 
            // btnMaskGenerate
            // 
            this.btnMaskGenerate.Location = new System.Drawing.Point(259, 57);
            this.btnMaskGenerate.Name = "btnMaskGenerate";
            this.btnMaskGenerate.Size = new System.Drawing.Size(299, 26);
            this.btnMaskGenerate.TabIndex = 41;
            this.btnMaskGenerate.Text = "Generate";
            this.btnMaskGenerate.UseVisualStyleBackColor = true;
            this.btnMaskGenerate.Click += new System.EventHandler(this.ItemClicked);
            // 
            // FrmMutationMask
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(599, 795);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.cbInvertMask);
            this.Controls.Add(this.lbMaskResolution);
            this.Controls.Add(this.lbPrinterResolution);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnImportImageMask);
            this.Controls.Add(this.btnLayerRangeSelect);
            this.Controls.Add(this.lbLayerRangeTo);
            this.Controls.Add(this.nmLayerRangeEnd);
            this.Controls.Add(this.nmLayerRangeStart);
            this.Controls.Add(this.lbLayerRange);
            this.Controls.Add(this.btnMutate);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lbDescription);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmMutationMask";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Form1";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeStart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeEnd)).EndInit();
            this.cmLayerRange.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbMask)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmGeneratorMinBrightness)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmGeneratorMaxBrightness)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmGeneratorDiameter)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbDescription;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnMutate;
        private System.Windows.Forms.Label lbLayerRange;
        private System.Windows.Forms.NumericUpDown nmLayerRangeStart;
        private System.Windows.Forms.NumericUpDown nmLayerRangeEnd;
        private System.Windows.Forms.Label lbLayerRangeTo;
        private Controls.SplitButton btnLayerRangeSelect;
        private System.Windows.Forms.ContextMenuStrip cmLayerRange;
        private System.Windows.Forms.ToolStripMenuItem btnLayerRangeAllLayers;
        private System.Windows.Forms.ToolStripMenuItem btnLayerRangeCurrentLayer;
        private System.Windows.Forms.ToolStripMenuItem btnLayerRangeBottomLayers;
        private System.Windows.Forms.ToolStripMenuItem btnLayerRangeNormalLayers;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Button btnImportImageMask;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.PictureBox pbMask;
        private System.Windows.Forms.Label lbPrinterResolution;
        private System.Windows.Forms.Label lbMaskResolution;
        private System.Windows.Forms.CheckBox cbInvertMask;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nmGeneratorMaxBrightness;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nmGeneratorMinBrightness;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown nmGeneratorDiameter;
        private System.Windows.Forms.Button btnMaskGenerate;
    }
}