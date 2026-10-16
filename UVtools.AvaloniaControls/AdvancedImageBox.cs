/*
 *                               The MIT License (MIT)
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so.
 */
// Port from: https://github.com/cyotek/Cyotek.Windows.Forms.ImageBox to AvaloniaUI

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Color = Avalonia.Media.Color;
using Pen = Avalonia.Media.Pen;
using Point = Avalonia.Point;
using Size = Avalonia.Size;

namespace UVtools.AvaloniaControls;

[TemplatePart("PART_MainGrid", typeof(Grid))]
[TemplatePart("PART_ContentPresenter", typeof(ScrollContentPresenter))] // ViewPort
[TemplatePart("PART_HorizontalScrollBar", typeof(ScrollBar))]
[TemplatePart("PART_VerticalScrollBar", typeof(ScrollBar))]
[TemplatePart("PART_ScrollBarsSeparator", typeof(Panel))]
public partial class AdvancedImageBox : TemplatedControl, IScrollable
{
    #region Sub Classes

    /// <summary>
    /// Represents available levels of zoom in an <see cref="AdvancedImageBox"/> control
    /// </summary>
    public class ZoomLevelCollection : ICollection<int>
    {
        #region Public Class Properties

        /// <summary>
        /// Returns the default zoom levels
        /// </summary>
        public static ZoomLevelCollection Default =>
            new([
                7, 10, 15, 20, 25, 30, 50, 70, 100, 150, 200, 300, 400, 500, 600, 700, 800, 1200, 1600, 3200, 6400
            ]);

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets or sets the backing list.
        /// </summary>
        protected SortedList<int, int> List { get; set; }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="ZoomLevelCollection" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomLevelCollection"/> class.
        /// </summary>
        public ZoomLevelCollection()
        {
            List = new SortedList<int, int>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomLevelCollection"/> class.
        /// </summary>
        /// <param name="collection">The default values to populate the collection with.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if the <c>collection</c> parameter is null</exception>
        public ZoomLevelCollection(IEnumerable<int> collection)
            : this()
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            AddRange(collection);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the number of elements contained in the <see cref="ZoomLevelCollection" />.
        /// </summary>
        /// <returns>
        /// The number of elements contained in the <see cref="ZoomLevelCollection" />.
        /// </returns>
        public int Count => List.Count;

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
        /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.
        /// </returns>
        public bool IsReadOnly => false;

        #endregion

        #region Public Members

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        public void Add(int item)
        {
            List.Add(item, item);
        }

        /// <summary>
        /// Adds a range of items to the <see cref="ZoomLevelCollection"/>.
        /// </summary>
        /// <param name="collection">The items to add to the collection.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if the <c>collection</c> parameter is null.</exception>
        public void AddRange(IEnumerable<int> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            foreach (var value in collection)
            {
                Add(value);
            }
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        public void Clear()
        {
            List.Clear();
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.</returns>
        public bool Contains(int item)
        {
            return List.ContainsKey(item);
        }

        /// <summary>
        /// Copies a range of elements this collection into a destination <see cref="Array"/>.
        /// </summary>
        /// <param name="array">The <see cref="Array"/> that receives the data.</param>
        /// <param name="arrayIndex">A 64-bit integer that represents the index in the <see cref="Array"/> at which storing begins.</param>
        public void CopyTo(int[] array, int arrayIndex)
        {
            for (var i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = List.Values[i];
            }
        }

        /// <summary>
        /// Finds the index of a zoom level matching or nearest to the specified value.
        /// </summary>
        /// <param name="zoomLevel">The zoom level.</param>
        public int FindNearest(int zoomLevel)
        {
            if (Count == 0) return zoomLevel;
            var nearestValue = List.Values[0];
            var nearestDifference = Math.Abs(nearestValue - zoomLevel);
            for (var i = 1; i < Count; i++)
            {
                var value = List.Values[i];
                var difference = Math.Abs(value - zoomLevel);
                if (difference < nearestDifference)
                {
                    nearestValue = value;
                    nearestDifference = difference;
                }
            }

            return nearestValue;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
        public IEnumerator<int> GetEnumerator()
        {
            return List.Values.GetEnumerator();
        }

        /// <summary>
        /// Returns the next increased zoom level for the given current zoom.
        /// </summary>
        /// <param name="zoomLevel">The current zoom level.</param>
        /// <param name="constrainZoomLevel">When positive, constrain maximum zoom to this value</param>
        /// <returns>The next matching increased zoom level for the given current zoom if applicable, otherwise the nearest zoom.</returns>
        public int NextZoom(int zoomLevel, int constrainZoomLevel = 0)
        {
            if (Count == 0) return zoomLevel;

            var nextZoom = List.Values[Count - 1];
            for (var i = 0; i < Count; i++)
            {
                if (List.Values[i] <= zoomLevel) continue;
                nextZoom = List.Values[i];
                break;
            }

            return constrainZoomLevel > 0 ? Math.Min(nextZoom, constrainZoomLevel) : nextZoom;
        }

        /// <summary>
        /// Returns the next decreased zoom level for the given current zoom.
        /// </summary>
        /// <param name="zoomLevel">The current zoom level.</param>
        /// <param name="constrainZoomLevel">When positive, constrain minimum zoom to this value</param>
        /// <returns>The next matching decreased zoom level for the given current zoom if applicable, otherwise the nearest zoom.</returns>
        public int PreviousZoom(int zoomLevel, int constrainZoomLevel = 0)
        {
            if (Count == 0) return zoomLevel;

            var previousZoom = List.Values[0];
            for (var i = Count - 1; i >= 0; i--)
            {
                if (List.Values[i] >= zoomLevel) continue;
                previousZoom = List.Values[i];
                break;
            }

            return constrainZoomLevel > 0 ? Math.Max(previousZoom, constrainZoomLevel) : previousZoom;
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        public bool Remove(int item)
        {
            return List.Remove(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="ZoomLevelCollection"/> to a new array.
        /// </summary>
        /// <returns>An array containing copies of the elements of the <see cref="ZoomLevelCollection"/>.</returns>
        public int[] ToArray()
        {
            var results = new int[Count];
            CopyTo(results, 0);

            return results;
        }

        #endregion
    }

    /// <summary>
    /// The display transform of the image, a flip in image space followed by a rotation, and the conversions between
    /// the source space (the real image pixels) and the view space (the image as it is displayed).
    /// </summary>
    /// <remarks>
    /// <para>View space has its origin in the top-left corner of the displayed image. Its size is the source size, with
    /// the width and height swapped for a rotation of 90 degrees.</para>
    /// <para>All the rotations and flips are multiples of 90 degrees, so rectangles stay rectangles and every matrix
    /// entry is -1, 0 or 1.</para>
    /// </remarks>
    internal readonly struct ViewOrientation
    {
        private readonly ImageRotations _rotation;
        private readonly bool _flipHorizontal;
        private readonly bool _flipVertical;

        public ViewOrientation(ImageRotations rotation, bool flipHorizontal, bool flipVertical)
        {
            _rotation = rotation;
            _flipHorizontal = flipHorizontal;
            _flipVertical = flipVertical;
        }

        /// <summary>
        /// Gets if the view is the same as the source.
        /// </summary>
        public bool IsIdentity => _rotation == ImageRotations.None && !_flipHorizontal && !_flipVertical;

        /// <summary>
        /// Gets if the source axes are swapped in the view, which is a rotation of 90 degrees.
        /// </summary>
        public bool IsTransposed => _rotation is ImageRotations.Rotate90Clockwise
            or ImageRotations.Rotate90CounterClockwise;

        public bool Equals(ViewOrientation other)
        {
            return _rotation == other._rotation
                   && _flipHorizontal == other._flipHorizontal
                   && _flipVertical == other._flipVertical;
        }

        /// <summary>
        /// Gets the size of the displayed image for the given source size.
        /// </summary>
        public Size GetViewSize(Size source)
        {
            return IsTransposed ? new Size(source.Height, source.Width) : source;
        }

        /// <summary>
        /// Converts a point from source space to view space.
        /// </summary>
        public Point SourceToView(Point point, Size source)
        {
            var x = _flipHorizontal ? source.Width - point.X : point.X;
            var y = _flipVertical ? source.Height - point.Y : point.Y;

            return _rotation switch
            {
                ImageRotations.Rotate90Clockwise => new Point(source.Height - y, x),
                ImageRotations.Rotate180 => new Point(source.Width - x, source.Height - y),
                ImageRotations.Rotate90CounterClockwise => new Point(y, source.Width - x),
                _ => new Point(x, y)
            };
        }

        /// <summary>
        /// Converts a point from view space to source space.
        /// </summary>
        /// <param name="point">The point in view space.</param>
        /// <param name="source">The source size.</param>
        /// <param name="pixelAddress">
        /// When true the result is meant to address the pixel containing the point, which is the floor of its
        /// coordinates. The pixel is the one displayed at the point: along the mirrored axes it is found from the
        /// floor of the view coordinate, so a point exactly on a pixel boundary belongs to the pixel after it in view
        /// space, which is the pixel before it in source space.
        /// </param>
        public Point ViewToSource(Point point, Size source, bool pixelAddress = false)
        {
            // The view axis which feeds each source axis, and if that source axis is mirrored
            var transposed = IsTransposed;
            var viewX = transposed ? point.Y : point.X;
            var viewY = transposed ? point.X : point.Y;
            var mirroredX = (_rotation is ImageRotations.Rotate180 or ImageRotations.Rotate90CounterClockwise)
                            != _flipHorizontal;
            var mirroredY = (_rotation is ImageRotations.Rotate180 or ImageRotations.Rotate90Clockwise)
                            != _flipVertical;

            return new Point(
                mirroredX ? Mirror(viewX, source.Width, pixelAddress) : viewX,
                mirroredY ? Mirror(viewY, source.Height, pixelAddress) : viewY);
        }

        private static double Mirror(double coordinate, double size, bool pixelAddress)
        {
            if (!pixelAddress) return size - coordinate;

            // Mirror the pixel, and the position inside it, so the floor of the result is the mirrored pixel
            var pixel = Math.Floor(coordinate);
            var mirrored = size - 1 - pixel + (1 - (coordinate - pixel));
            return Math.Min(mirrored, Math.BitDecrement(size - pixel));
        }

        /// <summary>
        /// Converts a rectangle from source space to view space.
        /// </summary>
        public Rect SourceToView(Rect rect, Size source)
        {
            if (IsIdentity) return rect;
            return FromCorners(SourceToView(rect.TopLeft, source), SourceToView(rect.BottomRight, source));
        }

        /// <summary>
        /// Converts a rectangle from view space to source space.
        /// </summary>
        public Rect ViewToSource(Rect rect, Size source)
        {
            if (IsIdentity) return rect;
            return FromCorners(ViewToSource(rect.TopLeft, source), ViewToSource(rect.BottomRight, source));
        }

        /// <summary>
        /// Converts the pixel at the given position of a <paramref name="width"/> x <paramref name="height"/> image to
        /// the position of that pixel in the displayed image.
        /// </summary>
        public (int X, int Y) SourceToViewPixel(int x, int y, int width, int height)
        {
            if (_flipHorizontal) x = width - 1 - x;
            if (_flipVertical) y = height - 1 - y;

            return _rotation switch
            {
                ImageRotations.Rotate90Clockwise => (height - 1 - y, x),
                ImageRotations.Rotate180 => (width - 1 - x, height - 1 - y),
                ImageRotations.Rotate90CounterClockwise => (y, width - 1 - x),
                _ => (x, y)
            };
        }

        /// <summary>
        /// Gets the matrix which transforms a point of the source image scaled to the <paramref name="scaledSource"/>
        /// size (the local space) to the control.
        /// </summary>
        /// <param name="scaledSource">The size of the source image in local space.</param>
        /// <param name="origin">The position in the control of the top-left corner of the displayed image.</param>
        public Matrix GetMatrix(Size scaledSource, Point origin)
        {
            var flipX = _flipHorizontal ? -1.0 : 1.0;
            var flipY = _flipVertical ? -1.0 : 1.0;

            double xToX; // How the local x contributes to the view x, and so on
            double yToX;
            double xToY;
            double yToY;

            switch (_rotation)
            {
                case ImageRotations.Rotate90Clockwise:
                    xToX = 0;
                    yToX = -flipY;
                    xToY = flipX;
                    yToY = 0;
                    break;
                case ImageRotations.Rotate180:
                    xToX = -flipX;
                    yToX = 0;
                    xToY = 0;
                    yToY = -flipY;
                    break;
                case ImageRotations.Rotate90CounterClockwise:
                    xToX = 0;
                    yToX = flipY;
                    xToY = -flipX;
                    yToY = 0;
                    break;
                default:
                    xToX = flipX;
                    yToX = 0;
                    xToY = 0;
                    yToY = flipY;
                    break;
            }

            var offset = SourceToView(new Point(), scaledSource);
            return new Matrix(xToX, xToY, yToX, yToY, origin.X + offset.X, origin.Y + offset.Y);
        }

        private static Rect FromCorners(Point a, Point b)
        {
            var left = Math.Min(a.X, b.X);
            var top = Math.Min(a.Y, b.Y);
            return new Rect(left, top, Math.Max(a.X, b.X) - left, Math.Max(a.Y, b.Y) - top);
        }
    }

    #endregion

    #region BindableBase

    /// <summary>
    ///     Multicast event for property change notifications.
    /// </summary>
    private PropertyChangedEventHandler? _propertyChanged;

    public new event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            _propertyChanged -= value;
            _propertyChanged += value;
        }
        remove => _propertyChanged -= value;
    }

    protected bool RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        RaisePropertyChanged(propertyName!);
        return true;
    }

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
    }

    /// <summary>
    ///     Notifies listeners that a property value has changed.
    /// </summary>
    /// <param name="propertyName">
    ///     Name of the property used to notify listeners.  This
    ///     value is optional and can be provided automatically when invoked from compilers
    ///     that support <see cref="CallerMemberNameAttribute" />.
    /// </param>
    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        var e = new PropertyChangedEventArgs(propertyName);
        OnPropertyChanged(e);
        _propertyChanged?.Invoke(this, e);
    }

    #endregion

    #region Enums

    /// <summary>
    /// Determines the sizing mode of an image hosted in an <see cref="AdvancedImageBox" /> control.
    /// </summary>
    public enum SizeModes
    {
        /// <summary>
        /// The image is displayed according to current zoom and scroll properties.
        /// </summary>
        Normal,

        /// <summary>
        /// The image is stretched to fill the client area of the control.
        /// </summary>
        Stretch,

        /// <summary>
        /// The image is stretched to fill as much of the client area of the control as possible, whilst retaining the same aspect ratio for the width and height.
        /// </summary>
        Fit
    }

    /// <summary>
    /// Determines the bounds available when panning an image in <see cref="SizeModes.Normal"/> mode.
    /// </summary>
    public enum PanBoundsModes
    {
        /// <summary>
        /// Panning is constrained to the scaled image bounds.
        /// </summary>
        Image,

        /// <summary>
        /// Panning includes the control's <see cref="Padding"/> around the scaled image.
        /// </summary>
        Padding
    }

    /// <summary>
    /// Determines the rotation applied when displaying the image of an <see cref="AdvancedImageBox" /> control.
    /// </summary>
    /// <remarks>The rotation is a display transform, the image pixels are never modified.</remarks>
    public enum ImageRotations
    {
        /// <summary>
        /// The image is not rotated.
        /// </summary>
        None,

        /// <summary>
        /// The image is displayed rotated by 90 degrees clockwise.
        /// </summary>
        Rotate90Clockwise,

        /// <summary>
        /// The image is displayed rotated by 180 degrees.
        /// </summary>
        Rotate180,

        /// <summary>
        /// The image is displayed rotated by 90 degrees counter-clockwise.
        /// </summary>
        Rotate90CounterClockwise
    }

    /// <summary>
    /// Determines how the pixels of the image are sampled when it is drawn, see
    /// <see cref="AdvancedImageBox.BitmapSamplingMode"/>.
    /// </summary>
    public enum BitmapSamplingModes
    {
        /// <summary>
        /// Nearest neighbor when the image is zoomed in or at 100%, and smooth when it is zoomed out, which avoids the
        /// aliasing and the shimmering of a huge image drawn in a small area.
        /// </summary>
        Auto,

        /// <summary>
        /// Nearest neighbor at any zoom: every pixel of the image is a sharp block. It is the fastest, but a zoomed
        /// out image aliases.
        /// </summary>
        Nearest,

        /// <summary>
        /// Always smooth: interpolated when zoomed in, averaged when zoomed out.
        /// </summary>
        Smooth
    }

    [Flags]
    public enum MouseButtons
    {
        None = 0,
        LeftButton = 1,
        MiddleButton = 2,
        RightButton = 4
    }

    public enum MouseWheelZoomBehaviours
    {
        /// <summary>
        /// No action is performed when using the mouse wheel.
        /// </summary>
        None,

        /// <summary>
        /// Zoom in and out in a native way using the mouse wheel delta.
        /// </summary>
        ZoomNative,

        /// <summary>
        /// Zoom in and out in a native way using the mouse wheel delta, but change to tick levels when holding ALT key.
        /// </summary>
        ZoomNativeAltLevels,

        /// <summary>
        /// Zoom in and out using tick levels defined in the <see cref="AdvancedImageBox.ZoomLevels"/> collection.
        /// </summary>
        ZoomLevels,

        /// <summary>
        /// Zoom in and out using tick levels defined in the <see cref="AdvancedImageBox.ZoomLevels"/> collection, but change to native when holding ALT key.
        /// </summary>
        ZoomLevelsAltNative
    }

    /// <summary>
    /// Describes the zoom action occurring
    /// </summary>
    [Flags]
    public enum ZoomActions
    {
        /// <summary>
        /// No action.
        /// </summary>
        None = 0,

        /// <summary>
        /// The control is increasing the zoom.
        /// </summary>
        ZoomIn = 1,

        /// <summary>
        /// The control is decreasing the zoom.
        /// </summary>
        ZoomOut = 2,

        /// <summary>
        /// The control zoom was reset.
        /// </summary>
        ActualSize = 4
    }

    public enum SelectionModes
    {
        /// <summary>
        ///   No selection.
        /// </summary>
        None,

        /// <summary>
        ///   Rectangle selection.
        /// </summary>
        Rectangle,

        /// <summary>
        ///   Zoom selection.
        /// </summary>
        Zoom
    }

    #endregion

    #region UI Controls

    /// <inheritdoc />
    public Size Extent
    {
        get
        {
            var viewPort = ViewPort;
            if (viewPort is null) return default;
            var documentSize = GetDocumentSize();
            return new Size(Math.Max(viewPort.Bounds.Width, documentSize.Width),
                Math.Max(viewPort.Bounds.Height, documentSize.Height));
        }
    }

    /// <inheritdoc />
    public Vector Offset
    {
        get
        {
            var horizontalScrollBar = HorizontalScrollBar;
            if (horizontalScrollBar is null) return default;

            var verticalScrollBar = VerticalScrollBar;
            if (verticalScrollBar is null) return default;

            return new Vector(horizontalScrollBar.Value, verticalScrollBar.Value);
        }
        set
        {
            var horizontalScrollBar = HorizontalScrollBar;
            if (horizontalScrollBar is null) return;

            var verticalScrollBar = VerticalScrollBar;
            if (verticalScrollBar is null) return;

            horizontalScrollBar.Value = value.X;
            verticalScrollBar.Value = value.Y;
            RaisePropertyChanged();
            TriggerRender();
        }
    }

    /// <inheritdoc />
    public Size Viewport => ViewPort?.Bounds.Size ?? Bounds.Size;

    /// <inheritdoc />
    public bool CanHorizontallyScroll => IsHorizontalBarVisible;

    /// <inheritdoc />
    public bool CanVerticallyScroll => IsVerticalBarVisible;

    #endregion

    #region Private Members

    protected internal ScrollContentPresenter? ViewPort;
    protected internal ScrollBar? HorizontalScrollBar;
    protected internal ScrollBar? VerticalScrollBar;

    private Bitmap? _ownedImage;
    private IImage? _lastDisplayImage;
    private int _imageVersion;
    private readonly Action _grayscaleSkiaUnavailable;
    private Point _startMousePosition;
    private Vector _startScrollPosition;
    private bool _isPanning;
    private bool _isSelecting;
    private Bitmap? _trackerImage;
    private Cursor? _cursorBeforePan;
    private bool _centerImageOnNextLayout;
    private bool _canRender = true;
    private Point _pointerPosition;
    private ZoomLevelCollection _zoomLevels = ZoomLevelCollection.Default;
    private int _oldZoom = 100;
    private ViewOrientation _orientation;

    private DrawingBrush? _gridBrush;
    private Pen? _pixelGridPen;
    private Pen? _selectionBorderPen;

    private static readonly Cursor PanningCursor = new(StandardCursorType.SizeAll);

    #endregion

    #region Properties

    public static readonly DirectProperty<AdvancedImageBox, bool> CanRenderProperty =
        AvaloniaProperty.RegisterDirect<AdvancedImageBox, bool>(
            nameof(CanRender),
            o => o.CanRender);

    /// <summary>
    /// Gets or sets if control can render the image
    /// </summary>
    public bool CanRender
    {
        get => _canRender;
        set
        {
            if (!SetAndRaise(CanRenderProperty, ref _canRender, value)) return;
            if (_canRender) TriggerRender();
        }
    }

    public static readonly StyledProperty<byte> GridCellSizeProperty =
        AvaloniaProperty.Register<AdvancedImageBox, byte>(nameof(GridCellSize), 15);

    /// <summary>
    /// Gets or sets the grid cell size
    /// </summary>
    public byte GridCellSize
    {
        get => GetValue(GridCellSizeProperty);
        set => SetValue(GridCellSizeProperty, value);
    }

    public static readonly StyledProperty<ISolidColorBrush> GridColorProperty =
        AvaloniaProperty.Register<AdvancedImageBox, ISolidColorBrush>(nameof(GridColor), Brushes.Gainsboro);

    /// <summary>
    /// Gets or sets the color used to create the checkerboard style background
    /// </summary>
    public ISolidColorBrush GridColor
    {
        get => GetValue(GridColorProperty);
        set => SetValue(GridColorProperty, value);
    }

    public static readonly StyledProperty<ISolidColorBrush> GridColorAlternateProperty =
        AvaloniaProperty.Register<AdvancedImageBox, ISolidColorBrush>(nameof(GridColorAlternate), Brushes.White);

    /// <summary>
    /// Gets or sets the color used to create the checkerboard style background
    /// </summary>
    public ISolidColorBrush GridColorAlternate
    {
        get => GetValue(GridColorAlternateProperty);
        set => SetValue(GridColorAlternateProperty, value);
    }

    public static readonly StyledProperty<Bitmap?> ImageProperty =
        AvaloniaProperty.Register<AdvancedImageBox, Bitmap?>(nameof(Image));

    /// <summary>
    /// Gets or sets the image to be displayed
    /// </summary>
    /// <remarks>
    /// The <see cref="GrayscaleImage"/> is displayed instead of it while it is set, which is how the control switches
    /// between both without ever being empty: set the new one first and clear the other.
    /// </remarks>
    public Bitmap? Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public static readonly StyledProperty<GrayscaleImage?> GrayscaleImageProperty =
        AvaloniaProperty.Register<AdvancedImageBox, GrayscaleImage?>(nameof(GrayscaleImage));

    /// <summary>
    /// Gets or sets an 8-bit grayscale image to be displayed instead of the <see cref="Image"/>, optionally colored by
    /// the color map of the image, which needs a quarter of the memory of a 32-bit bitmap.
    /// </summary>
    /// <remarks>
    /// <para>All the features of the control work the same with it, such as the zoom, the rotation and the flips, the
    /// <see cref="PixelAspectRatio"/>, the selection, the overlays and the pixel grid.</para>
    /// <para>The control doesn't own the image, the caller disposes it. Call <see cref="InvalidateImage"/> after
    /// writing its pixels or changing its color map.</para>
    /// </remarks>
    public GrayscaleImage? GrayscaleImage
    {
        get => GetValue(GrayscaleImageProperty);
        set => SetValue(GrayscaleImageProperty, value);
    }

    /// <summary>
    /// Gets the image which is drawn: the <see cref="GrayscaleImage"/> when set, otherwise the <see cref="Image"/>.
    /// </summary>
    private IImage? DisplayImage => GetValue(GrayscaleImageProperty) ?? (IImage?)GetValue(ImageProperty);

    public static readonly StyledProperty<BitmapSamplingModes> BitmapSamplingModeProperty =
        AvaloniaProperty.Register<AdvancedImageBox, BitmapSamplingModes>(nameof(BitmapSamplingMode),
            BitmapSamplingModes.Nearest);

    /// <summary>
    /// Gets or sets how the pixels of the image are sampled when drawn. Default is
    /// <see cref="BitmapSamplingModes.Nearest"/>.
    /// </summary>
    /// <remarks>
    /// <para>Nearest neighbor aliases and shimmers when a large image is zoomed out, because most of its pixels are
    /// skipped. <see cref="BitmapSamplingModes.Auto"/> draws it averaged then, and still sharp when zoomed in.</para>
    /// <para>The averaging is cached: a <see cref="WriteableBitmap"/> in the Bgra8888 or Rgba8888 format is reduced by
    /// powers of two once for each content of the image, and the reduction is drawn, so panning and zooming cost the
    /// same as with <see cref="BitmapSamplingModes.Nearest"/>. The cache is built the next time the control draws
    /// after <see cref="InvalidateImage"/> or after setting the <see cref="Image"/>, which takes some milliseconds on
    /// a very large image (about 20 ms for 13312x5120), so a pixel change drawn into the bitmap needs
    /// <see cref="InvalidateImage"/> and not only <see cref="TriggerRender"/>, otherwise the reduction is stale.
    /// Other bitmaps use the smooth sampling of Avalonia, which is slow for a bitmap that changes.</para>
    /// <para>A <see cref="GrayscaleImage"/> is sampled by Skia, with mipmaps when zoomed out.</para>
    /// </remarks>
    public BitmapSamplingModes BitmapSamplingMode
    {
        get => GetValue(BitmapSamplingModeProperty);
        set => SetValue(BitmapSamplingModeProperty, value);
    }

    /// <summary>
    /// Gets the image as a writeable bitmap
    /// </summary>
    public WriteableBitmap? ImageAsWriteableBitmap => Image as WriteableBitmap;

    /// <summary>
    /// Returns true if image is loaded, otherwise false.
    /// </summary>
    /// <remarks>It is also <c>true</c> when a <see cref="GrayscaleImage"/> is displayed and the <see cref="Image"/> is
    /// <see langword="null"/>.</remarks>
    public bool IsImageLoaded => DisplayImage is not null;

    private readonly ImageBoxOverlayCollection _overlays;

    /// <summary>
    /// Gets the vector overlays drawn over the <see cref="Image"/>, in the order of the collection.
    /// </summary>
    /// <remarks>
    /// <para>The geometry of an overlay is in pixels of the source <see cref="Image"/>, whatever the
    /// <see cref="ImageRotation"/>, the flips and the <see cref="PixelAspectRatio"/>, it is transformed like the image
    /// and stays crisp at any zoom. The thickness of the strokes and the size of the texts are in control units
    /// (screen pixels), so they don't change with the zoom, and a text is always upright.</para>
    /// <para>The overlays are drawn after the image, the tracker image and the pixel grid, and before the selection,
    /// clipped to the viewport. Changing the collection or the properties of its items redraws the control, without
    /// touching the zoom or the scroll position.</para>
    /// </remarks>
    public ImageBoxOverlayCollection Overlays => _overlays;

    public static readonly StyledProperty<bool> ShowOverlaysProperty =
        AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(ShowOverlays), true);

