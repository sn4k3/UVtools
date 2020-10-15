/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BrightIdeasSoftware;
using Cyotek.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;
using UVtools.Core.PixelEditor;
using UVtools.GUI.Controls;
using UVtools.GUI.Controls.Tools;
using UVtools.GUI.Extensions;
using UVtools.GUI.Forms;
using UVtools.GUI.Properties;

namespace UVtools.GUI
{
    public partial class FrmMain : Form
    {
        #region Enums

        #endregion

        #region Properties

        public static readonly OperationMenuItem[] MenuTools = {
            new OperationMenuItem(new OperationEditParameters(), Resources.Wrench_16x16),
            new OperationMenuItem(new OperationRepairLayers(), Resources.toolbox_16x16),
            new OperationMenuItem(new OperationMove(), Resources.move_16x16),
            new OperationMenuItem(new OperationResize(), Resources.crop_16x16),
            new OperationMenuItem(new OperationFlip(), Resources.flip_16x16),
            new OperationMenuItem(new OperationRotate(), Resources.sync_16x16),
            new OperationMenuItem(new OperationSolidify(), Resources.square_solid_16x16),
            new OperationMenuItem(new OperationMorph(), Resources.Geometry_16x16),
            new OperationMenuItem(new OperationThreshold(), Resources.th_16x16),
            new OperationMenuItem(new OperationArithmetic(), Resources.square_root_16x16),
            new OperationMenuItem(new OperationMask(), Resources.mask_16x16),
            new OperationMenuItem(new OperationPixelDimming(), Resources.pixel_16x16),
            new OperationMenuItem(new OperationBlur(), Resources.blur_16x16),
            new OperationMenuItem(new OperationPattern(), Resources.pattern_16x16),
            new OperationMenuItem(new OperationLayerReHeight(), Resources.ladder_16x16),
            new OperationMenuItem(new OperationChangeResolution(), Resources.resize_16x16),
            new OperationMenuItem(new OperationCalculator(), Resources.calculator_16x16),
        };

        public static readonly OperationMenuItem[] LayerActions = {
            new OperationMenuItem(new OperationLayerImport(), Resources.file_import_16x16),
            new OperationMenuItem(new OperationLayerClone(), Resources.copy_16x16), 
            new OperationMenuItem(new OperationLayerRemove(), Resources.trash_16x16), 
        };

        public FrmLoading FrmLoading { get; }

        public Rectangle ROI
        {
            get
            {
                var rectangleF = pbLayer.SelectionRegion;
                return rectangleF.IsEmpty ? Rectangle.Empty : GetTransposedRectangle(rectangleF, false);
            }
            set => pbLayer.SelectionRegion = value;
        }

        public static FileFormat SlicerFile
        {
            get => Program.SlicerFile;
            set => Program.SlicerFile = value;
        }

        public LayerCache LayerCache { get; set; } = new LayerCache();

        public uint ActualLayer { get; set; }

        public Mat ActualLayerImage { get; private set; }

        public Mat ActualLayerImageBgr { get; private set; } = new Mat();

        public List<LayerIssue> Issues { get; set; }

        public int TotalIssues => Issues?.Count ?? 0;

        public bool IsChangingLayer { get; set; }

        // Supported ZoomLevels for Layer Preview.
        // These settings eliminate very small zoom factors from the ImageBox default values,
        // while ensuring that 4K/5K build plates can still easily fit on screen.  
        public static readonly int[] ZoomLevels =
            {20, 25, 30, 50, 75, 100, 150, 200, 300, 400, 500, 600, 700, 800, 1200, 1600};

        // Count of the bottom portion of the full zoom range which will be skipped for
        // assignable actions such as auto-zoom level, and crosshair fade level.  If values
        // are added/removed from ZoomLevels above, this value may also need to be adjusted.
        public static int ZoomLevelSkipCount { get; } = 7; // Start at 2x which is index 7.

        /// <summary>
        /// Returns the zoom level at which the crosshairs will fade and no longer be displayed
        /// </summary>
        public int CrosshairFadeLevel => ZoomLevels[Settings.Default.CrosshairFadeLevel + ZoomLevelSkipCount];

        /// <summary>
        /// Returns the zoom level that will be used for autozoom actions
        /// </summary>
        private int LockedZoomLevel { get; set; } = 1200;


        /// <summary>
        /// Minimum Zoom level to which autozoom can be locked. 
        /// </summary>
        private static int MinLockedZoomLevel { get; } = 200;

        public PixelHistory PixelHistory { get; } = new PixelHistory();

        // Track last open tab for when PixelEditor tab is removed.
        public TabPage ControlLeftLastTab { get; set; }

        public bool CanSave
        {
            get => menuFileSave.Enabled;
            set => menuFileSave.Enabled = value;
        }

        public uint SavesCount { get; set; }

        private bool SupressLayerZoomEvent { get; set; }

        public bool IsLogVerbose => btnLogVerbose.Checked;

        private Cursor pixelEditCursor;

        #endregion

        #region Constructors

        public FrmMain()
        {
            //SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            InitializeComponent();
            FrmLoading = new FrmLoading();
            Program.SetAllControlsFontSize(Controls, 11);
            Program.SetAllControlsFontSize(FrmLoading.Controls, 11);

            if (Settings.Default.UpgradeSettings)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeSettings = false;
                Settings.Default.Save();
            }

            ControlLeftLastTab = tbpThumbnailsAndInfo;
            Clear();

            btnLayerImageLayerDifference.Checked = Settings.Default.LayerDifferenceDefault;
            tsIssuesDetectIslands.Checked = Settings.Default.ComputeIslands;
            tsIssuesDetectOverhangs.Checked = Settings.Default.ComputeOverhangs;
            tsIssuesDetectResinTraps.Checked = Settings.Default.ComputeResinTraps;
            tsIssuesDetectTouchingBounds.Checked = Settings.Default.ComputeTouchingBounds;
            tsIssuesDetectEmptyLayers.Checked = Settings.Default.ComputeEmptyLayers;
            btnLayerImageLayerOutlinePrintVolumeBounds.Checked = Settings.Default.OutlinePrintVolumeBounds;
            btnLayerImageLayerOutlineLayerBounds.Checked = Settings.Default.OutlineLayerBounds;
            btnLayerImageLayerOutlineHollowAreas.Checked = Settings.Default.OutlineHollowAreas;
            lbLayerImageTooltipOverlay.TransparentBackColor = Settings.Default.LayerTooltipOverlayColor;
            lbLayerImageTooltipOverlay.Opacity = Settings.Default.LayerTooltipOverlayOpacity;


            // Initialize pbLayer zoom levels to use the discrete factors from ZoomLevels
            pbLayer.ZoomLevels = new ZoomLevelCollection(ZoomLevels);
            // Initialize the zoom level used for autozoom based on the stored default settings.
            LockedZoomLevel = ZoomLevels[Settings.Default.ZoomLockLevel + ZoomLevelSkipCount];

            if (Settings.Default.StartMaximized || Width >= Screen.FromControl(this).WorkingArea.Width ||
                Height >= Screen.FromControl(this).WorkingArea.Height)
            {
                WindowState = FormWindowState.Maximized;
            }


            DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            };
            DragDrop += (s, e) => { ProcessFile((string[]) e.Data.GetData(DataFormats.FileDrop)); };

            foreach (var menuItem in MenuTools)
            {
                var item = new ToolStripMenuItem(menuItem.Operation.Title)
                {
                    AutoToolTip = true,
                    ToolTipText = menuItem.Operation.Description,
                    Tag = menuItem.Operation,
                    Image = menuItem.Icon
                };
                item.Click += EventClick;
                menuTools.DropDownItems.Add(item);
            }
            foreach (var menuItem in LayerActions)
            {
                var item = new ToolStripMenuItem(menuItem.Operation.Title)
                {
                    ToolTipText = menuItem.Operation.Description,
                    Tag = menuItem.Operation,
                    AutoToolTip = true,
                    Image = menuItem.Icon
                };
                item.Click += EventClick;
                btnLayerImageActions.DropDownItems.Add(item);
            }

            foreach (PixelDrawing.BrushShapeType brushShape in (PixelDrawing.BrushShapeType[]) Enum.GetValues(
                typeof(PixelDrawing.BrushShapeType)))
            {
                cbPixelEditorDrawingBrushShape.Items.Add(brushShape);
            }

            cbPixelEditorDrawingBrushShape.SelectedIndex = 0;

            foreach (LineType lineType in (LineType[]) Enum.GetValues(
                typeof(LineType)))
            {
                if (lineType == LineType.Filled) continue;
                cbPixelEditorDrawingLineType.Items.Add(lineType);
                cbPixelEditorTextLineType.Items.Add(lineType);
            }

            cbPixelEditorDrawingLineType.SelectedItem = LineType.AntiAlias;
            cbPixelEditorTextLineType.SelectedItem = LineType.AntiAlias;

            foreach (FontFace font in (FontFace[]) Enum.GetValues(
                typeof(FontFace)))
            {
                cbPixelEditorTextFontFace.Items.Add(font);
            }

            cbPixelEditorTextFontFace.SelectedIndex = 0;
            pixelEditCursor = CursorResourceLoader.LoadEmbeddedCursor(Properties.Resources.pixel_edit);


            tbLayer.MouseWheel += TbLayerOnMouseWheel;

            flvIssues.ShowGroups = btnIssueGroup.Checked;
            flvIssues.AlwaysGroupByColumn = flvIssuesColType;
            flvIssues.PrimarySortColumn = flvIssuesColType;
            flvIssues.PrimarySortOrder = SortOrder.Ascending;
            flvIssues.SecondarySortColumn = flvIssuesColLayerIndex;
            flvIssues.SecondarySortOrder = SortOrder.Ascending;

            Generator.GenerateColumns(lvLog, typeof(LogItem), true);
            lvLog.PrimarySortColumn = lvLog.AllColumns[0];
            lvLog.PrimarySortOrder = SortOrder.Descending;

            flvPixelHistory.ShowGroups = true;
            flvPixelHistory.AlwaysGroupByColumn = flvPixelHistoryColOperation;
            flvPixelHistory.PrimarySortColumn = flvPixelHistoryColNumber;
            flvPixelHistory.PrimarySortOrder = SortOrder.Descending;

            Generator.GenerateColumns(flvProperties, typeof(SlicerPropertyItem), true);
            flvProperties.ShowGroups = true;
            flvProperties.AlwaysGroupByColumn = flvProperties.AllColumns[2];

