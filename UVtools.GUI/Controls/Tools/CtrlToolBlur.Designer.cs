namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolBlur
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CtrlToolBlur));
            this.ctrlKernel = new UVtools.GUI.Controls.CtrlKernel();
            this.label1 = new System.Windows.Forms.Label();
            this.cbBlurOperation = new System.Windows.Forms.ComboBox();
            this.nmSize = new System.Windows.Forms.NumericUpDown();
            this.lbIterationsStart = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nmSize)).BeginInit();
            this.SuspendLayout();
            // 
            // ctrlKernel
            // 
            this.ctrlKernel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ctrlKernel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ctrlKernel.Location = new System.Drawing.Point(3, 80);
            this.ctrlKernel.Name = "ctrlKernel";
            this.ctrlKernel.Size = new System.Drawing.Size(585, 217);
            this.ctrlKernel.TabIndex = 22;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 12);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 20);
            this.label1.TabIndex = 26;
            this.label1.Text = "Algorithm:";
            // 
            // cbBlurOperation
            // 
            this.cbBlurOperation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbBlurOperation.DropDownWidth = 600;
            this.cbBlurOperation.FormattingEnabled = true;
            this.cbBlurOperation.Location = new System.Drawing.Point(92, 8);
            this.cbBlurOperation.Name = "cbBlurOperation";
            this.cbBlurOperation.Size = new System.Drawing.Size(496, 28);
            this.cbBlurOperation.TabIndex = 25;
            this.cbBlurOperation.SelectedIndexChanged += new System.EventHandler(this.cbBlurOperation_SelectedIndexChanged);
            // 
            // nmSize
            // 
            this.nmSize.Location = new System.Drawing.Point(92, 44);
            this.nmSize.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmSize.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmSize.Name = "nmSize";
            this.nmSize.Size = new System.Drawing.Size(83, 26);
            this.nmSize.TabIndex = 23;
            this.nmSize.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lbIterationsStart
            // 
            this.lbIterationsStart.AutoSize = true;
            this.lbIterationsStart.Location = new System.Drawing.Point(40, 47);
            this.lbIterationsStart.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbIterationsStart.Name = "lbIterationsStart";
            this.lbIterationsStart.Size = new System.Drawing.Size(44, 20);
            this.lbIterationsStart.TabIndex = 24;
            this.lbIterationsStart.Text = "Size:";
            this.toolTip.SetToolTip(this.lbIterationsStart, resources.GetString("lbIterationsStart.ToolTip"));
            // 
            // CtrlToolBlur
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbBlurOperation);
            this.Controls.Add(this.nmSize);
            this.Controls.Add(this.lbIterationsStart);
            this.Controls.Add(this.ctrlKernel);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "CtrlToolBlur";
            this.Size = new System.Drawing.Size(596, 309);
            ((System.ComponentModel.ISupportInitialize)(this.nmSize)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public CtrlKernel ctrlKernel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbBlurOperation;
        private System.Windows.Forms.NumericUpDown nmSize;
        private System.Windows.Forms.Label lbIterationsStart;
    }
}
