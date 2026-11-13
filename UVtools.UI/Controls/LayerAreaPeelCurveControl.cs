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
using Avalonia.Media.Immutable;
using UVtools.Core.Layers;

namespace UVtools.UI.Controls;

/// <summary>
/// What the <see cref="LayerAreaPeelCurveControl"/> plots for each layer.
/// </summary>
public enum PeelCurveMode
{
    /// <summary>The cross-section area of the layer.</summary>
    Area,

    /// <summary>The signed area change relative to the layer below, in percent.</summary>
    AreaDelta,

    /// <summary>The distance the area centroid moved relative to the layer below, in mm.</summary>
    CentroidDrift,

    /// <summary>The combined mechanical risk score, which is relative to the loaded file.</summary>
    RiskScore
}

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

    public static readonly StyledProperty<bool> HaveTiltingVatProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, bool>(nameof(HaveTiltingVat));

    public static readonly StyledProperty<System.Windows.Input.ICommand?> LayerSelectedCommandProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, System.Windows.Input.ICommand?>(nameof(LayerSelectedCommand));

    public static readonly StyledProperty<PeelCurveMode> ModeProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, PeelCurveMode>(nameof(Mode));

    public static readonly StyledProperty<IReadOnlyList<float>?> AreaDeltaPercentProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, IReadOnlyList<float>?>(nameof(AreaDeltaPercent));

    public static readonly StyledProperty<IReadOnlyList<float>?> CentroidDriftProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, IReadOnlyList<float>?>(nameof(CentroidDrift));

    public static readonly StyledProperty<IReadOnlyList<float>?> RiskScoresProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, IReadOnlyList<float>?>(nameof(RiskScores));

    public static readonly StyledProperty<IReadOnlyList<int>?> HighRiskLayersProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, IReadOnlyList<int>?>(nameof(HighRiskLayers));

    public static readonly StyledProperty<bool> IsComputingCentroidsProperty =
        AvaloniaProperty.Register<LayerAreaPeelCurveControl, bool>(nameof(IsComputingCentroids));

    public event Action<int>? LayerSelected;

    public System.Windows.Input.ICommand? LayerSelectedCommand
    {
        get => GetValue(LayerSelectedCommandProperty);
        set => SetValue(LayerSelectedCommandProperty, value);
    }

    // Brushes, pens and typefaces are immutable, share them instead of allocating them on every render
    private static readonly IBrush CardBrush = new ImmutableSolidColorBrush(Color.FromArgb(225, 14, 18, 26));
    private static readonly Pen CardBorderPen = new Pen(new ImmutableSolidColorBrush(Color.FromArgb(140, 40, 55, 75)), 1.0);
    private static readonly IBrush NoDataBrush = new ImmutableSolidColorBrush(Color.FromRgb(160, 168, 184));
    private static readonly IBrush HoverTextBrush = new ImmutableSolidColorBrush(Color.FromRgb(100, 220, 255));
    private static readonly IBrush PeakTextBrush = new ImmutableSolidColorBrush(Color.FromRgb(145, 160, 180));
    private static readonly IBrush HazardTextBrush = new ImmutableSolidColorBrush(Color.FromRgb(255, 110, 110));
    private static readonly IBrush HazardBackgroundBrush = new ImmutableSolidColorBrush(Color.FromArgb(60, 248, 81, 73));
    private static readonly Pen HazardPen = new Pen(new ImmutableSolidColorBrush(Color.FromArgb(180, 248, 81, 73)), 1.0);
    private static readonly IBrush TsmcTextBrush = new ImmutableSolidColorBrush(Color.FromRgb(120, 195, 255));
    private static readonly Pen GridPen = new Pen(new ImmutableSolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 0.8, DashStyle.Dash);
    private static readonly IBrush FillBrush = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0.5, 0.0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0.5, 1.0, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.FromArgb(140, 0, 210, 255), 0.0),
            new GradientStop(Color.FromArgb(20, 0, 150, 220), 1.0)
        }
    }.ToImmutable();    private static readonly Pen CurvePen = new Pen(new ImmutableSolidColorBrush(Color.FromRgb(0, 225, 255)), 1.5);
    private static readonly IBrush TsmcBandBrush = new ImmutableSolidColorBrush(Color.FromArgb(50, 255, 60, 40));
    private static readonly Pen SpikePen = new Pen(new ImmutableSolidColorBrush(Color.FromArgb(160, 255, 40, 60)), 1.0, DashStyle.Dash);
    private static readonly IBrush SpikeDotBrush = new ImmutableSolidColorBrush(Color.FromRgb(255, 50, 70));
    private static readonly IBrush NeedleBrush = new ImmutableSolidColorBrush(Color.FromRgb(255, 215, 0));
    private static readonly Pen NeedlePen = new Pen(NeedleBrush, 1.5);
    private static readonly Pen NeedleMarkerPen = new Pen(Brushes.White, 1.0);
    private static readonly Pen HoverPen = new Pen(new ImmutableSolidColorBrush(Color.FromArgb(180, 255, 255, 255)), 1.0, DashStyle.Dash);
    private static readonly IBrush RiseBrush = new ImmutableSolidColorBrush(Color.FromRgb(255, 120, 100));
    private static readonly IBrush DropBrush = new ImmutableSolidColorBrush(Color.FromRgb(70, 200, 230));
    private static readonly IBrush ClipMarkerBrush = new ImmutableSolidColorBrush(Color.FromRgb(255, 255, 255));
    private static readonly Pen BaselinePen = new Pen(new ImmutableSolidColorBrush(Color.FromArgb(110, 255, 255, 255)), 1.0);
    private static readonly IBrush DriftFillBrush = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0.5, 0.0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0.5, 1.0, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.FromArgb(130, 190, 120, 255), 0.0),
            new GradientStop(Color.FromArgb(20, 140, 90, 220), 1.0)
        }
    }.ToImmutable();
    private static readonly Pen DriftPen = new Pen(new ImmutableSolidColorBrush(Color.FromRgb(200, 140, 255)), 1.5);

    // Risk level colors, indexed by LayerMechanicalRiskLevel
    private static readonly IBrush[] RiskBrushes =
    [
        new ImmutableSolidColorBrush(Color.FromArgb(220, 63, 185, 80)),
        new ImmutableSolidColorBrush(Color.FromArgb(230, 227, 179, 0)),
        new ImmutableSolidColorBrush(Color.FromArgb(240, 255, 140, 0)),
        new ImmutableSolidColorBrush(Color.FromArgb(250, 248, 81, 73))
    ];
    private static readonly IBrush RiskOrangeTextBrush = new ImmutableSolidColorBrush(Color.FromRgb(255, 165, 60));
    private static readonly IBrush RiskOrangeBackgroundBrush = new ImmutableSolidColorBrush(Color.FromArgb(60, 255, 140, 0));
    private static readonly Pen RiskOrangePen = new Pen(new ImmutableSolidColorBrush(Color.FromArgb(180, 255, 140, 0)), 1.0);
    private static readonly Typeface SemiBoldTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);
    private static readonly Typeface BoldTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
    private static readonly Typeface NormalTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);

    private int? _hoveredLayer;
    private bool _isDragging;

    // The curve only changes with the data or the size of the control, not when the hover or current layer move
    private StreamGeometry? _fillGeometry;
    private StreamGeometry? _lineGeometry;
    private IReadOnlyList<float>? _geometryAreas;
    private float _geometryMaxArea;
    private Size _geometrySize;

    // The geometry and stats of the AreaDelta, CentroidDrift and RiskScore modes, rebuilt like the area one
    private static readonly StreamGeometry EmptyGeometry = new();
    private readonly StreamGeometry?[] _seriesGeometries = new StreamGeometry?[4];
    private readonly List<int> _seriesClipped = [];
    private double _seriesBarWidth = 1.0;
    private IReadOnlyList<float>? _seriesData;
    private PeelCurveMode _seriesMode;
    private Size _seriesSize;
    private float _seriesRange;
    private int _seriesPeakIndex;

    public PeelCurveMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    /// <summary>
    /// Gets or sets the signed area change of each layer in percent, plotted by <see cref="PeelCurveMode.AreaDelta"/>.
    /// </summary>
    public IReadOnlyList<float>? AreaDeltaPercent
    {
        get => GetValue(AreaDeltaPercentProperty);
        set => SetValue(AreaDeltaPercentProperty, value);
    }

    /// <summary>
    /// Gets or sets the centroid drift of each layer in mm, plotted by <see cref="PeelCurveMode.CentroidDrift"/>.
    /// </summary>
    public IReadOnlyList<float>? CentroidDrift
    {
        get => GetValue(CentroidDriftProperty);
        set => SetValue(CentroidDriftProperty, value);
    }

    /// <summary>
    /// Gets or sets the relative risk score of each layer, plotted by <see cref="PeelCurveMode.RiskScore"/>.
    /// </summary>
    public IReadOnlyList<float>? RiskScores
    {
        get => GetValue(RiskScoresProperty);
        set => SetValue(RiskScoresProperty, value);
    }

    /// <summary>
    /// Gets or sets the layers with an orange or red risk level, marked by <see cref="PeelCurveMode.RiskScore"/>.
    /// </summary>
    public IReadOnlyList<int>? HighRiskLayers
    {
        get => GetValue(HighRiskLayersProperty);
        set => SetValue(HighRiskLayersProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the centroids are still being calculated, which the drift and risk modes need.
    /// </summary>
    public bool IsComputingCentroids
    {
        get => GetValue(IsComputingCentroidsProperty);
        set => SetValue(IsComputingCentroidsProperty, value);
    }

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

    /// <summary>
    /// Gets or sets whether the loaded file uses a tilting vat, which peels by tilt rather than a vertical
    /// lift, so any lift speed value read from it does not reflect the actual release motion.
    /// </summary>
    public bool HaveTiltingVat
    {
        get => GetValue(HaveTiltingVatProperty);
        set => SetValue(HaveTiltingVatProperty, value);
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
            ShowTsmcProperty,
            HaveTiltingVatProperty,
            ModeProperty,
            AreaDeltaPercentProperty,
            CentroidDriftProperty,
            RiskScoresProperty,
            HighRiskLayersProperty,
            IsComputingCentroidsProperty);
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
        var previousLayer = _hoveredLayer;
        UpdateHoverFromPoint(pt);

        // Only navigate when the layer under the pointer changed
        if (_isDragging && _hoveredLayer.HasValue && _hoveredLayer != previousLayer)
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
        context.DrawRectangle(CardBrush, CardBorderPen, new Rect(0, 0, bounds.Width, bounds.Height), 6, 6);

        var areas = LayerAreas;
        if (areas is null || areas.Count < 2)
        {
            var noDataText = new FormattedText(
                "No slice area data available",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                SemiBoldTypeface,
                13.0,
                NoDataBrush);
            context.DrawText(noDataText, new Point(14, bounds.Height / 2.0 - 9));
            return;
        }

        int count = areas.Count;
        var mode = Mode;
        IReadOnlyList<float>? series = null;
        if (mode != PeelCurveMode.Area)
        {
            series = GetSeries(mode, count);
            if (series is null)
            {
                var unavailableText = new FormattedText(
                    IsComputingCentroids && mode is PeelCurveMode.CentroidDrift or PeelCurveMode.RiskScore
                        ? "Computing centroids…"
                        : "No data available for this mode",
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    SemiBoldTypeface,
                    13.0,
                    NoDataBrush);
                context.DrawText(unavailableText, new Point(14, bounds.Height / 2.0 - 9));
                return;
            }

            EnsureSeriesGeometry(mode, series, bounds.Size);
        }

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
        if (activeLayer > 0 && LayerMechanicalAnalysis.IsPeelSpike(areas[activeLayer - 1], activeArea))
        {
            isCurrentSpike = true;
            spikeDeltaPercent = (activeArea - areas[activeLayer - 1]) / areas[activeLayer - 1] * 100f;
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
        string activeValueText = series is null ? $"{activeArea:N1} mm²" : FormatSeriesValue(mode, series[activeLayer]);

        FormattedText CreateLine1LeftText(bool withHoverTag)
        {
            string layerPrefix = withHoverTag ? $"Layer {activeLayer + 1}/{count} (Hover):" : $"Layer {activeLayer + 1}/{count}:";
            return new FormattedText(
                $"{layerPrefix}  {activeValueText}",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                BoldTypeface,
                12.5,
                isHovered ? HoverTextBrush : Brushes.White);
        }

        FormattedText CreatePeakText(bool compact)
        {
            string peakStr;
            if (series is not null)
            {
                var peakValue = series[_seriesPeakIndex];
                peakStr = compact
                    ? $"Peak: {FormatSeriesValue(mode, peakValue, true)}"
                    : $"Peak: {FormatSeriesValue(mode, peakValue, true)} (L{_seriesPeakIndex + 1})";
            }
            else
            {
                peakStr = compact
                    ? $"Peak: {maxArea:N0} mm²"
                    : PeakLayer >= 0 ? $"Peak: {maxArea:N1} mm² (L{PeakLayer + 1})" : $"Peak: {maxArea:N1} mm²";
            }

            return new FormattedText(
                peakStr,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                SemiBoldTypeface,
                11.5,
                PeakTextBrush);
        }

        // Shorten the line when the layer and the peak don't fit side by side: the hover color already tells it is
        // hovered, then the peak is rounded without its layer, and at last it is hidden
        const double line1Gap = 10.0;
        var line1LeftText = CreateLine1LeftText(isHovered);
        var peakText = CreatePeakText(false);
        if (padL + line1LeftText.Width + line1Gap > padR - peakText.Width && isHovered)
        {
            line1LeftText = CreateLine1LeftText(false);
        }

        if (padL + line1LeftText.Width + line1Gap > padR - peakText.Width)
        {
            peakText = CreatePeakText(true);
        }

        context.DrawText(line1LeftText, new Point(padL, 6));
        if (padL + line1LeftText.Width + line1Gap <= padR - peakText.Width)
        {
            context.DrawText(peakText, new Point(padR - peakText.Width, 7));
        }

        // =========================================================================
        // LINE 2: TSMC / Lift Speeds (Left) and Delamination Hazard Alert Badge (Right)
        // =========================================================================
        double rightBadgeWidth = 0.0;
        string? hazardLabel = null;
        IBrush hazardTextBrush = HazardTextBrush;
        IBrush hazardBackgroundBrush = HazardBackgroundBrush;
        Pen hazardPen = HazardPen;
        if (series is null)
        {
            if (isDelamHazard)
            {
                hazardLabel = isCurrentSpike
                    ? $"⚠️ PEEL SPIKE (+{spikeDeltaPercent:F0}%)"
                    : (isTsmcSpeedDanger
                        ? $"⚠️ FAST PEEL ({speed1:F0}mm/m)"
                        : "⚠️ SHORT LIFT STAGE");
            }
        }
        else if (mode == PeelCurveMode.RiskScore)
        {
            var riskScore = series[activeLayer];
            var riskLevel = LayerMechanicalAnalysis.GetRiskLevel(riskScore);
            if (riskLevel >= LayerMechanicalRiskLevel.Medium)
            {
                hazardLabel = $"⚠️ {riskLevel.ToString().ToUpperInvariant()} RISK ({riskScore:F1})";
                if (riskLevel == LayerMechanicalRiskLevel.Medium)
                {
                    hazardTextBrush = RiskOrangeTextBrush;
                    hazardBackgroundBrush = RiskOrangeBackgroundBrush;
                    hazardPen = RiskOrangePen;
                }
            }
        }

        if (hazardLabel is not null)
        {
            var hazardText = new FormattedText(
                hazardLabel,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                BoldTypeface,
                10.5,
                hazardTextBrush);

            double badgePadX = 6.0;
            double badgePadY = 2.0;
            double badgeW = hazardText.Width + badgePadX * 2.0;
            double badgeH = hazardText.Height + badgePadY * 2.0;
            double badgeX = padR - badgeW;
            double badgeY = 25.0;

            context.DrawRectangle(hazardBackgroundBrush, hazardPen, new Rect(badgeX, badgeY, badgeW, badgeH), 4, 4);
            context.DrawText(hazardText, new Point(badgeX + badgePadX, badgeY + badgePadY));

            rightBadgeWidth = badgeW + 8.0;
        }

        string tsmcDesc;
        if (series is not null)
        {
            tsmcDesc = mode switch
            {
                PeelCurveMode.AreaDelta => "Area change vs previous layer",
                PeelCurveMode.CentroidDrift => "Centroid shift vs previous layer",
                _ => "Relative to this file"
            };
        }
        else if (hasTsmc)
        {
            tsmcDesc = $"TSMC: {speed1:F0}mm/m ({h1:F1}mm) → {speed2:F0}mm/m ({h2:F1}mm)";
        }
        else if (speed1 > 0f && !HaveTiltingVat)
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
            NormalTypeface,
            11.0,
            TsmcTextBrush);

        if (tsmcText.Width > availableLine2Width && hasTsmc && series is null)
        {
            tsmcDesc = $"TSMC: {speed1:F0} → {speed2:F0}mm/m";
            tsmcText = new FormattedText(
                tsmcDesc,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                NormalTypeface,
                11.0,
                TsmcTextBrush);
        }

        context.DrawText(tsmcText, new Point(padL, 27));

        if (series is not null)
        {
            DrawSeriesPlot(context, bounds, mode, series, count, curLayer);
            return;
        }

        // =========================================================================
        // GRAPH AREA BOUNDS & PLOT
        // =========================================================================
        const double padTop = 50.0;
        double padBtm = bounds.Height - 10.0;
        double plotW = Math.Max(1.0, padR - padL);
        double plotH = Math.Max(1.0, padBtm - padTop);

        // Grid lines (3 horizontal dashed lines: Top, Mid, Btm)
        var gridPen = GridPen;
        context.DrawLine(gridPen, new Point(padL, padTop), new Point(padR, padTop));
        context.DrawLine(gridPen, new Point(padL, padTop + plotH * 0.5), new Point(padR, padTop + plotH * 0.5));
        context.DrawLine(gridPen, new Point(padL, padBtm), new Point(padR, padBtm));

        // Build StreamGeometry for curve and filled area, rebuilt only when the data or the size changed
        if (_fillGeometry is null || _lineGeometry is null || !ReferenceEquals(_geometryAreas, areas)
            || _geometryMaxArea != maxArea || _geometrySize != bounds.Size)
        {
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

            _fillGeometry = fillGeo;
            _lineGeometry = lineGeo;
            _geometryAreas = areas;
            _geometryMaxArea = maxArea;
            _geometrySize = bounds.Size;
        }
        // Fill area below curve
        context.DrawGeometry(FillBrush, null, _fillGeometry);

        // Stroke curve
        context.DrawGeometry(null, CurvePen, _lineGeometry);

        // Draw TSMC hazard bands (layers with high peel suction & high lift speed)
        var speeds = LayerLiftSpeeds;
        if (ShowTsmc && speeds is not null && speeds.Count >= count)
        {
            var hazardBrush = TsmcBandBrush;
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
            var spikePen = SpikePen;
            var spikeDotBrush = SpikeDotBrush;
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
        context.DrawLine(NeedlePen, new Point(curX, padTop - 2), new Point(curX, padBtm + 2));
        context.DrawEllipse(NeedleBrush, NeedleMarkerPen, new Point(curX, curY), 3.5, 3.5);

        // Hover guide line
        if (_hoveredLayer.HasValue && _hoveredLayer.Value != curLayer)
        {
            int hLayer = _hoveredLayer.Value;
            double hx = padL + (double)hLayer / (count - 1) * plotW;
            double hy = padBtm - (areas[hLayer] / maxArea) * plotH;
            context.DrawLine(HoverPen, new Point(hx, padTop), new Point(hx, padBtm));
            context.DrawEllipse(Brushes.White, null, new Point(hx, hy), 2.5, 2.5);
        }
    }

    private IReadOnlyList<float>? GetSeries(PeelCurveMode mode, int count)
    {
        var series = mode switch
        {
            PeelCurveMode.AreaDelta => AreaDeltaPercent,
            PeelCurveMode.CentroidDrift => CentroidDrift,
            PeelCurveMode.RiskScore => RiskScores,
            _ => null
        };

        return series is not null && series.Count == count ? series : null;
    }

    private static string FormatSeriesValue(PeelCurveMode mode, float value, bool peak = false)
    {
        return mode switch
        {
            PeelCurveMode.AreaDelta => peak ? $"{value:+0;-0;0}%" : $"Δ {value:+0.0;-0.0;0.0}%",
            PeelCurveMode.CentroidDrift => peak ? $"{value:N2} mm" : $"drift {value:N2} mm",
            _ => peak
                ? $"{value:N1}"
                : $"risk {value:N1}{FormatRiskLevel(LayerMechanicalAnalysis.GetRiskLevel(value))}"
        };
    }

    private static string FormatRiskLevel(LayerMechanicalRiskLevel level) => level switch
    {
        LayerMechanicalRiskLevel.Low => " (Low risk)",
        LayerMechanicalRiskLevel.Medium => " (Medium risk)",
        LayerMechanicalRiskLevel.High => " (High risk)",
        _ => string.Empty
    };

    private StreamGeometry GetSeriesGeometry(int index) => _seriesGeometries[index] ?? EmptyGeometry;

    private static float[] ToArray(IReadOnlyList<float> values)
    {
        if (values is float[] array) return array;

        var copy = new float[values.Count];
        for (var i = 0; i < copy.Length; i++)
        {
            copy[i] = values[i];
        }

        return copy;
    }

    /// <summary>
    /// Calculates the range of the plotted series and builds its geometry, only when the data, the mode or the size
    /// changed. The range is robust, so a few outliers do not flatten the rest of the plot: they are clipped to the
    /// edge of it and marked.
    /// </summary>
    private void EnsureSeriesGeometry(PeelCurveMode mode, IReadOnlyList<float> series, Size size)
    {
        if (_seriesGeometries[0] is not null && ReferenceEquals(_seriesData, series) && _seriesMode == mode &&
            _seriesSize == size)
        {
            return;
        }

        const double padL = 12.0;
        const double padTop = 50.0;
        double padR = size.Width - 12.0;
        double padBtm = size.Height - 10.0;
        double plotW = Math.Max(1.0, padR - padL);
        double plotH = Math.Max(1.0, padBtm - padTop);
        int count = series.Count;
        var values = ToArray(series);

        _seriesRange = mode switch
        {
            PeelCurveMode.AreaDelta => Math.Max(50f, LayerMechanicalAnalysis.GetPercentile(values, 99f, true, true)),
            PeelCurveMode.CentroidDrift => Math.Max(0.01f, LayerMechanicalAnalysis.GetPercentile(values, 99f, false, true)),
            _ => Math.Max(LayerMechanicalAnalysis.HighRiskScore * 1.25f, LayerMechanicalAnalysis.GetPercentile(values, 99f))
        };
        float range = _seriesRange;

        _seriesPeakIndex = 0;
        var peakAbs = 0f;
        for (var i = 0; i < values.Length; i++)
        {
            var abs = MathF.Abs(values[i]);
            if (abs > peakAbs)
            {
                peakAbs = abs;
                _seriesPeakIndex = i;
            }
        }

        // One value per pixel column is enough, the one that stands out the most so no peak is skipped
        int columns = (int)Math.Clamp(Math.Floor(plotW), 1.0, count);
        var representatives = new int[columns];
        Array.Fill(representatives, -1);
        for (var i = 0; i < values.Length; i++)
        {
            int column = (int)((long)i * columns / count);
            var current = representatives[column];
            if (current < 0 || MathF.Abs(values[i]) > MathF.Abs(values[current])) representatives[column] = i;
        }

        _seriesBarWidth = Math.Clamp(plotW / columns, 1.0, 4.0);
        _seriesClipped.Clear();
        foreach (var layer in representatives)
        {
            var value = values[layer];
            if (mode == PeelCurveMode.AreaDelta ? MathF.Abs(value) > range : value > range) _seriesClipped.Add(layer);
        }

        double LayerToX(int layer) => padL + (double)layer / (count - 1) * plotW;

        Array.Clear(_seriesGeometries);
        switch (mode)
        {
            case PeelCurveMode.AreaDelta:
            {
                double y0 = padTop + plotH * 0.5;
                var rise = new StreamGeometry();
                var drop = new StreamGeometry();
                using (var riseCtx = rise.Open())
                using (var dropCtx = drop.Open())
                {
                    foreach (var layer in representatives)
                    {
                        var value = values[layer];
                        if (value == 0f || !float.IsFinite(value)) continue;

                        var ctx = value > 0f ? riseCtx : dropCtx;
                        double x = LayerToX(layer);
                        double y = y0 - Math.Clamp(value / range, -1f, 1f) * plotH * 0.5;
                        ctx.BeginFigure(new Point(x, y0), false);
                        ctx.LineTo(new Point(x, y));
                        ctx.EndFigure(false);
                    }
                }

                _seriesGeometries[0] = rise;
                _seriesGeometries[1] = drop;
                break;
            }
            case PeelCurveMode.CentroidDrift:
            {
                var fill = new StreamGeometry();
                var line = new StreamGeometry();
                using (var fillCtx = fill.Open())
                using (var lineCtx = line.Open())
                {
                    double lastX = padL;
                    for (var column = 0; column < representatives.Length; column++)
                    {
                        var layer = representatives[column];
                        var pt = new Point(LayerToX(layer),
                            padBtm - Math.Clamp(values[layer] / range, 0f, 1f) * plotH);
                        if (column == 0)
                        {
                            fillCtx.BeginFigure(new Point(pt.X, padBtm), true);
                            lineCtx.BeginFigure(pt, false);
                        }

                        fillCtx.LineTo(pt);
                        lineCtx.LineTo(pt);
                        lastX = pt.X;
                    }

                    fillCtx.LineTo(new Point(lastX, padBtm));
                    fillCtx.EndFigure(true);
                    lineCtx.EndFigure(false);
                }

                _seriesGeometries[0] = fill;
                _seriesGeometries[1] = line;
                break;
            }
            default:
            {
                var geometries = new StreamGeometry[RiskBrushes.Length];
                var contexts = new StreamGeometryContext[RiskBrushes.Length];
                try
                {
                    for (var level = 0; level < geometries.Length; level++)
                    {
                        geometries[level] = new StreamGeometry();
                        contexts[level] = geometries[level].Open();
                    }

                    foreach (var layer in representatives)
                    {
                        var value = values[layer];
                        if (value <= 0f || !float.IsFinite(value)) continue;

                        var ctx = contexts[(int)LayerMechanicalAnalysis.GetRiskLevel(value)];
                        double x = LayerToX(layer);
                        double y = padBtm - Math.Min(value / range, 1f) * plotH;
                        ctx.BeginFigure(new Point(x, padBtm), false);
                        ctx.LineTo(new Point(x, y));
                        ctx.EndFigure(false);
                    }
                }
                finally
                {
                    foreach (var ctx in contexts)
                    {
                        ctx?.Dispose();
                    }
                }

                Array.Copy(geometries, _seriesGeometries, geometries.Length);
                break;
            }
        }

        _seriesData = series;
        _seriesMode = mode;
        _seriesSize = size;
    }

    /// <summary>
    /// Draws the plot of the AreaDelta, CentroidDrift and RiskScore modes, with the current layer and hover indicators.
    /// </summary>
    private void DrawSeriesPlot(DrawingContext context, Rect bounds, PeelCurveMode mode,
        IReadOnlyList<float> series, int count, int curLayer)
    {
        const double padL = 12.0;
        const double padTop = 50.0;
        double padR = bounds.Width - 12.0;
        double padBtm = bounds.Height - 10.0;
        double plotW = Math.Max(1.0, padR - padL);
        double plotH = Math.Max(1.0, padBtm - padTop);
        float range = _seriesRange;
        bool isSigned = mode == PeelCurveMode.AreaDelta;
        double y0 = padTop + plotH * 0.5;

        double LayerToX(int layer) => padL + (double)layer / (count - 1) * plotW;
        double ValueToY(float value) => isSigned
            ? y0 - Math.Clamp(value / range, -1f, 1f) * plotH * 0.5
            : padBtm - Math.Clamp(value / range, 0f, 1f) * plotH;

        // Grid lines (3 horizontal dashed lines: Top, Mid, Btm)
        context.DrawLine(GridPen, new Point(padL, padTop), new Point(padR, padTop));
        context.DrawLine(GridPen, new Point(padL, y0), new Point(padR, y0));
        context.DrawLine(GridPen, new Point(padL, padBtm), new Point(padR, padBtm));

        if (isSigned)
        {
            context.DrawLine(BaselinePen, new Point(padL, y0), new Point(padR, y0));
        }
        else if (mode == PeelCurveMode.RiskScore)
        {
            // Where the levels start, so the bars can be read against them
            foreach (var threshold in (ReadOnlySpan<float>)
                     [LayerMechanicalAnalysis.LowRiskScore, LayerMechanicalAnalysis.MediumRiskScore,
                         LayerMechanicalAnalysis.HighRiskScore])
            {
                if (threshold >= range) continue;
                double ty = ValueToY(threshold);
                context.DrawLine(GridPen, new Point(padL, ty), new Point(padR, ty));
            }
        }

        // Scale of the plot
        string topLabel;
        string bottomLabel;
        switch (mode)
        {
            case PeelCurveMode.AreaDelta:
                topLabel = $"+{range:N0}%";
                bottomLabel = $"-{range:N0}%";
                break;
            case PeelCurveMode.CentroidDrift:
                topLabel = $"{range:N2} mm";
                bottomLabel = "0";
                break;
            default:
                topLabel = $"{range:N1}";
                bottomLabel = "0";
                break;
        }

        var topText = new FormattedText(topLabel, System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, NormalTypeface, 9.5, PeakTextBrush);
        var bottomText = new FormattedText(bottomLabel, System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, NormalTypeface, 9.5, PeakTextBrush);
        context.DrawText(topText, new Point(padL + 2, padTop + 1));
        context.DrawText(bottomText, new Point(padL + 2, padBtm - bottomText.Height - 1));

        // The series itself
        switch (mode)
        {
            case PeelCurveMode.AreaDelta:
                context.DrawGeometry(null, new Pen(RiseBrush, _seriesBarWidth), GetSeriesGeometry(0));
                context.DrawGeometry(null, new Pen(DropBrush, _seriesBarWidth), GetSeriesGeometry(1));
                break;
            case PeelCurveMode.CentroidDrift:
                context.DrawGeometry(DriftFillBrush, null, GetSeriesGeometry(0));
                context.DrawGeometry(null, DriftPen, GetSeriesGeometry(1));
                break;
            default:
                for (var level = 0; level < RiskBrushes.Length; level++)
                {
                    context.DrawGeometry(null, new Pen(RiskBrushes[level], _seriesBarWidth), GetSeriesGeometry(level));
                }

                break;
        }

        // Values beyond the range are clipped to the edge of the plot
        foreach (var layer in _seriesClipped)
        {
            double cy = isSigned && series[layer] < 0f ? padBtm : padTop;
            context.DrawEllipse(ClipMarkerBrush, null, new Point(LayerToX(layer), cy), 2.0, 2.0);
        }

        if (mode == PeelCurveMode.AreaDelta)
        {
            // The same peel spikes of the area curve
            var spikes = PeelSpikes;
            if (spikes is not null)
            {
                foreach (var spk in spikes)
                {
                    if (spk < 0 || spk >= count) continue;
                    double sx = LayerToX(spk);
                    context.DrawLine(SpikePen, new Point(sx, padTop), new Point(sx, padBtm));
                    context.DrawEllipse(SpikeDotBrush, null, new Point(sx, ValueToY(series[spk])), 2.5, 2.5);
                }
            }
        }
        else if (mode == PeelCurveMode.RiskScore)
        {
            var highRiskLayers = HighRiskLayers;
            if (highRiskLayers is not null)
            {
                int stride = Math.Max(1, highRiskLayers.Count / 300);
                for (var i = 0; i < highRiskLayers.Count; i += stride)
                {
                    int layer = highRiskLayers[i];
                    if (layer < 0 || layer >= count) continue;

                    var level = LayerMechanicalAnalysis.GetRiskLevel(series[layer]);
                    context.DrawEllipse(RiskBrushes[(int)level], NeedleMarkerPen,
                        new Point(LayerToX(layer), ValueToY(series[layer])), 2.5, 2.5);
                }
            }
        }

        // Current layer needle line & glowing indicator
        double curX = LayerToX(curLayer);
        context.DrawLine(NeedlePen, new Point(curX, padTop - 2), new Point(curX, padBtm + 2));
        context.DrawEllipse(NeedleBrush, NeedleMarkerPen, new Point(curX, ValueToY(series[curLayer])), 3.5, 3.5);

        // Hover guide line
        if (_hoveredLayer.HasValue && _hoveredLayer.Value != curLayer)
        {
            int hLayer = _hoveredLayer.Value;
            double hx = LayerToX(hLayer);
            context.DrawLine(HoverPen, new Point(hx, padTop), new Point(hx, padBtm));
            context.DrawEllipse(Brushes.White, null, new Point(hx, ValueToY(series[hLayer])), 2.5, 2.5);
        }
    }
}
