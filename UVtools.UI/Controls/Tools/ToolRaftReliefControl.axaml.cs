using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolRaftReliefControl : ToolControl
{
    public OperationRaftRelief Operation => (BaseOperation as OperationRaftRelief)!;

    public ToolRaftReliefControl()
    {
        BaseOperation = new OperationRaftRelief(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }


    public void UseCurrentLayerAsMask()
    {
        Operation.MaskLayerIndex = App.MainWindow.ActualLayer;
    }
}