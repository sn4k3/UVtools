using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolResizeControl : ToolControl
{
    public OperationResize Operation => BaseOperation as OperationResize;

    public ToolResizeControl()
    {
        BaseOperation = new OperationResize(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }
}