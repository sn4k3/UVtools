using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MessageBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UVtools.Core;
using UVtools.Core.FileFormats;
using UVtools.Core.Network;
using UVtools.Core.Objects;
using UVtools.WPF.Controls;
using UVtools.WPF.Extensions;
using Color = UVtools.WPF.Structures.Color;

namespace UVtools.WPF.Windows;

public class SettingsWindow : WindowEx
{
    private double _scrollViewerMaxHeight;
    private int _selectedTabIndex;
    private readonly DataGrid _sendToCustomLocationsGrid;
    private readonly DataGrid _sendToProcessGrid;
    private readonly ComboBox _networkRemotePrinterComboBox;

    public int MaxProcessorCount => Environment.ProcessorCount;

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
        Title += $" [v{About.VersionStr}]";
        SettingsBackup = UserSettings.Instance.Clone();

        var fileFormats = new List<string>
        {
            "All slicer files"
        };
        fileFormats.AddRange(from format in FileFormat.AvailableFormats from extension in format.FileExtensions where extension.IsVisibleOnFileFilters select $"{extension.Description} (.{extension.Extension})");
        FileOpenDialogFilters = fileFormats.ToArray();


        // Derive strings for the zoom lock and crosshair fade combo-boxes from the
        // ZoomLevels constant array, and add those strings to the comboboxes.
        ZoomRanges = AppSettings.ZoomLevels.Skip(AppSettings.ZoomLevelSkipCount).Select(
            s => Convert.ToString(s / 100, CultureInfo.InvariantCulture) + "x").ToArray();

        ScrollViewerMaxHeight = this.GetScreenWorkingArea().Height - Settings.General.WindowsVerticalMargin;

        DataContext = this;
        InitializeComponent();

        _sendToCustomLocationsGrid = this.FindControl<DataGrid>("SendToCustomLocationsGrid");
        _sendToProcessGrid = this.FindControl<DataGrid>("SendToProcessGrid");
        _networkRemotePrinterComboBox = this.FindControl<ComboBox>("NetworkRemotePrinterComboBox");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
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

    public void SetMaxDegreeOfParallelism(string to)
    {
        if (to == "*")
        {
            Settings.General.MaxDegreeOfParallelism = MaxProcessorCount;
            return;
        }

        if (to == "!")
        {
            Settings.General.MaxDegreeOfParallelism = Math.Max(1, MaxProcessorCount-2);
            return;
        }

        if (int.TryParse(to, out var i))
        {
            Settings.General.MaxDegreeOfParallelism = i;
            return;
        }

        if (decimal.TryParse(to, out var d))
        {
            Settings.General.MaxDegreeOfParallelism = (int)(MaxProcessorCount * d);
            return;
        }
    }


    public async void GeneralOpenFolderField(string field)
    {
        foreach (var propertyInfo in Settings.General.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (propertyInfo.Name != field) continue;
            var dialog = new OpenFolderDialog();
            var filename = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(filename)) return;
            propertyInfo.SetValue(Settings.General, filename);
            return;
        }

    }


    public void GeneralClearField(string field)
    {
        var properties = Settings.General.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var propertyInfo in properties)
        {
            if (propertyInfo.Name != field) continue;
            propertyInfo.SetValue(Settings.General, null);
            return;
        }
            
    }

    public async void SendToAddCustomLocation()
    {
        var openFolder = new OpenFolderDialog();
        var result = await openFolder.ShowAsync(this);

        if (string.IsNullOrWhiteSpace(result)) return;
        var directory = new MappedDevice(result);
        if (Settings.General.SendToCustomLocations.Contains(directory))
        {
            await this.MessageBoxError("The selected location already exists on the list:\n" +
                                       $"{result}");
            return;
        }

        Settings.General.SendToCustomLocations.Add(directory);
    }

    public async void SendToRemoveCustomLocations()
    {
        if (_sendToCustomLocationsGrid.SelectedItems.Count == 0) return;

        if (await this.MessageBoxQuestion(
                $"Are you sure you want to remove the {_sendToCustomLocationsGrid.SelectedItems.Count} selected entries?") !=
            ButtonResult.Yes) return;

        Settings.General.SendToCustomLocations.RemoveRange(_sendToCustomLocationsGrid.SelectedItems.Cast<MappedDevice>());
    }

    public async void SendToAddProcess()
    {
        var openFolder = new OpenFileDialog();
        var result = await openFolder.ShowAsync(this);

        if (result is null || result.Length == 0) return;
        var file = new MappedProcess(result[0]);
        if (Settings.General.SendToProcess.Contains(file))
        {
            await this.MessageBoxError("The selected process already exists on the list:\n" +
                                       $"{result}");
            return;
        }

        Settings.General.SendToProcess.Add(file);
    }

    public async void SendToRemoveProcess()
    {
        if (_sendToProcessGrid.SelectedItems.Count == 0) return;

        if (await this.MessageBoxQuestion(
                $"Are you sure you want to remove the {_sendToProcessGrid.SelectedItems.Count} selected entries?") !=
            ButtonResult.Yes) return;

        Settings.General.SendToProcess.RemoveRange(_sendToProcessGrid.SelectedItems.Cast<MappedProcess>());
    }

    public async void SelectColor(string property)
    {
        foreach (var packObject in UserSettings.PackObjects)
        {
            var properties = packObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.Name != property) continue;
                var color = (Color)propertyInfo.GetValue(packObject, null) ?? new Color(255,255,255,255);
                var window = new ColorPickerWindow(color.ToAvalonia());
                var result = await window.ShowDialog<DialogResults>(this);
                if (result != DialogResults.OK) return;
                propertyInfo.SetValue(packObject, new Color(window.ResultColor));
                return;
            }
        }
    }

    public async void AddNetworkRemotePrinter()
    {
        var result = await this.MessageBoxQuestion("Are you sure you want to add a new remote printer", "Add new remote printer?");
        if (result != ButtonResult.Yes) return;

        var remotePrinter = new RemotePrinter
        {
            Name = "My new remote printer"
        };

        Settings.Network.RemotePrinters.Add(remotePrinter);
        _networkRemotePrinterComboBox.SelectedItem = remotePrinter;
    }

    public async void RemoveSelectedNetworkRemotePrinter()
    {
        if (_networkRemotePrinterComboBox.SelectedItem is not RemotePrinter remotePrinter) return;
        var result = await this.MessageBoxQuestion("Are you sure you want to remove the following remote printer?\n" +
                                                   remotePrinter, "Remove remote printer?");
        if (result != ButtonResult.Yes) return;
        Settings.Network.RemotePrinters.Remove(remotePrinter);
    }

    public async void DuplicateSelectedNetworkRemotePrinter()
    {
        if (_networkRemotePrinterComboBox.SelectedItem is not RemotePrinter remotePrinter) return;
        var result = await this.MessageBoxQuestion("Are you sure you want to duplicate the following remote printer?\n" +
                                                   remotePrinter, "Duplicate remote printer?");
        if (result != ButtonResult.Yes) return;
        var clone = remotePrinter.Clone();
        clone.Name += " Duplicated";
        Settings.Network.RemotePrinters.Add(clone);
        _networkRemotePrinterComboBox.SelectedItem = clone;
    }

    public async void OnClickResetAllDefaults()
    {
        var result = await this.MessageBoxQuestion("Are you sure you want to reset all settings to the default values?", "Reset settings?");
        if (result != ButtonResult.Yes) return;
        UserSettings.Reset();
        ResetDataContext();
    }

    public void OnClickSave()
    {
        DialogResult = DialogResults.OK;
        CloseWithResult();
    }
}