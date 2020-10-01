namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolArithmetic
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
            this.label1 = new System.Windows.Forms.Label();
            this.tbSentence = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Sentence:";
            // 
            // tbSentence
            // 
            this.tbSentence.Dock = System.Windows.Forms.DockStyle.Top;
            this.tbSentence.Location = new System.Drawing.Point(0, 20);
            this.tbSentence.Multiline = true;
            this.tbSentence.Name = "tbSentence";
            this.tbSentence.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbSentence.Size = new System.Drawing.Size(668, 239);
            this.tbSentence.TabIndex = 1;
            // 
            // CtrlToolArithmetic
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CanROI = true;
            this.Controls.Add(this.tbSentence);
            this.Controls.Add(this.label1);
            this.Description = "";
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LayerRangeVisible = false;
            this.Name = "CtrlToolArithmetic";
            this.Size = new System.Drawing.Size(668, 273);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbSentence;
    }
}
