using UVtools.Core.Operations;
using UVtools.UI.Windows;

namespace UVtools.UI.Controls.Tools;

public partial class ToolLayerReHeightControl : ToolControl
{
    public OperationLayerReHeight Operation => (BaseOperation as OperationLayerReHeight)!;

    public string CurrentLayers => $"Current layers: {SlicerFile!.LayerCount} at {SlicerFile.LayerHeight}mm";

    public ToolLayerReHeightControl()
    {
        BaseOperation = new OperationLayerReHeight(SlicerFile!);
        if (!ValidateSpawn()) return;

        InitializeComponent();
    }

    public override void Callback(ToolWindow.Callbacks callback)
    {
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
            case ToolWindow.Callbacks.AfterLoadProfile:
                if (ParentWindow is not null) ParentWindow.LayerRangeVisible = Operation.Method == OperationLayerReHeight.OperationLayerReHeightMethod.OffsetPositionZ;
                Operation.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(Operation.Method))
                    {
                        ParentWindow!.LayerRangeVisible = Operation.Method == OperationLayerReHeight.OperationLayerReHeightMethod.OffsetPositionZ;
                        return;
                    }
                };
                break;
        }
    }
}