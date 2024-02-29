using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolStirResinControl : ToolControl
{
    public OperationStirResin Operation => (BaseOperation as OperationStirResin)!;
    public ToolStirResinControl()
    {
        BaseOperation = new OperationStirResin(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }
}