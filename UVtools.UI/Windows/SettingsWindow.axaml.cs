using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using UVtools.Core;
using UVtools.Core.Dialogs;
using UVtools.Core.FileFormats;
using UVtools.Core.Network;
using UVtools.Core.Objects;
using UVtools.UI.Controls;
using UVtools.UI.Extensions;
using ZLinq;

namespace UVtools.UI.Windows;

public partial class SettingsWindow : WindowEx
{
    private double _scrollViewerMaxHeight;
    private int _selectedTabIndex;

    public UserSettings SettingsBackup { get; }

    public string[] FileOpenDialogFilters { get; }
    public string[] ZoomRanges { get; }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => RaiseAndSetIfChanged(ref _selectedTabIndex, value);
    }

    public double ScrollViewerMaxHeight
    {
        get => _scrollViewerMaxHeight;
        set => RaiseAndSetIfChanged(ref _scrollViewerMaxHeight, value);
    }

    public SettingsWindow()
    {
        Title += $" [v{About.VersionString}]";
        SettingsBackup = UserSettings.Instance.Clone();

        var fileFormats = new List<string>
        {
            "All slicer files"
        };
        fileFormats.AddRange(FileFormat.AvailableFormats
            .SelectMany(fileFormat => fileFormat.FileExtensions, (fileFormat, extension) => new { fileFormat = fileFormat, extension })
            .Where(obj => obj.extension.IsVisibleOnFileFilters)
            .Select(obj => $"{obj.extension.Description} (.{obj.extension.Extension})"));
        FileOpenDialogFilters = fileFormats.ToArray();


        // Derive strings for the zoom lock and crosshair fade combo-boxes from the
        // ZoomLevels constant array, and add those strings to the comboboxes.
        ZoomRanges = AppSettings.ZoomLevels.AsValueEnumerable().Skip(AppSettings.ZoomLevelSkipCount).Select(
            s => Convert.ToString(s / 100, CultureInfo.InvariantCulture) + "x").ToArray();

        ScrollViewerMaxHeight = this.GetScreenWorkingArea().Height - Settings.General.WindowsVerticalMargin;

        DataContext = this;
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (DialogResult != DialogResults.OK)
        {
            UserSettings.Instance = SettingsBackup;
        }
        else
        {
            UserSettings.Save();
        }
    }

    public void SetMaxDegreeOfParallelism(object toObject)
    {
        var to = toObject.ToString()!;
        if (to == "*")
        {
            Settings.General.MaxDegreeOfParallelism = Environment.ProcessorCount;
            return;
        }

        if (to == "!")
        {
            Settings.General.MaxDegreeOfParallelism = CoreSettings.OptimalMaxDegreeOfParallelism;
            return;
        }

        if (int.TryParse(to, out var i))
        {
            Settings.General.MaxDegreeOfParallelism = i;
            return;
        }

        if (decimal.TryParse(to, out var d))
        {
            Settings.General.MaxDegreeOfParallelism = (int)(Environment.ProcessorCount * d);
            return;
        }
    }


    public async Task GeneralOpenFolderField(object fieldObj)
    {
        var field = fieldObj.ToString()!;
        foreach (var propertyInfo in Settings.General.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (propertyInfo.Name != field) continue;
            var folders = await OpenFolderPickerAsync();
            if (folders.Count == 0) return;
            propertyInfo.SetValue(Settings.General, folders[0].TryGetLocalPath());
            return;
        }

    }

    public void GeneralClearField(object fieldObj)
    {
        var field = fieldObj.ToString()!;
        var properties = Settings.General.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var propertyInfo in properties)
        {
            if (propertyInfo.Name != field) continue;
            propertyInfo.SetValue(Settings.General, null);
            return;
        }

    }

    public async Task AutomationsOpenFileField(object fieldObj)
    {
        var field = fieldObj.ToString()!;
        foreach (var propertyInfo in Settings.Automations.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (propertyInfo.Name != field) continue;
            var folders = await OpenFilePickerAsync(AvaloniaStatic.ScriptsFileFilter);
            if (folders.Count == 0) return;
            propertyInfo.SetValue(Settings.Automations, folders[0].TryGetLocalPath());
            return;
        }

    }


    public void AutomationsClearField(object fieldObj)
    {
        var field = fieldObj.ToString()!;
        var properties = Settings.Automations.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var propertyInfo in properties)
        {
            if (propertyInfo.Name != field) continue;
            propertyInfo.SetValue(Settings.Automations, null);
            return;
        }
    }

    public void PlayBeep()
    {
        App.BeepIfAble();
    }

    public async Task SendToAddCustomLocation()
    {
        var folders = await OpenFolderPickerAsync();
        if (folders.Count == 0) return;

        var directory = new MappedDevice(folders[0].TryGetLocalPath()!);
        if (Settings.General.SendToCustomLocations.Contains(directory))
        {
            await this.MessageBoxError("The selected location already exists on the list:\n" +
                                       $"{folders[0].TryGetLocalPath()}");
            return;
        }

        Settings.General.SendToCustomLocations.Add(directory);
    }

    public async Task SendToRemoveCustomLocations()
    {
        if (SendToCustomLocationsGrid.SelectedItems.Count == 0) return;

        if (await this.MessageBoxQuestion(
                $"Are you sure you want to remove the {SendToCustomLocationsGrid.SelectedItems.Count} selected entries?") !=
            MessageButtonResult.Yes) return;

        Settings.General.SendToCustomLocations.RemoveRange(SendToCustomLocationsGrid.SelectedItems.Cast<MappedDevice>());
    }

    public async Task SendToAddProcess()
    {
        var files = await OpenFilePickerAsync();
        if (files.Count == 0) return;
        var file = new MappedProcess(files[0].TryGetLocalPath()!);
        if (Settings.General.SendToProcess.Contains(file))
        {
            await this.MessageBoxError("The selected process already exists on the list:\n" +
                                       $"{files[0].TryGetLocalPath()}");
            return;
        }

        Settings.General.SendToProcess.Add(file);
    }

    public async Task SendToRemoveProcess()
    {
        if (SendToProcessGrid.SelectedItems.Count == 0) return;

        if (await this.MessageBoxQuestion(
                $"Are you sure you want to remove the {SendToProcessGrid.SelectedItems.Count} selected entries?") !=
            MessageButtonResult.Yes) return;

        Settings.General.SendToProcess.RemoveRange(SendToProcessGrid.SelectedItems.Cast<MappedProcess>());
    }

    public async Task AddNetworkRemotePrinter()
    {
        var result = await this.MessageBoxQuestion("Are you sure you want to add a new remote printer", "Add new remote printer?");
        if (result != MessageButtonResult.Yes) return;

        var remotePrinter = new RemotePrinter
        {
            Name = "My new remote printer"
        };

        Settings.Network.RemotePrinters.Add(remotePrinter);
        NetworkRemotePrinterComboBox.SelectedItem = remotePrinter;
    }

    public async Task RemoveSelectedNetworkRemotePrinter()
    {
        if (NetworkRemotePrinterComboBox.SelectedItem is not RemotePrinter remotePrinter) return;
        var result = await this.MessageBoxQuestion("Are you sure you want to remove the following remote printer?\n" +
                                                   remotePrinter, "Remove remote printer?");
        if (result != MessageButtonResult.Yes) return;
        Settings.Network.RemotePrinters.Remove(remotePrinter);
    }

    public async Task DuplicateSelectedNetworkRemotePrinter()
    {
        if (NetworkRemotePrinterComboBox.SelectedItem is not RemotePrinter remotePrinter) return;
        var result = await this.MessageBoxQuestion("Are you sure you want to duplicate the following remote printer?\n" +
                                                   remotePrinter, "Duplicate remote printer?");
        if (result != MessageButtonResult.Yes) return;
        var clone = remotePrinter.Clone();
        clone.Name += " Duplicated";
        Settings.Network.RemotePrinters.Add(clone);
        NetworkRemotePrinterComboBox.SelectedItem = clone;
    }

    public async Task OnClickResetAllDefaults()
    {
        var result = await this.MessageBoxQuestion("Are you sure you want to reset all settings to the default values?", "Reset settings?");
        if (result != MessageButtonResult.Yes) return;
        UserSettings.Reset();
        ResetDataContext();
    }

    public void OnClickSave()
    {
        DialogResult = DialogResults.OK;
        CloseWithResult();
    }
}