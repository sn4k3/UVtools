namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolThreshold
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnPresetSetPixelsBrightness = new System.Windows.Forms.Button();
            this.btnPresetFreeUse = new System.Windows.Forms.Button();
            this.btnPresetStripAntiAliasing = new System.Windows.Forms.Button();
            this.nmMaximum = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.nmThreshold = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.cbThresholdType = new System.Windows.Forms.ComboBox();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmMaximum)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmThreshold)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnPresetSetPixelsBrightness);
            this.groupBox1.Controls.Add(this.btnPresetFreeUse);
            this.groupBox1.Controls.Add(this.btnPresetStripAntiAliasing);
            this.groupBox1.Location = new System.Drawing.Point(8, 47);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(567, 77);
            this.groupBox1.TabIndex = 24;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Presets / Helpers";
            // 
            // btnPresetSetPixelsBrightness
            // 
            this.btnPresetSetPixelsBrightness.BackColor = System.Drawing.SystemColors.Control;
            this.btnPresetSetPixelsBrightness.Location = new System.Drawing.Point(338, 25);
            this.btnPresetSetPixelsBrightness.Name = "btnPresetSetPixelsBrightness";
            this.btnPresetSetPixelsBrightness.Size = new System.Drawing.Size(223, 36);
            this.btnPresetSetPixelsBrightness.TabIndex = 2;
            this.btnPresetSetPixelsBrightness.Text = "Set pixels brightness";
            this.btnPresetSetPixelsBrightness.UseVisualStyleBackColor = false;
            this.btnPresetSetPixelsBrightness.Click += new System.EventHandler(this.EventClick);
            // 
            // btnPresetFreeUse
            // 
            this.btnPresetFreeUse.BackColor = System.Drawing.SystemColors.Control;
            this.btnPresetFreeUse.Location = new System.Drawing.Point(6, 25);
            this.btnPresetFreeUse.Name = "btnPresetFreeUse";
            this.btnPresetFreeUse.Size = new System.Drawing.Size(160, 36);
            this.btnPresetFreeUse.TabIndex = 1;
            this.btnPresetFreeUse.Text = "Free use";
            this.btnPresetFreeUse.UseVisualStyleBackColor = false;
            this.btnPresetFreeUse.Click += new System.EventHandler(this.EventClick);
            // 
            // btnPresetStripAntiAliasing
            // 
            this.btnPresetStripAntiAliasing.BackColor = System.Drawing.SystemColors.Control;
            this.btnPresetStripAntiAliasing.Location = new System.Drawing.Point(172, 25);
            this.btnPresetStripAntiAliasing.Name = "btnPresetStripAntiAliasing";
            this.btnPresetStripAntiAliasing.Size = new System.Drawing.Size(160, 36);
            this.btnPresetStripAntiAliasing.TabIndex = 0;
            this.btnPresetStripAntiAliasing.Text = "Strip AntiAliasing";
            this.btnPresetStripAntiAliasing.UseVisualStyleBackColor = false;
            this.btnPresetStripAntiAliasing.Click += new System.EventHandler(this.EventClick);
            // 
            // nmMaximum
            // 
            this.nmMaximum.Location = new System.Drawing.Point(305, 7);
            this.nmMaximum.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmMaximum.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nmMaximum.Name = "nmMaximum";
            this.nmMaximum.Size = new System.Drawing.Size(120, 26);
            this.nmMaximum.TabIndex = 23;
            this.nmMaximum.Value = new decimal(new int[] {
            255,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(251, 10);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 20);
            this.label2.TabIndex = 22;
            this.label2.Text = "Max.:";
            // 
            // nmThreshold
            // 
            this.nmThreshold.Location = new System.Drawing.Point(109, 7);
            this.nmThreshold.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmThreshold.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nmThreshold.Name = "nmThreshold";
            this.nmThreshold.Size = new System.Drawing.Size(120, 26);
            this.nmThreshold.TabIndex = 21;
            this.nmThreshold.Value = new decimal(new int[] {
            127,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 10);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 20);
            this.label1.TabIndex = 20;
            this.label1.Text = "Threshold:";
            // 
            // cbThresholdType
            // 
            this.cbThresholdType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbThresholdType.FormattingEnabled = true;
            this.cbThresholdType.Location = new System.Drawing.Point(437, 6);
            this.cbThresholdType.Name = "cbThresholdType";
            this.cbThresholdType.Size = new System.Drawing.Size(138, 28);
            this.cbThresholdType.TabIndex = 19;
            // 
            // CtrlToolThreshold
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.nmMaximum);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.nmThreshold);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbThresholdType);
            this.Description = "";
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "CtrlToolThreshold";
            this.Size = new System.Drawing.Size(578, 127);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nmMaximum)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmThreshold)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnPresetSetPixelsBrightness;
        private System.Windows.Forms.Button btnPresetFreeUse;
        private System.Windows.Forms.Button btnPresetStripAntiAliasing;
        private System.Windows.Forms.NumericUpDown nmMaximum;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nmThreshold;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbThresholdType;
    }
}
