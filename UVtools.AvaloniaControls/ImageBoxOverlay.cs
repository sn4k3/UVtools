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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace UVtools.AvaloniaControls;

/// <summary>
/// Determines which point of the text box of an <see cref="ImageBoxTextOverlay"/> is placed on its position.
/// </summary>
/// <remarks>The anchor is a point of the displayed text, which is always upright, it is not affected by the rotation
/// of the <see cref="AdvancedImageBox"/>.</remarks>
public enum ImageBoxTextAnchor
{
    /// <summary>The top-left corner of the text is on the position.</summary>
    TopLeft,

    /// <summary>The middle of the top edge of the text is on the position.</summary>
    Top,

    /// <summary>The top-right corner of the text is on the position.</summary>
    TopRight,

    /// <summary>The middle of the left edge of the text is on the position.</summary>
    Left,

    /// <summary>The center of the text is on the position.</summary>
    Center,

    /// <summary>The middle of the right edge of the text is on the position.</summary>
    Right,

    /// <summary>The bottom-left corner of the text is on the position.</summary>
    BottomLeft,

    /// <summary>The middle of the bottom edge of the text is on the position.</summary>
    Bottom,

    /// <summary>The bottom-right corner of the text is on the position.</summary>
    BottomRight
}

/// <summary>
/// Determines the shape of an <see cref="ImageBoxMarkerOverlay"/>.
/// </summary>
public enum ImageBoxMarkerStyle
{
    /// <summary>A horizontal and a vertical line crossing at the position, drawn with the stroke.</summary>
    Plus,

    /// <summary>Two diagonal lines crossing at the position, drawn with the stroke.</summary>
    Cross,

    /// <summary>A circle centered on the position, drawn with the stroke and the fill.</summary>
    Circle,

    /// <summary>A square centered on the position, drawn with the stroke and the fill.</summary>
    Square
}

/// <summary>
/// Maps points of the source image to the control, it is a snapshot of the state of an <see cref="AdvancedImageBox"/>
/// taken when it renders its overlays.
/// </summary>
internal readonly struct OverlayTransform : IEquatable<OverlayTransform>
{
    private readonly AdvancedImageBox.ViewOrientation _orientation;
    private readonly Size _imageSize;
    private readonly Point _origin;
    private readonly double _scaleX;
    private readonly double _scaleY;

    /// <param name="orientation">The display orientation of the image.</param>
    /// <param name="imageSize">The size of the source image.</param>
    /// <param name="origin">The position in the control of the top-left corner of the displayed image.</param>
    /// <param name="scaleX">The horizontal scale of the displayed image, in control units per image pixel.</param>
    /// <param name="scaleY">The vertical scale of the displayed image, in control units per image pixel.</param>
    public OverlayTransform(AdvancedImageBox.ViewOrientation orientation, Size imageSize, Point origin, double scaleX,
        double scaleY)
    {
        _orientation = orientation;
        _imageSize = imageSize;
        _origin = origin;
        _scaleX = scaleX;
        _scaleY = scaleY;
    }

    /// <summary>
    /// Converts a point from source image pixels to the control.
    /// </summary>
    public Point Map(Point source)
    {
        var view = _orientation.SourceToView(source, _imageSize);
        return new Point(view.X * _scaleX + _origin.X, view.Y * _scaleY + _origin.Y);
    }

    /// <summary>
    /// Converts a rectangle from source image pixels to the control, rotations are multiples of 90 degrees so it
    /// stays a rectangle.
    /// </summary>
    public Rect Map(Rect source)
    {
        var view = _orientation.SourceToView(source, _imageSize);
        return new Rect(view.Left * _scaleX + _origin.X, view.Top * _scaleY + _origin.Y, view.Width * _scaleX,
            view.Height * _scaleY);
    }

    public bool Equals(OverlayTransform other)
    {
        return _orientation.Equals(other._orientation)
               && _imageSize == other._imageSize
               && _origin == other._origin
               && _scaleX.Equals(other._scaleX)
               && _scaleY.Equals(other._scaleY);
    }

    public override bool Equals(object? obj) => obj is OverlayTransform other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_imageSize, _origin, _scaleX, _scaleY);
}

