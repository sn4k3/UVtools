/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2020-2026 PTRTECH
 */

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace UVtools.UI.Controls;

public class LayerAreaPeelCurveControl : Control
{
    public static readonly StyledProperty<IReadOnlyList<float>?> LayerAreasProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, IReadOnlyList<float>?>(nameof(LayerAreas));

    public static readonly StyledProperty<int> CurrentLayerProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, int>(nameof(CurrentLayer));

    public static readonly StyledProperty<float> MaxAreaProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, float>(nameof(MaxArea));

    public static readonly StyledProperty<int> PeakLayerProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, int>(nameof(PeakLayer));

    public static readonly StyledProperty<IReadOnlyList<int>?> PeelSpikesProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, IReadOnlyList<int>?>(nameof(PeelSpikes));

    public static readonly StyledProperty<System.Windows.Input.ICommand?> LayerSelectedCommandProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, System.Windows.Input.ICommand?>(nameof(LayerSelectedCommand));

    public event Action<int>? LayerSelected;

    public System.Windows.Input.ICommand? LayerSelectedCommand
    {
        get => GetValue(LayerSelectedCommandProperty);
        set => SetValue(LayerSelectedCommandProperty, value);
    }

    private int? _hoveredLayer;
    private bool _isDragging;

    public IReadOnlyList<float>? LayerAreas
    {
        get => GetValue(LayerAreasProperty);
        set => SetValue(LayerAreasProperty, value);
    }

    public int CurrentLayer
    {
        get => GetValue(CurrentLayerProperty);
        set => SetValue(CurrentLayerProperty, value);
    }

    public float MaxArea
    {
        get => GetValue(MaxAreaProperty);
        set => SetValue(MaxAreaProperty, value);
    }

    public int PeakLayer
    {
        get => GetValue(PeakLayerProperty);
        set => SetValue(PeakLayerProperty, value);
    }

    public IReadOnlyList<int>? PeelSpikes
    {
        get => GetValue(PeelSpikesProperty);
        set => SetValue(PeelSpikesProperty, value);
    }

    static LayerAreaPeelCurveControl()
    {
        AffectsRender<LayerAreaPeelCurveControl>(
            LayerAreasProperty,
            CurrentLayerProperty,
            MaxAreaProperty,
            PeakLayerProperty,
            PeelSpikesProperty);
    }

    public LayerAreaPeelCurveControl()
    {
        ClipToBounds = true;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetPosition(this);
        UpdateHoverFromPoint(pt);

        if (_isDragging && _hoveredLayer.HasValue)
        {
            LayerSelected?.Invoke(_hoveredLayer.Value);
            if (LayerSelectedCommand?.CanExecute(_hoveredLayer.Value) == true)
            {
                LayerSelectedCommand.Execute(_hoveredLayer.Value);
            }
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            e.Pointer.Capture(this);
            var pt = e.GetPosition(this);
            UpdateHoverFromPoint(pt);
            if (_hoveredLayer.HasValue)
            {
                LayerSelected?.Invoke(_hoveredLayer.Value);
                if (LayerSelectedCommand?.CanExecute(_hoveredLayer.Value) == true)
                {
                    LayerSelectedCommand.Execute(_hoveredLayer.Value);
                }
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (!_isDragging)
        {
            _hoveredLayer = null;
            InvalidateVisual();
        }
    }

    private void UpdateHoverFromPoint(Point pt)
    {
        var areas = LayerAreas;
        if (areas is null || areas.Count == 0) return;

        const double padL = 10.0;
        double padR = Bounds.Width - 10.0;
        double plotW = Math.Max(1.0, padR - padL);

        double ratio = Math.Clamp((pt.X - padL) / plotW, 0.0, 1.0);
        int targetLayer = Math.Clamp((int)Math.Round(ratio * (areas.Count - 1)), 0, areas.Count - 1);
        if (_hoveredLayer != targetLayer)
        {
            _hoveredLayer = targetLayer;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width < 20 || bounds.Height < 20) return;

        // Background card
        var cardBrush = new SolidColorBrush(Color.FromArgb(235, 18, 22, 30));
        var borderPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 45, 60, 80)), 1.0);
        context.DrawRectangle(cardBrush, borderPen, new Rect(0, 0, bounds.Width, bounds.Height), 6, 6);

