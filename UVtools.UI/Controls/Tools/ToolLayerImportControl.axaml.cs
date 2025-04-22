using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.Core.SystemOS;
using UVtools.UI.Windows;

namespace UVtools.UI.Controls.Tools;

public partial class ToolLayerImportControl : ToolControl
{
    private bool _isAutoSortLayersByFileNameChecked;
    private GenericFileRepresentation? _selectedFile;
    private Bitmap? _previewImage;

    public OperationLayerImport Operation => (BaseOperation as OperationLayerImport)!;

    public uint MaximumLayer => SlicerFile!.LastLayerIndex;

    public string InfoLayerHeightString => $"({SlicerFile?[Operation.StartLayerIndex].PositionZ}mm)";

    public bool IsAutoSortLayersByFileNameChecked
    {
        get => _isAutoSortLayersByFileNameChecked;
        set => RaiseAndSetIfChanged(ref _isAutoSortLayersByFileNameChecked, value);
    }

    public string? InfoImportResult 
    {
        get
        {
            if (Operation.Files.Count <= 0) return null;
            /*uint modelTotalLayers = (uint) Operation.Files.Count;//Operation.CalculateTotalLayers(App.SlicerFile.LayerCount);
            string textFactor = "grow";
            if (modelTotalLayers < App.SlicerFile.LayerCount)
            {
                textFactor = "shrink";
            }
            else if (modelTotalLayers == App.SlicerFile.LayerCount)
            {
                textFactor = "keep";
            }*/

            return
                $"{Operation.Files.Count} files will be imported into model starting from layer {Operation.StartLayerIndex} {InfoLayerHeightString}.";
            //$"Model will {textFactor} from layers {App.SlicerFile.LayerCount} ({App.SlicerFile.TotalHeight}mm) to {modelTotalLayers} ({App.SlicerFile.GetHeightFromLayer(modelTotalLayers, false)}mm)";
        }
            
    }

    public GenericFileRepresentation? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if(!RaiseAndSetIfChanged(ref _selectedFile, value)) return;
            if (_selectedFile is null)
            {
                PreviewImage = null;
                return;
            }
            if (!OperationLayerImport.ValidImageExtensions.Any(extension => _selectedFile.IsExtension(extension))) return;
            PreviewImage = new Bitmap(_selectedFile.FilePath);
        }
    }

    public Bitmap? PreviewImage
    {
        get => _previewImage;
        set => RaiseAndSetIfChanged(ref _previewImage, value);
    }


    public ToolLayerImportControl()
    {
        BaseOperation = new OperationLayerImport(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();

        FilesListBox.DoubleTapped += (sender, args) =>
        {
            if (FilesListBox.SelectedItem is not GenericFileRepresentation file) return;
            SystemAware.StartProcess(file.FilePath);
        };
        FilesListBox.KeyUp += (sender, e) =>
        {
            switch (e.Key)
            {
                case Key.Escape:
                    FilesListBox.SelectedItems!.Clear();
                    e.Handled = true;
                    break;
                case Key.Delete:
                    RemoveFiles();
                    e.Handled = true;
                    break;
                case Key.A:
                    if ((e.KeyModifiers & KeyModifiers.Control) != 0)
                    {
                        FilesListBox.SelectAll();
                        e.Handled = true;
                    }
                    break;
            }
        };
    }

    /*public override async Task<bool> ValidateForm()
    {
        UpdateOperation();
        var message = Operation.Validate();
        if (message is null) return true;


        message.Content += "\nDo you want to remove all invalid files from list?";
        if (await ParentWindow.MessageBoxQuestion(message.ToString()) == MessageButtonResult.Yes)
        {
            ConcurrentBag<StringTag> result = (ConcurrentBag<StringTag>)message.Tag;
            foreach (var file in result)
            {
                Operation.Files.Remove(file);
            }
        }

        return false;
    }*/

    public override void Callback(ToolWindow.Callbacks callback)
    {
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
            case ToolWindow.Callbacks.AfterLoadProfile:
                RefreshGUI();
                Operation.Files.CollectionChanged += (sender, args) => RefreshGUI();
                Operation.PropertyChanged += (sender, args) => RefreshGUI();
                break;
        }
    }

    public void RefreshGUI()
    {
        RaisePropertyChanged(nameof(InfoLayerHeightString));
        RaisePropertyChanged(nameof(InfoImportResult));
        if(ParentWindow is not null) ParentWindow.ButtonOkEnabled = Operation.Files.Count > 0;
    }

    public void SetCurrentLayer()
    {
        Operation.StartLayerIndex = App.MainWindow.ActualLayer;
    }

    public async Task AddFiles()
    {
        var filters = AvaloniaStatic.ToAvaloniaFileFilter(FileFormat.AllFileFiltersAvalonia);
        var orderedFilters = new List<FilePickerFileType> { filters[UserSettings.Instance.General.DefaultOpenFileExtensionIndex] };
        for (int i = 0; i < filters.Count; i++)
        {
            if (i == UserSettings.Instance.General.DefaultOpenFileExtensionIndex) continue;
            orderedFilters.Add(filters[i]);
        }

        var files = await App.MainWindow.OpenFilePickerAsync(UserSettings.Instance.General.DefaultDirectoryOpenFile, orderedFilters, allowMultiple: true);
        if (files.Count == 0) return;
        
        foreach (var filename in files)
        {
            Operation.AddFile(filename.TryGetLocalPath()!);
        }

        if (_isAutoSortLayersByFileNameChecked)
        {
            Operation.Sort();
        }
    }

    public void RemoveFiles()
    {
        if (FilesListBox.SelectedItems!.Count == 0) return;
        Operation.Files.RemoveRange(FilesListBox.SelectedItems.OfType<GenericFileRepresentation>());
    }

    public void ClearFiles()
    {
        Operation.Files.Clear();
        PreviewImage = null;
    }
}