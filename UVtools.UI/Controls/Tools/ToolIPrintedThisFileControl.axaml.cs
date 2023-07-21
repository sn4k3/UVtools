using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolIPrintedThisFileControl : ToolControl
{
    public OperationIPrintedThisFile Operation => (BaseOperation as OperationIPrintedThisFile)!;

    public ToolIPrintedThisFileControl()
    {
        BaseOperation = new OperationIPrintedThisFile(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }
}