/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using UVtools.Core.Dialogs;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.PixelEditor;
using UVtools.UI.Extensions;
using UVtools.UI.Structures;
using DrawingExtensions = UVtools.Core.Extensions.DrawingExtensions;

namespace UVtools.UI;

public partial class MainWindow
{
    public RangeObservableCollection<PixelOperation> Drawings { get; } = new ();
    private int _selectedPixelOperationTabIndex;

    public PixelDrawing DrawingPixelDrawing { get; } = new();
    public PixelText DrawingPixelText { get; } = new();
    public PixelEraser DrawingPixelEraser { get; } = new();
    public PixelSupport DrawingPixelSupport { get; } = new();
    public PixelDrainHole DrawingPixelDrainHole { get; } = new();

    public RangeObservableCollection<PixelOperation> DrawingPixelDrawingProfiles { get; } = new();
    public RangeObservableCollection<PixelOperation> DrawingPixelTextProfiles { get; } = new();
    public RangeObservableCollection<PixelOperation> DrawingPixelEraserProfiles { get; } = new();
    public RangeObservableCollection<PixelOperation> DrawingPixelSupportProfiles { get; } = new();
    public RangeObservableCollection<PixelOperation> DrawingPixelDrainHoleProfiles { get; } = new();

    public int SelectedPixelOperationTabIndex
    {
        get => _selectedPixelOperationTabIndex;
        set => RaiseAndSetIfChanged(ref _selectedPixelOperationTabIndex, value);
    }

    public void InitPixelEditor()
    {
        DrawingPixelDrawingProfiles.Add(DrawingPixelDrawing.Clone());
        DrawingPixelTextProfiles.Add(DrawingPixelText.Clone());
        DrawingPixelEraserProfiles.Add(DrawingPixelEraser.Clone());
        DrawingPixelSupportProfiles.Add(DrawingPixelSupport.Clone());
        DrawingPixelDrainHoleProfiles.Add(DrawingPixelDrainHole.Clone());

        DrawingPixelDrawingProfiles.AddRange(PixelEditorProfiles.GetProfiles<PixelDrawing>());
        DrawingPixelTextProfiles.AddRange(PixelEditorProfiles.GetProfiles<PixelText>());
        DrawingPixelEraserProfiles.AddRange(PixelEditorProfiles.GetProfiles<PixelEraser>());
        DrawingPixelSupportProfiles.AddRange(PixelEditorProfiles.GetProfiles<PixelSupport>());
        DrawingPixelDrainHoleProfiles.AddRange(PixelEditorProfiles.GetProfiles<PixelDrainHole>());
    }

