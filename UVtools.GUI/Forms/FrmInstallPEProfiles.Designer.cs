namespace UVtools.GUI.Forms
{
    partial class FrmInstallPEProfiles
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmInstallPEProfiles));
            this.lvPrintProfiles = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lvPrinterProfiles = new System.Windows.Forms.ListView();
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.tsPrinterProfiles = new System.Windows.Forms.ToolStrip();
            this.btnPrinterProfilesUnselectAll = new System.Windows.Forms.ToolStripButton();
            this.btnPrinterProfilesSelectAll = new System.Windows.Forms.ToolStripButton();
            this.lbPrinterProfilesCount = new System.Windows.Forms.ToolStripLabel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tsPrintProfiles = new System.Windows.Forms.ToolStrip();
            this.btnPrintProfilesUnselectAll = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnPrintProfilesSelectAll = new System.Windows.Forms.ToolStripButton();
            this.lbPrintProfilesCount = new System.Windows.Forms.ToolStripLabel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.panel4 = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel6 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.panel7 = new System.Windows.Forms.Panel();
            this.btnRefreshProfiles = new System.Windows.Forms.Button();
            this.btnInstall = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tsPrinterProfiles.SuspendLayout();
            this.panel1.SuspendLayout();
            this.tsPrintProfiles.SuspendLayout();
            this.panel3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.panel7.SuspendLayout();
            this.SuspendLayout();
            // 
            // lvPrintProfiles
            // 
            this.lvPrintProfiles.CheckBoxes = true;
            this.lvPrintProfiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.lvPrintProfiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvPrintProfiles.FullRowSelect = true;
            this.lvPrintProfiles.GridLines = true;
            this.lvPrintProfiles.HideSelection = false;
            this.lvPrintProfiles.Location = new System.Drawing.Point(0, 25);
            this.lvPrintProfiles.Margin = new System.Windows.Forms.Padding(4);
            this.lvPrintProfiles.Name = "lvPrintProfiles";
            this.lvPrintProfiles.Size = new System.Drawing.Size(498, 441);
            this.lvPrintProfiles.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.lvPrintProfiles.TabIndex = 0;
            this.lvPrintProfiles.UseCompatibleStateImageBehavior = false;
            this.lvPrintProfiles.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Print Profiles";
            this.columnHeader1.Width = 400;
            // 
            // lvPrinterProfiles
            // 
            this.lvPrinterProfiles.CheckBoxes = true;
            this.lvPrinterProfiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader2});
            this.lvPrinterProfiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvPrinterProfiles.FullRowSelect = true;
            this.lvPrinterProfiles.GridLines = true;
            this.lvPrinterProfiles.HideSelection = false;
            this.lvPrinterProfiles.Location = new System.Drawing.Point(0, 25);
            this.lvPrinterProfiles.Margin = new System.Windows.Forms.Padding(4);
            this.lvPrinterProfiles.Name = "lvPrinterProfiles";
            this.lvPrinterProfiles.Size = new System.Drawing.Size(498, 441);
            this.lvPrinterProfiles.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.lvPrinterProfiles.TabIndex = 1;
            this.lvPrinterProfiles.UseCompatibleStateImageBehavior = false;
            this.lvPrinterProfiles.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Printers";
            this.columnHeader2.Width = 400;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.panel2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 133);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1008, 472);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.lvPrinterProfiles);
            this.panel2.Controls.Add(this.tsPrinterProfiles);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(507, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(498, 466);
            this.panel2.TabIndex = 4;
            // 
            // tsPrinterProfiles
            // 
            this.tsPrinterProfiles.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsPrinterProfiles.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnPrinterProfilesUnselectAll,
            this.btnPrinterProfilesSelectAll,
            this.lbPrinterProfilesCount});
            this.tsPrinterProfiles.Location = new System.Drawing.Point(0, 0);
            this.tsPrinterProfiles.Name = "tsPrinterProfiles";
            this.tsPrinterProfiles.Size = new System.Drawing.Size(498, 25);
            this.tsPrinterProfiles.TabIndex = 1;
            this.tsPrinterProfiles.Text = "toolStrip2";
            // 
            // btnPrinterProfilesUnselectAll
            // 
            this.btnPrinterProfilesUnselectAll.Image = global::UVtools.GUI.Properties.Resources.checkbox_unmarked_16x16;
            this.btnPrinterProfilesUnselectAll.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnPrinterProfilesUnselectAll.Name = "btnPrinterProfilesUnselectAll";
            this.btnPrinterProfilesUnselectAll.Size = new System.Drawing.Size(89, 22);
            this.btnPrinterProfilesUnselectAll.Text = "Unselect All";
            this.btnPrinterProfilesUnselectAll.Click += new System.EventHandler(this.EventClick);
            // 
            // btnPrinterProfilesSelectAll
            // 
            this.btnPrinterProfilesSelectAll.Image = global::UVtools.GUI.Properties.Resources.checkbox_marked_16x16;
            this.btnPrinterProfilesSelectAll.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnPrinterProfilesSelectAll.Name = "btnPrinterProfilesSelectAll";
            this.btnPrinterProfilesSelectAll.Size = new System.Drawing.Size(75, 22);
            this.btnPrinterProfilesSelectAll.Text = "Select All";
            this.btnPrinterProfilesSelectAll.Click += new System.EventHandler(this.EventClick);
            // 
            // lbPrinterProfilesCount
            // 
            this.lbPrinterProfilesCount.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lbPrinterProfilesCount.Name = "lbPrinterProfilesCount";
            this.lbPrinterProfilesCount.Size = new System.Drawing.Size(36, 22);
            this.lbPrinterProfilesCount.Text = "Items";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lvPrintProfiles);
            this.panel1.Controls.Add(this.tsPrintProfiles);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(498, 466);
            this.panel1.TabIndex = 3;
            // 
            // tsPrintProfiles
            // 
            this.tsPrintProfiles.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsPrintProfiles.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnPrintProfilesUnselectAll,
            this.toolStripSeparator1,
            this.btnPrintProfilesSelectAll,
            this.lbPrintProfilesCount});
            this.tsPrintProfiles.Location = new System.Drawing.Point(0, 0);
            this.tsPrintProfiles.Name = "tsPrintProfiles";
            this.tsPrintProfiles.Size = new System.Drawing.Size(498, 25);
            this.tsPrintProfiles.TabIndex = 0;
            this.tsPrintProfiles.Text = "toolStrip1";
            // 
            // btnPrintProfilesUnselectAll
            // 
            this.btnPrintProfilesUnselectAll.Image = global::UVtools.GUI.Properties.Resources.checkbox_unmarked_16x16;
            this.btnPrintProfilesUnselectAll.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnPrintProfilesUnselectAll.Name = "btnPrintProfilesUnselectAll";
            this.btnPrintProfilesUnselectAll.Size = new System.Drawing.Size(89, 22);
            this.btnPrintProfilesUnselectAll.Text = "Unselect All";
            this.btnPrintProfilesUnselectAll.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // btnPrintProfilesSelectAll
            // 
            this.btnPrintProfilesSelectAll.Image = global::UVtools.GUI.Properties.Resources.checkbox_marked_16x16;
            this.btnPrintProfilesSelectAll.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnPrintProfilesSelectAll.Name = "btnPrintProfilesSelectAll";
            this.btnPrintProfilesSelectAll.Size = new System.Drawing.Size(75, 22);
            this.btnPrintProfilesSelectAll.Text = "Select All";
            this.btnPrintProfilesSelectAll.Click += new System.EventHandler(this.EventClick);
            // 
            // lbPrintProfilesCount
            // 
            this.lbPrintProfilesCount.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lbPrintProfilesCount.Name = "lbPrintProfilesCount";
            this.lbPrintProfilesCount.Size = new System.Drawing.Size(36, 22);
            this.lbPrintProfilesCount.Text = "Items";
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.groupBox2);
            this.panel3.Controls.Add(this.groupBox1);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(1008, 133);
            this.panel3.TabIndex = 3;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(379, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(629, 133);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Information";
            // 
            // label4
            // 
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(3, 20);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(623, 110);
            this.label4.TabIndex = 2;
            this.label4.Text = resources.GetString("label4.Text");
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.panel4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.panel6);
            this.groupBox1.Controls.Add(this.panel5);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(379, 133);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Legend";
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.Color.Black;
            this.panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel4.Location = new System.Drawing.Point(12, 76);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(18, 18);
            this.panel4.TabIndex = 0;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(36, 52);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(332, 18);
            this.label3.TabIndex = 5;
            this.label3.Text = "Installed Profile - Files mismatch, update available";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(35, 76);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(317, 18);
            this.label1.TabIndex = 1;
            this.label1.Text = "Uninstalled Profile - Not present on PrusaSlicer";
            // 
            // panel6
            // 
            this.panel6.BackColor = System.Drawing.Color.Red;
            this.panel6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel6.Location = new System.Drawing.Point(12, 52);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(18, 18);
            this.panel6.TabIndex = 4;
            // 
            // panel5
            // 
            this.panel5.BackColor = System.Drawing.Color.Green;
            this.panel5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel5.Location = new System.Drawing.Point(12, 28);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(18, 18);
            this.panel5.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(36, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(322, 18);
            this.label2.TabIndex = 3;
            this.label2.Text = "Installed Profile - Files match, no need to update";
            // 
            // panel7
            // 
            this.panel7.Controls.Add(this.btnRefreshProfiles);
            this.panel7.Controls.Add(this.btnInstall);
            this.panel7.Controls.Add(this.btnCancel);
            this.panel7.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel7.Location = new System.Drawing.Point(0, 605);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(1008, 76);
            this.panel7.TabIndex = 4;
            // 
            // btnRefreshProfiles
            // 
            this.btnRefreshProfiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRefreshProfiles.Image = global::UVtools.GUI.Properties.Resources.refresh_16x16;
            this.btnRefreshProfiles.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnRefreshProfiles.Location = new System.Drawing.Point(11, 15);
            this.btnRefreshProfiles.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnRefreshProfiles.Name = "btnRefreshProfiles";
            this.btnRefreshProfiles.Size = new System.Drawing.Size(173, 48);
            this.btnRefreshProfiles.TabIndex = 9;
            this.btnRefreshProfiles.Text = "&Refresh Profiles";
            this.btnRefreshProfiles.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnRefreshProfiles.UseVisualStyleBackColor = true;
            this.btnRefreshProfiles.Click += new System.EventHandler(this.EventClick);
            // 
            // btnInstall
            // 
            this.btnInstall.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnInstall.Image = global::UVtools.GUI.Properties.Resources.Ok_24x24;
            this.btnInstall.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnInstall.Location = new System.Drawing.Point(637, 14);
            this.btnInstall.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnInstall.Name = "btnInstall";
            this.btnInstall.Size = new System.Drawing.Size(200, 48);
            this.btnInstall.TabIndex = 7;
            this.btnInstall.Text = "&Install selected profiles";
            this.btnInstall.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnInstall.UseVisualStyleBackColor = true;
            this.btnInstall.Click += new System.EventHandler(this.EventClick);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Image = global::UVtools.GUI.Properties.Resources.Cancel_24x24;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(845, 14);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(150, 48);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // FrmInstallPEProfiles
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 681);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel7);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(1024, 360);
            this.Name = "FrmInstallPEProfiles";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Install profiles into PrusaSlicer";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.tsPrinterProfiles.ResumeLayout(false);
            this.tsPrinterProfiles.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.tsPrintProfiles.ResumeLayout(false);
            this.tsPrintProfiles.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.panel7.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView lvPrintProfiles;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ListView lvPrinterProfiles;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.ToolStrip tsPrinterProfiles;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ToolStrip tsPrintProfiles;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.ToolStripButton btnPrintProfilesSelectAll;
        private System.Windows.Forms.ToolStripButton btnPrintProfilesUnselectAll;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripLabel lbPrintProfilesCount;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.ToolStripButton btnPrinterProfilesUnselectAll;
        private System.Windows.Forms.ToolStripButton btnPrinterProfilesSelectAll;
        private System.Windows.Forms.ToolStripLabel lbPrinterProfilesCount;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.Button btnInstall;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnRefreshProfiles;
    }
}