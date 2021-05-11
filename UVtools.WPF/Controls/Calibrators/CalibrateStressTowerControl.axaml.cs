using System.Diagnostics;
using System.Timers;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using UVtools.Core.Operations;
using UVtools.WPF.Controls.Tools;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Calibrators
{
    public class CalibrateStressTowerControl : ToolControl
    {
        public OperationCalibrateStressTower Operation => BaseOperation as OperationCalibrateStressTower;

        public CalibrateStressTowerControl()
        {
            InitializeComponent();
            BaseOperation = new OperationCalibrateStressTower(SlicerFile);
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
