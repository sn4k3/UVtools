/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using UVtools.GUI.Extensions;
using UVtools.GUI.Forms;
using UVtools.Parser;
using Color = System.Drawing.Color;
using Image = SixLabors.ImageSharp.Image;
using Point = System.Drawing.Point;
using PointF = System.Drawing.PointF;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;

namespace UVtools.GUI
{
    public partial class FrmMain : Form
    {
        #region Enums
        #endregion

        #region Properties

        public static readonly Dictionary<Mutation.Mutates, Mutation> Mutations =
            new Dictionary<Mutation.Mutates, Mutation>
            {
                {Mutation.Mutates.Resize, new Mutation(Mutation.Mutates.Resize,
                    "Resizes layer images in a X and/or Y factor, starting from 100% value\n" +
                    "NOTE 1: Build volume bounds are not validated after operation, please ensure scaling stays inside your limits.\n" +
                    "NOTE 2: X and Y are applied to original image, not to the rotated preview (If enabled)."
                )},
                {Mutation.Mutates.Solidify, new Mutation(Mutation.Mutates.Solidify,
                    "Solidifies the selected layers, closes all inner holes.\n" +
                    "Warning: All surrounded holes are filled, no exceptions! Make sure you don't require any of holes in layer path.",
                    Properties.Resources.mutation_solidify
                )},
                {Mutation.Mutates.Erode, new Mutation(Mutation.Mutates.Erode, 
                "The basic idea of erosion is just like soil erosion only, it erodes away the boundaries of foreground object (Always try to keep foreground in white). " +
                        "So what happends is that, all the pixels near boundary will be discarded depending upon the size of kernel. So the thickness or size of the foreground object decreases or simply white region decreases in the image. It is useful for removing small white noises, detach two connected objects, etc.",
                        Properties.Resources.mutation_erosion
                )},
                {Mutation.Mutates.Dilate, new Mutation(Mutation.Mutates.Dilate,
                    "It is just opposite of erosion. Here, a pixel element is '1' if atleast one pixel under the kernel is '1'. So it increases the white region in the image or size of foreground object increases. Normally, in cases like noise removal, erosion is followed by dilation. Because, erosion removes white noises, but it also shrinks our object. So we dilate it. Since noise is gone, they won't come back, but our object area increases. It is also useful in joining broken parts of an object.",
                    Properties.Resources.mutation_dilation
                )},
                {Mutation.Mutates.Opening, new Mutation(Mutation.Mutates.Opening,
                    "Opening is just another name of erosion followed by dilation. It is useful in removing noise.",
                    Properties.Resources.mutation_opening
                )},
                {Mutation.Mutates.Closing, new Mutation(Mutation.Mutates.Closing,
                    "Closing is reverse of Opening, Dilation followed by Erosion. It is useful in closing small holes inside the foreground objects, or small black points on the object.",
                    Properties.Resources.mutation_closing
                )},
                {Mutation.Mutates.Gradient, new Mutation(Mutation.Mutates.Gradient,
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
                {Mutation.Mutates.PyrDownUp, new Mutation(Mutation.Mutates.PyrDownUp,
                    "Performs downsampling step of Gaussian pyramid decomposition.\n" +
                    "First it convolves image with the specified filter and then downsamples the image by rejecting even rows and columns.\n" +
                    "After performs up-sampling step of Gaussian pyramid decomposition\n"
                )},
                {Mutation.Mutates.SmoothMedian, new Mutation(Mutation.Mutates.SmoothMedian,
                    "Each pixel becomes the median of its surrounding pixels. Also a good way to remove noise.\n" +
                    "Note: Iterations must be a odd number."
                )},
                {Mutation.Mutates.SmoothGaussian, new Mutation(Mutation.Mutates.SmoothGaussian,
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

        public Image<L8> ActualLayerImage { get; private set; }

        public Dictionary<uint, List<LayerIssue>> Issues { get; set; }

        public uint TotalIssues { get; set; }

        public bool IsChagingLayer { get; set; }

        #endregion

        #region Constructors
        public FrmMain()
        {
            InitializeComponent();
            FrmLoading = new FrmLoading();
            Program.SetAllControlsFontSize(Controls, 11);
            Program.SetAllControlsFontSize(FrmLoading.Controls, 11);
            
            Clear();

            if (Width >= Screen.FromControl(this).WorkingArea.Width ||
                Height >= Screen.FromControl(this).WorkingArea.Height)
            {
                WindowState = FormWindowState.Maximized;
            }


            DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            DragDrop += (s, e) => { ProcessFile((string[])e.Data.GetData(DataFormats.FileDrop)); };

            foreach (Mutation.Mutates mutate in (Mutation.Mutates[])Enum.GetValues(typeof(Mutation.Mutates)))
            {
                if(!Mutations.ContainsKey(mutate)) continue;
                var item = new ToolStripMenuItem(mutate.ToString())
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
                    pbLayer.ZoomToFit();
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
            pbLayer.ZoomToFit();
            lbLayerActual.Location = new Point(lbLayerActual.Location.X,
                Math.Max(1,
                    Math.Min(tbLayer.Height - 40,
                        (int)(tbLayer.Height - tbLayer.Value * ((float)tbLayer.Height / tbLayer.Maximum)) - lbLayerActual.Height / 2)
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
                        openFile.FilterIndex = 0;
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
                        openFile.FilterIndex = 0;
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
                    DisableGUI();
                    FrmLoading.SetDescription($"Saving {Path.GetFileName(SlicerFile.FileFullPath)}");

                    Task<bool> task = Task<bool>.Factory.StartNew(() =>
                    {
                        bool result = false;
                        try
                        {
                            SlicerFile.Save();
                            result = true;
                        }
                        catch (Exception)
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

                        return result;
                    });

                    FrmLoading.ShowDialog();

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
                        dialog.FileName =
                            $"{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}_copy";
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            DisableGUI();
                            FrmLoading.SetDescription($"Saving {Path.GetFileName(dialog.FileName)}");

                            Task<bool> task = Task<bool>.Factory.StartNew(() =>
                            {
                                bool result = false;
                                try
                                {
                                    SlicerFile.SaveAs(dialog.FileName);
                                    result = true;
                                }
                                catch (Exception)
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

                                return result;
                            });

                            FrmLoading.ShowDialog();

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
                        folder.SelectedPath = Path.GetDirectoryName(SlicerFile.FileFullPath);
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
                                    SlicerFile.Extract(finalPath);
                                    Invoke((MethodInvoker) delegate
                                    {
                                        // Running on the UI thread
                                        EnableGUI(true);
                                    });
                                });

                                FrmLoading.ShowDialog();

                                if (MessageBox.Show(
                                        $"Extraction was successful ({FrmLoading.StopWatch.ElapsedMilliseconds / 1000}s), browser folder to see it contents.\n{finalPath}\nPress 'Yes' if you want open the target folder, otherwise select 'No' to continue.",
                                        "Extraction completed", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                                    DialogResult.Yes)
                                {
                                    Process.Start(finalPath);
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

                    if (item.Tag.GetType() == typeof(Mutation.Mutates))
                    {
                        Mutation.Mutates mutate = (Mutation.Mutates) item.Tag;
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
                        ComputeIssues();
                    }

                    DisableGUI();
                    FrmLoading.SetDescription("Reparing Layers and Issues");

                    Task<bool> task = Task<bool>.Factory.StartNew(() =>
                    {
                        bool result = false;
                        try
                        {

                            Parallel.For(layerStart, layerEnd + 1, layerIndex =>
                            {
                                Layer layer = SlicerFile[layerIndex];
                                using (var image = layer.Image)
                                {
                                    var imageEgmu = image.ToEmguImage();

                                    if (repairResinTraps)
                                    {
                                        if (Issues.TryGetValue((uint) layerIndex, out var issues))
                                        {
                                            foreach (var issue in issues)
                                            {
                                                if(issue.Type != LayerIssue.IssueType.ResinTrap) continue;
                                                imageEgmu.Draw(new VectorOfVectorOfPoint(new VectorOfPoint(issue.Pixels)), -1, new Gray(255), -1);
                                            }
                                        }
                                    }

                                    if (repairIslands)
                                    {
                                        if (closingIterations > 0)
                                        {
                                            imageEgmu = imageEgmu.Dilate((int) closingIterations);
                                            imageEgmu = imageEgmu.Erode((int) closingIterations);
                                        }

                                        if (openingIterations > 0)
                                        {
                                            imageEgmu = imageEgmu.Erode((int) openingIterations);
                                            imageEgmu = imageEgmu.Dilate((int) openingIterations);
                                        }
                                    }

                                    layer.Image = imageEgmu.ToImageSharpL8();
                                    imageEgmu.Dispose();
                                }
                            });
                            result = true;
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

                        return result;
                    });

                    FrmLoading.ShowDialog();

                    ShowLayer();

                    ComputeIssues();

                    menuFileSave.Enabled =
                        menuFileSaveAs.Enabled = true;
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
                    Process.Start(About.Website);
                    return;
                }

                if (ReferenceEquals(sender, menuHelpDonate))
                {
                    MessageBox.Show(
                        "All my work here is given for free (OpenSource), it took some hours to build, test and polish the program.\n" +
                        "If you're happy to contribute for a better program and for my work i will appreciate the tip.\n" +
                        "A browser window will be open and forward to my paypal address after you click 'OK'.\nHappy Printing!",
                        "Donation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Process.Start(About.Donate);
                    return;
                }

                if (ReferenceEquals(sender, menuHelpInstallPrinters))
                {
                    var PEFolder = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}PrusaSlicer";
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
                                Process.Start("https://www.prusa3d.com/prusaslicer/");
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
                        using (var stream = dialog.OpenFile())
                        {
                            SlicerFile.Thumbnails[i].SaveAsPng(stream);
                            stream.Close();
                        }
                    }

                    return;
                }
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
                            Process.Start(dialog.FileName);
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
                            Process.Start(dialog.FileName);
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

                Dictionary<uint, List<LayerIssue>>  processIssues = new Dictionary<uint, List<LayerIssue>>();

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

                Task<bool> task = Task<bool>.Factory.StartNew(() =>
                {
                    bool result = false;
                    try
                    {
                        Parallel.ForEach(processIssues, layerIssues =>
                        {
                            using (var image = SlicerFile[layerIssues.Key].Image)
                            {
                                using (var imageEmgu = image.ToEmguImage())
                                {
                                    var data = imageEmgu.Data;

                                    bool edited = false;

                                    foreach (var issue in layerIssues.Value)
                                    {
                                        if (issue.Type == LayerIssue.IssueType.Island)
                                        {
                                            foreach (var pixel in issue)
                                            {
                                                data[pixel.Y, pixel.X, 0] = 0;
                                            }

                                            edited = true;
                                        }
                                        else if (issue.Type == LayerIssue.IssueType.ResinTrap)
                                        {
                                            imageEmgu.Draw(new VectorOfVectorOfPoint(new VectorOfPoint(issue.Pixels)),
                                                -1, new Gray(255), -1);
                                            edited = true;
                                        }

                                    }

                                    if (!edited) return;
                                    SlicerFile[layerIssues.Key].Image = imageEmgu.ToImageSharpL8();
                                    result = true;
                                }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Unsuccessful Removal", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        Invoke((MethodInvoker)delegate {
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

                ComputeIssues();

                return;
            }

            /************************
             *      Layer Menu      *
             ***********************/
            if (ReferenceEquals(sender, tsLayerImageRotate) || ReferenceEquals(sender, tsLayerImageLayerDifference) ||
                ReferenceEquals(sender, tsLayerImageHighlightIssues) ||
                ReferenceEquals(sender, tsLayerImageLayerOutline))
            {
                ShowLayer();
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
                        using (var stream = dialog.OpenFile())
                        {
                            Image image = (Image) pbLayer.Image.Tag;
                            image.Save(stream, Helpers.PngEncoder);
                            stream.Close();
                        }
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

                Image image = (Image)pbLayer.Image.Tag;
                Clipboard.SetImage(image.ToBitmap());

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

                //using (FileFormat instance = (FileFormat)Activator.CreateInstance(type)) 
                //using (CbddlpFile file = new CbddlpFile())



                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    DisableGUI();
                    FrmLoading.SetDescription($"Converting {Path.GetFileName(SlicerFile.FileFullPath)} to {Path.GetExtension(dialog.FileName)}");

                    Task<bool> task = Task<bool>.Factory.StartNew(() =>
                    {
                        bool result = false;
                        try
                        {
                            result = SlicerFile.Convert(fileFormat, dialog.FileName);
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

                        return result;
                    });

                    FrmLoading.ShowDialog();

                    if (task.Result)
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

            if (Control.ModifierKeys == Keys.Shift)
            {
                DrawPixel(false, location);
            }
            else
            {
                DrawPixel(true, location);
            }

            SlicerFile[ActualLayer].Image = ActualLayerImage;
        }

        private void pbLayer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!pbLayer.IsPointInImage(e.Location)) return;
            var location = pbLayer.PointToImage(e.Location);

            int x = location.X;
            int y = location.Y;

            if (tsLayerImageRotate.Checked)
            {
                x = location.Y;
                y = ActualLayerImage.Height - 1 - location.X;
            }

            tsLayerImageMouseLocation.Text = $"{{X={x}, Y={y}, B={ActualLayerImage[x,y].PackedValue}}}";

            if (!tsLayerImagePixelEdit.Checked || (e.Button & MouseButtons.Right) == 0) return;
            if (!pbLayer.IsPointInImage(e.Location)) return;

            if (Control.ModifierKeys == Keys.Shift)
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
                    pbLayer.ZoomToFit();
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
        #endregion

        #region Methods

        /// <summary>
        /// Closes file and clear UI
        /// </summary>
        void Clear()
        {
            Text = $"{FrmAbout.AssemblyTitle}   Version: {FrmAbout.AssemblyVersion}";
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

            lbMaxLayer.Text = 
            lbLayerActual.Text = 
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

                false;

            tsThumbnailsCount.Text = "0/0";
            tsThumbnailsCount.Tag = null;

            tabControlLeft.TabPages.Remove(tabPageGCode);
            tabControlLeft.TabPages.Remove(tabPageIssues);
            tabControlLeft.SelectedIndex = 0;
        }

        void DisableGUI()
        {
            mainTable.Enabled = 
            menu.Enabled = false;
        }

        void EnableGUI(bool closeLoading = false)
        {
            if(closeLoading)  FrmLoading.Close();

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
            FrmLoading.SetDescription($"Loading {Path.GetFileName(fileName)}");

            var task = Task.Factory.StartNew(() =>
            {
                SlicerFile.Decode(fileName);
                Invoke((MethodInvoker)delegate {
                    // Running on the UI thread
                    EnableGUI(true);
                });
            });

            FrmLoading.ShowDialog();

            if (SlicerFile.LayerCount == 0)
            {
                MessageBox.Show("It seens the file don't have any layer, the causes can be:\n" +
                                "- Empty\n" +
                                "- Corrupted\n" +
                                "- Lacking a sliced model\n" +
                                "- A programing internal error\n\n" +
                                "Please check your file and retry", "Error reading the file - Lacking of layers", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Clear();
                return;
            }


            ActualLayerImage = SlicerFile[0].Image;
            lbMaxLayer.Text = $"{SlicerFile.TotalHeight}mm\n{SlicerFile.LayerCount-1}";
            lbInitialLayer.Text = $"{SlicerFile.LayerHeight}mm\n0";


            tsLayerImageRotate.Checked = ActualLayerImage.Height > ActualLayerImage.Width;

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
                            Image = Properties.Resources.layers_16x16
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

            //ShowLayer(0);

            tabControlLeft.SelectedIndex = 0;
            tsLayerResolution.Text = $"{{Width={SlicerFile.ResolutionX}, Height={SlicerFile.ResolutionY}}}";

            tbLayer.Maximum = (int)SlicerFile.LayerCount - 1;
            ShowLayer(actualLayer);
            


            RefreshInfo();

            pbLayer.ZoomToFit();

            UpdateTitle();
        }

        void UpdateTitle()
        {
            Text = $"{FrmAbout.AssemblyTitle}   File: {Path.GetFileName(SlicerFile.FileFullPath)}   Version: {FrmAbout.AssemblyVersion}";
        }

        void RefreshInfo()
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
                        Image = Properties.Resources.Wrench_16x16,
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

            try
            {
                // OLD
                //if(!ReferenceEquals(pbLayer.Image, null))
                //pbLayer.Image?.Dispose(); // SLOW! LET GC DO IT
                //pbLayer.Image = Image.FromStream(SlicerFile.LayerEntries[layerNum].Open());
                //pbLayer.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);


                Stopwatch watch = Stopwatch.StartNew();
                ActualLayerImage = SlicerFile[layerNum].Image;
                
                //ActualLayerImage = image;

                var imageRgba = ActualLayerImage.CloneAs<Rgba32>();

                if (tsLayerImageLayerOutline.Checked)
                {
                    Image<Gray, byte> greyscale = ActualLayerImage.ToEmguImage();

#if DEBUG
                    greyscale = greyscale.ThresholdBinary(new Gray(254), new Gray(255));
                    VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                    Mat hierarchy = new Mat();

                    CvInvoke.FindContours(greyscale, contours, hierarchy, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);

                    /*
                     * hierarchy[i][0]: the index of the next contour of the same level
                     * hierarchy[i][1]: the index of the previous contour of the same level
                     * hierarchy[i][2]: the index of the first child
                     * hierarchy[i][3]: the index of the parent
                     */
                    var arr = hierarchy.GetData();
                    for (int i = 0; i < contours.Size; i++)
                    {
                        if ((int)arr.GetValue(0, i, 3) >= 0) continue;
                        var r = CvInvoke.BoundingRectangle(contours[i]);
                        CvInvoke.Rectangle(greyscale, r, new MCvScalar(80), 2);

                        greyscale.Draw(contours, i, new Gray(125), -1);
                        
                    }

#else
                    greyscale = greyscale.Canny(80, 40, 3, true);
#endif
                    imageRgba = greyscale.ToImageSharpL8().CloneAs<Rgba32>();
                    greyscale.Dispose();
                }
                else if (tsLayerImageLayerDifference.Checked)
                {
                    if (layerNum > 0 && layerNum < SlicerFile.LayerCount-1)
                    {
                        var previousImage = SlicerFile[layerNum-1].Image;
                        var nextImage = SlicerFile[layerNum+1].Image;
                        //var newImage = new Image<Rgba32>(previousImage.Width, previousImage.Height);

                        /*Parallel.For(0, ActualLayerImage.Height, y => {
                            var newImageSpan = newImage.GetPixelRowSpan(y);
                            var previousImageSpan = previousImage.GetPixelRowSpan(y);
                            for (int x = 0; x < ActualLayerImage.Width; x++)
                            {
                                if (previousImageSpan[x].PackedValue == 0) continue;
                                newImageSpan[x] = new Rgba32(0, 0, previousImageSpan[x].PackedValue);
                            }
                        });


                        imageRgba.Mutate(imgMaskIn =>
                        {
                                imgMaskIn.DrawImage(newImage, PixelColorBlendingMode.Screen, 0.5f);
                        });*/

                        //var nextImage = SlicerFile.GetLayerImage(layerNum+1);
                        Parallel.For(0, ActualLayerImage.Height, y =>
                        {
                            var imageSpan = imageRgba.GetPixelRowSpan(y);
                            var previousImageSpan = previousImage.GetPixelRowSpan(y);
                            var nextImageSpan = nextImage.GetPixelRowSpan(y);
                            for (int x = 0; x < ActualLayerImage.Width; x++)
                            {
                                if (imageSpan[x].PackedValue == 4278190080)
                                {
                                    if (previousImageSpan[x].PackedValue > 0 && nextImageSpan[x].PackedValue > 0)
                                    {
                                        imageSpan[x] = new Rgba32(255, 0, 0);
                                    }
                                    else if (previousImageSpan[x].PackedValue > 0)
                                    {
                                        imageSpan[x] = new Rgba32(previousImageSpan[x].PackedValue, 0, previousImageSpan[x].PackedValue);
                                    }
                                    else if (nextImageSpan[x].PackedValue > 0)
                                    {
                                        imageSpan[x] = new Rgba32(0, nextImageSpan[x].PackedValue, nextImageSpan[x].PackedValue);
                                    }
                                }
                                /*else
                                {
                                    if (previousImageSpan[x].PackedValue > 0 && nextImageSpan[x].PackedValue > 0)
                                    {
                                        imageSpan[x] = new Rgba32(225, 225, 225);
                                    }
                                }*/

                                //if (previousImageSpan[x].PackedValue == 0) continue;
                                //imageSpan[x] = new Rgba32(0, 0, 255, 50);
                            }
                        });

                        /*for (int y = 0; y < image.Height; y++)
                        {
                            var imageSpan = image.GetPixelRowSpan(y);
                            var previousImageSpan = previousImage.GetPixelRowSpan(y);
                            //var nextImageSpan = nextImage.GetPixelRowSpan(y);
                            for (int x = 0; x < image.Width; x++)
                            {
                                if(imageSpan[x].PackedValue == 0 || previousImageSpan[x].PackedValue == 0) continue;
                                imageSpan[x].PackedValue = (byte) (previousImageSpan[x].PackedValue / 2);
                            }
                        }*/
                    }
                }

                if (tsLayerImageHighlightIssues.Checked &&
                    !ReferenceEquals(Issues, null) && 
                    Issues.TryGetValue(ActualLayer, out var issues))
                {
                    imageRgba.TryGetSinglePixelSpan(out var span);
                    byte alpha;
                    foreach (var issue in issues)
                    {
                        if (ReferenceEquals(issue, null)) continue; // Removed issue
                        if(!issue.HaveValidPoint) continue;
                        
                        if (issue.Type == LayerIssue.IssueType.ResinTrap)
                        {
                            using (var dummyImage = new Image<Gray, byte>(ActualLayerImage.Width,
                                ActualLayerImage.Height))
                            {
                                dummyImage.Draw(new VectorOfVectorOfPoint(new VectorOfPoint(issue.Pixels)), -1, new Gray(255), -1);
                                //dummyImage.FillConvexPoly(issue.Pixels, new Gray(255));
                                dummyImage.DrawPolyline(issue.Pixels, true, new Gray(125), 1);
                                byte[,,] data = dummyImage.Data;
                                for (int y = issue.BoundingRectangle.Y; y < issue.BoundingRectangle.Bottom; y++)
                                {
                                    for (int x = issue.BoundingRectangle.X; x < issue.BoundingRectangle.Right; x++)
                                    {
                                        if (data[y, x, 0] == 0) continue;
                                        if(data[y, x, 0] == 255)
                                            span[y * ActualLayerImage.Width + x] = new Rgba32(255, 180, 0);
                                        else
                                            span[y * ActualLayerImage.Width + x] = new Rgba32(255, 0, 0);
                                    }
                                }
                            }

                            continue;
                        }

                        foreach (var pixel in issue)
                        {
                            switch (issue.Type)
                            {
                                /*case LayerIssue.IssueType.ResinTrap:
                                    break;*/
                                case LayerIssue.IssueType.Island:
                                    alpha = ActualLayerImage[pixel.X, pixel.Y].PackedValue;
                                    if (alpha == 0) continue;
                                    // alpha, Color.Yellow
                                    alpha = Math.Max((byte)80, alpha);
                                    span[pixel.Y * ActualLayerImage.Width + pixel.X] = new Rgba32(alpha, alpha, 0);
                                    break;
                                default:
                                    alpha = ActualLayerImage[pixel.X, pixel.Y].PackedValue;
                                    if (alpha == 0) continue;
                                    // alpha, Color.Yellow
                                    alpha = Math.Max((byte)80, alpha);
                                    span[pixel.Y * ActualLayerImage.Width + pixel.X] = new Rgba32(alpha, 0, 0);
                                    break;
                            }
                            
                        }
                    }

                }

                if (tsLayerImageRotate.Checked)
                {
                    //watch.Restart();
                    imageRgba.Mutate(x => x.Rotate(RotateMode.Rotate90));
                    //Debug.Write($"/{watch.ElapsedMilliseconds}");
                }

                //watch.Restart();
                var imageBmp = imageRgba.ToBitmap();
                //imageBmp.MakeTransparent();
                pbLayer.Image = imageBmp;
                pbLayer.Image.Tag = imageRgba;
                //Debug.WriteLine(watch.ElapsedMilliseconds);

                //UniversalLayer layer = new UniversalLayer(image);
                //pbLayer.Image = layer.ToBitmap(image.Width, image.Height);


                // NEW
                /*Stopwatch watch = Stopwatch.StartNew();
                Bitmap bmp;
                using (var ms = new MemoryStream(SlicerFile.GetLayer(layerNum)))
                {
                    bmp = new Bitmap(ms);
                }

                if (tsLayerImageRotate.Checked)
                {
                    //watch.Restart();
                    bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    //Debug.Write($"/{watch.ElapsedMilliseconds}");
                }



                pbLayer.Image = bmp;
                Debug.WriteLine(watch.ElapsedMilliseconds);
                //pbLayer.Image.Tag = image;*/


                byte percent = (byte)((layerNum + 1) * 100 / SlicerFile.LayerCount);


                watch.Stop();
                tsLayerPreviewTime.Text = $"{watch.ElapsedMilliseconds}ms";
                //lbLayers.Text = $"{SlicerFile.GetHeightFromLayer(layerNum)} / {SlicerFile.TotalHeight}mm\n{layerNum} / {SlicerFile.LayerCount-1}\n{percent}%";
                lbLayerActual.Text = $"{SlicerFile.GetHeightFromLayer(ActualLayer)}mm\n{ActualLayer}\n{percent}%";
                lbLayerActual.Location = new Point(lbLayerActual.Location.X, 
                    Math.Max(1, 
                        Math.Min(tbLayer.Height- lbLayerActual.Height, 
                            (int)(tbLayer.Height - tbLayer.Value * ((float)tbLayer.Height / tbLayer.Maximum)) - lbLayerActual.Height/2)
                ));

                pbLayers.Value = percent;
                lbLayerActual.Invalidate();
                lbLayerActual.Update();
                lbLayerActual.Refresh();
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

            if (ReferenceEquals(sender, tabControlLeft))
            {
                if(ReferenceEquals(tabControlLeft.SelectedTab, tabPageIssues))
                {
                    if (!ReferenceEquals(tabPageIssues.Tag, null)) return;
                    ComputeIssues();
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
                    foreach (ListViewItem item in lvIssues.Items)
                    {
                        item.Selected = false;
                    }
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
        }
#endregion



        void DrawPixel(bool isAdd, Point location)
        {
            //var point = pbLayer.PointToImage(location);
            int x = location.X;
            int y = location.Y;

            if (tsLayerImageRotate.Checked)
            {
                x = location.Y;
                y = ActualLayerImage.Height - 1 - location.X;
            }

            Color color;
            L8 pixelL8;

            if (isAdd)
            {
                if (ActualLayerImage[x, y].PackedValue > 200) return;
                color = Color.Green;
                pixelL8 = Helpers.L8White;
            }
            else
            {
                if (ActualLayerImage[x, y].PackedValue == 0) return;
                color = Color.DarkRed;
                pixelL8 = Helpers.L8Black;
            }


            ActualLayerImage[x, y] = pixelL8;
            Bitmap bmp = pbLayer.Image as Bitmap;
            bmp.SetPixel(location.X, location.Y, color);

            /*if (bmp.GetPixel(point.X, point.Y).GetBrightness() == returnif) return;
            bmp.SetPixel(point.X, point.Y, color);
            ActualLayerImage[point.X, point.Y] = pixelL8;
            var newImage = ActualLayerImage.Clone();
            if (tsLayerImageRotate.Checked)
            {
                newImage.Mutate(mut => mut.Rotate(RotateMode.Rotate270));
            }
            SlicerFile[ActualLayer].Image = newImage;*/
            pbLayer.Invalidate();
            menuFileSave.Enabled = menuFileSaveAs.Enabled = true;
        }

        

        public void MutateLayers(Mutation.Mutates type)
        {
            uint layerStart;
            uint layerEnd;
            uint iterationsStart = 0;
            uint iterationsEnd = 0;
            bool fade = false;
            float iterationSteps = 0;
            uint maxIteration = 0;

            float x = 0;
            float y = 0;

            switch (type)
            {
                case Mutation.Mutates.Resize:
                    using (FrmMutationResize inputBox = new FrmMutationResize(Mutations[type]))
                    {
                        if (inputBox.ShowDialog() != DialogResult.OK) return;
                        layerStart = inputBox.LayerRangeStart;
                        layerEnd = inputBox.LayerRangeEnd;
                        x = inputBox.X;
                        y = inputBox.Y;
                        fade = inputBox.Fade;
                    }

                    break;
                default:
                    using (FrmMutation inputBox = new FrmMutation(Mutations[type]))
                    {
                        if (inputBox.ShowDialog() != DialogResult.OK) return;
                        iterationsStart = inputBox.Iterations;
                        if (iterationsStart == 0) return;
                        layerStart = inputBox.LayerRangeStart;
                        layerEnd = inputBox.LayerRangeEnd;
                        iterationsEnd = inputBox.IterationsEnd;
                        fade = layerStart != layerEnd && iterationsStart != iterationsEnd && inputBox.IterationsFade;
                        if (fade)
                        {
                            iterationSteps = Math.Abs((float)iterationsStart - iterationsEnd) / (layerEnd - layerStart);
                            maxIteration = Math.Max(iterationsStart, iterationsEnd);
                        }
                    }

                    break;
            }

            

            DisableGUI();
            FrmLoading.SetDescription($"Mutating - {type}");

            Task<bool> task = Task<bool>.Factory.StartNew(() =>
            {
                bool result = false;
                try
                {
                    if (type == Mutation.Mutates.Resize)
                    {
                        SlicerFile.Resize(layerStart, layerEnd, x / 100f, y / 100f, fade);
                    }
                    else
                    {
                        Parallel.For(layerStart, layerEnd + 1, layerIndex =>
                        {
                            var iterations = iterationsStart;
                            if (fade)
                            {
                                // calculate iterations based on range
                                iterations = iterationsStart < iterationsEnd
                                    ? (uint) (iterationsStart + (layerIndex - layerStart) * iterationSteps)
                                    : (uint) (iterationsStart - (layerIndex - layerStart) * iterationSteps);

                                // constrain
                                iterations = Math.Min(Math.Max(1, iterations), maxIteration);
                                //Debug.WriteLine($"A Layer: {i} = {iterations}");
                            }

                            Layer layer = SlicerFile[layerIndex];
                            var image = layer.Image;
                            var imageEgmu = image.ToEmguImage();
                            switch (type)
                            {
                                case Mutation.Mutates.Resize:
                                    var resizedImage = imageEgmu.Resize( (int) (iterationsStart / 100.0 * image.Width), (int) (iterationsEnd / 100.0 * image.Height), Inter.Lanczos4);
                                    imageEgmu = resizedImage.Copy(new Rectangle(0, 0, image.Width, image.Height));
                                    break;
                                case Mutation.Mutates.Solidify:
                                    for (byte pass = 0; pass < 1; pass++) // Passes
                                    {
                                        var imageThreshold = imageEgmu.ThresholdBinary(new Gray(254), new Gray(255)); // Clean AA
                                        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                                        Mat hierarchy = new Mat();

                                        CvInvoke.FindContours(imageThreshold, contours, hierarchy, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);

                                        var arr = hierarchy.GetData();

                                        /*
                                         * hierarchy[i][0]: the index of the next contour of the same level
                                         * hierarchy[i][1]: the index of the previous contour of the same level
                                         * hierarchy[i][2]: the index of the first child
                                         * hierarchy[i][3]: the index of the parent
                                         */
                                        for (int i = 0; i < contours.Size; i++)
                                        {
                                            if ((int)arr.GetValue(0, i, 2) != -1 || (int)arr.GetValue(0, i, 3) == -1) continue;
                                            var r = CvInvoke.BoundingRectangle(contours[i]);
                                            //imageEgmu.FillConvexPoly(contours[i].ToArray(), new Gray(255));
                                            //imageThreshold.FillConvexPoly(contours[i].ToArray(), new Gray(255));
                                            imageEgmu.Draw(contours, i, new Gray(255), -1);
                                        }

                                        // Attempt to close any tiny region
                                        //imageEgmu = imageEgmu.Dilate(2).Erode(2);
                                    }



                                    break;
                                case Mutation.Mutates.Erode:
                                    imageEgmu = imageEgmu.Erode((int) iterations);
                                    break;
                                case Mutation.Mutates.Dilate:
                                    imageEgmu = imageEgmu.Dilate((int) iterations);
                                    break;
                                case Mutation.Mutates.Opening:
                                    imageEgmu = imageEgmu.Erode((int) iterations).Dilate((int) iterations);
                                    break;
                                case Mutation.Mutates.Closing:
                                    imageEgmu = imageEgmu.Dilate((int) iterations).Erode((int) iterations);
                                    break;
                                case Mutation.Mutates.Gradient:
                                    imageEgmu = imageEgmu.MorphologyEx(MorphOp.Gradient, Program.KernelStar3x3,
                                        new Point(-1, -1), (int) iterations,
                                        BorderType.Default, new MCvScalar());
                                    break;
                                case Mutation.Mutates.TopHat:
                                    imageEgmu = imageEgmu.MorphologyEx(MorphOp.Tophat,
                                        CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(9, 9),
                                            new Point(-1, -1)), new Point(-1, -1), (int) iterations,
                                        BorderType.Default, new MCvScalar());
                                    break;
                                case Mutation.Mutates.BlackHat:
                                    imageEgmu = imageEgmu.MorphologyEx(MorphOp.Blackhat,
                                        CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(9, 9),
                                            new Point(-1, -1)), new Point(-1, -1), (int) iterations,
                                        BorderType.Default, new MCvScalar());
                                    break;
                                case Mutation.Mutates.HitMiss:
                                    imageEgmu = imageEgmu.MorphologyEx(MorphOp.HitMiss, Program.KernelFindIsolated,
                                        new Point(-1, -1), (int) iterations,
                                        BorderType.Default, new MCvScalar());
                                    break;
                                case Mutation.Mutates.PyrDownUp:
                                    imageEgmu = imageEgmu.PyrDown().PyrUp();
                                    break;
                                case Mutation.Mutates.SmoothMedian:
                                    imageEgmu = imageEgmu.SmoothMedian((int) iterations);
                                    break;
                                case Mutation.Mutates.SmoothGaussian:
                                    imageEgmu = imageEgmu.SmoothGaussian((int) iterations);
                                    break;
                            }

                            layer.Image = imageEgmu.ToImageSharpL8();
                            imageEgmu.Dispose();
                        });
                    }

                    result = true;
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

                return result;
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

        private void ComputeIssues()
        {
            tabPageIssues.Tag = true;
            TotalIssues = 0;
            lvIssues.BeginUpdate();
            lvIssues.Items.Clear();
            lvIssues.EndUpdate();
            UpdateIssuesInfo();

            DisableGUI();
            FrmLoading.SetDescription("Computing Issues");

            Task<bool> task = Task<bool>.Factory.StartNew(() =>
            {
                bool result = false;
                try
                {
                    Task taskGenericIssues = Task.Factory.StartNew(() =>
                    {
                        var issues = SlicerFile.LayerManager.GetAllIssues();
                        Issues = new Dictionary<uint, List<LayerIssue>>();

                        for (uint i = 0; i < SlicerFile.LayerCount; i++)
                        {
                            if (issues.TryGetValue(i, out var list))
                            {
                                Issues.Add(i, list);
                            }
                        }
                    });



                    var layerHollowAreas = new ConcurrentDictionary<uint, List<LayerHollowArea>>();
                    
                    Parallel.ForEach(SlicerFile,
                    //new ParallelOptions{MaxDegreeOfParallelism = 1},
                    layer =>
                    {
                        using (var image = layer.Image)
                        {
                            using (Image<Gray, byte> grayscale = image.ToEmguImage().ThresholdBinary(new Gray(254), new Gray(255)))
                            {
                                var listHollowArea = new List<LayerHollowArea>();

                                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                                Mat hierarchy = new Mat();

                                CvInvoke.FindContours(grayscale, contours, hierarchy, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);

                                var arr = hierarchy.GetData();
                                //
                                //hierarchy[i][0]: the index of the next contour of the same level
                                //hierarchy[i][1]: the index of the previous contour of the same level
                                //hierarchy[i][2]: the index of the first child
                                //hierarchy[i][3]: the index of the parent
                                //

                                for (int i = 0; i < contours.Size; i++)
                                {
                                    if ((int) arr.GetValue(0, i, 2) != -1 || (int) arr.GetValue(0, i, 3) == -1)
                                        continue;

                                    listHollowArea.Add(new LayerHollowArea(contours[i].ToArray(), CvInvoke.BoundingRectangle(contours[i]), layer.Index == 0 || layer.Index == SlicerFile.LayerCount-1 ? LayerHollowArea.AreaType.Drain : LayerHollowArea.AreaType.Unknown));

                                    if (listHollowArea.Count > 0)
                                        layerHollowAreas.TryAdd(layer.Index, listHollowArea);
                                }
                            }
                        }
                    });

                    

                    for (uint layerIndex = 1; layerIndex < SlicerFile.LayerCount-1; layerIndex++) // Ignore first and last layers, always drains
                    {
                        if(!layerHollowAreas.TryGetValue(layerIndex, out var areas)) continue; // No hollow areas in this layer, ignore

                        byte areaCount = 0;
                        //foreach (var area in areas)
                        Parallel.ForEach(areas, area =>
                        {
                            if (area.Type != LayerHollowArea.AreaType.Unknown) return; // processed, ignore
                            area.Type = LayerHollowArea.AreaType.Trap;

                            areaCount++;

                            List<LayerHollowArea> linkedAreas = new List<LayerHollowArea>();

                            for (sbyte dir = 1; dir >= -1 && area.Type != LayerHollowArea.AreaType.Drain; dir -= 2)
                                //Parallel.ForEach(dirs, new ParallelOptions {MaxDegreeOfParallelism = 2}, dir =>
                            {
                                Queue<LayerHollowArea> queue = new Queue<LayerHollowArea>();
                                queue.Enqueue(area);
                                area.Processed = false;
                                int nextLayerIndex = (int) layerIndex;
                                while (queue.Count > 0 && area.Type != LayerHollowArea.AreaType.Drain)
                                {
                                    LayerHollowArea checkArea = queue.Dequeue();
                                    if (checkArea.Processed) continue;
                                    checkArea.Processed = true;
                                    nextLayerIndex += dir;
                                    Debug.WriteLine(
                                        $"Area Count: {areaCount} | Layer: {layerIndex} | Next Layer: {nextLayerIndex} | Dir: {dir}");
                                    if (nextLayerIndex < 0 && nextLayerIndex >= SlicerFile.LayerCount)
                                        break; // Exhaust layers
                                    bool haveNextAreas =
                                        layerHollowAreas.TryGetValue((uint) nextLayerIndex, out var nextAreas);

                                    using (var image = SlicerFile[nextLayerIndex].Image)
                                    {
                                        using (var emguImage = new Image<Gray, byte>(ActualLayerImage.Width,
                                            ActualLayerImage.Height))
                                        {
                                            image.TryGetSinglePixelSpan(out var span);
                                            //emguImage.FillConvexPoly(checkArea.Contour, new Gray(255));
                                            emguImage.Draw(new VectorOfVectorOfPoint(new VectorOfPoint(checkArea.Contour)), -1,
                                                new Gray(255), -1);

                                            bool exitPixelLoop = false;
                                            uint blackCount = 0;

                                            byte[,,] data = emguImage.Data;
                                            for (int y = checkArea.BoundingRectangle.Y;
                                                y <= checkArea.BoundingRectangle.Bottom &&
                                                area.Type != LayerHollowArea.AreaType.Drain && !exitPixelLoop;
                                                y++)
                                            {
                                                for (int x = checkArea.BoundingRectangle.X;
                                                    x <= checkArea.BoundingRectangle.Right &&
                                                    area.Type != LayerHollowArea.AreaType.Drain && !exitPixelLoop;
                                                    x++)
                                                {

                                                    if (data[y, x, 0] != 255) continue;
                                                    if (span[y * image.Width + x].PackedValue > 30) continue;
                                                    blackCount++;

                                                    if (haveNextAreas
                                                    ) // Have areas, can be on same area path or not
                                                    {
                                                        foreach (var nextArea in nextAreas)
                                                        {
                                                            /*if (!area.BoundingRectangle.IntersectsWith(nextArea.BoundingRectangle)) // If not intersect futher ispection is useless
                                                            {
                                                                continue;
                                                            }*/
                                                            if (!(CvInvoke.PointPolygonTest(
                                                                new VectorOfPoint(nextArea.Contour),
                                                                new PointF(x, y), false) >= 0)) continue;
                                                            if (nextArea.Type == LayerHollowArea.AreaType.Drain
                                                            ) // Found a drain, stop query
                                                            {
                                                                area.Type = LayerHollowArea.AreaType.Drain;
                                                            }
                                                            else
                                                            {
                                                                queue.Enqueue(nextArea);
                                                            }

                                                            linkedAreas.Add(nextArea);

                                                            exitPixelLoop = true;
                                                            break;
                                                        }
                                                    }
                                                    else if(blackCount > Math.Min(checkArea.Contour.Length / 2, 10)) // Black pixel without next areas = Drain
                                                    {
                                                        area.Type = LayerHollowArea.AreaType.Drain;
                                                        exitPixelLoop = true;
                                                        break;
                                                    }
                                                } // X loop
                                            } // Y loop

                                            if (queue.Count == 0 && blackCount > Math.Min(checkArea.Contour.Length / 2, 10))
                                            {
                                                /*Invoke((MethodInvoker)delegate
                                                {
                                                    // Running on the UI thread
                                                    ShowLayer((uint)nextLayerIndex);
                                                });*/
                                                area.Type = LayerHollowArea.AreaType.Drain;
                                            }

                                        } // Dispose emgu image
                                    } // Dispose image
                                } // Areas loop
                            } // Dir layer loop

                            foreach (var linkedArea in linkedAreas) // Update linked areas
                            {
                                linkedArea.Type = area.Type;
                            }
                        });
                    }

                    taskGenericIssues.Wait();

                    for (uint layerIndex = 0; layerIndex < SlicerFile.LayerCount; layerIndex++)
                    {
                        if (!layerHollowAreas.TryGetValue(layerIndex, out var list)) continue;
                        if (list.Count > 0)
                        {
                            var issuesHollow = new List<LayerIssue>();
                            
                            foreach (var area in list)
                            {
                                if (area.Type == LayerHollowArea.AreaType.Trap)
                                {
                                    issuesHollow.Add(new LayerIssue(SlicerFile[layerIndex], LayerIssue.IssueType.ResinTrap, area.Contour, area.BoundingRectangle));
                                }
                            }

                            if (issuesHollow.Count > 0)
                            {
                                if (Issues.TryGetValue(layerIndex, out var currentIssue))
                                {
                                    currentIssue.AddRange(issuesHollow);
                                }
                                else
                                {
                                    Issues.Add(layerIndex, issuesHollow);
                                }
                            }
                        }
                    }


                    result = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error while trying compute issues", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    Invoke((MethodInvoker)delegate {
                        // Running on the UI thread
                        EnableGUI(true);
                    });
                }

                return result;
            });

            FrmLoading.ShowDialog();

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
    }
}
