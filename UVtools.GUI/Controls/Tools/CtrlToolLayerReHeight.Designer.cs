namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolLayerReHeight
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
            this.cbMultiplier = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.lbCurrent = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cbMultiplier
            // 
            this.cbMultiplier.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbMultiplier.FormattingEnabled = true;
            this.cbMultiplier.Location = new System.Drawing.Point(78, 37);
            this.cbMultiplier.Name = "cbMultiplier";
            this.cbMultiplier.Size = new System.Drawing.Size(459, 28);
            this.cbMultiplier.TabIndex = 12;
            this.cbMultiplier.SelectedIndexChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 20);
            this.label2.TabIndex = 11;
            this.label2.Text = "Modifier:";
            // 
            // lbCurrent
            // 
            this.lbCurrent.AutoSize = true;
            this.lbCurrent.Location = new System.Drawing.Point(3, 9);
            this.lbCurrent.Name = "lbCurrent";
            this.lbCurrent.Size = new System.Drawing.Size(284, 20);
            this.lbCurrent.TabIndex = 10;
            this.lbCurrent.Text = "Current layers: (No valid configurations)";
            // 
            // CtrlToolLayerReHeight
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ButtonOkEnabled = false;
            this.Controls.Add(this.cbMultiplier);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lbCurrent);
            this.Description = "";
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LayerRangeVisible = false;
            this.Name = "CtrlToolLayerReHeight";
            this.Size = new System.Drawing.Size(540, 68);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cbMultiplier;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lbCurrent;
    }
}
