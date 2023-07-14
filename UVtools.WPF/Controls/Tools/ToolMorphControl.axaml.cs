using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools;

public partial class ToolMorphControl : ToolControl
{
    public OperationMorph Operation => BaseOperation as OperationMorph;

    public ToolMorphControl()
    {
        BaseOperation = new OperationMorph(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }
}