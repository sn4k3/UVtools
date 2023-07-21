using UVtools.Core.Operations;
using UVtools.UI.Controls.Tools;

namespace UVtools.UI.Controls.Calibrators;

public partial class CalibrateStressTowerControl : ToolControl
{
    public OperationCalibrateStressTower Operation => (BaseOperation as OperationCalibrateStressTower)!;

    public CalibrateStressTowerControl()
    {
        BaseOperation = new OperationCalibrateStressTower(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }
}