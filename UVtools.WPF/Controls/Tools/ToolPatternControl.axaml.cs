using System;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolPatternControl : ToolControl
    {
        public OperationPattern Operation => BaseOperation as OperationPattern;
        private bool _isDefaultAnchorChecked = true;

        public bool IsDefaultAnchorChecked
        {
            get => _isDefaultAnchorChecked;
            set => RaiseAndSetIfChanged(ref _isDefaultAnchorChecked, value);
        }

        public ToolPatternControl()
        {
            InitializeComponent();
            var roi = App.MainWindow.ROI;
            BaseOperation = new OperationPattern(SlicerFile, roi);

            if (Operation.MaxRows < 2 && Operation.MaxCols < 2)
            {
                App.MainWindow.MessageBoxInfo("The available free volume is not enough to pattern this object.\n" +
                                              "To run this tool the free space must allow at least 1 copy.", "Unable to pattern");
                CanRun = false;
                return;
            }
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
                case ToolWindow.Callbacks.Loaded:
                    Operation.ROI = App.MainWindow.ROI.IsEmpty ? SlicerFile.BoundingRectangle : App.MainWindow.ROI;
                    Operation.PropertyChanged += (sender, e) =>
                    {
                        if (e.PropertyName.Equals(nameof(Operation.IsWithinBoundary)))
                        {
                            ParentWindow.ButtonOkEnabled = Operation.IsWithinBoundary;
                        }
                    };
                    break;
                case ToolWindow.Callbacks.ClearROI:
                    Operation.SetRoi(SlicerFile.BoundingRectangle);
                    break;
            }
        }
    }
}
