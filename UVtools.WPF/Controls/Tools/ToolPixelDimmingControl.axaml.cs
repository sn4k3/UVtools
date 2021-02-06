using System;
using Avalonia.Markup.Xaml;
using Emgu.CV;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolPixelDimmingControl : ToolControl
    {
        public OperationPixelDimming Operation => BaseOperation as OperationPixelDimming;

       
        public ToolPixelDimmingControl()
        {
            InitializeComponent();
            BaseOperation = new OperationPixelDimming(SlicerFile);
            Operation.GeneratePixelDimming("Chessboard");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
      

    }
}