/// <summary>
/// The base of the vector items drawn by an <see cref="AdvancedImageBox"/> over its image, see
/// <see cref="AdvancedImageBox.Overlays"/>.
/// </summary>
/// <remarks>
/// <para>The geometry of an overlay is in pixels of the source image, as <see cref="double"/>, so a pixel is the unit
/// square starting at its integer coordinates and the center of the pixel at (10, 20) is (10.5, 20.5). The geometry is
/// transformed like the image, following its zoom, scroll, <see cref="AdvancedImageBox.ImageRotation"/>, flips and
/// <see cref="AdvancedImageBox.PixelAspectRatio"/>. Sizes that describe how the overlay looks, like the stroke thickness
/// and the font size, are in control units (screen pixels) and don't change with the zoom.</para>
/// <para>Changing a property redraws the control. An overlay can be in a single <see cref="AdvancedImageBox"/>, and
/// overlays must be used from the UI thread.</para>
/// <para>The overlay types are the ones of this assembly, the class can't be inherited.</para>
/// </remarks>
public abstract class ImageBoxOverlay : INotifyPropertyChanged
{
    private bool _isVisible = true;
    private double _opacity = 1;
    private object? _tag;

    private protected ImageBoxOverlay()
    {
    }

    /// <summary>
    /// The control that draws this overlay, <see langword="null"/> when it isn't in an
    /// <see cref="AdvancedImageBox.Overlays"/> collection.
    /// </summary>
    internal AdvancedImageBox? Owner { get; set; }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets if the overlay is drawn. Default is <c>true</c>.
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    /// <summary>
    /// Gets or sets the opacity of the whole overlay, from 0 (transparent) to 1 (opaque), it is applied over the alpha
    /// of its brushes. Default is 1.
    /// </summary>
    public double Opacity
    {
        get => _opacity;
        set => SetProperty(ref _opacity, double.IsNaN(value) ? 1 : Math.Clamp(value, 0, 1));
    }

    /// <summary>
    /// Gets or sets an object to associate with the overlay, it is not used by the control.
    /// </summary>
    public object? Tag
    {
        get => _tag;
        set => SetProperty(ref _tag, value);
    }

    /// <summary>
    /// Draws the overlay.
    /// </summary>
    /// <param name="context">The drawing context, in control coordinates.</param>
    /// <param name="transform">Converts the geometry from image pixels to the control.</param>
    /// <param name="clip">The area of the control where the overlay is visible, the drawing outside of it is cut.</param>
    internal abstract void Render(DrawingContext context, in OverlayTransform transform, Rect clip);

    /// <summary>
    /// Called after a property changed, to drop what was cached from it.
    /// </summary>
    private protected virtual void OnPropertyValueChanged(string? propertyName)
    {
    }

    /// <summary>
    /// Sets the value of a property and redraws the control when it changed.
    /// </summary>
    private protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyValueChanged(propertyName);
        RaisePropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Notifies that a property changed and redraws the control.
    /// </summary>
    private protected void RaisePropertyChanged(string? propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        Owner?.TriggerRender();
    }

    /// <summary>
    /// Gets if the rectangle, inflated by the half of the stroke thickness, can be seen in the clip.
    /// </summary>
    private protected static bool IsVisibleIn(Rect screenBounds, Rect clip, double padding)
    {
        return screenBounds.Inflate(padding).Intersects(clip);
    }
}

/// <summary>
/// The base of the overlays which are drawn with a stroke and, when they are closed shapes, a fill.
/// </summary>
public abstract class ImageBoxShapeOverlay : ImageBoxOverlay
{
    private IBrush? _stroke;
    private double _strokeThickness = 1;
    private IBrush? _fill;
    private IPen? _pen;

