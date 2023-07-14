using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolDynamicLiftsControl : ToolControl
{
    public OperationDynamicLifts Operation => BaseOperation as OperationDynamicLifts;
    public ToolDynamicLiftsControl()
    {
        BaseOperation = new OperationDynamicLifts(SlicerFile);
        if (!ValidateSpawn()) return;

        InitializeComponent();
    }

    public void ViewSmallestBottomLayer()
    {
        var layerFound = Operation.GetSmallestLayer(true);
        if (layerFound is null) return;
        App.MainWindow.ActualLayer = layerFound.Index;
    }

    public void ViewSmallestNormalLayer()
    {
        var layerFound = Operation.GetSmallestLayer(false);
        if (layerFound is null) return;
        App.MainWindow.ActualLayer = layerFound.Index;
    }

    public void ViewLargestBottomLayer()
    {
        var layerFound = Operation.GetLargestLayer(true);
        if (layerFound is null) return;
        App.MainWindow.ActualLayer = layerFound.Index;
    }

    public void ViewLargestNormalLayer()
    {
        var layerFound = Operation.GetLargestLayer(false);
        if (layerFound is null) return;
        App.MainWindow.ActualLayer = layerFound.Index;
    }
}