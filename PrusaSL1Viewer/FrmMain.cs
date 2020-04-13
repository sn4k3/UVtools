/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using PrusaSL1Reader;
using SixLabors.ImageSharp.Processing;

namespace PrusaSL1Viewer
{
    public partial class FrmMain : Form
    {
        #region Constructors
        public FrmMain()
        {
            InitializeComponent();
            Text = $"{FrmAbout.AssemblyTitle}   Version: {FrmAbout.AssemblyVersion}";

            DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            DragDrop += (s, e) => { ProcessFile((string[])e.Data.GetData(DataFormats.FileDrop)); };

            ProcessFile(Environment.GetCommandLineArgs());
        }

        #endregion

        #region Events
        private void sbLayers_ValueChanged(object sender, EventArgs e)
        {
            ShowLayer((uint)(sbLayers.Maximum - sbLayers.Value));
        }
        
        private void MenuItemClicked(object sender, EventArgs e)
        {
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
                            MessageBox.Show(exception.ToString(), "Error while try opening the file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
                return;
            }

            if (ReferenceEquals(sender, menuFileExit))
            {
                Application.Exit();
                return;
            }

            if (ReferenceEquals(sender, menuEditExtract))
            {
                using (FolderBrowserDialog folder = new FolderBrowserDialog())
                {
                    if (folder.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            Program.SlicerFile.Extract(folder.SelectedPath);
                            if (MessageBox.Show(
                                $"Extraction was successful, browser folder to see it contents.\n{folder.SelectedPath}\nPress 'Yes' if you want open the target folder, otherwise select 'No' to continue.",
                                "Extraction completed", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                Process.Start(folder.SelectedPath);
                            }
                        }
                        catch (Exception exception)
                        {
                            MessageBox.Show(exception.ToString(), "Error while try extracting the file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        
                    }
                }
                return;
            }

            if (ReferenceEquals(sender, menuAboutAbout))
            {
                Program.FrmAbout.ShowDialog();
            }
            if (ReferenceEquals(sender, menuAboutWebsite))
            {
                Process.Start(About.Website);
                return;
            }

            if (ReferenceEquals(sender, menuAboutDonate))
            {
                MessageBox.Show("All my work here is given for free (OpenSource), it took some hours to build, test and polish the program.\n" +
                                "If you're happy to contribute for a better program and for my work i will appreciate the tip.\n" +
                                "A browser window will be open and forward to my paypal address after you click 'OK'.\nHappy Printing!", "Donation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Process.Start(About.Donate);
                return;
            }
        }

        private void ConvertToItemOnClick(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            FileFormat fileFormat = (FileFormat)menuItem.Tag;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.FileName = Path.GetFileNameWithoutExtension(Program.SlicerFile.FileFullPath);

                //using (FileFormat instance = (FileFormat)Activator.CreateInstance(type)) 
                //using (CbddlpFile file = new CbddlpFile())
                {
                    dialog.Filter = fileFormat.GetFileFilter();
                }


                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    Program.SlicerFile.Convert(fileFormat, dialog.FileName);
                }

            }
        }
        #endregion

        #region Methods
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
            Program.SlicerFile?.Dispose();
            Program.SlicerFile = FileFormat.FindByExtension(fileName, true, true);
            if (ReferenceEquals(Program.SlicerFile, null)) return;
            Program.SlicerFile.Decode(fileName);

            menuEditConvert.DropDownItems.Clear();
            foreach (var fileFormat in FileFormat.AvaliableFormats)
            {
                if (fileFormat.GetType() == Program.SlicerFile.GetType()) continue;
                ToolStripMenuItem menuItem = new ToolStripMenuItem(fileFormat.GetType().Name.Replace("File", string.Empty))
                {
                    Tag = fileFormat,
                    Image = Properties.Resources.layers_16x16
                };
                menuItem.Click += ConvertToItemOnClick;
                menuEditConvert.DropDownItems.Add(menuItem);
            }

            pbThumbnail.Image = Program.SlicerFile.Thumbnails[0]?.ToBitmap();
            //ShowLayer(0);

            sbLayers.SmallChange = 1;
            sbLayers.Minimum = 0;
            sbLayers.Maximum = (int)Program.SlicerFile.LayerCount-1;
            sbLayers.Value = sbLayers.Maximum;

            sbLayers.Enabled = 
            menuEdit.Enabled = 
            menuEditExtract.Enabled = 
            menuEditConvert.Enabled = true;

            lvProperties.BeginUpdate();
            lvProperties.Items.Clear();

            lvProperties.Groups.Clear();

            foreach (object config in Program.SlicerFile.Configs)
            {
                ListViewGroup group = new ListViewGroup(config.GetType().Name);
                lvProperties.Groups.Add(group);
                foreach (PropertyInfo propertyInfo in config.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    ListViewItem item = new ListViewItem(propertyInfo.Name, group);
                    object obj = new object();
                    item.SubItems.Add(propertyInfo.GetValue(config)?.ToString());
                    lvProperties.Items.Add(item);
                }
            }
            lvProperties.EndUpdate();

            statusBar.Items.Clear();

            AddStatusBarItem(nameof(Program.SlicerFile.LayerHeight), Program.SlicerFile.LayerHeight, "mm");
            AddStatusBarItem(nameof(Program.SlicerFile.InitialExposureTime), Program.SlicerFile.InitialExposureTime, "s");
            AddStatusBarItem(nameof(Program.SlicerFile.LayerExposureTime), Program.SlicerFile.LayerExposureTime, "s");
            AddStatusBarItem(nameof(Program.SlicerFile.PrintTime), Math.Round(Program.SlicerFile.PrintTime / 3600, 2), "h");
            AddStatusBarItem(nameof(Program.SlicerFile.UsedMaterial), Math.Round(Program.SlicerFile.UsedMaterial, 2), "ml");
            AddStatusBarItem(nameof(Program.SlicerFile.MaterialCost), Program.SlicerFile.MaterialCost, "€");
            AddStatusBarItem(nameof(Program.SlicerFile.MaterialName), Program.SlicerFile.MaterialName);
            AddStatusBarItem(nameof(Program.SlicerFile.MachineName), Program.SlicerFile.MachineName);

            Text = $"{FrmAbout.AssemblyTitle}   Version: {FrmAbout.AssemblyVersion}   File: {Path.GetFileName(fileName)}";
        }

