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
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BrightIdeasSoftware;
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
using UVtools.GUI.Forms;
using UVtools.GUI.Properties;

namespace UVtools.GUI
{
    public partial class FrmMain : Form
    {
        #region Enums
        #endregion

        #region Properties

        public static readonly Dictionary<LayerManager.Mutate, Mutation> Mutations =
            new Dictionary<LayerManager.Mutate, Mutation>
            {
                {LayerManager.Mutate.Move, new Mutation(LayerManager.Mutate.Move, null, Resources.move_16x16,
                    "Moves the entire print volume around the plate.\n" +
                    "Note: Margins are in pixel values."
                )},
                {LayerManager.Mutate.Resize, new Mutation(LayerManager.Mutate.Resize, null, Resources.crop_16x16,
                    "Resizes layer images in a X and/or Y factor, starting from 100% value.\n" +
                    "NOTE 1: Build volume bounds are not validated after operation, please ensure scaling stays inside your limits.\n" +
                    "NOTE 2: X and Y are applied to original image, not to the rotated preview (If enabled)."
                )},
                {LayerManager.Mutate.Flip, new Mutation(LayerManager.Mutate.Flip, null, Resources.flip_16x16,
                    "Flips layer images vertically and/or horizontally."
                )},
                {LayerManager.Mutate.Rotate, new Mutation(LayerManager.Mutate.Rotate, null, Resources.refresh_16x16,
                    "Rotate layer images in a certain degrees."
                )},
                {LayerManager.Mutate.Solidify, new Mutation(LayerManager.Mutate.Solidify, null, Resources.square_solid_16x16,
                    "Solidifies the selected layers, closes all inner holes.\n" +
                    "Warning: All surrounded holes are filled, no exceptions! Make sure you don't require any of holes in layer path.",
                    Resources.mutation_solidify
                )},
                {LayerManager.Mutate.Mask, new Mutation(LayerManager.Mutate.Mask, "Mask", Resources.mask_16x16,
                    "Masks the LCD output image given a greyscale (0-255) pixel input image.\n" +
                    "Useful to correct light uniformity, but a proper mask must be created first based on real measurements per printer.\n" +
                    "NOTE 1: Masks should respect printer resolution or they will be resized to fit.\n" +
                    "NOTE 2: Run only this tool after all repairs and other transformations."
                )},
                {LayerManager.Mutate.PixelDimming, new Mutation(LayerManager.Mutate.PixelDimming, "Pixel Dimming", Resources.chessboard_16x16,
                    "Dims pixels in a chosen pattern over white pixels neighborhood. The selected pattern will be repeated over the image width and height as a mask. Benefits are:\n" +
                    "1) Reduce layer expansion in big masses\n" +
                    "2) Reduce cross layer exposure\n" +
                    "3) Extend pixels life\n" +
                    "NOTE: Run only this tool after all repairs and other transformations."
                )},
                {LayerManager.Mutate.Erode, new Mutation(LayerManager.Mutate.Erode, null, Resources.compress_alt_16x16,
                "The basic idea of erosion is just like soil erosion only, it erodes away the boundaries of foreground object (Always try to keep foreground in white). " +
                        "So what happens is that, all the pixels near boundary will be discarded depending upon the size of kernel. So the thickness or size of the foreground object decreases or simply white region decreases in the image. It is useful for removing small white noises, detach two connected objects, etc.",
                        Resources.mutation_erosion
                )},
                {LayerManager.Mutate.Dilate, new Mutation(LayerManager.Mutate.Dilate, null, Resources.expand_alt_16x16,
                    "It is just opposite of erosion. Here, a pixel element is '1' if at least one pixel under the kernel is '1'. So it increases the white region in the image or size of foreground object increases. Normally, in cases like noise removal, erosion is followed by dilation. Because, erosion removes white noises, but it also shrinks our object. So we dilate it. Since noise is gone, they won't come back, but our object area increases. It is also useful in joining broken parts of an object.",
                    Resources.mutation_dilation
                )},
                {LayerManager.Mutate.Opening, new Mutation(LayerManager.Mutate.Opening, "Noise Removal", null,
                    "Noise Removal/Opening is just another name of erosion followed by dilation. It is useful in removing noise.",
                    Resources.mutation_opening
                )},
                {LayerManager.Mutate.Closing, new Mutation(LayerManager.Mutate.Closing, "Gap Closing", Resources.bowling_ball_16x16,
                    "Gap Closing is reverse of Opening, Dilation followed by Erosion. It is useful in closing small holes inside the foreground objects, or small black points on the object.",
                    Resources.mutation_closing
                )},
                {LayerManager.Mutate.Gradient, new Mutation(LayerManager.Mutate.Gradient, null, Resources.burn_16x16,
                    "It's the difference between dilation and erosion of an image.",
                    Resources.mutation_gradient
                )},
                /*{Mutation.Mutates.TopHat, new Mutation(Mutation.Mutates.TopHat,
                    "It's the difference between input image and Opening of the image.",
                    Properties.Resources.mutation_tophat
                )},
                {Mutation.Mutates.BlackHat, new Mutation(Mutation.Mutates.BlackHat,
                    "It's the difference between the closing of the input image and input image.",
                    Properties.Resources.mutation_blackhat
                )},*/
                /*{Mutation.Mutates.HitMiss, new Mutation(Mutation.Mutates.HitMiss,
                    "The Hit-or-Miss transformation is useful to find patterns in binary images. In particular, it finds those pixels whose neighbourhood matches the shape of a first structuring element B1 while not matching the shape of a second structuring element B2 at the same time.",
                    null
                )},*/
                {LayerManager.Mutate.ThresholdPixels, new Mutation(LayerManager.Mutate.ThresholdPixels, "Threshold Pixels", Resources.th_16x16,
                    "Manipulates pixels values giving a threshold, maximum and a operation type.\n" +
                    "If a pixel brightness is less or equal to the threshold value, set this pixel to 0, otherwise set to defined maximum value.\n" +
                    "More info: https://docs.opencv.org/master/d7/d4d/tutorial_py_thresholding.html"
                )},
                {LayerManager.Mutate.Blur, new Mutation(LayerManager.Mutate.Blur, "Blur", Resources.blur_16x16,
                    "Blur and averaging images with various low pass filters\n" +
                    "Note: Printer must support AntiAliasing on firmware to able to use this function\n" +
                    "More information: https://docs.opencv.org/master/d4/d13/tutorial_py_filtering.html "
                )},
                /*{LayerManager.Mutate.PyrDownUp, new Mutation(LayerManager.Mutate.PyrDownUp, "Big Blur", Resources.blur_16x16,
                    "Performs down/up-sampling step of Gaussian pyramid decomposition.\n" +
                    "First down-samples the image by rejecting even rows and columns, after performs up-sampling step of Gaussian pyramid decomposition.\n" +
                    "This operation will add a big blur to edges, creating a over-exaggerated anti-aliasing and as result can make edges smoother\n" +
                    "Note: Printer must support AntiAliasing on firmware to able to use this function."
                )},
                {LayerManager.Mutate.SmoothMedian, new Mutation(LayerManager.Mutate.SmoothMedian, "Smooth Median", Resources.blur_16x16,
                    "Each pixel becomes the median of its surrounding pixels.\n" +
                    "A good way to remove noise and can be used to reconstruct or intensify the antialiasing level.\n" +
                    "Note 1: Printer must support AntiAliasing on firmware to able to use this function.\n" +
                    "Note 2: Iterations must be a odd number."
                )},
                {LayerManager.Mutate.SmoothGaussian, new Mutation(LayerManager.Mutate.SmoothGaussian,  "Smooth Gaussian", Resources.blur_16x16,
                    "Each pixel is a sum of fractions of each pixel in its neighborhood\n" +
                    "A good way to remove noise and can be used to reconstruct or intensify the antialiasing level.\n" +
                    "Very fast, but does not preserve sharp edges well.\n" +
                    "Note 1: Printer must support AntiAliasing on firmware to able to use this function.\n" +
                    "Note 2: Iterations must be a odd number."
                )},*/
            };


        public FrmLoading FrmLoading { get; }

        public static FileFormat SlicerFile
        {
            get => Program.SlicerFile;
            set => Program.SlicerFile = value;
        }

        public uint ActualLayer { get; set; }

        public Mat ActualLayerImage { get; private set; }

        public Mat ActualLayerImageBgr { get; private set; } = new Mat();

        public List<LayerIssue> Issues { get; set; }

        public int TotalIssues => Issues?.Count ?? 0;

        public bool IsChangingLayer { get; set; }

        // Represents a reverse index from the end of the ZoomLevels array
        public int AutoZoomBackIndex { get; set; } = 1;

        // Supported ZoomLevels for Layer Preview.
        // These settings eliminate very small zoom factors from the ImageBox default values,
        // while ensuring that 4K/5K build plates can still easily fit on screen.  
        public static readonly int[] ZoomLevels =
            { 20, 25, 30, 50, 75, 100, 150, 200, 300, 400, 500, 600, 700, 800, 1200, 1600 };

        // Count of the bottom portion of the full zoom range which will be skipped for
        // assignable actions such as auto-zoom level, and crosshair fade level.  If values
        // are added/removed from ZoomLevels above, this value may also need to be adjusted.
        public static int ZoomLevelSkipCount { get; } = 7; // Start at 2x which is index 7.

        /// <summary>
        /// Returns the zoom level at which the crosshairs will fade and no longer be displayed
        /// </summary>
        public int CrosshairFadeLevel => ZoomLevels[Settings.Default.DefaultCrosshairFade + ZoomLevelSkipCount];

        /// <summary>
        /// Returns the zoom level that will be used for autozoom actions
        /// </summary>
        private int AutoZoomLevel => ZoomLevels[ZoomLevels.Length - AutoZoomBackIndex -1];

        public PixelHistory PixelHistory { get; } = new PixelHistory();

	    // Track last open tab for when PixelEditor tab is removed.
        public TabPage ControlLeftLastTab { get; set; }

        public uint SavesCount { get; set; }

        private bool SupressLayerZoomEvent { get; set; }

        #endregion

        #region Constructors
        public FrmMain()
        {
            InitializeComponent();
            FrmLoading = new FrmLoading();
            Program.SetAllControlsFontSize(Controls, 11);
            Program.SetAllControlsFontSize(FrmLoading.Controls, 11);

            if (Settings.Default.UpdateSettings)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                Settings.Default.Save();
            }
            Clear();

            tsLayerImageLayerDifference.Checked = Settings.Default.LayerDifferenceDefault;
            tsIssuesRefreshIslands.Checked = Settings.Default.ComputeIslands;
            tsIssuesRefreshResinTraps.Checked = Settings.Default.ComputeResinTraps;
            tsLayerImageLayerOutlinePrintVolumeBounds.Checked = Settings.Default.OutlinePrintVolumeBounds;
            tsLayerImageLayerOutlineLayerBounds.Checked = Settings.Default.OutlineLayerBounds;
            tsLayerImageLayerOutlineHollowAreas.Checked = Settings.Default.OutlineHollowAreas;

            // Initialize pbLayer zoom levels to use the discrete factors from ZoomLevels
            pbLayer.ZoomLevels = new Cyotek.Windows.Forms.ZoomLevelCollection(ZoomLevels);
            // Initialize the zoom level used for autozoom based on the stored default settings.
            AutoZoomBackIndex = 
                ConvZoomToBackIndex(ZoomLevels[Settings.Default.DefaultAutoZoomLock + ZoomLevelSkipCount]);

            if (Settings.Default.StartMaximized || Width >= Screen.FromControl(this).WorkingArea.Width ||
                Height >= Screen.FromControl(this).WorkingArea.Height)
            {
                WindowState = FormWindowState.Maximized;
            }


            DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            DragDrop += (s, e) => { ProcessFile((string[])e.Data.GetData(DataFormats.FileDrop)); };

            foreach (LayerManager.Mutate mutate in (LayerManager.Mutate[])Enum.GetValues(typeof(LayerManager.Mutate)))
            {
                if(!Mutations.ContainsKey(mutate)) continue;
                var item = new ToolStripMenuItem(Mutations[mutate].MenuName)
                {
                    ToolTipText = Mutations[mutate].Description, Tag = mutate, AutoToolTip = true, Image = Mutations[mutate].MenuImage
                };
                item.Click += EventClick;
                menuMutate.DropDownItems.Add(item);
            }

            
            foreach (PixelDrawing.BrushShapeType brushShape in (PixelDrawing.BrushShapeType[])Enum.GetValues(
                typeof(PixelDrawing.BrushShapeType)))
            {
                cbPixelEditorDrawingBrushShape.Items.Add(brushShape);
            }
            cbPixelEditorDrawingBrushShape.SelectedIndex = 0;

