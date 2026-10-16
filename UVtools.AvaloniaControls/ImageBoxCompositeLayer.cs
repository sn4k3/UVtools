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
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace UVtools.AvaloniaControls;

/// <summary>
/// Determines if a <see cref="ImageBoxCompositeLayer"/> is drawn over or under the main image of the
/// <see cref="AdvancedImageBox"/>.
/// </summary>
public enum ImageBoxCompositePlacement
{
    /// <summary>The layer is drawn over the main image.</summary>
    Above,

    /// <summary>The layer is drawn under the main image, so it is only visible where the main image is
    /// transparent.</summary>
    Below
}

/// <summary>
/// Determines how a <see cref="ImageBoxCompositeLayer"/> is combined with what is under it.
/// </summary>
/// <remarks>A <see cref="GrayscaleImage"/> is blended by Skia and a bitmap by the bitmap blending of Avalonia, they
/// look the same.</remarks>
public enum ImageBoxCompositeBlend
{
    /// <summary>The layer is drawn over what is under it, source over.</summary>
    Normal,

    /// <summary>The colors are added, a light layer brightens what is under it.</summary>
    Plus,

    /// <summary>The inverse of the colors are multiplied, a softer <see cref="Plus"/>.</summary>
    Screen,

    /// <summary>The colors are multiplied, a dark layer darkens what is under it.</summary>
    Multiply,

    /// <summary>The lighter of each channel is kept.</summary>
    Lighten,

    /// <summary>The darker of each channel is kept.</summary>
    Darken
}

/// <summary>
/// Determines where the alpha channel of a <see cref="ImageBoxCompositeLayer"/> comes from.
/// </summary>
public enum ImageBoxCompositeAlpha
{
    /// <summary>The layer is opaque, or has the alpha of its bitmap.</summary>
    Image,

    /// <summary>The luminance of each pixel of a <see cref="GrayscaleImage"/> is the alpha of the layer, and the
    /// <see cref="ImageBoxCompositeLayer.Tint"/> its color: black pixels are transparent and white pixels have the
    /// color of the tint. It is what an onion skin needs. A bitmap ignores it.</summary>
    Luminance
}

/// <summary>
/// Determines on which side of the <see cref="AdvancedImageBox.CompareSplitPosition"/> a
/// <see cref="ImageBoxCompositeLayer"/> is drawn.
/// </summary>
public enum ImageBoxCompareSide
{
    /// <summary>The layer is drawn everywhere, the split doesn't affect it.</summary>
    None,

    /// <summary>The layer is drawn on the left of the split, in the displayed image, and only while the split is
    /// active.</summary>
    Left,

    /// <summary>The layer is drawn on the right of the split, in the displayed image, and only while the split is
    /// active.</summary>
    Right
}

/// <summary>
/// An extra image drawn by an <see cref="AdvancedImageBox"/> over or under its main image, with the same transform: the
/// zoom, the scroll, the rotation, the flips and the pixel aspect ratio. See
/// <see cref="AdvancedImageBox.CompositeLayers"/>.
/// </summary>
/// <remarks>
/// <para>The layer is meant for the same size as the main image, for example the previous and the next layers of a
/// stack for an onion skin, or the second image of a comparison. When the size is different the layer is stretched to
/// cover the main image.</para>
/// <para>It is sampled like the main image, see <see cref="AdvancedImageBox.BitmapSamplingMode"/>, with its own cached
/// reductions when zoomed out.</para>
/// <para>The <see cref="GrayscaleImage"/> is drawn by Skia, which applies the <see cref="Tint"/>, the
/// <see cref="Opacity"/>, the <see cref="Blend"/> and the <see cref="Alpha"/> on the GPU or the Skia raster pipeline
/// without converting the image. A <see cref="Image">bitmap</see> supports the <see cref="Opacity"/> and the
/// <see cref="Blend"/> only: the <see cref="Tint"/> is ignored, and so is the <see cref="Alpha"/>. A grayscale image
/// that can't be drawn by Skia falls back to a bitmap, and behaves the same.</para>
/// <para>The control doesn't own the images, the caller disposes them. Call <see cref="Invalidate"/> after writing
/// the pixels of a bitmap in place; a <see cref="GrayscaleImage"/> is tracked by its version. A layer can be in a
/// single <see cref="AdvancedImageBox"/> and must be used from the UI thread.</para>
/// </remarks>
public sealed class ImageBoxCompositeLayer : INotifyPropertyChanged
{
    private Bitmap? _image;
    private GrayscaleImage? _grayscaleImage;
    private Color _tint = Colors.White;
    private double _opacity = 1;
    private ImageBoxCompositePlacement _placement;
    private ImageBoxCompositeBlend _blend;
    private ImageBoxCompositeAlpha _alpha;
    private ImageBoxCompareSide _compareSide;
    private bool _isVisible = true;
    private object? _tag;
    private int _version;
    private ReducedImageCache? _reducedCache;

