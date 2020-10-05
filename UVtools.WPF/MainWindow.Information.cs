/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Emgu.CV.CvEnum;
using MessageBox.Avalonia.Enums;
using UVtools.Core.PixelEditor;
using UVtools.WPF.Controls;
using UVtools.WPF.Extensions;
using UVtools.WPF.Structures;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Point = System.Drawing.Point;

namespace UVtools.WPF
{
    public partial class MainWindow
    {
        public ObservableCollection<SlicerProperty> SlicerProperties { get; } = new ObservableCollection<SlicerProperty>();

        private uint _visibleThumbnailIndex;
        private Bitmap _visibleThumbnailImage;

        #region Thumbnails
        public uint VisibleThumbnailIndex
        {
            get => _visibleThumbnailIndex;
            set
            {
                if (value == 0)
                {
                    RaiseAndSetIfChanged(ref _visibleThumbnailIndex, value);
                    RaisePropertyChanged(nameof(ThumbnailCanGoPrevious));
                    RaisePropertyChanged(nameof(ThumbnailCanGoNext));
                    VisibleThumbnailImage = null;
                    return;
                }

                if (!IsFileLoaded) return;
                var index = value - 1;
                if (index >= SlicerFile.CreatedThumbnailsCount) return;
                if (SlicerFile.Thumbnails[index] is null) return;
                if (!RaiseAndSetIfChanged(ref _visibleThumbnailIndex, value)) return;

                VisibleThumbnailImage = SlicerFile.Thumbnails[index].ToBitmap();
                RaisePropertyChanged(nameof(ThumbnailCanGoPrevious));
                RaisePropertyChanged(nameof(ThumbnailCanGoNext));
            }
        }

        public bool ThumbnailCanGoPrevious => SlicerFile is { } && _visibleThumbnailIndex > 1;
        public bool ThumbnailCanGoNext => SlicerFile is { } && _visibleThumbnailIndex < SlicerFile.CreatedThumbnailsCount;

        public void ThumbnailGoPrevious()
        {
            if (!ThumbnailCanGoPrevious) return;
            VisibleThumbnailIndex--;
        }

        public void ThumbnailGoNext()
        {
            if (!ThumbnailCanGoNext) return;
            VisibleThumbnailIndex++;
        }

        public Bitmap VisibleThumbnailImage
        {
            get => _visibleThumbnailImage;
            set
            {
                RaiseAndSetIfChanged(ref _visibleThumbnailImage, value);
                RaisePropertyChanged(nameof(VisibleThumbnailResolution));
            }
        }

        public string VisibleThumbnailResolution => _visibleThumbnailImage is null ? null : $"{{Width: {_visibleThumbnailImage.Size.Width}, Height: {_visibleThumbnailImage.Size.Height}}}";

        public async void OnClickThumbnailSave()
        {
            if (SlicerFile is null) return;
            if (ReferenceEquals(SlicerFile.Thumbnails[_visibleThumbnailIndex - 1], null))
            {
                return; // This should never happen!
            }
            var dialog = new SaveFileDialog
            {
                Filters = Helpers.PngFileFilter,
                Directory = Path.GetDirectoryName(SlicerFile.FileFullPath),
                InitialFileName = $"{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}_thumbnail{_visibleThumbnailIndex}.png"
            };

            var filepath = await dialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(filepath))
            {
                SlicerFile.Thumbnails[_visibleThumbnailIndex - 1].Save(filepath);
            }
        }

        public async void OnClickThumbnailImport()
        {
            if (_visibleThumbnailIndex <= 0) return;
            if (ReferenceEquals(SlicerFile.Thumbnails[_visibleThumbnailIndex - 1], null))
            {
                return; // This should never happen!
            }

            var dialog = new OpenFileDialog
            {
                Filters = Helpers.ImagesFileFilter,
                AllowMultiple = false
            };

            var filepath = await dialog.ShowAsync(this);

            if (filepath is null || filepath.Length <= 0) return;
            int i = (int)(_visibleThumbnailIndex - 1);
            SlicerFile.SetThumbnail(i, filepath[0]);
            VisibleThumbnailImage = SlicerFile.Thumbnails[i].ToBitmap();
        }
        #endregion

        #region Slicer Properties

        public async void OnClickPropertiesSaveFile()
        {
            if (SlicerFile?.Configs is null) return;

            var dialog = new SaveFileDialog
            {
                Filters = Helpers.IniFileFilter,
                Directory = Path.GetDirectoryName(SlicerFile.FileFullPath),
                InitialFileName = $"{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}_properties.ini"
            };

            var file = await dialog.ShowAsync(this);

            if (string.IsNullOrEmpty(file)) return;

            try
            {
                using (TextWriter tw = new StreamWriter(file))
                {
                    foreach (var config in SlicerFile.Configs)
                    {
                        var type = config.GetType();
                        tw.WriteLine($"[{type.Name}]");
                        foreach (var property in type.GetProperties())
                        {
                            tw.WriteLine($"{property.Name} = {property.GetValue(config)}");
                        }

                        tw.WriteLine();
                    }

                    tw.Close();
                }
            }
            catch (Exception e)
            {
                await this.MessageBoxError(e.ToString(), "Error occur while save properties");
                return;
            }

            var result = await this.MessageBoxQuestion(
                "Properties save was successful. Do you want open the file in the default editor?",
                "Properties save complete");
            if (result != ButtonResult.Yes) return;

            App.StartProcess(file);
        }

        public void OnClickPropertiesSaveClipboard()
        {
            if (SlicerFile?.Configs is null) return;
            var sb = new StringBuilder();
            foreach (var config in SlicerFile.Configs)
            {
                var type = config.GetType();
                sb.AppendLine($"[{type.Name}]");
                foreach (var property in type.GetProperties())
                {
                    sb.AppendLine($"{property.Name} = {property.GetValue(config)}");
                }

                sb.AppendLine();
            }

            Application.Current.Clipboard.SetTextAsync(sb.ToString());
        }

        public void RefreshProperties()
        {
            SlicerProperties.Clear();
            if (!(SlicerFile.Configs is null))
            {
                foreach (var config in SlicerFile.Configs)
                {
                    var type = config.GetType();
                    foreach (var property in type.GetProperties())
                    {
                        SlicerProperties.Add(new SlicerProperty(property.Name, property.GetValue(config, null)?.ToString(), type.Name));
                    }
                }
            }
        }
        #endregion
    }
}
