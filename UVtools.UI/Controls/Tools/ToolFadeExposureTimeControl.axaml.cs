using UVtools.Core.Operations;
using UVtools.UI.Windows;

namespace UVtools.UI.Controls.Tools;

public partial class ToolFadeExposureTimeControl : ToolControl
{
    public OperationFadeExposureTime Operation => (BaseOperation as OperationFadeExposureTime)!;

    public ToolFadeExposureTimeControl()
    {
        BaseOperation = new OperationFadeExposureTime(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    public override void Callback(ToolWindow.Callbacks callback)
    {
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
            case ToolWindow.Callbacks.AfterLoadProfile:
                ParentWindow!.LayerIndexEnd = Operation.LayerIndexStart + Operation.LayerCount - 1;
                Operation.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName != nameof(Operation.LayerCount)) return;
                    ParentWindow.LayerIndexEnd = Operation.LayerIndexStart + Operation.LayerCount - 1;
                };
                ParentWindow.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName is nameof(ParentWindow.LayerIndexStart) or nameof(ParentWindow.LayerIndexEnd))
                    {
                        ParentWindow.LayerIndexEnd = Operation.LayerIndexStart + Operation.LayerCount - 1;
                    }
                };
                break;
        }

    }
}