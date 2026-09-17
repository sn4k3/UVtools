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

    public static readonly StyledProperty<IReadOnlyList<float>?> LayerLiftSpeedsProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, IReadOnlyList<float>?>(nameof(LayerLiftSpeeds));

    public static readonly StyledProperty<IReadOnlyList<float>?> LayerLiftSpeeds2Property =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, IReadOnlyList<float>?>(nameof(LayerLiftSpeeds2));

    public static readonly StyledProperty<IReadOnlyList<float>?> LayerLiftHeightsProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, IReadOnlyList<float>?>(nameof(LayerLiftHeights));

    public static readonly StyledProperty<IReadOnlyList<float>?> LayerLiftHeights2Property =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, IReadOnlyList<float>?>(nameof(LayerLiftHeights2));

    public static readonly StyledProperty<bool> ShowTsmcProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, bool>(nameof(ShowTsmc), true);

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

    public IReadOnlyList<float>? LayerLiftSpeeds
    {
        get => GetValue(LayerLiftSpeedsProperty);
        set => SetValue(LayerLiftSpeedsProperty, value);
    }

    public IReadOnlyList<float>? LayerLiftSpeeds2
    {
        get => GetValue(LayerLiftSpeeds2Property);
        set => SetValue(LayerLiftSpeeds2Property, value);
    }

    public IReadOnlyList<float>? LayerLiftHeights
    {
        get => GetValue(LayerLiftHeightsProperty);
        set => SetValue(LayerLiftHeightsProperty, value);
    }

    public IReadOnlyList<float>? LayerLiftHeights2
    {
        get => GetValue(LayerLiftHeights2Property);
        set => SetValue(LayerLiftHeights2Property, value);
    }

    public bool ShowTsmc
    {
        get => GetValue(ShowTsmcProperty);
        set => SetValue(ShowTsmcProperty, value);
    }

    static LayerAreaPeelCurveControl()
    {
        AffectsRender<LayerAreaPeelCurveControl>(
            LayerAreasProperty,
            CurrentLayerProperty,
            MaxAreaProperty,
            PeakLayerProperty,
            PeelSpikesProperty,
            LayerLiftSpeedsProperty,
            LayerLiftSpeeds2Property,
            LayerLiftHeightsProperty,
            LayerLiftHeights2Property,
            ShowTsmcProperty);
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

        const double padL = 12.0;
        double padR = Bounds.Width - 12.0;
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

        // Background canvas card for the chart
        var cardBrush = new SolidColorBrush(Color.FromArgb(225, 14, 18, 26));
        var borderPen = new Pen(new SolidColorBrush(Color.FromArgb(140, 40, 55, 75)), 1.0);
        context.DrawRectangle(cardBrush, borderPen, new Rect(0, 0, bounds.Width, bounds.Height), 6, 6);

        var areas = LayerAreas;
        if (areas is null || areas.Count < 2)
        {
            var noDataText = new FormattedText(
                "No slice area data available",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold),
                13.0,
                new SolidColorBrush(Color.FromRgb(160, 168, 184)));
            context.DrawText(noDataText, new Point(14, bounds.Height / 2.0 - 9));
            return;
        }

        int count = areas.Count;
        float maxArea = MaxArea;
        if (maxArea <= 0.001f)
        {
            foreach (var a in areas) if (a > maxArea) maxArea = a;
        }
        if (maxArea <= 0.001f) maxArea = 1.0f;

        bool isHovered = _hoveredLayer.HasValue && _hoveredLayer.Value >= 0 && _hoveredLayer.Value < count;
        int activeLayer = isHovered ? _hoveredLayer!.Value : Math.Clamp(CurrentLayer, 0, count - 1);
        int curLayer = Math.Clamp(CurrentLayer, 0, count - 1);
        float activeArea = areas[activeLayer];
        float curArea = areas[curLayer];

        // Check if active layer is a peel spike
        bool isCurrentSpike = false;
        float spikeDeltaPercent = 0f;
        if (activeLayer > 0 && areas[activeLayer - 1] > 0.01f)
        {
            float delta = activeArea - areas[activeLayer - 1];
            if (delta / areas[activeLayer - 1] >= 0.35f && delta > 4.0f)
            {
                isCurrentSpike = true;
                spikeDeltaPercent = (delta / areas[activeLayer - 1]) * 100f;
            }
        }

        float speed1 = LayerLiftSpeeds is not null && activeLayer < LayerLiftSpeeds.Count ? LayerLiftSpeeds[activeLayer] : 0f;
        float speed2 = LayerLiftSpeeds2 is not null && activeLayer < LayerLiftSpeeds2.Count ? LayerLiftSpeeds2[activeLayer] : 0f;
        float h1 = LayerLiftHeights is not null && activeLayer < LayerLiftHeights.Count ? LayerLiftHeights[activeLayer] : 0f;
        float h2 = LayerLiftHeights2 is not null && activeLayer < LayerLiftHeights2.Count ? LayerLiftHeights2[activeLayer] : 0f;

        bool hasTsmc = speed1 > 0f && (speed2 > 0f || h2 > 0f);
        bool isTsmcSpeedDanger = (activeArea > 0.40f * maxArea && speed1 > 90f);
        bool isTsmcHeightDanger = (activeArea > 0.45f * maxArea && h1 > 0f && h1 < 2.5f);
        bool isDelamHazard = isCurrentSpike || isTsmcSpeedDanger || isTsmcHeightDanger;

        const double padL = 12.0;
        double padR = bounds.Width - 12.0;

        // =========================================================================
        // LINE 1: Active Layer & Area (Left), Peak Area & Layer (Right)
        // =========================================================================
        string layerPrefix = isHovered ? $"Layer {activeLayer + 1}/{count} (Hover):" : $"Layer {activeLayer + 1}/{count}:";
        string line1LeftStr = $"{layerPrefix}  {activeArea:N1} mm²";

        var line1LeftText = new FormattedText(
            line1LeftStr,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold),
            12.5,
            isHovered ? new SolidColorBrush(Color.FromRgb(100, 220, 255)) : Brushes.White);
        context.DrawText(line1LeftText, new Point(padL, 6));

        string peakStr = PeakLayer >= 0 ? $"Peak: {maxArea:N1} mm² (L{PeakLayer + 1})" : $"Peak: {maxArea:N1} mm²";
        var peakText = new FormattedText(
            peakStr,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold),
            11.5,
            new SolidColorBrush(Color.FromRgb(145, 160, 180)));
        context.DrawText(peakText, new Point(padR - peakText.Width, 7));

        // =========================================================================
        // LINE 2: TSMC / Lift Speeds (Left) and Delamination Hazard Alert Badge (Right)
        // =========================================================================
        double rightBadgeWidth = 0.0;
        if (isDelamHazard)
        {
            string hazardLabel = isCurrentSpike
                ? $"⚠️ PEEL SPIKE (+{spikeDeltaPercent:F0}%)"
                : (isTsmcSpeedDanger
                    ? $"⚠️ FAST PEEL ({speed1:F0}mm/m)"
                    : "⚠️ SHORT LIFT STAGE");

            var hazardText = new FormattedText(
                hazardLabel,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold),
                10.5,
                new SolidColorBrush(Color.FromRgb(255, 110, 110)));

            double badgePadX = 6.0;
            double badgePadY = 2.0;
            double badgeW = hazardText.Width + badgePadX * 2.0;
            double badgeH = hazardText.Height + badgePadY * 2.0;
            double badgeX = padR - badgeW;
            double badgeY = 25.0;

            var hazardBg = new SolidColorBrush(Color.FromArgb(60, 248, 81, 73));
            var hazardPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 248, 81, 73)), 1.0);
            context.DrawRectangle(hazardBg, hazardPen, new Rect(badgeX, badgeY, badgeW, badgeH), 4, 4);
            context.DrawText(hazardText, new Point(badgeX + badgePadX, badgeY + badgePadY));

            rightBadgeWidth = badgeW + 8.0;
        }

        string tsmcDesc;
        if (hasTsmc)
        {
            tsmcDesc = $"TSMC: {speed1:F0}mm/m ({h1:F1}mm) → {speed2:F0}mm/m ({h2:F1}mm)";
        }
        else if (speed1 > 0f)
        {
            tsmcDesc = $"Lift Speed: {speed1:F0}mm/m ({h1:F1}mm)";
        }
        else
        {
            tsmcDesc = "Cross-section area profile";
        }

        double availableLine2Width = Math.Max(50.0, padR - padL - rightBadgeWidth);
        var tsmcText = new FormattedText(
            tsmcDesc,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal),
            11.0,
            new SolidColorBrush(Color.FromRgb(120, 195, 255)));

        if (tsmcText.Width > availableLine2Width && hasTsmc)
        {
            tsmcDesc = $"TSMC: {speed1:F0} → {speed2:F0}mm/m";
            tsmcText = new FormattedText(
                tsmcDesc,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal),
                11.0,
                new SolidColorBrush(Color.FromRgb(120, 195, 255)));
        }

        context.DrawText(tsmcText, new Point(padL, 27));

        // =========================================================================
        // GRAPH AREA BOUNDS & PLOT
        // =========================================================================
        const double padTop = 50.0;
        double padBtm = bounds.Height - 10.0;
        double plotW = Math.Max(1.0, padR - padL);
        double plotH = Math.Max(1.0, padBtm - padTop);

        // Grid lines (3 horizontal dashed lines: Top, Mid, Btm)
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 0.8, DashStyle.Dash);
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

        // Draw TSMC hazard bands (layers with high peel suction & high lift speed)
        var speeds = LayerLiftSpeeds;
        if (ShowTsmc && speeds is not null && speeds.Count >= count)
        {
            var hazardBrush = new SolidColorBrush(Color.FromArgb(50, 255, 60, 40));
            for (int i = 0; i < count; i += Math.Max(1, count / 100))
            {
                if (areas[i] > 0.40f * maxArea && speeds[i] > 90f)
                {
                    double hx = padL + (double)i / (count - 1) * plotW;
                    double bandW = Math.Max(2.0, plotW / 100.0);
                    context.DrawRectangle(hazardBrush, null, new Rect(hx - bandW * 0.5, padTop, bandW, plotH));
                }
            }
        }

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
