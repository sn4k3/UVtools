using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.MeshFormats;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools;

public partial class ToolLayerExportMeshControl : ToolControl
{
    public OperationLayerExportMesh Operation => BaseOperation as OperationLayerExportMesh;
    public ToolLayerExportMeshControl()
    {
        BaseOperation = new OperationLayerExportMesh(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public async void ChooseFilePath()
    {
        var dialog = new SaveFileDialog
        {
            Directory = Path.GetDirectoryName(SlicerFile.FileFullPath),
            DefaultExtension = ".stl",
            InitialFileName = SlicerFile.FilenameNoExt,
            Filters = GetFilters(),
        };
        var filePath = await dialog.ShowAsync(ParentWindow);
        if (string.IsNullOrWhiteSpace(filePath)) return;
        Operation.FilePath = filePath;
    }

    public List<FileDialogFilter> GetFilters()
    {
        var list = new List<FileDialogFilter>();
        foreach (var fileExtension in MeshFile.AvailableMeshFiles)
        {
            list.Add(new FileDialogFilter
            {
                Name = fileExtension.Description,
                Extensions = new List<string>{fileExtension.Extension}
            });
        }

        return list;
    }
}