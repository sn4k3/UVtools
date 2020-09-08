namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolLayerImport
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cbReplaceStartLayer = new System.Windows.Forms.CheckBox();
            this.lbHeight = new System.Windows.Forms.Label();
            this.nmInsertAfterLayer = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.tsBar = new System.Windows.Forms.ToolStrip();
            this.btnAdd = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnRemove = new System.Windows.Forms.ToolStripButton();
            this.lbCount = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.btnClear = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.btnSort = new System.Windows.Forms.ToolStripButton();
            this.cbAutoSort = new System.Windows.Forms.CheckBox();
            this.lbResult = new System.Windows.Forms.Label();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.lbFiles = new System.Windows.Forms.ListBox();
            this.pbSelectedImage = new System.Windows.Forms.PictureBox();
            this.cbReplaceSubsequentLayers = new System.Windows.Forms.CheckBox();
            this.cbDiscardRemainingLayers = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.nmInsertAfterLayer)).BeginInit();
            this.tsBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbSelectedImage)).BeginInit();
            this.SuspendLayout();
            // 
            // cbReplaceStartLayer
            // 
            this.cbReplaceStartLayer.AutoSize = true;
            this.cbReplaceStartLayer.Location = new System.Drawing.Point(10, 49);
            this.cbReplaceStartLayer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbReplaceStartLayer.Name = "cbReplaceStartLayer";
            this.cbReplaceStartLayer.Size = new System.Drawing.Size(153, 24);
            this.cbReplaceStartLayer.TabIndex = 33;
            this.cbReplaceStartLayer.Text = "Replace this layer";
            this.cbReplaceStartLayer.UseVisualStyleBackColor = true;
            this.cbReplaceStartLayer.CheckedChanged += new System.EventHandler(this.EventClick);
            // 
            // lbHeight
            // 
            this.lbHeight.AutoSize = true;
            this.lbHeight.Location = new System.Drawing.Point(216, 13);
            this.lbHeight.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lbHeight.Name = "lbHeight";
            this.lbHeight.Size = new System.Drawing.Size(85, 20);
            this.lbHeight.TabIndex = 32;
            this.lbHeight.Text = "(10.10mm)";
            // 
            // nmInsertAfterLayer
            // 
            this.nmInsertAfterLayer.Location = new System.Drawing.Point(138, 10);
            this.nmInsertAfterLayer.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
            this.nmInsertAfterLayer.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.nmInsertAfterLayer.Name = "nmInsertAfterLayer";
            this.nmInsertAfterLayer.Size = new System.Drawing.Size(74, 26);
            this.nmInsertAfterLayer.TabIndex = 30;
            this.nmInsertAfterLayer.ValueChanged += new System.EventHandler(this.nmInsertAfterLayer_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 13);
            this.label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(128, 20);
            this.label1.TabIndex = 31;
            this.label1.Text = "Insert after layer:";
            // 
            // tsBar
            // 
            this.tsBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tsBar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnAdd,
            this.toolStripSeparator1,
            this.btnRemove,
            this.lbCount,
            this.toolStripSeparator2,
            this.btnClear,
            this.toolStripSeparator3,
            this.btnSort});
            this.tsBar.Location = new System.Drawing.Point(0, 177);
            this.tsBar.Name = "tsBar";
            this.tsBar.Size = new System.Drawing.Size(707, 25);
            this.tsBar.TabIndex = 35;
            // 
            // btnAdd
            // 
            this.btnAdd.Image = global::UVtools.GUI.Properties.Resources.plus_16x16;
            this.btnAdd.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(49, 22);
            this.btnAdd.Text = "&Add";
            this.btnAdd.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // btnRemove
            // 
            this.btnRemove.Enabled = false;
            this.btnRemove.Image = global::UVtools.GUI.Properties.Resources.minus_16x16;
            this.btnRemove.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(70, 22);
            this.btnRemove.Text = "&Remove";
            this.btnRemove.Click += new System.EventHandler(this.EventClick);
            // 
            // lbCount
            // 
            this.lbCount.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lbCount.Name = "lbCount";
            this.lbCount.Size = new System.Drawing.Size(52, 22);
            this.lbCount.Text = "Layers: 0";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // btnClear
            // 
            this.btnClear.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnClear.Enabled = false;
            this.btnClear.Image = global::UVtools.GUI.Properties.Resources.delete_16x16;
            this.btnClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(54, 22);
            this.btnClear.Text = "&Clear";
            this.btnClear.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // btnSort
            // 
            this.btnSort.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnSort.Enabled = false;
            this.btnSort.Image = global::UVtools.GUI.Properties.Resources.sort_alpha_up_16x16;
            this.btnSort.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSort.Name = "btnSort";
            this.btnSort.Size = new System.Drawing.Size(116, 22);
            this.btnSort.Text = "&Sort by file name";
            this.btnSort.Click += new System.EventHandler(this.EventClick);
            // 
            // cbAutoSort
            // 
            this.cbAutoSort.AutoSize = true;
            this.cbAutoSort.Checked = true;
            this.cbAutoSort.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbAutoSort.Location = new System.Drawing.Point(10, 83);
            this.cbAutoSort.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbAutoSort.Name = "cbAutoSort";
            this.cbAutoSort.Size = new System.Drawing.Size(226, 24);
            this.cbAutoSort.TabIndex = 36;
            this.cbAutoSort.Text = "Auto sort layers by file name";
            this.cbAutoSort.UseVisualStyleBackColor = true;
            // 
            // lbResult
            // 
            this.lbResult.AutoSize = true;
            this.lbResult.Location = new System.Drawing.Point(6, 117);
            this.lbResult.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lbResult.Name = "lbResult";
            this.lbResult.Size = new System.Drawing.Size(61, 20);
            this.lbResult.TabIndex = 37;
            this.lbResult.Text = "             ";
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitContainer.Location = new System.Drawing.Point(0, 202);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.lbFiles);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.pbSelectedImage);
            this.splitContainer.Size = new System.Drawing.Size(707, 376);
            this.splitContainer.SplitterDistance = 415;
            this.splitContainer.TabIndex = 38;
            // 
            // lbFiles
            // 
            this.lbFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbFiles.FormattingEnabled = true;
            this.lbFiles.HorizontalScrollbar = true;
            this.lbFiles.ItemHeight = 20;
            this.lbFiles.Location = new System.Drawing.Point(0, 0);
            this.lbFiles.Name = "lbFiles";
            this.lbFiles.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbFiles.Size = new System.Drawing.Size(415, 376);
            this.lbFiles.TabIndex = 35;
            this.lbFiles.SelectedIndexChanged += new System.EventHandler(this.lbFiles_SelectedIndexChanged);
            this.lbFiles.KeyUp += new System.Windows.Forms.KeyEventHandler(this.lbFiles_KeyUp);
            this.lbFiles.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lbFiles_MouseDoubleClick);
            // 
            // pbSelectedImage
            // 
            this.pbSelectedImage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbSelectedImage.Location = new System.Drawing.Point(0, 0);
            this.pbSelectedImage.Name = "pbSelectedImage";
            this.pbSelectedImage.Size = new System.Drawing.Size(288, 376);
            this.pbSelectedImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbSelectedImage.TabIndex = 0;
            this.pbSelectedImage.TabStop = false;
            // 
            // cbReplaceSubsequentLayers
            // 
            this.cbReplaceSubsequentLayers.AutoSize = true;
            this.cbReplaceSubsequentLayers.Location = new System.Drawing.Point(171, 49);
            this.cbReplaceSubsequentLayers.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbReplaceSubsequentLayers.Name = "cbReplaceSubsequentLayers";
            this.cbReplaceSubsequentLayers.Size = new System.Drawing.Size(220, 24);
            this.cbReplaceSubsequentLayers.TabIndex = 39;
            this.cbReplaceSubsequentLayers.Text = "Replace subsequent layers";
            this.cbReplaceSubsequentLayers.UseVisualStyleBackColor = true;
            this.cbReplaceSubsequentLayers.CheckedChanged += new System.EventHandler(this.EventClick);
            // 
            // cbDiscardRemainingLayers
            // 
            this.cbDiscardRemainingLayers.AutoSize = true;
            this.cbDiscardRemainingLayers.Enabled = false;
            this.cbDiscardRemainingLayers.Location = new System.Drawing.Point(399, 49);
            this.cbDiscardRemainingLayers.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbDiscardRemainingLayers.Name = "cbDiscardRemainingLayers";
            this.cbDiscardRemainingLayers.Size = new System.Drawing.Size(200, 24);
            this.cbDiscardRemainingLayers.TabIndex = 40;
            this.cbDiscardRemainingLayers.Text = "Discard remaining layers";
            this.cbDiscardRemainingLayers.UseVisualStyleBackColor = true;
            this.cbDiscardRemainingLayers.CheckedChanged += new System.EventHandler(this.EventClick);
            // 
            // CtrlToolLayerImport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.cbDiscardRemainingLayers);
            this.Controls.Add(this.cbReplaceSubsequentLayers);
            this.Controls.Add(this.lbResult);
            this.Controls.Add(this.cbAutoSort);
            this.Controls.Add(this.tsBar);
            this.Controls.Add(this.cbReplaceStartLayer);
            this.Controls.Add(this.lbHeight);
            this.Controls.Add(this.nmInsertAfterLayer);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.splitContainer);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LayerRangeVisible = false;
            this.Name = "CtrlToolLayerImport";
            this.Size = new System.Drawing.Size(707, 578);
            ((System.ComponentModel.ISupportInitialize)(this.nmInsertAfterLayer)).EndInit();
            this.tsBar.ResumeLayout(false);
            this.tsBar.PerformLayout();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbSelectedImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox cbReplaceStartLayer;
        private System.Windows.Forms.Label lbHeight;
        private System.Windows.Forms.NumericUpDown nmInsertAfterLayer;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolStrip tsBar;
        private System.Windows.Forms.ToolStripButton btnAdd;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton btnRemove;
        private System.Windows.Forms.ToolStripButton btnClear;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton btnSort;
        private System.Windows.Forms.CheckBox cbAutoSort;
        private System.Windows.Forms.ToolStripLabel lbCount;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.Label lbResult;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.ListBox lbFiles;
        private System.Windows.Forms.PictureBox pbSelectedImage;
        private System.Windows.Forms.CheckBox cbReplaceSubsequentLayers;
        private System.Windows.Forms.CheckBox cbDiscardRemainingLayers;
    }
}