    private protected ImageBoxShapeOverlay()
    {
    }

    /// <summary>
    /// Gets or sets the brush of the outline. Default is <see langword="null"/>, no outline.
    /// </summary>
    public IBrush? Stroke
    {
        get => _stroke;
        set => SetProperty(ref _stroke, value);
    }

    /// <summary>
    /// Gets or sets the thickness of the outline in control units (screen pixels), it is the same at any zoom.
    /// Default is 1.
    /// </summary>
    public double StrokeThickness
    {
        get => _strokeThickness;
        set => SetProperty(ref _strokeThickness, double.IsFinite(value) ? Math.Max(0, value) : 1);
    }

    /// <summary>
    /// Gets or sets the brush which fills the shape. Default is <see langword="null"/>, no fill. It is ignored by the
    /// shapes that have no inside, like a <see cref="ImageBoxLineOverlay"/>.
    /// </summary>
    public IBrush? Fill
    {
        get => _fill;
        set => SetProperty(ref _fill, value);
    }

    private protected virtual PenLineCap StrokeLineCap => PenLineCap.Flat;

    private protected virtual PenLineJoin StrokeLineJoin => PenLineJoin.Round;

    /// <summary>
    /// Gets the cached pen of the outline, or <see langword="null"/> when there is no visible outline.
    /// </summary>
    private protected IPen? GetPen()
    {
        if (_stroke is null || _strokeThickness <= 0) return null;
        return _pen ??= new ImmutablePen(_stroke.ToImmutable(), _strokeThickness, null, StrokeLineCap, StrokeLineJoin);
    }

    private protected override void OnPropertyValueChanged(string? propertyName)
    {
        if (propertyName is nameof(Stroke) or nameof(StrokeThickness))
        {
            _pen = null;
        }
    }
}

/// <summary>
/// A line between two points of the image.
/// </summary>
public sealed class ImageBoxLineOverlay : ImageBoxShapeOverlay
{
    private Point _start;
    private Point _end;

    /// <summary>
    /// Initializes a new line at (0, 0).
    /// </summary>
    public ImageBoxLineOverlay()
    {
    }

    /// <summary>
    /// Initializes a new line.
    /// </summary>
    /// <param name="start">The first point, in image pixels.</param>
    /// <param name="end">The second point, in image pixels.</param>
    public ImageBoxLineOverlay(Point start, Point end)
    {
        _start = start;
        _end = end;
    }

    /// <summary>
    /// Gets or sets the first point, in image pixels.
    /// </summary>
    public Point Start
    {
        get => _start;
        set => SetProperty(ref _start, value);
    }

    /// <summary>
    /// Gets or sets the second point, in image pixels.
    /// </summary>
    public Point End
    {
        get => _end;
        set => SetProperty(ref _end, value);
    }

    internal override void Render(DrawingContext context, in OverlayTransform transform, Rect clip)
    {
        var pen = GetPen();
        if (pen is null) return;

        var start = transform.Map(_start);
        var end = transform.Map(_end);
        var bounds = new Rect(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Abs(start.X - end.X),
            Math.Abs(start.Y - end.Y));
        if (!IsVisibleIn(bounds, clip, StrokeThickness / 2 + 1)) return;

        context.DrawLine(pen, start, end);
    }
}

/// <summary>
/// An axis aligned rectangle of the image, it stays a rectangle when the image is rotated.
/// </summary>
public sealed class ImageBoxRectangleOverlay : ImageBoxShapeOverlay
{
    private Rect _bounds;

    /// <summary>
    /// Initializes a new empty rectangle.
    /// </summary>
    public ImageBoxRectangleOverlay()
    {
    }

    /// <summary>
    /// Initializes a new rectangle.
    /// </summary>
    /// <param name="bounds">The rectangle in image pixels.</param>
    public ImageBoxRectangleOverlay(Rect bounds)
    {
        _bounds = bounds;
    }

