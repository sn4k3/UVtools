using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools;

public partial class ToolRaiseOnPrintFinishControl : ToolControl
{
    public OperationRaiseOnPrintFinish Operation => BaseOperation as OperationRaiseOnPrintFinish;

    public ToolRaiseOnPrintFinishControl()
    {
        BaseOperation = new OperationRaiseOnPrintFinish(SlicerFile);
        if (!ValidateSpawn()) return;

        InitializeComponent();
    }
}