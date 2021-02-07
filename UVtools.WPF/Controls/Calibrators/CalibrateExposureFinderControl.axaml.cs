using System.Linq;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DynamicData;
using MessageBox.Avalonia.Enums;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.WPF.Controls.Tools;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Calibrators
{
    public class CalibrateExposureFinderControl : ToolControl
    {
        public OperationCalibrateExposureFinder Operation => BaseOperation as OperationCalibrateExposureFinder;

        private Timer _timer;

        private Bitmap _previewImage;
        private DataGrid _exposureTable;

        public Bitmap PreviewImage
        {
            get => _previewImage;
            set => RaiseAndSetIfChanged(ref _previewImage, value);
        }

        public bool CanSupportPerLayerSettings => SlicerFile.HavePrintParameterPerLayerModifier(FileFormat.PrintParameterModifier.ExposureSeconds);

        public CalibrateExposureFinderControl()
        {
            InitializeComponent();
            _exposureTable = this.FindControl<DataGrid>("ExposureTable");
            BaseOperation = new OperationCalibrateExposureFinder(SlicerFile);

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
                case ToolWindow.Callbacks.ProfileLoaded:
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
            var layers = Operation.GetLayers();
            _previewImage?.Dispose();
            if (layers is not null)
            {
                PreviewImage = layers[^1].ToBitmap();
                foreach (var layer in layers)
                {
                    layer.Dispose();
                }
            }
        }

        public async void GenerateExposureTable()
        {
            if (Operation.ExposureTable.Count > 0)
            {
                if (await ParentWindow.MessageBoxQuestion(
                    "This automatic exposure table generation will clear the current table data!\n" +
                    "Do you want to continue?"
                    ) != ButtonResult.Yes) return;
            }

            Operation.GenerateExposure();
        }

        public async void ExposureTableAddManual()
        {
            var exposure = Operation.ExposureManualEntry;
            if (!exposure.IsValid)
            {
                await ParentWindow.MessageBoxError(
                    $"Layer height and exposures must be higher than zero (0).\n{exposure}");
                return;
            }

            if (Operation.ExposureTable.Contains(exposure))
            {
                await ParentWindow.MessageBoxError(
                    $"The configured layer height and exposure data already exists on the table.\n{exposure}");
                return;
            }

            Operation.ExposureTable.Add(exposure);
            Operation.SanitizeExposureTable();
        }

        public async void ExposureTableClearEntries()
        {
            if (Operation.ExposureTable.Count <= 0) return;
            if (await ParentWindow.MessageBoxQuestion(
                $"Are you sure you want to all the {Operation.ExposureTable.Count} entries?"
            ) != ButtonResult.Yes) return;

            Operation.ExposureTable.Clear();
        }

        public async void ExposureTableRemoveSelectedEntries()
        {
            if (_exposureTable.SelectedItems.Count <= 0) return;
            if (await ParentWindow.MessageBoxQuestion(
                $"Are you sure you want to remove the {_exposureTable.SelectedItems.Count} selected entries?"
            ) != ButtonResult.Yes) return;

            Operation.ExposureTable.RemoveMany(_exposureTable.SelectedItems.Cast<ExposureItem>());
        }
    }
}
