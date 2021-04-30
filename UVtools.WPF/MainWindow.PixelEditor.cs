/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using MessageBox.Avalonia.Enums;
using SkiaSharp;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.PixelEditor;
using UVtools.WPF.Extensions;

namespace UVtools.WPF
{
    public partial class MainWindow
    {
        public RangeObservableCollection<PixelOperation> Drawings { get; } = new ();
        public DataGrid DrawingsGrid;
        private int _selectedPixelOperationTabIndex;

        public PixelDrawing DrawingPixelDrawing { get; } = new();
        public PixelText DrawingPixelText { get; } = new();
        public PixelEraser DrawingPixelEraser { get; } = new();
        public PixelSupport DrawingPixelSupport { get; } = new();
        public PixelDrainHole DrawingPixelDrainHole { get; } = new();

        public int SelectedPixelOperationTabIndex
        {
            get => _selectedPixelOperationTabIndex;
            set => RaiseAndSetIfChanged(ref _selectedPixelOperationTabIndex, value);
        }

        public void InitPixelEditor()
        {
            DrawingsGrid = this.FindControl<DataGrid>("DrawingsGrid");
            DrawingsGrid.KeyUp += DrawingsGridOnKeyUp;
            DrawingsGrid.SelectionChanged += DrawingsGridOnSelectionChanged;
            DrawingsGrid.CellPointerPressed += DrawingsGridOnCellPointerPressed;
        }

        private void DrawingsGridOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DrawingsGrid.SelectedItem is not PixelOperation operation)
            {
                ShowLayer();
                return;
            }

            Point location = GetTransposedPoint(operation.Location, true);

            if (Settings.LayerPreview.ZoomIssues ^ (_globalModifiers & KeyModifiers.Alt) != 0)
            {
                CenterLayerAt(new Rectangle(location, operation.Size), AppSettings.LockedZoomLevel);
            }
            else
            {
                CenterLayerAt(location);
            }


