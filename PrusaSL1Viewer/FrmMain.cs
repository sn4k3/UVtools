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
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV.Structure;
using PrusaSL1Reader;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;

namespace PrusaSL1Viewer
{
    public partial class FrmMain : Form
    {
        #region Enums
        public enum eMutate
        {
            Erode,
            Dilate,
            PyrDownUp,
            SmoothMedian,
            SmoothGaussian
        }
        #endregion

        #region Properties

        public static readonly Dictionary<eMutate, string> MutateDescriptions = new Dictionary<eMutate, string>
        {
            {eMutate.Erode, "Erodes image using a 3x3 rectangular structuring element.\n" +
                            "Erosion are applied several (iterations) times"},
            {eMutate.Dilate, "Dilates image using a 3x3 rectangular structuring element.\n" +
                             "Dilation are applied several (iterations) times"},
            {eMutate.PyrDownUp, "Performs downsampling step of Gaussian pyramid decomposition.\n" +
                                "First it convolves image with the specified filter and then downsamples the image by rejecting even rows and columns.\n" +
                                "After performs up-sampling step of Gaussian pyramid decomposition\n" +
                                "First it upsamples image by injecting even zero rows and columns and then convolves result with the specified filter multiplied by 4 for interpolation"},
            {eMutate.SmoothMedian, "Finding median of size neighborhood"},
            {eMutate.SmoothGaussian, "Perform Gaussian Smoothing"}
        };

        public FrmLoading FrmLoading { get; }
        public static FileFormat SlicerFile
        {
            get => Program.SlicerFile;
            set => Program.SlicerFile = value;
        }

        public uint ActualLayer => (uint)(sbLayers.Maximum - sbLayers.Value);
        public Image<L8> ActualLayerImage { get; private set; }

        #endregion

        #region Constructors
        public FrmMain()
        {
            InitializeComponent();
            FrmLoading = new FrmLoading();
            Program.SetAllControlsFontSize(Controls, 11);
            Program.SetAllControlsFontSize(FrmLoading.Controls, 11);

            Clear();

            DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            DragDrop += (s, e) => { ProcessFile((string[])e.Data.GetData(DataFormats.FileDrop)); };

            foreach (eMutate mutate in (eMutate[])Enum.GetValues(typeof(eMutate)))
            {
                var item = new ToolStripMenuItem(mutate.ToString())
                {
                    ToolTipText = MutateDescriptions[mutate], Tag = mutate, AutoToolTip = true
                };
                item.Click += ItemClicked;
                menuMutate.DropDownItems.Add(item);
            }
        }

        #endregion

        #region Overrides

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ProcessFile(Program.Args);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.KeyCode == Keys.R && e.Control)
            {
                tsLayerImageRotate.PerformClick();
                e.Handled = true;
                return;
            }

            if ((e.KeyCode == Keys.NumPad0 || e.KeyCode == Keys.D0) && e.Control)
            {
                pbLayer.ZoomToFit();
                e.Handled = true;
                return;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            pbLayer.ZoomToFit();
        }

        #endregion

        #region Events

        private void ItemClicked(object sender, EventArgs e)
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
                    ProcessFile();
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
                            Invoke((MethodInvoker)delegate {
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
                                    Invoke((MethodInvoker)delegate {
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
                        folder.Description = $"A \"{fileNameNoExt}\" folder will be created on your selected folder to dump the content.";
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
                                    Invoke((MethodInvoker)delegate {
                                        // Running on the UI thread
                                        EnableGUI(true);
                                    });
                                });

                                FrmLoading.ShowDialog();
                                
                                if (MessageBox.Show(
                                        $"Extraction was successful ({FrmLoading.StopWatch.ElapsedMilliseconds/1000}s), browser folder to see it contents.\n{finalPath}\nPress 'Yes' if you want open the target folder, otherwise select 'No' to continue.",
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
                                    $"Unable to set '{modifier.Name}' value, was not found, it may not implemented yet.", "Operation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            RefreshInfo();

                            menuFileSave.Enabled =
                            menuFileSaveAs.Enabled = true;
                        }

                        return;

                    }

                    if (item.Tag.GetType() == typeof(eMutate))
                    {
                        eMutate mutate = (eMutate)item.Tag;
                        MutateLayers(mutate);
                        return;
                    }
                }

                // View


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
                    string printerFolder = $"{Application.StartupPath}{Path.DirectorySeparatorChar}PrusaSlicer{Path.DirectorySeparatorChar}printer";
                    try
                    {
                        string[] profiles = Directory.GetFiles(printerFolder);
                        string profilesNames = String.Empty;

                        foreach (var profile in profiles)
                        {
                            profilesNames += $"{Path.GetFileNameWithoutExtension(profile)}\n";
                        }

                        var result = MessageBox.Show(
                            "This action will install following printer profiles into PrusaSlicer:\n" +
                            "---------------\n" +
                            profilesNames +
                            "---------------\n" +
                            "Click 'Yes' to override all profiles\n" +
                            "Click 'No' to install only missing profiles without override\n" +
                            "Clock 'Abort' to cancel this operation",
                            "Install printers into PrusaSlicer", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);


                        if (result == DialogResult.Abort)
                        {
                            return;
                        }

                        bool overwrite = result == DialogResult.Yes;
                        string targetFolder = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}PrusaSlicer{Path.DirectorySeparatorChar}printer";

                        foreach (var profile in profiles)
                        {
                            string targetFile = $"{targetFolder}{Path.DirectorySeparatorChar}{Path.GetFileName(profile)}";
                            if (!overwrite && File.Exists(targetFile)) continue;
                            File.Copy(profile, targetFile, overwrite);
                        }

                        MessageBox.Show(
                            "Printers were installed.\nRestart PrusaSlicer and check if printers are present.",
                            "Operation Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    

                    
                    return;
                }
            }

