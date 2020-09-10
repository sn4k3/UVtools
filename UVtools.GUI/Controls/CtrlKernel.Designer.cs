namespace UVtools.GUI.Controls
{
    partial class CtrlKernel
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CtrlKernel));
            this.tbKernel = new System.Windows.Forms.TextBox();
            this.cbShape = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.nmSizeX = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.nmSizeY = new System.Windows.Forms.NumericUpDown();
            this.btnGen = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.nmAnchorY = new System.Windows.Forms.NumericUpDown();
            this.nmAnchorX = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.nmSizeX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmSizeY)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmAnchorY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmAnchorX)).BeginInit();
            this.SuspendLayout();
            // 
            // toolTip
            // 
            this.toolTip.AutoPopDelay = 32767;
            this.toolTip.InitialDelay = 500;
            this.toolTip.ReshowDelay = 100;
            this.toolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.toolTip.ToolTipTitle = "Information";
            // 
            // tbKernel
            // 
            this.tbKernel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbKernel.Location = new System.Drawing.Point(6, 25);
            this.tbKernel.Multiline = true;
            this.tbKernel.Name = "tbKernel";
            this.tbKernel.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbKernel.Size = new System.Drawing.Size(210, 178);
            this.tbKernel.TabIndex = 1;
            this.toolTip.SetToolTip(this.tbKernel, resources.GetString("tbKernel.ToolTip"));
            this.tbKernel.WordWrap = false;
            // 
            // cbShape
            // 
            this.cbShape.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbShape.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbShape.FormattingEnabled = true;
            this.cbShape.Location = new System.Drawing.Point(308, 24);
            this.cbShape.Name = "cbShape";
            this.cbShape.Size = new System.Drawing.Size(133, 28);
            this.cbShape.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(248, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Shape:";
            // 
            // nmSizeX
            // 
            this.nmSizeX.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nmSizeX.Location = new System.Drawing.Point(308, 58);
            this.nmSizeX.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nmSizeX.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmSizeX.Name = "nmSizeX";
            this.nmSizeX.Size = new System.Drawing.Size(50, 26);
            this.nmSizeX.TabIndex = 3;
            this.nmSizeX.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(258, 61);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 20);
            this.label2.TabIndex = 4;
            this.label2.Text = "Size:";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(366, 61);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(16, 20);
            this.label3.TabIndex = 5;
            this.label3.Text = "x";
            // 
            // nmSizeY
            // 
            this.nmSizeY.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nmSizeY.Location = new System.Drawing.Point(392, 58);
            this.nmSizeY.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nmSizeY.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmSizeY.Name = "nmSizeY";
            this.nmSizeY.Size = new System.Drawing.Size(50, 26);
            this.nmSizeY.TabIndex = 6;
            this.nmSizeY.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // btnGen
            // 
            this.btnGen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGen.Location = new System.Drawing.Point(226, 147);
            this.btnGen.Name = "btnGen";
            this.btnGen.Size = new System.Drawing.Size(131, 56);
            this.btnGen.TabIndex = 7;
            this.btnGen.Text = "Generate";
            this.btnGen.UseVisualStyleBackColor = true;
            this.btnGen.Click += new System.EventHandler(this.EventClick);
            // 
            // btnReset
            // 
            this.btnReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReset.Location = new System.Drawing.Point(367, 147);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(74, 56);
            this.btnReset.TabIndex = 8;
            this.btnReset.Text = "Reset";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.EventClick);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.nmAnchorY);
            this.groupBox1.Controls.Add(this.nmAnchorX);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.btnReset);
            this.groupBox1.Controls.Add(this.btnGen);
            this.groupBox1.Controls.Add(this.tbKernel);
            this.groupBox1.Controls.Add(this.nmSizeY);
            this.groupBox1.Controls.Add(this.cbShape);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.nmSizeX);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(448, 215);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Kernel";
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(363, 93);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(24, 20);
            this.label5.TabIndex = 12;
            this.label5.Text = "Y:";
            // 
            // nmAnchorY
            // 
            this.nmAnchorY.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nmAnchorY.Location = new System.Drawing.Point(392, 90);
            this.nmAnchorY.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nmAnchorY.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.nmAnchorY.Name = "nmAnchorY";
            this.nmAnchorY.Size = new System.Drawing.Size(50, 26);
            this.nmAnchorY.TabIndex = 11;
            this.toolTip.SetToolTip(this.nmAnchorY, "Y coordinate of the kernel origin, -1 for auto-center");
            this.nmAnchorY.Value = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            // 
            // nmAnchorX
            // 
            this.nmAnchorX.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nmAnchorX.Location = new System.Drawing.Point(308, 90);
            this.nmAnchorX.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nmAnchorX.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.nmAnchorX.Name = "nmAnchorX";
            this.nmAnchorX.Size = new System.Drawing.Size(50, 26);
            this.nmAnchorX.TabIndex = 10;
            this.toolTip.SetToolTip(this.nmAnchorX, "X coordinate of the kenel origin, -1 for auto-center.");
            this.nmAnchorX.Value = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(223, 93);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(79, 20);
            this.label4.TabIndex = 9;
            this.label4.Text = "Anchor X:";
            // 
            // CtrlKernel
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.groupBox1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "CtrlKernel";
            this.Size = new System.Drawing.Size(448, 215);
            ((System.ComponentModel.ISupportInitialize)(this.nmSizeX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmSizeY)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmAnchorY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmAnchorX)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ComboBox cbShape;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nmSizeX;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nmSizeY;
        private System.Windows.Forms.Button btnGen;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown nmAnchorY;
        private System.Windows.Forms.NumericUpDown nmAnchorX;
        private System.Windows.Forms.Label label4;
        public System.Windows.Forms.TextBox tbKernel;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
