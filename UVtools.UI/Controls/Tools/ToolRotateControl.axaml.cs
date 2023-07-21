using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolRotateControl : ToolControl
{
    public OperationRotate Operation => (BaseOperation as OperationRotate)!;

    public ToolRotateControl()
    {
        BaseOperation = new OperationRotate(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }
}