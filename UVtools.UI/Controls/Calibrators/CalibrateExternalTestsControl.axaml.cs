using UVtools.Core.Operations;
using UVtools.Core.SystemOS;
using UVtools.UI.Controls.Tools;

namespace UVtools.UI.Controls.Calibrators;

public partial class CalibrateExternalTestsControl : ToolControl
{
    public OperationCalibrateExternalTests Operation => BaseOperation as OperationCalibrateExternalTests;
    public CalibrateExternalTestsControl()
    {
        BaseOperation = new OperationCalibrateExternalTests(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
            
    }

    public void ButtonClicked(object url)
    {
        SystemAware.OpenBrowser((string)url);
    }
}