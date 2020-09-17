using System.Diagnostics;
using System.Reflection;
using Avalonia.Controls;
using JetBrains.Annotations;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
using UVtools.Core.Objects;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;

namespace UVtools.WPF.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private MainWindow Parent;

        private bool _isGUIEnabled = true;
        public bool IsGUIEnabled
        {
            get => _isGUIEnabled;
            set => SetProperty(ref _isGUIEnabled, value);
        }

        public bool IsFileLoaded
        {
            get => !ReferenceEquals(App.SlicerFile, null);
            set => SetProperty();
        }

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

        public async void MenuFileSettingsClicked()
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Title += $" [v{Assembly.GetEntryAssembly().GetName().Version}]";
            await settingsWindow.ShowDialog(Parent);
        }
    }
}
