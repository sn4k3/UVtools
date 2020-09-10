namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolPattern
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableAnchor = new System.Windows.Forms.TableLayoutPanel();
            this.rbAnchorNone = new System.Windows.Forms.RadioButton();
            this.rbAnchorBottomLeft = new System.Windows.Forms.RadioButton();
            this.rbAnchorBottomCenter = new System.Windows.Forms.RadioButton();
            this.rbAnchorBottomRight = new System.Windows.Forms.RadioButton();
            this.rbAnchorMiddleRight = new System.Windows.Forms.RadioButton();
            this.rbAnchorMiddleCenter = new System.Windows.Forms.RadioButton();
            this.rbAnchorMiddleLeft = new System.Windows.Forms.RadioButton();
            this.rbAnchorTopCenter = new System.Windows.Forms.RadioButton();
            this.rbAnchorTopLeft = new System.Windows.Forms.RadioButton();
            this.rbAnchorTopRight = new System.Windows.Forms.RadioButton();
            this.btnAutoMarginRow = new System.Windows.Forms.Button();
            this.btnAutoMarginCol = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.nmMarginCol = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.nmMarginRow = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.nmRows = new System.Windows.Forms.NumericUpDown();
            this.lbInsideBounds = new System.Windows.Forms.Label();
            this.lbRows = new System.Windows.Forms.Label();
            this.lbCols = new System.Windows.Forms.Label();
            this.lbVolumeHeight = new System.Windows.Forms.Label();
            this.lbVolumeWidth = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.nmCols = new System.Windows.Forms.NumericUpDown();
            this.groupBox1.SuspendLayout();
            this.tableAnchor.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginCol)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginRow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmRows)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmCols)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tableAnchor);
            this.groupBox1.Location = new System.Drawing.Point(432, 81);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(143, 105);
            this.groupBox1.TabIndex = 55;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Anchor";
            // 
            // tableAnchor
            // 
            this.tableAnchor.AutoSize = true;
            this.tableAnchor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableAnchor.ColumnCount = 4;
            this.tableAnchor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableAnchor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableAnchor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableAnchor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableAnchor.Controls.Add(this.rbAnchorNone, 3, 1);
            this.tableAnchor.Controls.Add(this.rbAnchorBottomLeft, 0, 2);
            this.tableAnchor.Controls.Add(this.rbAnchorBottomCenter, 0, 2);
            this.tableAnchor.Controls.Add(this.rbAnchorBottomRight, 0, 2);
            this.tableAnchor.Controls.Add(this.rbAnchorMiddleRight, 2, 1);
            this.tableAnchor.Controls.Add(this.rbAnchorMiddleCenter, 1, 1);
            this.tableAnchor.Controls.Add(this.rbAnchorMiddleLeft, 0, 1);
            this.tableAnchor.Controls.Add(this.rbAnchorTopCenter, 1, 0);
            this.tableAnchor.Controls.Add(this.rbAnchorTopLeft, 0, 0);
            this.tableAnchor.Controls.Add(this.rbAnchorTopRight, 2, 0);
            this.tableAnchor.Location = new System.Drawing.Point(6, 25);
            this.tableAnchor.Name = "tableAnchor";
            this.tableAnchor.RowCount = 3;
            this.tableAnchor.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableAnchor.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableAnchor.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableAnchor.Size = new System.Drawing.Size(131, 68);
            this.tableAnchor.TabIndex = 38;
            // 
            // rbAnchorNone
            // 
            this.rbAnchorNone.AutoSize = true;
            this.rbAnchorNone.Checked = true;
            this.rbAnchorNone.Location = new System.Drawing.Point(63, 22);
            this.rbAnchorNone.Name = "rbAnchorNone";
            this.rbAnchorNone.Size = new System.Drawing.Size(65, 24);
            this.rbAnchorNone.TabIndex = 39;
            this.rbAnchorNone.TabStop = true;
            this.rbAnchorNone.Text = "None";
            this.rbAnchorNone.UseVisualStyleBackColor = true;
            // 
            // rbAnchorBottomLeft
            // 
            this.rbAnchorBottomLeft.AutoSize = true;
            this.rbAnchorBottomLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorBottomLeft.Location = new System.Drawing.Point(23, 52);
            this.rbAnchorBottomLeft.Name = "rbAnchorBottomLeft";
            this.rbAnchorBottomLeft.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorBottomLeft.TabIndex = 8;
            this.rbAnchorBottomLeft.UseVisualStyleBackColor = true;
            // 
            // rbAnchorBottomCenter
            // 
            this.rbAnchorBottomCenter.AutoSize = true;
            this.rbAnchorBottomCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorBottomCenter.Location = new System.Drawing.Point(3, 52);
            this.rbAnchorBottomCenter.Name = "rbAnchorBottomCenter";
            this.rbAnchorBottomCenter.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorBottomCenter.TabIndex = 7;
            this.rbAnchorBottomCenter.UseVisualStyleBackColor = true;
            // 
            // rbAnchorBottomRight
            // 
            this.rbAnchorBottomRight.AutoSize = true;
            this.rbAnchorBottomRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorBottomRight.Location = new System.Drawing.Point(43, 52);
            this.rbAnchorBottomRight.Name = "rbAnchorBottomRight";
            this.rbAnchorBottomRight.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorBottomRight.TabIndex = 6;
            this.rbAnchorBottomRight.UseVisualStyleBackColor = true;
            // 
            // rbAnchorMiddleRight
            // 
            this.rbAnchorMiddleRight.AutoSize = true;
            this.rbAnchorMiddleRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorMiddleRight.Location = new System.Drawing.Point(43, 22);
            this.rbAnchorMiddleRight.Name = "rbAnchorMiddleRight";
            this.rbAnchorMiddleRight.Size = new System.Drawing.Size(14, 24);
            this.rbAnchorMiddleRight.TabIndex = 5;
            this.rbAnchorMiddleRight.UseVisualStyleBackColor = true;
            // 
            // rbAnchorMiddleCenter
            // 
            this.rbAnchorMiddleCenter.AutoSize = true;
            this.rbAnchorMiddleCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorMiddleCenter.Location = new System.Drawing.Point(23, 22);
            this.rbAnchorMiddleCenter.Name = "rbAnchorMiddleCenter";
            this.rbAnchorMiddleCenter.Size = new System.Drawing.Size(14, 24);
            this.rbAnchorMiddleCenter.TabIndex = 4;
            this.rbAnchorMiddleCenter.UseVisualStyleBackColor = true;
            // 
            // rbAnchorMiddleLeft
            // 
            this.rbAnchorMiddleLeft.AutoSize = true;
            this.rbAnchorMiddleLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorMiddleLeft.Location = new System.Drawing.Point(3, 22);
            this.rbAnchorMiddleLeft.Name = "rbAnchorMiddleLeft";
            this.rbAnchorMiddleLeft.Size = new System.Drawing.Size(14, 24);
            this.rbAnchorMiddleLeft.TabIndex = 3;
            this.rbAnchorMiddleLeft.UseVisualStyleBackColor = true;
            // 
            // rbAnchorTopCenter
            // 
            this.rbAnchorTopCenter.AutoSize = true;
            this.rbAnchorTopCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorTopCenter.Location = new System.Drawing.Point(23, 3);
            this.rbAnchorTopCenter.Name = "rbAnchorTopCenter";
            this.rbAnchorTopCenter.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorTopCenter.TabIndex = 2;
            this.rbAnchorTopCenter.UseVisualStyleBackColor = true;
            // 
            // rbAnchorTopLeft
            // 
            this.rbAnchorTopLeft.AutoSize = true;
            this.rbAnchorTopLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorTopLeft.Location = new System.Drawing.Point(3, 3);
            this.rbAnchorTopLeft.Name = "rbAnchorTopLeft";
            this.rbAnchorTopLeft.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorTopLeft.TabIndex = 1;
            this.rbAnchorTopLeft.UseVisualStyleBackColor = true;
            // 
            // rbAnchorTopRight
            // 
            this.rbAnchorTopRight.AutoSize = true;
            this.rbAnchorTopRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorTopRight.Location = new System.Drawing.Point(43, 3);
            this.rbAnchorTopRight.Name = "rbAnchorTopRight";
            this.rbAnchorTopRight.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorTopRight.TabIndex = 0;
            this.rbAnchorTopRight.UseVisualStyleBackColor = true;
            // 
            // btnAutoMarginRow
            // 
            this.btnAutoMarginRow.Image = global::UVtools.GUI.Properties.Resources.resize_16x16;
            this.btnAutoMarginRow.Location = new System.Drawing.Point(392, 42);
            this.btnAutoMarginRow.Name = "btnAutoMarginRow";
            this.btnAutoMarginRow.Size = new System.Drawing.Size(33, 26);
            this.btnAutoMarginRow.TabIndex = 54;
            this.btnAutoMarginRow.Click += new System.EventHandler(this.EventClick);
            // 
            // btnAutoMarginCol
            // 
            this.btnAutoMarginCol.Image = global::UVtools.GUI.Properties.Resources.resize_16x16;
            this.btnAutoMarginCol.Location = new System.Drawing.Point(392, 10);
            this.btnAutoMarginCol.Name = "btnAutoMarginCol";
            this.btnAutoMarginCol.Size = new System.Drawing.Size(33, 26);
            this.btnAutoMarginCol.TabIndex = 53;
            this.btnAutoMarginCol.Click += new System.EventHandler(this.EventClick);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(237, 13);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 20);
            this.label3.TabIndex = 52;
            this.label3.Text = "Margin:";
            // 
            // nmMarginCol
            // 
            this.nmMarginCol.Location = new System.Drawing.Point(305, 10);
            this.nmMarginCol.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmMarginCol.Name = "nmMarginCol";
            this.nmMarginCol.Size = new System.Drawing.Size(81, 26);
            this.nmMarginCol.TabIndex = 51;
            this.nmMarginCol.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.nmMarginCol.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(237, 45);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(61, 20);
            this.label2.TabIndex = 50;
            this.label2.Text = "Margin:";
            // 
            // nmMarginRow
            // 
            this.nmMarginRow.Location = new System.Drawing.Point(305, 42);
            this.nmMarginRow.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmMarginRow.Name = "nmMarginRow";
            this.nmMarginRow.Size = new System.Drawing.Size(81, 26);
            this.nmMarginRow.TabIndex = 49;
            this.nmMarginRow.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.nmMarginRow.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(49, 44);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 20);
            this.label1.TabIndex = 48;
            this.label1.Text = "Rows:";
            // 
            // nmRows
            // 
            this.nmRows.Location = new System.Drawing.Point(109, 42);
            this.nmRows.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmRows.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmRows.Name = "nmRows";
            this.nmRows.Size = new System.Drawing.Size(121, 26);
            this.nmRows.TabIndex = 47;
            this.nmRows.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmRows.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // lbInsideBounds
            // 
            this.lbInsideBounds.AutoSize = true;
            this.lbInsideBounds.Location = new System.Drawing.Point(7, 139);
            this.lbInsideBounds.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbInsideBounds.Name = "lbInsideBounds";
            this.lbInsideBounds.Size = new System.Drawing.Size(147, 20);
            this.lbInsideBounds.TabIndex = 46;
            this.lbInsideBounds.Text = "Inside Bounds: Yes";
            // 
            // lbRows
            // 
            this.lbRows.AutoSize = true;
            this.lbRows.Location = new System.Drawing.Point(433, 44);
            this.lbRows.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbRows.Name = "lbRows";
            this.lbRows.Size = new System.Drawing.Size(53, 20);
            this.lbRows.TabIndex = 45;
            this.lbRows.Text = "Rows:";
            // 
            // lbCols
            // 
            this.lbCols.AutoSize = true;
            this.lbCols.Location = new System.Drawing.Point(433, 13);
            this.lbCols.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbCols.Name = "lbCols";
            this.lbCols.Size = new System.Drawing.Size(79, 20);
            this.lbCols.TabIndex = 44;
            this.lbCols.Text = "Columns: ";
            // 
            // lbVolumeHeight
            // 
            this.lbVolumeHeight.AutoSize = true;
            this.lbVolumeHeight.Location = new System.Drawing.Point(4, 110);
            this.lbVolumeHeight.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbVolumeHeight.Name = "lbVolumeHeight";
            this.lbVolumeHeight.Size = new System.Drawing.Size(174, 20);
            this.lbVolumeHeight.TabIndex = 43;
            this.lbVolumeHeight.Text = "Volume/Pattern Height:";
            // 
            // lbVolumeWidth
            // 
            this.lbVolumeWidth.AutoSize = true;
            this.lbVolumeWidth.Location = new System.Drawing.Point(4, 81);
            this.lbVolumeWidth.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbVolumeWidth.Name = "lbVolumeWidth";
            this.lbVolumeWidth.Size = new System.Drawing.Size(168, 20);
            this.lbVolumeWidth.TabIndex = 42;
            this.lbVolumeWidth.Text = "Volume/Pattern Width:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(27, 12);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(75, 20);
            this.label4.TabIndex = 41;
            this.label4.Text = "Columns:";
            // 
            // nmCols
            // 
            this.nmCols.Location = new System.Drawing.Point(109, 10);
            this.nmCols.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmCols.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmCols.Name = "nmCols";
            this.nmCols.Size = new System.Drawing.Size(121, 26);
            this.nmCols.TabIndex = 40;
            this.nmCols.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmCols.ValueChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // CtrlToolPattern
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ButtonOkEnabled = false;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnAutoMarginRow);
            this.Controls.Add(this.btnAutoMarginCol);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.nmMarginCol);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.nmMarginRow);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.nmRows);
            this.Controls.Add(this.lbInsideBounds);
            this.Controls.Add(this.lbRows);
            this.Controls.Add(this.lbCols);
            this.Controls.Add(this.lbVolumeHeight);
            this.Controls.Add(this.lbVolumeWidth);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.nmCols);
            this.Description = "";
            this.ExtraButtonVisible = true;
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "CtrlToolPattern";
            this.Size = new System.Drawing.Size(590, 198);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tableAnchor.ResumeLayout(false);
            this.tableAnchor.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginCol)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginRow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmRows)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmCols)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TableLayoutPanel tableAnchor;
        private System.Windows.Forms.RadioButton rbAnchorNone;
        private System.Windows.Forms.RadioButton rbAnchorBottomLeft;
        private System.Windows.Forms.RadioButton rbAnchorBottomCenter;
        private System.Windows.Forms.RadioButton rbAnchorBottomRight;
        private System.Windows.Forms.RadioButton rbAnchorMiddleRight;
        private System.Windows.Forms.RadioButton rbAnchorMiddleCenter;
        private System.Windows.Forms.RadioButton rbAnchorMiddleLeft;
        private System.Windows.Forms.RadioButton rbAnchorTopCenter;
        private System.Windows.Forms.RadioButton rbAnchorTopLeft;
        private System.Windows.Forms.RadioButton rbAnchorTopRight;
        private System.Windows.Forms.Button btnAutoMarginRow;
        private System.Windows.Forms.Button btnAutoMarginCol;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nmMarginCol;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nmMarginRow;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nmRows;
        private System.Windows.Forms.Label lbInsideBounds;
        private System.Windows.Forms.Label lbRows;
        private System.Windows.Forms.Label lbCols;
        private System.Windows.Forms.Label lbVolumeHeight;
        private System.Windows.Forms.Label lbVolumeWidth;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown nmCols;
    }
}
