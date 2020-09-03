using UVtools.GUI.Controls;

namespace UVtools.GUI.Forms
{
    partial class FrmMutationPixelDimming
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMutationPixelDimming));
            this.lbDescription = new System.Windows.Forms.Label();
            this.lbX = new System.Windows.Forms.Label();
            this.lbLayerRange = new System.Windows.Forms.Label();
            this.nmLayerRangeStart = new System.Windows.Forms.NumericUpDown();
            this.nmLayerRangeEnd = new System.Windows.Forms.NumericUpDown();
            this.lbLayerRangeTo = new System.Windows.Forms.Label();
            this.cmLayerRange = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.btnLayerRangeAllLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeCurrentLayer = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeBottomLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeNormalLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnMutate = new System.Windows.Forms.Button();
            this.nmBorderSize = new System.Windows.Forms.NumericUpDown();
            this.tbEvenPattern = new System.Windows.Forms.TextBox();
            this.nmPixelDimBrightness = new System.Windows.Forms.NumericUpDown();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnDimPatternStrips = new System.Windows.Forms.Button();
            this.btnDimPatternWaves = new System.Windows.Forms.Button();
            this.btnPatternRandom = new System.Windows.Forms.Button();
            this.btnDimPatternSlashes = new System.Windows.Forms.Button();
            this.btnDimPatternHearts = new System.Windows.Forms.Button();
            this.btnDimPatternRhombus = new System.Windows.Forms.Button();
            this.btnDimPatternPyramid = new System.Windows.Forms.Button();
            this.btnDimPatternCrosses = new System.Windows.Forms.Button();
            this.btnDimPatternSparse = new System.Windows.Forms.Button();
            this.btnDimPatternChessBoard = new System.Windows.Forms.Button();
            this.tbOddPattern = new System.Windows.Forms.TextBox();
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
            this.btnImportImageMask = new System.Windows.Forms.Button();
            this.btnLayerRangeSelect = new UVtools.GUI.Controls.SplitButton();
            this.cbDimsOnlyBorders = new System.Windows.Forms.CheckBox();
            this.btnDimPatternSolid = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeStart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeEnd)).BeginInit();
            this.cmLayerRange.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmBorderSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelDimBrightness)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmInfillSpacing)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmInfillThickness)).BeginInit();
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
            // lbX
            // 
            this.lbX.AutoSize = true;
            this.lbX.Location = new System.Drawing.Point(17, 190);
            this.lbX.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbX.Name = "lbX";
            this.lbX.Size = new System.Drawing.Size(93, 20);
            this.lbX.TabIndex = 3;
            this.lbX.Text = "Border size:";
            this.toolTip.SetToolTip(this.lbX, resources.GetString("lbX.ToolTip"));
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
            this.label1.Location = new System.Drawing.Point(246, 190);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(25, 20);
            this.label1.TabIndex = 16;
            this.label1.Text = "px";
            this.toolTip.SetToolTip(this.label1, resources.GetString("label1.ToolTip"));
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(19, 478);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(548, 20);
            this.label6.TabIndex = 25;
            this.label6.Text = "(Leave this field empty to use only the even layer pattern for the layers range)";
            this.toolTip.SetToolTip(this.label6, resources.GetString("label6.ToolTip"));
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 243);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(94, 80);
            this.label2.TabIndex = 18;
            this.label2.Text = "Even Layer\r\nPattern:\r\n0 = Black\r\n255 = White";
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
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(17, 363);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(94, 100);
            this.label5.TabIndex = 24;
            this.label5.Text = "Odd Layer\r\nPattern:\r\n0 = Black\r\n255 = White\r\n(Optional)";
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
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Image = global::UVtools.GUI.Properties.Resources.Cancel_24x24;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(434, 854);
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
            this.btnMutate.Image = global::UVtools.GUI.Properties.Resources.Ok_24x24;
            this.btnMutate.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnMutate.Location = new System.Drawing.Point(276, 854);
            this.btnMutate.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnMutate.Name = "btnMutate";
            this.btnMutate.Size = new System.Drawing.Size(150, 48);
            this.btnMutate.TabIndex = 5;
            this.btnMutate.Text = "&Dim";
            this.btnMutate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnMutate.UseVisualStyleBackColor = true;
            this.btnMutate.Click += new System.EventHandler(this.ItemClicked);
            // 
            // nmBorderSize
            // 
            this.nmBorderSize.Location = new System.Drawing.Point(118, 187);
            this.nmBorderSize.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmBorderSize.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nmBorderSize.Name = "nmBorderSize";
            this.nmBorderSize.Size = new System.Drawing.Size(120, 26);
            this.nmBorderSize.TabIndex = 3;
            this.nmBorderSize.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // tbEvenPattern
            // 
            this.tbEvenPattern.Location = new System.Drawing.Point(118, 221);
            this.tbEvenPattern.Multiline = true;
            this.tbEvenPattern.Name = "tbEvenPattern";
            this.tbEvenPattern.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbEvenPattern.Size = new System.Drawing.Size(469, 124);
            this.tbEvenPattern.TabIndex = 17;
            this.tbEvenPattern.Text = "127 255 255 255\r\n255 255 127 255";
            this.tbEvenPattern.WordWrap = false;
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
            this.groupBox1.Location = new System.Drawing.Point(12, 517);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(575, 162);
            this.groupBox1.TabIndex = 22;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Pixel dimming generator";
            // 
            // btnDimPatternStrips
            // 
            this.btnDimPatternStrips.Location = new System.Drawing.Point(332, 25);
            this.btnDimPatternStrips.Name = "btnDimPatternStrips";
            this.btnDimPatternStrips.Size = new System.Drawing.Size(94, 35);
            this.btnDimPatternStrips.TabIndex = 31;
            this.btnDimPatternStrips.Text = "Strips";
            this.btnDimPatternStrips.UseVisualStyleBackColor = true;
            this.btnDimPatternStrips.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnDimPatternWaves
            // 
            this.btnDimPatternWaves.Location = new System.Drawing.Point(232, 106);
            this.btnDimPatternWaves.Name = "btnDimPatternWaves";
            this.btnDimPatternWaves.Size = new System.Drawing.Size(94, 35);
            this.btnDimPatternWaves.TabIndex = 29;
            this.btnDimPatternWaves.Text = "Waves";
            this.btnDimPatternWaves.UseVisualStyleBackColor = true;
            this.btnDimPatternWaves.Click += new System.EventHandler(this.ItemClicked);
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
            this.btnPatternRandom.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnDimPatternSlashes
            // 
            this.btnDimPatternSlashes.Location = new System.Drawing.Point(332, 106);
            this.btnDimPatternSlashes.Name = "btnDimPatternSlashes";
            this.btnDimPatternSlashes.Size = new System.Drawing.Size(94, 35);
            this.btnDimPatternSlashes.TabIndex = 28;
            this.btnDimPatternSlashes.Text = "Slashes";
            this.btnDimPatternSlashes.UseVisualStyleBackColor = true;
            this.btnDimPatternSlashes.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnDimPatternHearts
            // 
            this.btnDimPatternHearts.Location = new System.Drawing.Point(432, 106);
            this.btnDimPatternHearts.Name = "btnDimPatternHearts";
            this.btnDimPatternHearts.Size = new System.Drawing.Size(94, 35);
            this.btnDimPatternHearts.TabIndex = 27;
            this.btnDimPatternHearts.Text = "Hearts";
            this.btnDimPatternHearts.UseVisualStyleBackColor = true;
            this.btnDimPatternHearts.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnDimPatternRhombus
            // 
            this.btnDimPatternRhombus.Location = new System.Drawing.Point(112, 106);
            this.btnDimPatternRhombus.Name = "btnDimPatternRhombus";
            this.btnDimPatternRhombus.Size = new System.Drawing.Size(113, 35);
            this.btnDimPatternRhombus.TabIndex = 26;
            this.btnDimPatternRhombus.Text = "Rhombus";
            this.btnDimPatternRhombus.UseVisualStyleBackColor = true;
            this.btnDimPatternRhombus.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnDimPatternPyramid
            // 
            this.btnDimPatternPyramid.Location = new System.Drawing.Point(432, 65);
            this.btnDimPatternPyramid.Name = "btnDimPatternPyramid";
            this.btnDimPatternPyramid.Size = new System.Drawing.Size(94, 35);
            this.btnDimPatternPyramid.TabIndex = 25;
            this.btnDimPatternPyramid.Text = "Pyramid";
            this.btnDimPatternPyramid.UseVisualStyleBackColor = true;
            this.btnDimPatternPyramid.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnDimPatternCrosses
            // 
            this.btnDimPatternCrosses.Location = new System.Drawing.Point(332, 65);
            this.btnDimPatternCrosses.Name = "btnDimPatternCrosses";
            this.btnDimPatternCrosses.Size = new System.Drawing.Size(94, 35);
            this.btnDimPatternCrosses.TabIndex = 24;
            this.btnDimPatternCrosses.Text = "Crosses";
            this.btnDimPatternCrosses.UseVisualStyleBackColor = true;
            this.btnDimPatternCrosses.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnDimPatternSparse
            // 
            this.btnDimPatternSparse.Location = new System.Drawing.Point(232, 65);
            this.btnDimPatternSparse.Name = "btnDimPatternSparse";
            this.btnDimPatternSparse.Size = new System.Drawing.Size(94, 35);
            this.btnDimPatternSparse.TabIndex = 23;
            this.btnDimPatternSparse.Text = "Sparse";
            this.btnDimPatternSparse.UseVisualStyleBackColor = true;
            this.btnDimPatternSparse.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnDimPatternChessBoard
            // 
            this.btnDimPatternChessBoard.Location = new System.Drawing.Point(112, 65);
            this.btnDimPatternChessBoard.Name = "btnDimPatternChessBoard";
            this.btnDimPatternChessBoard.Size = new System.Drawing.Size(114, 35);
            this.btnDimPatternChessBoard.TabIndex = 22;
            this.btnDimPatternChessBoard.Text = "Chess Board";
            this.btnDimPatternChessBoard.UseVisualStyleBackColor = true;
            this.btnDimPatternChessBoard.Click += new System.EventHandler(this.ItemClicked);
            // 
            // tbOddPattern
            // 
            this.tbOddPattern.Location = new System.Drawing.Point(118, 351);
            this.tbOddPattern.Multiline = true;
            this.tbOddPattern.Name = "tbOddPattern";
            this.tbOddPattern.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbOddPattern.Size = new System.Drawing.Size(469, 124);
            this.tbOddPattern.TabIndex = 23;
            this.tbOddPattern.Text = "255 255 127 255\r\n127 255 255 255";
            this.tbOddPattern.WordWrap = false;
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
            this.groupBox2.Location = new System.Drawing.Point(12, 685);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(575, 160);
            this.groupBox2.TabIndex = 26;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Infill generator";
            // 
            // label13
            // 
            this.label13.Location = new System.Drawing.Point(7, 22);
            this.label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(561, 46);
            this.label13.TabIndex = 34;
            this.label13.Text = "The infill function can create a ton of resin traps, use only this tool if you kn" +
    "ow what are you doing or for specific parts. You always need to ensure the drain" +
    "s.";
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
            this.btnInfillPatternWaves.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnInfillPatternSquareGrid
            // 
            this.btnInfillPatternSquareGrid.Location = new System.Drawing.Point(232, 107);
            this.btnInfillPatternSquareGrid.Name = "btnInfillPatternSquareGrid";
            this.btnInfillPatternSquareGrid.Size = new System.Drawing.Size(113, 35);
            this.btnInfillPatternSquareGrid.TabIndex = 26;
            this.btnInfillPatternSquareGrid.Text = "Square Grid";
            this.btnInfillPatternSquareGrid.UseVisualStyleBackColor = true;
            this.btnInfillPatternSquareGrid.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnInfillPatternRectilinear
            // 
            this.btnInfillPatternRectilinear.Location = new System.Drawing.Point(112, 107);
            this.btnInfillPatternRectilinear.Name = "btnInfillPatternRectilinear";
            this.btnInfillPatternRectilinear.Size = new System.Drawing.Size(114, 35);
            this.btnInfillPatternRectilinear.TabIndex = 22;
            this.btnInfillPatternRectilinear.Text = "Rectilinear";
            this.btnInfillPatternRectilinear.UseVisualStyleBackColor = true;
            this.btnInfillPatternRectilinear.Click += new System.EventHandler(this.ItemClicked);
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
            // btnImportImageMask
            // 
            this.btnImportImageMask.Location = new System.Drawing.Point(161, 88);
            this.btnImportImageMask.Name = "btnImportImageMask";
            this.btnImportImageMask.Size = new System.Drawing.Size(273, 32);
            this.btnImportImageMask.TabIndex = 30;
            this.btnImportImageMask.Text = "Import grayscale image as mask";
            this.btnImportImageMask.UseVisualStyleBackColor = true;
            this.btnImportImageMask.Visible = false;
            this.btnImportImageMask.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnLayerRangeSelect
            // 
            this.btnLayerRangeSelect.Location = new System.Drawing.Point(446, 146);
            this.btnLayerRangeSelect.Menu = this.cmLayerRange;
            this.btnLayerRangeSelect.Name = "btnLayerRangeSelect";
            this.btnLayerRangeSelect.Size = new System.Drawing.Size(141, 26);
            this.btnLayerRangeSelect.TabIndex = 2;
            this.btnLayerRangeSelect.Text = "Select";
            this.btnLayerRangeSelect.UseVisualStyleBackColor = true;
            // 
            // cbDimsOnlyBorders
            // 
            this.cbDimsOnlyBorders.AutoSize = true;
            this.cbDimsOnlyBorders.Location = new System.Drawing.Point(314, 188);
            this.cbDimsOnlyBorders.Name = "cbDimsOnlyBorders";
            this.cbDimsOnlyBorders.Size = new System.Drawing.Size(181, 24);
            this.cbDimsOnlyBorders.TabIndex = 31;
            this.cbDimsOnlyBorders.Text = "Dims only the borders";
            this.cbDimsOnlyBorders.UseVisualStyleBackColor = true;
            // 
            // btnDimPatternSolid
            // 
            this.btnDimPatternSolid.Location = new System.Drawing.Point(432, 25);
            this.btnDimPatternSolid.Name = "btnDimPatternSolid";
            this.btnDimPatternSolid.Size = new System.Drawing.Size(94, 35);
            this.btnDimPatternSolid.TabIndex = 32;
            this.btnDimPatternSolid.Text = "Solid";
            this.btnDimPatternSolid.UseVisualStyleBackColor = true;
            this.btnDimPatternSolid.Click += new System.EventHandler(this.ItemClicked);
            // 
            // FrmMutationPixelDimming
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(599, 916);
            this.Controls.Add(this.cbDimsOnlyBorders);
            this.Controls.Add(this.btnImportImageMask);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.tbOddPattern);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tbEvenPattern);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnLayerRangeSelect);
            this.Controls.Add(this.lbLayerRangeTo);
            this.Controls.Add(this.nmLayerRangeEnd);
            this.Controls.Add(this.nmLayerRangeStart);
            this.Controls.Add(this.lbLayerRange);
            this.Controls.Add(this.btnMutate);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.nmBorderSize);
            this.Controls.Add(this.lbX);
            this.Controls.Add(this.lbDescription);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmMutationPixelDimming";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Form1";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeStart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeEnd)).EndInit();
            this.cmLayerRange.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nmBorderSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelDimBrightness)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmInfillSpacing)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmInfillThickness)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbDescription;
        private System.Windows.Forms.Label lbX;
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
        private System.Windows.Forms.NumericUpDown nmBorderSize;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbEvenPattern;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nmPixelDimBrightness;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnDimPatternChessBoard;
        private System.Windows.Forms.Button btnDimPatternSparse;
        private System.Windows.Forms.Button btnDimPatternPyramid;
        private System.Windows.Forms.Button btnDimPatternCrosses;
        private System.Windows.Forms.Button btnDimPatternRhombus;
        private System.Windows.Forms.Button btnDimPatternHearts;
        private System.Windows.Forms.Button btnDimPatternSlashes;
        private System.Windows.Forms.Button btnPatternRandom;
        private System.Windows.Forms.Button btnDimPatternWaves;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbOddPattern;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button btnInfillPatternWaves;
        private System.Windows.Forms.Button btnInfillPatternSquareGrid;
        private System.Windows.Forms.Button btnInfillPatternRectilinear;
        private System.Windows.Forms.NumericUpDown nmInfillThickness;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.NumericUpDown nmInfillSpacing;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Button btnImportImageMask;
        private System.Windows.Forms.Button btnDimPatternStrips;
        private System.Windows.Forms.CheckBox cbDimsOnlyBorders;
        private System.Windows.Forms.Button btnDimPatternSolid;
    }
}