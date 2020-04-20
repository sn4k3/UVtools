namespace PrusaSL1Viewer
{
    partial class FrmMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            this.menu = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.menuEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusBar = new System.Windows.Forms.StatusStrip();
            this.sbLayers = new System.Windows.Forms.VScrollBar();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.lbLayers = new System.Windows.Forms.Label();
            this.scLeft = new System.Windows.Forms.SplitContainer();
            this.tsThumbnails = new System.Windows.Forms.ToolStrip();
            this.tsThumbnailsCount = new System.Windows.Forms.ToolStripLabel();
            this.lvProperties = new System.Windows.Forms.ListView();
            this.lvChKey = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lvChValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.scCenter = new System.Windows.Forms.SplitContainer();
            this.pbLayers = new System.Windows.Forms.ProgressBar();
            this.pbThumbnail = new System.Windows.Forms.PictureBox();
            this.tsThumbnailsPrevious = new System.Windows.Forms.ToolStripButton();
            this.tsThumbnailsNext = new System.Windows.Forms.ToolStripButton();
            this.tsThumbnailsExport = new System.Windows.Forms.ToolStripButton();
            this.pbLayer = new System.Windows.Forms.PictureBox();
            this.menuFileOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileReload = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileSave = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileSaveAs = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileClose = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileExtract = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileConvert = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuViewRotateImage = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAboutWebsite = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAboutDonate = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAboutAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.menu.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scLeft)).BeginInit();
            this.scLeft.Panel1.SuspendLayout();
            this.scLeft.Panel2.SuspendLayout();
            this.scLeft.SuspendLayout();
            this.tsThumbnails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scCenter)).BeginInit();
            this.scCenter.Panel1.SuspendLayout();
            this.scCenter.Panel2.SuspendLayout();
            this.scCenter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbThumbnail)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbLayer)).BeginInit();
            this.SuspendLayout();
            // 
            // menu
            // 
            this.menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.menuEdit,
            this.viewToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menu.Location = new System.Drawing.Point(0, 0);
            this.menu.Name = "menu";
            this.menu.Size = new System.Drawing.Size(1631, 24);
            this.menu.TabIndex = 0;
            this.menu.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFileOpen,
            this.menuFileReload,
            this.menuFileSave,
            this.menuFileSaveAs,
            this.menuFileClose,
            this.toolStripSeparator1,
            this.menuFileExtract,
            this.menuFileConvert,
            this.toolStripSeparator2,
            this.menuFileExit});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(183, 6);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(183, 6);
            // 
            // menuEdit
            // 
            this.menuEdit.Enabled = false;
            this.menuEdit.Name = "menuEdit";
            this.menuEdit.Size = new System.Drawing.Size(39, 20);
            this.menuEdit.Text = "&Edit";
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuViewRotateImage});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "&View";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuAboutWebsite,
            this.menuAboutDonate,
            this.menuAboutAbout});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // statusBar
            // 
            this.statusBar.Location = new System.Drawing.Point(0, 761);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(1631, 22);
            this.statusBar.TabIndex = 1;
            this.statusBar.Text = "statusStrip1";
            // 
            // sbLayers
            // 
            this.sbLayers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sbLayers.Enabled = false;
            this.sbLayers.LargeChange = 1;
            this.sbLayers.Location = new System.Drawing.Point(0, 0);
            this.sbLayers.Name = "sbLayers";
            this.sbLayers.Size = new System.Drawing.Size(94, 642);
            this.sbLayers.TabIndex = 4;
            this.sbLayers.ValueChanged += new System.EventHandler(this.sbLayers_ValueChanged);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 400F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.Controls.Add(this.splitContainer1, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.scLeft, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.scCenter, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 24);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 737F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1631, 737);
            this.tableLayoutPanel1.TabIndex = 5;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.IsSplitterFixed = true;
            this.splitContainer1.Location = new System.Drawing.Point(1534, 3);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.sbLayers);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.lbLayers);
            this.splitContainer1.Size = new System.Drawing.Size(94, 731);
            this.splitContainer1.SplitterDistance = 642;
            this.splitContainer1.TabIndex = 0;
            // 
            // lbLayers
            // 
            this.lbLayers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbLayers.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbLayers.Location = new System.Drawing.Point(0, 0);
            this.lbLayers.Name = "lbLayers";
            this.lbLayers.Size = new System.Drawing.Size(94, 85);
            this.lbLayers.TabIndex = 0;
            this.lbLayers.Text = "Layers";
            this.lbLayers.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // scLeft
            // 
            this.scLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scLeft.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.scLeft.IsSplitterFixed = true;
            this.scLeft.Location = new System.Drawing.Point(3, 3);
            this.scLeft.Name = "scLeft";
            this.scLeft.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // scLeft.Panel1
            // 
            this.scLeft.Panel1.Controls.Add(this.pbThumbnail);
            this.scLeft.Panel1.Controls.Add(this.tsThumbnails);
            this.scLeft.Panel1MinSize = 425;
            // 
            // scLeft.Panel2
            // 
            this.scLeft.Panel2.Controls.Add(this.lvProperties);
            this.scLeft.Size = new System.Drawing.Size(394, 731);
            this.scLeft.SplitterDistance = 425;
            this.scLeft.TabIndex = 3;
            // 
            // tsThumbnails
            // 
            this.tsThumbnails.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsThumbnails.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsThumbnailsPrevious,
            this.tsThumbnailsCount,
            this.tsThumbnailsNext,
            this.tsThumbnailsExport});
            this.tsThumbnails.Location = new System.Drawing.Point(0, 0);
            this.tsThumbnails.Name = "tsThumbnails";
            this.tsThumbnails.Size = new System.Drawing.Size(394, 25);
            this.tsThumbnails.TabIndex = 5;
            this.tsThumbnails.Text = "tsThumbnails";
            // 
            // tsThumbnailsCount
            // 
            this.tsThumbnailsCount.Enabled = false;
            this.tsThumbnailsCount.Name = "tsThumbnailsCount";
            this.tsThumbnailsCount.Size = new System.Drawing.Size(24, 22);
            this.tsThumbnailsCount.Text = "0/0";
            // 
            // lvProperties
            // 
            this.lvProperties.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.lvChKey,
            this.lvChValue});
            this.lvProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvProperties.FullRowSelect = true;
            this.lvProperties.GridLines = true;
            this.lvProperties.HideSelection = false;
            this.lvProperties.Location = new System.Drawing.Point(0, 0);
            this.lvProperties.Name = "lvProperties";
            this.lvProperties.Size = new System.Drawing.Size(394, 302);
            this.lvProperties.TabIndex = 0;
            this.lvProperties.UseCompatibleStateImageBehavior = false;
            this.lvProperties.View = System.Windows.Forms.View.Details;
            // 
            // lvChKey
            // 
            this.lvChKey.Text = "Key";
            this.lvChKey.Width = 183;
            // 
            // lvChValue
            // 
            this.lvChValue.Text = "Value";
            this.lvChValue.Width = 205;
            // 
            // scCenter
            // 
            this.scCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scCenter.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.scCenter.IsSplitterFixed = true;
            this.scCenter.Location = new System.Drawing.Point(403, 3);
            this.scCenter.Name = "scCenter";
            this.scCenter.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // scCenter.Panel1
            // 
            this.scCenter.Panel1.Controls.Add(this.pbLayer);
            // 
            // scCenter.Panel2
            // 
            this.scCenter.Panel2.Controls.Add(this.pbLayers);
            this.scCenter.Panel2MinSize = 18;
            this.scCenter.Size = new System.Drawing.Size(1125, 731);
            this.scCenter.SplitterDistance = 702;
            this.scCenter.TabIndex = 4;
            // 
            // pbLayers
            // 
            this.pbLayers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbLayers.Location = new System.Drawing.Point(0, 0);
            this.pbLayers.Name = "pbLayers";
            this.pbLayers.Size = new System.Drawing.Size(1125, 25);
            this.pbLayers.Step = 1;
            this.pbLayers.TabIndex = 6;
            // 
            // pbThumbnail
            // 
            this.pbThumbnail.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbThumbnail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbThumbnail.Location = new System.Drawing.Point(0, 25);
            this.pbThumbnail.Name = "pbThumbnail";
            this.pbThumbnail.Size = new System.Drawing.Size(394, 400);
            this.pbThumbnail.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbThumbnail.TabIndex = 4;
            this.pbThumbnail.TabStop = false;
            // 
            // tsThumbnailsPrevious
            // 
            this.tsThumbnailsPrevious.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsThumbnailsPrevious.Enabled = false;
            this.tsThumbnailsPrevious.Image = global::PrusaSL1Viewer.Properties.Resources.Back_16x16;
            this.tsThumbnailsPrevious.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsThumbnailsPrevious.Name = "tsThumbnailsPrevious";
            this.tsThumbnailsPrevious.Size = new System.Drawing.Size(23, 22);
            this.tsThumbnailsPrevious.Text = "toolStripButton1";
            this.tsThumbnailsPrevious.ToolTipText = "Show previous thumbnail";
            this.tsThumbnailsPrevious.Click += new System.EventHandler(this.ItemClicked);
            // 
            // tsThumbnailsNext
            // 
            this.tsThumbnailsNext.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsThumbnailsNext.Enabled = false;
            this.tsThumbnailsNext.Image = global::PrusaSL1Viewer.Properties.Resources.Next_16x16;
            this.tsThumbnailsNext.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsThumbnailsNext.Name = "tsThumbnailsNext";
            this.tsThumbnailsNext.Size = new System.Drawing.Size(23, 22);
            this.tsThumbnailsNext.Text = "toolStripButton2";
            this.tsThumbnailsNext.ToolTipText = "Show next thumbnail";
            this.tsThumbnailsNext.Click += new System.EventHandler(this.ItemClicked);
            // 
            // tsThumbnailsExport
            // 
            this.tsThumbnailsExport.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsThumbnailsExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsThumbnailsExport.Enabled = false;
            this.tsThumbnailsExport.Image = global::PrusaSL1Viewer.Properties.Resources.Save_16x16;
            this.tsThumbnailsExport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsThumbnailsExport.Name = "tsThumbnailsExport";
            this.tsThumbnailsExport.Size = new System.Drawing.Size(23, 22);
            this.tsThumbnailsExport.Text = "toolStripButton3";
            this.tsThumbnailsExport.ToolTipText = "Save thumbnail to file";
            this.tsThumbnailsExport.Click += new System.EventHandler(this.ItemClicked);
            // 
            // pbLayer
            // 
            this.pbLayer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pbLayer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbLayer.Location = new System.Drawing.Point(0, 0);
            this.pbLayer.Name = "pbLayer";
            this.pbLayer.Size = new System.Drawing.Size(1125, 702);
            this.pbLayer.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbLayer.TabIndex = 5;
            this.pbLayer.TabStop = false;
            // 
            // menuFileOpen
            // 
            this.menuFileOpen.Image = global::PrusaSL1Viewer.Properties.Resources.Open_16x16;
            this.menuFileOpen.Name = "menuFileOpen";
            this.menuFileOpen.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.menuFileOpen.Size = new System.Drawing.Size(186, 22);
            this.menuFileOpen.Text = "&Open";
            this.menuFileOpen.Click += new System.EventHandler(this.ItemClicked);
            // 
            // menuFileReload
            // 
            this.menuFileReload.Image = global::PrusaSL1Viewer.Properties.Resources.File_Refresh_16x16;
            this.menuFileReload.Name = "menuFileReload";
            this.menuFileReload.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F5)));
            this.menuFileReload.Size = new System.Drawing.Size(186, 22);
            this.menuFileReload.Text = "&Reload";
            this.menuFileReload.Click += new System.EventHandler(this.ItemClicked);
            // 
            // menuFileSave
            // 
            this.menuFileSave.Enabled = false;
            this.menuFileSave.Image = global::PrusaSL1Viewer.Properties.Resources.Save_16x16;
            this.menuFileSave.Name = "menuFileSave";
            this.menuFileSave.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.menuFileSave.Size = new System.Drawing.Size(186, 22);
            this.menuFileSave.Text = "&Save";
            this.menuFileSave.Click += new System.EventHandler(this.ItemClicked);
            // 
            // menuFileSaveAs
            // 
            this.menuFileSaveAs.Enabled = false;
            this.menuFileSaveAs.Image = global::PrusaSL1Viewer.Properties.Resources.SaveAs_16x16;
            this.menuFileSaveAs.Name = "menuFileSaveAs";
            this.menuFileSaveAs.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
            this.menuFileSaveAs.Size = new System.Drawing.Size(186, 22);
            this.menuFileSaveAs.Text = "Save As";
            this.menuFileSaveAs.Click += new System.EventHandler(this.ItemClicked);
            // 
            // menuFileClose
            // 
            this.menuFileClose.Enabled = false;
            this.menuFileClose.Image = global::PrusaSL1Viewer.Properties.Resources.File_Close_16x16;
            this.menuFileClose.Name = "menuFileClose";
            this.menuFileClose.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
            this.menuFileClose.Size = new System.Drawing.Size(186, 22);
            this.menuFileClose.Text = "&Close";
            this.menuFileClose.Click += new System.EventHandler(this.ItemClicked);
            // 
            // menuFileExtract
            // 
            this.menuFileExtract.Enabled = false;
            this.menuFileExtract.Image = global::PrusaSL1Viewer.Properties.Resources.Extract_object_16x16;
            this.menuFileExtract.Name = "menuFileExtract";
            this.menuFileExtract.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt) 
            | System.Windows.Forms.Keys.E)));
            this.menuFileExtract.Size = new System.Drawing.Size(186, 22);
            this.menuFileExtract.Text = "&Extract";
            this.menuFileExtract.Click += new System.EventHandler(this.ItemClicked);
            // 
            // menuFileConvert
            // 
            this.menuFileConvert.Enabled = false;
            this.menuFileConvert.Image = global::PrusaSL1Viewer.Properties.Resources.Convert_16x16;
            this.menuFileConvert.Name = "menuFileConvert";
            this.menuFileConvert.Size = new System.Drawing.Size(186, 22);
            this.menuFileConvert.Text = "&Convert To";
            // 
            // menuFileExit
            // 
            this.menuFileExit.Image = global::PrusaSL1Viewer.Properties.Resources.Exit_16x16;
            this.menuFileExit.Name = "menuFileExit";
            this.menuFileExit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.menuFileExit.Size = new System.Drawing.Size(186, 22);
            this.menuFileExit.Text = "&Exit";
            this.menuFileExit.Click += new System.EventHandler(this.ItemClicked);
            // 
            // menuViewRotateImage
            // 
            this.menuViewRotateImage.Checked = true;
            this.menuViewRotateImage.CheckOnClick = true;
            this.menuViewRotateImage.CheckState = System.Windows.Forms.CheckState.Checked;
            this.menuViewRotateImage.Image = global::PrusaSL1Viewer.Properties.Resources.Rotate_16x16;
            this.menuViewRotateImage.Name = "menuViewRotateImage";
            this.menuViewRotateImage.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.menuViewRotateImage.Size = new System.Drawing.Size(205, 22);
            this.menuViewRotateImage.Text = "&Rotate Image 90º";
            this.menuViewRotateImage.ToolTipText = "Auto rotate layer preview image at 90º (This can slow down the layer preview)";
            this.menuViewRotateImage.Click += new System.EventHandler(this.ItemClicked);
            // 
            // menuAboutWebsite
            // 
            this.menuAboutWebsite.Image = global::PrusaSL1Viewer.Properties.Resources.Global_Network_icon_16x16;
            this.menuAboutWebsite.Name = "menuAboutWebsite";
            this.menuAboutWebsite.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.W)));
            this.menuAboutWebsite.Size = new System.Drawing.Size(193, 22);
            this.menuAboutWebsite.Text = "&Website";
            this.menuAboutWebsite.Click += new System.EventHandler(this.ItemClicked);
            // 
            // menuAboutDonate
            // 
            this.menuAboutDonate.Image = global::PrusaSL1Viewer.Properties.Resources.Donate_16x16;
            this.menuAboutDonate.Name = "menuAboutDonate";
            this.menuAboutDonate.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
            this.menuAboutDonate.Size = new System.Drawing.Size(193, 22);
            this.menuAboutDonate.Text = "&Donate";
            this.menuAboutDonate.Click += new System.EventHandler(this.ItemClicked);
            // 
            // menuAboutAbout
            // 
            this.menuAboutAbout.Image = global::PrusaSL1Viewer.Properties.Resources.Button_Info_16x16;
            this.menuAboutAbout.Name = "menuAboutAbout";
            this.menuAboutAbout.ShortcutKeys = System.Windows.Forms.Keys.F1;
            this.menuAboutAbout.Size = new System.Drawing.Size(193, 22);
            this.menuAboutAbout.Text = "&About";
            this.menuAboutAbout.Click += new System.EventHandler(this.ItemClicked);
            // 
            // FrmMain
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1631, 783);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.statusBar);
            this.Controls.Add(this.menu);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menu;
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "FrmMain";
            this.Text = "PrusaSL1Viewer";
            this.menu.ResumeLayout(false);
            this.menu.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.scLeft.Panel1.ResumeLayout(false);
            this.scLeft.Panel1.PerformLayout();
            this.scLeft.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scLeft)).EndInit();
            this.scLeft.ResumeLayout(false);
            this.tsThumbnails.ResumeLayout(false);
            this.tsThumbnails.PerformLayout();
            this.scCenter.Panel1.ResumeLayout(false);
            this.scCenter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scCenter)).EndInit();
            this.scCenter.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbThumbnail)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbLayer)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menu;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuFileOpen;
        private System.Windows.Forms.ToolStripMenuItem menuFileExit;
        private System.Windows.Forms.StatusStrip statusBar;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuAboutWebsite;
        private System.Windows.Forms.ToolStripMenuItem menuAboutDonate;
        private System.Windows.Forms.ToolStripMenuItem menuAboutAbout;
        private System.Windows.Forms.VScrollBar sbLayers;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Label lbLayers;
        private System.Windows.Forms.ToolStripMenuItem menuEdit;
        private System.Windows.Forms.PictureBox pbThumbnail;
        private System.Windows.Forms.ListView lvProperties;
        private System.Windows.Forms.ColumnHeader lvChKey;
        private System.Windows.Forms.ColumnHeader lvChValue;
        private System.Windows.Forms.SplitContainer scLeft;
        private System.Windows.Forms.SplitContainer scCenter;
        private System.Windows.Forms.PictureBox pbLayer;
        private System.Windows.Forms.ProgressBar pbLayers;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuViewRotateImage;
        private System.Windows.Forms.ToolStrip tsThumbnails;
        private System.Windows.Forms.ToolStripLabel tsThumbnailsCount;
        private System.Windows.Forms.ToolStripButton tsThumbnailsPrevious;
        private System.Windows.Forms.ToolStripButton tsThumbnailsNext;
        private System.Windows.Forms.ToolStripButton tsThumbnailsExport;
        private System.Windows.Forms.ToolStripMenuItem menuFileClose;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem menuFileExtract;
        private System.Windows.Forms.ToolStripMenuItem menuFileConvert;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem menuFileReload;
        private System.Windows.Forms.ToolStripMenuItem menuFileSave;
        private System.Windows.Forms.ToolStripMenuItem menuFileSaveAs;
    }
}

