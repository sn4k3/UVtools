using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolLayerArithmeticControl : ToolControl
    {
        public OperationLayerArithmetic Operation => BaseOperation as OperationLayerArithmetic;

        public ToolLayerArithmeticControl()
        {
            InitializeComponent();
            BaseOperation = new OperationLayerArithmetic(SlicerFile);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void Callback(ToolWindow.Callbacks callback)
        {
            switch (callback)
            {
                case ToolWindow.Callbacks.Init:
                case ToolWindow.Callbacks.ProfileLoaded:
                    ParentWindow.ButtonOkEnabled = !string.IsNullOrWhiteSpace(Operation.Sentence);
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
}
