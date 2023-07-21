using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolBlurControl : ToolControl
{
    public OperationBlur Operation => (BaseOperation as OperationBlur)!;

    public ToolBlurControl()
    {
        BaseOperation = new OperationBlur(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }
}