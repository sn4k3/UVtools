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
    public class CalibrateToleranceControl : ToolControl
    {
        public OperationCalibrateTolerance Operation => BaseOperation as OperationCalibrateTolerance;

        private Timer _timer;

        /*public string ProfileName
        {
            get => _profileName;
            set => RaiseAndSetIfChanged(ref _profileName, value);
        }*/

        //public bool IsProfileAddEnabled => Operation.ScaleXFactor != 100 || Operation.ScaleYFactor != 100;

        private Bitmap _previewImage;
        /*private string _profileName;
        private bool _isProfileNameEnabled;*/

        public Bitmap PreviewImage
        {
            get => _previewImage;
            set => RaiseAndSetIfChanged(ref _previewImage, value);
        }

        public bool IsDisplaySizeVisible => App.SlicerFile.DisplayWidth <= 0 && App.SlicerFile.DisplayHeight <= 0;

        public CalibrateToleranceControl()
        {
            BaseOperation = new OperationCalibrateTolerance(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
            

            _timer = new Timer(100)
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
                        /*if (e.PropertyName == nameof(Operation.ScaleXFactor) || e.PropertyName == nameof(Operation.ScaleYFactor))
                        {
                            RaisePropertyChanged(nameof(IsProfileAddEnabled));
                            return;
                        }*/
                    };
                    _timer.Stop();
                    _timer.Start();
                    break;
            }
        }

        public void UpdatePreview()
        {
            var layers = Operation.GetLayers();
            _previewImage?.Dispose();
            PreviewImage = layers[^1].ToBitmap();
            foreach (var layer in layers)
            {
                layer.Dispose();
            }
        }

        /*public async void AddProfile()
        {
            OperationResize resize = new OperationResize
            {
                ProfileName = ProfileName,
                X = Operation.ScaleXFactor,
                Y = Operation.ScaleYFactor
            };
            var find = OperationProfiles.FindByName(resize, ProfileName);
            if (find is not null)
            {
                if (await ParentWindow.MessageBoxQuestion(
                    $"A profile with same name and/or values already exists, do you want to overwrite:\n{find}\nwith:\n{resize}\n?") != ButtonResult.Yes) return;

                OperationProfiles.RemoveProfile(resize, false);
            }

            OperationProfiles.AddProfile(resize);
            await ParentWindow.MessageBoxInfo($"The resize profile has been added.\nGo to Tools - Resize and select the saved profile to load it in.\n{resize}");
        }

        public void AutoNameProfile()
        {
            var printerName = string.IsNullOrEmpty(App.SlicerFile.MachineName)
                ? "MyPrinterX"
                : App.SlicerFile.MachineName;
            ProfileName = $"{printerName}, MyResinX, {Operation.Microns}µm, {Operation.BottomExposure}s/{Operation.NormalExposure}s";
        }
        */
    }
}
