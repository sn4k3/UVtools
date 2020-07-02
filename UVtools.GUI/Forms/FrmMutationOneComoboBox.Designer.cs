using UVtools.GUI.Controls;

namespace UVtools.GUI.Forms
{
    partial class FrmMutationOneComboBox
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMutationOneComboBox));
            this.lbDescription = new System.Windows.Forms.Label();
            this.lbValue = new System.Windows.Forms.Label();
            this.lbLayerRange = new System.Windows.Forms.Label();
            this.nmLayerRangeStart = new System.Windows.Forms.NumericUpDown();
            this.nmLayerRangeEnd = new System.Windows.Forms.NumericUpDown();
            this.lbLayerRangeTo = new System.Windows.Forms.Label();
            this.cmLayerRange = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.btnLayerRangeAllLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeCurrentLayer = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeBottomLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerRangeNormalLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnMutate = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.cbValue = new System.Windows.Forms.ComboBox();
            this.btnLayerRangeSelect = new UVtools.GUI.Controls.SplitButton();
            this.cbMakeCopy = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeStart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeEnd)).BeginInit();
            this.cmLayerRange.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbDescription
            // 
            this.lbDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lbDescription.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbDescription.Location = new System.Drawing.Point(13, 14);
            this.lbDescription.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbDescription.Name = "lbDescription";
            this.lbDescription.Size = new System.Drawing.Size(584, 128);
            this.lbDescription.TabIndex = 0;
            this.lbDescription.Text = "Description";
            // 
            // lbValue
            // 
            this.lbValue.Location = new System.Drawing.Point(2, 185);
            this.lbValue.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbValue.Name = "lbValue";
            this.lbValue.Size = new System.Drawing.Size(108, 20);
            this.lbValue.TabIndex = 3;
            this.lbValue.Text = "Label:";
            this.lbValue.TextAlign = System.Drawing.ContentAlignment.TopRight;
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
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Image = global::UVtools.GUI.Properties.Resources.Cancel_24x24;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(434, 230);
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
            this.btnMutate.Location = new System.Drawing.Point(276, 230);
            this.btnMutate.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnMutate.Name = "btnMutate";
            this.btnMutate.Size = new System.Drawing.Size(150, 48);
            this.btnMutate.TabIndex = 5;
            this.btnMutate.Text = "&Mutate";
            this.btnMutate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnMutate.UseVisualStyleBackColor = true;
            this.btnMutate.Click += new System.EventHandler(this.ItemClicked);
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
            // cbValue
            // 
            this.cbValue.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbValue.FormattingEnabled = true;
            this.cbValue.Location = new System.Drawing.Point(117, 181);
            this.cbValue.Name = "cbValue";
            this.cbValue.Size = new System.Drawing.Size(121, 28);
            this.cbValue.TabIndex = 13;
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
            // cbMakeCopy
            // 
            this.cbMakeCopy.AutoSize = true;
            this.cbMakeCopy.Location = new System.Drawing.Point(244, 183);
            this.cbMakeCopy.Name = "cbMakeCopy";
            this.cbMakeCopy.Size = new System.Drawing.Size(104, 24);
            this.cbMakeCopy.TabIndex = 14;
            this.cbMakeCopy.Text = "Make copy";
            this.cbMakeCopy.UseVisualStyleBackColor = true;
            // 
            // FrmMutationOneComboBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(599, 292);
            this.Controls.Add(this.cbMakeCopy);
            this.Controls.Add(this.cbValue);
            this.Controls.Add(this.btnLayerRangeSelect);
            this.Controls.Add(this.lbLayerRangeTo);
            this.Controls.Add(this.nmLayerRangeEnd);
            this.Controls.Add(this.nmLayerRangeStart);
            this.Controls.Add(this.lbLayerRange);
            this.Controls.Add(this.btnMutate);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lbValue);
            this.Controls.Add(this.lbDescription);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmMutationOneComboBox";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Form1";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeStart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmLayerRangeEnd)).EndInit();
            this.cmLayerRange.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbDescription;
        private System.Windows.Forms.Label lbValue;
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
        private System.Windows.Forms.ComboBox cbValue;
        private System.Windows.Forms.CheckBox cbMakeCopy;
    }
}