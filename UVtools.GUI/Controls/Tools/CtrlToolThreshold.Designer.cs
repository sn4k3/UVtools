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
            this.nmMaximum = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.nmThreshold = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.cbThresholdType = new System.Windows.Forms.ComboBox();
            this.cbPresetHelpers = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nmMaximum)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmThreshold)).BeginInit();
            this.SuspendLayout();
            // 
            // nmMaximum
            // 
            this.nmMaximum.Location = new System.Drawing.Point(270, 51);
            this.nmMaximum.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmMaximum.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nmMaximum.Name = "nmMaximum";
            this.nmMaximum.Size = new System.Drawing.Size(70, 26);
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
            this.label2.Location = new System.Drawing.Point(216, 54);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 20);
            this.label2.TabIndex = 22;
            this.label2.Text = "Max.:";
            // 
            // nmThreshold
            // 
            this.nmThreshold.Location = new System.Drawing.Point(128, 51);
            this.nmThreshold.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmThreshold.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nmThreshold.Name = "nmThreshold";
            this.nmThreshold.Size = new System.Drawing.Size(70, 26);
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
            this.label1.Location = new System.Drawing.Point(4, 54);
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
            this.cbThresholdType.Location = new System.Drawing.Point(347, 50);
            this.cbThresholdType.Name = "cbThresholdType";
            this.cbThresholdType.Size = new System.Drawing.Size(189, 28);
            this.cbThresholdType.TabIndex = 19;
            // 
            // cbPresetHelpers
            // 
            this.cbPresetHelpers.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPresetHelpers.FormattingEnabled = true;
            this.cbPresetHelpers.Items.AddRange(new object[] {
            "Free use",
            "Strip AntiAliasing",
            "Set pixel brightness"});
            this.cbPresetHelpers.Location = new System.Drawing.Point(128, 10);
            this.cbPresetHelpers.Name = "cbPresetHelpers";
            this.cbPresetHelpers.Size = new System.Drawing.Size(408, 28);
            this.cbPresetHelpers.TabIndex = 25;
            this.cbPresetHelpers.SelectedIndexChanged += new System.EventHandler(this.cbPresetHelpers_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(4, 13);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(115, 20);
            this.label3.TabIndex = 26;
            this.label3.Text = "Preset helpers:";
            // 
            // CtrlToolThreshold
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cbPresetHelpers);
            this.Controls.Add(this.nmMaximum);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.nmThreshold);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbThresholdType);
            this.Description = "";
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "CtrlToolThreshold";
            this.Size = new System.Drawing.Size(540, 82);
            ((System.ComponentModel.ISupportInitialize)(this.nmMaximum)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmThreshold)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.NumericUpDown nmMaximum;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nmThreshold;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbThresholdType;
        private System.Windows.Forms.ComboBox cbPresetHelpers;
        private System.Windows.Forms.Label label3;
    }
}
