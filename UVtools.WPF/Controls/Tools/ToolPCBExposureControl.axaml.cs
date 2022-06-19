using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using UVtools.Core.Extensions;
using UVtools.Core.Objects;
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
        private ListBox FilesListBox;

        private Bitmap _previewImage;
        private ValueDescription _selectedFile;

        public Bitmap PreviewImage
        {
            get => _previewImage;
            set => RaiseAndSetIfChanged(ref _previewImage, value);
        }

        public ValueDescription SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (!RaiseAndSetIfChanged(ref _selectedFile, value)) return;
                UpdatePreview();
            }
        }

        public ToolPCBExposureControl()
        {
            BaseOperation = new OperationPCBExposure(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();

            FilesListBox = this.Find<ListBox>("FilesListBox");

            _timer = new Timer(20)
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
                    _timer.Stop();
                    _timer.Start();
                    ParentWindow.ButtonOkEnabled = Operation.FileCount > 0;
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
                if (!_selectedFile.ValueAsString.EndsWith(".gbr", StringComparison.OrdinalIgnoreCase) || !File.Exists(_selectedFile.ValueAsString)) return;
                using var mat = Operation.GetMat(_selectedFile.ValueAsString);
                using var matCropped = mat.CropByBounds(20);
                _previewImage?.Dispose();
                PreviewImage = matCropped.ToBitmap();
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
                        Extensions = new List<string>{ "gbr" }
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
            Operation.Files.RemoveRange(FilesListBox.SelectedItems.OfType<ValueDescription>());
        }

        public void ClearFiles()
        {
            Operation.Files.Clear();
            PreviewImage = null;
        }
    }
}
