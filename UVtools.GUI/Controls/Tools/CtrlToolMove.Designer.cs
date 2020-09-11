namespace UVtools.GUI.Controls.Tools
{
    partial class CtrlToolMove
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
            this.lbInsideBounds = new System.Windows.Forms.Label();
            this.lbPlacementY = new System.Windows.Forms.Label();
            this.lbPlacementX = new System.Windows.Forms.Label();
            this.lbVolumeHeight = new System.Windows.Forms.Label();
            this.lbVolumeWidth = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.tableAnchor = new System.Windows.Forms.TableLayoutPanel();
            this.rbAnchorBottomLeft = new System.Windows.Forms.RadioButton();
            this.rbAnchorBottomCenter = new System.Windows.Forms.RadioButton();
            this.rbAnchorBottomRight = new System.Windows.Forms.RadioButton();
            this.rbAnchorMiddleRight = new System.Windows.Forms.RadioButton();
            this.rbAnchorMiddleCenter = new System.Windows.Forms.RadioButton();
            this.rbAnchorMiddleLeft = new System.Windows.Forms.RadioButton();
            this.rbAnchorTopCenter = new System.Windows.Forms.RadioButton();
            this.rbAnchorTopLeft = new System.Windows.Forms.RadioButton();
            this.rbAnchorTopRight = new System.Windows.Forms.RadioButton();
            this.nmMarginTop = new System.Windows.Forms.NumericUpDown();
            this.nmMarginBottom = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.nmMarginRight = new System.Windows.Forms.NumericUpDown();
            this.nmMarginLeft = new System.Windows.Forms.NumericUpDown();
            this.groupBox1.SuspendLayout();
            this.tableAnchor.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginTop)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginBottom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginRight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginLeft)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.AutoSize = true;
            this.groupBox1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.groupBox1.Controls.Add(this.lbInsideBounds);
            this.groupBox1.Controls.Add(this.lbPlacementY);
            this.groupBox1.Controls.Add(this.lbPlacementX);
            this.groupBox1.Controls.Add(this.lbVolumeHeight);
            this.groupBox1.Controls.Add(this.lbVolumeWidth);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.tableAnchor);
            this.groupBox1.Controls.Add(this.nmMarginTop);
            this.groupBox1.Controls.Add(this.nmMarginBottom);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.nmMarginRight);
            this.groupBox1.Controls.Add(this.nmMarginLeft);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(570, 214);
            this.groupBox1.TabIndex = 24;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Location && Margins";
            // 
            // lbInsideBounds
            // 
            this.lbInsideBounds.AutoSize = true;
            this.lbInsideBounds.Location = new System.Drawing.Point(7, 172);
            this.lbInsideBounds.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbInsideBounds.Name = "lbInsideBounds";
            this.lbInsideBounds.Size = new System.Drawing.Size(147, 20);
            this.lbInsideBounds.TabIndex = 27;
            this.lbInsideBounds.Text = "Inside Bounds: Yes";
            // 
            // lbPlacementY
            // 
            this.lbPlacementY.AutoSize = true;
            this.lbPlacementY.Location = new System.Drawing.Point(7, 60);
            this.lbPlacementY.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbPlacementY.Name = "lbPlacementY";
            this.lbPlacementY.Size = new System.Drawing.Size(24, 20);
            this.lbPlacementY.TabIndex = 26;
            this.lbPlacementY.Text = "Y:";
            // 
            // lbPlacementX
            // 
            this.lbPlacementX.AutoSize = true;
            this.lbPlacementX.Location = new System.Drawing.Point(7, 31);
            this.lbPlacementX.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbPlacementX.Name = "lbPlacementX";
            this.lbPlacementX.Size = new System.Drawing.Size(24, 20);
            this.lbPlacementX.TabIndex = 25;
            this.lbPlacementX.Text = "X:";
            // 
            // lbVolumeHeight
            // 
            this.lbVolumeHeight.AutoSize = true;
            this.lbVolumeHeight.Location = new System.Drawing.Point(7, 134);
            this.lbVolumeHeight.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbVolumeHeight.Name = "lbVolumeHeight";
            this.lbVolumeHeight.Size = new System.Drawing.Size(60, 20);
            this.lbVolumeHeight.TabIndex = 24;
            this.lbVolumeHeight.Text = "Height:";
            // 
            // lbVolumeWidth
            // 
            this.lbVolumeWidth.AutoSize = true;
            this.lbVolumeWidth.Location = new System.Drawing.Point(7, 103);
            this.lbVolumeWidth.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbVolumeWidth.Name = "lbVolumeWidth";
            this.lbVolumeWidth.Size = new System.Drawing.Size(54, 20);
            this.lbVolumeWidth.TabIndex = 23;
            this.lbVolumeWidth.Text = "Width:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(223, 95);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(37, 20);
            this.label5.TabIndex = 22;
            this.label5.Text = "Left";
            // 
            // tableAnchor
            // 
            this.tableAnchor.AutoSize = true;
            this.tableAnchor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableAnchor.ColumnCount = 3;
            this.tableAnchor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableAnchor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableAnchor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableAnchor.Controls.Add(this.rbAnchorBottomLeft, 0, 2);
            this.tableAnchor.Controls.Add(this.rbAnchorBottomCenter, 0, 2);
            this.tableAnchor.Controls.Add(this.rbAnchorBottomRight, 0, 2);
            this.tableAnchor.Controls.Add(this.rbAnchorMiddleRight, 2, 1);
            this.tableAnchor.Controls.Add(this.rbAnchorMiddleCenter, 1, 1);
            this.tableAnchor.Controls.Add(this.rbAnchorMiddleLeft, 0, 1);
            this.tableAnchor.Controls.Add(this.rbAnchorTopCenter, 1, 0);
            this.tableAnchor.Controls.Add(this.rbAnchorTopLeft, 0, 0);
            this.tableAnchor.Controls.Add(this.rbAnchorTopRight, 2, 0);
            this.tableAnchor.Location = new System.Drawing.Point(358, 76);
            this.tableAnchor.Name = "tableAnchor";
            this.tableAnchor.RowCount = 3;
            this.tableAnchor.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableAnchor.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableAnchor.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableAnchor.Size = new System.Drawing.Size(60, 57);
            this.tableAnchor.TabIndex = 13;
            // 
            // rbAnchorBottomLeft
            // 
            this.rbAnchorBottomLeft.AutoSize = true;
            this.rbAnchorBottomLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorBottomLeft.Location = new System.Drawing.Point(3, 41);
            this.rbAnchorBottomLeft.Name = "rbAnchorBottomLeft";
            this.rbAnchorBottomLeft.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorBottomLeft.TabIndex = 8;
            this.rbAnchorBottomLeft.UseVisualStyleBackColor = true;
            this.rbAnchorBottomLeft.CheckedChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // rbAnchorBottomCenter
            // 
            this.rbAnchorBottomCenter.AutoSize = true;
            this.rbAnchorBottomCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorBottomCenter.Location = new System.Drawing.Point(23, 41);
            this.rbAnchorBottomCenter.Name = "rbAnchorBottomCenter";
            this.rbAnchorBottomCenter.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorBottomCenter.TabIndex = 7;
            this.rbAnchorBottomCenter.UseVisualStyleBackColor = true;
            this.rbAnchorBottomCenter.CheckedChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // rbAnchorBottomRight
            // 
            this.rbAnchorBottomRight.AutoSize = true;
            this.rbAnchorBottomRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorBottomRight.Location = new System.Drawing.Point(43, 41);
            this.rbAnchorBottomRight.Name = "rbAnchorBottomRight";
            this.rbAnchorBottomRight.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorBottomRight.TabIndex = 6;
            this.rbAnchorBottomRight.UseVisualStyleBackColor = true;
            this.rbAnchorBottomRight.CheckedChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // rbAnchorMiddleRight
            // 
            this.rbAnchorMiddleRight.AutoSize = true;
            this.rbAnchorMiddleRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorMiddleRight.Location = new System.Drawing.Point(43, 22);
            this.rbAnchorMiddleRight.Name = "rbAnchorMiddleRight";
            this.rbAnchorMiddleRight.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorMiddleRight.TabIndex = 5;
            this.rbAnchorMiddleRight.UseVisualStyleBackColor = true;
            this.rbAnchorMiddleRight.CheckedChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // rbAnchorMiddleCenter
            // 
            this.rbAnchorMiddleCenter.AutoSize = true;
            this.rbAnchorMiddleCenter.Checked = true;
            this.rbAnchorMiddleCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorMiddleCenter.Location = new System.Drawing.Point(23, 22);
            this.rbAnchorMiddleCenter.Name = "rbAnchorMiddleCenter";
            this.rbAnchorMiddleCenter.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorMiddleCenter.TabIndex = 4;
            this.rbAnchorMiddleCenter.TabStop = true;
            this.rbAnchorMiddleCenter.UseVisualStyleBackColor = true;
            this.rbAnchorMiddleCenter.CheckedChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // rbAnchorMiddleLeft
            // 
            this.rbAnchorMiddleLeft.AutoSize = true;
            this.rbAnchorMiddleLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAnchorMiddleLeft.Location = new System.Drawing.Point(3, 22);
            this.rbAnchorMiddleLeft.Name = "rbAnchorMiddleLeft";
            this.rbAnchorMiddleLeft.Size = new System.Drawing.Size(14, 13);
            this.rbAnchorMiddleLeft.TabIndex = 3;
            this.rbAnchorMiddleLeft.UseVisualStyleBackColor = true;
            this.rbAnchorMiddleLeft.CheckedChanged += new System.EventHandler(this.EventValueChanged);
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
            this.rbAnchorTopCenter.CheckedChanged += new System.EventHandler(this.EventValueChanged);
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
            this.rbAnchorTopLeft.CheckedChanged += new System.EventHandler(this.EventValueChanged);
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
            this.rbAnchorTopRight.CheckedChanged += new System.EventHandler(this.EventValueChanged);
            // 
            // nmMarginTop
            // 
            this.nmMarginTop.Location = new System.Drawing.Point(346, 44);
            this.nmMarginTop.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmMarginTop.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.nmMarginTop.Name = "nmMarginTop";
            this.nmMarginTop.Size = new System.Drawing.Size(85, 26);
            this.nmMarginTop.TabIndex = 15;
            this.nmMarginTop.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip.SetToolTip(this.nmMarginTop, "Top margin in pixels");
            this.nmMarginTop.ValueChanged += new System.EventHandler(this.EventValueChanged);
            this.nmMarginTop.KeyUp += new System.Windows.Forms.KeyEventHandler(this.EventValueChanged);
            // 
            // nmMarginBottom
            // 
            this.nmMarginBottom.Location = new System.Drawing.Point(346, 139);
            this.nmMarginBottom.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmMarginBottom.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.nmMarginBottom.Name = "nmMarginBottom";
            this.nmMarginBottom.Size = new System.Drawing.Size(85, 26);
            this.nmMarginBottom.TabIndex = 17;
            this.nmMarginBottom.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip.SetToolTip(this.nmMarginBottom, "Bottom margin in pixels");
            this.nmMarginBottom.ValueChanged += new System.EventHandler(this.EventValueChanged);
            this.nmMarginBottom.KeyUp += new System.Windows.Forms.KeyEventHandler(this.EventValueChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(355, 168);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 20);
            this.label3.TabIndex = 18;
            this.label3.Text = "Bottom";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(366, 21);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(36, 20);
            this.label2.TabIndex = 16;
            this.label2.Text = "Top";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(516, 94);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(47, 20);
            this.label4.TabIndex = 21;
            this.label4.Text = "Right";
            // 
            // nmMarginRight
            // 
            this.nmMarginRight.Location = new System.Drawing.Point(424, 92);
            this.nmMarginRight.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmMarginRight.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.nmMarginRight.Name = "nmMarginRight";
            this.nmMarginRight.Size = new System.Drawing.Size(85, 26);
            this.nmMarginRight.TabIndex = 19;
            this.nmMarginRight.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.toolTip.SetToolTip(this.nmMarginRight, "Right margin in pixels");
            this.nmMarginRight.ValueChanged += new System.EventHandler(this.EventValueChanged);
            this.nmMarginRight.KeyUp += new System.Windows.Forms.KeyEventHandler(this.EventValueChanged);
            // 
            // nmMarginLeft
            // 
            this.nmMarginLeft.Location = new System.Drawing.Point(267, 92);
            this.nmMarginLeft.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nmMarginLeft.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.nmMarginLeft.Name = "nmMarginLeft";
            this.nmMarginLeft.Size = new System.Drawing.Size(85, 26);
            this.nmMarginLeft.TabIndex = 20;
            this.toolTip.SetToolTip(this.nmMarginLeft, "Left Margin in pixels");
            this.nmMarginLeft.UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left;
            this.nmMarginLeft.ValueChanged += new System.EventHandler(this.EventValueChanged);
            this.nmMarginLeft.KeyUp += new System.Windows.Forms.KeyEventHandler(this.EventValueChanged);
            // 
            // CtrlToolMove
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.groupBox1);
            this.Description = "";
            this.ExtraButtonVisible = true;
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "CtrlToolMove";
            this.Size = new System.Drawing.Size(570, 214);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tableAnchor.ResumeLayout(false);
            this.tableAnchor.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginTop)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginRight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmMarginLeft)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lbInsideBounds;
        private System.Windows.Forms.Label lbPlacementY;
        private System.Windows.Forms.Label lbPlacementX;
        private System.Windows.Forms.Label lbVolumeHeight;
        private System.Windows.Forms.Label lbVolumeWidth;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TableLayoutPanel tableAnchor;
        private System.Windows.Forms.RadioButton rbAnchorBottomLeft;
        private System.Windows.Forms.RadioButton rbAnchorBottomCenter;
        private System.Windows.Forms.RadioButton rbAnchorBottomRight;
        private System.Windows.Forms.RadioButton rbAnchorMiddleRight;
        private System.Windows.Forms.RadioButton rbAnchorMiddleCenter;
        private System.Windows.Forms.RadioButton rbAnchorMiddleLeft;
        private System.Windows.Forms.RadioButton rbAnchorTopCenter;
        private System.Windows.Forms.RadioButton rbAnchorTopLeft;
        private System.Windows.Forms.RadioButton rbAnchorTopRight;
        private System.Windows.Forms.NumericUpDown nmMarginTop;
        private System.Windows.Forms.NumericUpDown nmMarginBottom;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown nmMarginRight;
        private System.Windows.Forms.NumericUpDown nmMarginLeft;
    }
}
