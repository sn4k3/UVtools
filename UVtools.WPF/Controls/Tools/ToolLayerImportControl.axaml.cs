using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolLayerImportControl : ToolControl
    {
        private bool _isAutoSortLayersByFileNameChecked;
        private ValueDescription _selectedFile;
        private Bitmap _previewImage;

        private ListBox FilesListBox;

        public OperationLayerImport Operation => BaseOperation as OperationLayerImport;

        public uint MaximumLayer => App.SlicerFile.LastLayerIndex;

        public string InfoLayerHeightStr => $"({App.SlicerFile.GetHeightFromLayer(Operation.StartLayerIndex)}mm)";

        public bool IsAutoSortLayersByFileNameChecked
        {
            get => _isAutoSortLayersByFileNameChecked;
            set => RaiseAndSetIfChanged(ref _isAutoSortLayersByFileNameChecked, value);
        }

        public string InfoImportResult 
        {
            get
            {
                if (Operation.Files.Count <= 0) return null;
                /*uint modelTotalLayers = (uint) Operation.Files.Count;//Operation.CalculateTotalLayers(App.SlicerFile.LayerCount);
                string textFactor = "grow";
                if (modelTotalLayers < App.SlicerFile.LayerCount)
                {
                    textFactor = "shrink";
                }
                else if (modelTotalLayers == App.SlicerFile.LayerCount)
                {
                    textFactor = "keep";
                }*/

                return
                    $"{Operation.Files.Count} files will be imported into model starting from layer {Operation.StartLayerIndex} {InfoLayerHeightStr}.";
                    //$"Model will {textFactor} from layers {App.SlicerFile.LayerCount} ({App.SlicerFile.TotalHeight}mm) to {modelTotalLayers} ({App.SlicerFile.GetHeightFromLayer(modelTotalLayers, false)}mm)";
            }
            
        }

        public ValueDescription SelectedFile
        {
            get => _selectedFile;
            set
            {
                if(!RaiseAndSetIfChanged(ref _selectedFile, value)) return;
                if (_selectedFile is null)
                {
                    PreviewImage = null;
                    return;
                }
                if (!_selectedFile.ValueAsString.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                    !_selectedFile.ValueAsString.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) &&
                    !_selectedFile.ValueAsString.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) &&
                    !_selectedFile.ValueAsString.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) &&
                    !_selectedFile.ValueAsString.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)) return;
                PreviewImage = new Bitmap(_selectedFile.ValueAsString);
            }
        }

        public Bitmap PreviewImage
        {
            get => _previewImage;
            set => RaiseAndSetIfChanged(ref _previewImage, value);
        }


        public ToolLayerImportControl()
        {
            BaseOperation = new OperationLayerImport(SlicerFile);
            if (!ValidateSpawn()) return;
            InitializeComponent();

            FilesListBox = this.Find<ListBox>("FilesListBox");
            FilesListBox.DoubleTapped += (sender, args) =>
            {
                if (FilesListBox.SelectedItem is not ValueDescription file) return;
                App.StartProcess(file.ValueAsString);
            };
            FilesListBox.KeyUp += (sender, e) =>
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        FilesListBox.SelectedItems.Clear();
                        e.Handled = true;
                        break;
                    case Key.Delete:
                        RemoveFiles();
                        e.Handled = true;
                        break;
                    case Key.A:
                        if ((e.KeyModifiers & KeyModifiers.Control) != 0)
                        {
                            FilesListBox.SelectAll();
                            e.Handled = true;
                        }
                        break;
                }
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /*public override async Task<bool> ValidateForm()
        {
            UpdateOperation();
            var message = Operation.Validate();
            if (message is null) return true;


            message.Content += "\nDo you want to remove all invalid files from list?";
            if (await ParentWindow.MessageBoxQuestion(message.ToString()) == ButtonResult.Yes)
            {
                ConcurrentBag<StringTag> result = (ConcurrentBag<StringTag>)message.Tag;
                foreach (var file in result)
                {
                    Operation.Files.Remove(file);
                }
            }

            return false;
        }*/

        public override void Callback(ToolWindow.Callbacks callback)
        {
            switch (callback)
            {
                case ToolWindow.Callbacks.Init:
                case ToolWindow.Callbacks.Loaded:
                    RefreshGUI();
                    Operation.Files.CollectionChanged += (sender, args) => RefreshGUI();
                    Operation.PropertyChanged += (sender, args) => RefreshGUI();
                    break;
            }
        }

        public void RefreshGUI()
        {
            RaisePropertyChanged(nameof(InfoLayerHeightStr));
            RaisePropertyChanged(nameof(InfoImportResult));
            if(ParentWindow is not null) ParentWindow.ButtonOkEnabled = Operation.Files.Count > 0;
        }

        public async void AddFiles()
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
                AllowMultiple = true,
                Filters = orderedFilters,
                Directory = UserSettings.Instance.General.DefaultDirectoryOpenFile
            };

            var files = await dialog.ShowAsync(ParentWindow);
            if (files is null || files.Length == 0) return;
            foreach (var filename in files)
            {
                Operation.AddFile(filename);
            }

            if (_isAutoSortLayersByFileNameChecked)
            {
                Operation.Sort();
            }
        }

        public void RemoveFiles()
        {
            Operation.Files.RemoveRange(FilesListBox.SelectedItems.OfType<ValueDescription>());
        }

        public void ClearFiles()
        {
            Operation.Files.Clear();
            PreviewImage = null;
        }
    }
}
