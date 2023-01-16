using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System.Timers;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools
{
    public partial class ToolLithophaneControl : ToolControl
    {
        public OperationLithophane Operation => BaseOperation as OperationLithophane;

        private readonly Timer _timer;

        private Bitmap _previewImage;
        public Bitmap PreviewImage
        {
            get => _previewImage;
            set => RaiseAndSetIfChanged(ref _previewImage, value);
        }

        public ToolLithophaneControl()
        {
            BaseOperation = new OperationLithophane(SlicerFile);
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
            using var mat = Operation.GetTargetMat();
            _previewImage?.Dispose();
            PreviewImage = mat?.ToBitmapParallel();
        }

        public async void SelectFile()
        {
            var dialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Filters = Helpers.ImagesFileFilter,
            };

            var files = await dialog.ShowAsync(ParentWindow);
            if (files is null || files.Length == 0) return;

            Operation.FilePath = files[0];
        }
    }
}
