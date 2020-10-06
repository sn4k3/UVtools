/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DynamicData;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using MessageBox.Avalonia.Enums;
using UVtools.Core.Extensions;
using UVtools.Core.PixelEditor;
using UVtools.WPF.Extensions;

namespace UVtools.WPF
{
    public partial class MainWindow
    {
        public ObservableCollection<PixelOperation> Drawings { get; } = new ObservableCollection<PixelOperation>();
        public DataGrid DrawingsGrid;
        private int _selectedPixelOperationTabIndex;

        public PixelDrawing DrawingPixelDrawing { get; } = new PixelDrawing();
        public PixelText DrawingPixelText { get; } = new PixelText();
        public PixelEraser DrawingPixelEraser { get; } = new PixelEraser();
        public PixelSupport DrawingPixelSupport { get; } = new PixelSupport();
        public PixelDrainHole DrawingPixelDrainHole { get; } = new PixelDrainHole();

        public int SelectedPixelOperationTabIndex
        {
            get => _selectedPixelOperationTabIndex;
            set => RaiseAndSetIfChanged(ref _selectedPixelOperationTabIndex, value);
        }

        public void InitPixelEditor()
        {
            DrawingsGrid = this.FindControl<DataGrid>("DrawingsGrid");
            DrawingsGrid.KeyUp += DrawingsGridOnKeyUp;
            DrawingsGrid.SelectionChanged += (sender, args) =>
            {
                ShowLayer();
            };
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
            Drawings.RemoveMany(DrawingsGrid.SelectedItems.Cast<PixelOperation>());
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
                var removeItems = Drawings.Where(item =>
                {
                    Rectangle rect = new Rectangle(item.Location, item.Size);
                    rect.X -= item.Size.Width / 2;
                    rect.Y -= item.Size.Height / 2;
                    return rect.Contains(realLocation);
                });
                if (removeItems.Any())
                {
                    Drawings.RemoveMany(removeItems);
                    ShowLayer();
                }
                
                return;
            }

            WriteableBitmap bitmap = (WriteableBitmap)LayerImageBox.Image;
            //var context = CreateRenderTarget().CreateDrawingContext(bitmap);


            //Bitmap bmp = pbLayer.Image as Bitmap;
            if (SelectedPixelOperationTabIndex == (byte)PixelOperation.PixelOperationType.Drawing)
            {
                uint minLayer = Math.Max(0, _actualLayer - DrawingPixelDrawing.LayersBelow);
                uint maxLayer = Math.Min(SlicerFile.LayerCount - 1, _actualLayer + DrawingPixelDrawing.LayersAbove);
                for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
                {
                    var operationDrawing = new PixelDrawing(layerIndex, realLocation, DrawingPixelDrawing.LineType,
                        DrawingPixelDrawing.BrushShape, DrawingPixelDrawing.BrushSize, DrawingPixelDrawing.Thickness, isAdd);

                    //if (PixelHistory.Contains(operation)) continue;
                    Drawings.Add(operationDrawing);

                    if (layerIndex == _actualLayer)
                    {
                        var color = isAdd
                            ? Settings.PixelEditor.AddPixelColor
                            : Settings.PixelEditor.RemovePixelColor;

                        if (operationDrawing.BrushSize == 1)
                        {
                            unsafe
                            {
                                using (var bl = bitmap.Lock())
                                {
                                    var data = (uint*)bl.Address.ToPointer();
                                    data[bitmap.GetPixelPos(location)] =
                                        color.ToUint32();
                                }
                            }
                            
                            LayerImageBox.InvalidateArrange();
                               // LayerCache.ImageBgr.SetByte(operationDrawing.Location.X, operationDrawing.Location.Y,
                                 //   new[] { color.B, color.G, color.R });
                            continue;
                        }

                        switch (operationDrawing.BrushShape)
                        {
                            case PixelDrawing.BrushShapeType.Rectangle:
                                CvInvoke.Rectangle(LayerCache.ImageBgr, GetTransposedRectangle(operationDrawing.Rectangle),
                                    new MCvScalar(color.B, color.G, color.R), operationDrawing.Thickness,
                                    operationDrawing.LineType);
                                break;
                            case PixelDrawing.BrushShapeType.Circle:
                                CvInvoke.Circle(LayerCache.ImageBgr, location, operationDrawing.BrushSize / 2,
                                    new MCvScalar(color.B, color.G, color.R), operationDrawing.Thickness,
                                    operationDrawing.LineType);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        RefreshLayerImage();
                    }
                }
            }
            else if (SelectedPixelOperationTabIndex == (byte)PixelOperation.PixelOperationType.Text)
            {
                if (string.IsNullOrEmpty(DrawingPixelText.Text) || DrawingPixelText.FontScale < 0.2) return;

                uint minLayer = Math.Max(0, ActualLayer - DrawingPixelText.LayersBelow);
                uint maxLayer = Math.Min(SlicerFile.LayerCount - 1,
                    ActualLayer + DrawingPixelText.LayersAbove);
                for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
                {
                    var operationText = new PixelText(layerIndex, realLocation, DrawingPixelText.LineType,
                        DrawingPixelText.Font, DrawingPixelText.FontScale, DrawingPixelText.Thickness,
                        DrawingPixelText.Text, DrawingPixelText.Mirror, isAdd);

                    //if (PixelHistory.Contains(operation)) continue;
                    //PixelHistory.Add(operation);
                    Drawings.Add(operationText);

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
                uint minLayer = Math.Max(0, ActualLayer - DrawingPixelEraser.LayersBelow);
                uint maxLayer = Math.Min(SlicerFile.LayerCount - 1,
                    ActualLayer + DrawingPixelEraser.LayersAbove);
                for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
                {
                    var operationEraser = new PixelEraser(layerIndex, realLocation);

                    //if (PixelHistory.Contains(operation)) continue;
                    Drawings.Add(operationEraser);

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
                if (ActualLayer == 0) return;
                var operationSupport = new PixelSupport(ActualLayer, realLocation,
                    DrawingPixelSupport.TipDiameter, DrawingPixelSupport.PillarDiameter,
                    DrawingPixelSupport.BaseDiameter);

                //if (PixelHistory.Contains(operation)) return;
                Drawings.Add(operationSupport);

                CvInvoke.Circle(LayerCache.ImageBgr, location, operationSupport.TipDiameter / 2,
                    new MCvScalar(Settings.PixelEditor.SupportsColor.B, Settings.PixelEditor.SupportsColor.G, Settings.PixelEditor.SupportsColor.R), -1);
                RefreshLayerImage();
            }
            else if (SelectedPixelOperationTabIndex == (byte)PixelOperation.PixelOperationType.DrainHole)
            {
                if (ActualLayer == 0) return;
                var operationDrainHole = new PixelDrainHole(ActualLayer, realLocation, DrawingPixelDrainHole.Diameter);

                //if (PixelHistory.Contains(operation)) return;
                Drawings.Add(operationDrainHole);

                CvInvoke.Circle(LayerCache.ImageBgr, operationDrainHole.Location, operationDrainHole.Diameter / 2,
                    new MCvScalar(Settings.PixelEditor.DrainHoleColor.B, Settings.PixelEditor.DrainHoleColor.G, Settings.PixelEditor.DrainHoleColor.R), -1);
                RefreshLayerImage();
            }
            else
            {
                throw new NotImplementedException("Missing pixel operation");
            }
        }
    }
}
