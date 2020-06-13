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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            this.menu = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileOpenNewWindow = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileReload = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileSave = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileSaveAs = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileClose = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.menuFileExtract = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileConvert = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.menuFileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuMutate = new System.Windows.Forms.ToolStripMenuItem();
            this.menuTools = new System.Windows.Forms.ToolStripMenuItem();
            this.menuToolsRepairLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpWebsite = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpDonate = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
            this.menuHelpInstallPrinters = new System.Windows.Forms.ToolStripMenuItem();
            this.statusBar = new System.Windows.Forms.StatusStrip();
            this.mainTable = new System.Windows.Forms.TableLayoutPanel();
            this.scCenter = new System.Windows.Forms.SplitContainer();
            this.pbLayer = new Cyotek.Windows.Forms.ImageBox();
            this.tsLayer = new System.Windows.Forms.ToolStrip();
            this.tsLayerImageExport = new System.Windows.Forms.ToolStripButton();
            this.tsLayerResolution = new System.Windows.Forms.ToolStripLabel();
            this.tsLayerImageRotate = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.tsLayerImageLayerDifference = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.tsLayerPreviewTime = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator14 = new System.Windows.Forms.ToolStripSeparator();
            this.tsLayerImageHighlightIssues = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.tsLayerImageLayerOutline = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            this.tsLayerImagePixelEdit = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.tsLayerImageZoom = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
            this.tsLayerImageMouseLocation = new System.Windows.Forms.ToolStripLabel();
            this.pbLayers = new System.Windows.Forms.ProgressBar();
            this.tabControlLeft = new System.Windows.Forms.TabControl();
            this.tbpThumbnailsAndInfo = new System.Windows.Forms.TabPage();
            this.scLeft = new System.Windows.Forms.SplitContainer();
            this.pbThumbnail = new System.Windows.Forms.PictureBox();
            this.tsThumbnails = new System.Windows.Forms.ToolStrip();
            this.tsThumbnailsPrevious = new System.Windows.Forms.ToolStripButton();
            this.tsThumbnailsCount = new System.Windows.Forms.ToolStripLabel();
            this.tsThumbnailsNext = new System.Windows.Forms.ToolStripButton();
            this.tsThumbnailsExport = new System.Windows.Forms.ToolStripButton();
            this.tsThumbnailsResolution = new System.Windows.Forms.ToolStripLabel();
            this.lvProperties = new System.Windows.Forms.ListView();
            this.lvChKey = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lvChValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tsProperties = new System.Windows.Forms.ToolStrip();
            this.tsPropertiesLabelCount = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.tsPropertiesLabelGroups = new System.Windows.Forms.ToolStripLabel();
            this.tsPropertiesButtonSave = new System.Windows.Forms.ToolStripButton();
            this.tabPageGCode = new System.Windows.Forms.TabPage();
            this.tbGCode = new System.Windows.Forms.TextBox();
            this.tsGCode = new System.Windows.Forms.ToolStrip();
            this.tsGCodeLabelLines = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsGcodeLabelChars = new System.Windows.Forms.ToolStripLabel();
            this.tsGCodeButtonSave = new System.Windows.Forms.ToolStripButton();
            this.tabPageIssues = new System.Windows.Forms.TabPage();
            this.lvIssues = new System.Windows.Forms.ListView();
            this.lvIssuesType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lvIssuesCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lvIssuesLayerHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lvIssuesXY = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lvIssuesPixels = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tsIssues = new System.Windows.Forms.ToolStrip();
            this.tsIssuePrevious = new System.Windows.Forms.ToolStripButton();
            this.tsIssueCount = new System.Windows.Forms.ToolStripLabel();
            this.tsIssueNext = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
            this.tsIsuesRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsIssueRemove = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
            this.tsIssuesRepair = new System.Windows.Forms.ToolStripButton();
            this.imageList16x16 = new System.Windows.Forms.ImageList(this.components);
            this.tlRight = new System.Windows.Forms.TableLayoutPanel();
            this.btnPreviousLayer = new System.Windows.Forms.Button();
            this.btnNextLayer = new System.Windows.Forms.Button();
            this.lbMaxLayer = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lbLayerActual = new System.Windows.Forms.Label();
            this.tbLayer = new System.Windows.Forms.TrackBar();
            this.lbInitialLayer = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnFindLayer = new System.Windows.Forms.Button();
            this.btnLastLayer = new System.Windows.Forms.Button();
            this.btnFirstLayer = new System.Windows.Forms.Button();
            this.toolTipInformation = new System.Windows.Forms.ToolTip(this.components);
            this.layerScrollTimer = new System.Windows.Forms.Timer(this.components);
            this.menu.SuspendLayout();
            this.mainTable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scCenter)).BeginInit();
            this.scCenter.Panel1.SuspendLayout();
            this.scCenter.Panel2.SuspendLayout();
            this.scCenter.SuspendLayout();
            this.tsLayer.SuspendLayout();
            this.tabControlLeft.SuspendLayout();
            this.tbpThumbnailsAndInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scLeft)).BeginInit();
            this.scLeft.Panel1.SuspendLayout();
            this.scLeft.Panel2.SuspendLayout();
            this.scLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbThumbnail)).BeginInit();
            this.tsThumbnails.SuspendLayout();
            this.tsProperties.SuspendLayout();
            this.tabPageGCode.SuspendLayout();
            this.tsGCode.SuspendLayout();
            this.tabPageIssues.SuspendLayout();
            this.tsIssues.SuspendLayout();
            this.tlRight.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbLayer)).BeginInit();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // menu
            // 
            this.menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.menuEdit,
            this.menuMutate,
            this.menuTools,
            this.viewToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menu.Location = new System.Drawing.Point(0, 0);
            this.menu.Name = "menu";
            this.menu.Size = new System.Drawing.Size(1684, 24);
            this.menu.TabIndex = 0;
            this.menu.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFileOpen,
            this.menuFileOpenNewWindow,
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
            // menuFileOpen
            // 
            this.menuFileOpen.Image = global::PrusaSL1Viewer.Properties.Resources.Open_16x16;
            this.menuFileOpen.Name = "menuFileOpen";
            this.menuFileOpen.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.menuFileOpen.Size = new System.Drawing.Size(261, 22);
            this.menuFileOpen.Text = "&Open";
            this.menuFileOpen.Click += new System.EventHandler(this.EventClick);
            // 
            // menuFileOpenNewWindow
            // 
            this.menuFileOpenNewWindow.Image = global::PrusaSL1Viewer.Properties.Resources.Open_16x16;
            this.menuFileOpenNewWindow.Name = "menuFileOpenNewWindow";
            this.menuFileOpenNewWindow.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.O)));
            this.menuFileOpenNewWindow.Size = new System.Drawing.Size(261, 22);
            this.menuFileOpenNewWindow.Text = "Open in new window";
            this.menuFileOpenNewWindow.Click += new System.EventHandler(this.EventClick);
            // 
            // menuFileReload
            // 
            this.menuFileReload.Image = global::PrusaSL1Viewer.Properties.Resources.File_Refresh_16x16;
            this.menuFileReload.Name = "menuFileReload";
            this.menuFileReload.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F5)));
            this.menuFileReload.Size = new System.Drawing.Size(261, 22);
            this.menuFileReload.Text = "&Reload";
            this.menuFileReload.Click += new System.EventHandler(this.EventClick);
            // 
            // menuFileSave
            // 
            this.menuFileSave.Enabled = false;
            this.menuFileSave.Image = global::PrusaSL1Viewer.Properties.Resources.Save_16x16;
            this.menuFileSave.Name = "menuFileSave";
            this.menuFileSave.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.menuFileSave.Size = new System.Drawing.Size(261, 22);
            this.menuFileSave.Text = "&Save";
            this.menuFileSave.Click += new System.EventHandler(this.EventClick);
            // 
            // menuFileSaveAs
            // 
            this.menuFileSaveAs.Enabled = false;
            this.menuFileSaveAs.Image = global::PrusaSL1Viewer.Properties.Resources.SaveAs_16x16;
            this.menuFileSaveAs.Name = "menuFileSaveAs";
            this.menuFileSaveAs.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
            this.menuFileSaveAs.Size = new System.Drawing.Size(261, 22);
            this.menuFileSaveAs.Text = "Save As";
            this.menuFileSaveAs.Click += new System.EventHandler(this.EventClick);
            // 
            // menuFileClose
            // 
            this.menuFileClose.Enabled = false;
            this.menuFileClose.Image = global::PrusaSL1Viewer.Properties.Resources.File_Close_16x16;
            this.menuFileClose.Name = "menuFileClose";
            this.menuFileClose.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
            this.menuFileClose.Size = new System.Drawing.Size(261, 22);
            this.menuFileClose.Text = "&Close";
            this.menuFileClose.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(258, 6);
            // 
            // menuFileExtract
            // 
            this.menuFileExtract.Enabled = false;
            this.menuFileExtract.Image = global::PrusaSL1Viewer.Properties.Resources.Extract_object_16x16;
            this.menuFileExtract.Name = "menuFileExtract";
            this.menuFileExtract.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.menuFileExtract.Size = new System.Drawing.Size(261, 22);
            this.menuFileExtract.Text = "&Extract";
            this.menuFileExtract.Click += new System.EventHandler(this.EventClick);
            // 
            // menuFileConvert
            // 
            this.menuFileConvert.Enabled = false;
            this.menuFileConvert.Image = global::PrusaSL1Viewer.Properties.Resources.Convert_16x16;
            this.menuFileConvert.Name = "menuFileConvert";
            this.menuFileConvert.Size = new System.Drawing.Size(261, 22);
            this.menuFileConvert.Text = "&Convert To";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(258, 6);
            // 
            // menuFileExit
            // 
            this.menuFileExit.Image = global::PrusaSL1Viewer.Properties.Resources.Exit_16x16;
            this.menuFileExit.Name = "menuFileExit";
            this.menuFileExit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.menuFileExit.Size = new System.Drawing.Size(261, 22);
            this.menuFileExit.Text = "&Exit";
            this.menuFileExit.Click += new System.EventHandler(this.EventClick);
            // 
            // menuEdit
            // 
            this.menuEdit.Enabled = false;
            this.menuEdit.Name = "menuEdit";
            this.menuEdit.Size = new System.Drawing.Size(39, 20);
            this.menuEdit.Text = "&Edit";
            // 
            // menuMutate
            // 
            this.menuMutate.Enabled = false;
            this.menuMutate.Name = "menuMutate";
            this.menuMutate.Size = new System.Drawing.Size(57, 20);
            this.menuMutate.Text = "&Mutate";
            // 
            // menuTools
            // 
            this.menuTools.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuToolsRepairLayers});
            this.menuTools.Enabled = false;
            this.menuTools.Name = "menuTools";
            this.menuTools.Size = new System.Drawing.Size(46, 20);
            this.menuTools.Text = "&Tools";
            // 
            // menuToolsRepairLayers
            // 
            this.menuToolsRepairLayers.Enabled = false;
            this.menuToolsRepairLayers.Image = global::PrusaSL1Viewer.Properties.Resources.Wrench_16x16;
            this.menuToolsRepairLayers.Name = "menuToolsRepairLayers";
            this.menuToolsRepairLayers.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt) 
            | System.Windows.Forms.Keys.R)));
            this.menuToolsRepairLayers.Size = new System.Drawing.Size(204, 22);
            this.menuToolsRepairLayers.Text = "&Repair layers";
            this.menuToolsRepairLayers.Click += new System.EventHandler(this.EventClick);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "&View";
            this.viewToolStripMenuItem.Visible = false;
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuHelpWebsite,
            this.menuHelpDonate,
            this.menuHelpAbout,
            this.toolStripSeparator10,
            this.menuHelpInstallPrinters});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // menuHelpWebsite
            // 
            this.menuHelpWebsite.Image = global::PrusaSL1Viewer.Properties.Resources.Global_Network_icon_16x16;
            this.menuHelpWebsite.Name = "menuHelpWebsite";
            this.menuHelpWebsite.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F1)));
            this.menuHelpWebsite.Size = new System.Drawing.Size(231, 22);
            this.menuHelpWebsite.Text = "&Website";
            this.menuHelpWebsite.Click += new System.EventHandler(this.EventClick);
            // 
            // menuHelpDonate
            // 
            this.menuHelpDonate.Image = global::PrusaSL1Viewer.Properties.Resources.Donate_16x16;
            this.menuHelpDonate.Name = "menuHelpDonate";
            this.menuHelpDonate.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.F1)));
            this.menuHelpDonate.Size = new System.Drawing.Size(231, 22);
            this.menuHelpDonate.Text = "&Donate";
            this.menuHelpDonate.Click += new System.EventHandler(this.EventClick);
            // 
            // menuHelpAbout
            // 
            this.menuHelpAbout.Image = global::PrusaSL1Viewer.Properties.Resources.Button_Info_16x16;
            this.menuHelpAbout.Name = "menuHelpAbout";
            this.menuHelpAbout.ShortcutKeys = System.Windows.Forms.Keys.F1;
            this.menuHelpAbout.Size = new System.Drawing.Size(231, 22);
            this.menuHelpAbout.Text = "&About";
            this.menuHelpAbout.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator10
            // 
            this.toolStripSeparator10.Name = "toolStripSeparator10";
            this.toolStripSeparator10.Size = new System.Drawing.Size(228, 6);
            // 
            // menuHelpInstallPrinters
            // 
            this.menuHelpInstallPrinters.Image = global::PrusaSL1Viewer.Properties.Resources.CNCMachine_16x16;
            this.menuHelpInstallPrinters.Name = "menuHelpInstallPrinters";
            this.menuHelpInstallPrinters.Size = new System.Drawing.Size(231, 22);
            this.menuHelpInstallPrinters.Text = "Install profiles into PrusaSlicer";
            this.menuHelpInstallPrinters.Click += new System.EventHandler(this.EventClick);
            // 
            // statusBar
            // 
            this.statusBar.Location = new System.Drawing.Point(0, 764);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(1684, 22);
            this.statusBar.TabIndex = 1;
            this.statusBar.Text = "statusStrip1";
            // 
            // mainTable
            // 
            this.mainTable.ColumnCount = 3;
            this.mainTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 400F));
            this.mainTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.mainTable.Controls.Add(this.scCenter, 1, 0);
            this.mainTable.Controls.Add(this.tabControlLeft, 0, 0);
            this.mainTable.Controls.Add(this.tlRight, 2, 0);
            this.mainTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTable.Location = new System.Drawing.Point(0, 24);
            this.mainTable.Name = "mainTable";
            this.mainTable.RowCount = 1;
            this.mainTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTable.Size = new System.Drawing.Size(1684, 740);
            this.mainTable.TabIndex = 5;
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
            this.scCenter.Panel1.Controls.Add(this.tsLayer);
            // 
            // scCenter.Panel2
            // 
            this.scCenter.Panel2.Controls.Add(this.pbLayers);
            this.scCenter.Panel2MinSize = 18;
            this.scCenter.Size = new System.Drawing.Size(1148, 734);
            this.scCenter.SplitterDistance = 705;
            this.scCenter.TabIndex = 4;
            // 
            // pbLayer
            // 
            this.pbLayer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbLayer.GridScale = Cyotek.Windows.Forms.ImageBoxGridScale.Large;
            this.pbLayer.Location = new System.Drawing.Point(0, 25);
            this.pbLayer.Name = "pbLayer";
            this.pbLayer.PanMode = Cyotek.Windows.Forms.ImageBoxPanMode.Left;
            this.pbLayer.ShowPixelGrid = true;
            this.pbLayer.Size = new System.Drawing.Size(1148, 680);
            this.pbLayer.TabIndex = 7;
            this.pbLayer.Zoomed += new System.EventHandler<Cyotek.Windows.Forms.ImageBoxZoomEventArgs>(this.pbLayer_Zoomed);
            this.pbLayer.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pbLayer_MouseMove);
            this.pbLayer.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pbLayer_MouseUp);
            // 
            // tsLayer
            // 
            this.tsLayer.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsLayer.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsLayerImageExport,
            this.tsLayerResolution,
            this.tsLayerImageRotate,
            this.toolStripSeparator5,
            this.tsLayerImageLayerDifference,
            this.toolStripSeparator6,
            this.tsLayerPreviewTime,
            this.toolStripSeparator14,
            this.tsLayerImageHighlightIssues,
            this.toolStripSeparator7,
            this.tsLayerImageLayerOutline,
            this.toolStripSeparator9,
            this.tsLayerImagePixelEdit,
            this.toolStripSeparator8,
            this.tsLayerImageZoom,
            this.toolStripSeparator11,
            this.tsLayerImageMouseLocation});
            this.tsLayer.Location = new System.Drawing.Point(0, 0);
            this.tsLayer.Name = "tsLayer";
            this.tsLayer.Size = new System.Drawing.Size(1148, 25);
            this.tsLayer.TabIndex = 6;
            this.tsLayer.Text = "Layer Menu";
            // 
            // tsLayerImageExport
            // 
            this.tsLayerImageExport.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsLayerImageExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsLayerImageExport.Enabled = false;
            this.tsLayerImageExport.Image = global::PrusaSL1Viewer.Properties.Resources.Save_16x16;
            this.tsLayerImageExport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsLayerImageExport.Name = "tsLayerImageExport";
            this.tsLayerImageExport.Size = new System.Drawing.Size(23, 22);
            this.tsLayerImageExport.Text = "Save Layer";
            this.tsLayerImageExport.ToolTipText = "Save layer image to file";
            this.tsLayerImageExport.Click += new System.EventHandler(this.EventClick);
            // 
            // tsLayerResolution
            // 
            this.tsLayerResolution.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsLayerResolution.Name = "tsLayerResolution";
            this.tsLayerResolution.Size = new System.Drawing.Size(63, 22);
            this.tsLayerResolution.Text = "Resolution";
            this.tsLayerResolution.ToolTipText = "Layer Resolution";
            // 
            // tsLayerImageRotate
            // 
            this.tsLayerImageRotate.Checked = true;
            this.tsLayerImageRotate.CheckOnClick = true;
            this.tsLayerImageRotate.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsLayerImageRotate.Image = global::PrusaSL1Viewer.Properties.Resources.Rotate_16x16;
            this.tsLayerImageRotate.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsLayerImageRotate.Name = "tsLayerImageRotate";
            this.tsLayerImageRotate.Size = new System.Drawing.Size(117, 22);
            this.tsLayerImageRotate.Text = "Rotate Image 90º";
            this.tsLayerImageRotate.ToolTipText = "Auto rotate layer preview image at 90º (This can slow down the layer preview) [CT" +
    "RL+R]";
            this.tsLayerImageRotate.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(6, 25);
            // 
            // tsLayerImageLayerDifference
            // 
            this.tsLayerImageLayerDifference.CheckOnClick = true;
            this.tsLayerImageLayerDifference.Image = global::PrusaSL1Viewer.Properties.Resources.layers_16x16;
            this.tsLayerImageLayerDifference.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsLayerImageLayerDifference.Name = "tsLayerImageLayerDifference";
            this.tsLayerImageLayerDifference.Size = new System.Drawing.Size(81, 22);
            this.tsLayerImageLayerDifference.Text = "Difference";
            this.tsLayerImageLayerDifference.ToolTipText = "Show layer differences where daker pixels were also present on previous layer and" +
    " the white pixels the difference between previous and current layer.";
            this.tsLayerImageLayerDifference.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(6, 25);
            // 
            // tsLayerPreviewTime
            // 
            this.tsLayerPreviewTime.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsLayerPreviewTime.Name = "tsLayerPreviewTime";
            this.tsLayerPreviewTime.Size = new System.Drawing.Size(77, 22);
            this.tsLayerPreviewTime.Text = "Preview Time";
            this.tsLayerPreviewTime.ToolTipText = "Layer Resolution";
            // 
            // toolStripSeparator14
            // 
            this.toolStripSeparator14.Name = "toolStripSeparator14";
            this.toolStripSeparator14.Size = new System.Drawing.Size(6, 25);
            // 
            // tsLayerImageHighlightIssues
            // 
            this.tsLayerImageHighlightIssues.Checked = true;
            this.tsLayerImageHighlightIssues.CheckOnClick = true;
            this.tsLayerImageHighlightIssues.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsLayerImageHighlightIssues.Image = global::PrusaSL1Viewer.Properties.Resources.warning_16x16;
            this.tsLayerImageHighlightIssues.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsLayerImageHighlightIssues.Name = "tsLayerImageHighlightIssues";
            this.tsLayerImageHighlightIssues.Size = new System.Drawing.Size(58, 22);
            this.tsLayerImageHighlightIssues.Text = "Issues";
            this.tsLayerImageHighlightIssues.ToolTipText = "Highlight Issues on current layer.\r\nValid only if Issues are calculated.";
            this.tsLayerImageHighlightIssues.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            this.toolStripSeparator7.Size = new System.Drawing.Size(6, 25);
            // 
            // tsLayerImageLayerOutline
            // 
            this.tsLayerImageLayerOutline.CheckOnClick = true;
            this.tsLayerImageLayerOutline.Image = global::PrusaSL1Viewer.Properties.Resources.Geometry_16x16;
            this.tsLayerImageLayerOutline.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsLayerImageLayerOutline.Name = "tsLayerImageLayerOutline";
            this.tsLayerImageLayerOutline.Size = new System.Drawing.Size(66, 22);
            this.tsLayerImageLayerOutline.Text = "Outline";
            this.tsLayerImageLayerOutline.ToolTipText = "Show layer outlines only";
            this.tsLayerImageLayerOutline.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator9
            // 
            this.toolStripSeparator9.Name = "toolStripSeparator9";
            this.toolStripSeparator9.Size = new System.Drawing.Size(6, 25);
            // 
            // tsLayerImagePixelEdit
            // 
            this.tsLayerImagePixelEdit.CheckOnClick = true;
            this.tsLayerImagePixelEdit.Image = global::PrusaSL1Viewer.Properties.Resources.pixel_16x16;
            this.tsLayerImagePixelEdit.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsLayerImagePixelEdit.Name = "tsLayerImagePixelEdit";
            this.tsLayerImagePixelEdit.Size = new System.Drawing.Size(75, 22);
            this.tsLayerImagePixelEdit.Text = "Pixel Edit";
            this.tsLayerImagePixelEdit.ToolTipText = "Edit layer image pixels (Righ click to add pixel and SHIFT + Right click to remov" +
    "e pixel)\r\nRed pixels are removed pixels\r\nGreen pixels are added pixels";
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            this.toolStripSeparator8.Size = new System.Drawing.Size(6, 25);
            // 
            // tsLayerImageZoom
            // 
            this.tsLayerImageZoom.Image = global::PrusaSL1Viewer.Properties.Resources.search_16x16;
            this.tsLayerImageZoom.Name = "tsLayerImageZoom";
            this.tsLayerImageZoom.Size = new System.Drawing.Size(89, 22);
            this.tsLayerImageZoom.Text = "Zoom: 100%";
            this.tsLayerImageZoom.ToolTipText = "Layer image zoom level, use mouse scroll to zoom in/out into image\r\nCtrl + 0 to s" +
    "cale to fit";
            // 
            // toolStripSeparator11
            // 
            this.toolStripSeparator11.Name = "toolStripSeparator11";
            this.toolStripSeparator11.Size = new System.Drawing.Size(6, 25);
            // 
            // tsLayerImageMouseLocation
            // 
            this.tsLayerImageMouseLocation.Image = global::PrusaSL1Viewer.Properties.Resources.pointer_16x16;
            this.tsLayerImageMouseLocation.Name = "tsLayerImageMouseLocation";
            this.tsLayerImageMouseLocation.Size = new System.Drawing.Size(79, 22);
            this.tsLayerImageMouseLocation.Text = "{X=0, Y=0}";
            this.tsLayerImageMouseLocation.ToolTipText = "Mouse over pixel location and pixel brightness";
            // 
            // pbLayers
            // 
            this.pbLayers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbLayers.Location = new System.Drawing.Point(0, 0);
            this.pbLayers.Name = "pbLayers";
            this.pbLayers.Size = new System.Drawing.Size(1148, 25);
            this.pbLayers.Step = 1;
            this.pbLayers.TabIndex = 6;
            // 
            // tabControlLeft
            // 
            this.tabControlLeft.Controls.Add(this.tbpThumbnailsAndInfo);
            this.tabControlLeft.Controls.Add(this.tabPageGCode);
            this.tabControlLeft.Controls.Add(this.tabPageIssues);
            this.tabControlLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlLeft.ImageList = this.imageList16x16;
            this.tabControlLeft.ItemSize = new System.Drawing.Size(130, 19);
            this.tabControlLeft.Location = new System.Drawing.Point(3, 3);
            this.tabControlLeft.Name = "tabControlLeft";
            this.tabControlLeft.SelectedIndex = 0;
            this.tabControlLeft.Size = new System.Drawing.Size(394, 734);
            this.tabControlLeft.TabIndex = 5;
            this.tabControlLeft.SelectedIndexChanged += new System.EventHandler(this.EventSelectedIndexChanged);
            // 
            // tbpThumbnailsAndInfo
            // 
            this.tbpThumbnailsAndInfo.Controls.Add(this.scLeft);
            this.tbpThumbnailsAndInfo.ImageKey = "PhotoInfo-16x16.png";
            this.tbpThumbnailsAndInfo.Location = new System.Drawing.Point(4, 23);
            this.tbpThumbnailsAndInfo.Name = "tbpThumbnailsAndInfo";
            this.tbpThumbnailsAndInfo.Padding = new System.Windows.Forms.Padding(3);
            this.tbpThumbnailsAndInfo.Size = new System.Drawing.Size(386, 707);
            this.tbpThumbnailsAndInfo.TabIndex = 0;
            this.tbpThumbnailsAndInfo.Text = "Information";
            this.tbpThumbnailsAndInfo.UseVisualStyleBackColor = true;
            // 
            // scLeft
            // 
            this.scLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scLeft.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.scLeft.Location = new System.Drawing.Point(3, 3);
            this.scLeft.Name = "scLeft";
            this.scLeft.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // scLeft.Panel1
            // 
            this.scLeft.Panel1.Controls.Add(this.pbThumbnail);
            this.scLeft.Panel1.Controls.Add(this.tsThumbnails);
            this.scLeft.Panel1MinSize = 150;
            // 
            // scLeft.Panel2
            // 
            this.scLeft.Panel2.Controls.Add(this.lvProperties);
            this.scLeft.Panel2.Controls.Add(this.tsProperties);
            this.scLeft.Size = new System.Drawing.Size(380, 701);
            this.scLeft.SplitterDistance = 425;
            this.scLeft.TabIndex = 4;
            // 
            // pbThumbnail
            // 
            this.pbThumbnail.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbThumbnail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbThumbnail.Location = new System.Drawing.Point(0, 25);
            this.pbThumbnail.Name = "pbThumbnail";
            this.pbThumbnail.Size = new System.Drawing.Size(380, 400);
            this.pbThumbnail.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbThumbnail.TabIndex = 4;
            this.pbThumbnail.TabStop = false;
            // 
            // tsThumbnails
            // 
            this.tsThumbnails.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsThumbnails.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsThumbnailsPrevious,
            this.tsThumbnailsCount,
            this.tsThumbnailsNext,
            this.tsThumbnailsExport,
            this.tsThumbnailsResolution});
            this.tsThumbnails.Location = new System.Drawing.Point(0, 0);
            this.tsThumbnails.Name = "tsThumbnails";
            this.tsThumbnails.Size = new System.Drawing.Size(380, 25);
            this.tsThumbnails.TabIndex = 5;
            this.tsThumbnails.Text = "Thumbnail Menu";
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
            this.tsThumbnailsPrevious.Click += new System.EventHandler(this.EventClick);
            // 
            // tsThumbnailsCount
            // 
            this.tsThumbnailsCount.Enabled = false;
            this.tsThumbnailsCount.Name = "tsThumbnailsCount";
            this.tsThumbnailsCount.Size = new System.Drawing.Size(24, 22);
            this.tsThumbnailsCount.Tag = "";
            this.tsThumbnailsCount.Text = "0/0";
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
            this.tsThumbnailsNext.Click += new System.EventHandler(this.EventClick);
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
            this.tsThumbnailsExport.Text = "Save Thumbnail";
            this.tsThumbnailsExport.ToolTipText = "Save thumbnail to file";
            this.tsThumbnailsExport.Click += new System.EventHandler(this.EventClick);
            // 
            // tsThumbnailsResolution
            // 
            this.tsThumbnailsResolution.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsThumbnailsResolution.Name = "tsThumbnailsResolution";
            this.tsThumbnailsResolution.Size = new System.Drawing.Size(63, 22);
            this.tsThumbnailsResolution.Text = "Resolution";
            this.tsThumbnailsResolution.ToolTipText = "Thumbnail Resolution";
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
            this.lvProperties.Location = new System.Drawing.Point(0, 25);
            this.lvProperties.Name = "lvProperties";
            this.lvProperties.Size = new System.Drawing.Size(380, 247);
            this.lvProperties.TabIndex = 1;
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
            // tsProperties
            // 
            this.tsProperties.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsProperties.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsPropertiesLabelCount,
            this.toolStripSeparator4,
            this.tsPropertiesLabelGroups,
            this.tsPropertiesButtonSave});
            this.tsProperties.Location = new System.Drawing.Point(0, 0);
            this.tsProperties.Name = "tsProperties";
            this.tsProperties.Size = new System.Drawing.Size(380, 25);
            this.tsProperties.TabIndex = 0;
            this.tsProperties.Text = "Properties";
            // 
            // tsPropertiesLabelCount
            // 
            this.tsPropertiesLabelCount.Name = "tsPropertiesLabelCount";
            this.tsPropertiesLabelCount.Size = new System.Drawing.Size(40, 22);
            this.tsPropertiesLabelCount.Text = "Count";
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
            // 
            // tsPropertiesLabelGroups
            // 
            this.tsPropertiesLabelGroups.Name = "tsPropertiesLabelGroups";
            this.tsPropertiesLabelGroups.Size = new System.Drawing.Size(45, 22);
            this.tsPropertiesLabelGroups.Text = "Groups";
            // 
            // tsPropertiesButtonSave
            // 
            this.tsPropertiesButtonSave.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsPropertiesButtonSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsPropertiesButtonSave.Enabled = false;
            this.tsPropertiesButtonSave.Image = global::PrusaSL1Viewer.Properties.Resources.Save_16x16;
            this.tsPropertiesButtonSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsPropertiesButtonSave.Name = "tsPropertiesButtonSave";
            this.tsPropertiesButtonSave.Size = new System.Drawing.Size(23, 22);
            this.tsPropertiesButtonSave.Text = "Save Thumbnail";
            this.tsPropertiesButtonSave.ToolTipText = "Save properties to a file";
            this.tsPropertiesButtonSave.Click += new System.EventHandler(this.EventClick);
            // 
            // tabPageGCode
            // 
            this.tabPageGCode.Controls.Add(this.tbGCode);
            this.tabPageGCode.Controls.Add(this.tsGCode);
            this.tabPageGCode.ImageKey = "GCode-16x16.png";
            this.tabPageGCode.Location = new System.Drawing.Point(4, 23);
            this.tabPageGCode.Name = "tabPageGCode";
            this.tabPageGCode.Size = new System.Drawing.Size(386, 707);
            this.tabPageGCode.TabIndex = 2;
            this.tabPageGCode.Text = "GCode";
            this.tabPageGCode.UseVisualStyleBackColor = true;
            // 
            // tbGCode
            // 
            this.tbGCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbGCode.Location = new System.Drawing.Point(0, 25);
            this.tbGCode.Multiline = true;
            this.tbGCode.Name = "tbGCode";
            this.tbGCode.ReadOnly = true;
            this.tbGCode.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbGCode.Size = new System.Drawing.Size(386, 682);
            this.tbGCode.TabIndex = 1;
            // 
            // tsGCode
            // 
            this.tsGCode.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsGCode.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsGCodeLabelLines,
            this.toolStripSeparator3,
            this.tsGcodeLabelChars,
            this.tsGCodeButtonSave});
            this.tsGCode.Location = new System.Drawing.Point(0, 0);
            this.tsGCode.Name = "tsGCode";
            this.tsGCode.Size = new System.Drawing.Size(386, 25);
            this.tsGCode.TabIndex = 0;
            // 
            // tsGCodeLabelLines
            // 
            this.tsGCodeLabelLines.Name = "tsGCodeLabelLines";
            this.tsGCodeLabelLines.Size = new System.Drawing.Size(34, 22);
            this.tsGCodeLabelLines.Text = "Lines";
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // tsGcodeLabelChars
            // 
            this.tsGcodeLabelChars.Name = "tsGcodeLabelChars";
            this.tsGcodeLabelChars.Size = new System.Drawing.Size(37, 22);
            this.tsGcodeLabelChars.Text = "Chars";
            // 
            // tsGCodeButtonSave
            // 
            this.tsGCodeButtonSave.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsGCodeButtonSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsGCodeButtonSave.Image = global::PrusaSL1Viewer.Properties.Resources.Save_16x16;
            this.tsGCodeButtonSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsGCodeButtonSave.Name = "tsGCodeButtonSave";
            this.tsGCodeButtonSave.Size = new System.Drawing.Size(23, 22);
            this.tsGCodeButtonSave.Text = "Save GCode";
            this.tsGCodeButtonSave.ToolTipText = "Save GCode to file";
            this.tsGCodeButtonSave.Click += new System.EventHandler(this.EventClick);
            // 
            // tabPageIssues
            // 
            this.tabPageIssues.Controls.Add(this.lvIssues);
            this.tabPageIssues.Controls.Add(this.tsIssues);
            this.tabPageIssues.ImageKey = "warning-16x16.png";
            this.tabPageIssues.Location = new System.Drawing.Point(4, 23);
            this.tabPageIssues.Name = "tabPageIssues";
            this.tabPageIssues.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageIssues.Size = new System.Drawing.Size(386, 707);
            this.tabPageIssues.TabIndex = 3;
            this.tabPageIssues.Text = "Issues";
            this.tabPageIssues.UseVisualStyleBackColor = true;
            // 
            // lvIssues
            // 
            this.lvIssues.Activation = System.Windows.Forms.ItemActivation.TwoClick;
            this.lvIssues.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.lvIssuesType,
            this.lvIssuesCount,
            this.lvIssuesLayerHeader,
            this.lvIssuesXY,
            this.lvIssuesPixels});
            this.lvIssues.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvIssues.FullRowSelect = true;
            this.lvIssues.GridLines = true;
            this.lvIssues.HideSelection = false;
            this.lvIssues.Location = new System.Drawing.Point(3, 28);
            this.lvIssues.Name = "lvIssues";
            this.lvIssues.Size = new System.Drawing.Size(380, 676);
            this.lvIssues.TabIndex = 8;
            this.lvIssues.UseCompatibleStateImageBehavior = false;
            this.lvIssues.View = System.Windows.Forms.View.Details;
            this.lvIssues.ItemActivate += new System.EventHandler(this.EventItemActivate);
            this.lvIssues.SelectedIndexChanged += new System.EventHandler(this.EventSelectedIndexChanged);
            this.lvIssues.KeyUp += new System.Windows.Forms.KeyEventHandler(this.EventKeyUp);
            // 
            // lvIssuesType
            // 
            this.lvIssuesType.Text = "Type";
            this.lvIssuesType.Width = 100;
            // 
            // lvIssuesCount
            // 
            this.lvIssuesCount.Text = "#";
            this.lvIssuesCount.Width = 50;
            // 
            // lvIssuesLayerHeader
            // 
            this.lvIssuesLayerHeader.Text = "Layer";
            this.lvIssuesLayerHeader.Width = 50;
            // 
            // lvIssuesXY
            // 
            this.lvIssuesXY.Text = "X, Y";
            this.lvIssuesXY.Width = 100;
            // 
            // lvIssuesPixels
            // 
            this.lvIssuesPixels.Text = "Pixels";
            this.lvIssuesPixels.Width = 55;
            // 
            // tsIssues
            // 
            this.tsIssues.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsIssues.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsIssuePrevious,
            this.tsIssueCount,
            this.tsIssueNext,
            this.toolStripSeparator13,
            this.tsIsuesRefresh,
            this.tsIssueRemove,
            this.toolStripSeparator12,
            this.tsIssuesRepair});
            this.tsIssues.Location = new System.Drawing.Point(3, 3);
            this.tsIssues.Name = "tsIssues";
            this.tsIssues.Size = new System.Drawing.Size(380, 25);
            this.tsIssues.TabIndex = 7;
            this.tsIssues.Text = "Thumbnail Menu";
            // 
            // tsIssuePrevious
            // 
            this.tsIssuePrevious.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsIssuePrevious.Enabled = false;
            this.tsIssuePrevious.Image = global::PrusaSL1Viewer.Properties.Resources.Back_16x16;
            this.tsIssuePrevious.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsIssuePrevious.Name = "tsIssuePrevious";
            this.tsIssuePrevious.Size = new System.Drawing.Size(23, 22);
            this.tsIssuePrevious.Text = "Previous";
            this.tsIssuePrevious.ToolTipText = "Show previous issue [CTRL+Left]";
            this.tsIssuePrevious.Click += new System.EventHandler(this.EventClick);
            // 
            // tsIssueCount
            // 
            this.tsIssueCount.Enabled = false;
            this.tsIssueCount.Name = "tsIssueCount";
            this.tsIssueCount.Size = new System.Drawing.Size(24, 22);
            this.tsIssueCount.Text = "0/0";
            // 
            // tsIssueNext
            // 
            this.tsIssueNext.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsIssueNext.Enabled = false;
            this.tsIssueNext.Image = global::PrusaSL1Viewer.Properties.Resources.Next_16x16;
            this.tsIssueNext.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsIssueNext.Name = "tsIssueNext";
            this.tsIssueNext.Size = new System.Drawing.Size(23, 22);
            this.tsIssueNext.Text = "tsIslandsNext";
            this.tsIssueNext.ToolTipText = "Show next issue [CTRL+Right]";
            this.tsIssueNext.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator13
            // 
            this.toolStripSeparator13.Name = "toolStripSeparator13";
            this.toolStripSeparator13.Size = new System.Drawing.Size(6, 25);
            // 
            // tsIsuesRefresh
            // 
            this.tsIsuesRefresh.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsIsuesRefresh.Enabled = false;
            this.tsIsuesRefresh.Image = global::PrusaSL1Viewer.Properties.Resources.refresh_16x16;
            this.tsIsuesRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsIsuesRefresh.Name = "tsIsuesRefresh";
            this.tsIsuesRefresh.Size = new System.Drawing.Size(61, 22);
            this.tsIsuesRefresh.Text = "Detect";
            this.tsIsuesRefresh.ToolTipText = "Compute Issues";
            this.tsIsuesRefresh.Click += new System.EventHandler(this.EventClick);
            // 
            // tsIssueRemove
            // 
            this.tsIssueRemove.Enabled = false;
            this.tsIssueRemove.Image = global::PrusaSL1Viewer.Properties.Resources.delete_16x16;
            this.tsIssueRemove.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsIssueRemove.Name = "tsIssueRemove";
            this.tsIssueRemove.Size = new System.Drawing.Size(70, 22);
            this.tsIssueRemove.Text = "Remove";
            this.tsIssueRemove.ToolTipText = "Remove selected issue when possible";
            this.tsIssueRemove.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator12
            // 
            this.toolStripSeparator12.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator12.Name = "toolStripSeparator12";
            this.toolStripSeparator12.Size = new System.Drawing.Size(6, 25);
            // 
            // tsIssuesRepair
            // 
            this.tsIssuesRepair.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsIssuesRepair.Enabled = false;
            this.tsIssuesRepair.Image = global::PrusaSL1Viewer.Properties.Resources.Wrench_16x16;
            this.tsIssuesRepair.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsIssuesRepair.Name = "tsIssuesRepair";
            this.tsIssuesRepair.Size = new System.Drawing.Size(60, 22);
            this.tsIssuesRepair.Text = "Repair";
            this.tsIssuesRepair.ToolTipText = "Attempt to repair issues";
            this.tsIssuesRepair.Click += new System.EventHandler(this.EventClick);
            // 
            // imageList16x16
            // 
            this.imageList16x16.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList16x16.ImageStream")));
            this.imageList16x16.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList16x16.Images.SetKeyName(0, "DataList-16x16.png");
            this.imageList16x16.Images.SetKeyName(1, "PhotoInfo-16x16.png");
            this.imageList16x16.Images.SetKeyName(2, "GCode-16x16.png");
            this.imageList16x16.Images.SetKeyName(3, "warning-16x16.png");
            // 
            // tlRight
            // 
            this.tlRight.ColumnCount = 1;
            this.tlRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlRight.Controls.Add(this.btnPreviousLayer, 0, 3);
            this.tlRight.Controls.Add(this.btnNextLayer, 0, 1);
            this.tlRight.Controls.Add(this.lbMaxLayer, 0, 0);
            this.tlRight.Controls.Add(this.panel1, 0, 2);
            this.tlRight.Controls.Add(this.lbInitialLayer, 0, 5);
            this.tlRight.Controls.Add(this.panel2, 0, 4);
            this.tlRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlRight.Location = new System.Drawing.Point(1557, 3);
            this.tlRight.Name = "tlRight";
            this.tlRight.RowCount = 6;
            this.tlRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tlRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tlRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tlRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tlRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tlRight.Size = new System.Drawing.Size(124, 734);
            this.tlRight.TabIndex = 6;
            // 
            // btnPreviousLayer
            // 
            this.btnPreviousLayer.Enabled = false;
            this.btnPreviousLayer.Image = global::PrusaSL1Viewer.Properties.Resources.arrow_down_16x16;
            this.btnPreviousLayer.Location = new System.Drawing.Point(3, 623);
            this.btnPreviousLayer.Name = "btnPreviousLayer";
            this.btnPreviousLayer.Size = new System.Drawing.Size(118, 26);
            this.btnPreviousLayer.TabIndex = 14;
            this.btnPreviousLayer.Tag = "0";
            this.btnPreviousLayer.Text = "-";
            this.btnPreviousLayer.UseVisualStyleBackColor = true;
            this.btnPreviousLayer.Click += new System.EventHandler(this.EventClick);
            this.btnPreviousLayer.MouseDown += new System.Windows.Forms.MouseEventHandler(this.EventMouseDown);
            this.btnPreviousLayer.MouseUp += new System.Windows.Forms.MouseEventHandler(this.EventMouseUp);
            // 
            // btnNextLayer
            // 
            this.btnNextLayer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNextLayer.Enabled = false;
            this.btnNextLayer.Image = global::PrusaSL1Viewer.Properties.Resources.arrow_up_16x16;
            this.btnNextLayer.Location = new System.Drawing.Point(3, 53);
            this.btnNextLayer.Name = "btnNextLayer";
            this.btnNextLayer.Size = new System.Drawing.Size(118, 26);
            this.btnNextLayer.TabIndex = 8;
            this.btnNextLayer.Tag = "1";
            this.btnNextLayer.Text = "+";
            this.btnNextLayer.UseVisualStyleBackColor = true;
            this.btnNextLayer.Click += new System.EventHandler(this.EventClick);
            this.btnNextLayer.MouseDown += new System.Windows.Forms.MouseEventHandler(this.EventMouseDown);
            this.btnNextLayer.MouseUp += new System.Windows.Forms.MouseEventHandler(this.EventMouseUp);
            // 
            // lbMaxLayer
            // 
            this.lbMaxLayer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbMaxLayer.Location = new System.Drawing.Point(3, 0);
            this.lbMaxLayer.Name = "lbMaxLayer";
            this.lbMaxLayer.Size = new System.Drawing.Size(118, 50);
            this.lbMaxLayer.TabIndex = 12;
            this.lbMaxLayer.Text = "Layers";
            this.lbMaxLayer.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lbLayerActual);
            this.panel1.Controls.Add(this.tbLayer);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 85);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(118, 532);
            this.panel1.TabIndex = 13;
            // 
            // lbLayerActual
            // 
            this.lbLayerActual.AutoSize = true;
            this.lbLayerActual.Location = new System.Drawing.Point(3, 551);
            this.lbLayerActual.Name = "lbLayerActual";
            this.lbLayerActual.Size = new System.Drawing.Size(13, 13);
            this.lbLayerActual.TabIndex = 9;
            this.lbLayerActual.Text = "?";
            // 
            // tbLayer
            // 
            this.tbLayer.Dock = System.Windows.Forms.DockStyle.Right;
            this.tbLayer.Location = new System.Drawing.Point(73, 0);
            this.tbLayer.Name = "tbLayer";
            this.tbLayer.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.tbLayer.Size = new System.Drawing.Size(45, 532);
            this.tbLayer.TabIndex = 8;
            this.tbLayer.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
            this.tbLayer.ValueChanged += new System.EventHandler(this.ValueChanged);
            // 
            // lbInitialLayer
            // 
            this.lbInitialLayer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbInitialLayer.Location = new System.Drawing.Point(3, 684);
            this.lbInitialLayer.Name = "lbInitialLayer";
            this.lbInitialLayer.Size = new System.Drawing.Size(118, 50);
            this.lbInitialLayer.TabIndex = 11;
            this.lbInitialLayer.Text = "Layers";
            this.lbInitialLayer.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btnFindLayer);
            this.panel2.Controls.Add(this.btnLastLayer);
            this.panel2.Controls.Add(this.btnFirstLayer);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(3, 655);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(118, 26);
            this.panel2.TabIndex = 15;
            // 
            // btnFindLayer
            // 
            this.btnFindLayer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnFindLayer.Enabled = false;
            this.btnFindLayer.Image = global::PrusaSL1Viewer.Properties.Resources.search_16x16;
            this.btnFindLayer.Location = new System.Drawing.Point(37, 0);
            this.btnFindLayer.Name = "btnFindLayer";
            this.btnFindLayer.Size = new System.Drawing.Size(44, 26);
            this.btnFindLayer.TabIndex = 17;
            this.btnFindLayer.Text = "-";
            this.toolTipInformation.SetToolTip(this.btnFindLayer, "Go to a layer index [CTRL+F]");
            this.btnFindLayer.UseVisualStyleBackColor = true;
            this.btnFindLayer.Click += new System.EventHandler(this.EventClick);
            // 
            // btnLastLayer
            // 
            this.btnLastLayer.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnLastLayer.Enabled = false;
            this.btnLastLayer.Image = global::PrusaSL1Viewer.Properties.Resources.arrow_top_16x16;
            this.btnLastLayer.Location = new System.Drawing.Point(81, 0);
            this.btnLastLayer.Name = "btnLastLayer";
            this.btnLastLayer.Size = new System.Drawing.Size(37, 26);
            this.btnLastLayer.TabIndex = 16;
            this.btnLastLayer.Text = "-";
            this.toolTipInformation.SetToolTip(this.btnLastLayer, "Go to the last layer [END]");
            this.btnLastLayer.UseVisualStyleBackColor = true;
            this.btnLastLayer.Click += new System.EventHandler(this.EventClick);
            // 
            // btnFirstLayer
            // 
            this.btnFirstLayer.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnFirstLayer.Enabled = false;
            this.btnFirstLayer.Image = global::PrusaSL1Viewer.Properties.Resources.arrow_end_16x16;
            this.btnFirstLayer.Location = new System.Drawing.Point(0, 0);
            this.btnFirstLayer.Name = "btnFirstLayer";
            this.btnFirstLayer.Size = new System.Drawing.Size(37, 26);
            this.btnFirstLayer.TabIndex = 15;
            this.btnFirstLayer.Text = "-";
            this.toolTipInformation.SetToolTip(this.btnFirstLayer, "Go to the first layer [HOME]");
            this.btnFirstLayer.UseVisualStyleBackColor = true;
            this.btnFirstLayer.Click += new System.EventHandler(this.EventClick);
            // 
            // toolTipInformation
            // 
            this.toolTipInformation.AutoPopDelay = 10000;
            this.toolTipInformation.InitialDelay = 500;
            this.toolTipInformation.ReshowDelay = 100;
            this.toolTipInformation.ToolTipTitle = "Information";
            // 
            // layerScrollTimer
            // 
            this.layerScrollTimer.Interval = 150;
            this.layerScrollTimer.Tick += new System.EventHandler(this.EventTimerTick);
            // 
            // FrmMain
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1684, 786);
            this.Controls.Add(this.mainTable);
            this.Controls.Add(this.statusBar);
            this.Controls.Add(this.menu);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MainMenuStrip = this.menu;
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "FrmMain";
            this.Text = "PrusaSL1Viewer";
            this.menu.ResumeLayout(false);
            this.menu.PerformLayout();
            this.mainTable.ResumeLayout(false);
            this.scCenter.Panel1.ResumeLayout(false);
            this.scCenter.Panel1.PerformLayout();
            this.scCenter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scCenter)).EndInit();
            this.scCenter.ResumeLayout(false);
            this.tsLayer.ResumeLayout(false);
            this.tsLayer.PerformLayout();
            this.tabControlLeft.ResumeLayout(false);
            this.tbpThumbnailsAndInfo.ResumeLayout(false);
            this.scLeft.Panel1.ResumeLayout(false);
            this.scLeft.Panel1.PerformLayout();
            this.scLeft.Panel2.ResumeLayout(false);
            this.scLeft.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scLeft)).EndInit();
            this.scLeft.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbThumbnail)).EndInit();
            this.tsThumbnails.ResumeLayout(false);
            this.tsThumbnails.PerformLayout();
            this.tsProperties.ResumeLayout(false);
            this.tsProperties.PerformLayout();
            this.tabPageGCode.ResumeLayout(false);
            this.tabPageGCode.PerformLayout();
            this.tsGCode.ResumeLayout(false);
            this.tsGCode.PerformLayout();
            this.tabPageIssues.ResumeLayout(false);
            this.tabPageIssues.PerformLayout();
            this.tsIssues.ResumeLayout(false);
            this.tsIssues.PerformLayout();
            this.tlRight.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbLayer)).EndInit();
            this.panel2.ResumeLayout(false);
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
        private System.Windows.Forms.ToolStripMenuItem menuHelpWebsite;
        private System.Windows.Forms.ToolStripMenuItem menuHelpDonate;
        private System.Windows.Forms.ToolStripMenuItem menuHelpAbout;
        private System.Windows.Forms.TableLayoutPanel mainTable;
        private System.Windows.Forms.ToolStripMenuItem menuEdit;
        private System.Windows.Forms.SplitContainer scCenter;
        private System.Windows.Forms.ProgressBar pbLayers;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuFileClose;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem menuFileExtract;
        private System.Windows.Forms.ToolStripMenuItem menuFileConvert;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem menuFileReload;
        private System.Windows.Forms.ToolStripMenuItem menuFileSave;
        private System.Windows.Forms.ToolStripMenuItem menuFileSaveAs;
        private System.Windows.Forms.ToolStrip tsLayer;
        private System.Windows.Forms.ToolStripButton tsLayerImageExport;
        private System.Windows.Forms.ToolStripLabel tsLayerResolution;
        private System.Windows.Forms.TabControl tabControlLeft;
        private System.Windows.Forms.TabPage tbpThumbnailsAndInfo;
        private System.Windows.Forms.SplitContainer scLeft;
        private System.Windows.Forms.PictureBox pbThumbnail;
        private System.Windows.Forms.ToolStrip tsThumbnails;
        private System.Windows.Forms.ToolStripButton tsThumbnailsPrevious;
        private System.Windows.Forms.ToolStripLabel tsThumbnailsCount;
        private System.Windows.Forms.ToolStripButton tsThumbnailsNext;
        private System.Windows.Forms.ToolStripButton tsThumbnailsExport;
        private System.Windows.Forms.ToolStripLabel tsThumbnailsResolution;
        private System.Windows.Forms.TabPage tabPageGCode;
        private System.Windows.Forms.ImageList imageList16x16;
        private System.Windows.Forms.TextBox tbGCode;
        private System.Windows.Forms.ToolStrip tsGCode;
        private System.Windows.Forms.ToolStripLabel tsGCodeLabelLines;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripLabel tsGcodeLabelChars;
        private System.Windows.Forms.ToolStripButton tsGCodeButtonSave;
        private System.Windows.Forms.ListView lvProperties;
        private System.Windows.Forms.ColumnHeader lvChKey;
        private System.Windows.Forms.ColumnHeader lvChValue;
        private System.Windows.Forms.ToolStrip tsProperties;
        private System.Windows.Forms.ToolStripLabel tsPropertiesLabelCount;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripLabel tsPropertiesLabelGroups;
        private System.Windows.Forms.ToolStripButton tsPropertiesButtonSave;
        private System.Windows.Forms.ToolStripMenuItem menuFileOpenNewWindow;
        private System.Windows.Forms.ToolStripButton tsLayerImageRotate;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripButton tsLayerImageLayerDifference;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripLabel tsLayerPreviewTime;
        private Cyotek.Windows.Forms.ImageBox pbLayer;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripLabel tsLayerImageZoom;
        private System.Windows.Forms.ToolStripButton tsLayerImagePixelEdit;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripButton tsLayerImageLayerOutline;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
        private System.Windows.Forms.ToolStripMenuItem menuMutate;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
        private System.Windows.Forms.ToolStripMenuItem menuHelpInstallPrinters;
        private System.Windows.Forms.TabPage tabPageIssues;
        private System.Windows.Forms.ToolStrip tsIssues;
        private System.Windows.Forms.ToolStripButton tsIssuePrevious;
        private System.Windows.Forms.ToolStripButton tsIssueNext;
        private System.Windows.Forms.ToolStripLabel tsIssueCount;
        private System.Windows.Forms.ListView lvIssues;
        private System.Windows.Forms.ColumnHeader lvIssuesCount;
        private System.Windows.Forms.ColumnHeader lvIssuesXY;
        private System.Windows.Forms.ColumnHeader lvIssuesPixels;
        private System.Windows.Forms.ToolStripButton tsIsuesRefresh;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
        private System.Windows.Forms.ToolStripLabel tsLayerImageMouseLocation;
        private System.Windows.Forms.ColumnHeader lvIssuesLayerHeader;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
        private System.Windows.Forms.ToolStripButton tsIssueRemove;
        private System.Windows.Forms.ToolStripMenuItem menuTools;
        private System.Windows.Forms.ToolStripMenuItem menuToolsRepairLayers;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator13;
        private System.Windows.Forms.ToolStripButton tsIssuesRepair;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator14;
        private System.Windows.Forms.ToolStripButton tsLayerImageHighlightIssues;
        private System.Windows.Forms.TrackBar tbLayer;
        private System.Windows.Forms.TableLayoutPanel tlRight;
        private System.Windows.Forms.Label lbMaxLayer;
        private System.Windows.Forms.Label lbInitialLayer;
        private System.Windows.Forms.Button btnNextLayer;
        private System.Windows.Forms.Button btnPreviousLayer;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lbLayerActual;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnFirstLayer;
        private System.Windows.Forms.Button btnLastLayer;
        private System.Windows.Forms.Button btnFindLayer;
        private System.Windows.Forms.ToolTip toolTipInformation;
        private System.Windows.Forms.ColumnHeader lvIssuesType;
        private System.Windows.Forms.Timer layerScrollTimer;
    }
}

