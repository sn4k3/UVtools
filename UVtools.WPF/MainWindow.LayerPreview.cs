/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.PixelEditor;
using UVtools.WPF.Controls;
using UVtools.WPF.Extensions;
using UVtools.WPF.Structures;
using Color = UVtools.WPF.Structures.Color;
using Helpers = UVtools.WPF.Controls.Helpers;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace UVtools.WPF
{
    public partial class MainWindow
    {
        #region Enum

        public enum ZoomToFitType : byte
        {
            Auto,
            Image,
            Volume,
            Selection
        };
        #endregion

        public AdvancedImageBox LayerImageBox { get; private set; }
        public SliderEx LayerSlider;
        public Panel LayerNavigationTooltipPanel;
        public Border LayerNavigationTooltipBorder;
        private Canvas _issuesSliderCanvas;


        private Timer _layerNavigationTooltipTimer = new Timer(0.1) { AutoReset = false };
        private uint _actualLayer;

        private bool _showLayerImageRotated;
        private bool _showLayerImageDifference;
        private bool _showLayerImageIssues = true;
        private bool _showLayerImageCrosshairs = true;
        private bool _isPixelEditorActive;
        private bool _showLayerOutlinePrintVolumeBoundary;
        private bool _showLayerOutlineLayerBoundary;
        private bool _showLayerOutlineHollowAreas;
        private bool _showLayerOutlineEdgeDetection;

        private bool _isTooltipOverlayVisible;
        private string _tooltipOverlayText;

        private long _showLayerRenderMs;

        public LayerCache LayerCache = new LayerCache();
        private Point _lastPixelMouseLocation = Point.Empty;


        public void InitLayerPreview()
        {
            LayerImageBox = this.FindControl<AdvancedImageBox>("LayerImage");
            LayerSlider = this.FindControl<SliderEx>("Layer.Navigation.Slider");
            LayerNavigationTooltipPanel = this.FindControl<Panel>("Layer.Navigation.Tooltip.Panel");
            LayerNavigationTooltipBorder = this.FindControl<Border>("Layer.Navigation.Tooltip.Border");
            _issuesSliderCanvas = this.Find<Canvas>("Layer.Navigation.IssuesCanvas");



            _showLayerImageDifference = Settings.LayerPreview.ShowLayerDifference;
            _showLayerOutlinePrintVolumeBoundary = Settings.LayerPreview.VolumeBoundsOutline;
            _showLayerOutlineLayerBoundary = Settings.LayerPreview.LayerBoundsOutline;
            _showLayerOutlineHollowAreas = Settings.LayerPreview.HollowOutline;

            LayerImageBox.ZoomLevels = new AdvancedImageBox.ZoomLevelCollection(AppSettings.ZoomLevels);
            LayerImageBox.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(LayerImageBox.Zoom))
                {
                    RaisePropertyChanged(nameof(LayerZoomStr));
                    AddLogVerbose($"Zoomed from {LayerImageBox.OldZoom} to {LayerImageBox.Zoom}");

                    if (ShowLayerImageCrosshairs &&
                        Issues.Count > 0 &&
                        (LayerImageBox.OldZoom < 50 &&
                         LayerImageBox.Zoom >= 50 // Trigger refresh as crosshair thickness increases at lower zoom levels
                         || LayerImageBox.OldZoom > 100 && LayerImageBox.Zoom <= 100
                         || LayerImageBox.OldZoom >= 50 && LayerImageBox.OldZoom <= 100 && (LayerImageBox.Zoom < 50 || LayerImageBox.Zoom > 100)
                         || LayerImageBox.OldZoom <= AppSettings.CrosshairFadeLevel &&
                         LayerImageBox.Zoom > AppSettings.CrosshairFadeLevel // Trigger refresh as zoom level manually crosses fade threshold
                         || LayerImageBox.OldZoom > AppSettings.CrosshairFadeLevel && LayerImageBox.Zoom <= AppSettings.CrosshairFadeLevel)

                    )
                    {
                        if (Settings.LayerPreview.CrosshairShowOnlyOnSelectedIssues)
                        {
                            if (IssuesGrid.SelectedItems.Count == 0 || !IssuesGrid.SelectedItems.Cast<LayerIssue>().Any(
                                issue => // Find a valid candidate to update layer preview, otherwise quit
                                    issue.LayerIndex == _actualLayer && issue.Type != LayerIssue.IssueType.EmptyLayer &&
                                    issue.Type != LayerIssue.IssueType.TouchingBound)) return;
                        }
                        else
                        {
                            if (!Issues.Any(
                                issue => // Find a valid candidate to update layer preview, otherwise quit
                                    issue.LayerIndex == _actualLayer && issue.Type != LayerIssue.IssueType.EmptyLayer &&
                                    issue.Type != LayerIssue.IssueType.TouchingBound)) return;
                        }

                        // A timer is used here rather than invoking ShowLayer directly to eliminate sublte visual flashing
                        // that will occur on the transition when the crosshair fades or unfades if ShowLayer is called directly.
                        ShowLayer();
                    }

                    return;
                }

                if (e.PropertyName == nameof(LayerImageBox.SelectionRegion))
                {
                    RaisePropertyChanged(nameof(LayerROIStr));
                }

            };

            LayerImageBox.PointerMoved += LayerImageBoxOnPointerMoved;
            LayerImageBox.KeyUp += LayerImageBox_KeyUp;
            LayerImageBox.PointerReleased += LayerImageBox_PointerReleased;
            LayerImageBox.PointerPressed += LayerImageBoxOnPointerPressed;
            LayerImageBox.DoubleTapped += LayerImageBoxOnDoubleTapped;

            _issuesSliderCanvas.PointerWheelChanged += LayerSliderOnPointerWheelChanged;
            LayerSlider.PointerWheelChanged += LayerSliderOnPointerWheelChanged;
            //this.FindControl<Grid>("LayerNavigationSliderGrid").PointerWheelChanged += LayerSliderOnPointerWheelChanged;

            _layerNavigationTooltipTimer.Elapsed += (sender, args) =>
            {
                Dispatcher.UIThread.InvokeAsync(() => RaisePropertyChanged(nameof(LayerNavigationTooltipMargin)));
            };
        }

        private void LayerSliderOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (e.Delta.Y > 0)
                ActualLayer++;
            else if (e.Delta.Y < 0 && _actualLayer > 0)
                ActualLayer--;
        }


        public bool ShowLayerImageRotated
        {
            get => _showLayerImageRotated;
            set
            {
                if (!RaiseAndSetIfChanged(ref _showLayerImageRotated, value)) return;
                var rect = LayerImageBox.SelectionRegion;
                if (!rect.IsEmpty)
                {
                    LayerImageBox.SelectionRegion = GetTransposedRectangle(rect.ToDotNet(), _showLayerImageRotated, true).ToAvalonia();
                }

                ZoomToFit();
                ShowLayer();
            }
        }

        public bool ShowLayerImageDifference
        {
            get => _showLayerImageDifference;
            set
            {
                if (!RaiseAndSetIfChanged(ref _showLayerImageDifference, value)) return;
                ShowLayer();
            }
        }

        public bool ShowLayerImageIssues
        {
            get => _showLayerImageIssues;
            set
            {
                if (RaiseAndSetIfChanged(ref _showLayerImageIssues, value))
                {
                    ShowLayer();
                }
            }
        }

        public bool ShowLayerImageCrosshairs
        {
            get => _showLayerImageCrosshairs;
            set
            {
                if (!RaiseAndSetIfChanged(ref _showLayerImageCrosshairs, value)) return;
                ShowLayer();
            }
        }

        public bool ShowLayerOutlinePrintVolumeBoundary
        {
            get => _showLayerOutlinePrintVolumeBoundary;
            set
            {
                if (!RaiseAndSetIfChanged(ref _showLayerOutlinePrintVolumeBoundary, value)) return;
                ShowLayer();
            }
        }

        public bool ShowLayerOutlineLayerBoundary
        {
            get => _showLayerOutlineLayerBoundary;
            set
            {
                if (!RaiseAndSetIfChanged(ref _showLayerOutlineLayerBoundary, value)) return;
                ShowLayer();
            }
        }

        public bool ShowLayerOutlineHollowAreas
        {
            get => _showLayerOutlineHollowAreas;
            set
            {
                if (!RaiseAndSetIfChanged(ref _showLayerOutlineHollowAreas, value)) return;
                ShowLayer();
            }
        }

        public bool ShowLayerOutlineEdgeDetection
        {
            get => _showLayerOutlineEdgeDetection;
            set
            {
                if (!RaiseAndSetIfChanged(ref _showLayerOutlineEdgeDetection, value)) return;
                ShowLayer();
            }
        }

        public bool IsPixelEditorActive
        {
            get => _isPixelEditorActive;
            set
            {
                if (!RaiseAndSetIfChanged(ref _isPixelEditorActive, value)) return;
                if (_isPixelEditorActive)
                {
                    SelectedTabItem = TabPixelEditor;
                }
                else
                {
                    DrawModifications(true);
                }
            }
        }

        public string MinimumLayerString => SlicerFile is null ? "???" : $"{SlicerFile.LayerHeight}mm\n0";
        public string MaximumLayerString => SlicerFile is null ? "???" : $"{SlicerFile.PrintHeight}mm\n{SlicerFile.LayerCount - 1}";
        public string ActualLayerTooltip => SlicerFile is null ? "???" : $"{LayerCache.Layer?.PositionZ:0.00}mm\n{ActualLayer}\n{(ActualLayer + 1) * 100 / (SlicerFile.LayerCount)}%";

        public uint SliderMaximumValue => SlicerFile?.LastLayerIndex ?? 0;

        public bool CanGoUp => _actualLayer < SliderMaximumValue;
        public bool CanGoDown => _actualLayer > 0;

        public bool IsTooltipOverlayVisible
        {
            get => _isTooltipOverlayVisible;
            set => RaiseAndSetIfChanged(ref _isTooltipOverlayVisible, value);
        }

        public string TooltipOverlayText
        {
            get => _tooltipOverlayText;
            set => RaiseAndSetIfChanged(ref _tooltipOverlayText, value);
        }

        public string LayerPixelCountStr
        {
            get
            {
                if (!LayerCache.IsCached) return "0";
                var pixelPercent =
                    Math.Round(
                        LayerCache.Layer.NonZeroPixelCount * 100.0 / (SlicerFile.ResolutionX * SlicerFile.ResolutionY), 2);
                return $"{LayerCache.Layer.NonZeroPixelCount} ({pixelPercent}%)";
            }
        }

        public string LayerBoundsStr => LayerCache.Layer is null ? "NS" : $"{LayerCache.Layer.BoundingRectangle} ({LayerCache.Layer.BoundingRectangle.GetArea()}px²)";
        public string LayerROIStr
        {
            get
            {
                var roi = ROI;
                return roi.IsEmpty ? "NS" : $"{roi} ({roi.GetArea()}px²)";
            }
        }

        public long ShowLayerRenderMs
        {
            get => _showLayerRenderMs;
            set => RaiseAndSetIfChanged(ref _showLayerRenderMs, value);
        }

        public PixelPicker LayerPixelPicker { get; } = new PixelPicker();

        public string LayerZoomStr => $"{LayerImageBox.Zoom / 100m}x" +
                                      (AppSettings.LockedZoomLevel == LayerImageBox.Zoom ? " 🔒" : string.Empty);
        public string LayerResolutionStr => SlicerFile?.Resolution.ToString() ?? "Unloaded";

        public uint ActualLayer
        {
            get => _actualLayer;
            set
            {
                if (DataContext is null) return;
                if (!RaiseAndSetIfChanged(ref _actualLayer, value)) return;
                ShowLayer();
                InvalidateLayerNavigation();
            }
        }

        public void ForceUpdateActualLayer(uint layerIndex = 0)
        {
            //_actualLayer = layerIndex;
            /*ShowLayer();
            InvalidateLayerNavigation();
            RaisePropertyChanged(nameof(ActualLayer));*/
            _actualLayer = uint.MaxValue;
            ActualLayer = layerIndex;
        }

        public void InvalidateLayerNavigation()
        {
            RaisePropertyChanged(nameof(CanGoDown));
            RaisePropertyChanged(nameof(CanGoUp));
            RaisePropertyChanged(nameof(ActualLayerTooltip));
            //RaisePropertyChanged(nameof(LayerNavigationTooltipMargin));
            RaisePropertyChanged(nameof(LayerPixelCountStr));
            RaisePropertyChanged(nameof(LayerBoundsStr));
            _layerNavigationTooltipTimer.Start();
        }

        public Thickness LayerNavigationTooltipMargin
        {
            get
            {
                double top = 0;
                if (LayerSlider?.Track != null)
                {
                    double trackerPos = LayerSlider.Track.Thumb.Bounds.Height / 2 + LayerSlider.Track.Thumb.Bounds.Top;
                    double halfTooltipHeight = LayerNavigationTooltipBorder.Bounds.Height / 2;
                    top = (trackerPos - halfTooltipHeight).Clamp(0,
                        LayerSlider.Bounds.Height - LayerNavigationTooltipBorder.Bounds.Height);

                }
                return new Thickness(
                    0,
                    top,
                    5,
                    0);
            }
        }


        #region ROI
        public Rectangle ROI
        {
            get
            {
                var rect = LayerImageBox.SelectionRegion;
                return rect.IsEmpty ? Rectangle.Empty : GetTransposedRectangle(rect.ToDotNet(), false);
            }
            set => LayerImageBox.SelectionRegion = value.ToAvalonia();
        }

        public void OnROIClick()
        {
            ZoomToFit(ZoomToFitType.Selection);
        }
        #endregion

        public void GoFirstLayer()
        {
            if (SlicerFile is null) return;
            if (!CanGoDown) return;
            ActualLayer = 0;
        }

        public void GoPreviousLayer()
        {
            if (SlicerFile is null) return;
            if (!CanGoDown) return;
            ActualLayer--;
        }

        public void GoNextLayer()
        {
            if (SlicerFile is null) return;
            if (!CanGoUp) return;
            ActualLayer++;
        }

        public void GoLastLayer()
        {
            if (SlicerFile is null) return;
            if (!CanGoUp) return;
            ActualLayer = SliderMaximumValue;
        }

        public void RefreshLayerImage()
        {
            LayerImageBox.Image = LayerCache.ImageBgr.ToBitmap();
        }

        /// <summary>
        /// Shows a layer number
        /// </summary>
        unsafe void ShowLayer()
        {
            if (!IsFileLoaded) return;

            var sanitizedLayerIndex = Math.Min(_actualLayer, SlicerFile.LastLayerIndex);
            if (sanitizedLayerIndex != _actualLayer)
            {
                _actualLayer = sanitizedLayerIndex;
                InvalidateLayerNavigation();
            }

            Stopwatch watch = Stopwatch.StartNew();
            LayerCache.Layer = SlicerFile[_actualLayer];

            try
            {
                //var imageSpan = LayerCache.Image.GetPixelSpan<byte>();
                //var imageBgrSpan = LayerCache.ImageBgr.GetPixelSpan<byte>();

                var imageSpan = LayerCache.ImageSpan;
                var imageBgrSpan = LayerCache.ImageBgrSpan;

                if (_showLayerOutlineEdgeDetection)
                {
                    using var canny = new Mat();
                    CvInvoke.Canny(LayerCache.Image, canny, 80, 40, 3, true);
                    CvInvoke.CvtColor(canny, LayerCache.ImageBgr, ColorConversion.Gray2Bgra);
                }
                else if (_showLayerImageDifference)
                {
                    if (_actualLayer > 0 && _actualLayer < SlicerFile.LayerCount - 1)
                    {
                        Mat previousImage = null;
                        Mat nextImage = null;

                        // Can improve performance on >4K images?
                        Parallel.Invoke(
                            () => { previousImage = SlicerFile[_actualLayer - 1].LayerMat; },
                            () => { nextImage = SlicerFile[_actualLayer + 1].LayerMat; });

                        /*using (var previousImage = SlicerFile[_actualLayer - 1].LayerMat)
                            using (var nextImage = SlicerFile[_actualLayer + 1].LayerMat)
                            {*/
                        //var previousSpan = previousImage.GetPixelSpan<byte>();
                        //var nextSpan = nextImage.GetPixelSpan<byte>();

                        var previousSpan = previousImage.GetBytePointer();
                        var nextSpan = nextImage.GetBytePointer();

                        int width = LayerCache.Image.Width;
                        int channels = LayerCache.ImageBgr.NumberOfChannels;
                        Parallel.For(0, LayerCache.Image.Height, y =>
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int pixel = y * width + x;
                                if (imageSpan[pixel] != 0) continue;
                                Color color = Color.Empty;
                                if (previousSpan[pixel] > 0 && nextSpan[pixel] > 0)
                                {
                                    color = Settings.LayerPreview.NextLayerDifferenceColor;
                                }
                                else if (previousSpan[pixel] > 0)
                                {
                                    color = Settings.LayerPreview.PreviousLayerDifferenceColor;
                                }
                                else if (nextSpan[pixel] > 0)
                                {
                                    color = Settings.LayerPreview.NextLayerDifferenceColor;
                                }

                                if (color.IsEmpty) continue;
                                var bgrPixel = pixel * channels;
                                imageBgrSpan[bgrPixel] = color.B; // B
                                imageBgrSpan[++bgrPixel] = color.G; // G
                                imageBgrSpan[++bgrPixel] = color.R; // R
                                //imageBgrSpan[++bgrPixel] = color.A; // A
                            }
                        });

                        previousImage.Dispose();
                        nextImage.Dispose();
                    }
                }


                var selectedIssues = IssuesGrid.SelectedItems;

                if (_showLayerImageIssues && Issues.Count > 0)
                {
                    foreach (var issue in Issues)
                    {
                        if (issue.LayerIndex != ActualLayer) continue;
                        if (!issue.HaveValidPoint) continue;

                        Color color = Color.Empty;

                        if (issue.Type == LayerIssue.IssueType.ResinTrap)
                        {
                            color = selectedIssues.Count > 0 && selectedIssues.Contains(issue)
                                ? Settings.LayerPreview.ResinTrapHighlightColor
                                : Settings.LayerPreview.ResinTrapColor;


                            using (var vec = new VectorOfVectorOfPoint(new VectorOfPoint(issue.Pixels)))
                            {
                                CvInvoke.DrawContours(LayerCache.ImageBgr, vec, -1,
                                    new MCvScalar(color.B, color.G, color.R), -1);
                            }

                            if (_showLayerImageCrosshairs &&
                                !Settings.LayerPreview.CrosshairShowOnlyOnSelectedIssues &&
                                LayerImageBox.Zoom <= AppSettings.CrosshairFadeLevel)
                            {
                                DrawCrosshair(issue.BoundingRectangle);
                            }

                            continue;
                        }

                        switch (issue.Type)
                        {
                            case LayerIssue.IssueType.Island:
                                color = selectedIssues.Count > 0 && selectedIssues.Contains(issue)
                                    ? Settings.LayerPreview.IslandHighlightColor
                                    : Settings.LayerPreview.IslandColor;
                                if (_showLayerImageCrosshairs &&
                                    !Settings.LayerPreview.CrosshairShowOnlyOnSelectedIssues &&
                                    LayerImageBox.Zoom <= AppSettings.CrosshairFadeLevel)
                                {
                                    DrawCrosshair(issue.BoundingRectangle);
                                }

                                break;
                            case LayerIssue.IssueType.Overhang:
                                color = selectedIssues.Count > 0 && selectedIssues.Contains(issue)
                                    ? Settings.LayerPreview.OverhangHighlightColor
                                    : Settings.LayerPreview.OverhangColor;
                                if (_showLayerImageCrosshairs &&
                                    !Settings.LayerPreview.CrosshairShowOnlyOnSelectedIssues &&
                                    LayerImageBox.Zoom <= AppSettings.CrosshairFadeLevel)
                                {
                                    DrawCrosshair(issue.BoundingRectangle);
                                }

                                break;
                            case LayerIssue.IssueType.TouchingBound:
                                color = Settings.LayerPreview.TouchingBoundsColor;
                                break;
                        }

                        if (color.IsEmpty) continue;

                        foreach (var pixel in issue)
                        {
                            int pixelPos = LayerCache.Image.GetPixelPos(pixel);
                            byte brightness = imageSpan[pixelPos];
                            if (brightness == 0) continue;

                            int pixelBgrPos = pixelPos * LayerCache.ImageBgr.NumberOfChannels;

                            var newColor = color.FactorColor(brightness, 80);

                            imageBgrSpan[pixelBgrPos] = newColor.B; // B
                            imageBgrSpan[pixelBgrPos + 1] = newColor.G; // G
                            imageBgrSpan[pixelBgrPos + 2] = newColor.R; // R
                        }
                    }
                }

                if (_showLayerOutlinePrintVolumeBoundary)
                {
                    CvInvoke.Rectangle(LayerCache.ImageBgr, SlicerFile.LayerManager.BoundingRectangle,
                        new MCvScalar(Settings.LayerPreview.VolumeBoundsOutlineColor.B,
                            Settings.LayerPreview.VolumeBoundsOutlineColor.G,
                            Settings.LayerPreview.VolumeBoundsOutlineColor.R),
                        Settings.LayerPreview.VolumeBoundsOutlineThickness);
                }

                if (_showLayerOutlineLayerBoundary && !SlicerFile[_actualLayer].BoundingRectangle.IsEmpty)
                {
                    CvInvoke.Rectangle(LayerCache.ImageBgr, SlicerFile[_actualLayer].BoundingRectangle,
                        new MCvScalar(Settings.LayerPreview.LayerBoundsOutlineColor.B,
                            Settings.LayerPreview.LayerBoundsOutlineColor.G, Settings.LayerPreview.LayerBoundsOutlineColor.R),
                        Settings.LayerPreview.LayerBoundsOutlineThickness);
                }

                if (_showLayerOutlineHollowAreas)
                {
                    //CvInvoke.Threshold(ActualLayerImage, grayscale, 1, 255, ThresholdType.Binary);

                    /*
                         * hierarchy[i][0]: the index of the next contour of the same level
                         * hierarchy[i][1]: the index of the previous contour of the same level
                         * hierarchy[i][2]: the index of the first child
                         * hierarchy[i][3]: the index of the parent
                         */
                    for (int i = 0; i < LayerCache.LayerContours.Size; i++)
                    {
                        if ((int)LayerCache.LayerHierarchyJagged.GetValue(0, i, 2) == -1 &&
                            (int)LayerCache.LayerHierarchyJagged.GetValue(0, i, 3) != -1)
                        {
                            //var r = CvInvoke.BoundingRectangle(contours[i]);
                            //CvInvoke.Rectangle(ActualLayerImageBgr, r, new MCvScalar(0, 0, 255), 2);
                            CvInvoke.DrawContours(LayerCache.ImageBgr, LayerCache.LayerContours, i,
                                new MCvScalar(Settings.LayerPreview.HollowOutlineColor.B,
                                    Settings.LayerPreview.HollowOutlineColor.G,
                                    Settings.LayerPreview.HollowOutlineColor.R),
                                Settings.LayerPreview.HollowOutlineLineThickness);
                        }
                    }
                }

                for (var index = 0; index < Drawings.Count; index++)
                {
                    if (Drawings[index].LayerIndex != ActualLayer) continue;
                    var operation = Drawings[index];
                    if (operation.OperationType == PixelOperation.PixelOperationType.Drawing)
                    {
                        var operationDrawing = (PixelDrawing) operation;
                        var color = operationDrawing.IsAdd
                            ? (DrawingsGrid.SelectedItems.Contains(operation)
                                ? Settings.PixelEditor.AddPixelHighlightColor
                                : Settings.PixelEditor.AddPixelColor)
                            : (DrawingsGrid.SelectedItems.Contains(operation)
                                ? Settings.PixelEditor.RemovePixelHighlightColor
                                : Settings.PixelEditor.RemovePixelColor);
                        if (operationDrawing.BrushSize == 1)
                        {
                            LayerCache.ImageBgr.SetByte(operation.Location.X, operation.Location.Y,
                                new[] {color.B, color.G, color.R});
                            continue;
                        }

                        switch (operationDrawing.BrushShape)
                        {
                            case PixelDrawing.BrushShapeType.Rectangle:
                                CvInvoke.Rectangle(LayerCache.ImageBgr, operationDrawing.Rectangle,
                                    new MCvScalar(color.B, color.G, color.R), operationDrawing.Thickness,
                                    operationDrawing.LineType);
                                break;
                            case PixelDrawing.BrushShapeType.Circle:
                                CvInvoke.Circle(LayerCache.ImageBgr, operation.Location, operationDrawing.BrushSize / 2,
                                    new MCvScalar(color.B, color.G, color.R), operationDrawing.Thickness,
                                    operationDrawing.LineType);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (operation.OperationType == PixelOperation.PixelOperationType.Text)
                    {
                        var operationText = (PixelText) operation;
                        var color = operationText.IsAdd
                            ? (DrawingsGrid.SelectedItems.Contains(operation)
                                ? Settings.PixelEditor.AddPixelHighlightColor
                                : Settings.PixelEditor.AddPixelColor)
                            : (DrawingsGrid.SelectedItems.Contains(operation)
                                ? Settings.PixelEditor.RemovePixelHighlightColor
                                : Settings.PixelEditor.RemovePixelColor);

                        CvInvoke.PutText(LayerCache.ImageBgr, operationText.Text, operationText.Location,
                            operationText.Font, operationText.FontScale, new MCvScalar(color.B, color.G, color.R),
                            operationText.Thickness, operationText.LineType, operationText.Mirror);
                    }
                    else if (operation.OperationType == PixelOperation.PixelOperationType.Eraser)
                    {
                        if (imageSpan[LayerCache.Image.GetPixelPos(operation.Location)] < 10) continue;
                        var color = DrawingsGrid.SelectedItems.Contains(operation)
                            ? Settings.PixelEditor.RemovePixelHighlightColor
                            : Settings.PixelEditor.RemovePixelColor;
                        for (int i = 0; i < LayerCache.LayerContours.Size; i++)
                        {
                            if (CvInvoke.PointPolygonTest(LayerCache.LayerContours[i], operation.Location, false) >= 0)
                            {
                                CvInvoke.DrawContours(LayerCache.ImageBgr, LayerCache.LayerContours, i,
                                    new MCvScalar(color.B, color.G, color.R), -1);
                                break;
                            }
                        }
                    }
                    else if (operation.OperationType == PixelOperation.PixelOperationType.Supports)
                    {
                        var operationSupport = (PixelSupport) operation;
                        var color = DrawingsGrid.SelectedItems.Contains(operation)
                            ? Settings.PixelEditor.SupportsHighlightColor
                            : Settings.PixelEditor.SupportsColor;

                        CvInvoke.Circle(LayerCache.ImageBgr, operation.Location, operationSupport.TipDiameter / 2,
                            new MCvScalar(color.B, color.G, color.R), -1);
                    }
                    else if (operation.OperationType == PixelOperation.PixelOperationType.DrainHole)
                    {
                        var operationDrainHole = (PixelDrainHole) operation;
                        var color = DrawingsGrid.SelectedItems.Contains(operation)
                            ? Settings.PixelEditor.DrainHoleHighlightColor
                            : Settings.PixelEditor.DrainHoleColor;

                        CvInvoke.Circle(LayerCache.ImageBgr, operation.Location, operationDrainHole.Diameter / 2,
                            new MCvScalar(color.B, color.G, color.R), -1);
                    }
                }

                // Show crosshairs for selected issues if crosshair mode is enabled via toolstrip button.
                // Even when enabled, crosshairs are hidden in pixel edit mode when SHIFT is pressed.
                if (_showLayerImageCrosshairs &&
                    Settings.LayerPreview.CrosshairShowOnlyOnSelectedIssues &&
                    Issues.Count > 0 &&
                    IssuesGrid.SelectedItems.Count > 0 &&
                    LayerImageBox.Zoom <=
                    AppSettings.CrosshairFadeLevel && // Only draw crosshairs when zoom level is below the configurable crosshair fade threshold.
                    !_isPixelEditorActive)
                {


                    foreach (LayerIssue issue in selectedIssues)
                    {
                        // Don't render crosshairs for selected issue that are not on the current layer, or for 
                        // issue types that don't have a specific location or bounds.
                        if (issue.LayerIndex != ActualLayer || issue.Type == LayerIssue.IssueType.EmptyLayer
                                                            || issue.Type == LayerIssue.IssueType.TouchingBound)
                            continue;

                        DrawCrosshair(issue.BoundingRectangle);
                    }
                }

                if (_showLayerImageRotated)
                {
                    CvInvoke.Rotate(LayerCache.ImageBgr, LayerCache.ImageBgr, RotateFlags.Rotate90Clockwise);
                }


                LayerImageBox.Image = LayerCache.Bitmap = LayerCache.ImageBgr.ToBitmap();

                RefreshCurrentLayerData();

                watch.Stop();
                ShowLayerRenderMs = watch.ElapsedMilliseconds;
                AddLogVerbose($"Show Layer: {_actualLayer}", watch.Elapsed.TotalSeconds);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// Draw a crosshair around a rectangle
        /// </summary>
        /// <param name="rect"></param>
        public void DrawCrosshair(Rectangle rect)
        {
            // Gradually increase line thickness from 1 to 3 at the lower-end of the zoom range.
            // This prevents the crosshair lines from disappearing due to being too thin to
            // render at very low zoom factors.
            var lineThickness = (LayerImageBox.Zoom > 100) ? 1 : (LayerImageBox.Zoom < 50) ? 3 : 2;
            var color = new MCvScalar(Settings.LayerPreview.CrosshairColor.B, Settings.LayerPreview.CrosshairColor.G,
                Settings.LayerPreview.CrosshairColor.R);


            // LEFT
            var startPoint = new System.Drawing.Point(Math.Max(0, rect.X - Settings.LayerPreview.CrosshairMargin - 1),
                rect.Y + rect.Height / 2);
            var endPoint =
                new System.Drawing.Point(
                    Settings.LayerPreview.CrosshairLength == 0
                        ? 0
                        : (int)Math.Max(0, startPoint.X - Settings.LayerPreview.CrosshairLength + 1),
                    startPoint.Y);

            CvInvoke.Line(LayerCache.ImageBgr,
                startPoint,
                endPoint,
                color,
                lineThickness);


            // RIGHT
            startPoint.X = Math.Min(LayerCache.ImageBgr.Width,
                rect.Right + Settings.LayerPreview.CrosshairMargin);
            endPoint.X = Settings.LayerPreview.CrosshairLength == 0
                ? LayerCache.ImageBgr.Width
                : (int)Math.Min(LayerCache.ImageBgr.Width, startPoint.X + Settings.LayerPreview.CrosshairLength - 1);

            CvInvoke.Line(LayerCache.ImageBgr,
                startPoint,
                endPoint,
                color,
                lineThickness);

            // TOP
            startPoint = new System.Drawing.Point(rect.X + rect.Width / 2,
                Math.Max(0, rect.Y - Settings.LayerPreview.CrosshairMargin - 1));
            endPoint = new System.Drawing.Point(startPoint.X,
                (int)(Settings.LayerPreview.CrosshairLength == 0
                    ? 0
                    : Math.Max(0, startPoint.Y - Settings.LayerPreview.CrosshairLength + 1)));


            CvInvoke.Line(LayerCache.ImageBgr,
                startPoint,
                endPoint,
                color,
                lineThickness);

            // Bottom
            startPoint.Y = Math.Min(LayerCache.ImageBgr.Height, rect.Bottom + Settings.LayerPreview.CrosshairMargin);
            endPoint.Y = Settings.LayerPreview.CrosshairLength == 0
                ? LayerCache.ImageBgr.Height
                : (int)Math.Min(LayerCache.ImageBgr.Height, startPoint.Y + Settings.LayerPreview.CrosshairLength - 1);

            CvInvoke.Line(LayerCache.ImageBgr,
                startPoint,
                endPoint,
                color,
                lineThickness);
        }

        public Point GetTransposedPoint(Point point, bool clockWise = true, bool ignoreLayerRotation = false)
        {
            if (!_showLayerImageRotated && !ignoreLayerRotation) return point;
            return clockWise
                ? new Point(point.Y, LayerCache.Image.Height - 1 - point.X)
                : new Point(LayerCache.Image.Height - 1 - point.Y, point.X);
        }

        public Rectangle GetTransposedRectangle(RectangleF rectangleF, bool clockWise = true, bool ignoreLayerRotation = false) =>
            GetTransposedRectangle(Rectangle.Round(rectangleF), clockWise, ignoreLayerRotation);

        public Rectangle GetTransposedRectangle(Rectangle rectangle, bool clockWise = true, bool ignoreLayerRotation = false)
        {
            if (rectangle.IsEmpty || (!ignoreLayerRotation && !_showLayerImageRotated)) return rectangle;
            return clockWise
                ? new Rectangle(LayerCache.Image.Height - rectangle.Bottom,
                    rectangle.Left, rectangle.Height, rectangle.Width)
                //: new Rectangle(ActualLayerImage.Width - rectangle.Bottom, rectangle.Left, rectangle.Width, rectangle.Height);
                //: new Rectangle(ActualLayerImage.Width - rectangle.Bottom, ActualLayerImage.Height-rectangle.Right, rectangle.Width, rectangle.Height); // Rotate90FlipX: // = Rotate270FlipY
                //: new Rectangle(rectangle.Top, rectangle.Left, rectangle.Width, rectangle.Height); // Rotate270FlipX:  // = Rotate90FlipY
                : new Rectangle(rectangle.Top, LayerCache.Image.Height - rectangle.Right, rectangle.Height, rectangle.Width); // Rotate90FlipNone:  // = Rotate270FlipXY
        }

        /// <summary>
        /// Gets the bounding rectangle of the passed issue, automatically adjusting
        /// the coordinates and width/height to account for whether or not the layer
        /// preview image is rotated.  Used to ensure images are properly zoomed or
        /// centered independent of the layer preview rotation.
        /// </summary>
        private Rectangle GetTransposedIssueBounds(LayerIssue issue)
        {
            if (issue.X >= 0 && issue.Y >= 0 && (issue.BoundingRectangle.IsEmpty || issue.Size == 1) &&
                _showLayerImageRotated)
                return new Rectangle(LayerCache.Image.Height - 1 - issue.Y,
                    issue.X, 1, 1);

            return GetTransposedRectangle(issue.BoundingRectangle);
        }

        /// <summary>
        /// Centers layer view on a X,Y coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">X coordinate</param>
        /// <param name="zoomLevel">Zoom level to set, 0 to ignore or negative value to get current locked zoom level</param>
        public void CenterLayerAt(double x, double y, int zoomLevel = 0)
        {
            if (zoomLevel < 0) zoomLevel = AppSettings.LockedZoomLevel;
            if (zoomLevel > 0) LayerImageBox.Zoom = zoomLevel;
            LayerImageBox.CenterAt(x, y);
        }

        /// <summary>
        /// Centers layer view on a X,Y coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">X coordinate</param>
        /// <param name="zoomLevel">Zoom level to set, 0 to ignore or negative value to get current locked zoom level</param>
        public void CenterLayerAt(int x, int y, int zoomLevel = 0)
        {
            if (zoomLevel < 0) zoomLevel = AppSettings.LockedZoomLevel;
            if (zoomLevel > 0) LayerImageBox.Zoom = zoomLevel;
            LayerImageBox.CenterAt(x, y);
        }


        public void CenterLayerAt(Rectangle rectangle, int zoomLevel = 0, bool zoomToRegion = false)
        {
            var viewPort = LayerImageBox.GetSourceImageRegion();
            if (zoomToRegion ||
                rectangle.Width * AppSettings.LockedZoomLevel / LayerImageBox.Zoom > viewPort.Width ||
                rectangle.Height * AppSettings.LockedZoomLevel / LayerImageBox.Zoom > viewPort.Height)
            {
                Debug.WriteLine("zoom to region");
                //SupressLayerZoomEvent = true;
                LayerImageBox.ZoomToRegion(rectangle, 10);
                //SupressLayerZoomEvent = false;
                //LayerImageBox.ZoomOut(true);
                return;
            }
            Debug.WriteLine($"Center at {zoomLevel}");
            CenterLayerAt(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2, zoomLevel);
        }

        /// <summary>
        /// Centers layer view on a <see cref="Point"/>
        /// </summary>
        /// <param name="point">Point holding X and Y coordinates</param>
        /// <param name="zoomLevel">Zoom level to set, 0 to ignore or negative value to get current locked zoom level</param>
        public void CenterLayerAt(Point point, int zoomLevel = 0) => CenterLayerAt(point.X, point.Y, zoomLevel);


        /// <summary>
        /// Zoom the layer preview to the passed issue, or if appropriate for issue type,
        /// Zoom to fit the plate or print bounds.
        /// </summary>
        private void ZoomToIssue(LayerIssue issue)
        {
            if (issue.Type == LayerIssue.IssueType.TouchingBound || issue.Type == LayerIssue.IssueType.EmptyLayer ||
                (issue.X == -1 && issue.Y == -1))
            {
                ZoomToFit();
                return;
            }

            if (issue.X >= 0 && issue.Y >= 0)
            {
                // Check to see if this zoom action will cross the crosshair fade threshold
                /*if (tsLayerImageShowCrosshairs.Checked && !ReferenceEquals(Issues, null) && flvIssues.SelectedIndices.Count > 0
                   && pbLayer.Zoom <= CrosshairFadeLevel && LockedZoomLevel > CrosshairFadeLevel)
                {
                    // Refresh the preview without the crosshairs before zooming-in.
                    // Prevents zoomed-in crosshairs from breifly being displayed before
                    // the Layer Preview is refreshed post-zoom.
                    tsLayerImageShowCrosshairs.Checked = false;
                    ShowLayer();
                    tsLayerImageShowCrosshairs.Checked = true;
                }*/


                CenterLayerAt(GetTransposedIssueBounds(issue), AppSettings.LockedZoomLevel);

            }
        }

        /// <summary>
        /// Center the layer preview on the passed issue, or if appropriate for issue type,
        /// Zoom to fit the plate or print bounds.
        /// </summary>
        private void CenterAtIssue(LayerIssue issue)
        {
            if (issue.Type == LayerIssue.IssueType.TouchingBound || issue.Type == LayerIssue.IssueType.EmptyLayer ||
                (issue.X == -1 && issue.Y == -1))
            {
                ZoomToFit();
            }

            if (issue.X >= 0 && issue.Y >= 0)
            {
                CenterLayerAt(GetTransposedIssueBounds(issue));
            }
        }

        public void ZoomToFitSimple()
        {
            ZoomToFit(ZoomToFitType.Image);
        }

        public void ZoomToFitPrintVolume()
        {
            ZoomToFit(ZoomToFitType.Volume);
        }

        private void ZoomToFit(ZoomToFitType fitType = ZoomToFitType.Auto)
        {
            if (!IsFileLoaded) return;

            const byte margin = 10;
            // If ALT key is pressed when ZoomToFit is performed, the configured option for 
            // zoom to plate vs. zoom to print bounds will be inverted.

            switch (fitType)
            {
                case ZoomToFitType.Auto:
                    if (Settings.LayerPreview.ZoomToFitPrintVolumeBounds ^ (_globalModifiers & KeyModifiers.Alt) != 0)
                    {
                        if (!_showLayerImageRotated)
                        {
                            LayerImageBox.ZoomToRegion(SlicerFile.LayerManager.BoundingRectangle, margin);
                        }
                        else
                        {
                            LayerImageBox.ZoomToRegion(LayerCache.Image.Height - 1 - SlicerFile.LayerManager.BoundingRectangle.Bottom,
                                SlicerFile.LayerManager.BoundingRectangle.X,
                                SlicerFile.LayerManager.BoundingRectangle.Height,
                                SlicerFile.LayerManager.BoundingRectangle.Width, margin
                            );
                        }
                    }
                    else
                    {
                        LayerImageBox.ZoomToFit();
                    }
                    break;
                case ZoomToFitType.Image:
                    LayerImageBox.ZoomToFit();
                    break;
                case ZoomToFitType.Volume:
                    LayerImageBox.ZoomToRegion(GetTransposedRectangle(SlicerFile.LayerManager.BoundingRectangle), margin);
                    break;
                case ZoomToFitType.Selection:
                    LayerImageBox.ZoomToSelectionRegion(margin);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fitType), fitType, null);
            }
            
        }

        /// <summary>
        /// If there is an issue under the point location passed, that issue will be selected and
        /// scrolled into view on the IssueList.
        /// </summary>
        private void SelectIssueAtPoint(Point location)
        {
            //location = GetTransposedPoint(location);
            // If location clicked is within an issue, activate it.
            for (var i = 0; i < Issues.Count; i++)
            {
                LayerIssue issue = Issues[i];

                if (issue.LayerIndex != ActualLayer) continue;
                if (!GetTransposedIssueBounds(issue).Contains(location)) continue;

                IssueSelectedIndex = i;
                SelectedTabItem = TabIssues;
                break;
            }
        }

        private void LayerImageBox_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(LayerImageBox);
            if (!LayerImageBox.IsPointInImage(pointer.Position)) return;
            Point location = LayerImageBox.PointToImage(pointer.Position).ToDotNet();
            if (LayerImageBox.SelectionMode == AdvancedImageBox.SelectionModes.Rectangle)
            {
                if (e.InitialPressMouseButton == MouseButton.Left)
                {
                    if ((e.KeyModifiers & KeyModifiers.Alt) != 0)
                    {
                        if (SelectObjectRoi(ROI) == 0) SelectObjectRoi(location);
                        return;
                    }
                    return;
                }

                if (e.InitialPressMouseButton == MouseButton.Right)
                {
                    if (!LayerImageBox.IsPointInImage(pointer.Position)) return;
                    SelectObjectRoi(location);
                    
                    return;
                }

                return;
            }

            if ((e.KeyModifiers & KeyModifiers.Control) != 0)
            {
                // Check to see if the clicked location is an issue,
                // and if so, select it in the ListView.
                SelectIssueAtPoint(location);

                return;
            }

            // Shift must be pressed for any pixel edit action, middle button is ignored.
            if (!IsPixelEditorActive || e.InitialPressMouseButton == MouseButton.Middle ||
                (e.KeyModifiers & KeyModifiers.Shift) == 0) return;
            _lastPixelMouseLocation = Point.Empty;

            // Left or Alt-Right Adds pixel, Right or Alt-Left removes pixel
            DrawPixel(e.InitialPressMouseButton == MouseButton.Left ^ (e.KeyModifiers & KeyModifiers.Alt) != 0, location, e.KeyModifiers);
        }

        private void LayerImageBoxOnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.ClickCount != 2 || (e.KeyModifiers & KeyModifiers.Alt) != 0 || (e.KeyModifiers & KeyModifiers.Shift) != 0) return;
            var pointer = e.GetCurrentPoint(LayerImageBox);
            if (pointer.Properties.IsLeftButtonPressed)
            {
                if (!LayerImageBox.IsPointInImage(pointer.Position)) return;
                var location = LayerImageBox.PointToImage(pointer.Position).ToDotNet();
                CenterLayerAt(location, AppSettings.LockedZoomLevel);

                // Check to see if the clicked location is an issue, and if so, select it in the ListView.
                SelectIssueAtPoint(location);

                return;
            }

            if (pointer.Properties.IsRightButtonPressed)
            {
                ZoomToFit();
                return; 
            }
            e.Handled = true;
        }

        private void LayerImageBoxOnDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if ((_globalModifiers & KeyModifiers.Alt) != 0 || (_globalModifiers & KeyModifiers.Shift) != 0) return;
            
            e.Handled = true;
        }

        private void LayerImageBox_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                LayerImageBox.SelectNone();
                e.Handled = true;
                return;
            }

            if ((e.KeyModifiers & KeyModifiers.Control) != 0)
            {
                if (e.Key == Key.D0 || e.Key == Key.NumPad0)
                {
                    ZoomToFit();
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.R)
                {
                    ShowLayerImageRotated = !_showLayerImageRotated;
                    e.Handled = true;
                    return;
                }
            }
        }

        private void LayerImageBoxOnPointerMoved(object? sender, PointerEventArgs e)
        {
            var pointer = e.GetCurrentPoint(LayerImageBox);
            
            if (!LayerImageBox.IsPointInImage(pointer.Position)) return;
            var location = LayerImageBox.PointToImage(pointer.Position).ToDotNet();

            if ((e.KeyModifiers & KeyModifiers.Control) != 0)
            {
                Point realLocation = GetTransposedPoint(location);
                unsafe
                {
                    var brightness = LayerCache.ImageSpan[LayerCache.Image.GetPixelPos(realLocation)];
                    LayerPixelPicker.Set(realLocation, brightness);
                }

                RaisePropertyChanged(nameof(LayerPixelPicker));
            }

            if ((e.KeyModifiers & KeyModifiers.Shift) == 0) return;

            if (_lastPixelMouseLocation == location) return;
            _lastPixelMouseLocation = location;


            // Bail here if we're not in a draw operation, if the mouse button is not either
            // left or right, or if the location of the mouse pointer is not within the image.
            if (SelectedPixelOperationTabIndex != (int)PixelOperation.PixelOperationType.Drawing) return;
            if (!IsPixelEditorActive || pointer.Properties.IsMiddleButtonPressed) return;
            //if (!pbLayer.IsPointInImage(e.Location)) return;

            if (pointer.Properties.IsRightButtonPressed)
            {
                // Right or Alt-Left will remove a pixel
                DrawPixel(false ^ (e.KeyModifiers & KeyModifiers.Alt) != 0, location, e.KeyModifiers);
                return;
            }

            if (pointer.Properties.IsLeftButtonPressed)
            {
                // Left or Alt-Right will add a pixel
                DrawPixel(true ^ (e.KeyModifiers & KeyModifiers.Alt) != 0, location, e.KeyModifiers);
                return;
            }
        }

        public bool SelectObjectRoi(Point location)
        {
            var point = GetTransposedPoint(location);
            var brightness = LayerCache.Image.GetByte(point);

            if (brightness == 0) return false;
            for (int i = 0; i < LayerCache.LayerContours.Size; i++)
            {
                if (CvInvoke.PointPolygonTest(LayerCache.LayerContours[i], point, false) >= 0)
                {
                    var rectangle =
                        GetTransposedRectangle(CvInvoke.BoundingRectangle(LayerCache.LayerContours[i]));
                    ROI = rectangle;

                    return true;
                }
            }

            return false;
        }

        public uint SelectObjectRoi(Rectangle roiRectangle)
        {
            if (roiRectangle.IsEmpty) return 0;
            List<Rectangle> rectangles = new List<Rectangle>();
            for (int i = 0; i < LayerCache.LayerContours.Size; i++)
            {
                var rectangle = CvInvoke.BoundingRectangle(LayerCache.LayerContours[i]);
                //roi.Intersect(rectangle);
                if (roiRectangle.IntersectsWith(rectangle))
                {
                    rectangles.Add(rectangle);
                }

            }
            roiRectangle = rectangles.Count == 0 ? Rectangle.Empty : rectangles[0];
            for (var i = 1; i < rectangles.Count; i++)
            {
                var rectangle = rectangles[i];
                roiRectangle = Rectangle.Union(roiRectangle, rectangle);
            }

            ROI = GetTransposedRectangle(roiRectangle);

            return (uint)rectangles.Count;
        }

        public void OnLayerPixelPickerClicked()
        {
            if (!LayerPixelPicker.IsSet) return;
            CenterLayerAt(GetTransposedPoint(LayerPixelPicker.Location, false), -1);
        }

        public async void SaveCurrentLayerImage()
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filters = Helpers.PngFileFilter,
                DefaultExtension = ".png",
                InitialFileName = $"{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}_layer{ActualLayer}.png"
            };

            var result = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(result)) return;

            LayerCache.ImageBgr.Save(result);
        }

        const byte _pixelEditorCursorMinDiamater = 10;
        public void UpdatePixelEditorCursor()
        {
            Mat cursor = null;
            MCvScalar _pixelEditorCursorColor = new MCvScalar(
                Settings.PixelEditor.CursorColor.B, 
                Settings.PixelEditor.CursorColor.G,
                Settings.PixelEditor.CursorColor.R,
                Settings.PixelEditor.CursorColor.A);
            switch ((PixelOperation.PixelOperationType)SelectedPixelOperationTabIndex)
            {
                case PixelOperation.PixelOperationType.Drawing:
                    
                    if (DrawingPixelDrawing.BrushSize > 1)
                    {
                        cursor = EmguExtensions.InitMat(new Size(DrawingPixelDrawing.BrushSize, DrawingPixelDrawing.BrushSize), 4);
                        switch (DrawingPixelDrawing.BrushShape)
                        {
                            case PixelDrawing.BrushShapeType.Rectangle:
                                CvInvoke.Rectangle(cursor,
                                    new Rectangle(Point.Empty, new Size(DrawingPixelDrawing.BrushSize, DrawingPixelDrawing.BrushSize)),
                                    _pixelEditorCursorColor, DrawingPixelDrawing.Thickness, DrawingPixelDrawing.LineType);
                                _pixelEditorCursorColor.V3 = 255;
                                CvInvoke.Rectangle(cursor,
                                    new Rectangle(Point.Empty, new Size(DrawingPixelDrawing.BrushSize-1, DrawingPixelDrawing.BrushSize-1)),
                                    _pixelEditorCursorColor, 1, DrawingPixelDrawing.LineType);
                                break;
                            case PixelDrawing.BrushShapeType.Circle:
                                var center = new Point(DrawingPixelDrawing.BrushSize / 2, DrawingPixelDrawing.BrushSize / 2);
                                CvInvoke.Circle(cursor,
                                   center,
                                   center.X,
                                   _pixelEditorCursorColor,
                                   DrawingPixelDrawing.Thickness, DrawingPixelDrawing.LineType
                                   );
                                _pixelEditorCursorColor.V3 = 255;
                                CvInvoke.Circle(cursor,
                                    center,
                                    center.X,
                                    _pixelEditorCursorColor,
                                    1, DrawingPixelDrawing.LineType
                                );
                                break;
                        }
                    }
                    break;
                case PixelOperation.PixelOperationType.Text:
                    var text = DrawingPixelText.Text;
                    if (string.IsNullOrEmpty(text) || DrawingPixelText.FontScale < 0.2) return;

                    int baseLine = 0;
                    var size = CvInvoke.GetTextSize(text, DrawingPixelText.Font, DrawingPixelText.FontScale, DrawingPixelText.Thickness, ref baseLine);
                    cursor = EmguExtensions.InitMat(new Size(size.Width * 2, size.Height * 2), 4);
                    //CvInvoke.Rectangle(cursor, new Rectangle(Point.Empty, size), _pixelEditorCursorColor, -1, DrawingPixelText.LineType);
                    //_pixelEditorCursorColor.V3 = 255;
                    //CvInvoke.Rectangle(cursor, new Rectangle(new Point(size.Width, 0), size), _pixelEditorCursorColor, 1, DrawingPixelText.LineType);
                    
                    CvInvoke.PutText(cursor, text, new Point(size.Width, size.Height), DrawingPixelText.Font, DrawingPixelText.FontScale, _pixelEditorCursorColor, DrawingPixelText.Thickness, DrawingPixelText.LineType, DrawingPixelText.Mirror);
                    if (_showLayerImageRotated)
                    {
                        CvInvoke.Rotate(cursor, cursor, RotateFlags.Rotate90Clockwise);
                    }
                    break;
                case PixelOperation.PixelOperationType.Supports:
                case PixelOperation.PixelOperationType.DrainHole:
                    var diameter = SelectedPixelOperationTabIndex == (byte)PixelOperation.PixelOperationType.Supports ?
                        DrawingPixelSupport.TipDiameter : DrawingPixelDrainHole.Diameter;

                    if (diameter >= _pixelEditorCursorMinDiamater)
                    {
                        cursor = EmguExtensions.InitMat(new System.Drawing.Size(diameter, diameter), 4);
                        var center = new Point(diameter / 2, diameter / 2);
                        CvInvoke.Circle(cursor,
                            center,
                            center.X,
                            _pixelEditorCursorColor,
                            -1, LineType.AntiAlias
                        );
                        _pixelEditorCursorColor.V3 = 255;
                        CvInvoke.Circle(cursor,
                            center,
                            center.X,
                            _pixelEditorCursorColor,
                            1, LineType.AntiAlias
                        );
                    }
                    break;
            }

            if (!(cursor is null))
            {
                LayerImageBox.TrackerImage = cursor.ToBitmap();
                //cursor.Save("D:\\Cursor.png");
                //LayerImageBox.TrackerImage.Save("D:\\CursorAVA.png");
            }
            /*else if (tabControlPixelEditor.SelectedIndex == (byte)PixelOperation.PixelOperationType.Text)
            {
                var text = tbPixelEditorTextText.Text;
                if (string.IsNullOrEmpty(text) || nmPixelEditorTextFontScale.Value < 0.2m) return;

                LineType lineType = (LineType)cbPixelEditorTextLineType.SelectedItem;
                FontFace fontFace = (FontFace)cbPixelEditorTextFontFace.SelectedItem;
                double scale = (double) nmPixelEditorTextFontScale.Value * pbLayer.Zoom / 100;
                int thickness = (int) nmPixelEditorTextThickness.Value;
                int baseLine = 0;
                var size = CvInvoke.GetTextSize(text, fontFace, scale, thickness, ref baseLine);
                mat = new Mat(size, DepthType.Cv8U, 4);
                CvInvoke.PutText(mat, text, new Point(0,0), fontFace, scale, new MCvScalar(255,100,255, 255), thickness, lineType, cbPixelEditorTextMirror.Checked);
            }*/
        }
    }
}
