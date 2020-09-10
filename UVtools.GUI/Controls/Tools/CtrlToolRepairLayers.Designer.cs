namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolRepairLayers
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CtrlToolRepairLayers));
            this.groupAdvancedSettings = new System.Windows.Forms.GroupBox();
            this.nmOpeningIterations = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.nmClosingIterations = new System.Windows.Forms.NumericUpDown();
            this.lbIterationsStart = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.nmRemoveIslandsBelowEqualPixels = new System.Windows.Forms.NumericUpDown();
            this.cbRemoveEmptyLayers = new System.Windows.Forms.CheckBox();
            this.cbRepairResinTraps = new System.Windows.Forms.CheckBox();
            this.cbRepairIslands = new System.Windows.Forms.CheckBox();
            this.groupAdvancedSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmOpeningIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmClosingIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmRemoveIslandsBelowEqualPixels)).BeginInit();
            this.SuspendLayout();
            // 
            // groupAdvancedSettings
            // 
            this.groupAdvancedSettings.Controls.Add(this.nmOpeningIterations);
            this.groupAdvancedSettings.Controls.Add(this.label1);
            this.groupAdvancedSettings.Controls.Add(this.nmClosingIterations);
            this.groupAdvancedSettings.Controls.Add(this.lbIterationsStart);
            this.groupAdvancedSettings.Location = new System.Drawing.Point(3, 87);
            this.groupAdvancedSettings.Name = "groupAdvancedSettings";
            this.groupAdvancedSettings.Size = new System.Drawing.Size(601, 110);
            this.groupAdvancedSettings.TabIndex = 31;
            this.groupAdvancedSettings.TabStop = false;
            this.groupAdvancedSettings.Text = "Advanced Settings";
            this.groupAdvancedSettings.Visible = false;
            // 
            // nmOpeningIterations
            // 
            this.nmOpeningIterations.Location = new System.Drawing.Point(196, 68);
            this.nmOpeningIterations.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmOpeningIterations.Name = "nmOpeningIterations";
            this.nmOpeningIterations.Size = new System.Drawing.Size(89, 26);
            this.nmOpeningIterations.TabIndex = 27;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 71);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(181, 20);
            this.label1.TabIndex = 26;
            this.label1.Text = "Noise removal iterations:";
            this.toolTip.SetToolTip(this.label1, resources.GetString("label1.ToolTip"));
            // 
            // nmClosingIterations
            // 
            this.nmClosingIterations.Location = new System.Drawing.Point(196, 30);
            this.nmClosingIterations.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmClosingIterations.Name = "nmClosingIterations";
            this.nmClosingIterations.Size = new System.Drawing.Size(89, 26);
            this.nmClosingIterations.TabIndex = 22;
            this.nmClosingIterations.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lbIterationsStart
            // 
            this.lbIterationsStart.AutoSize = true;
            this.lbIterationsStart.Location = new System.Drawing.Point(7, 33);
            this.lbIterationsStart.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbIterationsStart.Name = "lbIterationsStart";
            this.lbIterationsStart.Size = new System.Drawing.Size(166, 20);
            this.lbIterationsStart.TabIndex = 23;
            this.lbIterationsStart.Text = "Gap closing iterations:";
            this.toolTip.SetToolTip(this.lbIterationsStart, resources.GetString("lbIterationsStart.ToolTip"));
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(319, 46);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(25, 20);
            this.label3.TabIndex = 30;
            this.label3.Text = "px";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 46);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(250, 20);
            this.label2.TabIndex = 29;
            this.label2.Text = "Do not remove islands larger than:";
            this.toolTip.SetToolTip(this.label2, "The pixel area theshold above which islands will not be removed by this repair.  " +
        " Islands remaining after repair will require supports to be added manually.");
            // 
            // nmRemoveIslandsBelowEqualPixels
            // 
            this.nmRemoveIslandsBelowEqualPixels.Location = new System.Drawing.Point(259, 43);
            this.nmRemoveIslandsBelowEqualPixels.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nmRemoveIslandsBelowEqualPixels.Name = "nmRemoveIslandsBelowEqualPixels";
            this.nmRemoveIslandsBelowEqualPixels.Size = new System.Drawing.Size(58, 26);
            this.nmRemoveIslandsBelowEqualPixels.TabIndex = 28;
            this.toolTip.SetToolTip(this.nmRemoveIslandsBelowEqualPixels, "The pixel area theshold above which islands will not be removed by this repair.  " +
        " Islands remaining after repair will require supports to be added manually.");
            // 
            // cbRemoveEmptyLayers
            // 
            this.cbRemoveEmptyLayers.AutoSize = true;
            this.cbRemoveEmptyLayers.Checked = true;
            this.cbRemoveEmptyLayers.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbRemoveEmptyLayers.Location = new System.Drawing.Point(425, 8);
            this.cbRemoveEmptyLayers.Name = "cbRemoveEmptyLayers";
            this.cbRemoveEmptyLayers.Size = new System.Drawing.Size(179, 24);
            this.cbRemoveEmptyLayers.TabIndex = 27;
            this.cbRemoveEmptyLayers.Text = "Remove empty layers";
            this.toolTip.SetToolTip(this.cbRemoveEmptyLayers, "If enabled, repair will remove all layers with no white pixels.  The model will b" +
        "e recalculated to ensure the correct Z height is maintained.");
            this.cbRemoveEmptyLayers.UseVisualStyleBackColor = true;
            // 
            // cbRepairResinTraps
            // 
            this.cbRepairResinTraps.AutoSize = true;
            this.cbRepairResinTraps.Checked = true;
            this.cbRepairResinTraps.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbRepairResinTraps.Location = new System.Drawing.Point(208, 8);
            this.cbRepairResinTraps.Name = "cbRepairResinTraps";
            this.cbRepairResinTraps.Size = new System.Drawing.Size(153, 24);
            this.cbRepairResinTraps.TabIndex = 26;
            this.cbRepairResinTraps.Text = "Repair resin traps";
            this.toolTip.SetToolTip(this.cbRepairResinTraps, "If enabled, repair will fill black pixels found within a resin trap with white pi" +
        "xels.  Hollow areas will become solid.");
            this.cbRepairResinTraps.UseVisualStyleBackColor = true;
            // 
            // cbRepairIslands
            // 
            this.cbRepairIslands.AutoSize = true;
            this.cbRepairIslands.Checked = true;
            this.cbRepairIslands.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbRepairIslands.Location = new System.Drawing.Point(7, 8);
            this.cbRepairIslands.Name = "cbRepairIslands";
            this.cbRepairIslands.Size = new System.Drawing.Size(128, 24);
            this.cbRepairIslands.TabIndex = 25;
            this.cbRepairIslands.Text = "Repair islands";
            this.toolTip.SetToolTip(this.cbRepairIslands, "If enabled, repair will first attempt to eliminate islands smaller than the pixel" +
        " area removal threshold, and then runs the “gap closure” technique.\r\n");
            this.cbRepairIslands.UseVisualStyleBackColor = true;
            // 
            // CtrlToolRepairLayers
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.groupAdvancedSettings);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.nmRemoveIslandsBelowEqualPixels);
            this.Controls.Add(this.cbRemoveEmptyLayers);
            this.Controls.Add(this.cbRepairResinTraps);
            this.Controls.Add(this.cbRepairIslands);
            this.Description = "";
            this.ExtraCheckboxVisible = true;
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LayerRangeVisible = false;
            this.Name = "CtrlToolRepairLayers";
            this.Size = new System.Drawing.Size(607, 200);
            this.groupAdvancedSettings.ResumeLayout(false);
            this.groupAdvancedSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmOpeningIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmClosingIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmRemoveIslandsBelowEqualPixels)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupAdvancedSettings;
        private System.Windows.Forms.NumericUpDown nmOpeningIterations;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nmClosingIterations;
        private System.Windows.Forms.Label lbIterationsStart;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nmRemoveIslandsBelowEqualPixels;
        private System.Windows.Forms.CheckBox cbRemoveEmptyLayers;
        private System.Windows.Forms.CheckBox cbRepairResinTraps;
        private System.Windows.Forms.CheckBox cbRepairIslands;
    }
}
