/*
 *                               The MIT License (MIT)
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;

namespace UVtools.AvaloniaControls;

public partial class AdvancedImageBox
{
    #region Rulers and scale bar

    private const double RulerMinorTickMinSpacing = 5;
    private const double RulerHorizontalLabelSpacing = 70;
    private const double RulerVerticalLabelSpacing = 36;
    private const double RulerFontSize = 10;
    private const double ScaleBarMaxLength = 160;

    private readonly Dictionary<long, TextLayout> _rulerLabels = new();
    private TextLayout? _scaleBarText;
    private double _scaleBarTextLength;
    private string? _scaleBarTextUnit;
    private ImmutablePen? _rulerPen;
    private ImmutablePen? _rulerPointerPen;
    private ImmutablePen? _scaleBarPen;

    public static readonly StyledProperty<bool> ShowRulersProperty = AvaloniaProperty.Register<
        AdvancedImageBox,
        bool
    >(nameof(ShowRulers));

    /// <summary>
    /// Gets or sets if the rulers are drawn along the top and the left edges of the viewport, with the physical
    /// units of <see cref="PixelWidth"/>, <see cref="PixelHeight"/> and <see cref="UnitName"/>. Default is
    /// <c>false</c>.
    /// </summary>
    /// <remarks>
    /// <para>The rulers are drawn over the viewport, they don't take space: the viewport, the zoom to fit, the
    /// scrollbars, <see cref="PointToImage(Point, bool)"/> and the other coordinate members are the same with or
    /// without them, and the image can be partly under a ruler. The top ruler is <see cref="RulerSize"/> high and the
    /// left one twice as wide. The minimap and the scale bar are placed clear of them.</para>
    /// <para>The ticks are placed at nice values (1, 2, 5, 10, 20, 50...) that adapt to the zoom, with a label every
    /// few ticks. The labels are always upright, and a mark on each ruler follows the pointer.</para>
    /// <para>The rulers show the coordinates of the source image along the axes of the screen: the origin is the
    /// top-left pixel of the source <see cref="Image"/>, and the values grow in the direction of the source axis,
    /// whatever the <see cref="ImageRotation"/> and the flips. With the image upright the top ruler shows X and the
    /// left ruler Y; rotated by 90 degrees the top ruler shows the source Y and the left one the source X, and when
    /// mirrored the values decrease towards the right or the bottom, they are not renumbered. The tick of a value is
    /// where <see cref="GetOffsetPoint(Point)"/> returns the point of that coordinate. They are only drawn along the
    /// extent of the image.</para>
    /// <para>The values are in pixels when the pixel size is unknown (<see cref="PixelWidth"/> or
    /// <see cref="PixelHeight"/> is 0).</para>
    /// </remarks>
    public bool ShowRulers
    {
        get => GetValue(ShowRulersProperty);
        set => SetValue(ShowRulersProperty, value);
    }

    public static readonly StyledProperty<bool> ShowScaleBarProperty = AvaloniaProperty.Register<
        AdvancedImageBox,
        bool
    >(nameof(ShowScaleBar));

    /// <summary>
    /// Gets or sets if the scale bar is drawn in a corner of the viewport: a horizontal bar with a nice physical length
    /// (1, 2, 5, 10, 20, 50...) that adapts to the zoom. Default is <c>false</c>.
    /// </summary>
    /// <remarks>
    /// It measures the horizontal axis of the screen, so for an image rotated by 90 degrees it uses the
    /// <see cref="PixelHeight"/>. The length is in pixels when the pixel size is unknown. It uses the
    /// <see cref="RulerBackground"/> and the <see cref="RulerForeground"/>.
    /// </remarks>
    public bool ShowScaleBar
    {
        get => GetValue(ShowScaleBarProperty);
        set => SetValue(ShowScaleBarProperty, value);
    }

    public static readonly StyledProperty<Corners> ScaleBarCornerProperty =
        AvaloniaProperty.Register<AdvancedImageBox, Corners>(
            nameof(ScaleBarCorner),
            Corners.BottomLeft
        );

    /// <summary>
    /// Gets or sets the corner of the viewport where the scale bar is. Default is <see cref="Corners.BottomLeft"/>.
    /// </summary>
    public Corners ScaleBarCorner
    {
        get => GetValue(ScaleBarCornerProperty);
        set => SetValue(ScaleBarCornerProperty, value);
    }

    public static readonly StyledProperty<double> PixelWidthProperty = AvaloniaProperty.Register<
        AdvancedImageBox,
        double
    >(nameof(PixelWidth), 0, coerce: CoercePixelSize);

    public static readonly StyledProperty<double> PixelHeightProperty = AvaloniaProperty.Register<
        AdvancedImageBox,
        double
    >(nameof(PixelHeight), 0, coerce: CoercePixelSize);

    private static double CoercePixelSize(AvaloniaObject sender, double value)
    {
        return double.IsFinite(value) && value > 0 ? value : 0;
    }

    /// <summary>
    /// Gets or sets the physical width of one pixel of the source image, in the <see cref="UnitName"/>, which the
    /// rulers and the scale bar use. Default is 0, unknown: they show pixels.
    /// </summary>
    /// <remarks>
    /// <para>It doesn't change how the image is drawn, that is the <see cref="PixelAspectRatio"/>, which is the
    /// <see cref="PixelHeight"/> divided by the <see cref="PixelWidth"/> when both are known. They are independent
    /// properties on purpose, the control never changes one when the other is set, so set both from the same
    /// source. The ticks of the rulers and the length of the scale bar always follow the zoom of the displayed image,
    /// so a pixel size that doesn't match the aspect ratio only changes the values of the labels.</para>
    /// <para>Values that are not finite or not greater than zero are replaced by 0.</para>
    /// </remarks>
    public double PixelWidth
    {
        get => GetValue(PixelWidthProperty);
        set => SetValue(PixelWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the physical height of one pixel of the source image, in the <see cref="UnitName"/>. Default is 0,
    /// unknown. See <see cref="PixelWidth"/>.
    /// </summary>
    public double PixelHeight
    {
        get => GetValue(PixelHeightProperty);
        set => SetValue(PixelHeightProperty, value);
    }

    public static readonly StyledProperty<string> UnitNameProperty = AvaloniaProperty.Register<
        AdvancedImageBox,
        string
    >(nameof(UnitName), "mm");

    /// <summary>
    /// Gets or sets the name of the unit of <see cref="PixelWidth"/> and <see cref="PixelHeight"/>, shown by the scale
    /// bar. Default is "mm".
    /// </summary>
    public string UnitName
    {
        get => GetValue(UnitNameProperty);
        set => SetValue(UnitNameProperty, value);
    }

    public static readonly StyledProperty<double> RulerSizeProperty = AvaloniaProperty.Register<
        AdvancedImageBox,
        double
    >(nameof(RulerSize), 20, coerce: CoerceRulerSize);

    private static double CoerceRulerSize(AvaloniaObject sender, double value)
    {
        return double.IsFinite(value) ? Math.Clamp(value, 14, 64) : 20;
    }

    /// <summary>
    /// Gets or sets the height of the top ruler in control units, from 14 to 64, the left ruler is twice as wide.
    /// Default is 20.
    /// </summary>
    public double RulerSize
    {
        get => GetValue(RulerSizeProperty);
        set => SetValue(RulerSizeProperty, value);
    }

    public static readonly StyledProperty<IBrush?> RulerBackgroundProperty =
        AvaloniaProperty.Register<AdvancedImageBox, IBrush?>(
            nameof(RulerBackground),
            new ImmutableSolidColorBrush(Color.FromArgb(0xC8, 0x20, 0x20, 0x20))
        );

    /// <summary>
    /// Gets or sets the brush behind the rulers and the scale bar. Default is a translucent dark gray.
    /// </summary>
    public IBrush? RulerBackground
    {
        get => GetValue(RulerBackgroundProperty);
        set => SetValue(RulerBackgroundProperty, value);
    }

    public static readonly StyledProperty<IBrush?> RulerForegroundProperty =
        AvaloniaProperty.Register<AdvancedImageBox, IBrush?>(
            nameof(RulerForeground),
            new ImmutableSolidColorBrush(Color.FromArgb(0xFF, 0xE0, 0xE0, 0xE0))
        );

    /// <summary>
    /// Gets or sets the brush of the ticks, the labels and the scale bar. Default is light gray.
    /// </summary>
    public IBrush? RulerForeground
    {
        get => GetValue(RulerForegroundProperty);
        set => SetValue(RulerForegroundProperty, value);
    }

    /// <summary>
    /// Drops what was built from the brushes and colors of the decorations.
    /// </summary>
    private void ResetDecorationCaches()
    {
        ClearRulerLabels();
        _scaleBarText?.Dispose();
        _scaleBarText = null;
        _rulerPen = null;
        _rulerPointerPen = null;
        _scaleBarPen = null;
        _minimapViewportPen = null;
        _minimapViewportFill = null;
    }

    private static string FormatValue(double value)
    {
        if (Math.Abs(value) < 1e-9)
            value = 0;
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    // A text layout draws with the glyph runs it keeps, FormattedText creates them on every draw
    private static TextLayout CreateText(string text, IBrush? brush)
    {
        return new TextLayout(text, Typeface.Default, RulerFontSize, brush?.ToImmutable());
    }

    private void ClearRulerLabels()
    {
        foreach (var label in _rulerLabels.Values)
        {
            label.Dispose();
        }

        _rulerLabels.Clear();
    }

    /// <summary>
    /// Gets the label of a ruler value, cached because the same ticks are drawn on every frame.
    /// </summary>
    private TextLayout GetRulerLabel(double value, IBrush? brush)
    {
        var key = (long)Math.Round(value * 1000000);
        if (_rulerLabels.TryGetValue(key, out var text))
            return text;

        if (_rulerLabels.Count > 512)
            ClearRulerLabels();
        text = CreateText(FormatValue(value), brush);
        _rulerLabels[key] = text;
        return text;
    }

    /// <summary>
    /// Gets the smallest nice number (1, 2 or 5 times a power of ten) that is not less than <paramref name="value"/>.
    /// </summary>
    private static double NiceCeiling(double value)
    {
        if (!(value > 0) || !double.IsFinite(value))
            return 1;
        var exponent = Math.Floor(Math.Log10(value));
        var magnitude = Math.Pow(10, exponent);
        var fraction = value / magnitude;
        var nice =
            fraction <= 1.0000001 ? 1
            : fraction <= 2.0000001 ? 2
            : fraction <= 5.0000001 ? 5
            : 10;
        return nice * magnitude;
    }

    /// <summary>
    /// Gets the largest nice number (1, 2 or 5 times a power of ten) that is not greater than <paramref name="value"/>.
    /// </summary>
    private static double NiceFloor(double value)
    {
        if (!(value > 0) || !double.IsFinite(value))
            return 1;
        var exponent = Math.Floor(Math.Log10(value));
        var magnitude = Math.Pow(10, exponent);
        var fraction = value / magnitude;
        var nice =
            fraction >= 5 ? 5
            : fraction >= 2 ? 2
            : 1;
        return nice * magnitude;
    }

    /// <summary>
    /// Gets the physical size of the source pixels along an axis of the screen.
    /// </summary>
    /// <returns>The size of one pixel, or 1 when it is unknown, and if it is known.</returns>
    private (double Size, bool Known) GetPhysicalPixelSize(
        bool horizontalAxis,
        ViewOrientation orientation
    )
    {
        // The horizontal axis of the screen is the source X unless the image is rotated by 90 degrees
        var useWidth = horizontalAxis != orientation.IsTransposed;
        var size = useWidth ? PixelWidth : PixelHeight;
        return size > 0 ? (size, true) : (1, false);
    }

    private void RenderRulers(
        DrawingContext context,
        ViewOrientation orientation,
        Size imageSize,
        Point origin,
        double scaleX,
        double scaleY
    )
    {
        var viewport = Viewport;
        var thickness = RulerSize;
        var leftThickness = thickness * 2;
        if (viewport.Width <= leftThickness || viewport.Height <= thickness)
            return;

        var background = RulerBackground;
        var foreground = RulerForeground;
        var pen = _rulerPen ??= new ImmutablePen(foreground?.ToImmutable(), 1);
        var pointerPen = _rulerPointerPen ??= new ImmutablePen(Brushes.OrangeRed.ToImmutable(), 1);

        var top = new Rect(0, 0, viewport.Width, thickness);
        var left = new Rect(0, 0, leftThickness, viewport.Height);
        if (background is not null)
        {
            context.DrawRectangle(background, null, top);
            context.DrawRectangle(background, null, left);
        }

        // The mapping along each axis of the screen: the position of the source coordinate c is a + k * c, where the
        // source axis is the one which runs along the screen axis, so the flips and the rotation need no special case
        var transposed = orientation.IsTransposed;
        var mapped0 = MapSourceToControl(
            orientation,
            imageSize,
            origin,
            scaleX,
            scaleY,
            new Point(0, 0)
        );
        var unitX = MapSourceToControl(
            orientation,
            imageSize,
            origin,
            scaleX,
            scaleY,
            new Point(1, 0)
        );
        var unitY = MapSourceToControl(
            orientation,
            imageSize,
            origin,
            scaleX,
            scaleY,
            new Point(0, 1)
        );

        var horizontalExtent = transposed ? imageSize.Height : imageSize.Width;
        var verticalExtent = transposed ? imageSize.Width : imageSize.Height;
        var horizontalUnit = transposed ? unitY : unitX;
        var verticalUnit = transposed ? unitX : unitY;

        var (horizontalPixel, _) = GetPhysicalPixelSize(true, orientation);
        var (verticalPixel, _) = GetPhysicalPixelSize(false, orientation);

        using (context.PushClip(top))
        {
            DrawRulerAxis(
                context,
                true,
                mapped0.X,
                horizontalUnit.X - mapped0.X,
                horizontalExtent,
                horizontalPixel,
                viewport.Width,
                leftThickness,
                thickness,
                thickness,
                pen,
                foreground
            );
        }

        using (context.PushClip(left))
        {
            DrawRulerAxis(
                context,
                false,
                mapped0.Y,
                verticalUnit.Y - mapped0.Y,
                verticalExtent,
                verticalPixel,
                viewport.Height,
                thickness,
                leftThickness,
                thickness,
                pen,
                foreground
            );
        }

        // The corner, and the pointer
        if (background is not null)
            context.DrawRectangle(background, null, new Rect(0, 0, leftThickness, thickness));
        var pointer = PointerPosition;
        if (
            pointer.X >= 0
            && pointer.Y >= 0
            && pointer.X <= viewport.Width
            && pointer.Y <= viewport.Height
        )
        {
            context.DrawLine(
                pointerPen,
                new Point(Math.Floor(pointer.X) + 0.5, 0),
                new Point(Math.Floor(pointer.X) + 0.5, thickness)
            );
            context.DrawLine(
                pointerPen,
                new Point(0, Math.Floor(pointer.Y) + 0.5),
                new Point(leftThickness, Math.Floor(pointer.Y) + 0.5)
            );
        }
    }

    private static Point MapSourceToControl(
        ViewOrientation orientation,
        Size imageSize,
        Point origin,
        double scaleX,
        double scaleY,
        Point source
    )
    {
        var view = orientation.SourceToView(source, imageSize);
        return new Point(view.X * scaleX + origin.X, view.Y * scaleY + origin.Y);
    }

    /// <summary>
    /// Draws the ticks and the labels of one ruler.
    /// </summary>
    /// <param name="context">The drawing context, clipped to the ruler.</param>
    /// <param name="horizontal"><c>true</c> for the top ruler.</param>
    /// <param name="a">The position in the control, along the axis of the ruler, of the source coordinate 0.</param>
    /// <param name="k">The change of that position for one pixel of the source image, negative when mirrored.</param>
    /// <param name="extent">The size of the source image along the axis, in pixels.</param>
    /// <param name="pixelSize">The physical size of a pixel along the axis.</param>
    /// <param name="length">The length of the viewport along the axis.</param>
    /// <param name="startOffset">The part of the start of the axis which is covered by the other ruler.</param>
    /// <param name="thickness">The size of the ruler across the axis.</param>
    /// <param name="tickBase">The size the length of the ticks is a fraction of.</param>
    private void DrawRulerAxis(
        DrawingContext context,
        bool horizontal,
        double a,
        double k,
        double extent,
        double pixelSize,
        double length,
        double startOffset,
        double thickness,
        double tickBase,
        IPen pen,
        IBrush? foreground
    )
    {
        var absK = Math.Abs(k);
        if (absK <= 0 || extent <= 0)
            return;

        // The physical size covered by one screen unit, and the nice step which keeps the labels apart
        var valuePerUnit = pixelSize / absK;
        var labelSpacing = horizontal ? RulerHorizontalLabelSpacing : RulerVerticalLabelSpacing;
        var majorStep = NiceCeiling(valuePerUnit * labelSpacing);

        // The ticks between two labels: 10, or 5 when the step starts with a 5, and fewer if they are too close
        var mantissa = majorStep / Math.Pow(10, Math.Floor(Math.Log10(majorStep)));
        var divisions =
            mantissa > 4 ? 5
            : mantissa > 1.5 ? 4
            : 10;
        while (divisions > 1 && majorStep / divisions / valuePerUnit < RulerMinorTickMinSpacing)
        {
            divisions =
                divisions == 10 ? 5
                : divisions == 5 ? 2
                : 1;
        }

        var minorStep = majorStep / divisions;

        // The values at the ends of the visible part of the image on this axis
        var s0 = a;
        var s1 = a + k * extent;
        var visibleStart = Math.Max(Math.Min(s0, s1), 0);
        var visibleEnd = Math.Min(Math.Max(s0, s1), length);
        if (visibleEnd <= visibleStart)
            return;

        var v0 = (visibleStart - a) / k * pixelSize;
        var v1 = (visibleEnd - a) / k * pixelSize;
        var valueMin = Math.Max(0, Math.Min(v0, v1));
        var valueMax = Math.Min(extent * pixelSize, Math.Max(v0, v1));

        var first = (long)Math.Ceiling(valueMin / minorStep - 1e-9);
        var last = (long)Math.Floor(valueMax / minorStep + 1e-9);
        if (last - first > 4000)
            return; // Can't happen with a step chosen for the zoom, protects against bad sizes

        var labelOffset = 3.0;
        for (var index = first; index <= last; index++)
        {
            var value = index * minorStep;
            var position = a + value / pixelSize * k;
            if (position < 0 || position > length)
                continue;

            var major = index % divisions == 0;
            var tickLength = major ? tickBase * 0.5 : tickBase * 0.25;
            var snapped = Math.Floor(position) + 0.5;

            if (horizontal)
            {
                context.DrawLine(
                    pen,
                    new Point(snapped, thickness - tickLength),
                    new Point(snapped, thickness)
                );
            }
            else
            {
                context.DrawLine(
                    pen,
                    new Point(thickness - tickLength, snapped),
                    new Point(thickness, snapped)
                );
            }

            if (!major || position < startOffset)
                continue;

            var label = GetRulerLabel(value, foreground);
            if (horizontal)
            {
                label.Draw(context, new Point(snapped + labelOffset, 2));
            }
            else
            {
                label.Draw(context, new Point(2, snapped + 1));
            }
        }
    }

    /// <summary>
    /// Gets the box of the scale bar in the viewport, the length of its bar in control units and its text.
    /// </summary>
    /// <returns><see langword="false"/> when the scale bar is not shown.</returns>
    private bool TryGetScaleBarLayout(out Rect box, out double barLength, out TextLayout? text)
    {
        box = default;
        barLength = 0;
        text = null;
        if (!ShowScaleBar || !IsImageLoaded)
            return false;
        if (!TryGetDisplayMapping(out var orientation, out _, out _, out _, out var scaleX, out _))
            return false;

        var (pixelSize, known) = GetPhysicalPixelSize(true, orientation);
        var unitLength = scaleX / pixelSize; // Control units for one unit of the pixel size
        if (!(unitLength > 0) || !double.IsFinite(unitLength))
            return false;

        var length = NiceFloor(ScaleBarMaxLength / unitLength);
        barLength = length * unitLength;
        if (barLength < 8 || barLength > Viewport.Width)
            return false;

        var unit = known ? UnitName : "px";
        if (
            _scaleBarText is null
            || _scaleBarTextLength != length
            || !string.Equals(_scaleBarTextUnit, unit)
        )
        {
            _scaleBarText?.Dispose();
            _scaleBarText = CreateText(FormatValue(length) + " " + unit, RulerForeground);
            _scaleBarTextLength = length;
            _scaleBarTextUnit = unit;
        }

        text = _scaleBarText!;
        var boxSize = new Size(
            Math.Max(barLength, text.Width) + ScaleBarPadding * 2,
            text.Height + 10 + ScaleBarPadding * 2
        );
        box = new Rect(GetCornerPosition(ScaleBarCorner, boxSize), boxSize);
        return true;
    }

    /// <summary>
    /// Gets the bounds of the scale bar in the coordinates of the control, or an empty rectangle when it is not shown.
    /// Useful to place other elements over the control without covering it.
    /// </summary>
    public Rect ScaleBarBounds =>
        TryGetScaleBarLayout(out var box, out _, out _) ? ViewportToControl(box) : default;

    private const double ScaleBarPadding = 6;

    private void RenderScaleBar(DrawingContext context, ViewOrientation orientation)
    {
        if (!TryGetScaleBarLayout(out var box, out var barLength, out var text))
            return;

        var foreground = RulerForeground;
        const double padding = ScaleBarPadding;

        var background = RulerBackground;
        if (background is not null)
            context.DrawRectangle(background, null, box, 4, 4);

        var pen = _scaleBarPen ??= new ImmutablePen(foreground?.ToImmutable(), 2);
        var barLeft = Math.Round(box.X + padding);
        var barY = Math.Round(box.Bottom - padding - 4) + 0.5;
        var barRight = barLeft + Math.Round(barLength);
        context.DrawLine(pen, new Point(barLeft, barY), new Point(barRight, barY));
        context.DrawLine(pen, new Point(barLeft, barY - 4), new Point(barLeft, barY + 4));
        context.DrawLine(pen, new Point(barRight, barY - 4), new Point(barRight, barY + 4));
        text!.Draw(context, new Point(barLeft, box.Y + padding));
    }

    #endregion
}
