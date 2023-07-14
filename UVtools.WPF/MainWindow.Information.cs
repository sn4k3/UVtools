/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UVtools.Core.Dialogs;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Objects;
using UVtools.Core.SystemOS;
using UVtools.WPF.Extensions;
using UVtools.WPF.Structures;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using AvaloniaStatic = UVtools.WPF.Controls.AvaloniaStatic;
using Avalonia.Platform.Storage;

namespace UVtools.WPF;

public partial class MainWindow
{
    public RangeObservableCollection<SlicerProperty> SlicerProperties { get; } = new();

    private int _visibleThumbnailIndex = -1;
    private Bitmap _visibleThumbnailImage;
    private RangeObservableCollection<ValueDescription> _currentLayerProperties = new();

    public RangeObservableCollection<ValueDescription> CurrentLayerProperties
    {
        get => _currentLayerProperties;
        set => RaiseAndSetIfChanged(ref _currentLayerProperties, value);
    }

    public void InitInformation()
    {
        PropertiesGrid.KeyUp += GridOnKeyUp;
        CurrentLayerGrid.KeyUp += GridOnKeyUp;
        /*CurrentLayerGrid.BeginningEdit += (sender, e) =>
        {
            if (e.Row.DataContext is StringTag stringTag)
            {
                if (e.Column.DisplayIndex == 0
                    || e.Row.DataContext.ToString() != nameof(LayerCache.Layer.ExposureTime)
                    && e.Row.DataContext.ToString() != nameof(LayerCache.Layer.LightPWM)
                )
                {
                    e.Cancel = true;
                }
            }
            else
            {
                e.Cancel = true;
            }
        };
        CurrentLayerGrid.RowEditEnding += (sender, e) =>
        {
            if (e.EditAction == DataGridEditAction.Cancel) return;
            if (!(e.Row.DataContext is StringTag stringTag)) return;
            if (float.TryParse(stringTag.TagString, out var result)) return;
            e.Cancel = true;
        };
        CurrentLayerGrid.RowEditEnded += (sender, e) =>
        {
            if (e.EditAction == DataGridEditAction.Cancel) return;
            if (!(e.Row.DataContext is StringTag stringTag)) return;
            switch (stringTag.Content)
            {
                //case nameof(LayerCache.)
            }
        };*/

    }

