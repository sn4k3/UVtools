
namespace UVtools.GUI.Forms
{
    partial class FrmToolChangeResolution
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmToolChangeResolution));
            this.lbDescription = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.lbCurrent = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cbPreset = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.nmNewX = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.nmNewY = new System.Windows.Forms.NumericUpDown();
            this.lbObjectVolume = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nmNewX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmNewY)).BeginInit();
            this.SuspendLayout();
            // 
            // lbDescription
            // 
            this.lbDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbDescription.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbDescription.Location = new System.Drawing.Point(13, 14);
            this.lbDescription.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbDescription.Name = "lbDescription";
            this.lbDescription.Size = new System.Drawing.Size(630, 98);
            this.lbDescription.TabIndex = 0;
            this.lbDescription.Text = resources.GetString("lbDescription.Text");
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Image = global::UVtools.GUI.Properties.Resources.Cancel_24x24;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(491, 226);
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
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Enabled = false;
            this.btnOk.Image = global::UVtools.GUI.Properties.Resources.Ok_24x24;
            this.btnOk.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOk.Location = new System.Drawing.Point(293, 226);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(190, 48);
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "Change Resolution";
            this.btnOk.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.EventClick);
            // 
            // lbCurrent
            // 
            this.lbCurrent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lbCurrent.AutoSize = true;
            this.lbCurrent.Location = new System.Drawing.Point(12, 112);
            this.lbCurrent.Name = "lbCurrent";
            this.lbCurrent.Size = new System.Drawing.Size(139, 20);
            this.lbCurrent.TabIndex = 7;
            this.lbCurrent.Text = "Current resolution:";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(287, 177);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 20);
            this.label2.TabIndex = 8;
            this.label2.Text = "Preset:";
            // 
            // cbPreset
            // 
            this.cbPreset.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbPreset.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPreset.FormattingEnabled = true;
            this.cbPreset.Location = new System.Drawing.Point(352, 173);
            this.cbPreset.Name = "cbPreset";
            this.cbPreset.Size = new System.Drawing.Size(289, 28);
            this.cbPreset.TabIndex = 9;
            this.cbPreset.SelectedIndexChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 176);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 20);
            this.label1.TabIndex = 11;
            this.label1.Text = "New X/Y:";
            // 
            // nmNewX
            // 
            this.nmNewX.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.nmNewX.Location = new System.Drawing.Point(93, 174);
            this.nmNewX.Maximum = new decimal(new int[] {
            20000,
            0,
            0,
            0});
            this.nmNewX.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nmNewX.Name = "nmNewX";
            this.nmNewX.Size = new System.Drawing.Size(80, 26);
            this.nmNewX.TabIndex = 12;
            this.nmNewX.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nmNewX.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(179, 177);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(16, 20);
            this.label3.TabIndex = 13;
            this.label3.Text = "x";
            // 
            // nmNewY
            // 
            this.nmNewY.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.nmNewY.Location = new System.Drawing.Point(201, 174);
            this.nmNewY.Maximum = new decimal(new int[] {
            20000,
            0,
            0,
            0});
            this.nmNewY.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nmNewY.Name = "nmNewY";
            this.nmNewY.Size = new System.Drawing.Size(80, 26);
            this.nmNewY.TabIndex = 14;
            this.nmNewY.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nmNewY.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // lbObjectVolume
            // 
            this.lbObjectVolume.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lbObjectVolume.AutoSize = true;
            this.lbObjectVolume.Location = new System.Drawing.Point(13, 144);
            this.lbObjectVolume.Name = "lbObjectVolume";
            this.lbObjectVolume.Size = new System.Drawing.Size(113, 20);
            this.lbObjectVolume.TabIndex = 15;
            this.lbObjectVolume.Text = "Object volume:";
            // 
            // FrmToolChangeResolution
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(656, 288);
            this.Controls.Add(this.lbObjectVolume);
            this.Controls.Add(this.nmNewY);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.nmNewX);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbPreset);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lbCurrent);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lbDescription);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmToolChangeResolution";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Change Resolution";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.nmNewX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmNewY)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbDescription;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Label lbCurrent;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbPreset;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nmNewX;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nmNewY;
        private System.Windows.Forms.Label lbObjectVolume;
    }
}