/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2020-2026 PTRTECH
 */

using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Reactive;
using UVtools.AvaloniaControls;

namespace UVtools.UI.Controls;

public class LayerMeasure2DOverlayControl : Control
{
    public static readonly StyledProperty<AdvancedImageBox?> ImageBoxProperty =
        AvaloniaProperty.Register<LayerMeasure2DOverlayControl, AdvancedImageBox?>(nameof(ImageBox));

    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<LayerMeasure2DOverlayControl, bool>(nameof(IsActive));

    public static readonly StyledProperty<Point?> StartPointProperty =
        AvaloniaProperty.Register<LayerMeasure2DOverlayControl, Point?>(nameof(StartPoint));

    public static readonly StyledProperty<Point?> EndPointProperty =
        AvaloniaProperty.Register<LayerMeasure2DOverlayControl, Point?>(nameof(EndPoint));

    public static readonly StyledProperty<Point?> CursorPointProperty =
        AvaloniaProperty.Register<LayerMeasure2DOverlayControl, Point?>(nameof(CursorPoint));

    public static readonly StyledProperty<double> PixelPitchXProperty =
        AvaloniaProperty.Register<LayerMeasure2DOverlayControl, double>(nameof(PixelPitchX), 0.05);

    public static readonly StyledProperty<double> PixelPitchYProperty =
        AvaloniaProperty.Register<LayerMeasure2DOverlayControl, double>(nameof(PixelPitchY), 0.05);

    public AdvancedImageBox? ImageBox
    {
        get => GetValue(ImageBoxProperty);
        set => SetValue(ImageBoxProperty, value);
    }

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public Point? StartPoint
    {
        get => GetValue(StartPointProperty);
        set => SetValue(StartPointProperty, value);
    }

    public Point? EndPoint
    {
        get => GetValue(EndPointProperty);
        set => SetValue(EndPointProperty, value);
    }

    public Point? CursorPoint
    {
        get => GetValue(CursorPointProperty);
        set => SetValue(CursorPointProperty, value);
    }

    public double PixelPitchX
    {
        get => GetValue(PixelPitchXProperty);
        set => SetValue(PixelPitchXProperty, value);
    }

    public double PixelPitchY
    {
        get => GetValue(PixelPitchYProperty);
        set => SetValue(PixelPitchYProperty, value);
    }

    private IDisposable? _zoomSub;

    static LayerMeasure2DOverlayControl()
    {
        AffectsRender<LayerMeasure2DOverlayControl>(
            IsActiveProperty,
            StartPointProperty,
            EndPointProperty,
            CursorPointProperty,
            PixelPitchXProperty,
            PixelPitchYProperty);

        ImageBoxProperty.Changed.AddClassHandler<LayerMeasure2DOverlayControl>((c, e) =>
        {
            c.OnImageBoxChanged(e.OldValue as AdvancedImageBox, e.NewValue as AdvancedImageBox);
        });
    }

    public LayerMeasure2DOverlayControl()
    {
        IsHitTestVisible = false;
    }

    private void OnImageBoxChanged(AdvancedImageBox? oldBox, AdvancedImageBox? newBox)
    {
        _zoomSub?.Dispose();
        _zoomSub = null;
        if (oldBox != null)
        {
            oldBox.LayoutUpdated -= OnImageBoxOnLayoutUpdated;
            oldBox.PropertyChanged -= OnImageBoxPropertyChanged;
        }

        if (newBox != null)
        {
            _zoomSub = newBox.GetObservable(AdvancedImageBox.ZoomProperty).Subscribe(new AnonymousObserver<int>(_ => InvalidateVisual()));
            newBox.LayoutUpdated += OnImageBoxOnLayoutUpdated;
            newBox.PropertyChanged += OnImageBoxPropertyChanged;
        }
    }