    /// <summary>
    /// Creates an empty layer.
    /// </summary>
    public ImageBoxCompositeLayer()
    {
    }

    /// <summary>
    /// Creates a layer which draws a grayscale image.
    /// </summary>
    public ImageBoxCompositeLayer(GrayscaleImage image)
    {
        _grayscaleImage = image;
    }

    /// <summary>
    /// Creates a layer which draws a bitmap.
    /// </summary>
    public ImageBoxCompositeLayer(Bitmap image)
    {
        _image = image;
    }

    /// <summary>
    /// The control that draws this layer, <see langword="null"/> when it isn't in an
    /// <see cref="AdvancedImageBox.CompositeLayers"/> collection.
    /// </summary>
    internal AdvancedImageBox? Owner { get; set; }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the bitmap to draw. The <see cref="GrayscaleImage"/> is drawn instead of it while it is set.
    /// </summary>
    public Bitmap? Image
    {
        get => _image;
        set
        {
            if (!SetProperty(ref _image, value)) return;
            ResetCache();
        }
    }

    /// <summary>
    /// Gets or sets the 8-bit grayscale image to draw, instead of the <see cref="Image"/>.
    /// </summary>
    public GrayscaleImage? GrayscaleImage
    {
        get => _grayscaleImage;
        set
        {
            if (!SetProperty(ref _grayscaleImage, value)) return;
            ResetCache();
        }
    }

    /// <summary>
    /// Gets the image which is drawn: the <see cref="GrayscaleImage"/> when set, otherwise the <see cref="Image"/>.
    /// </summary>
    internal IImage? DisplayImage => (IImage?)_grayscaleImage ?? _image;

    /// <summary>
    /// Gets or sets the color the layer is multiplied by, white by default which leaves it as it is. With the
    /// <see cref="ImageBoxCompositeAlpha.Luminance"/> alpha it is the color of the layer. The alpha channel of the tint
    /// multiplies the <see cref="Opacity"/>. Only a <see cref="GrayscaleImage"/> is tinted.
    /// </summary>
    public Color Tint
    {
        get => _tint;
        set => SetProperty(ref _tint, value);
    }

    /// <summary>
    /// Gets or sets the opacity of the layer, from 0 (transparent) to 1 (opaque). Default is 1.
    /// </summary>
    public double Opacity
    {
        get => _opacity;
        set => SetProperty(ref _opacity, double.IsNaN(value) ? 1 : Math.Clamp(value, 0, 1));
    }

    /// <summary>
    /// Gets or sets if the layer is drawn over or under the main image. Default is
    /// <see cref="ImageBoxCompositePlacement.Above"/>. The layers of the same placement are drawn in the order of the
    /// collection, the last one over the others.
    /// </summary>
    public ImageBoxCompositePlacement Placement
    {
        get => _placement;
        set => SetProperty(ref _placement, value);
    }

    /// <summary>
    /// Gets or sets how the layer is combined with what is under it. Default is
    /// <see cref="ImageBoxCompositeBlend.Normal"/>.
    /// </summary>
    public ImageBoxCompositeBlend Blend
    {
        get => _blend;
        set => SetProperty(ref _blend, value);
    }

    /// <summary>
    /// Gets or sets where the alpha channel of the layer comes from. Default is
    /// <see cref="ImageBoxCompositeAlpha.Image"/>.
    /// </summary>
    public ImageBoxCompositeAlpha Alpha
    {
        get => _alpha;
        set => SetProperty(ref _alpha, value);
    }

    /// <summary>
    /// Gets or sets the side of the <see cref="AdvancedImageBox.CompareSplitPosition"/> the layer is drawn on.
    /// Default is <see cref="ImageBoxCompareSide.None"/>.
    /// </summary>
    public ImageBoxCompareSide CompareSide
    {
        get => _compareSide;
        set => SetProperty(ref _compareSide, value);
    }

