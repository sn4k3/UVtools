using UVtools.GUI.Controls;

namespace UVtools.GUI
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
            this.menuFileSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator15 = new System.Windows.Forms.ToolStripSeparator();
            this.menuFileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuMutate = new System.Windows.Forms.ToolStripMenuItem();
            this.menuTools = new System.Windows.Forms.ToolStripMenuItem();
            this.menuToolsRepairLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.menuToolsPattern = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpWebsite = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpDonate = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpBenchmark = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
            this.menuHelpInstallPrinters = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuNewVersion = new System.Windows.Forms.ToolStripMenuItem();
            this.statusBar = new System.Windows.Forms.StatusStrip();
            this.mainTable = new System.Windows.Forms.TableLayoutPanel();
            this.scCenter = new System.Windows.Forms.SplitContainer();
            this.pbLayer = new Cyotek.Windows.Forms.ImageBox();
            this.tsLayer = new System.Windows.Forms.ToolStrip();
            this.btnLayerImageExport = new System.Windows.Forms.ToolStripSplitButton();
            this.btnLayerImageExportFile = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerImageExportClipboard = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerImageRotate = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.btnLayerImageLayerDifference = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator14 = new System.Windows.Forms.ToolStripSeparator();
            this.btnLayerImageHighlightIssues = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.btnLayerImageShowCrosshairs = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator25 = new System.Windows.Forms.ToolStripSeparator();
            this.btnLayerImageLayerOutline = new System.Windows.Forms.ToolStripSplitButton();
            this.btnLayerImageLayerOutlinePrintVolumeBounds = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerImageLayerOutlineLayerBounds = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerImageLayerOutlineHollowAreas = new System.Windows.Forms.ToolStripMenuItem();
            this.btnLayerImageLayerOutlineEdgeDetection = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            this.btnLayerImagePixelEdit = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator18 = new System.Windows.Forms.ToolStripSeparator();
            this.btnLayerImageActions = new System.Windows.Forms.ToolStripSplitButton();
            this.tsLayerInfo = new System.Windows.Forms.ToolStrip();
            this.tsLayerPreviewTime = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.tsLayerResolution = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
            this.tsLayerImageZoomLock = new System.Windows.Forms.ToolStripLabel();
            this.tsLayerImageZoom = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.tsLayerImagePanLocation = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator16 = new System.Windows.Forms.ToolStripSeparator();
            this.tsLayerImageMouseLocation = new System.Windows.Forms.ToolStripLabel();
            this.tsLayerImagePixelCount = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator17 = new System.Windows.Forms.ToolStripSeparator();
            this.tsLayerBounds = new System.Windows.Forms.ToolStripLabel();
            this.tabControlLeft = new System.Windows.Forms.TabControl();
            this.tbpThumbnailsAndInfo = new System.Windows.Forms.TabPage();
            this.scLeft = new System.Windows.Forms.SplitContainer();
            this.pbThumbnail = new System.Windows.Forms.PictureBox();
            this.tsThumbnails = new System.Windows.Forms.ToolStrip();
            this.tsThumbnailsPrevious = new System.Windows.Forms.ToolStripButton();
            this.tsThumbnailsCount = new System.Windows.Forms.ToolStripLabel();
            this.tsThumbnailsNext = new System.Windows.Forms.ToolStripButton();
            this.tsThumbnailsExport = new System.Windows.Forms.ToolStripSplitButton();
            this.tsThumbnailsExportFile = new System.Windows.Forms.ToolStripMenuItem();
            this.tsThumbnailsExportClipboard = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator21 = new System.Windows.Forms.ToolStripSeparator();
            this.tsThumbnailsImport = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator22 = new System.Windows.Forms.ToolStripSeparator();
            this.tsThumbnailsResolution = new System.Windows.Forms.ToolStripLabel();
            this.flvProperties = new BrightIdeasSoftware.FastObjectListView();
            this.tsProperties = new System.Windows.Forms.ToolStrip();
            this.tsPropertiesLabelCount = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.tsPropertiesLabelGroups = new System.Windows.Forms.ToolStripLabel();
            this.tsPropertiesExport = new System.Windows.Forms.ToolStripSplitButton();
            this.tsPropertiesExportFile = new System.Windows.Forms.ToolStripMenuItem();
            this.tsPropertiesExportClipboard = new System.Windows.Forms.ToolStripMenuItem();
            this.tabPageGCode = new System.Windows.Forms.TabPage();
            this.tbGCode = new System.Windows.Forms.TextBox();
            this.tsGCode = new System.Windows.Forms.ToolStrip();
            this.tsGCodeLabelLines = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsGcodeLabelChars = new System.Windows.Forms.ToolStripLabel();
            this.btnGCodeSave = new System.Windows.Forms.ToolStripSplitButton();
            this.btnGCodeSaveFile = new System.Windows.Forms.ToolStripMenuItem();
            this.btnGCodeSaveClipboard = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator24 = new System.Windows.Forms.ToolStripSeparator();
            this.btnGCodeRebuild = new System.Windows.Forms.ToolStripButton();
            this.tabPageIssues = new System.Windows.Forms.TabPage();
            this.flvIssues = new BrightIdeasSoftware.FastObjectListView();
            this.flvIssuesColType = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.flvIssuesColLayerIndex = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.flvIssuesColPosition = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.flvIssuesColPixels = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.tsIssuesLv = new System.Windows.Forms.ToolStrip();
            this.btnIssueGroup = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator23 = new System.Windows.Forms.ToolStripSeparator();
            this.btnIssueResort = new System.Windows.Forms.ToolStripButton();
            this.tsIssues = new System.Windows.Forms.ToolStrip();
            this.tsIssuePrevious = new System.Windows.Forms.ToolStripButton();
            this.tsIssueCount = new System.Windows.Forms.ToolStripLabel();
            this.tsIssueNext = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
            this.tsIssueRemove = new System.Windows.Forms.ToolStripButton();
            this.tsIssuesDetect = new System.Windows.Forms.ToolStripSplitButton();
            this.tsIssuesDetectIslands = new System.Windows.Forms.ToolStripMenuItem();
            this.tsIssuesDetectResinTraps = new System.Windows.Forms.ToolStripMenuItem();
            this.tsIssuesDetectTouchingBounds = new System.Windows.Forms.ToolStripMenuItem();
            this.tsIssuesDetectEmptyLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
            this.tsIssuesRepair = new System.Windows.Forms.ToolStripButton();
            this.tabPagePixelEditor = new System.Windows.Forms.TabPage();
            this.flvPixelHistory = new BrightIdeasSoftware.FastObjectListView();
            this.flvPixelHistoryColNumber = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.flvPixelHistoryColOperation = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvColumn3 = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvColumn4 = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.tsPixelEditorHistory = new System.Windows.Forms.ToolStrip();
            this.lbPixelHistoryOperations = new System.Windows.Forms.ToolStripLabel();
            this.btnPixelHistoryRemove = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator19 = new System.Windows.Forms.ToolStripSeparator();
            this.btnPixelHistoryApply = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator20 = new System.Windows.Forms.ToolStripSeparator();
            this.btnPixelHistoryClear = new System.Windows.Forms.ToolStripButton();
            this.tabControlPixelEditor = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label29 = new System.Windows.Forms.Label();
            this.label28 = new System.Windows.Forms.Label();
            this.nmPixelEditorDrawingThickness = new System.Windows.Forms.NumericUpDown();
            this.label27 = new System.Windows.Forms.Label();
            this.cbPixelEditorDrawingLineType = new System.Windows.Forms.ComboBox();
            this.label17 = new System.Windows.Forms.Label();
            this.nmPixelEditorDrawingLayersAbove = new System.Windows.Forms.NumericUpDown();
            this.label16 = new System.Windows.Forms.Label();
            this.nmPixelEditorDrawingLayersBelow = new System.Windows.Forms.NumericUpDown();
            this.label15 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.nmPixelEditorDrawingBrushSize = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cbPixelEditorDrawingBrushShape = new System.Windows.Forms.ComboBox();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.label21 = new System.Windows.Forms.Label();
            this.cbPixelEditorTextLineType = new System.Windows.Forms.ComboBox();
            this.cbPixelEditorTextMirror = new System.Windows.Forms.CheckBox();
            this.label26 = new System.Windows.Forms.Label();
            this.nmPixelEditorTextThickness = new System.Windows.Forms.NumericUpDown();
            this.label25 = new System.Windows.Forms.Label();
            this.tbPixelEditorTextText = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.nmPixelEditorTextLayersAbove = new System.Windows.Forms.NumericUpDown();
            this.label19 = new System.Windows.Forms.Label();
            this.nmPixelEditorTextLayersBelow = new System.Windows.Forms.NumericUpDown();
            this.label20 = new System.Windows.Forms.Label();
            this.nmPixelEditorTextFontScale = new System.Windows.Forms.NumericUpDown();
            this.label22 = new System.Windows.Forms.Label();
            this.panel6 = new System.Windows.Forms.Panel();
            this.label23 = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.cbPixelEditorTextFontFace = new System.Windows.Forms.ComboBox();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.label31 = new System.Windows.Forms.Label();
            this.nmPixelEditorEraserLayersAbove = new System.Windows.Forms.NumericUpDown();
            this.label32 = new System.Windows.Forms.Label();
            this.nmPixelEditorEraserLayersBelow = new System.Windows.Forms.NumericUpDown();
            this.label33 = new System.Windows.Forms.Label();
            this.panel7 = new System.Windows.Forms.Panel();
            this.label30 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.label10 = new System.Windows.Forms.Label();
            this.nmPixelEditorSupportsBaseDiameter = new System.Windows.Forms.NumericUpDown();
            this.label11 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.nmPixelEditorSupportsPillarDiameter = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.nmPixelEditorSupportsTipDiameter = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.panel4 = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.label13 = new System.Windows.Forms.Label();
            this.nmPixelEditorDrainHoleDiameter = new System.Windows.Forms.NumericUpDown();
            this.label14 = new System.Windows.Forms.Label();
            this.panel5 = new System.Windows.Forms.Panel();
            this.label12 = new System.Windows.Forms.Label();
            this.tabPageLog = new System.Windows.Forms.TabPage();
            this.lvLog = new BrightIdeasSoftware.FastObjectListView();
            this.tsLog = new System.Windows.Forms.ToolStrip();
            this.btnLogClear = new System.Windows.Forms.ToolStripButton();
            this.lbLogOperations = new System.Windows.Forms.ToolStripLabel();
            this.imageList16x16 = new System.Windows.Forms.ImageList(this.components);
            this.tlRight = new System.Windows.Forms.TableLayoutPanel();
            this.btnPreviousLayer = new System.Windows.Forms.Button();
            this.btnNextLayer = new System.Windows.Forms.Button();
            this.lbMaxLayer = new System.Windows.Forms.Label();
            this.panelLayerNavigation = new System.Windows.Forms.Panel();
            this.pbTrackerIssues = new System.Windows.Forms.PictureBox();
            this.lbActualLayer = new System.Windows.Forms.Label();
            this.tbLayer = new UVtools.GUI.Controls.TrackBarEx();
            this.lbInitialLayer = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnFindLayer = new System.Windows.Forms.Button();
            this.btnLastLayer = new System.Windows.Forms.Button();
            this.btnFirstLayer = new System.Windows.Forms.Button();
            this.layerZoomTimer = new System.Windows.Forms.Timer(this.components);
            this.issueScrollTimer = new System.Windows.Forms.Timer(this.components);
            this.toolTipInformation = new System.Windows.Forms.ToolTip(this.components);
            this.layerScrollTimer = new System.Windows.Forms.Timer(this.components);
            this.mouseHoldTimer = new System.Windows.Forms.Timer(this.components);
            this.menu.SuspendLayout();
            this.mainTable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scCenter)).BeginInit();
            this.scCenter.Panel1.SuspendLayout();
            this.scCenter.Panel2.SuspendLayout();
            this.scCenter.SuspendLayout();
            this.tsLayer.SuspendLayout();
            this.tsLayerInfo.SuspendLayout();
            this.tabControlLeft.SuspendLayout();
            this.tbpThumbnailsAndInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scLeft)).BeginInit();
            this.scLeft.Panel1.SuspendLayout();
            this.scLeft.Panel2.SuspendLayout();
            this.scLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbThumbnail)).BeginInit();
            this.tsThumbnails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.flvProperties)).BeginInit();
            this.tsProperties.SuspendLayout();
            this.tabPageGCode.SuspendLayout();
            this.tsGCode.SuspendLayout();
            this.tabPageIssues.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.flvIssues)).BeginInit();
            this.tsIssuesLv.SuspendLayout();
            this.tsIssues.SuspendLayout();
            this.tabPagePixelEditor.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.flvPixelHistory)).BeginInit();
            this.tsPixelEditorHistory.SuspendLayout();
            this.tabControlPixelEditor.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorDrawingThickness)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorDrawingLayersAbove)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorDrawingLayersBelow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorDrawingBrushSize)).BeginInit();
            this.panel3.SuspendLayout();
            this.tabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorTextThickness)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorTextLayersAbove)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorTextLayersBelow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorTextFontScale)).BeginInit();
            this.panel6.SuspendLayout();
            this.tabPage5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorEraserLayersAbove)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorEraserLayersBelow)).BeginInit();
            this.panel7.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorSupportsBaseDiameter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorSupportsPillarDiameter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorSupportsTipDiameter)).BeginInit();
            this.panel4.SuspendLayout();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorDrainHoleDiameter)).BeginInit();
            this.panel5.SuspendLayout();
            this.tabPageLog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lvLog)).BeginInit();
            this.tsLog.SuspendLayout();
            this.tlRight.SuspendLayout();
            this.panelLayerNavigation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbTrackerIssues)).BeginInit();
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
            this.helpToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.menuNewVersion});
            this.menu.Location = new System.Drawing.Point(0, 0);
            this.menu.Name = "menu";
            this.menu.Size = new System.Drawing.Size(1732, 24);
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
            this.menuFileSettings,
            this.toolStripSeparator15,
            this.menuFileExit});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // menuFileOpen
            // 
            this.menuFileOpen.Image = global::UVtools.GUI.Properties.Resources.Open_16x16;
            this.menuFileOpen.Name = "menuFileOpen";
            this.menuFileOpen.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.menuFileOpen.Size = new System.Drawing.Size(261, 22);
            this.menuFileOpen.Text = "&Open";
            this.menuFileOpen.Click += new System.EventHandler(this.EventClick);
            // 
            // menuFileOpenNewWindow
            // 
            this.menuFileOpenNewWindow.Image = global::UVtools.GUI.Properties.Resources.Open_16x16;
            this.menuFileOpenNewWindow.Name = "menuFileOpenNewWindow";
            this.menuFileOpenNewWindow.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.O)));
            this.menuFileOpenNewWindow.Size = new System.Drawing.Size(261, 22);
            this.menuFileOpenNewWindow.Text = "Open in new window";
            this.menuFileOpenNewWindow.Click += new System.EventHandler(this.EventClick);
            // 
            // menuFileReload
            // 
            this.menuFileReload.Image = global::UVtools.GUI.Properties.Resources.File_Refresh_16x16;
            this.menuFileReload.Name = "menuFileReload";
            this.menuFileReload.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F5)));
            this.menuFileReload.Size = new System.Drawing.Size(261, 22);
            this.menuFileReload.Text = "&Reload";
            this.menuFileReload.Click += new System.EventHandler(this.EventClick);
            // 
            // menuFileSave
            // 
            this.menuFileSave.Enabled = false;
            this.menuFileSave.Image = global::UVtools.GUI.Properties.Resources.Save_16x16;
            this.menuFileSave.Name = "menuFileSave";
            this.menuFileSave.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.menuFileSave.Size = new System.Drawing.Size(261, 22);
            this.menuFileSave.Text = "&Save";
            this.menuFileSave.Click += new System.EventHandler(this.EventClick);
            // 
            // menuFileSaveAs
            // 
            this.menuFileSaveAs.Enabled = false;
            this.menuFileSaveAs.Image = global::UVtools.GUI.Properties.Resources.SaveAs_16x16;
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
            this.menuFileClose.Image = global::UVtools.GUI.Properties.Resources.File_Close_16x16;
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
            this.menuFileExtract.Image = global::UVtools.GUI.Properties.Resources.Extract_object_16x16;
            this.menuFileExtract.Name = "menuFileExtract";
            this.menuFileExtract.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.menuFileExtract.Size = new System.Drawing.Size(261, 22);
            this.menuFileExtract.Text = "&Extract";
            this.menuFileExtract.Click += new System.EventHandler(this.EventClick);
            // 
            // menuFileConvert
            // 
            this.menuFileConvert.Enabled = false;
            this.menuFileConvert.Image = global::UVtools.GUI.Properties.Resources.Convert_16x16;
            this.menuFileConvert.Name = "menuFileConvert";
            this.menuFileConvert.Size = new System.Drawing.Size(261, 22);
            this.menuFileConvert.Text = "&Convert To";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(258, 6);
            // 
            // menuFileSettings
            // 
            this.menuFileSettings.Image = global::UVtools.GUI.Properties.Resources.settings_16x16;
            this.menuFileSettings.Name = "menuFileSettings";
            this.menuFileSettings.ShortcutKeys = System.Windows.Forms.Keys.F12;
            this.menuFileSettings.Size = new System.Drawing.Size(261, 22);
            this.menuFileSettings.Text = "&Settings";
            this.menuFileSettings.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator15
            // 
            this.toolStripSeparator15.Name = "toolStripSeparator15";
            this.toolStripSeparator15.Size = new System.Drawing.Size(258, 6);
            // 
            // menuFileExit
            // 
            this.menuFileExit.Image = global::UVtools.GUI.Properties.Resources.Exit_16x16;
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
            this.menuToolsRepairLayers,
            this.menuToolsPattern});
            this.menuTools.Enabled = false;
            this.menuTools.Name = "menuTools";
            this.menuTools.Size = new System.Drawing.Size(46, 20);
            this.menuTools.Text = "&Tools";
            // 
            // menuToolsRepairLayers
            // 
            this.menuToolsRepairLayers.Enabled = false;
            this.menuToolsRepairLayers.Image = global::UVtools.GUI.Properties.Resources.toolbox_16x16;
            this.menuToolsRepairLayers.Name = "menuToolsRepairLayers";
            this.menuToolsRepairLayers.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt) 
            | System.Windows.Forms.Keys.R)));
            this.menuToolsRepairLayers.Size = new System.Drawing.Size(261, 22);
            this.menuToolsRepairLayers.Text = "&Repair layers and Issues";
            this.menuToolsRepairLayers.Click += new System.EventHandler(this.EventClick);
            // 
            // menuToolsPattern
            // 
            this.menuToolsPattern.Enabled = false;
            this.menuToolsPattern.Image = global::UVtools.GUI.Properties.Resources.pattern_16x16;
            this.menuToolsPattern.Name = "menuToolsPattern";
            this.menuToolsPattern.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt) 
            | System.Windows.Forms.Keys.P)));
            this.menuToolsPattern.Size = new System.Drawing.Size(261, 22);
            this.menuToolsPattern.Text = "&Pattern";
            this.menuToolsPattern.Click += new System.EventHandler(this.EventClick);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuHelpWebsite,
            this.menuHelpDonate,
            this.menuHelpAbout,
            this.menuHelpBenchmark,
            this.toolStripSeparator10,
            this.menuHelpInstallPrinters});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // menuHelpWebsite
            // 
            this.menuHelpWebsite.Image = global::UVtools.GUI.Properties.Resources.internet_explorer_16x16;
            this.menuHelpWebsite.Name = "menuHelpWebsite";
            this.menuHelpWebsite.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F1)));
            this.menuHelpWebsite.Size = new System.Drawing.Size(231, 22);
            this.menuHelpWebsite.Text = "&Website";
            this.menuHelpWebsite.Click += new System.EventHandler(this.EventClick);
            // 
            // menuHelpDonate
            // 
            this.menuHelpDonate.Image = global::UVtools.GUI.Properties.Resources.Donate_16x16;
            this.menuHelpDonate.Name = "menuHelpDonate";
            this.menuHelpDonate.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.F1)));
            this.menuHelpDonate.Size = new System.Drawing.Size(231, 22);
            this.menuHelpDonate.Text = "&Donate";
            this.menuHelpDonate.Click += new System.EventHandler(this.EventClick);
            // 
            // menuHelpAbout
            // 
            this.menuHelpAbout.Image = global::UVtools.GUI.Properties.Resources.Button_Info_16x16;
            this.menuHelpAbout.Name = "menuHelpAbout";
            this.menuHelpAbout.ShortcutKeys = System.Windows.Forms.Keys.F1;
            this.menuHelpAbout.Size = new System.Drawing.Size(231, 22);
            this.menuHelpAbout.Text = "&About";
            this.menuHelpAbout.Click += new System.EventHandler(this.EventClick);
            // 
            // menuHelpBenchmark
            // 
            this.menuHelpBenchmark.Image = global::UVtools.GUI.Properties.Resources.microchip_16x16;
            this.menuHelpBenchmark.Name = "menuHelpBenchmark";
            this.menuHelpBenchmark.Size = new System.Drawing.Size(231, 22);
            this.menuHelpBenchmark.Text = "&Benchmark";
            this.menuHelpBenchmark.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator10
            // 
            this.toolStripSeparator10.Name = "toolStripSeparator10";
            this.toolStripSeparator10.Size = new System.Drawing.Size(228, 6);
            // 
            // menuHelpInstallPrinters
            // 
            this.menuHelpInstallPrinters.Image = global::UVtools.GUI.Properties.Resources.CNCMachine_16x16;
            this.menuHelpInstallPrinters.Name = "menuHelpInstallPrinters";
            this.menuHelpInstallPrinters.Size = new System.Drawing.Size(231, 22);
            this.menuHelpInstallPrinters.Text = "Install profiles into PrusaSlicer";
            this.menuHelpInstallPrinters.Click += new System.EventHandler(this.EventClick);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "&View";
            this.viewToolStripMenuItem.Visible = false;
            // 
            // menuNewVersion
            // 
            this.menuNewVersion.BackColor = System.Drawing.Color.Lime;
            this.menuNewVersion.Name = "menuNewVersion";
            this.menuNewVersion.Size = new System.Drawing.Size(147, 20);
            this.menuNewVersion.Text = "New version is available!";
            this.menuNewVersion.Visible = false;
            this.menuNewVersion.Click += new System.EventHandler(this.EventClick);
            // 
            // statusBar
            // 
            this.statusBar.Location = new System.Drawing.Point(0, 789);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(1732, 22);
            this.statusBar.TabIndex = 1;
            this.statusBar.Text = "statusStrip1";
            // 
            // mainTable
            // 
            this.mainTable.ColumnCount = 3;
            this.mainTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 400F));
            this.mainTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.mainTable.Controls.Add(this.scCenter, 1, 0);
            this.mainTable.Controls.Add(this.tabControlLeft, 0, 0);
            this.mainTable.Controls.Add(this.tlRight, 2, 0);
            this.mainTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTable.Location = new System.Drawing.Point(0, 24);
            this.mainTable.Name = "mainTable";
            this.mainTable.RowCount = 1;
            this.mainTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTable.Size = new System.Drawing.Size(1732, 765);
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
            this.scCenter.Panel2.Controls.Add(this.tsLayerInfo);
            this.scCenter.Panel2MinSize = 18;
            this.scCenter.Size = new System.Drawing.Size(1176, 759);
            this.scCenter.SplitterDistance = 730;
            this.scCenter.TabIndex = 4;
            // 
            // pbLayer
            // 
            this.pbLayer.AllowDoubleClick = true;
            this.pbLayer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbLayer.GridScale = Cyotek.Windows.Forms.ImageBoxGridScale.Large;
            this.pbLayer.Location = new System.Drawing.Point(0, 25);
            this.pbLayer.Name = "pbLayer";
            this.pbLayer.PanMode = Cyotek.Windows.Forms.ImageBoxPanMode.Left;
            this.pbLayer.ShowPixelGrid = true;
            this.pbLayer.Size = new System.Drawing.Size(1176, 705);
            this.pbLayer.TabIndex = 7;
            this.pbLayer.Zoomed += new System.EventHandler<Cyotek.Windows.Forms.ImageBoxZoomEventArgs>(this.pbLayer_Zoomed);
            this.pbLayer.MouseClick += new System.Windows.Forms.MouseEventHandler(this.EventMouseClick);
            this.pbLayer.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.EventMouseDoubleClick);
            this.pbLayer.MouseDown += new System.Windows.Forms.MouseEventHandler(this.EventMouseDown);
            this.pbLayer.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pbLayer_MouseMove);
            this.pbLayer.MouseUp += new System.Windows.Forms.MouseEventHandler(this.EventMouseUp);
            // 
            // tsLayer
            // 
            this.tsLayer.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsLayer.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnLayerImageExport,
            this.btnLayerImageRotate,
            this.toolStripSeparator5,
            this.btnLayerImageLayerDifference,
            this.toolStripSeparator14,
            this.btnLayerImageHighlightIssues,
            this.toolStripSeparator7,
            this.btnLayerImageShowCrosshairs,
            this.toolStripSeparator25,
            this.btnLayerImageLayerOutline,
            this.toolStripSeparator9,
            this.btnLayerImagePixelEdit,
            this.toolStripSeparator18,
            this.btnLayerImageActions});
            this.tsLayer.Location = new System.Drawing.Point(0, 0);
            this.tsLayer.Name = "tsLayer";
            this.tsLayer.Size = new System.Drawing.Size(1176, 25);
            this.tsLayer.TabIndex = 6;
            this.tsLayer.Text = "Layer Menu";
            // 
            // btnLayerImageExport
            // 
            this.btnLayerImageExport.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnLayerImageExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnLayerImageExport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnLayerImageExportFile,
            this.btnLayerImageExportClipboard});
            this.btnLayerImageExport.Image = global::UVtools.GUI.Properties.Resources.Save_16x16;
            this.btnLayerImageExport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnLayerImageExport.Name = "btnLayerImageExport";
            this.btnLayerImageExport.Size = new System.Drawing.Size(32, 22);
            this.btnLayerImageExport.Text = "Save to";
            this.btnLayerImageExport.ToolTipText = "Save layer image to a file or clipboard";
            this.btnLayerImageExport.ButtonClick += new System.EventHandler(this.EventClick);
            // 
            // btnLayerImageExportFile
            // 
            this.btnLayerImageExportFile.Image = global::UVtools.GUI.Properties.Resources.file_image_16x16;
            this.btnLayerImageExportFile.Name = "btnLayerImageExportFile";
            this.btnLayerImageExportFile.Size = new System.Drawing.Size(141, 22);
            this.btnLayerImageExportFile.Text = "To &File";
            this.btnLayerImageExportFile.Click += new System.EventHandler(this.EventClick);
            // 
            // btnLayerImageExportClipboard
            // 
            this.btnLayerImageExportClipboard.Image = global::UVtools.GUI.Properties.Resources.clipboard_16x16;
            this.btnLayerImageExportClipboard.Name = "btnLayerImageExportClipboard";
            this.btnLayerImageExportClipboard.Size = new System.Drawing.Size(141, 22);
            this.btnLayerImageExportClipboard.Text = "To &Clipboard";
            this.btnLayerImageExportClipboard.Click += new System.EventHandler(this.EventClick);
            // 
            // btnLayerImageRotate
            // 
            this.btnLayerImageRotate.Checked = true;
            this.btnLayerImageRotate.CheckOnClick = true;
            this.btnLayerImageRotate.CheckState = System.Windows.Forms.CheckState.Checked;
            this.btnLayerImageRotate.Image = global::UVtools.GUI.Properties.Resources.Rotate_16x16;
            this.btnLayerImageRotate.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnLayerImageRotate.Name = "btnLayerImageRotate";
            this.btnLayerImageRotate.Size = new System.Drawing.Size(117, 22);
            this.btnLayerImageRotate.Text = "&Rotate Image 90º";
            this.btnLayerImageRotate.ToolTipText = "Auto rotate layer preview image at 90º (This can slow down the layer preview) [CT" +
    "RL+R]";
            this.btnLayerImageRotate.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(6, 25);
            // 
            // btnLayerImageLayerDifference
            // 
            this.btnLayerImageLayerDifference.CheckOnClick = true;
            this.btnLayerImageLayerDifference.Image = global::UVtools.GUI.Properties.Resources.layers_16x16;
            this.btnLayerImageLayerDifference.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnLayerImageLayerDifference.Name = "btnLayerImageLayerDifference";
            this.btnLayerImageLayerDifference.Size = new System.Drawing.Size(81, 22);
            this.btnLayerImageLayerDifference.Text = "&Difference";
            this.btnLayerImageLayerDifference.ToolTipText = "Show layer differences where daker pixels were also present on previous layer and" +
    " the white pixels the difference between previous and current layer.";
            this.btnLayerImageLayerDifference.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator14
            // 
            this.toolStripSeparator14.Name = "toolStripSeparator14";
            this.toolStripSeparator14.Size = new System.Drawing.Size(6, 25);
            // 
            // btnLayerImageHighlightIssues
            // 
            this.btnLayerImageHighlightIssues.Checked = true;
            this.btnLayerImageHighlightIssues.CheckOnClick = true;
            this.btnLayerImageHighlightIssues.CheckState = System.Windows.Forms.CheckState.Checked;
            this.btnLayerImageHighlightIssues.Image = global::UVtools.GUI.Properties.Resources.warning_16x16;
            this.btnLayerImageHighlightIssues.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnLayerImageHighlightIssues.Name = "btnLayerImageHighlightIssues";
            this.btnLayerImageHighlightIssues.Size = new System.Drawing.Size(58, 22);
            this.btnLayerImageHighlightIssues.Text = "&Issues";
            this.btnLayerImageHighlightIssues.ToolTipText = "Highlight Issues on current layer.\r\nValid only if Issues are calculated.";
            this.btnLayerImageHighlightIssues.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            this.toolStripSeparator7.Size = new System.Drawing.Size(6, 25);
            // 
            // btnLayerImageShowCrosshairs
            // 
            this.btnLayerImageShowCrosshairs.Checked = true;
            this.btnLayerImageShowCrosshairs.CheckOnClick = true;
            this.btnLayerImageShowCrosshairs.CheckState = System.Windows.Forms.CheckState.Checked;
            this.btnLayerImageShowCrosshairs.Image = global::UVtools.GUI.Properties.Resources.crosshairs_16x16;
            this.btnLayerImageShowCrosshairs.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnLayerImageShowCrosshairs.Name = "btnLayerImageShowCrosshairs";
            this.btnLayerImageShowCrosshairs.Size = new System.Drawing.Size(81, 22);
            this.btnLayerImageShowCrosshairs.Text = "&Crosshairs";
            this.btnLayerImageShowCrosshairs.ToolTipText = "Show crosshairs for selected issues on the current layer.";
            this.btnLayerImageShowCrosshairs.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator25
            // 
            this.toolStripSeparator25.Name = "toolStripSeparator25";
            this.toolStripSeparator25.Size = new System.Drawing.Size(6, 25);
            // 
            // btnLayerImageLayerOutline
            // 
            this.btnLayerImageLayerOutline.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnLayerImageLayerOutlinePrintVolumeBounds,
            this.btnLayerImageLayerOutlineLayerBounds,
            this.btnLayerImageLayerOutlineHollowAreas,
            this.btnLayerImageLayerOutlineEdgeDetection});
            this.btnLayerImageLayerOutline.Image = global::UVtools.GUI.Properties.Resources.Geometry_16x16;
            this.btnLayerImageLayerOutline.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnLayerImageLayerOutline.Name = "btnLayerImageLayerOutline";
            this.btnLayerImageLayerOutline.Size = new System.Drawing.Size(78, 22);
            this.btnLayerImageLayerOutline.Text = "&Outline";
            this.btnLayerImageLayerOutline.ButtonClick += new System.EventHandler(this.EventClick);
            // 
            // btnLayerImageLayerOutlinePrintVolumeBounds
            // 
            this.btnLayerImageLayerOutlinePrintVolumeBounds.CheckOnClick = true;
            this.btnLayerImageLayerOutlinePrintVolumeBounds.Name = "btnLayerImageLayerOutlinePrintVolumeBounds";
            this.btnLayerImageLayerOutlinePrintVolumeBounds.Size = new System.Drawing.Size(196, 22);
            this.btnLayerImageLayerOutlinePrintVolumeBounds.Text = "Print Volume Boundary";
            this.btnLayerImageLayerOutlinePrintVolumeBounds.Click += new System.EventHandler(this.EventClick);
            // 
            // btnLayerImageLayerOutlineLayerBounds
            // 
            this.btnLayerImageLayerOutlineLayerBounds.CheckOnClick = true;
            this.btnLayerImageLayerOutlineLayerBounds.Name = "btnLayerImageLayerOutlineLayerBounds";
            this.btnLayerImageLayerOutlineLayerBounds.Size = new System.Drawing.Size(196, 22);
            this.btnLayerImageLayerOutlineLayerBounds.Text = "Layer Boundary";
            this.btnLayerImageLayerOutlineLayerBounds.Click += new System.EventHandler(this.EventClick);
            // 
            // btnLayerImageLayerOutlineHollowAreas
            // 
            this.btnLayerImageLayerOutlineHollowAreas.CheckOnClick = true;
            this.btnLayerImageLayerOutlineHollowAreas.Name = "btnLayerImageLayerOutlineHollowAreas";
            this.btnLayerImageLayerOutlineHollowAreas.Size = new System.Drawing.Size(196, 22);
            this.btnLayerImageLayerOutlineHollowAreas.Text = "Hollow Areas";
            this.btnLayerImageLayerOutlineHollowAreas.Click += new System.EventHandler(this.EventClick);
            // 
            // btnLayerImageLayerOutlineEdgeDetection
            // 
            this.btnLayerImageLayerOutlineEdgeDetection.CheckOnClick = true;
            this.btnLayerImageLayerOutlineEdgeDetection.Name = "btnLayerImageLayerOutlineEdgeDetection";
            this.btnLayerImageLayerOutlineEdgeDetection.Size = new System.Drawing.Size(196, 22);
            this.btnLayerImageLayerOutlineEdgeDetection.Text = "&Edge Detection";
            this.btnLayerImageLayerOutlineEdgeDetection.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator9
            // 
            this.toolStripSeparator9.Name = "toolStripSeparator9";
            this.toolStripSeparator9.Size = new System.Drawing.Size(6, 25);
            // 
            // btnLayerImagePixelEdit
            // 
            this.btnLayerImagePixelEdit.CheckOnClick = true;
            this.btnLayerImagePixelEdit.Image = global::UVtools.GUI.Properties.Resources.pixel_16x16;
            this.btnLayerImagePixelEdit.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnLayerImagePixelEdit.Name = "btnLayerImagePixelEdit";
            this.btnLayerImagePixelEdit.Size = new System.Drawing.Size(75, 22);
            this.btnLayerImagePixelEdit.Text = "Pixel &Edit";
            this.btnLayerImagePixelEdit.ToolTipText = "Edit layer image: Draw pixels, add supports and/or drain holes";
            this.btnLayerImagePixelEdit.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator18
            // 
            this.toolStripSeparator18.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator18.Name = "toolStripSeparator18";
            this.toolStripSeparator18.Size = new System.Drawing.Size(6, 25);
            // 
            // btnLayerImageActions
            // 
            this.btnLayerImageActions.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnLayerImageActions.Image = global::UVtools.GUI.Properties.Resources.layers_alt_16x16;
            this.btnLayerImageActions.Name = "btnLayerImageActions";
            this.btnLayerImageActions.Size = new System.Drawing.Size(79, 22);
            this.btnLayerImageActions.Text = "Actions";
            this.btnLayerImageActions.ButtonClick += new System.EventHandler(this.EventClick);
            // 
            // tsLayerInfo
            // 
            this.tsLayerInfo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tsLayerInfo.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsLayerInfo.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsLayerPreviewTime,
            this.toolStripSeparator6,
            this.tsLayerResolution,
            this.toolStripSeparator11,
            this.tsLayerImageZoomLock,
            this.tsLayerImageZoom,
            this.toolStripSeparator8,
            this.tsLayerImagePanLocation,
            this.toolStripSeparator16,
            this.tsLayerImageMouseLocation,
            this.tsLayerImagePixelCount,
            this.toolStripSeparator17,
            this.tsLayerBounds});
            this.tsLayerInfo.Location = new System.Drawing.Point(0, 0);
            this.tsLayerInfo.Name = "tsLayerInfo";
            this.tsLayerInfo.Size = new System.Drawing.Size(1176, 25);
            this.tsLayerInfo.TabIndex = 9;
            this.tsLayerInfo.Text = "tsLayerInfo";
            // 
            // tsLayerPreviewTime
            // 
            this.tsLayerPreviewTime.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsLayerPreviewTime.Name = "tsLayerPreviewTime";
            this.tsLayerPreviewTime.Size = new System.Drawing.Size(77, 22);
            this.tsLayerPreviewTime.Text = "Preview Time";
            this.tsLayerPreviewTime.ToolTipText = "Layer preview computation time";
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(6, 25);
            // 
            // tsLayerResolution
            // 
            this.tsLayerResolution.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsLayerResolution.Image = global::UVtools.GUI.Properties.Resources.expand_16x16;
            this.tsLayerResolution.Name = "tsLayerResolution";
            this.tsLayerResolution.Size = new System.Drawing.Size(79, 22);
            this.tsLayerResolution.Text = "Resolution";
            this.tsLayerResolution.ToolTipText = "Layer Resolution";
            // 
            // toolStripSeparator11
            // 
            this.toolStripSeparator11.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator11.Name = "toolStripSeparator11";
            this.toolStripSeparator11.Size = new System.Drawing.Size(6, 25);
            // 
            // tsLayerImageZoomLock
            // 
            this.tsLayerImageZoomLock.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsLayerImageZoomLock.Image = global::UVtools.GUI.Properties.Resources.Lock_16x16;
            this.tsLayerImageZoomLock.Name = "tsLayerImageZoomLock";
            this.tsLayerImageZoomLock.Size = new System.Drawing.Size(27, 22);
            this.tsLayerImageZoomLock.Text = "]";
            this.tsLayerImageZoomLock.ToolTipText = "This zoom factor will be used for auto-zoom functions. Use scroll wheel to select" +
    " desired auto zoom level\r\nand hold middle mouse button for 1 second to set.";
            this.tsLayerImageZoomLock.Visible = false;
            // 
            // tsLayerImageZoom
            // 
            this.tsLayerImageZoom.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsLayerImageZoom.Image = global::UVtools.GUI.Properties.Resources.search_16x16;
            this.tsLayerImageZoom.Name = "tsLayerImageZoom";
            this.tsLayerImageZoom.Size = new System.Drawing.Size(81, 22);
            this.tsLayerImageZoom.Text = "Zoom: [1x]";
            this.tsLayerImageZoom.ToolTipText = "Layer image zoom level, use mouse scroll to zoom in/out into image\r\nCtrl + 0 OR d" +
    "ouble right click to scale to fit";
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            this.toolStripSeparator8.Size = new System.Drawing.Size(6, 25);
            this.toolStripSeparator8.Visible = false;
            // 
            // tsLayerImagePanLocation
            // 
            this.tsLayerImagePanLocation.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsLayerImagePanLocation.Image = global::UVtools.GUI.Properties.Resources.map_marker_16x16;
            this.tsLayerImagePanLocation.Name = "tsLayerImagePanLocation";
            this.tsLayerImagePanLocation.Size = new System.Drawing.Size(79, 22);
            this.tsLayerImagePanLocation.Text = "{X=0, Y=0}";
            this.tsLayerImagePanLocation.ToolTipText = "Image pan location";
            this.tsLayerImagePanLocation.Visible = false;
            // 
            // toolStripSeparator16
            // 
            this.toolStripSeparator16.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator16.Name = "toolStripSeparator16";
            this.toolStripSeparator16.Size = new System.Drawing.Size(6, 25);
            // 
            // tsLayerImageMouseLocation
            // 
            this.tsLayerImageMouseLocation.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsLayerImageMouseLocation.Image = global::UVtools.GUI.Properties.Resources.map_marker_16x16;
            this.tsLayerImageMouseLocation.Name = "tsLayerImageMouseLocation";
            this.tsLayerImageMouseLocation.Size = new System.Drawing.Size(79, 22);
            this.tsLayerImageMouseLocation.Text = "{X=0, Y=0}";
            this.tsLayerImageMouseLocation.ToolTipText = "Mouse over pixel location and pixel brightness\r\nUse SHIFT while move mouse hover " +
    "pixels";
            // 
            // tsLayerImagePixelCount
            // 
            this.tsLayerImagePixelCount.Image = global::UVtools.GUI.Properties.Resources.pixel_16x16;
            this.tsLayerImagePixelCount.Name = "tsLayerImagePixelCount";
            this.tsLayerImagePixelCount.Size = new System.Drawing.Size(65, 22);
            this.tsLayerImagePixelCount.Text = "Pixels: 0";
            this.tsLayerImagePixelCount.ToolTipText = "Number of pixels to cure on this layer image and the percetange of them against t" +
    "otal lcd pixels";
            // 
            // toolStripSeparator17
            // 
            this.toolStripSeparator17.Name = "toolStripSeparator17";
            this.toolStripSeparator17.Size = new System.Drawing.Size(6, 25);
            // 
            // tsLayerBounds
            // 
            this.tsLayerBounds.Image = global::UVtools.GUI.Properties.Resources.expand_16x16;
            this.tsLayerBounds.Name = "tsLayerBounds";
            this.tsLayerBounds.Size = new System.Drawing.Size(66, 22);
            this.tsLayerBounds.Text = "Bounds:";
            this.tsLayerBounds.ToolTipText = "Image bounds for this layer only, position and size";
            // 
            // tabControlLeft
            // 
            this.tabControlLeft.Controls.Add(this.tbpThumbnailsAndInfo);
            this.tabControlLeft.Controls.Add(this.tabPageGCode);
            this.tabControlLeft.Controls.Add(this.tabPageIssues);
            this.tabControlLeft.Controls.Add(this.tabPagePixelEditor);
            this.tabControlLeft.Controls.Add(this.tabPageLog);
            this.tabControlLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlLeft.ImageList = this.imageList16x16;
            this.tabControlLeft.ItemSize = new System.Drawing.Size(130, 19);
            this.tabControlLeft.Location = new System.Drawing.Point(3, 3);
            this.tabControlLeft.Multiline = true;
            this.tabControlLeft.Name = "tabControlLeft";
            this.tabControlLeft.SelectedIndex = 0;
            this.tabControlLeft.Size = new System.Drawing.Size(394, 759);
            this.tabControlLeft.TabIndex = 5;
            this.tabControlLeft.SelectedIndexChanged += new System.EventHandler(this.EventSelectedIndexChanged);
            // 
            // tbpThumbnailsAndInfo
            // 
            this.tbpThumbnailsAndInfo.Controls.Add(this.scLeft);
            this.tbpThumbnailsAndInfo.ImageKey = "Button-Info-16x16.png";
            this.tbpThumbnailsAndInfo.Location = new System.Drawing.Point(4, 42);
            this.tbpThumbnailsAndInfo.Name = "tbpThumbnailsAndInfo";
            this.tbpThumbnailsAndInfo.Padding = new System.Windows.Forms.Padding(3);
            this.tbpThumbnailsAndInfo.Size = new System.Drawing.Size(386, 713);
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
            this.scLeft.Panel2.Controls.Add(this.flvProperties);
            this.scLeft.Panel2.Controls.Add(this.tsProperties);
            this.scLeft.Size = new System.Drawing.Size(380, 707);
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
            this.toolStripSeparator21,
            this.tsThumbnailsImport,
            this.toolStripSeparator22,
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
            this.tsThumbnailsPrevious.Image = global::UVtools.GUI.Properties.Resources.Back_16x16;
            this.tsThumbnailsPrevious.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsThumbnailsPrevious.Name = "tsThumbnailsPrevious";
            this.tsThumbnailsPrevious.Size = new System.Drawing.Size(23, 22);
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
            this.tsThumbnailsNext.Image = global::UVtools.GUI.Properties.Resources.Next_16x16;
            this.tsThumbnailsNext.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsThumbnailsNext.Name = "tsThumbnailsNext";
            this.tsThumbnailsNext.Size = new System.Drawing.Size(23, 22);
            this.tsThumbnailsNext.ToolTipText = "Show next thumbnail";
            this.tsThumbnailsNext.Click += new System.EventHandler(this.EventClick);
            // 
            // tsThumbnailsExport
            // 
            this.tsThumbnailsExport.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsThumbnailsExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsThumbnailsExport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsThumbnailsExportFile,
            this.tsThumbnailsExportClipboard});
            this.tsThumbnailsExport.Enabled = false;
            this.tsThumbnailsExport.Image = global::UVtools.GUI.Properties.Resources.Save_16x16;
            this.tsThumbnailsExport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsThumbnailsExport.Name = "tsThumbnailsExport";
            this.tsThumbnailsExport.Size = new System.Drawing.Size(32, 22);
            this.tsThumbnailsExport.Text = "Save to";
            this.tsThumbnailsExport.ToolTipText = "Save thumbnail image to a file or clipboard";
            this.tsThumbnailsExport.ButtonClick += new System.EventHandler(this.EventClick);
            // 
            // tsThumbnailsExportFile
            // 
            this.tsThumbnailsExportFile.Image = global::UVtools.GUI.Properties.Resources.file_image_16x16;
            this.tsThumbnailsExportFile.Name = "tsThumbnailsExportFile";
            this.tsThumbnailsExportFile.Size = new System.Drawing.Size(141, 22);
            this.tsThumbnailsExportFile.Text = "To &File";
            this.tsThumbnailsExportFile.Click += new System.EventHandler(this.EventClick);
            // 
            // tsThumbnailsExportClipboard
            // 
            this.tsThumbnailsExportClipboard.Image = global::UVtools.GUI.Properties.Resources.clipboard_16x16;
            this.tsThumbnailsExportClipboard.Name = "tsThumbnailsExportClipboard";
            this.tsThumbnailsExportClipboard.Size = new System.Drawing.Size(141, 22);
            this.tsThumbnailsExportClipboard.Text = "To &Clipboard";
            this.tsThumbnailsExportClipboard.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator21
            // 
            this.toolStripSeparator21.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator21.Name = "toolStripSeparator21";
            this.toolStripSeparator21.Size = new System.Drawing.Size(6, 25);
            // 
            // tsThumbnailsImport
            // 
            this.tsThumbnailsImport.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsThumbnailsImport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsThumbnailsImport.Image = global::UVtools.GUI.Properties.Resources.photo_16x16;
            this.tsThumbnailsImport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsThumbnailsImport.Name = "tsThumbnailsImport";
            this.tsThumbnailsImport.Size = new System.Drawing.Size(23, 22);
            this.tsThumbnailsImport.Text = "Change Preview";
            this.tsThumbnailsImport.ToolTipText = "Replace the current preview image";
            this.tsThumbnailsImport.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator22
            // 
            this.toolStripSeparator22.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator22.Name = "toolStripSeparator22";
            this.toolStripSeparator22.Size = new System.Drawing.Size(6, 25);
            // 
            // tsThumbnailsResolution
            // 
            this.tsThumbnailsResolution.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsThumbnailsResolution.Name = "tsThumbnailsResolution";
            this.tsThumbnailsResolution.Size = new System.Drawing.Size(63, 22);
            this.tsThumbnailsResolution.Text = "Resolution";
            this.tsThumbnailsResolution.ToolTipText = "Thumbnail Resolution";
            // 
            // flvProperties
            // 
            this.flvProperties.AllowColumnReorder = true;
            this.flvProperties.Cursor = System.Windows.Forms.Cursors.Default;
            this.flvProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flvProperties.EmptyListMsg = "No properties";
            this.flvProperties.FullRowSelect = true;
            this.flvProperties.GridLines = true;
            this.flvProperties.HideSelection = false;
            this.flvProperties.IncludeColumnHeadersInCopy = true;
            this.flvProperties.Location = new System.Drawing.Point(0, 25);
            this.flvProperties.Name = "flvProperties";
            this.flvProperties.ShowGroups = false;
            this.flvProperties.ShowItemCountOnGroups = true;
            this.flvProperties.Size = new System.Drawing.Size(380, 253);
            this.flvProperties.TabIndex = 9;
            this.flvProperties.UseCompatibleStateImageBehavior = false;
            this.flvProperties.UseExplorerTheme = true;
            this.flvProperties.UseFilterIndicator = true;
            this.flvProperties.UseFiltering = true;
            this.flvProperties.UseHotItem = true;
            this.flvProperties.View = System.Windows.Forms.View.Details;
            this.flvProperties.VirtualMode = true;
            this.flvProperties.KeyUp += new System.Windows.Forms.KeyEventHandler(this.EventKeyUp);
            // 
            // tsProperties
            // 
            this.tsProperties.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsProperties.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsPropertiesLabelCount,
            this.toolStripSeparator4,
            this.tsPropertiesLabelGroups,
            this.tsPropertiesExport});
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
            // tsPropertiesExport
            // 
            this.tsPropertiesExport.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsPropertiesExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsPropertiesExport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsPropertiesExportFile,
            this.tsPropertiesExportClipboard});
            this.tsPropertiesExport.Enabled = false;
            this.tsPropertiesExport.Image = global::UVtools.GUI.Properties.Resources.Save_16x16;
            this.tsPropertiesExport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsPropertiesExport.Name = "tsPropertiesExport";
            this.tsPropertiesExport.Size = new System.Drawing.Size(32, 22);
            this.tsPropertiesExport.Text = "Save to";
            this.tsPropertiesExport.ToolTipText = "Save properties to a file or clipboard";
            this.tsPropertiesExport.Click += new System.EventHandler(this.EventClick);
            // 
            // tsPropertiesExportFile
            // 
            this.tsPropertiesExportFile.Image = global::UVtools.GUI.Properties.Resources.file_image_16x16;
            this.tsPropertiesExportFile.Name = "tsPropertiesExportFile";
            this.tsPropertiesExportFile.Size = new System.Drawing.Size(141, 22);
            this.tsPropertiesExportFile.Text = "To &File";
            this.tsPropertiesExportFile.Click += new System.EventHandler(this.EventClick);
            // 
            // tsPropertiesExportClipboard
            // 
            this.tsPropertiesExportClipboard.Image = global::UVtools.GUI.Properties.Resources.clipboard_16x16;
            this.tsPropertiesExportClipboard.Name = "tsPropertiesExportClipboard";
            this.tsPropertiesExportClipboard.Size = new System.Drawing.Size(141, 22);
            this.tsPropertiesExportClipboard.Text = "To &Clipboard";
            this.tsPropertiesExportClipboard.Click += new System.EventHandler(this.EventClick);
            // 
            // tabPageGCode
            // 
            this.tabPageGCode.Controls.Add(this.tbGCode);
            this.tabPageGCode.Controls.Add(this.tsGCode);
            this.tabPageGCode.ImageKey = "code-16x16.png";
            this.tabPageGCode.Location = new System.Drawing.Point(4, 42);
            this.tabPageGCode.Name = "tabPageGCode";
            this.tabPageGCode.Size = new System.Drawing.Size(386, 713);
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
            this.tbGCode.Size = new System.Drawing.Size(386, 688);
            this.tbGCode.TabIndex = 1;
            // 
            // tsGCode
            // 
            this.tsGCode.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsGCode.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsGCodeLabelLines,
            this.toolStripSeparator3,
            this.tsGcodeLabelChars,
            this.btnGCodeSave,
            this.toolStripSeparator24,
            this.btnGCodeRebuild});
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
            // btnGCodeSave
            // 
            this.btnGCodeSave.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnGCodeSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnGCodeSave.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnGCodeSaveFile,
            this.btnGCodeSaveClipboard});
            this.btnGCodeSave.Image = global::UVtools.GUI.Properties.Resources.Save_16x16;
            this.btnGCodeSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnGCodeSave.Name = "btnGCodeSave";
            this.btnGCodeSave.Size = new System.Drawing.Size(32, 22);
            this.btnGCodeSave.Text = "Save to";
            this.btnGCodeSave.ToolTipText = "Save GCode to a file or clipboard";
            this.btnGCodeSave.Click += new System.EventHandler(this.EventClick);
            // 
            // btnGCodeSaveFile
            // 
            this.btnGCodeSaveFile.Image = global::UVtools.GUI.Properties.Resources.file_image_16x16;
            this.btnGCodeSaveFile.Name = "btnGCodeSaveFile";
            this.btnGCodeSaveFile.Size = new System.Drawing.Size(141, 22);
            this.btnGCodeSaveFile.Text = "To &File";
            this.btnGCodeSaveFile.Click += new System.EventHandler(this.EventClick);
            // 
            // btnGCodeSaveClipboard
            // 
            this.btnGCodeSaveClipboard.Image = global::UVtools.GUI.Properties.Resources.clipboard_16x16;
            this.btnGCodeSaveClipboard.Name = "btnGCodeSaveClipboard";
            this.btnGCodeSaveClipboard.Size = new System.Drawing.Size(141, 22);
            this.btnGCodeSaveClipboard.Text = "To &Clipboard";
            this.btnGCodeSaveClipboard.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator24
            // 
            this.toolStripSeparator24.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator24.Name = "toolStripSeparator24";
            this.toolStripSeparator24.Size = new System.Drawing.Size(6, 25);
            // 
            // btnGCodeRebuild
            // 
            this.btnGCodeRebuild.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnGCodeRebuild.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnGCodeRebuild.Image = global::UVtools.GUI.Properties.Resources.refresh_16x16;
            this.btnGCodeRebuild.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnGCodeRebuild.Name = "btnGCodeRebuild";
            this.btnGCodeRebuild.Size = new System.Drawing.Size(23, 22);
            this.btnGCodeRebuild.Text = "Repair";
            this.btnGCodeRebuild.ToolTipText = "Rebuild GCode with current settings";
            this.btnGCodeRebuild.Click += new System.EventHandler(this.EventClick);
            // 
            // tabPageIssues
            // 
            this.tabPageIssues.Controls.Add(this.flvIssues);
            this.tabPageIssues.Controls.Add(this.tsIssuesLv);
            this.tabPageIssues.Controls.Add(this.tsIssues);
            this.tabPageIssues.ImageKey = "warning-16x16.png";
            this.tabPageIssues.Location = new System.Drawing.Point(4, 42);
            this.tabPageIssues.Name = "tabPageIssues";
            this.tabPageIssues.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageIssues.Size = new System.Drawing.Size(386, 713);
            this.tabPageIssues.TabIndex = 3;
            this.tabPageIssues.Text = "Issues";
            this.tabPageIssues.UseVisualStyleBackColor = true;
            // 
            // flvIssues
            // 
            this.flvIssues.AllColumns.Add(this.flvIssuesColType);
            this.flvIssues.AllColumns.Add(this.flvIssuesColLayerIndex);
            this.flvIssues.AllColumns.Add(this.flvIssuesColPosition);
            this.flvIssues.AllColumns.Add(this.flvIssuesColPixels);
            this.flvIssues.AllowColumnReorder = true;
            this.flvIssues.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.flvIssuesColType,
            this.flvIssuesColLayerIndex,
            this.flvIssuesColPosition,
            this.flvIssuesColPixels});
            this.flvIssues.Cursor = System.Windows.Forms.Cursors.Default;
            this.flvIssues.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flvIssues.EmptyListMsg = "No issues";
            this.flvIssues.FullRowSelect = true;
            this.flvIssues.GridLines = true;
            this.flvIssues.HideSelection = false;
            this.flvIssues.IncludeColumnHeadersInCopy = true;
            this.flvIssues.Location = new System.Drawing.Point(3, 53);
            this.flvIssues.Name = "flvIssues";
            this.flvIssues.ShowGroups = false;
            this.flvIssues.ShowItemCountOnGroups = true;
            this.flvIssues.Size = new System.Drawing.Size(380, 657);
            this.flvIssues.TabIndex = 9;
            this.flvIssues.UseCompatibleStateImageBehavior = false;
            this.flvIssues.UseExplorerTheme = true;
            this.flvIssues.UseFilterIndicator = true;
            this.flvIssues.UseFiltering = true;
            this.flvIssues.UseHotItem = true;
            this.flvIssues.View = System.Windows.Forms.View.Details;
            this.flvIssues.VirtualMode = true;
            this.flvIssues.ItemsChanged += new System.EventHandler<BrightIdeasSoftware.ItemsChangedEventArgs>(this.flvIssues_ItemsChanged);
            this.flvIssues.SelectionChanged += new System.EventHandler(this.EventSelectedIndexChanged);
            this.flvIssues.KeyUp += new System.Windows.Forms.KeyEventHandler(this.EventKeyUp);
            this.flvIssues.MouseClick += new System.Windows.Forms.MouseEventHandler(this.EventMouseClick);
            this.flvIssues.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.EventMouseDoubleClick);
            // 
            // flvIssuesColType
            // 
            this.flvIssuesColType.AspectName = "Type";
            this.flvIssuesColType.Text = "Type";
            this.flvIssuesColType.Width = 100;
            // 
            // flvIssuesColLayerIndex
            // 
            this.flvIssuesColLayerIndex.AspectName = "LayerIndex";
            this.flvIssuesColLayerIndex.Text = "Layer";
            this.flvIssuesColLayerIndex.Width = 56;
            // 
            // flvIssuesColPosition
            // 
            this.flvIssuesColPosition.AspectName = "FirstPointStr";
            this.flvIssuesColPosition.Text = "Position (X,Y)";
            this.flvIssuesColPosition.Width = 110;
            // 
            // flvIssuesColPixels
            // 
            this.flvIssuesColPixels.AspectName = "PixelsCount";
            this.flvIssuesColPixels.Text = "Pixels";
            this.flvIssuesColPixels.Width = 100;
            // 
            // tsIssuesLv
            // 
            this.tsIssuesLv.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsIssuesLv.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnIssueGroup,
            this.toolStripSeparator23,
            this.btnIssueResort});
            this.tsIssuesLv.Location = new System.Drawing.Point(3, 28);
            this.tsIssuesLv.Name = "tsIssuesLv";
            this.tsIssuesLv.Size = new System.Drawing.Size(380, 25);
            this.tsIssuesLv.TabIndex = 10;
            this.tsIssuesLv.Text = "Thumbnail Menu";
            // 
            // btnIssueGroup
            // 
            this.btnIssueGroup.CheckOnClick = true;
            this.btnIssueGroup.Image = global::UVtools.GUI.Properties.Resources.list_16x16;
            this.btnIssueGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnIssueGroup.Name = "btnIssueGroup";
            this.btnIssueGroup.Size = new System.Drawing.Size(60, 22);
            this.btnIssueGroup.Text = "Group";
            this.btnIssueGroup.ToolTipText = "Group items by issue type";
            this.btnIssueGroup.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator23
            // 
            this.toolStripSeparator23.Name = "toolStripSeparator23";
            this.toolStripSeparator23.Size = new System.Drawing.Size(6, 25);
            // 
            // btnIssueResort
            // 
            this.btnIssueResort.Image = global::UVtools.GUI.Properties.Resources.sort_alpha_up_16x16;
            this.btnIssueResort.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnIssueResort.Name = "btnIssueResort";
            this.btnIssueResort.Size = new System.Drawing.Size(60, 22);
            this.btnIssueResort.Text = "Resort";
            this.btnIssueResort.ToolTipText = "Reset sorting on issues";
            this.btnIssueResort.Click += new System.EventHandler(this.EventClick);
            // 
            // tsIssues
            // 
            this.tsIssues.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsIssues.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsIssuePrevious,
            this.tsIssueCount,
            this.tsIssueNext,
            this.toolStripSeparator13,
            this.tsIssueRemove,
            this.tsIssuesDetect,
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
            this.tsIssuePrevious.Image = global::UVtools.GUI.Properties.Resources.Back_16x16;
            this.tsIssuePrevious.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsIssuePrevious.Name = "tsIssuePrevious";
            this.tsIssuePrevious.Size = new System.Drawing.Size(23, 22);
            this.tsIssuePrevious.Text = "Previous";
            this.tsIssuePrevious.ToolTipText = "Show previous issue [CTRL+Left]";
            this.tsIssuePrevious.Click += new System.EventHandler(this.EventClick);
            this.tsIssuePrevious.MouseDown += new System.Windows.Forms.MouseEventHandler(this.EventMouseDown);
            this.tsIssuePrevious.MouseLeave += new System.EventHandler(this.EventMouseLeave);
            this.tsIssuePrevious.MouseUp += new System.Windows.Forms.MouseEventHandler(this.EventMouseUp);
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
            this.tsIssueNext.Image = global::UVtools.GUI.Properties.Resources.Next_16x16;
            this.tsIssueNext.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsIssueNext.Name = "tsIssueNext";
            this.tsIssueNext.Size = new System.Drawing.Size(23, 22);
            this.tsIssueNext.Text = "tsIslandsNext";
            this.tsIssueNext.ToolTipText = "Show next issue [CTRL+Right]";
            this.tsIssueNext.Click += new System.EventHandler(this.EventClick);
            this.tsIssueNext.MouseDown += new System.Windows.Forms.MouseEventHandler(this.EventMouseDown);
            this.tsIssueNext.MouseLeave += new System.EventHandler(this.EventMouseLeave);
            this.tsIssueNext.MouseUp += new System.Windows.Forms.MouseEventHandler(this.EventMouseUp);
            // 
            // toolStripSeparator13
            // 
            this.toolStripSeparator13.Name = "toolStripSeparator13";
            this.toolStripSeparator13.Size = new System.Drawing.Size(6, 25);
            // 
            // tsIssueRemove
            // 
            this.tsIssueRemove.Enabled = false;
            this.tsIssueRemove.Image = global::UVtools.GUI.Properties.Resources.trash_16x16;
            this.tsIssueRemove.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsIssueRemove.Name = "tsIssueRemove";
            this.tsIssueRemove.Size = new System.Drawing.Size(70, 22);
            this.tsIssueRemove.Text = "Remove";
            this.tsIssueRemove.ToolTipText = "Remove selected issue when possible\r\nIslands: All pixels are removed (turn black)" +
    "\r\nResinTrap: All trap areas are filled with white pixels.\r\nTouchingBounds: No ac" +
    "tion, need reslice.";
            this.tsIssueRemove.Click += new System.EventHandler(this.EventClick);
            // 
            // tsIssuesDetect
            // 
            this.tsIssuesDetect.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsIssuesDetect.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsIssuesDetectIslands,
            this.tsIssuesDetectResinTraps,
            this.tsIssuesDetectTouchingBounds,
            this.tsIssuesDetectEmptyLayers});
            this.tsIssuesDetect.Image = global::UVtools.GUI.Properties.Resources.refresh_16x16;
            this.tsIssuesDetect.Name = "tsIssuesDetect";
            this.tsIssuesDetect.Size = new System.Drawing.Size(73, 22);
            this.tsIssuesDetect.Text = "&Detect";
            this.tsIssuesDetect.ToolTipText = "Compute Issues";
            this.tsIssuesDetect.ButtonClick += new System.EventHandler(this.EventClick);
            // 
            // tsIssuesDetectIslands
            // 
            this.tsIssuesDetectIslands.Checked = true;
            this.tsIssuesDetectIslands.CheckOnClick = true;
            this.tsIssuesDetectIslands.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsIssuesDetectIslands.Name = "tsIssuesDetectIslands";
            this.tsIssuesDetectIslands.Size = new System.Drawing.Size(166, 22);
            this.tsIssuesDetectIslands.Text = "&Islands";
            // 
            // tsIssuesDetectResinTraps
            // 
            this.tsIssuesDetectResinTraps.Checked = true;
            this.tsIssuesDetectResinTraps.CheckOnClick = true;
            this.tsIssuesDetectResinTraps.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsIssuesDetectResinTraps.Name = "tsIssuesDetectResinTraps";
            this.tsIssuesDetectResinTraps.Size = new System.Drawing.Size(166, 22);
            this.tsIssuesDetectResinTraps.Text = "&Resin traps";
            // 
            // tsIssuesDetectTouchingBounds
            // 
            this.tsIssuesDetectTouchingBounds.Checked = true;
            this.tsIssuesDetectTouchingBounds.CheckOnClick = true;
            this.tsIssuesDetectTouchingBounds.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsIssuesDetectTouchingBounds.Name = "tsIssuesDetectTouchingBounds";
            this.tsIssuesDetectTouchingBounds.Size = new System.Drawing.Size(166, 22);
            this.tsIssuesDetectTouchingBounds.Text = "&Touching Bounds";
            // 
            // tsIssuesDetectEmptyLayers
            // 
            this.tsIssuesDetectEmptyLayers.Checked = true;
            this.tsIssuesDetectEmptyLayers.CheckOnClick = true;
            this.tsIssuesDetectEmptyLayers.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsIssuesDetectEmptyLayers.Name = "tsIssuesDetectEmptyLayers";
            this.tsIssuesDetectEmptyLayers.Size = new System.Drawing.Size(166, 22);
            this.tsIssuesDetectEmptyLayers.Text = "&Empty Layers";
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
            this.tsIssuesRepair.Image = global::UVtools.GUI.Properties.Resources.Wrench_16x16;
            this.tsIssuesRepair.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsIssuesRepair.Name = "tsIssuesRepair";
            this.tsIssuesRepair.Size = new System.Drawing.Size(60, 22);
            this.tsIssuesRepair.Text = "Repair";
            this.tsIssuesRepair.ToolTipText = "Attempt to repair issues";
            this.tsIssuesRepair.Click += new System.EventHandler(this.EventClick);
            // 
            // tabPagePixelEditor
            // 
            this.tabPagePixelEditor.Controls.Add(this.flvPixelHistory);
            this.tabPagePixelEditor.Controls.Add(this.tsPixelEditorHistory);
            this.tabPagePixelEditor.Controls.Add(this.tabControlPixelEditor);
            this.tabPagePixelEditor.ImageKey = "pixel-16x16.png";
            this.tabPagePixelEditor.Location = new System.Drawing.Point(4, 42);
            this.tabPagePixelEditor.Name = "tabPagePixelEditor";
            this.tabPagePixelEditor.Padding = new System.Windows.Forms.Padding(3);
            this.tabPagePixelEditor.Size = new System.Drawing.Size(386, 713);
            this.tabPagePixelEditor.TabIndex = 4;
            this.tabPagePixelEditor.Text = "Pixel Editor";
            this.tabPagePixelEditor.UseVisualStyleBackColor = true;
            // 
            // flvPixelHistory
            // 
            this.flvPixelHistory.AllColumns.Add(this.flvPixelHistoryColNumber);
            this.flvPixelHistory.AllColumns.Add(this.flvPixelHistoryColOperation);
            this.flvPixelHistory.AllColumns.Add(this.olvColumn3);
            this.flvPixelHistory.AllColumns.Add(this.olvColumn4);
            this.flvPixelHistory.AllowColumnReorder = true;
            this.flvPixelHistory.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.flvPixelHistoryColNumber,
            this.flvPixelHistoryColOperation,
            this.olvColumn3,
            this.olvColumn4});
            this.flvPixelHistory.Cursor = System.Windows.Forms.Cursors.Default;
            this.flvPixelHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flvPixelHistory.EmptyListMsg = "No operations";
            this.flvPixelHistory.FullRowSelect = true;
            this.flvPixelHistory.GridLines = true;
            this.flvPixelHistory.HideSelection = false;
            this.flvPixelHistory.IncludeColumnHeadersInCopy = true;
            this.flvPixelHistory.Location = new System.Drawing.Point(3, 350);
            this.flvPixelHistory.Name = "flvPixelHistory";
            this.flvPixelHistory.ShowGroups = false;
            this.flvPixelHistory.ShowItemCountOnGroups = true;
            this.flvPixelHistory.Size = new System.Drawing.Size(380, 360);
            this.flvPixelHistory.TabIndex = 8;
            this.flvPixelHistory.UseCompatibleStateImageBehavior = false;
            this.flvPixelHistory.UseExplorerTheme = true;
            this.flvPixelHistory.UseFilterIndicator = true;
            this.flvPixelHistory.UseFiltering = true;
            this.flvPixelHistory.UseHotItem = true;
            this.flvPixelHistory.View = System.Windows.Forms.View.Details;
            this.flvPixelHistory.VirtualMode = true;
            this.flvPixelHistory.SelectionChanged += new System.EventHandler(this.EventSelectedIndexChanged);
            this.flvPixelHistory.KeyUp += new System.Windows.Forms.KeyEventHandler(this.EventKeyUp);
            // 
            // flvPixelHistoryColNumber
            // 
            this.flvPixelHistoryColNumber.AspectName = "Index";
            this.flvPixelHistoryColNumber.Text = "#";
            this.flvPixelHistoryColNumber.Width = 46;
            // 
            // flvPixelHistoryColOperation
            // 
            this.flvPixelHistoryColOperation.AspectName = "OperationType";
            this.flvPixelHistoryColOperation.Text = "Operation";
            this.flvPixelHistoryColOperation.Width = 98;
            // 
            // olvColumn3
            // 
            this.olvColumn3.AspectName = "LayerIndex";
            this.olvColumn3.Text = "Layer";
            // 
            // olvColumn4
            // 
            this.olvColumn4.AspectName = "Location";
            this.olvColumn4.Text = "Position";
            this.olvColumn4.Width = 149;
            // 
            // tsPixelEditorHistory
            // 
            this.tsPixelEditorHistory.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsPixelEditorHistory.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lbPixelHistoryOperations,
            this.btnPixelHistoryRemove,
            this.toolStripSeparator19,
            this.btnPixelHistoryApply,
            this.toolStripSeparator20,
            this.btnPixelHistoryClear});
            this.tsPixelEditorHistory.Location = new System.Drawing.Point(3, 325);
            this.tsPixelEditorHistory.Name = "tsPixelEditorHistory";
            this.tsPixelEditorHistory.Size = new System.Drawing.Size(380, 25);
            this.tsPixelEditorHistory.TabIndex = 2;
            // 
            // lbPixelHistoryOperations
            // 
            this.lbPixelHistoryOperations.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lbPixelHistoryOperations.Name = "lbPixelHistoryOperations";
            this.lbPixelHistoryOperations.Size = new System.Drawing.Size(77, 22);
            this.lbPixelHistoryOperations.Text = "Operations: 0";
            // 
            // btnPixelHistoryRemove
            // 
            this.btnPixelHistoryRemove.Enabled = false;
            this.btnPixelHistoryRemove.Image = global::UVtools.GUI.Properties.Resources.delete_16x16;
            this.btnPixelHistoryRemove.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnPixelHistoryRemove.Name = "btnPixelHistoryRemove";
            this.btnPixelHistoryRemove.Size = new System.Drawing.Size(70, 22);
            this.btnPixelHistoryRemove.Text = "Remove";
            this.btnPixelHistoryRemove.ToolTipText = "Remove selected operations\r\n";
            this.btnPixelHistoryRemove.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator19
            // 
            this.toolStripSeparator19.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator19.Name = "toolStripSeparator19";
            this.toolStripSeparator19.Size = new System.Drawing.Size(6, 25);
            // 
            // btnPixelHistoryApply
            // 
            this.btnPixelHistoryApply.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnPixelHistoryApply.Enabled = false;
            this.btnPixelHistoryApply.Image = global::UVtools.GUI.Properties.Resources.accept_16x16;
            this.btnPixelHistoryApply.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnPixelHistoryApply.Name = "btnPixelHistoryApply";
            this.btnPixelHistoryApply.Size = new System.Drawing.Size(75, 22);
            this.btnPixelHistoryApply.Text = "&Apply All";
            this.btnPixelHistoryApply.ToolTipText = "Apply all operations/modifications";
            this.btnPixelHistoryApply.Click += new System.EventHandler(this.EventClick);
            // 
            // toolStripSeparator20
            // 
            this.toolStripSeparator20.Name = "toolStripSeparator20";
            this.toolStripSeparator20.Size = new System.Drawing.Size(6, 25);
            // 
            // btnPixelHistoryClear
            // 
            this.btnPixelHistoryClear.Enabled = false;
            this.btnPixelHistoryClear.Image = global::UVtools.GUI.Properties.Resources.delete_16x16;
            this.btnPixelHistoryClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnPixelHistoryClear.Name = "btnPixelHistoryClear";
            this.btnPixelHistoryClear.Size = new System.Drawing.Size(54, 22);
            this.btnPixelHistoryClear.Text = "Clear";
            this.btnPixelHistoryClear.ToolTipText = "Clears all operations";
            this.btnPixelHistoryClear.Click += new System.EventHandler(this.EventClick);
            // 
            // tabControlPixelEditor
            // 
            this.tabControlPixelEditor.Controls.Add(this.tabPage1);
            this.tabControlPixelEditor.Controls.Add(this.tabPage4);
            this.tabControlPixelEditor.Controls.Add(this.tabPage5);
            this.tabControlPixelEditor.Controls.Add(this.tabPage2);
            this.tabControlPixelEditor.Controls.Add(this.tabPage3);
            this.tabControlPixelEditor.Dock = System.Windows.Forms.DockStyle.Top;
            this.tabControlPixelEditor.Location = new System.Drawing.Point(3, 3);
            this.tabControlPixelEditor.Name = "tabControlPixelEditor";
            this.tabControlPixelEditor.SelectedIndex = 0;
            this.tabControlPixelEditor.Size = new System.Drawing.Size(380, 322);
            this.tabControlPixelEditor.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label29);
            this.tabPage1.Controls.Add(this.label28);
            this.tabPage1.Controls.Add(this.nmPixelEditorDrawingThickness);
            this.tabPage1.Controls.Add(this.label27);
            this.tabPage1.Controls.Add(this.cbPixelEditorDrawingLineType);
            this.tabPage1.Controls.Add(this.label17);
            this.tabPage1.Controls.Add(this.nmPixelEditorDrawingLayersAbove);
            this.tabPage1.Controls.Add(this.label16);
            this.tabPage1.Controls.Add(this.nmPixelEditorDrawingLayersBelow);
            this.tabPage1.Controls.Add(this.label15);
            this.tabPage1.Controls.Add(this.label6);
            this.tabPage1.Controls.Add(this.nmPixelEditorDrawingBrushSize);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.panel3);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.cbPixelEditorDrawingBrushShape);
            this.tabPage1.Location = new System.Drawing.Point(4, 27);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(372, 291);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Drawing";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label29
            // 
            this.label29.AutoSize = true;
            this.label29.Location = new System.Drawing.Point(7, 163);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(80, 18);
            this.label29.TabIndex = 34;
            this.label29.Text = "Thickness:";
            // 
            // label28
            // 
            this.label28.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label28.AutoSize = true;
            this.label28.Location = new System.Drawing.Point(338, 163);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(23, 18);
            this.label28.TabIndex = 33;
            this.label28.Text = "px";
            // 
            // nmPixelEditorDrawingThickness
            // 
            this.nmPixelEditorDrawingThickness.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nmPixelEditorDrawingThickness.Location = new System.Drawing.Point(108, 160);
            this.nmPixelEditorDrawingThickness.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nmPixelEditorDrawingThickness.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.nmPixelEditorDrawingThickness.Name = "nmPixelEditorDrawingThickness";
            this.nmPixelEditorDrawingThickness.Size = new System.Drawing.Size(224, 24);
            this.nmPixelEditorDrawingThickness.TabIndex = 32;
            this.nmPixelEditorDrawingThickness.Value = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.Location = new System.Drawing.Point(7, 70);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(70, 18);
            this.label27.TabIndex = 31;
            this.label27.Text = "Line type:";
            // 
            // cbPixelEditorDrawingLineType
            // 
            this.cbPixelEditorDrawingLineType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbPixelEditorDrawingLineType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPixelEditorDrawingLineType.FormattingEnabled = true;
            this.cbPixelEditorDrawingLineType.Location = new System.Drawing.Point(108, 66);
            this.cbPixelEditorDrawingLineType.Name = "cbPixelEditorDrawingLineType";
            this.cbPixelEditorDrawingLineType.Size = new System.Drawing.Size(258, 26);
            this.cbPixelEditorDrawingLineType.TabIndex = 30;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(268, 217);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(49, 18);
            this.label17.TabIndex = 11;
            this.label17.Text = "Above";
            // 
            // nmPixelEditorDrawingLayersAbove
            // 
            this.nmPixelEditorDrawingLayersAbove.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nmPixelEditorDrawingLayersAbove.Location = new System.Drawing.Point(259, 190);
            this.nmPixelEditorDrawingLayersAbove.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nmPixelEditorDrawingLayersAbove.Name = "nmPixelEditorDrawingLayersAbove";
            this.nmPixelEditorDrawingLayersAbove.Size = new System.Drawing.Size(73, 24);
            this.nmPixelEditorDrawingLayersAbove.TabIndex = 10;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(114, 217);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(49, 18);
            this.label16.TabIndex = 9;
            this.label16.Text = "Below";
            // 
            // nmPixelEditorDrawingLayersBelow
            // 
            this.nmPixelEditorDrawingLayersBelow.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nmPixelEditorDrawingLayersBelow.Location = new System.Drawing.Point(108, 190);
            this.nmPixelEditorDrawingLayersBelow.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nmPixelEditorDrawingLayersBelow.Name = "nmPixelEditorDrawingLayersBelow";
            this.nmPixelEditorDrawingLayersBelow.Size = new System.Drawing.Size(73, 24);
            this.nmPixelEditorDrawingLayersBelow.TabIndex = 8;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(7, 193);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(96, 18);
            this.label15.TabIndex = 7;
            this.label15.Text = "Layers depth:";
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(338, 133);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(28, 18);
            this.label6.TabIndex = 6;
            this.label6.Text = "px²";
            // 
            // nmPixelEditorDrawingBrushSize
            // 
            this.nmPixelEditorDrawingBrushSize.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nmPixelEditorDrawingBrushSize.Location = new System.Drawing.Point(108, 130);
            this.nmPixelEditorDrawingBrushSize.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nmPixelEditorDrawingBrushSize.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmPixelEditorDrawingBrushSize.Name = "nmPixelEditorDrawingBrushSize";
            this.nmPixelEditorDrawingBrushSize.Size = new System.Drawing.Size(224, 24);
            this.nmPixelEditorDrawingBrushSize.TabIndex = 5;
            this.nmPixelEditorDrawingBrushSize.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 133);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(84, 18);
            this.label3.TabIndex = 4;
            this.label3.Text = "Brush area:";
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.label1);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(3, 3);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(366, 57);
            this.panel3.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(264, 36);
            this.label1.TabIndex = 1;
            this.label1.Text = "Shift+Left click to add white pixels\r\nShift+Right click to remove white pixels";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 102);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(95, 18);
            this.label2.TabIndex = 2;
            this.label2.Text = "Brush shape:";
            // 
            // cbPixelEditorDrawingBrushShape
            // 
            this.cbPixelEditorDrawingBrushShape.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbPixelEditorDrawingBrushShape.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPixelEditorDrawingBrushShape.FormattingEnabled = true;
            this.cbPixelEditorDrawingBrushShape.Location = new System.Drawing.Point(108, 98);
            this.cbPixelEditorDrawingBrushShape.Name = "cbPixelEditorDrawingBrushShape";
            this.cbPixelEditorDrawingBrushShape.Size = new System.Drawing.Size(258, 26);
            this.cbPixelEditorDrawingBrushShape.TabIndex = 0;
            this.cbPixelEditorDrawingBrushShape.SelectedIndexChanged += new System.EventHandler(this.EventSelectedIndexChanged);
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.label21);
            this.tabPage4.Controls.Add(this.cbPixelEditorTextLineType);
            this.tabPage4.Controls.Add(this.cbPixelEditorTextMirror);
            this.tabPage4.Controls.Add(this.label26);
            this.tabPage4.Controls.Add(this.nmPixelEditorTextThickness);
            this.tabPage4.Controls.Add(this.label25);
            this.tabPage4.Controls.Add(this.tbPixelEditorTextText);
            this.tabPage4.Controls.Add(this.label18);
            this.tabPage4.Controls.Add(this.nmPixelEditorTextLayersAbove);
            this.tabPage4.Controls.Add(this.label19);
            this.tabPage4.Controls.Add(this.nmPixelEditorTextLayersBelow);
            this.tabPage4.Controls.Add(this.label20);
            this.tabPage4.Controls.Add(this.nmPixelEditorTextFontScale);
            this.tabPage4.Controls.Add(this.label22);
            this.tabPage4.Controls.Add(this.panel6);
            this.tabPage4.Controls.Add(this.label24);
            this.tabPage4.Controls.Add(this.cbPixelEditorTextFontFace);
            this.tabPage4.Location = new System.Drawing.Point(4, 27);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(372, 291);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Text";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(7, 70);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(70, 18);
            this.label21.TabIndex = 29;
            this.label21.Text = "Line type:";
            // 
            // cbPixelEditorTextLineType
            // 
            this.cbPixelEditorTextLineType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbPixelEditorTextLineType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPixelEditorTextLineType.FormattingEnabled = true;
            this.cbPixelEditorTextLineType.Location = new System.Drawing.Point(109, 66);
            this.cbPixelEditorTextLineType.Name = "cbPixelEditorTextLineType";
            this.cbPixelEditorTextLineType.Size = new System.Drawing.Size(257, 26);
            this.cbPixelEditorTextLineType.TabIndex = 28;
            // 
            // cbPixelEditorTextMirror
            // 
            this.cbPixelEditorTextMirror.AutoSize = true;
            this.cbPixelEditorTextMirror.Location = new System.Drawing.Point(109, 190);
            this.cbPixelEditorTextMirror.Name = "cbPixelEditorTextMirror";
            this.cbPixelEditorTextMirror.Size = new System.Drawing.Size(137, 22);
            this.cbPixelEditorTextMirror.TabIndex = 27;
            this.cbPixelEditorTextMirror.Text = "Flip text vertically";
            this.cbPixelEditorTextMirror.UseVisualStyleBackColor = true;
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(207, 133);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(80, 18);
            this.label26.TabIndex = 26;
            this.label26.Text = "Thickness:";
            // 
            // nmPixelEditorTextThickness
            // 
            this.nmPixelEditorTextThickness.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nmPixelEditorTextThickness.Location = new System.Drawing.Point(293, 130);
            this.nmPixelEditorTextThickness.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nmPixelEditorTextThickness.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmPixelEditorTextThickness.Name = "nmPixelEditorTextThickness";
            this.nmPixelEditorTextThickness.Size = new System.Drawing.Size(73, 24);
            this.nmPixelEditorTextThickness.TabIndex = 25;
            this.nmPixelEditorTextThickness.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(8, 163);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(40, 18);
            this.label25.TabIndex = 24;
            this.label25.Text = "Text:";
            // 
            // tbPixelEditorTextText
            // 
            this.tbPixelEditorTextText.Location = new System.Drawing.Point(109, 160);
            this.tbPixelEditorTextText.Name = "tbPixelEditorTextText";
            this.tbPixelEditorTextText.Size = new System.Drawing.Size(257, 24);
            this.tbPixelEditorTextText.TabIndex = 23;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(302, 245);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(49, 18);
            this.label18.TabIndex = 22;
            this.label18.Text = "Above";
            // 
            // nmPixelEditorTextLayersAbove
            // 
            this.nmPixelEditorTextLayersAbove.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nmPixelEditorTextLayersAbove.Location = new System.Drawing.Point(293, 218);
            this.nmPixelEditorTextLayersAbove.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nmPixelEditorTextLayersAbove.Name = "nmPixelEditorTextLayersAbove";
            this.nmPixelEditorTextLayersAbove.Size = new System.Drawing.Size(73, 24);
            this.nmPixelEditorTextLayersAbove.TabIndex = 21;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(115, 245);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(49, 18);
            this.label19.TabIndex = 20;
            this.label19.Text = "Below";
            // 
            // nmPixelEditorTextLayersBelow
            // 
            this.nmPixelEditorTextLayersBelow.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nmPixelEditorTextLayersBelow.Location = new System.Drawing.Point(109, 218);
            this.nmPixelEditorTextLayersBelow.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nmPixelEditorTextLayersBelow.Name = "nmPixelEditorTextLayersBelow";
            this.nmPixelEditorTextLayersBelow.Size = new System.Drawing.Size(73, 24);
            this.nmPixelEditorTextLayersBelow.TabIndex = 19;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(7, 221);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(96, 18);
            this.label20.TabIndex = 18;
            this.label20.Text = "Layers depth:";
            // 
            // nmPixelEditorTextFontScale
            // 
            this.nmPixelEditorTextFontScale.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nmPixelEditorTextFontScale.DecimalPlaces = 2;
            this.nmPixelEditorTextFontScale.Location = new System.Drawing.Point(109, 130);
            this.nmPixelEditorTextFontScale.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nmPixelEditorTextFontScale.Name = "nmPixelEditorTextFontScale";
            this.nmPixelEditorTextFontScale.Size = new System.Drawing.Size(73, 24);
            this.nmPixelEditorTextFontScale.TabIndex = 16;
            this.nmPixelEditorTextFontScale.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(7, 133);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(81, 18);
            this.label22.TabIndex = 15;
            this.label22.Text = "Font scale:";
            // 
            // panel6
            // 
            this.panel6.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panel6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel6.Controls.Add(this.label23);
            this.panel6.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel6.Location = new System.Drawing.Point(3, 3);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(366, 57);
            this.panel6.TabIndex = 14;
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(3, 9);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(254, 36);
            this.label23.TabIndex = 1;
            this.label23.Text = "Shift+Left click to add white text\r\nShift+Right click to remove white text ";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(7, 102);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(74, 18);
            this.label24.TabIndex = 13;
            this.label24.Text = "Font face:";
            // 
            // cbPixelEditorTextFontFace
            // 
            this.cbPixelEditorTextFontFace.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbPixelEditorTextFontFace.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPixelEditorTextFontFace.FormattingEnabled = true;
            this.cbPixelEditorTextFontFace.Location = new System.Drawing.Point(109, 98);
            this.cbPixelEditorTextFontFace.Name = "cbPixelEditorTextFontFace";
            this.cbPixelEditorTextFontFace.Size = new System.Drawing.Size(257, 26);
            this.cbPixelEditorTextFontFace.TabIndex = 12;
            // 
            // tabPage5
            // 
            this.tabPage5.Controls.Add(this.label31);
            this.tabPage5.Controls.Add(this.nmPixelEditorEraserLayersAbove);
            this.tabPage5.Controls.Add(this.label32);
            this.tabPage5.Controls.Add(this.nmPixelEditorEraserLayersBelow);
            this.tabPage5.Controls.Add(this.label33);
            this.tabPage5.Controls.Add(this.panel7);
            this.tabPage5.Location = new System.Drawing.Point(4, 27);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage5.Size = new System.Drawing.Size(372, 291);
            this.tabPage5.TabIndex = 4;
            this.tabPage5.Text = "Eraser";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // label31
            // 
            this.label31.AutoSize = true;
            this.label31.Location = new System.Drawing.Point(302, 93);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(49, 18);
            this.label31.TabIndex = 27;
            this.label31.Text = "Above";
            // 
            // nmPixelEditorEraserLayersAbove
            // 
            this.nmPixelEditorEraserLayersAbove.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nmPixelEditorEraserLayersAbove.Location = new System.Drawing.Point(293, 66);
            this.nmPixelEditorEraserLayersAbove.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nmPixelEditorEraserLayersAbove.Name = "nmPixelEditorEraserLayersAbove";
            this.nmPixelEditorEraserLayersAbove.Size = new System.Drawing.Size(73, 24);
            this.nmPixelEditorEraserLayersAbove.TabIndex = 26;
            // 
            // label32
            // 
            this.label32.AutoSize = true;
            this.label32.Location = new System.Drawing.Point(115, 93);
            this.label32.Name = "label32";
            this.label32.Size = new System.Drawing.Size(49, 18);
            this.label32.TabIndex = 25;
            this.label32.Text = "Below";
            // 
            // nmPixelEditorEraserLayersBelow
            // 
            this.nmPixelEditorEraserLayersBelow.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nmPixelEditorEraserLayersBelow.Location = new System.Drawing.Point(109, 66);
            this.nmPixelEditorEraserLayersBelow.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nmPixelEditorEraserLayersBelow.Name = "nmPixelEditorEraserLayersBelow";
            this.nmPixelEditorEraserLayersBelow.Size = new System.Drawing.Size(73, 24);
            this.nmPixelEditorEraserLayersBelow.TabIndex = 24;
            // 
            // label33
            // 
            this.label33.AutoSize = true;
            this.label33.Location = new System.Drawing.Point(7, 69);
            this.label33.Name = "label33";
            this.label33.Size = new System.Drawing.Size(96, 18);
            this.label33.TabIndex = 23;
            this.label33.Text = "Layers depth:";
            // 
            // panel7
            // 
            this.panel7.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panel7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel7.Controls.Add(this.label30);
            this.panel7.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel7.Location = new System.Drawing.Point(3, 3);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(366, 57);
            this.panel7.TabIndex = 4;
            // 
            // label30
            // 
            this.label30.AutoSize = true;
            this.label30.Location = new System.Drawing.Point(3, 9);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(305, 36);
            this.label30.TabIndex = 1;
            this.label30.Text = "Shift click over a white pixel to remove whole \r\nlinked area (Fill with black)";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.label10);
            this.tabPage2.Controls.Add(this.nmPixelEditorSupportsBaseDiameter);
            this.tabPage2.Controls.Add(this.label11);
            this.tabPage2.Controls.Add(this.label8);
            this.tabPage2.Controls.Add(this.nmPixelEditorSupportsPillarDiameter);
            this.tabPage2.Controls.Add(this.label9);
            this.tabPage2.Controls.Add(this.label7);
            this.tabPage2.Controls.Add(this.nmPixelEditorSupportsTipDiameter);
            this.tabPage2.Controls.Add(this.label5);
            this.tabPage2.Controls.Add(this.panel4);
            this.tabPage2.Location = new System.Drawing.Point(4, 27);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(372, 291);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Supports";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(343, 129);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(23, 18);
            this.label10.TabIndex = 14;
            this.label10.Text = "px";
            // 
            // nmPixelEditorSupportsBaseDiameter
            // 
            this.nmPixelEditorSupportsBaseDiameter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nmPixelEditorSupportsBaseDiameter.Location = new System.Drawing.Point(173, 126);
            this.nmPixelEditorSupportsBaseDiameter.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nmPixelEditorSupportsBaseDiameter.Minimum = new decimal(new int[] {
            25,
            0,
            0,
            0});
            this.nmPixelEditorSupportsBaseDiameter.Name = "nmPixelEditorSupportsBaseDiameter";
            this.nmPixelEditorSupportsBaseDiameter.Size = new System.Drawing.Size(164, 24);
            this.nmPixelEditorSupportsBaseDiameter.TabIndex = 13;
            this.nmPixelEditorSupportsBaseDiameter.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(6, 129);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(161, 18);
            this.label11.TabIndex = 12;
            this.label11.Text = "Support base diameter:";
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(343, 99);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(23, 18);
            this.label8.TabIndex = 11;
            this.label8.Text = "px";
            // 
            // nmPixelEditorSupportsPillarDiameter
            // 
            this.nmPixelEditorSupportsPillarDiameter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nmPixelEditorSupportsPillarDiameter.Location = new System.Drawing.Point(173, 96);
            this.nmPixelEditorSupportsPillarDiameter.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nmPixelEditorSupportsPillarDiameter.Minimum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.nmPixelEditorSupportsPillarDiameter.Name = "nmPixelEditorSupportsPillarDiameter";
            this.nmPixelEditorSupportsPillarDiameter.Size = new System.Drawing.Size(164, 24);
            this.nmPixelEditorSupportsPillarDiameter.TabIndex = 10;
            this.nmPixelEditorSupportsPillarDiameter.Value = new decimal(new int[] {
            32,
            0,
            0,
            0});
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 99);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(159, 18);
            this.label9.TabIndex = 9;
            this.label9.Text = "Support pillar diameter:";
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(343, 69);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(23, 18);
            this.label7.TabIndex = 8;
            this.label7.Text = "px";
            // 
            // nmPixelEditorSupportsTipDiameter
            // 
            this.nmPixelEditorSupportsTipDiameter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nmPixelEditorSupportsTipDiameter.Location = new System.Drawing.Point(173, 66);
            this.nmPixelEditorSupportsTipDiameter.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nmPixelEditorSupportsTipDiameter.Minimum = new decimal(new int[] {
            7,
            0,
            0,
            0});
            this.nmPixelEditorSupportsTipDiameter.Name = "nmPixelEditorSupportsTipDiameter";
            this.nmPixelEditorSupportsTipDiameter.Size = new System.Drawing.Size(164, 24);
            this.nmPixelEditorSupportsTipDiameter.TabIndex = 7;
            this.nmPixelEditorSupportsTipDiameter.Value = new decimal(new int[] {
            19,
            0,
            0,
            0});
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 69);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(144, 18);
            this.label5.TabIndex = 6;
            this.label5.Text = "Support tip diameter:";
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel4.Controls.Add(this.label4);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel4.Location = new System.Drawing.Point(3, 3);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(366, 57);
            this.panel4.TabIndex = 4;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 9);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(339, 36);
            this.label4.TabIndex = 1;
            this.label4.Text = "Shift click under a island to add a primitive support.\r\nNote: This operation can\'" +
    "t be previewed.";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.label13);
            this.tabPage3.Controls.Add(this.nmPixelEditorDrainHoleDiameter);
            this.tabPage3.Controls.Add(this.label14);
            this.tabPage3.Controls.Add(this.panel5);
            this.tabPage3.Location = new System.Drawing.Point(4, 27);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(372, 291);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Drain Holes";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // label13
            // 
            this.label13.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(341, 69);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(23, 18);
            this.label13.TabIndex = 14;
            this.label13.Text = "px";
            // 
            // nmPixelEditorDrainHoleDiameter
            // 
            this.nmPixelEditorDrainHoleDiameter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nmPixelEditorDrainHoleDiameter.Location = new System.Drawing.Point(150, 66);
            this.nmPixelEditorDrainHoleDiameter.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nmPixelEditorDrainHoleDiameter.Minimum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.nmPixelEditorDrainHoleDiameter.Name = "nmPixelEditorDrainHoleDiameter";
            this.nmPixelEditorDrainHoleDiameter.Size = new System.Drawing.Size(185, 24);
            this.nmPixelEditorDrainHoleDiameter.TabIndex = 13;
            this.nmPixelEditorDrainHoleDiameter.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(4, 69);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(140, 18);
            this.label14.TabIndex = 12;
            this.label14.Text = "Drain hole diameter:";
            // 
            // panel5
            // 
            this.panel5.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panel5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel5.Controls.Add(this.label12);
            this.panel5.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel5.Location = new System.Drawing.Point(3, 3);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(366, 57);
            this.panel5.TabIndex = 5;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(3, 9);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(271, 36);
            this.label12.TabIndex = 1;
            this.label12.Text = "Shift click to add a vertical drain hole.\r\nNote: This operation can\'t be previewe" +
    "d.";
            // 
            // tabPageLog
            // 
            this.tabPageLog.Controls.Add(this.lvLog);
            this.tabPageLog.Controls.Add(this.tsLog);
            this.tabPageLog.ImageKey = "log-16x16.png";
            this.tabPageLog.Location = new System.Drawing.Point(4, 42);
            this.tabPageLog.Name = "tabPageLog";
            this.tabPageLog.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageLog.Size = new System.Drawing.Size(386, 713);
            this.tabPageLog.TabIndex = 5;
            this.tabPageLog.Text = "Log";
            this.tabPageLog.UseVisualStyleBackColor = true;
            // 
            // lvLog
            // 
            this.lvLog.AllowColumnReorder = true;
            this.lvLog.Cursor = System.Windows.Forms.Cursors.Default;
            this.lvLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvLog.EmptyListMsg = "No operations";
            this.lvLog.FullRowSelect = true;
            this.lvLog.GridLines = true;
            this.lvLog.HideSelection = false;
            this.lvLog.IncludeColumnHeadersInCopy = true;
            this.lvLog.Location = new System.Drawing.Point(3, 28);
            this.lvLog.Name = "lvLog";
            this.lvLog.ShowGroups = false;
            this.lvLog.ShowItemCountOnGroups = true;
            this.lvLog.Size = new System.Drawing.Size(380, 682);
            this.lvLog.TabIndex = 1;
            this.lvLog.UseCompatibleStateImageBehavior = false;
            this.lvLog.UseExplorerTheme = true;
            this.lvLog.UseFilterIndicator = true;
            this.lvLog.UseFiltering = true;
            this.lvLog.UseHotItem = true;
            this.lvLog.View = System.Windows.Forms.View.Details;
            this.lvLog.VirtualMode = true;
            // 
            // tsLog
            // 
            this.tsLog.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsLog.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnLogClear,
            this.lbLogOperations});
            this.tsLog.Location = new System.Drawing.Point(3, 3);
            this.tsLog.Name = "tsLog";
            this.tsLog.Size = new System.Drawing.Size(380, 25);
            this.tsLog.TabIndex = 0;
            this.tsLog.Text = "Log";
            // 
            // btnLogClear
            // 
            this.btnLogClear.Image = global::UVtools.GUI.Properties.Resources.trash_16x16;
            this.btnLogClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnLogClear.Name = "btnLogClear";
            this.btnLogClear.Size = new System.Drawing.Size(54, 22);
            this.btnLogClear.Text = "Clear";
            this.btnLogClear.ToolTipText = "Clears all operations";
            this.btnLogClear.Click += new System.EventHandler(this.EventClick);
            // 
            // lbLogOperations
            // 
            this.lbLogOperations.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lbLogOperations.Name = "lbLogOperations";
            this.lbLogOperations.Size = new System.Drawing.Size(77, 22);
            this.lbLogOperations.Text = "Operations: 0";
            // 
            // imageList16x16
            // 
            this.imageList16x16.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList16x16.ImageStream")));
            this.imageList16x16.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList16x16.Images.SetKeyName(0, "Button-Info-16x16.png");
            this.imageList16x16.Images.SetKeyName(1, "PhotoInfo-16x16.png");
            this.imageList16x16.Images.SetKeyName(2, "code-16x16.png");
            this.imageList16x16.Images.SetKeyName(3, "warning-16x16.png");
            this.imageList16x16.Images.SetKeyName(4, "pixel-16x16.png");
            this.imageList16x16.Images.SetKeyName(5, "log-16x16.png");
            // 
            // tlRight
            // 
            this.tlRight.ColumnCount = 1;
            this.tlRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlRight.Controls.Add(this.btnPreviousLayer, 0, 3);
            this.tlRight.Controls.Add(this.btnNextLayer, 0, 1);
            this.tlRight.Controls.Add(this.lbMaxLayer, 0, 0);
            this.tlRight.Controls.Add(this.panelLayerNavigation, 0, 2);
            this.tlRight.Controls.Add(this.lbInitialLayer, 0, 5);
            this.tlRight.Controls.Add(this.panel2, 0, 4);
            this.tlRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlRight.Location = new System.Drawing.Point(1585, 3);
            this.tlRight.Name = "tlRight";
            this.tlRight.RowCount = 6;
            this.tlRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tlRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tlRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tlRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tlRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tlRight.Size = new System.Drawing.Size(144, 759);
            this.tlRight.TabIndex = 6;
            // 
            // btnPreviousLayer
            // 
            this.btnPreviousLayer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPreviousLayer.Enabled = false;
            this.btnPreviousLayer.Image = global::UVtools.GUI.Properties.Resources.arrow_down_16x16;
            this.btnPreviousLayer.Location = new System.Drawing.Point(3, 648);
            this.btnPreviousLayer.Name = "btnPreviousLayer";
            this.btnPreviousLayer.Size = new System.Drawing.Size(138, 26);
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
            this.btnNextLayer.Image = global::UVtools.GUI.Properties.Resources.arrow_up_16x16;
            this.btnNextLayer.Location = new System.Drawing.Point(3, 53);
            this.btnNextLayer.Name = "btnNextLayer";
            this.btnNextLayer.Size = new System.Drawing.Size(138, 26);
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
            this.lbMaxLayer.Size = new System.Drawing.Size(138, 50);
            this.lbMaxLayer.TabIndex = 12;
            this.lbMaxLayer.Text = "Layers";
            this.lbMaxLayer.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelLayerNavigation
            // 
            this.panelLayerNavigation.Controls.Add(this.pbTrackerIssues);
            this.panelLayerNavigation.Controls.Add(this.lbActualLayer);
            this.panelLayerNavigation.Controls.Add(this.tbLayer);
            this.panelLayerNavigation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelLayerNavigation.Location = new System.Drawing.Point(3, 85);
            this.panelLayerNavigation.Name = "panelLayerNavigation";
            this.panelLayerNavigation.Size = new System.Drawing.Size(138, 557);
            this.panelLayerNavigation.TabIndex = 13;
            // 
            // pbTrackerIssues
            // 
            this.pbTrackerIssues.Dock = System.Windows.Forms.DockStyle.Right;
            this.pbTrackerIssues.Location = new System.Drawing.Point(83, 0);
            this.pbTrackerIssues.Margin = new System.Windows.Forms.Padding(0);
            this.pbTrackerIssues.Name = "pbTrackerIssues";
            this.pbTrackerIssues.Size = new System.Drawing.Size(10, 557);
            this.pbTrackerIssues.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pbTrackerIssues.TabIndex = 10;
            this.pbTrackerIssues.TabStop = false;
            // 
            // lbActualLayer
            // 
            this.lbActualLayer.AutoSize = true;
            this.lbActualLayer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lbActualLayer.Location = new System.Drawing.Point(3, 248);
            this.lbActualLayer.Name = "lbActualLayer";
            this.lbActualLayer.Size = new System.Drawing.Size(18, 20);
            this.lbActualLayer.TabIndex = 9;
            this.lbActualLayer.Text = "?";
            // 
            // tbLayer
            // 
            this.tbLayer.Dock = System.Windows.Forms.DockStyle.Right;
            this.tbLayer.Location = new System.Drawing.Point(93, 0);
            this.tbLayer.Margin = new System.Windows.Forms.Padding(0);
            this.tbLayer.Name = "tbLayer";
            this.tbLayer.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.tbLayer.Size = new System.Drawing.Size(45, 557);
            this.tbLayer.TabIndex = 8;
            this.tbLayer.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
            this.tbLayer.ValueChanged += new System.EventHandler(this.ValueChanged);
            // 
            // lbInitialLayer
            // 
            this.lbInitialLayer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbInitialLayer.Location = new System.Drawing.Point(3, 709);
            this.lbInitialLayer.Name = "lbInitialLayer";
            this.lbInitialLayer.Size = new System.Drawing.Size(138, 50);
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
            this.panel2.Location = new System.Drawing.Point(3, 680);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(138, 26);
            this.panel2.TabIndex = 15;
            // 
            // btnFindLayer
            // 
            this.btnFindLayer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnFindLayer.Enabled = false;
            this.btnFindLayer.Image = global::UVtools.GUI.Properties.Resources.search_16x16;
            this.btnFindLayer.Location = new System.Drawing.Point(37, 0);
            this.btnFindLayer.Name = "btnFindLayer";
            this.btnFindLayer.Size = new System.Drawing.Size(64, 26);
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
            this.btnLastLayer.Image = global::UVtools.GUI.Properties.Resources.arrow_top_16x16;
            this.btnLastLayer.Location = new System.Drawing.Point(101, 0);
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
            this.btnFirstLayer.Image = global::UVtools.GUI.Properties.Resources.arrow_end_16x16;
            this.btnFirstLayer.Location = new System.Drawing.Point(0, 0);
            this.btnFirstLayer.Name = "btnFirstLayer";
            this.btnFirstLayer.Size = new System.Drawing.Size(37, 26);
            this.btnFirstLayer.TabIndex = 15;
            this.btnFirstLayer.Text = "-";
            this.toolTipInformation.SetToolTip(this.btnFirstLayer, "Go to the first layer [HOME]");
            this.btnFirstLayer.UseVisualStyleBackColor = true;
            this.btnFirstLayer.Click += new System.EventHandler(this.EventClick);
            // 
            // layerZoomTimer
            // 
            this.layerZoomTimer.Interval = 1;
            this.layerZoomTimer.Tick += new System.EventHandler(this.EventTimerTick);
            // 
            // issueScrollTimer
            // 
            this.issueScrollTimer.Interval = 500;
            this.issueScrollTimer.Tick += new System.EventHandler(this.EventTimerTick);
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
            this.layerScrollTimer.Interval = 500;
            this.layerScrollTimer.Tick += new System.EventHandler(this.EventTimerTick);
            // 
            // mouseHoldTimer
            // 
            this.mouseHoldTimer.Interval = 1000;
            this.mouseHoldTimer.Tick += new System.EventHandler(this.EventTimerTick);
            // 
            // FrmMain
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1732, 811);
            this.Controls.Add(this.mainTable);
            this.Controls.Add(this.statusBar);
            this.Controls.Add(this.menu);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MainMenuStrip = this.menu;
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "FrmMain";
            this.Text = "UVtools";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.EventKeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.EventKeyUp);
            this.menu.ResumeLayout(false);
            this.menu.PerformLayout();
            this.mainTable.ResumeLayout(false);
            this.scCenter.Panel1.ResumeLayout(false);
            this.scCenter.Panel1.PerformLayout();
            this.scCenter.Panel2.ResumeLayout(false);
            this.scCenter.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scCenter)).EndInit();
            this.scCenter.ResumeLayout(false);
            this.tsLayer.ResumeLayout(false);
            this.tsLayer.PerformLayout();
            this.tsLayerInfo.ResumeLayout(false);
            this.tsLayerInfo.PerformLayout();
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
            ((System.ComponentModel.ISupportInitialize)(this.flvProperties)).EndInit();
            this.tsProperties.ResumeLayout(false);
            this.tsProperties.PerformLayout();
            this.tabPageGCode.ResumeLayout(false);
            this.tabPageGCode.PerformLayout();
            this.tsGCode.ResumeLayout(false);
            this.tsGCode.PerformLayout();
            this.tabPageIssues.ResumeLayout(false);
            this.tabPageIssues.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.flvIssues)).EndInit();
            this.tsIssuesLv.ResumeLayout(false);
            this.tsIssuesLv.PerformLayout();
            this.tsIssues.ResumeLayout(false);
            this.tsIssues.PerformLayout();
            this.tabPagePixelEditor.ResumeLayout(false);
            this.tabPagePixelEditor.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.flvPixelHistory)).EndInit();
            this.tsPixelEditorHistory.ResumeLayout(false);
            this.tsPixelEditorHistory.PerformLayout();
            this.tabControlPixelEditor.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorDrawingThickness)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorDrawingLayersAbove)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorDrawingLayersBelow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorDrawingBrushSize)).EndInit();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.tabPage4.ResumeLayout(false);
            this.tabPage4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorTextThickness)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorTextLayersAbove)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorTextLayersBelow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorTextFontScale)).EndInit();
            this.panel6.ResumeLayout(false);
            this.panel6.PerformLayout();
            this.tabPage5.ResumeLayout(false);
            this.tabPage5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorEraserLayersAbove)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorEraserLayersBelow)).EndInit();
            this.panel7.ResumeLayout(false);
            this.panel7.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorSupportsBaseDiameter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorSupportsPillarDiameter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorSupportsTipDiameter)).EndInit();
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmPixelEditorDrainHoleDiameter)).EndInit();
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            this.tabPageLog.ResumeLayout(false);
            this.tabPageLog.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lvLog)).EndInit();
            this.tsLog.ResumeLayout(false);
            this.tsLog.PerformLayout();
            this.tlRight.ResumeLayout(false);
            this.panelLayerNavigation.ResumeLayout(false);
            this.panelLayerNavigation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbTrackerIssues)).EndInit();
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
        private System.Windows.Forms.TabControl tabControlLeft;
        private System.Windows.Forms.TabPage tbpThumbnailsAndInfo;
        private System.Windows.Forms.SplitContainer scLeft;
        private System.Windows.Forms.PictureBox pbThumbnail;
        private System.Windows.Forms.ToolStrip tsThumbnails;
        private System.Windows.Forms.ToolStripButton tsThumbnailsPrevious;
        private System.Windows.Forms.ToolStripLabel tsThumbnailsCount;
        private System.Windows.Forms.ToolStripButton tsThumbnailsNext;
        private System.Windows.Forms.ToolStripLabel tsThumbnailsResolution;
        private System.Windows.Forms.TabPage tabPageGCode;
        private System.Windows.Forms.ImageList imageList16x16;
        private System.Windows.Forms.TextBox tbGCode;
        private System.Windows.Forms.ToolStrip tsGCode;
        private System.Windows.Forms.ToolStripLabel tsGCodeLabelLines;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripLabel tsGcodeLabelChars;
        private System.Windows.Forms.ToolStrip tsProperties;
        private System.Windows.Forms.ToolStripLabel tsPropertiesLabelCount;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripLabel tsPropertiesLabelGroups;
        private System.Windows.Forms.ToolStripMenuItem menuFileOpenNewWindow;
        private System.Windows.Forms.ToolStripButton btnLayerImageRotate;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripButton btnLayerImageLayerDifference;
        private Cyotek.Windows.Forms.ImageBox pbLayer;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripButton btnLayerImagePixelEdit;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
        private System.Windows.Forms.ToolStripMenuItem menuMutate;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
        private System.Windows.Forms.ToolStripMenuItem menuHelpInstallPrinters;
        private System.Windows.Forms.TabPage tabPageIssues;
        private System.Windows.Forms.ToolStrip tsIssues;
        private System.Windows.Forms.ToolStripButton tsIssuePrevious;
        private System.Windows.Forms.ToolStripButton tsIssueNext;
        private System.Windows.Forms.ToolStripLabel tsIssueCount;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
        private System.Windows.Forms.ToolStripButton tsIssueRemove;
        private System.Windows.Forms.ToolStripMenuItem menuTools;
        private System.Windows.Forms.ToolStripMenuItem menuToolsRepairLayers;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator13;
        private System.Windows.Forms.ToolStripButton tsIssuesRepair;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator14;
        private System.Windows.Forms.ToolStripButton btnLayerImageHighlightIssues;
        private System.Windows.Forms.ToolStripButton btnLayerImageShowCrosshairs;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator25;
        private TrackBarEx tbLayer;
        private System.Windows.Forms.TableLayoutPanel tlRight;
        private System.Windows.Forms.Label lbMaxLayer;
        private System.Windows.Forms.Label lbInitialLayer;
        private System.Windows.Forms.Button btnNextLayer;
        private System.Windows.Forms.Button btnPreviousLayer;
        private System.Windows.Forms.Panel panelLayerNavigation;
        private System.Windows.Forms.Label lbActualLayer;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnFirstLayer;
        private System.Windows.Forms.Button btnLastLayer;
        private System.Windows.Forms.Button btnFindLayer;
        private System.Windows.Forms.ToolTip toolTipInformation;
        private System.Windows.Forms.Timer layerScrollTimer;
        private System.Windows.Forms.Timer layerZoomTimer;
        private System.Windows.Forms.Timer issueScrollTimer;
        private System.Windows.Forms.ToolStripSplitButton btnLayerImageExport;
        private System.Windows.Forms.ToolStripMenuItem btnLayerImageExportFile;
        private System.Windows.Forms.ToolStripMenuItem btnLayerImageExportClipboard;
        private System.Windows.Forms.ToolStripMenuItem menuNewVersion;
        private System.Windows.Forms.ToolStripMenuItem menuFileSettings;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator15;
        private System.Windows.Forms.ToolStripSplitButton tsIssuesDetect;
        private System.Windows.Forms.ToolStripMenuItem tsIssuesDetectIslands;
        private System.Windows.Forms.ToolStripMenuItem tsIssuesDetectResinTraps;
        private System.Windows.Forms.ToolStripSplitButton btnLayerImageLayerOutline;
        private System.Windows.Forms.ToolStripMenuItem btnLayerImageLayerOutlineEdgeDetection;
        private System.Windows.Forms.ToolStripMenuItem btnLayerImageLayerOutlinePrintVolumeBounds;
        private System.Windows.Forms.ToolStripMenuItem btnLayerImageLayerOutlineLayerBounds;
        private System.Windows.Forms.ToolStripMenuItem btnLayerImageLayerOutlineHollowAreas;
        private System.Windows.Forms.ToolStripMenuItem menuToolsPattern;
        private System.Windows.Forms.ToolStripSplitButton tsThumbnailsExport;
        private System.Windows.Forms.ToolStripMenuItem tsThumbnailsExportFile;
        private System.Windows.Forms.ToolStripMenuItem tsThumbnailsExportClipboard;
        private System.Windows.Forms.TabPage tabPagePixelEditor;
        private System.Windows.Forms.TabControl tabControlPixelEditor;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.ToolStrip tsPixelEditorHistory;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbPixelEditorDrawingBrushShape;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nmPixelEditorDrawingBrushSize;
        private System.Windows.Forms.ToolStripLabel lbPixelHistoryOperations;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown nmPixelEditorSupportsTipDiameter;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown nmPixelEditorSupportsPillarDiameter;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.NumericUpDown nmPixelEditorSupportsBaseDiameter;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.NumericUpDown nmPixelEditorDrainHoleDiameter;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.ToolStripButton btnPixelHistoryRemove;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator18;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator19;
        private System.Windows.Forms.ToolStripButton btnPixelHistoryApply;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator20;
        private System.Windows.Forms.ToolStripButton btnPixelHistoryClear;
        private System.Windows.Forms.ToolStripButton tsThumbnailsImport;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator21;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator22;
        private System.Windows.Forms.ToolStripSplitButton tsPropertiesExport;
        private System.Windows.Forms.ToolStripMenuItem tsPropertiesExportFile;
        private System.Windows.Forms.ToolStripMenuItem tsPropertiesExportClipboard;
        private System.Windows.Forms.TabPage tabPageLog;
        private BrightIdeasSoftware.FastObjectListView lvLog;
        private System.Windows.Forms.ToolStrip tsLog;
        private System.Windows.Forms.ToolStripButton btnLogClear;
        private System.Windows.Forms.ToolStripLabel lbLogOperations;
        private BrightIdeasSoftware.FastObjectListView flvPixelHistory;
        private BrightIdeasSoftware.OLVColumn flvPixelHistoryColNumber;
        private BrightIdeasSoftware.OLVColumn flvPixelHistoryColOperation;
        private BrightIdeasSoftware.OLVColumn olvColumn3;
        private BrightIdeasSoftware.OLVColumn olvColumn4;
        private BrightIdeasSoftware.FastObjectListView flvProperties;
        private BrightIdeasSoftware.FastObjectListView flvIssues;
        private BrightIdeasSoftware.OLVColumn flvIssuesColType;
        private BrightIdeasSoftware.OLVColumn flvIssuesColLayerIndex;
        private BrightIdeasSoftware.OLVColumn flvIssuesColPosition;
        private BrightIdeasSoftware.OLVColumn flvIssuesColPixels;
        private System.Windows.Forms.ToolStrip tsIssuesLv;
        private System.Windows.Forms.ToolStripButton btnIssueGroup;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator23;
        private System.Windows.Forms.ToolStripButton btnIssueResort;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.NumericUpDown nmPixelEditorDrawingLayersAbove;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.NumericUpDown nmPixelEditorDrawingLayersBelow;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.NumericUpDown nmPixelEditorTextLayersAbove;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.NumericUpDown nmPixelEditorTextLayersBelow;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.NumericUpDown nmPixelEditorTextFontScale;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.ComboBox cbPixelEditorTextFontFace;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.TextBox tbPixelEditorTextText;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.NumericUpDown nmPixelEditorTextThickness;
        private System.Windows.Forms.CheckBox cbPixelEditorTextMirror;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.ComboBox cbPixelEditorTextLineType;
        private System.Windows.Forms.Label label27;
        private System.Windows.Forms.ComboBox cbPixelEditorDrawingLineType;
        private System.Windows.Forms.Label label29;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.NumericUpDown nmPixelEditorDrawingThickness;
        private System.Windows.Forms.ToolStripSplitButton btnGCodeSave;
        private System.Windows.Forms.ToolStripMenuItem btnGCodeSaveFile;
        private System.Windows.Forms.ToolStripMenuItem btnGCodeSaveClipboard;
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.Label label30;
        private System.Windows.Forms.Label label31;
        private System.Windows.Forms.NumericUpDown nmPixelEditorEraserLayersAbove;
        private System.Windows.Forms.Label label32;
        private System.Windows.Forms.NumericUpDown nmPixelEditorEraserLayersBelow;
        private System.Windows.Forms.Label label33;
        private System.Windows.Forms.ToolStrip tsLayerInfo;
        private System.Windows.Forms.ToolStripLabel tsLayerPreviewTime;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripLabel tsLayerResolution;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
        private System.Windows.Forms.ToolStripLabel tsLayerImageZoom;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripLabel tsLayerImagePanLocation;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator16;
        private System.Windows.Forms.ToolStripLabel tsLayerImageMouseLocation;
        private System.Windows.Forms.ToolStripLabel tsLayerImagePixelCount;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator17;
        private System.Windows.Forms.ToolStripLabel tsLayerBounds;
        private System.Windows.Forms.ToolStripMenuItem menuHelpBenchmark;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator24;
        private System.Windows.Forms.ToolStripLabel tsLayerImageZoomLock;
        private System.Windows.Forms.ToolStripButton btnGCodeRebuild;
        private System.Windows.Forms.Timer mouseHoldTimer;
        private System.Windows.Forms.ToolStripMenuItem tsIssuesDetectEmptyLayers;
        private System.Windows.Forms.ToolStripMenuItem tsIssuesDetectTouchingBounds;
        private System.Windows.Forms.PictureBox pbTrackerIssues;
        private System.Windows.Forms.ToolStripSplitButton btnLayerImageActions;
    }
}