    private void GridOnKeyUp(object? sender, KeyEventArgs e)
    {
        if (sender is DataGrid dataGrid)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    dataGrid.SelectedItems.Clear();
                    break;
                case Key.Multiply:
                    foreach (var item in dataGrid.ItemsSource)
                    {
                        if (dataGrid.SelectedItems.Contains(item))
                            dataGrid.SelectedItems.Remove(item);
                        else
                            dataGrid.SelectedItems.Add(item);
                    }

                    break;
            }
        }
    }

    #region Thumbnails
    public int VisibleThumbnailIndex
    {
        get => _visibleThumbnailIndex;
        set
        {
            if (value < 0)
            {
                RaiseAndSetIfChanged(ref _visibleThumbnailIndex, value);
                VisibleThumbnailImage = null;
                return;
            }

            if (!IsFileLoaded) return;
            if (!RaiseAndSetIfChanged(ref _visibleThumbnailIndex, value)) return;
            if (value >= SlicerFile.CreatedThumbnailsCount) return;
            if (SlicerFile.Thumbnails[value] is null || SlicerFile.Thumbnails[value].IsEmpty) return;
            
            VisibleThumbnailImage = SlicerFile.Thumbnails[value].ToBitmapParallel();
        }
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
        if (!IsFileLoaded) return;
        if (SlicerFile?.Thumbnails[_visibleThumbnailIndex] is null)
        {
            return; // This should never happen!
        }

        using var file = await SaveFilePickerAsync(SlicerFile.DirectoryPath, $"{SlicerFile.FilenameNoExt}_thumbnail{_visibleThumbnailIndex+1}.png", AvaloniaStatic.PngFileFilter);
        if (file?.TryGetLocalPath() is not { } filePath) return;
        SlicerFile.Thumbnails[_visibleThumbnailIndex].Save(filePath);
    }

    public async void OnClickThumbnailImportFile(object replaceAllObj)
    {
        if (!IsFileLoaded) return;
        var replaceAll = Convert.ToBoolean(replaceAllObj);
        if (replaceAll)
        {
            if (SlicerFile.ThumbnailsCount == 0) return;
        }
        else
        {
            if (_visibleThumbnailIndex < 0) return;
            if (SlicerFile.Thumbnails[_visibleThumbnailIndex] is null)
            {
                return; // This should never happen!
            }
        }


        var files = await App.MainWindow.OpenFilePickerAsync(AvaloniaStatic.ImagesFileFilter);
        if (files.Count == 0 || files[0].TryGetLocalPath() is not { } filePath) return;

        bool result;

        if (replaceAll)
        {
            result = SlicerFile.SetThumbnails(filePath);
        }
        else
        {
            result = SlicerFile.SetThumbnail(_visibleThumbnailIndex, filePath);
        }

        if (result) CanSave = true;
    }

    public void OnClickThumbnailImportCurrentLayer(object replaceAllObj)
    {
        if (!IsFileLoaded) return;
        if (!LayerCache.IsCached || SlicerFile.DecodeType == FileFormat.FileDecodeType.Partial) return;
        var replaceAll = Convert.ToBoolean(replaceAllObj);
        if (replaceAll)
        {
            if (SlicerFile.ThumbnailsCount == 0) return;
        }
        else
        {
            if (_visibleThumbnailIndex < 0) return;
            if (SlicerFile.Thumbnails[_visibleThumbnailIndex] is null)
            {
                return; // This should never happen!
            }
        }

        using var matRoi = new Mat(LayerCache.Image, LayerCache.Layer.GetBoundingRectangle(50, 100));
        using var thumbnailMat = new Mat();
        CvInvoke.CvtColor(matRoi, thumbnailMat, ColorConversion.Gray2Bgr);

        bool result;

        if (replaceAll)
        {
            result = SlicerFile.SetThumbnails(thumbnailMat);
        }
        else
        {
            result = SlicerFile.SetThumbnail(_visibleThumbnailIndex, thumbnailMat);
        }

        if (result) CanSave = true;
    }

    public void OnClickThumbnailImportRandomLayer(object replaceAllObj)
    {
        if (!IsFileLoaded) return;
        if (SlicerFile.LayerCount == 0 || SlicerFile.DecodeType == FileFormat.FileDecodeType.Partial) return;
        var replaceAll = Convert.ToBoolean(replaceAllObj);
        if (replaceAll)
        {
            if (SlicerFile.ThumbnailsCount == 0) return;
        }
        else
        {
            if (_visibleThumbnailIndex < 0) return;
            if (SlicerFile.Thumbnails[_visibleThumbnailIndex] is null)
            {
                return; // This should never happen!
            }
        }

        var layer = SlicerFile[Random.Shared.Next((int) SlicerFile.LayerCount)];
        using var matRoi = layer.GetLayerMatBoundingRectangle(50, 100);
        CvInvoke.CvtColor(matRoi.RoiMat, matRoi.RoiMat, ColorConversion.Gray2Bgr);

        bool result;

        if (replaceAll)
        {
            result = SlicerFile.SetThumbnails(matRoi.RoiMat);
        }
        else
        {
            result = SlicerFile.SetThumbnail(_visibleThumbnailIndex, matRoi.RoiMat);
        }

        if (result) CanSave = true;
    }

    public async void OnClickThumbnailImportHeatmap(object replaceAllObj)
    {
        if (!IsFileLoaded) return;
        if (SlicerFile.DecodeType == FileFormat.FileDecodeType.Partial) return;
        var replaceAll = Convert.ToBoolean(replaceAllObj);
        if (replaceAll)
        {
            if (SlicerFile.ThumbnailsCount == 0) return;
        }
        else
        {
            if (_visibleThumbnailIndex < 0) return;
            if (SlicerFile.Thumbnails[_visibleThumbnailIndex] is null)
            {
                return; // This should never happen!
            }
        }

        IsGUIEnabled = false;
        ShowProgressWindow("Generating heatmap");

        Mat? mat = null;

        try
        {
            mat = await SlicerFile.GenerateHeatmapAsync(SlicerFile.GetBoundingRectangle(100, 50, Progress), Progress);
            CvInvoke.CvtColor(mat, mat, ColorConversion.Gray2Bgr);
        }
        catch (OperationCanceledException)
        { }
        catch (Exception exception)
        {
            await this.MessageBoxError(exception.ToString(), "Error while generating the heatmap");
        }

        IsGUIEnabled = true;

        if (mat is null) return;

        bool result;

        if (replaceAll)
        {
            result = SlicerFile.SetThumbnails(mat);
        }
        else
        {
            result = SlicerFile.SetThumbnail(_visibleThumbnailIndex, mat);
        }

        mat.Dispose();

        if(result) CanSave = true;
    }

    public void RefreshThumbnail()
    {
        if (!IsFileLoaded) return;
        if (_visibleThumbnailIndex < 0) return;
        if (SlicerFile.Thumbnails[_visibleThumbnailIndex] is null)
        {
            return; 
        }
        VisibleThumbnailImage = SlicerFile.Thumbnails[_visibleThumbnailIndex].ToBitmapParallel();
    }
    #endregion

    #region Slicer Properties

    public async void OnClickPropertiesSaveFile()
    {
        if (SlicerFile?.Configs is null) return;

        using var file = await SaveFilePickerAsync(SlicerFile.DirectoryPath, $"{SlicerFile.FilenameNoExt}_properties.ini", AvaloniaStatic.IniFileFilter);

        if (file?.TryGetLocalPath() is not { } filePath) return;

        
        try
        {
            await using TextWriter tw = new StreamWriter(filePath);
            foreach (var config in SlicerFile.Configs)
            {
                var type = config.GetType();
                await tw.WriteLineAsync($"[{type.Name}]");
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.Name.Equals("Item")) continue;
                    var value = property.GetValue(config);
                    switch (value)
                    {
                        case null:
                            continue;
                        case IList list:
                            await tw.WriteLineAsync($"{property.Name} = {list.Count}");
                            break;
                        default:
                            await tw.WriteLineAsync($"{property.Name} = {value}");
                            break;
                    }
                }
                await tw.WriteLineAsync();
            }
            tw.Close();
        }
        catch (Exception e)
        {
            await this.MessageBoxError(e.ToString(), "Error occur while save properties");
            return;
        }

        var result = await this.MessageBoxQuestion(
            "Properties save was successful. Do you want open the file in the default editor?",
            "Properties save complete");
        if (result != MessageButtonResult.Yes) return;

        SystemAware.StartProcess(filePath);
    }

    public void OnClickPropertiesSaveClipboard()
    {
        if (SlicerFile?.Configs is null) return;
        var sb = new StringBuilder();
        foreach (var config in SlicerFile.Configs)
        {
            var type = config.GetType();
            sb.AppendLine($"[{type.Name}]");
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.Name.Equals("Item")) continue;
                var value = property.GetValue(config);
                switch (value)
                {
                    case null:
                        continue;
                    case IList list:
                        sb.AppendLine($"{property.Name} = {list.Count}");
                        break;
                    default:
                        sb.AppendLine($"{property.Name} = {value}");
                        break;
                }
            }

            sb.AppendLine();
        }

        Clipboard?.SetTextAsync(sb.ToString());
    }

    public void RefreshProperties()
    {
        SlicerProperties.Clear();
        foreach (var config in SlicerFile.Configs)
        {
            var type = config.GetType();
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.Name.Equals("Item")) continue;
                var value = property.GetValue(config);
                switch (value)
                {
                    case null:
                        continue;
                    case IList list:
                        SlicerProperties.Add(new SlicerProperty(property.Name, list.Count.ToString(),
                            config.GetType().Name));
                        break;
                    default:
                        SlicerProperties.Add(
                            new SlicerProperty(property.Name, value.ToString(), config.GetType().Name));
                        break;
                }
            }
        }
    }
    #endregion

    #region Current Layer

    public void RefreshCurrentLayerData()
    {
            
        var layer = LayerCache.Layer;
        CurrentLayerProperties.Clear();
        CurrentLayerProperties.Add(new ValueDescription($"{layer.Index}", nameof(layer.Index)));
        CurrentLayerProperties.Add(new ValueDescription($"{Layer.ShowHeight(layer.LayerHeight)}mm", nameof(layer.LayerHeight)));
        CurrentLayerProperties.Add(new ValueDescription($"{Layer.ShowHeight(layer.PositionZ)}mm", nameof(layer.PositionZ)));
        CurrentLayerProperties.Add(new ValueDescription(layer.IsBottomLayer.ToString(), nameof(layer.IsBottomLayer)));
        CurrentLayerProperties.Add(new ValueDescription(layer.IsModified.ToString(), nameof(layer.IsModified)));

        if (SlicerFile.CanUseExposureTime)
            CurrentLayerProperties.Add(new ValueDescription($"{layer.ExposureTime:F2}s", nameof(layer.ExposureTime)));

        if (SlicerFile.SupportPerLayerSettings)
        {
            if (SlicerFile.CanUseLayerLiftHeight)
                CurrentLayerProperties.Add(new ValueDescription($"{layer.LiftHeight.ToString(CultureInfo.InvariantCulture)}mm @ {layer.LiftSpeed.ToString(CultureInfo.InvariantCulture)}mm/min", nameof(layer.LiftHeight)));
            if (SlicerFile.CanUseLayerLiftHeight2)
                CurrentLayerProperties.Add(new ValueDescription($"{layer.LiftHeight2.ToString(CultureInfo.InvariantCulture)}mm @ {layer.LiftSpeed2.ToString(CultureInfo.InvariantCulture)}mm/min", nameof(layer.LiftHeight2)));

            if (SlicerFile.CanUseLayerRetractSpeed)
                CurrentLayerProperties.Add(new ValueDescription($"{layer.RetractHeight.ToString(CultureInfo.InvariantCulture)}mm @ {layer.RetractSpeed}mm/min", nameof(layer.RetractHeight)));
            if (SlicerFile.CanUseLayerRetractHeight2)
                CurrentLayerProperties.Add(new ValueDescription($"{layer.RetractHeight2.ToString(CultureInfo.InvariantCulture)}mm @ {layer.RetractSpeed2}mm/min", nameof(layer.RetractHeight2)));

            if (SlicerFile.CanUseLayerLightOffDelay)
                CurrentLayerProperties.Add(new ValueDescription($"{layer.LightOffDelay}s", nameof(layer.LightOffDelay)));

            if (SlicerFile.CanUseLayerWaitTimeBeforeCure)
                CurrentLayerProperties.Add(new ValueDescription($"{layer.WaitTimeBeforeCure}/{layer.WaitTimeAfterCure}/{layer.WaitTimeAfterLift}s", "WaitTimes:"));

            if (SlicerFile.CanUseLayerLightPWM)
                CurrentLayerProperties.Add(new ValueDescription(layer.LightPWM.ToString(), nameof(layer.LightPWM)));
        }
        var materialMillilitersPercent = layer.MaterialMillilitersPercent;
        if (layer.MaterialMilliliters > 0 && !float.IsNaN(materialMillilitersPercent))
        {
            CurrentLayerProperties.Add(new ValueDescription($"{layer.MaterialMilliliters}ml ({materialMillilitersPercent:F2}%)", nameof(layer.MaterialMilliliters)));
        }

    }
    #endregion
}