using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolDynamicLayerHeightControl : ToolControl
    {
        public OperationDynamicLayerHeight Operation => BaseOperation as OperationDynamicLayerHeight;

        public double LayerHeight => SlicerFile.LayerHeight;
        public double MinimumLayerHeight => Layer.RoundHeight(SlicerFile.LayerHeight * 2);
        public double MaximumLayerHeight => FileFormat.MaximumLayerHeight;

        private DataGrid ExposureTable;

        public ToolDynamicLayerHeightControl()
        {
            BaseOperation = new OperationDynamicLayerHeight(SlicerFile);

            if (SlicerFile.LayerHeight * 2 > FileFormat.MaximumLayerHeight)
            {
                App.MainWindow.MessageBoxError($"This file already uses the maximum layer height possible ({SlicerFile.LayerHeight}mm).\n" +
                                                     $"Layers can not be stacked, please re-slice your file with the lowest layer height of 0.01mm.",
                    $"{BaseOperation.Title} unable to run");
                CanRun = false;
                return;
            }

            for (uint layerIndex = 1; layerIndex < SlicerFile.LayerCount; layerIndex++)
            {
                if ((decimal)Math.Round(SlicerFile[layerIndex].PositionZ - SlicerFile[layerIndex - 1].PositionZ, Layer.HeightPrecision) ==
                    (decimal)SlicerFile.LayerHeight) continue;
                App.MainWindow.MessageBoxError($"This file contain layer(s) with modified positions, starting at layer {layerIndex}.\n" +
                                                     $"This tool requires sequential layers with equal height.\n" +
                                                     $"If you run this tool before, you cant re-run.",
                    $"{BaseOperation.Title} unable to run");
                CanRun = false;
                return;
            }

            if (!SlicerFile.HavePrintParameterPerLayerModifier(FileFormat.PrintParameterModifier.ExposureSeconds))
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
}
