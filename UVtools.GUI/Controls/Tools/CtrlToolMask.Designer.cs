namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolMask
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
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnMaskGenerate = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.nmGeneratorDiameter = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.nmGeneratorMaxBrightness = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.nmGeneratorMinBrightness = new System.Windows.Forms.NumericUpDown();
            this.cbInvertMask = new System.Windows.Forms.CheckBox();
            this.lbMaskResolution = new System.Windows.Forms.Label();
            this.lbPrinterResolution = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.pbMask = new System.Windows.Forms.PictureBox();
            this.btnImportImageMask = new System.Windows.Forms.Button();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmGeneratorDiameter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmGeneratorMaxBrightness)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmGeneratorMinBrightness)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbMask)).BeginInit();
            this.SuspendLayout();
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
            this.groupBox2.Location = new System.Drawing.Point(3, 41);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(564, 96);
            this.groupBox2.TabIndex = 42;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Mask Generator (Round from center)";
            // 
            // btnMaskGenerate
            // 
            this.btnMaskGenerate.Location = new System.Drawing.Point(259, 57);
            this.btnMaskGenerate.Name = "btnMaskGenerate";
            this.btnMaskGenerate.Size = new System.Drawing.Size(299, 26);
            this.btnMaskGenerate.TabIndex = 41;
            this.btnMaskGenerate.Text = "Generate";
            this.btnMaskGenerate.UseVisualStyleBackColor = true;
            this.btnMaskGenerate.Click += new System.EventHandler(this.EventClick);
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
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(255, 28);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 20);
            this.label3.TabIndex = 38;
            this.label3.Text = "(0-255)";
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
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(154, 20);
            this.label1.TabIndex = 35;
            this.label1.Text = "Minimum brightness:";
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
            // cbInvertMask
            // 
            this.cbInvertMask.AutoSize = true;
            this.cbInvertMask.Enabled = false;
            this.cbInvertMask.Location = new System.Drawing.Point(432, 8);
            this.cbInvertMask.Name = "cbInvertMask";
            this.cbInvertMask.Size = new System.Drawing.Size(110, 24);
            this.cbInvertMask.TabIndex = 41;
            this.cbInvertMask.Text = "Invert Mask";
            this.cbInvertMask.UseVisualStyleBackColor = true;
            this.cbInvertMask.CheckedChanged += new System.EventHandler(this.EventClick);
            // 
            // lbMaskResolution
            // 
            this.lbMaskResolution.AutoSize = true;
            this.lbMaskResolution.Location = new System.Drawing.Point(-1, 174);
            this.lbMaskResolution.Name = "lbMaskResolution";
            this.lbMaskResolution.Size = new System.Drawing.Size(207, 20);
            this.lbMaskResolution.TabIndex = 40;
            this.lbMaskResolution.Text = "Mask resolution: (Unloaded)";
            // 
            // lbPrinterResolution
            // 
            this.lbPrinterResolution.AutoSize = true;
            this.lbPrinterResolution.Location = new System.Drawing.Point(-1, 144);
            this.lbPrinterResolution.Name = "lbPrinterResolution";
            this.lbPrinterResolution.Size = new System.Drawing.Size(136, 20);
            this.lbPrinterResolution.TabIndex = 39;
            this.lbPrinterResolution.Text = "Printer resolution: ";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.pbMask);
            this.groupBox1.Location = new System.Drawing.Point(0, 209);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(567, 335);
            this.groupBox1.TabIndex = 38;
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
            // btnImportImageMask
            // 
            this.btnImportImageMask.Location = new System.Drawing.Point(3, 3);
            this.btnImportImageMask.Name = "btnImportImageMask";
            this.btnImportImageMask.Size = new System.Drawing.Size(417, 32);
            this.btnImportImageMask.TabIndex = 37;
            this.btnImportImageMask.Text = "Import grayscale mask image from file";
            this.btnImportImageMask.UseVisualStyleBackColor = true;
            this.btnImportImageMask.Click += new System.EventHandler(this.EventClick);
            // 
            // CtrlToolMask
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ButtonOkEnabled = false;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.cbInvertMask);
            this.Controls.Add(this.lbMaskResolution);
            this.Controls.Add(this.lbPrinterResolution);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnImportImageMask);
            this.Description = "";
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "CtrlToolMask";
            this.Size = new System.Drawing.Size(570, 547);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmGeneratorDiameter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmGeneratorMaxBrightness)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmGeneratorMinBrightness)).EndInit();
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbMask)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnMaskGenerate;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown nmGeneratorDiameter;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nmGeneratorMaxBrightness;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nmGeneratorMinBrightness;
        private System.Windows.Forms.CheckBox cbInvertMask;
        private System.Windows.Forms.Label lbMaskResolution;
        private System.Windows.Forms.Label lbPrinterResolution;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.PictureBox pbMask;
        private System.Windows.Forms.Button btnImportImageMask;
    }
}