        void ShowLayer(uint layerNum)
        {
            //if(!ReferenceEquals(pbLayer.Image, null))
            //    pbLayer.Image.Dispose(); SLOW! LET GC DO IT
            //pbLayer.Image = Image.FromStream(Program.SlicerFile.LayerImages[layerNum].Open());
            //pbLayer.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
            //Stopwatch watch = Stopwatch.StartNew();
            var image = Program.SlicerFile.GetLayerImage(layerNum);
            //Debug.Write(watch.ElapsedMilliseconds.ToString());
            if (menuViewRotateImage.Checked)
            {
                //watch.Restart();
                image.Mutate(x => x.Rotate(RotateMode.Rotate90));
                //Debug.Write($"/{watch.ElapsedMilliseconds}");
            }

            //watch.Restart();
            pbLayer.Image = image.ToBitmap();
            //Debug.WriteLine($"/{watch.ElapsedMilliseconds}");

            //UniversalLayer layer = new UniversalLayer(image);
            //pbLayer.Image = layer.ToBitmap(image.Width, image.Height);


            byte percent = (byte)((layerNum + 1) * 100 / Program.SlicerFile.LayerCount);


            lbLayers.Text = $"{Program.SlicerFile.TotalHeight}mm\n{layerNum+1} / {Program.SlicerFile.LayerCount}\n{Program.SlicerFile.GetHeightFromLayer((uint)layerNum+1)}mm\n{percent}%";
            pbLayers.Value = percent;

        }

        void AddStatusBarItem(string name, object item, string extraText = "")
        {
            if (statusBar.Items.Count > 0)
                statusBar.Items.Add(new ToolStripSeparator());

            ToolStripLabel label = new ToolStripLabel($"{name}: {item}{extraText}");
            statusBar.Items.Add(label);
        }
        #endregion
    }
}