    /// <summary>
    /// Gets or sets if the <see cref="Overlays"/> are drawn. Default is <c>true</c>.
    /// </summary>
    public bool ShowOverlays
    {
        get => GetValue(ShowOverlaysProperty);
        set => SetValue(ShowOverlaysProperty, value);
    }

    public static readonly DirectProperty<AdvancedImageBox, Bitmap?> TrackerImageProperty =
        AvaloniaProperty.RegisterDirect<AdvancedImageBox, Bitmap?>(
            nameof(TrackerImage),
            o => o.TrackerImage,
            (o, v) => o.TrackerImage = v);

    /// <summary>
    /// Gets or sets an image to follow the mouse pointer
    /// </summary>
    /// <remarks>
    /// The image is in the same space as the source <see cref="Image"/>, so it is flipped and rotated like it
    /// according to <see cref="ImageRotation"/>, <see cref="FlipHorizontal"/> and <see cref="FlipVertical"/>.
    /// </remarks>
    public Bitmap? TrackerImage
    {
        get => _trackerImage;
        set
        {
            if (!SetAndRaise(TrackerImageProperty, ref _trackerImage, value)) return;
            TriggerRender();
            RaisePropertyChanged(nameof(HaveTrackerImage));
        }
    }

    [MemberNotNullWhen(true, nameof(TrackerImage))]
    [MemberNotNullWhen(true, nameof(_trackerImage))]
    public bool HaveTrackerImage => _trackerImage is not null;

    public static readonly StyledProperty<bool> TrackerImageAutoZoomProperty =
        AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(TrackerImageAutoZoom), true);

    /// <summary>
    /// Gets or sets if the tracker image will be scaled to the current zoom
    /// </summary>
    public bool TrackerImageAutoZoom
    {
        get => GetValue(TrackerImageAutoZoomProperty);
        set => SetValue(TrackerImageAutoZoomProperty, value);
    }

    public bool IsHorizontalBarVisible
    {
        get
        {
            if (!IsImageLoaded) return false;
            if (SizeMode != SizeModes.Normal) return false;
            return GetDocumentSize().Width > Viewport.Width;
        }
    }

    public bool IsVerticalBarVisible
    {
        get
        {
            if (DisplayImage is null) return false;
            if (SizeMode != SizeModes.Normal) return false;
            return GetDocumentSize().Height > Viewport.Height;
        }
    }

