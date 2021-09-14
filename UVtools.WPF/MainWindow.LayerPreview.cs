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
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.AvaloniaControls;
using UVtools.Core;
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.Core.PixelEditor;
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
        public Slider LayerSlider;
        public Track LayerSlicerTrack;
        public Panel LayerNavigationTooltipPanel;
        public Border LayerNavigationTooltipBorder;
        private Canvas _issuesSliderCanvas;


        private Timer _layerNavigationTooltipTimer = new(0.1) { AutoReset = false };
        private uint _actualLayer;

        private bool _showLayerImageRotated;
        private bool _showLayerImageRotateCwDirection = true;
        private bool _showLayerImageRotateCcwDirection;
        private bool _showLayerImageFlipped;
        private bool _showLayerImageFlippedHorizontally = true;
        private bool _showLayerImageFlippedVertically;
        private bool _showLayerImageDifference;
        private bool _showLayerImageIssues = true;
        private bool _showLayerImageCrosshairs = true;
        private bool _isPixelEditorActive;
        private bool _showLayerOutlinePrintVolumeBoundary;
        private bool _showLayerOutlineLayerBoundary;
        private bool _showLayerOutlineContourBoundary;
        private bool _showLayerOutlineHollowAreas;
        private bool _showLayerOutlineCentroids;
        private bool _showLayerOutlineEdgeDetection;
        private bool _showLayerOutlineDistanceDetection;
        private bool _showLayerOutlineSkeletonize;


        private bool _isTooltipOverlayVisible;
        private string _tooltipOverlayText;

        private long _showLayerRenderMs;

        public LayerCache LayerCache = new ();
        private Point _lastPixelMouseLocation = Point.Empty;
        private readonly List<Point[]> _maskPoints = new ();



        public void InitLayerPreview()
        {
            LayerImageBox = this.FindControl<AdvancedImageBox>("LayerImage");
            LayerSlider = this.FindControl<Slider>("Layer.Navigation.Slider");
            LayerNavigationTooltipPanel = this.FindControl<Panel>("Layer.Navigation.Tooltip.Panel");
            LayerNavigationTooltipBorder = this.FindControl<Border>("Layer.Navigation.Tooltip.Border");
            _issuesSliderCanvas = this.Find<Canvas>("Layer.Navigation.IssuesCanvas");

            LayerSlider.TemplateApplied += (sender, e) =>
            {
                LayerSlicerTrack = e.NameScope.Find<Track>("PART_Track");
            };

            _showLayerImageDifference = Settings.LayerPreview.ShowLayerDifference;
            _showLayerOutlinePrintVolumeBoundary = Settings.LayerPreview.VolumeBoundsOutline;
            _showLayerOutlineLayerBoundary = Settings.LayerPreview.LayerBoundsOutline;
            _showLayerOutlineContourBoundary = Settings.LayerPreview.ContourBoundsOutline;
            _showLayerOutlineHollowAreas = Settings.LayerPreview.HollowOutline;
            _showLayerOutlineCentroids = Settings.LayerPreview.CentroidOutline;
            
            LayerImageBox.ZoomLevels = new AdvancedImageBox.ZoomLevelCollection(AppSettings.ZoomLevels);
            
            LayerImageBox.GetObservable(AdvancedImageBox.ZoomProperty).Subscribe(zoom =>
            {
                var newZoom = zoom;
                var oldZoom = LayerImageBox.OldZoom;
                RaisePropertyChanged(nameof(LayerZoomStr));
                AddLogVerbose($"Zoomed from {oldZoom} to {newZoom}");
                
                if (_showLayerImageCrosshairs &&
                    Issues.Count > 0 &&
                    (oldZoom < 50 &&
                     newZoom >= 50 // Trigger refresh as crosshair thickness increases at lower zoom levels
                     || oldZoom > 100 && newZoom <= 100
                     || oldZoom is >= 50 and <= 100 && (newZoom is < 50 or > 100)
                     || oldZoom <= AppSettings.CrosshairFadeLevel &&
                     newZoom > AppSettings.CrosshairFadeLevel // Trigger refresh as zoom level manually crosses fade threshold
                     || oldZoom > AppSettings.CrosshairFadeLevel && newZoom <= AppSettings.CrosshairFadeLevel)

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
            });

            LayerImageBox.GetObservable(AdvancedImageBox.SelectionRegionProperty)
                .Subscribe(rect => RaisePropertyChanged(nameof(LayerROIStr)));

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
                var rect = LayerImageBox.SelectionRegion;
                if (!rect.IsEmpty)
                {
                    rect = GetTransposedRectangle(rect.ToDotNet(), true).ToAvalonia();
                }

                if (!RaiseAndSetIfChanged(ref _showLayerImageRotated, value) || !IsFileLoaded) return;
                
                if (!rect.IsEmpty)
                {
                    LayerImageBox.SelectionRegion = GetTransposedRectangle(rect.ToDotNet()).ToAvalonia();
                }

                ZoomToFit();
                ShowLayer();
            }
        }

        public bool ShowLayerImageRotateCWDirection
        {
            get => _showLayerImageRotateCwDirection;
            set
            {
                var rect = LayerImageBox.SelectionRegion;
                if (!rect.IsEmpty)
                {
                    rect = GetTransposedRectangle(rect.ToDotNet(), true).ToAvalonia();
                }

                if (!RaiseAndSetIfChanged(ref _showLayerImageRotateCwDirection, value)) return;
                if (!_showLayerImageRotated) return;

                if (!rect.IsEmpty)
                {
                    LayerImageBox.SelectionRegion = GetTransposedRectangle(rect.ToDotNet()).ToAvalonia();
                }

                ZoomToFit();
                ShowLayer();
            }
        }

        public bool ShowLayerImageRotateCCWDirection
        {
            get => _showLayerImageRotateCcwDirection;
            set
            {
                var rect = LayerImageBox.SelectionRegion;
                if (!rect.IsEmpty)
                {
                    rect = GetTransposedRectangle(rect.ToDotNet(), true).ToAvalonia();
                }

                if (!RaiseAndSetIfChanged(ref _showLayerImageRotateCcwDirection, value)) return;
                if (!_showLayerImageRotated) return;

                if (!rect.IsEmpty)
                {
                    LayerImageBox.SelectionRegion = GetTransposedRectangle(rect.ToDotNet()).ToAvalonia();
                }

                ZoomToFit();
                ShowLayer();
            }
        }

        public bool ShowLayerImageFlipped
        {
            get => _showLayerImageFlipped;
            set
            {
                var rect = LayerImageBox.SelectionRegion;
                if (!rect.IsEmpty)
                {
                    rect = GetTransposedRectangle(rect.ToDotNet(), true).ToAvalonia();
                }

                if (!RaiseAndSetIfChanged(ref _showLayerImageFlipped, value)) return;

                if (!rect.IsEmpty)
                {
                    LayerImageBox.SelectionRegion = GetTransposedRectangle(rect.ToDotNet()).ToAvalonia();
                }

                ShowLayer();
            }
        }

        public bool ShowLayerImageFlippedHorizontally
        {
            get => _showLayerImageFlippedHorizontally;
            set
            {
                var rect = LayerImageBox.SelectionRegion;
                if (!rect.IsEmpty)
                {
                    rect = GetTransposedRectangle(rect.ToDotNet(), true).ToAvalonia();
                }

                if (!RaiseAndSetIfChanged(ref _showLayerImageFlippedHorizontally, value)) return;
                if (!_showLayerImageFlipped) return;

                if (!rect.IsEmpty)
                {
                    LayerImageBox.SelectionRegion = GetTransposedRectangle(rect.ToDotNet()).ToAvalonia();
                }

                ShowLayer();
            }
        }

        public bool ShowLayerImageFlippedVertically
        {
            get => _showLayerImageFlippedVertically;
            set
            {
                var rect = LayerImageBox.SelectionRegion;
                if (!rect.IsEmpty)
                {
                    rect = GetTransposedRectangle(rect.ToDotNet(), true).ToAvalonia();
                }

                if (!RaiseAndSetIfChanged(ref _showLayerImageFlippedVertically, value)) return;
                if (!_showLayerImageFlipped) return;

                if (!rect.IsEmpty)
                {
                    LayerImageBox.SelectionRegion = GetTransposedRectangle(rect.ToDotNet()).ToAvalonia();
                }

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

        public bool ShowLayerOutlineContourBoundary
        {
            get => _showLayerOutlineContourBoundary;
            set
            {
                if (!RaiseAndSetIfChanged(ref _showLayerOutlineContourBoundary, value)) return;
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

        public bool ShowLayerOutlineCentroids
        {
            get => _showLayerOutlineCentroids;
            set
            {
                if (!RaiseAndSetIfChanged(ref _showLayerOutlineCentroids, value)) return;
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

        public bool ShowLayerOutlineDistanceDetection
        {
            get => _showLayerOutlineDistanceDetection;
            set
            {
                if (!RaiseAndSetIfChanged(ref _showLayerOutlineDistanceDetection, value)) return;
                ShowLayer();
            }
        }

        public bool ShowLayerOutlineSkeletonize
        {
            get => _showLayerOutlineSkeletonize;
            set
            {
                if (!RaiseAndSetIfChanged(ref _showLayerOutlineSkeletonize, value)) return;
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
        public string ActualLayerTooltip => SlicerFile is null ? "???" : $"{Layer.ShowHeight(LayerCache.Layer?.PositionZ ?? 0)}mm\n" +
                                                                         $"{ActualLayer}\n" +
                                                                         $"{(ActualLayer + 1) * 100 / SlicerFile.LayerCount}%";

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
                if (!LayerCache.IsCached) return "Pixels: 0";
                var pixelPercent = Math.Round(LayerCache.Layer.NonZeroPixelCount * 100.0 / (SlicerFile.ResolutionX * SlicerFile.ResolutionY), 2);
                string text = $"Pixels: {LayerCache.Layer.NonZeroPixelCount} ({pixelPercent}%)";
                var exposedMillimeters = LayerCache.Layer.ExposureMillimeters;
                if (exposedMillimeters > 0)
                {
                    text += $"\nMillimeters: {exposedMillimeters}";
                }
                return text;
            }
        }

        public string LayerBoundsStr
        {
            get
            {
                if (LayerCache.Layer is null) return "Bounds: NS";
                var text = $"Bounds: {LayerCache.Layer.BoundingRectangle} ({LayerCache.Layer.BoundingRectangle.Area()}px²)";
                var rectMillimeters = LayerCache.Layer.BoundingRectangleMillimeters;
                if (!rectMillimeters.IsEmpty)
                {
                    text += $"\nBounds: {rectMillimeters} ({rectMillimeters.Area(2)}mm²)";
                }

                return text;
            }
        } 
                                                                          
        public string LayerROIStr
        {
            get
            {
                var roi = ROI;
                if (roi.IsEmpty)
                {
                    return _maskPoints.Count > 0 ? $"Masks: {_maskPoints.Count}" : "ROI: NS";
                }
                var text = $"ROI: {roi} ({roi.Area()}px²)";
                var roiMillimeters = ROIMillimeters;
                if (!roiMillimeters.IsEmpty)
                {
                    text += $"\nROI: {roiMillimeters} ({roiMillimeters.Area(2)}mm²)";
                }
                return text;
            }
        }

        public long ShowLayerRenderMs
        {
            get => _showLayerRenderMs;
            set => RaiseAndSetIfChanged(ref _showLayerRenderMs, value);
        }

        public PixelPicker LayerPixelPicker { get; } = new ();

        public string LayerZoomStr
        {
            get
            {
                var pixelSize = SlicerFile?.PixelSizeMicronsMax;
                var text = $"Zoom: [ {LayerImageBox.Zoom / 100m}x{(AppSettings.LockedZoomLevel == LayerImageBox.Zoom ? " 🔒 ]" : " ]")}";
                if (pixelSize > 0) text += $"\nPixel: {SlicerFile.PixelSizeMicronsMax}µm";
                return text;
            }
        }

        public string LayerResolutionStr
        {
            get
            {
                if (SlicerFile is null) return "Unloaded";
                var text = $"{SlicerFile.Resolution} px";
                var display = SlicerFile.Display;
                if (!display.IsEmpty)
                {
                    text += $"\n{SlicerFile.Display} mm";
                }

                return text;
            }
        }

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
                if (LayerSlicerTrack != null)
                {
                    double trackerPos = LayerSlicerTrack.Thumb.Bounds.Height / 2 + LayerSlicerTrack.Thumb.Bounds.Top;
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


        #region ROI & Mask
        public Rectangle ROI
        {
            get
            {
                var rect = LayerImageBox.SelectionRegion;
                return rect.IsEmpty ? Rectangle.Empty : GetTransposedRectangle(rect.ToDotNet(), true);
            }
            set => LayerImageBox.SelectionRegion = GetTransposedRectangle(value).ToAvalonia();
        }

        public RectangleF ROIMillimeters
        {
            get
            {
                var roi = ROI;
                var pixelSize = SlicerFile.PixelSize;
                if(roi.IsEmpty || pixelSize.IsEmpty) return RectangleF.Empty;
                return new RectangleF(
                    (float)Math.Round(roi.X * pixelSize.Width, 2),
                    (float)Math.Round(roi.Y * pixelSize.Height, 2),
                    (float)Math.Round(roi.Width * pixelSize.Width, 2),
                    (float)Math.Round(roi.Height * pixelSize.Height, 2));
            }
        }

        public void SelectModelVolumeRoi()
        {
            ROI = SlicerFile.BoundingRectangle;
        }

        public void SelectLayerVolumeRoi()
        {
            ROI = LayerCache.Layer.BoundingRectangle;
        }

       
        public List<Point[]> MaskPoints => _maskPoints;

        /*private set
            {
                if(!RaiseAndSetIfChanged(ref _maskPoints, value)) return;
                ShowLayer();
            }*/
        public void AddMaskPoints(Point[] points, bool refreshLayer = true)
        {
            if (_maskPoints.RemoveAll(points1 => points1.SequenceEqual(points)) <= 0)
            {
                _maskPoints.Add(points);
            }

            if(_maskPoints.Count > 0 && Settings.LayerPreview.MaskClearROIAfterSet) ClearROI();

            if(refreshLayer) ShowLayer();
            RaisePropertyChanged(nameof(LayerROIStr));
        }

        public void AddMaskPoints(IEnumerable<Point[]> pointsOfPoints, bool clear = true)
        {
            if (clear)
            {
                _maskPoints.Clear();
                _maskPoints.AddRange(pointsOfPoints);
            }
            else
            {
                foreach (var points in pointsOfPoints)
                {
                    if (_maskPoints.RemoveAll(points1 => points1.SequenceEqual(points)) <= 0)
                    {
                        _maskPoints.Add(points);
                    }
                }
                
            }
            
            ShowLayer();
            RaisePropertyChanged(nameof(LayerROIStr));
        }

        public void SelectLayerPositiveAreasMask()
        {
            AddMaskPoints(LayerCache.LayerContours.ToArrayOfArray());
            if (_maskPoints.Count > 0 && Settings.LayerPreview.MaskClearROIAfterSet) ClearROI();
        }

        public void SelectLayerHollowAreasMask()
        {
            var contours = EmguContours.GetNegativeContours(LayerCache.LayerContours, LayerCache.LayerContourHierarchy);
            AddMaskPoints(contours.ToArrayOfArray());
            if (_maskPoints.Count > 0 && Settings.LayerPreview.MaskClearROIAfterSet) ClearROI();
        }

        public void ClearMask()
        {
            if (_maskPoints.Count <= 0) return;
            _maskPoints.Clear();
            ShowLayer();
            RaisePropertyChanged(nameof(LayerROIStr));
        }

        public void ClearROI()
        {
            ROI = Rectangle.Empty;
        }

        public void ClearROIAndMask()
        {
            ClearROI();
            ClearMask();
        }

        public void OnROIClick()
        {
            ZoomToFit(ZoomToFitType.Selection);
        }
        #endregion

        public void GoFirstLayer()
        {
            if (!IsFileLoaded) return;
            if (!CanGoDown) return;
            ActualLayer = 0;
        }

        public void GoPreviousLayer()
        {
            if (!IsFileLoaded) return;
            if (!CanGoDown) return;
            ActualLayer--;
        }

        public void GoNextLayer()
        {
            if (!IsFileLoaded) return;
            if (!CanGoUp) return;
            ActualLayer++;
        }

        public void GoLastLayer()
        {
            if (!IsFileLoaded) return;
            if (!CanGoUp) return;
            ActualLayer = SliderMaximumValue;
        }

        public void GoMassLayer(string which)
        {
            if (!IsFileLoaded) return;
            var layer = which switch
            {
                "SB" => SlicerFile.LayerManager.SmallestBottomLayer,
                "LB" => SlicerFile.LayerManager.LargestBottomLayer,
                "SN" => SlicerFile.LayerManager.SmallestNormalLayer,
                "LN" => SlicerFile.LayerManager.LargestNormalLayer,
                _ => null
            };
            if (layer is null) return;
            ActualLayer = layer.Index;
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

            var watch = Stopwatch.StartNew();
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
                    CvInvoke.CvtColor(canny, LayerCache.ImageBgr, ColorConversion.Gray2Bgr);
                }
                else if (_showLayerOutlineDistanceDetection)
                {
                    using var distance = new Mat();
                    CvInvoke.DistanceTransform(LayerCache.Image, distance, null, DistType.C, 3);
                    //distance.ConvertTo(distance, DepthType.Cv8U);
                    CvInvoke.Normalize(distance, distance, byte.MinValue, byte.MaxValue, NormType.MinMax, DepthType.Cv8U);
                    CvInvoke.CvtColor(distance, LayerCache.ImageBgr, ColorConversion.Gray2Bgr);
                }
                else if (_showLayerOutlineSkeletonize)
                {
                    using var skeletonize = LayerCache.Image.Skeletonize();
                    CvInvoke.CvtColor(skeletonize, LayerCache.ImageBgr, ColorConversion.Gray2Bgr);
                }
                else if (_showLayerImageDifference)
                {
                    //if (_actualLayer > 0 && _actualLayer < SlicerFile.LayerCount - 1)
                   // {
                    var previousLayer = _actualLayer > 0 ? SlicerFile[_actualLayer - 1] : null;
                    var nextLayer = _actualLayer < SlicerFile.LastLayerIndex ? SlicerFile[_actualLayer + 1] : null;
                    Mat previousImage = null;
                    Mat nextImage = null;

                    // Optimize empties for now...
                    var rect = Rectangle.Empty;
                    if (!LayerCache.Layer.IsEmpty)
                    {
                        rect = LayerCache.Layer.BoundingRectangle;
                    }
                    else if (previousLayer is not null && !previousLayer.IsEmpty)
                    {
                        rect = previousLayer.BoundingRectangle;
                    }
                    else if (nextLayer is not null && !nextLayer.IsEmpty)
                    {
                        rect = nextLayer.BoundingRectangle;
                    }

                    if (previousLayer is not null && !previousLayer.IsEmpty)
                    {
                        rect = Rectangle.Union(rect, previousLayer.BoundingRectangle);
                    }
                    if (nextLayer is not null && !nextLayer.IsEmpty)
                    {
                        rect = Rectangle.Union(rect, nextLayer.BoundingRectangle);
                    }

                    /*var rect = Rectangle.Union(
                        Rectangle.Union(LayerCache.Layer.BoundingRectangle, previousLayer.BoundingRectangle),
                        nextLayer.BoundingRectangle);*/

                    if (!rect.IsEmpty && (previousLayer is not null || nextLayer is not null))
                    {
                        byte* previousSpan = null;
                        byte* nextSpan = null;
                        // Can improve performance on >4K images?
                        Parallel.Invoke(
                            () =>
                            {
                                if (previousLayer is null) return;
                                previousImage = previousLayer.LayerMat;
                                previousSpan = previousImage.GetBytePointer();
                            },
                            () =>
                            {
                                if (nextLayer is null) return;
                                nextImage = nextLayer.LayerMat;
                                nextSpan = nextImage.GetBytePointer();
                            });

                        /*using (var previousImage = SlicerFile[_actualLayer - 1].LayerMat)
                            using (var nextImage = SlicerFile[_actualLayer + 1].LayerMat)
                            {*/
                        //var previousSpan = previousImage.GetPixelSpan<byte>();
                        //var nextSpan = nextImage.GetPixelSpan<byte>();


                        int width = LayerCache.Image.Step;
                        int channels = LayerCache.ImageBgr.NumberOfChannels;
                        bool showSimilarityInstead =
                            Settings.LayerPreview.LayerDifferenceHighlightSimilarityInstead;

                        Parallel.For(rect.Y, rect.Bottom, CoreSettings.ParallelOptions, y =>
                        {
                            for (int x = rect.X; x < rect.Right; x++)
                            {
                                int pixel = y * width + x;
                                if (showSimilarityInstead)
                                {
                                    if (imageSpan[pixel] == 0) continue;
                                }
                                else
                                {
                                    if (imageSpan[pixel] != 0) continue;
                                }

                                byte brightness = 0;

                                var color = Color.Empty;
                                if (previousSpan is not null && nextSpan is not null && previousSpan[pixel] > 0 && nextSpan[pixel] > 0)
                                {
                                    brightness = Math.Max(previousSpan[pixel], nextSpan[pixel]);
                                    color = Settings.LayerPreview.BothLayerDifferenceColor;
                                }
                                else if (previousSpan is not null && previousSpan[pixel] > 0)
                                {
                                    brightness = previousSpan[pixel];
                                    color = Settings.LayerPreview.PreviousLayerDifferenceColor;
                                }
                                else if (nextSpan is not null && nextSpan[pixel] > 0)
                                {
                                    brightness = nextSpan[pixel];
                                    color = Settings.LayerPreview.NextLayerDifferenceColor;
                                }

                                if (color.IsEmpty) continue;

                                color = color.FactorColor(brightness);

                                var bgrPixel = pixel * channels;
                                imageBgrSpan[bgrPixel] = color.B; // B
                                imageBgrSpan[bgrPixel + 1] = color.G; // G
                                imageBgrSpan[bgrPixel + 2] = color.R; // R
                                //imageBgrSpan[++bgrPixel] = color.A; // A
                            }
                        });
                    }

                    previousImage?.Dispose();
                    nextImage?.Dispose();
                   // }
                }


                var selectedIssues = IssuesGrid.SelectedItems;

                if (_showLayerImageIssues && Issues.Count > 0)
                {
                    foreach (var issue in Issues)
                    {
                        if (issue.LayerIndex != ActualLayer) continue;
                        if (!issue.HaveValidPoint) continue;

                        var color = Color.Empty;
                        bool drawCrosshair = false;

                        switch (issue.Type)
                        {
                            case LayerIssue.IssueType.Island:
                                color = selectedIssues.Count > 0 && selectedIssues.Contains(issue)
                                    ? Settings.LayerPreview.IslandHighlightColor
                                    : Settings.LayerPreview.IslandColor;
                                drawCrosshair = true;

                                break;
                            case LayerIssue.IssueType.Overhang:
                                color = selectedIssues.Count > 0 && selectedIssues.Contains(issue)
                                    ? Settings.LayerPreview.OverhangHighlightColor
                                    : Settings.LayerPreview.OverhangColor;
                                drawCrosshair = true;

                                break;
                            case LayerIssue.IssueType.ResinTrap:
                                color = selectedIssues.Count > 0 && selectedIssues.Contains(issue)
                                    ? Settings.LayerPreview.ResinTrapHighlightColor
                                    : Settings.LayerPreview.ResinTrapColor;
                                drawCrosshair = true;
                                break;
                            case LayerIssue.IssueType.SuctionCup:
                                color = selectedIssues.Count > 0 && selectedIssues.Contains(issue)
                                    ? Settings.LayerPreview.SuctionCupHighlightColor
                                    : Settings.LayerPreview.SuctionCupColor;
                                drawCrosshair = true;
                                break;
                            case LayerIssue.IssueType.TouchingBound:
                                color = Settings.LayerPreview.TouchingBoundsColor;
                                break;
                        }

                        if (color.IsEmpty) continue;

                        if (drawCrosshair && _showLayerImageCrosshairs &&
                            !Settings.LayerPreview.CrosshairShowOnlyOnSelectedIssues &&
                            LayerImageBox.Zoom <= AppSettings.CrosshairFadeLevel)
                        {
                            DrawCrosshair(issue.BoundingRectangle);
                        }

                        if (issue.Contours is not null)
                        {
                            using var vec = new VectorOfVectorOfPoint(issue.Contours);
                            CvInvoke.DrawContours(LayerCache.ImageBgr, vec, -1, new MCvScalar(color.B, color.G, color.R), -1);
                            continue;
                        }

                        if (issue.Pixels is null) continue;
                        foreach (var pixel in issue.Pixels)
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

                if (_showLayerOutlineContourBoundary)
                {
                    int lastParent = -1;
                    uint reps = 0;
                    for (int i = 0; i < LayerCache.LayerContours.Size; i++)
                    {
                        var parent = LayerCache.LayerContourHierarchy[i, EmguContour.HierarchyParent];
                        if (parent == -1)
                        {
                            reps = 0;
                        }
                        if(parent == -1 || parent != lastParent) reps++;
                        if (reps % 2 == 0)
                        {
                            lastParent = parent;
                            continue;
                        }

                        CvInvoke.Rectangle(LayerCache.ImageBgr, CvInvoke.BoundingRectangle(LayerCache.LayerContours[i]),
                            new MCvScalar(
                                Settings.LayerPreview.ContourBoundsOutlineColor.B,
                                Settings.LayerPreview.ContourBoundsOutlineColor.G, 
                                Settings.LayerPreview.ContourBoundsOutlineColor.R),
                            Settings.LayerPreview.ContourBoundsOutlineThickness);

                        lastParent = parent;
                    }
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
                    using var vec = EmguContours.GetNegativeContours(LayerCache.LayerContours, LayerCache.LayerContourHierarchy);
                   if (vec.Size > 0)
                    {
                        CvInvoke.DrawContours(LayerCache.ImageBgr, vec, -1,
                            new MCvScalar(Settings.LayerPreview.HollowOutlineColor.B,
                                Settings.LayerPreview.HollowOutlineColor.G,
                                Settings.LayerPreview.HollowOutlineColor.R),
                            Settings.LayerPreview.HollowOutlineLineThickness);
                    }
                }

                if (_showLayerOutlineCentroids)
                {
                    int lastParent = -1;
                    uint reps = 0;
                    for (int i = 0; i < LayerCache.LayerContours.Size; i++)
                    {
                        var parent = LayerCache.LayerContourHierarchy[i, EmguContour.HierarchyParent];
                        if (parent == -1)
                        {
                            reps = 0;
                        }
                        if (parent == -1 || parent != lastParent) reps++;
                        if (reps % 2 == 0)
                        {
                            if (Settings.LayerPreview.CentroidOutlineHollow)
                            {
                                CvInvoke.Circle(LayerCache.ImageBgr, EmguContour.GetCentroid(LayerCache.LayerContours[i]),
                                    Settings.LayerPreview.CentroidOutlineDiameter / 2, new MCvScalar(
                                        Settings.LayerPreview.HollowOutlineColor.B,
                                        Settings.LayerPreview.HollowOutlineColor.G,
                                        Settings.LayerPreview.HollowOutlineColor.R), 1, LineType.AntiAlias);
                            }
                        }
                        else
                        {
                            CvInvoke.Circle(LayerCache.ImageBgr, EmguContour.GetCentroid(LayerCache.LayerContours[i]),
                            Settings.LayerPreview.CentroidOutlineDiameter / 2, new MCvScalar(
                                Settings.LayerPreview.CentroidOutlineColor.B,
                                Settings.LayerPreview.CentroidOutlineColor.G,
                                Settings.LayerPreview.CentroidOutlineColor.R), -1, LineType.AntiAlias);
                        }

                        

                        lastParent = parent;
                    }
                }

                if (_maskPoints is not null && _maskPoints.Count > 0)
                {
                    using var vec = new VectorOfVectorOfPoint(_maskPoints.ToArray());
                    CvInvoke.DrawContours(LayerCache.ImageBgr, vec, -1,
                        new MCvScalar(Settings.LayerPreview.MaskOutlineColor.B,
                            Settings.LayerPreview.MaskOutlineColor.G,
                            Settings.LayerPreview.MaskOutlineColor.R),
                        Settings.LayerPreview.MaskOutlineLineThickness);
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

                        LayerCache.ImageBgr.DrawPolygon((byte)operationDrawing.BrushShape, operationDrawing.BrushSize / 2, operationDrawing.Location,
                            new MCvScalar(color.B, color.G, color.R), operationDrawing.RotationAngle, operationDrawing.Thickness, operationDrawing.LineType);
                        /*switch (operationDrawing.BrushShape)
                        {
                            case PixelDrawing.BrushShapeType.Square:
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
                        }*/
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



                        /*CvInvoke.PutText(LayerCache.ImageBgr, operationText.Text, operationText.Location,
                            operationText.Font, operationText.FontScale, new MCvScalar(color.B, color.G, color.R),
                            operationText.Thickness, operationText.LineType, operationText.Mirror);*/
                        LayerCache.ImageBgr.PutTextRotated(operationText.Text, operationText.Location,
                        operationText.Font, operationText.FontScale, new MCvScalar(color.B, color.G, color.R),
                        operationText.Thickness, operationText.LineType, operationText.Mirror, operationText.LineAlignment, operationText.Angle);
                    }
                    else if (operation.OperationType == PixelOperation.PixelOperationType.Eraser)
                    {
                        //var pixelBrightness = LayerCache.Image.GetPixelPos(operation.Location);
                        if (imageSpan[LayerCache.Image.GetPixelPos(operation.Location)] < 10) continue;
                        var color = DrawingsGrid.SelectedItems.Contains(operation)
                            ? Settings.PixelEditor.RemovePixelHighlightColor
                            : Settings.PixelEditor.RemovePixelColor;

                        using var vec = EmguContours.GetContoursInside(LayerCache.LayerContours, LayerCache.LayerContourHierarchy, operation.Location);
                        if (vec.Size > 0) CvInvoke.DrawContours(LayerCache.ImageBgr, vec, -1, new MCvScalar(color.B, color.G, color.R), -1);

                        /*var hollowGroups = EmguContours.GetPositiveContoursInGroups(LayerCache.LayerContours, LayerCache.LayerContourHierarchy);

                        foreach (var vec in hollowGroups)
                        {
                            CvInvoke.PutText(LayerCache.ImageBgr, vec.Size.ToString(), vec[0][0], FontFace.HersheyDuplex, 2, new MCvScalar(255, 0, 0), 2);
                            CvInvoke.DrawContours(LayerCache.ImageBgr, vec, -1,
                                new MCvScalar(color.B, color.G, color.R), -1);
                            vec.Dispose();
                        }*/
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
                        if (issue.LayerIndex != ActualLayer || issue.Type is LayerIssue.IssueType.EmptyLayer or LayerIssue.IssueType.TouchingBound)
                            continue;

                        DrawCrosshair(issue.BoundingRectangle);
                    }
                }

                if (_showLayerImageFlipped && (_showLayerImageFlippedHorizontally || _showLayerImageFlippedVertically))
                {
                    var flipType = FlipType.Both;

                    if (_showLayerImageFlippedHorizontally && _showLayerImageFlippedVertically)
                        flipType = FlipType.Both;
                    else if (_showLayerImageFlippedHorizontally)
                        flipType = FlipType.Horizontal;
                    else if (_showLayerImageFlippedVertically)
                        flipType = FlipType.Vertical;

                    CvInvoke.Flip(LayerCache.ImageBgr, LayerCache.ImageBgr, flipType);
                }

                if (_showLayerImageRotated)
                {
                    CvInvoke.Rotate(LayerCache.ImageBgr, LayerCache.ImageBgr, _showLayerImageRotateCcwDirection ? RotateFlags.Rotate90CounterClockwise : RotateFlags.Rotate90Clockwise);
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

        public Point GetTransposedPoint(Point point, bool inverse = false)
        {
            if (point.IsEmpty) return point;

            void Flip()
            {
                if (!_showLayerImageFlipped) return;
                if (_showLayerImageFlippedHorizontally)
                {
                    point = new Point(LayerCache.Image.Width - 1 - point.X, point.Y);
                }

                if (_showLayerImageFlippedVertically)
                {
                    point = new Point(point.X, LayerCache.Image.Height - 1 - point.Y);
                }
            }

            void Rotate()
            {
                if (!_showLayerImageRotated) return;
                if (_showLayerImageRotateCcwDirection)
                {
                    point = inverse
                        ? new Point(point.Y, LayerCache.Image.Width - 1 - point.X) // 90º CCW
                        : new Point(LayerCache.Image.Width - 1 - point.Y, point.X); // 90º CW

                }
                else
                {
                    point = inverse
                        ? new Point(LayerCache.Image.Height - 1 - point.Y, point.X) // 90º CW
                        : new Point(point.Y, LayerCache.Image.Height - 1 - point.X); // 90º CCW
                }
            }

            if (inverse)
            {
                Flip();
                Rotate();
            }
            else
            {
                Rotate();
                Flip();
            }

            return point;
        }

        public Rectangle GetTransposedRectangle(RectangleF rectangleF, bool inverse = true) =>
            GetTransposedRectangle(Rectangle.Round(rectangleF), inverse);

        public Rectangle GetTransposedRectangle(Rectangle rectangle, bool inverse = false)
        {
            if (rectangle.IsEmpty) return rectangle;

            void Flip()
            {
                if (!_showLayerImageFlipped) return;
                if (_showLayerImageFlippedHorizontally)
                {
                    rectangle.Location = new Point(LayerCache.Image.Width - rectangle.Right, rectangle.Y);
                }

                if (_showLayerImageFlippedVertically)
                {
                    rectangle.Location = new Point(rectangle.X, LayerCache.Image.Height - 1 - rectangle.Bottom);
                }
            }

            void Rotate()
            {
                if (!_showLayerImageRotated) return;
                if (_showLayerImageRotateCcwDirection)
                {
                    rectangle = !inverse
                        ? new Rectangle(rectangle.Y, LayerCache.Image.Width - rectangle.Right, rectangle.Height, rectangle.Width) // 90º CCW
                        : new Rectangle(LayerCache.Image.Width - rectangle.Bottom, rectangle.X, rectangle.Height, rectangle.Width); // 90º CW

                }
                else
                {
                    rectangle = !inverse
                        ? new Rectangle(LayerCache.Image.Height - rectangle.Bottom, rectangle.X, rectangle.Height, rectangle.Width) // 90º CW
                        : new Rectangle(rectangle.Y, LayerCache.Image.Height - rectangle.Right, rectangle.Height, rectangle.Width); // 90º CCW
                }
            }

            if (!inverse)
            {
                Flip();
                Rotate();
            }
            else
            {
                Rotate();
                Flip();
            }

            return rectangle;

            /*return inverse
                ? new Rectangle(LayerCache.Image.Height - rectangle.Bottom,
                    rectangle.Left, rectangle.Height, rectangle.Width)
                //: new Rectangle(ActualLayerImage.Width - rectangle.Bottom, rectangle.Left, rectangle.Width, rectangle.Height);
                //: new Rectangle(ActualLayerImage.Width - rectangle.Bottom, ActualLayerImage.Height-rectangle.Right, rectangle.Width, rectangle.Height); // Rotate90FlipX: // = Rotate270FlipY
                //: new Rectangle(rectangle.Top, rectangle.Left, rectangle.Width, rectangle.Height); // Rotate270FlipX:  // = Rotate90FlipY
                : new Rectangle(rectangle.Top, LayerCache.Image.Height - rectangle.Right, rectangle.Height, rectangle.Width); // Rotate90FlipNone:  // = Rotate270FlipXY*/
        }

        /// <summary>
        /// Gets the bounding rectangle of the passed issue, automatically adjusting
        /// the coordinates and width/height to account for whether or not the layer
        /// preview image is rotated.  Used to ensure images are properly zoomed or
        /// centered independent of the layer preview rotation.
        /// </summary>
        private Rectangle GetTransposedIssueBounds(LayerIssue issue)
        {
            if (issue.X >= 0 && issue.Y >= 0 && (issue.BoundingRectangle.IsEmpty || issue.PixelsCount == 1))
                return new Rectangle(GetTransposedPoint(issue.FirstPoint, true), new Size(1, 1));
            //return new Rectangle(LayerCache.Image.Height - 1 - issue.Y, issue.X, 1, 1);

            return GetTransposedRectangle(issue.BoundingRectangle);
        }

        public void CenterLayer(int zoomLevel = 0)
        {
            if (zoomLevel < 0) zoomLevel = AppSettings.LockedZoomLevel;
            if (zoomLevel > 0) LayerImageBox.Zoom = zoomLevel;
            LayerImageBox.CenterToImage();
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
            if (issue.Type is LayerIssue.IssueType.TouchingBound or LayerIssue.IssueType.EmptyLayer || (issue.X == -1 && issue.Y == -1))
            {
                ZoomToFit();
            }

            if (issue.X >= 0 && issue.Y >= 0)
            {
                CenterLayerAt(GetTransposedIssueBounds(issue));
            }
        }

        public void ZoomToNormal()
        {
            LayerImageBox.Zoom = (_globalModifiers & KeyModifiers.Shift) != 0 ? AppSettings.LockedZoomLevel : 100;
            LayerImageBox.CenterToImage();
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
                        /*if (!_showLayerImageRotated)
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
                        }*/
                        LayerImageBox.ZoomToRegion(GetTransposedRectangle(SlicerFile.BoundingRectangle), margin);
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
                    LayerImageBox.ZoomToRegion(GetTransposedRectangle(SlicerFile.BoundingRectangle), margin);
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

                    if ((e.KeyModifiers & KeyModifiers.Alt) != 0)
                    {
                        SelectObjectMask(location);
                        return;
                    }
                    
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
                if (e.KeyModifiers == KeyModifiers.Shift)
                {
                    ClearROI();
                }
                /*else if(e.KeyModifiers == KeyModifiers.Alt)
                {
                    ClearMask();
                }*/
                else
                {
                    ClearROIAndMask();
                }
                e.Handled = true;
                return;
            }

            if ((e.KeyModifiers & KeyModifiers.Control) != 0)
            {
                if (e.Key is Key.LeftShift or Key.RightShift || (e.KeyModifiers & KeyModifiers.Shift) != 0) // Ctrl + Shift
                {
                    if (e.Key == Key.R)
                    {
                        ShowLayerImageRotated = true;
                        if (_showLayerImageRotateCwDirection)
                        {
                            ShowLayerImageRotateCWDirection = false;
                            ShowLayerImageRotateCCWDirection = true;
                        }
                        else
                        {
                            ShowLayerImageRotateCCWDirection = false;
                            ShowLayerImageRotateCWDirection = true;
                        }
                        e.Handled = true;
                        return;
                    }

                    if (e.Key == Key.F)
                    {
                        ShowLayerImageFlipped = true;
                        if (!_showLayerImageFlippedHorizontally && !_showLayerImageFlippedVertically)
                        {
                            ShowLayerImageFlippedHorizontally = true;
                        }
                        else if (_showLayerImageFlippedHorizontally && !_showLayerImageFlippedVertically)
                        {
                            ShowLayerImageFlippedHorizontally = false;
                            ShowLayerImageFlippedVertically = true;
                        }
                        else if (!_showLayerImageFlippedHorizontally && _showLayerImageFlippedVertically)
                        {
                            ShowLayerImageFlippedHorizontally = true;
                        }
                        else if (_showLayerImageFlippedHorizontally && _showLayerImageFlippedVertically)
                        {
                            ShowLayerImageFlippedVertically = false;
                        }

                        e.Handled = true;
                        return;
                    }

                    if (e.Key == Key.B)
                    {
                        SelectModelVolumeRoi();
                        return;
                    }

                }

                if (e.Key is Key.D0 or Key.NumPad0)
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

                if (e.Key == Key.F)
                {
                    ShowLayerImageFlipped = !_showLayerImageFlipped;
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
                    LayerPixelPicker.Set(realLocation, brightness, SlicerFile.PixelToDisplayPosition(realLocation, 2));
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

            for (int i = LayerCache.LayerContours.Size-1; i >= 0; i--)
            {
                if (CvInvoke.PointPolygonTest(LayerCache.LayerContours[i], point, false) < 0) continue;
                var rectangle = CvInvoke.BoundingRectangle(LayerCache.LayerContours[i]);
                ROI = rectangle;
                return true;
            }

            return false;
        }

        public uint SelectObjectRoi(Rectangle roiRectangle)
        {
            if (roiRectangle.IsEmpty) return 0;
            List<Rectangle> rectangles = new();
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

            ROI = roiRectangle;

            return (uint)rectangles.Count;
        }

        public bool SelectObjectMask(Point location)
        {
            var point = GetTransposedPoint(location);

            using var vec = EmguContours.GetContoursInside(LayerCache.LayerContours, LayerCache.LayerContourHierarchy, point, (_globalModifiers & KeyModifiers.Control) != 0);
            AddMaskPoints(vec.ToArrayOfArray(), false);
            return vec.Size > 0;
        }

        public void OnLayerPixelPickerClicked()
        {
            if (!LayerPixelPicker.IsSet) return;
            CenterLayerAt(GetTransposedPoint(LayerPixelPicker.Location, true), -1);
        }

        public async void SaveCurrentLayerImage()
        {
            if (!IsFileLoaded) return;
            SaveFileDialog dialog = new()
            {
                Filters = Helpers.PngFileFilter,
                DefaultExtension = ".png",
                InitialFileName = $"{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}_layer{ActualLayer}.png"
            };

            var result = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(result)) return;

            LayerCache.ImageBgr.Save(result);
        }

        public async void SaveCurrentROIImage()
        {
            if (!IsFileLoaded || !LayerImageBox.HaveSelection) return;
            SaveFileDialog dialog = new()
            {
                Filters = Helpers.PngFileFilter,
                DefaultExtension = ".png",
                InitialFileName = $"{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}_layer{ActualLayer}_ROI.png"
            };

            var result = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(result)) return;

            LayerImageBox.GetSelectedBitmap()?.Save(result);
        }

        const byte _pixelEditorCursorMinDiamater = 10;
        public void UpdatePixelEditorCursor()
        {
            Mat cursor = null;
            MCvScalar _pixelEditorCursorColor = new(
                Settings.PixelEditor.CursorColor.B, 
                Settings.PixelEditor.CursorColor.G,
                Settings.PixelEditor.CursorColor.R,
                Settings.PixelEditor.CursorColor.A);
            switch ((PixelOperation.PixelOperationType)SelectedPixelOperationTabIndex)
            {
                case PixelOperation.PixelOperationType.Drawing:
                    
                    if (DrawingPixelDrawing.BrushSize > 1)
                    {
                        if ((byte)DrawingPixelDrawing.BrushShape >= 1)
                        {
                            int cursorSize = DrawingPixelDrawing.BrushSize;
                            if (DrawingPixelDrawing.Thickness > 1)
                            {
                                cursorSize += DrawingPixelDrawing.Thickness;
                            }
                            cursor = EmguExtensions.InitMat(new Size(cursorSize, cursorSize), 4);

                            cursor.DrawPolygon((byte) DrawingPixelDrawing.BrushShape, DrawingPixelDrawing.BrushSize / 2, cursor.Size.ToPoint().Half(), 
                                _pixelEditorCursorColor, DrawingPixelDrawing.RotationAngle, DrawingPixelDrawing.Thickness, DrawingPixelDrawing.LineType);

                            if (DrawingPixelDrawing.BrushShape != PixelDrawing.BrushShapeType.Circle)
                            {
                                if (_showLayerImageFlipped && (_showLayerImageFlippedHorizontally || _showLayerImageFlippedVertically))
                                {
                                    var flipType = FlipType.Both;

                                    if (_showLayerImageFlippedHorizontally && _showLayerImageFlippedVertically)
                                        flipType = FlipType.Both;
                                    else if (_showLayerImageFlippedHorizontally)
                                        flipType = FlipType.Horizontal;
                                    else if (_showLayerImageFlippedVertically)
                                        flipType = FlipType.Vertical;

                                    CvInvoke.Flip(cursor, cursor, flipType);
                                }

                                if (_showLayerImageRotated)
                                {
                                    CvInvoke.Rotate(cursor, cursor,
                                        _showLayerImageRotateCcwDirection
                                            ? RotateFlags.Rotate90CounterClockwise
                                            : RotateFlags.Rotate90Clockwise);
                                }
                            }
                        }

                        /*switch (DrawingPixelDrawing.BrushShape)
                        {
                            case PixelDrawing.BrushShapeType.Square:
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
                        }*/
                    }
                    break;
                case PixelOperation.PixelOperationType.Text:
                    var text = DrawingPixelText.Text;
                    if (string.IsNullOrEmpty(text) || DrawingPixelText.FontScale < 0.2) return;

                    int baseLine = 0;
                    //var size = CvInvoke.GetTextSize(text, DrawingPixelText.Font, DrawingPixelText.FontScale, DrawingPixelText.Thickness, ref baseLine);
                    var size = EmguExtensions.GetTextSizeExtended(text, DrawingPixelText.Font, DrawingPixelText.FontScale, DrawingPixelText.Thickness, ref baseLine, DrawingPixelText.LineAlignment);
                    //var rotatedSize = size.Rotate(DrawingPixelText.Angle);
                    //Point point = (rotatedSize.Inflate(rotatedSize)).Rotate(DrawingPixelText.Angle, rotatedSize.ToPoint());
                    cursor = EmguExtensions.InitMat(size.Inflate(), 4);
                    //CvInvoke.Rectangle(cursor, new Rectangle(Point.Empty, size), _pixelEditorCursorColor, -1, DrawingPixelText.LineType);
                    //_pixelEditorCursorColor.V3 = 255;
                    //CvInvoke.Rectangle(cursor, new Rectangle(new Point(size.Width, 0), size), _pixelEditorCursorColor, 1, DrawingPixelText.LineType);

                    cursor.PutTextExtended(text, size.ToPoint(), DrawingPixelText.Font, DrawingPixelText.FontScale, _pixelEditorCursorColor, DrawingPixelText.Thickness, DrawingPixelText.LineType, DrawingPixelText.Mirror, DrawingPixelText.LineAlignment);
                    //CvInvoke.PutText(cursor, text, size.ToPoint(), DrawingPixelText.Font, DrawingPixelText.FontScale, _pixelEditorCursorColor, DrawingPixelText.Thickness, DrawingPixelText.LineType, DrawingPixelText.Mirror);
                    cursor.RotateAdjustBounds(DrawingPixelText.Angle);
                    //cursor.Rotate(DrawingPixelText.Angle);
                    //cursor.PutTextRotated(text, cursor.Size.ToPoint().Half(), DrawingPixelText.Font, DrawingPixelText.FontScale, _pixelEditorCursorColor, DrawingPixelText.Thickness, DrawingPixelText.LineType, DrawingPixelText.Mirror, DrawingPixelText.Angle);
                    if (_showLayerImageFlipped && (_showLayerImageFlippedHorizontally || _showLayerImageFlippedVertically))
                    {
                        var flipType = FlipType.Both;

                        if (_showLayerImageFlippedHorizontally && _showLayerImageFlippedVertically)
                            flipType = FlipType.Both;
                        else if (_showLayerImageFlippedHorizontally)
                            flipType = FlipType.Horizontal;
                        else if (_showLayerImageFlippedVertically)
                            flipType = FlipType.Vertical;

                        CvInvoke.Flip(cursor, cursor, flipType);
                    }

                    if (_showLayerImageRotated)
                    {
                        CvInvoke.Rotate(cursor, cursor, _showLayerImageRotateCcwDirection ? RotateFlags.Rotate90CounterClockwise : RotateFlags.Rotate90Clockwise);
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

            if (cursor is not null)
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
