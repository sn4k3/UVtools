/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using EmguExtensions;
using UVtools.AvaloniaControls;
using UVtools.Core.Layers;
using ZLinq;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaRect = Avalonia.Rect;
using Color = UVtools.UI.Structures.Color;

namespace UVtools.UI;

/// <summary>
/// The vector overlays of the layer preview, drawn by the layer image box over the layer image. They are in layer
/// pixels, so the layer image box transforms them like the image (zoom, rotation, flips and real aspect ratio), and
/// they are rebuilt without rendering the layer image again.
/// </summary>
public partial class MainWindow
{
    /// <summary>
    /// The outline thickness and the diameter settings are in layer pixels, which scale with the zoom, but the
    /// overlays have a size in screen pixels that is the same at any zoom. The settings are converted using the
    /// scale of 50% zoom, where large layers are usually viewed, so a bound of the default thickness of 3 is 1.5
    /// screen pixels thick, like it was drawn at 50% zoom, and it is no longer 0.6 screen pixels at 20% zoom or 12
    /// screen pixels at 400% zoom.
    /// </summary>
    private const double LayerOverlaySettingScale = 0.5;

    /// <summary>
    /// The thickness of the crosshair lines in screen pixels, it replaces the thickness in layer pixels that was
    /// increased from 1 to 3 at lower zoom levels to keep the lines visible.
    /// </summary>
    private const double LayerOverlayCrosshairThickness = 2;

    private readonly List<ImageBoxOverlay> _layerOverlayBuffer = [];
    private readonly List<ImageBoxLineOverlay> _layerCrosshairOverlays = [];

    /// <summary>
    /// The triangles found by the last render of the layer image with the triangulate outline, it is the text of the
    /// overlay.
    /// </summary>
    private uint _layerTriangleCount;

    private static double ToLayerOverlayThickness(double settingValue)
    {
        return Math.Max(1, settingValue * LayerOverlaySettingScale);
    }

    private static IBrush ToLayerOverlayBrush(Color color)
    {
        return new ImmutableSolidColorBrush(color.ToAvalonia());
    }

    /// <summary>
    /// Removes the overlays of the layer image box.
    /// </summary>
    private void ClearLayerOverlays()
    {
        _layerCrosshairOverlays.Clear();
        LayerImageBox.Overlays.ReplaceAll([]);
    }

