using Avalonia.Markup.Xaml;
using System.Linq;
using UVtools.Core.FileFormats;
using UVtools.WPF.Controls;

namespace UVtools.WPF.Windows;

public partial class VersionSelectorWindow : WindowEx
{
    private uint _version;

    public string DescriptionText =>
        $"This file format \"{FileExtension.Description}\" contains multiple available versions. Some versions may require a specific firmware version in order to run.\n" +
        "Select the version you wish to use on the output file.\n" +
        $"If unsure, use the default version {SlicerFile.DefaultVersion}.";

    public sealed override FileFormat SlicerFile { get; set; }

    public FileExtension FileExtension { get; init; }

    public uint[] AvailableVersions { get; init; }

    public uint Version
    {
        get => _version;
        set => RaiseAndSetIfChanged(ref _version, value);
    }

    public VersionSelectorWindow()
    {
        InitializeComponent();
        DialogResult = DialogResults.Cancel;
    }

    public VersionSelectorWindow(FileFormat slicerFile, FileExtension fileExtension, uint[] availableVersions) : this()
    {
        SlicerFile = slicerFile;
        FileExtension = fileExtension;
        Version = availableVersions.Contains(slicerFile.DefaultVersion) ? SlicerFile.DefaultVersion : availableVersions[^1];
        AvailableVersions = availableVersions;
        Title += $" - {FileExtension.Description}";
        DataContext = this;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void SelectVersion()
    {
        DialogResult = DialogResults.OK;
        Close();
    }

    public void SelectDefault()
    {
        Version = AvailableVersions.Contains(SlicerFile.DefaultVersion) ? SlicerFile.DefaultVersion : AvailableVersions[^1];
        DialogResult = DialogResults.OK;
        Close();
    }
}