        var areas = LayerAreas;
        if (areas is null || areas.Count < 2)
        {
            var noDataText = new FormattedText(
                "No slice area data available",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold),
                11.0,
                new SolidColorBrush(Color.FromRgb(160, 168, 184)));
            context.DrawText(noDataText, new Point(12, bounds.Height / 2.0 - 7));
            return;
        }

        int count = areas.Count;
        float maxArea = MaxArea;
        if (maxArea <= 0.001f)
        {
            foreach (var a in areas) if (a > maxArea) maxArea = a;
        }
        if (maxArea <= 0.001f) maxArea = 1.0f;

        int curLayer = Math.Clamp(CurrentLayer, 0, count - 1);
        float curArea = areas[curLayer];

        // Check if current layer is a peel spike
        bool isCurrentSpike = false;
        if (curLayer > 0 && areas[curLayer - 1] > 0.01f)
        {
            float delta = curArea - areas[curLayer - 1];
            if (delta / areas[curLayer - 1] >= 0.35f && delta > 4.0f)
            {
                isCurrentSpike = true;
            }
        }

        // Header info text
        var titleText = new FormattedText(
            "PEEL FORCE / CROSS-SECTION AREA PROFILE",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold),
            9.5,
            new SolidColorBrush(Color.FromRgb(140, 155, 175)));
        context.DrawText(titleText, new Point(10, 6));

        string readout = $"Layer {curLayer + 1}/{count} • {curArea:F1} mm² (Peak: {maxArea:F1} mm²)";
        var readoutText = new FormattedText(
            readout,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold),
            11.0,
            Brushes.White);
        context.DrawText(readoutText, new Point(10, 20));

        if (isCurrentSpike)
        {
            var spikeText = new FormattedText(
                "⚠️ PEEL SURGE",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold),
                10.0,
                new SolidColorBrush(Color.FromRgb(255, 60, 60)));
            context.DrawText(spikeText, new Point(bounds.Width - spikeText.Width - 10, 20));
        }

        // Graph area bounds
        const double padL = 10.0;
        double padR = bounds.Width - 10.0;
        const double padTop = 38.0;
        double padBtm = bounds.Height - 8.0;
        double plotW = Math.Max(1.0, padR - padL);
        double plotH = Math.Max(1.0, padBtm - padTop);

        // Grid lines (2 horizontal lines)
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)), 0.7, DashStyle.Dash);
        context.DrawLine(gridPen, new Point(padL, padTop), new Point(padR, padTop));
        context.DrawLine(gridPen, new Point(padL, padTop + plotH * 0.5), new Point(padR, padTop + plotH * 0.5));
        context.DrawLine(gridPen, new Point(padL, padBtm), new Point(padR, padBtm));

        // Build StreamGeometry for curve and filled area
        var fillGeo = new StreamGeometry();
        var lineGeo = new StreamGeometry();

        using (var fillCtx = fillGeo.Open())
        using (var lineCtx = lineGeo.Open())
        {
            Point startPt = new Point(padL, padBtm - (areas[0] / maxArea) * plotH);
            fillCtx.BeginFigure(new Point(padL, padBtm), true);
            fillCtx.LineTo(startPt);

            lineCtx.BeginFigure(startPt, false);

            int stride = Math.Max(1, count / 250);
            for (int i = stride; i < count; i += stride)
            {
                double x = padL + (double)i / (count - 1) * plotW;
                double y = padBtm - (areas[i] / maxArea) * plotH;
                var pt = new Point(x, y);
                fillCtx.LineTo(pt);
                lineCtx.LineTo(pt);
            }

            // Ensure last point is included
            double lastX = padR;
            double lastY = padBtm - (areas[count - 1] / maxArea) * plotH;
            var finalPt = new Point(lastX, lastY);
            fillCtx.LineTo(finalPt);
            lineCtx.LineTo(finalPt);

            fillCtx.LineTo(new Point(padR, padBtm));
            fillCtx.EndFigure(true);
            lineCtx.EndFigure(false);
        }

        // Fill area below curve
        var fillBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0.5, 0.0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.5, 1.0, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(140, 0, 210, 255), 0.0),
                new GradientStop(Color.FromArgb(20, 0, 150, 220), 1.0)
            }
        };
        context.DrawGeometry(fillBrush, null, fillGeo);

        // Stroke curve
        var curvePen = new Pen(new SolidColorBrush(Color.FromRgb(0, 225, 255)), 1.5);
        context.DrawGeometry(null, curvePen, lineGeo);

        // Draw peel hazard spikes (red dots / vertical lines)
        var spikes = PeelSpikes;
        if (spikes is not null && spikes.Count > 0)
        {
            var spikePen = new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 40, 60)), 1.0, DashStyle.Dash);
            var spikeDotBrush = new SolidColorBrush(Color.FromRgb(255, 50, 70));
            foreach (var spk in spikes)
            {
                if (spk < 0 || spk >= count) continue;
                double sx = padL + (double)spk / (count - 1) * plotW;
                double sy = padBtm - (areas[spk] / maxArea) * plotH;
                context.DrawLine(spikePen, new Point(sx, padTop), new Point(sx, padBtm));
                context.DrawEllipse(spikeDotBrush, null, new Point(sx, sy), 2.5, 2.5);
            }
        }

        // Current layer needle line & glowing indicator
        double curX = padL + (double)curLayer / (count - 1) * plotW;
        double curY = padBtm - (curArea / maxArea) * plotH;
        var needlePen = new Pen(new SolidColorBrush(Color.FromRgb(255, 215, 0)), 1.5);
        context.DrawLine(needlePen, new Point(curX, padTop - 2), new Point(curX, padBtm + 2));
        context.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 0)), new Pen(Brushes.White, 1.0), new Point(curX, curY), 3.5, 3.5);

        // Hover guide line
        if (_hoveredLayer.HasValue && _hoveredLayer.Value != curLayer)
        {
            int hLayer = _hoveredLayer.Value;
            double hx = padL + (double)hLayer / (count - 1) * plotW;
            double hy = padBtm - (areas[hLayer] / maxArea) * plotH;
            var hoverPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)), 1.0, DashStyle.Dash);
            context.DrawLine(hoverPen, new Point(hx, padTop), new Point(hx, padBtm));
            context.DrawEllipse(Brushes.White, null, new Point(hx, hy), 2.5, 2.5);
        }
    }
}
