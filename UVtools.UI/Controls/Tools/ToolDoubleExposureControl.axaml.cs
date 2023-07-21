using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolDoubleExposureControl : ToolControl
{
    public OperationDoubleExposure Operation => (BaseOperation as OperationDoubleExposure)!;

    public ToolDoubleExposureControl()
    {
        BaseOperation = new OperationDoubleExposure(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }
}