    private void DrawingsGrid_OnLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        e.Row.Header = e.Row.GetIndex() + 1;
    }

    private void DrawingsGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
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

    private void DrawingsGrid_OnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        if (e.PointerPressedEventArgs.ClickCount == 2) return;
        if (DrawingsGrid.SelectedItem is not MainIssue) return;
        // Double clicking an issue will center and zoom into the 
        // selected issue. Left click on an issue will zoom to fit.

        var pointer = e.PointerPressedEventArgs.GetCurrentPoint(DrawingsGrid);

        if (pointer.Properties.IsRightButtonPressed)
        {
            ZoomToFit();
            return;
        }

    }

    private void DrawingsGrid_OnKeyUp(object? sender, KeyEventArgs e)
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
                "Clear pixel editor operations?") != MessageButtonResult.Yes) return;
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

        WriteableBitmap bitmap = LayerImageBox.ImageAsWriteableBitmap!;
        //var context = CreateRenderTarget().CreateDrawingContext(bitmap);

        
        //Bitmap bmp = pbLayer.Image as Bitmap;
        if (SelectedPixelOperationTabIndex == (byte)PixelOperation.PixelOperationType.Drawing)
        {
            var drawings = new List<PixelOperation>();
            uint minLayer = SlicerFile!.SanitizeLayerIndex((int)ActualLayer - (int)DrawingPixelDrawing.LayersBelow);
            uint maxLayer = SlicerFile.SanitizeLayerIndex(ActualLayer + DrawingPixelDrawing.LayersAbove);
            for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
            {
                var operationDrawing = new PixelDrawing(layerIndex, realLocation, DrawingPixelDrawing.LineType,
                    DrawingPixelDrawing.BrushShape, DrawingPixelDrawing.RotationAngle, DrawingPixelDrawing.BrushSize, DrawingPixelDrawing.Thickness, DrawingPixelDrawing.RemovePixelBrightness, DrawingPixelDrawing.PixelBrightness, isAdd);

                //if (PixelHistory.Contains(operation)) continue;
                //AddDrawing(operationDrawing);
                drawings.Add(operationDrawing);

                if (layerIndex == _actualLayer)
                {
                    var color = isAdd
                        ? Settings.PixelEditor.AddPixelColor
                        : Settings.PixelEditor.RemovePixelColor;

                    if (operationDrawing.BrushSize == 1)
                    {
                        LayerCache.Canvas?.DrawPoint(location.X, location.Y, new SKColor(color.ToUint32()));
                        LayerImageBox.InvalidateVisual();
                        continue;
                    }

                    int halfBrush = operationDrawing.BrushSize / 2;
                    double angle = operationDrawing.RotationAngle;
                    switch (operationDrawing.BrushShape)
                    {
                        case PixelDrawing.BrushShapeType.Line:
                            Point point1 = location with {X = location.X - halfBrush};
                            Point point2 = location with {X = location.X + halfBrush};

                            if (_showLayerImageRotated)
                            {
                                if (_showLayerImageRotateCcwDirection)
                                {
                                    angle += 90;
                                }
                                else
                                {
                                    angle -= 90;
                                }
                            }

                            point1 = point1.Rotate(angle, location);
                            point2 = point2.Rotate(angle, location);
                                

                            if (_showLayerImageFlipped)
                            {
                                if (_showLayerImageFlippedHorizontally)
                                {
                                    var newPoint1 = new Point(point2.X, point1.Y);
                                    var newPoint2 = new Point(point1.X, point2.Y);

                                    point1 = newPoint1;
                                    point2 = newPoint2;
                                }

                                if (_showLayerImageFlippedVertically)
                                {
                                    var newPoint1 = new Point(point1.X, point2.Y);
                                    var newPoint2 = new Point(point2.X, point1.Y);

                                    point1 = newPoint1;
                                    point2 = newPoint2;
                                }
                            }

                            /*if (_showLayerImageRotated)
                            {
                                if (!_showLayerImageFlipped || _showLayerImageFlippedHorizontally && _showLayerImageFlippedVertically)
                                {
                                    if (_showLayerImageRotateCcwDirection)
                                    {
                                        angle -= 90;
                                    }
                                    else
                                    {
                                        angle += 90;
                                    }
                                }
                                else
                                {
                                    if (_showLayerImageRotateCcwDirection)
                                    {
                                        angle += 90;
                                    }
                                    else
                                    {
                                        angle -= 90;
                                    }
                                }
                            }*/



                            LayerCache.Canvas?.DrawLine(point1.X, point1.Y, point2.X, point2.Y, new SKPaint
                            {
                                IsAntialias = operationDrawing.LineType == LineType.AntiAlias,
                                Color = new SKColor(color.ToUint32()),
                                IsStroke = operationDrawing.Thickness >= 0,
                                StrokeWidth = operationDrawing.Thickness,
                                StrokeCap = SKStrokeCap.Round
                            });
                            break;
                        /*case PixelDrawing.BrushShapeType.Square:
                            LayerCache.Canvas.DrawRect(location.X - halfBrush, location.Y - halfBrush, 
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
                        //break;
                        case PixelDrawing.BrushShapeType.Circle:
                            LayerCache.Canvas?.DrawCircle(location.X, location.Y, operationDrawing.BrushSize / 2f,
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
                            if (_showLayerImageRotated)
                            {
                                if (!_showLayerImageFlipped || _showLayerImageFlippedHorizontally && _showLayerImageFlippedVertically)
                                {
                                    if (_showLayerImageRotateCcwDirection)
                                    {
                                        angle -= 90;
                                    }
                                    else
                                    {
                                        angle += 90;
                                    }
                                }
                                else
                                {
                                    if (_showLayerImageRotateCcwDirection)
                                    {
                                        angle += 90;
                                    }
                                    else
                                    {
                                        angle -= 90;
                                    }
                                }
                            }


                            var vertices = DrawingExtensions.GetPolygonVertices((byte) operationDrawing.BrushShape,
                                operationDrawing.BrushSize / 2, location, angle, _showLayerImageFlipped && _showLayerImageFlippedHorizontally, _showLayerImageFlipped && _showLayerImageFlippedVertically);

                            //if(angle % 360 != 0) PointExtensions.Rotate(vertices, angle, location);

                            var path = new SKPath();
                            path.MoveTo(vertices[0].X, vertices[0].Y);
                            for (var i = 1; i < vertices.Length; i++)
                            {
                                path.LineTo(vertices[i].X, vertices[i].Y);
                            }
                            path.Close();

                            LayerCache.Canvas?.DrawPath(path, new SKPaint
                            {
                                IsAntialias = operationDrawing.LineType == LineType.AntiAlias,
                                Color = new SKColor(color.ToUint32()),
                                IsStroke = operationDrawing.Thickness >= 0,
                                StrokeWidth = operationDrawing.Thickness
                            });
                            /*LayerCache.Canvas.DrawPoints(SKPointMode.Polygon, points,
                                new SKPaint
                                {
                                    IsAntialias = operationDrawing.LineType == LineType.AntiAlias,
                                    Color = new SKColor(color.ToUint32()),
                                    IsStroke = operationDrawing.Thickness >= 0,
                                    StrokeWidth = operationDrawing.Thickness
                                });*/
                            break;
                    }
                    LayerImageBox.InvalidateVisual();
                    //RefreshLayerImage();
                }
            }

            AddDrawings(drawings);
            return;
        }
        else if (SelectedPixelOperationTabIndex == (byte)PixelOperation.PixelOperationType.Text)
        {
            if (string.IsNullOrEmpty(DrawingPixelText.Text) || DrawingPixelText.FontScale < 0.2) return;

            var drawings = new List<PixelOperation>();
            uint minLayer = SlicerFile!.SanitizeLayerIndex((int)ActualLayer - (int)DrawingPixelText.LayersBelow);
            uint maxLayer = SlicerFile!.SanitizeLayerIndex(ActualLayer + DrawingPixelText.LayersAbove);
            
            for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
            {
                var operationText = new PixelText(layerIndex, realLocation, DrawingPixelText.LineType,
                    DrawingPixelText.Font, DrawingPixelText.FontScale, DrawingPixelText.Thickness,
                    DrawingPixelText.Text, DrawingPixelText.Mirror, DrawingPixelText.LineAlignment, DrawingPixelText.Angle, DrawingPixelText.RemovePixelBrightness, DrawingPixelText.PixelBrightness, isAdd);

                //if (PixelHistory.Contains(operation)) continue;
                //PixelHistory.Add(operation);
                //AddDrawing(operationText);
                drawings.Add(operationText);

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

            AddDrawings(drawings);
            ShowLayer();
            return;
        }
        else if (SelectedPixelOperationTabIndex == (byte)PixelOperation.PixelOperationType.Eraser)
        {
            if (LayerCache.Image!.GetByte(realLocation) < 10) return;

            var drawings = new List<PixelOperation>();
            uint minLayer = SlicerFile!.SanitizeLayerIndex((int)ActualLayer - (int)DrawingPixelEraser.LayersBelow);
            uint maxLayer = SlicerFile!.SanitizeLayerIndex(ActualLayer + DrawingPixelEraser.LayersAbove);
            for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
            {
                var operationEraser = new PixelEraser(layerIndex, realLocation, DrawingPixelEraser.PixelBrightness);

                //if (PixelHistory.Contains(operation)) continue;
                //AddDrawing(operationEraser);
                drawings.Add(operationEraser);

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

            AddDrawings(drawings);
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
            return;
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
            return;
        }
        else
        {
            throw new NotImplementedException("Missing pixel operation");
        }
    }

    public void AddDrawing(PixelOperation operation)
    {
        Drawings.Insert(0, operation);
    }

    public void AddDrawings(IEnumerable<PixelOperation> operations)
    {
        Drawings.InsertRange(0, operations);
    }

    public void DrawModificationsCommand()
    {
        DrawModifications(false);
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

        MessageButtonResult result;

        if (exitEditor)
        {
            result = await this.MessageBoxQuestion(
                "There are edit operations that have not been applied.  " +
                "Would you like to apply all operations before closing the editor?",
                "Closing image editor?", MessageButtons.YesNoCancel);
        }
        else
        {

            result = await this.MessageBoxQuestion(
                "Are you sure you want to apply all operations?",
                "Apply image editor changes?");

            // For the "apply" case, We aren't exiting the editor, so map "No" to "Cancel" here
            // in order to prevent pixel history from being cleared.
            result = result == MessageButtonResult.No ? MessageButtonResult.Cancel : MessageButtonResult.Yes;
        }

        if (result == MessageButtonResult.Cancel)
        {
            IsPixelEditorActive = true;
            return;
        }

        if (result == MessageButtonResult.Yes)
        {
            IsGUIEnabled = false;
            ShowProgressWindow("Drawing pixels");
            ClipboardManager.Snapshot();

            var task = await Task.Run(() =>
            {
                try
                {
                    SlicerFile!.DrawModifications(Drawings, Progress);
                    return Task.FromResult(true);
                }
                catch (OperationCanceledException)
                {

                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await this.MessageBoxError(ex.ToString(), "Drawing operation failed!");
                    });
                }

                return Task.FromResult(false);
            }, Progress.Token);

            IsGUIEnabled = true;

            if (!task)
            {
                ClipboardManager.RestoreSnapshot();
                ShowLayer();
                return;
            }

            ClipboardManager.Clip($"Draw {Drawings.Count} modifications");
            PopulateSuggestions();

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
                    if (nextLayer < SlicerFile!.LayerCount &&
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

        if (exitEditor || (Settings.PixelEditor.CloseEditorOnApply && result == MessageButtonResult.Yes))
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