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
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.pnActions = new System.Windows.Forms.Panel();
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
            this.btnLayerRangeBottomLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeNormalLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.lbLayerRange = new System.Windows.Forms.Label();
            this.lbLayerRangeTo = new System.Windows.Forms.Label();
            this.nmLayerRangeStart = new System.Windows.Forms.NumericUpDown();
            this.pnDescription.SuspendLayout();
            this.pnActions.SuspendLayout();
            this.pnLayerRange.SuspendLayout();
            this.gbLayerRange.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeEnd)).BeginInit();
            this.cmLayerRange.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeStart)).BeginInit();
            this.SuspendLayout();
            // 
            // pnDescription
            // 
            this.pnDescription.AutoSize = true;
            this.pnDescription.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnDescription.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnDescription.Controls.Add(this.lbDescription);
            this.pnDescription.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnDescription.Location = new System.Drawing.Point(0, 0);
            this.pnDescription.Name = "pnDescription";
            this.pnDescription.Size = new System.Drawing.Size(547, 62);
            this.pnDescription.TabIndex = 7;
            // 
            // lbDescription
            // 
            this.lbDescription.AutoSize = true;
            this.lbDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbDescription.Location = new System.Drawing.Point(0, 0);
            this.lbDescription.Name = "lbDescription";
            this.lbDescription.Padding = new System.Windows.Forms.Padding(20);
            this.lbDescription.Size = new System.Drawing.Size(129, 60);
            this.lbDescription.TabIndex = 1;
            this.lbDescription.Text = "Description";
            this.lbDescription.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Image = global::UVtools.GUI.Properties.Resources.Cancel_24x24;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(383, 14);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.btnCancel.Size = new System.Drawing.Size(150, 48);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.EventClick);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Image = global::UVtools.GUI.Properties.Resources.Ok_24x24;
            this.btnOk.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOk.Location = new System.Drawing.Point(225, 14);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnOk.Name = "btnOk";
            this.btnOk.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.btnOk.Size = new System.Drawing.Size(150, 48);
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "&Ok";
            this.btnOk.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.EventClick);
            // 
            // pnActions
            // 
            this.pnActions.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnActions.Controls.Add(this.btnCancel);
            this.pnActions.Controls.Add(this.btnOk);
            this.pnActions.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnActions.Location = new System.Drawing.Point(0, 386);
            this.pnActions.Name = "pnActions";
            this.pnActions.Size = new System.Drawing.Size(547, 78);
            this.pnActions.TabIndex = 8;
            // 
            // pnContent
            // 
            this.pnContent.BackColor = System.Drawing.Color.White;
            this.pnContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnContent.Location = new System.Drawing.Point(0, 170);
            this.pnContent.Name = "pnContent";
            this.pnContent.Size = new System.Drawing.Size(547, 216);
            this.pnContent.TabIndex = 9;
            // 
            // pnLayerRange
            // 
            this.pnLayerRange.BackColor = System.Drawing.Color.White;
            this.pnLayerRange.Controls.Add(this.gbLayerRange);
            this.pnLayerRange.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnLayerRange.Location = new System.Drawing.Point(0, 62);
            this.pnLayerRange.Name = "pnLayerRange";
            this.pnLayerRange.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.pnLayerRange.Size = new System.Drawing.Size(547, 108);
            this.pnLayerRange.TabIndex = 0;
            // 
            // gbLayerRange
            // 
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
            this.gbLayerRange.Size = new System.Drawing.Size(547, 98);
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
            this.btnLayerRangeSelect.Size = new System.Drawing.Size(175, 26);
            this.btnLayerRangeSelect.TabIndex = 18;
            this.btnLayerRangeSelect.Text = "Select";
            this.btnLayerRangeSelect.UseVisualStyleBackColor = true;
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
            // FrmToolWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(547, 464);
            this.Controls.Add(this.pnContent);
            this.Controls.Add(this.pnLayerRange);
            this.Controls.Add(this.pnActions);
            this.Controls.Add(this.pnDescription);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(563, 0);
            this.Name = "FrmToolWindow";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "UVtools ToolWindow";
            this.TopMost = true;
            this.pnDescription.ResumeLayout(false);
            this.pnDescription.PerformLayout();
            this.pnActions.ResumeLayout(false);
            this.pnLayerRange.ResumeLayout(false);
            this.gbLayerRange.ResumeLayout(false);
            this.gbLayerRange.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeEnd)).EndInit();
            this.cmLayerRange.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeStart)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Panel pnActions;
        public System.Windows.Forms.Button btnCancel;
        public System.Windows.Forms.Button btnOk;
        public System.Windows.Forms.Panel pnDescription;
        public System.Windows.Forms.Label lbDescription;
        private System.Windows.Forms.Panel pnContent;
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
    }
}