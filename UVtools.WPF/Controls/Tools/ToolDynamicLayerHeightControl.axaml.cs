using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools;

public class ToolDynamicLayerHeightControl : ToolControl
{
    public OperationDynamicLayerHeight Operation => BaseOperation as OperationDynamicLayerHeight;

    public double LayerHeight => SlicerFile.LayerHeight;
    public double MinimumLayerHeight => Layer.RoundHeight(SlicerFile.LayerHeight * 2);
    public double MaximumLayerHeight => FileFormat.MaximumLayerHeight;

    private readonly DataGrid ExposureTable;

    public ToolDynamicLayerHeightControl()
    {
        BaseOperation = new OperationDynamicLayerHeight(SlicerFile);
        if (!ValidateSpawn()) return;

        if (!SlicerFile.CanUseLayerExposureTime)
        {
            App.MainWindow.MessageBoxWaring($"Your printer seems to not support this tool, still you are allowed to run it for analyze, packing layers or simulation.\n" +
                                            $"Do not print this file after run this tool on a not compatible printer, it will result in malformed model and height violation.\n" +
                                            $"Run this at your own risk!",
                BaseOperation.Title).ConfigureAwait(false);
        }


        InitializeComponent();

        ExposureTable = this.FindControl<DataGrid>("ExposureTable");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }


    public override void Callback(ToolWindow.Callbacks callback)
    {
        if (SlicerFile is null) return;
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
            case ToolWindow.Callbacks.Loaded:
                /*Operation.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName.Equals(nameof(Operation.CacheObjectCount)))
                    {
                        RaisePropertyChanged(nameof(CacheRAMUsed));
                        return;
                    }
                };*/
                Operation.RebuildAutoExposureTable();
                break;
        }
    }
}