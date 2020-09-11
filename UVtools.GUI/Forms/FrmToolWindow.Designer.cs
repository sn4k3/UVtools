using UVtools.GUI.Controls;

namespace UVtools.GUI.Forms
{
    partial class FrmToolWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmToolWindow));
            this.pnDescription = new System.Windows.Forms.Panel();
            this.lbDescription = new System.Windows.Forms.Label();
            this.pnContent = new System.Windows.Forms.Panel();
            this.pnLayerRange = new System.Windows.Forms.Panel();
            this.gbLayerRange = new System.Windows.Forms.GroupBox();
            this.lbLayerRangeCount = new System.Windows.Forms.Label();
            this.lbLayerRangeFromMM = new System.Windows.Forms.Label();
            this.lbLayerRangeToMM = new System.Windows.Forms.Label();
            this.nmLayerRangeEnd = new System.Windows.Forms.NumericUpDown();
            this.btnLayerRangeSelect = new UVtools.GUI.Controls.SplitButton();
            this.cmLayerRange = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.btnLayerRangeAllLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeCurrentLayer = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnLayerRangeBottomLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeNormalLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.btnLayerRangeFirstLayer = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeLastLayer = new System.Windows.Forms.ToolStripMenuItem();
            this.lbLayerRange = new System.Windows.Forms.Label();
            this.lbLayerRangeTo = new System.Windows.Forms.Label();
            this.nmLayerRangeStart = new System.Windows.Forms.NumericUpDown();
            this.table = new System.Windows.Forms.TableLayoutPanel();
            this.pnActions = new System.Windows.Forms.Panel();
            this.cbActionExtra = new System.Windows.Forms.CheckBox();
            this.btnActionExtra = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.pnDescription.SuspendLayout();
            this.pnLayerRange.SuspendLayout();
            this.gbLayerRange.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeEnd)).BeginInit();
            this.cmLayerRange.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeStart)).BeginInit();
            this.table.SuspendLayout();
            this.pnActions.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnDescription
            // 
            this.pnDescription.AutoSize = true;
            this.pnDescription.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnDescription.BackColor = System.Drawing.SystemColors.Control;
            this.pnDescription.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnDescription.Controls.Add(this.lbDescription);
            this.pnDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnDescription.Location = new System.Drawing.Point(3, 3);
            this.pnDescription.Name = "pnDescription";
            this.pnDescription.Size = new System.Drawing.Size(548, 62);
            this.pnDescription.TabIndex = 7;
            // 
            // lbDescription
            // 
            this.lbDescription.AutoSize = true;
            this.lbDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbDescription.Location = new System.Drawing.Point(0, 0);
            this.lbDescription.Name = "lbDescription";
            this.lbDescription.Padding = new System.Windows.Forms.Padding(20, 20, 10, 20);
            this.lbDescription.Size = new System.Drawing.Size(119, 60);
            this.lbDescription.TabIndex = 1;
            this.lbDescription.Text = "Description";
            this.lbDescription.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnContent
            // 
            this.pnContent.AutoSize = true;
            this.pnContent.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnContent.BackColor = System.Drawing.Color.White;
            this.pnContent.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnContent.Location = new System.Drawing.Point(3, 191);
            this.pnContent.Name = "pnContent";
            this.pnContent.Size = new System.Drawing.Size(548, 2);
            this.pnContent.TabIndex = 9;
            // 
            // pnLayerRange
            // 
            this.pnLayerRange.AutoSize = true;
            this.pnLayerRange.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnLayerRange.BackColor = System.Drawing.Color.White;
            this.pnLayerRange.Controls.Add(this.gbLayerRange);
            this.pnLayerRange.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnLayerRange.Location = new System.Drawing.Point(3, 71);
            this.pnLayerRange.Name = "pnLayerRange";
            this.pnLayerRange.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.pnLayerRange.Size = new System.Drawing.Size(548, 114);
            this.pnLayerRange.TabIndex = 0;
            // 
            // gbLayerRange
            // 
            this.gbLayerRange.AutoSize = true;
            this.gbLayerRange.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.gbLayerRange.BackColor = System.Drawing.Color.White;
            this.gbLayerRange.Controls.Add(this.lbLayerRangeCount);
            this.gbLayerRange.Controls.Add(this.lbLayerRangeFromMM);
            this.gbLayerRange.Controls.Add(this.lbLayerRangeToMM);
            this.gbLayerRange.Controls.Add(this.nmLayerRangeEnd);
            this.gbLayerRange.Controls.Add(this.btnLayerRangeSelect);
            this.gbLayerRange.Controls.Add(this.lbLayerRange);
            this.gbLayerRange.Controls.Add(this.lbLayerRangeTo);
            this.gbLayerRange.Controls.Add(this.nmLayerRangeStart);
            this.gbLayerRange.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbLayerRange.Location = new System.Drawing.Point(0, 10);
            this.gbLayerRange.Name = "gbLayerRange";
            this.gbLayerRange.Size = new System.Drawing.Size(548, 104);
            this.gbLayerRange.TabIndex = 19;
            this.gbLayerRange.TabStop = false;
            this.gbLayerRange.Text = "Layer Range Selector";
            // 
            // lbLayerRangeCount
            // 
            this.lbLayerRangeCount.Location = new System.Drawing.Point(359, 62);
            this.lbLayerRangeCount.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbLayerRangeCount.Name = "lbLayerRangeCount";
            this.lbLayerRangeCount.Size = new System.Drawing.Size(175, 20);
            this.lbLayerRangeCount.TabIndex = 21;
            this.lbLayerRangeCount.Text = "(layers)";
            this.lbLayerRangeCount.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lbLayerRangeFromMM
            // 
            this.lbLayerRangeFromMM.Location = new System.Drawing.Point(65, 62);
            this.lbLayerRangeFromMM.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbLayerRangeFromMM.Name = "lbLayerRangeFromMM";
            this.lbLayerRangeFromMM.Size = new System.Drawing.Size(120, 20);
            this.lbLayerRangeFromMM.TabIndex = 20;
            this.lbLayerRangeFromMM.Text = "(mm)";
            this.lbLayerRangeFromMM.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lbLayerRangeToMM
            // 
            this.lbLayerRangeToMM.Location = new System.Drawing.Point(232, 62);
            this.lbLayerRangeToMM.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbLayerRangeToMM.Name = "lbLayerRangeToMM";
            this.lbLayerRangeToMM.Size = new System.Drawing.Size(120, 20);
            this.lbLayerRangeToMM.TabIndex = 19;
            this.lbLayerRangeToMM.Text = "(mm)";
            this.lbLayerRangeToMM.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // nmLayerRangeEnd
            // 
            this.nmLayerRangeEnd.Location = new System.Drawing.Point(232, 31);
            this.nmLayerRangeEnd.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmLayerRangeEnd.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.nmLayerRangeEnd.Name = "nmLayerRangeEnd";
            this.nmLayerRangeEnd.Size = new System.Drawing.Size(120, 26);
            this.nmLayerRangeEnd.TabIndex = 14;
            this.nmLayerRangeEnd.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // btnLayerRangeSelect
            // 
            this.btnLayerRangeSelect.Location = new System.Drawing.Point(359, 31);
            this.btnLayerRangeSelect.Menu = this.cmLayerRange;
            this.btnLayerRangeSelect.Name = "btnLayerRangeSelect";
            this.btnLayerRangeSelect.Size = new System.Drawing.Size(180, 26);
            this.btnLayerRangeSelect.TabIndex = 18;
            this.btnLayerRangeSelect.Text = "Select";
            this.btnLayerRangeSelect.UseVisualStyleBackColor = true;
            // 
            // cmLayerRange
            // 
            this.cmLayerRange.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnLayerRangeAllLayers,
            this.btnLayerRangeCurrentLayer,
            this.toolStripSeparator1,
            this.btnLayerRangeBottomLayers,
            this.btnLayerRangeNormalLayers,
            this.toolStripSeparator2,
            this.btnLayerRangeFirstLayer,
            this.btnLayerRangeLastLayer});
            this.cmLayerRange.Name = "cmLayerRange";
            this.cmLayerRange.Size = new System.Drawing.Size(226, 148);
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
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(222, 6);
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
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(222, 6);
            // 
            // btnLayerRangeFirstLayer
            // 
            this.btnLayerRangeFirstLayer.Name = "btnLayerRangeFirstLayer";
            this.btnLayerRangeFirstLayer.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.F)));
            this.btnLayerRangeFirstLayer.Size = new System.Drawing.Size(225, 22);
            this.btnLayerRangeFirstLayer.Text = "&First Layer";
            this.btnLayerRangeFirstLayer.Click += new System.EventHandler(this.EventClick);
            // 
            // btnLayerRangeLastLayer
            // 
            this.btnLayerRangeLastLayer.Name = "btnLayerRangeLastLayer";
            this.btnLayerRangeLastLayer.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.L)));
            this.btnLayerRangeLastLayer.Size = new System.Drawing.Size(225, 22);
            this.btnLayerRangeLastLayer.Text = "&Last Layer";
            this.btnLayerRangeLastLayer.Click += new System.EventHandler(this.EventClick);
            // 
            // lbLayerRange
            // 
            this.lbLayerRange.AutoSize = true;
            this.lbLayerRange.Location = new System.Drawing.Point(7, 34);
            this.lbLayerRange.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbLayerRange.Name = "lbLayerRange";
            this.lbLayerRange.Size = new System.Drawing.Size(50, 20);
            this.lbLayerRange.TabIndex = 16;
            this.lbLayerRange.Text = "From:";
            // 
            // lbLayerRangeTo
            // 
            this.lbLayerRangeTo.AutoSize = true;
            this.lbLayerRangeTo.Location = new System.Drawing.Point(193, 34);
            this.lbLayerRangeTo.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbLayerRangeTo.Name = "lbLayerRangeTo";
            this.lbLayerRangeTo.Size = new System.Drawing.Size(31, 20);
            this.lbLayerRangeTo.TabIndex = 17;
            this.lbLayerRangeTo.Text = "To:";
            // 
            // nmLayerRangeStart
            // 
            this.nmLayerRangeStart.Location = new System.Drawing.Point(65, 31);
            this.nmLayerRangeStart.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmLayerRangeStart.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.nmLayerRangeStart.Name = "nmLayerRangeStart";
            this.nmLayerRangeStart.Size = new System.Drawing.Size(120, 26);
            this.nmLayerRangeStart.TabIndex = 13;
            this.nmLayerRangeStart.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // table
            // 
            this.table.AutoSize = true;
            this.table.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.table.ColumnCount = 1;
            this.table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.table.Controls.Add(this.pnDescription, 0, 0);
            this.table.Controls.Add(this.pnActions, 0, 3);
            this.table.Controls.Add(this.pnContent, 0, 2);
            this.table.Controls.Add(this.pnLayerRange, 0, 1);
            this.table.Dock = System.Windows.Forms.DockStyle.Fill;
            this.table.Location = new System.Drawing.Point(0, 0);
            this.table.Name = "table";
            this.table.RowCount = 4;
            this.table.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.table.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.table.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.table.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.table.Size = new System.Drawing.Size(554, 281);
            this.table.TabIndex = 10;
            // 
            // pnActions
            // 
            this.pnActions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnActions.BackColor = System.Drawing.SystemColors.Control;
            this.pnActions.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnActions.Controls.Add(this.cbActionExtra);
            this.pnActions.Controls.Add(this.btnActionExtra);
            this.pnActions.Controls.Add(this.btnCancel);
            this.pnActions.Controls.Add(this.btnOk);
            this.pnActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnActions.Location = new System.Drawing.Point(3, 199);
            this.pnActions.Name = "pnActions";
            this.pnActions.Size = new System.Drawing.Size(548, 85);
            this.pnActions.TabIndex = 8;
            // 
            // cbActionExtra
            // 
            this.cbActionExtra.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cbActionExtra.AutoSize = true;
            this.cbActionExtra.Location = new System.Drawing.Point(4, 32);
            this.cbActionExtra.Name = "cbActionExtra";
            this.cbActionExtra.Size = new System.Drawing.Size(202, 24);
            this.cbActionExtra.TabIndex = 26;
            this.cbActionExtra.Text = "Show Advanced Options";
            this.cbActionExtra.UseVisualStyleBackColor = true;
            this.cbActionExtra.Visible = false;
            this.cbActionExtra.CheckedChanged += new System.EventHandler(this.EventClick);
            // 
            // btnActionExtra
            // 
            this.btnActionExtra.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnActionExtra.AutoSize = true;
            this.btnActionExtra.Image = global::UVtools.GUI.Properties.Resources.Rotate_16x16;
            this.btnActionExtra.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnActionExtra.Location = new System.Drawing.Point(4, 19);
            this.btnActionExtra.Name = "btnActionExtra";
            this.btnActionExtra.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.btnActionExtra.Size = new System.Drawing.Size(177, 48);
            this.btnActionExtra.TabIndex = 7;
            this.btnActionExtra.Text = "&Reset to defaults";
            this.btnActionExtra.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnActionExtra.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnActionExtra.Visible = false;
            this.btnActionExtra.Click += new System.EventHandler(this.EventClick);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Image = global::UVtools.GUI.Properties.Resources.Cancel_24x24;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(430, 19);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.btnCancel.Size = new System.Drawing.Size(112, 48);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.EventClick);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.AutoSize = true;
            this.btnOk.Image = global::UVtools.GUI.Properties.Resources.Ok_24x24;
            this.btnOk.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOk.Location = new System.Drawing.Point(330, 19);
            this.btnOk.Name = "btnOk";
            this.btnOk.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.btnOk.Size = new System.Drawing.Size(93, 48);
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "&Ok";
            this.btnOk.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnOk.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnOk.Click += new System.EventHandler(this.EventClick);
            // 
            // FrmToolWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(554, 281);
            this.Controls.Add(this.table);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(570, 39);
            this.Name = "FrmToolWindow";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "UVtools ToolWindow";
            this.TopMost = true;
            this.pnDescription.ResumeLayout(false);
            this.pnDescription.PerformLayout();
            this.pnLayerRange.ResumeLayout(false);
            this.pnLayerRange.PerformLayout();
            this.gbLayerRange.ResumeLayout(false);
            this.gbLayerRange.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeEnd)).EndInit();
            this.cmLayerRange.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeStart)).EndInit();
            this.table.ResumeLayout(false);
            this.table.PerformLayout();
            this.pnActions.ResumeLayout(false);
            this.pnActions.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        public System.Windows.Forms.Panel pnDescription;
        public System.Windows.Forms.Label lbDescription;
        private System.Windows.Forms.Panel pnLayerRange;
        private System.Windows.Forms.ContextMenuStrip cmLayerRange;
        private System.Windows.Forms.ToolStripMenuItem btnLayerRangeAllLayers;
        private System.Windows.Forms.ToolStripMenuItem btnLayerRangeCurrentLayer;
        private System.Windows.Forms.ToolStripMenuItem btnLayerRangeBottomLayers;
        private System.Windows.Forms.ToolStripMenuItem btnLayerRangeNormalLayers;
        public System.Windows.Forms.Label lbLayerRangeTo;
        public System.Windows.Forms.NumericUpDown nmLayerRangeEnd;
        public System.Windows.Forms.NumericUpDown nmLayerRangeStart;
        public System.Windows.Forms.Label lbLayerRange;
        public System.Windows.Forms.GroupBox gbLayerRange;
        public SplitButton btnLayerRangeSelect;
        public System.Windows.Forms.Label lbLayerRangeToMM;
        public System.Windows.Forms.Label lbLayerRangeFromMM;
        public System.Windows.Forms.Label lbLayerRangeCount;
        public System.Windows.Forms.Panel pnContent;
        private System.Windows.Forms.TableLayoutPanel table;
        public System.Windows.Forms.Panel pnActions;
        public System.Windows.Forms.Button btnCancel;
        public System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem btnLayerRangeFirstLayer;
        private System.Windows.Forms.ToolStripMenuItem btnLayerRangeLastLayer;
        public System.Windows.Forms.Button btnActionExtra;
        private System.Windows.Forms.CheckBox cbActionExtra;
    }
}