namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolLayerImport
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
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.lbHeight = new System.Windows.Forms.Label();
            this.nmInsertAfterLayer = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nmInsertAfterLayer)).BeginInit();
            this.SuspendLayout();
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(212, 74);
            this.checkBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(153, 24);
            this.checkBox1.TabIndex = 33;
            this.checkBox1.Text = "Replace this layer";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // lbHeight
            // 
            this.lbHeight.AutoSize = true;
            this.lbHeight.Location = new System.Drawing.Point(376, 116);
            this.lbHeight.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lbHeight.Name = "lbHeight";
            this.lbHeight.Size = new System.Drawing.Size(85, 20);
            this.lbHeight.TabIndex = 32;
            this.lbHeight.Text = "(10.10mm)";
            // 
            // nmInsertAfterLayer
            // 
            this.nmInsertAfterLayer.Location = new System.Drawing.Point(230, 111);
            this.nmInsertAfterLayer.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
            this.nmInsertAfterLayer.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.nmInsertAfterLayer.Name = "nmInsertAfterLayer";
            this.nmInsertAfterLayer.Size = new System.Drawing.Size(135, 26);
            this.nmInsertAfterLayer.TabIndex = 30;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(26, 116);
            this.label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(128, 20);
            this.label1.TabIndex = 31;
            this.label1.Text = "Insert after layer:";
            // 
            // CtrlToolLayerImport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.lbHeight);
            this.Controls.Add(this.nmInsertAfterLayer);
            this.Controls.Add(this.label1);
            this.Description = "Import layer(s) from local files into a selected height.\r\nNOTE: Images must respe" +
    "ct file resolution and greyscale color.";
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "CtrlToolLayerImport";
            this.Size = new System.Drawing.Size(563, 263);
            ((System.ComponentModel.ISupportInitialize)(this.nmInsertAfterLayer)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label lbHeight;
        private System.Windows.Forms.NumericUpDown nmInsertAfterLayer;
        private System.Windows.Forms.Label label1;
    }
}