            foreach (LineType lineType in (LineType[])Enum.GetValues(
                typeof(LineType)))
            {
                if(lineType == LineType.Filled) continue;
                cbPixelEditorDrawingLineType.Items.Add(lineType);
                cbPixelEditorTextLineType.Items.Add(lineType);
            }
            cbPixelEditorDrawingLineType.SelectedItem = LineType.AntiAlias;
            cbPixelEditorTextLineType.SelectedItem = LineType.AntiAlias;

            foreach (FontFace font in (FontFace[])Enum.GetValues(
                typeof(FontFace)))
            {
                cbPixelEditorTextFontFace.Items.Add(font);
            }
            cbPixelEditorTextFontFace.SelectedIndex = 0;

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
                    var startIndex = htmlCode.IndexOf(searchFor, StringComparison.InvariantCultureIgnoreCase) + searchFor.Length;
                    var endIndex = htmlCode.IndexOf("\"", startIndex, StringComparison.InvariantCultureIgnoreCase);
                    var version = htmlCode.Substring(startIndex, endIndex- startIndex);
                    if (string.Compare(version, $"v{FrmAbout.AssemblyVersion}", StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        Invoke((MethodInvoker)delegate
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
                Debug.WriteLine(e.Message);
            }
        }

        #endregion

        #region Overrides

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            AddLog("UVtools Start");
            ProcessFile(Program.Args);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (ReferenceEquals(SlicerFile, null) || IsChangingLayer)
            {
                return;
            }

            if (e.KeyChar == '-')
            {
                ShowLayer(false);
                e.Handled = true;
                return;
            }

            if (e.KeyChar == '+')
            {
                ShowLayer(true);
                e.Handled = true;
                return;
            }

            base.OnKeyPress(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            
            if (ReferenceEquals(SlicerFile, null))
            {
                return;
            }

            if (e.KeyCode == Keys.Home)
            {
                btnFirstLayer.PerformClick();
                e.Handled = true;
                return;
            }

            

            if (e.KeyCode == Keys.End)
            {
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
                    tsLayerImageRotate.PerformClick();
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

            lbActualLayer.Location = new Point(lbActualLayer.Location.X,
                Math.Max(1,
                    Math.Min(tbLayer.Height - 40,
                        (int)(tbLayer.Height - tbLayer.Value * ((float)tbLayer.Height / tbLayer.Maximum)) - lbActualLayer.Height / 2)
                ));
        }

        
        #endregion

        #region Events

        private void EventClick(object sender, EventArgs e)
        {
            if (sender.GetType() == typeof(ToolStripMenuItem))
            {
                ToolStripMenuItem item = (ToolStripMenuItem) sender;
                /*******************
                 *    Main Menu    *
                 ******************/
                // File
                if (ReferenceEquals(sender, menuFileOpen))
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
                                MessageBox.Show(exception.ToString(), "Error while try opening the file",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }

                    return;
                }

                if (ReferenceEquals(sender, menuFileOpenNewWindow))
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

                if (ReferenceEquals(sender, menuFileReload))
                {
                    ProcessFile(ActualLayer);
                    return;
                }

                if (ReferenceEquals(sender, menuFileSave))
                {
                    if (SavesCount == 0 && Settings.Default.FileSavePromptOverwrite)
                    {
                        if (MessageBox.Show(
                            "This action will overwrite the input file, if it's the original is best practice to make a copy first (Save As) and work from that copy instead.\n" +
                            "Do you want to continue and overwrite the file?", "Overwrite input file?",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                    }


                    DisableGUI();
                    FrmLoading.SetDescription($"Saving {Path.GetFileName(SlicerFile.FileFullPath)}");

                    Task<bool> task = Task<bool>.Factory.StartNew(() =>
                    {
                        bool result = false;
                        try
                        {
                            SlicerFile.Save(FrmLoading.RestartProgress());
                            result = true;
                        }
                        catch (OperationCanceledException)
                        {

                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error while saving the file", MessageBoxButtons.OK,
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

                        return result;
                    });

                    FrmLoading.ShowDialog();

                    SavesCount++;
                    menuFileSave.Enabled =
                        menuFileSaveAs.Enabled = false;

                    return;
                }

                if (ReferenceEquals(sender, menuFileSaveAs))
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
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            DisableGUI();
                            FrmLoading.SetDescription($"Saving {Path.GetFileName(dialog.FileName)}");

                            Task<bool> task = Task<bool>.Factory.StartNew(() =>
                            {
                                bool result = false;
                                try
                                {
                                    SlicerFile.SaveAs(dialog.FileName, FrmLoading.RestartProgress());
                                    result = true;
                                }
                                catch (OperationCanceledException)
                                {

                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message, "Error while saving the file", MessageBoxButtons.OK,
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

                                return result;
                            });

                            FrmLoading.ShowDialog();

                            SavesCount++;
                            menuFileSave.Enabled =
                                menuFileSaveAs.Enabled = false;
                            UpdateTitle();
                            //ProcessFile(dialog.FileName);
                        }
                    }


                    return;
                }

                if (ReferenceEquals(sender, menuFileClose))
                {
                    if (menuFileSave.Enabled)
                    {
                        if (MessageBox.Show("There are unsaved changes, do you want close this file without save?",
                                "Close file without save?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) !=
                            DialogResult.Yes)
                        {
                            return;
                        }
                    }

                    Clear();
                    return;
                }

                if (ReferenceEquals(sender, menuFileSettings))
                {
                    using (FrmSettings frmSettings = new FrmSettings())
                    {
                        if (frmSettings.ShowDialog() != DialogResult.OK) return;
                    }

                    return;
                }

                if (ReferenceEquals(sender, menuFileExit))
                {
                    if (menuFileSave.Enabled)
                    {
                        if (MessageBox.Show("There are unsaved changes, do you want exit without save?",
                                "Exit without save?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) !=
                            DialogResult.Yes)
                        {
                            return;
                        }
                    }

                    Application.Exit();
                    return;
                }


                if (ReferenceEquals(sender, menuFileExtract))
                {
                    using (FolderBrowserDialog folder = new FolderBrowserDialog())
                    {
                        string fileNameNoExt = Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath);
                        folder.SelectedPath = string.IsNullOrEmpty(Settings.Default.FileExtractDefaultDirectory)
                            ? Path.GetDirectoryName(SlicerFile.FileFullPath)
                            : Settings.Default.FileExtractDefaultDirectory;
                        folder.Description =
                            $"A \"{fileNameNoExt}\" folder will be created on your selected folder to dump the content.";
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
                                        $"Extraction was successful ({FrmLoading.StopWatch.ElapsedMilliseconds / 1000}s), browser folder to see it contents.\n{finalPath}\nPress 'Yes' if you want open the target folder, otherwise select 'No' to continue.",
                                        "Extraction completed", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
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
                if (!ReferenceEquals(item.Tag, null))
                {
                    if (item.Tag.GetType() == typeof(FileFormat.PrintParameterModifier))
                    {
                        FileFormat.PrintParameterModifier modifier = (FileFormat.PrintParameterModifier) item.Tag;
                        using (FrmInputBox inputBox = new FrmInputBox(modifier,
                            decimal.Parse(SlicerFile.GetValueFromPrintParameterModifier(modifier).ToString())))
                        {
                            if (inputBox.ShowDialog() != DialogResult.OK) return;
                            var value = inputBox.NewValue;

                            if (!SlicerFile.SetValueFromPrintParameterModifier(modifier, value))
                            {
                                MessageBox.Show(
                                    $"Unable to set '{modifier.Name}' value, was not found, it may not implemented yet.",
                                    "Operation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            RefreshInfo();

                            menuFileSave.Enabled =
                                menuFileSaveAs.Enabled = true;
                        }

                        return;

                    }

                    if (item.Tag.GetType() == typeof(LayerManager.Mutate))
                    {
                        LayerManager.Mutate mutate = (LayerManager.Mutate) item.Tag;
                        MutateLayers(mutate);
                        return;
                    }
                }

                // View

                // Tools
                if (ReferenceEquals(sender, menuToolsRepairLayers))
                {
                    uint layerStart;
                    uint layerEnd;
                    uint closingIterations;
                    uint openingIterations;
                    byte removeIslandsBelowEqualPixels;
                    bool repairIslands;
                    bool removeEmptyLayers;
                    bool repairResinTraps;
                    using (var frmRepairLayers = new FrmToolRepairLayers())
                    {
                        if (frmRepairLayers.ShowDialog() != DialogResult.OK) return;

                        layerStart = frmRepairLayers.LayerRangeStart;
                        layerEnd = frmRepairLayers.LayerRangeEnd;
                        closingIterations = frmRepairLayers.ClosingIterations;
                        openingIterations = frmRepairLayers.OpeningIterations;
                        removeIslandsBelowEqualPixels = frmRepairLayers.RemoveIslandsBelowEqualPixels;
                        repairIslands = frmRepairLayers.RepairIslands;
                        removeEmptyLayers = frmRepairLayers.RemoveEmptyLayers;
                        repairResinTraps = frmRepairLayers.RepairResinTraps;
                    }

                    if (ReferenceEquals(Issues, null))
                    {
                        var islandConfig = GetIslandDetectionConfiguration();
                        islandConfig.Enabled = repairIslands && removeIslandsBelowEqualPixels > 0;
                        var resinTrapConfig = GetResinTrapDetectionConfiguration();
                        resinTrapConfig.Enabled = repairResinTraps;

                        if(islandConfig.Enabled || resinTrapConfig.Enabled)
                            ComputeIssues(islandConfig, resinTrapConfig);
                    }

                    DisableGUI();
                    FrmLoading.SetDescription("Repairing Layers and Issues");

                    var task = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            SlicerFile.LayerManager.RepairLayers(layerStart, layerEnd, closingIterations,
                                openingIterations, removeIslandsBelowEqualPixels, repairIslands, removeEmptyLayers, repairResinTraps, Issues,
                                FrmLoading.RestartProgress());
                        }
                        catch (OperationCanceledException)
                        {

                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                    if (removeEmptyLayers)
                    {
                        UpdateLayerLimits();
                        RefreshInfo();
                    }

                    ShowLayer();

                    ComputeIssues(GetIslandDetectionConfiguration(), GetResinTrapDetectionConfiguration());

                    menuFileSave.Enabled =
                        menuFileSaveAs.Enabled = true;
                    return;
                }

                if (ReferenceEquals(sender, menuToolsChangeResolution))
                {
                    uint newResolutionX;
                    uint newResolutionY;
                    using (var frm = new FrmToolChangeResolution(SlicerFile.ResolutionX, SlicerFile.ResolutionY, SlicerFile.LayerManager.BoundingRectangle))
                    {
                        if (frm.IsDisposed || frm.ShowDialog() != DialogResult.OK) return;
                        newResolutionX = frm.NewResolutionX;
                        newResolutionY = frm.NewResolutionY;
                    }

                    DisableGUI();
                    FrmLoading.SetDescription($"Change Resolution from {SlicerFile.ResolutionX} x {SlicerFile.ResolutionY} to {newResolutionX} x {newResolutionY}");

                    var task = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            SlicerFile.LayerManager.ChangeResolution(newResolutionX, newResolutionY, FrmLoading.RestartProgress(false));
                        }
                        catch (OperationCanceledException)
                        {

                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                    //UpdateLayerLimits();
                    RefreshInfo();
                    ShowLayer();
                    tsLayerResolution.Text = ActualLayerImage.Size.ToString();

                    menuFileSave.Enabled =
                        menuFileSaveAs.Enabled = true;

                    return;
                }

                if (ReferenceEquals(sender, menuToolsLayerReHeight))
                {
                    OperationLayerReHeight operation = null;
                    using (var frm = new FrmToolLayerReHeight(SlicerFile.LayerCount, SlicerFile.LayerHeight))
                    {
                        if (frm.IsDisposed || frm.ShowDialog() != DialogResult.OK) return;
                        operation = frm.Operation;
                    }

                    DisableGUI();
                    FrmLoading.SetDescription($"Layer Re-Height from {SlicerFile.LayerHeight}mm to {operation.LayerHeight}mm");

                    var task = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            SlicerFile.LayerManager.ReHeight(operation, FrmLoading.RestartProgress());
                        }
                        catch (OperationCanceledException)
                        {

                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                    UpdateLayerLimits();
                    RefreshInfo();
                    ShowLayer();

                    menuFileSave.Enabled =
                        menuFileSaveAs.Enabled = true;

                    return;
                }

                if (ReferenceEquals(sender, menuToolsLayerRemoval))
                {
                    using (var frm = new FrmToolEmpty(ActualLayer, "Layer Removal", "Removes layer(s) in a given range", "Remove"))
                    {
                        if (frm.ShowDialog() != DialogResult.OK) return;

                        DisableGUI();
                        FrmLoading.SetDescription($"Layer Removal from {frm.LayerRangeStart} to {frm.LayerRangeEnd}");

                        var task = Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                SlicerFile.LayerManager.RemoveLayer(frm.LayerRangeStart, frm.LayerRangeEnd);
                            }
                            catch (OperationCanceledException)
                            {

                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                        UpdateLayerLimits();
                        RefreshInfo();
                        ShowLayer();

                        menuFileSave.Enabled =
                            menuFileSaveAs.Enabled = true;
                    }

                    return;
                }

                if (ReferenceEquals(sender, menuToolsPattern))
                {
                    OperationPattern operation = new OperationPattern(SlicerFile.LayerManager.BoundingRectangle,
                        (uint)ActualLayerImage.Width, (uint)ActualLayerImage.Height);

                    if (operation.MaxRows < 2 && operation.MaxCols < 2)
                    {
                        MessageBox.Show("The available free volume is not enough to pattern this object.\n" +
                                        "To run this tool the free space must allow at least 1 copy.", "Unable to pattern", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    using (var frm = new FrmToolPattern(operation))
                    {
                        if (frm.ShowDialog() != DialogResult.OK) return;

                        DisableGUI();
                        FrmLoading.SetDescription($"Pattern {frm.OperationPattern.Cols} x {frm.OperationPattern.Rows}");

                        var task = Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                SlicerFile.LayerManager.ToolPattern(frm.LayerRangeStart, frm.LayerRangeEnd,
                                    frm.OperationPattern, FrmLoading.RestartProgress());
                            }
                            catch (OperationCanceledException)
                            {

                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                        ShowLayer();

                        menuFileSave.Enabled =
                            menuFileSaveAs.Enabled = true;


                    }

                    return;
                }

                // About
                if (ReferenceEquals(sender, menuHelpAbout))
                {
                    Program.FrmAbout.ShowDialog();
                    return;
                }

                if (ReferenceEquals(sender, menuHelpWebsite))
                {
                    using (Process.Start(About.Website))
                    {
                    }

                    return;
                }

                if (ReferenceEquals(sender, menuHelpDonate))
                {
                    MessageBox.Show(
                        "All my work here is given for free (OpenSource), it took some hours to build, test and polish the program.\n" +
                        "If you're happy to contribute for a better program and for my work i will appreciate the tip.\n" +
                        "A browser window will be open and forward to my paypal address after you click 'OK'.\nHappy Printing!",
                        "Donation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    using (Process.Start(About.Donate))
                    {
                    }

                    return;
                }

                if (ReferenceEquals(sender, menuHelpBenchmark))
                {
                    using (var frmBenchmark = new FrmBenchmark())
                    {
                        frmBenchmark.ShowDialog();
                    }
                    return;
                }

                if (ReferenceEquals(sender, menuHelpInstallPrinters))
                {
                    var PEFolder =
                        $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}PrusaSlicer";
                    if (!Directory.Exists(PEFolder))
                    {
                        var result = MessageBox.Show(
                            "Unable to detect PrusaSlicer on your system, make sure you have lastest version installed on your system.\n" +
                            "Click 'OK' to open PrusaSlicer webpage for program download\n" +
                            "Click 'Cancel' to cancel this operation",
                            "Unable to detect PrusaSlicer on your system",
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

            if (ReferenceEquals(sender, menuToolsLayerClone) || ReferenceEquals(sender, tsLayerClone))
            {
                using (var frm = new FrmToolLayerClone(ReferenceEquals(sender, menuToolsLayerClone) ? -1 : (int)ActualLayer))
                {
                    if (frm.ShowDialog() != DialogResult.OK) return;

                    DisableGUI();
                    FrmLoading.SetDescription($"Layer clone from {frm.LayerRangeStart} to {frm.LayerRangeEnd}");

                    var task = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            SlicerFile.LayerManager.CloneLayer(frm.LayerRangeStart, frm.LayerRangeEnd, frm.Clones, FrmLoading.RestartProgress());
                        }
                        catch (OperationCanceledException)
                        {

                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                    UpdateLayerLimits();
                    RefreshInfo();
                    ShowLayer();

                    menuFileSave.Enabled =
                        menuFileSaveAs.Enabled = true;
                }

                return;
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
                    menuFileSave.Enabled = menuFileSaveAs.Enabled = true;
                }
            }

            if (ReferenceEquals(sender, tsThumbnailsExport))
            {
                tsThumbnailsExport.ShowDropDown();
                return;
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
            if (ReferenceEquals(sender, tsPropertiesExport))
            {
                tsPropertiesExport.ShowDropDown();
                return;
            }

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
                                foreach (var property in type.GetProperties())
                                {
                                    tw.WriteLine($"{property.Name} = {property.GetValue(config)}");
                                }

                                tw.WriteLine();
                            }

                            tw.Close();
                        }

                        if (MessageBox.Show(
                                $"Properties save was successful, do you want open the file with default editor?.\nPress 'Yes' if you want open the target file, otherwise select 'No' to continue.",
                                "Properties save completed", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
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
                    foreach (var property in type.GetProperties())
                    {
                        sb.AppendLine($"{property.Name} = {property.GetValue(config)}");
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
            if (ReferenceEquals(sender, btnGCodeSave))
            {
                btnGCodeSave.ShowDropDown();
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
                                "GCode save was successful, do you want open the file with default editor?.\nPress 'Yes' if you want open the target file, otherwise select 'No' to continue.",
                                "GCode save completed", MessageBoxButtons.YesNo, MessageBoxIcon.Question) !=
                            DialogResult.Yes) return;
                        using (Process.Start(dialog.FileName)) { }
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

                if (MessageBox.Show("Are you sure you want to remove all selected issues from image?\n" +
                                    "Warning: Removing a island can cause another issues to appears if the next layer have land in top of the removed island.\n" +
                                    "Always check previous and next layer before perform a island removal to ensure safe operation.",
                    "Remove Issues?",
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
                            SlicerFile.LayerManager.RemoveLayer(layersRemove);
                            result = true;
                        }

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Unsuccessful Removal", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                List<LayerIssue> removeList = new List<LayerIssue>();
                foreach (LayerIssue issue in flvIssues.SelectedObjects)
                {
                    //if (!issue.HaveValidPoint) continue;
                    if (issue.Type == LayerIssue.IssueType.TouchingBound) continue;
                    if (issue.Type == LayerIssue.IssueType.Island)
                    {
                        var nextLayer = issue.Layer.Index + 1;
                        if (nextLayer >= SlicerFile.LayerCount) continue;
                        if (whiteListLayers.Contains(nextLayer)) continue;
                        whiteListLayers.Add(nextLayer);
                    }

                    Issues.Remove(issue);
                    removeList.Add(issue);
                }
                flvIssues.RemoveObjects(removeList);

                if (layersRemove.Count > 0)
                {
                    UpdateLayerLimits();
                    RefreshInfo();
                }

                if (Settings.Default.PartialUpdateIslandsOnEditing)
                {
                    UpdateIslands(whiteListLayers);
                }

                ShowLayer();
                UpdateIssuesInfo();
                menuFileSave.Enabled =
                    menuFileSaveAs.Enabled = true;

                return;
            }

            if (ReferenceEquals(sender, tsIssuesRepair))
            {
                EventClick(menuToolsRepairLayers, e);
                return;
            }

            if (ReferenceEquals(sender, tsIssuesRefresh))
            {
                if (MessageBox.Show("Are you sure you want to compute issues?", "Issues Compute",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

                ComputeIssues(GetIslandDetectionConfiguration(), GetResinTrapDetectionConfiguration());

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
                foreach (PixelOperation item in flvPixelHistory.SelectedObjects)
                {
                    PixelHistory.Items.Remove(item);
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
            if (ReferenceEquals(sender, tsLayerImageRotate) || 
                ReferenceEquals(sender, tsLayerImageLayerDifference) ||
                ReferenceEquals(sender, tsLayerImageHighlightIssues) ||
                ReferenceEquals(sender, tsLayerImageShowCrosshairs) ||
                ReferenceEquals(sender, tsLayerImageLayerOutlinePrintVolumeBounds) ||
                ReferenceEquals(sender, tsLayerImageLayerOutlineLayerBounds) ||
                ReferenceEquals(sender, tsLayerImageLayerOutlineHollowAreas) ||
                ReferenceEquals(sender, tsLayerImageLayerOutlineEdgeDetection)

                )
            {
                ShowLayer();
                if (ReferenceEquals(sender, tsLayerImageRotate))
                {
                    ZoomToFit();
                }
                return;
            }

            if (ReferenceEquals(sender, tsLayerImageLayerOutline))
            {
                tsLayerImageLayerOutline.ShowDropDown();
                return;
            }

            if (ReferenceEquals(sender, tsLayerImagePixelEdit))
            {
                if (tsLayerImagePixelEdit.Checked)
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

            if (ReferenceEquals(sender, tsLayerRmove))
            {
                if (MessageBox.Show("Are you sure you want to remove current layer?\nThis operation is irreversible!",
                        "Remove current layer?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) !=
                    DialogResult.Yes) return;

                SlicerFile.LayerManager.RemoveLayer(ActualLayer);
                UpdateLayerLimits();
                RefreshInfo();
                ShowLayer();
                menuFileSave.Enabled = menuFileSaveAs.Enabled = true;

                return;
            }

            if (ReferenceEquals(sender, tsLayerImageExport))
            {
                tsLayerImageExport.ShowDropDown();
                return;
            }
            if (ReferenceEquals(sender, tsLayerImageExportFile))
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

            if (ReferenceEquals(sender, tsLayerImageExportClipboard))
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
                ShowLayer(SlicerFile.LayerCount-1);
                return;
            }

            if (ReferenceEquals(sender, btnFindLayer))
            {
                using (FrmInputBox inputBox = new FrmInputBox("Go to layer", "Select a layer index to go to", ActualLayer, null, 0, SlicerFile.LayerCount - 1, 0, "Layer"))
                {
                    if (inputBox.ShowDialog() == DialogResult.OK)
                    {
                        ShowLayer((uint)inputBox.NewValue);
                    }
                }
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
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            FileFormat fileFormat = (FileFormat)menuItem.Tag;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.FileName = Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath);
                dialog.Filter = fileFormat.FileFilter;
                dialog.InitialDirectory = string.IsNullOrEmpty(Settings.Default.FileConvertDefaultDirectory) ? Path.GetDirectoryName(SlicerFile.FileFullPath) : Settings.Default.FileConvertDefaultDirectory;

                //using (FileFormat instance = (FileFormat)Activator.CreateInstance(type)) 
                //using (CbddlpFile file = new CbddlpFile())



                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    DisableGUI();
                    FrmLoading.SetDescription($"Converting {Path.GetFileName(SlicerFile.FileFullPath)} to {Path.GetExtension(dialog.FileName)}");

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
                            MessageBox.Show($"Convertion was unsuccessful! Maybe not implemented...\n{ex.Message}", "Convertion unsuccessful", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        finally
                        {
                            Invoke((MethodInvoker)delegate {
                                // Running on the UI thread
                                EnableGUI(true);
                            });
                        }

                        return false;
                    });

                    if (FrmLoading.ShowDialog() == DialogResult.OK && task.Result)
                    {
                        if (MessageBox.Show($"Convertion is completed: {Path.GetFileName(dialog.FileName)} in {FrmLoading.StopWatch.ElapsedMilliseconds / 1000}s\n" +
                                            "Do you want open the converted file in a new window?",
                            "Convertion completed", MessageBoxButtons.YesNo,
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

        private void pbLayer_Zoomed(object sender, Cyotek.Windows.Forms.ImageBoxZoomEventArgs e)
        {
            if (SupressLayerZoomEvent) return;
            Debug.WriteLine($"{DateTime.Now.Ticks}: Zoomed");
            // Update zoom level display in the toolstrip
            if (ConvZoomToBackIndex(e.NewZoom) == AutoZoomBackIndex)
            {
                tsLayerImageZoom.Text = $"Zoom: [ {e.NewZoom / 100f}x";
                tsLayerImageZoomLock.Visible = true;
            }
            else
            {
                tsLayerImageZoom.Text = $"Zoom: [ {e.NewZoom / 100f}x ]";
                tsLayerImageZoomLock.Visible = false;
            }
            
            // Refresh the layer to properly render the crosshair at various zoom transitions
            if (tsLayerImageShowCrosshairs.Checked && !ReferenceEquals(Issues, null) && 
                flvIssues.SelectedIndices.Count > 0 &&
                (e.OldZoom < 50 && e.NewZoom >= 50  // Trigger refresh as crosshair thickness increases at lower zoom levels
                    || e.OldZoom > 100 && e.NewZoom <= 100
                    || e.OldZoom >= 50 && e.OldZoom <= 100 && (e.NewZoom < 50 || e.NewZoom > 100)
                    || e.OldZoom <= CrosshairFadeLevel && e.NewZoom > CrosshairFadeLevel // Trigger refresh as zoom level manually crosses fade threshold
                    || e.OldZoom > CrosshairFadeLevel && e.NewZoom <= CrosshairFadeLevel) &&
                flvIssues.SelectedObjects.Cast<LayerIssue>().Any(issue => // Find a valid candidate to update layer preview, otherwise quit
                    issue.LayerIndex == ActualLayer && issue.Type != LayerIssue.IssueType.EmptyLayer && issue.Type != LayerIssue.IssueType.TouchingBound)
                )
            {
                // A timer is used here rather than invoking ShowLayer directly to eliminate sublte visual flashing
                // that will occur on the transition when the crosshair fades or unfades if ShowLayer is called directly.
                layerZoomTimer.Start();
            }
        }

        private void pbLayer_MouseUp(object sender, MouseEventArgs e)
        {
            // Shift must be pressed for any pixel edit action, middle button is ignored.
            if (!tsLayerImagePixelEdit.Checked || (e.Button & MouseButtons.Middle) != 0 || 
                (ModifierKeys & Keys.Shift) == 0) return;
            if (!pbLayer.IsPointInImage(e.Location)) return;
            var location = pbLayer.PointToImage(e.Location);
            _lastPixelMouseLocation = Point.Empty;

            // Left or Alt-Right Adds pixel, Right or Alt-Left removes pixel
            DrawPixel(e.Button == MouseButtons.Left ^ (ModifierKeys & Keys.Alt) != 0, location);
            //SlicerFile[ActualLayer].LayerMat = ActualLayerImage;
            RefreshPixelHistory();
        }

        private Point _lastPixelMouseLocation = Point.Empty;
        private void pbLayer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!pbLayer.IsPointInImage(e.Location) ||(ModifierKeys & Keys.Shift) == 0) return;
            var location = pbLayer.PointToImage(e.Location);
            if (_lastPixelMouseLocation == e.Location) return;
            _lastPixelMouseLocation = e.Location;

            int x = location.X;
            int y = location.Y;

            if (tsLayerImageRotate.Checked)
            {
                x = location.Y;
                y = ActualLayerImage.Height - 1 - location.X;
            }

            tsLayerImageMouseLocation.Text = $"{{X={x}, Y={y}, B={ActualLayerImage.GetByte(x, y)}}}";
            // Bail here if we're not in a draw operation, if the mouse button is not either
            // left or right, or if the location of the mouse pointer is not within the image.
            if (tabControlPixelEditor.SelectedIndex != (int) PixelOperation.PixelOperationType.Drawing) return;
            if (!tsLayerImagePixelEdit.Checked || (e.Button & MouseButtons.Middle) != 0) return;
            if (!pbLayer.IsPointInImage(e.Location)) return;

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
                if(!(flvIssues.SelectedObject is LayerIssue issue)) return;

                if (issue.Type == LayerIssue.IssueType.TouchingBound || issue.Type == LayerIssue.IssueType.EmptyLayer || (issue.X == -1 && issue.Y == -1))
                {
                    ZoomToFit();
                }
                else if (issue.X >= 0 && issue.Y >= 0)
                {
                    if (Settings.Default.AutoZoomIssues ^ (ModifierKeys & Keys.Alt) != 0)
                    {
                        ZoomToIssue(issue);
                    }
                    else
                    {
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

                int x = operation.Location.X;
                int y = operation.Location.Y;

                if (tsLayerImageRotate.Checked)
                {
                    x = ActualLayerImage.Height - 1 - operation.Location.Y;
                    y = operation.Location.X;
                }

                if (Settings.Default.AutoZoomIssues ^ (ModifierKeys & Keys.Alt) != 0)
                {
                    SupressLayerZoomEvent = true;
                    pbLayer.ZoomToRegion(x, y, operation.Size.Width, operation.Size.Height);
                    SupressLayerZoomEvent = false;
                    pbLayer.ZoomOut(true);
                }
                else
                {
                    CenterLayerAt(x, y);
                }

                // Unconditionally refresh layer preview here to ensure highlighting for pixel
                // operations properly reflects the active selection.
                ShowLayer(operation.LayerIndex);
            }
        }

        private void TbLayerOnMouseWheel(object sender, MouseEventArgs e)
        {
            ((HandledMouseEventArgs)e).Handled = true;
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
            foreach (ToolStripItem item in menuMutate.DropDownItems)
            {
                item.Enabled = false;
            }

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
            tsLayerBounds.Text = "Bounds:";
            tsLayerImageMouseLocation.Text = "{X=0, Y=0}";

            tsThumbnailsResolution.Text = 
            tsLayerPreviewTime.Text =
            tsLayerResolution.Text =
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
            menuMutate.Enabled =
            menuTools.Enabled =
            btnFirstLayer.Enabled =
            btnNextLayer.Enabled =
            btnPreviousLayer.Enabled =
            btnLastLayer.Enabled =
            btnFindLayer.Enabled =

            tsLayerImagePixelEdit.Checked =

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
                    FrmLoading.DialogResult = FrmLoading.Progress.Token.IsCancellationRequested ? DialogResult.Cancel : DialogResult.OK;
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
                    MessageBox.Show(exception.ToString(), "Error while try opening the file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                return;
            }
        }

        void ProcessFile(string fileName, uint actualLayer = 0)
        {
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
                    Invoke((MethodInvoker)delegate {
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
            
            if (SlicerFile.LayerCount == 0 )
            {
                MessageBox.Show("It seens the file don't have any layer, the causes can be:\n" +
                                "- Empty\n" +
                                "- Corrupted\n" +
                                "- Lacking a sliced model\n" +
                                "- A programing internal error\n\n" +
                                "Please check your file and retry", "Error reading the file - Lacking of layers", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //Clear();
                return;
            }

            ActualLayerImage?.Dispose();
            ActualLayerImage = SlicerFile[0].LayerMat;

            if (Settings.Default.LayerAutoRotateBestView)
            {
                tsLayerImageRotate.Checked = ActualLayerImage.Height > ActualLayerImage.Width;
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

            foreach (ToolStripItem item in menuMutate.DropDownItems)
            {
                item.Enabled = true;
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
                
            tbLayer.Enabled =
            //pbLayers.Enabled =
            menuEdit.Enabled = 
            menuMutate.Enabled =
            menuTools.Enabled =

            tsIssuesRepair.Enabled =
            tsIssuesRefresh.Enabled =

                btnFindLayer.Enabled =
                true;


            tabControlLeft.TabPages.Insert(1, tabPageIssues);
            if (!ReferenceEquals(SlicerFile.GCode, null))
            {
                tabControlLeft.TabPages.Insert(1, tabPageGCode);
            }
            


            tabControlLeft.SelectedIndex = 0;
            tsLayerResolution.Text = $"{{Width={SlicerFile.ResolutionX}, Height={SlicerFile.ResolutionY}}}";

            UpdateLayerLimits();
            ShowLayer(Math.Min(actualLayer, SlicerFile.LayerCount-1));

            RefreshInfo();

            if (Settings.Default.LayerZoomToFit)
            {
                ZoomToFit();
            }

            UpdateTitle();

            if (Settings.Default.ComputeIssuesOnLoad)
            {
                ComputeIssues(GetIslandDetectionConfiguration(), GetResinTrapDetectionConfiguration());
            }
        }

        private void UpdateLayerLimits()
        {
            lbMaxLayer.Text = $"{SlicerFile.TotalHeight}mm\n{SlicerFile.LayerCount - 1}";
            lbInitialLayer.Text = $"{SlicerFile.LayerHeight}mm\n0";
            tbLayer.Maximum = (int)SlicerFile.LayerCount - 1;
        }

        private void UpdateTitle()
        {
            Text = ReferenceEquals(SlicerFile, null) ?
                $"{FrmAbout.AssemblyTitle}   Version: {FrmAbout.AssemblyVersion}" : 
                $"{FrmAbout.AssemblyTitle}   File: {Path.GetFileName(SlicerFile.FileFullPath)} ({Math.Round(FrmLoading.StopWatch.ElapsedMilliseconds/1000m, 2)}s)   Version: {FrmAbout.AssemblyVersion}";

#if  DEBUG
            Text += "   [DEBUG]";
#endif
        }

        private void RefreshInfo()
        {
            menuEdit.DropDownItems.Clear();

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
            }

            flvProperties.ClearObjects();

            if (!ReferenceEquals(SlicerFile.Configs, null))
            {
                List<SlicerPropertyItem> items = new List<SlicerPropertyItem>();
                foreach (object config in SlicerFile.Configs)
                {
                    foreach (PropertyInfo propertyInfo in config.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if(propertyInfo.Name.Equals("Item")) continue;
                        var value = propertyInfo.GetValue(config);
                        if (ReferenceEquals(value, null)) continue;
                        if (value is IList list)
                        {
                            items.Add(new SlicerPropertyItem(propertyInfo.Name, list.Count.ToString(), config.GetType().Name));
                        }
                        else
                        {
                            items.Add(new SlicerPropertyItem(propertyInfo.Name, value.ToString(), config.GetType().Name));
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
            AddStatusBarItem(nameof(SlicerFile.InitialLayerCount), SlicerFile.InitialLayerCount);
            AddStatusBarItem(nameof(SlicerFile.InitialExposureTime), SlicerFile.InitialExposureTime, "s");
            AddStatusBarItem(nameof(SlicerFile.LayerExposureTime), SlicerFile.LayerExposureTime, "s");
            AddStatusBarItem(nameof(SlicerFile.PrintTime), Math.Round(SlicerFile.PrintTime / 3600, 2), "h");
            AddStatusBarItem(nameof(SlicerFile.UsedMaterial), Math.Round(SlicerFile.UsedMaterial, 2), "ml");
            AddStatusBarItem(nameof(SlicerFile.MaterialCost), SlicerFile.MaterialCost, "€");
            AddStatusBarItem(nameof(SlicerFile.MaterialName), SlicerFile.MaterialName);
            AddStatusBarItem(nameof(SlicerFile.MachineName), SlicerFile.MachineName);
        }

        private void UpdateGCode()
        {
            if (SlicerFile.GCode is null) return;
            tbGCode.Text = SlicerFile.GCode.ToString();
            tsGCodeLabelLines.Text = $"Lines: {tbGCode.Lines.Length}";
            tsGcodeLabelChars.Text = $"Chars: {tbGCode.Text.Length}";
        }

        /// <summary>
        /// Reshow current layer
        /// </summary>
        void ShowLayer() => ShowLayer(Math.Min(ActualLayer, SlicerFile.LayerCount-1));

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
        void ShowLayer(uint layerNum)
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

            Debug.WriteLine($"Show Layer: {layerNum}");

            ActualLayer = layerNum;
            btnLastLayer.Enabled = btnNextLayer.Enabled = layerNum < SlicerFile.LayerCount - 1;
            btnFirstLayer.Enabled = btnPreviousLayer.Enabled = layerNum > 0;


            var layer = SlicerFile[ActualLayer];
            VectorOfVectorOfPoint layerContours = null;
            Mat layerHierarchy = null;
            Array layerHierarchyJagged = null;

            void initContours()
            {
                if (!ReferenceEquals(layerContours, null)) return;
                layerContours = new VectorOfVectorOfPoint();
                layerHierarchy = new Mat();
                CvInvoke.FindContours(ActualLayerImage, layerContours, layerHierarchy, RetrType.Ccomp,
                    ChainApproxMethod.ChainApproxSimple);
                layerHierarchyJagged = layerHierarchy.GetData();
            }

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

                CvInvoke.CvtColor(ActualLayerImage, ActualLayerImageBgr, ColorConversion.Gray2Bgr);

                var imageSpan = ActualLayerImage.GetPixelSpan<byte>();
                var imageBgrSpan = ActualLayerImageBgr.GetPixelSpan<byte>();


                if (tsLayerImageLayerOutlineEdgeDetection.Checked)
                {
                    using (var grayscale = new Mat())
                    {
                        CvInvoke.Canny(ActualLayerImage, grayscale, 80, 40, 3, true);
                        CvInvoke.CvtColor(grayscale, ActualLayerImageBgr, ColorConversion.Gray2Bgr);
                    }
                }
                else if (tsLayerImageLayerDifference.Checked)
                {
                    if (layerNum > 0 && layerNum < SlicerFile.LayerCount-1)
                    {
                        using (var previousImage = SlicerFile[layerNum - 1].LayerMat)
                        {
                            using (var nextImage = SlicerFile[layerNum + 1].LayerMat)
                            {
                                var previousSpan = previousImage.GetPixelSpan<byte>();
                                var nextSpan = nextImage.GetPixelSpan<byte>();

                                for (int pixel = 0; pixel < imageSpan.Length; pixel++)
                                {
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
                                    var bgrPixel = pixel * 3;
                                    imageBgrSpan[bgrPixel] = color.B; // B
                                    imageBgrSpan[++bgrPixel] = color.G; // G
                                    imageBgrSpan[++bgrPixel] = color.R; // R
                                }
                            }
                        }
                    }
                }

                var selectedIssues = flvIssues.SelectedObjects;
                //List<LayerIssue> selectedIssues = (from object t in selectedIssuesRaw where ((LayerIssue) t).LayerIndex == ActualLayer select (LayerIssue) t).ToList();

                if (tsLayerImageHighlightIssues.Checked &&
                    !ReferenceEquals(Issues, null))
                {
                    foreach (var issue in Issues)
                    {
                        if(issue.LayerIndex != ActualLayer) continue;
                        if(!issue.HaveValidPoint) continue;

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

                            continue;
                        }

                        switch (issue.Type)
                        {
                            case LayerIssue.IssueType.Island:
                                color = selectedIssues.Contains(issue)
                                    ? Settings.Default.IslandHLColor
                                    : Settings.Default.IslandColor;
                                break;
                            case LayerIssue.IssueType.TouchingBound:
                                color = Settings.Default.TouchingBoundsColor;
                                break;
                        }

                        foreach (var pixel in issue)
                        {
                            int pixelPos = ActualLayerImage.GetPixelPos(pixel);
                            byte brightness = imageSpan[pixelPos];
                            if (brightness == 0) continue;

                            int pixelBgrPos = pixelPos*ActualLayerImageBgr.NumberOfChannels;

                            var newColor = color.FactorColor(brightness, 80);

                            imageBgrSpan[pixelBgrPos] = newColor.B; // B
                            imageBgrSpan[pixelBgrPos + 1] = newColor.G; // G
                            imageBgrSpan[pixelBgrPos + 2] = newColor.R; // R
                        }
                    }
                }

                if (tsLayerImageLayerOutlinePrintVolumeBounds.Checked)
                {
                    CvInvoke.Rectangle(ActualLayerImageBgr, SlicerFile.LayerManager.BoundingRectangle, 
                        new MCvScalar(Settings.Default.OutlinePrintVolumeBoundsColor.B, Settings.Default.OutlinePrintVolumeBoundsColor.G, Settings.Default.OutlinePrintVolumeBoundsColor.R), Settings.Default.OutlinePrintVolumeBoundsLineThickness);
                }

                if (tsLayerImageLayerOutlineLayerBounds.Checked)
                {
                    CvInvoke.Rectangle(ActualLayerImageBgr, SlicerFile[layerNum].BoundingRectangle, 
                        new MCvScalar(Settings.Default.OutlineLayerBoundsColor.B, Settings.Default.OutlineLayerBoundsColor.G, Settings.Default.OutlineLayerBoundsColor.R), Settings.Default.OutlineLayerBoundsLineThickness);
                }

                if (tsLayerImageLayerOutlineHollowAreas.Checked)
                {
                    //CvInvoke.Threshold(ActualLayerImage, grayscale, 1, 255, ThresholdType.Binary);
                    initContours();

                    /*
                     * hierarchy[i][0]: the index of the next contour of the same level
                     * hierarchy[i][1]: the index of the previous contour of the same level
                     * hierarchy[i][2]: the index of the first child
                     * hierarchy[i][3]: the index of the parent
                     */
                    for (int i = 0; i < layerContours.Size; i++)
                    {
                        if ((int)layerHierarchyJagged.GetValue(0, i, 2) == -1 && (int)layerHierarchyJagged.GetValue(0, i, 3) != -1)
                        {
                            //var r = CvInvoke.BoundingRectangle(contours[i]);
                            //CvInvoke.Rectangle(ActualLayerImageBgr, r, new MCvScalar(0, 0, 255), 2);
                            CvInvoke.DrawContours(ActualLayerImageBgr, layerContours, i,
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
                    if(PixelHistory[index].LayerIndex != ActualLayer) continue;
                    var operation = PixelHistory[index];
                    if (operation.OperationType == PixelOperation.PixelOperationType.Drawing)
                    {
                        var operationDrawing = (PixelDrawing)operation;
                        var color = operationDrawing.IsAdd
                            ? (flvPixelHistory.SelectedObjects.Contains(operation)
                                  ? Settings.Default.PixelEditorAddPixelHLColor
                                  : Settings.Default.PixelEditorAddPixelColor)
                            : (flvPixelHistory.SelectedObjects.Contains(operation)
                                  ? Settings.Default.PixelEditorRemovePixelHLColor
                                  : Settings.Default.PixelEditorRemovePixelColor);
                        if (operationDrawing.BrushSize == 1)
                        {
                            ActualLayerImageBgr.SetByte(operation.Location.X, operation.Location.Y, new []{ color.B, color.G, color.R });
                            continue;
                        }

                        switch (operationDrawing.BrushShape)
                        {
                            case PixelDrawing.BrushShapeType.Rectangle:
                                CvInvoke.Rectangle(ActualLayerImageBgr, operationDrawing.Rectangle, new MCvScalar(color.B, color.G, color.R), operationDrawing.Thickness, operationDrawing.LineType);
                                break;
                            case PixelDrawing.BrushShapeType.Circle:
                                CvInvoke.Circle(ActualLayerImageBgr, operation.Location, operationDrawing.BrushSize / 2, new MCvScalar(color.B, color.G, color.R), operationDrawing.Thickness, operationDrawing.LineType);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (operation.OperationType == PixelOperation.PixelOperationType.Text)
                    {
                        var operationText = (PixelText)operation;
                        var color = operationText.IsAdd
                            ? (flvPixelHistory.SelectedObjects.Contains(operation)
                                  ? Settings.Default.PixelEditorAddPixelHLColor
                                  : Settings.Default.PixelEditorAddPixelColor)
                            : (flvPixelHistory.SelectedObjects.Contains(operation)
                                  ? Settings.Default.PixelEditorRemovePixelHLColor
                                  : Settings.Default.PixelEditorRemovePixelColor);

                        CvInvoke.PutText(ActualLayerImageBgr, operationText.Text, operationText.Location, operationText.Font, operationText.FontScale, new MCvScalar(color.B, color.G, color.R), operationText.Thickness, operationText.LineType, operationText.Mirror);
                    }
                    else if (operation.OperationType == PixelOperation.PixelOperationType.Eraser)
                    {
                        initContours();
                        if(imageSpan[ActualLayerImage.GetPixelPos(operation.Location)] < 10) continue;
                        var color = flvPixelHistory.SelectedObjects.Contains(operation)
                                        ? Settings.Default.PixelEditorRemovePixelHLColor
                                        : Settings.Default.PixelEditorRemovePixelColor;
                        for (int i = 0; i < layerContours.Size; i++)
                        {
                            if (CvInvoke.PointPolygonTest(layerContours[i], operation.Location, false) >= 0)
                            {
                                CvInvoke.DrawContours(ActualLayerImageBgr, layerContours, i, new MCvScalar(color.B, color.G, color.R), -1);
                                break;
                            }
                        }
                    }
                    else if (operation.OperationType == PixelOperation.PixelOperationType.Supports)
                    {
                        var operationSupport = (PixelSupport)operation;
                        var color = flvPixelHistory.SelectedObjects.Contains(operation)
                                        ? Settings.Default.PixelEditorSupportHLColor
                                        : Settings.Default.PixelEditorSupportColor;

                        CvInvoke.Circle(ActualLayerImageBgr, operation.Location, operationSupport.TipDiameter / 2, new MCvScalar(color.B, color.G, color.R), -1);
                    }
                    else if (operation.OperationType == PixelOperation.PixelOperationType.DrainHole)
                    {
                        var operationDrainHole = (PixelDrainHole)operation;
                        var color = flvPixelHistory.SelectedObjects.Contains(operation)
                                        ? Settings.Default.PixelEditorDrainHoleHLColor
                                        : Settings.Default.PixelEditorDrainHoleColor;

                        CvInvoke.Circle(ActualLayerImageBgr, operation.Location, operationDrainHole.Diameter / 2, new MCvScalar(color.B, color.G, color.R), -1);
                    }
                }

                // Show crosshairs for selected issues if crosshair mode is enabled via toolstrip button.
                // Even when enabled, crosshairs are hidden in pixel edit mode when SHIFT is pressed.
                if (tsLayerImageShowCrosshairs.Checked && 
                    !ReferenceEquals(Issues, null) &&
                    flvIssues.SelectedIndices.Count > 0 &&
                    pbLayer.Zoom <= CrosshairFadeLevel && // Only draw crosshairs when zoom level is below the configurable crosshair fade threshold.
                    !(tsLayerImagePixelEdit.Checked && (ModifierKeys & Keys.Shift) != 0))
                {
                    // Gradually increase line thickness from 1 to 3 at the lower-end of the zoom range.
                    // This prevents the crosshair lines from disappearing due to being too thin to
                    // render at very low zoom factors.
                    var lineThickness = (pbLayer.Zoom > 100) ? 1 : (pbLayer.Zoom < 50) ? 3 : 2;

                    foreach (LayerIssue issue in selectedIssues)
                    {
                        // Don't render crosshairs for selected issue that are not on the current layer, or for 
                        // issue types that don't have a specific location or bounds.
                        if (issue.LayerIndex != ActualLayer || issue.Type == LayerIssue.IssueType.EmptyLayer
                               || issue.Type == LayerIssue.IssueType.TouchingBound) continue;

                        var color = new MCvScalar(Color.Red.B, Color.Red.G, Color.Red.R);
                        CvInvoke.Line(ActualLayerImageBgr,
                            new Point(0, issue.BoundingRectangle.Y + issue.BoundingRectangle.Height / 2),
                            new Point(issue.BoundingRectangle.Left - 10, issue.BoundingRectangle.Y + issue.BoundingRectangle.Height / 2),
                            color,
                            lineThickness);

                        CvInvoke.Line(ActualLayerImageBgr,
                            new Point(issue.BoundingRectangle.Right + 10, issue.BoundingRectangle.Y + issue.BoundingRectangle.Height / 2),
                            new Point(ActualLayerImageBgr.Width, issue.BoundingRectangle.Y + issue.BoundingRectangle.Height / 2),
                            color,
                            lineThickness);


                        CvInvoke.Line(ActualLayerImageBgr,
                            new Point(issue.BoundingRectangle.X + issue.BoundingRectangle.Width / 2, 0),
                            new Point(issue.BoundingRectangle.X + issue.BoundingRectangle.Width / 2, issue.BoundingRectangle.Top - 10),
                            color,
                            lineThickness);

                        CvInvoke.Line(ActualLayerImageBgr,
                            new Point(issue.BoundingRectangle.X + issue.BoundingRectangle.Width / 2, issue.BoundingRectangle.Bottom + 10),
                            new Point(issue.BoundingRectangle.X + issue.BoundingRectangle.Width / 2, ActualLayerImageBgr.Height),
                            color,
                            lineThickness);
                    }
                }

                if (tsLayerImageRotate.Checked)
                {
                    CvInvoke.Rotate(ActualLayerImageBgr, ActualLayerImageBgr, RotateFlags.Rotate90Clockwise);
                }
                

                //watch.Restart();
                var imageBmp = ActualLayerImageBgr.ToBitmap();
                //imageBmp.MakeTransparent();
                pbLayer.Image = imageBmp;
                pbLayer.Image.Tag = ActualLayerImageBgr;
                //Debug.WriteLine(watch.ElapsedMilliseconds);


                byte percent = (byte)((layerNum + 1) * 100 / SlicerFile.LayerCount);

                float pixelPercent = (float) Math.Round(layer.NonZeroPixelCount * 100f / (SlicerFile.ResolutionX * SlicerFile.ResolutionY), 2);
                tsLayerImagePixelCount.Text = $"Pixels: {layer.NonZeroPixelCount} ({pixelPercent}%)";
                tsLayerBounds.Text = $"Bounds: {layer.BoundingRectangle}";
                tsLayerImagePixelCount.Invalidate();
                tsLayerBounds.Invalidate();
                tsLayerInfo.Update();
                tsLayerInfo.Refresh();

                layerContours?.Dispose();
                layerHierarchy?.Dispose();


                watch.Stop();
                tsLayerPreviewTime.Text = $"{watch.ElapsedMilliseconds}ms";
                //lbLayers.Text = $"{SlicerFile.GetHeightFromLayer(layerNum)} / {SlicerFile.TotalHeight}mm\n{layerNum} / {SlicerFile.LayerCount-1}\n{percent}%";
                lbActualLayer.Text = $"{layer.PositionZ}mm\n{ActualLayer}\n{percent}%";
                lbActualLayer.Location = new Point(lbActualLayer.Location.X, 
                    Math.Max(1, 
                        Math.Min(tbLayer.Height- lbActualLayer.Height, 
                            (int)(tbLayer.Height - tbLayer.Value * ((float)tbLayer.Height / tbLayer.Maximum)) - lbActualLayer.Height/2)
                ));

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
                    if (!ReferenceEquals(tabPageIssues.Tag, null) || !Settings.Default.AutoComputeIssuesClickOnTab) return;
                    ComputeIssues(GetIslandDetectionConfiguration(), GetResinTrapDetectionConfiguration());
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
                if (cbPixelEditorDrawingBrushShape.SelectedIndex == (int)PixelDrawing.BrushShapeType.Circle)
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
            if (ReferenceEquals(sender, this))
            {
                // This event repeats for as long as the key is pressed, so if we've
                // already set the cursor from a previous key down event, just return.
                if (pbLayer.Cursor == Cursors.Cross) return;

                // Pixel Edit is active, Shift is down, and the cursor is over the image region.
                if (e.KeyCode == Keys.ShiftKey &&
                    pbLayer.ClientRectangle.Contains(pbLayer.PointToClient(Control.MousePosition)) &&
                    tsLayerImagePixelEdit.Checked)
                {
                    pbLayer.Cursor = Cursors.Cross;
                    pbLayer.PanMode = Cyotek.Windows.Forms.ImageBoxPanMode.None;
                    if (!ReferenceEquals(SlicerFile, null)) ShowLayer();
                }
                return;
            }
        }

        private void EventKeyUp(object sender, KeyEventArgs e)
        {
            // As with EventKeyDown, we handle this event at the to top level
            // to ensure cursor and pan functionaty are restored regardless
            // of which form has focus when shift is released.
            if (ReferenceEquals(sender, this))
            {
                if (e.KeyCode == Keys.ShiftKey)
                {
                    pbLayer.Cursor = Cursors.Default;
                    pbLayer.PanMode = Cyotek.Windows.Forms.ImageBoxPanMode.Left;
                    if (!ReferenceEquals(SlicerFile, null)) ShowLayer();
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
                ShowLayer((bool)layerScrollTimer.Tag);
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
                if ((bool)issueScrollTimer.Tag)
                {
                    EventClick(tsIssueNext, null);
                }
                else
                {
                    EventClick(tsIssuePrevious, null);
                }
                return;
            }
        }

        private void EventMouseClick(object sender, MouseEventArgs e)
        {
            if (ReferenceEquals(sender, pbLayer))
            {
                if ((ModifierKeys & Keys.Control) != 0)
                {
                    // CTRL click within pbLayer performs double click action
                    HandleMouseDoubleClick(sender, e);
                }
                return;
            }
        }

        private void EventMouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Ignore double click if CTRL is pressed. Prevents CTRL-click
            // events that emulate double click from firing twice.
            if ((ModifierKeys & Keys.Control) != 0) return;
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
                    if (tsLayerImageShowCrosshairs.Checked &&
                        !ReferenceEquals(Issues, null) && flvIssues.SelectedIndices.Count > 0 && 
                        pbLayer.Zoom <= CrosshairFadeLevel && AutoZoomLevel > CrosshairFadeLevel &&
                        flvIssues.SelectedObjects.Cast<LayerIssue>().Any(issue => // Find a valid candidate to update layer preview, otherwise quit
                        issue.LayerIndex == ActualLayer && issue.Type != LayerIssue.IssueType.EmptyLayer && issue.Type != LayerIssue.IssueType.TouchingBound))
                    {
                        // Refresh the preview without the crosshairs before zooming-in.
                        // Prevents zoomed-in crosshairs from breifly being displayed before
                        // the Layer Preview is refreshed post-zoom.
                        tsLayerImageShowCrosshairs.Checked = false;
                        ShowLayer();
                        tsLayerImageShowCrosshairs.Checked = true;
                    }

                    CenterLayerAt(location, AutoZoomLevel);
                    return;
                }
                if ((e.Button & MouseButtons.Middle) != 0)
                {
                    // Reset auto-zoom level based on current zoom level and
                    // refresh toolstrip zoom indicator.
                    var currentBackIndex = ConvZoomToBackIndex(pbLayer.Zoom);
                    // Don't allow small zoom values to be locked for auto-zoom
                    if (currentBackIndex >= ZoomLevels.Length - ZoomLevelSkipCount) return;
                    AutoZoomBackIndex = currentBackIndex;
                    tsLayerImageZoom.Text = $"Zoom: [ {pbLayer.Zoom / 100f}x";
                    tsLayerImageZoomLock.Visible = true;
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
            int x = location.X;
            int y = location.Y;

            if (tsLayerImageRotate.Checked)
            {
                x = location.Y;
                y = ActualLayerImage.Height - 1 - location.X;
            }

            PixelOperation operation = null;
            Bitmap bmp = pbLayer.Image as Bitmap;

            if (tabControlPixelEditor.SelectedIndex == (byte) PixelOperation.PixelOperationType.Drawing)
            {
                LineType lineType = (LineType)cbPixelEditorDrawingLineType.SelectedItem;
                PixelDrawing.BrushShapeType shapeType =
                    (PixelDrawing.BrushShapeType) cbPixelEditorDrawingBrushShape.SelectedIndex;

                ushort brushSize = (ushort) nmPixelEditorDrawingBrushSize.Value;
                short thickness = (short) (nmPixelEditorDrawingThickness.Value == 0 ? 1 : nmPixelEditorDrawingThickness.Value);

                uint minLayer = (uint) Math.Max(0, ActualLayer - nmPixelEditorDrawingLayersBelow.Value);
                uint maxLayer = (uint) Math.Min(SlicerFile.LayerCount-1, ActualLayer+nmPixelEditorDrawingLayersAbove.Value);
                for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
                {
                    operation = new PixelDrawing(layerIndex, new Point(x, y), lineType,
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
                                        gfx.DrawRectangle(new Pen(color, thickness), Math.Max(0, location.X - shiftPos), Math.Max(0, location.Y - shiftPos), (int)nmPixelEditorDrawingBrushSize.Value, (int)nmPixelEditorDrawingBrushSize.Value);
                                    else
                                        gfx.FillRectangle(new SolidBrush(color), Math.Max(0, location.X - shiftPos), Math.Max(0, location.Y - shiftPos), (int)nmPixelEditorDrawingBrushSize.Value, (int)nmPixelEditorDrawingBrushSize.Value);
                                    break;
                                case PixelDrawing.BrushShapeType.Circle:
                                    if (thickness > 0)
                                        gfx.DrawEllipse(new Pen(color, thickness), Math.Max(0, location.X - shiftPos), Math.Max(0, location.Y - shiftPos), (int)nmPixelEditorDrawingBrushSize.Value, (int)nmPixelEditorDrawingBrushSize.Value);
                                    else
                                        gfx.FillEllipse(new SolidBrush(color), Math.Max(0, location.X - shiftPos), Math.Max(0, location.Y - shiftPos), (int)nmPixelEditorDrawingBrushSize.Value, (int)nmPixelEditorDrawingBrushSize.Value);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }
                }
            }
            else if (tabControlPixelEditor.SelectedIndex == (byte)PixelOperation.PixelOperationType.Text)
            {
                if (string.IsNullOrEmpty(tbPixelEditorTextText.Text) || nmPixelEditorTextFontScale.Value < 0.2m) return;

                LineType lineType = (LineType)cbPixelEditorTextLineType.SelectedItem;
                FontFace fontFace = (FontFace)cbPixelEditorTextFontFace.SelectedItem;

                uint minLayer = (uint)Math.Max(0, ActualLayer - nmPixelEditorTextLayersBelow.Value);
                uint maxLayer = (uint)Math.Min(SlicerFile.LayerCount - 1, ActualLayer + nmPixelEditorTextLayersAbove.Value);
                for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
                {
                    operation = new PixelText(layerIndex, new Point(x, y), lineType,
                        fontFace, (double) nmPixelEditorTextFontScale.Value, (ushort) nmPixelEditorTextThickness.Value, tbPixelEditorTextText.Text, cbPixelEditorTextMirror.Checked, isAdd);

                    if (PixelHistory.Contains(operation)) continue;
                    PixelHistory.Add(operation);
                }
                ShowLayer();
                return;
            }
            else if (tabControlPixelEditor.SelectedIndex == (byte) PixelOperation.PixelOperationType.Eraser)
            {
                if (ActualLayerImage.GetByte(x, y) < 10) return;
                uint minLayer = (uint)Math.Max(0, ActualLayer - nmPixelEditorEraserLayersBelow.Value);
                uint maxLayer = (uint)Math.Min(SlicerFile.LayerCount - 1, ActualLayer + nmPixelEditorEraserLayersAbove.Value);
                for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
                {
                    operation = new PixelEraser(layerIndex, new Point(x, y));

                    if (PixelHistory.Contains(operation)) continue;
                    PixelHistory.Add(operation);
                }
                ShowLayer();
                return;
            }
            else if (tabControlPixelEditor.SelectedIndex == (byte) PixelOperation.PixelOperationType.Supports)
            {
                if (ActualLayer == 0) return;
                operation = new PixelSupport(ActualLayer, new Point(x, y),
                    (byte) nmPixelEditorSupportsTipDiameter.Value, (byte)nmPixelEditorSupportsPillarDiameter.Value, (byte)nmPixelEditorSupportsBaseDiameter.Value);

                if (PixelHistory.Contains(operation)) return;
                PixelHistory.Add(operation);

                SolidBrush brush = new SolidBrush(flvPixelHistory.SelectedObjects.Contains(operation)
                                                      ? Settings.Default.PixelEditorSupportHLColor
                                                      : Settings.Default.PixelEditorSupportColor);
                using (var gfx = Graphics.FromImage(bmp))
                {
                    int shiftPos = (int)nmPixelEditorSupportsTipDiameter.Value / 2;
                    gfx.SmoothingMode = SmoothingMode.HighSpeed;
                    gfx.FillEllipse(brush, Math.Max(0, location.X - shiftPos), Math.Max(0, location.Y - shiftPos), (int)nmPixelEditorSupportsTipDiameter.Value, (int)nmPixelEditorSupportsTipDiameter.Value);
                }
            }
            else if (tabControlPixelEditor.SelectedIndex == (byte)PixelOperation.PixelOperationType.DrainHole)
            {
                if (ActualLayer == 0) return;
                operation = new PixelDrainHole(ActualLayer, new Point(x, y), (byte)nmPixelEditorDrainHoleDiameter.Value);

                if (PixelHistory.Contains(operation)) return;
                PixelHistory.Add(operation);

                SolidBrush brush = new SolidBrush(flvPixelHistory.SelectedObjects.Contains(operation)
                                                      ? Settings.Default.PixelEditorDrainHoleHLColor
                                                      : Settings.Default.PixelEditorDrainHoleColor);
                using (var gfx = Graphics.FromImage(bmp))
                {
                    int shiftPos = (int)nmPixelEditorDrainHoleDiameter.Value / 2;
                    gfx.SmoothingMode = SmoothingMode.HighSpeed;
                    gfx.FillEllipse(brush, Math.Max(0, location.X - shiftPos), Math.Max(0, location.Y - shiftPos), (int)nmPixelEditorDrainHoleDiameter.Value, (int)nmPixelEditorDrainHoleDiameter.Value);
                }
            }
            else
            {
                throw new NotImplementedException("Missing pixel operation");
            }

            
            
            pbLayer.Invalidate();
            //pbLayer.Update();
            //pbLayer.Refresh();
            //menuFileSave.Enabled = menuFileSaveAs.Enabled = true;
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

        

        public void MutateLayers(LayerManager.Mutate mutator)
        {
            uint layerStart;
            uint layerEnd;
            uint iterationsStart = 0;
            uint iterationsEnd = 0;
            bool fade = false;

            Matrix<byte> kernel = null;
            Point kernelAnchor = Point.Empty;

            ThresholdType thresholdType = ThresholdType.Binary;

            OperationMove operationMove = null;

            double x = 0;
            double y = 0;

            Mat mat = null;

            Matrix<byte> evenPattern = null;
            Matrix<byte> oddPattern = null;

            FrmMutationBlur.BlurAlgorithm blurAlgorithm = FrmMutationBlur.BlurAlgorithm.Blur;

            switch (mutator)
            {
                case LayerManager.Mutate.Move:
                    using (FrmMutationMove inputBox = new FrmMutationMove(Mutations[mutator], SlicerFile.LayerManager.BoundingRectangle, (uint) ActualLayerImage.Width, (uint) ActualLayerImage.Height))
                    {
                        if (inputBox.ShowDialog() != DialogResult.OK) return;
                        layerStart = inputBox.LayerRangeStart;
                        layerEnd = inputBox.LayerRangeEnd;
                        operationMove = inputBox.OperationMove;
                    }

                    break;
                case LayerManager.Mutate.Resize:
                    using (FrmMutationResize inputBox = new FrmMutationResize(Mutations[mutator]))
                    {
                        if (inputBox.ShowDialog() != DialogResult.OK) return;
                        layerStart = inputBox.LayerRangeStart;
                        layerEnd = inputBox.LayerRangeEnd;
                        x = inputBox.X;
                        y = inputBox.Y;
                        fade = inputBox.Fade;
                    }

                    break;
                case LayerManager.Mutate.Flip:
                    using (FrmMutationOneComboBox inputBox = new FrmMutationOneComboBox(Mutations[mutator]))
                    {
                        if (inputBox.ShowDialog() != DialogResult.OK) return;
                        layerStart = inputBox.LayerRangeStart;
                        layerEnd = inputBox.LayerRangeEnd;
                        iterationsStart = (uint) inputBox.SelectedValue;
                        fade = inputBox.MakeCopy;
                    }
                    break;
                case LayerManager.Mutate.Rotate:
                    using (FrmMutationOneNumericalInput inputBox = new FrmMutationOneNumericalInput(Mutations[mutator]))
                    {
                        if (inputBox.ShowDialog() != DialogResult.OK) return;
                        layerStart = inputBox.LayerRangeStart;
                        layerEnd = inputBox.LayerRangeEnd;
                        x = (double) inputBox.Value;
                    }
                    break;
                case LayerManager.Mutate.Solidify:
                    using (FrmToolEmpty inputBox = new FrmToolEmpty(Mutations[mutator]))
                    {
                        if (inputBox.ShowDialog() != DialogResult.OK) return;
                        layerStart = inputBox.LayerRangeStart;
                        layerEnd = inputBox.LayerRangeEnd;
                    }
                    break;
                case LayerManager.Mutate.Mask:
                    using (FrmMutationMask inputBox = new FrmMutationMask(Mutations[mutator]))
                    {
                        if (inputBox.ShowDialog() != DialogResult.OK) return;
                        layerStart = inputBox.LayerRangeStart;
                        layerEnd = inputBox.LayerRangeEnd;
                        mat = inputBox.Mask;
                    }
                    break;
                case LayerManager.Mutate.PixelDimming:
                    using (FrmMutationPixelDimming inputBox = new FrmMutationPixelDimming(Mutations[mutator]))
                    {
                        if (inputBox.ShowDialog() != DialogResult.OK) return;
                        layerStart = inputBox.LayerRangeStart;
                        layerEnd = inputBox.LayerRangeEnd;
                        iterationsStart = inputBox.BorderSize;
                        evenPattern = inputBox.EvenPattern;
                        oddPattern = inputBox.OddPattern;
                    }
                    break;
                case LayerManager.Mutate.ThresholdPixels:
                    using (FrmMutationThreshold inputBox = new FrmMutationThreshold(Mutations[mutator]))
                    {
                        if (inputBox.ShowDialog() != DialogResult.OK) return;
                        layerStart = inputBox.LayerRangeStart;
                        layerEnd = inputBox.LayerRangeEnd;
                        iterationsStart = inputBox.Threshold;
                        iterationsEnd = inputBox.Maximum;
                        thresholdType = inputBox.ThresholdTypeValue;
                    }
                    break;
                case LayerManager.Mutate.Blur:
                    using (FrmMutationBlur inputBox = new FrmMutationBlur(Mutations[mutator]))
                    {
                        if (inputBox.ShowDialog() != DialogResult.OK) return;
                        iterationsStart = inputBox.KSize;
                        if (iterationsStart == 0) return;
                        layerStart = inputBox.LayerRangeStart;
                        layerEnd = inputBox.LayerRangeEnd;

                        blurAlgorithm = inputBox.BlurAlgorithmType;

                        if (blurAlgorithm == FrmMutationBlur.BlurAlgorithm.Filter2D)
                        {
                            kernel = inputBox.KernelMatrix;
                            kernelAnchor = inputBox.KernelAnchor;
                        }
                    }

                    break;
                default:
                    using (FrmMutation inputBox = new FrmMutation(Mutations[mutator]))
                    {
                        if (inputBox.ShowDialog() != DialogResult.OK) return;
                        iterationsStart = inputBox.Iterations;
                        if (iterationsStart == 0) return;
                        layerStart = inputBox.LayerRangeStart;
                        layerEnd = inputBox.LayerRangeEnd;
                        iterationsEnd = inputBox.IterationsEnd;

                        kernel = inputBox.KernelMatrix;
                        kernelAnchor = inputBox.KernelAnchor;
                    }

                    break;
            }

            DisableGUI();
            FrmLoading.SetDescription($"Mutating - {Mutations[mutator].MenuName}");
            var progress = FrmLoading.RestartProgress();

            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    switch (mutator)
                    {
                        case LayerManager.Mutate.Move:
                            SlicerFile.LayerManager.MutateMove(layerStart, layerEnd, operationMove, progress);
                            break;
                        case LayerManager.Mutate.Resize:
                            SlicerFile.LayerManager.MutateResize(layerStart, layerEnd, x / 100.0, y / 100.0, fade, progress);
                            break;
                        case LayerManager.Mutate.Flip:
                            FlipType flipType = FlipType.Horizontal;
                            switch (iterationsStart)
                            {
                                case 0:
                                    flipType = FlipType.Horizontal;
                                    break;
                                case 1:
                                    flipType = FlipType.Vertical;
                                    break;
                                case 2:
                                    flipType = FlipType.Horizontal | FlipType.Vertical;
                                    break;
                            }
                            SlicerFile.LayerManager.MutateFlip(layerStart, layerEnd, flipType, fade, progress);
                            break;
                        case LayerManager.Mutate.Rotate:
                            SlicerFile.LayerManager.MutateRotate(layerStart, layerEnd, x, Inter.Linear, progress);
                            break;
                        case LayerManager.Mutate.Solidify:
                            SlicerFile.LayerManager.MutateSolidify(layerStart, layerEnd, progress);
                            break;
                        case LayerManager.Mutate.Mask:
                            SlicerFile.LayerManager.MutateMask(layerStart, layerEnd, mat, progress);
                            mat?.Dispose();
                            break;
                        case LayerManager.Mutate.PixelDimming:
                            SlicerFile.LayerManager.MutatePixelDimming(layerStart, layerEnd, evenPattern, oddPattern, (ushort) iterationsStart, progress);
                            break;
                        case LayerManager.Mutate.Erode:
                            SlicerFile.LayerManager.MutateErode(layerStart, layerEnd, (int) iterationsStart, (int) iterationsEnd, fade, progress, kernel, kernelAnchor);
                            break;
                        case LayerManager.Mutate.Dilate:
                            SlicerFile.LayerManager.MutateDilate(layerStart, layerEnd, (int)iterationsStart, (int)iterationsEnd, fade, progress, kernel, kernelAnchor);
                            break;
                        case LayerManager.Mutate.Opening:
                            SlicerFile.LayerManager.MutateOpen(layerStart, layerEnd, (int)iterationsStart, (int)iterationsEnd, fade, progress, kernel, kernelAnchor);
                            break;
                        case LayerManager.Mutate.Closing:
                            SlicerFile.LayerManager.MutateClose(layerStart, layerEnd, (int)iterationsStart, (int)iterationsEnd, fade, progress, kernel, kernelAnchor);
                            break;
                        case LayerManager.Mutate.Gradient:
                            SlicerFile.LayerManager.MutateGradient(layerStart, layerEnd, (int)iterationsStart, (int)iterationsEnd, fade, progress, kernel, kernelAnchor);
                            break;
                        /*case Mutation.Mutates.TopHat:
                            kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(9, 9),
                                new Point(-1, -1));
                            CvInvoke.MorphologyEx(image, image, MorphOp.Tophat, kernel, new Point(-1, -1),
                                (int) iterations, BorderType.Default, new MCvScalar());
                            break;
                        case Mutation.Mutates.BlackHat:
                            kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(9, 9),
                                new Point(-1, -1));
                            CvInvoke.MorphologyEx(image, image, MorphOp.Blackhat, kernel, new Point(-1, -1),
                                (int) iterations, BorderType.Default, new MCvScalar());
                            break;
                        case Mutation.Mutates.HitMiss:
                            CvInvoke.MorphologyEx(image, image, MorphOp.HitMiss, Program.KernelFindIsolated,
                                new Point(-1, -1), (int) iterations, BorderType.Default, new MCvScalar());
                            break;*/
                        case LayerManager.Mutate.ThresholdPixels:
                            SlicerFile.LayerManager.MutateThresholdPixels(layerStart, layerEnd, (byte) iterationsStart, (byte) iterationsEnd, thresholdType, progress);
                            break;
                        case LayerManager.Mutate.Blur:
                            switch (blurAlgorithm)
                            {
                                case FrmMutationBlur.BlurAlgorithm.Blur:
                                    SlicerFile.LayerManager.MutateBlur(layerStart, layerEnd, new Size((int)iterationsStart, (int)iterationsStart), kernelAnchor, BorderType.Reflect101, progress);
                                    break;
                                case FrmMutationBlur.BlurAlgorithm.Pyramid:
                                    SlicerFile.LayerManager.MutatePyrDownUp(layerStart, layerEnd, BorderType.Default, progress);
                                    break;
                                case FrmMutationBlur.BlurAlgorithm.MedianBlur:
                                    SlicerFile.LayerManager.MutateMedianBlur(layerStart, layerEnd, (int) iterationsStart, progress);
                                    break;
                                case FrmMutationBlur.BlurAlgorithm.GaussianBlur:
                                    SlicerFile.LayerManager.MutateGaussianBlur(layerStart, layerEnd, new Size((int)iterationsStart, (int)iterationsStart), 0, 0, BorderType.Reflect101, progress);
                                    break;
                                case FrmMutationBlur.BlurAlgorithm.Filter2D:
                                    SlicerFile.LayerManager.MutateFilter2D(layerStart, layerEnd, kernel, kernelAnchor, BorderType.Reflect101, progress);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                            /*case LayerManager.Mutate.PyrDownUp:
                                SlicerFile.LayerManager.MutatePyrDownUp(layerStart, layerEnd, BorderType.Default, progress);
                                break;
                            case LayerManager.Mutate.SmoothMedian:
                                SlicerFile.LayerManager.MutateMedianBlur(layerStart, layerEnd, (int)iterationsStart, progress);
                                break;
                            case LayerManager.Mutate.SmoothGaussian:
                                SlicerFile.LayerManager.MutateGaussianBlur(layerStart, layerEnd, new Size((int) iterationsStart, (int) iterationsStart), 0,0, BorderType.Default, progress);
                                break;*/
                    }

                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}\nPlease try different values for the mutation", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Invoke((MethodInvoker)delegate {
                        // Running on the UI thread
                        EnableGUI(true);
                    });
                }
            });

            FrmLoading.ShowDialog();

            ShowLayer();

            menuFileSave.Enabled =
            menuFileSaveAs.Enabled = true;
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
                tsIssueNext.Enabled = currentIssueSelected+1 < TotalIssues;
            }
            
        }

        private void ComputeIssues(IslandDetectionConfiguration islandConfig = null, ResinTrapDetectionConfiguration resinTrapConfig = null)
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
                    Issues = SlicerFile.LayerManager.GetAllIssues(islandConfig, resinTrapConfig, null, FrmLoading.RestartProgress());
                }
                catch (OperationCanceledException)
                {

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error while trying compute issues", MessageBoxButtons.OK,
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
            });

            var loadingResult = FrmLoading.ShowDialog();

            UpdateIssuesList();
        }

        /// <summary>
        /// Gets the bounding rectangle of the passed issue, automatically adjusting
        /// the coordinates and width/height to account for whether or not the layer
        /// preview image is rotated.  Used to ensure images are properly zoomed or
        /// centered independent of the layer preview rotation.
        /// </summary>
        private Rectangle GetTransposedIssueBounds(LayerIssue issue)
        {
            if (issue.X >= 0 && issue.Y >= 0)
            {
                if (issue.BoundingRectangle.IsEmpty || issue.Size == 1)
                {
                    if (tsLayerImageRotate.Checked)
                        return new Rectangle(ActualLayerImage.Height - 1 - issue.Y,
                            issue.X, 5, 5);
                }
                else
                {
                    if (tsLayerImageRotate.Checked)
                        return new Rectangle(ActualLayerImage.Height - 1 - issue.BoundingRectangle.Bottom,
                            issue.X, issue.BoundingRectangle.Height, issue.BoundingRectangle.Width);
                }
            }

            return issue.BoundingRectangle;
        }

        /// <summary>
        /// Centers layer view on a X,Y coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">X coordinate</param>
        /// <param name="zoomLevel">Zoom level to set, 0 to skip</param>
        public void CenterLayerAt(int x, int y, int zoomLevel = 0)
        {
            if (zoomLevel > 0)
                pbLayer.Zoom = zoomLevel;
            pbLayer.CenterAt(x, y);
        }

        /// <summary>
        /// Centers layer view on a middle of a given rectangle
        /// </summary>
        /// <param name="rectangle">Rectangle holding coordinates and bounds</param>
        /// <param name="zoomLevel">Zoom level to set, 0 to skip</param>
        public void CenterLayerAt(Rectangle rectangle, int zoomLevel = 0) => 
            CenterLayerAt(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2, zoomLevel);

        /// <summary>
        /// Centers layer view on a <see cref="Point"/>
        /// </summary>
        /// <param name="point">Point holding X and Y coordinates</param>
        /// <param name="zoomLevel">Zoom level to set, 0 to skip</param>
        public void CenterLayerAt(Point point, int zoomLevel = 0) => 
            CenterLayerAt(point.X, point.Y, zoomLevel);
      
        
        /// <summary>
        /// Zoom the layer preview to the passed issue, or if appropriate for issue type,
        /// Zoom to fit the plate or print bounds.
        /// </summary>
        private void ZoomToIssue(LayerIssue issue)
        {
            if (issue.Type == LayerIssue.IssueType.TouchingBound || issue.Type == LayerIssue.IssueType.EmptyLayer ||  (issue.X == -1 && issue.Y == -1))
            {
                ZoomToFit();
                return;
            }
            
            if (issue.X >= 0 && issue.Y >= 0)
            {
                // Check to see if this zoom action will cross the crosshair fade threshold
                if (tsLayerImageShowCrosshairs.Checked && !ReferenceEquals(Issues, null) && flvIssues.SelectedIndices.Count > 0
                   && pbLayer.Zoom <= CrosshairFadeLevel && AutoZoomLevel > CrosshairFadeLevel)
                {
                    // Refresh the preview without the crosshairs before zooming-in.
                    // Prevents zoomed-in crosshairs from breifly being displayed before
                    // the Layer Preview is refreshed post-zoom.
                    tsLayerImageShowCrosshairs.Checked = false;
                    ShowLayer();
                    tsLayerImageShowCrosshairs.Checked = true;
                }

                CenterLayerAt(GetTransposedIssueBounds(issue), AutoZoomLevel);
            }
        }

        /// <summary>
        /// Center the layer preview on the passed issue, or if appropriate for issue type,
        /// Zoom to fit the plate or print bounds.
        /// </summary>
        private void CenterAtIssue(LayerIssue issue)
        {
            if (issue.Type == LayerIssue.IssueType.TouchingBound || issue.Type == LayerIssue.IssueType.EmptyLayer || (issue.X == -1 && issue.Y == -1))
            {
                ZoomToFit();
            }
            if (issue.X >= 0 && issue.Y >= 0)
            {
                var issueBounds = GetTransposedIssueBounds(issue);
                pbLayer.CenterAt(issueBounds.X+issueBounds.Width/2, issueBounds.Y+issueBounds.Height/2);
            }
        }

        /// <summary>
        /// Given a zoom factor (200, 300, ..., 1200, 1600) this function finds the
        /// closest matching zoom factor from within the ZoomLevels constant array, and
        /// returns the reverse index (from the end of the array) of that zoom factor.
        /// This is used by auto-zoom to determine how many times zoom-out needs to be
        /// called from max zoom in order to arrive at the desired zoom.
        /// </summary>
        private int ConvZoomToBackIndex(int zoom)
        {
            return pbLayer.ZoomLevels.Count -
                pbLayer.ZoomLevels.IndexOf(pbLayer.ZoomLevels.FindNearest(zoom)) - 1;
        }

        private void ZoomToFit()
        {
            if (ReferenceEquals(SlicerFile, null)) return;

            // If ALT key is pressed when ZoomToFit is performed, the configured option for 
            // zoom to plate vs. zoom to print bounds will be inverted.
            if (Settings.Default.ZoomToFitPrintVolumeBounds ^ (ModifierKeys & Keys.Alt) != 0)
            {
                if (!tsLayerImageRotate.Checked)
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

        public IslandDetectionConfiguration GetIslandDetectionConfiguration()
        {
            return new IslandDetectionConfiguration
            {
                Enabled = tsIssuesRefreshIslands.Checked,
                AllowDiagonalBonds = Settings.Default.IslandAllowDiagonalBonds,
                BinaryThreshold = Settings.Default.IslandBinaryThreshold,
                RequiredAreaToProcessCheck = Settings.Default.IslandRequiredAreaToProcessCheck,
                RequiredPixelBrightnessToProcessCheck = Settings.Default.IslandRequiredPixelBrightnessToProcessCheck,
                RequiredPixelsToSupport = Settings.Default.IslandRequiredPixelsToSupport,
                RequiredPixelBrightnessToSupport = Settings.Default.IslandRequiredPixelBrightnessToSupport
            };
        }

        public ResinTrapDetectionConfiguration GetResinTrapDetectionConfiguration()
        {
            return new ResinTrapDetectionConfiguration
            {
                Enabled = tsIssuesRefreshResinTraps.Checked,
                BinaryThreshold = Settings.Default.ResinTrapBinaryThreshold,
                RequiredAreaToProcessCheck = Settings.Default.ResinTrapRequiredAreaToProcessCheck,
                RequiredBlackPixelsToDrain = Settings.Default.ResinTrapRequiredBlackPixelsToDrain,
                MaximumPixelBrightnessToDrain = Settings.Default.ResinTrapMaximumPixelBrightnessToDrain
            };
        }

        public void AddLog(LogItem log)
        {
            int count = log.Index = lvLog.GetItemCount()+1;
            lvLog.AddObject(log);
            lbLogOperations.Text = $"Operations: {count}";
        }
        
        public void AddLog(string description, decimal elapsedTime = 0)
        {
            int count = lvLog.GetItemCount()+1;
            lvLog.AddObject(new LogItem(count, description));
            lbLogOperations.Text = $"Operations: {count}";
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
            var result = MessageBox.Show(
                                "There are unsaved changes on image editor, do you want to apply modifications?",
                                "Unsaved changes on image editor", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (result == DialogResult.Cancel)
            {
                tsLayerImagePixelEdit.Checked = true;
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
                        SlicerFile.LayerManager.DrawModifications(PixelHistory, FrmLoading.RestartProgress());
                    }
                    catch (OperationCanceledException)
                    {

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"{ex.Message}", "Drawing was unsuccessful!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        Invoke((MethodInvoker)delegate {
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
                        if (SlicerFile.LayerCount < nextLayer &&
                            !whiteListLayers.Contains(nextLayer))
                        {
                            whiteListLayers.Add(nextLayer);
                        }
                    }

                    UpdateIslands(whiteListLayers);
                }
            }

            if (exitEditor)
            {
                tabControlLeft.SelectedTab = ControlLeftLastTab;
                tabControlLeft.TabPages.Remove(tabPagePixelEditor);
            }

            PixelHistory.Clear();
            RefreshPixelHistory();
            ShowLayer();

            menuFileSave.Enabled = menuFileSaveAs.Enabled = true;
        }

        private void UpdateIslands(List<uint> whiteListLayers)
        {
            if (whiteListLayers.Count == 0) return;
            var islandConfig = GetIslandDetectionConfiguration();
            var resinTrapConfig = new ResinTrapDetectionConfiguration { Enabled = false };
            islandConfig.Enabled = true;
            islandConfig.WhiteListLayers = whiteListLayers;
            
            if (ReferenceEquals(Issues, null))
            {
                ComputeIssues(islandConfig, resinTrapConfig);
            }
            else
            {
                DisableGUI();
                FrmLoading.SetDescription("Updating Issues");

                foreach (var layerIndex in islandConfig.WhiteListLayers)
                {
                    Issues.RemoveAll(issue => issue.LayerIndex == layerIndex && issue.Type == LayerIssue.IssueType.Island); // Remove all islands for update
                }

                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var issues = SlicerFile.LayerManager.GetAllIssues(islandConfig, resinTrapConfig, null,
                            FrmLoading.RestartProgress());

                        issues.RemoveAll(issue => issue.Type != LayerIssue.IssueType.Island); // Remove all non islands
                        Issues.AddRange(issues);
                        Issues = Issues.OrderBy(issue => issue.Type).ThenBy(issue => issue.LayerIndex).ThenBy(issue => issue.PixelsCount).ToList();
                    }
                    catch (OperationCanceledException)
                    {

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error while trying compute issues",
                            MessageBoxButtons.OK,
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
                });

                FrmLoading.ShowDialog();
                UpdateIssuesList();
            }
        }

        void UpdateIssuesList()
        {
            flvIssues.ClearObjects();
            if (!ReferenceEquals(Issues, null))
            {
                flvIssues.SetObjects(Issues);
            }
            UpdateIssuesInfo();
            ShowLayer();
        }
    }
}
