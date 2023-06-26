using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Linq;
using System.Timers;
using UVtools.Core.Dialogs;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.WPF.Controls.Tools;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Calibrators;

public class CalibrateExposureFinderControl : ToolControl
{
    public OperationCalibrateExposureFinder Operation => BaseOperation as OperationCalibrateExposureFinder;

    private readonly Timer _timer;

    private Bitmap _previewImage;
    private readonly DataGrid _exposureTable;

    public Bitmap PreviewImage
    {
        get => _previewImage;
        set => RaiseAndSetIfChanged(ref _previewImage, value);
    }

    public bool CanSupportPerLayerSettings => SlicerFile.HaveLayerParameterModifier(FileFormat.PrintParameterModifier.ExposureTime);

    public CalibrateExposureFinderControl()
    {
        BaseOperation = new OperationCalibrateExposureFinder(SlicerFile);
        if (!ValidateSpawn()) return;

        InitializeComponent();
            
        _exposureTable = this.FindControl<DataGrid>("ExposureTable");

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
            case ToolWindow.Callbacks.AfterLoadProfile:
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
        var layers = Operation.GetLayers(true);
        _previewImage?.Dispose();
        if (layers is not null)
        {
            PreviewImage = layers[^1].ToBitmapParallel();
            foreach (var layer in layers)
            {
                layer.Dispose();
            }
        }
    }

    public void BrightnessExposureGenAdd()
    {
        var values = Operation.MultipleBrightnessValuesArray.ToList();
        // normal exposure - 255
        //     wanted      -  x
        byte brightness = (byte) Math.Clamp(Math.Round(Operation.MultipleBrightnessGenExposureTime * byte.MaxValue / Operation.NormalExposure), 1, byte.MaxValue);
        if (values.Contains(brightness)) return;
        values.Add(brightness);
        values.Sort((b, b1) => b1.CompareTo(b));
        Operation.MultipleBrightnessValues = string.Join(", ", values);
    }

    public async void GenerateExposureTable()
    {
        if (Operation.ExposureTable.Count > 0)
        {
            if (await ParentWindow.MessageBoxQuestion(
                    "This automatic exposure table generation will clear the current table data!\n" +
                    "Do you want to continue?"
                ) != MessageButtonResult.Yes) return;
        }

        Operation.GenerateExposureTable();
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
            ) != MessageButtonResult.Yes) return;

        Operation.ExposureTable.Clear();
    }

    public async void ExposureTableRemoveSelectedEntries()
    {
        if (_exposureTable.SelectedItems.Count <= 0) return;
        if (await ParentWindow.MessageBoxQuestion(
                $"Are you sure you want to remove the {_exposureTable.SelectedItems.Count} selected entries?"
            ) != MessageButtonResult.Yes) return;

        Operation.ExposureTable.RemoveRange(_exposureTable.SelectedItems.Cast<ExposureItem>());
    }
}