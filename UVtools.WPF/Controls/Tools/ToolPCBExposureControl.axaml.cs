using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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

        private Bitmap _previewImage;
        public Bitmap PreviewImage
        {
            get => _previewImage;
            set => RaiseAndSetIfChanged(ref _previewImage, value);
        }

        public ToolPCBExposureControl()
        {
            BaseOperation = new OperationPCBExposure(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();

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
                    break;
            }
        }

        public void UpdatePreview()
        {
            try
            {
                using var mat = Operation.GetMat();
                using var matCropped = mat.CropByBounds(20);
                _previewImage?.Dispose();
                PreviewImage = matCropped.ToBitmap();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            
        }

        public async void SelectFile()
        {
            var dialog = new OpenFileDialog
            {
                AllowMultiple = false,
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

            Operation.FilePath = files[0];
        }
    }
}
