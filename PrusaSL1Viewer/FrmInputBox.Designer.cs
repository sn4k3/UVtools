namespace PrusaSL1Viewer
{
    partial class FrmInputBox
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
            this.lbDescription = new System.Windows.Forms.Label();
            this.lbCurrentValue = new System.Windows.Forms.Label();
            this.tbCurrentValue = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.numNewValue = new System.Windows.Forms.NumericUpDown();
            this.btnModify = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numNewValue)).BeginInit();
            this.SuspendLayout();
            // 
            // lbDescription
            // 
            this.lbDescription.AutoSize = true;
            this.lbDescription.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbDescription.Location = new System.Drawing.Point(12, 9);
            this.lbDescription.Name = "lbDescription";
            this.lbDescription.Size = new System.Drawing.Size(89, 20);
            this.lbDescription.TabIndex = 0;
            this.lbDescription.Text = "Description";
            // 
            // lbCurrentValue
            // 
            this.lbCurrentValue.AutoSize = true;
            this.lbCurrentValue.Location = new System.Drawing.Point(13, 47);
            this.lbCurrentValue.Name = "lbCurrentValue";
            this.lbCurrentValue.Size = new System.Drawing.Size(74, 13);
            this.lbCurrentValue.TabIndex = 1;
            this.lbCurrentValue.Text = "Current Value:";
            // 
            // tbCurrentValue
            // 
            this.tbCurrentValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbCurrentValue.CausesValidation = false;
            this.tbCurrentValue.Location = new System.Drawing.Point(93, 44);
            this.tbCurrentValue.Name = "tbCurrentValue";
            this.tbCurrentValue.ReadOnly = true;
            this.tbCurrentValue.Size = new System.Drawing.Size(261, 20);
            this.tbCurrentValue.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 72);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(62, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "New Value:";
            // 
            // numNewValue
            // 
            this.numNewValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.numNewValue.Location = new System.Drawing.Point(93, 70);
            this.numNewValue.Name = "numNewValue";
            this.numNewValue.Size = new System.Drawing.Size(261, 20);
            this.numNewValue.TabIndex = 4;
            this.numNewValue.ValueChanged += new System.EventHandler(this.ValueChanged);
            // 
            // btnModify
            // 
            this.btnModify.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnModify.Enabled = false;
            this.btnModify.Image = global::PrusaSL1Viewer.Properties.Resources.Ok_24x24;
            this.btnModify.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnModify.Location = new System.Drawing.Point(148, 96);
            this.btnModify.Name = "btnModify";
            this.btnModify.Size = new System.Drawing.Size(100, 40);
            this.btnModify.TabIndex = 6;
            this.btnModify.Text = "&Modify";
            this.btnModify.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnModify.UseVisualStyleBackColor = true;
            this.btnModify.Click += new System.EventHandler(this.ItemClicked);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Image = global::PrusaSL1Viewer.Properties.Resources.Cancel_24x24;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(254, 96);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 40);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.ItemClicked);
            // 
            // FrmInputBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(366, 146);
            this.Controls.Add(this.btnModify);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.numNewValue);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbCurrentValue);
            this.Controls.Add(this.lbCurrentValue);
            this.Controls.Add(this.lbDescription);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmInputBox";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Form1";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.numNewValue)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbDescription;
        private System.Windows.Forms.Label lbCurrentValue;
        private System.Windows.Forms.TextBox tbCurrentValue;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numNewValue;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnModify;
    }
}