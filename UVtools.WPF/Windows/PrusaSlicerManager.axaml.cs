using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MessageBox.Avalonia.Enums;
using UVtools.WPF.Controls;
using UVtools.WPF.Extensions;
using UVtools.WPF.Structures;

namespace UVtools.WPF.Windows
{
    public class PrusaSlicerManager : WindowEx
    {
        public PEProfileFolder[] Profiles { get;}

        public PrusaSlicerManager()
        {
            InitializeComponent();
            Profiles = new[]
            {
                new PEProfileFolder(PEProfileFolder.FolderType.Print),
                new PEProfileFolder(PEProfileFolder.FolderType.Printer),
            };
            
            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void RefreshProfiles()
        {
            foreach (var profile in Profiles)
            {
                profile.Reset();
            }
        }

        public async void InstallProfiles()
        {
            var printProfiles = Profiles[0].SelectedFiles;
            var printerProfiles = Profiles[1].SelectedFiles;

            if (string.IsNullOrEmpty(printProfiles) && string.IsNullOrEmpty(printerProfiles))
            {
                await this.MessageBoxError("Select at least one profile to install", "None profile was selected");
                return;
            }

            if (await this.MessageBoxQuestion(
                "This action will install and override the following profiles into PrusaSlicer:\n" +
                "---------- PRINT PROFILES ----------\n" +
                Profiles[0].SelectedFiles +
                "--------- PRINTER PROFILES ---------\n" +
                Profiles[1].SelectedFiles +
                "---------------\n" +
                "Click 'Yes' to continue\n" +
                "Click 'No' to cancel this operation",
                "Install printers into PrusaSlicer") != ButtonResult.Yes) return;

            ushort count = 0;

            try
            {
                foreach (var profile in Profiles)
                {
                    foreach (CheckBox item in profile.Items)
                    {
                        if (!item.IsChecked.HasValue || !item.IsChecked.Value) continue;
                        var fi = item.Tag as FileInfo;
                        fi.CopyTo($"{profile.TargetPath}{Path.DirectorySeparatorChar}{fi.Name}", true);
                        count++;
                    }
                }
            }
            catch (Exception exception)
            {
                await this.MessageBoxError(exception.ToString(), "Unable to install the profiles");
                return;
            }


            await this.MessageBoxInfo(
                $"{count} profiles were installed.\nRestart PrusaSlicer and check if profiles are present.",
                "Operation completed");

            RefreshProfiles();

        }
    }
}
