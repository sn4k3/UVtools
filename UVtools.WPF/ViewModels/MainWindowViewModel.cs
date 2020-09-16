using System.Diagnostics;
using Avalonia.Controls;
using JetBrains.Annotations;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
using UVtools.Core.Objects;
using UVtools.WPF.Extensions;

namespace UVtools.WPF.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private MainWindow Parent;
        public bool IsFileLoaded => !ReferenceEquals(App.SlicerFile, null);

        public MainWindowViewModel(MainWindow parent)
        {
            Parent = parent;
        }

        public async void MenuFileOpenClicked()
        {
            var dialog = new OpenFileDialog
            {
                AllowMultiple = false,
            };
            var files = await dialog.ShowAsync(Parent);
            Parent.ProcessFiles(files);
        }
    }
}
