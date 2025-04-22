using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;
using UVtools.UI.Windows;

namespace UVtools.UI.Controls.Tools;

public partial class ToolRedrawModelControl : ToolControl
{
    public OperationRedrawModel Operation => (BaseOperation as OperationRedrawModel)!;
    public ToolRedrawModelControl()
    {
        BaseOperation = new OperationRedrawModel(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    public override void Callback(ToolWindow.Callbacks callback)
    {
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
            case ToolWindow.Callbacks.AfterLoadProfile:
                if (ParentWindow is not null) ParentWindow.ButtonOkEnabled = !string.IsNullOrWhiteSpace(Operation.FilePath);
                Operation.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(Operation.FilePath))
                    {
                        ParentWindow!.ButtonOkEnabled = !string.IsNullOrWhiteSpace(Operation.FilePath);
                    }
                };
                break;
        }
    }

    public async Task ImportFile()
    {
        var filters = AvaloniaStatic.ToAvaloniaFileFilter(FileFormat.AllFileFiltersAvalonia);
        var orderedFilters = new List<FilePickerFileType> { filters[UserSettings.Instance.General.DefaultOpenFileExtensionIndex] };
        for (int i = 0; i < filters.Count; i++)
        {
            if (i == UserSettings.Instance.General.DefaultOpenFileExtensionIndex) continue;
            orderedFilters.Add(filters[i]);
        }

        var files = await App.MainWindow.OpenFilePickerAsync(UserSettings.Instance.General.DefaultDirectoryOpenFile, orderedFilters);
        if (files.Count == 0 || files[0].TryGetLocalPath() is not { } filePath) return;
        if (FileFormat.FindByExtensionOrFilePath(filePath) is null) return;
        Operation.FilePath = filePath;
    }
}