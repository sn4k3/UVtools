using System.Collections.Generic;
using Avalonia.Platform.Storage;
using UVtools.Core.MeshFormats;
using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolLayerExportMeshControl : ToolControl
{
    public OperationLayerExportMesh Operation => BaseOperation as OperationLayerExportMesh;
    public ToolLayerExportMeshControl()
    {
        BaseOperation = new OperationLayerExportMesh(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    public async void ChooseFilePath()
    {
        using var file = await App.MainWindow.SaveFilePickerAsync(SlicerFile, GetFilters());
        if (file?.TryGetLocalPath() is not { } filePath) return;
        
        Operation.FilePath = filePath;
    }

    public List<FilePickerFileType> GetFilters()
    {
        var list = new List<FilePickerFileType>();
        foreach (var fileExtension in MeshFile.AvailableMeshFiles)
        {
            list.Add(AvaloniaStatic.CreateFilePickerFileType(fileExtension.Description, fileExtension.Extension));
        }

        return list;
    }
}