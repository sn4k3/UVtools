using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MessageBox.Avalonia.Enums;
using UVtools.Core.FileFormats;
using UVtools.Core.Network;
using UVtools.WPF.Controls;
using UVtools.WPF.Extensions;
using Color = UVtools.WPF.Structures.Color;

namespace UVtools.WPF.Windows
{
    public class SettingsWindow : WindowEx
    {
        private double _scrollViewerMaxHeight;
        private int _selectedTabIndex;
        private DataGrid _sendToCustomLocationsGrid;
        private ComboBox _networkRemotePrinterComboBox;

        public int MaxProcessorCount => Environment.ProcessorCount;

        public UserSettings SettingsBackup { get; }

        public string[] FileOpenDialogFilters { get; }
        public string[] ZoomRanges { get; }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if(!RaiseAndSetIfChanged(ref _selectedTabIndex, value)) return;

                var scrollViewer = this.FindControl<ScrollViewer>($"ScrollViewer{_selectedTabIndex}");
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

            _sendToCustomLocationsGrid = this.FindControl<DataGrid>("SendToCustomLocationsGrid");
            _networkRemotePrinterComboBox = this.FindControl<ComboBox>("NetworkRemotePrinterComboBox");
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
            DispatcherTimer.RunOnce(() => SelectedTabIndex = 0, TimeSpan.FromMilliseconds(1));
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

        public void SetMaxDegreeOfParallelism(string to)
        {
            if (to == "*")
            {
                Settings.General.MaxDegreeOfParallelism = MaxProcessorCount;
                return;
            }

            if (to == "!")
            {
                Settings.General.MaxDegreeOfParallelism = Math.Max(1, MaxProcessorCount-2);
                return;
            }

            if (int.TryParse(to, out var i))
            {
                Settings.General.MaxDegreeOfParallelism = i;
                return;
            }

            if (decimal.TryParse(to, out var d))
            {
                Settings.General.MaxDegreeOfParallelism = (int)(MaxProcessorCount * d);
                return;
            }
        }


        public async void GeneralOpenFolderField(string field)
        {
            foreach (var propertyInfo in Settings.General.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
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
            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.Name != field) continue;
                propertyInfo.SetValue(Settings.General, null);
                return;
            }
            
        }

        public async void SendToAddCustomLocation()
        {
            var openFolder = new OpenFolderDialog();
            var result = await openFolder.ShowAsync(this);

            if (string.IsNullOrWhiteSpace(result)) return;
            var directory = new MappedDevice(result);
            if (Settings.General.SendToCustomLocations.Contains(directory))
            {
                await this.MessageBoxError("The selected location already exists on the list:\n" +
                                     $"{result}");
                return;
            }

            Settings.General.SendToCustomLocations.Add(directory);
        }

        public async void SendToRemoveCustomLocations()
        {
            if (_sendToCustomLocationsGrid.SelectedItems.Count == 0) return;

            if (await this.MessageBoxQuestion(
                    $"Are you sure you want to remove the {_sendToCustomLocationsGrid.SelectedItems.Count} selected entries?") !=
                ButtonResult.Yes) return;

            Settings.General.SendToCustomLocations.RemoveRange(_sendToCustomLocationsGrid.SelectedItems.Cast<MappedDevice>());
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

        public async void AddNetworkRemotePrinter()
        {
            var result = await this.MessageBoxQuestion("Are you sure you want to add a new remote printer", "Add new remote printer?");
            if (result != ButtonResult.Yes) return;

            var remotePrinter = new RemotePrinter
            {
                Name = "My new remote printer"
            };

            Settings.Network.RemotePrinters.Add(remotePrinter);
            _networkRemotePrinterComboBox.SelectedItem = remotePrinter;
        }

        public async void RemoveSelectedNetworkRemotePrinter()
        {
            if (_networkRemotePrinterComboBox.SelectedItem is not RemotePrinter remotePrinter) return;
            var result = await this.MessageBoxQuestion("Are you sure you want to remove the following remote printer?\n" +
                                                       remotePrinter, "Remove remote printer?");
            if (result != ButtonResult.Yes) return;
            Settings.Network.RemotePrinters.Remove(remotePrinter);
        }

        public async void DuplicateSelectedNetworkRemotePrinter()
        {
            if (_networkRemotePrinterComboBox.SelectedItem is not RemotePrinter remotePrinter) return;
            var result = await this.MessageBoxQuestion("Are you sure you want to duplicate the following remote printer?\n" +
                                                       remotePrinter, "Duplicate remote printer?");
            if (result != ButtonResult.Yes) return;
            var clone = remotePrinter.Clone();
            clone.Name += " Duplicated";
            Settings.Network.RemotePrinters.Add(clone);
            _networkRemotePrinterComboBox.SelectedItem = clone;
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