    /// <summary>
    /// Gets or sets the rectangle, in image pixels. The outline is centered on its edges.
    /// </summary>
    public Rect Bounds
    {
        get => _bounds;
        set => SetProperty(ref _bounds, value);
    }

    private protected override PenLineJoin StrokeLineJoin => PenLineJoin.Miter;

    internal override void Render(DrawingContext context, in OverlayTransform transform, Rect clip)
    {
        var pen = GetPen();
        var fill = Fill;
        if (pen is null && fill is null) return;

        var bounds = transform.Map(_bounds);
        if (!IsVisibleIn(bounds, clip, StrokeThickness / 2 + 1)) return;

        context.DrawRectangle(fill, pen, bounds);
    }
}

/// <summary>
/// An ellipse, or a circle, of the image. Its axes stay horizontal and vertical when the image is rotated.
/// </summary>
/// <remarks>The radii are in image pixels, so on an <see cref="AdvancedImageBox.PixelAspectRatio"/> other than 1 a
/// circle is displayed as the ellipse that covers the same pixels as the image.</remarks>
public sealed class ImageBoxEllipseOverlay : ImageBoxShapeOverlay
{
    private Point _center;
    private double _radiusX;
    private double _radiusY;

    /// <summary>
    /// Initializes a new ellipse with no size.
    /// </summary>
    public ImageBoxEllipseOverlay()
    {
    }

    /// <summary>
    /// Initializes a new circle.
    /// </summary>
    /// <param name="center">The center, in image pixels.</param>
    /// <param name="radius">The radius, in image pixels.</param>
    public ImageBoxEllipseOverlay(Point center, double radius) : this(center, radius, radius)
    {
    }

    /// <summary>
    /// Initializes a new ellipse.
    /// </summary>
    /// <param name="center">The center, in image pixels.</param>
    /// <param name="radiusX">The horizontal radius of the image, in image pixels.</param>
    /// <param name="radiusY">The vertical radius of the image, in image pixels.</param>
    public ImageBoxEllipseOverlay(Point center, double radiusX, double radiusY)
    {
        _center = center;
        _radiusX = Math.Max(0, radiusX);
        _radiusY = Math.Max(0, radiusY);
    }

    /// <summary>
    /// Gets or sets the center, in image pixels.
    /// </summary>
    public Point Center
    {
        get => _center;
        set => SetProperty(ref _center, value);
    }

    /// <summary>
    /// Gets or sets the horizontal radius of the image, in image pixels.
    /// </summary>
    public double RadiusX
    {
        get => _radiusX;
        set => SetProperty(ref _radiusX, double.IsFinite(value) ? Math.Max(0, value) : 0);
    }

    /// <summary>
    /// Gets or sets the vertical radius of the image, in image pixels.
    /// </summary>
    public double RadiusY
    {
        get => _radiusY;
        set => SetProperty(ref _radiusY, double.IsFinite(value) ? Math.Max(0, value) : 0);
    }

    internal override void Render(DrawingContext context, in OverlayTransform transform, Rect clip)
    {
        var pen = GetPen();
        var fill = Fill;
        if (pen is null && fill is null) return;

        var bounds = transform.Map(new Rect(_center.X - _radiusX, _center.Y - _radiusY, _radiusX * 2, _radiusY * 2));
        if (!IsVisibleIn(bounds, clip, StrokeThickness / 2 + 1)) return;

        context.DrawEllipse(fill, pen, bounds.Center, bounds.Width / 2, bounds.Height / 2);
    }
}

/// <summary>
/// A connected series of line segments of the image, closed to make a polygon when <see cref="IsClosed"/> is set.
/// </summary>
/// <remarks>The screen geometry is cached and only rebuilt when the points change or when the image moves, zooms or
/// is rotated, so a still polyline costs nothing to build per frame.</remarks>
public sealed class ImageBoxPolylineOverlay : ImageBoxShapeOverlay
{
    private Point[] _points = [];
    private bool _isClosed;
    private bool _haveBounds;
    private Rect _sourceBounds;
    private StreamGeometry? _geometry;
    private OverlayTransform _geometryTransform;
    private bool _geometryFilled;

