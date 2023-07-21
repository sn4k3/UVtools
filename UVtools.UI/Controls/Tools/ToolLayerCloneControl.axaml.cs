using System.ComponentModel;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using UVtools.UI.Windows;

namespace UVtools.UI.Controls.Tools;

public partial class ToolLayerCloneControl : ToolControl
{
    public OperationLayerClone Operation => (BaseOperation as OperationLayerClone)!;

        
    public string InfoLayersStr
    {
        get
        {
            uint extraLayers = Operation.ExtraLayers;
            return $"Layers: {SlicerFile!.LayerCount} → {SlicerFile.LayerCount + extraLayers} (+ {extraLayers})";
        }
    }

    public string InfoHeightsStr
    {
        get
        {
            float extraHeight = Operation.KeepSamePositionZ ? 0 : Layer.RoundHeight(Operation.ExtraLayers * SlicerFile!.LayerHeight);
            return $"Height: {SlicerFile!.PrintHeight}mm → {Layer.RoundHeight(SlicerFile.PrintHeight + extraHeight)}mm (+ {extraHeight}mm)";
        }
    }

    public ToolLayerCloneControl()
    {
        BaseOperation = new OperationLayerClone(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    public override void Callback(ToolWindow.Callbacks callback)
    {
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
            case ToolWindow.Callbacks.AfterLoadProfile:
                Operation.PropertyChanged += OnPropertyChanged;
                break;
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        RaisePropertyChanged(nameof(InfoLayersStr));
        RaisePropertyChanged(nameof(InfoHeightsStr));
    }
}