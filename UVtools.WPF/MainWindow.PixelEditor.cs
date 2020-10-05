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
using System.Drawing.Drawing2D;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using DynamicData;
using Emgu.CV.CvEnum;
using MessageBox.Avalonia.Enums;
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

        public void OnClickDrawingRemove()
        {
            if (DrawingsGrid.SelectedItems.Count == 0) return;
            Drawings.RemoveMany(DrawingsGrid.SelectedItems.Cast<PixelOperation>());
        }

        public async void OnClickDrawingClear()
        {
            if(await this.MessageBoxQuestion($"Are you sure you want to clear {Drawings.Count} operations?",
                "Clear pixel editor operations?") != ButtonResult.Yes) return;
            Drawings.Clear();
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

            PixelOperation operation = null;
            //Bitmap bmp = pbLayer.Image as Bitmap;

            if (SelectedPixelOperationTabIndex == (byte)PixelOperation.PixelOperationType.Drawing)
            {
                uint minLayer = Math.Max(0, _actualLayer - DrawingPixelDrawing.LayersBelow);
                uint maxLayer = Math.Min(SlicerFile.LayerCount - 1,
                    _actualLayer + DrawingPixelDrawing.LayersAbove);
                for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
                {
                    operation = new PixelDrawing(layerIndex, realLocation, DrawingPixelDrawing.LineType,
                        DrawingPixelDrawing.BrushShape, DrawingPixelDrawing.BrushSize, DrawingPixelDrawing.Thickness, isAdd);

                    //if (PixelHistory.Contains(operation)) continue;
                    Drawings.Add(operation);

                    if (layerIndex == ActualLayer)
                    {
                        /*using (var gfx = Graphics.FromImage(bmp))
                        {
                            int shiftPos = brushSize / 2;
                            gfx.SmoothingMode = SmoothingMode.HighSpeed;

                            var color = isAdd
                                ? (flvPixelHistory.SelectedObjects.Contains(operation)
                                    ? Settings.Default.PixelEditorAddPixelHLColor
                                    : Settings.Default.PixelEditorAddPixelColor)
                                : (flvPixelHistory.SelectedObjects.Contains(operation)
                                    ? Settings.Default.PixelEditorRemovePixelHLColor
                                    : Settings.Default.PixelEditorRemovePixelColor);
                            if (lineType == LineType.AntiAlias && brushSize > 1)
                            {
                                gfx.SmoothingMode = SmoothingMode.AntiAlias;
                            }

                            switch (shapeType)
                            {
                                case PixelDrawing.BrushShapeType.Rectangle:
                                    if (thickness > 0)
                                        gfx.DrawRectangle(new Pen(color, thickness), Math.Max(0, location.X - shiftPos),
                                            Math.Max(0, location.Y - shiftPos),
                                            (int)nmPixelEditorDrawingBrushSize.Value,
                                            (int)nmPixelEditorDrawingBrushSize.Value);
                                    else
                                        gfx.FillRectangle(new SolidBrush(color), Math.Max(0, location.X - shiftPos),
                                            Math.Max(0, location.Y - shiftPos),
                                            (int)nmPixelEditorDrawingBrushSize.Value,
                                            (int)nmPixelEditorDrawingBrushSize.Value);
                                    break;
                                case PixelDrawing.BrushShapeType.Circle:
                                    if (thickness > 0)
                                        gfx.DrawEllipse(new Pen(color, thickness), Math.Max(0, location.X - shiftPos),
                                            Math.Max(0, location.Y - shiftPos),
                                            (int)nmPixelEditorDrawingBrushSize.Value,
                                            (int)nmPixelEditorDrawingBrushSize.Value);
                                    else
                                        gfx.FillEllipse(new SolidBrush(color), Math.Max(0, location.X - shiftPos),
                                            Math.Max(0, location.Y - shiftPos),
                                            (int)nmPixelEditorDrawingBrushSize.Value,
                                            (int)nmPixelEditorDrawingBrushSize.Value);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }*/
                    }
                }
            }
            /*else if (tabControlPixelEditor.SelectedIndex == (byte)PixelOperation.PixelOperationType.Text)
            {
                if (string.IsNullOrEmpty(tbPixelEditorTextText.Text) || nmPixelEditorTextFontScale.Value < 0.2m) return;

                LineType lineType = (LineType)cbPixelEditorTextLineType.SelectedItem;
                FontFace fontFace = (FontFace)cbPixelEditorTextFontFace.SelectedItem;

                uint minLayer = (uint)Math.Max(0, ActualLayer - nmPixelEditorTextLayersBelow.Value);
                uint maxLayer = (uint)Math.Min(SlicerFile.LayerCount - 1,
                    ActualLayer + nmPixelEditorTextLayersAbove.Value);
                for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
                {
                    operation = new PixelText(layerIndex, realLocation, lineType,
                        fontFace, (double)nmPixelEditorTextFontScale.Value, (ushort)nmPixelEditorTextThickness.Value,
                        tbPixelEditorTextText.Text, cbPixelEditorTextMirror.Checked, isAdd);

                    if (PixelHistory.Contains(operation)) continue;
                    PixelHistory.Add(operation);
                }

                ShowLayer();
                return;
            }
            else if (tabControlPixelEditor.SelectedIndex == (byte)PixelOperation.PixelOperationType.Eraser)
            {
                if (ActualLayerImage.GetByte(realLocation) < 10) return;
                uint minLayer = (uint)Math.Max(0, ActualLayer - nmPixelEditorEraserLayersBelow.Value);
                uint maxLayer = (uint)Math.Min(SlicerFile.LayerCount - 1,
                    ActualLayer + nmPixelEditorEraserLayersAbove.Value);
                for (uint layerIndex = minLayer; layerIndex <= maxLayer; layerIndex++)
                {
                    operation = new PixelEraser(layerIndex, realLocation);

                    if (PixelHistory.Contains(operation)) continue;
                    PixelHistory.Add(operation);
                }

                ShowLayer();
                return;
            }
            else if (tabControlPixelEditor.SelectedIndex == (byte)PixelOperation.PixelOperationType.Supports)
            {
                if (ActualLayer == 0) return;
                operation = new PixelSupport(ActualLayer, realLocation,
                    (byte)nmPixelEditorSupportsTipDiameter.Value, (byte)nmPixelEditorSupportsPillarDiameter.Value,
                    (byte)nmPixelEditorSupportsBaseDiameter.Value);

                if (PixelHistory.Contains(operation)) return;
                PixelHistory.Add(operation);

                SolidBrush brush = new SolidBrush(flvPixelHistory.SelectedObjects.Contains(operation)
                    ? Settings.Default.PixelEditorSupportHLColor
                    : Settings.Default.PixelEditorSupportColor);
                using (var gfx = Graphics.FromImage(bmp))
                {
                    int shiftPos = (int)nmPixelEditorSupportsTipDiameter.Value / 2;
                    gfx.SmoothingMode = SmoothingMode.HighSpeed;
                    gfx.FillEllipse(brush, Math.Max(0, location.X - shiftPos), Math.Max(0, location.Y - shiftPos),
                        (int)nmPixelEditorSupportsTipDiameter.Value, (int)nmPixelEditorSupportsTipDiameter.Value);
                }
            }
            else if (tabControlPixelEditor.SelectedIndex == (byte)PixelOperation.PixelOperationType.DrainHole)
            {
                if (ActualLayer == 0) return;
                operation = new PixelDrainHole(ActualLayer, realLocation, (byte)nmPixelEditorDrainHoleDiameter.Value);

                if (PixelHistory.Contains(operation)) return;
                PixelHistory.Add(operation);

                SolidBrush brush = new SolidBrush(flvPixelHistory.SelectedObjects.Contains(operation)
                    ? Settings.Default.PixelEditorDrainHoleHLColor
                    : Settings.Default.PixelEditorDrainHoleColor);
                using (var gfx = Graphics.FromImage(bmp))
                {
                    int shiftPos = (int)nmPixelEditorDrainHoleDiameter.Value / 2;
                    gfx.SmoothingMode = SmoothingMode.HighSpeed;
                    gfx.FillEllipse(brush, Math.Max(0, location.X - shiftPos), Math.Max(0, location.Y - shiftPos),
                        (int)nmPixelEditorDrainHoleDiameter.Value, (int)nmPixelEditorDrainHoleDiameter.Value);
                }
            }
            else
            {
                throw new NotImplementedException("Missing pixel operation");
            }

            */
        }
    }
}
