using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools;

public class ToolSolidifyControl : ToolControl
{
    public OperationSolidify Operation => BaseOperation as OperationSolidify;
    public ToolSolidifyControl()
    {
        BaseOperation = new OperationSolidify(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}