namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolEditParameters
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
            this.table = new System.Windows.Forms.TableLayoutPanel();
            this.lbValue = new System.Windows.Forms.Label();
            this.lbProperty = new System.Windows.Forms.Label();
            this.lbUnit = new System.Windows.Forms.Label();
            this.lbOldValue = new System.Windows.Forms.Label();
            this.lbAction = new System.Windows.Forms.Label();
            this.table.SuspendLayout();
            this.SuspendLayout();
            // 
            // table
            // 
            this.table.AutoSize = true;
            this.table.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.table.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.InsetDouble;
            this.table.ColumnCount = 5;
            this.table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.table.Controls.Add(this.lbAction, 4, 0);
            this.table.Controls.Add(this.lbOldValue, 1, 0);
            this.table.Controls.Add(this.lbUnit, 3, 0);
            this.table.Controls.Add(this.lbValue, 2, 0);
            this.table.Controls.Add(this.lbProperty, 0, 0);
            this.table.Dock = System.Windows.Forms.DockStyle.Fill;
            this.table.Location = new System.Drawing.Point(0, 0);
            this.table.Name = "table";
            this.table.RowCount = 1;
            this.table.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.table.Size = new System.Drawing.Size(540, 26);
            this.table.TabIndex = 0;
            // 
            // lbValue
            // 
            this.lbValue.AutoSize = true;
            this.lbValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbValue.Location = new System.Drawing.Point(183, 3);
            this.lbValue.Name = "lbValue";
            this.lbValue.Size = new System.Drawing.Size(55, 20);
            this.lbValue.TabIndex = 0;
            this.lbValue.Text = "Value";
            this.lbValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lbProperty
            // 
            this.lbProperty.AutoSize = true;
            this.lbProperty.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbProperty.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbProperty.Location = new System.Drawing.Point(6, 3);
            this.lbProperty.Name = "lbProperty";
            this.lbProperty.Size = new System.Drawing.Size(76, 20);
            this.lbProperty.TabIndex = 1;
            this.lbProperty.Text = "Property";
            this.lbProperty.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lbUnit
            // 
            this.lbUnit.AutoSize = true;
            this.lbUnit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbUnit.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbUnit.Location = new System.Drawing.Point(247, 3);
            this.lbUnit.Name = "lbUnit";
            this.lbUnit.Size = new System.Drawing.Size(42, 20);
            this.lbUnit.TabIndex = 3;
            this.lbUnit.Text = "Unit";
            this.lbUnit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lbOldValue
            // 
            this.lbOldValue.AutoSize = true;
            this.lbOldValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbOldValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbOldValue.Location = new System.Drawing.Point(91, 3);
            this.lbOldValue.Name = "lbOldValue";
            this.lbOldValue.Size = new System.Drawing.Size(83, 20);
            this.lbOldValue.TabIndex = 4;
            this.lbOldValue.Text = "Old value";
            this.lbOldValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lbAction
            // 
            this.lbAction.AutoSize = true;
            this.lbAction.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbAction.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbAction.Location = new System.Drawing.Point(298, 3);
            this.lbAction.Name = "lbAction";
            this.lbAction.Size = new System.Drawing.Size(236, 20);
            this.lbAction.TabIndex = 5;
            this.lbAction.Text = "Reset";
            this.lbAction.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // CtrlToolEditParameters
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.table);
            this.Description = "";
            this.ExtraButtonVisible = true;
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LayerRangeVisible = false;
            this.Name = "CtrlToolEditParameters";
            this.Size = new System.Drawing.Size(540, 26);
            this.table.ResumeLayout(false);
            this.table.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel table;
        private System.Windows.Forms.Label lbValue;
        private System.Windows.Forms.Label lbProperty;
        private System.Windows.Forms.Label lbOldValue;
        private System.Windows.Forms.Label lbUnit;
        private System.Windows.Forms.Label lbAction;
    }
}
