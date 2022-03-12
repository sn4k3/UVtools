using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools;

public partial class ToolLightBleedCompensationControl : ToolControl
{
    public OperationLightBleedCompensation Operation => BaseOperation as OperationLightBleedCompensation;
    public ToolLightBleedCompensationControl()
    {
        BaseOperation = new OperationLightBleedCompensation(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}