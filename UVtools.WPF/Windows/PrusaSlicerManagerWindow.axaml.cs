using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MessageBox.Avalonia.Enums;
using UVtools.WPF.Controls;
using UVtools.WPF.Extensions;
using UVtools.WPF.Structures;

namespace UVtools.WPF.Windows;

public class PrusaSlicerManagerWindow : WindowEx
{
    public PSProfileFolder[] PrusaSlicerProfiles { get; } = {
        new (PSProfileFolder.FolderType.Print),
        new (PSProfileFolder.FolderType.Printer),
    };
    public PSProfileFolder[] SuperSlicerProfiles { get; } = {
        new (PSProfileFolder.FolderType.Print, true),
        new (PSProfileFolder.FolderType.Printer, true),
    };

    public bool HavePrusaSlicer
    {
        get
        {
            var PSFolder = App.GetPrusaSlicerDirectory();
            return !string.IsNullOrEmpty(PSFolder) && Directory.Exists(PSFolder);
        }
    }

    public bool HaveSuperSlicer
    {
        get
        {
            var SSFolder = App.GetPrusaSlicerDirectory(true);
            return !string.IsNullOrEmpty(SSFolder) && Directory.Exists(SSFolder);
        }
    }

    public int TabSlicerSelectedIndex { get; set; }

    public PrusaSlicerManagerWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void RefreshProfiles()
    {
        foreach (var profile in PrusaSlicerProfiles)
        {
            profile.Reset();
        }

        foreach (var profile in SuperSlicerProfiles)
        {
            profile.Reset();
        }
    }

    public async void InstallProfiles()
    {
        bool isSuperSlicer = TabSlicerSelectedIndex == 1;
        var printProfiles = isSuperSlicer ? SuperSlicerProfiles[0].SelectedFiles : PrusaSlicerProfiles[0].SelectedFiles;
        var printerProfiles = isSuperSlicer ? SuperSlicerProfiles[1].SelectedFiles : PrusaSlicerProfiles[1].SelectedFiles;
        var slicerName = isSuperSlicer ? "SuperSlicer" : "PrusaSlicer";

        if (string.IsNullOrEmpty(printProfiles) && string.IsNullOrEmpty(printerProfiles))
        {
            await this.MessageBoxError("Select at least one profile to install", "None profile was selected");
            return;
        }

        if (await this.MessageBoxQuestion(
                $"This action will install and override the following profiles into {slicerName}:\n" +
                "---------- PRINT PROFILES ----------\n" +
                printProfiles +
                "--------- PRINTER PROFILES ---------\n" +
                printerProfiles +
                "---------------\n" +
                "Click 'Yes' to continue\n" +
                "Click 'No' to cancel this operation",
                $"Install printers into {slicerName}") != ButtonResult.Yes) return;

        ushort count = 0;

        try
        {
            foreach (var profile in isSuperSlicer ? SuperSlicerProfiles : PrusaSlicerProfiles)
            {
                foreach (var item in profile.Items)
                {
                    if (!item.IsChecked.HasValue || !item.IsChecked.Value) continue;
                    var fi = item.Tag as FileInfo;
                    if (fi is null) continue;
                    fi.CopyTo(Path.Combine(profile.TargetPath, fi.Name), true);
                    count++;
                }
            }
        }
        catch (Exception exception)
        {
            await this.MessageBoxError(exception.Message, "Unable to install the profiles");
            return;
        }


        await this.MessageBoxInfo(
            $"{count} profiles were installed.\nRestart {slicerName} and check if profiles are present.",
            "Operation completed");

        RefreshProfiles();

    }
}