namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolRotate
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
            this.nmDegrees = new System.Windows.Forms.NumericUpDown();
            this.lbX = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nmDegrees)).BeginInit();
            this.SuspendLayout();
            // 
            // nmDegrees
            // 
            this.nmDegrees.DecimalPlaces = 2;
            this.nmDegrees.Location = new System.Drawing.Point(129, 7);
            this.nmDegrees.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nmDegrees.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
            this.nmDegrees.Minimum = new decimal(new int[] {
            360,
            0,
            0,
            -2147483648});
            this.nmDegrees.Name = "nmDegrees";
            this.nmDegrees.Size = new System.Drawing.Size(101, 26);
            this.nmDegrees.TabIndex = 23;
            this.nmDegrees.Value = new decimal(new int[] {
            90,
            0,
            0,
            0});
            this.nmDegrees.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // lbX
            // 
            this.lbX.AutoSize = true;
            this.lbX.Location = new System.Drawing.Point(4, 9);
            this.lbX.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbX.Name = "lbX";
            this.lbX.Size = new System.Drawing.Size(117, 20);
            this.lbX.TabIndex = 22;
            this.lbX.Text = "Rotation angle:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(238, 10);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 20);
            this.label1.TabIndex = 24;
            this.label1.Text = "degrees";
            // 
            // CtrlToolRotate
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.nmDegrees);
            this.Controls.Add(this.lbX);
            this.Description = "";
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "CtrlToolRotate";
            this.Size = new System.Drawing.Size(540, 45);
            ((System.ComponentModel.ISupportInitialize)(this.nmDegrees)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown nmDegrees;
        private System.Windows.Forms.Label lbX;
        private System.Windows.Forms.Label label1;
    }
}
