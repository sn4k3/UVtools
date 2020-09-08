namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolLayerClone
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
            this.lbHeights = new System.Windows.Forms.Label();
            this.lbLayersCount = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.nmClones = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.nmClones)).BeginInit();
            this.SuspendLayout();
            // 
            // lbHeights
            // 
            this.lbHeights.AutoSize = true;
            this.lbHeights.Location = new System.Drawing.Point(4, 77);
            this.lbHeights.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbHeights.Name = "lbHeights";
            this.lbHeights.Size = new System.Drawing.Size(56, 20);
            this.lbHeights.TabIndex = 27;
            this.lbHeights.Text = "Height";
            // 
            // lbLayersCount
            // 
            this.lbLayersCount.AutoSize = true;
            this.lbLayersCount.Location = new System.Drawing.Point(4, 49);
            this.lbLayersCount.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbLayersCount.Name = "lbLayersCount";
            this.lbLayersCount.Size = new System.Drawing.Size(56, 20);
            this.lbLayersCount.TabIndex = 26;
            this.lbLayersCount.Text = "Layers";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(4, 19);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(62, 20);
            this.label4.TabIndex = 25;
            this.label4.Text = "Clones:";
            // 
            // nmClones
            // 
            this.nmClones.Location = new System.Drawing.Point(74, 16);
            this.nmClones.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmClones.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmClones.Name = "nmClones";
            this.nmClones.Size = new System.Drawing.Size(71, 26);
            this.nmClones.TabIndex = 24;
            this.nmClones.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmClones.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // CtrlToolLayerClone
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.lbHeights);
            this.Controls.Add(this.lbLayersCount);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.nmClones);
            this.Description = "";
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "CtrlToolLayerClone";
            this.Size = new System.Drawing.Size(540, 110);
            ((System.ComponentModel.ISupportInitialize)(this.nmClones)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbHeights;
        private System.Windows.Forms.Label lbLayersCount;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown nmClones;
    }
}
