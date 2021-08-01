using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolLayerExportGifControl : ToolControl
    {
        public OperationLayerExportGif Operation => BaseOperation as OperationLayerExportGif;
        public ToolLayerExportGifControl()
        {
            BaseOperation = new OperationLayerExportGif(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public async void ChooseFilePath()
        {
            var dialog = new SaveFileDialog
            {
                Filters = new List<FileDialogFilter>
                {
                    new()
                    {
                        Extensions = new List<string>{"gif"},
                        Name = "GIF files"
                    }
                },
                InitialFileName = Path.GetFileName(SlicerFile.FileFullPath)+".gif",
                Directory = Path.GetDirectoryName(SlicerFile.FileFullPath)
            };
            var file = await dialog.ShowAsync(ParentWindow);
            if (string.IsNullOrWhiteSpace(file)) return;
            Operation.FilePath = file;
        }
    }
}
