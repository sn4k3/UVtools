using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolInfillControl : ToolControl
{
    public OperationInfill Operation => (BaseOperation as OperationInfill)!;

    public ToolInfillControl()
    {
        BaseOperation = new OperationInfill(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }
}