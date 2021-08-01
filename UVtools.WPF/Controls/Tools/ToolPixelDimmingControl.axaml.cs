using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolPixelDimmingControl : ToolControl
    {
        public OperationPixelDimming Operation => BaseOperation as OperationPixelDimming;

       
        public ToolPixelDimmingControl()
        {
            BaseOperation = new OperationPixelDimming(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
            
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
