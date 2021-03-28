using System;
using Avalonia.Controls;
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

        public async void LoadPatternFromImage(bool isAlternatePattern = false)
        {
            var dialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Filters = Helpers.ImagesFileFilter,
            };
            var files = await dialog.ShowAsync(ParentWindow);
            if (files is null || files.Length == 0) return;
            Operation.LoadPatternFromImage(files[0], isAlternatePattern);
        }
    }
}
