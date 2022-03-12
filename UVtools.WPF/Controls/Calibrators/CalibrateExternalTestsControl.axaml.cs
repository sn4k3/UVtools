using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;
using UVtools.Core.SystemOS;
using UVtools.WPF.Controls.Tools;

namespace UVtools.WPF.Controls.Calibrators;

public class CalibrateExternalTestsControl : ToolControl
{
    public OperationCalibrateExternalTests Operation => BaseOperation as OperationCalibrateExternalTests;
    public CalibrateExternalTestsControl()
    {
        BaseOperation = new OperationCalibrateExternalTests(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
            
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void ButtonClicked(string url)
    {
        SystemAware.OpenBrowser(url);
    }
}