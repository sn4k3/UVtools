using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools;

public class ToolRedrawModelControl : ToolControl
{
    public OperationRedrawModel Operation => BaseOperation as OperationRedrawModel;
    public ToolRedrawModelControl()
    {
        BaseOperation = new OperationRedrawModel(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void Callback(ToolWindow.Callbacks callback)
    {
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
            case ToolWindow.Callbacks.Loaded:
                ParentWindow.ButtonOkEnabled = !string.IsNullOrWhiteSpace(Operation.FilePath);
                Operation.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(Operation.FilePath))
                    {
                        ParentWindow.ButtonOkEnabled = !string.IsNullOrWhiteSpace(Operation.FilePath);
                    }
                };
                break;
        }
    }

    public async void ImportFile()
    {
        var filters = Helpers.ToAvaloniaFileFilter(FileFormat.AllFileFiltersAvalonia);
        var orderedFilters = new List<FileDialogFilter> { filters[UserSettings.Instance.General.DefaultOpenFileExtensionIndex] };
        for (int i = 0; i < filters.Count; i++)
        {
            if (i == UserSettings.Instance.General.DefaultOpenFileExtensionIndex) continue;
            orderedFilters.Add(filters[i]);
        }

        var dialog = new OpenFileDialog
        {
            AllowMultiple = false,
            Filters = orderedFilters,
            Directory = UserSettings.Instance.General.DefaultDirectoryOpenFile
        };
        var files = await dialog.ShowAsync(ParentWindow);
        if (files is null || files.Length <= 0) return;
        if (FileFormat.FindByExtensionOrFilePath(files[0]) is null) return;
        Operation.FilePath = files[0];
    }
}