            /************************
             *    Thumbnail Menu    *
             ***********************/
            if (ReferenceEquals(sender, tsThumbnailsPrevious))
            {
                byte i = (byte)tsThumbnailsCount.Tag;
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

                tsThumbnailsCount.Text = $"{i+1}/{SlicerFile.CreatedThumbnailsCount}";
                tsThumbnailsNext.Enabled = true;

                tsThumbnailsResolution.Text = pbThumbnail.Image.PhysicalDimension.ToString();

                AdjustThumbnailSplitter();
                return;
            }

            if (ReferenceEquals(sender, tsThumbnailsNext))
            {
                byte i = byte.Parse(tsThumbnailsCount.Tag.ToString());
                if (i >= SlicerFile.CreatedThumbnailsCount-1)
                {
                    // This should never happen!
                    tsThumbnailsNext.Enabled = false;
                    return;
                }

                tsThumbnailsCount.Tag = ++i;

                if (i >= SlicerFile.CreatedThumbnailsCount-1)
                {
                    tsThumbnailsNext.Enabled = false;
                }

                pbThumbnail.Image = SlicerFile.Thumbnails[i]?.ToBitmap();

                tsThumbnailsCount.Text = $"{i+1}/{SlicerFile.CreatedThumbnailsCount}";
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
                    if(ReferenceEquals(SlicerFile.Thumbnails[i], null))
                    {
                        return; // This should never happen!
                    }

                    dialog.Filter = "Image Files|.*png";
                    dialog.AddExtension = true;
                    dialog.FileName = $"{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}_thumbnail{i+1}.png";

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
             *      Layer Menu      *
             ***********************/
            if (ReferenceEquals(sender, tsLayerImageRotate) || ReferenceEquals(sender, tsLayerImageLayerDifference) || ReferenceEquals(sender, tsLayerImageLayerOutline)) 
            {
                sbLayers_ValueChanged(sbLayers, null);
                return;
            }

            if (ReferenceEquals(sender, tsLayerImageExport))
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    if (ReferenceEquals(pbLayer, null))
                    {
                        return; // This should never happen!
                    }

                    dialog.Filter = "Image Files|.*png";
                    dialog.AddExtension = true;
                    dialog.FileName = $"{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}_layer{ActualLayer}.png";

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        using (var stream = dialog.OpenFile())
                        {
                            Image<L8> image = (Image<L8>)pbLayer.Image.Tag;
                            image.Save(stream, new PngEncoder());
                            stream.Close();
                        }
                    }

