using UVtools.GUI.Controls;

namespace UVtools.GUI.Forms
{
    partial class FrmRepairLayers
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmRepairLayers));
            this.lbDescription = new System.Windows.Forms.Label();
            this.lbIterationsStart = new System.Windows.Forms.Label();
            this.numClosingIterations = new System.Windows.Forms.NumericUpDown();
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
            this.btnRepair = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.cbRepairIslands = new System.Windows.Forms.CheckBox();
            this.numOpeningIterations = new System.Windows.Forms.NumericUpDown();
            this.cbRepairResinTraps = new System.Windows.Forms.CheckBox();
            this.btnLayerRangeSelect = new UVtools.GUI.Controls.SplitButton();
            this.cbRemoveEmptyLayers = new System.Windows.Forms.CheckBox();
            this.nmRemoveIslandsBelowEqualPixels = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numClosingIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeStart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeEnd)).BeginInit();
            this.cmLayerRange.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numOpeningIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmRemoveIslandsBelowEqualPixels)).BeginInit();
            this.SuspendLayout();
            // 
            // lbDescription
            // 
            this.lbDescription.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbDescription.Location = new System.Drawing.Point(13, 14);
            this.lbDescription.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbDescription.Name = "lbDescription";
            this.lbDescription.Size = new System.Drawing.Size(584, 149);
            this.lbDescription.TabIndex = 0;
            this.lbDescription.Text = resources.GetString("lbDescription.Text");
            // 
            // lbIterationsStart
            // 
            this.lbIterationsStart.AutoSize = true;
            this.lbIterationsStart.Location = new System.Drawing.Point(13, 211);
            this.lbIterationsStart.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbIterationsStart.Name = "lbIterationsStart";
            this.lbIterationsStart.Size = new System.Drawing.Size(171, 20);
            this.lbIterationsStart.TabIndex = 3;
            this.lbIterationsStart.Text = "Gap Closing Iterations:";
            this.toolTip.SetToolTip(this.lbIterationsStart, resources.GetString("lbIterationsStart.ToolTip"));
            // 
            // numClosingIterations
            // 
            this.numClosingIterations.Location = new System.Drawing.Point(192, 208);
            this.numClosingIterations.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.numClosingIterations.Name = "numClosingIterations";
            this.numClosingIterations.Size = new System.Drawing.Size(89, 26);
            this.numClosingIterations.TabIndex = 3;
            this.numClosingIterations.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lbLayerRange
            // 
            this.lbLayerRange.AutoSize = true;
            this.lbLayerRange.Location = new System.Drawing.Point(13, 171);
            this.lbLayerRange.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbLayerRange.Name = "lbLayerRange";
            this.lbLayerRange.Size = new System.Drawing.Size(97, 20);
            this.lbLayerRange.TabIndex = 9;
            this.lbLayerRange.Text = "Layer range:";
            this.toolTip.SetToolTip(this.lbLayerRange, resources.GetString("lbLayerRange.ToolTip"));
            // 
            // nmLayerRangeStart
            // 
            this.nmLayerRangeStart.Location = new System.Drawing.Point(157, 168);
            this.nmLayerRangeStart.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmLayerRangeStart.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.nmLayerRangeStart.Name = "nmLayerRangeStart";
            this.nmLayerRangeStart.Size = new System.Drawing.Size(124, 26);
            this.nmLayerRangeStart.TabIndex = 0;
            // 
            // nmLayerRangeEnd
            // 
            this.nmLayerRangeEnd.Location = new System.Drawing.Point(328, 168);
            this.nmLayerRangeEnd.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmLayerRangeEnd.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.nmLayerRangeEnd.Name = "nmLayerRangeEnd";
            this.nmLayerRangeEnd.Size = new System.Drawing.Size(124, 26);
            this.nmLayerRangeEnd.TabIndex = 1;
            // 
            // lbLayerRangeTo
            // 
            this.lbLayerRangeTo.AutoSize = true;
            this.lbLayerRangeTo.Location = new System.Drawing.Point(289, 171);
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
            this.btnCancel.Location = new System.Drawing.Point(447, 286);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(150, 48);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnRepair
            // 
            this.btnRepair.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRepair.Image = global::UVtools.GUI.Properties.Resources.Ok_24x24;
            this.btnRepair.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnRepair.Location = new System.Drawing.Point(289, 286);
            this.btnRepair.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnRepair.Name = "btnRepair";
            this.btnRepair.Size = new System.Drawing.Size(150, 48);
            this.btnRepair.TabIndex = 5;
            this.btnRepair.Text = "&Repair";
            this.btnRepair.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnRepair.UseVisualStyleBackColor = true;
            this.btnRepair.Click += new System.EventHandler(this.ItemClicked);
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
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(307, 210);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(190, 20);
            this.label1.TabIndex = 17;
            this.label1.Text = "Noise Removal Iterations:";
            this.toolTip.SetToolTip(this.label1, resources.GetString("label1.ToolTip"));
            // 
            // cbRepairIslands
            // 
            this.cbRepairIslands.AutoSize = true;
            this.cbRepairIslands.Checked = true;
            this.cbRepairIslands.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbRepairIslands.Location = new System.Drawing.Point(17, 246);
            this.cbRepairIslands.Name = "cbRepairIslands";
            this.cbRepairIslands.Size = new System.Drawing.Size(204, 24);
            this.cbRepairIslands.TabIndex = 16;
            this.cbRepairIslands.Text = "Repair layers and islands";
            this.cbRepairIslands.UseVisualStyleBackColor = true;
            // 
            // numOpeningIterations
            // 
            this.numOpeningIterations.Location = new System.Drawing.Point(505, 208);
            this.numOpeningIterations.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.numOpeningIterations.Name = "numOpeningIterations";
            this.numOpeningIterations.Size = new System.Drawing.Size(92, 26);
            this.numOpeningIterations.TabIndex = 18;
            this.numOpeningIterations.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // cbRepairResinTraps
            // 
            this.cbRepairResinTraps.AutoSize = true;
            this.cbRepairResinTraps.Checked = true;
            this.cbRepairResinTraps.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbRepairResinTraps.Location = new System.Drawing.Point(17, 305);
            this.cbRepairResinTraps.Name = "cbRepairResinTraps";
            this.cbRepairResinTraps.Size = new System.Drawing.Size(164, 24);
            this.cbRepairResinTraps.TabIndex = 19;
            this.cbRepairResinTraps.Text = "Repair Resin Traps";
            this.cbRepairResinTraps.UseVisualStyleBackColor = true;
            // 
            // btnLayerRangeSelect
            // 
            this.btnLayerRangeSelect.Location = new System.Drawing.Point(459, 168);
            this.btnLayerRangeSelect.Menu = this.cmLayerRange;
            this.btnLayerRangeSelect.Name = "btnLayerRangeSelect";
            this.btnLayerRangeSelect.Size = new System.Drawing.Size(138, 26);
            this.btnLayerRangeSelect.TabIndex = 2;
            this.btnLayerRangeSelect.Text = "Select";
            this.btnLayerRangeSelect.UseVisualStyleBackColor = true;
            // 
            // cbRemoveEmptyLayers
            // 
            this.cbRemoveEmptyLayers.AutoSize = true;
            this.cbRemoveEmptyLayers.Checked = true;
            this.cbRemoveEmptyLayers.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbRemoveEmptyLayers.Location = new System.Drawing.Point(17, 276);
            this.cbRemoveEmptyLayers.Name = "cbRemoveEmptyLayers";
            this.cbRemoveEmptyLayers.Size = new System.Drawing.Size(179, 24);
            this.cbRemoveEmptyLayers.TabIndex = 20;
            this.cbRemoveEmptyLayers.Text = "Remove empty layers";
            this.cbRemoveEmptyLayers.UseVisualStyleBackColor = true;
            // 
            // nmRemoveIslandsBelowEqual
            // 
            this.nmRemoveIslandsBelowEqualPixels.Location = new System.Drawing.Point(505, 245);
            this.nmRemoveIslandsBelowEqualPixels.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nmRemoveIslandsBelowEqualPixels.Name = "nmRemoveIslandsBelowEqualPixels";
            this.nmRemoveIslandsBelowEqualPixels.Size = new System.Drawing.Size(58, 26);
            this.nmRemoveIslandsBelowEqualPixels.TabIndex = 21;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(248, 248);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(249, 20);
            this.label2.TabIndex = 22;
            this.label2.Text = "Remove islands below or equal to:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(569, 248);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(25, 20);
            this.label3.TabIndex = 23;
            this.label3.Text = "px";
            // 
            // FrmRepairLayers
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(606, 348);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.nmRemoveIslandsBelowEqualPixels);
            this.Controls.Add(this.cbRemoveEmptyLayers);
            this.Controls.Add(this.cbRepairResinTraps);
            this.Controls.Add(this.numOpeningIterations);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbRepairIslands);
            this.Controls.Add(this.btnLayerRangeSelect);
            this.Controls.Add(this.lbLayerRangeTo);
            this.Controls.Add(this.nmLayerRangeEnd);
            this.Controls.Add(this.nmLayerRangeStart);
            this.Controls.Add(this.lbLayerRange);
            this.Controls.Add(this.btnRepair);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.numClosingIterations);
            this.Controls.Add(this.lbIterationsStart);
            this.Controls.Add(this.lbDescription);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmRepairLayers";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Repair Layers";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.numClosingIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeStart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeEnd)).EndInit();
            this.cmLayerRange.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numOpeningIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmRemoveIslandsBelowEqualPixels)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbDescription;
        private System.Windows.Forms.Label lbIterationsStart;
        private System.Windows.Forms.NumericUpDown numClosingIterations;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnRepair;
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
        private System.Windows.Forms.CheckBox cbRepairIslands;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numOpeningIterations;
        private System.Windows.Forms.CheckBox cbRepairResinTraps;
        private System.Windows.Forms.CheckBox cbRemoveEmptyLayers;
        private System.Windows.Forms.NumericUpDown nmRemoveIslandsBelowEqualPixels;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}