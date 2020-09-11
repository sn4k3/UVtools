namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolMorph
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CtrlToolMorph));
            this.ctrlKernel = new UVtools.GUI.Controls.CtrlKernel();
            this.cbIterationsFade = new System.Windows.Forms.CheckBox();
            this.nmIterationsEnd = new System.Windows.Forms.NumericUpDown();
            this.lbIterationsStop = new System.Windows.Forms.Label();
            this.nmIterationsStart = new System.Windows.Forms.NumericUpDown();
            this.lbIterationsStart = new System.Windows.Forms.Label();
            this.cbMorphOperation = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nmIterationsEnd)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmIterationsStart)).BeginInit();
            this.SuspendLayout();
            // 
            // ctrlKernel
            // 
            this.ctrlKernel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ctrlKernel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ctrlKernel.Location = new System.Drawing.Point(3, 87);
            this.ctrlKernel.Name = "ctrlKernel";
            this.ctrlKernel.Size = new System.Drawing.Size(533, 217);
            this.ctrlKernel.TabIndex = 22;
            this.ctrlKernel.Visible = false;
            // 
            // cbIterationsFade
            // 
            this.cbIterationsFade.AutoSize = true;
            this.cbIterationsFade.Location = new System.Drawing.Point(273, 14);
            this.cbIterationsFade.Name = "cbIterationsFade";
            this.cbIterationsFade.Size = new System.Drawing.Size(108, 24);
            this.cbIterationsFade.TabIndex = 21;
            this.cbIterationsFade.Text = "Fade in/out";
            this.toolTip.SetToolTip(this.cbIterationsFade, "Allow the number of iterations to be gradually varied as the operation progresses" +
        " from the starting layer to the ending layer.");
            this.cbIterationsFade.UseVisualStyleBackColor = true;
            this.cbIterationsFade.CheckedChanged += new System.EventHandler(this.EventCheckedChanged);
            // 
            // nmIterationsEnd
            // 
            this.nmIterationsEnd.Enabled = false;
            this.nmIterationsEnd.Location = new System.Drawing.Point(205, 13);
            this.nmIterationsEnd.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmIterationsEnd.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmIterationsEnd.Name = "nmIterationsEnd";
            this.nmIterationsEnd.Size = new System.Drawing.Size(61, 26);
            this.nmIterationsEnd.TabIndex = 20;
            this.nmIterationsEnd.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lbIterationsStop
            // 
            this.lbIterationsStop.AutoSize = true;
            this.lbIterationsStop.Enabled = false;
            this.lbIterationsStop.Location = new System.Drawing.Point(166, 16);
            this.lbIterationsStop.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbIterationsStop.Name = "lbIterationsStop";
            this.lbIterationsStop.Size = new System.Drawing.Size(31, 20);
            this.lbIterationsStop.TabIndex = 19;
            this.lbIterationsStop.Text = "To:";
            // 
            // nmIterationsStart
            // 
            this.nmIterationsStart.Location = new System.Drawing.Point(93, 13);
            this.nmIterationsStart.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmIterationsStart.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmIterationsStart.Name = "nmIterationsStart";
            this.nmIterationsStart.Size = new System.Drawing.Size(61, 26);
            this.nmIterationsStart.TabIndex = 17;
            this.nmIterationsStart.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lbIterationsStart
            // 
            this.lbIterationsStart.AutoSize = true;
            this.lbIterationsStart.Location = new System.Drawing.Point(4, 15);
            this.lbIterationsStart.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbIterationsStart.Name = "lbIterationsStart";
            this.lbIterationsStart.Size = new System.Drawing.Size(80, 20);
            this.lbIterationsStart.TabIndex = 18;
            this.lbIterationsStart.Text = "Iterations:";
            this.toolTip.SetToolTip(this.lbIterationsStart, resources.GetString("lbIterationsStart.ToolTip"));
            // 
            // cbMorphOperation
            // 
            this.cbMorphOperation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbMorphOperation.FormattingEnabled = true;
            this.cbMorphOperation.Location = new System.Drawing.Point(93, 46);
            this.cbMorphOperation.Name = "cbMorphOperation";
            this.cbMorphOperation.Size = new System.Drawing.Size(443, 28);
            this.cbMorphOperation.TabIndex = 23;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 49);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 20);
            this.label1.TabIndex = 24;
            this.label1.Text = "Operation:";
            // 
            // CtrlToolMorph
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoSize = true;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbMorphOperation);
            this.Controls.Add(this.ctrlKernel);
            this.Controls.Add(this.cbIterationsFade);
            this.Controls.Add(this.nmIterationsEnd);
            this.Controls.Add(this.lbIterationsStop);
            this.Controls.Add(this.nmIterationsStart);
            this.Controls.Add(this.lbIterationsStart);
            this.ExtraCheckboxVisible = true;
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "CtrlToolMorph";
            this.Size = new System.Drawing.Size(540, 307);
            ((System.ComponentModel.ISupportInitialize)(this.nmIterationsEnd)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmIterationsStart)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public CtrlKernel ctrlKernel;
        private System.Windows.Forms.CheckBox cbIterationsFade;
        private System.Windows.Forms.NumericUpDown nmIterationsEnd;
        private System.Windows.Forms.Label lbIterationsStop;
        private System.Windows.Forms.NumericUpDown nmIterationsStart;
        private System.Windows.Forms.Label lbIterationsStart;
        private System.Windows.Forms.ComboBox cbMorphOperation;
        private System.Windows.Forms.Label label1;
    }
}
