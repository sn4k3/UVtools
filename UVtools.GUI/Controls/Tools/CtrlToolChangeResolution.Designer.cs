namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolChangeResolution
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
            this.lbObjectVolume = new System.Windows.Forms.Label();
            this.nmNewY = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.nmNewX = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.cbPreset = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.lbCurrent = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nmNewY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmNewX)).BeginInit();
            this.SuspendLayout();
            // 
            // lbObjectVolume
            // 
            this.lbObjectVolume.AutoSize = true;
            this.lbObjectVolume.Location = new System.Drawing.Point(4, 45);
            this.lbObjectVolume.Name = "lbObjectVolume";
            this.lbObjectVolume.Size = new System.Drawing.Size(113, 20);
            this.lbObjectVolume.TabIndex = 23;
            this.lbObjectVolume.Text = "Object volume:";
            // 
            // nmNewY
            // 
            this.nmNewY.Location = new System.Drawing.Point(192, 75);
            this.nmNewY.Maximum = new decimal(new int[] {
            20000,
            0,
            0,
            0});
            this.nmNewY.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nmNewY.Name = "nmNewY";
            this.nmNewY.Size = new System.Drawing.Size(80, 26);
            this.nmNewY.TabIndex = 22;
            this.nmNewY.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nmNewY.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(170, 78);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(16, 20);
            this.label3.TabIndex = 21;
            this.label3.Text = "x";
            // 
            // nmNewX
            // 
            this.nmNewX.Location = new System.Drawing.Point(84, 75);
            this.nmNewX.Maximum = new decimal(new int[] {
            20000,
            0,
            0,
            0});
            this.nmNewX.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nmNewX.Name = "nmNewX";
            this.nmNewX.Size = new System.Drawing.Size(80, 26);
            this.nmNewX.TabIndex = 20;
            this.nmNewX.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nmNewX.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 77);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 20);
            this.label1.TabIndex = 19;
            this.label1.Text = "New X/Y:";
            // 
            // cbPreset
            // 
            this.cbPreset.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbPreset.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPreset.FormattingEnabled = true;
            this.cbPreset.Location = new System.Drawing.Point(343, 75);
            this.cbPreset.Name = "cbPreset";
            this.cbPreset.Size = new System.Drawing.Size(191, 28);
            this.cbPreset.TabIndex = 18;
            this.cbPreset.SelectedIndexChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(278, 78);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 20);
            this.label2.TabIndex = 17;
            this.label2.Text = "Preset:";
            // 
            // lbCurrent
            // 
            this.lbCurrent.AutoSize = true;
            this.lbCurrent.Location = new System.Drawing.Point(3, 13);
            this.lbCurrent.Name = "lbCurrent";
            this.lbCurrent.Size = new System.Drawing.Size(139, 20);
            this.lbCurrent.TabIndex = 16;
            this.lbCurrent.Text = "Current resolution:";
            // 
            // CtrlToolChangeResolution
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.lbObjectVolume);
            this.Controls.Add(this.nmNewY);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.nmNewX);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbPreset);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lbCurrent);
            this.Description = "";
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LayerRangeVisible = false;
            this.Name = "CtrlToolChangeResolution";
            this.Size = new System.Drawing.Size(540, 123);
            ((System.ComponentModel.ISupportInitialize)(this.nmNewY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmNewX)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbObjectVolume;
        private System.Windows.Forms.NumericUpDown nmNewY;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nmNewX;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbPreset;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lbCurrent;
    }
}
