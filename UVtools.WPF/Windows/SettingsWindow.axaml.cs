using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MessageBox.Avalonia.Enums;
using UVtools.Core.FileFormats;
using UVtools.WPF.Controls;
using UVtools.WPF.Extensions;
using Color = UVtools.WPF.Structures.Color;

namespace UVtools.WPF.Windows
{
    public class SettingsWindow : WindowEx
    {
        private double _scrollViewerMaxHeight;
        private int _selectedTabIndex;
        public UserSettings SettingsBackup { get; }

        public string[] FileOpenDialogFilters { get; }
        public string[] ZoomRanges { get; }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if(!RaiseAndSetIfChanged(ref _selectedTabIndex, value)) return;

                ScrollViewer scrollViewer = this.FindControl<ScrollViewer>($"ScrollViewer{_selectedTabIndex}");
                SizeToContent = SizeToContent.Manual;
                Height = MaxHeight;

                DispatcherTimer.RunOnce(() =>
                {
                    if (Math.Max((int)scrollViewer.Extent.Height - (int)scrollViewer.Viewport.Height, 0) == 0)
                    {
                        Height = 10;
                        SizeToContent = SizeToContent.Height;
                    }
                }, TimeSpan.FromMilliseconds(2));
                
            }
        }

        public double ScrollViewerMaxHeight
        {
            get => _scrollViewerMaxHeight;
            set => RaiseAndSetIfChanged(ref _scrollViewerMaxHeight, value);
        }

        public SettingsWindow()
        {
            Title += $" [v{App.VersionStr}]";
            SettingsBackup = UserSettings.Instance.Clone();

            var fileFormats = new List<string>
            {
                FileFormat.AllSlicerFiles.Replace("*", string.Empty)
            };
            fileFormats.AddRange(from format in FileFormat.AvailableFormats from extension in format.FileExtensions where extension.IsVisibleOnFileFilters select $"{extension.Description} (.{extension.Extension})");
            FileOpenDialogFilters = fileFormats.ToArray();


            // Derive strings for the zoom lock and crosshair fade combo-boxes from the
            // ZoomLevels constant array, and add those strings to the comboboxes.
            ZoomRanges = AppSettings.ZoomLevels.Skip(AppSettings.ZoomLevelSkipCount).Select(
                s => Convert.ToString(s / 100, CultureInfo.InvariantCulture) + "x").ToArray();

            ScrollViewerMaxHeight = this.GetScreenWorkingArea().Height - Settings.General.WindowsVerticalMargin;

            DataContext = this;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            //SizeToContent = SizeToContent.Manual;
            
            Position = new PixelPoint(
                (int)(App.MainWindow.Position.X + App.MainWindow.Width / 2 - Width / 2),
                App.MainWindow.Position.Y + 20
            );

            SelectedTabIndex = 1;
            SelectedTabIndex = 0;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (DialogResult != DialogResults.OK)
            {
                UserSettings.Instance = SettingsBackup;
            }
            else
            {
                UserSettings.Save();
            }
        }
        
        public async void GeneralOpenFolderField(string field)
        {
            foreach (PropertyInfo propertyInfo in Settings.General.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propertyInfo.Name != field) continue;
                OpenFolderDialog dialog = new();
                var filename = await dialog.ShowAsync(this);
                if (string.IsNullOrEmpty(filename)) return;
                propertyInfo.SetValue(Settings.General, filename);
                return;
            }

        }


        public void GeneralClearField(string field)
        {
            var properties = Settings.General.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo propertyInfo in properties)
            {
                if (propertyInfo.Name != field) continue;
                propertyInfo.SetValue(Settings.General, null);
                return;
            }
            
        }

        public async void SelectColor(string property)
        {
            foreach (var packObject in UserSettings.PackObjects)
            {
                var properties = packObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var propertyInfo in properties)
                {
                    if (propertyInfo.Name != property) continue;
                    var color = (Color)propertyInfo.GetValue(packObject, null) ?? new Color(255,255,255,255);
                    var window = new ColorPickerWindow(color.ToAvalonia());
                    var result = await window.ShowDialog<DialogResults>(this);
                    if (result != DialogResults.OK) return;
                    propertyInfo.SetValue(packObject, new Color(window.ResultColor));
                    return;
                }
            }
            
        }

        public async void OnClickResetAllDefaults()
        {
            var result = await this.MessageBoxQuestion("Are you sure you want to reset all settings to the default values?", "Reset settings?");
            if (result != ButtonResult.Yes) return;
            UserSettings.Reset();
            ResetDataContext();
        }

        public void OnClickSave()
        {
            DialogResult = DialogResults.OK;
            CloseWithResult();
        }
    }
}
