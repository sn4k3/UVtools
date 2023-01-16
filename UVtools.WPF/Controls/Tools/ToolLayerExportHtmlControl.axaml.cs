using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System.IO;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public partial class ToolLayerExportHtmlControl : ToolControl
    {
        public OperationLayerExportHtml Operation => BaseOperation as OperationLayerExportHtml;
        public ToolLayerExportHtmlControl()
        {
            BaseOperation = new OperationLayerExportHtml(SlicerFile);
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
                        Extensions = new List<string>{"html"},
                        Name = "HTML files"
                    }
                },
                InitialFileName = Path.GetFileName(SlicerFile.FileFullPath) + ".html",
                Directory = Path.GetDirectoryName(SlicerFile.FileFullPath)
            };
            var file = await dialog.ShowAsync(ParentWindow);
            if (string.IsNullOrWhiteSpace(file)) return;
            Operation.FilePath = file;
        }
    }
}
