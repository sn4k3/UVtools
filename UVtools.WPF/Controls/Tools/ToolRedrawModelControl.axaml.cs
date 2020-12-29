using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolRedrawModelControl : ToolControl
    {
        public OperationRedrawModel Operation => BaseOperation as OperationRedrawModel;
        public ToolRedrawModelControl()
        {
            InitializeComponent();
            BaseOperation = new OperationRedrawModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public async void ImportFile()
        {
            var filters = Helpers.ToAvaloniaFileFilter(FileFormat.AllFileFiltersAvalonia);
            var orderedFilters = new List<FileDialogFilter> { filters[UserSettings.Instance.General.DefaultOpenFileExtensionIndex] };
            for (int i = 0; i < filters.Count; i++)
            {
                if (i == UserSettings.Instance.General.DefaultOpenFileExtensionIndex) continue;
                orderedFilters.Add(filters[i]);
            }

            var dialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Filters = orderedFilters,
                Directory = UserSettings.Instance.General.DefaultDirectoryOpenFile
            };
            var files = await dialog.ShowAsync(ParentWindow);
            if (files is null || files.Length <= 0) return;
            if (FileFormat.FindByExtension(files[0], true) is null) return;
            Operation.FilePath = files[0];
        }
    }
}
