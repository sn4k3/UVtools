using Avalonia.Markup.Xaml;
using System;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools;

public class ToolLayerRemoveControl : ToolControl
{
    public OperationLayerRemove Operation => BaseOperation as OperationLayerRemove;

    public string InfoLayersStr
    {
        get
        {
            uint extraLayers = Operation.LayerRemoveCount;
            return $"Layers: {SlicerFile.LayerCount} → {SlicerFile.LayerCount - extraLayers} (- {extraLayers})";
        }
    }

    public string InfoHeightsStr
    {
        get
        {
            float extraHeight = 0;
            for (uint layerIndex = Operation.LayerIndexStart; layerIndex <= Operation.LayerIndexEnd; layerIndex++)
            {
                if (Operation.UseThreshold && SlicerFile[layerIndex].NonZeroPixelCount > Operation.PixelThreshold) continue;
                extraHeight += SlicerFile[layerIndex].RelativePositionZ;
            }

            extraHeight = Layer.RoundHeight(extraHeight);
            return $"Height: {SlicerFile.PrintHeight}mm → {Math.Max(0, Layer.RoundHeight(SlicerFile.PrintHeight - extraHeight))}mm (- {extraHeight}mm)";
        }
    }

    public ToolLayerRemoveControl()
    {
        BaseOperation = new OperationLayerRemove(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void Callback(ToolWindow.Callbacks callback)
    {
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
            case ToolWindow.Callbacks.Loaded:
                Operation.PropertyChanged += (sender, args) =>
                {
                    RaisePropertyChanged(nameof(InfoLayersStr));
                    RaisePropertyChanged(nameof(InfoHeightsStr));
                };
                break;
        }
    }
}