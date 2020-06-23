/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace UVtools.GUI.Forms
{
    public partial class FrmInstallPEProfiles : Form
    {
        private PEProfileFolder[] Profiles { get; }
        public FrmInstallPEProfiles()
        {
            InitializeComponent();
            
            Profiles = new[]
            {
                new PEProfileFolder(PEProfileFolder.FolderType.Print, lvPrintProfiles, lbPrintProfilesCount),
                new PEProfileFolder(PEProfileFolder.FolderType.Printer, lvPrinterProfiles, lbPrinterProfilesCount)
            };

            Init();
        }

        private void Init()
        {
            foreach (var profile in Profiles)
            {
                DirectoryInfo di = new DirectoryInfo(profile.SourcePath);
                var files = di.GetFiles("*.ini");

                byte installedCount = 0;
                byte updateCount = 0;

                profile.ListView.BeginUpdate();
                profile.ListView.Items.Clear();
                foreach (var fileInfo in files)
                {
                    ListViewItem item = new ListViewItem
                    {
                        Text = Path.GetFileNameWithoutExtension(fileInfo.Name),
                        Tag = fileInfo
                    };

                    var targetFile = $"{profile.TargetPath}{Path.DirectorySeparatorChar}{fileInfo.Name}";
                    FileInfo targetFileInfo = new FileInfo(targetFile);
                    if (targetFileInfo.Exists)
                    {
                        installedCount++;
                        if (targetFileInfo.Length != fileInfo.Length || targetFileInfo.LastWriteTime != fileInfo.LastWriteTime)
                        {
                            item.ForeColor = Color.Red;
                            item.Checked = true;
                            updateCount++;
                        }
                        else
                        {
                            item.ForeColor = Color.Green;
                        }
                    }
                    else if (ReferenceEquals(profile.ListView, lvPrintProfiles))
                    {
                        item.Checked = true;
                    }

                    profile.ListView.Items.Add(item);
                }
                profile.ListView.EndUpdate();
                profile.LabelCount.Text = $"{updateCount} Update(s) | {installedCount} Installed | {profile.ListView.Items.Count} Profiles";
            }
        }

        #region Events
        private void EventClick(object sender, EventArgs e)
        {

            if (ReferenceEquals(sender, btnRefreshProfiles))
            {
                Init();
                return;
            }

            if (ReferenceEquals(sender, btnPrintProfilesSelectAll) ||
                ReferenceEquals(sender, btnPrintProfilesUnselectAll) ||
                ReferenceEquals(sender, btnPrinterProfilesSelectAll) ||
                ReferenceEquals(sender, btnPrinterProfilesUnselectAll))
            {
                bool isChecked = ReferenceEquals(sender, btnPrintProfilesSelectAll) ||
                              ReferenceEquals(sender, btnPrinterProfilesSelectAll);

                ListView lv = ReferenceEquals(sender, btnPrintProfilesSelectAll) || ReferenceEquals(sender,
                    btnPrintProfilesUnselectAll) ? lvPrintProfiles : lvPrinterProfiles;

                lv.BeginUpdate();
                foreach (ListViewItem item in lv.Items)
                {
                    item.Checked = isChecked;
                }
                lv.EndUpdate();

                return;
            }


            if (ReferenceEquals(sender, btnInstall))
            {

                var result = MessageBox.Show(
                    "This action will install and override the following profiles into PrusaSlicer:\n" +
                    "---------- PRINT PROFILES ----------\n" +
                    Profiles[0].SelectedFiles +
                    "--------- PRINTER PROFILES ---------\n" +
                    Profiles[1].SelectedFiles +
                    "---------------\n" +
                    "Click 'Yes' to continue\n" +
                    "Click 'No' to cancel this operation",
                    "Install printers into PrusaSlicer", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);


                if (result != DialogResult.Yes)
                {
                    return;
                }

                ushort count = 0;

                try
                {
                    foreach (var profile in Profiles)
                    {
                        foreach (ListViewItem item in profile.ListView.Items)
                        {
                            if (!item.Checked) continue;
                            var fi = item.Tag as FileInfo;
                            fi.CopyTo($"{profile.TargetPath}{Path.DirectorySeparatorChar}{fi.Name}", true);
                            count++;
                        }
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Unable to install the profiles", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    DialogResult = DialogResult.Abort;

                    return;
                }
                

                DialogResult = DialogResult.OK;

                MessageBox.Show(
                    $"Profiles ({count}) were installed.\nRestart PrusaSlicer and check if profiles are present.",
                    "Operation Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;
            }
        }
        #endregion
    }
}
