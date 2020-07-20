
namespace UVtools.GUI.Forms
{
    partial class FrmToolLayerReHeight
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmToolLayerReHeight));
            this.lbDescription = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.lbCurrent = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cbMultiplier = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // lbDescription
            // 
            this.lbDescription.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbDescription.Location = new System.Drawing.Point(13, 14);
            this.lbDescription.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbDescription.Name = "lbDescription";
            this.lbDescription.Size = new System.Drawing.Size(584, 95);
            this.lbDescription.TabIndex = 0;
            this.lbDescription.Text = resources.GetString("lbDescription.Text");
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Image = global::UVtools.GUI.Properties.Resources.Cancel_24x24;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(434, 324);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(150, 48);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.EventClick);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnOk.Image = global::UVtools.GUI.Properties.Resources.Ok_24x24;
            this.btnOk.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOk.Location = new System.Drawing.Point(276, 324);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(150, 48);
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "Re-Height";
            this.btnOk.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.EventClick);
            // 
            // lbCurrent
            // 
            this.lbCurrent.AutoSize = true;
            this.lbCurrent.Location = new System.Drawing.Point(13, 109);
            this.lbCurrent.Name = "lbCurrent";
            this.lbCurrent.Size = new System.Drawing.Size(234, 20);
            this.lbCurrent.TabIndex = 7;
            this.lbCurrent.Text = "Current layers: xxxxxxxxxxxxxxxxx";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 136);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 20);
            this.label2.TabIndex = 8;
            this.label2.Text = "Multiplier:";
            // 
            // cbMultiplier
            // 
            this.cbMultiplier.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbMultiplier.FormattingEnabled = true;
            this.cbMultiplier.Location = new System.Drawing.Point(126, 132);
            this.cbMultiplier.Name = "cbMultiplier";
            this.cbMultiplier.Size = new System.Drawing.Size(121, 28);
            this.cbMultiplier.TabIndex = 9;
            // 
            // FrmToolLayerReHeight
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(599, 386);
            this.Controls.Add(this.cbMultiplier);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lbCurrent);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lbDescription);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmToolLayerReHeight";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Layer Re-Height";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbDescription;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Label lbCurrent;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbMultiplier;
    }
}