    /// <summary>
    /// Rebuilds the overlays of the layer image box from the current layer and the preview settings, the layer image
    /// itself is not rendered again.
    /// </summary>
    public void RefreshLayerOverlays()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(RefreshLayerOverlays);
            return;
        }

        if (LayerImageBox is null) return; // The window is still initializing

        _layerCrosshairOverlays.Clear();
        _layerOverlayBuffer.Clear();

        if (IsFileLoaded && LayerCache is { IsCached: true, Image: not null })
        {
            AddLayerBoundsOverlays(_layerOverlayBuffer);
            AddContourBoundsOverlays(_layerOverlayBuffer);
            AddEnclosingCirclesOverlays(_layerOverlayBuffer);
            AddCentroidOverlays(_layerOverlayBuffer);
            AddCrosshairOverlays(_layerOverlayBuffer);
        }
        else
        {
            _layerTriangleCount = 0;
        }

        UpdateTriangleCountText();

        LayerImageBox.Overlays.ReplaceAll(_layerOverlayBuffer);
        _layerOverlayBuffer.Clear(); // Don't keep the overlays alive from here
    }

    /// <summary>
    /// Shows or hides the crosshairs when the zoom crosses the crosshair fade level, they only exist while zoomed out.
    /// </summary>
    private void UpdateLayerCrosshairVisibility()
    {
        if (_layerCrosshairOverlays.Count == 0) return;

        var isVisible = LayerImageBox.Zoom <= AppSettings.CrosshairFadeLevel;
        foreach (var crosshair in _layerCrosshairOverlays)
        {
            crosshair.IsVisible = isVisible;
        }
    }

    private void AddLayerBoundsOverlays(List<ImageBoxOverlay> overlays)
    {
        var settings = Settings.LayerPreview;

        if (_showLayerOutlinePrintVolumeBoundary && !SlicerFile!.BoundingRectangle.IsEmpty)
        {
            overlays.Add(new ImageBoxRectangleOverlay(ToAvaloniaRect(SlicerFile.BoundingRectangle))
            {
                Stroke = ToLayerOverlayBrush(settings.VolumeBoundsOutlineColor),
                StrokeThickness = ToLayerOverlayThickness(settings.VolumeBoundsOutlineThickness)
            });
        }

        if (_showLayerOutlineLayerBoundary && !LayerCache.Layer!.BoundingRectangle.IsEmpty)
        {
            overlays.Add(new ImageBoxRectangleOverlay(ToAvaloniaRect(LayerCache.Layer.BoundingRectangle))
            {
                Stroke = ToLayerOverlayBrush(settings.LayerBoundsOutlineColor),
                StrokeThickness = ToLayerOverlayThickness(settings.LayerBoundsOutlineThickness)
            });
        }
    }

    private void AddContourBoundsOverlays(List<ImageBoxOverlay> overlays)
    {
        if (!_showLayerOutlineContourBoundary) return;

        var contours = LayerCache.Layer!.Contours;
        var brush = ToLayerOverlayBrush(Settings.LayerPreview.ContourBoundsOutlineColor);
        var thickness = ToLayerOverlayThickness(Settings.LayerPreview.ContourBoundsOutlineThickness);

        var lastParent = -1;
        uint reps = 0;
        for (var i = 0; i < contours.Count; i++)
        {
            var parent = contours[i, EmguContour.HierarchyParent];
            if (parent == -1)
            {
                reps = 0;
            }

            if (parent == -1 || parent != lastParent) reps++;
            if (reps % 2 == 0)
            {
                lastParent = parent;
                continue;
            }

            overlays.Add(new ImageBoxRectangleOverlay(ToAvaloniaRect(contours[i].BoundingRectangle))
            {
                Stroke = brush,
                StrokeThickness = thickness
            });

            lastParent = parent;
        }
    }

    private void AddEnclosingCirclesOverlays(List<ImageBoxOverlay> overlays)
    {
        if (!_showLayerOutlineEnclosingCircles) return;

        var contours = LayerCache.Layer!.Contours;
        var brush = ToLayerOverlayBrush(Settings.LayerPreview.EnclosingCirclesOutlineColor);
        var thickness = ToLayerOverlayThickness(Settings.LayerPreview.EnclosingCirclesOutlineThickness);

        for (var i = 0; i < contours.Count; i++)
        {
            var circle = contours[i].MinEnclosingCircle;

            // The circle was drawn by pixel with an integer center and radius, +0.5 is the center of the pixel
            overlays.Add(new ImageBoxEllipseOverlay(
                new AvaloniaPoint((int)circle.Center.X + 0.5, (int)circle.Center.Y + 0.5),
                Math.Round(circle.Radius))
            {
                Stroke = brush,
                StrokeThickness = thickness
            });
        }
    }

    private void AddCentroidOverlays(List<ImageBoxOverlay> overlays)
    {
        if (!_showLayerOutlineCentroids) return;

        var settings = Settings.LayerPreview;
        var contours = LayerCache.Layer!.Contours;
        var brush = ToLayerOverlayBrush(settings.CentroidOutlineColor);
        var hollowBrush = ToLayerOverlayBrush(settings.HollowOutlineColor);
        var diameter = Math.Max(3, settings.CentroidOutlineDiameter * LayerOverlaySettingScale);

        var lastParent = -1;
        uint reps = 0;
        for (var i = 0; i < contours.Count; i++)
        {
            var parent = contours[i, EmguContour.HierarchyParent];
            if (parent == -1)
            {
                reps = 0;
            }

            if (parent == -1 || parent != lastParent) reps++;
            if (reps % 2 == 0)
            {
                if (settings.CentroidOutlineHollow)
                {
                    var centroid = contours[i].Centroid;
                    overlays.Add(new ImageBoxMarkerOverlay(new AvaloniaPoint(centroid.X + 0.5, centroid.Y + 0.5),
                        ImageBoxMarkerStyle.Circle, diameter)
                    {
                        Stroke = hollowBrush,
                        StrokeThickness = 1
                    });
                }
            }
            else
            {
                var centroid = contours[i].Centroid;
                overlays.Add(new ImageBoxMarkerOverlay(new AvaloniaPoint(centroid.X + 0.5, centroid.Y + 0.5),
                    ImageBoxMarkerStyle.Circle, diameter)
                {
                    Fill = brush
                });
            }

            lastParent = parent;
        }
    }

    /// <summary>
    /// Gets the number of triangles of the triangulate outline of the layer, or <see langword="null"/> when it is not
    /// shown. It is displayed fixed over the layer preview, not on the layer, so it stays visible at any zoom, scroll,
    /// rotation and flip.
    /// </summary>
    public string? LayerTriangleCountText { get; private set; }

    private void UpdateTriangleCountText()
    {
        var text = _showLayerOutlineTriangulate && _layerTriangleCount > 0 &&
                   Settings.LayerPreview.TriangulateOutlineShowCount
            ? $"Triangles: {_layerTriangleCount:N0}"
            : null;
        if (text == LayerTriangleCountText) return;
        LayerTriangleCountText = text;
        RaisePropertyChanged(nameof(LayerTriangleCountText));
    }

    private void AddCrosshairOverlays(List<ImageBoxOverlay> overlays)
    {
        if (!_showLayerImageCrosshairs || SlicerFile!.IssueManager.Count == 0) return;

        var settings = Settings.LayerPreview;
        var brush = ToLayerOverlayBrush(settings.CrosshairColor);

        if (!settings.CrosshairShowOnlyOnSelectedIssues)
        {
            var selectedIssues = IssuesGrid.SelectedItems;
            foreach (var issue in SlicerFile.IssueManager.GetIssuesBy(_actualLayer).AsValueEnumerable()
                         .Where(issue => issue.Parent!.Type
                             is not MainIssue.IssueType.PrintHeight
                             and not MainIssue.IssueType.EmptyLayer))
            {
                var isSelected = selectedIssues.Count > 0 && selectedIssues.Contains(issue.Parent);
                var color = issue.Parent!.Type switch
                {
                    MainIssue.IssueType.Island => isSelected ? settings.IslandHighlightColor : settings.IslandColor,
                    MainIssue.IssueType.Overhang => isSelected
                        ? settings.OverhangHighlightColor
                        : settings.OverhangColor,
                    MainIssue.IssueType.ResinTrap => isSelected
                        ? settings.ResinTrapHighlightColor
                        : settings.ResinTrapColor,
                    MainIssue.IssueType.SuctionCup => isSelected
                        ? settings.SuctionCupHighlightColor
                        : settings.SuctionCupColor,
                    _ => Color.Empty // The other issues have no crosshair
                };

                if (color.IsEmpty) continue;
                AddCrosshair(overlays, issue.BoundingRectangle, brush);
            }
        }
        else if (IssuesGrid.SelectedItems.Count > 0 && !_isPixelEditorActive)
        {
            // Don't draw crosshairs for issues that don't have a specific location or bounds.
            foreach (var issue in SlicerFile.IssueManager.GetIssuesBy(_actualLayer).AsValueEnumerable()
                         .Where(issue => issue.Parent!.Type
                             is not MainIssue.IssueType.TouchingBound
                             and not MainIssue.IssueType.PrintHeight
                             and not MainIssue.IssueType.EmptyLayer))
            {
                AddCrosshair(overlays, issue.BoundingRectangle, brush);
            }
        }

        UpdateLayerCrosshairVisibility();
    }

    /// <summary>
    /// Adds a crosshair around a rectangle of the layer, its lines are as long and as far from the rectangle as the
    /// crosshair settings say, in layer pixels. They are hidden when zoomed in more than the crosshair fade level, see
    /// <see cref="UpdateLayerCrosshairVisibility"/>.
    /// </summary>
    private void AddCrosshair(List<ImageBoxOverlay> overlays, Rectangle rect, IBrush brush)
    {
        var width = LayerCache.Image!.Width;
        var height = LayerCache.Image.Height;
        var length = (int)Settings.LayerPreview.CrosshairLength;
        var margin = Settings.LayerPreview.CrosshairMargin;

        // Integer pixel to the position of its center, the layer limits are kept on the edge of the layer
        static double Pixel(int value, int max)
        {
            return value <= 0 ? 0 : value >= max ? max : value + 0.5;
        }

        var centerX = rect.X + rect.Width / 2;
        var centerY = rect.Y + rect.Height / 2;

        var leftStart = Math.Max(0, rect.X - margin - 1);
        var leftEnd = length == 0 ? 0 : Math.Max(0, leftStart - length + 1);
        var rightStart = Math.Min(width, rect.Right + margin);
        var rightEnd = length == 0 ? width : Math.Min(width, rightStart + length - 1);
        var topStart = Math.Max(0, rect.Y - margin - 1);
        var topEnd = length == 0 ? 0 : Math.Max(0, topStart - length + 1);
        var bottomStart = Math.Min(height, rect.Bottom + margin);
        var bottomEnd = length == 0 ? height : Math.Min(height, bottomStart + length - 1);

        AddCrosshairLine(overlays, brush, Pixel(leftStart, width), Pixel(centerY, height), Pixel(leftEnd, width),
            Pixel(centerY, height));
        AddCrosshairLine(overlays, brush, Pixel(rightStart, width), Pixel(centerY, height), Pixel(rightEnd, width),
            Pixel(centerY, height));
        AddCrosshairLine(overlays, brush, Pixel(centerX, width), Pixel(topStart, height), Pixel(centerX, width),
            Pixel(topEnd, height));
        AddCrosshairLine(overlays, brush, Pixel(centerX, width), Pixel(bottomStart, height), Pixel(centerX, width),
            Pixel(bottomEnd, height));
    }

    private void AddCrosshairLine(List<ImageBoxOverlay> overlays, IBrush brush, double x1, double y1, double x2,
        double y2)
    {
        var line = new ImageBoxLineOverlay(new AvaloniaPoint(x1, y1), new AvaloniaPoint(x2, y2))
        {
            Stroke = brush,
            StrokeThickness = LayerOverlayCrosshairThickness
        };

        overlays.Add(line);
        _layerCrosshairOverlays.Add(line);
    }

    private static AvaloniaRect ToAvaloniaRect(Rectangle rectangle)
    {
        return new AvaloniaRect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }
}
