using UVtools.GUI.Controls;

namespace UVtools.GUI.Forms
{
    partial class FrmMutationMove
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
            this.lbDescription = new System.Windows.Forms.Label();
            this.lbLayerRange = new System.Windows.Forms.Label();
            this.nmLayerRangeStart = new System.Windows.Forms.NumericUpDown();
            this.nmLayerRangeEnd = new System.Windows.Forms.NumericUpDown();
            this.lbLayerRangeTo = new System.Windows.Forms.Label();
            this.cmLayerRange = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.btnLayerRangeAllLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeCurrentLayer = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeBottomLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeNormalLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnMutate = new System.Windows.Forms.Button();
            this.tableAnchor = new System.Windows.Forms.TableLayoutPanel();
            this.rbAnchorBottomLeft = new System.Windows.Forms.RadioButton();
            this.rbAnchorBottomCenter = new System.Windows.Forms.RadioButton();
            this.rbAnchorBottomRight = new System.Windows.Forms.RadioButton();
            this.rbAnchorMiddleRight = new System.Windows.Forms.RadioButton();
            this.rbAnchorMiddleCenter = new System.Windows.Forms.RadioButton();
            this.rbAnchorMiddleLeft = new System.Windows.Forms.RadioButton();
            this.rbAnchorTopCenter = new System.Windows.Forms.RadioButton();
            this.rbAnchorTopLeft = new System.Windows.Forms.RadioButton();
            this.rbAnchorTopRight = new System.Windows.Forms.RadioButton();
            this.nmMarginTop = new System.Windows.Forms.NumericUpDown();
            this.nmMarginBottom = new System.Windows.Forms.NumericUpDown();
            this.nmMarginRight = new System.Windows.Forms.NumericUpDown();
            this.nmMarginLeft = new System.Windows.Forms.NumericUpDown();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lbInsideBounds = new System.Windows.Forms.Label();
            this.lbPlacementY = new System.Windows.Forms.Label();
            this.lbPlacementX = new System.Windows.Forms.Label();
            this.lbVolumeHeight = new System.Windows.Forms.Label();
            this.lbVolumeWidth = new System.Windows.Forms.Label();
            this.btnResetDefaults = new System.Windows.Forms.Button();
            this.btnLayerRangeSelect = new UVtools.GUI.Controls.SplitButton();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeStart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeEnd)).BeginInit();
            this.cmLayerRange.SuspendLayout();
            this.tableAnchor.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginTop)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginBottom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginRight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginLeft)).BeginInit();
            this.groupBox1.SuspendLayout();
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
            // lbLayerRange
            // 
            this.lbLayerRange.AutoSize = true;
            this.lbLayerRange.Location = new System.Drawing.Point(13, 150);
            this.lbLayerRange.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbLayerRange.Name = "lbLayerRange";
            this.lbLayerRange.Size = new System.Drawing.Size(97, 20);
            this.lbLayerRange.TabIndex = 9;
            this.lbLayerRange.Text = "Layer range:";
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
            this.btnLayerRangeAllLayers.Click += new System.EventHandler(this.EventClick);
            // 
            // btnLayerRangeCurrentLayer
            // 
            this.btnLayerRangeCurrentLayer.Name = "btnLayerRangeCurrentLayer";
            this.btnLayerRangeCurrentLayer.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.C)));
            this.btnLayerRangeCurrentLayer.Size = new System.Drawing.Size(225, 22);
            this.btnLayerRangeCurrentLayer.Text = "&Current Layer";
            this.btnLayerRangeCurrentLayer.Click += new System.EventHandler(this.EventClick);
            // 
            // btnLayerRangeBottomLayers
            // 
            this.btnLayerRangeBottomLayers.Name = "btnLayerRangeBottomLayers";
            this.btnLayerRangeBottomLayers.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.B)));
            this.btnLayerRangeBottomLayers.Size = new System.Drawing.Size(225, 22);
            this.btnLayerRangeBottomLayers.Text = "&Bottom Layers";
            this.btnLayerRangeBottomLayers.Click += new System.EventHandler(this.EventClick);
            // 
            // btnLayerRangeNormalLayers
            // 
            this.btnLayerRangeNormalLayers.Name = "btnLayerRangeNormalLayers";
            this.btnLayerRangeNormalLayers.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.N)));
            this.btnLayerRangeNormalLayers.Size = new System.Drawing.Size(225, 22);
            this.btnLayerRangeNormalLayers.Text = "&Normal Layers";
            this.btnLayerRangeNormalLayers.Click += new System.EventHandler(this.EventClick);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(366, 21);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(36, 20);
            this.label2.TabIndex = 16;
            this.label2.Text = "Top";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(355, 168);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 20);
            this.label3.TabIndex = 18;
            this.label3.Text = "Bottom";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(516, 94);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(47, 20);
            this.label4.TabIndex = 21;
            this.label4.Text = "Right";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(223, 95);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(37, 20);
            this.label5.TabIndex = 22;
            this.label5.Text = "Left";
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Image = global::UVtools.GUI.Properties.Resources.Cancel_24x24;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(434, 404);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(150, 48);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.EventClick);
            // 
            // btnMutate
            // 
            this.btnMutate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnMutate.Image = global::UVtools.GUI.Properties.Resources.Ok_24x24;
            this.btnMutate.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnMutate.Location = new System.Drawing.Point(276, 404);
            this.btnMutate.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnMutate.Name = "btnMutate";
            this.btnMutate.Size = new System.Drawing.Size(150, 48);
            this.btnMutate.TabIndex = 5;
            this.btnMutate.Text = "&Move";
            this.btnMutate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnMutate.UseVisualStyleBackColor = true;
            this.btnMutate.Click += new System.EventHandler(this.EventClick);
            // 
            // tableAnchor
            // 
            this.tableAnchor.AutoSize = true;
            this.tableAnchor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableAnchor.ColumnCount = 3;
            this.tableAnchor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableAnchor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableAnchor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableAnchor.Controls.Add(this.rbAnchorBottomLeft, 0, 2);
            this.tableAnchor.Controls.Add(this.rbAnchorBottomCenter, 0, 2);
            this.tableAnchor.Controls.Add(this.rbAnchorBottomRight, 0, 2);
            this.tableAnchor.Controls.Add(this.rbAnchorMiddleRight, 2, 1);
            this.tableAnchor.Controls.Add(this.rbAnchorMiddleCenter, 1, 1);
            this.tableAnchor.Controls.Add(this.rbAnchorMiddleLeft, 0, 1);
            this.tableAnchor.Controls.Add(this.rbAnchorTopCenter, 1, 0);
            this.tableAnchor.Controls.Add(this.rbAnchorTopLeft, 0, 0);
            this.tableAnchor.Controls.Add(this.rbAnchorTopRight, 2, 0);
            this.tableAnchor.Location = new System.Drawing.Point(358, 76);
            this.tableAnchor.Name = "tableAnchor";
            this.tableAnchor.RowCount = 3;
            this.tableAnchor.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableAnchor.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableAnchor.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableAnchor.Size = new System.Drawing.Size(60, 57);
            this.tableAnchor.TabIndex = 13;
            // 
            // rbAnchorBottomLeft
            // 
            this.rbAnchorBottomLeft.AutoSize = true;
            this.rbAnchorBottomLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorBottomLeft.Location = new System.Drawing.Point(3, 41);
            this.rbAnchorBottomLeft.Name = "rbAnchorBottomLeft";
            this.rbAnchorBottomLeft.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorBottomLeft.TabIndex = 8;
            this.rbAnchorBottomLeft.UseVisualStyleBackColor = true;
            this.rbAnchorBottomLeft.CheckedChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // rbAnchorBottomCenter
            // 
            this.rbAnchorBottomCenter.AutoSize = true;
            this.rbAnchorBottomCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorBottomCenter.Location = new System.Drawing.Point(23, 41);
            this.rbAnchorBottomCenter.Name = "rbAnchorBottomCenter";
            this.rbAnchorBottomCenter.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorBottomCenter.TabIndex = 7;
            this.rbAnchorBottomCenter.UseVisualStyleBackColor = true;
            this.rbAnchorBottomCenter.CheckedChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // rbAnchorBottomRight
            // 
            this.rbAnchorBottomRight.AutoSize = true;
            this.rbAnchorBottomRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorBottomRight.Location = new System.Drawing.Point(43, 41);
            this.rbAnchorBottomRight.Name = "rbAnchorBottomRight";
            this.rbAnchorBottomRight.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorBottomRight.TabIndex = 6;
            this.rbAnchorBottomRight.UseVisualStyleBackColor = true;
            this.rbAnchorBottomRight.CheckedChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // rbAnchorMiddleRight
            // 
            this.rbAnchorMiddleRight.AutoSize = true;
            this.rbAnchorMiddleRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorMiddleRight.Location = new System.Drawing.Point(43, 22);
            this.rbAnchorMiddleRight.Name = "rbAnchorMiddleRight";
            this.rbAnchorMiddleRight.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorMiddleRight.TabIndex = 5;
            this.rbAnchorMiddleRight.UseVisualStyleBackColor = true;
            this.rbAnchorMiddleRight.CheckedChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // rbAnchorMiddleCenter
            // 
            this.rbAnchorMiddleCenter.AutoSize = true;
            this.rbAnchorMiddleCenter.Checked = true;
            this.rbAnchorMiddleCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorMiddleCenter.Location = new System.Drawing.Point(23, 22);
            this.rbAnchorMiddleCenter.Name = "rbAnchorMiddleCenter";
            this.rbAnchorMiddleCenter.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorMiddleCenter.TabIndex = 4;
            this.rbAnchorMiddleCenter.TabStop = true;
            this.rbAnchorMiddleCenter.UseVisualStyleBackColor = true;
            this.rbAnchorMiddleCenter.CheckedChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // rbAnchorMiddleLeft
            // 
            this.rbAnchorMiddleLeft.AutoSize = true;
            this.rbAnchorMiddleLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorMiddleLeft.Location = new System.Drawing.Point(3, 22);
            this.rbAnchorMiddleLeft.Name = "rbAnchorMiddleLeft";
            this.rbAnchorMiddleLeft.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorMiddleLeft.TabIndex = 3;
            this.rbAnchorMiddleLeft.UseVisualStyleBackColor = true;
            this.rbAnchorMiddleLeft.CheckedChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // rbAnchorTopCenter
            // 
            this.rbAnchorTopCenter.AutoSize = true;
            this.rbAnchorTopCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorTopCenter.Location = new System.Drawing.Point(23, 3);
            this.rbAnchorTopCenter.Name = "rbAnchorTopCenter";
            this.rbAnchorTopCenter.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorTopCenter.TabIndex = 2;
            this.rbAnchorTopCenter.UseVisualStyleBackColor = true;
            this.rbAnchorTopCenter.CheckedChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // rbAnchorTopLeft
            // 
            this.rbAnchorTopLeft.AutoSize = true;
            this.rbAnchorTopLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorTopLeft.Location = new System.Drawing.Point(3, 3);
            this.rbAnchorTopLeft.Name = "rbAnchorTopLeft";
            this.rbAnchorTopLeft.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorTopLeft.TabIndex = 1;
            this.rbAnchorTopLeft.UseVisualStyleBackColor = true;
            this.rbAnchorTopLeft.CheckedChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // rbAnchorTopRight
            // 
            this.rbAnchorTopRight.AutoSize = true;
            this.rbAnchorTopRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorTopRight.Location = new System.Drawing.Point(43, 3);
            this.rbAnchorTopRight.Name = "rbAnchorTopRight";
            this.rbAnchorTopRight.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorTopRight.TabIndex = 0;
            this.rbAnchorTopRight.UseVisualStyleBackColor = true;
            this.rbAnchorTopRight.CheckedChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // nmMarginTop
            // 
            this.nmMarginTop.Location = new System.Drawing.Point(346, 44);
            this.nmMarginTop.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmMarginTop.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.nmMarginTop.Name = "nmMarginTop";
            this.nmMarginTop.Size = new System.Drawing.Size(85, 26);
            this.nmMarginTop.TabIndex = 15;
            this.nmMarginTop.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nmMarginTop.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // nmMarginBottom
            // 
            this.nmMarginBottom.Location = new System.Drawing.Point(346, 139);
            this.nmMarginBottom.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmMarginBottom.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.nmMarginBottom.Name = "nmMarginBottom";
            this.nmMarginBottom.Size = new System.Drawing.Size(85, 26);
            this.nmMarginBottom.TabIndex = 17;
            this.nmMarginBottom.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nmMarginBottom.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // nmMarginRight
            // 
            this.nmMarginRight.Location = new System.Drawing.Point(424, 92);
            this.nmMarginRight.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmMarginRight.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.nmMarginRight.Name = "nmMarginRight";
            this.nmMarginRight.Size = new System.Drawing.Size(85, 26);
            this.nmMarginRight.TabIndex = 19;
            this.nmMarginRight.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nmMarginRight.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // nmMarginLeft
            // 
            this.nmMarginLeft.Location = new System.Drawing.Point(267, 92);
            this.nmMarginLeft.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmMarginLeft.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.nmMarginLeft.Name = "nmMarginLeft";
            this.nmMarginLeft.Size = new System.Drawing.Size(85, 26);
            this.nmMarginLeft.TabIndex = 20;
            this.nmMarginLeft.UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left;
            this.nmMarginLeft.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lbInsideBounds);
            this.groupBox1.Controls.Add(this.lbPlacementY);
            this.groupBox1.Controls.Add(this.lbPlacementX);
            this.groupBox1.Controls.Add(this.lbVolumeHeight);
            this.groupBox1.Controls.Add(this.lbVolumeWidth);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.tableAnchor);
            this.groupBox1.Controls.Add(this.nmMarginTop);
            this.groupBox1.Controls.Add(this.nmMarginBottom);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.nmMarginRight);
            this.groupBox1.Controls.Add(this.nmMarginLeft);
            this.groupBox1.Location = new System.Drawing.Point(17, 181);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(570, 199);
            this.groupBox1.TabIndex = 23;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Margins and Anchor";
            // 
            // lbInsideBounds
            // 
            this.lbInsideBounds.AutoSize = true;
            this.lbInsideBounds.Location = new System.Drawing.Point(7, 168);
            this.lbInsideBounds.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbInsideBounds.Name = "lbInsideBounds";
            this.lbInsideBounds.Size = new System.Drawing.Size(147, 20);
            this.lbInsideBounds.TabIndex = 27;
            this.lbInsideBounds.Text = "Inside Bounds: Yes";
            // 
            // lbPlacementY
            // 
            this.lbPlacementY.AutoSize = true;
            this.lbPlacementY.Location = new System.Drawing.Point(7, 138);
            this.lbPlacementY.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbPlacementY.Name = "lbPlacementY";
            this.lbPlacementY.Size = new System.Drawing.Size(103, 20);
            this.lbPlacementY.TabIndex = 26;
            this.lbPlacementY.Text = "Placement Y:";
            // 
            // lbPlacementX
            // 
            this.lbPlacementX.AutoSize = true;
            this.lbPlacementX.Location = new System.Drawing.Point(7, 107);
            this.lbPlacementX.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbPlacementX.Name = "lbPlacementX";
            this.lbPlacementX.Size = new System.Drawing.Size(103, 20);
            this.lbPlacementX.TabIndex = 25;
            this.lbPlacementX.Text = "Placement X:";
            // 
            // lbVolumeHeight
            // 
            this.lbVolumeHeight.AutoSize = true;
            this.lbVolumeHeight.Location = new System.Drawing.Point(7, 56);
            this.lbVolumeHeight.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbVolumeHeight.Name = "lbVolumeHeight";
            this.lbVolumeHeight.Size = new System.Drawing.Size(118, 20);
            this.lbVolumeHeight.TabIndex = 24;
            this.lbVolumeHeight.Text = "Volume Height:";
            // 
            // lbVolumeWidth
            // 
            this.lbVolumeWidth.AutoSize = true;
            this.lbVolumeWidth.Location = new System.Drawing.Point(7, 27);
            this.lbVolumeWidth.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbVolumeWidth.Name = "lbVolumeWidth";
            this.lbVolumeWidth.Size = new System.Drawing.Size(112, 20);
            this.lbVolumeWidth.TabIndex = 23;
            this.lbVolumeWidth.Text = "Volume Width:";
            // 
            // btnResetDefaults
            // 
            this.btnResetDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnResetDefaults.Image = global::UVtools.GUI.Properties.Resources.Rotate_16x16;
            this.btnResetDefaults.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnResetDefaults.Location = new System.Drawing.Point(13, 404);
            this.btnResetDefaults.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnResetDefaults.Name = "btnResetDefaults";
            this.btnResetDefaults.Size = new System.Drawing.Size(150, 48);
            this.btnResetDefaults.TabIndex = 24;
            this.btnResetDefaults.Text = "&Reset defaults";
            this.btnResetDefaults.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnResetDefaults.UseVisualStyleBackColor = true;
            this.btnResetDefaults.Click += new System.EventHandler(this.EventClick);
            // 
            // btnLayerRangeSelect
            // 
            this.btnLayerRangeSelect.Location = new System.Drawing.Point(446, 146);
            this.btnLayerRangeSelect.Menu = this.cmLayerRange;
            this.btnLayerRangeSelect.Name = "btnLayerRangeSelect";
            this.btnLayerRangeSelect.Size = new System.Drawing.Size(138, 26);
            this.btnLayerRangeSelect.TabIndex = 2;
            this.btnLayerRangeSelect.Text = "Select";
            this.btnLayerRangeSelect.UseVisualStyleBackColor = true;
            // 
            // FrmMutationMove
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(599, 466);
            this.Controls.Add(this.btnResetDefaults);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnLayerRangeSelect);
            this.Controls.Add(this.lbLayerRangeTo);
            this.Controls.Add(this.nmLayerRangeEnd);
            this.Controls.Add(this.nmLayerRangeStart);
            this.Controls.Add(this.lbLayerRange);
            this.Controls.Add(this.btnMutate);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lbDescription);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmMutationMove";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Form1";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeStart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeEnd)).EndInit();
            this.cmLayerRange.ResumeLayout(false);
            this.tableAnchor.ResumeLayout(false);
            this.tableAnchor.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginTop)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginRight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginLeft)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbDescription;
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
        private System.Windows.Forms.TableLayoutPanel tableAnchor;
        private System.Windows.Forms.RadioButton rbAnchorTopRight;
        private System.Windows.Forms.RadioButton rbAnchorBottomLeft;
        private System.Windows.Forms.RadioButton rbAnchorBottomCenter;
        private System.Windows.Forms.RadioButton rbAnchorBottomRight;
        private System.Windows.Forms.RadioButton rbAnchorMiddleRight;
        private System.Windows.Forms.RadioButton rbAnchorMiddleCenter;
        private System.Windows.Forms.RadioButton rbAnchorMiddleLeft;
        private System.Windows.Forms.RadioButton rbAnchorTopCenter;
        private System.Windows.Forms.RadioButton rbAnchorTopLeft;
        private System.Windows.Forms.NumericUpDown nmMarginTop;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nmMarginBottom;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nmMarginRight;
        private System.Windows.Forms.NumericUpDown nmMarginLeft;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnResetDefaults;
        private System.Windows.Forms.Label lbVolumeHeight;
        private System.Windows.Forms.Label lbVolumeWidth;
        private System.Windows.Forms.Label lbPlacementX;
        private System.Windows.Forms.Label lbPlacementY;
        private System.Windows.Forms.Label lbInsideBounds;
    }
}