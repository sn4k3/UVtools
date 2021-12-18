using System.Timers;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using UVtools.Core.Operations;
using UVtools.WPF.Controls.Tools;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Calibrators
{
    public class CalibrateElephantFootControl : ToolControl
    {
        public OperationCalibrateElephantFoot Operation => BaseOperation as OperationCalibrateElephantFoot;

        private readonly Timer _timer;

        private Bitmap _previewImage;
        public Bitmap PreviewImage
        {
            get => _previewImage;
            set => RaiseAndSetIfChanged(ref _previewImage, value);
        }


        public CalibrateElephantFootControl()
        {
            BaseOperation = new OperationCalibrateElephantFoot(SlicerFile);
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
                        if (e.PropertyName == nameof(Operation.ObjectCount))
                        {
                            ParentWindow.ButtonOkEnabled = Operation.ObjectCount > 0;
                            return;
                        }
                    };
                    if(ParentWindow is not null) ParentWindow.ButtonOkEnabled = Operation.ObjectCount > 0;
                    _timer.Stop();
                    _timer.Start();
                    break;
            }
        }

        public void UpdatePreview()
        {
            var layers = Operation.GetLayers();
            _previewImage?.Dispose();
            PreviewImage = layers[0].ToBitmap();
            foreach (var layer in layers)
            {
                layer.Dispose();
            }
        }
    }
}
