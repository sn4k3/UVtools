using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolFlipControl : ToolControl
{
    public OperationFlip Operation => (BaseOperation as OperationFlip)!;

    public ToolFlipControl()
    {
        BaseOperation = new OperationFlip(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }
}