    private void OnImageBoxPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AdvancedImageBox.Offset) or nameof(AdvancedImageBox.Zoom) or nameof(AdvancedImageBox.PointerPosition))
        {
            InvalidateVisual();
        }
    }

    private void OnImageBoxOnLayoutUpdated(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (!IsActive) return;
        var startImgPt = StartPoint;
        if (!startImgPt.HasValue) return;

        var box = ImageBox;
        if (box == null || box.Image == null) return;

        var endImgPt = EndPoint ?? CursorPoint;

        // Convert image points to screen points
        Point rawP1 = box.GetOffsetPoint(startImgPt.Value);
        Point p1 = box.TranslatePoint(rawP1, this) ?? rawP1;

        // Styling brushes & pens
        var shadowPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)), 3.5);
        var linePen = new Pen(new SolidColorBrush(Color.FromRgb(0, 210, 255)), 2.0);
        var previewLinePen = new Pen(new SolidColorBrush(Color.FromArgb(200, 0, 210, 255)), 1.8, DashStyle.Dash);
        var tickPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 215, 0)), 2.0);
        var deltaPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 200, 200, 200)), 1.0, DashStyle.Dash);
        var pt1Brush = new SolidColorBrush(Color.FromRgb(0, 255, 128));
        var pt2Brush = new SolidColorBrush(Color.FromRgb(255, 215, 0));
        var markerBorderPen = new Pen(Brushes.White, 1.2);

        if (endImgPt.HasValue)
        {
            Point rawP2 = box.GetOffsetPoint(endImgPt.Value);
            Point p2 = box.TranslatePoint(rawP2, this) ?? rawP2;
            bool isPreview = !EndPoint.HasValue;

            // 1. Right-angle delta triangle (horizontal & vertical lines)
            Point corner = new Point(p2.X, p1.Y);
            if (Math.Abs(p2.X - p1.X) > 2 && Math.Abs(p2.Y - p1.Y) > 2)
            {
                context.DrawLine(deltaPen, p1, corner);
                context.DrawLine(deltaPen, corner, p2);
            }

            // 2. Main Connecting Line
            var activePen = isPreview ? previewLinePen : linePen;
            context.DrawLine(shadowPen, p1, p2);
            context.DrawLine(activePen, p1, p2);

            // 3. Perpendicular end ticks
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len > 4.0)
            {
                double nx = -dy / len * 8.0;
                double ny = dx / len * 8.0;
                context.DrawLine(tickPen, new Point(p1.X - nx, p1.Y - ny), new Point(p1.X + nx, p1.Y + ny));
                context.DrawLine(tickPen, new Point(p2.X - nx, p2.Y - ny), new Point(p2.X + nx, p2.Y + ny));
            }

            // 4. Point 2 Marker
            context.DrawEllipse(pt2Brush, markerBorderPen, p2, 4.5, 4.5);

            // 5. Dimension Badge at Line Midpoint
            double imgDx = Math.Abs(endImgPt.Value.X - startImgPt.Value.X);
            double imgDy = Math.Abs(endImgPt.Value.Y - startImgPt.Value.Y);
            double pitchX = PixelPitchX > 0 ? PixelPitchX : 0.05;
            double pitchY = PixelPitchY > 0 ? PixelPitchY : 0.05;
            double dxMm = imgDx * pitchX;
            double dyMm = imgDy * pitchY;
            double distMm = Math.Sqrt(dxMm * dxMm + dyMm * dyMm);
            double distPx = Math.Sqrt(imgDx * imgDx + imgDy * imgDy);

            string badgeText = $"{distMm:F2} mm ({distPx:F0} px)";
            var formatted = new FormattedText(
                badgeText,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold),
                11.5,
                Brushes.White);

            Point mid = new Point((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5);
            double badgePadX = 6.0;
            double badgePadY = 3.0;
            double badgeW = formatted.Width + badgePadX * 2.0;
            double badgeH = formatted.Height + badgePadY * 2.0;
            Rect badgeRect = new Rect(mid.X - badgeW * 0.5, mid.Y - badgeH - 8.0, badgeW, badgeH);

            var badgeBg = new SolidColorBrush(Color.FromArgb(220, 16, 22, 34));
            var badgePen = new Pen(new SolidColorBrush(Color.FromArgb(180, 0, 210, 255)), 1.2);
            context.DrawRectangle(badgeBg, badgePen, badgeRect, 4, 4);
            context.DrawText(formatted, new Point(badgeRect.X + badgePadX, badgeRect.Y + badgePadY));
        }

        // Point 1 Marker
        context.DrawEllipse(pt1Brush, markerBorderPen, p1, 4.5, 4.5);
    }
}
