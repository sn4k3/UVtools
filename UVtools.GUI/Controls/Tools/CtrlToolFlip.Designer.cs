namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolFlip
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
            this.lbX = new System.Windows.Forms.Label();
            this.cbMakeCopy = new System.Windows.Forms.CheckBox();
            this.cbFlipDirection = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // lbX
            // 
            this.lbX.AutoSize = true;
            this.lbX.Location = new System.Drawing.Point(4, 10);
            this.lbX.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbX.Name = "lbX";
            this.lbX.Size = new System.Drawing.Size(102, 20);
            this.lbX.TabIndex = 20;
            this.lbX.Text = "Flip direction:";
            // 
            // cbMakeCopy
            // 
            this.cbMakeCopy.AutoSize = true;
            this.cbMakeCopy.Location = new System.Drawing.Point(240, 8);
            this.cbMakeCopy.Name = "cbMakeCopy";
            this.cbMakeCopy.Size = new System.Drawing.Size(120, 24);
            this.cbMakeCopy.TabIndex = 22;
            this.cbMakeCopy.Text = "Blend Layers";
            this.toolTip.SetToolTip(this.cbMakeCopy, "If checked, rather than simply flipping the layer, a copy of each layer will be f" +
        "lipped and blended with the layer.");
            this.cbMakeCopy.UseVisualStyleBackColor = true;
            // 
            // cbFlipDirection
            // 
            this.cbFlipDirection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbFlipDirection.FormattingEnabled = true;
            this.cbFlipDirection.Location = new System.Drawing.Point(113, 6);
            this.cbFlipDirection.Name = "cbFlipDirection";
            this.cbFlipDirection.Size = new System.Drawing.Size(121, 28);
            this.cbFlipDirection.TabIndex = 21;
            // 
            // CtrlToolFlip
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.CanROI = true;
            this.Controls.Add(this.cbMakeCopy);
            this.Controls.Add(this.cbFlipDirection);
            this.Controls.Add(this.lbX);
            this.Description = "";
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "CtrlToolFlip";
            this.Size = new System.Drawing.Size(540, 37);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbX;
        private System.Windows.Forms.CheckBox cbMakeCopy;
        private System.Windows.Forms.ComboBox cbFlipDirection;
    }
}
