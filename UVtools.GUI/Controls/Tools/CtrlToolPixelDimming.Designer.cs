namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolPixelDimming
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CtrlToolPixelDimming));
            this.cbDimsOnlyBorders = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.nmInfillSpacing = new System.Windows.Forms.NumericUpDown();
            this.label12 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.btnInfillPatternWaves = new System.Windows.Forms.Button();
            this.btnInfillPatternSquareGrid = new System.Windows.Forms.Button();
            this.btnInfillPatternRectilinear = new System.Windows.Forms.Button();
            this.nmInfillThickness = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.tbOddPattern = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnDimPatternSolid = new System.Windows.Forms.Button();
            this.btnDimPatternStrips = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.btnDimPatternWaves = new System.Windows.Forms.Button();
            this.btnPatternRandom = new System.Windows.Forms.Button();
            this.btnDimPatternSlashes = new System.Windows.Forms.Button();
            this.btnDimPatternHearts = new System.Windows.Forms.Button();
            this.btnDimPatternRhombus = new System.Windows.Forms.Button();
            this.btnDimPatternPyramid = new System.Windows.Forms.Button();
            this.btnDimPatternCrosses = new System.Windows.Forms.Button();
            this.btnDimPatternSparse = new System.Windows.Forms.Button();
            this.btnDimPatternChessBoard = new System.Windows.Forms.Button();
            this.nmPixelDimBrightness = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.tbEvenPattern = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.nmBorderSize = new System.Windows.Forms.NumericUpDown();
            this.lbX = new System.Windows.Forms.Label();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmInfillSpacing)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmInfillThickness)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelDimBrightness)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmBorderSize)).BeginInit();
            this.SuspendLayout();
            // 
            // cbDimsOnlyBorders
            // 
            this.cbDimsOnlyBorders.AutoSize = true;
            this.cbDimsOnlyBorders.Location = new System.Drawing.Point(301, 9);
            this.cbDimsOnlyBorders.Name = "cbDimsOnlyBorders";
            this.cbDimsOnlyBorders.Size = new System.Drawing.Size(146, 24);
            this.cbDimsOnlyBorders.TabIndex = 42;
            this.cbDimsOnlyBorders.Text = "Dim only borders";
            this.cbDimsOnlyBorders.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label13);
            this.groupBox2.Controls.Add(this.label11);
            this.groupBox2.Controls.Add(this.nmInfillSpacing);
            this.groupBox2.Controls.Add(this.label12);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.btnInfillPatternWaves);
            this.groupBox2.Controls.Add(this.btnInfillPatternSquareGrid);
            this.groupBox2.Controls.Add(this.btnInfillPatternRectilinear);
            this.groupBox2.Controls.Add(this.nmInfillThickness);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Location = new System.Drawing.Point(-1, 476);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(575, 149);
            this.groupBox2.TabIndex = 41;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Infill generator";
            // 
            // label13
            // 
            this.label13.Location = new System.Drawing.Point(7, 30);
            this.label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(561, 30);
            this.label13.TabIndex = 34;
            this.label13.Text = "Warning: This function can genearte a large number of resin traps.";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(543, 76);
            this.label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(25, 20);
            this.label11.TabIndex = 33;
            this.label11.Text = "px";
            // 
            // nmInfillSpacing
            // 
            this.nmInfillSpacing.Location = new System.Drawing.Point(422, 73);
            this.nmInfillSpacing.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmInfillSpacing.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmInfillSpacing.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nmInfillSpacing.Name = "nmInfillSpacing";
            this.nmInfillSpacing.Size = new System.Drawing.Size(113, 26);
            this.nmInfillSpacing.TabIndex = 31;
            this.nmInfillSpacing.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(343, 76);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(71, 20);
            this.label12.TabIndex = 32;
            this.label12.Text = "Spacing:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(233, 76);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(25, 20);
            this.label8.TabIndex = 30;
            this.label8.Text = "px";
            // 
            // btnInfillPatternWaves
            // 
            this.btnInfillPatternWaves.Location = new System.Drawing.Point(351, 107);
            this.btnInfillPatternWaves.Name = "btnInfillPatternWaves";
            this.btnInfillPatternWaves.Size = new System.Drawing.Size(94, 35);
            this.btnInfillPatternWaves.TabIndex = 28;
            this.btnInfillPatternWaves.Text = "Waves";
            this.btnInfillPatternWaves.UseVisualStyleBackColor = true;
            this.btnInfillPatternWaves.Click += new System.EventHandler(this.EventClick);
            // 
            // btnInfillPatternSquareGrid
            // 
            this.btnInfillPatternSquareGrid.Location = new System.Drawing.Point(232, 107);
            this.btnInfillPatternSquareGrid.Name = "btnInfillPatternSquareGrid";
            this.btnInfillPatternSquareGrid.Size = new System.Drawing.Size(113, 35);
            this.btnInfillPatternSquareGrid.TabIndex = 26;
            this.btnInfillPatternSquareGrid.Text = "Square Grid";
            this.btnInfillPatternSquareGrid.UseVisualStyleBackColor = true;
            this.btnInfillPatternSquareGrid.Click += new System.EventHandler(this.EventClick);
            // 
            // btnInfillPatternRectilinear
            // 
            this.btnInfillPatternRectilinear.Location = new System.Drawing.Point(112, 107);
            this.btnInfillPatternRectilinear.Name = "btnInfillPatternRectilinear";
            this.btnInfillPatternRectilinear.Size = new System.Drawing.Size(114, 35);
            this.btnInfillPatternRectilinear.TabIndex = 22;
            this.btnInfillPatternRectilinear.Text = "Rectilinear";
            this.btnInfillPatternRectilinear.UseVisualStyleBackColor = true;
            this.btnInfillPatternRectilinear.Click += new System.EventHandler(this.EventClick);
            // 
            // nmInfillThickness
            // 
            this.nmInfillThickness.Location = new System.Drawing.Point(112, 73);
            this.nmInfillThickness.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmInfillThickness.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmInfillThickness.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nmInfillThickness.Name = "nmInfillThickness";
            this.nmInfillThickness.Size = new System.Drawing.Size(113, 26);
            this.nmInfillThickness.TabIndex = 20;
            this.nmInfillThickness.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(7, 114);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(65, 20);
            this.label9.TabIndex = 19;
            this.label9.Text = "Pattern:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(7, 76);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(84, 20);
            this.label10.TabIndex = 21;
            this.label10.Text = "Thickness:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(4, 184);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(94, 100);
            this.label5.TabIndex = 39;
            this.label5.Text = "Odd Layer\r\nPattern:\r\n0 = Black\r\n255 = White\r\n(Optional)";
            // 
            // tbOddPattern
            // 
            this.tbOddPattern.Location = new System.Drawing.Point(105, 172);
            this.tbOddPattern.Multiline = true;
            this.tbOddPattern.Name = "tbOddPattern";
            this.tbOddPattern.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbOddPattern.Size = new System.Drawing.Size(469, 124);
            this.tbOddPattern.TabIndex = 38;
            this.tbOddPattern.Text = "255 255 127 255\r\n127 255 255 255";
            this.toolTip.SetToolTip(this.tbOddPattern, "Leave this field empty in order to use only the even layer pattern.");
            this.tbOddPattern.WordWrap = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnDimPatternSolid);
            this.groupBox1.Controls.Add(this.btnDimPatternStrips);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.btnDimPatternWaves);
            this.groupBox1.Controls.Add(this.btnPatternRandom);
            this.groupBox1.Controls.Add(this.btnDimPatternSlashes);
            this.groupBox1.Controls.Add(this.btnDimPatternHearts);
            this.groupBox1.Controls.Add(this.btnDimPatternRhombus);
            this.groupBox1.Controls.Add(this.btnDimPatternPyramid);
            this.groupBox1.Controls.Add(this.btnDimPatternCrosses);
            this.groupBox1.Controls.Add(this.btnDimPatternSparse);
            this.groupBox1.Controls.Add(this.btnDimPatternChessBoard);
            this.groupBox1.Controls.Add(this.nmPixelDimBrightness);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Location = new System.Drawing.Point(-1, 308);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(575, 162);
            this.groupBox1.TabIndex = 37;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Pixel dimming generator";
            // 
            // btnDimPatternSolid
            // 
            this.btnDimPatternSolid.Location = new System.Drawing.Point(432, 25);
            this.btnDimPatternSolid.Name = "btnDimPatternSolid";
            this.btnDimPatternSolid.Size = new System.Drawing.Size(94, 35);
            this.btnDimPatternSolid.TabIndex = 32;
            this.btnDimPatternSolid.Text = "Solid";
            this.btnDimPatternSolid.UseVisualStyleBackColor = true;
            this.btnDimPatternSolid.Click += new System.EventHandler(this.EventClick);
            // 
            // btnDimPatternStrips
            // 
            this.btnDimPatternStrips.Location = new System.Drawing.Point(332, 25);
            this.btnDimPatternStrips.Name = "btnDimPatternStrips";
            this.btnDimPatternStrips.Size = new System.Drawing.Size(94, 35);
            this.btnDimPatternStrips.TabIndex = 31;
            this.btnDimPatternStrips.Text = "Strips";
            this.btnDimPatternStrips.UseVisualStyleBackColor = true;
            this.btnDimPatternStrips.Click += new System.EventHandler(this.EventClick);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(233, 34);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(60, 20);
            this.label7.TabIndex = 30;
            this.label7.Text = "(0-254)";
            // 
            // btnDimPatternWaves
            // 
            this.btnDimPatternWaves.Location = new System.Drawing.Point(232, 106);
            this.btnDimPatternWaves.Name = "btnDimPatternWaves";
            this.btnDimPatternWaves.Size = new System.Drawing.Size(94, 35);
            this.btnDimPatternWaves.TabIndex = 29;
            this.btnDimPatternWaves.Text = "Waves";
            this.btnDimPatternWaves.UseVisualStyleBackColor = true;
            this.btnDimPatternWaves.Click += new System.EventHandler(this.EventClick);
            // 
            // btnPatternRandom
            // 
            this.btnPatternRandom.Location = new System.Drawing.Point(6, 121);
            this.btnPatternRandom.Name = "btnPatternRandom";
            this.btnPatternRandom.Size = new System.Drawing.Size(94, 35);
            this.btnPatternRandom.TabIndex = 29;
            this.btnPatternRandom.Text = "Random";
            this.btnPatternRandom.UseVisualStyleBackColor = true;
            this.btnPatternRandom.Visible = false;
            // 
            // btnDimPatternSlashes
            // 
            this.btnDimPatternSlashes.Location = new System.Drawing.Point(332, 106);
            this.btnDimPatternSlashes.Name = "btnDimPatternSlashes";
            this.btnDimPatternSlashes.Size = new System.Drawing.Size(94, 35);
            this.btnDimPatternSlashes.TabIndex = 28;
            this.btnDimPatternSlashes.Text = "Slashes";
            this.btnDimPatternSlashes.UseVisualStyleBackColor = true;
            this.btnDimPatternSlashes.Click += new System.EventHandler(this.EventClick);
            // 
            // btnDimPatternHearts
            // 
            this.btnDimPatternHearts.Location = new System.Drawing.Point(432, 106);
            this.btnDimPatternHearts.Name = "btnDimPatternHearts";
            this.btnDimPatternHearts.Size = new System.Drawing.Size(94, 35);
            this.btnDimPatternHearts.TabIndex = 27;
            this.btnDimPatternHearts.Text = "Hearts";
            this.btnDimPatternHearts.UseVisualStyleBackColor = true;
            this.btnDimPatternHearts.Click += new System.EventHandler(this.EventClick);
            // 
            // btnDimPatternRhombus
            // 
            this.btnDimPatternRhombus.Location = new System.Drawing.Point(112, 106);
            this.btnDimPatternRhombus.Name = "btnDimPatternRhombus";
            this.btnDimPatternRhombus.Size = new System.Drawing.Size(113, 35);
            this.btnDimPatternRhombus.TabIndex = 26;
            this.btnDimPatternRhombus.Text = "Rhombus";
            this.btnDimPatternRhombus.UseVisualStyleBackColor = true;
            this.btnDimPatternRhombus.Click += new System.EventHandler(this.EventClick);
            // 
            // btnDimPatternPyramid
            // 
            this.btnDimPatternPyramid.Location = new System.Drawing.Point(432, 65);
            this.btnDimPatternPyramid.Name = "btnDimPatternPyramid";
            this.btnDimPatternPyramid.Size = new System.Drawing.Size(94, 35);
            this.btnDimPatternPyramid.TabIndex = 25;
            this.btnDimPatternPyramid.Text = "Pyramid";
            this.btnDimPatternPyramid.UseVisualStyleBackColor = true;
            this.btnDimPatternPyramid.Click += new System.EventHandler(this.EventClick);
            // 
            // btnDimPatternCrosses
            // 
            this.btnDimPatternCrosses.Location = new System.Drawing.Point(332, 65);
            this.btnDimPatternCrosses.Name = "btnDimPatternCrosses";
            this.btnDimPatternCrosses.Size = new System.Drawing.Size(94, 35);
            this.btnDimPatternCrosses.TabIndex = 24;
            this.btnDimPatternCrosses.Text = "Crosses";
            this.btnDimPatternCrosses.UseVisualStyleBackColor = true;
            this.btnDimPatternCrosses.Click += new System.EventHandler(this.EventClick);
            // 
            // btnDimPatternSparse
            // 
            this.btnDimPatternSparse.Location = new System.Drawing.Point(232, 65);
            this.btnDimPatternSparse.Name = "btnDimPatternSparse";
            this.btnDimPatternSparse.Size = new System.Drawing.Size(94, 35);
            this.btnDimPatternSparse.TabIndex = 23;
            this.btnDimPatternSparse.Text = "Sparse";
            this.btnDimPatternSparse.UseVisualStyleBackColor = true;
            this.btnDimPatternSparse.Click += new System.EventHandler(this.EventClick);
            // 
            // btnDimPatternChessBoard
            // 
            this.btnDimPatternChessBoard.Location = new System.Drawing.Point(112, 65);
            this.btnDimPatternChessBoard.Name = "btnDimPatternChessBoard";
            this.btnDimPatternChessBoard.Size = new System.Drawing.Size(114, 35);
            this.btnDimPatternChessBoard.TabIndex = 22;
            this.btnDimPatternChessBoard.Text = "Chess Board";
            this.btnDimPatternChessBoard.UseVisualStyleBackColor = true;
            this.btnDimPatternChessBoard.Click += new System.EventHandler(this.EventClick);
            // 
            // nmPixelDimBrightness
            // 
            this.nmPixelDimBrightness.Location = new System.Drawing.Point(112, 31);
            this.nmPixelDimBrightness.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmPixelDimBrightness.Maximum = new decimal(new int[] {
            254,
            0,
            0,
            0});
            this.nmPixelDimBrightness.Name = "nmPixelDimBrightness";
            this.nmPixelDimBrightness.Size = new System.Drawing.Size(113, 26);
            this.nmPixelDimBrightness.TabIndex = 20;
            this.nmPixelDimBrightness.Value = new decimal(new int[] {
            127,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 94);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(65, 20);
            this.label3.TabIndex = 19;
            this.label3.Text = "Pattern:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(7, 34);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(89, 20);
            this.label4.TabIndex = 21;
            this.label4.Text = "Brightness:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(4, 64);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(94, 80);
            this.label2.TabIndex = 36;
            this.label2.Text = "Even Layer\r\nPattern:\r\n0 = Black\r\n255 = White";
            // 
            // tbEvenPattern
            // 
            this.tbEvenPattern.Location = new System.Drawing.Point(105, 42);
            this.tbEvenPattern.Multiline = true;
            this.tbEvenPattern.Name = "tbEvenPattern";
            this.tbEvenPattern.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbEvenPattern.Size = new System.Drawing.Size(469, 124);
            this.tbEvenPattern.TabIndex = 35;
            this.tbEvenPattern.Text = "127 255 255 255\r\n255 255 127 255";
            this.tbEvenPattern.WordWrap = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(233, 11);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(25, 20);
            this.label1.TabIndex = 34;
            this.label1.Text = "px";
            this.toolTip.SetToolTip(this.label1, resources.GetString("label1.ToolTip"));
            // 
            // nmBorderSize
            // 
            this.nmBorderSize.Location = new System.Drawing.Point(105, 8);
            this.nmBorderSize.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmBorderSize.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nmBorderSize.Name = "nmBorderSize";
            this.nmBorderSize.Size = new System.Drawing.Size(120, 26);
            this.nmBorderSize.TabIndex = 32;
            this.nmBorderSize.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // lbX
            // 
            this.lbX.AutoSize = true;
            this.lbX.Location = new System.Drawing.Point(4, 11);
            this.lbX.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbX.Name = "lbX";
            this.lbX.Size = new System.Drawing.Size(93, 20);
            this.lbX.TabIndex = 33;
            this.lbX.Text = "Border size:";
            this.toolTip.SetToolTip(this.lbX, resources.GetString("lbX.ToolTip"));
            // 
            // CtrlToolPixelDimming
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.CanROI = true;
            this.Controls.Add(this.cbDimsOnlyBorders);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.tbOddPattern);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tbEvenPattern);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.nmBorderSize);
            this.Controls.Add(this.lbX);
            this.Description = "";
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "CtrlToolPixelDimming";
            this.Size = new System.Drawing.Size(577, 628);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmInfillSpacing)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmInfillThickness)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelDimBrightness)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmBorderSize)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox cbDimsOnlyBorders;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.NumericUpDown nmInfillSpacing;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button btnInfillPatternWaves;
        private System.Windows.Forms.Button btnInfillPatternSquareGrid;
        private System.Windows.Forms.Button btnInfillPatternRectilinear;
        private System.Windows.Forms.NumericUpDown nmInfillThickness;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbOddPattern;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnDimPatternSolid;
        private System.Windows.Forms.Button btnDimPatternStrips;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button btnDimPatternWaves;
        private System.Windows.Forms.Button btnPatternRandom;
        private System.Windows.Forms.Button btnDimPatternSlashes;
        private System.Windows.Forms.Button btnDimPatternHearts;
        private System.Windows.Forms.Button btnDimPatternRhombus;
        private System.Windows.Forms.Button btnDimPatternPyramid;
        private System.Windows.Forms.Button btnDimPatternCrosses;
        private System.Windows.Forms.Button btnDimPatternSparse;
        private System.Windows.Forms.Button btnDimPatternChessBoard;
        private System.Windows.Forms.NumericUpDown nmPixelDimBrightness;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbEvenPattern;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nmBorderSize;
        private System.Windows.Forms.Label lbX;
    }
}