                    return;

                }
            }
        }


        private void sbLayers_ValueChanged(object sender, EventArgs e)
        {
            if (ReferenceEquals(SlicerFile, null)) return;
            ShowLayer(ActualLayer);
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
                        catch (Exception)
                        {
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
                        if (MessageBox.Show($"Convertion is completed: {Path.GetFileName(dialog.FileName)} in {FrmLoading.StopWatch.ElapsedMilliseconds/1000}s\n" +
                                            "Do you want open the converted file in a new window?",
                            "Convertion completed", MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information) == DialogResult.Yes)
                        {
                            Program.NewInstance(dialog.FileName);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Convertion was unsuccessful! Maybe not implemented...", "Convertion unsuccessful", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

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

            // GUI CLEAN
            pbThumbnail.Image = null;
            pbLayer.Image = null;
            pbThumbnail.Image = null;
            tbGCode.Clear();
            lbLayers.Text = "Layers";
            lvProperties.BeginUpdate();
            lvProperties.Items.Clear();
            lvProperties.Groups.Clear();
            lvProperties.EndUpdate();
            pbLayers.Value = 0;
            sbLayers.Value = 0;

            statusBar.Items.Clear();
            menuFileConvert.DropDownItems.Clear();

            menuEdit.DropDownItems.Clear();
            foreach (ToolStripItem item in menuMutate.DropDownItems)
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
            sbLayers.Enabled = 
            pbLayers.Enabled = 
            menuEdit.Enabled = 
            menuMutate.Enabled =

                false;

            tsThumbnailsCount.Text = "0/0";
            tsThumbnailsCount.Tag = null;

            tabControlLeft.TabPages.Remove(tbpGCode);
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

        void ProcessFile()
        {
            if (ReferenceEquals(SlicerFile, null)) return;
            ProcessFile(SlicerFile.FileFullPath);
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

        void ProcessFile(string fileName)
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
            
            sbLayers.Enabled =
            pbLayers.Enabled =
            menuEdit.Enabled = 
            menuMutate.Enabled =
                true;

            if (!ReferenceEquals(SlicerFile.GCode, null))
            {
                tabControlLeft.TabPages.Add(tbpGCode);
            }

            //ShowLayer(0);

            sbLayers.SmallChange = 1;
            sbLayers.Minimum = 0;
            sbLayers.Maximum = (int)SlicerFile.LayerCount - 1;
            sbLayers.Value = sbLayers.Maximum;

            tabControlLeft.SelectedIndex = 0;
            tsLayerResolution.Text = $"{{Width={SlicerFile.ResolutionX}, Height={SlicerFile.ResolutionY}}}";


            RefreshInfo();

            pbLayer.ZoomToFit();

            UpdateTitle();
        }

        void UpdateTitle()
        {
            Text = $"{FrmAbout.AssemblyTitle}   Version: {FrmAbout.AssemblyVersion}   File: {Path.GetFileName(SlicerFile.FileFullPath)}";
        }

        void RefreshInfo()
        {
            menuEdit.DropDownItems.Clear();
            foreach (var modifier in SlicerFile.PrintParameterModifiers)
            {
                ToolStripItem item = new ToolStripMenuItem
                {
                    Text = $"{modifier.Name} ({SlicerFile.GetValueFromPrintParameterModifier(modifier)}{modifier.ValueUnit})",
                    Tag = modifier,
                    Image = Properties.Resources.Wrench_16x16,
                };
                menuEdit.DropDownItems.Add(item);

                item.Click += ItemClicked;
            }

            lvProperties.BeginUpdate();
            lvProperties.Items.Clear();

            foreach (object config in SlicerFile.Configs)
            {
                ListViewGroup group = new ListViewGroup(config.GetType().Name);
                lvProperties.Groups.Add(group);
                foreach (PropertyInfo propertyInfo in config.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    ListViewItem item = new ListViewItem(propertyInfo.Name, group);
                    object obj = new object();
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

        void ShowLayer(uint layerNum)
        {
            try
            {
                // OLD
                //if(!ReferenceEquals(pbLayer.Image, null))
                //pbLayer.Image?.Dispose(); // SLOW! LET GC DO IT
                //pbLayer.Image = Image.FromStream(SlicerFile.LayerEntries[layerNum].Open());
                //pbLayer.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);


                Stopwatch watch = Stopwatch.StartNew();
                var image = SlicerFile[layerNum].Image;
                ActualLayerImage = image.Clone();

                if (tsLayerImageLayerOutline.Checked)
                {
                    Emgu.CV.Image<Gray, byte> grayscale = image.ToEmguImage();
                    grayscale = grayscale.Canny(100, 200, 3, true);
                    image = grayscale.ToImageSharpL8();
                    grayscale.Dispose();
                }
                else if (tsLayerImageLayerDifference.Checked)
                {
                    if (layerNum > 0)
                    {
                        var previousImage = SlicerFile[layerNum-1].Image;

                        //var nextImage = SlicerFile.GetLayerImage(layerNum+1);

                        Parallel.For(0, image.Height, y => {
                            var imageSpan = image.GetPixelRowSpan(y);
                            var previousImageSpan = previousImage.GetPixelRowSpan(y);
                            for (int x = 0; x < image.Width; x++)
                            {
                                if (imageSpan[x].PackedValue == 0 || previousImageSpan[x].PackedValue == 0) continue;
                                imageSpan[x].PackedValue = (byte)(previousImageSpan[x].PackedValue / 2);
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

                if (tsLayerImageRotate.Checked)
                {
                    //watch.Restart();
                    image.Mutate(x => x.Rotate(RotateMode.Rotate90));
                    //Debug.Write($"/{watch.ElapsedMilliseconds}");
                }

                //watch.Restart();
                pbLayer.Image = image.ToBitmap();
                pbLayer.Image.Tag = image;
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
                lbLayers.Text = $"{SlicerFile.GetHeightFromLayer((uint)layerNum + 1)} / {SlicerFile.TotalHeight}mm\n{layerNum + 1} / {SlicerFile.LayerCount}\n{percent}%";
                pbLayers.Value = percent;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

        }

        void AddStatusBarItem(string name, object item, string extraText = "")
        {
            if (statusBar.Items.Count > 0)
                statusBar.Items.Add(new ToolStripSeparator());

            ToolStripLabel label = new ToolStripLabel($"{name}: {item}{extraText}");
            statusBar.Items.Add(label);
        }

        void AdjustThumbnailSplitter()
        {
            scLeft.SplitterDistance = Math.Min(pbThumbnail.Image.Height + 5, 400);
        }
        #endregion

        private void pbLayer_Zoomed(object sender, Cyotek.Windows.Forms.ImageBoxZoomEventArgs e)
        {
            tsLayerImageZoomValueLabel.Text = $"{e.NewZoom} %";
        }

        void DrawPixel(bool isAdd, Point location, Color color, L8 pixelL8)
        {
            var point = pbLayer.PointToImage(location);
            int x = point.X;
            int y = point.Y;

            if (tsLayerImageRotate.Checked)
            {
                x = point.Y;
                y = ActualLayerImage.Height - 1 - point.X;
            }


            if (isAdd && ActualLayerImage[x, y].PackedValue > byte.MaxValue / 2)
            {
                return;
            }
            if (!isAdd && ActualLayerImage[x, y].PackedValue < byte.MaxValue / 2)
            {
                return;
            }

            ActualLayerImage[x, y] = pixelL8;
            Bitmap bmp = pbLayer.Image as Bitmap;
            bmp.SetPixel(point.X, point.Y, color);

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

        private void pbLayer_MouseUp(object sender, MouseEventArgs e)
        {
            if (!tsLayerImagePixelEdit.Checked || (e.Button & MouseButtons.Right) == 0) return;
            if (!pbLayer.IsPointInImage(e.Location)) return;
            

            if (Control.ModifierKeys == Keys.Shift)
            {
                DrawPixel(false, e.Location, Color.DarkRed, Helpers.L8Black);
            }
            else
            {
                DrawPixel(true, e.Location, Color.Green, Helpers.L8White);
            }

            SlicerFile[ActualLayer].Image = ActualLayerImage;
        }

        private void pbLayer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!tsLayerImagePixelEdit.Checked || (e.Button & MouseButtons.Right) == 0) return;
            if (!pbLayer.IsPointInImage(e.Location)) return;

            if (Control.ModifierKeys == Keys.Shift)
            {
                DrawPixel(false, e.Location, Color.DarkRed, Helpers.L8Black);
                return;
            }

            DrawPixel(true, e.Location, Color.Green, Helpers.L8White);
        }

        public void MutateLayers(eMutate type)
        {
            decimal value = 0;
            using (FrmInputBox inputBox = new FrmInputBox($"Mutate - {type}", MutateDescriptions[type], 0))
            {
                if (inputBox.ShowDialog() != DialogResult.OK) return;
                value = inputBox.NewValue;
                if (value == 0) return;
            }

            DisableGUI();
            FrmLoading.SetDescription($"Mutating - {type}");

            Task<bool> task = Task<bool>.Factory.StartNew(() =>
            {
                bool result = false;
                try
                {
                    Parallel.ForEach(SlicerFile, (layer) =>
                    {
                        var image = layer.Image;
                        var imageEgmu = image.ToEmguImage();
                        switch (type)
                        {
                            case eMutate.Erode:
                                imageEgmu = imageEgmu.Erode((int) value);
                                break;
                            case eMutate.Dilate:
                                imageEgmu = imageEgmu.Dilate((int) value);
                                break;
                            case eMutate.PyrDownUp:
                                imageEgmu = imageEgmu.PyrDown().PyrUp();
                                break;
                            case eMutate.SmoothMedian:
                                imageEgmu = imageEgmu.SmoothMedian((int) value);
                                break;
                            case eMutate.SmoothGaussian:
                                imageEgmu = imageEgmu.SmoothGaussian((int)value);
                                break;
                        }
                        layer.Image = imageEgmu.ToImageSharpL8();
                        imageEgmu.Dispose();
                    });
                    result = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}\nPlease try diferent values for the mutation", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            ShowLayer(ActualLayer);

            menuFileSave.Enabled =
            menuFileSaveAs.Enabled = true;
        }
    }
}