            ForceUpdateActualLayer(operation.LayerIndex);
        }

        private void DrawingsGridOnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
        {
            if (e.PointerPressedEventArgs.ClickCount == 2) return;
            if (DrawingsGrid.SelectedItem is not LayerIssue) return;
            // Double clicking an issue will center and zoom into the 
            // selected issue. Left click on an issue will zoom to fit.

            var pointer = e.PointerPressedEventArgs.GetCurrentPoint(DrawingsGrid);

            if (pointer.Properties.IsRightButtonPressed)
            {
                ZoomToFit();
                return;
            }

        }

        private void DrawingsGridOnKeyUp(object? sender, KeyEventArgs e)
        {
            
            switch (e.Key)
            {
                case Key.Escape:
                    DrawingsGrid.SelectedItems.Clear();
                    break;
                case Key.Multiply:
                    var selectedItems = DrawingsGrid.SelectedItems.OfType<PixelOperation>().ToList();
                    DrawingsGrid.SelectedItems.Clear();
                    foreach (PixelOperation item in Drawings)
                    {
                        if (!selectedItems.Contains(item))
                            DrawingsGrid.SelectedItems.Add(item);
                    }


                    break;
                case Key.Delete:
                    OnClickDrawingRemove();
                    break;
            }
        }

        public void OnClickDrawingRemove()
        {
            if (DrawingsGrid.SelectedItems.Count == 0) return;
            Drawings.RemoveRange(DrawingsGrid.SelectedItems.Cast<PixelOperation>());
            ShowLayer();
        }

        public async void OnClickDrawingClear()
        {
            if (Drawings.Count == 0) return;
            if (await this.MessageBoxQuestion($"Are you sure you want to clear {Drawings.Count} operations?",
                "Clear pixel editor operations?") != ButtonResult.Yes) return;
            Drawings.Clear();
            ShowLayer();
        }

        void DrawPixel(bool isAdd, Point location, KeyModifiers keyModifiers)
        {
            //Stopwatch sw = Stopwatch.StartNew();
            //var point = pbLayer.PointToImage(location);

            Point realLocation = GetTransposedPoint(location);

            if ((keyModifiers & KeyModifiers.Control) != 0)
            {
                if (Drawings.RemoveAll(operation =>
                {
                    Rectangle rect = new(operation.Location, operation.Size);
                    rect.X -= operation.Size.Width / 2;
                    rect.Y -= operation.Size.Height / 2;
                    return rect.Contains(realLocation);
                }) > 0)
                {
                    ShowLayer();
                }
                /*var removeItems = Drawings.Where(item =>
                {
                    Rectangle rect = new(item.Location, item.Size);
                    rect.X -= item.Size.Width / 2;
                    rect.Y -= item.Size.Height / 2;
                    return rect.Contains(realLocation);
                });
                if (removeItems.Any())
                {
                    Drawings.RemoveMany(removeItems);
                    ShowLayer();
                }*/
                
                return;
            }

            WriteableBitmap bitmap = (WriteableBitmap)LayerImageBox.Image;
            //var context = CreateRenderTarget().CreateDrawingContext(bitmap);


            //Bitmap bmp = pbLayer.Image as Bitmap;
            if (SelectedPixelOperationTabIndex == (byte)PixelOperation.PixelOperationType.Drawing)
            {
                uint minLayer = (uint) Math.Max(0, (int)_actualLayer - DrawingPixelDrawing.LayersBelow);
                uint maxLayer = Math.Min(SlicerFile.LastLayerIndex, _actualLayer + DrawingPixelDrawing.LayersAbove);
                for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
                {
                    var operationDrawing = new PixelDrawing(layerIndex, realLocation, DrawingPixelDrawing.LineType,
                        DrawingPixelDrawing.BrushShape, DrawingPixelDrawing.BrushSize, DrawingPixelDrawing.Thickness, DrawingPixelDrawing.RemovePixelBrightness, DrawingPixelDrawing.PixelBrightness, isAdd);

                    //if (PixelHistory.Contains(operation)) continue;
                    AddDrawing(operationDrawing);

                    if (layerIndex == _actualLayer)
                    {
                        var color = isAdd
                            ? Settings.PixelEditor.AddPixelColor
                            : Settings.PixelEditor.RemovePixelColor;

                        if (operationDrawing.BrushSize == 1)
                        {
                            unsafe
                            {
                                using var framebuffer = bitmap.Lock();
                                var data = (uint*)framebuffer.Address.ToPointer();
                                data[bitmap.GetPixelPos(location)] =
                                    color.ToUint32();
                            }
                            
                            LayerImageBox.InvalidateArrange();
                               // LayerCache.ImageBgr.SetByte(operationDrawing.Location.X, operationDrawing.Location.Y,
                                 //   new[] { color.B, color.G, color.R });
                            continue;
                        }

                        switch (operationDrawing.BrushShape)
                        {
                            case PixelDrawing.BrushShapeType.Rectangle:

                                int shiftPos = operationDrawing.BrushSize / 2;
                                LayerCache.Canvas.DrawRect(location.X - shiftPos, location.Y - shiftPos, 
                                    operationDrawing.BrushSize, 
                                    operationDrawing.BrushSize,
                                    new SKPaint
                                    {
                                        IsAntialias = operationDrawing.LineType == LineType.AntiAlias,
                                        Color = new SKColor(color.ToUint32()),
                                        IsStroke = operationDrawing.Thickness >= 0,
                                        StrokeWidth = operationDrawing.Thickness
                                    } );
                                /*CvInvoke.Rectangle(LayerCache.ImageBgr, GetTransposedRectangle(operationDrawing.Rectangle),
                                    new MCvScalar(color.B, color.G, color.R), operationDrawing.Thickness,
                                    operationDrawing.LineType);*/
                                break;
                            case PixelDrawing.BrushShapeType.Circle:

                               
                                LayerCache.Canvas.DrawCircle(location.X, location.Y, operationDrawing.BrushSize / 2f,
                                    new SKPaint
                                    {
                                        IsAntialias = operationDrawing.LineType == LineType.AntiAlias,
                                        Color = new SKColor(color.ToUint32()),
                                        IsStroke = operationDrawing.Thickness >= 0,
                                        StrokeWidth = operationDrawing.Thickness
                                    });

                                /*CvInvoke.Circle(LayerCache.ImageBgr, location, operationDrawing.BrushSize / 2,
                                    new MCvScalar(color.B, color.G, color.R), operationDrawing.Thickness,
                                    operationDrawing.LineType);*/
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        LayerImageBox.InvalidateVisual();
                        //RefreshLayerImage();
                    }
                }
            }
            else if (SelectedPixelOperationTabIndex == (byte)PixelOperation.PixelOperationType.Text)
            {
                if (string.IsNullOrEmpty(DrawingPixelText.Text) || DrawingPixelText.FontScale < 0.2) return;

                uint minLayer = (uint) Math.Max(0, (int)ActualLayer - DrawingPixelText.LayersBelow);
                uint maxLayer = Math.Min(SlicerFile.LastLayerIndex, ActualLayer + DrawingPixelText.LayersAbove);
                for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
                {
                    var operationText = new PixelText(layerIndex, realLocation, DrawingPixelText.LineType,
                        DrawingPixelText.Font, DrawingPixelText.FontScale, DrawingPixelText.Thickness,
                        DrawingPixelText.Text, DrawingPixelText.Mirror, DrawingPixelText.RemovePixelBrightness, DrawingPixelText.PixelBrightness, isAdd);

                    //if (PixelHistory.Contains(operation)) continue;
                    //PixelHistory.Add(operation);
                    AddDrawing(operationText);
                    
                    /*var color = isAdd
                        ? Settings.PixelEditor.AddPixelColor : Settings.PixelEditor.RemovePixelColor;

                    if (layerIndex == _actualLayer)
                    {
                        CvInvoke.PutText(LayerCache.ImageBgr, operationText.Text, location,
                            operationText.Font, operationText.FontScale, new MCvScalar(color.B, color.G, color.R),
                            operationText.Thickness, operationText.LineType, operationText.Mirror);
                        RefreshLayerImage();
                    }*/
                }

                ShowLayer();
                return;
            }
            else if (SelectedPixelOperationTabIndex == (byte)PixelOperation.PixelOperationType.Eraser)
            {
                if (LayerCache.Image.GetByte(realLocation) < 10) return;
                uint minLayer = (uint) Math.Max(0, (int)ActualLayer - DrawingPixelEraser.LayersBelow);
                uint maxLayer = Math.Min(SlicerFile.LastLayerIndex, ActualLayer + DrawingPixelEraser.LayersAbove);
                for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
                {
                    var operationEraser = new PixelEraser(layerIndex, realLocation, DrawingPixelEraser.PixelBrightness);

                    //if (PixelHistory.Contains(operation)) continue;
                    AddDrawing(operationEraser);

                    /*if (layerIndex == _actualLayer)
                    {
                        for (int i = 0; i < LayerCache.LayerContours.Size; i++)
                        {
                            if (CvInvoke.PointPolygonTest(LayerCache.LayerContours[i], operationEraser.Location, false) >= 0)
                            {
                                CvInvoke.DrawContours(LayerCache.ImageBgr, LayerCache.LayerContours, i,
                                    new MCvScalar(Settings.PixelEditor.RemovePixelColor.B, Settings.PixelEditor.RemovePixelColor.G, Settings.PixelEditor.RemovePixelColor.R), -1);
                                RefreshLayerImage();
                                break;
                            }
                        }
                    }*/
                }

                ShowLayer();
                return;
            }
            else if (SelectedPixelOperationTabIndex == (byte)PixelOperation.PixelOperationType.Supports)
            {
                if (_actualLayer == 0) return;
                var operationSupport = new PixelSupport(ActualLayer, realLocation,
                    DrawingPixelSupport.TipDiameter, DrawingPixelSupport.PillarDiameter,
                    DrawingPixelSupport.BaseDiameter, DrawingPixelSupport.PixelBrightness);

                //if (PixelHistory.Contains(operation)) return;
                AddDrawing(operationSupport);

                CvInvoke.Circle(LayerCache.ImageBgr, location, operationSupport.TipDiameter / 2,
                    new MCvScalar(Settings.PixelEditor.SupportsColor.B, Settings.PixelEditor.SupportsColor.G, Settings.PixelEditor.SupportsColor.R), -1);
                RefreshLayerImage();
            }
            else if (SelectedPixelOperationTabIndex == (byte)PixelOperation.PixelOperationType.DrainHole)
            {
                if (_actualLayer == 0) return;
                var operationDrainHole = new PixelDrainHole(ActualLayer, realLocation, DrawingPixelDrainHole.Diameter);

                //if (PixelHistory.Contains(operation)) return;
                AddDrawing(operationDrainHole);

                CvInvoke.Circle(LayerCache.ImageBgr, location, operationDrainHole.Diameter / 2,
                    new MCvScalar(Settings.PixelEditor.DrainHoleColor.B, Settings.PixelEditor.DrainHoleColor.G, Settings.PixelEditor.DrainHoleColor.R), -1);
                RefreshLayerImage();
            }
            else
            {
                throw new NotImplementedException("Missing pixel operation");
            }
        }

        public void AddDrawing(PixelOperation operation)
        {
            Drawings.Insert(0, operation);
            for (int i = 0; i < Drawings.Count; i++)
            {
                Drawings[i].Index = (uint) (Drawings.Count - i);
            }
        }

        public async void DrawModifications(bool exitEditor)
        {
            if (Drawings.Count == 0)
            {
                if (exitEditor && !ReferenceEquals(LastSelectedTabItem, TabPixelEditor))
                {
                    SelectedTabItem = LastSelectedTabItem;
                }

                return;
            }

            ButtonResult result;

            if (exitEditor)
            {
                result = await this.MessageBoxQuestion(
                    "There are edit operations that have not been applied.  " +
                    "Would you like to apply all operations before closing the editor?",
                    "Closing image editor?", ButtonEnum.YesNoCancel);
            }
            else
            {

                result = await this.MessageBoxQuestion(
                    "Are you sure you want to apply all operations?",
                    "Apply image editor changes?");

                // For the "apply" case, We aren't exiting the editor, so map "No" to "Cancel" here
                // in order to prevent pixel history from being cleared.
                result = result == ButtonResult.No ? ButtonResult.Cancel : ButtonResult.Yes;
            }

            if (result == ButtonResult.Cancel)
            {
                IsPixelEditorActive = true;
                return;
            }

            if (result == ButtonResult.Yes)
            {
                IsGUIEnabled = false;
                ShowProgressWindow("Drawing pixels");
                Clipboard.Snapshot();

                var task = await Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        SlicerFile.LayerManager.DrawModifications(Drawings, Progress);
                        return true;
                    }
                    catch (OperationCanceledException)
                    {

                    }
                    catch (Exception ex)
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            await this.MessageBoxError(ex.ToString(), "Drawing operation failed!");
                        });
                    }

                    return false;
                });

                IsGUIEnabled = true;

                if (!task.Result)
                {
                    Clipboard.RestoreSnapshot();
                    ShowLayer();
                    return;
                }

                Clipboard.Clip($"Draw {Drawings.Count} modifications");

                if (Settings.PixelEditor.PartialUpdateIslandsOnEditing)
                {
                    List<uint> whiteListLayers = new();
                    foreach (var item in Drawings)
                    {
                        /*if (item.OperationType != PixelOperation.PixelOperationType.Drawing &&
                            item.OperationType != PixelOperation.PixelOperationType.Text &&
                            item.OperationType != PixelOperation.PixelOperationType.Eraser &&
                            item.OperationType != PixelOperation.PixelOperationType.Supports) continue;*/
                        if (!whiteListLayers.Contains(item.LayerIndex))
                            whiteListLayers.Add(item.LayerIndex);

                        uint nextLayer = item.LayerIndex + 1;
                        if (nextLayer < SlicerFile.LayerCount &&
                            !whiteListLayers.Contains(nextLayer))
                        {
                            whiteListLayers.Add(nextLayer);
                        }
                    }

                    await UpdateIslandsOverhangs(whiteListLayers);
                }
            }

            Drawings.Clear();
            ShowLayer();

            if (exitEditor || (Settings.PixelEditor.CloseEditorOnApply && result == ButtonResult.Yes))
            {
                IsPixelEditorActive = false;
                if (!ReferenceEquals(LastSelectedTabItem, TabPixelEditor))
                {
                    SelectedTabItem = LastSelectedTabItem;
                }
            }

            CanSave = true;
        }
    }
}