    /// <summary>
    /// Gets or sets if the layer is drawn. Default is <c>true</c>.
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    /// <summary>
    /// Gets or sets an object to associate with the layer, it is not used by the control.
    /// </summary>
    public object? Tag
    {
        get => _tag;
        set
        {
            if (ReferenceEquals(_tag, value)) return;
            _tag = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tag)));
        }
    }

    /// <summary>
    /// Gets a number that changes each time <see cref="Invalidate"/> is called or an image is set.
    /// </summary>
    internal int Version => Volatile.Read(ref _version);

    /// <summary>
    /// Gets the cache of the reductions of the image.
    /// </summary>
    internal ReducedImageCache ReducedCache => _reducedCache ??= new ReducedImageCache();

    /// <summary>
    /// Redraws the control after the pixels of the <see cref="Image"/> were modified in place. A
    /// <see cref="GrayscaleImage"/> doesn't need it, it is tracked by its version.
    /// </summary>
    public void Invalidate()
    {
        Interlocked.Increment(ref _version);
        Owner?.TriggerRender();
    }

    /// <summary>
    /// Disposes the cached reductions, they are created again when needed.
    /// </summary>
    internal void ReleaseCache()
    {
        _reducedCache?.Dispose();
        _reducedCache = null;
    }

    private void ResetCache()
    {
        Interlocked.Increment(ref _version);
        ReleaseCache();
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        Owner?.TriggerRender();
        return true;
    }
}

/// <summary>
/// The composite layers of an <see cref="AdvancedImageBox"/>, the layers of the same placement are drawn in the order
/// of the collection, the last one over the others.
/// </summary>
/// <remarks>Adding or removing layers redraws the control. To change many at once use <see cref="ReplaceAll"/>, which
/// redraws once. A layer removed from the collection releases its cached reductions.</remarks>
public sealed class ImageBoxCompositeLayerCollection : Collection<ImageBoxCompositeLayer>, INotifyCollectionChanged
{
    private readonly AdvancedImageBox _owner;

    internal ImageBoxCompositeLayerCollection(AdvancedImageBox owner)
    {
        _owner = owner;
    }

    /// <inheritdoc />
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Replaces all the layers by the given ones, the control is redrawn once. The previous layers are removed and can
    /// be reused.
    /// </summary>
    /// <param name="layers">The new layers, in drawing order. It can't be this collection.</param>
    /// <exception cref="InvalidOperationException">A layer is already in an <see cref="AdvancedImageBox"/> or is
    /// repeated, the collection is left empty.</exception>
    public void ReplaceAll(IEnumerable<ImageBoxCompositeLayer> layers)
    {
        ArgumentNullException.ThrowIfNull(layers);
        if (ReferenceEquals(layers, this)) throw new ArgumentException("The collection can't replace itself.", nameof(layers));

        var hadItems = Items.Count > 0;
        DetachAll();
        Items.Clear();

        var added = false;
        try
        {
            foreach (var layer in layers)
            {
                Attach(layer);
                Items.Add(layer);
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
    protected override void InsertItem(int index, ImageBoxCompositeLayer item)
    {
        Attach(item);
        base.InsertItem(index, item);
        OnChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    }

    /// <inheritdoc />
    protected override void SetItem(int index, ImageBoxCompositeLayer item)
    {
        var oldItem = Items[index];
        if (ReferenceEquals(oldItem, item)) return;

        Attach(item);
        base.SetItem(index, item);
        Detach(oldItem);
        OnChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, index));
    }

    /// <inheritdoc />
    protected override void RemoveItem(int index)
    {
        var item = Items[index];
        base.RemoveItem(index);
        Detach(item);
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

    private void Attach(ImageBoxCompositeLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        if (layer.Owner is not null)
        {
            throw new InvalidOperationException(ReferenceEquals(layer.Owner, _owner)
                ? "The layer is already in the collection."
                : "The layer is already in the composite layers of another AdvancedImageBox.");
        }

        layer.Owner = _owner;
    }

    private static void Detach(ImageBoxCompositeLayer layer)
    {
        layer.Owner = null;
        layer.ReleaseCache();
    }

    private void DetachAll()
    {
        foreach (var layer in Items)
        {
            Detach(layer);
        }
    }

    private void OnReset() => OnChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

    private void OnChanged(NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(this, e);
        _owner.TriggerRender();
    }
}