    public static readonly StyledProperty<bool> ShowGridProperty =
        AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(ShowGrid), true);

    /// <summary>
    /// Gets or sets the grid visibility when reach high zoom levels
    /// </summary>
    public bool ShowGrid
    {
        get => GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public static readonly DirectProperty<AdvancedImageBox, Point> PointerPositionProperty =
        AvaloniaProperty.RegisterDirect<AdvancedImageBox, Point>(
            nameof(PointerPosition),
            o => o.PointerPosition);

    /// <summary>
    /// Gets the current pointer position
    /// </summary>
    public Point PointerPosition
    {
        get => _pointerPosition;
        private set => SetAndRaise(PointerPositionProperty, ref _pointerPosition, value);
    }

    public static readonly DirectProperty<AdvancedImageBox, bool> IsPanningProperty =
        AvaloniaProperty.RegisterDirect<AdvancedImageBox, bool>(
            nameof(IsPanning),
            o => o.IsPanning);

    /// <summary>
    /// Gets if control is currently panning
    /// </summary>
    public bool IsPanning
    {
        get => _isPanning;
        protected set
        {
            if (!SetAndRaise(IsPanningProperty, ref _isPanning, value)) return;
            _startScrollPosition = Offset;

            if (value)
            {
                _cursorBeforePan = Cursor;
                Cursor = PanningCursor;
                //this.OnPanStart(EventArgs.Empty);
            }
            else
            {
                Cursor = _cursorBeforePan;
                _cursorBeforePan = null;
                //this.OnPanEnd(EventArgs.Empty);
            }

            PseudoClasses.Set(":panning", value);
        }
    }

    public static readonly DirectProperty<AdvancedImageBox, bool> IsSelectingProperty =
        AvaloniaProperty.RegisterDirect<AdvancedImageBox, bool>(
            nameof(IsSelecting),
            o => o.IsSelecting);

    /// <summary>
    /// Gets if control is currently selecting a ROI
    /// </summary>
    public bool IsSelecting
    {
        get => _isSelecting;
        protected set
        {
            if (!SetAndRaise(IsSelectingProperty, ref _isSelecting, value)) return;
            PseudoClasses.Set(":selecting", value);
        }
    }

    /// <summary>
    /// Gets the center point of the viewport
    /// </summary>
    public Point CenterPoint
    {
        get
        {
            var viewport = GetImageViewPort();
            return viewport.Center;
        }
    }

    public static readonly StyledProperty<bool> AutoPanProperty =
        AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(AutoPan), true);

    /// <summary>
    /// Gets or sets if the control can pan with the mouse
    /// </summary>
    public bool AutoPan
    {
        get => GetValue(AutoPanProperty);
        set => SetValue(AutoPanProperty, value);
    }

    public static readonly StyledProperty<MouseButtons> PanWithMouseButtonsProperty =
        AvaloniaProperty.Register<AdvancedImageBox, MouseButtons>(nameof(PanWithMouseButtons),
            MouseButtons.LeftButton | MouseButtons.MiddleButton | MouseButtons.RightButton);

    /// <summary>
    /// Gets or sets the mouse buttons to pan the image
    /// </summary>
    public MouseButtons PanWithMouseButtons
    {
        get => GetValue(PanWithMouseButtonsProperty);
        set => SetValue(PanWithMouseButtonsProperty, value);
    }

    public static readonly StyledProperty<int> PanOffsetProperty =
        AvaloniaProperty.Register<AdvancedImageBox, int>(nameof(PanOffset), 20);

    /// <summary>
    /// Gets or sets the pan offset to displace everytime a key is pressed
    /// </summary>
    public int PanOffset
    {
        get => GetValue(PanOffsetProperty);
        set => SetValue(PanOffsetProperty, value);
    }

    public static readonly StyledProperty<bool> PanWithArrowsProperty =
        AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(PanWithArrows), true);

    /// <summary>
    /// Gets or sets if the control can pan with the keyboard arrows
    /// </summary>
    public bool PanWithArrows
    {
        get => GetValue(PanWithArrowsProperty);
        set => SetValue(PanWithArrowsProperty, value);
    }


    public static readonly StyledProperty<Key?> PanLeftKeyProperty =
        AvaloniaProperty.Register<AdvancedImageBox, Key?>(nameof(PanLeftKey));

    /// <summary>
    /// Gets or sets the key to pan left
    /// </summary>
    public Key? PanLeftKey
    {
        get => GetValue(PanLeftKeyProperty);
        set => SetValue(PanLeftKeyProperty, value);
    }

    public static readonly StyledProperty<Key?> PanUpKeyProperty =
        AvaloniaProperty.Register<AdvancedImageBox, Key?>(nameof(PanUpKey));

    /// <summary>
    /// Gets or sets the key to pan up
    /// </summary>
    public Key? PanUpKey
    {
        get => GetValue(PanUpKeyProperty);
        set => SetValue(PanUpKeyProperty, value);
    }

    public static readonly StyledProperty<Key?> PanRightKeyProperty =
        AvaloniaProperty.Register<AdvancedImageBox, Key?>(nameof(PanRightKey));

    /// <summary>
    /// Gets or sets the key to pan right
    /// </summary>
    public Key? PanRightKey
    {
        get => GetValue(PanRightKeyProperty);
        set => SetValue(PanRightKeyProperty, value);
    }

    public static readonly StyledProperty<Key?> PanDownKeyProperty =
        AvaloniaProperty.Register<AdvancedImageBox, Key?>(nameof(PanDownKey));

    /// <summary>
    /// Gets or sets the key to pan down
    /// </summary>
    public Key? PanDownKey
    {
        get => GetValue(PanDownKeyProperty);
        set => SetValue(PanDownKeyProperty, value);
    }

    public static readonly StyledProperty<MouseButtons> SelectWithMouseButtonsProperty =
        AvaloniaProperty.Register<AdvancedImageBox, MouseButtons>(nameof(SelectWithMouseButtons),
            MouseButtons.LeftButton | MouseButtons.RightButton);

    /// <summary>
    /// Gets or sets the mouse buttons to select a region on image
    /// </summary>
    public MouseButtons SelectWithMouseButtons
    {
        get => GetValue(SelectWithMouseButtonsProperty);
        set => SetValue(SelectWithMouseButtonsProperty, value);
    }

    public static readonly StyledProperty<bool> InvertMousePanProperty =
        AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(InvertMousePan), false);

    /// <summary>
    /// Gets or sets if mouse pan is inverted
    /// </summary>
    public bool InvertMousePan
    {
        get => GetValue(InvertMousePanProperty);
        set => SetValue(InvertMousePanProperty, value);
    }

    public static readonly StyledProperty<bool> AutoCenterProperty =
        AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(AutoCenter), true);

    /// <summary>
    /// Gets or sets if image is auto centered
    /// </summary>
    public bool AutoCenter
    {
        get => GetValue(AutoCenterProperty);
        set => SetValue(AutoCenterProperty, value);
    }

    public static readonly StyledProperty<PanBoundsModes> PanBoundsModeProperty =
        AvaloniaProperty.Register<AdvancedImageBox, PanBoundsModes>(nameof(PanBoundsMode));

    /// <summary>
    /// Gets or sets the bounds available when panning in <see cref="SizeModes.Normal"/> mode.
    /// </summary>
    public PanBoundsModes PanBoundsMode
    {
        get => GetValue(PanBoundsModeProperty);
        set => SetValue(PanBoundsModeProperty, value);
    }

    public static readonly StyledProperty<SizeModes> SizeModeProperty =
        AvaloniaProperty.Register<AdvancedImageBox, SizeModes>(nameof(SizeMode), SizeModes.Normal);

    /// <summary>
    /// Gets or sets the image size mode
    /// </summary>
    public SizeModes SizeMode
    {
        get => GetValue(SizeModeProperty);
        set => SetValue(SizeModeProperty, value);
    }

    private void SizeModeChanged()
    {
        var horizontalScrollBar = HorizontalScrollBar;
        if (horizontalScrollBar is null) return;

        var verticalScrollBar = VerticalScrollBar;
        if (verticalScrollBar is null) return;

        switch (SizeMode)
        {
            case SizeModes.Normal:
                horizontalScrollBar.Visibility = ScrollBarVisibility.Auto;
                verticalScrollBar.Visibility = ScrollBarVisibility.Auto;
                break;
            case SizeModes.Stretch:
            case SizeModes.Fit:
                horizontalScrollBar.Visibility = ScrollBarVisibility.Hidden;
                verticalScrollBar.Visibility = ScrollBarVisibility.Hidden;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(SizeMode), SizeMode, null);
        }
    }

    public static readonly StyledProperty<int> HorizontalScrollWithMouseFactorProperty =
        AvaloniaProperty.Register<AdvancedImageBox, int>(nameof(HorizontalScrollWithMouseFactor), 100);

    /// <summary>
    /// Gets or sets the factor over the delta to scroll horizontally with the mouse (Left and right button on supported mice).
    /// </summary>
    /// <remarks>Set to 0 to disable horizontal scroll with mouse buttons.</remarks>
    public int HorizontalScrollWithMouseFactor
    {
        get => GetValue(HorizontalScrollWithMouseFactorProperty);
        set => SetValue(HorizontalScrollWithMouseFactorProperty, value);
    }

    public static readonly StyledProperty<int> HorizontalScrollWithMouseAlternativeFactorProperty =
        AvaloniaProperty.Register<AdvancedImageBox, int>(nameof(HorizontalScrollWithMouseAlternativeFactor), 50);

    /// <summary>
    /// Gets or sets the alternative (ALT modifier) factor over the delta to scroll horizontally with the mouse (Left and right button on supported mice).
    /// </summary>
    /// <remarks>Set to 0 to disable the alternative horizontal scroll with mouse buttons.</remarks>
    public int HorizontalScrollWithMouseAlternativeFactor
    {
        get => GetValue(HorizontalScrollWithMouseAlternativeFactorProperty);
        set => SetValue(HorizontalScrollWithMouseAlternativeFactorProperty, value);
    }


    public static readonly StyledProperty<int> VerticalScrollWithMouseFactorProperty =
        AvaloniaProperty.Register<AdvancedImageBox, int>(nameof(VerticalScrollWithMouseFactor), 100);

    public static readonly StyledProperty<KeyModifiers?> VerticalScrollWithMouseWheelKeyModifierProperty =
        AvaloniaProperty.Register<AdvancedImageBox, KeyModifiers?>(nameof(VerticalScrollWithMouseWheelKeyModifier),
            KeyModifiers.Control);

    /// <summary>
    /// Gets or sets the required <see cref="KeyModifiers"/> to enable the vertical scroll with the mouse wheel.
    /// </summary>
    /// <remarks>Set <c>null</c> to disable vertical scroll with the mouse wheel.</remarks>
    public KeyModifiers? VerticalScrollWithMouseWheelKeyModifier
    {
        get => GetValue(VerticalScrollWithMouseWheelKeyModifierProperty);
        set => SetValue(VerticalScrollWithMouseWheelKeyModifierProperty, value);
    }

    /// <summary>
    /// Gets or sets the factor over the delta to scroll vertically with the mouse wheel.
    /// </summary>
    /// <remarks>Set to 0 to disable horizontal scroll with mouse buttons.</remarks>
    public int VerticalScrollWithMouseFactor
    {
        get => GetValue(VerticalScrollWithMouseFactorProperty);
        set => SetValue(VerticalScrollWithMouseFactorProperty, value);
    }

    public static readonly StyledProperty<int> VerticalScrollWithMouseAlternativeFactorProperty =
        AvaloniaProperty.Register<AdvancedImageBox, int>(nameof(VerticalScrollWithMouseAlternativeFactor), 50);

    /// <summary>
    /// Gets or sets the alternative (ALT modifier) factor over the delta to scroll vertically with the mouse wheel.
    /// </summary>
    /// <remarks>Set to 0 to disable the alternative horizontal scroll with mouse buttons.</remarks>
    public int VerticalScrollWithMouseAlternativeFactor
    {
        get => GetValue(VerticalScrollWithMouseAlternativeFactorProperty);
        set => SetValue(VerticalScrollWithMouseAlternativeFactorProperty, value);
    }

    public static readonly StyledProperty<MouseWheelZoomBehaviours> ZoomWithMouseWheelBehaviourProperty =
        AvaloniaProperty.Register<AdvancedImageBox, MouseWheelZoomBehaviours>(nameof(ZoomWithMouseWheelBehaviour),
            MouseWheelZoomBehaviours.ZoomNativeAltLevels);

    /// <summary>
    /// Gets or sets the mouse wheel behaviour.
    /// </summary>
    public MouseWheelZoomBehaviours ZoomWithMouseWheelBehaviour
    {
        get => GetValue(ZoomWithMouseWheelBehaviourProperty);
        set => SetValue(ZoomWithMouseWheelBehaviourProperty, value);
    }

    public static readonly StyledProperty<KeyModifiers> ZoomWithMouseWheelKeyModifierProperty =
        AvaloniaProperty.Register<AdvancedImageBox, KeyModifiers>(nameof(ZoomWithMouseWheelKeyModifier));

    /// <summary>
    /// Gets or sets the required <see cref="KeyModifiers"/> to work with any of <see cref="ZoomWithMouseWheelBehaviour"/> zoom behaviours.
    /// </summary>
    /// <remarks>Set <para>null</para> to ignore modifiers.</remarks>
    public KeyModifiers ZoomWithMouseWheelKeyModifier
    {
        get => GetValue(ZoomWithMouseWheelKeyModifierProperty);
        set => SetValue(ZoomWithMouseWheelKeyModifierProperty, value);
    }

    public static readonly StyledProperty<bool> ZoomWithMouseWheelStrictKeyModifierProperty =
        AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(ZoomWithMouseWheelStrictKeyModifier));

    /// <summary>
    /// Gets or sets to use strict key modifier to work with <see cref="ZoomWithMouseWheelKeyModifier"/>.
    /// When true it will check if modifiers exactly match the required modifier,
    /// otherwise it will perform a bitwise inclusion check.
    /// </summary>
    public bool ZoomWithMouseWheelStrictKeyModifier
    {
        get => GetValue(ZoomWithMouseWheelStrictKeyModifierProperty);
        set => SetValue(ZoomWithMouseWheelStrictKeyModifierProperty, value);
    }

    public static readonly StyledProperty<int> ZoomWithMouseWheelDebounceMillisecondsProperty =
        AvaloniaProperty.Register<AdvancedImageBox, int>(nameof(ZoomWithMouseWheelDebounceMilliseconds), 20);

    /// <summary>
    /// Gets or sets the debounce milliseconds to perform zoom with mouse wheel
    /// </summary>
    public int ZoomWithMouseWheelDebounceMilliseconds
    {
        get => GetValue(ZoomWithMouseWheelDebounceMillisecondsProperty);
        set => SetValue(ZoomWithMouseWheelDebounceMillisecondsProperty, value);
    }

    private ulong _lastZoomWithMouseWheelTimestamp;

    public static readonly DirectProperty<AdvancedImageBox, ZoomLevelCollection> ZoomLevelsProperty =
        AvaloniaProperty.RegisterDirect<AdvancedImageBox, ZoomLevelCollection>(
            nameof(ZoomLevels),
            o => o.ZoomLevels,
            (o, v) => o.ZoomLevels = v);

    /// <summary>
    ///   Gets or sets the zoom levels.
    /// </summary>
    /// <value>The zoom levels.</value>
    public ZoomLevelCollection ZoomLevels
    {
        get => _zoomLevels;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetAndRaise(ZoomLevelsProperty, ref _zoomLevels, value);
        }
    }

    public static readonly StyledProperty<int> MinZoomProperty =
        AvaloniaProperty.Register<AdvancedImageBox, int>(nameof(MinZoom), 10);

    /// <summary>
    /// Gets or sets the minimum possible zoom.
    /// </summary>
    /// <value>The zoom.</value>
    public int MinZoom
    {
        get => GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public static readonly StyledProperty<int> MaxZoomProperty =
        AvaloniaProperty.Register<AdvancedImageBox, int>(nameof(MaxZoom), 6400);

    /// <summary>
    /// Gets or sets the maximum possible zoom.
    /// </summary>
    /// <value>The zoom.</value>
    public int MaxZoom
    {
        get => GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    public static readonly StyledProperty<bool> ConstrainZoomOutToFitLevelProperty =
        AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(ConstrainZoomOutToFitLevel));

    /// <summary>
    /// Gets or sets if the zoom out should constrain to fit image as the lowest zoom level.
    /// </summary>
    public bool ConstrainZoomOutToFitLevel
    {
        get => GetValue(ConstrainZoomOutToFitLevelProperty);
        set => SetValue(ConstrainZoomOutToFitLevelProperty, value);
    }


    public static readonly DirectProperty<AdvancedImageBox, int> OldZoomProperty =
        AvaloniaProperty.RegisterDirect<AdvancedImageBox, int>(
            nameof(OldZoom),
            o => o.OldZoom);

    /// <summary>
    /// Gets the previous zoom value
    /// </summary>
    /// <value>The zoom.</value>
    public int OldZoom
    {
        get => _oldZoom;
        private set => SetAndRaise(OldZoomProperty, ref _oldZoom, value);
    }

    public static readonly StyledProperty<int> ZoomProperty =
        AvaloniaProperty.Register<AdvancedImageBox, int>(nameof(Zoom), 100);

    /// <summary>
    ///  Gets or sets the zoom.
    /// </summary>
    /// <value>The zoom.</value>
    public int Zoom
    {
        get => GetValue(ZoomProperty);
        set
        {
            var minZoom = Math.Min(MinZoom, MaxZoom);
            var maxZoom = Math.Max(MinZoom, MaxZoom);
            if (ConstrainZoomOutToFitLevel)
            {
                minZoom = Math.Clamp(Math.Max(ZoomLevelToFit, minZoom), minZoom, maxZoom);
            }

            var newZoom = Math.Clamp(value, minZoom, maxZoom);

            var previousZoom = Zoom;
            if (previousZoom == newZoom) return;
            OldZoom = previousZoom;
            SetValue(ZoomProperty, newZoom);
        }
    }

    /// <summary>
    /// <para>Gets if the image have zoom.</para>
    /// <para>True if zoomed in or out</para>
    /// <para>False if no zoom applied</para>
    /// </summary>
    public bool IsActualSize => Zoom == 100;

    /// <summary>
    /// Gets the zoom factor, the zoom / 100.0
    /// </summary>
    public double ZoomFactor => Zoom / 100.0;

    /// <summary>
    /// Gets the horizontal zoom factor, which is the same as <see cref="ZoomFactor"/>.
    /// </summary>
    /// <remarks>
    /// Use it to convert horizontal distances between the displayed image pixels and control units. It applies to the
    /// image as displayed, so for a rotation of 90 degrees the horizontal axis of the control is the vertical axis of
    /// the source image.
    /// </remarks>
    public double ZoomFactorX => ZoomFactor;

    /// <summary>
    /// Gets the vertical zoom factor, the <see cref="ZoomFactor"/> multiplied by the <see cref="ViewPixelAspectRatio"/>.
    /// </summary>
    /// <remarks>
    /// Use it to convert vertical distances between the displayed image pixels and control units. It applies to the
    /// image as displayed, so for a rotation of 90 degrees the vertical axis of the control is the horizontal axis of
    /// the source image.
    /// </remarks>
    public double ZoomFactorY => ZoomFactor * ViewPixelAspectRatio;

    public static readonly StyledProperty<double> PixelAspectRatioProperty =
        AvaloniaProperty.Register<AdvancedImageBox, double>(nameof(PixelAspectRatio), 1.0,
            coerce: CoercePixelAspectRatio);

    private static double CoercePixelAspectRatio(AvaloniaObject sender, double value)
    {
        return double.IsFinite(value) && value > 0 ? value : 1.0;
    }

    /// <summary>
    /// Gets or sets the physical height of one pixel of the source image divided by its physical width. Default is
    /// 1.0 (square pixels).
    /// </summary>
    /// <remarks>
    /// <para>Use it to show images whose pixels are not square, for example the layers of a screen with rectangular
    /// pixels, with their physical proportions. The image is only stretched when drawing, it is never resampled.</para>
    /// <para>It always describes the pixels of the source image, whatever the <see cref="ImageRotation"/>: for a
    /// rotation of 90 degrees the pixel width and height are swapped on the screen, which the control does by itself.
    /// <see cref="ZoomFactorX"/> is the <see cref="ZoomFactor"/> and <see cref="ZoomFactorY"/> is the
    /// <see cref="ZoomFactor"/> multiplied by the <see cref="ViewPixelAspectRatio"/>.</para>
    /// <para>Values that are not finite or not greater than zero are replaced by 1.0.</para>
    /// </remarks>
    public double PixelAspectRatio
    {
        get => GetValue(PixelAspectRatioProperty);
        set => SetValue(PixelAspectRatioProperty, value);
    }

    /// <summary>
    /// Gets the on-screen height of one pixel of the displayed image divided by its on-screen width, which is the
    /// <see cref="PixelAspectRatio"/>, or its inverse when the image is rotated by 90 degrees.
    /// </summary>
    public double ViewPixelAspectRatio => _orientation.IsTransposed ? 1.0 / PixelAspectRatio : PixelAspectRatio;

    public static readonly StyledProperty<ImageRotations> ImageRotationProperty =
        AvaloniaProperty.Register<AdvancedImageBox, ImageRotations>(nameof(ImageRotation), ImageRotations.None);

    /// <summary>
    /// Gets or sets the rotation applied when displaying the image. Default is <see cref="ImageRotations.None"/>.
    /// </summary>
    /// <remarks>
    /// <para>It is a display transform: the <see cref="Image"/> is not modified or processed. The image is flipped
    /// according to <see cref="FlipHorizontal"/> and <see cref="FlipVertical"/> first, in image space, and then
    /// rotated.</para>
    /// <para>The coordinates of the public members, such as <see cref="PointToImage(Point, bool)"/>,
    /// <see cref="GetOffsetPoint(Point)"/>, <see cref="CenterAt(Point)"/> and <see cref="SelectionRegion"/>, are always
    /// the pixels of the source <see cref="Image"/>, whatever the rotation and the flips.</para>
    /// <para>When changed with the image zoomed in, the zoom is kept and the same image pixel stays at the center of
    /// the control. When the whole image was in view, it zooms to fit instead.</para>
    /// </remarks>
    public ImageRotations ImageRotation
    {
        get => GetValue(ImageRotationProperty);
        set => SetValue(ImageRotationProperty, value);
    }

    public static readonly StyledProperty<bool> FlipHorizontalProperty =
        AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(FlipHorizontal));

    /// <summary>
    /// Gets or sets if the image is displayed mirrored horizontally, in image space, before the
    /// <see cref="ImageRotation"/> is applied. Default is <c>false</c>.
    /// </summary>
    /// <remarks>It is a display transform, see <see cref="ImageRotation"/> for details.</remarks>
    public bool FlipHorizontal
    {
        get => GetValue(FlipHorizontalProperty);
        set => SetValue(FlipHorizontalProperty, value);
    }

    public static readonly StyledProperty<bool> FlipVerticalProperty =
        AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(FlipVertical));

    /// <summary>
    /// Gets or sets if the image is displayed mirrored vertically, in image space, before the
    /// <see cref="ImageRotation"/> is applied. Default is <c>false</c>.
    /// </summary>
    /// <remarks>It is a display transform, see <see cref="ImageRotation"/> for details.</remarks>
    public bool FlipVertical
    {
        get => GetValue(FlipVerticalProperty);
        set => SetValue(FlipVerticalProperty, value);
    }

    /// <summary>
    /// Gets the size of the image as displayed, before zoom: the size of the <see cref="Image"/>, with the width and
    /// height swapped when the <see cref="ImageRotation"/> is 90 degrees.
    /// </summary>
    /// <returns>An empty size when no image is loaded.</returns>
    public Size ViewImageSize => DisplayImage is { } image ? _orientation.GetViewSize(image.Size) : default;

    /// <summary>
    /// Gets the zoom to fit level which shows all the image
    /// </summary>
    public int ZoomLevelToFit
    {
        get
        {
            var image = DisplayImage;
            if (image is null) return 100;

            var viewImageSize = _orientation.GetViewSize(image.Size);
            var zoom = Math.Min(Bounds.Width / viewImageSize.Width, Bounds.Height / (viewImageSize.Height * ViewPixelAspectRatio)) * 100.0;
            return zoom <= 0 ? 100 : (int)zoom;
        }
    }

    public static readonly StyledProperty<bool> AutoZoomToFitProperty =
        AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(AutoZoomToFit));

    /// <summary>
    /// Gets or sets if the zoom level should be auto set to fit when loading a new image.
    /// </summary>
    /// <remarks>Requires <see cref="SizeMode"/> to be <see cref="SizeModes.Normal"/>.</remarks>
    public bool AutoZoomToFit
    {
        get => GetValue(AutoZoomToFitProperty);
        set => SetValue(AutoZoomToFitProperty, value);
    }


    public static readonly StyledProperty<KeyGesture[]?> ZoomInKeyGesturesProperty =
        AvaloniaProperty.Register<AdvancedImageBox, KeyGesture[]?>(nameof(ZoomInKeyGestures),
            OperatingSystem.IsMacOS()
                ? [new KeyGesture(Key.Add, KeyModifiers.Meta), new KeyGesture(Key.OemPlus, KeyModifiers.Meta)]
                : [new KeyGesture(Key.Add, KeyModifiers.Control), new KeyGesture(Key.OemPlus, KeyModifiers.Control)]
        );

    /// <summary>
    /// Gets or sets the hot key to zoom in
    /// </summary>
    public KeyGesture[]? ZoomInKeyGestures
    {
        get => GetValue(ZoomInKeyGesturesProperty);
        set => SetValue(ZoomInKeyGesturesProperty, value);
    }

    public static readonly StyledProperty<KeyGesture[]?> ZoomOutKeyGesturesProperty =
        AvaloniaProperty.Register<AdvancedImageBox, KeyGesture[]?>(nameof(ZoomOutKeyGestures),
            OperatingSystem.IsMacOS()
                ? [new KeyGesture(Key.Subtract, KeyModifiers.Meta), new KeyGesture(Key.OemMinus, KeyModifiers.Meta)]
                :
                [
                    new KeyGesture(Key.Subtract, KeyModifiers.Control),
                    new KeyGesture(Key.OemMinus, KeyModifiers.Control)
                ]
        );

    /// <summary>
    /// Gets or sets the hot key to zoom out
    /// </summary>
    public KeyGesture[]? ZoomOutKeyGestures
    {
        get => GetValue(ZoomOutKeyGesturesProperty);
        set => SetValue(ZoomOutKeyGesturesProperty, value);
    }

    public static readonly StyledProperty<KeyGesture[]?> ZoomTo100KeyGesturesProperty =
        AvaloniaProperty.Register<AdvancedImageBox, KeyGesture[]?>(nameof(ZoomTo100KeyGestures),
            OperatingSystem.IsMacOS()
                ? [new KeyGesture(Key.D0, KeyModifiers.Meta), new KeyGesture(Key.NumPad0, KeyModifiers.Meta)]
                : [new KeyGesture(Key.D0, KeyModifiers.Control), new KeyGesture(Key.NumPad0, KeyModifiers.Control)]
        );

    /// <summary>
    /// Gets or sets the hot key to zoom to 100%
    /// </summary>
    public KeyGesture[]? ZoomTo100KeyGestures
    {
        get => GetValue(ZoomTo100KeyGesturesProperty);
        set => SetValue(ZoomTo100KeyGesturesProperty, value);
    }

    public static readonly StyledProperty<KeyGesture[]?> ZoomToFitKeyGesturesProperty =
        AvaloniaProperty.Register<AdvancedImageBox, KeyGesture[]?>(nameof(ZoomToFitKeyGestures),
            OperatingSystem.IsMacOS()
                ?
                [
                    new KeyGesture(Key.D0, KeyModifiers.Meta | KeyModifiers.Alt),
                    new KeyGesture(Key.NumPad0, KeyModifiers.Meta | KeyModifiers.Alt)
                ]
                :
                [
                    new KeyGesture(Key.D0, KeyModifiers.Control | KeyModifiers.Alt),
                    new KeyGesture(Key.NumPad0, KeyModifiers.Control | KeyModifiers.Alt)
                ]
        );

    /// <summary>
    /// Gets or sets the hot key to zoom to fit
    /// </summary>
    public KeyGesture[]? ZoomToFitKeyGestures
    {
        get => GetValue(ZoomToFitKeyGesturesProperty);
        set => SetValue(ZoomToFitKeyGesturesProperty, value);
    }


    /// <summary>
    /// Gets the size of the scaled image, as it is displayed.
    /// </summary>
    /// <value>The size of the scaled image.</value>
    public Size ScaledImageSize => new(ScaledImageWidth, ScaledImageHeight);

    /// <summary>
    /// Gets the width of the scaled image, as it is displayed.
    /// </summary>
    /// <value>The width of the scaled image.</value>
    public double ScaledImageWidth => ViewImageSize.Width * ZoomFactorX;

    /// <summary>
    /// Gets the height of the scaled image, as it is displayed.
    /// </summary>
    /// <value>The height of the scaled image.</value>
    public double ScaledImageHeight => ViewImageSize.Height * ZoomFactorY;

    public static readonly StyledProperty<ISolidColorBrush> PixelGridColorProperty =
        AvaloniaProperty.Register<AdvancedImageBox, ISolidColorBrush>(nameof(PixelGridColor), Brushes.DimGray);

    /// <summary>
    /// Gets or sets the color of the pixel grid.
    /// </summary>
    /// <value>The color of the pixel grid.</value>
    public ISolidColorBrush PixelGridColor
    {
        get => GetValue(PixelGridColorProperty);
        set => SetValue(PixelGridColorProperty, value);
    }

    public static readonly StyledProperty<int> PixelGridZoomThresholdProperty =
        AvaloniaProperty.Register<AdvancedImageBox, int>(nameof(PixelGridZoomThreshold), 5);

    /// <summary>
    /// Gets or sets the minimum size of zoomed pixel's before the pixel grid will be drawn
    /// </summary>
    /// <value>The pixel grid threshold.</value>

    public int PixelGridZoomThreshold
    {
        get => GetValue(PixelGridZoomThresholdProperty);
        set => SetValue(PixelGridZoomThresholdProperty, value);
    }

    public static readonly StyledProperty<SelectionModes> SelectionModeProperty =
        AvaloniaProperty.Register<AdvancedImageBox, SelectionModes>(nameof(SelectionMode), SelectionModes.None);

    public SelectionModes SelectionMode
    {
        get => GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    public static readonly StyledProperty<ISolidColorBrush> SelectionColorProperty =
        AvaloniaProperty.Register<AdvancedImageBox, ISolidColorBrush>(nameof(SelectionColor),
            new SolidColorBrush(new Color(127, 0, 128, 255)));

    public ISolidColorBrush SelectionColor
    {
        get => GetValue(SelectionColorProperty);
        set => SetValue(SelectionColorProperty, value);
    }

    public static readonly StyledProperty<Rect> SelectionRegionProperty =
        AvaloniaProperty.Register<AdvancedImageBox, Rect>(nameof(SelectionRegion));

    /// <summary>
    /// Gets or sets the selection region, in pixels of the source <see cref="Image"/>, whatever the
    /// <see cref="ImageRotation"/> and the flips.
    /// </summary>
    public Rect SelectionRegion
    {
        get => GetValue(SelectionRegionProperty);
        set
        {
            if (SelectionRegion.Equals(value)) return;
            SetValue(SelectionRegionProperty, value);
            RaisePropertyChanged(nameof(HaveSelection));
            RaisePropertyChanged(nameof(SelectionRegionNet));
            RaisePropertyChanged(nameof(SelectionRegionPixel));
        }
    }

    /// <summary>
    /// Gets the <see cref="SelectionRegionPixel"/> in source image pixels as a <see cref="Rectangle"/>.
    /// </summary>
    public Rectangle SelectionRegionNet
    {
        get
        {
            var rect = SelectionRegionPixel;
            return new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }
    }

    /// <summary>
    /// Gets the <see cref="SelectionRegion"/> rounded outwards to whole pixels and clamped to the source
    /// <see cref="Image"/>, in source image pixels.
    /// </summary>
    public PixelRect SelectionRegionPixel => GetClampedSelectionPixelRect();

    public bool HaveSelection => SelectionRegionPixel is { Width: > 0, Height: > 0 };

    #endregion

    #region Constructor

    static AdvancedImageBox()
    {
        FocusableProperty.OverrideDefaultValue(typeof(AdvancedImageBox), true);
        AffectsRender<AdvancedImageBox>(
            AutoCenterProperty,
            PanBoundsModeProperty,
            ShowGridProperty,
            GridCellSizeProperty,
            GridColorProperty,
            GridColorAlternateProperty,
            PixelGridColorProperty,
            PixelGridZoomThresholdProperty,
            TrackerImageAutoZoomProperty,
            //ImageProperty,
            BitmapSamplingModeProperty,
            ShowOverlaysProperty,
            ShowRulersProperty,
            ShowScaleBarProperty,
            ScaleBarCornerProperty,
            PixelWidthProperty,
            PixelHeightProperty,
            UnitNameProperty,
            RulerSizeProperty,
            RulerBackgroundProperty,
            RulerForegroundProperty,
            ShowMinimapProperty,
            MinimapCornerProperty,
            MinimapSizeProperty,
            MinimapOpacityProperty,
            MinimapViewportColorProperty,
            CompareSplitPositionProperty,
            SelectionColorProperty,
            SelectionRegionProperty
        );
    }

    public AdvancedImageBox()
    {
        _overlays = new ImageBoxOverlayCollection(this);
        _compositeLayers = new ImageBoxCompositeLayerCollection(this);
        _grayscaleSkiaUnavailable = () => TriggerRender();
        InitializeGestures();
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
    }

    #endregion

    #region Overrides

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ViewPort is not null)
        {
            ViewPort.PointerPressed -= ViewPortOnPointerPressed;
            ViewPort.PointerExited -= ViewPortOnPointerExited;
            ViewPort.PointerMoved -= ViewPortOnPointerMoved;
            ViewPort.PointerWheelChanged -= ViewPortOnPointerWheelChanged;
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (HorizontalScrollBar is not null)
        {
            HorizontalScrollBar.Scroll -= ScrollBarOnScroll;
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (VerticalScrollBar is not null)
        {
            VerticalScrollBar.Scroll -= ScrollBarOnScroll;
        }

        ViewPort = e.NameScope.Find<ScrollContentPresenter>("PART_ContentPresenter")!;
        HorizontalScrollBar = e.NameScope.Find<ScrollBar>("PART_HorizontalScrollBar")!;
        VerticalScrollBar = e.NameScope.Find<ScrollBar>("PART_VerticalScrollBar")!;

        SizeModeChanged();

        ViewPort.PointerPressed += ViewPortOnPointerPressed;
        ViewPort.PointerExited += ViewPortOnPointerExited;
        ViewPort.PointerMoved += ViewPortOnPointerMoved;
        ViewPort.PointerWheelChanged += ViewPortOnPointerWheelChanged;
        EnsurePinchRecognizer();

        HorizontalScrollBar.Scroll += ScrollBarOnScroll;
        VerticalScrollBar.Scroll += ScrollBarOnScroll;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        UpdateViewPort();
        if (IsImageLoaded)
        {
            if (AutoZoomToFit)
            {
                Zoom = ZoomLevelToFit;
            }
            else if (ConstrainZoomOutToFitLevel)
            {
                var zoomLevelToFit = ZoomLevelToFit;
                if (Zoom < zoomLevelToFit)
                {
                    Zoom = zoomLevelToFit;
                }
            }

            if (PanBoundsMode == PanBoundsModes.Padding)
            {
                RequestCenterImage();
            }
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateViewPort();
        CenterImageIfReady();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        var imageChanged = ReferenceEquals(e.Property, ImageProperty);
        var displayImageChanged = false;
        IImage? previousDisplayImage = null;
        if (imageChanged)
        {
            var previousImage = e.GetOldValue<Bitmap?>();
            if (ReferenceEquals(previousImage, _ownedImage))
            {
                _ownedImage = null;
                previousImage?.Dispose();
            }
        }

        if (imageChanged || ReferenceEquals(e.Property, GrayscaleImageProperty))
        {
            // The image shown is the grayscale one when set, so changing the other doesn't change what is displayed
            previousDisplayImage = _lastDisplayImage;
            _lastDisplayImage = DisplayImage;
            displayImageChanged = !ReferenceEquals(previousDisplayImage, _lastDisplayImage);
            _imageVersion++;
            ClearReducedBitmaps();
            imageChanged = true;
        }
        else if (ReferenceEquals(e.Property, BitmapSamplingModeProperty))
        {
            ClearReducedBitmaps(); // Frees the reductions when they are not needed
            ReleaseCompositeCaches();
        }
        else if (ReferenceEquals(e.Property, RulerForegroundProperty)
                 || ReferenceEquals(e.Property, MinimapViewportColorProperty))
        {
            ResetDecorationCaches();
        }

        if (ReferenceEquals(e.Property, ImageRotationProperty)
            || ReferenceEquals(e.Property, FlipHorizontalProperty)
            || ReferenceEquals(e.Property, FlipVerticalProperty))
        {
            OrientationChanged();
            return;
        }

        if (!IsLoaded) return;
        if (imageChanged)
        {
            if (!displayImageChanged)
            {
                TriggerRender();
                return;
            }

            UpdateViewPort();

            if (!IsImageLoaded)
            {
                _centerImageOnNextLayout = false;
                SelectNone();
            }
            else
            {
                if (AutoZoomToFit)
                {
                    Zoom = ZoomLevelToFit;
                }
                else if (ConstrainZoomOutToFitLevel)
                {
                    var zoomLevelToFit = ZoomLevelToFit;
                    if (Zoom < zoomLevelToFit)
                    {
                        Zoom = zoomLevelToFit;
                    }
                }

                if (previousDisplayImage is null && PanBoundsMode == PanBoundsModes.Padding)
                {
                    RequestCenterImage();
                }
            }

            TriggerRender();

            RaisePropertyChanged(nameof(ImageAsWriteableBitmap));
            RaisePropertyChanged(nameof(IsImageLoaded));
            RaisePropertyChanged(nameof(ViewImageSize));
            RaisePropertyChanged(nameof(ScaledImageWidth));
            RaisePropertyChanged(nameof(ScaledImageHeight));
            RaisePropertyChanged(nameof(ScaledImageSize));
            RaisePropertyChanged(nameof(Extent));
            RaisePropertyChanged(nameof(ZoomLevelToFit));
        }
        else if (ReferenceEquals(e.Property, SizeModeProperty))
        {
            SizeModeChanged();
            TriggerRender();
            RaisePropertyChanged(nameof(IsHorizontalBarVisible));
            RaisePropertyChanged(nameof(IsVerticalBarVisible));
        }
        else if (ReferenceEquals(e.Property, ZoomProperty))
        {
            UpdateViewPort();
            TriggerRender();
            RaisePropertyChanged(nameof(IsHorizontalBarVisible));
            RaisePropertyChanged(nameof(IsVerticalBarVisible));
            RaisePropertyChanged(nameof(IsActualSize));
            RaisePropertyChanged(nameof(ZoomFactor));
            RaisePropertyChanged(nameof(ZoomFactorX));
            RaisePropertyChanged(nameof(ZoomFactorY));
            RaisePropertyChanged(nameof(ScaledImageWidth));
            RaisePropertyChanged(nameof(ScaledImageHeight));
            RaisePropertyChanged(nameof(ScaledImageSize));
            RaisePropertyChanged(nameof(Extent));
        }
        else if (ReferenceEquals(e.Property, PixelAspectRatioProperty))
        {
            PixelAspectRatioChanged(e.GetOldValue<double>());
        }
        else if (ReferenceEquals(e.Property, PaddingProperty)
                 || ReferenceEquals(e.Property, PanBoundsModeProperty))
        {
            UpdateViewPort();
            if (ReferenceEquals(e.Property, PanBoundsModeProperty))
            {
                if (PanBoundsMode == PanBoundsModes.Padding)
                {
                    RequestCenterImage();
                }
                else
                {
                    _centerImageOnNextLayout = false;
                }
            }

            TriggerRender();
            RaisePropertyChanged(nameof(Extent));
            RaisePropertyChanged(nameof(IsHorizontalBarVisible));
            RaisePropertyChanged(nameof(IsVerticalBarVisible));
        }
        else if (ReferenceEquals(e.Property, GridCellSizeProperty)
                 || ReferenceEquals(e.Property, GridColorProperty)
                 || ReferenceEquals(e.Property, GridColorAlternateProperty))
        {
            RebuildGridBrush();
        }
        else if (ReferenceEquals(e.Property, PixelGridColorProperty))
        {
            _pixelGridPen = null;
        }
        else if (ReferenceEquals(e.Property, SelectionColorProperty))
        {
            _selectionBorderPen = null;
        }
        else if (ReferenceEquals(e.Property, MinZoomProperty)
                 || ReferenceEquals(e.Property, MaxZoomProperty)
                 || ReferenceEquals(e.Property, ConstrainZoomOutToFitLevelProperty))
        {
            Zoom = Zoom;
        }
    }

    #endregion

    #region Brushes

    private void RebuildGridBrush()
    {
        _gridBrush = null;
    }

    private Pen EnsurePixelGridPen()
    {
        return _pixelGridPen ??= new Pen(PixelGridColor);
    }

    private Pen EnsureSelectionBorderPen()
    {
        if (_selectionBorderPen is not null) return _selectionBorderPen;
        var color = SelectionColor.Color;
        return _selectionBorderPen = new Pen(Color.FromArgb(255, color.R, color.G, color.B).ToUInt32());
    }

    private DrawingBrush EnsureGridBrush()
    {
        if (_gridBrush != null)
            return _gridBrush;

        var gridCellSize = Math.Max((byte)1, GridCellSize); // guard

        // Build a 2s x 2s checkerboard tile (top-left and bottom-right = GridColor)
        var group = new DrawingGroup
        {
            Children =
            {
                new GeometryDrawing
                    { Brush = GridColor, Geometry = new RectangleGeometry(new Rect(0, 0, gridCellSize, gridCellSize)) },
                new GeometryDrawing
                {
                    Brush = GridColorAlternate,
                    Geometry = new RectangleGeometry(new Rect(gridCellSize, 0, gridCellSize, gridCellSize))
                },
                new GeometryDrawing
                {
                    Brush = GridColorAlternate,
                    Geometry = new RectangleGeometry(new Rect(0, gridCellSize, gridCellSize, gridCellSize))
                },
                new GeometryDrawing
                {
                    Brush = GridColor,
                    Geometry = new RectangleGeometry(new Rect(gridCellSize, gridCellSize, gridCellSize, gridCellSize))
                }
            }
        };

        _gridBrush = new DrawingBrush
        {
            Drawing = group,
            Stretch = Stretch.None,
            TileMode = TileMode.Tile,
            // DestinationRect in ABSOLUTE units defines the tile size in device-independent px
            DestinationRect = new RelativeRect(0, 0, 2 * gridCellSize, 2 * gridCellSize, RelativeUnit.Absolute),
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top
        };
        return _gridBrush;
    }

    #endregion

    #region Render methods

    public void TriggerRender(bool renderOnlyCursorTracker = false)
    {
        if (!_canRender) return;
        if (renderOnlyCursorTracker && _trackerImage is null && !NeedsPointerRender) return;
        InvalidateVisual();
    }

    /// <summary>
    /// Redraws the control after the pixels of the <see cref="Image"/> were modified in place, for example the
    /// <see cref="WriteableBitmap"/> was written again.
    /// </summary>
    /// <remarks>
    /// Setting the same bitmap instance to <see cref="Image"/> raises no change, this method redraws it instead.
    /// Only the control is redrawn, the zoom, the scroll position and the centering are not touched, because the size
    /// of the image didn't change. When the image has a different size or is another bitmap, set <see cref="Image"/>.
    /// It also redraws the <see cref="GrayscaleImage"/> after its pixels or its color map changed.
    /// </remarks>
    public void InvalidateImage()
    {
        _imageVersion++;
        TriggerRender();
    }

    public override void Render(DrawingContext context)
    {
        //Debug.WriteLine($"Render: {DateTime.Now.Ticks}");
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        // Draw Grid
        if (ShowGrid && GridCellSize > 0)
        {
            var brush = EnsureGridBrush();
            // One call fills everything with the tiled checkerboard
            context.FillRectangle(brush, new Rect(bounds.Size));
        }
        /*else
        {
            context.FillRectangle(Background, new Rect(0, 0, Viewport.Width, Viewport.Height));
        }*/

        var image = DisplayImage;
        if (image is null) return;
        var orientation = _orientation;
        var imageViewPort = GetImageViewPort();
        var viewImageRegion = GetViewImageRegion();

        // Draw image
        if (imageViewPort.Width > 0 && imageViewPort.Height > 0 &&
            viewImageRegion.Width > 0 && viewImageRegion.Height > 0)
        {
            var hasCompositeLayers = _compositeLayers.Count > 0;
            if (hasCompositeLayers)
            {
                DrawCompositeLayers(context, ImageBoxCompositePlacement.Below, orientation, image.Size,
                    viewImageRegion, imageViewPort);
            }

            DrawImageLayer(context, image, image.Size, orientation, viewImageRegion, imageViewPort, _reducedCache,
                _imageVersion, default);

            if (hasCompositeLayers)
            {
                DrawCompositeLayers(context, ImageBoxCompositePlacement.Above, orientation, image.Size,
                    viewImageRegion, imageViewPort);
            }
        }

        var zoomFactorX = ZoomFactorX;
        var zoomFactorY = ZoomFactorY;

        if (HaveTrackerImage && _pointerPosition is { X: >= 0, Y: >= 0 })
        {
            if (orientation.IsIdentity)
            {
                var destSize = TrackerImageAutoZoom
                    ? new Size(_trackerImage.Size.Width * zoomFactorX, _trackerImage.Size.Height * zoomFactorY)
                    : _trackerImage.Size;

                var destPos = new Point(
                    _pointerPosition.X - destSize.Width / 2,
                    _pointerPosition.Y - destSize.Height / 2
                );
                context.DrawImage(_trackerImage!, new Rect(destPos, destSize));
            }
            else
            {
                // The tracker image is in source space, it is scaled along the axes of the source image
                var localSize = TrackerImageAutoZoom
                    ? orientation.IsTransposed
                        ? new Size(_trackerImage.Size.Width * zoomFactorY, _trackerImage.Size.Height * zoomFactorX)
                        : new Size(_trackerImage.Size.Width * zoomFactorX, _trackerImage.Size.Height * zoomFactorY)
                    : _trackerImage.Size;

                var viewSize = orientation.GetViewSize(localSize);
                var origin = new Point(
                    _pointerPosition.X - viewSize.Width / 2,
                    _pointerPosition.Y - viewSize.Height / 2);

                using (context.PushTransform(orientation.GetMatrix(localSize, origin)))
                {
                    context.DrawImage(_trackerImage!, new Rect(localSize));
                }
            }
        }

        // The pixel grid and the selection are clipped to the viewport, otherwise the parts of the image bounds outside
        // of it (e.g. the bottom of a clipped image) are drawn over the scroll bars area
        using var viewportClip = context.PushClip(new Rect(Viewport));

        // Draw pixel grid
        if (SizeMode == SizeModes.Normal && Math.Min(zoomFactorX, zoomFactorY) > PixelGridZoomThreshold &&
            imageViewPort.Width > 0 && imageViewPort.Height > 0)
        {
            var imageBounds = GetNormalImageBoundsInViewport();
            var left = imageViewPort.X;
            var top = imageViewPort.Y;
            var right = imageViewPort.Right;
            var bottom = imageViewPort.Bottom;

            var pixelGridPen = EnsurePixelGridPen();

            // First vertical line position aligned to zoom steps
            var verticalGridIndex = Math.Max(1, Math.Ceiling((left - imageBounds.Left) / zoomFactorX));
            var startX = imageBounds.Left + verticalGridIndex * zoomFactorX;
            for (var x = startX; x < right; x += zoomFactorX)
            {
                context.DrawLine(pixelGridPen, new Point(x, top), new Point(x, bottom));
            }

            // First horizontal line position aligned to zoom steps
            var horizontalGridIndex = Math.Max(1, Math.Ceiling((top - imageBounds.Top) / zoomFactorY));
            var startY = imageBounds.Top + horizontalGridIndex * zoomFactorY;
            for (var y = startY; y < bottom; y += zoomFactorY)
            {
                context.DrawLine(pixelGridPen, new Point(left, y), new Point(right, y));
            }

            context.DrawRectangle(pixelGridPen, imageBounds);
        }

        if (_overlays.Count > 0 && ShowOverlays)
        {
            RenderOverlays(context, orientation, imageViewPort);
        }

        var selectionRegion = SelectionRegion;
        if (selectionRegion != default)
        {
            var rect = GetOffsetRectangle(selectionRegion);
            var selectionColor = SelectionColor;
            context.FillRectangle(selectionColor, rect);
            context.DrawRectangle(EnsureSelectionBorderPen(), rect);
        }

        RenderDecorations(context);
    }

    /// <summary>
    /// Draws the <see cref="Overlays"/> over the image, in the viewport clip of <see cref="Render"/>.
    /// </summary>
    /// <param name="context">The drawing context.</param>
    /// <param name="orientation">The display orientation of the image.</param>
    /// <param name="imageViewPort">The visible part of the image in the control.</param>
    private void RenderOverlays(DrawingContext context, ViewOrientation orientation, Rect imageViewPort)
    {
        var image = DisplayImage;
        if (image is null) return;

        var imageSize = image.Size;
        Point origin;
        double scaleX;
        double scaleY;
        if (SizeMode == SizeModes.Normal)
        {
            // The same mapping as GetOffsetPoint()
            origin = GetNormalImageBoundsInViewport().Position;
            scaleX = ZoomFactorX;
            scaleY = ZoomFactorY;
        }
        else
        {
            // The image is stretched to the viewport rectangle, see DrawOrientedImage()
            var viewSize = orientation.GetViewSize(imageSize);
            if (imageViewPort.Width <= 0 || imageViewPort.Height <= 0 || viewSize.Width <= 0 || viewSize.Height <= 0) return;
            origin = imageViewPort.Position;
            scaleX = imageViewPort.Width / viewSize.Width;
            scaleY = imageViewPort.Height / viewSize.Height;
        }

        var transform = new OverlayTransform(orientation, imageSize, origin, scaleX, scaleY);
        var clip = new Rect(Viewport);
        var overlays = _overlays;

        for (var i = 0; i < overlays.Count; i++)
        {
            var overlay = overlays[i];
            if (!overlay.IsVisible) continue;

            var opacity = overlay.Opacity;
            if (opacity <= 0) continue;

            if (opacity < 1)
            {
                using (context.PushOpacity(opacity))
                {
                    overlay.Render(context, transform, clip);
                }
            }
            else
            {
                overlay.Render(context, transform, clip);
            }
        }
    }

    private void PixelAspectRatioChanged(double oldPixelAspectRatio)
    {
        var image = DisplayImage;
        var viewport = Viewport;
        var keepCenter = image is not null && SizeMode == SizeModes.Normal && viewport is { Width: > 0, Height: > 0 };
        var centerViewPoint = default(Point);

        if (keepCenter)
        {
            // Only the vertical axis is affected, find the image point in the center of the viewport before the change
            var padding = GetScrollablePadding();
            var oldViewPixelAspectRatio = _orientation.IsTransposed ? 1.0 / oldPixelAspectRatio : oldPixelAspectRatio;
            var oldZoomFactorY = ZoomFactorX * oldViewPixelAspectRatio;
            var oldDocumentHeight = padding.Top + ViewImageSize.Height * oldZoomFactorY + padding.Bottom;
            var oldImageTop = padding.Top - Offset.Y;
            if (AutoCenter && oldDocumentHeight < viewport.Height)
            {
                oldImageTop += (viewport.Height - oldDocumentHeight) / 2;
            }

            centerViewPoint = new Point(
                (viewport.Width / 2 - GetNormalImageBoundsInViewport().X) / ZoomFactorX,
                (viewport.Height / 2 - oldImageTop) / oldZoomFactorY);
        }

        if (ConstrainZoomOutToFitLevel)
        {
            Zoom = Zoom; // The zoom to fit level depends on the aspect ratio
        }

        UpdateViewPort();
        if (keepCenter)
        {
            CenterAtView(centerViewPoint);
        }

        TriggerRender();
        RaisePropertyChanged(nameof(ViewPixelAspectRatio));
        RaisePropertyChanged(nameof(ZoomFactorY));
        RaisePropertyChanged(nameof(ScaledImageHeight));
        RaisePropertyChanged(nameof(ScaledImageSize));
        RaisePropertyChanged(nameof(Extent));
        RaisePropertyChanged(nameof(ZoomLevelToFit));
        RaisePropertyChanged(nameof(IsHorizontalBarVisible));
        RaisePropertyChanged(nameof(IsVerticalBarVisible));
    }

    private void OrientationChanged()
    {
        var newOrientation = new ViewOrientation(ImageRotation, FlipHorizontal, FlipVertical);
        if (_orientation.Equals(newOrientation)) return;

        var viewport = Viewport;
        var keepView = IsLoaded && IsImageLoaded && SizeMode == SizeModes.Normal &&
                       viewport is { Width: > 0, Height: > 0 };
        var zoomToFit = false;
        var centerPixel = default(Point);

        if (keepView)
        {
            zoomToFit = Zoom <= ZoomLevelToFit;
            if (!zoomToFit)
            {
                // Still the previous orientation, find the source pixel in the center of the viewport
                var imageBounds = GetNormalImageBoundsInViewport();
                var centerViewPoint = new Point(
                    (viewport.Width / 2 - imageBounds.X) / ZoomFactorX,
                    (viewport.Height / 2 - imageBounds.Y) / ZoomFactorY);
                centerPixel = _orientation.ViewToSource(centerViewPoint, DisplayImage!.Size);
            }
        }

        _orientation = newOrientation;

        if (keepView)
        {
            if (zoomToFit)
            {
                Zoom = ZoomLevelToFit;
                UpdateViewPort();
            }
            else
            {
                if (ConstrainZoomOutToFitLevel)
                {
                    Zoom = Zoom; // The zoom to fit level depends on the orientation
                }

                UpdateViewPort();
                CenterAt(centerPixel);
            }
        }

        TriggerRender();
        RaisePropertyChanged(nameof(ViewImageSize));
        RaisePropertyChanged(nameof(ViewPixelAspectRatio));
        RaisePropertyChanged(nameof(ZoomFactorY));
        RaisePropertyChanged(nameof(ScaledImageWidth));
        RaisePropertyChanged(nameof(ScaledImageHeight));
        RaisePropertyChanged(nameof(ScaledImageSize));
        RaisePropertyChanged(nameof(Extent));
        RaisePropertyChanged(nameof(ZoomLevelToFit));
        RaisePropertyChanged(nameof(IsHorizontalBarVisible));
        RaisePropertyChanged(nameof(IsVerticalBarVisible));
    }

    /// <summary>
    /// Draws the visible part of the image flipped and rotated, using a transform so only that part is sampled.
    /// </summary>
    /// <param name="context">The drawing context.</param>
    /// <param name="image">The image to draw.</param>
    /// <param name="mainSize">The size of the source image of the control, which is the size of the
    /// <paramref name="image"/> unless it is a composite layer of another size, stretched over it.</param>
    /// <param name="orientation">The display orientation of the image.</param>
    /// <param name="origin">The position in the control of the top-left corner of the displayed image.</param>
    /// <param name="scaleX">Control units for each pixel of the displayed image, horizontally.</param>
    /// <param name="scaleY">Control units for each pixel of the displayed image, vertically.</param>
    /// <param name="viewImageRegion">The part of the image to draw, in view space pixels.</param>
    /// <param name="imageViewPort">Where the part of the image is drawn in the control.</param>
    /// <param name="cache">The reductions of the image.</param>
    /// <param name="imageVersion">The version of the pixels of the image, for the <paramref name="cache"/>.</param>
    /// <param name="style">How the image is combined with what is under it.</param>
    /// <param name="forceSmooth">Draws averaged whatever the <see cref="BitmapSamplingMode"/>, for the minimap.</param>
    private void DrawImageMapped(DrawingContext context, IImage image, Size mainSize, ViewOrientation orientation,
        Point origin, double scaleX, double scaleY, Rect viewImageRegion, Rect imageViewPort,
        ReducedImageCache cache, int imageVersion, in LayerStyle style, bool forceSmooth)
    {
        // A composite layer which has another size covers the main image, its pixels are not the ones of the source
        var imageSize = image.Size;
        var ratioX = imageSize.Width / mainSize.Width;
        var ratioY = imageSize.Height / mainSize.Height;

        if (orientation.IsIdentity)
        {
            if (ratioX != 1 || ratioY != 1)
            {
                viewImageRegion = new Rect(viewImageRegion.X * ratioX, viewImageRegion.Y * ratioY,
                    viewImageRegion.Width * ratioX, viewImageRegion.Height * ratioY);
                scaleX /= ratioX;
                scaleY /= ratioY;
            }

            DrawImageSampled(context, image, viewImageRegion, imageViewPort, scaleX, scaleY, cache, imageVersion,
                in style, forceSmooth);
            return;
        }

        var sourceSize = mainSize;

        // The local space is the source image scaled, before being flipped and rotated by the transform
        var localSize = orientation.IsTransposed
            ? new Size(sourceSize.Width * scaleY, sourceSize.Height * scaleX)
            : new Size(sourceSize.Width * scaleX, sourceSize.Height * scaleY);

        var sourceRegion = orientation.ViewToSource(viewImageRegion, sourceSize);
        var destRegion = orientation.ViewToSource(
            new Rect(imageViewPort.X - origin.X, imageViewPort.Y - origin.Y, imageViewPort.Width,
                imageViewPort.Height), localSize);

        var sampleScaleX = scaleX;
        var sampleScaleY = scaleY;
        if (ratioX != 1 || ratioY != 1)
        {
            sourceRegion = new Rect(sourceRegion.X * ratioX, sourceRegion.Y * ratioY, sourceRegion.Width * ratioX,
                sourceRegion.Height * ratioY);
            // The scales are along the axes of the displayed image, the ratios along the ones of the source
            sampleScaleX = scaleX / (orientation.IsTransposed ? ratioY : ratioX);
            sampleScaleY = scaleY / (orientation.IsTransposed ? ratioX : ratioY);
        }

        using (context.PushTransform(orientation.GetMatrix(localSize, origin)))
        {
            DrawImageSampled(context, image, sourceRegion, destRegion, sampleScaleX, sampleScaleY, cache,
                imageVersion, in style, forceSmooth);
        }
    }

    /// <summary>
    /// Draws a part of the image with the sampling chosen by the <see cref="BitmapSamplingMode"/>.
    /// </summary>
    /// <param name="context">The drawing context.</param>
    /// <param name="image">The image to draw.</param>
    /// <param name="sourceRect">The part of the image, in its pixels.</param>
    /// <param name="destRect">Where to draw it.</param>
    /// <param name="scaleX">The size on the screen of a pixel of the image divided by its size in pixels, horizontally.</param>
    /// <param name="scaleY">The same, vertically.</param>
    /// <param name="cache">The reductions of the image.</param>
    /// <param name="imageVersion">The version of the pixels of the image, for the <paramref name="cache"/>.</param>
    /// <param name="style">How the image is combined with what is under it, the default draws it as it is.</param>
    /// <param name="forceSmooth">Draws averaged whatever the <see cref="BitmapSamplingMode"/>.</param>
    private void DrawImageSampled(DrawingContext context, IImage image, Rect sourceRect, Rect destRect,
        double scaleX, double scaleY, ReducedImageCache cache, int imageVersion, in LayerStyle style,
        bool forceSmooth)
    {
        var minScale = Math.Min(scaleX, scaleY);
        var smooth = forceSmooth || BitmapSamplingMode switch
        {
            BitmapSamplingModes.Smooth => true,
            BitmapSamplingModes.Auto => minScale < 1,
            _ => false
        };

        if (image is GrayscaleImage grayscaleImage)
        {
            if (grayscaleImage.IsSkiaRenderingAvailable)
            {
                var grayscaleLevel = smooth ? (int)Math.Round(-Math.Log2(Math.Max(scaleX, scaleY))) : 0;
                if (grayscaleLevel >= 1 &&
                    cache.GetGrayscale(grayscaleImage, Math.Min(BitmapDownscaler.MaxLevel, grayscaleLevel)) is { } reducedGrayscale)
                {
                    // The reduction is cached like the one of a bitmap. Mipmapping the full image with Skia costs the
                    // same on every layer, and on every frame with the software renderer
                    var factor = 1 << reducedGrayscale.ReductionLevel;
                    reducedGrayscale.Draw(context,
                        new Rect(sourceRect.X / factor, sourceRect.Y / factor, sourceRect.Width / factor,
                            sourceRect.Height / factor),
                        destRect, GrayscaleSampling.Linear, _grayscaleSkiaUnavailable, in style);
                    if (reducedGrayscale.IsSkiaRenderingAvailable) return;
                }
                else
                {
                    grayscaleImage.Draw(context, sourceRect, destRect,
                        smooth ? GrayscaleSampling.Linear : GrayscaleSampling.Nearest, _grayscaleSkiaUnavailable,
                        in style);
                    if (grayscaleImage.IsSkiaRenderingAvailable) return;
                }
            }

            // Not drawn by Skia, use a 32-bit copy of it
            if (grayscaleImage.GetFallbackBitmap() is not { } fallback) return;
            image = fallback;
        }

        // The tint and the alpha from luminance only exist for the Skia path, the opacity and the blend mode are
        // available to bitmaps
        using var styleScope = new BitmapStyleScope(context, in style);

        if (!smooth)
        {
            cache.Clear();
            context.DrawImage(image, sourceRect, destRect);
            return;
        }

        // The power of two closest to the scale, the reduction is then drawn between 0.7x and 1.4x
        var maxScale = Math.Max(scaleX, scaleY);
        var level = (int)Math.Round(-Math.Log2(maxScale));
        if (level < 1)
        {
            // Less than 1.4 pixels for each screen pixel: the 2x2 taps of bilinear cover them
            cache.Clear();
            using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.LowQuality }))
            {
                context.DrawImage(image, sourceRect, destRect);
            }

            return;
        }

        if (image is WriteableBitmap writeableBitmap && BitmapDownscaler.IsSupported(writeableBitmap))
        {
            level = Math.Min(BitmapDownscaler.MaxLevel, level);
            var reduced = cache.GetBitmap(writeableBitmap, level, imageVersion);
            var factor = 1 << level;
            using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.LowQuality }))
            {
                context.DrawImage(reduced,
                    new Rect(sourceRect.X / factor, sourceRect.Y / factor, sourceRect.Width / factor,
                        sourceRect.Height / factor),
                    destRect);
            }

            return;
        }

        // Avalonia mipmaps it, which is slow for a bitmap that changes but there is nothing else for this format
        cache.Clear();
        using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.HighQuality }))
        {
            context.DrawImage(image, sourceRect, destRect);
        }
    }

    private bool UpdateViewPort()
    {
        var horizontalScrollBar = HorizontalScrollBar;
        if (horizontalScrollBar is null) return false;

        var verticalScrollBar = VerticalScrollBar;
        if (verticalScrollBar is null) return false;

        if (!IsImageLoaded || SizeMode != SizeModes.Normal)
        {
            horizontalScrollBar.Maximum = 0;
            verticalScrollBar.Maximum = 0;
            return true;
        }

        var documentSize = GetDocumentSize();
        var width = Math.Max(0, documentSize.Width - horizontalScrollBar.ViewportSize);
        var height = Math.Max(0, documentSize.Height - verticalScrollBar.ViewportSize);
        //var width = scaledImageWidth <= Viewport.Width ? Viewport.Width : scaledImageWidth;
        //var height = scaledImageHeight <= Viewport.Height ? Viewport.Height : scaledImageHeight;

        var changed = false;
        if (Math.Abs(horizontalScrollBar.Maximum - width) > 0.01)
        {
            horizontalScrollBar.Maximum = width;
            changed = true;
        }

        if (Math.Abs(verticalScrollBar.Maximum - height) > 0.01)
        {
            verticalScrollBar.Maximum = height;
            changed = true;
        }

        /*if (changed)
        {
            var newContainer = new ContentControl
            {
                Width = width,
                Height = height
            };
            FillContainer.Content = SizedContainer = newContainer;
            Debug.WriteLine($"Updated ViewPort: {DateTime.Now.Ticks}");
            //TriggerRender();
        }*/

        return changed;
    }

    #endregion

    #region Events and Overrides

    private void ScrollBarOnScroll(object? sender, ScrollEventArgs e)
    {
        TriggerRender();
    }

    /*protected override void OnScrollChanged(ScrollChangedEventArgs e)
    {
        Debug.WriteLine($"ViewportDelta: {e.ViewportDelta} | OffsetDelta: {e.OffsetDelta} | ExtentDelta: {e.ExtentDelta}");
        if (!e.ViewportDelta.IsDefault)
        {
            UpdateViewPort();
        }

        TriggerRender();

        base.OnScrollChanged(e);
    }*/

    private void ViewPortOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!IsImageLoaded || SizeMode != SizeModes.Normal) return;

        // Process horizontal scroll
        if (e.Delta.X != 0 && IsHorizontalBarVisible)
        {
            var factor = (e.KeyModifiers & KeyModifiers.Alt) != 0
                ? HorizontalScrollWithMouseAlternativeFactor
                : HorizontalScrollWithMouseFactor;
            if (factor != 0)
            {
                Offset = Offset.WithX(Offset.X - e.Delta.X * factor);
            }
        }

        // Process vertical scroll
        if (e.Delta.Y == 0) return;

        var verticalScrollWithMouseWheelKeyModifier = VerticalScrollWithMouseWheelKeyModifier;
        if (verticalScrollWithMouseWheelKeyModifier.HasValue &&
            (e.KeyModifiers & verticalScrollWithMouseWheelKeyModifier) == verticalScrollWithMouseWheelKeyModifier)
        {
            if (!IsVerticalBarVisible) return;
            var factor = (e.KeyModifiers & KeyModifiers.Alt) != 0
                ? VerticalScrollWithMouseAlternativeFactor
                : VerticalScrollWithMouseFactor;
            if (factor != 0)
            {
                Offset = Offset.WithY(Offset.Y - e.Delta.Y * factor);
            }

            return;
        }


        var mouseWheelBehaviour = ZoomWithMouseWheelBehaviour;
        if (mouseWheelBehaviour == MouseWheelZoomBehaviours.None) return;

        /*
#if DEBUG
        //File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "UVtoolsScroll.txt")
        //, $"WheelEvent {{Timestamp: {e.Timestamp}, Handled: {e.Handled}, Delta: {e.Delta}}}{Environment.NewLine}");
#endif
        */

        var zoomWithMouseWheelKeyModifier = ZoomWithMouseWheelKeyModifier;
        var canZoom = ZoomWithMouseWheelStrictKeyModifier switch
        {
            false => (e.KeyModifiers & zoomWithMouseWheelKeyModifier) == zoomWithMouseWheelKeyModifier,
            true => e.KeyModifiers == zoomWithMouseWheelKeyModifier
        };

        if (!canZoom) return;

        // Debounce for sensitive touchpads
        var zoomWithMouseWheelDebounceMilliseconds = ZoomWithMouseWheelDebounceMilliseconds;
        if (zoomWithMouseWheelDebounceMilliseconds > 0 && e.Timestamp - _lastZoomWithMouseWheelTimestamp <
            (ulong)zoomWithMouseWheelDebounceMilliseconds) return;

        // The MouseWheel event can contain multiple "spins" of the wheel so we need to adjust accordingly
        //double spins = Math.Abs(e.Delta.Y);
        //Debug.WriteLine(e.GetPosition(this));
        // TODO: Really should update the source method to handle multiple increments rather than calling it multiple times
        /*for (int i = 0; i < spins; i++)
        {*/


        switch (mouseWheelBehaviour)
        {
            case MouseWheelZoomBehaviours.ZoomNative:
                SetZoom(Zoom + (int)(e.Delta.Y * 100), e.GetPosition(ViewPort));
                break;
            case MouseWheelZoomBehaviours.ZoomNativeAltLevels:
                if ((e.KeyModifiers & KeyModifiers.Alt) == 0)
                {
                    SetZoom(Zoom + (int)(e.Delta.Y * 100), e.GetPosition(ViewPort));
                }
                else
                {
                    PerformZoom(e.Delta.Y > 0 ? ZoomActions.ZoomIn : ZoomActions.ZoomOut, e.GetPosition(ViewPort));
                }

                break;
            case MouseWheelZoomBehaviours.ZoomLevels:
                PerformZoom(e.Delta.Y > 0 ? ZoomActions.ZoomIn : ZoomActions.ZoomOut, e.GetPosition(ViewPort));
                break;
            case MouseWheelZoomBehaviours.ZoomLevelsAltNative:
                if ((e.KeyModifiers & KeyModifiers.Alt) == 0)
                {
                    PerformZoom(e.Delta.Y > 0 ? ZoomActions.ZoomIn : ZoomActions.ZoomOut, e.GetPosition(ViewPort));
                }
                else
                {
                    SetZoom(Zoom + (int)(e.Delta.Y * 100), e.GetPosition(ViewPort));
                }

                break;
        }

        _lastZoomWithMouseWheelTimestamp = e.Timestamp;
        //}
    }

    private void ViewPortOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Handled
            || _isPanning
            || _isSelecting
            || DisplayImage is null) return;

        var viewPort = ViewPort;
        if (viewPort is null) return;

        var pointer = e.GetCurrentPoint(viewPort);
        var location = pointer.Position;
        if (!new Rect(viewPort.Bounds.Size).Contains(location)) return;

        if (TryBeginChromeDrag(e, pointer, location)) return;

        if (SelectionMode != SelectionModes.None)
        {
            if (!GetImageViewPort().Contains(location)) return;

            if (!(
                    (pointer.Properties.IsLeftButtonPressed &&
                     (SelectWithMouseButtons & MouseButtons.LeftButton) != 0) ||
                    (pointer.Properties.IsMiddleButtonPressed &&
                     (SelectWithMouseButtons & MouseButtons.MiddleButton) != 0) ||
                    (pointer.Properties.IsRightButtonPressed &&
                     (SelectWithMouseButtons & MouseButtons.RightButton) != 0)
                )
               ) return;
            IsSelecting = true;
        }
        else
        {
            if (!(
                    (pointer.Properties.IsLeftButtonPressed && (PanWithMouseButtons & MouseButtons.LeftButton) != 0) ||
                    (pointer.Properties.IsMiddleButtonPressed &&
                     (PanWithMouseButtons & MouseButtons.MiddleButton) != 0) ||
                    (pointer.Properties.IsRightButtonPressed && (PanWithMouseButtons & MouseButtons.RightButton) != 0)
                )
                || !AutoPan
                || SizeMode != SizeModes.Normal
               ) return;

            IsPanning = true;
        }

        _startMousePosition = location;
        _activePointer = e.Pointer;
        e.Pointer.Capture(viewPort);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _isPinching = false;
        if (EndChromeDrag(e))
        {
            base.OnPointerReleased(e);
            return;
        }

        var wasInteracting = _isPanning || _isSelecting;
        IsPanning = false;
        IsSelecting = false;
        if (wasInteracting)
        {
            e.Pointer.Capture(null);
        }

        base.OnPointerReleased(e);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        EndChromeDrag(null);
        IsPanning = false;
        IsSelecting = false;
        base.OnPointerCaptureLost(e);
    }

    private void ViewPortOnPointerExited(object? sender, PointerEventArgs e)
    {
        PointerPosition = new Point(-1, -1);
        TriggerRender(true);
    }

    /*protected override void OnPointerLeave(PointerEventArgs e)
    {
        base.OnPointerLeave(e);
        PointerPosition = new Point(-1, -1);
        TriggerRender(true);
    }*/

    private void ViewPortOnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (e.Handled) return;

        var viewPort = ViewPort;
        if (viewPort is null)
        {
            e.Handled = true;
            return;
        }

        // Another finger touching while one is panning or selecting must not move it
        if ((_isPanning || _isSelecting || _chromeDrag != ChromeDrags.None) && _activePointer is not null &&
            !ReferenceEquals(e.Pointer, _activePointer)) return;

        var pointer = e.GetCurrentPoint(viewPort);
        PointerPosition = pointer.Position;

        if (_chromeDrag != ChromeDrags.None)
        {
            UpdateChromeDrag(pointer.Position);
            e.Handled = true;
            return;
        }

        if (!_isPanning && !_isSelecting)
        {
            TriggerRender(true);
            return;
        }

        if (_isPanning)
        {
            double x;
            double y;

            if (!InvertMousePan)
            {
                x = _startScrollPosition.X + (_startMousePosition.X - _pointerPosition.X);
                y = _startScrollPosition.Y + (_startMousePosition.Y - _pointerPosition.Y);
            }
            else
            {
                x = _startScrollPosition.X - (_startMousePosition.X - _pointerPosition.X);
                y = _startScrollPosition.Y - (_startMousePosition.Y - _pointerPosition.Y);
            }

            Offset = new Vector(x, y);
        }
        else if (_isSelecting)
        {
            var viewPortPoint = new Point(
                Math.Min(_pointerPosition.X, viewPort.Bounds.Right),
                Math.Min(_pointerPosition.Y, viewPort.Bounds.Bottom));

            double x;
            double y;
            double w;
            double h;

            var imageOrigin = GetImageOriginInViewport();

            if (viewPortPoint.X < _startMousePosition.X)
            {
                x = viewPortPoint.X;
                w = _startMousePosition.X - viewPortPoint.X;
            }
            else
            {
                x = _startMousePosition.X;
                w = viewPortPoint.X - _startMousePosition.X;
            }

            if (viewPortPoint.Y < _startMousePosition.Y)
            {
                y = viewPortPoint.Y;
                h = _startMousePosition.Y - viewPortPoint.Y;
            }
            else
            {
                y = _startMousePosition.Y;
                h = viewPortPoint.Y - _startMousePosition.Y;
            }

            x -= imageOrigin.X;
            y -= imageOrigin.Y;

            var zoomFactorX = ZoomFactorX;
            var zoomFactorY = ZoomFactorY;
            x /= zoomFactorX;
            y /= zoomFactorY;
            w /= zoomFactorX;
            h /= zoomFactorY;

            if (w > 0 && h > 0 && DisplayImage is { } image)
            {
                // The selection is dragged in view space and stored in source space
                var viewRegion = new Rect(x, y, w, h).Intersect(new Rect(ViewImageSize));
                SelectionRegion = viewRegion is { Width: > 0, Height: > 0 }
                    ? _orientation.ViewToSource(viewRegion, image.Size)
                    : default;
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!IsImageLoaded || SizeMode != SizeModes.Normal) return;

        var zoomInKeyGestures = ZoomInKeyGestures;
        if (zoomInKeyGestures is not null)
        {
            foreach (var zoomInKeyGesture in zoomInKeyGestures)
            {
                if (e.KeyModifiers == zoomInKeyGesture.KeyModifiers && e.Key == zoomInKeyGesture.Key)
                {
                    ZoomIn();
                    return;
                }
            }
        }

        var zoomOutKeyGestures = ZoomOutKeyGestures;
        if (zoomOutKeyGestures is not null)
        {
            foreach (var zoomOutKeyGesture in zoomOutKeyGestures)
            {
                if (e.KeyModifiers == zoomOutKeyGesture.KeyModifiers && e.Key == zoomOutKeyGesture.Key)
                {
                    ZoomOut();
                    return;
                }
            }
        }

        if (e.KeyModifiers != KeyModifiers.None) return;

        var panLeft = false;
        var panUp = false;
        var panRight = false;
        var panDown = false;

        if (PanWithArrows)
        {
            switch (e.Key)
            {
                case Key.Left:
                    panLeft = true;
                    break;
                case Key.Up:
                    panUp = true;
                    break;
                case Key.Right:
                    panRight = true;
                    break;
                case Key.Down:
                    panDown = true;
                    break;
            }
        }

        if (e.Key == PanLeftKey)
        {
            panLeft = true;
        }
        else if (e.Key == PanUpKey)
        {
            panUp = true;
        }
        else if (e.Key == PanRightKey)
        {
            panRight = true;
        }
        else if (e.Key == PanDownKey)
        {
            panDown = true;
        }

        if (panLeft)
        {
            Offset = new Vector(Offset.X - PanOffset * ZoomFactorX, Offset.Y);
            return;
        }

        if (panUp)
        {
            Offset = new Vector(Offset.X, Offset.Y - PanOffset * ZoomFactorY);
            return;
        }

        if (panRight)
        {
            Offset = new Vector(Offset.X + PanOffset * ZoomFactorX, Offset.Y);
            return;
        }

        if (panDown)
        {
            Offset = new Vector(Offset.X, Offset.Y + PanOffset * ZoomFactorY);
            return;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (!IsImageLoaded || SizeMode != SizeModes.Normal) return;

        var zoomTo100KeyGestures = ZoomTo100KeyGestures;
        if (zoomTo100KeyGestures is not null && Zoom != 100)
        {
            foreach (var zoomTo100KeyGesture in zoomTo100KeyGestures)
            {
                if (e.KeyModifiers == zoomTo100KeyGesture.KeyModifiers && e.Key == zoomTo100KeyGesture.Key)
                {
                    Zoom = 100;
                    return;
                }
            }
        }

        var zoomToFitKeyGestures = ZoomToFitKeyGestures;
        if (zoomToFitKeyGestures is not null)
        {
            foreach (var zoomToFitKeyGesture in zoomToFitKeyGestures)
            {
                if (e.KeyModifiers == zoomToFitKeyGesture.KeyModifiers && e.Key == zoomToFitKeyGesture.Key)
                {
                    Zoom = ZoomLevelToFit;
                    return;
                }
            }
        }
    }


    /*protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.Handled || !ViewPort.IsPointerOver) return;

        var pointer = e.GetCurrentPoint(ViewPort);
        PointerPosition = pointer.Position;

        if (!_isPanning && !_isSelecting)
        {
            TriggerRender(true);
            return;
        }

        if (_isPanning)
        {
            double x;
            double y;

            if (!InvertMousePan)
            {
                x = _startScrollPosition.X + (_startMousePosition.X - _pointerPosition.X);
                y = _startScrollPosition.Y + (_startMousePosition.Y - _pointerPosition.Y);
            }
            else
            {
                x = (_startScrollPosition.X - (_startMousePosition.X - _pointerPosition.X));
                y = (_startScrollPosition.Y - (_startMousePosition.Y - _pointerPosition.Y));
            }

            Offset = new Vector(x, y);
        }
        else if (_isSelecting)
        {
            double x;
            double y;
            double w;
            double h;

            var imageOffset = GetImageViewPort().Position;

            if (_pointerPosition.X < _startMousePosition.X)
            {
                x = _pointerPosition.X;
                w = _startMousePosition.X - _pointerPosition.X;
            }
            else
            {
                x = _startMousePosition.X;
                w = _pointerPosition.X - _startMousePosition.X;
            }

            if (_pointerPosition.Y < _startMousePosition.Y)
            {
                y = _pointerPosition.Y;
                h = _startMousePosition.Y - _pointerPosition.Y;
            }
            else
            {
                y = _startMousePosition.Y;
                h = _pointerPosition.Y - _startMousePosition.Y;
            }

            x -= imageOffset.X - Offset.X;
            y -= imageOffset.Y - Offset.Y;

            var zoomFactor = ZoomFactor;
            x /= zoomFactor;
            y /= zoomFactor;
            w /= zoomFactor;
            h /= zoomFactor;

            if (w > 0 && h > 0)
            {

                SelectionRegion = FitRectangle(new Rect(x, y, w, h));
            }
        }

    }*/

    #endregion

    #region Zoom and Size modes

    /// <summary>
    /// Resets the <see cref="SizeModes"/> property whilsts retaining the original <see cref="Zoom"/>.
    /// </summary>
    protected void RestoreSizeMode()
    {
        if (SizeMode != SizeModes.Normal)
        {
            var previousZoom = Zoom;
            SizeMode = SizeModes.Normal;
            Zoom = previousZoom; // Stop the zoom getting reset to 100% before calculating the new zoom
        }
    }

    /// <summary>
    /// Returns an appropriate zoom level based on the specified action, relative to the current zoom level.
    /// </summary>
    /// <param name="action">The action to determine the zoom level.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown if an unsupported action is specified.</exception>
    public int GetZoomLevel(ZoomActions action)
    {
        var result = action switch
        {
            ZoomActions.None => Zoom,
            ZoomActions.ZoomIn => _zoomLevels.NextZoom(Zoom),
            ZoomActions.ZoomOut => _zoomLevels.PreviousZoom(Zoom),
            ZoomActions.ActualSize => 100,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
        return result;
    }

    /// <summary>
    ///  Performs the specified zoom action.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="relativePoint">Preserve position at given relative point. If null, <see cref="CenterPoint"/>> will be used.</param>
    public void PerformZoom(ZoomActions action, Point? relativePoint)
    {
        SetZoom(GetZoomLevel(action), true, relativePoint);
    }

    /// <summary>
    /// Performs the specified zoom action.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="preservePosition"><c>true</c> if the current scrolling position should be preserved relative to the new zoom level, <c>false</c> to reset.</param>
    /// <param name="relativePoint">Preserve position at given relative point. If null, <see cref="CenterPoint"/>> will be used.</param>
    public void PerformZoom(ZoomActions action, bool preservePosition = true, Point? relativePoint = null)
    {
        SetZoom(GetZoomLevel(action), preservePosition, relativePoint);
    }

    /// <summary>
    /// Sets the zoom level to the specified value.
    /// </summary>
    /// <param name="zoom"></param>
    /// <param name="relativePoint">Preserve position at given relative point. If null, <see cref="CenterPoint"/>> will be used.</param>
    public void SetZoom(int zoom, Point? relativePoint)
    {
        SetZoom(zoom, true, relativePoint);
    }

    /// <summary>
    /// Sets the zoom level to the specified value.
    /// </summary>
    /// <param name="zoom"></param>
    /// <param name="preservePosition"><c>true</c> if the current scrolling position should be preserved relative to the new zoom level, <c>false</c> to reset.</param>
    /// <param name="relativePoint">Preserve position at given relative point. If null, <see cref="CenterPoint"/>> will be used.</param>
    public void SetZoom(int zoom, bool preservePosition = true, Point? relativePoint = null)
    {
        relativePoint ??= CenterPoint;
        var currentZoom = Zoom;
        var currentPixel = PointToView(relativePoint.Value);

        RestoreSizeMode();
        Zoom = zoom;

        if (preservePosition && Zoom != currentZoom)
        {
            ScrollToView(currentPixel, relativePoint.Value);
        }
    }

    /// <summary>
    ///   Zooms into the image
    /// </summary>
    public void ZoomIn()
    {
        ZoomIn(true);
    }

    /// <summary>
    ///   Zooms into the image
    /// </summary>
    /// <param name="preservePosition"><c>true</c> if the current scrolling position should be preserved relative to the new zoom level, <c>false</c> to reset.</param>
    public void ZoomIn(bool preservePosition)
    {
        PerformZoom(ZoomActions.ZoomIn, preservePosition);
    }

    /// <summary>
    ///   Zooms out of the image
    /// </summary>
    public void ZoomOut()
    {
        ZoomOut(true);
    }

    /// <summary>
    ///   Zooms out of the image
    /// </summary>
    /// <param name="preservePosition"><c>true</c> if the current scrolling position should be preserved relative to the new zoom level, <c>false</c> to reset.</param>
    public void ZoomOut(bool preservePosition)
    {
        PerformZoom(ZoomActions.ZoomOut, preservePosition);
    }

    /// <summary>
    /// Zooms to the maximum size for displaying the entire image within the bounds of the control.
    /// </summary>
    public void ZoomToFit()
    {
        Zoom = ZoomLevelToFit;
    }

    /// <summary>
    ///   Adjusts the view port to fit the given region, in source image pixels
    /// </summary>
    /// <param name="x">The X co-ordinate of the selection region.</param>
    /// <param name="y">The Y co-ordinate of the selection region.</param>
    /// <param name="width">The width of the selection region.</param>
    /// <param name="height">The height of the selection region.</param>
    /// <param name="margin">Give a margin to rectangle by a value to zoom-out that pixel value</param>
    public void ZoomToRegion(double x, double y, double width, double height, double margin = 0)
    {
        ZoomToRegion(new Rect(x, y, width, height), margin);
    }

    /// <summary>
    ///   Adjusts the view port to fit the given region
    /// </summary>
    /// <param name="x">The X co-ordinate of the selection region.</param>
    /// <param name="y">The Y co-ordinate of the selection region.</param>
    /// <param name="width">The width of the selection region.</param>
    /// <param name="height">The height of the selection region.</param>
    /// <param name="margin">Give a margin to rectangle by a value to zoom-out that pixel value</param>
    public void ZoomToRegion(int x, int y, int width, int height, double margin = 0)
    {
        ZoomToRegion(new Rect(x, y, width, height), margin);
    }

    /// <summary>
    ///   Adjusts the view port to fit the given region
    /// </summary>
    /// <param name="rectangle">The rectangle to fit the view port to.</param>
    /// <param name="margin">Give a margin to rectangle by a value to zoom-out that pixel value</param>
    public void ZoomToRegion(Rectangle rectangle, double margin = 0)
    {
        ZoomToRegion(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, margin);
    }

    /// <summary>
    ///   Adjusts the view port to fit the given region, in source image pixels
    /// </summary>
    /// <param name="rectangle">The rectangle to fit the view port to.</param>
    /// <param name="margin">Give a margin to rectangle by a value to zoom-out that pixel value</param>
    public void ZoomToRegion(Rect rectangle, double margin = 0)
    {
        if (!IsImageLoaded || rectangle.Width <= 0 || rectangle.Height <= 0) return;
        if (margin > 0) rectangle = rectangle.Inflate(margin);
        rectangle = _orientation.SourceToView(rectangle, DisplayImage!.Size);
        var ratioX = Viewport.Width / rectangle.Width;
        var ratioY = Viewport.Height / (rectangle.Height * ViewPixelAspectRatio);
        var zoomFactor = Math.Min(ratioX, ratioY);
        var cx = rectangle.X + rectangle.Width / 2;
        var cy = rectangle.Y + rectangle.Height / 2;

        CanRender = false;
        Zoom = (int)(zoomFactor * 100); // This function sets the zoom so viewport will change

        //Dispatcher.UIThread.Post(() => CenterAt(new Point(cx, cy)));
        CenterAtView(new Point(cx,
            cy)); // If I call this here, it will move to the wrong position due wrong viewport, dispatcher would solve but slower?
    }

    /// <summary>
    /// Zooms to current selection region
    /// </summary>
    public void ZoomToSelectionRegion(double margin = 0)
    {
        if (!HaveSelection) return;
        ZoomToRegion(SelectionRegion, margin);
    }

    /// <summary>
    /// Resets the zoom to 100%.
    /// </summary>
    public void PerformActualSize()
    {
        SizeMode = SizeModes.Normal;
        //SetZoom(100, ImageZoomActions.ActualSize | (Zoom < 100 ? ImageZoomActions.ZoomIn : ImageZoomActions.ZoomOut));
        Zoom = 100;
    }

    #endregion

    #region Utility methods

    /// <summary>
    ///   Determines whether the specified point is located within the image view port
    /// </summary>
    /// <param name="point">The point.</param>
    /// <returns>
    ///   <c>true</c> if the specified point is located within the image view port; otherwise, <c>false</c>.
    /// </returns>
    public bool IsPointInImage(Point point)
    {
        return GetImageViewPort().Contains(point);
    }

    /// <summary>
    ///   Determines whether the specified point is located within the image view port
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to check.</param>
    /// <param name="y">The Y co-ordinate of the point to check.</param>
    /// <returns>
    ///   <c>true</c> if the specified point is located within the image view port; otherwise, <c>false</c>.
    /// </returns>
    public bool IsPointInImage(int x, int y)
    {
        return IsPointInImage(new Point(x, y));
    }

    /// <summary>
    ///   Determines whether the specified point is located within the image view port
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to check.</param>
    /// <param name="y">The Y co-ordinate of the point to check.</param>
    /// <returns>
    ///   <c>true</c> if the specified point is located within the image view port; otherwise, <c>false</c>.
    /// </returns>
    public bool IsPointInImage(double x, double y)
    {
        return IsPointInImage(new Point(x, y));
    }

    /// <summary>
    ///   Converts the given client size point to represent a coordinate on the source image.
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to convert.</param>
    /// <param name="y">The Y co-ordinate of the point to convert.</param>
    /// <param name="fitToBounds">
    ///   if set to <c>true</c> and the point is outside the bounds of the source image, it will be mapped to the nearest edge.
    /// </param>
    /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
    public Point PointToImage(double x, double y, bool fitToBounds = true)
    {
        return PointToImage(new Point(x, y), fitToBounds);
    }

    /// <summary>
    ///   Converts the given client size point to represent a coordinate on the source image.
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to convert.</param>
    /// <param name="y">The Y co-ordinate of the point to convert.</param>
    /// <param name="fitToBounds">
    ///   if set to <c>true</c> and the point is outside the bounds of the source image, it will be mapped to the nearest edge.
    /// </param>
    /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
    public Point PointToImage(int x, int y, bool fitToBounds = true)
    {
        return PointToImage(new Point(x, y), fitToBounds);
    }

    /// <summary>
    ///   Converts the given client size point to represent a coordinate on the source image.
    /// </summary>
    /// <param name="point">The source point.</param>
    /// <param name="fitToBounds">
    ///   if set to <c>true</c> and the point is outside the bounds of the source image, it will be mapped to the nearest edge.
    /// </param>
    /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
    /// <remarks>
    /// <para>The result is in pixels of the source <see cref="Image"/>, whatever the <see cref="ImageRotation"/> and the
    /// flips, so it is the inverse of <see cref="GetOffsetPoint(Point)"/>.</para>
    /// <para>When <paramref name="fitToBounds"/> is <c>true</c> the result is meant to address a pixel, which is the
    /// floor of its coordinates. Along the mirrored axes, a point exactly on the boundary between two pixels is
    /// mapped to just before it, so it belongs to the pixel that is displayed under the point.</para>
    /// </remarks>
    public Point PointToImage(Point point, bool fitToBounds = true)
    {
        double x;
        double y;

        var image = DisplayImage;
        if (image is null) return default;

        var imageBounds = SizeMode == SizeModes.Normal
            ? GetNormalImageBoundsInViewport()
            : GetImageViewPort();

        if (!fitToBounds || imageBounds.Contains(point))
        {
            var viewPoint = new Point((point.X - imageBounds.X) / ZoomFactorX, (point.Y - imageBounds.Y) / ZoomFactorY);
            var sourcePoint = _orientation.ViewToSource(viewPoint, image.Size, fitToBounds);
            x = sourcePoint.X;
            y = sourcePoint.Y;

            if (fitToBounds)
            {
                x = Math.Clamp(x, 0, image.Size.Width - 1);
                y = Math.Clamp(y, 0, image.Size.Height - 1);
            }
        }
        else
        {
            x = 0; // Return Point.Empty if we couldn't match
            y = 0;
        }

        return new Point(x, y);
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.Point" /> repositioned to include the current image offset and scaled by the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="Point"/> to offset.</param>
    /// <returns>A <see cref="Point"/> which has been repositioned to match the current zoom level and image offset</returns>
    public Point GetOffsetPoint(System.Drawing.Point source)
    {
        var offset = GetOffsetPoint(new Point(source.X, source.Y));

        return new Point((int)offset.X, (int)offset.Y);
    }

    /// <summary>
    ///   Returns the source co-ordinates repositioned to include the current image offset and scaled by the current zoom level
    /// </summary>
    /// <param name="x">The source X co-ordinate.</param>
    /// <param name="y">The source Y co-ordinate.</param>
    /// <returns>A <see cref="Point"/> which has been repositioned to match the current zoom level and image offset</returns>
    public Point GetOffsetPoint(int x, int y)
    {
        return GetOffsetPoint(new Point(x, y));
    }

    /// <summary>
    ///   Returns the source co-ordinates repositioned to include the current image offset and scaled by the current zoom level
    /// </summary>
    /// <param name="x">The source X co-ordinate.</param>
    /// <param name="y">The source Y co-ordinate.</param>
    /// <returns>A <see cref="Point"/> which has been repositioned to match the current zoom level and image offset</returns>
    public Point GetOffsetPoint(double x, double y)
    {
        return GetOffsetPoint(new Point(x, y));
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.PointF" /> repositioned to include the current image offset and scaled by the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="PointF"/> to offset.</param>
    /// <returns>A <see cref="PointF"/> which has been repositioned to match the current zoom level and image offset</returns>
    /// <remarks>
    /// The <paramref name="source"/> is in pixels of the source <see cref="Image"/>, whatever the
    /// <see cref="ImageRotation"/> and the flips. The result is the position in the control where that point is
    /// displayed.
    /// </remarks>
    public Point GetOffsetPoint(Point source)
    {
        var scaled = GetScaledPoint(source);
        var imageOrigin = GetImageOriginInViewport();

        return new Point(scaled.X + imageOrigin.X, scaled.Y + imageOrigin.Y);
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.RectangleF" /> scaled according to the current zoom level and repositioned to include the current image offset
    /// </summary>
    /// <param name="source">The source <see cref="RectangleF"/> to offset.</param>
    /// <returns>A <see cref="RectangleF"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
    /// <remarks>
    /// The <paramref name="source"/> is in pixels of the source <see cref="Image"/>, whatever the
    /// <see cref="ImageRotation"/> and the flips. The result is the rectangle in the control where it is displayed.
    /// </remarks>
    public Rect GetOffsetRectangle(Rect source)
    {
        var scaled = GetScaledRectangle(source);
        var imageOrigin = GetImageOriginInViewport();

        return new Rect(new Point(scaled.Left + imageOrigin.X, scaled.Top + imageOrigin.Y), scaled.Size);
    }

    /// <summary>
    ///   Returns the source rectangle scaled according to the current zoom level and repositioned to include the current image offset
    /// </summary>
    /// <param name="x">The X co-ordinate of the source rectangle.</param>
    /// <param name="y">The Y co-ordinate of the source rectangle.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <returns>A <see cref="Rectangle"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
    public Rectangle GetOffsetRectangle(int x, int y, int width, int height)
    {
        return GetOffsetRectangle(new Rectangle(x, y, width, height));
    }

    /// <summary>
    ///   Returns the source rectangle scaled according to the current zoom level and repositioned to include the current image offset
    /// </summary>
    /// <param name="x">The X co-ordinate of the source rectangle.</param>
    /// <param name="y">The Y co-ordinate of the source rectangle.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <returns>A <see cref="RectangleF"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
    public Rect GetOffsetRectangle(double x, double y, double width, double height)
    {
        return GetOffsetRectangle(new Rect(x, y, width, height));
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.Rectangle" /> scaled according to the current zoom level and repositioned to include the current image offset
    /// </summary>
    /// <param name="source">The source <see cref="Rectangle"/> to offset.</param>
    /// <returns>A <see cref="Rectangle"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
    public Rectangle GetOffsetRectangle(Rectangle source)
    {
        var scaled = GetScaledRectangle(source);
        var imageOrigin = GetImageOriginInViewport();

        return new Rectangle(
            new System.Drawing.Point((int)(scaled.Left + imageOrigin.X), (int)(scaled.Top + imageOrigin.Y)),
            new System.Drawing.Size((int)scaled.Size.Width, (int)scaled.Size.Height));
    }

    /// <summary>
    ///   Fits a given <see cref="T:System.Drawing.Rectangle" /> to match image boundaries
    /// </summary>
    /// <param name="rectangle">The rectangle.</param>
    /// <returns>
    ///   A <see cref="T:System.Drawing.Rectangle" /> structure remapped to fit the image boundaries
    /// </returns>
    public Rectangle FitRectangle(Rectangle rectangle)
    {
        var image = DisplayImage;
        if (image is null) return Rectangle.Empty;
        var imageBounds = new Rectangle(0, 0, (int)image.Size.Width, (int)image.Size.Height);
        return Rectangle.Intersect(rectangle, imageBounds);
    }

    /// <summary>
    ///   Fits a given <see cref="T:System.Drawing.RectangleF" /> to match image boundaries
    /// </summary>
    /// <param name="rectangle">The rectangle.</param>
    /// <returns>
    ///   A <see cref="T:System.Drawing.RectangleF" /> structure remapped to fit the image boundaries
    /// </returns>
    public Rect FitRectangle(Rect rectangle)
    {
        var image = DisplayImage;
        if (image is null) return default;
        return rectangle.Intersect(new Rect(image.Size));
    }

    private PixelRect GetClampedSelectionPixelRect()
    {
        var image = DisplayImage;
        var selection = SelectionRegion;
        if (image is null || selection.Width <= 0 || selection.Height <= 0) return default;

        var left = Math.Max(0, (int)Math.Floor(selection.Left));
        var top = Math.Max(0, (int)Math.Floor(selection.Top));
        var right = Math.Min((int)image.Size.Width, (int)Math.Ceiling(selection.Right));
        var bottom = Math.Min((int)image.Size.Height, (int)Math.Ceiling(selection.Bottom));

        return right <= left || bottom <= top
            ? default
            : new PixelRect(left, top, right - left, bottom - top);
    }

    #endregion

    #region Navigate / Scroll methods

    /// <summary>
    ///   Scrolls the control to the given point in the image, offset at the specified display point
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to scroll to.</param>
    /// <param name="y">The Y co-ordinate of the point to scroll to.</param>
    /// <param name="relativeX">The X co-ordinate relative to the <c>x</c> parameter.</param>
    /// <param name="relativeY">The Y co-ordinate relative to the <c>y</c> parameter.</param>
    public void ScrollTo(double x, double y, double relativeX, double relativeY)
    {
        ScrollTo(new Point(x, y), new Point(relativeX, relativeY));
    }

    /// <summary>
    ///   Scrolls the control to the given point in the image, offset at the specified display point
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to scroll to.</param>
    /// <param name="y">The Y co-ordinate of the point to scroll to.</param>
    /// <param name="relativeX">The X co-ordinate relative to the <c>x</c> parameter.</param>
    /// <param name="relativeY">The Y co-ordinate relative to the <c>y</c> parameter.</param>
    public void ScrollTo(int x, int y, int relativeX, int relativeY)
    {
        ScrollTo(new Point(x, y), new Point(relativeX, relativeY));
    }

    /// <summary>
    ///   Scrolls the control to the given point in the image, offset at the specified display point
    /// </summary>
    /// <param name="imageLocation">The point of the image to attempt to scroll to, in source image pixels.</param>
    /// <param name="relativeDisplayPoint">The relative display point to offset scrolling by.</param>
    public void ScrollTo(Point imageLocation, Point relativeDisplayPoint)
    {
        ScrollToView(_orientation.SourceToView(imageLocation, DisplayImage?.Size ?? default), relativeDisplayPoint);
    }

    /// <summary>
    ///   Scrolls the control to the given point in the displayed image, offset at the specified display point
    /// </summary>
    /// <param name="viewLocation">The point of the displayed image to attempt to scroll to, in view space.</param>
    /// <param name="relativeDisplayPoint">The relative display point to offset scrolling by.</param>
    private void ScrollToView(Point viewLocation, Point relativeDisplayPoint)
    {
        var padding = GetScrollablePadding();
        var x = padding.Left + viewLocation.X * ZoomFactorX - relativeDisplayPoint.X;
        var y = padding.Top + viewLocation.Y * ZoomFactorY - relativeDisplayPoint.Y;


        Offset = new Vector(x, y);
        CanRender = true;

        /*Debug.WriteLine(
            $"X/Y: {x},{y} | \n" +
            $"Offset: {Offset} | \n" +
            $"ZoomFactor: {ZoomFactor} | \n" +
            $"Image Location: {imageLocation}\n" +
            $"MAX: {HorizontalScrollBar.Maximum},{VerticalScrollBar.Maximum} \n" +
            $"ViewPort: {Viewport.Width},{Viewport.Height} \n" +
            $"Container: {HorizontalScrollBar.ViewportSize},{VerticalScrollBar.ViewportSize} \n" +
            $"Relative: {relativeDisplayPoint}");*/
    }

    /// <summary>
    ///   Centers the given point in the image in the center of the control
    /// </summary>
    /// <param name="imageLocation">The point of the image to attempt to center.</param>
    public void CenterAt(System.Drawing.Point imageLocation)
    {
        ScrollTo(new Point(imageLocation.X, imageLocation.Y), new Point(Viewport.Width / 2, Viewport.Height / 2));
    }

    /// <summary>
    ///   Centers the given point in the image in the center of the control
    /// </summary>
    /// <param name="imageLocation">The point of the image to attempt to center, in source image pixels.</param>
    public void CenterAt(Point imageLocation)
    {
        ScrollTo(imageLocation, new Point(Viewport.Width / 2, Viewport.Height / 2));
    }

    /// <summary>
    ///   Centers the given point in the displayed image in the center of the control
    /// </summary>
    /// <param name="viewLocation">The point of the displayed image to attempt to center, in view space.</param>
    private void CenterAtView(Point viewLocation)
    {
        ScrollToView(viewLocation, new Point(Viewport.Width / 2, Viewport.Height / 2));
    }

    /// <summary>
    ///   Centers the given point in the image in the center of the control
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to center.</param>
    /// <param name="y">The Y co-ordinate of the point to center.</param>
    public void CenterAt(int x, int y)
    {
        CenterAt(new Point(x, y));
    }

    /// <summary>
    ///   Centers the given point in the image in the center of the control
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to center.</param>
    /// <param name="y">The Y co-ordinate of the point to center.</param>
    public void CenterAt(double x, double y)
    {
        CenterAt(new Point(x, y));
    }

    /// <summary>
    /// Resets the viewport to show the center of the image.
    /// </summary>
    public void CenterToImage()
    {
        var image = DisplayImage;
        if (image is null) return;
        CenterAt(new Point(image.Size.Width / 2, image.Size.Height / 2));
    }

    private void RequestCenterImage()
    {
        _centerImageOnNextLayout = true;
        CenterImageIfReady();
    }

    private void CenterImageIfReady()
    {
        if (!_centerImageOnNextLayout || !IsImageLoaded ||
            HorizontalScrollBar is null || VerticalScrollBar is null ||
            Viewport.Width <= 0 || Viewport.Height <= 0)
        {
            return;
        }

        _centerImageOnNextLayout = false;
        CenterToImage();
    }

    #endregion

    #region Selection / ROI methods

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.Point" /> scaled according to the current zoom level
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to scale.</param>
    /// <param name="y">The Y co-ordinate of the point to scale.</param>
    /// <returns>A <see cref="Point"/> which has been scaled to match the current zoom level</returns>
    public Point GetScaledPoint(int x, int y)
    {
        return GetScaledPoint(new Point(x, y));
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.Point" /> scaled according to the current zoom level
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to scale.</param>
    /// <param name="y">The Y co-ordinate of the point to scale.</param>
    /// <returns>A <see cref="Point"/> which has been scaled to match the current zoom level</returns>
    public PointF GetScaledPoint(float x, float y)
    {
        return GetScaledPoint(new PointF(x, y));
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.Point" /> scaled according to the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="Point"/> to scale.</param>
    /// <returns>A <see cref="Point"/> which has been scaled to match the current zoom level</returns>
    /// <remarks>
    /// The <paramref name="source"/> is in pixels of the source <see cref="Image"/>. The result is the position of that
    /// point in the displayed image, scaled to the current zoom level, relative to its top-left corner, so it includes
    /// the <see cref="ImageRotation"/> and the flips.
    /// </remarks>
    public Point GetScaledPoint(Point source)
    {
        var view = _orientation.SourceToView(source, DisplayImage?.Size ?? default);
        return new Point(view.X * ZoomFactorX, view.Y * ZoomFactorY);
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.PointF" /> scaled according to the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="PointF"/> to scale.</param>
    /// <returns>A <see cref="PointF"/> which has been scaled to match the current zoom level</returns>
    public PointF GetScaledPoint(PointF source)
    {
        var scaled = GetScaledPoint(new Point(source.X, source.Y));
        return new PointF((float)scaled.X, (float)scaled.Y);
    }

    /// <summary>
    ///   Returns the source rectangle scaled according to the current zoom level
    /// </summary>
    /// <param name="x">The X co-ordinate of the source rectangle.</param>
    /// <param name="y">The Y co-ordinate of the source rectangle.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <returns>A <see cref="Rectangle"/> which has been scaled to match the current zoom level</returns>
    public Rect GetScaledRectangle(int x, int y, int width, int height)
    {
        return GetScaledRectangle(new Rect(x, y, width, height));
    }

    /// <summary>
    ///   Returns the source rectangle scaled according to the current zoom level
    /// </summary>
    /// <param name="x">The X co-ordinate of the source rectangle.</param>
    /// <param name="y">The Y co-ordinate of the source rectangle.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <returns>A <see cref="RectangleF"/> which has been scaled to match the current zoom level</returns>
    public RectangleF GetScaledRectangle(float x, float y, float width, float height)
    {
        return GetScaledRectangle(new RectangleF(x, y, width, height));
    }

    /// <summary>
    ///   Returns the source rectangle scaled according to the current zoom level
    /// </summary>
    /// <param name="location">The location of the source rectangle.</param>
    /// <param name="size">The size of the source rectangle.</param>
    /// <returns>A <see cref="Rectangle"/> which has been scaled to match the current zoom level</returns>
    public Rect GetScaledRectangle(Point location, Size size)
    {
        return GetScaledRectangle(new Rect(location, size));
    }

    /// <summary>
    ///   Returns the source rectangle scaled according to the current zoom level
    /// </summary>
    /// <param name="location">The location of the source rectangle.</param>
    /// <param name="size">The size of the source rectangle.</param>
    /// <returns>A <see cref="Rectangle"/> which has been scaled to match the current zoom level</returns>
    public RectangleF GetScaledRectangle(PointF location, SizeF size)
    {
        return GetScaledRectangle(new RectangleF(location, size));
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.Rectangle" /> scaled according to the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="Rectangle"/> to scale.</param>
    /// <returns>A <see cref="Rectangle"/> which has been scaled to match the current zoom level</returns>
    /// <remarks>
    /// The <paramref name="source"/> is in pixels of the source <see cref="Image"/>. The result is the rectangle that
    /// covers it in the displayed image, scaled to the current zoom level, relative to its top-left corner, so it
    /// includes the <see cref="ImageRotation"/> and the flips.
    /// </remarks>
    public Rect GetScaledRectangle(Rect source)
    {
        var view = _orientation.SourceToView(source, DisplayImage?.Size ?? default);
        return new Rect(view.Left * ZoomFactorX, view.Top * ZoomFactorY, view.Width * ZoomFactorX,
            view.Height * ZoomFactorY);
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.RectangleF" /> scaled according to the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="RectangleF"/> to scale.</param>
    /// <returns>A <see cref="RectangleF"/> which has been scaled to match the current zoom level</returns>
    public RectangleF GetScaledRectangle(RectangleF source)
    {
        var scaled = GetScaledRectangle(new Rect(source.X, source.Y, source.Width, source.Height));
        return new RectangleF((float)scaled.X, (float)scaled.Y, (float)scaled.Width, (float)scaled.Height);
    }

    /// <summary>
    ///   Returns the source size scaled according to the current zoom level
    /// </summary>
    /// <param name="width">The width of the size to scale.</param>
    /// <param name="height">The height of the size to scale.</param>
    /// <returns>A <see cref="SizeF"/> which has been resized to match the current zoom level</returns>
    public SizeF GetScaledSize(float width, float height)
    {
        return GetScaledSize(new SizeF(width, height));
    }

    /// <summary>
    ///   Returns the source size scaled according to the current zoom level
    /// </summary>
    /// <param name="width">The width of the size to scale.</param>
    /// <param name="height">The height of the size to scale.</param>
    /// <returns>A <see cref="Size"/> which has been resized to match the current zoom level</returns>
    public Size GetScaledSize(int width, int height)
    {
        return GetScaledSize(new Size(width, height));
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.SizeF" /> scaled according to the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="SizeF"/> to scale.</param>
    /// <returns>A <see cref="SizeF"/> which has been resized to match the current zoom level</returns>
    public SizeF GetScaledSize(SizeF source)
    {
        var scaled = GetScaledSize(new Size(source.Width, source.Height));
        return new SizeF((float)scaled.Width, (float)scaled.Height);
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.Size" /> scaled according to the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="Size"/> to scale.</param>
    /// <returns>A <see cref="Size"/> which has been resized to match the current zoom level</returns>
    /// <remarks>
    /// The <paramref name="source"/> is a size in pixels of the source <see cref="Image"/>. The result is the size it
    /// takes in the displayed image, so the width and height are swapped for a rotation of 90 degrees.
    /// </remarks>
    public Size GetScaledSize(Size source)
    {
        var view = _orientation.GetViewSize(source);
        return new Size(view.Width * ZoomFactorX, view.Height * ZoomFactorY);
    }

    /// <summary>
    ///   Creates a selection region which encompasses the entire image
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown if no image is currently set</exception>
    public void SelectAll()
    {
        var image = DisplayImage;
        if (image is null) return;
        SelectionRegion = new Rect(0, 0, image.Size.Width, image.Size.Height);
    }

    /// <summary>
    /// Clears any existing selection region
    /// </summary>
    public void SelectNone()
    {
        SelectionRegion = default;
    }

    #endregion

    #region Viewport and image region methods

    private Thickness GetScrollablePadding()
    {
        if (PanBoundsMode != PanBoundsModes.Padding || SizeMode != SizeModes.Normal) return default;

        var padding = Padding;
        return new Thickness(
            Math.Max(0, padding.Left),
            Math.Max(0, padding.Top),
            Math.Max(0, padding.Right),
            Math.Max(0, padding.Bottom));
    }

    private Size GetDocumentSize()
    {
        if (!IsImageLoaded || SizeMode != SizeModes.Normal) return default;

        var padding = GetScrollablePadding();
        return new Size(
            padding.Left + ScaledImageWidth + padding.Right,
            padding.Top + ScaledImageHeight + padding.Bottom);
    }

    private Rect GetNormalImageBoundsInViewport()
    {
        var padding = GetScrollablePadding();
        var documentSize = GetDocumentSize();
        var viewport = Viewport;
        var offset = Offset;
        var x = padding.Left - offset.X;
        var y = padding.Top - offset.Y;

        if (AutoCenter && documentSize.Width < viewport.Width)
        {
            x += (viewport.Width - documentSize.Width) / 2;
        }

        if (AutoCenter && documentSize.Height < viewport.Height)
        {
            y += (viewport.Height - documentSize.Height) / 2;
        }

        return new Rect(x, y, ScaledImageWidth, ScaledImageHeight);
    }

    private Point GetImageOriginInViewport()
    {
        return SizeMode == SizeModes.Normal
            ? GetNormalImageBoundsInViewport().Position
            : GetImageViewPort().Position;
    }

    /// <summary>
    ///   Gets the source image region.
    /// </summary>
    /// <returns>The part of the image that is visible, in pixels of the source <see cref="Image"/>, whatever the
    /// <see cref="ImageRotation"/> and the flips.</returns>
    public Rect GetSourceImageRegion()
    {
        var image = DisplayImage;
        if (image is null) return default;

        var viewRegion = GetViewImageRegion();
        return viewRegion.Width <= 0 || viewRegion.Height <= 0
            ? default
            : _orientation.ViewToSource(viewRegion, image.Size);
    }

    /// <summary>
    /// Converts a point of the control to a point of the displayed image, in image pixels, in view space.
    /// </summary>
    private Point PointToView(Point point)
    {
        if (DisplayImage is null) return default;

        var imageBounds = SizeMode == SizeModes.Normal
            ? GetNormalImageBoundsInViewport()
            : GetImageViewPort();

        return new Point((point.X - imageBounds.X) / ZoomFactorX, (point.Y - imageBounds.Y) / ZoomFactorY);
    }

    /// <summary>
    /// Gets the part of the displayed image that is visible, in image pixels, in view space.
    /// </summary>
    private Rect GetViewImageRegion()
    {
        var image = DisplayImage;
        if (image is null) return default;

        switch (SizeMode)
        {
            case SizeModes.Normal:
                var imageBounds = GetNormalImageBoundsInViewport();
                var visibleImageBounds = imageBounds.Intersect(new Rect(Viewport));
                if (visibleImageBounds.Width <= 0 || visibleImageBounds.Height <= 0) return default;

                var zoomFactorX = ZoomFactorX;
                var zoomFactorY = ZoomFactorY;
                var sourceLeft = (visibleImageBounds.Left - imageBounds.Left) / zoomFactorX;
                var sourceTop = (visibleImageBounds.Top - imageBounds.Top) / zoomFactorY;
                var sourceWidth = visibleImageBounds.Width / zoomFactorX;
                var sourceHeight = visibleImageBounds.Height / zoomFactorY;

                return new Rect(sourceLeft, sourceTop, sourceWidth, sourceHeight);
        }

        return new Rect(_orientation.GetViewSize(image.Size));
    }

    /// <summary>
    /// Gets the image view port.
    /// </summary>
    /// <returns>The image viewport rectangle.</returns>
    public Rect GetImageViewPort()
    {
        var image = DisplayImage;
        if (image is null) return default;

        var viewPortSize = Viewport;
        if (viewPortSize is { Width: 0, Height: 0 }) return default;

        var xOffset = 0.0;
        var yOffset = 0.0;
        var width = 0.0;
        var height = 0.0;

        var padding = Padding;

        switch (SizeMode)
        {
            case SizeModes.Normal:
                return GetNormalImageBoundsInViewport().Intersect(new Rect(viewPortSize));
            case SizeModes.Stretch:
                width = viewPortSize.Width - padding.Left - padding.Right;
                if (width <= 0) return new Rect();
                height = viewPortSize.Height - padding.Top - padding.Bottom;
                if (height <= 0) return new Rect();

                xOffset = padding.Left;
                yOffset = padding.Top;
                break;
            case SizeModes.Fit:
                var pixelAspectRatio = ViewPixelAspectRatio;
                var viewImageSize = _orientation.GetViewSize(image.Size);
                var scaleFactor = Math.Min((viewPortSize.Width - padding.Left - padding.Right) / viewImageSize.Width,
                    (viewPortSize.Height - padding.Top - padding.Bottom) / (viewImageSize.Height * pixelAspectRatio));

                if (scaleFactor <= 0) return new Rect();

                width = Math.Floor(viewImageSize.Width * scaleFactor);
                height = Math.Floor(viewImageSize.Height * pixelAspectRatio * scaleFactor);

                if (AutoCenter)
                {
                    xOffset = (viewPortSize.Width - width) / 2.0;
                    yOffset = (viewPortSize.Height - height) / 2.0;
                }
                else
                {
                    xOffset = padding.Left;
                    yOffset = padding.Top;
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(SizeMode), SizeMode, null);
        }

        return new Rect(xOffset, yOffset, width, height);
    }

    #endregion

    #region Image methods

    /// <summary>
    /// Loads the image from the specified path.
    /// </summary>
    /// <param name="path">Image path from disk</param>
    public void LoadImage(string path)
    {
        var image = new Bitmap(path);
        try
        {
            Image = image;
            _ownedImage = image;
        }
        catch
        {
            image.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Creates a bitmap containing the current selection.
    /// </summary>
    /// <returns>A bitmap owned by the caller, or <see langword="null"/> when no valid selection exists.</returns>
    /// <remarks>The bitmap has the pixels of the source <see cref="Image"/>, it is not rotated or flipped.</remarks>
    public Bitmap? GetSelectedBitmap()
    {
        return GetSelectedBitmap(false);
    }

    /// <summary>
    /// Creates a bitmap containing the current selection.
    /// </summary>
    /// <param name="applyOrientation">
    /// <c>true</c> to flip and rotate the bitmap like the image is displayed, according to <see cref="ImageRotation"/>,
    /// <see cref="FlipHorizontal"/> and <see cref="FlipVertical"/>, <c>false</c> to get the pixels of the source image.
    /// </param>
    /// <returns>A bitmap owned by the caller, or <see langword="null"/> when no valid selection exists.</returns>
    public Bitmap? GetSelectedBitmap(bool applyOrientation)
    {
        if (!HaveSelection) return null;

        var selection = SelectionRegionPixel;
        if (selection.Width <= 0 || selection.Height <= 0) return null;

        if (GrayscaleImage is { } grayscaleImage)
        {
            // Converted to a 32-bit bitmap with its color map, only the selection
            using var converted = grayscaleImage.ToBitmap(selection);
            return CropBitmap(converted, new PixelRect(converted.PixelSize), applyOrientation);
        }

        var image = ImageAsWriteableBitmap;
        return image is null ? null : CropBitmap(image, selection, applyOrientation);
    }

    private WriteableBitmap? CropBitmap(WriteableBitmap image, PixelRect selection, bool applyOrientation)
    {
        using var srcBuffer = image.Lock();
        if (selection.Right > srcBuffer.Size.Width || selection.Bottom > srcBuffer.Size.Height)
        {
            return null;
        }

        var bitsPerPixel = srcBuffer.Format.BitsPerPixel;
        if (bitsPerPixel <= 0 || bitsPerPixel % 8 != 0 || srcBuffer.RowBytes <= 0)
        {
            throw new NotSupportedException(
                $"Pixel format '{srcBuffer.Format}' does not have a supported byte-aligned stride.");
        }

        var bytesPerPixel = bitsPerPixel / 8;
        var bytesPerRow = checked(selection.Width * bytesPerPixel);
        if (bytesPerRow > srcBuffer.RowBytes - checked(selection.X * bytesPerPixel))
        {
            throw new InvalidOperationException("The selected region exceeds the source framebuffer stride.");
        }

        var orientation = applyOrientation ? _orientation : default;
        var cropSize = orientation.IsTransposed
            ? new PixelSize(selection.Height, selection.Width)
            : selection.Size;

        var cropBitmap = new WriteableBitmap(cropSize, srcBuffer.Dpi, srcBuffer.Format, srcBuffer.AlphaFormat);
        try
        {
            using (var dstBuffer = cropBitmap.Lock())
            {
                if (dstBuffer.RowBytes < checked(cropSize.Width * bytesPerPixel))
                {
                    throw new InvalidOperationException(
                        "The destination framebuffer stride is smaller than the selected row.");
                }

                unsafe
                {
                    var source = srcBuffer.Address + srcBuffer.RowBytes * selection.Y + selection.X * bytesPerPixel;
                    var destination = dstBuffer.Address;

                    if (orientation.IsIdentity)
                    {
                        for (var row = 0; row < selection.Height; row++)
                        {
                            Buffer.MemoryCopy(source.ToPointer(), destination.ToPointer(), dstBuffer.RowBytes,
                                bytesPerRow);
                            source += srcBuffer.RowBytes;
                            destination += dstBuffer.RowBytes;
                        }
                    }
                    else
                    {
                        for (var row = 0; row < selection.Height; row++)
                        {
                            for (var column = 0; column < selection.Width; column++)
                            {
                                var (x, y) = orientation.SourceToViewPixel(column, row, selection.Width,
                                    selection.Height);
                                Buffer.MemoryCopy((source + column * bytesPerPixel).ToPointer(),
                                    (destination + dstBuffer.RowBytes * y + x * bytesPerPixel).ToPointer(),
                                    bytesPerPixel, bytesPerPixel);
                            }

                            source += srcBuffer.RowBytes;
                        }
                    }
                }
            }

            return cropBitmap;
        }
        catch
        {
            cropBitmap.Dispose();
            throw;
        }
    }

    #endregion
}