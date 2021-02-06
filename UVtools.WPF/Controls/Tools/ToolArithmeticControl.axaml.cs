using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolArithmeticControl : ToolControl
    {
        public OperationArithmetic Operation => BaseOperation as OperationArithmetic;

        public ToolArithmeticControl()
        {
            InitializeComponent();
            BaseOperation = new OperationArithmetic(SlicerFile);
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
