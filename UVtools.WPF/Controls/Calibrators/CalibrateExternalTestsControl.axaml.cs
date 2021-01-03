using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;
using UVtools.WPF.Controls.Tools;

namespace UVtools.WPF.Controls.Calibrators
{
    public class CalibrateExternalTestsControl : ToolControl
    {
        public OperationCalibrateExternalTests Operation => BaseOperation as OperationCalibrateExternalTests;
        public CalibrateExternalTestsControl()
        {
            this.InitializeComponent();
            BaseOperation = new OperationCalibrateExternalTests();
            Debug.WriteLine("asdasdasdasd");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonClicked(string url)
        {
            App.OpenBrowser(url);
        }
    }
}
