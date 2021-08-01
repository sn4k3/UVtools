using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;
using UVtools.WPF.Controls.Tools;

namespace UVtools.WPF.Controls.Calibrators
{
    public class CalibrateStressTowerControl : ToolControl
    {
        public OperationCalibrateStressTower Operation => BaseOperation as OperationCalibrateStressTower;

        public CalibrateStressTowerControl()
        {
            BaseOperation = new OperationCalibrateStressTower(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /*public override void Callback(ToolWindow.Callbacks callback)
        {
            if (App.SlicerFile is null) return;
            switch (callback)
            {
                case ToolWindow.Callbacks.Init:
                case ToolWindow.Callbacks.Loaded:
                    break;
            }
        }*/
    }
}
