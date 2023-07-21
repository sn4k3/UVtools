using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolMorphControl : ToolControl
{
    public OperationMorph Operation => (BaseOperation as OperationMorph)!;

    public ToolMorphControl()
    {
        BaseOperation = new OperationMorph(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }
}