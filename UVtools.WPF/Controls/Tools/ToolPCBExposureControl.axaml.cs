using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace UVtools.WPF.Controls.Tools
{
    public partial class ToolPCBExposureControl : ToolControl
    {

        public OperationPCBExposure Operation => BaseOperation as OperationPCBExposure;

        private readonly Timer _timer;
        private DataGrid FilesDataGrid;

        private Bitmap _previewImage;
        private OperationPCBExposure.PCBExposureFile _selectedFile;
        private bool _cropPreview  = true;

        public Bitmap PreviewImage
        {
            get => _previewImage;
            set => RaiseAndSetIfChanged(ref _previewImage, value);
        }

        public OperationPCBExposure.PCBExposureFile SelectedFile
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
            BaseOperation = new OperationPCBExposure(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();

            FilesDataGrid = this.Find<DataGrid>("FilesDataGrid");

            AddHandler(DragDrop.DropEvent, (sender, args) =>
            {
                Operation.AddFiles(args.Data.GetFileNames()?.ToArray() ?? Array.Empty<string>());
            });

            _timer = new Timer(50)
            {
                AutoReset = false
            };
            _timer.Elapsed += (sender, e) => Dispatcher.UIThread.InvokeAsync(UpdatePreview);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void Callback(ToolWindow.Callbacks callback)
        {
            if (App.SlicerFile is null) return;
            switch (callback)
            {
                case ToolWindow.Callbacks.Init:
                case ToolWindow.Callbacks.Loaded:
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
                    Operation.Files.CollectionChanged += (sender, e) => ParentWindow.ButtonOkEnabled = Operation.FileCount > 0;
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
                file.InvertPolarity = file.IsExtension(".drl");
                _previewImage?.Dispose();
                using var mat = Operation.GetMat(file);

                if (_cropPreview)
                {
                    using var matCropped = mat.CropByBounds(20);
                    PreviewImage = matCropped.ToBitmapParallel();
                }
                else
                {
                    PreviewImage = mat.ToBitmapParallel();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            
        }

        public async void AddFiles()
        {
            var dialog = new OpenFileDialog
            {
                AllowMultiple = true,
                Filters = new List<FileDialogFilter>
                {
                    new()
                    {
                        Name = "Gerber files",
                        Extensions = new List<string>(OperationPCBExposure.ValidExtensions)
                    },
                },
            };

            var files = await dialog.ShowAsync(ParentWindow);
            if (files is null || files.Length == 0) return;

            Operation.AddFiles(files);
        }

        public async void AddFilesFromZip()
        {
            var dialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Filters = Helpers.ZipFileFilter
            };

            var files = await dialog.ShowAsync(ParentWindow);
            if (files is null || files.Length == 0) return;

            Operation.AddFilesFromZip(files[0]);
        }

        public void RemoveFiles()
        {
            Operation.Files.RemoveRange(FilesDataGrid.SelectedItems.OfType<OperationPCBExposure.PCBExposureFile>());
        }

        public void ClearFiles()
        {
            Operation.Files.Clear();
            PreviewImage = null;
        }
    }
}
