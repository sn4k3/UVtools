using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using UVtools.Core.Excellon;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;
using UVtools.UI.Extensions;
using UVtools.UI.Windows;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace UVtools.UI.Controls.Tools;

public partial class ToolPCBExposureControl : ToolControl
{

    public OperationPCBExposure Operation => (BaseOperation as OperationPCBExposure)!;

    private readonly Timer _timer = null!;

    private Bitmap? _previewImage;
    private OperationPCBExposure.PCBExposureFile? _selectedFile;
    private bool _cropPreview  = true;

    public Bitmap? PreviewImage
    {
        get => _previewImage;
        set => RaiseAndSetIfChanged(ref _previewImage, value);
    }

    public OperationPCBExposure.PCBExposureFile? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (!RaiseAndSetIfChanged(ref _selectedFile, value)) return;
            UpdatePreview();
        }
    }

    public bool CropPreview
    {
        get => _cropPreview;
        set
        {
            if(!RaiseAndSetIfChanged(ref _cropPreview, value)) return;
            UpdatePreview();
        }
    }

    public ToolPCBExposureControl()
    {
        BaseOperation = new OperationPCBExposure(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, (sender, args) =>
        {
            var files = args.Data.GetFiles();
            if (files is null) return;
            Operation.AddFiles(files.Select(file => file.TryGetLocalPath()).ToArray()!);
        });

        _timer = new Timer(50)
        {
            AutoReset = false
        };
        _timer.Elapsed += (sender, e) =>
        {
            Dispatcher.UIThread.InvokeAsync(UpdatePreview);
        };
    }

    public override void Callback(ToolWindow.Callbacks callback)
    {
        if (SlicerFile is null) return;
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
            case ToolWindow.Callbacks.AfterLoadProfile:
                Operation.PropertyChanged += (sender, e) =>
                {
                    _timer.Stop();
                    _timer.Start();
                };
                    
                Operation.Files.CollectionChanged += (sender, e) =>
                {
                    if (e.NewItems is null) return;
                    foreach (OperationPCBExposure.PCBExposureFile file in e.NewItems)
                    {
                        file.PropertyChanged += (o, args) =>
                        {
                            if (!ReferenceEquals(_selectedFile, file)) return;
                            _timer.Stop();
                            _timer.Start();
                        };
                    }
                };

                _timer.Stop();
                _timer.Start();
                if(ParentWindow is not null) ParentWindow.ButtonOkEnabled = Operation.FileCount > 0;
                Operation.Files.CollectionChanged += (sender, e) => ParentWindow!.ButtonOkEnabled = Operation.FileCount > 0;
                break;
        }
    }

    public void UpdatePreview()
    {
        try
        {
            PreviewImage = null;
            if (_selectedFile is null)
            {
                return;
            }
                
            if (!OperationPCBExposure.ValidExtensions.Any(extension => _selectedFile.IsExtension(extension)) || !_selectedFile.Exists) return;
            var file = (OperationPCBExposure.PCBExposureFile)_selectedFile.Clone();
            file.InvertPolarity = ExcellonDrillFormat.Extensions.Any(extension => file.IsExtension(extension));
            _previewImage?.Dispose();
            using var mat = Operation.GetMat(file);

            if (_cropPreview)
            {
                using var matCropped = mat.CropByBounds(20);
                PreviewImage = matCropped.ToBitmap();
            }
            else
            {
                PreviewImage = mat.ToBitmap();
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
            
    }

    public async void AddFiles()
    {
        var files = await App.MainWindow.OpenFilePickerAsync(
            AvaloniaStatic.CreateFilePickerFileTypes("Gerber files", OperationPCBExposure.ValidExtensions),
            allowMultiple: true);

        if (files.Count == 0) return;

        Operation.AddFiles(files.Select(file => file.TryGetLocalPath()!).ToArray());
    }

    public async void AddFilesFromZip()
    {
        var files = await App.MainWindow.OpenFilePickerAsync(AvaloniaStatic.ZipFileFilter);
        if (files.Count == 0 || files[0].TryGetLocalPath() is not { } filePath) return;

        Operation.AddFilesFromZip(filePath);
    }

    public void RemoveFiles()
    {
        Operation.Files.RemoveRange(FilesDataGrid.SelectedItems.OfType<OperationPCBExposure.PCBExposureFile>());
    }

    public void MoveFileTop()
    {
        if (FilesDataGrid.SelectedIndex <= 0) return;
        var selectedFile = SelectedFile;
        Operation.Files.RemoveAt(FilesDataGrid.SelectedIndex);
        Operation.Files.Insert(0, selectedFile!);
        FilesDataGrid.SelectedIndex = 0;
        FilesDataGrid.ScrollIntoView(selectedFile, FilesDataGrid.Columns[0]);
    }

    public void MoveFileUp()
    {
        if (FilesDataGrid.SelectedIndex <= 0) return;
        var selectedFile = SelectedFile;
        var newIndex = FilesDataGrid.SelectedIndex - 1;
        Operation.Files.RemoveAt(FilesDataGrid.SelectedIndex);
        Operation.Files.Insert(newIndex, selectedFile!);
        FilesDataGrid.SelectedIndex = newIndex;
        FilesDataGrid.ScrollIntoView(selectedFile, FilesDataGrid.Columns[0]);
    }

    public void MoveFileDown()
    {
        if (FilesDataGrid.SelectedIndex == -1 || FilesDataGrid.SelectedIndex == Operation.FileCount - 1) return;
        var selectedFile = SelectedFile;
        var newIndex = FilesDataGrid.SelectedIndex + 1;
        Operation.Files.RemoveAt(FilesDataGrid.SelectedIndex);
        Operation.Files.Insert(newIndex, selectedFile!);
        FilesDataGrid.SelectedIndex = newIndex;
        FilesDataGrid.ScrollIntoView(selectedFile, FilesDataGrid.Columns[0]);
    }

    public void MoveFileBottom()
    {
        var lastIndex = Operation.Files.Count - 1;
        if (FilesDataGrid.SelectedIndex == -1 || FilesDataGrid.SelectedIndex == lastIndex) return;
        var selectedFile = SelectedFile;
        Operation.Files.RemoveAt(FilesDataGrid.SelectedIndex);
        Operation.Files.Insert(lastIndex, selectedFile!);
        FilesDataGrid.SelectedIndex = lastIndex;
        FilesDataGrid.ScrollIntoView(selectedFile, FilesDataGrid.Columns[0]);
    }

    public void ClearFiles()
    {
        Operation.Files.Clear();
        PreviewImage = null;
    }
}