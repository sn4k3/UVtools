using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolSolidifyControl : ToolControl
{
    public OperationSolidify Operation => (BaseOperation as OperationSolidify)!;
    public ToolSolidifyControl()
    {
        BaseOperation = new OperationSolidify(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }
}