using UVtools.GUI.Controls;

namespace UVtools.GUI.Forms
{
    partial class FrmToolLayerImport
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmToolLayerImport));
            this.lbLayerRange = new System.Windows.Forms.Label();
            this.nmInsertAfterLayer = new System.Windows.Forms.NumericUpDown();
            this.cmLayerRange = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.btnLayerRangeAllLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeCurrentLayer = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeBottomLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeNormalLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.lbHeight = new System.Windows.Forms.Label();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.ctrlDescriptionPanel1 = new UVtools.GUI.Controls.CtrlDescriptionPanel();
            ((System.ComponentModel.ISupportInitialize)(this.nmInsertAfterLayer)).BeginInit();
            this.cmLayerRange.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbLayerRange
            // 
            this.lbLayerRange.AutoSize = true;
            this.lbLayerRange.Location = new System.Drawing.Point(20, 181);
            this.lbLayerRange.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbLayerRange.Name = "lbLayerRange";
            this.lbLayerRange.Size = new System.Drawing.Size(128, 20);
            this.lbLayerRange.TabIndex = 9;
            this.lbLayerRange.Text = "Insert after layer:";
            // 
            // nmInsertAfterLayer
            // 
            this.nmInsertAfterLayer.Location = new System.Drawing.Point(156, 178);
            this.nmInsertAfterLayer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmInsertAfterLayer.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.nmInsertAfterLayer.Name = "nmInsertAfterLayer";
            this.nmInsertAfterLayer.Size = new System.Drawing.Size(90, 26);
            this.nmInsertAfterLayer.TabIndex = 0;
            this.nmInsertAfterLayer.ValueChanged += new System.EventHandler(this.EventValueChanged);
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
            // lbHeight
            // 
            this.lbHeight.AutoSize = true;
            this.lbHeight.Location = new System.Drawing.Point(254, 181);
            this.lbHeight.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbHeight.Name = "lbHeight";
            this.lbHeight.Size = new System.Drawing.Size(85, 20);
            this.lbHeight.TabIndex = 24;
            this.lbHeight.Text = "(10.10mm)";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(438, 179);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(153, 24);
            this.checkBox1.TabIndex = 25;
            this.checkBox1.Text = "Replace this layer";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Image = global::UVtools.GUI.Properties.Resources.Cancel_24x24;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(434, 418);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(150, 48);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.EventClick);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnOk.Image = global::UVtools.GUI.Properties.Resources.Ok_24x24;
            this.btnOk.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOk.Location = new System.Drawing.Point(276, 418);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(150, 48);
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "&Clone";
            this.btnOk.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.EventClick);
            // 
            // ctrlDescriptionPanel1
            // 
            this.ctrlDescriptionPanel1.AutoSize = true;
            this.ctrlDescriptionPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ctrlDescriptionPanel1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ctrlDescriptionPanel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ctrlDescriptionPanel1.Description = "Import layers from local files at a defined height.";
            this.ctrlDescriptionPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.ctrlDescriptionPanel1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ctrlDescriptionPanel1.Location = new System.Drawing.Point(0, 0);
            this.ctrlDescriptionPanel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ctrlDescriptionPanel1.Name = "ctrlDescriptionPanel1";
            this.ctrlDescriptionPanel1.Size = new System.Drawing.Size(599, 62);
            this.ctrlDescriptionPanel1.TabIndex = 26;
            // 
            // FrmToolLayerImport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(599, 480);
            this.Controls.Add(this.ctrlDescriptionPanel1);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.lbHeight);
            this.Controls.Add(this.nmInsertAfterLayer);
            this.Controls.Add(this.lbLayerRange);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmToolLayerImport";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Import layer(s)";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.nmInsertAfterLayer)).EndInit();
            this.cmLayerRange.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Label lbLayerRange;
        private System.Windows.Forms.NumericUpDown nmInsertAfterLayer;
        private System.Windows.Forms.ContextMenuStrip cmLayerRange;
        private System.Windows.Forms.ToolStripMenuItem btnLayerRangeAllLayers;
        private System.Windows.Forms.ToolStripMenuItem btnLayerRangeCurrentLayer;
        private System.Windows.Forms.ToolStripMenuItem btnLayerRangeBottomLayers;
        private System.Windows.Forms.ToolStripMenuItem btnLayerRangeNormalLayers;
        private System.Windows.Forms.Label lbHeight;
        private System.Windows.Forms.CheckBox checkBox1;
        private CtrlDescriptionPanel ctrlDescriptionPanel1;
    }
}