            if (Settings.Default.CheckForUpdatesOnStartup)
            {
                Task.Factory.StartNew(AppLoadTask);
            }
        }

        private void AppLoadTask()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string htmlCode = client.DownloadString($"{About.Website}/releases");
                    const string searchFor = "/releases/tag/";
                    var startIndex = htmlCode.IndexOf(searchFor, StringComparison.InvariantCultureIgnoreCase) +
                                     searchFor.Length;
                    var endIndex = htmlCode.IndexOf("\"", startIndex, StringComparison.InvariantCultureIgnoreCase);
                    var version = htmlCode.Substring(startIndex, endIndex - startIndex);
                    if (string.Compare(version, $"v{FrmAbout.AssemblyVersion}", StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        Invoke((MethodInvoker) delegate
                        {
                            // Running on the UI thread
                            menuNewVersion.Text = $"New version {version} is available!";
                            menuNewVersion.Tag = $"{About.Website}/releases/tag/{version}";
                            menuNewVersion.Visible = true;
                        });
                    }

                    //Debug.WriteLine(version);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        #endregion

        #region Overrides

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            AddLog("UVtools Start");
            ProcessFile(Program.Args);

            if (SlicerFile is null && Settings.Default.LoadDemoFileOnStartup)
            {
                ProcessFile(About.DemoFile);
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (SlicerFile is null || IsChangingLayer)
            {
                return;
            }
            if (e.KeyChar == 's')
            {
                if (!CanGlobalHotKey) return;
                ShowLayer(false);
                e.Handled = true;
                return;
            }

            if (e.KeyChar == 'w')
            {
                if (!CanGlobalHotKey) return;
                ShowLayer(true);
                e.Handled = true;
                return;
            }

            base.OnKeyPress(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {

            if (SlicerFile is null)
            {
                return;
            }

            if (e.KeyCode == Keys.Home)
            {
                if (!CanGlobalHotKey) return;
                btnFirstLayer.PerformClick();
                e.Handled = true;
                return;
            }



            if (e.KeyCode == Keys.End)
            {
                if (!CanGlobalHotKey) return;
                btnLastLayer.PerformClick();
                e.Handled = true;
                return;
            }

            if (e.Control)
            {
                if (e.KeyCode == Keys.F)
                {
                    btnFindLayer.PerformClick();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.R)
                {
                    btnLayerImageRotate.PerformClick();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.NumPad0 || e.KeyCode == Keys.D0)
                {
                    ZoomToFit();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.Left)
                {
                    tsIssuePrevious.PerformClick();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.Right)
                {
                    tsIssueNext.PerformClick();
                    e.Handled = true;
                    return;
                }
            }
            else
            {
                if (e.KeyCode == Keys.F5)
                {
                    ShowLayer();
                }
            }

            base.OnKeyUp(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Settings.Default.LayerZoomToFit)
            {
                ZoomToFit();
            }

            tbLayer.Invalidate();
            tbLayer.Update();
            tbLayer.Refresh();
            lbActualLayer.Location = new Point(lbActualLayer.Location.X,
                ((int) (tbLayer.Height - (float) tbLayer.Height / tbLayer.Maximum * tbLayer.Value) -
                 lbActualLayer.Height / 2)
                .Clamp(1, tbLayer.Height - lbActualLayer.Height));

            UpdateLayerTrackerHighlightIssues();
        }


        #endregion

        #region Events

        private void EventClick(object sender, EventArgs e)
        {
            if (sender is ToolStripSplitButton splitButton && !ReferenceEquals(sender, tsIssuesDetect))
            {
                splitButton.ShowDropDown();
                return;
            }

            if (sender is ToolStripMenuItem menuItem)
            {
                /*******************
                 *    Main Menu    *
                 ******************/
                // File
                if (ReferenceEquals(menuItem, menuFileOpen))
                {
                    using (OpenFileDialog openFile = new OpenFileDialog())
                    {
                        openFile.CheckFileExists = true;

                        openFile.Filter = FileFormat.AllFileFilters;
                        openFile.FilterIndex = Settings.Default.DefaultOpenFileExtension + 1;
                        openFile.InitialDirectory = Settings.Default.FileOpenDefaultDirectory;
                        if (openFile.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                ProcessFile(openFile.FileName);
                            }
                            catch (Exception exception)
                            {
                                MessageBox.Show(exception.ToString(), "Error while trying to open the file",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }

                    return;
                }

                if (ReferenceEquals(menuItem, menuFileOpenNewWindow))
                {
                    using (OpenFileDialog openFile = new OpenFileDialog())
                    {
                        openFile.CheckFileExists = true;
                        openFile.Filter = FileFormat.AllFileFilters;
                        openFile.FilterIndex = Settings.Default.DefaultOpenFileExtension + 1;
                        openFile.InitialDirectory = Settings.Default.FileOpenDefaultDirectory;
                        if (openFile.ShowDialog() == DialogResult.OK)
                        {
                            Program.NewInstance(openFile.FileName);
                        }
                    }

                    return;
                }

                if (ReferenceEquals(menuItem, menuFileReload))
                {
                    ProcessFile(ActualLayer);
                    return;
                }

                if (ReferenceEquals(menuItem, menuFileSave))
                {
                    SaveFile();
                    return;
                }

                if (ReferenceEquals(menuItem, menuFileSaveAs))
                {
                    using (SaveFileDialog dialog = new SaveFileDialog())
                    {
                        var ext = Path.GetExtension(SlicerFile.FileFullPath);
                        dialog.Filter = $"{ext.Remove(0, 1)} files (*{ext})|*{ext}";
                        dialog.AddExtension = true;
                        dialog.InitialDirectory = string.IsNullOrEmpty(Settings.Default.FileSaveDefaultDirectory)
                            ? Path.GetDirectoryName(SlicerFile.FileFullPath)
                            : Settings.Default.FileSaveDefaultDirectory;
                        dialog.FileName =
                            $"{Settings.Default.FileSaveNamePreffix}{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}{Settings.Default.FileSaveNameSuffix}";
                        if (dialog.ShowDialog() != DialogResult.OK) return;
                        SaveFile(dialog.FileName);
                    }


                    return;
                }

                if (ReferenceEquals(menuItem, menuFileClose))
                {
                    if (menuFileSave.Enabled)
                    {
                        if (MessageBox.Show("There are unsaved changes.  Do you want close this file without saving?",
                                "Close file without save?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) !=
                            DialogResult.Yes)
                        {
                            return;
                        }
                    }

                    Clear();
                    return;
                }

                if (ReferenceEquals(menuItem, menuFileSettings))
                {
                    using (FrmSettings frmSettings = new FrmSettings())
                    {
                        if (frmSettings.ShowDialog() != DialogResult.OK) return;
                    }

                    return;
                }

                if (ReferenceEquals(menuItem, menuFileExit))
                {
                    if (menuFileSave.Enabled)
                    {
                        if (MessageBox.Show("There are unsaved changes, do you want exit without saving?",
                                "Exit without save?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) !=
                            DialogResult.Yes)
                        {
                            return;
                        }
                    }

                    Application.Exit();
                    return;
                }


                if (ReferenceEquals(menuItem, menuFileExtract))
                {
                    using (FolderBrowserDialog folder = new FolderBrowserDialog())
                    {
                        string fileNameNoExt = Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath);
                        folder.SelectedPath = string.IsNullOrEmpty(Settings.Default.FileExtractDefaultDirectory)
                            ? Path.GetDirectoryName(SlicerFile.FileFullPath)
                            : Settings.Default.FileExtractDefaultDirectory;
                        folder.Description =
                            $"A \"{fileNameNoExt}\" folder will be created within your selected folder to dump the contents.";
                        if (folder.ShowDialog() == DialogResult.OK)
                        {
                            string finalPath = Path.Combine(folder.SelectedPath, fileNameNoExt);
                            try
                            {
                                DisableGUI();
                                FrmLoading.SetDescription($"Extracting {Path.GetFileName(SlicerFile.FileFullPath)}");

                                var task = Task.Factory.StartNew(() =>
                                {
                                    try
                                    {
                                        SlicerFile.Extract(finalPath, true, true, FrmLoading.RestartProgress());
                                    }
                                    catch (OperationCanceledException)
                                    {

                                    }
                                    finally
                                    {
                                        Invoke((MethodInvoker) delegate
                                        {
                                            // Running on the UI thread
                                            EnableGUI(true);
                                        });
                                    }
                                });

                                var loadingResult = FrmLoading.ShowDialog();
                                /*if (loadingResult != DialogResult.OK)
                                {
                                    return;
                                }*/

                                if (MessageBox.Show(
                                        $"Extraction to {finalPath} completed in ({FrmLoading.StopWatch.ElapsedMilliseconds / 1000}s)\n\n" + 
                                        "'Yes' to open target folder, 'No' to continue.",
                                        "Extraction complete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                                    DialogResult.Yes)
                                {
                                    using (Process.Start(finalPath))
                                    {
                                    }
                                }
                            }
                            catch (Exception exception)
                            {
                                MessageBox.Show(exception.ToString(), "Error while try extracting the file",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                        }
                    }

                    return;
                }

                // Edit & mutate
                if (menuItem.Tag is Operation baseOperation)
                {
                    ShowRunOperation(baseOperation.GetType());
                    return;
                }

                // About
                if (ReferenceEquals(menuItem, menuHelpAbout))
                {
                    Program.FrmAbout.ShowDialog();
                    return;
                }

                if (ReferenceEquals(menuItem, menuHelpWebsite))
                {
                    using (Process.Start(About.Website))
                    {
                    }

                    return;
                }

                if (ReferenceEquals(menuItem, menuHelpDonate))
                {
                    MessageBox.Show(
                        "All my work here is given for free (OpenSource), it took some hours to build, test and polish the program.\n" +
                        "If would like to contribute for a better program and for my work, I would appreciate the tip.\n" +
                        "A browser window will be open and you will be forwarded to my paypal address after you click 'OK'.\nHappy Printing!",
                        "Donation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    using (Process.Start(About.Donate))
                    {
                    }

                    return;
                }

                if (ReferenceEquals(menuItem, menuHelpBenchmark))
                {
                    using (var frmBenchmark = new FrmBenchmark())
                    {
                        frmBenchmark.ShowDialog();
                    }

                    return;
                }

                if (ReferenceEquals(menuItem, menuHelpInstallPrinters))
                {
                    var PEFolder =
                        $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}PrusaSlicer";
                    if (!Directory.Exists(PEFolder))
                    {
                        var result = MessageBox.Show(
                            "Unable to detect PrusaSlicer on your system, please ensure you have lastest version installed.\n\n" +
                            "Click 'OK' to open the PrusaSlicer webpage for download\n" +
                            "Click 'Cancel' to cancel this operation",
                            "Unable to detect PrusaSlicer",
                            MessageBoxButtons.OKCancel, MessageBoxIcon.Error);

                        switch (result)
                        {
                            case DialogResult.OK:
                                using (Process.Start("https://www.prusa3d.com/prusaslicer/"))
                                {
                                }

                                return;
                            default:
                                return;
                        }
                    }

                    using (FrmInstallPEProfiles form = new FrmInstallPEProfiles())
                    {
                        form.ShowDialog();
                    }

                    return;
                }
            }

            if (ReferenceEquals(sender, menuNewVersion))
            {
                try
                {
                    using (Process.Start(menuNewVersion.Tag.ToString()))
                    {
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);
                }

                return;
            }

            /************************
             *    Thumbnail Menu    *
             ***********************/
            if (ReferenceEquals(sender, tsThumbnailsPrevious))
            {
                byte i = (byte) tsThumbnailsCount.Tag;
                if (i == 0)
                {
                    // This should never happen!
                    tsThumbnailsPrevious.Enabled = false;
                    return;
                }

                tsThumbnailsCount.Tag = --i;

                if (i == 0)
                {
                    tsThumbnailsPrevious.Enabled = false;
                }

                pbThumbnail.Image = SlicerFile.Thumbnails[i]?.ToBitmap();

                tsThumbnailsCount.Text = $"{i + 1}/{SlicerFile.CreatedThumbnailsCount}";
                tsThumbnailsNext.Enabled = true;

                tsThumbnailsResolution.Text = pbThumbnail.Image.PhysicalDimension.ToString();

                AdjustThumbnailSplitter();
                return;
            }

            if (ReferenceEquals(sender, tsThumbnailsNext))
            {
                byte i = byte.Parse(tsThumbnailsCount.Tag.ToString());
                if (i >= SlicerFile.CreatedThumbnailsCount - 1)
                {
                    // This should never happen!
                    tsThumbnailsNext.Enabled = false;
                    return;
                }

                tsThumbnailsCount.Tag = ++i;

                if (i >= SlicerFile.CreatedThumbnailsCount - 1)
                {
                    tsThumbnailsNext.Enabled = false;
                }

                pbThumbnail.Image = SlicerFile.Thumbnails[i]?.ToBitmap();

                tsThumbnailsCount.Text = $"{i + 1}/{SlicerFile.CreatedThumbnailsCount}";
                tsThumbnailsPrevious.Enabled = true;

                tsThumbnailsResolution.Text = pbThumbnail.Image.PhysicalDimension.ToString();

                AdjustThumbnailSplitter();
                return;
            }

            if (ReferenceEquals(sender, tsThumbnailsImport))
            {
                using (var fileOpen = new OpenFileDialog
                {
                    CheckFileExists = true,
                    Filter = "Image Files(*.PNG;*.BMP;*.JPEG;*.JPG;*.GIF)|*.PNG;*.BMP;*.JPEG;*.JPG;*.GIF"
                })
                {
                    if (fileOpen.ShowDialog() != DialogResult.OK) return;
                    byte i = byte.Parse(tsThumbnailsCount.Tag.ToString());
                    SlicerFile.SetThumbnail(i, fileOpen.FileName);
                    pbThumbnail.Image = SlicerFile.Thumbnails[i].ToBitmap();
                    SlicerFile.RequireFullEncode = true;
                    CanSave = true;
                }
            }

            if (ReferenceEquals(sender, tsThumbnailsExportFile))
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    byte i = byte.Parse(tsThumbnailsCount.Tag.ToString());
                    if (ReferenceEquals(SlicerFile.Thumbnails[i], null))
                    {
                        return; // This should never happen!
                    }

                    dialog.Filter = "Image Files|.*png";
                    dialog.AddExtension = true;
                    dialog.FileName =
                        $"{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}_thumbnail{i + 1}.png";

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        SlicerFile.Thumbnails[i].Save(dialog.FileName);
                    }
                }

                return;
            }

            if (ReferenceEquals(sender, tsThumbnailsExportClipboard))
            {
                Clipboard.SetImage(pbThumbnail.Image);
                return;
            }

            /************************
             *   Properties Menu    *
             ***********************/
            if (ReferenceEquals(sender, tsPropertiesExportFile))
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "Ini Files|.*ini";
                    dialog.AddExtension = true;
                    dialog.FileName = $"{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}_properties.ini";

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        using (TextWriter tw = new StreamWriter(dialog.OpenFile()))
                        {
                            foreach (var config in SlicerFile.Configs)
                            {
                                var type = config.GetType();
                                tw.WriteLine($"[{type.Name}]");
                                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                                {
                                    if (property.Name.Equals("Item")) continue;
                                    var value = property.GetValue(config);
                                    switch (value)
                                    {
                                        case null:
                                            continue;
                                        case IList list:
                                            tw.WriteLine($"{property.Name} = {list.Count}");
                                            break;
                                        default:
                                            tw.WriteLine($"{property.Name} = {value}");
                                            break;
                                    }
                                }
                                tw.WriteLine();
                            }

                            tw.Close();
                        }

                        if (MessageBox.Show(
                                "Properties save was successful. Do you want open the file in the default editor?",
                                "Properties save complete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                            DialogResult.Yes)
                        {
                            using (Process.Start(dialog.FileName))
                            {
                            }
                        }
                    }

                    return;
                }
            }

            if (ReferenceEquals(sender, tsPropertiesExportClipboard))
            {
                StringBuilder sb = new StringBuilder();
                foreach (var config in SlicerFile.Configs)
                {
                    var type = config.GetType();
                    sb.AppendLine($"[{type.Name}]");
                    foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (property.Name.Equals("Item")) continue;
                        var value = property.GetValue(config);
                        switch (value)
                        {
                            case null:
                                continue;
                            case IList list:
                                sb.AppendLine($"{property.Name} = {list.Count}");
                                break;
                            default:
                                sb.AppendLine($"{property.Name} = {value}");
                                break;
                        }
                    }

                    sb.AppendLine();
                }

                Clipboard.SetText(sb.ToString());
                return;
            }

            /************************
             *      GCode Menu      *
             ***********************/
            if (ReferenceEquals(sender, btnGCodeRebuild))
            {
                SlicerFile.RebuildGCode();
                UpdateGCode();
                return;
            }

            if (ReferenceEquals(sender, btnGCodeSaveFile))
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "Text Files|.*txt";
                    dialog.AddExtension = true;
                    dialog.FileName = $"{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}_gcode.txt";

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        using (TextWriter tw = new StreamWriter(dialog.OpenFile()))
                        {
                            tw.Write(SlicerFile.GCode);
                            tw.Close();
                        }

                        if (MessageBox.Show(
                                "GCode save was successful.  Do you want open the file in the default editor?",
                                "GCode save complete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) !=
                            DialogResult.Yes) return;
                        using (Process.Start(dialog.FileName))
                        {
                        }
                    }

                    return;
                }
            }

            if (ReferenceEquals(sender, btnGCodeSaveClipboard))
            {
                Clipboard.SetText(SlicerFile.GCode.ToString());
                return;
            }

            /************************
             *     Issues Menu    *
             ***********************/
            if (ReferenceEquals(sender, tsIssuePrevious))
            {
                if (!tsIssuePrevious.Enabled) return;
                int index = Convert.ToInt32(tsIssueCount.Tag);
                flvIssues.SelectedIndices.Clear();
                //flvIssues.SelectObject(Issues[--index]);
                flvIssues.SelectedIndex = --index;
                flvIssues.EnsureVisible(index); // Keep selection on screen
                EventItemActivate(flvIssues, EventArgs.Empty);
                return;
            }

            if (ReferenceEquals(sender, tsIssueNext))
            {
                if (!tsIssueNext.Enabled) return;
                int index = Convert.ToInt32(tsIssueCount.Tag);
                flvIssues.SelectedIndices.Clear();
                //flvIssues.SelectObject(Issues[++index]);
                flvIssues.SelectedIndex = ++index;
                flvIssues.EnsureVisible(index); // Keep selection on screen
                EventItemActivate(flvIssues, EventArgs.Empty);
                return;
            }


            if (ReferenceEquals(sender, tsIssueRemove))
            {
                if (!tsIssueRemove.Enabled || ReferenceEquals(Issues, null)) return;

                if (MessageBox.Show("Are you sure you want to remove all selected issues?\n\n" +
                                    "Warning: Removing an island can cause other issues to appear if there is material present in the layers above it.\n" +
                                    "Always check previous and next layers before performing an island removal.", "Remove Issues?",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

                Dictionary<uint, List<LayerIssue>> processIssues = new Dictionary<uint, List<LayerIssue>>();
                List<uint> layersRemove = new List<uint>();


                foreach (LayerIssue issue in flvIssues.SelectedObjects)
                {
                    //if (!issue.HaveValidPoint) continue;
                    if (issue.Type == LayerIssue.IssueType.TouchingBound) continue;

                    if (!processIssues.TryGetValue(issue.Layer.Index, out var issueList))
                    {
                        issueList = new List<LayerIssue>();
                        processIssues.Add(issue.Layer.Index, issueList);
                    }

                    issueList.Add(issue);
                    if (issue.Type == LayerIssue.IssueType.EmptyLayer)
                    {
                        layersRemove.Add(issue.Layer.Index);
                    }
                }


                DisableGUI();
                FrmLoading.SetDescription("Removing selected issues");
                var progress = FrmLoading.RestartProgress(false);

                progress.Reset("Removing selected issues", (uint) processIssues.Count);
                Task<bool> task = Task<bool>.Factory.StartNew(() =>
                {
                    bool result = false;
                    try
                    {
                        Parallel.ForEach(processIssues, layerIssues =>
                        {
                            if (progress.Token.IsCancellationRequested) return;
                            using (var image = SlicerFile[layerIssues.Key].LayerMat)
                            {
                                var bytes = image.GetPixelSpan<byte>();

                                bool edited = false;

                                foreach (var issue in layerIssues.Value)
                                {
                                    if (issue.Type == LayerIssue.IssueType.Island)
                                    {
                                        foreach (var pixel in issue)
                                        {
                                            bytes[image.GetPixelPos(pixel.X, pixel.Y)] = 0;
                                        }

                                        edited = true;
                                    }
                                    else if (issue.Type == LayerIssue.IssueType.ResinTrap)
                                    {
                                        using (var contours =
                                            new VectorOfVectorOfPoint(new VectorOfPoint(issue.Pixels)))
                                        {
                                            CvInvoke.DrawContours(image, contours, -1, new MCvScalar(255), -1);
                                            //CvInvoke.DrawContours(image, contours, -1, new MCvScalar(255), 2);
                                        }

                                        edited = true;
                                    }

                                }

                                if (edited)
                                {
                                    SlicerFile[layerIssues.Key].LayerMat = image;
                                    result = true;
                                }
                            }

                            progress++;
                        });

                        if (layersRemove.Count > 0)
                        {
                            SlicerFile.LayerManager.RemoveLayers(layersRemove);
                            result = true;
                        }

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "Removal failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        Invoke((MethodInvoker) delegate
                        {
                            // Running on the UI thread
                            EnableGUI(true);
                        });
                    }

                    return result;
                });

                FrmLoading.ShowDialog();

                if (!task.Result) return;

                var whiteListLayers = new List<uint>();

                // Update GUI
                List<LayerIssue> removeSelectedObjects = new List<LayerIssue>();
                foreach (LayerIssue issue in flvIssues.SelectedObjects)
                {
                    //if (!issue.HaveValidPoint) continue;
                    if (issue.Type != LayerIssue.IssueType.Island &&
                        issue.Type != LayerIssue.IssueType.ResinTrap &&
                        issue.Type != LayerIssue.IssueType.EmptyLayer) continue;

                    Issues.Remove(issue);
                    removeSelectedObjects.Add(issue);

                    if (issue.Type == LayerIssue.IssueType.Island)
                    {
                        var nextLayer = issue.Layer.Index + 1;
                        if (nextLayer >= SlicerFile.LayerCount) continue;
                        if (whiteListLayers.Contains(nextLayer)) continue;
                        whiteListLayers.Add(nextLayer);
                    }
                }

                flvIssues.RemoveObjects(removeSelectedObjects);

                if (layersRemove.Count > 0)
                {
                    UpdateLayerLimits();
                    RefreshInfo();
                }

                if (Settings.Default.PartialUpdateIslandsOnEditing)
                {
                    UpdateIslandsOverhangs(whiteListLayers);
                }

                //ShowLayer(); // It will call latter so its a extra call
                UpdateIssuesInfo();
                CanSave = true;

                return;
            }

            if (ReferenceEquals(sender, tsIssuesRepair))
            {
                ShowRunOperation(typeof(OperationRepairLayers));
                return;
            }

            if (ReferenceEquals(sender, tsIssuesDetect))
            {
                /*if (MessageBox.Show("Are you sure you want to compute issues?", "Issues Compute",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;*/

                ComputeIssues(
                    GetIslandDetectionConfiguration(), 
                    GetOverhangDetectionConfiguration(), 
                    GetResinTrapDetectionConfiguration(),
                    GetTouchingBoundsDetectionConfiguration(), 
                    tsIssuesDetectEmptyLayers.Checked);

                return;
            }

            if (ReferenceEquals(sender, btnIssueGroup))
            {
                flvIssues.ClearObjects();
                flvIssues.ShowGroups = btnIssueGroup.Checked;
                UpdateIssuesList();
                return;
            }

            if (ReferenceEquals(sender, btnIssueResort))
            {
                flvIssues.PrimarySortColumn = flvIssuesColType;
                flvIssues.PrimarySortOrder = SortOrder.Ascending;
                flvIssues.SecondarySortColumn = flvIssuesColLayerIndex;
                flvIssues.SecondarySortOrder = SortOrder.Ascending;
                flvIssues.RebuildColumns();
                return;
            }

            if (ReferenceEquals(sender, btnPixelHistoryRemove))
            {
                if (!btnPixelHistoryRemove.Enabled) return;
                if (flvPixelHistory.SelectedIndices.Count == 0) return;
                foreach (PixelOperation operation in flvPixelHistory.SelectedObjects)
                {
                    PixelHistory.Items.Remove(operation);
                }

                RefreshPixelHistory();

                ShowLayer();
            }

            if (ReferenceEquals(sender, btnPixelHistoryClear))
            {
                if (!btnPixelHistoryApply.Enabled) return;
                if (PixelHistory.Count == 0) return;
                if (MessageBox.Show(
                    "Are you sure you want to clear all operations?",
                    "Clear operations?", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes) return;

                PixelHistory.Clear();
                RefreshPixelHistory();
                ShowLayer();

            }

            if (ReferenceEquals(sender, btnPixelHistoryApply))
            {
                if (!btnPixelHistoryApply.Enabled) return;
                DrawModifications(false);
            }

            if (ReferenceEquals(sender, btnLogClear))
            {
                if (MessageBox.Show(
                    "Are you sure you want to clear the log?",
                    "Clear log?", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes) return;

                lvLog.ClearObjects();
                AddLog("Log cleared");
            }


            /************************
             *      Layer Menu      *
             ***********************/
            if (ReferenceEquals(sender, btnLayerImageRotate) ||
                ReferenceEquals(sender, btnLayerImageLayerDifference) ||
                ReferenceEquals(sender, btnLayerImageHighlightIssues) ||
                ReferenceEquals(sender, btnLayerImageShowCrosshairs) ||
                ReferenceEquals(sender, btnLayerImageLayerOutlinePrintVolumeBounds) ||
                ReferenceEquals(sender, btnLayerImageLayerOutlineLayerBounds) ||
                ReferenceEquals(sender, btnLayerImageLayerOutlineHollowAreas) ||
                ReferenceEquals(sender, btnLayerImageLayerOutlineEdgeDetection)

            )
            {
                ShowLayer();
                if (ReferenceEquals(sender, btnLayerImageRotate))
                {
                    // Arrange selection rotation
                    var rectangleF = pbLayer.SelectionRegion;
                    if (!rectangleF.IsEmpty)
                    {
                        var rectangle = Rectangle.Round(rectangleF);
                        pbLayer.SelectionRegion = GetTransposedRectangle(rectangle, btnLayerImageRotate.Checked, true);
                    }
                    
                    ZoomToFit();
                }

                return;
            }

            if (ReferenceEquals(sender, btnLayerImagePixelEdit))
            {
                if (btnLayerImagePixelEdit.Checked)
                {
                    tabControlLeft.TabPages.Add(tabPagePixelEditor);
                    tabControlLeft.SelectedTab = tabPagePixelEditor;
                }
                else
                {
                    DrawModifications(true);
                }

                return;
            }

            if (ReferenceEquals(sender, btnLayerImageExportFile))
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    if (ReferenceEquals(pbLayer, null))
                    {
                        return; // This should never happen!
                    }

                    dialog.Filter = "Image Files|.*png";
                    dialog.AddExtension = true;
                    dialog.FileName =
                        $"{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}_layer{ActualLayer}.png";

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        pbLayer.Image.Save(dialog.FileName);
                    }
                }

                return;
            }

            if (ReferenceEquals(sender, btnLayerImageExportClipboard))
            {
                if (ReferenceEquals(pbLayer, null))
                {
                    return; // This should never happen!
                }

                Clipboard.SetImage(pbLayer.Image);

                return;
            }

            if (ReferenceEquals(sender, btnFirstLayer))
            {
                ShowLayer(0);
                return;
            }

            if (ReferenceEquals(sender, btnPreviousLayer))
            {
                ShowLayer(false);
                return;
            }

            if (ReferenceEquals(sender, btnNextLayer))
            {
                ShowLayer(true);
                return;
            }

            if (ReferenceEquals(sender, btnLastLayer))
            {
                ShowLayer(SlicerFile.LayerCount - 1);
                return;
            }

            if (ReferenceEquals(sender, btnFindLayer))
            {
                using (FrmInputBox inputBox = new FrmInputBox("Go to layer", "Select a layer to go to",
                    ActualLayer, null, 0, SlicerFile.LayerCount - 1, 0, "Layer"))
                {
                    if (inputBox.ShowDialog() == DialogResult.OK)
                    {
                        ShowLayer((uint) inputBox.NewValue);
                    }
                }

                return;
            }

            if (ReferenceEquals(sender, btnLayerBounds))
            {
                CenterLayerAt(GetTransposedRectangle(SlicerFile[ActualLayer].BoundingRectangle), 0, true);
                return;
            }

            if (ReferenceEquals(sender, btnLayerROI))
            {
                var roi = pbLayer.SelectionRegion;
                if (roi.IsEmpty) return;

                if ((ModifierKeys & Keys.Shift) != 0)
                {
                    pbLayer.SelectNone();
                    return;
                }

                CenterLayerAt(roi, 0, true);
                return;
            }

            if (ReferenceEquals(sender, btnLayerMouseLocation))
            {
                if (btnLayerMouseLocation.Tag is Point point)
                {
                    CenterLayerAt(point);
                }
                return;
            }

            if (ReferenceEquals(sender, btnLayerResolution))
            {
                pbLayer.ZoomToFit();
                return;
            }
        }

        private void ValueChanged(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, tbLayer))
            {
                if (tbLayer.Value == ActualLayer) return;
                /*Debug.WriteLine($"{tbLayer.Height} + {tbLayer.Value} * ({tbLayer.Height} / {tbLayer.Maximum})");
                lbLayerActual.Location = new Point(lbLayerActual.Location.X, (int) (tbLayer.Height - tbLayer.Value * ((float)tbLayer.Height / tbLayer.Maximum)));
                Debug.WriteLine(lbLayerActual.Location);*/
                ShowLayer((uint) tbLayer.Value);
                return;
            }
        }

        private void ConvertToItemOnClick(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem) sender;
            FileFormat fileFormat = (FileFormat) menuItem.Tag;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.FileName = Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath);
                dialog.Filter = fileFormat.FileFilter;
                dialog.InitialDirectory = string.IsNullOrEmpty(Settings.Default.FileConvertDefaultDirectory)
                    ? Path.GetDirectoryName(SlicerFile.FileFullPath)
                    : Settings.Default.FileConvertDefaultDirectory;

                //using (FileFormat instance = (FileFormat)Activator.CreateInstance(type)) 
                //using (CbddlpFile file = new CbddlpFile())



                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    DisableGUI();
                    FrmLoading.SetDescription(
                        $"Converting {Path.GetFileName(SlicerFile.FileFullPath)} to {Path.GetExtension(dialog.FileName)}");

                    Task<bool> task = Task<bool>.Factory.StartNew(() =>
                    {
                        try
                        {
                            return SlicerFile.Convert(fileFormat, dialog.FileName, FrmLoading.RestartProgress());
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            string extraMessage = string.Empty;
                            if (SlicerFile.FileFullPath.EndsWith(".sl1"))
                            {
                                extraMessage = "Note: When converting from SL1 make sure you have the correct printer selected, you MUST use a UVtools base printer.\n" +
                                               "Go to \"Help\" -> \"Install profiles into PrusaSlicer\" to install printers.\n";
                            }

                            MessageBox.Show($"Convertion was not successful! Maybe not implemented...\n{extraMessage}{ex}",
                                "Convertion unsuccessful", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        finally
                        {
                            Invoke((MethodInvoker) delegate
                            {
                                // Running on the UI thread
                                EnableGUI(true);
                            });
                        }

                        return false;
                    });

                    if (FrmLoading.ShowDialog() == DialogResult.OK && task.Result)
                    {
                        if (MessageBox.Show(
                            $"Conversion completed in {FrmLoading.StopWatch.ElapsedMilliseconds / 1000}s\n\n" +
                            $"Do you want to open {Path.GetFileName(dialog.FileName)} in a new window?",
                            "Conversion complete", MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information) == DialogResult.Yes)
                        {
                            Program.NewInstance(dialog.FileName);
                        }
                    }
                    else
                    {
                        try
                        {
                            if (File.Exists(dialog.FileName))
                            {
                                File.Delete(dialog.FileName);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }

                        //MessageBox.Show("Convertion was unsuccessful! Maybe not implemented...", "Convertion unsuccessful", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void pbLayer_SelectionRegionChanged(object sender, EventArgs e)
        {
            var roi = ROI;
            btnLayerROI.Text = roi.IsEmpty ? "ROI: (NS)" : $"ROI: {roi}";
            btnLayerROI.Enabled = !roi.IsEmpty;
        }

        private void pbLayer_Zoomed(object sender, ImageBoxZoomEventArgs e)
        {
            if (SupressLayerZoomEvent) return;
            AddLogVerbose($"Zoomed from {e.OldZoom} to {e.NewZoom}");
            // Update zoom level display in the toolstrip
            if (e.NewZoom == LockedZoomLevel)
            {
                tsLayerImageZoom.Text = $"Zoom: [ {e.NewZoom / 100f}x";
                tsLayerImageZoomLock.Visible = true;
            }
            else
            {
                tsLayerImageZoom.Text = $"Zoom: [ {e.NewZoom / 100f}x ]";
                tsLayerImageZoomLock.Visible = false;
            }

            if (pbLayer.Cursor.Tag is string str || ReferenceEquals(pbLayer.Cursor, pixelEditCursor))
            {
                UpdatePixelEditorCursor();
            }

            // Refresh the layer to properly render the crosshair at various zoom transitions
            if (btnLayerImageShowCrosshairs.Checked &&
                !ReferenceEquals(Issues, null) &&
                (e.OldZoom < 50 &&
                 e.NewZoom >= 50 // Trigger refresh as crosshair thickness increases at lower zoom levels
                 || e.OldZoom > 100 && e.NewZoom <= 100
                 || e.OldZoom >= 50 && e.OldZoom <= 100 && (e.NewZoom < 50 || e.NewZoom > 100)
                 || e.OldZoom <= CrosshairFadeLevel &&
                 e.NewZoom > CrosshairFadeLevel // Trigger refresh as zoom level manually crosses fade threshold
                 || e.OldZoom > CrosshairFadeLevel && e.NewZoom <= CrosshairFadeLevel)

            )
            {
                if (Settings.Default.CrosshairShowOnlyOnSelectedIssues)
                {
                    if (flvIssues.SelectedIndices.Count == 0 || !flvIssues.SelectedObjects.Cast<LayerIssue>().Any(
                        issue => // Find a valid candidate to update layer preview, otherwise quit
                            issue.LayerIndex == ActualLayer && issue.Type != LayerIssue.IssueType.EmptyLayer &&
                            issue.Type != LayerIssue.IssueType.TouchingBound)) return;
                }
                else
                {
                    if (!Issues.Any(
                        issue => // Find a valid candidate to update layer preview, otherwise quit
                            issue.LayerIndex == ActualLayer && issue.Type != LayerIssue.IssueType.EmptyLayer &&
                            issue.Type != LayerIssue.IssueType.TouchingBound)) return;
                }

                // A timer is used here rather than invoking ShowLayer directly to eliminate sublte visual flashing
                // that will occur on the transition when the crosshair fades or unfades if ShowLayer is called directly.
                layerZoomTimer.Start();
            }
        }

        private Point _lastPixelMouseLocation = Point.Empty;

        private void pbLayer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!pbLayer.IsPointInImage(e.Location)) return;
            var location = pbLayer.PointToImage(e.Location);

            if ((ModifierKeys & Keys.Control) != 0)
            {
                Point realLocation = GetTransposedPoint(location);
                btnLayerMouseLocation.Text =
                    $"{{X={realLocation.X}, Y={realLocation.Y}, B={ActualLayerImage.GetByte(realLocation)}}}";
                btnLayerMouseLocation.Tag = location;
                btnLayerMouseLocation.Enabled = true;
            }
            
            if ((ModifierKeys & Keys.Shift) == 0) return;

            if (_lastPixelMouseLocation == location) return;
            _lastPixelMouseLocation = location;
            

            // Bail here if we're not in a draw operation, if the mouse button is not either
            // left or right, or if the location of the mouse pointer is not within the image.
            if (tabControlPixelEditor.SelectedIndex != (int) PixelOperation.PixelOperationType.Drawing) return;
            if (!btnLayerImagePixelEdit.Checked || (e.Button & MouseButtons.Middle) != 0) return;
            //if (!pbLayer.IsPointInImage(e.Location)) return;

            if (e.Button == MouseButtons.Right)
            {
                // Right or Alt-Left will remove a pixel
                DrawPixel(false ^ (ModifierKeys & Keys.Alt) != 0, location);
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                // Left or Alt-Right will add a pixel
                DrawPixel(true ^ (ModifierKeys & Keys.Alt) != 0, location);
                return;
            }
        }

        private void EventItemActivate(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, flvIssues))
            {
                if (!(flvIssues.SelectedObject is LayerIssue issue)) return;

                if (issue.Type == LayerIssue.IssueType.TouchingBound || issue.Type == LayerIssue.IssueType.EmptyLayer ||
                    (issue.X == -1 && issue.Y == -1))
                {
                    ZoomToFit();
                }
                else if (issue.X >= 0 && issue.Y >= 0)
                {
                    if (Settings.Default.ZoomIssues ^ (ModifierKeys & Keys.Alt) != 0)
                    {
                        ZoomToIssue(issue);
                    }
                    else
                    {
                        //CenterLayerAt(GetTransposedIssueBounds(issue));
                        // If issue is not already visible, center on it and bring it into view.
                        // Issues already in view will not be centered, though their color may
                        // change and the crosshair may move to reflect active selections.

                        if (!Rectangle.Round(pbLayer.GetSourceImageRegion()).Contains(GetTransposedIssueBounds(issue)))
                        {
                            CenterAtIssue(issue);
                        }
                    }
                }

                // Unconditionally refresh layer preview here, even if layer is already active.
                // This ensures highlight colors are updated to reflect active selections within
                // the current layer and the crosshair is refreshed.
                ShowLayer(issue.Layer.Index);

                tsIssueCount.Tag = flvIssues.SelectedIndices[0];
                UpdateIssuesInfo();
                return;
            }

            if (ReferenceEquals(sender, flvPixelHistory))
            {
                if (!(flvPixelHistory.SelectedObject is PixelOperation operation)) return;

                Point location = GetTransposedPoint(operation.Location, false);

                if (Settings.Default.ZoomIssues ^ (ModifierKeys & Keys.Alt) != 0)
                {
                    CenterLayerAt(new Rectangle(location, operation.Size), LockedZoomLevel);
                }
                else
                {
                    CenterLayerAt(location);
                }

                // Unconditionally refresh layer preview here to ensure highlighting for pixel
                // operations properly reflects the active selection.
                ShowLayer(operation.LayerIndex);
            }
        }

        private void TbLayerOnMouseWheel(object sender, MouseEventArgs e)
        {
            ((HandledMouseEventArgs) e).Handled = true;
            if (!tbLayer.Enabled) return;
            if (e.Delta > 0)
            {
                ShowLayer(true);
            }
            else if (e.Delta < 0)
            {
                ShowLayer(false);
            }
        }

        private void flvIssues_ItemsChanged(object sender, ItemsChangedEventArgs e)
        {
            UpdateLayerTrackerHighlightIssues();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Closes file and clear UI
        /// </summary>
        void Clear()
        {
            UpdateTitle();

            SlicerFile?.Dispose();
            SlicerFile = null;
            ActualLayer = 0;

            // GUI CLEAN
            pbThumbnail.Image = null;
            pbLayer.Image = null;
            pbLayer.SelectNone();
            lbLayerImageTooltipOverlay.Visible = false;
            pbThumbnail.Image = null;
            tbGCode.Clear();
            tabPageIssues.Tag = null;

            Issues = null;
            flvIssues.ClearObjects();
            UpdateIssuesInfo();

            PixelHistory.Clear();
            RefreshPixelHistory();

            lbMaxLayer.Text =
                lbActualLayer.Text =
                    lbInitialLayer.Text = "???";
            flvProperties.ClearObjects();
            //pbLayers.Value = 0;
            tbLayer.Value = 0;

            statusBar.Items.Clear();
            menuFileConvert.DropDownItems.Clear();

            menuEdit.DropDownItems.Clear();

            foreach (ToolStripItem item in menuTools.DropDownItems)
            {
                item.Enabled = false;
            }

            foreach (ToolStripItem item in tsThumbnails.Items)
            {
                item.Enabled = false;
            }

            foreach (ToolStripItem item in tsLayer.Items)
            {
                item.Enabled = false;
            }

            foreach (ToolStripItem item in tsLayerInfo.Items)
            {
                item.Enabled = false;
            }

            foreach (ToolStripItem item in tsProperties.Items)
            {
                item.Enabled = false;
            }

            foreach (ToolStripItem item in tsIssues.Items)
            {
                item.Enabled = false;
            }

            tsLayerImagePixelCount.Text = "Pixels: 0";
            btnLayerBounds.Text = "Bounds: (Unloaded)";
            btnLayerROI.Text = "ROI: (NA)";
            btnLayerMouseLocation.Text = "{X=0, Y=0}";

            tsThumbnailsResolution.Text =
                tsLayerPreviewTime.Text =
                    btnLayerResolution.Text =
                        tsPropertiesLabelCount.Text =
                            tsPropertiesLabelGroups.Text = string.Empty;


            menuFileReload.Enabled =
                menuFileSave.Enabled =
                    menuFileSaveAs.Enabled =
                        menuFileClose.Enabled =
                            menuFileExtract.Enabled =
                                menuFileConvert.Enabled =
                                    tbLayer.Enabled =
                                        //pbLayers.Enabled = 
                                        menuEdit.Enabled =
                                                menuTools.Enabled =
                                                    btnFirstLayer.Enabled =
                                                        btnNextLayer.Enabled =
                                                            btnPreviousLayer.Enabled =
                                                                btnLastLayer.Enabled =
                                                                    btnFindLayer.Enabled =

                                                                        btnLayerImagePixelEdit.Checked =

                                                                            false;

            tsThumbnailsCount.Text = "0/0";
            tsThumbnailsCount.Tag = null;

            tabControlLeft.TabPages.Remove(tabPageGCode);
            tabControlLeft.TabPages.Remove(tabPageIssues);
            tabControlLeft.TabPages.Remove(tabPagePixelEditor);
            tabControlLeft.SelectedIndex = 0;
        }

        void DisableGUI()
        {
            mainTable.Enabled =
                menu.Enabled = false;
        }

        void EnableGUI(bool closeLoading = false)
        {
            if (closeLoading)
            {
                if (ReferenceEquals(FrmLoading.Progress, null))
                {
                    FrmLoading.DialogResult = DialogResult.OK;
                }
                else
                {
                    FrmLoading.DialogResult = FrmLoading.Progress.Token.IsCancellationRequested
                        ? DialogResult.Cancel
                        : DialogResult.OK;
                }

                //FrmLoading.Close();
            }

            mainTable.Enabled =
                menu.Enabled = true;
        }

        void ProcessFile(uint actualLayer = 0)
        {
            if (ReferenceEquals(SlicerFile, null)) return;
            ProcessFile(SlicerFile.FileFullPath, actualLayer);
        }

        void ProcessFile(string[] files)
        {
            if (ReferenceEquals(files, null)) return;
            foreach (string file in files)
            {
                try
                {
                    ProcessFile(file);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.ToString(), "Error opening the file", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                return;
            }
        }

        void ProcessFile(string fileName, uint actualLayer = 0)
        {
            if (!File.Exists(fileName)) return;
            Clear();

            var fileNameOnly = Path.GetFileName(fileName);
            SlicerFile = FileFormat.FindByExtension(fileName, true, true);
            if (ReferenceEquals(SlicerFile, null)) return;

            DisableGUI();
            FrmLoading.SetDescription($"Decoding {fileNameOnly}");

            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    SlicerFile.Decode(fileName, FrmLoading.RestartProgress());
                }
                catch (OperationCanceledException)
                {
                    SlicerFile.Clear();
                }
                finally
                {
                    Invoke((MethodInvoker) delegate
                    {
                        // Running on the UI thread
                        EnableGUI(true);
                    });
                }
            });

            var loadingResult = FrmLoading.ShowDialog();
            if (loadingResult != DialogResult.OK)
            {
                SlicerFile = null;
                return;
            }

            if (SlicerFile.LayerCount == 0)
            {
                MessageBox.Show("It seems this file has no layers.  Possible causes could be:\n" +
                                "- File is empty\n" +
                                "- File is corrupted\n" +
                                "- File has not been sliced\n" +
                                "- An internal programing error\n\n" +
                                "Please check your file and retry", "Error reading file",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                //Clear();
                return;
            }

            ActualLayerImage?.Dispose();
            ActualLayerImage = SlicerFile[0].LayerMat;

            if (Settings.Default.LayerAutoRotateBestView)
            {
                btnLayerImageRotate.Checked = ActualLayerImage.Height > ActualLayerImage.Width;
            }

            if (!ReferenceEquals(SlicerFile.ConvertToFormats, null))
            {
                foreach (var fileFormatType in SlicerFile.ConvertToFormats)
                {
                    FileFormat fileFormat = FileFormat.FindByType(fileFormatType);
                    //if (fileFormat.GetType() == SlicerFile.GetType()) continue;

                    string extensions = fileFormat.FileExtensions.Length > 0
                        ? $" ({fileFormat.GetFileExtensions()})"
                        : string.Empty;
                    ToolStripMenuItem menuItem =
                        new ToolStripMenuItem(fileFormat.GetType().Name.Replace("File", extensions))
                        {
                            Tag = fileFormat,
                            Image = Resources.layers_16x16
                        };
                    menuItem.Click += ConvertToItemOnClick;
                    menuFileConvert.DropDownItems.Add(menuItem);
                }

                menuFileConvert.Enabled = menuFileConvert.DropDownItems.Count > 0;
            }

            scLeft.Panel1Collapsed = SlicerFile.CreatedThumbnailsCount == 0;
            if (SlicerFile.CreatedThumbnailsCount > 0)
            {
                tsThumbnailsCount.Tag = 0;
                tsThumbnailsCount.Text = $"1/{SlicerFile.CreatedThumbnailsCount}";
                pbThumbnail.Image = SlicerFile.Thumbnails[0]?.ToBitmap();
                tsThumbnailsResolution.Text = pbThumbnail.Image.PhysicalDimension.ToString();

                for (var i = 1; i < tsThumbnails.Items.Count; i++)
                {
                    tsThumbnails.Items[i].Enabled = true;
                }

                tsThumbnailsNext.Enabled = SlicerFile.CreatedThumbnailsCount > 1;
                AdjustThumbnailSplitter();
            }

            foreach (ToolStripItem item in menuTools.DropDownItems)
            {
                item.Enabled = true;
            }

            foreach (ToolStripItem item in tsLayer.Items)
            {
                item.Enabled = true;
            }

            foreach (ToolStripItem item in tsLayerInfo.Items)
            {
                if(ReferenceEquals(item, btnLayerROI) ||
                   ReferenceEquals(item, btnLayerMouseLocation)
                   ) continue;
                item.Enabled = true;
            }

            foreach (ToolStripItem item in tsProperties.Items)
            {
                item.Enabled = true;
            }

            tsPropertiesLabelCount.Text = $"Properties: {flvProperties.GetItemCount()}";
            tsPropertiesLabelGroups.Text = $"Groups: {flvProperties.OLVGroups?.Count ?? 0}";

            menuFileReload.Enabled =
                menuFileClose.Enabled =
                    menuFileExtract.Enabled =
                        menuFileSaveAs.Enabled =
                        tbLayer.Enabled =
                            //pbLayers.Enabled =
                            menuEdit.Enabled =
                                    menuTools.Enabled =

                                        tsIssuesRepair.Enabled =
                                            tsIssuesDetect.Enabled =

                                                btnFindLayer.Enabled =
                                                    true;


            tabControlLeft.TabPages.Insert(1, tabPageIssues);
            if (!ReferenceEquals(SlicerFile.GCode, null))
            {
                tabControlLeft.TabPages.Insert(1, tabPageGCode);
            }



            tabControlLeft.SelectedIndex = 0;
            btnLayerResolution.Text = $"{{Width={SlicerFile.ResolutionX}, Height={SlicerFile.ResolutionY}}}";

            UpdateLayerLimits();
            ShowLayer(Math.Min(actualLayer, SlicerFile.LayerCount - 1));

            RefreshInfo();

            if (Settings.Default.LayerZoomToFit)
            {
                ZoomToFit();
            }

            UpdateTitle();

            if (Settings.Default.ComputeIssuesOnLoad)
            {
                tsIssuesDetect.PerformButtonClick();
            }
        }

        public bool SaveFile(string filepath = null)
        {
            if (filepath is null)
            {
                if (SavesCount == 0 && Settings.Default.FileSavePromptOverwrite)
                {
                    if (MessageBox.Show(
                        "Original input file will be overwritten.  Do you wish to proceed?", "Overwrite file?",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                        return false;
                }

                filepath = SlicerFile.FileFullPath;
            }

            var oldFile = SlicerFile.FileFullPath;
            var tempFile = filepath + FileFormat.TemporaryFileAppend;

            DisableGUI();
            FrmLoading.SetDescription($"Saving {Path.GetFileName(filepath)}");

            Task<bool> task = Task<bool>.Factory.StartNew(() =>
            {
                bool result = false;

                try
                {
                    SlicerFile.SaveAs(tempFile, FrmLoading.RestartProgress());
                    if (File.Exists(filepath))
                    {
                        File.Delete(filepath);
                    }
                    File.Move(tempFile, filepath);
                    SlicerFile.FileFullPath = filepath;
                    result = true;
                }
                catch (OperationCanceledException)
                {
                    SlicerFile.FileFullPath = oldFile;
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error while saving the file", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    Invoke((MethodInvoker)delegate
                    {
                        // Running on the UI thread
                        EnableGUI(true);
                    });
                }

                return result;
            });

            FrmLoading.ShowDialog();

            if (task.Result)
            {
                SavesCount++;
                CanSave = false;
                UpdateTitle();
            }

            return task.Result;
        }

        private void UpdateLayerLimits()
        {
            lbMaxLayer.Text = $"{SlicerFile.TotalHeight}mm\n{SlicerFile.LayerCount - 1}";
            lbInitialLayer.Text = $"{SlicerFile.LayerHeight}mm\n0";
            tbLayer.Maximum = (int) SlicerFile.LayerCount - 1;
        }

        private void UpdateTitle()
        {
            Text = ReferenceEquals(SlicerFile, null)
                ? $"{FrmAbout.AssemblyTitle}   Version: {FrmAbout.AssemblyVersion}"
                : $"{FrmAbout.AssemblyTitle}   File: {Path.GetFileName(SlicerFile.FileFullPath)} ({Math.Round(FrmLoading.StopWatch.ElapsedMilliseconds / 1000m, 2)}s)   Version: {FrmAbout.AssemblyVersion}";

#if DEBUG
            Text += "   [DEBUG]";
#endif
        }

        private void RefreshInfo()
        {
            /*menuEdit.DropDownItems.Clear();

            if (!ReferenceEquals(SlicerFile.PrintParameterModifiers, null))
            {
                foreach (var modifier in SlicerFile.PrintParameterModifiers)
                {
                    ToolStripItem item = new ToolStripMenuItem
                    {
                        Text =
                            $"{modifier.Name} ({SlicerFile.GetValueFromPrintParameterModifier(modifier)}{modifier.ValueUnit})",
                        Tag = modifier,
                        Image = Resources.Wrench_16x16,
                    };
                    menuEdit.DropDownItems.Add(item);

                    item.Click += EventClick;
                }
            }*/

            flvProperties.ClearObjects();

            if (!ReferenceEquals(SlicerFile.Configs, null))
            {
                List<SlicerPropertyItem> items = new List<SlicerPropertyItem>();
                foreach (object config in SlicerFile.Configs)
                {
                    foreach (PropertyInfo propertyInfo in config.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (propertyInfo.Name.Equals("Item")) continue;
                        var value = propertyInfo.GetValue(config);
                        if (ReferenceEquals(value, null)) continue;
                        if (value is IList list)
                        {
                            items.Add(new SlicerPropertyItem(propertyInfo.Name, list.Count.ToString(),
                                config.GetType().Name));
                        }
                        else
                        {
                            items.Add(
                                new SlicerPropertyItem(propertyInfo.Name, value.ToString(), config.GetType().Name));
                        }
                    }
                }

                flvProperties.SetObjects(items);
            }

            tsPropertiesLabelCount.Text = $"Properties: {flvProperties.GetItemCount()}";
            tsPropertiesLabelGroups.Text = $"Groups: {flvProperties.OLVGroups?.Count ?? 0}";

            UpdateGCode();

            statusBar.Items.Clear();
            AddStatusBarItem(nameof(SlicerFile.LayerHeight), SlicerFile.LayerHeight, "mm");
            AddStatusBarItem(nameof(SlicerFile.BottomLayerCount), SlicerFile.BottomLayerCount);
            AddStatusBarItem(nameof(SlicerFile.BottomExposureTime), SlicerFile.BottomExposureTime, "s");
            AddStatusBarItem(nameof(SlicerFile.ExposureTime), SlicerFile.ExposureTime, "s");
            AddStatusBarItem(nameof(SlicerFile.PrintTime), Math.Round(SlicerFile.PrintTime / 3600, 2), "h");
            AddStatusBarItem(nameof(SlicerFile.UsedMaterial), Math.Round(SlicerFile.UsedMaterial, 2), "ml");
            AddStatusBarItem(nameof(SlicerFile.MaterialCost), SlicerFile.MaterialCost, "€");
            AddStatusBarItem(nameof(SlicerFile.MaterialName), SlicerFile.MaterialName);
            AddStatusBarItem(nameof(SlicerFile.MachineName), SlicerFile.MachineName);

            btnLayerResolution.Text = ActualLayerImage.Size.ToString();
        }

        private void UpdateGCode()
        {
            if (SlicerFile.GCode is null) return;
            tbGCode.Text = SlicerFile.GCode.ToString();
            tsGCodeLabelLines.Text = $"Lines: {tbGCode.Lines.Length}";
            tsGcodeLabelChars.Text = $"Chars: {tbGCode.Text.Length}";
        }

        /// <summary>
        /// Draw a crosshair around a rectangle
        /// </summary>
        /// <param name="rect"></param>
        public void DrawCrosshair(Rectangle rect)
        {
            // Gradually increase line thickness from 1 to 3 at the lower-end of the zoom range.
            // This prevents the crosshair lines from disappearing due to being too thin to
            // render at very low zoom factors.
            var lineThickness = (pbLayer.Zoom > 100) ? 1 : (pbLayer.Zoom < 50) ? 3 : 2;
            var color = new MCvScalar(Settings.Default.CrosshairColor.B, Settings.Default.CrosshairColor.G,
                Settings.Default.CrosshairColor.R);


            // LEFT
            Point startPoint = new Point(Math.Max(0, rect.X - Settings.Default.CrosshairLineMargin - 1),
                rect.Y + rect.Height / 2);
            Point endPoint =
                new Point(
                    Settings.Default.CrosshairLineLength == 0
                        ? 0
                        : (int) Math.Max(0, startPoint.X - Settings.Default.CrosshairLineLength + 1),
                    startPoint.Y);

            CvInvoke.Line(ActualLayerImageBgr,
                startPoint,
                endPoint,
                color,
                lineThickness);


            // RIGHT
            startPoint.X = Math.Min(ActualLayerImageBgr.Width,
                rect.Right + Settings.Default.CrosshairLineMargin);
            endPoint.X = Settings.Default.CrosshairLineLength == 0
                ? ActualLayerImageBgr.Width
                : (int) Math.Min(ActualLayerImageBgr.Width, startPoint.X + Settings.Default.CrosshairLineLength - 1);

            CvInvoke.Line(ActualLayerImageBgr,
                startPoint,
                endPoint,
                color,
                lineThickness);

            // TOP
            startPoint = new Point(rect.X + rect.Width / 2,
                Math.Max(0, rect.Y - Settings.Default.CrosshairLineMargin - 1));
            endPoint = new Point(startPoint.X,
                (int) (Settings.Default.CrosshairLineLength == 0
                    ? 0
                    : Math.Max(0, startPoint.Y - Settings.Default.CrosshairLineLength + 1)));


            CvInvoke.Line(ActualLayerImageBgr,
                startPoint,
                endPoint,
                color,
                lineThickness);

            // Bottom
            startPoint.Y = Math.Min(ActualLayerImageBgr.Height, rect.Bottom + Settings.Default.CrosshairLineMargin);
            endPoint.Y = Settings.Default.CrosshairLineLength == 0
                ? ActualLayerImageBgr.Height
                : (int) Math.Min(ActualLayerImageBgr.Height, startPoint.Y + Settings.Default.CrosshairLineLength - 1);

            CvInvoke.Line(ActualLayerImageBgr,
                startPoint,
                endPoint,
                color,
                lineThickness);
        }

        /// <summary>
        /// Reshow current layer
        /// </summary>
        void ShowLayer() => ShowLayer(Math.Min(ActualLayer, SlicerFile.LastLayerIndex));

        void ShowLayer(bool direction)
        {
            if (direction)
            {
                if (ActualLayer < SlicerFile.LayerCount - 1)
                {
                    ShowLayer(ActualLayer + 1);
                }

                return;
            }

            if (ActualLayer > 0)
            {
                ShowLayer(ActualLayer - 1);
            }
        }

        /// <summary>
        /// Shows a layer number
        /// </summary>
        /// <param name="layerNum">Layer number</param>
        unsafe void ShowLayer(uint layerNum)
        {
            if (SlicerFile is null) return;

            //int layerOnSlider = (int)(SlicerFile.LayerCount - layerNum - 1);
            if (tbLayer.Value != layerNum)
            {
                tbLayer.Value = (int) layerNum;
                return;
            }

            if (IsChangingLayer) return;
            IsChangingLayer = true;

            AddLogVerbose($"Show Layer: {layerNum}");

            ActualLayer = layerNum;
            btnLastLayer.Enabled = btnNextLayer.Enabled = layerNum < SlicerFile.LayerCount - 1;
            btnFirstLayer.Enabled = btnPreviousLayer.Enabled = layerNum > 0;


            var layer = SlicerFile[ActualLayer];

            try
            {
                // OLD
                //if(!ReferenceEquals(pbLayer.Image, null))
                //pbLayer.Image?.Dispose(); // SLOW! LET GC DO IT
                //pbLayer.Image = Image.FromStream(SlicerFile.LayerEntries[layerNum].Open());
                //pbLayer.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);


                Stopwatch watch = Stopwatch.StartNew();


                ActualLayerImage?.Dispose();
                ActualLayerImage = SlicerFile[layerNum].LayerMat;
                LayerCache.Image = ActualLayerImage;

                CvInvoke.CvtColor(ActualLayerImage, ActualLayerImageBgr, ColorConversion.Gray2Bgr);

                //var imageSpan = ActualLayerImage.GetPixelSpan<byte>();
                //var imageBgrSpan = ActualLayerImageBgr.GetPixelSpan<byte>();
                var imageSpan = ActualLayerImage.GetBytePointer();
                var imageBgrSpan = ActualLayerImageBgr.GetBytePointer();


                if (btnLayerImageLayerOutlineEdgeDetection.Checked)
                {
                    using (var grayscale = new Mat())
                    {
                        CvInvoke.Canny(ActualLayerImage, grayscale, 80, 40, 3, true);
                        CvInvoke.CvtColor(grayscale, ActualLayerImageBgr, ColorConversion.Gray2Bgr);
                    }
                }
                else if (btnLayerImageLayerDifference.Checked)
                {
                    if (layerNum > 0 && layerNum < SlicerFile.LayerCount - 1)
                    {
                        Mat previousImage = null;
                        Mat nextImage = null;

                        // Can improve performance on >4K images?
                        Parallel.Invoke(
                () => { previousImage = SlicerFile[ActualLayer - 1].LayerMat; },
                            () => { nextImage = SlicerFile[ActualLayer + 1].LayerMat; });

                        /*using (var previousImage = SlicerFile[_actualLayer - 1].LayerMat)
                        using (var nextImage = SlicerFile[_actualLayer + 1].LayerMat)
                        {*/
                        //var previousSpan = previousImage.GetPixelSpan<byte>();
                        //var nextSpan = nextImage.GetPixelSpan<byte>();

                        var previousSpan = previousImage.GetBytePointer();
                        var nextSpan = nextImage.GetBytePointer();

                        int width = ActualLayerImage.Width;
                        int channels = ActualLayerImageBgr.NumberOfChannels;
                        Parallel.For(0, ActualLayerImageBgr.Height, y =>
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int pixel = y * width + x;
                                if (imageSpan[pixel] != 0) continue;
                                Color color = Color.Empty;
                                if (previousSpan[pixel] > 0 && nextSpan[pixel] > 0)
                                {
                                    color = Settings.Default.PreviousNextLayerColor;
                                }
                                else if (previousSpan[pixel] > 0)
                                {
                                    color = Settings.Default.PreviousLayerColor;
                                }
                                else if (nextSpan[pixel] > 0)
                                {
                                    color = Settings.Default.NextLayerColor;
                                }

                                if (color.IsEmpty) continue;
                                var bgrPixel = pixel * channels;
                                imageBgrSpan[bgrPixel] = color.B; // B
                                imageBgrSpan[++bgrPixel] = color.G; // G
                                imageBgrSpan[++bgrPixel] = color.R; // R
                                                                    //imageBgrSpan[++bgrPixel] = color.A; // A
                            }
                        });

                        previousImage.Dispose();
                        nextImage.Dispose();
                    }
                }

                var selectedIssues = flvIssues.SelectedObjects;
                //List<LayerIssue> selectedIssues = (from object t in selectedIssuesRaw where ((LayerIssue) t).LayerIndex == ActualLayer select (LayerIssue) t).ToList();

                if (btnLayerImageHighlightIssues.Checked &&
                    !ReferenceEquals(Issues, null))
                {
                    foreach (var issue in Issues)
                    {
                        if (issue.LayerIndex != ActualLayer) continue;
                        if (!issue.HaveValidPoint) continue;

                        Color color = Color.Empty;

                        if (issue.Type == LayerIssue.IssueType.ResinTrap)
                        {
                            color = selectedIssues.Contains(issue)
                                ? Settings.Default.ResinTrapHLColor
                                : Settings.Default.ResinTrapColor;


                            using (var vec = new VectorOfVectorOfPoint(new VectorOfPoint(issue.Pixels)))
                            {
                                CvInvoke.DrawContours(ActualLayerImageBgr, vec, -1,
                                    new MCvScalar(color.B, color.G, color.R), -1);
                                //CvInvoke.DrawContours(ActualLayerImageBgr, new VectorOfVectorOfPoint(new VectorOfPoint(issue.Pixels)), -1, new MCvScalar(0, 0, 255), 1);
                            }

                            if (btnLayerImageShowCrosshairs.Checked &&
                                !Settings.Default.CrosshairShowOnlyOnSelectedIssues &&
                                pbLayer.Zoom <= CrosshairFadeLevel)
                            {
                                DrawCrosshair(issue.BoundingRectangle);
                            }

                            continue;
                        }

                        switch (issue.Type)
                        {
                            case LayerIssue.IssueType.Overhang:
                                color = selectedIssues.Contains(issue)
                                    ? Settings.Default.OverhangHLColor
                                    : Settings.Default.OverhangColor;
                                if (btnLayerImageShowCrosshairs.Checked &&
                                    !Settings.Default.CrosshairShowOnlyOnSelectedIssues &&
                                    pbLayer.Zoom <= CrosshairFadeLevel)
                                {
                                    DrawCrosshair(issue.BoundingRectangle);
                                }

                                break;
                            case LayerIssue.IssueType.Island:
                                color = selectedIssues.Contains(issue)
                                    ? Settings.Default.IslandHLColor
                                    : Settings.Default.IslandColor;
                                if (btnLayerImageShowCrosshairs.Checked &&
                                    !Settings.Default.CrosshairShowOnlyOnSelectedIssues &&
                                    pbLayer.Zoom <= CrosshairFadeLevel)
                                {
                                    DrawCrosshair(issue.BoundingRectangle);
                                }

                                break;
                            case LayerIssue.IssueType.TouchingBound:
                                color = Settings.Default.TouchingBoundsColor;
                                break;
                        }

                        if(color.IsEmpty) continue;

                        foreach (var pixel in issue)
                        {
                            int pixelPos = ActualLayerImage.GetPixelPos(pixel);
                            byte brightness = imageSpan[pixelPos];
                            if (brightness == 0) continue;

                            int pixelBgrPos = pixelPos * ActualLayerImageBgr.NumberOfChannels;

                            var newColor = color.FactorColor(brightness, 80);

                            imageBgrSpan[pixelBgrPos] = newColor.B; // B
                            imageBgrSpan[pixelBgrPos + 1] = newColor.G; // G
                            imageBgrSpan[pixelBgrPos + 2] = newColor.R; // R
                        }
                    }
                }

                if (btnLayerImageLayerOutlinePrintVolumeBounds.Checked)
                {
                    CvInvoke.Rectangle(ActualLayerImageBgr, SlicerFile.LayerManager.BoundingRectangle,
                        new MCvScalar(Settings.Default.OutlinePrintVolumeBoundsColor.B,
                            Settings.Default.OutlinePrintVolumeBoundsColor.G,
                            Settings.Default.OutlinePrintVolumeBoundsColor.R),
                        Settings.Default.OutlinePrintVolumeBoundsLineThickness);
                }

                if (btnLayerImageLayerOutlineLayerBounds.Checked)
                {
                    CvInvoke.Rectangle(ActualLayerImageBgr, SlicerFile[layerNum].BoundingRectangle,
                        new MCvScalar(Settings.Default.OutlineLayerBoundsColor.B,
                            Settings.Default.OutlineLayerBoundsColor.G, Settings.Default.OutlineLayerBoundsColor.R),
                        Settings.Default.OutlineLayerBoundsLineThickness);
                }

                if (btnLayerImageLayerOutlineHollowAreas.Checked)
                {
                    //CvInvoke.Threshold(ActualLayerImage, grayscale, 1, 255, ThresholdType.Binary);

                    /*
                     * hierarchy[i][0]: the index of the next contour of the same level
                     * hierarchy[i][1]: the index of the previous contour of the same level
                     * hierarchy[i][2]: the index of the first child
                     * hierarchy[i][3]: the index of the parent
                     */
                    for (int i = 0; i < LayerCache.LayerContours.Size; i++)
                    {
                        if ((int)LayerCache.LayerHierarchyJagged.GetValue(0, i, 2) == -1 &&
                            (int)LayerCache.LayerHierarchyJagged.GetValue(0, i, 3) != -1)
                        {
                            //var r = CvInvoke.BoundingRectangle(contours[i]);
                            //CvInvoke.Rectangle(ActualLayerImageBgr, r, new MCvScalar(0, 0, 255), 2);
                            CvInvoke.DrawContours(ActualLayerImageBgr, LayerCache.LayerContours, i,
                                new MCvScalar(Settings.Default.OutlineHollowAreasColor.B,
                                    Settings.Default.OutlineHollowAreasColor.G,
                                    Settings.Default.OutlineHollowAreasColor.R),
                                Settings.Default.OutlineHollowAreasLineThickness);
                        }
                        /*else
                        {
                            CvInvoke.DrawContours(ActualLayerImageBgr, contours, i,
                                new MCvScalar(Settings.Default.ResinTrapColor.B,
                                    Settings.Default.IslandColor.G, Settings.Default.IslandColor.R),
                                2);
                        }*/

                        //if ((int) arr.GetValue(0, i, 2) == -1 && (int) arr.GetValue(0, i, 3) != -1)
                        //    CvInvoke.DrawContours(ActualLayerImageBgr, contours, i, new MCvScalar(0, 0, 0), -1);
                    }
                }

                for (var index = 0; index < PixelHistory.Count; index++)
                {
                    if (PixelHistory[index].LayerIndex != ActualLayer) continue;
                    var operation = PixelHistory[index];
                    if (operation.OperationType == PixelOperation.PixelOperationType.Drawing)
                    {
                        var operationDrawing = (PixelDrawing) operation;
                        var color = operationDrawing.IsAdd
                            ? (flvPixelHistory.SelectedObjects.Contains(operation)
                                ? Settings.Default.PixelEditorAddPixelHLColor
                                : Settings.Default.PixelEditorAddPixelColor)
                            : (flvPixelHistory.SelectedObjects.Contains(operation)
                                ? Settings.Default.PixelEditorRemovePixelHLColor
                                : Settings.Default.PixelEditorRemovePixelColor);
                        if (operationDrawing.BrushSize == 1)
                        {
                            ActualLayerImageBgr.SetByte(operation.Location.X, operation.Location.Y,
                                new[] {color.B, color.G, color.R});
                            continue;
                        }

                        switch (operationDrawing.BrushShape)
                        {
                            case PixelDrawing.BrushShapeType.Rectangle:
                                CvInvoke.Rectangle(ActualLayerImageBgr, operationDrawing.Rectangle,
                                    new MCvScalar(color.B, color.G, color.R), operationDrawing.Thickness,
                                    operationDrawing.LineType);
                                break;
                            case PixelDrawing.BrushShapeType.Circle:
                                CvInvoke.Circle(ActualLayerImageBgr, operation.Location, operationDrawing.BrushSize / 2,
                                    new MCvScalar(color.B, color.G, color.R), operationDrawing.Thickness,
                                    operationDrawing.LineType);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (operation.OperationType == PixelOperation.PixelOperationType.Text)
                    {
                        var operationText = (PixelText) operation;
                        var color = operationText.IsAdd
                            ? (flvPixelHistory.SelectedObjects.Contains(operation)
                                ? Settings.Default.PixelEditorAddPixelHLColor
                                : Settings.Default.PixelEditorAddPixelColor)
                            : (flvPixelHistory.SelectedObjects.Contains(operation)
                                ? Settings.Default.PixelEditorRemovePixelHLColor
                                : Settings.Default.PixelEditorRemovePixelColor);

                        CvInvoke.PutText(ActualLayerImageBgr, operationText.Text, operationText.Location,
                            operationText.Font, operationText.FontScale, new MCvScalar(color.B, color.G, color.R),
                            operationText.Thickness, operationText.LineType, operationText.Mirror);
                    }
                    else if (operation.OperationType == PixelOperation.PixelOperationType.Eraser)
                    {
                        if (imageSpan[ActualLayerImage.GetPixelPos(operation.Location)] < 10) continue;
                        var color = flvPixelHistory.SelectedObjects.Contains(operation)
                            ? Settings.Default.PixelEditorRemovePixelHLColor
                            : Settings.Default.PixelEditorRemovePixelColor;
                        for (int i = 0; i < LayerCache.LayerContours.Size; i++)
                        {
                            if (CvInvoke.PointPolygonTest(LayerCache.LayerContours[i], operation.Location, false) >= 0)
                            {
                                CvInvoke.DrawContours(ActualLayerImageBgr, LayerCache.LayerContours, i,
                                    new MCvScalar(color.B, color.G, color.R), -1);
                                break;
                            }
                        }
                    }
                    else if (operation.OperationType == PixelOperation.PixelOperationType.Supports)
                    {
                        var operationSupport = (PixelSupport) operation;
                        var color = flvPixelHistory.SelectedObjects.Contains(operation)
                            ? Settings.Default.PixelEditorSupportHLColor
                            : Settings.Default.PixelEditorSupportColor;

                        CvInvoke.Circle(ActualLayerImageBgr, operation.Location, operationSupport.TipDiameter / 2,
                            new MCvScalar(color.B, color.G, color.R), -1);
                    }
                    else if (operation.OperationType == PixelOperation.PixelOperationType.DrainHole)
                    {
                        var operationDrainHole = (PixelDrainHole) operation;
                        var color = flvPixelHistory.SelectedObjects.Contains(operation)
                            ? Settings.Default.PixelEditorDrainHoleHLColor
                            : Settings.Default.PixelEditorDrainHoleColor;

                        CvInvoke.Circle(ActualLayerImageBgr, operation.Location, operationDrainHole.Diameter / 2,
                            new MCvScalar(color.B, color.G, color.R), -1);
                    }
                }

                // Show crosshairs for selected issues if crosshair mode is enabled via toolstrip button.
                // Even when enabled, crosshairs are hidden in pixel edit mode when SHIFT is pressed.
                if (btnLayerImageShowCrosshairs.Checked &&
                    Settings.Default.CrosshairShowOnlyOnSelectedIssues &&
                    !ReferenceEquals(Issues, null) &&
                    flvIssues.SelectedIndices.Count > 0 &&
                    pbLayer.Zoom <=
                    CrosshairFadeLevel && // Only draw crosshairs when zoom level is below the configurable crosshair fade threshold.
                    !(btnLayerImagePixelEdit.Checked && (ModifierKeys & Keys.Shift) != 0))
                {


                    foreach (LayerIssue issue in selectedIssues)
                    {
                        // Don't render crosshairs for selected issue that are not on the current layer, or for 
                        // issue types that don't have a specific location or bounds.
                        if (issue.LayerIndex != ActualLayer || issue.Type == LayerIssue.IssueType.EmptyLayer
                                                            || issue.Type == LayerIssue.IssueType.TouchingBound)
                            continue;

                        DrawCrosshair(issue.BoundingRectangle);
                    }
                }

                if (btnLayerImageRotate.Checked)
                {
                    CvInvoke.Rotate(ActualLayerImageBgr, ActualLayerImageBgr, RotateFlags.Rotate90Clockwise);
                    /*var roi = Rectangle.Round(pbLayer.SelectionRegion);
                    if (roi != Rectangle.Empty)
                    {
                        pbLayer.SelectionRegion = G
                    }*/
                }


                //watch.Restart();
                var imageBmp = ActualLayerImageBgr.ToBitmap();
                //imageBmp.MakeTransparent();
                pbLayer.Image = imageBmp;
                pbLayer.Image.Tag = ActualLayerImageBgr;
                //Debug.WriteLine(watch.ElapsedMilliseconds);


                byte percent = (byte) ((layerNum + 1) * 100 / SlicerFile.LayerCount);

                float pixelPercent =
                    (float) Math.Round(
                        layer.NonZeroPixelCount * 100f / (SlicerFile.ResolutionX * SlicerFile.ResolutionY), 2);
                tsLayerImagePixelCount.Text = $"Pixels: {layer.NonZeroPixelCount} ({pixelPercent}%)";
                btnLayerBounds.Text = $"Bounds: {layer.BoundingRectangle}";
                tsLayerImagePixelCount.Invalidate();
                btnLayerBounds.Invalidate();
                tsLayerInfo.Update();
                tsLayerInfo.Refresh();

                watch.Stop();
                tsLayerPreviewTime.Text = $"{watch.ElapsedMilliseconds}ms";
                //lbLayers.Text = $"{SlicerFile.GetHeightFromLayer(layerNum)} / {SlicerFile.TotalHeight}mm\n{layerNum} / {SlicerFile.LayerCount-1}\n{percent}%";
                lbActualLayer.Text = $"{layer.PositionZ}mm\n{ActualLayer}\n{percent}%";
                lbActualLayer.Location = new Point(lbActualLayer.Location.X,
                    ((int) (tbLayer.Height - (float) tbLayer.Height / tbLayer.Maximum * tbLayer.Value) -
                     lbActualLayer.Height / 2)
                    .Clamp(1, tbLayer.Height - lbActualLayer.Height));

                //pbLayers.Value = percent;
                lbActualLayer.Invalidate();
                lbActualLayer.Update();
                lbActualLayer.Refresh();
                pbLayer.Invalidate();
                pbLayer.Update();
                pbLayer.Refresh();
                //Application.DoEvents();

            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            IsChangingLayer = false;
        }

        void AddStatusBarItem(string name, object item, string extraText = "")
        {
            if (ReferenceEquals(item, null)) return;
            //if (item.ToString().Equals(0)) return;
            if (statusBar.Items.Count > 0)
                statusBar.Items.Add(new ToolStripSeparator());

            ToolStripLabel label = new ToolStripLabel($"{name}: {item}{extraText}");
            statusBar.Items.Add(label);
        }

        void AdjustThumbnailSplitter()
        {
            scLeft.SplitterDistance = Math.Min(pbThumbnail.Image.Height + 5, 400);
        }

        private void EventSelectedIndexChanged(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, flvIssues))
            {
                tsIssueRemove.Enabled = flvIssues.SelectedIndices.Count > 0;

                // If selected index change has resulted in a single selected issue,
                // activate it immediately. Otherwise, selection was cleared or multiple
                // items are selected, and in either case, update layer preview
                // to refresh selection highlighting and crosshair locations.
                if (flvIssues.SelectedIndices.Count == 1)
                {
                    EventItemActivate(flvIssues, EventArgs.Empty);
                }
                else
                {
                    ShowLayer();
                }

                return;
            }

            if (ReferenceEquals(sender, flvPixelHistory))
            {
                btnPixelHistoryRemove.Enabled = flvPixelHistory.SelectedIndices.Count > 0;

                // If single item is selected, activate it, otherwise, just refresh layer preview.
                if (flvPixelHistory.SelectedIndices.Count == 1)
                {
                    EventItemActivate(flvPixelHistory, EventArgs.Empty);
                }
                else
                {
                    ShowLayer();
                }

                return;
            }

            if (ReferenceEquals(sender, tabControlLeft))
            {
                if (!ReferenceEquals(tabControlLeft.SelectedTab, tabPagePixelEditor))
                {
                    // Remember the last tab to be selected.  This is the tab that will
                    // opened when Pixel Editor closes and it's tab is removed from the control.
                    ControlLeftLastTab = tabControlLeft.SelectedTab;
                }

                if (ReferenceEquals(tabControlLeft.SelectedTab, tabPageIssues))
                {
                    if (!ReferenceEquals(tabPageIssues.Tag, null) ||
                        !Settings.Default.AutoComputeIssuesClickOnTab) return;
                    tsIssuesDetect.PerformButtonClick();
                }

                return;
            }

            if (ReferenceEquals(sender, cbPixelEditorDrawingBrushShape))
            {
                if (cbPixelEditorDrawingBrushShape.SelectedIndex == (int) PixelDrawing.BrushShapeType.Rectangle)
                {
                    nmPixelEditorDrawingBrushSize.Minimum = PixelDrawing.MinRectangleBrush;
                    return;
                }

                if (cbPixelEditorDrawingBrushShape.SelectedIndex == (int) PixelDrawing.BrushShapeType.Circle)
                {
                    nmPixelEditorDrawingBrushSize.Minimum = PixelDrawing.MinCircleBrush;
                    return;
                }

                return;
            }
        }

        private void EventKeyDown(object sender, KeyEventArgs e)
        {
            // We handle this event at the to top level rather than pbLayer to ensure
            // the cross cursor is displayed even before the pblayer control has focus.
            // This ensures the user is aware that even in this case, a click in the layer
            // preview will draw a pixel.
            if (!ReferenceEquals(sender, this) || ReferenceEquals(SlicerFile, null)) return;
            
            // This event repeats for as long as the key is pressed, so if we've
            // already set the cursor from a previous key down event, just return.
            if (!ReferenceEquals(pbLayer.Cursor.Tag, null) || pbLayer.Cursor == pixelEditCursor || pbLayer.Cursor == Cursors.Cross
                || pbLayer.Cursor == Cursors.Hand || pbLayer.SelectionMode == ImageBoxSelectionMode.Rectangle) return;

            // Pixel Edit is active, Shift is down, and the cursor is over the image region.
            if (pbLayer.ClientRectangle.Contains(pbLayer.PointToClient(MousePosition)))
            {
                if (e.Modifiers == Keys.Shift)
                {
                    if (btnLayerImagePixelEdit.Checked)
                    {
                        pbLayer.PanMode = ImageBoxPanMode.None;
                        lbLayerImageTooltipOverlay.Text = "Pixel editing is on:\n" +
                                                          "» Click over a pixel to draw\n" +
                                                          "» Hold CTRL to clear pixels";

                        UpdatePixelEditorCursor();
                    }
                    else
                    {
                        pbLayer.Cursor = Cursors.Cross;
                        pbLayer.SelectionMode = ImageBoxSelectionMode.Rectangle;
                        lbLayerImageTooltipOverlay.Text = "ROI selection mode:\n" +
                                                          "» Left-click drag to select a fixed region\n" +
                                                          "» Left-click + ALT drag to select specific objects\n" +
                                                          "» Right click on a specific object to select it\n" +
                                                          "Press Esc to clear the ROI";
                    }

                    lbLayerImageTooltipOverlay.Visible = Settings.Default.LayerTooltipOverlay;

                    return;
                }
                if (e.Modifiers == Keys.Control)
                {
                    pbLayer.Cursor = Cursors.Hand;
                    pbLayer.PanMode = ImageBoxPanMode.None;
                    lbLayerImageTooltipOverlay.Text = "Issue selection mode:\n" +
                                                      "» Click over an issue to select it";

                    lbLayerImageTooltipOverlay.Visible = Settings.Default.LayerTooltipOverlay;

                    return;
                }
            }

            return;
        }

        private void EventKeyUp(object sender, KeyEventArgs e)
        {
            // As with EventKeyDown, we handle this event at the to top level
            // to ensure cursor and pan functionality are restored regardless
            // of which form has focus when shift is released.
            if (ReferenceEquals(sender, this))
            {
                if (e.KeyCode == Keys.ShiftKey || ((ModifierKeys & Keys.Shift) == 0 && e.KeyCode == Keys.ControlKey))
                {
                    pbLayer.Cursor = Cursors.Default;
                    pbLayer.PanMode = ImageBoxPanMode.Left;
                    pbLayer.SelectionMode = ImageBoxSelectionMode.None;
                    lbLayerImageTooltipOverlay.Visible = false;
                    //if (!ReferenceEquals(SlicerFile, null)) ShowLayer(); // Not needed?
                    e.Handled = true;
                }

                return;
            }

            if (ReferenceEquals(sender, pbLayer))
            {
                if (e.KeyCode == Keys.Escape)
                {
                    pbLayer.SelectNone();
                    e.Handled = true;
                }

                return;
            }

            if (ReferenceEquals(sender, flvProperties))
            {
                if (e.KeyCode == Keys.Escape)
                {
                    flvProperties.SelectedIndices.Clear();
                    e.Handled = true;
                    return;
                }


                if (e.KeyCode == Keys.Multiply)
                {
                    int[] selectedArray = new int[flvProperties.SelectedIndices.Count];
                    flvProperties.SelectedIndices.CopyTo(selectedArray, 0);
                    flvProperties.SelectedIndices.Clear();
                    for (int i = 0; i < flvProperties.GetItemCount(); i++)
                    {
                        if (selectedArray.Contains(i)) continue;
                        flvProperties.SelectedIndices.Add(i);
                    }

                    e.Handled = true;
                    return;
                }


                return;
            }

            if (ReferenceEquals(sender, flvIssues))
            {
                if (e.KeyCode == Keys.Escape)
                {
                    flvIssues.SelectedIndices.Clear();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.Multiply)
                {
                    int[] selectedArray = new int[flvIssues.SelectedIndices.Count];
                    flvIssues.SelectedIndices.CopyTo(selectedArray, 0);
                    flvIssues.SelectedIndices.Clear();
                    for (int i = 0; i < flvIssues.GetItemCount(); i++)
                    {
                        if (selectedArray.Contains(i)) continue;
                        flvIssues.SelectedIndices.Add(i);
                    }

                    e.Handled = true;
                    return;
                }

                /*if (e.Control && e.KeyCode == Keys.C)
                {
                    StringBuilder clip = new StringBuilder();
                    foreach (ListViewItem item in lvIssues.Items)
                    {
                        if (!item.Selected) continue;
                        if (clip.Length > 0) clip.AppendLine();
                        clip.Append($"{item.Text}: {item.SubItems[1].Text} Layer: {item.SubItems[2].Text} X,Y: {{{item.SubItems[3].Text}}} Pixels: {item.SubItems[4].Text}");
                    }

                    if (clip.Length > 0)
                    {
                        Clipboard.SetText(clip.ToString());
                    }
                    e.Handled = true;
                    return;
                }*/

                if (e.KeyCode == Keys.Delete)
                {
                    tsIssueRemove.PerformClick();
                    e.Handled = true;
                    return;
                }

                return;
            }

            if (ReferenceEquals(sender, flvPixelHistory))
            {
                if (e.KeyCode == Keys.Escape)
                {
                    flvPixelHistory.SelectedIndices.Clear();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.Multiply)
                {
                    int[] selectedArray = new int[flvPixelHistory.SelectedIndices.Count];
                    flvPixelHistory.SelectedIndices.CopyTo(selectedArray, 0);
                    flvPixelHistory.SelectedIndices.Clear();
                    for (int i = 0; i < flvPixelHistory.GetItemCount(); i++)
                    {
                        if (selectedArray.Contains(i)) continue;
                        flvPixelHistory.SelectedIndices.Add(i);
                    }

                    e.Handled = true;
                    return;
                }

                /*if (e.Control && e.KeyCode == Keys.C)
                {
                    StringBuilder clip = new StringBuilder();
                    foreach (ListViewItem item in flvPixelHistory.Items)
                    {
                        if (!item.Selected) continue;
                        if (clip.Length > 0) clip.AppendLine();
                        clip.Append($"{item.Text}: {item.SubItems[1].Text} Layer: {item.SubItems[2].Text} X,Y: {{{item.SubItems[3].Text}}}");
                    }

                    if (clip.Length > 0)
                    {
                        Clipboard.SetText(clip.ToString());
                    }
                    e.Handled = true;
                    return;
                }*/

                if (e.KeyCode == Keys.Delete)
                {
                    btnPixelHistoryRemove.PerformClick();
                    e.Handled = true;
                    return;
                }

                return;
            }
        }

        private void EventMouseDown(object sender, MouseEventArgs e)
        {
            if (ReferenceEquals(sender, btnNextLayer) || ReferenceEquals(sender, btnPreviousLayer))
            {
                layerScrollTimer.Tag = ReferenceEquals(sender, btnNextLayer);
                layerScrollTimer.Start();
                return;
            }

            if (ReferenceEquals(sender, tsIssueNext) || ReferenceEquals(sender, tsIssuePrevious))
            {
                issueScrollTimer.Tag = ReferenceEquals(sender, tsIssueNext);
                issueScrollTimer.Start();
                return;
            }

            if (ReferenceEquals(sender, pbLayer))
            {
                if (e.Button == MouseButtons.Middle)
                {
                    mouseHoldTimer.Tag = e.Button;
                    mouseHoldTimer.Start();
                }

                return;
            }
        }

        private void EventMouseUp(object sender, MouseEventArgs e)
        {
            if (ReferenceEquals(sender, btnNextLayer) || ReferenceEquals(sender, btnPreviousLayer))
            {
                layerScrollTimer.Stop();
                layerScrollTimer.Interval = 500;
                return;
            }

            if (ReferenceEquals(sender, tsIssueNext) || ReferenceEquals(sender, tsIssuePrevious))
            {
                issueScrollTimer.Stop();
                issueScrollTimer.Interval = 500;
                return;
            }

            if (ReferenceEquals(sender, pbLayer))
            {
                // unconditionally stop any pending mouse timer here.
                mouseHoldTimer.Stop();

                if (!pbLayer.IsPointInImage(e.Location)) return;
                Point location = pbLayer.PointToImage(e.Location);
                if (pbLayer.SelectionMode == ImageBoxSelectionMode.Rectangle)
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        if ((ModifierKeys & Keys.Alt) != 0)
                        {
                            if (SelectObjectRoi(ROI) == 0) SelectObjectRoi(location);
                            return;
                        }
                        return;
                    }

                    if (e.Button == MouseButtons.Right)
                    {
                        if (!pbLayer.IsPointInImage(e.Location)) return;
                        SelectObjectRoi(location);

                        return;
                    }
                }

                // Shift must be pressed for any pixel edit action, middle button is ignored.
                if (!btnLayerImagePixelEdit.Checked || (e.Button & MouseButtons.Middle) != 0 ||
                    (ModifierKeys & Keys.Shift) == 0) return;
                //if (!pbLayer.IsPointInImage(e.Location)) return;
                //location = pbLayer.PointToImage(e.Location);
                _lastPixelMouseLocation = Point.Empty;

                // Left or Alt-Right Adds pixel, Right or Alt-Left removes pixel
                DrawPixel(e.Button == MouseButtons.Left ^ (ModifierKeys & Keys.Alt) != 0, location);
                //SlicerFile[ActualLayer].LayerMat = ActualLayerImage;
                RefreshPixelHistory();
            }
        }

        public bool SelectObjectRoi(Point location)
        {
            var point = GetTransposedPoint(location);
            var brightness = ActualLayerImage.GetByte(point);

            if (brightness == 0) return false;
            for (int i = 0; i < LayerCache.LayerContours.Size; i++)
            {
                if (CvInvoke.PointPolygonTest(LayerCache.LayerContours[i], point, false) >= 0)
                {
                    var rectangle =
                        GetTransposedRectangle(CvInvoke.BoundingRectangle(LayerCache.LayerContours[i]));
                    ROI = rectangle;

                    return true;
                }
            }

            return false;
        }

        public uint SelectObjectRoi(Rectangle roiRectangle)
        {
            if (roiRectangle.IsEmpty) return 0;
            List<Rectangle> rectangles = new List<Rectangle>();
            for (int i = 0; i < LayerCache.LayerContours.Size; i++)
            {
                var rectangle = CvInvoke.BoundingRectangle(LayerCache.LayerContours[i]);
                //roi.Intersect(rectangle);
                if (roiRectangle.IntersectsWith(rectangle))
                {
                    rectangles.Add(rectangle);
                }

            }
            roiRectangle = rectangles.Count == 0 ? Rectangle.Empty : rectangles[0];
            for (var i = 1; i < rectangles.Count; i++)
            {
                var rectangle = rectangles[i];
                roiRectangle = Rectangle.Union(roiRectangle, rectangle);
            }

            ROI = GetTransposedRectangle(roiRectangle);

            return (uint) rectangles.Count;
        }

        private void EventMouseLeave(object sender, EventArgs e)
        {
            // Toolstrip Buttons do not register mouseup events if the mouse is no longer over
            // the button when the click is released.  Cancel scroll timer if the mouse is
            // moved from over top of the button.
            if (ReferenceEquals(sender, tsIssueNext) || ReferenceEquals(sender, tsIssuePrevious))
            {
                issueScrollTimer.Stop();
                issueScrollTimer.Interval = 500;
                return;
            }
        }

        private void EventTimerTick(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, layerScrollTimer))
            {
                if (!btnPreviousLayer.Enabled || !btnNextLayer.Enabled)
                {
                    layerScrollTimer.Stop();
                    return;
                }

                layerScrollTimer.Interval = 150;
                ShowLayer((bool) layerScrollTimer.Tag);
                return;
            }

            if (ReferenceEquals(sender, layerZoomTimer))
            {
                ShowLayer();
                layerZoomTimer.Stop();
                return;
            }

            if (ReferenceEquals(sender, issueScrollTimer))
            {
                if (!tsIssuePrevious.Enabled || !tsIssueNext.Enabled)
                {
                    issueScrollTimer.Stop();
                    return;
                }

                issueScrollTimer.Interval = 150;
                if ((bool) issueScrollTimer.Tag)
                {
                    EventClick(tsIssueNext, null);
                }
                else
                {
                    EventClick(tsIssuePrevious, null);
                }

                return;
            }

            if (ReferenceEquals(sender, mouseHoldTimer))
            {
                mouseHoldTimer.Stop(); // one-shot timer

                if ((MouseButtons) mouseHoldTimer.Tag == MouseButtons.Middle)
                {
                    // Reset auto-zoom level based on current zoom level and
                    // refresh toolstrip zoom indicator.
                    if (pbLayer.Zoom >= MinLockedZoomLevel)
                    {
                        LockedZoomLevel = pbLayer.Zoom;
                        tsLayerImageZoom.Text = $"Zoom: [ {pbLayer.Zoom / 100f}x";
                        tsLayerImageZoomLock.Visible = true;
                    }
                }

                return;
            }

        }

        private void EventMouseClick(object sender, MouseEventArgs e)
        {
            if (ReferenceEquals(sender, pbLayer))
            {
                if ((ModifierKeys & Keys.Alt) != 0)
                {
                    // ALT click within pbLayer performs double click action
                    HandleMouseDoubleClick(sender, e);

                    return;
                }

                if (!pbLayer.IsPointInImage(e.Location)) return;
                var location = pbLayer.PointToImage(e.Location);

                // Check to see if the clicked location is an issue,
                // and if so, select it in the ListView.
                if ((ModifierKeys & Keys.Control) != 0)
                    SelectIssueAtPoint(location);

                return;
            }
        }

        private void EventMouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Ignore double click if CTRL is pressed. Prevents ALT-click
            // events that emulate double click from firing twice.
            if ((ModifierKeys & Keys.Alt) != 0) return;
            HandleMouseDoubleClick(sender, e);
        }

        private void HandleMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (ReferenceEquals(sender, pbLayer))
            {
                // Ignore double click events if shift is pressed.  This prevents zoom
                // operations from inadvertently occuring when pixels are being edited, and
                // for consistency, there is no reason not to just disallow this any time
                // shift is pressed regardless of whether pixel edit mode is enabled or not.
                if ((ModifierKeys & Keys.Shift) != 0) return;

                if ((e.Button & MouseButtons.Left) != 0)
                {
                    if (!pbLayer.IsPointInImage(e.Location)) return;
                    var location = pbLayer.PointToImage(e.Location);

                    // Check to see if this zoom action will cross the crosshair fade threshold
                    // Not needed? visual artifact is not severe to make an extra showlayer call, also it acts like an "animation" removing crosshairs after zoom
                    /*if (tsLayerImageShowCrosshairs.Checked &&
                        !ReferenceEquals(Issues, null) && flvIssues.SelectedIndices.Count > 0 && 
                        pbLayer.Zoom <= CrosshairFadeLevel && LockedZoomLevel > CrosshairFadeLevel &&
                        flvIssues.SelectedObjects.Cast<LayerIssue>().Any(issue => // Find a valid candidate to update layer preview, otherwise quit
                        issue.LayerIndex == ActualLayer && issue.Type != LayerIssue.IssueType.EmptyLayer && issue.Type != LayerIssue.IssueType.TouchingBound))
                    {
                        // Refresh the preview without the crosshairs before zooming-in.
                        // Prevents zoomed-in crosshairs from breifly being displayed before
                        // the Layer Preview is refreshed post-zoom.
                        tsLayerImageShowCrosshairs.Checked = false;
                        ShowLayer();
                        tsLayerImageShowCrosshairs.Checked = true;
                    }*/


                    CenterLayerAt(location, LockedZoomLevel);

                    // Check to see if the clicked location is an issue, and if so, select it in the ListView.
                    SelectIssueAtPoint(location);

                    return;
                }

                if ((e.Button & MouseButtons.Right) != 0)
                {
                    ZoomToFit();
                    return;
                }

                return;
            }

            if (ReferenceEquals(sender, flvIssues))
            {
                if (!(flvIssues.SelectedObject is LayerIssue issue)) return;
                // Double clicking an issue will center and zoom into the 
                // selected issue. Left click on an issue will zoom to fit.
                if ((e.Button & MouseButtons.Left) != 0)
                {
                    ZoomToIssue(issue);
                    return;
                }

                if ((e.Button & MouseButtons.Right) != 0)
                {
                    ZoomToFit();
                    return;
                }

                return;
            }
        }

        #endregion

        void DrawPixel(bool isAdd, Point location)
        {
            //Stopwatch sw = Stopwatch.StartNew();
            //var point = pbLayer.PointToImage(location);

            Point realLocation = GetTransposedPoint(location);

            if ((ModifierKeys & Keys.Control) != 0)
            {
                var removedItems = PixelHistory.Items.RemoveAll(item =>
                {
                    Rectangle rect = new Rectangle(item.Location, item.Size);
                    rect.X -= item.Size.Width / 2;
                    rect.Y -= item.Size.Height / 2;
                    return rect.Contains(realLocation);
                });
                if(removedItems > 0) ShowLayer();
                return;
            }
            
            PixelOperation operation = null;
            Bitmap bmp = pbLayer.Image as Bitmap;

            if (tabControlPixelEditor.SelectedIndex == (byte) PixelOperation.PixelOperationType.Drawing)
            {
                LineType lineType = (LineType) cbPixelEditorDrawingLineType.SelectedItem;
                PixelDrawing.BrushShapeType shapeType =
                    (PixelDrawing.BrushShapeType) cbPixelEditorDrawingBrushShape.SelectedIndex;

                ushort brushSize = (ushort) nmPixelEditorDrawingBrushSize.Value;
                short thickness = (short) (nmPixelEditorDrawingThickness.Value == 0
                    ? 1
                    : nmPixelEditorDrawingThickness.Value);

                uint minLayer = (uint) Math.Max(0, ActualLayer - nmPixelEditorDrawingLayersBelow.Value);
                uint maxLayer = (uint) Math.Min(SlicerFile.LayerCount - 1,
                    ActualLayer + nmPixelEditorDrawingLayersAbove.Value);
                for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
                {
                    operation = new PixelDrawing(layerIndex, realLocation, lineType,
                        shapeType, brushSize, thickness, isAdd);

                    if (PixelHistory.Contains(operation)) continue;
                    PixelHistory.Add(operation);

                    if (layerIndex == ActualLayer)
                    {
                        using (var gfx = Graphics.FromImage(bmp))
                        {
                            int shiftPos = brushSize / 2;
                            gfx.SmoothingMode = SmoothingMode.HighSpeed;

                            var color = isAdd
                                ? (flvPixelHistory.SelectedObjects.Contains(operation)
                                    ? Settings.Default.PixelEditorAddPixelHLColor
                                    : Settings.Default.PixelEditorAddPixelColor)
                                : (flvPixelHistory.SelectedObjects.Contains(operation)
                                    ? Settings.Default.PixelEditorRemovePixelHLColor
                                    : Settings.Default.PixelEditorRemovePixelColor);
                            if (lineType == LineType.AntiAlias && brushSize > 1)
                            {
                                gfx.SmoothingMode = SmoothingMode.AntiAlias;
                            }

                            switch (shapeType)
                            {
                                case PixelDrawing.BrushShapeType.Rectangle:
                                    if (thickness > 0)
                                        gfx.DrawRectangle(new Pen(color, thickness), Math.Max(0, location.X - shiftPos),
                                            Math.Max(0, location.Y - shiftPos),
                                            (int) nmPixelEditorDrawingBrushSize.Value,
                                            (int) nmPixelEditorDrawingBrushSize.Value);
                                    else
                                        gfx.FillRectangle(new SolidBrush(color), Math.Max(0, location.X - shiftPos),
                                            Math.Max(0, location.Y - shiftPos),
                                            (int) nmPixelEditorDrawingBrushSize.Value,
                                            (int) nmPixelEditorDrawingBrushSize.Value);
                                    break;
                                case PixelDrawing.BrushShapeType.Circle:
                                    if (thickness > 0)
                                        gfx.DrawEllipse(new Pen(color, thickness), Math.Max(0, location.X - shiftPos),
                                            Math.Max(0, location.Y - shiftPos),
                                            (int) nmPixelEditorDrawingBrushSize.Value,
                                            (int) nmPixelEditorDrawingBrushSize.Value);
                                    else
                                        gfx.FillEllipse(new SolidBrush(color), Math.Max(0, location.X - shiftPos),
                                            Math.Max(0, location.Y - shiftPos),
                                            (int) nmPixelEditorDrawingBrushSize.Value,
                                            (int) nmPixelEditorDrawingBrushSize.Value);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }
                }
            }
            else if (tabControlPixelEditor.SelectedIndex == (byte) PixelOperation.PixelOperationType.Text)
            {
                if (string.IsNullOrEmpty(tbPixelEditorTextText.Text) || nmPixelEditorTextFontScale.Value < 0.2m) return;

                LineType lineType = (LineType) cbPixelEditorTextLineType.SelectedItem;
                FontFace fontFace = (FontFace) cbPixelEditorTextFontFace.SelectedItem;

                uint minLayer = (uint) Math.Max(0, ActualLayer - nmPixelEditorTextLayersBelow.Value);
                uint maxLayer = (uint) Math.Min(SlicerFile.LayerCount - 1,
                    ActualLayer + nmPixelEditorTextLayersAbove.Value);
                for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
                {
                    operation = new PixelText(layerIndex, realLocation, lineType,
                        fontFace, (double) nmPixelEditorTextFontScale.Value, (ushort) nmPixelEditorTextThickness.Value,
                        tbPixelEditorTextText.Text, cbPixelEditorTextMirror.Checked, isAdd);

                    if (PixelHistory.Contains(operation)) continue;
                    PixelHistory.Add(operation);
                }

                ShowLayer();
                return;
            }
            else if (tabControlPixelEditor.SelectedIndex == (byte) PixelOperation.PixelOperationType.Eraser)
            {
                if (ActualLayerImage.GetByte(realLocation) < 10) return;
                uint minLayer = (uint) Math.Max(0, ActualLayer - nmPixelEditorEraserLayersBelow.Value);
                uint maxLayer = (uint) Math.Min(SlicerFile.LayerCount - 1,
                    ActualLayer + nmPixelEditorEraserLayersAbove.Value);
                for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
                {
                    operation = new PixelEraser(layerIndex, realLocation);

                    if (PixelHistory.Contains(operation)) continue;
                    PixelHistory.Add(operation);
                }

                ShowLayer();
                return;
            }
            else if (tabControlPixelEditor.SelectedIndex == (byte) PixelOperation.PixelOperationType.Supports)
            {
                if (ActualLayer == 0) return;
                operation = new PixelSupport(ActualLayer, realLocation,
                    (byte) nmPixelEditorSupportsTipDiameter.Value, (byte) nmPixelEditorSupportsPillarDiameter.Value,
                    (byte) nmPixelEditorSupportsBaseDiameter.Value);

                if (PixelHistory.Contains(operation)) return;
                PixelHistory.Add(operation);

                SolidBrush brush = new SolidBrush(flvPixelHistory.SelectedObjects.Contains(operation)
                    ? Settings.Default.PixelEditorSupportHLColor
                    : Settings.Default.PixelEditorSupportColor);
                using (var gfx = Graphics.FromImage(bmp))
                {
                    int shiftPos = (int) nmPixelEditorSupportsTipDiameter.Value / 2;
                    gfx.SmoothingMode = SmoothingMode.HighSpeed;
                    gfx.FillEllipse(brush, Math.Max(0, location.X - shiftPos), Math.Max(0, location.Y - shiftPos),
                        (int) nmPixelEditorSupportsTipDiameter.Value, (int) nmPixelEditorSupportsTipDiameter.Value);
                }
            }
            else if (tabControlPixelEditor.SelectedIndex == (byte) PixelOperation.PixelOperationType.DrainHole)
            {
                if (ActualLayer == 0) return;
                operation = new PixelDrainHole(ActualLayer, realLocation, (byte) nmPixelEditorDrainHoleDiameter.Value);

                if (PixelHistory.Contains(operation)) return;
                PixelHistory.Add(operation);

                SolidBrush brush = new SolidBrush(flvPixelHistory.SelectedObjects.Contains(operation)
                    ? Settings.Default.PixelEditorDrainHoleHLColor
                    : Settings.Default.PixelEditorDrainHoleColor);
                using (var gfx = Graphics.FromImage(bmp))
                {
                    int shiftPos = (int) nmPixelEditorDrainHoleDiameter.Value / 2;
                    gfx.SmoothingMode = SmoothingMode.HighSpeed;
                    gfx.FillEllipse(brush, Math.Max(0, location.X - shiftPos), Math.Max(0, location.Y - shiftPos),
                        (int) nmPixelEditorDrainHoleDiameter.Value, (int) nmPixelEditorDrainHoleDiameter.Value);
                }
            }
            else
            {
                throw new NotImplementedException("Missing pixel operation");
            }



            pbLayer.Invalidate();
            //pbLayer.Update();
            //pbLayer.Refresh();
            //CanSavemenuFileSaveAs.Enabled = true;
            //sw.Stop();
            //Debug.WriteLine(sw.ElapsedMilliseconds);
        }

        public void RefreshPixelHistory()
        {
            PixelHistory.Renumber();
            flvPixelHistory.SetObjects(PixelHistory);

            lbPixelHistoryOperations.Text = $"Operations: {PixelHistory.Count}";

            btnPixelHistoryRemove.Enabled = flvPixelHistory.SelectedIndices.Count > 0;
            btnPixelHistoryClear.Enabled =
                btnPixelHistoryApply.Enabled = PixelHistory.Count > 0;
        }

        private void UpdateIssuesInfo()
        {
            if (TotalIssues == 0)
            {
                tsIssuePrevious.Enabled =
                    tsIssueCount.Enabled =
                        tsIssueNext.Enabled = false;

                tsIssueCount.Text = "0/0";
                tsIssueCount.Tag = -1;
            }
            else
            {
                int currentIssueSelected = Convert.ToInt32(tsIssueCount.Tag);
                // Convert text to fixed field length in order to prevent
                // prev/next buttons from moving as the number of digits in
                // the display for current index changes.  Without this,
                // scrolling may unexpectedly stop due to the button moving
                // out from underneath the mouse pointer.
                var digits = Math.Floor(Math.Log10(TotalIssues) + 1);
                var issueNum = currentIssueSelected + 1;
                var formatString = $"D{digits}";

                tsIssueCount.Enabled = true;
                tsIssueCount.Text = $"{issueNum.ToString(formatString)}/{TotalIssues}";
                tsIssuePrevious.Enabled = currentIssueSelected > 0;
                tsIssueNext.Enabled = currentIssueSelected + 1 < TotalIssues;
            }

        }

        private void ComputeIssues(IslandDetectionConfiguration islandConfig = null,
            OverhangDetectionConfiguration overhangConfig = null,
            ResinTrapDetectionConfiguration resinTrapConfig = null,
            TouchingBoundDetectionConfiguration touchingBoundConfig = null, bool emptyLayersConfig = true)
        {
            tabPageIssues.Tag = true;
            flvIssues.ClearObjects();
            UpdateIssuesInfo();

            DisableGUI();
            FrmLoading.SetDescription("Computing Issues");

            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    Issues = SlicerFile.LayerManager.GetAllIssues(islandConfig, overhangConfig, resinTrapConfig, touchingBoundConfig,
                        emptyLayersConfig, FrmLoading.RestartProgress());
                }
                catch (OperationCanceledException)
                {

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error while trying compute issues", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    Invoke((MethodInvoker) delegate
                    {
                        // Running on the UI thread
                        EnableGUI(true);
                    });
                }
            });

            var loadingResult = FrmLoading.ShowDialog();

            UpdateIssuesList();
        }

        public Point GetTransposedPoint(Point point, bool clockWise = true)
        {
            if (!btnLayerImageRotate.Checked) return point;
            return clockWise
                ? new Point(point.Y, ActualLayerImage.Height - 1 - point.X)
                : new Point(ActualLayerImage.Height - 1 - point.Y, point.X);
        }

        public Rectangle GetTransposedRectangle(RectangleF rectangleF, bool clockWise = true, bool ignoreLayerRotation = false) =>
            GetTransposedRectangle(Rectangle.Round(rectangleF), clockWise, ignoreLayerRotation);

        public Rectangle GetTransposedRectangle(Rectangle rectangle, bool clockWise = true, bool ignoreLayerRotation = false)
        {
            if (rectangle.IsEmpty || (!ignoreLayerRotation && !btnLayerImageRotate.Checked)) return rectangle;
            return clockWise
                ? new Rectangle(ActualLayerImage.Height - rectangle.Bottom,
                    rectangle.Left, rectangle.Height, rectangle.Width)
                //: new Rectangle(ActualLayerImage.Width - rectangle.Bottom, rectangle.Left, rectangle.Width, rectangle.Height);
                //: new Rectangle(ActualLayerImage.Width - rectangle.Bottom, ActualLayerImage.Height-rectangle.Right, rectangle.Width, rectangle.Height); // Rotate90FlipX: // = Rotate270FlipY
                //: new Rectangle(rectangle.Top, rectangle.Left, rectangle.Width, rectangle.Height); // Rotate270FlipX:  // = Rotate90FlipY
                : new Rectangle(rectangle.Top, ActualLayerImage.Height - rectangle.Right, rectangle.Height, rectangle.Width); // Rotate90FlipNone:  // = Rotate270FlipXY
        }

        /// <summary>
        /// Gets the bounding rectangle of the passed issue, automatically adjusting
        /// the coordinates and width/height to account for whether or not the layer
        /// preview image is rotated.  Used to ensure images are properly zoomed or
        /// centered independent of the layer preview rotation.
        /// </summary>
        private Rectangle GetTransposedIssueBounds(LayerIssue issue)
        {
            if (issue.X >= 0 && issue.Y >= 0 && (issue.BoundingRectangle.IsEmpty || issue.Size == 1) &&
                btnLayerImageRotate.Checked)
                return new Rectangle(ActualLayerImage.Height - 1 - issue.Y,
                    issue.X, 1, 1);

            return GetTransposedRectangle(issue.BoundingRectangle);
        }

        /// <summary>
        /// Centers layer view on a X,Y coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">X coordinate</param>
        /// <param name="zoomLevel">Zoom level to set, 0 to ignore or negative value to get current locked zoom level</param>
        public void CenterLayerAt(int x, int y, int zoomLevel = 0)
        {
            if (zoomLevel < 0) zoomLevel = LockedZoomLevel;
            if (zoomLevel > 0) pbLayer.Zoom = zoomLevel;
            pbLayer.CenterAt(x, y);
        }

        /// <summary>
        /// Centers layer view on a middle of a given rectangle
        /// </summary>
        /// <param name="rectangle">Rectangle holding coordinates and bounds</param>
        /// <param name="zoomLevel">Zoom level to set, 0 to ignore or negative value to get current locked zoom level</param>
        /// <param name="zoomToRegion">Auto zoom to a region and ensure that region area stays all visible when possible, when true this will overwrite zoomLevel</param></param>
        public void CenterLayerAt(Rectangle rectangle, int zoomLevel = 0, bool zoomToRegion = false)
        {
            Rectangle viewPort = Rectangle.Round(pbLayer.GetSourceImageRegion());
            if (zoomToRegion ||
                rectangle.Width * LockedZoomLevel / pbLayer.Zoom > viewPort.Width ||
                rectangle.Height * LockedZoomLevel / pbLayer.Zoom > viewPort.Height)
            {
                SupressLayerZoomEvent = true;
                pbLayer.ZoomToRegion(rectangle);
                SupressLayerZoomEvent = false;
                pbLayer.ZoomOut(true);
                return;
            }
            CenterLayerAt(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2, zoomLevel);
        }

        /// <summary>
        /// Centers layer view on a middle of a given rectangle
        /// </summary>
        /// <param name="rectangle">Rectangle holding coordinates and bounds</param>
        /// <param name="zoomLevel">Zoom level to set, 0 to ignore or negative value to get current locked zoom level</param>
        /// <param name="zoomToRegion">Auto zoom to a region and ensure that region area stays all visible when possible, when true this will overwrite zoomLevel</param></param>
        public void CenterLayerAt(RectangleF rectangle, int zoomLevel = 0, bool zoomToRegion = false) =>
            CenterLayerAt(Rectangle.Round(rectangle), zoomLevel, zoomToRegion);


        /// <summary>
        /// Centers layer view on a <see cref="Point"/>
        /// </summary>
        /// <param name="point">Point holding X and Y coordinates</param>
        /// <param name="zoomLevel">Zoom level to set, 0 to ignore or negative value to get current locked zoom level</param>
        public void CenterLayerAt(Point point, int zoomLevel = 0) => CenterLayerAt(point.X, point.Y, zoomLevel);


        /// <summary>
        /// Zoom the layer preview to the passed issue, or if appropriate for issue type,
        /// Zoom to fit the plate or print bounds.
        /// </summary>
        private void ZoomToIssue(LayerIssue issue)
        {
            if (issue.Type == LayerIssue.IssueType.TouchingBound || issue.Type == LayerIssue.IssueType.EmptyLayer ||
                (issue.X == -1 && issue.Y == -1))
            {
                ZoomToFit();
                return;
            }

            if (issue.X >= 0 && issue.Y >= 0)
            {
                // Check to see if this zoom action will cross the crosshair fade threshold
                /*if (tsLayerImageShowCrosshairs.Checked && !ReferenceEquals(Issues, null) && flvIssues.SelectedIndices.Count > 0
                   && pbLayer.Zoom <= CrosshairFadeLevel && LockedZoomLevel > CrosshairFadeLevel)
                {
                    // Refresh the preview without the crosshairs before zooming-in.
                    // Prevents zoomed-in crosshairs from breifly being displayed before
                    // the Layer Preview is refreshed post-zoom.
                    tsLayerImageShowCrosshairs.Checked = false;
                    ShowLayer();
                    tsLayerImageShowCrosshairs.Checked = true;
                }*/

                CenterLayerAt(GetTransposedIssueBounds(issue), LockedZoomLevel);

            }
        }

        /// <summary>
        /// Center the layer preview on the passed issue, or if appropriate for issue type,
        /// Zoom to fit the plate or print bounds.
        /// </summary>
        private void CenterAtIssue(LayerIssue issue)
        {
            if (issue.Type == LayerIssue.IssueType.TouchingBound || issue.Type == LayerIssue.IssueType.EmptyLayer ||
                (issue.X == -1 && issue.Y == -1))
            {
                ZoomToFit();
            }

            if (issue.X >= 0 && issue.Y >= 0)
            {
                CenterLayerAt(GetTransposedIssueBounds(issue));
            }
        }

        private void ZoomToFit()
        {
            if (ReferenceEquals(SlicerFile, null)) return;

            // If ALT key is pressed when ZoomToFit is performed, the configured option for 
            // zoom to plate vs. zoom to print bounds will be inverted.
            if (Settings.Default.ZoomToFitPrintVolumeBounds ^ (ModifierKeys & Keys.Alt) != 0)
            {
                if (!btnLayerImageRotate.Checked)
                {
                    pbLayer.ZoomToRegion(SlicerFile.LayerManager.BoundingRectangle);
                }
                else
                {
                    pbLayer.ZoomToRegion(ActualLayerImage.Height - 1 - SlicerFile.LayerManager.BoundingRectangle.Bottom,
                        SlicerFile.LayerManager.BoundingRectangle.X,
                        SlicerFile.LayerManager.BoundingRectangle.Height,
                        SlicerFile.LayerManager.BoundingRectangle.Width
                    );
                }
            }
            else
            {
                pbLayer.ZoomToFit();
            }
        }

        /// <summary>
        /// If there is an issue under the point location passed, that issue will be selected and
        /// scrolled into view on the IssueList.
        /// </summary>
        private void SelectIssueAtPoint(Point location)
        {
            //location = GetTransposedPoint(location);
            // If location clicked is within an issue, activate it.
            int index = -1;
            foreach (LayerIssue issue in flvIssues.Objects)
            {
                index++;

                if (issue.LayerIndex != ActualLayer) continue;
                if (!GetTransposedIssueBounds(issue).Contains(location)) continue;

                flvIssues.SelectedIndex = index;
                flvIssues.EnsureVisible(index);
                //flvIssues.Refresh();
                break;
            }

        }

        public IslandDetectionConfiguration GetIslandDetectionConfiguration()
        {
            return new IslandDetectionConfiguration
            {
                Enabled = tsIssuesDetectIslands.Checked,
                AllowDiagonalBonds = Settings.Default.IslandAllowDiagonalBonds,
                BinaryThreshold = Settings.Default.IslandBinaryThreshold,
                RequiredAreaToProcessCheck = Settings.Default.IslandRequiredAreaToProcessCheck,
                RequiredPixelBrightnessToProcessCheck = Settings.Default.IslandRequiredPixelBrightnessToProcessCheck,
                RequiredPixelsToSupport = Settings.Default.IslandRequiredPixelsToSupport,
                RequiredPixelBrightnessToSupport = Settings.Default.IslandRequiredPixelBrightnessToSupport
            };
        }

        public OverhangDetectionConfiguration GetOverhangDetectionConfiguration()
        {
            return new OverhangDetectionConfiguration
            {
                Enabled = tsIssuesDetectOverhangs.Checked,
                IndependentFromIslands = Settings.Default.OverhangIndependentFromIslands,
                ErodeIterations = Settings.Default.OverhangErodeIterations,
            };
        }

        public ResinTrapDetectionConfiguration GetResinTrapDetectionConfiguration()
        {
            return new ResinTrapDetectionConfiguration
            {
                Enabled = tsIssuesDetectResinTraps.Checked,
                BinaryThreshold = Settings.Default.ResinTrapBinaryThreshold,
                RequiredAreaToProcessCheck = Settings.Default.ResinTrapRequiredAreaToProcessCheck,
                RequiredBlackPixelsToDrain = Settings.Default.ResinTrapRequiredBlackPixelsToDrain,
                MaximumPixelBrightnessToDrain = Settings.Default.ResinTrapMaximumPixelBrightnessToDrain
            };
        }

        public TouchingBoundDetectionConfiguration GetTouchingBoundsDetectionConfiguration()
        {
            return new TouchingBoundDetectionConfiguration
            {
                Enabled = tsIssuesDetectTouchingBounds.Checked,
                //MaximumPixelBrightness = 100
            };
        }

        public void AddLog(LogItem log)
        {
            int count = log.Index = lvLog.GetItemCount() + 1;
            lvLog.AddObject(log);
            lbLogOperations.Text = $"Operations: {count}";
        }

        public void AddLog(string description, decimal elapsedTime = 0)
        {
            int count = lvLog.GetItemCount() + 1;
            lvLog.AddObject(new LogItem(count, description));
            lbLogOperations.Text = $"Operations: {count}";
        }

        public void AddLogVerbose(string description, decimal elapsedTime = 0)
        {
            if (!IsLogVerbose) return;
            AddLog(description, elapsedTime);
            Debug.WriteLine(description);
        }

        public void EditLastLogElapsedTime(decimal elapsedTime = 0)
        {
            if (lvLog.GetModelObject(lvLog.GetItemCount() - 1) is LogItem log) log.ElapsedTime = elapsedTime;
        }

        public void DrawModifications(bool exitEditor)
        {
            if (PixelHistory.Count == 0)
            {
                if (exitEditor)
                {
                    tabControlLeft.SelectedTab = ControlLeftLastTab;
                    tabControlLeft.TabPages.Remove(tabPagePixelEditor);
                }

                return;
            }

            var result = DialogResult.None;

            if (exitEditor)
            {
                result = MessageBox.Show(
                    "There are edit operations that have not been applied.  " +
                    "Would you like to apply all operations before closing the editor?",
                    "Closing image editor", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            }
            else
            {

                result = MessageBox.Show(
                    "Are you sure you want to apply all operations?",
                    "Apply image editor changes?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                // For the "apply" case, We aren't exiting the editor, so map "No" to "Cancel" here
                // in order to prevent pixel history from being cleared.
                result = result == DialogResult.No ? DialogResult.Cancel : DialogResult.Yes;
            }

            if (result == DialogResult.Cancel)
            {
                btnLayerImagePixelEdit.Checked = true;
                return;
            }

            if (result == DialogResult.Yes)
            {
                DisableGUI();
                FrmLoading.SetDescription("Drawing pixels");

                Task task = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        SlicerFile.LayerManager.DrawModifications(PixelHistory.Items, FrmLoading.RestartProgress());
                    }
                    catch (OperationCanceledException)
                    {

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "Drawing operation failed!", MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    finally
                    {
                        Invoke((MethodInvoker) delegate
                        {
                            // Running on the UI thread
                            EnableGUI(true);
                        });
                    }

                    return false;
                });

                FrmLoading.ShowDialog();

                if (Settings.Default.PartialUpdateIslandsOnEditing)
                {
                    List<uint> whiteListLayers = new List<uint>();
                    foreach (var item in PixelHistory.Items)
                    {
                        if (item.OperationType != PixelOperation.PixelOperationType.Drawing &&
                            item.OperationType != PixelOperation.PixelOperationType.Text &&
                            item.OperationType != PixelOperation.PixelOperationType.Eraser &&
                            item.OperationType != PixelOperation.PixelOperationType.Supports) continue;
                        if (whiteListLayers.Contains(item.LayerIndex)) continue;
                        whiteListLayers.Add(item.LayerIndex);

                        uint nextLayer = item.LayerIndex + 1;
                        if (nextLayer < SlicerFile.LayerCount &&
                            !whiteListLayers.Contains(nextLayer))
                        {
                            whiteListLayers.Add(nextLayer);
                        }
                    }

                    UpdateIslandsOverhangs(whiteListLayers);
                }
            }

            if (exitEditor || (Settings.Default.CloseEditOnApply && result == DialogResult.Yes))
            {
                btnLayerImagePixelEdit.Checked = false;
                tabControlLeft.SelectedTab = ControlLeftLastTab;
                tabControlLeft.TabPages.Remove(tabPagePixelEditor);
            }

            PixelHistory.Clear();
            RefreshPixelHistory();
            ShowLayer();

            CanSave = true;
        }

        private void UpdateIslandsOverhangs(List<uint> whiteListLayers)
        {
            if (whiteListLayers.Count == 0) return;
            var islandConfig = GetIslandDetectionConfiguration();
            var overhangConfig = GetOverhangDetectionConfiguration();
            var resinTrapConfig = new ResinTrapDetectionConfiguration {Enabled = false};
            var touchingBoundConfig = new TouchingBoundDetectionConfiguration {Enabled = false};
            islandConfig.Enabled = true;
            islandConfig.WhiteListLayers = whiteListLayers;
            overhangConfig.Enabled = true;
            overhangConfig.WhiteListLayers = whiteListLayers;

            if (Issues is null)
            {
                ComputeIssues(islandConfig, overhangConfig, resinTrapConfig, touchingBoundConfig, false);
            }
            else
            {
                DisableGUI();
                FrmLoading.SetDescription("Updating Issues");

                foreach (var layerIndex in islandConfig.WhiteListLayers)
                {
                    Issues.RemoveAll(issue =>
                        issue.LayerIndex == layerIndex &&
                        (issue.Type == LayerIssue.IssueType.Island || issue.Type == LayerIssue.IssueType.Overhang)); // Remove all islands and overhangs for update
                }

                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var issues = SlicerFile.LayerManager.GetAllIssues(islandConfig, overhangConfig, resinTrapConfig,
                            touchingBoundConfig, false,
                            FrmLoading.RestartProgress());

                        issues.RemoveAll(issue => issue.Type != LayerIssue.IssueType.Island && issue.Type != LayerIssue.IssueType.Overhang); // Remove all non islands
                        Issues.AddRange(issues);
                        Issues = Issues.OrderBy(issue => issue.Type).ThenBy(issue => issue.LayerIndex)
                            .ThenBy(issue => issue.PixelsCount).ToList();
                    }
                    catch (OperationCanceledException)
                    {

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "Error while trying to compute issues",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    finally
                    {
                        Invoke((MethodInvoker) delegate
                        {
                            // Running on the UI thread
                            EnableGUI(true);
                        });
                    }
                });

                FrmLoading.ShowDialog();
                UpdateIssuesList();
            }
        }

        void UpdateIssuesList()
        {
            flvIssues.ClearObjects();
            
            if (!ReferenceEquals(Issues, null) && Issues.Count > 0)
            {
                flvIssues.SetObjects(Issues);
            }
            else
            {
                UpdateLayerTrackerHighlightIssues();
            }

            UpdateIssuesInfo();
            ShowLayer();
        }

        public Dictionary<uint, uint> GetIssuesCountPerLayer()
        {
            if (Issues is null) return null;
            Dictionary<uint, uint> layerIndexIssueCount = new Dictionary<uint, uint>();
            foreach (var issue in Issues)
            {
                if (!layerIndexIssueCount.ContainsKey(issue.LayerIndex))
                {
                    layerIndexIssueCount.Add(issue.LayerIndex, 1);
                }
                else
                {
                    layerIndexIssueCount[issue.LayerIndex]++;
                }
            }

            return layerIndexIssueCount;
        }

        void UpdateLayerTrackerHighlightIssues()
        {
            var issuesCountPerLayer = GetIssuesCountPerLayer();
            if (issuesCountPerLayer is null)
            {
                pbTrackerIssues.Image = null;
                return;
            }

            using (Mat mat = new Mat(pbTrackerIssues.Height, pbTrackerIssues.Width, DepthType.Cv8U, 3))
            {
                mat.SetTo(new MCvScalar(255, 255, 255));
                var color = new MCvScalar(0, 0, 255);

                foreach (var value in issuesCountPerLayer)
                {
                    var tickPos = tbLayer.GetTickPos((int) value.Key);
                    if (tickPos == -1) continue;
                    int y = (pbTrackerIssues.Height - tickPos).Clamp(0, pbTrackerIssues.Height);
                    CvInvoke.Line(mat, new Point(0, y), new Point(mat.Width, y), color);
                }

                pbTrackerIssues.Image = mat.ToBitmap();
            }
        }

        public Operation ShowRunOperation(Type type)
        {
            var operation = ShowOperation(type);
            RunOperation(operation);
            return operation;
        }

        public Operation ShowOperation(Type type)
        {
            Operation operation;
            
            var typeBase = typeof(CtrlToolTemplate);
            var classname = $"{typeBase.Namespace}.CtrlTool{type.Name.Remove(0,Operation.ClassNameLength)}";
            var controlType = Type.GetType(classname);
            CtrlToolWindowContent control;

            bool removeContent = false;
            if (controlType is null)
            {
                controlType = typeBase;
                removeContent = true;
                control = new CtrlToolTemplate(type.CreateInstance<Operation>());
            }
            else
            {
                control = controlType.CreateInstance<CtrlToolWindowContent>();
                if (control is null) return null;
            }

            if (!control.CanRun)
            {
                control.Dispose();
                return null;
            }
            
            if (removeContent)
            {
                control.Visible = false;
            }

            using (var frm = new FrmToolWindow(control, control.BaseOperation.PassActualLayerIndex ? (int)ActualLayer : -1))
            {
                if (removeContent)
                {
                    frm.pnContent.Visible = false;
                }
                if (frm.IsDisposed || frm.ShowDialog() != DialogResult.OK) return null;
                operation = control.BaseOperation;
            }

            return operation;
        }

        public bool RunOperation(Operation baseOperation)
        {
            if (baseOperation is null) return false;

            switch (baseOperation)
            {
                case OperationEditParameters operation:
                    /*foreach (var modifier in operation.Modifiers.Where(modifier => modifier.HasChanged))
                    {
                        SlicerFile.SetValueFromPrintParameterModifier(modifier, modifier.NewValue);
                    }*/
                    SlicerFile.SetValuesFromPrintParametersModifiers();
                    RefreshInfo();

                    CanSave = true;

                    return false;
                case OperationRepairLayers operation:
                    if (Issues is null)
                    {
                        var islandConfig = GetIslandDetectionConfiguration();
                        islandConfig.Enabled = operation.RepairIslands && operation.RemoveIslandsBelowEqualPixelCount > 0;
                        var overhangConfig = new OverhangDetectionConfiguration {Enabled = false};
                        var resinTrapConfig = GetResinTrapDetectionConfiguration();
                        resinTrapConfig.Enabled = operation.RepairResinTraps;
                        var touchingBoundConfig = new TouchingBoundDetectionConfiguration {Enabled = false};

                        if (islandConfig.Enabled || resinTrapConfig.Enabled)
                        {
                            ComputeIssues(
                                islandConfig, 
                                overhangConfig,
                                resinTrapConfig,
                                touchingBoundConfig,
                                tsIssuesDetectEmptyLayers.Checked);
                        }
                    }

                    operation.Issues = Issues;

                    break;
            }

            DisableGUI();

            FrmLoading.SetDescription(baseOperation.ProgressTitle);

            var task = Task.Factory.StartNew(() =>
            {
                /*var backup = new Layer[baseOperation.LayerRangeCount];
                uint i = 0;
                for (uint layerIndex = baseOperation.LayerIndexStart; layerIndex <= baseOperation.LayerIndexEnd; layerIndex++)
                {
                    backup[i++] = SlicerFile[layerIndex].Clone();
                }*/

                var backup = SlicerFile.LayerManager.Clone();

                try
                {
                    switch (baseOperation)
                    {
                        // Tools
                        case OperationRepairLayers operation:
                            operation.IslandDetectionConfig = GetIslandDetectionConfiguration();
                            SlicerFile.LayerManager.RepairLayers(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;
                        case OperationMove operation:
                            SlicerFile.LayerManager.Move(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;
                        case OperationResize operation:
                            SlicerFile.LayerManager.Resize(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;
                        case OperationFlip operation:
                            SlicerFile.LayerManager.Flip(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;
                        case OperationRotate operation:
                            SlicerFile.LayerManager.Rotate(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;
                        case OperationSolidify operation:
                            SlicerFile.LayerManager.Solidify(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;
                        case OperationMorph operation:
                            SlicerFile.LayerManager.Morph(operation, BorderType.Default, new MCvScalar(), FrmLoading.RestartProgress(operation.CanCancel));
                            break;
                        case OperationThreshold operation:
                            SlicerFile.LayerManager.ThresholdPixels(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;
                        case OperationArithmetic operation:
                            SlicerFile.LayerManager.Arithmetic(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;
                        case OperationMask operation:
                            SlicerFile.LayerManager.Mask(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;
                        case OperationPixelDimming operation:
                            SlicerFile.LayerManager.PixelDimming(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;
                        case OperationBlur operation:
                            SlicerFile.LayerManager.Blur(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;

                        case OperationChangeResolution operation:
                            SlicerFile.LayerManager.ChangeResolution(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;
                        case OperationLayerReHeight operation:
                            SlicerFile.LayerManager.ReHeight(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;
                        case OperationPattern operation:
                            SlicerFile.LayerManager.Pattern(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;
                        // Actions
                        case OperationLayerImport operation:
                            SlicerFile.LayerManager.Import(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;
                        case OperationLayerClone operation:
                            SlicerFile.LayerManager.CloneLayer(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;
                        case OperationLayerRemove operation:
                            SlicerFile.LayerManager.RemoveLayers(operation, FrmLoading.RestartProgress(operation.CanCancel));
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }
                catch (OperationCanceledException)
                {
                    /*i = 0;
                    for (uint layerIndex = baseOperation.LayerIndexStart; layerIndex <= baseOperation.LayerIndexEnd; layerIndex++)
                    {
                        SlicerFile[layerIndex] = backup[i++];
                    }*/
                    SlicerFile.LayerManager = backup;
                }
                catch (Exception ex)
                {
                    GUIExtensions.MessageBoxError($"{baseOperation.Title} Error", ex.ToString());
                }
                finally
                {
                    Invoke((MethodInvoker)delegate
                    {
                        // Running on the UI thread
                        EnableGUI(true);
                    });
                }
            });
   
            var loadingResult = FrmLoading.ShowDialog();
 
            ShowLayer();
            UpdateLayerLimits();
            RefreshInfo();

            CanSave = true;

            switch (baseOperation)
            {
                // Tools
                case OperationRepairLayers operation:
                    tsIssuesDetect.PerformButtonClick();
                    break;
            }


            return true;
        }

        readonly SolidBrush _pixelEditorCursorBrush = new SolidBrush(Color.FromArgb(150, 120, 255, 255));
        const byte _pixelEditorCursorMinDiamater = 10;
        public void UpdatePixelEditorCursor()
        {
            Bitmap bitmap = null;

            if (tabControlPixelEditor.SelectedIndex == (byte)PixelOperation.PixelOperationType.Drawing)
            {
                PixelDrawing.BrushShapeType shapeType =
                    (PixelDrawing.BrushShapeType)cbPixelEditorDrawingBrushShape.SelectedIndex;

                ushort brushSize = (ushort)nmPixelEditorDrawingBrushSize.Value;
                short thickness = (short)(nmPixelEditorDrawingThickness.Value == 0
                    ? 1
                    : nmPixelEditorDrawingThickness.Value);


                var cursorSize = thickness > 1 ? brushSize + thickness : brushSize;
                int diameter = (int) (cursorSize * pbLayer.ZoomFactor);
                if (brushSize > 1 && diameter >= _pixelEditorCursorMinDiamater)
                {
                    bitmap = new Bitmap(diameter, diameter, PixelFormat.Format32bppArgb);
                    using (Graphics gr = Graphics.FromImage(bitmap))
                    {
                        gr.SmoothingMode = SmoothingMode.AntiAlias;
                        gr.CompositingMode = CompositingMode.SourceCopy;

                        switch (shapeType)
                        {
                            case PixelDrawing.BrushShapeType.Rectangle:
                                if (thickness >= 1)
                                {
                                    gr.DrawRectangle(new Pen(_pixelEditorCursorBrush, (int)(thickness * 2 * pbLayer.ZoomFactor)), 0, 0, diameter, diameter);
                                }
                                else
                                {
                                    gr.FillRectangle(_pixelEditorCursorBrush, 0, 0, diameter, diameter);
                                }
                                break;
                            case PixelDrawing.BrushShapeType.Circle:
                                if (thickness >= 1)
                                {
                                    gr.DrawEllipse(new Pen(_pixelEditorCursorBrush, (int)(thickness * 2 * pbLayer.ZoomFactor)), 0, 0, diameter, diameter);
                                }
                                else
                                {
                                    gr.FillEllipse(_pixelEditorCursorBrush, 0, 0, diameter, diameter);
                                }
                                break;
                        }
                    }
                }
            }
            /*else if (tabControlPixelEditor.SelectedIndex == (byte)PixelOperation.PixelOperationType.Text)
            {
                var text = tbPixelEditorTextText.Text;
                if (string.IsNullOrEmpty(text) || nmPixelEditorTextFontScale.Value < 0.2m) return;

                LineType lineType = (LineType)cbPixelEditorTextLineType.SelectedItem;
                FontFace fontFace = (FontFace)cbPixelEditorTextFontFace.SelectedItem;
                double scale = (double) nmPixelEditorTextFontScale.Value * pbLayer.Zoom / 100;
                int thickness = (int) nmPixelEditorTextThickness.Value;
                int baseLine = 0;
                var size = CvInvoke.GetTextSize(text, fontFace, scale, thickness, ref baseLine);
                mat = new Mat(size, DepthType.Cv8U, 4);
                CvInvoke.PutText(mat, text, new Point(0,0), fontFace, scale, new MCvScalar(255,100,255, 255), thickness, lineType, cbPixelEditorTextMirror.Checked);
            }*/
            else if (tabControlPixelEditor.SelectedIndex == (byte)PixelOperation.PixelOperationType.Supports || tabControlPixelEditor.SelectedIndex == (byte)PixelOperation.PixelOperationType.DrainHole)
            {
                var diameter = (int)(
                    (tabControlPixelEditor.SelectedIndex == (byte)PixelOperation.PixelOperationType.Supports ? 
                        nmPixelEditorSupportsTipDiameter.Value : nmPixelEditorDrainHoleDiameter.Value) 
                    * pbLayer.Zoom / 100);

                if (diameter >= _pixelEditorCursorMinDiamater)
                {
                    bitmap = new Bitmap(diameter, diameter, PixelFormat.Format32bppArgb);
                    using (Graphics gr = Graphics.FromImage(bitmap))
                    {
                        gr.SmoothingMode = SmoothingMode.AntiAlias;
                        gr.CompositingMode = CompositingMode.SourceCopy;
                        gr.FillEllipse(_pixelEditorCursorBrush, 0, 0, diameter, diameter);
                    }
                }
            }
            

            pbLayer.Cursor = bitmap is null ? pixelEditCursor : new Cursor(bitmap.GetHicon()) { Tag = "Custom" };
        }

        public bool CanGlobalHotKey
            => !(ActiveControl is TextBox 
                 || ActiveControl is ComboBox 
                 || ActiveControl is NumericUpDown 
                 || ActiveControl is RichTextBox 
                 || ActiveControl is ListView 
                 || ActiveControl is ObjectListView 
                 || ActiveControl is FastObjectListView);


    }
}
