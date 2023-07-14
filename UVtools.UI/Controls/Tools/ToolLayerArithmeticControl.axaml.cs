using UVtools.Core.Operations;
using UVtools.UI.Windows;

namespace UVtools.UI.Controls.Tools;

public partial class ToolLayerArithmeticControl : ToolControl
{
    public OperationLayerArithmetic Operation => BaseOperation as OperationLayerArithmetic;

    public ToolLayerArithmeticControl()
    {
        BaseOperation = new OperationLayerArithmetic(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    public override void Callback(ToolWindow.Callbacks callback)
    {
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
            case ToolWindow.Callbacks.AfterLoadProfile:
                if(ParentWindow is not null) ParentWindow.ButtonOkEnabled = !string.IsNullOrWhiteSpace(Operation.Sentence);
                Operation.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(Operation.Sentence))
                    {
                        ParentWindow.ButtonOkEnabled = !string.IsNullOrWhiteSpace(Operation.Sentence);
                    }
                };
                break;
        }
    }
}