    /// <summary>
    /// Initializes a new empty polyline.
    /// </summary>
    public ImageBoxPolylineOverlay()
    {
    }

    /// <summary>
    /// Initializes a new polyline.
    /// </summary>
    /// <param name="points">The vertices in image pixels, they are copied.</param>
    /// <param name="isClosed"><c>true</c> to join the last point to the first and make a polygon.</param>
    public ImageBoxPolylineOverlay(IEnumerable<Point> points, bool isClosed = false)
    {
        ArgumentNullException.ThrowIfNull(points);
        _points = [.. points];
        _isClosed = isClosed;
    }

    /// <summary>
    /// Gets or sets the vertices, in image pixels. The array is copied when set, assign a new array to change the
    /// points.
    /// </summary>
    public IReadOnlyList<Point> Points
    {
        get => _points;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _points = [.. value];
            OnPropertyValueChanged(nameof(Points));
            RaisePropertyChanged(nameof(Points)); // The points are replaced, not compared, so it can't use SetProperty
        }
    }

    /// <summary>
    /// Gets or sets if the last point is joined to the first, and the shape can be filled. Default is <c>false</c>.
    /// </summary>
    public bool IsClosed
    {
        get => _isClosed;
        set => SetProperty(ref _isClosed, value);
    }

    private protected override void OnPropertyValueChanged(string? propertyName)
    {
        base.OnPropertyValueChanged(propertyName);
        if (propertyName is nameof(Points) or nameof(IsClosed))
        {
            _haveBounds = false;
            _geometry = null;
        }
    }

    internal override void Render(DrawingContext context, in OverlayTransform transform, Rect clip)
    {
        var points = _points;
        if (points.Length < 2) return;

        var pen = GetPen();
        var fill = _isClosed ? Fill : null;
        if (pen is null && fill is null) return;

        if (!_haveBounds)
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var point in points)
            {
                if (point.X < minX) minX = point.X;
                if (point.X > maxX) maxX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.Y > maxY) maxY = point.Y;
            }

            _sourceBounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            _haveBounds = true;
        }

        var bounds = transform.Map(_sourceBounds);
        if (!IsVisibleIn(bounds, clip, StrokeThickness / 2 + Math.Max(1, StrokeThickness))) return; // Room for joins

        var isFilled = fill is not null;
        if (_geometry is null || _geometryFilled != isFilled || !_geometryTransform.Equals(transform))
        {
            var geometry = new StreamGeometry();
            using (var geometryContext = geometry.Open())
            {
                geometryContext.BeginFigure(transform.Map(points[0]), isFilled);
                for (var i = 1; i < points.Length; i++)
                {
                    geometryContext.LineTo(transform.Map(points[i]));
                }

                geometryContext.EndFigure(_isClosed);
            }

            _geometry = geometry;
            _geometryTransform = transform;
            _geometryFilled = isFilled;
        }

        context.DrawGeometry(fill, pen, _geometry);
    }
}

/// <summary>
/// A marker with a position in the image and a size in screen pixels, which stays the same at any zoom.
/// </summary>
public sealed class ImageBoxMarkerOverlay : ImageBoxShapeOverlay
{
    private Point _position;
    private double _size = 10;
    private ImageBoxMarkerStyle _style;

    /// <summary>
    /// Initializes a new marker at (0, 0).
    /// </summary>
    public ImageBoxMarkerOverlay()
    {
    }

    /// <summary>
    /// Initializes a new marker.
    /// </summary>
    /// <param name="position">The position of the center, in image pixels.</param>
    /// <param name="style">The shape of the marker.</param>
    /// <param name="size">The width and height of the marker, in screen pixels.</param>
    public ImageBoxMarkerOverlay(Point position, ImageBoxMarkerStyle style = ImageBoxMarkerStyle.Plus,
        double size = 10)
    {
        _position = position;
        _style = style;
        _size = double.IsFinite(size) ? Math.Max(0, size) : 10;
    }

