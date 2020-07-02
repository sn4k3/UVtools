using UVtools.GUI.Controls;

namespace UVtools.GUI.Forms
{
    partial class FrmToolPattern
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmToolPattern));
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
            this.label4 = new System.Windows.Forms.Label();
            this.nmCols = new System.Windows.Forms.NumericUpDown();
            this.lbInsideBounds = new System.Windows.Forms.Label();
            this.lbRows = new System.Windows.Forms.Label();
            this.lbCols = new System.Windows.Forms.Label();
            this.lbVolumeHeight = new System.Windows.Forms.Label();
            this.lbVolumeWidth = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.nmRows = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.nmMarginRow = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.nmMarginCol = new System.Windows.Forms.NumericUpDown();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnAutoMarginCol = new System.Windows.Forms.Button();
            this.btnResetDefaults = new System.Windows.Forms.Button();
            this.btnPattern = new System.Windows.Forms.Button();
            this.btnAutoMarginRow = new System.Windows.Forms.Button();
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
            this.rbAnchorNone = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnLayerRangeSelect = new UVtools.GUI.Controls.SplitButton();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeStart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeEnd)).BeginInit();
            this.cmLayerRange.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmCols)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmRows)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginRow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginCol)).BeginInit();
            this.tableAnchor.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbDescription
            // 
            this.lbDescription.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbDescription.Location = new System.Drawing.Point(13, 14);
            this.lbDescription.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbDescription.Name = "lbDescription";
            this.lbDescription.Size = new System.Drawing.Size(584, 39);
            this.lbDescription.TabIndex = 0;
            this.lbDescription.Text = "Rectangular pattern the print arround the plate.";
            // 
            // lbLayerRange
            // 
            this.lbLayerRange.AutoSize = true;
            this.lbLayerRange.Location = new System.Drawing.Point(13, 62);
            this.lbLayerRange.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbLayerRange.Name = "lbLayerRange";
            this.lbLayerRange.Size = new System.Drawing.Size(97, 20);
            this.lbLayerRange.TabIndex = 9;
            this.lbLayerRange.Text = "Layer range:";
            // 
            // nmLayerRangeStart
            // 
            this.nmLayerRangeStart.Location = new System.Drawing.Point(118, 59);
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
            this.nmLayerRangeEnd.Location = new System.Drawing.Point(314, 59);
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
            this.lbLayerRangeTo.Location = new System.Drawing.Point(275, 62);
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
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(36, 95);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(75, 20);
            this.label4.TabIndex = 21;
            this.label4.Text = "Columns:";
            // 
            // nmCols
            // 
            this.nmCols.Location = new System.Drawing.Point(118, 93);
            this.nmCols.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmCols.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmCols.Name = "nmCols";
            this.nmCols.Size = new System.Drawing.Size(121, 26);
            this.nmCols.TabIndex = 19;
            this.nmCols.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmCols.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // lbInsideBounds
            // 
            this.lbInsideBounds.AutoSize = true;
            this.lbInsideBounds.Location = new System.Drawing.Point(16, 222);
            this.lbInsideBounds.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbInsideBounds.Name = "lbInsideBounds";
            this.lbInsideBounds.Size = new System.Drawing.Size(147, 20);
            this.lbInsideBounds.TabIndex = 27;
            this.lbInsideBounds.Text = "Inside Bounds: Yes";
            // 
            // lbRows
            // 
            this.lbRows.AutoSize = true;
            this.lbRows.Location = new System.Drawing.Point(442, 127);
            this.lbRows.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbRows.Name = "lbRows";
            this.lbRows.Size = new System.Drawing.Size(53, 20);
            this.lbRows.TabIndex = 26;
            this.lbRows.Text = "Rows:";
            // 
            // lbCols
            // 
            this.lbCols.AutoSize = true;
            this.lbCols.Location = new System.Drawing.Point(442, 96);
            this.lbCols.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbCols.Name = "lbCols";
            this.lbCols.Size = new System.Drawing.Size(79, 20);
            this.lbCols.TabIndex = 25;
            this.lbCols.Text = "Columns: ";
            // 
            // lbVolumeHeight
            // 
            this.lbVolumeHeight.AutoSize = true;
            this.lbVolumeHeight.Location = new System.Drawing.Point(13, 193);
            this.lbVolumeHeight.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbVolumeHeight.Name = "lbVolumeHeight";
            this.lbVolumeHeight.Size = new System.Drawing.Size(174, 20);
            this.lbVolumeHeight.TabIndex = 24;
            this.lbVolumeHeight.Text = "Volume/Pattern Height:";
            // 
            // lbVolumeWidth
            // 
            this.lbVolumeWidth.AutoSize = true;
            this.lbVolumeWidth.Location = new System.Drawing.Point(13, 164);
            this.lbVolumeWidth.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbVolumeWidth.Name = "lbVolumeWidth";
            this.lbVolumeWidth.Size = new System.Drawing.Size(168, 20);
            this.lbVolumeWidth.TabIndex = 23;
            this.lbVolumeWidth.Text = "Volume/Pattern Width:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(58, 127);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 20);
            this.label1.TabIndex = 29;
            this.label1.Text = "Rows:";
            // 
            // nmRows
            // 
            this.nmRows.Location = new System.Drawing.Point(118, 125);
            this.nmRows.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmRows.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmRows.Name = "nmRows";
            this.nmRows.Size = new System.Drawing.Size(121, 26);
            this.nmRows.TabIndex = 28;
            this.nmRows.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmRows.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(246, 128);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(61, 20);
            this.label2.TabIndex = 31;
            this.label2.Text = "Margin:";
            // 
            // nmMarginRow
            // 
            this.nmMarginRow.Location = new System.Drawing.Point(314, 125);
            this.nmMarginRow.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmMarginRow.Name = "nmMarginRow";
            this.nmMarginRow.Size = new System.Drawing.Size(81, 26);
            this.nmMarginRow.TabIndex = 30;
            this.nmMarginRow.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.nmMarginRow.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(246, 96);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 20);
            this.label3.TabIndex = 33;
            this.label3.Text = "Margin:";
            // 
            // nmMarginCol
            // 
            this.nmMarginCol.Location = new System.Drawing.Point(314, 93);
            this.nmMarginCol.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmMarginCol.Name = "nmMarginCol";
            this.nmMarginCol.Size = new System.Drawing.Size(81, 26);
            this.nmMarginCol.TabIndex = 32;
            this.nmMarginCol.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.nmMarginCol.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Image = global::UVtools.GUI.Properties.Resources.Cancel_24x24;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(434, 277);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(150, 48);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.EventClick);
            // 
            // btnAutoMarginCol
            // 
            this.btnAutoMarginCol.Image = global::UVtools.GUI.Properties.Resources.resize_16x16;
            this.btnAutoMarginCol.Location = new System.Drawing.Point(401, 93);
            this.btnAutoMarginCol.Name = "btnAutoMarginCol";
            this.btnAutoMarginCol.Size = new System.Drawing.Size(33, 26);
            this.btnAutoMarginCol.TabIndex = 35;
            this.btnAutoMarginCol.Click += new System.EventHandler(this.EventClick);
            // 
            // btnResetDefaults
            // 
            this.btnResetDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnResetDefaults.Image = global::UVtools.GUI.Properties.Resources.Rotate_16x16;
            this.btnResetDefaults.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnResetDefaults.Location = new System.Drawing.Point(13, 277);
            this.btnResetDefaults.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnResetDefaults.Name = "btnResetDefaults";
            this.btnResetDefaults.Size = new System.Drawing.Size(150, 48);
            this.btnResetDefaults.TabIndex = 24;
            this.btnResetDefaults.Text = "&Reset defaults";
            this.btnResetDefaults.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnResetDefaults.UseVisualStyleBackColor = true;
            this.btnResetDefaults.Click += new System.EventHandler(this.EventClick);
            // 
            // btnPattern
            // 
            this.btnPattern.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnPattern.Image = global::UVtools.GUI.Properties.Resources.Ok_24x24;
            this.btnPattern.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnPattern.Location = new System.Drawing.Point(276, 277);
            this.btnPattern.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnPattern.Name = "btnPattern";
            this.btnPattern.Size = new System.Drawing.Size(150, 48);
            this.btnPattern.TabIndex = 5;
            this.btnPattern.Text = "&Pattern";
            this.btnPattern.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnPattern.UseVisualStyleBackColor = true;
            this.btnPattern.Click += new System.EventHandler(this.EventClick);
            // 
            // btnAutoMarginRow
            // 
            this.btnAutoMarginRow.Image = global::UVtools.GUI.Properties.Resources.resize_16x16;
            this.btnAutoMarginRow.Location = new System.Drawing.Point(401, 125);
            this.btnAutoMarginRow.Name = "btnAutoMarginRow";
            this.btnAutoMarginRow.Size = new System.Drawing.Size(33, 26);
            this.btnAutoMarginRow.TabIndex = 36;
            this.btnAutoMarginRow.Click += new System.EventHandler(this.EventClick);
            // 
            // tableAnchor
            // 
            this.tableAnchor.AutoSize = true;
            this.tableAnchor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableAnchor.ColumnCount = 4;
            this.tableAnchor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableAnchor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableAnchor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableAnchor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableAnchor.Controls.Add(this.rbAnchorNone, 3, 1);
            this.tableAnchor.Controls.Add(this.rbAnchorBottomLeft, 0, 2);
            this.tableAnchor.Controls.Add(this.rbAnchorBottomCenter, 0, 2);
            this.tableAnchor.Controls.Add(this.rbAnchorBottomRight, 0, 2);
            this.tableAnchor.Controls.Add(this.rbAnchorMiddleRight, 2, 1);
            this.tableAnchor.Controls.Add(this.rbAnchorMiddleCenter, 1, 1);
            this.tableAnchor.Controls.Add(this.rbAnchorMiddleLeft, 0, 1);
            this.tableAnchor.Controls.Add(this.rbAnchorTopCenter, 1, 0);
            this.tableAnchor.Controls.Add(this.rbAnchorTopLeft, 0, 0);
            this.tableAnchor.Controls.Add(this.rbAnchorTopRight, 2, 0);
            this.tableAnchor.Location = new System.Drawing.Point(6, 25);
            this.tableAnchor.Name = "tableAnchor";
            this.tableAnchor.RowCount = 3;
            this.tableAnchor.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableAnchor.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableAnchor.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableAnchor.Size = new System.Drawing.Size(131, 68);
            this.tableAnchor.TabIndex = 38;
            // 
            // rbAnchorBottomLeft
            // 
            this.rbAnchorBottomLeft.AutoSize = true;
            this.rbAnchorBottomLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorBottomLeft.Location = new System.Drawing.Point(23, 52);
            this.rbAnchorBottomLeft.Name = "rbAnchorBottomLeft";
            this.rbAnchorBottomLeft.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorBottomLeft.TabIndex = 8;
            this.rbAnchorBottomLeft.UseVisualStyleBackColor = true;
            // 
            // rbAnchorBottomCenter
            // 
            this.rbAnchorBottomCenter.AutoSize = true;
            this.rbAnchorBottomCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorBottomCenter.Location = new System.Drawing.Point(3, 52);
            this.rbAnchorBottomCenter.Name = "rbAnchorBottomCenter";
            this.rbAnchorBottomCenter.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorBottomCenter.TabIndex = 7;
            this.rbAnchorBottomCenter.UseVisualStyleBackColor = true;
            // 
            // rbAnchorBottomRight
            // 
            this.rbAnchorBottomRight.AutoSize = true;
            this.rbAnchorBottomRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorBottomRight.Location = new System.Drawing.Point(43, 52);
            this.rbAnchorBottomRight.Name = "rbAnchorBottomRight";
            this.rbAnchorBottomRight.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorBottomRight.TabIndex = 6;
            this.rbAnchorBottomRight.UseVisualStyleBackColor = true;
            // 
            // rbAnchorMiddleRight
            // 
            this.rbAnchorMiddleRight.AutoSize = true;
            this.rbAnchorMiddleRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorMiddleRight.Location = new System.Drawing.Point(43, 22);
            this.rbAnchorMiddleRight.Name = "rbAnchorMiddleRight";
            this.rbAnchorMiddleRight.Size = new System.Drawing.Size(14, 24);
            this.rbAnchorMiddleRight.TabIndex = 5;
            this.rbAnchorMiddleRight.UseVisualStyleBackColor = true;
            // 
            // rbAnchorMiddleCenter
            // 
            this.rbAnchorMiddleCenter.AutoSize = true;
            this.rbAnchorMiddleCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorMiddleCenter.Location = new System.Drawing.Point(23, 22);
            this.rbAnchorMiddleCenter.Name = "rbAnchorMiddleCenter";
            this.rbAnchorMiddleCenter.Size = new System.Drawing.Size(14, 24);
            this.rbAnchorMiddleCenter.TabIndex = 4;
            this.rbAnchorMiddleCenter.UseVisualStyleBackColor = true;
            // 
            // rbAnchorMiddleLeft
            // 
            this.rbAnchorMiddleLeft.AutoSize = true;
            this.rbAnchorMiddleLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorMiddleLeft.Location = new System.Drawing.Point(3, 22);
            this.rbAnchorMiddleLeft.Name = "rbAnchorMiddleLeft";
            this.rbAnchorMiddleLeft.Size = new System.Drawing.Size(14, 24);
            this.rbAnchorMiddleLeft.TabIndex = 3;
            this.rbAnchorMiddleLeft.UseVisualStyleBackColor = true;
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
            // 
            // rbAnchorNone
            // 
            this.rbAnchorNone.AutoSize = true;
            this.rbAnchorNone.Checked = true;
            this.rbAnchorNone.Location = new System.Drawing.Point(63, 22);
            this.rbAnchorNone.Name = "rbAnchorNone";
            this.rbAnchorNone.Size = new System.Drawing.Size(65, 24);
            this.rbAnchorNone.TabIndex = 39;
            this.rbAnchorNone.TabStop = true;
            this.rbAnchorNone.Text = "None";
            this.rbAnchorNone.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tableAnchor);
            this.groupBox1.Location = new System.Drawing.Point(441, 164);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(143, 105);
            this.groupBox1.TabIndex = 39;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Anchor";
            // 
            // btnLayerRangeSelect
            // 
            this.btnLayerRangeSelect.Location = new System.Drawing.Point(446, 58);
            this.btnLayerRangeSelect.Menu = this.cmLayerRange;
            this.btnLayerRangeSelect.Name = "btnLayerRangeSelect";
            this.btnLayerRangeSelect.Size = new System.Drawing.Size(138, 26);
            this.btnLayerRangeSelect.TabIndex = 2;
            this.btnLayerRangeSelect.Text = "Select";
            this.btnLayerRangeSelect.UseVisualStyleBackColor = true;
            // 
            // FrmToolPattern
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(599, 339);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnAutoMarginRow);
            this.Controls.Add(this.btnAutoMarginCol);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.nmMarginCol);
            this.Controls.Add(this.btnResetDefaults);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.nmMarginRow);
            this.Controls.Add(this.btnLayerRangeSelect);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lbLayerRangeTo);
            this.Controls.Add(this.nmRows);
            this.Controls.Add(this.nmLayerRangeEnd);
            this.Controls.Add(this.lbInsideBounds);
            this.Controls.Add(this.nmLayerRangeStart);
            this.Controls.Add(this.lbRows);
            this.Controls.Add(this.lbLayerRange);
            this.Controls.Add(this.lbCols);
            this.Controls.Add(this.btnPattern);
            this.Controls.Add(this.lbVolumeHeight);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lbVolumeWidth);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lbDescription);
            this.Controls.Add(this.nmCols);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmToolPattern";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Pattern";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeStart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeEnd)).EndInit();
            this.cmLayerRange.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nmCols)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmRows)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginRow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginCol)).EndInit();
            this.tableAnchor.ResumeLayout(false);
            this.tableAnchor.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbDescription;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnPattern;
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
        private System.Windows.Forms.NumericUpDown nmCols;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnResetDefaults;
        private System.Windows.Forms.Label lbVolumeHeight;
        private System.Windows.Forms.Label lbVolumeWidth;
        private System.Windows.Forms.Label lbCols;
        private System.Windows.Forms.Label lbRows;
        private System.Windows.Forms.Label lbInsideBounds;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nmRows;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nmMarginCol;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nmMarginRow;
        private System.Windows.Forms.Button btnAutoMarginCol;
        private System.Windows.Forms.Button btnAutoMarginRow;
        private System.Windows.Forms.TableLayoutPanel tableAnchor;
        private System.Windows.Forms.RadioButton rbAnchorBottomLeft;
        private System.Windows.Forms.RadioButton rbAnchorBottomCenter;
        private System.Windows.Forms.RadioButton rbAnchorBottomRight;
        private System.Windows.Forms.RadioButton rbAnchorMiddleRight;
        private System.Windows.Forms.RadioButton rbAnchorMiddleCenter;
        private System.Windows.Forms.RadioButton rbAnchorMiddleLeft;
        private System.Windows.Forms.RadioButton rbAnchorTopCenter;
        private System.Windows.Forms.RadioButton rbAnchorTopLeft;
        private System.Windows.Forms.RadioButton rbAnchorTopRight;
        private System.Windows.Forms.RadioButton rbAnchorNone;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}