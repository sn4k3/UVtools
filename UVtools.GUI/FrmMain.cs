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
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;
using UVtools.Core.PixelEditor;
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
                {LayerManager.Mutate.Move, new Mutation(LayerManager.Mutate.Move, null,
                    "Moves the entire print volume around the plate.\n" +
                    "Note: Margins are in pixel values"
                )},
                {LayerManager.Mutate.Resize, new Mutation(LayerManager.Mutate.Resize, null,
                    "Resizes layer images in a X and/or Y factor, starting from 100% value\n" +
                    "NOTE 1: Build volume bounds are not validated after operation, please ensure scaling stays inside your limits.\n" +
                    "NOTE 2: X and Y are applied to original image, not to the rotated preview (If enabled)."
                )},
                {LayerManager.Mutate.Flip, new Mutation(LayerManager.Mutate.Flip, null,
                    "Flips layer images vertically and/or horizontally"
                )},
                {LayerManager.Mutate.Rotate, new Mutation(LayerManager.Mutate.Rotate, null,
                    "Rotate layer images in a certain degrees"
                )},
                {LayerManager.Mutate.Solidify, new Mutation(LayerManager.Mutate.Solidify, null,
                    "Solidifies the selected layers, closes all inner holes.\n" +
                    "Warning: All surrounded holes are filled, no exceptions! Make sure you don't require any of holes in layer path.",
                    Resources.mutation_solidify
                )},
                {LayerManager.Mutate.PixelDimming, new Mutation(LayerManager.Mutate.PixelDimming, "Pixel Dimming",
                    "Dims pixels in a chosen pattern over white pixels neighborhood. The selected pattern will be repeated over the image width and height as a mask. Benefits are:\n" +
                    "1) Reduce layer expansion in big masses\n" +
                    "2) Reduce cross layer exposure\n" +
                    "3) Extend pixels life\n" +
                    "NOTE: Run only this tool after all repairs and other transformations"
                )},
                {LayerManager.Mutate.Erode, new Mutation(LayerManager.Mutate.Erode, null,
                "The basic idea of erosion is just like soil erosion only, it erodes away the boundaries of foreground object (Always try to keep foreground in white). " +
                        "So what happens is that, all the pixels near boundary will be discarded depending upon the size of kernel. So the thickness or size of the foreground object decreases or simply white region decreases in the image. It is useful for removing small white noises, detach two connected objects, etc.",
                        Properties.Resources.mutation_erosion
                )},
                {LayerManager.Mutate.Dilate, new Mutation(LayerManager.Mutate.Dilate, null,
                    "It is just opposite of erosion. Here, a pixel element is '1' if at least one pixel under the kernel is '1'. So it increases the white region in the image or size of foreground object increases. Normally, in cases like noise removal, erosion is followed by dilation. Because, erosion removes white noises, but it also shrinks our object. So we dilate it. Since noise is gone, they won't come back, but our object area increases. It is also useful in joining broken parts of an object.",
                    Resources.mutation_dilation
                )},
                {LayerManager.Mutate.Opening, new Mutation(LayerManager.Mutate.Opening, "Noise Removal",
                    "Noise Removal/Opening is just another name of erosion followed by dilation. It is useful in removing noise.",
                    Properties.Resources.mutation_opening
                )},
                {LayerManager.Mutate.Closing, new Mutation(LayerManager.Mutate.Closing, "Gap Closing",
                    "Gap Closing is reverse of Opening, Dilation followed by Erosion. It is useful in closing small holes inside the foreground objects, or small black points on the object.",
                    Properties.Resources.mutation_closing
                )},
                {LayerManager.Mutate.Gradient, new Mutation(LayerManager.Mutate.Gradient, null,
                    "It's the difference between dilation and erosion of an image.",
                    Properties.Resources.mutation_gradient
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
                {LayerManager.Mutate.PyrDownUp, new Mutation(LayerManager.Mutate.PyrDownUp, null,
                    "Performs downsampling step of Gaussian pyramid decomposition.\n" +
                    "First it convolves image with the specified filter and then downsamples the image by rejecting even rows and columns.\n" +
                    "After performs up-sampling step of Gaussian pyramid decomposition\n"
                )},
                {LayerManager.Mutate.SmoothMedian, new Mutation(LayerManager.Mutate.SmoothMedian, "Smooth Median",
                    "Each pixel becomes the median of its surrounding pixels. Also a good way to remove noise.\n" +
                    "Note: Iterations must be a odd number."
                )},
                {LayerManager.Mutate.SmoothGaussian, new Mutation(LayerManager.Mutate.SmoothGaussian,  "Smooth Gaussian",
                    "Each pixel is a sum of fractions of each pixel in its neighborhood\n" +
                    "Very fast, but does not preserve sharp edges well.\n" +
                    "Note: Iterations must be a odd number."
                )},
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

        public Dictionary<uint, List<LayerIssue>> Issues { get; set; }

        public uint TotalIssues { get; set; }

        public bool IsChagingLayer { get; set; }

        public PixelHistory PixelHistory { get; } = new PixelHistory();

        public uint SavesCount { get; set; } = 0;

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
            tsIsuesRefreshIslands.Checked = Settings.Default.ComputeIslands;
            tsIsuesRefreshResinTraps.Checked = Settings.Default.ComputeResinTraps;
            tsLayerImageLayerOutlinePrintVolumeBounds.Checked = Settings.Default.OutlinePrintVolumeBounds;
            tsLayerImageLayerOutlineLayerBounds.Checked = Settings.Default.OutlineLayerBounds;
            tsLayerImageLayerOutlineHollowAreas.Checked = Settings.Default.OutlineHollowAreas;

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
                    ToolTipText = Mutations[mutate].Description, Tag = mutate, AutoToolTip = true, Image = Properties.Resources.filter_filled_16x16
                };
                item.Click += EventClick;
                menuMutate.DropDownItems.Add(item);
            }

            foreach (LayerIssue.IssueType issueType in (LayerIssue.IssueType[]) Enum.GetValues(
                typeof(LayerIssue.IssueType)))
            {
                var group = new ListViewGroup(issueType.ToString(), $"{issueType}s"){HeaderAlignment = HorizontalAlignment.Center};
                lvIssues.Groups.Add(group);
            }

            foreach (PixelDrawing.BrushShapeType brushShape in (PixelDrawing.BrushShapeType[])Enum.GetValues(
                typeof(PixelDrawing.BrushShapeType)))
            {
                cbPixelEditorBrushShape.Items.Add(brushShape);
            }

            cbPixelEditorBrushShape.SelectedIndex = 0;

            tbLayer.MouseWheel += TbLayerOnMouseWheel;

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
            ProcessFile(Program.Args);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (ReferenceEquals(SlicerFile, null) || IsChagingLayer)
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
                    bool repairIslands;
                    bool repairResinTraps;
                    using (var frmRepairLayers = new FrmRepairLayers(2))
                    {
                        if (frmRepairLayers.ShowDialog() != DialogResult.OK) return;

                        layerStart = frmRepairLayers.LayerRangeStart;
                        layerEnd = frmRepairLayers.LayerRangeEnd;
                        closingIterations = frmRepairLayers.ClosingIterations;
                        openingIterations = frmRepairLayers.OpeningIterations;
                        repairIslands = frmRepairLayers.RepairIslands;
                        repairResinTraps = frmRepairLayers.RepairResinTraps;
                    }

                    if (repairResinTraps && ReferenceEquals(Issues, null))
                    {
                        ComputeIssues(new IslandDetectionConfiguration
                            {Enabled = false}); // Ignore islands as we dont require it
                    }

                    DisableGUI();
                    FrmLoading.SetDescription("Repairing Layers and Issues");

                    var task = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            SlicerFile.LayerManager.RepairLayers(layerStart, layerEnd, closingIterations,
                                openingIterations, repairIslands, repairResinTraps, Issues,
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


                    ShowLayer();

                    ComputeIssues(GetIslandDetectionConfiguration(), GetResinTrapDetectionConfiguration());

                    menuFileSave.Enabled =
                        menuFileSaveAs.Enabled = true;
                    return;
                }

                if (ReferenceEquals(sender, menuToolsPattern))
                {
                    using (var frm = new FrmToolPattern(SlicerFile.LayerManager.BoundingRectangle,
                        (uint) ActualLayerImage.Width, (uint) ActualLayerImage.Height))
                    {
                        if (frm.ShowDialog() != DialogResult.OK) return;

                        DisableGUI();
                        FrmLoading.SetDescription("Pattern");

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
            if (ReferenceEquals(sender, tsPropertiesButtonSave))
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

            /************************
             *      GCode Menu      *
             ***********************/
            if (ReferenceEquals(sender, tsGCodeButtonSave))
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
                                $"GCode save was successful, do you want open the file with default editor?.\nPress 'Yes' if you want open the target file, otherwise select 'No' to continue.",
                                "GCode save completed", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
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

            /************************
             *     Issues Menu    *
             ***********************/
            if (ReferenceEquals(sender, tsIssuePrevious))
            {
                if (!tsIssuePrevious.Enabled) return;
                int index = Convert.ToInt32(tsIssueCount.Tag);
                lvIssues.SelectedItems.Clear();
                lvIssues.Items[--index].Selected = true;
                EventItemActivate(lvIssues, null);
                return;
            }

            if (ReferenceEquals(sender, tsIssueNext))
            {
                if (!tsIssueNext.Enabled) return;
                int index = Convert.ToInt32(tsIssueCount.Tag);
                lvIssues.SelectedItems.Clear();
                lvIssues.Items[++index].Selected = true;
                EventItemActivate(lvIssues, null);
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

                foreach (ListViewItem item in lvIssues.SelectedItems)
                {
                    if (!(item.Tag is LayerIssue issue)) continue;
                    if (!issue.HaveValidPoint) continue;

                    if (!processIssues.TryGetValue(issue.Layer.Index, out var issueList))
                    {
                        issueList = new List<LayerIssue>();
                        processIssues.Add(issue.Layer.Index, issueList);
                    }

                    issueList.Add(issue);
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

                // Update GUI
                lvIssues.BeginUpdate();
                foreach (ListViewItem item in lvIssues.SelectedItems)
                {
                    if (!(item.Tag is LayerIssue issue)) continue;
                    if (!issue.HaveValidPoint) continue;
                    if (issue.Type == LayerIssue.IssueType.TouchingBound) continue;

                    Issues[issue.Layer.Index].Remove(issue);
                    item.Remove();
                    TotalIssues--;
                }

                lvIssues.EndUpdate();

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

            if (ReferenceEquals(sender, tsIsuesRefresh))
            {
                if (MessageBox.Show("Are you sure you want to compute issues?", "Issues Compute",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

                ComputeIssues(GetIslandDetectionConfiguration(), GetResinTrapDetectionConfiguration());

                return;
            }

            if (ReferenceEquals(sender, btnPixelHistoryRemove))
            {
                if (!btnPixelHistoryRemove.Enabled) return;
                lvPixelHistory.BeginUpdate();
                foreach (ListViewItem item in lvPixelHistory.SelectedItems)
                {
                    PixelOperation operation = item.Tag as PixelOperation;
                    item.Remove();
                    PixelHistory.Items.Remove(operation);
                }
                lvPixelHistory.EndUpdate();
                lbPixelHistoryOperations.Text = $"Operations: {PixelHistory.Count}";

                ShowLayer();
            }
        

            /************************
             *      Layer Menu      *
             ***********************/
            if (ReferenceEquals(sender, tsLayerImageRotate) || 
                ReferenceEquals(sender, tsLayerImageLayerDifference) ||
                ReferenceEquals(sender, tsLayerImageHighlightIssues) ||
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
                    if (PixelHistory.Count > 0)
                    {
                        var result =
                            MessageBox.Show(
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
                            FrmLoading.SetDescription($"Drawing pixels");

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
                        }

                        lvPixelHistory.Items.Clear();
                        PixelHistory.Clear();
                        ShowLayer();
                    }

                    tabControlLeft.TabPages.Remove(tabPagePixelEditor);
                }
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
            tsLayerImageZoom.Text = $"Zoom: {e.NewZoom}% ({e.NewZoom/100f}x)";
        }

        private void pbLayer_MouseUp(object sender, MouseEventArgs e)
        {
            if (!tsLayerImagePixelEdit.Checked || (e.Button & MouseButtons.Right) == 0) return;
            if (!pbLayer.IsPointInImage(e.Location)) return;
            var location = pbLayer.PointToImage(e.Location);
            _lastPixelMouseLocation = Point.Empty;

            DrawPixel(ModifierKeys != Keys.Shift, location);

            //SlicerFile[ActualLayer].LayerMat = ActualLayerImage;
            RefreshPixelHistory();
        }

        private Point _lastPixelMouseLocation = Point.Empty;
        private void pbLayer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!pbLayer.IsPointInImage(e.Location)) return;
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

            if (tabControlPixelEditor.SelectedIndex != (int) PixelOperation.PixelOperationType.Drawing) return;
                if (!tsLayerImagePixelEdit.Checked || (e.Button & MouseButtons.Right) == 0) return;
            if (!pbLayer.IsPointInImage(e.Location)) return;

            if (ModifierKeys == Keys.Shift)
            {
                DrawPixel(false, location);
                return;
            }

            DrawPixel(true, location);
        }

        private void EventItemActivate(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, lvIssues))
            {
                if (lvIssues.SelectedItems.Count == 0) return;
                var item = lvIssues.SelectedItems[0];

                if (!(item.Tag is LayerIssue issue)) return;
                if (issue.Layer.Index != ActualLayer)
                {
                    ShowLayer(issue.Layer.Index);
                }

                if (issue.Type == LayerIssue.IssueType.TouchingBound)
                {
                    ZoomToFit();
                }
                else if (issue.X >= 0 && issue.Y >= 0)
                {
                    if (issue.BoundingRectangle.IsEmpty || issue.Size == 1)
                    {
                        int x = issue.X;
                        int y = issue.Y;

                        if (tsLayerImageRotate.Checked)
                        {
                            x = ActualLayerImage.Height - 1 - issue.Y;
                            y = issue.X;
                        }

                        pbLayer.ZoomToRegion(x, y, 5, 5);
                        pbLayer.ZoomOut(true);
                    }
                    else
                    {
                        pbLayer.ZoomToRegion(
                            tsLayerImageRotate.Checked ? ActualLayerImage.Height - 1 - issue.BoundingRectangle.Bottom : issue.BoundingRectangle.X,
                            tsLayerImageRotate.Checked ? issue.BoundingRectangle.X : issue.BoundingRectangle.Y,
                            tsLayerImageRotate.Checked ? issue.BoundingRectangle.Height : issue.BoundingRectangle.Width,
                            tsLayerImageRotate.Checked ? issue.BoundingRectangle.Width : issue.BoundingRectangle.Height
                            );
                        pbLayer.ZoomOut(true);
                    }
                }

                tsIssueCount.Tag = lvIssues.SelectedIndices[0];
                UpdateIssuesInfo();
                return;
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
            TotalIssues = 0;
            lvIssues.BeginUpdate();
            lvIssues.Items.Clear();
            lvIssues.EndUpdate();
            UpdateIssuesInfo();

            PixelHistory.Clear();
            RefreshPixelHistory();

            lbMaxLayer.Text = 
            lbActualLayer.Text = 
            lbInitialLayer.Text = "???";
            lvProperties.BeginUpdate();
            lvProperties.Items.Clear();
            lvProperties.Groups.Clear();
            lvProperties.EndUpdate();
            pbLayers.Value = 0;
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
            foreach (ToolStripItem item in tsProperties.Items)
            {
                item.Enabled = false;
            }
            foreach (ToolStripItem item in tsIssues.Items)
            {
                item.Enabled = false;
            }

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
            pbLayers.Enabled = 
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
            SlicerFile = FileFormat.FindByExtension(fileName, true, true);
            if (ReferenceEquals(SlicerFile, null)) return;

            DisableGUI();
            FrmLoading.Description = $"Decoding {Path.GetFileName(fileName)}";
            
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

            lbMaxLayer.Text = $"{SlicerFile.TotalHeight}mm\n{SlicerFile.LayerCount-1}";
            lbInitialLayer.Text = $"{SlicerFile.LayerHeight}mm\n0";


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
            foreach (ToolStripItem item in tsProperties.Items)
            {
                item.Enabled = true;
            }
            tsPropertiesLabelCount.Text = $"Properties: {lvProperties.Items.Count}";
            tsPropertiesLabelGroups.Text = $"Groups: {lvProperties.Groups.Count}";

            menuFileReload.Enabled =
            menuFileClose.Enabled =
            menuFileExtract.Enabled =
                
            tbLayer.Enabled =
            pbLayers.Enabled =
            menuEdit.Enabled = 
            menuMutate.Enabled =
            menuTools.Enabled =

            tsIssuesRepair.Enabled =
            tsIsuesRefresh.Enabled =

                btnFindLayer.Enabled =
                true;

            if (!ReferenceEquals(SlicerFile.GCode, null))
            {
                tabControlLeft.TabPages.Add(tabPageGCode);
            }
            tabControlLeft.TabPages.Add(tabPageIssues);


            tabControlLeft.SelectedIndex = 0;
            tsLayerResolution.Text = $"{{Width={SlicerFile.ResolutionX}, Height={SlicerFile.ResolutionY}}}";

            tbLayer.Maximum = (int)SlicerFile.LayerCount - 1;
            ShowLayer(actualLayer);
            


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

        private void UpdateTitle()
        {
            Text = ReferenceEquals(SlicerFile, null) ?
                $"{FrmAbout.AssemblyTitle}   Version: {FrmAbout.AssemblyVersion}" : 
                $"{FrmAbout.AssemblyTitle}   File: {Path.GetFileName(SlicerFile.FileFullPath)} ({FrmLoading.StopWatch.ElapsedMilliseconds}ms)   Version: {FrmAbout.AssemblyVersion}";

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

            lvProperties.BeginUpdate();
            lvProperties.Items.Clear();

            if (!ReferenceEquals(SlicerFile.Configs, null))
            {
                foreach (object config in SlicerFile.Configs)
                {
                    ListViewGroup group = new ListViewGroup(config.GetType().Name);
                    lvProperties.Groups.Add(group);
                    foreach (PropertyInfo propertyInfo in config.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if(propertyInfo.Name.Equals("Item")) continue;
                        ListViewItem item = new ListViewItem(propertyInfo.Name, group);
                        var value = propertyInfo.GetValue(config);
                        if (!ReferenceEquals(value, null))
                        {
                            if (value is IList list)
                            {
                                item.SubItems.Add(list.Count.ToString());
                            }
                            else
                            {
                                item.SubItems.Add(value.ToString());
                            }

                        }

                        lvProperties.Items.Add(item);
                    }
                }
            }

            lvProperties.EndUpdate();

            tsPropertiesLabelCount.Text = $"Properties: {lvProperties.Items.Count}";
            tsPropertiesLabelGroups.Text = $"Groups: {lvProperties.Groups.Count}";

            if (!ReferenceEquals(SlicerFile.GCode, null))
            {
                tbGCode.Text = SlicerFile.GCode.ToString();
                tsGCodeLabelLines.Text = $"Lines: {tbGCode.Lines.Length}";
                tsGcodeLabelChars.Text = $"Chars: {tbGCode.Text.Length}";
            }

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

        /// <summary>
        /// Reshow current layer
        /// </summary>
        void ShowLayer() => ShowLayer(ActualLayer);

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
            if (ReferenceEquals(SlicerFile, null)) return;

            //int layerOnSlider = (int)(SlicerFile.LayerCount - layerNum - 1);
            if (tbLayer.Value != layerNum)
            {
                tbLayer.Value = (int) layerNum;
                return;
            }

            if (IsChagingLayer) return ;
            IsChagingLayer = true;

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

                if (tsLayerImageHighlightIssues.Checked &&
                    !ReferenceEquals(Issues, null) && 
                    Issues.TryGetValue(ActualLayer, out var issues))
                {
                    foreach (var issue in issues)
                    {
                        if (ReferenceEquals(issue, null)) continue; // Removed issue
                        if(!issue.HaveValidPoint) continue;
                        Color color = Settings.Default.ResinTrapColor;

                        if (issue.Type == LayerIssue.IssueType.ResinTrap)
                        {
                            using (var vec = new VectorOfVectorOfPoint(new VectorOfPoint(issue.Pixels)))
                            {
                                CvInvoke.DrawContours(ActualLayerImageBgr, vec, -1,
                                    new MCvScalar(color.B, color.G, color.R), -1);
                                //CvInvoke.DrawContours(ActualLayerImageBgr, new VectorOfVectorOfPoint(new VectorOfPoint(issue.Pixels)), -1, new MCvScalar(0, 0, 255), 1);
                            }

                            continue;
                        }

                        foreach (var pixel in issue)
                        {
                            int pixelPos = ActualLayerImage.GetPixelPos(pixel);
                            byte brightness = imageSpan[pixelPos];
                            if (brightness == 0) continue;

                            int pixelBgrPos = pixelPos*ActualLayerImageBgr.NumberOfChannels;

                            
                            switch (issue.Type)
                            {
                                case LayerIssue.IssueType.Island:
                                    color = Settings.Default.IslandColor;
                                    break;
                                case LayerIssue.IssueType.TouchingBound:
                                    color = Settings.Default.TouchingBoundsColor;
                                    break;
                            }


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
                    using (Mat grayscale = new Mat())
                    {
                        //CvInvoke.Threshold(ActualLayerImage, grayscale, 1, 255, ThresholdType.Binary);
                        using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                        {
                            using (Mat hierarchy = new Mat())
                            {
                                CvInvoke.FindContours(ActualLayerImage, contours, hierarchy, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);

                                /*
                                 * hierarchy[i][0]: the index of the next contour of the same level
                                 * hierarchy[i][1]: the index of the previous contour of the same level
                                 * hierarchy[i][2]: the index of the first child
                                 * hierarchy[i][3]: the index of the parent
                                 */
                                var arr = hierarchy.GetData();
                                for (int i = 0; i < contours.Size; i++)
                                {
                                    if ((int) arr.GetValue(0, i, 2) == -1 && (int) arr.GetValue(0, i, 3) != -1)
                                    {
                                        //var r = CvInvoke.BoundingRectangle(contours[i]);
                                        //CvInvoke.Rectangle(ActualLayerImageBgr, r, new MCvScalar(0, 0, 255), 2);
                                        CvInvoke.DrawContours(ActualLayerImageBgr, contours, i,
                                            new MCvScalar(Settings.Default.OutlineHollowAreasColor.B,
                                                Settings.Default.OutlineHollowAreasColor.G, Settings.Default.OutlineHollowAreasColor.R),
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
                        }
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
                            ? Settings.Default.PixelEditorAddPixelColor
                            : Settings.Default.PixelEditorRemovePixelColor;


                        if (operationDrawing.Rectangle.Size.GetArea() == 1)
                        {
                            ActualLayerImageBgr.SetByte(operation.Location.X, operation.Location.Y, new []{ color.B, color.G, color.R });
                            continue;
                        }

                        switch (operationDrawing.BrushShape)
                        {
                            case PixelDrawing.BrushShapeType.Rectangle:
                                CvInvoke.Rectangle(ActualLayerImageBgr, operationDrawing.Rectangle, new MCvScalar(color.B, color.G, color.R), -1);
                                break;
                            case PixelDrawing.BrushShapeType.Circle:
                                CvInvoke.Circle(ActualLayerImageBgr, operation.Location, operationDrawing.BrushSize / 2, new MCvScalar(color.B, color.G, color.R), -1);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (operation.OperationType == PixelOperation.PixelOperationType.Supports)
                    {
                        var operationSupport = (PixelSupport)operation;
                        var color = Settings.Default.PixelEditorSupportColor;
                        CvInvoke.Circle(ActualLayerImageBgr, operation.Location, operationSupport.TipDiameter / 2, new MCvScalar(color.B, color.G, color.R), -1);
                    }
                    else if (operation.OperationType == PixelOperation.PixelOperationType.DrainHole)
                    {
                        var operationDrainHole = (PixelDrainHole)operation;
                        var color = Settings.Default.PixelEditorDrainHoleColor;
                        CvInvoke.Circle(ActualLayerImageBgr, operation.Location, operationDrainHole.Diameter / 2, new MCvScalar(color.B, color.G, color.R), -1);
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


                watch.Stop();
                tsLayerPreviewTime.Text = $"{watch.ElapsedMilliseconds}ms";
                //lbLayers.Text = $"{SlicerFile.GetHeightFromLayer(layerNum)} / {SlicerFile.TotalHeight}mm\n{layerNum} / {SlicerFile.LayerCount-1}\n{percent}%";
                lbActualLayer.Text = $"{layer.PositionZ}mm\n{ActualLayer}\n{percent}%";
                lbActualLayer.Location = new Point(lbActualLayer.Location.X, 
                    Math.Max(1, 
                        Math.Min(tbLayer.Height- lbActualLayer.Height, 
                            (int)(tbLayer.Height - tbLayer.Value * ((float)tbLayer.Height / tbLayer.Maximum)) - lbActualLayer.Height/2)
                ));

                pbLayers.Value = percent;
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

            IsChagingLayer = false;
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
            if (ReferenceEquals(sender, lvIssues))
            {
                tsIssueRemove.Enabled = lvIssues.SelectedIndices.Count > 0;
                return;
            }

            if (ReferenceEquals(sender, lvPixelHistory))
            {
                btnPixelHistoryRemove.Enabled = lvPixelHistory.SelectedIndices.Count > 0;
                return;
            }

            if (ReferenceEquals(sender, tabControlLeft))
            {
                if(ReferenceEquals(tabControlLeft.SelectedTab, tabPageIssues))
                {
                    if (!ReferenceEquals(tabPageIssues.Tag, null) || !Settings.Default.AutoComputeIssuesClickOnTab) return;
                    ComputeIssues(GetIslandDetectionConfiguration(), GetResinTrapDetectionConfiguration());
                }
                return;
            }

            if (ReferenceEquals(sender, cbPixelEditorBrushShape))
            {
                if (cbPixelEditorBrushShape.SelectedIndex == (int) PixelDrawing.BrushShapeType.Rectangle)
                {
                    nmPixelEditorBrushSize.Minimum = PixelDrawing.MinRectangleBrush;
                    return;
                }
                if (cbPixelEditorBrushShape.SelectedIndex == (int)PixelDrawing.BrushShapeType.Circle)
                {
                    nmPixelEditorBrushSize.Minimum = PixelDrawing.MinCircleBrush;
                    return;
                }
                return;
            }
        }

        private void EventKeyUp(object sender, KeyEventArgs e)
        {
            if (ReferenceEquals(sender, lvProperties))
            {
                if (e.KeyCode == Keys.Escape)
                {
                    foreach (ListViewItem item in lvProperties.Items)
                    {
                        item.Selected = false;
                    }
                    e.Handled = true;
                    return;
                }

                if (e.Control && e.KeyCode == Keys.A)
                {
                    foreach (ListViewItem item in lvProperties.Items)
                    {
                        item.Selected = true;
                    }
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.Multiply)
                {
                    foreach (ListViewItem item in lvProperties.Items)
                    {
                        item.Selected = !item.Selected;
                    }
                    e.Handled = true;
                    return;
                }

                if (e.Control && e.KeyCode == Keys.C)
                {
                    StringBuilder clip = new StringBuilder();
                    foreach (ListViewItem item in lvProperties.Items)
                    {
                        if(!item.Selected) continue;
                        if (clip.Length > 0) clip.AppendLine();
                        clip.Append($"{item.Text}: {item.SubItems[1].Text}");
                    }

                    if (clip.Length > 0)
                    {
                        Clipboard.SetText(clip.ToString());
                    }
                    e.Handled = true;
                    return;
                }
                return;
            }

            if (ReferenceEquals(sender, lvIssues))
            {
                if (e.KeyCode == Keys.Escape)
                {
                    lvIssues.SelectedItems.Clear();
                    e.Handled = true;
                    return;
                }

                if (e.Control && e.KeyCode == Keys.A)
                {
                    foreach (ListViewItem item in lvIssues.Items)
                    {
                        item.Selected = true;
                    }
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.Multiply)
                {
                    foreach (ListViewItem item in lvIssues.Items)
                    {
                        item.Selected = !item.Selected;
                    }
                    e.Handled = true;
                    return;
                }

                if (e.Control && e.KeyCode == Keys.C)
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
                }

                if (e.KeyCode == Keys.Delete)
                {
                    tsIssueRemove.PerformClick();
                    e.Handled = true;
                    return;
                }
                return;
            }

            if (ReferenceEquals(sender, lvPixelHistory))
            {
                if (e.KeyCode == Keys.Escape)
                {
                    lvPixelHistory.SelectedItems.Clear();
                    e.Handled = true;
                    return;
                }

                if (e.Control && e.KeyCode == Keys.A)
                {
                    foreach (ListViewItem item in lvPixelHistory.Items)
                    {
                        item.Selected = true;
                    }
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.Multiply)
                {
                    foreach (ListViewItem item in lvPixelHistory.Items)
                    {
                        item.Selected = !item.Selected;
                    }
                    e.Handled = true;
                    return;
                }

                if (e.Control && e.KeyCode == Keys.C)
                {
                    StringBuilder clip = new StringBuilder();
                    foreach (ListViewItem item in lvPixelHistory.Items)
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
                }

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
        }

        private void EventMouseUp(object sender, MouseEventArgs e)
        {
            if (ReferenceEquals(sender, btnNextLayer) || ReferenceEquals(sender, btnPreviousLayer))
            {
                layerScrollTimer.Stop();
                return;
            }
        }

        private void EventTimerTick(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, layerScrollTimer))
            {
                ShowLayer((bool)layerScrollTimer.Tag);
                return;
            }
        }

        private void EventMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (ReferenceEquals(sender, pbLayer))
            {
                if ((e.Button & MouseButtons.Left) != 0)
                {
                    ZoomToFit();
                    return;
                }
                if ((e.Button & MouseButtons.Middle) != 0)
                {
                    pbLayer.ZoomToFit();
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

            if (tabControlPixelEditor.SelectedIndex == (int) PixelOperation.PixelOperationType.Drawing)
            {
                PixelDrawing.BrushShapeType shapeType =
                    (PixelDrawing.BrushShapeType) cbPixelEditorBrushShape.SelectedIndex;
                operation = new PixelDrawing(ActualLayer, new Point(x, y),
                    shapeType, (ushort) nmPixelEditorBrushSize.Value, isAdd);

                if (PixelHistory.Contains(operation)) return;

                using (var gfx = Graphics.FromImage(bmp))
                {
                    int shiftPos = (int)nmPixelEditorBrushSize.Value / 2;
                    gfx.SmoothingMode = SmoothingMode.HighSpeed;

                    var color = isAdd ? Settings.Default.PixelEditorAddPixelColor : Settings.Default.PixelEditorRemovePixelColor;
                    SolidBrush brush = new SolidBrush(color);

                    switch (shapeType)
                    {
                        case PixelDrawing.BrushShapeType.Rectangle:
                            gfx.FillRectangle(brush, Math.Max(0, location.X - shiftPos), Math.Max(0, location.Y - shiftPos), (int)nmPixelEditorBrushSize.Value, (int)nmPixelEditorBrushSize.Value);
                            break;
                        case PixelDrawing.BrushShapeType.Circle:
                            gfx.FillEllipse(brush, Math.Max(0, location.X - shiftPos), Math.Max(0, location.Y - shiftPos), (int)nmPixelEditorBrushSize.Value, (int)nmPixelEditorBrushSize.Value);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                }
            }
            else if (tabControlPixelEditor.SelectedIndex == (int) PixelOperation.PixelOperationType.Supports)
            {
                if (ActualLayer == 0) return;
                operation = new PixelSupport(ActualLayer, new Point(x, y),
                    (byte) nmPixelEditorSupportsTipDiameter.Value, (byte)nmPixelEditorSupportsPillarDiameter.Value, (byte)nmPixelEditorSupportsBaseDiameter.Value);

                if (PixelHistory.Contains(operation)) return;

                SolidBrush brush = new SolidBrush(Settings.Default.PixelEditorSupportColor);
                using (var gfx = Graphics.FromImage(bmp))
                {
                    int shiftPos = (int)nmPixelEditorSupportsTipDiameter.Value / 2;
                    gfx.SmoothingMode = SmoothingMode.HighSpeed;
                    gfx.FillEllipse(brush, Math.Max(0, location.X - shiftPos), Math.Max(0, location.Y - shiftPos), (int)nmPixelEditorSupportsTipDiameter.Value, (int)nmPixelEditorSupportsTipDiameter.Value);
                }
            }
            else if (tabControlPixelEditor.SelectedIndex == (int)PixelOperation.PixelOperationType.DrainHole)
            {
                if (ActualLayer == 0) return;
                operation = new PixelDrainHole(ActualLayer, new Point(x, y), (byte)nmPixelEditorDrainHoleDiameter.Value);

                if (PixelHistory.Contains(operation)) return;

                SolidBrush brush = new SolidBrush(Settings.Default.PixelEditorDrainHoleColor);
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

            PixelHistory.Add(operation);
            
            pbLayer.Invalidate();
            //pbLayer.Update();
            //pbLayer.Refresh();
            menuFileSave.Enabled = menuFileSaveAs.Enabled = true;
            //sw.Stop();
            //Debug.WriteLine(sw.ElapsedMilliseconds);
        }

        public void RefreshPixelHistory()
        {
            lbPixelHistoryOperations.Text = $"Operations: {PixelHistory.Count}";
            lvPixelHistory.BeginUpdate();
            lvPixelHistory.Items.Clear();
            for (var i = PixelHistory.Count-1; i >= 0; i--)
            {
                var operation = PixelHistory[i];
                var item = new ListViewItem
                {
                    Text = i.ToString(),
                    Tag = operation
                };
                item.SubItems.Add(operation.OperationType.ToString());
                item.SubItems.Add(operation.LayerIndex.ToString());
                item.SubItems.Add(operation.Location.ToString());
                //item.SubItems.Add(operation.BrushSize.ToString());

                lvPixelHistory.Items.Add(item);
            }

            lvPixelHistory.EndUpdate();
            btnPixelHistoryRemove.Enabled = false;
        }

        

        public void MutateLayers(LayerManager.Mutate mutator)
        {
            uint layerStart;
            uint layerEnd;
            uint iterationsStart = 0;
            uint iterationsEnd = 0;
            bool fade = false;

            OperationMove operationMove = null;

            double x = 0;
            double y = 0;

            Matrix<byte> evenPattern = null;
            Matrix<byte> oddPattern = null;

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
                default:
                    using (FrmMutation inputBox = new FrmMutation(Mutations[mutator]))
                    {
                        if (inputBox.ShowDialog() != DialogResult.OK) return;
                        iterationsStart = inputBox.Iterations;
                        if (iterationsStart == 0) return;
                        layerStart = inputBox.LayerRangeStart;
                        layerEnd = inputBox.LayerRangeEnd;
                        iterationsEnd = inputBox.IterationsEnd;
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
                        case LayerManager.Mutate.PixelDimming:
                            SlicerFile.LayerManager.MutatePixelDimming(layerStart, layerEnd, evenPattern, oddPattern, (ushort) iterationsStart, progress);
                            break;
                        case LayerManager.Mutate.Erode:
                            SlicerFile.LayerManager.MutateErode(layerStart, layerEnd, (int) iterationsStart, (int) iterationsEnd, fade, progress);
                            break;
                        case LayerManager.Mutate.Dilate:
                            SlicerFile.LayerManager.MutateDilate(layerStart, layerEnd, (int)iterationsStart, (int)iterationsEnd, fade, progress);
                            break;
                        case LayerManager.Mutate.Opening:
                            SlicerFile.LayerManager.MutateOpen(layerStart, layerEnd, (int)iterationsStart, (int)iterationsEnd, fade, progress);
                            break;
                        case LayerManager.Mutate.Closing:
                            SlicerFile.LayerManager.MutateClose(layerStart, layerEnd, (int)iterationsStart, (int)iterationsEnd, fade, progress);
                            break;
                        case LayerManager.Mutate.Gradient:
                            SlicerFile.LayerManager.MutateGradient(layerStart, layerEnd, (int)iterationsStart, (int)iterationsEnd, fade, progress);
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
                        case LayerManager.Mutate.PyrDownUp:
                            SlicerFile.LayerManager.MutatePyrDownUp(layerStart, layerEnd, BorderType.Default, progress);
                            break;
                        case LayerManager.Mutate.SmoothMedian:
                            SlicerFile.LayerManager.MutateMedianBlur(layerStart, layerEnd, (int)iterationsStart, progress);
                            break;
                        case LayerManager.Mutate.SmoothGaussian:
                            SlicerFile.LayerManager.MutateGaussianBlur(layerStart, layerEnd, new Size((int) iterationsStart, (int) iterationsStart), 0,0, BorderType.Default, progress);
                            break;
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
                tsIssueCount.Enabled = true;
                tsIssueCount.Text = $"{currentIssueSelected+1}/{TotalIssues}";

                tsIssuePrevious.Enabled = currentIssueSelected > 0;
                tsIssueNext.Enabled = currentIssueSelected+1 < TotalIssues;
            }
            
        }

        private void ComputeIssues(IslandDetectionConfiguration islandConfig = null, ResinTrapDetectionConfiguration resinTrapConfig = null)
        {
            tabPageIssues.Tag = true;
            TotalIssues = 0;
            lvIssues.BeginUpdate();
            lvIssues.Items.Clear();
            lvIssues.EndUpdate();
            UpdateIssuesInfo();

            DisableGUI();
            FrmLoading.SetDescription("Computing Issues");

            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    var issues = SlicerFile.LayerManager.GetAllIssues(islandConfig, resinTrapConfig, FrmLoading.RestartProgress());
                    Issues = new Dictionary<uint, List<LayerIssue>>();

                    for (uint i = 0; i < SlicerFile.LayerCount; i++)
                    {
                        if (issues.TryGetValue(i, out var list))
                        {
                            Issues.Add(i, list);
                        }
                    }
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

            lvIssues.BeginUpdate();
            uint count = 0;

            try
            {
                if (!ReferenceEquals(Issues, null))
                {
                    foreach (var kv in Issues)
                    {
                        foreach (var issue in kv.Value)
                        {
                            TotalIssues++;
                            count++;
                            ListViewItem item = new ListViewItem(lvIssues.Groups[(int) issue.Type])
                                {Text = issue.Type.ToString()};
                            item.SubItems.Add(count.ToString());
                            item.SubItems.Add(kv.Key.ToString());
                            item.SubItems.Add($"{issue.X}, {issue.Y}");
                            item.SubItems.Add(issue.Size.ToString());
                            item.Tag = issue;
                            lvIssues.Items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error while trying compute issues", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                throw;
            }
            
            lvIssues.EndUpdate();
            UpdateIssuesInfo();
            ShowLayer();
        }

        private void ZoomToFit()
        {
            if (ReferenceEquals(SlicerFile, null)) return;
            if (Settings.Default.ZoomToFitPrintVolumeBounds)
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
                Enabled = tsIsuesRefreshIslands.Checked,
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
                Enabled = tsIsuesRefreshResinTraps.Checked,
                BinaryThreshold = Settings.Default.ResinTrapBinaryThreshold,
                RequiredAreaToProcessCheck = Settings.Default.ResinTrapRequiredAreaToProcessCheck,
                RequiredBlackPixelsToDrain = Settings.Default.ResinTrapRequiredBlackPixelsToDrain,
                MaximumPixelBrightnessToDrain = Settings.Default.ResinTrapMaximumPixelBrightnessToDrain
            };
        }
    }
}