    /// <summary>
    /// Gets or sets the position of the center of the marker, in image pixels.
    /// </summary>
    public Point Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }

    /// <summary>
    /// Gets or sets the width and height of the marker (the diameter of a circle), in screen pixels. Default is 10.
    /// </summary>
    public double Size
    {
        get => _size;
        set => SetProperty(ref _size, double.IsFinite(value) ? Math.Max(0, value) : 10);
    }

    /// <summary>
    /// Gets or sets the shape of the marker. Default is <see cref="ImageBoxMarkerStyle.Plus"/>.
    /// </summary>
    public ImageBoxMarkerStyle Style
    {
        get => _style;
        set => SetProperty(ref _style, value);
    }

    internal override void Render(DrawingContext context, in OverlayTransform transform, Rect clip)
    {
        var pen = GetPen();
        var fill = _style is ImageBoxMarkerStyle.Circle or ImageBoxMarkerStyle.Square ? Fill : null;
        if (pen is null && fill is null) return;

        var center = transform.Map(_position);
        var half = _size / 2;
        var bounds = new Rect(center.X - half, center.Y - half, _size, _size);
        if (!IsVisibleIn(bounds, clip, StrokeThickness / 2 + 1)) return;

        switch (_style)
        {
            case ImageBoxMarkerStyle.Plus when pen is not null:
                context.DrawLine(pen, new Point(bounds.Left, center.Y), new Point(bounds.Right, center.Y));
                context.DrawLine(pen, new Point(center.X, bounds.Top), new Point(center.X, bounds.Bottom));
                break;
            case ImageBoxMarkerStyle.Cross when pen is not null:
                context.DrawLine(pen, bounds.TopLeft, bounds.BottomRight);
                context.DrawLine(pen, bounds.TopRight, bounds.BottomLeft);
                break;
            case ImageBoxMarkerStyle.Circle:
                context.DrawEllipse(fill, pen, center, half, half);
                break;
            case ImageBoxMarkerStyle.Square:
                context.DrawRectangle(fill, pen, bounds);
                break;
        }
    }
}

/// <summary>
/// A text anchored to a position of the image. The position follows the image but the text is always upright and
/// has the same size at any zoom, rotation, flip and <see cref="AdvancedImageBox.PixelAspectRatio"/>.
/// </summary>
public sealed class ImageBoxTextOverlay : ImageBoxOverlay
{
    private Point _position;
    private string _text = string.Empty;
    private double _fontSize = 12;
    private IBrush? _foreground = Brushes.White;
    private IBrush? _background;
    private double _padding = 2;
    private ImageBoxTextAnchor _anchor;
    private FontFamily? _fontFamily;
    private FontWeight _fontWeight = FontWeight.Normal;
    private FormattedText? _formattedText;

    /// <summary>
    /// Initializes a new empty text at (0, 0).
    /// </summary>
    public ImageBoxTextOverlay()
    {
    }

    /// <summary>
    /// Initializes a new text.
    /// </summary>
    /// <param name="position">The position of the <paramref name="anchor"/>, in image pixels.</param>
    /// <param name="text">The text to show.</param>
    /// <param name="anchor">The point of the text which is placed on the position.</param>
    public ImageBoxTextOverlay(Point position, string text, ImageBoxTextAnchor anchor = ImageBoxTextAnchor.TopLeft)
    {
        _position = position;
        _text = text ?? string.Empty;
        _anchor = anchor;
    }

