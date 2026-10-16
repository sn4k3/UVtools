/*
 *                               The MIT License (MIT)
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so.
 */

using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace UVtools.AvaloniaControls;

public partial class AdvancedImageBox
{
    #region Minimap

    /// <summary>
    /// The reductions of the image used by the minimap, they are reduced much more than the ones of the main image.
    /// </summary>
    private readonly ReducedImageCache _minimapCache = new();

    private ImmutablePen? _minimapViewportPen;
    private IBrush? _minimapViewportFill;
    private IBrush? _minimapBackground;
    private ImmutablePen? _minimapBorderPen;

    public static readonly StyledProperty<bool> ShowMinimapProperty =
        AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(ShowMinimap));

    /// <summary>
    /// Gets or sets if the minimap is drawn: a small thumbnail of the whole image in a corner, with a rectangle over
    /// the part that is visible. Default is <c>false</c>.
    /// </summary>
    /// <remarks>
    /// <para>The minimap is drawn only while the image is zoomed in so that part of it is out of view, in
    /// <see cref="SizeModes.Normal"/> mode; it hides by itself when the whole image is in view. Clicking or dragging
    /// over it with the left mouse button or a finger centers the view on that point: the pointer events don't start a
    /// pan or a selection and don't reach the handlers of the control.</para>
    /// <para>It is the displayed image, so it follows the <see cref="ImageRotation"/>, the flips and the
    /// <see cref="PixelAspectRatio"/>. It is drawn from the cached reductions of the image, see
    /// <see cref="BitmapSamplingMode"/>, which are rebuilt only when the image changed: a 13312x5120 image takes some
    /// milliseconds to reduce the first time the minimap is drawn after each change. The
    /// <see cref="CompositeLayers"/> and the <see cref="Overlays"/> are not drawn in it.</para>
    /// </remarks>
    public bool ShowMinimap
    {
        get => GetValue(ShowMinimapProperty);
        set => SetValue(ShowMinimapProperty, value);
    }

    public static readonly StyledProperty<Corners> MinimapCornerProperty =
        AvaloniaProperty.Register<AdvancedImageBox, Corners>(nameof(MinimapCorner), Corners.BottomRight);

    /// <summary>
    /// Gets or sets the corner of the viewport where the minimap is. Default is <see cref="Corners.BottomRight"/>.
    /// </summary>
    public Corners MinimapCorner
    {
        get => GetValue(MinimapCornerProperty);
        set => SetValue(MinimapCornerProperty, value);
    }

    public static readonly StyledProperty<double> MinimapSizeProperty =
        AvaloniaProperty.Register<AdvancedImageBox, double>(nameof(MinimapSize), 160, coerce: CoerceMinimapSize);

    private static double CoerceMinimapSize(AvaloniaObject sender, double value)
    {
        return double.IsFinite(value) ? Math.Clamp(value, 32, 1024) : 160;
    }

    /// <summary>
    /// Gets or sets the size in control units of the longest side of the minimap, from 32 to 1024. Default is 160.
    /// </summary>
    public double MinimapSize
    {
        get => GetValue(MinimapSizeProperty);
        set => SetValue(MinimapSizeProperty, value);
    }

    public static readonly StyledProperty<double> MinimapOpacityProperty =
        AvaloniaProperty.Register<AdvancedImageBox, double>(nameof(MinimapOpacity), 0.9, coerce: CoerceOpacity);

    private static double CoerceOpacity(AvaloniaObject sender, double value)
    {
        return double.IsNaN(value) ? 1 : Math.Clamp(value, 0, 1);
    }

    /// <summary>
    /// Gets or sets the opacity of the minimap thumbnail, from 0 to 1. Default is 0.9. The rectangle of the visible
    /// part is always opaque.
    /// </summary>
    public double MinimapOpacity
    {
        get => GetValue(MinimapOpacityProperty);
        set => SetValue(MinimapOpacityProperty, value);
    }

    public static readonly StyledProperty<Color> MinimapViewportColorProperty =
        AvaloniaProperty.Register<AdvancedImageBox, Color>(nameof(MinimapViewportColor),
            Color.FromRgb(255, 165, 0));

    /// <summary>
    /// Gets or sets the color of the rectangle which marks the visible part of the image in the minimap. Default is
    /// orange.
    /// </summary>
    public Color MinimapViewportColor
    {
        get => GetValue(MinimapViewportColorProperty);
        set => SetValue(MinimapViewportColorProperty, value);
    }

    /// <summary>
    /// Gets the bounds of the minimap in the coordinates of the control, or an empty rectangle when it is not shown,
    /// for example while the whole image is in view. Useful to place other elements over the control without covering
    /// it.
    /// </summary>
    public Rect MinimapBounds => TryGetMinimapLayout(out var rect, out _) ? ViewportToControl(rect) : default;

    /// <summary>
    /// Gets the rectangle of the minimap in the control, and the size of the displayed image it shows.
    /// </summary>
    /// <returns><see langword="false"/> when the minimap is not shown: it is disabled, the image is not displayed in
    /// <see cref="SizeModes.Normal"/> mode, or the whole image is in view.</returns>
    private bool TryGetMinimapLayout(out Rect rect, out Size viewSize)
    {
        rect = default;
        viewSize = default;
        if (!ShowMinimap || SizeMode != SizeModes.Normal || !IsImageLoaded) return false;

        viewSize = ViewImageSize;
        if (viewSize.Width <= 0 || viewSize.Height <= 0) return false;

        var region = GetViewImageRegion();
        if (region.Width <= 0 || region.Height <= 0) return false;
        if (region.Width >= viewSize.Width - 0.5 && region.Height >= viewSize.Height - 0.5) return false;

        // The proportions of the minimap are the ones of the image on the screen
        var physicalHeight = viewSize.Height * ViewPixelAspectRatio;
        var fit = MinimapSize / Math.Max(viewSize.Width, physicalHeight);
        var size = new Size(Math.Max(1, viewSize.Width * fit), Math.Max(1, physicalHeight * fit));
        rect = new Rect(GetCornerPosition(MinimapCorner, size), size);
        return true;
    }

    private void RenderMinimap(DrawingContext context, ViewOrientation orientation, Size imageSize)
    {
        if (!TryGetMinimapLayout(out var rect, out var viewSize)) return;

        var image = DisplayImage;
        if (image is null) return;

        var region = GetViewImageRegion();
        var scaleX = rect.Width / viewSize.Width;
        var scaleY = rect.Height / viewSize.Height;
        var background = _minimapBackground ??= new ImmutableSolidColorBrush(Color.FromArgb(170, 0, 0, 0));
        var borderPen = _minimapBorderPen ??= new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(200, 255, 255, 255)), 1);

        var opacity = MinimapOpacity;
        using (context.PushOpacity(opacity))
        {
            context.DrawRectangle(background, null, rect.Inflate(2));
            using (context.PushClip(rect))
            {
                DrawImageMapped(context, image, imageSize, orientation, rect.Position, scaleX, scaleY,
                    new Rect(viewSize), rect, _minimapCache, _imageVersion, default, true);
            }

            context.DrawRectangle(null, borderPen, rect.Inflate(2));
        }

        var color = MinimapViewportColor;
        var pen = _minimapViewportPen ??= new ImmutablePen(new ImmutableSolidColorBrush(color), 1.5);
        var fill = _minimapViewportFill ??= new ImmutableSolidColorBrush(Color.FromArgb(48, color.R, color.G, color.B));
        var view = new Rect(rect.X + region.X * scaleX, rect.Y + region.Y * scaleY, region.Width * scaleX,
            region.Height * scaleY);
        context.DrawRectangle(fill, pen, view);
    }

    /// <summary>
    /// Centers the view on the point of the image shown by a point of the minimap.
    /// </summary>
    private void CenterAtMinimapPoint(Point location)
    {
        if (!TryGetMinimapLayout(out var rect, out var viewSize)) return;

        var viewPoint = new Point(
            Math.Clamp((location.X - rect.X) / rect.Width, 0, 1) * viewSize.Width,
            Math.Clamp((location.Y - rect.Y) / rect.Height, 0, 1) * viewSize.Height);
        CenterAtView(viewPoint);
    }

    #endregion
}
