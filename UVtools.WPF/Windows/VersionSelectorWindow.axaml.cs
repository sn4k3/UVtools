using Avalonia.Markup.Xaml;
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

    public FileExtension FileExtension { get; set; }

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

    public VersionSelectorWindow(FileFormat slicerFile, FileExtension fileExtension) : this()
    {
        SlicerFile = slicerFile;
        FileExtension = fileExtension;
        Version = slicerFile.DefaultVersion;
        Title += $" - {FileExtension.Description}";
        DataContext = this;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void SelectVersion()
    {
        DialogResult = Version == SlicerFile.DefaultVersion ? DialogResults.Unknown : DialogResults.OK;
        Close();
    }

    public void SelectDefault()
    {
        DialogResult = DialogResults.Unknown;
        Close();
    }
}