    /// <summary>
    /// Gets or sets the position of the <see cref="Anchor"/> of the text, in image pixels.
    /// </summary>
    public Point Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }

    /// <summary>
    /// Gets or sets the text, it may have several lines.
    /// </summary>
    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the size of the font in control units (screen pixels). Default is 12.
    /// </summary>
    public double FontSize
    {
        get => _fontSize;
        set => SetProperty(ref _fontSize, double.IsFinite(value) ? Math.Max(0, value) : 12);
    }

    /// <summary>
    /// Gets or sets the brush of the text. Default is <see cref="Brushes.White"/>.
    /// </summary>
    public IBrush? Foreground
    {
        get => _foreground;
        set => SetProperty(ref _foreground, value);
    }

    /// <summary>
    /// Gets or sets the brush which fills the box behind the text. Default is <see langword="null"/>, no background.
    /// </summary>
    public IBrush? Background
    {
        get => _background;
        set => SetProperty(ref _background, value);
    }

    /// <summary>
    /// Gets or sets how much the <see cref="Background"/> extends around the text, in control units. Default is 2.
    /// </summary>
    public double Padding
    {
        get => _padding;
        set => SetProperty(ref _padding, double.IsFinite(value) ? Math.Max(0, value) : 2);
    }

    /// <summary>
    /// Gets or sets which point of the text is placed on the <see cref="Position"/>. Default is
    /// <see cref="ImageBoxTextAnchor.TopLeft"/>.
    /// </summary>
    public ImageBoxTextAnchor Anchor
    {
        get => _anchor;
        set => SetProperty(ref _anchor, value);
    }

    /// <summary>
    /// Gets or sets the font family. Default is <see langword="null"/>, the default font.
    /// </summary>
    public FontFamily? FontFamily
    {
        get => _fontFamily;
        set => SetProperty(ref _fontFamily, value);
    }

    /// <summary>
    /// Gets or sets the weight of the font. Default is <see cref="Avalonia.Media.FontWeight.Normal"/>.
    /// </summary>
    public FontWeight FontWeight
    {
        get => _fontWeight;
        set => SetProperty(ref _fontWeight, value);
    }

    private protected override void OnPropertyValueChanged(string? propertyName)
    {
        // The position and the anchor don't change the text layout
        if (propertyName is not (nameof(Position) or nameof(Anchor) or nameof(Background) or nameof(Padding)))
        {
            _formattedText = null;
        }
    }

    private static (double X, double Y) GetAnchorFraction(ImageBoxTextAnchor anchor)
    {
        return anchor switch
        {
            ImageBoxTextAnchor.Top => (0.5, 0),
            ImageBoxTextAnchor.TopRight => (1, 0),
            ImageBoxTextAnchor.Left => (0, 0.5),
            ImageBoxTextAnchor.Center => (0.5, 0.5),
            ImageBoxTextAnchor.Right => (1, 0.5),
            ImageBoxTextAnchor.BottomLeft => (0, 1),
            ImageBoxTextAnchor.Bottom => (0.5, 1),
            ImageBoxTextAnchor.BottomRight => (1, 1),
            _ => (0, 0)
        };
    }

    internal override void Render(DrawingContext context, in OverlayTransform transform, Rect clip)
    {
        if (_text.Length == 0 || _fontSize <= 0 || (_foreground is null && _background is null)) return;

        var formattedText = _formattedText ??= new FormattedText(_text, CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(_fontFamily ?? FontFamily.Default, FontStyle.Normal, _fontWeight), _fontSize, _foreground);

        // Only the position is transformed, the text is laid out and drawn in control space so it stays upright
        var position = transform.Map(_position);
        var (anchorX, anchorY) = GetAnchorFraction(_anchor);
        var width = formattedText.Width;
        var height = formattedText.Height;
        var origin = new Point(Math.Round(position.X - width * anchorX), Math.Round(position.Y - height * anchorY));

        var box = new Rect(origin, new Size(width, height));
        if (_background is not null) box = box.Inflate(_padding);
        if (!box.Intersects(clip)) return;

        if (_background is not null)
        {
            context.DrawRectangle(_background, null, box);
        }

        if (_foreground is not null)
        {
            context.DrawText(formattedText, origin);
        }
    }
}

