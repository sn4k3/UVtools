/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Diagnostics;
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

            foreach (var type in Helpers.AvaliableFileFormats)
            {
                if(type == typeof(SL1File)) continue;
                ToolStripMenuItem menuItem = new ToolStripMenuItem(type.Name.Replace("File", string.Empty))
                {
                    Tag = type,
                    Image = Properties.Resources.layers_16x16
                };
                menuItem.Click += ConvertToItemOnClick;
                menuEditConvert.DropDownItems.Add(menuItem);
            }

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
                    openFile.Filter = Program.SL1File.GetFileFilter();
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
                            if (Directory.Exists(folder.SelectedPath))
                            {
                                DirectoryInfo di = new DirectoryInfo(folder.SelectedPath);

                                foreach (FileInfo file in di.GetFiles())
                                {
                                    file.Delete();
                                }
                                foreach (DirectoryInfo dir in di.GetDirectories())
                                {
                                    dir.Delete(true);
                                }
                            }
                            Program.SL1File.Archive.ExtractToDirectory(folder.SelectedPath);
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
            Type type = (Type)menuItem.Tag;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.FileName = Path.GetFileNameWithoutExtension(Program.SL1File.FileFullPath);

                using (FileFormat instance = (FileFormat)Activator.CreateInstance(type)) 
                //using (CbddlpFile file = new CbddlpFile())
                {
                    dialog.Filter = instance.GetFileFilter();
                }


                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    Program.SL1File.Convert(type, dialog.FileName);
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
                if (!Program.SL1File.IsExtensionValid(file, true)) continue;
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
           /* if (!ReferenceEquals(Program.SL1File, null))
            {
                Program.SL1File.Dispose();
            }*/

            Program.SL1File.Decode(fileName);

            pbThumbnail.Image = Program.SL1File.Thumbnails[0].ToBitmap();
            //ShowLayer(0);

            sbLayers.SmallChange = 1;
            sbLayers.Minimum = 0;
            sbLayers.Maximum = (int)Program.SL1File.GetLayerCount-1;
            sbLayers.Value = sbLayers.Maximum;

            sbLayers.Enabled = 
            menuEdit.Enabled = 
            menuEditExtract.Enabled = 
            menuEditConvert.Enabled = true;

            lvProperties.BeginUpdate();
            lvProperties.Items.Clear();

            object[] configs = { Program.SL1File.PrinterSettings, Program.SL1File.MaterialSettings, Program.SL1File.PrintSettings, Program.SL1File.OutputConfigSettings };
            byte configNum = 0;
            foreach (object config in configs)
            {
                foreach (PropertyInfo propertyInfo in config.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    ListViewItem item = new ListViewItem(propertyInfo.Name, lvProperties.Groups[configNum]);
                    object obj = new object();
                    item.SubItems.Add(propertyInfo.GetValue(config)?.ToString());
                    lvProperties.Items.Add(item);
                }

                configNum++;
            }
            lvProperties.EndUpdate();

            statusBar.Items.Clear();

            AddStatusBarItem(nameof(Program.SL1File.OutputConfigSettings.LayerHeight), Program.SL1File.OutputConfigSettings.LayerHeight, "mm");
            AddStatusBarItem(nameof(Program.SL1File.OutputConfigSettings.ExpTimeFirst), Program.SL1File.OutputConfigSettings.ExpTimeFirst);
            AddStatusBarItem(nameof(Program.SL1File.OutputConfigSettings.ExpTime), Program.SL1File.OutputConfigSettings.ExpTime);
            AddStatusBarItem(nameof(Program.SL1File.OutputConfigSettings.PrintTime), Math.Round(Program.SL1File.OutputConfigSettings.PrintTime/ 3600, 2), "h");
            AddStatusBarItem(nameof(Program.SL1File.OutputConfigSettings.UsedMaterial), Math.Round(Program.SL1File.OutputConfigSettings.UsedMaterial, 2), "ml");
            AddStatusBarItem(nameof(Program.SL1File.OutputConfigSettings.MaterialName), Program.SL1File.OutputConfigSettings.MaterialName);
            AddStatusBarItem(nameof(Program.SL1File.OutputConfigSettings.PrinterProfile), Program.SL1File.OutputConfigSettings.PrinterProfile);

            Text = $"{FrmAbout.AssemblyTitle}   Version: {FrmAbout.AssemblyVersion}   File: {Path.GetFileName(fileName)}";
        }

        void ShowLayer(uint layerNum)
        {
            //if(!ReferenceEquals(pbLayer.Image, null))
            //    pbLayer.Image.Dispose(); SLOW! LET GC DO IT
            //pbLayer.Image = Image.FromStream(Program.SL1File.LayerImages[layerNum].Open());
            //pbLayer.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
            var image = Program.SL1File.GetLayerImage(layerNum);
            image.Mutate(x => x.Rotate(RotateMode.Rotate90));
            pbLayer.Image = image.ToBitmap();

            byte percent = (byte)((layerNum + 1) * 100 / Program.SL1File.GetLayerCount);


            lbLayers.Text = $"{Program.SL1File.TotalHeight}mm\n{layerNum+1} / {Program.SL1File.GetLayerCount}\n{Program.SL1File.GetHeightFromLayer((uint)layerNum+1)}mm\n{percent}%";
            pbLayers.Value = percent;
        }

        void AddStatusBarItem(string name, object item, string extraText = "")
        {
            if (statusBar.Items.Count > 0)
                statusBar.Items.Add(new ToolStripSeparator());

            ToolStripLabel label = new ToolStripLabel($"{name}: {item.ToString()}{extraText}");
            statusBar.Items.Add(label);
        }
        #endregion
    }
}
