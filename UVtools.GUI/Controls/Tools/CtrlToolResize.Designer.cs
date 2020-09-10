namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolResize
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CtrlToolResize));
            this.cbFade = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.cbConstrainXY = new System.Windows.Forms.CheckBox();
            this.nmY = new System.Windows.Forms.NumericUpDown();
            this.lbY = new System.Windows.Forms.Label();
            this.nmX = new System.Windows.Forms.NumericUpDown();
            this.lbX = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nmY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmX)).BeginInit();
            this.SuspendLayout();
            // 
            // cbFade
            // 
            this.cbFade.AutoSize = true;
            this.cbFade.Location = new System.Drawing.Point(8, 41);
            this.cbFade.Name = "cbFade";
            this.cbFade.Size = new System.Drawing.Size(283, 24);
            this.cbFade.TabIndex = 26;
            this.cbFade.Text = "Increase or decrease towards 100%";
            this.toolTip.SetToolTip(this.cbFade, "If checked, resize will gradually adjust the scale factor from the percentage specified" +
        " to 100% as the operation progresses from the starting layer to the ending layer.");
            this.cbFade.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(360, 10);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(23, 20);
            this.label2.TabIndex = 25;
            this.label2.Text = "%";
            this.toolTip.SetToolTip(this.label2, resources.GetString("label2.ToolTip"));
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(164, 10);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(23, 20);
            this.label1.TabIndex = 24;
            this.label1.Text = "%";
            this.toolTip.SetToolTip(this.label1, resources.GetString("label1.ToolTip"));
            // 
            // cbConstrainXY
            // 
            this.cbConstrainXY.AutoSize = true;
            this.cbConstrainXY.Checked = true;
            this.cbConstrainXY.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbConstrainXY.Location = new System.Drawing.Point(388, 8);
            this.cbConstrainXY.Name = "cbConstrainXY";
            this.cbConstrainXY.Size = new System.Drawing.Size(181, 24);
            this.cbConstrainXY.TabIndex = 23;
            this.cbConstrainXY.Text = "Constrain Proportions";
            this.cbConstrainXY.CheckedChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // nmY
            // 
            this.nmY.DecimalPlaces = 2;
            this.nmY.Enabled = false;
            this.nmY.Location = new System.Drawing.Point(232, 7);
            this.nmY.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmY.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nmY.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmY.Name = "nmY";
            this.nmY.Size = new System.Drawing.Size(120, 26);
            this.nmY.TabIndex = 22;
            this.nmY.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nmY.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // lbY
            // 
            this.lbY.AutoSize = true;
            this.lbY.Enabled = false;
            this.lbY.Location = new System.Drawing.Point(200, 10);
            this.lbY.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbY.Name = "lbY";
            this.lbY.Size = new System.Drawing.Size(24, 20);
            this.lbY.TabIndex = 21;
            this.lbY.Text = "Y:";
            // 
            // nmX
            // 
            this.nmX.DecimalPlaces = 2;
            this.nmX.Location = new System.Drawing.Point(36, 7);
            this.nmX.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmX.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nmX.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmX.Name = "nmX";
            this.nmX.Size = new System.Drawing.Size(120, 26);
            this.nmX.TabIndex = 19;
            this.nmX.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nmX.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // lbX
            // 
            this.lbX.AutoSize = true;
            this.lbX.Location = new System.Drawing.Point(4, 10);
            this.lbX.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbX.Name = "lbX";
            this.lbX.Size = new System.Drawing.Size(24, 20);
            this.lbX.TabIndex = 20;
            this.lbX.Text = "X:";
            // 
            // CtrlToolResize
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ButtonOkEnabled = false;
            this.Controls.Add(this.cbFade);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbConstrainXY);
            this.Controls.Add(this.nmY);
            this.Controls.Add(this.lbY);
            this.Controls.Add(this.nmX);
            this.Controls.Add(this.lbX);
            this.Description = "";
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "CtrlToolResize";
            this.Size = new System.Drawing.Size(720, 77);
            ((System.ComponentModel.ISupportInitialize)(this.nmY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmX)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox cbFade;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox cbConstrainXY;
        private System.Windows.Forms.NumericUpDown nmY;
        private System.Windows.Forms.Label lbY;
        private System.Windows.Forms.NumericUpDown nmX;
        private System.Windows.Forms.Label lbX;
    }
}
