using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools;

public class ToolInfillControl : ToolControl
{
    public OperationInfill Operation => BaseOperation as OperationInfill;

    public ToolInfillControl()
    {
        BaseOperation = new OperationInfill(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}