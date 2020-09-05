namespace UVtools.GUI.Controls
{
    partial class CtrlDescriptionPanel
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
            this.lbDescription = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lbDescription
            // 
            this.lbDescription.AutoSize = true;
            this.lbDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbDescription.Location = new System.Drawing.Point(0, 0);
            this.lbDescription.Name = "lbDescription";
            this.lbDescription.Padding = new System.Windows.Forms.Padding(20);
            this.lbDescription.Size = new System.Drawing.Size(129, 60);
            this.lbDescription.TabIndex = 0;
            this.lbDescription.Text = "Description";
            // 
            // CtrlDescriptionPanel
            // 
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.lbDescription);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "CtrlDescriptionPanel";
            this.Size = new System.Drawing.Size(129, 60);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbDescription;
    }
}
