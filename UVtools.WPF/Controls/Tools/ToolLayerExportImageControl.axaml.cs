using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.IO;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools;

public partial class ToolLayerExportImageControl : ToolControl
{
    public OperationLayerExportImage Operation => BaseOperation as OperationLayerExportImage;
    public ToolLayerExportImageControl()
    {
        BaseOperation = new OperationLayerExportImage(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public async void ChooseFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Directory = Path.GetDirectoryName(SlicerFile.FileFullPath),
        };
        var folder = await dialog.ShowAsync(ParentWindow);
        if (string.IsNullOrWhiteSpace(folder)) return;
        Operation.OutputFolder = folder;
    }
}