/// <summary>
/// The overlays of an <see cref="AdvancedImageBox"/>, they are drawn in the order of the collection, the last one
/// over the others.
/// </summary>
/// <remarks>Adding or removing overlays redraws the control. To change many at once use <see cref="ReplaceAll"/> or
/// <see cref="AddRange"/>, which redraw once.</remarks>
public sealed class ImageBoxOverlayCollection : Collection<ImageBoxOverlay>, INotifyCollectionChanged
{
    private readonly AdvancedImageBox _owner;

    internal ImageBoxOverlayCollection(AdvancedImageBox owner)
    {
        _owner = owner;
    }

    /// <inheritdoc />
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Adds many overlays, the control is redrawn once.
    /// </summary>
    /// <param name="overlays">The overlays to add, in drawing order.</param>
    /// <exception cref="InvalidOperationException">An overlay is already in an <see cref="AdvancedImageBox"/>, the
    /// overlays added before it remain in the collection.</exception>
    public void AddRange(IEnumerable<ImageBoxOverlay> overlays)
    {
        ArgumentNullException.ThrowIfNull(overlays);
        if (ReferenceEquals(overlays, this)) throw new ArgumentException("The collection can't add itself.", nameof(overlays));

        var changed = false;
        try
        {
            foreach (var overlay in overlays)
            {
                Attach(overlay);
                Items.Add(overlay);
                changed = true;
            }
        }
        finally
        {
            if (changed) OnReset();
        }
    }

    /// <summary>
    /// Replaces all the overlays by the given ones, the control is redrawn once. The previous overlays are removed
    /// and can be reused.
    /// </summary>
    /// <param name="overlays">The new overlays, in drawing order. It can't be this collection.</param>
    /// <exception cref="InvalidOperationException">An overlay is already in an <see cref="AdvancedImageBox"/> or is
    /// repeated, the collection is left empty.</exception>
    public void ReplaceAll(IEnumerable<ImageBoxOverlay> overlays)
    {
        ArgumentNullException.ThrowIfNull(overlays);
        if (ReferenceEquals(overlays, this)) throw new ArgumentException("The collection can't replace itself.", nameof(overlays));

        var hadItems = Items.Count > 0;
        DetachAll();
        Items.Clear();

        var added = false;
        try
        {
            foreach (var overlay in overlays)
            {
                Attach(overlay);
                Items.Add(overlay);
                added = true;
            }
        }
        catch
        {
            DetachAll();
            Items.Clear();
            OnReset();
            throw;
        }

        if (hadItems || added) OnReset();
    }

    /// <inheritdoc />
    protected override void InsertItem(int index, ImageBoxOverlay item)
    {
        Attach(item);
        base.InsertItem(index, item);
        OnChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    }

    /// <inheritdoc />
    protected override void SetItem(int index, ImageBoxOverlay item)
    {
        var oldItem = Items[index];
        if (ReferenceEquals(oldItem, item)) return;

        Attach(item);
        base.SetItem(index, item);
        oldItem.Owner = null;
        OnChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, index));
    }

    /// <inheritdoc />
    protected override void RemoveItem(int index)
    {
        var item = Items[index];
        base.RemoveItem(index);
        item.Owner = null;
        OnChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
    }

    /// <inheritdoc />
    protected override void ClearItems()
    {
        var hadItems = Items.Count > 0;
        DetachAll();
        base.ClearItems();
        if (hadItems) OnReset();
    }

    private void Attach(ImageBoxOverlay overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        if (overlay.Owner is not null)
        {
            throw new InvalidOperationException(ReferenceEquals(overlay.Owner, _owner)
                ? "The overlay is already in the collection."
                : "The overlay is already in the overlays of another AdvancedImageBox.");
        }

        overlay.Owner = _owner;
    }

    private void DetachAll()
    {
        foreach (var overlay in Items)
        {
            overlay.Owner = null;
        }
    }

    private void OnReset() => OnChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

    private void OnChanged(NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(this, e);
        _owner.TriggerRender();
    }
}
