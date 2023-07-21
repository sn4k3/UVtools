using UVtools.Core.Operations;
using UVtools.UI.Windows;

namespace UVtools.UI.Controls.Tools;

public partial class ToolTimelapseControl : ToolControl
{
    public OperationTimelapse Operation => (BaseOperation as OperationTimelapse)!;

    public ToolTimelapseControl()
    {
        BaseOperation = new OperationTimelapse(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    public override void Callback(ToolWindow.Callbacks callback)
    {
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
            case ToolWindow.Callbacks.AfterLoadProfile:
                Operation.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(Operation.NumberOfLifts))
                    {
                        ParentWindow!.ButtonOkEnabled = Operation.NumberOfLifts > 0;
                        return;
                    }
                };
                break;
        }
    }
}