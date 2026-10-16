/*
 *                               The MIT License (MIT)
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so.
 */

#region

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

#endregion

namespace UVtools.AvaloniaControls;

/// <summary>
/// An 8-bit grayscale image, one byte per pixel, which the <see cref="AdvancedImageBox"/> displays through
/// <see cref="AdvancedImageBox.GrayscaleImage"/> without expanding it to a 32-bit bitmap, optionally colored by a
/// 256 entry <see cref="SetColorMap">color map</see> that is applied when drawing.
/// </summary>
/// <remarks>
/// <para>It needs a lot less memory and bandwidth than a <see cref="WriteableBitmap"/>: a 13312x5120 image takes
/// 65 MB instead of 272 MB, and nothing is converted or copied when it is drawn. Avalonia can't do this on its own:
/// a <see cref="WriteableBitmap"/> created with <see cref="PixelFormats.Gray8"/> is converted by the Skia backend to a
/// 32-bit copy on every <see cref="WriteableBitmap.Lock"/>, which doubles the memory and takes a couple of hundred
/// milliseconds on large images.</para>
/// <para>The image is drawn by Skia, with the color map and the sampling applied by the GPU or the Skia raster
/// pipeline, through a custom draw operation that needs the Skia render backend of Avalonia (the default on all
/// desktop platforms). When another backend is used, or Skia can't be reached, the control falls back to a 32-bit
/// copy of the image, created once for each <see cref="Version"/> of the pixels, so it still displays correctly.</para>
/// <para>The pixels are guarded by a lock, because they are read by the render thread. Write them only through
/// <see cref="Lock"/> and call <see cref="AdvancedImageBox.InvalidateImage"/> afterwards to redraw. Disposing the
/// image waits for the drawing in progress, and an image that is disposed while still displayed draws nothing.</para>
/// </remarks>
public sealed unsafe class GrayscaleImage : IImage, IDisposable
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private readonly long _allocatedBytes;
    private nint _address;
    private bool _disposed;
    private int _version;
    private uint[]? _colorMap;

    // State of the draw back ends, they are touched by the render thread under the read lock or by the owner
    private IDisposable? _skiaCache;
    private WriteableBitmap? _fallbackBitmap;
    private int _fallbackVersion = -1;
    private volatile bool _skiaUnavailable;

    /// <summary>
    /// Creates a zero filled image.
    /// </summary>
    /// <param name="width">The width in pixels, greater than zero.</param>
    /// <param name="height">The height in pixels, greater than zero.</param>
    public GrayscaleImage(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
        _allocatedBytes = (long)width * height;
        _address = (nint)NativeMemory.AllocZeroed((nuint)_allocatedBytes);
        GC.AddMemoryPressure(_allocatedBytes);
    }

    /// <summary>
    /// Finalizer, frees the pixels when the image was not disposed.
    /// </summary>
    ~GrayscaleImage()
    {
        FreePixels();
    }

    /// <summary>Gets the width in pixels.</summary>
    public int Width { get; }

    /// <summary>Gets the height in pixels.</summary>
    public int Height { get; }

    /// <summary>Gets the size in pixels.</summary>
    public PixelSize PixelSize => new(Width, Height);

    /// <summary>
    /// Gets the size of the image. A grayscale image has no DPI, so it is the size in pixels.
    /// </summary>
    public Size Size => new(Width, Height);

    /// <summary>
    /// Gets the number of bytes of a row of the pixels, which are tightly packed: it is the <see cref="Width"/>.
    /// </summary>
    public int RowBytes => Width;

    /// <summary>
    /// Gets a number that changes each time the pixels or the color map are modified.
    /// </summary>
    public int Version => Volatile.Read(ref _version);

    /// <summary>
    /// Gets if the image was disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Gets if a <see cref="SetColorMap">color map</see> is set, otherwise the image is displayed in grayscale.
    /// </summary>
    public bool HasColorMap => _colorMap is not null;

    /// <summary>
    /// Gets if the image can be drawn by Skia, which is not known until it is drawn for the first time. When
    /// <see langword="false"/> the control draws a 32-bit copy of the image instead.
    /// </summary>
    public bool IsSkiaRenderingAvailable => !_skiaUnavailable;

    /// <summary>
    /// Sets the colors used to display the pixels, indexed by their value: a pixel with the value <c>n</c> is drawn
    /// with the color <c>colors[n]</c>. Without a color map the pixels are displayed as grays.
    /// </summary>
    /// <param name="colors">
    /// The 256 colors as <c>0xAARRGGBB</c>, which is <c>Color.ToUInt32()</c> of Avalonia. The alpha channel is ignored,
    /// the image is always opaque. The colors are copied.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="colors"/> doesn't have 256 entries.</exception>
    public void SetColorMap(ReadOnlySpan<uint> colors)
    {
        if (colors.Length != 256)
        {
            throw new ArgumentException("The color map must have 256 entries.", nameof(colors));
        }

        var colorMap = colors.ToArray();
        _lock.EnterWriteLock();
        try
        {
            _colorMap = colorMap;
            Interlocked.Increment(ref _version);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes the color map, the pixels are displayed as grays again.
    /// </summary>
    public void ClearColorMap()
    {
        _lock.EnterWriteLock();
        try
        {
            if (_colorMap is null)
            {
                return;
            }

            _colorMap = null;
            Interlocked.Increment(ref _version);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets the color a pixel value is displayed with: the color of the <see cref="SetColorMap">color map</see> when
    /// set, otherwise the gray of that level.
    /// </summary>
    /// <param name="value">The pixel value.</param>
    public Color GetColor(byte value)
    {
        var colorMap = _colorMap;
        return colorMap is null
            ? Color.FromRgb(value, value, value)
            : Color.FromUInt32(colorMap[value] | 0xFF000000u);
    }

    /// <summary>
    /// Locks the pixels for writing, which waits for the drawing in progress. The lock must be disposed to make the
    /// pixels available to the render thread again, which also changes the <see cref="Version"/>.
    /// </summary>
    /// <returns>The pixels, valid until disposed.</returns>
    /// <exception cref="ObjectDisposedException">The image was disposed.</exception>
    public GrayscaleImageLock Lock()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _lock.EnterWriteLock();
        if (_disposed)
        {
            _lock.ExitWriteLock();
            throw new ObjectDisposedException(nameof(GrayscaleImage));
        }

        return new GrayscaleImageLock(this);
    }

    /// <summary>
    /// Copies the pixels from an unmanaged buffer of 8-bit pixels with the same size, for example the data of an
    /// 8-bit single channel OpenCV matrix, and changes the <see cref="Version"/>.
    /// </summary>
    /// <param name="source">The address of the first pixel of the first row.</param>
    /// <param name="sourceRowBytes">The number of bytes between the start of two rows of the source.</param>
    public void CopyFrom(IntPtr source, int sourceRowBytes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sourceRowBytes, Width);

        using var buffer = Lock();
        var src = (byte*)source;
        var dst = (byte*)buffer.Address;
        if (sourceRowBytes == Width)
        {
            Buffer.MemoryCopy(src, dst, (long)Width * Height, (long)Width * Height);
            return;
        }

        for (var y = 0; y < Height; y++)
        {
            Buffer.MemoryCopy(src + (long)y * sourceRowBytes, dst + (long)y * Width, Width, Width);
        }
    }

    /// <summary>
    /// Creates a 32-bit bitmap with the pixels of a region of the image as they are displayed, with the
    /// <see cref="SetColorMap">color map</see> applied.
    /// </summary>
    /// <param name="region">The region to convert, in pixels. It must be inside of the image.</param>
    /// <returns>A new <see cref="PixelFormats.Bgra8888"/> opaque bitmap owned by the caller.</returns>
    public WriteableBitmap ToBitmap(PixelRect region)
    {
        if (
            region.Width <= 0
            || region.Height <= 0
            || region.X < 0
            || region.Y < 0
            || region.Right > Width
            || region.Bottom > Height
        )
        {
            throw new ArgumentOutOfRangeException(nameof(region));
        }

        var bitmap = CreateBgraBitmap(region.Size);
        try
        {
            _lock.EnterReadLock();
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                ConvertToBgra(bitmap, region);
            }
            finally
            {
                _lock.ExitReadLock();
            }

            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Frees the pixels, waiting for the drawing in progress.
    /// </summary>
    public void Dispose()
    {
        _lock.EnterWriteLock();
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _skiaCache?.Dispose();
            _skiaCache = null;
            _fallbackBitmap?.Dispose();
            _fallbackBitmap = null;
            FreePixels();
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        GC.SuppressFinalize(this);
    }

    private void FreePixels()
    {
        var address = Interlocked.Exchange(ref _address, 0);
        if (address == 0)
        {
            return;
        }

        NativeMemory.Free((void*)address);
        GC.RemoveMemoryPressure(_allocatedBytes);
    }

    /// <inheritdoc />
    /// <remarks>Draws the image with nearest neighbor sampling, the <see cref="AdvancedImageBox"/> chooses the sampling
    /// by itself according to its <see cref="AdvancedImageBox.BitmapSamplingMode"/>.</remarks>
    void IImage.Draw(DrawingContext context, Rect sourceRect, Rect destRect)
    {
        Draw(context, sourceRect, destRect, GrayscaleSampling.Nearest, null);
    }

    #region Internals

    internal nint PixelsForLock => _address;

    internal void ReleaseWrite()
    {
        Interlocked.Increment(ref _version);
        _lock.ExitWriteLock();
    }

    internal bool SkiaUnavailable => _skiaUnavailable;

    internal void MarkSkiaUnavailable()
    {
        _skiaUnavailable = true;
    }

    internal static bool IsSkiaUnavailableForProcess;

    /// <summary>
    /// Draws the image, with Skia when possible, otherwise through the 32-bit fallback copy.
    /// </summary>
    internal void Draw(
        DrawingContext context,
        Rect source,
        Rect dest,
        GrayscaleSampling sampling,
        Action? onSkiaUnavailable,
        in LayerStyle style = default
    )
    {
        if (!_skiaUnavailable && !IsSkiaUnavailableForProcess)
        {
            try
            {
                GrayscaleSkia.Draw(this, context, source, dest, sampling, onSkiaUnavailable, style);
                return;
            }
            catch (Exception e)
                when (e
                        is FileNotFoundException
                            or FileLoadException
                            or TypeLoadException
                            or MissingMethodException
                            or MissingFieldException
                            or BadImageFormatException
                )
            {
                // Avalonia.Skia or SkiaSharp are not there, or are another version
                _skiaUnavailable = true;
                IsSkiaUnavailableForProcess = true;
            }
        }

        var bitmap = GetFallbackBitmap();
        if (bitmap is not null)
        {
            context.DrawImage(bitmap, source, dest);
        }
    }

    /// <summary>
    /// For an image created by <see cref="CreateReduced"/>, the level it was reduced by.
    /// </summary>
    internal int ReductionLevel { get; private set; }

    /// <summary>
    /// Creates the image reduced by <c>2^level</c>: each pixel is the average of a block of pixels. The color map is
    /// copied, so it is applied to the averaged values.
    /// </summary>
    /// <param name="level">The level, between 1 and <see cref="BitmapDownscaler.MaxLevel"/>.</param>
    /// <param name="reuse">A previous result to write into when it has the right size, which avoids the allocation.</param>
    /// <returns>The reduction, or <see langword="null"/> when this image was disposed.</returns>
    internal GrayscaleImage? CreateReduced(int level, GrayscaleImage? reuse)
    {
        level = Math.Clamp(level, 1, BitmapDownscaler.MaxLevel);
        var factor = 1 << level;
        var width = (Width + factor - 1) / factor;
        var height = (Height + factor - 1) / factor;

        var result =
            reuse is not null && !reuse._disposed && reuse.Width == width && reuse.Height == height
                ? reuse
                : new GrayscaleImage(width, height);

        _lock.EnterReadLock();
        try
        {
            if (_disposed)
            {
                if (!ReferenceEquals(result, reuse))
                {
                    result.Dispose();
                }

                return null;
            }

            using (var buffer = result.Lock())
            {
                BitmapDownscaler.ReduceGrayscale(
                    (byte*)_address,
                    Width,
                    Width,
                    Height,
                    level,
                    (byte*)buffer.Address,
                    result.Width
                );
            }

            result.ReductionLevel = level;
            if (_colorMap is { } colorMap)
            {
                result.SetColorMap(colorMap);
            }
            else
            {
                result.ClearColorMap();
            }

            return result;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the 32-bit copy of the image used when it can't be drawn by Skia, created again when the
    /// <see cref="Version"/> changed.
    /// </summary>
    internal WriteableBitmap? GetFallbackBitmap()
    {
        _lock.EnterReadLock();
        try
        {
            if (_disposed)
            {
                return null;
            }

            var version = Version;
            if (_fallbackBitmap is not null && _fallbackVersion == version)
            {
                return _fallbackBitmap;
            }

            if (_fallbackBitmap is null)
            {
                _fallbackBitmap = CreateBgraBitmap(PixelSize);
            }

            ConvertToBgra(_fallbackBitmap, new PixelRect(0, 0, Width, Height));
            _fallbackVersion = version;
            return _fallbackBitmap;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Enters the read lock to access the pixels from the render thread.
    /// </summary>
    /// <param name="pixels">The pixels, valid until <see cref="ExitRead"/>.</param>
    /// <param name="version">The version of the pixels and the color map.</param>
    /// <param name="colorMap">The color map or <see langword="null"/>.</param>
    /// <returns><see langword="false"/> when the image was disposed, the read lock is not held then.</returns>
    internal bool TryEnterRead(out byte* pixels, out int version, out uint[]? colorMap)
    {
        _lock.EnterReadLock();
        if (_disposed)
        {
            _lock.ExitReadLock();
            pixels = null;
            version = 0;
            colorMap = null;
            return false;
        }

        pixels = (byte*)_address;
        version = Version;
        colorMap = _colorMap;
        return true;
    }

    internal void ExitRead() => _lock.ExitReadLock();

    /// <summary>
    /// The state of the Skia back end, owned by the image so it is released with it. Only touched under the read
    /// lock by the render thread, which is the only thread drawing.
    /// </summary>
    internal IDisposable? SkiaCache
    {
        get => _skiaCache;
        set => _skiaCache = value;
    }

    private static WriteableBitmap CreateBgraBitmap(PixelSize size)
    {
        return new WriteableBitmap(
            size,
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque
        );
    }

    /// <summary>
    /// Converts a region to the BGRA pixels of the bitmap, the read or write lock must be held.
    /// </summary>
    private void ConvertToBgra(WriteableBitmap bitmap, PixelRect region)
    {
        var colorMap = _colorMap;
        using var framebuffer = bitmap.Lock();
        var width = region.Width;

        for (var y = 0; y < region.Height; y++)
        {
            var src = (byte*)_address + (long)(region.Y + y) * Width + region.X;
            var dst = (uint*)((byte*)framebuffer.Address + (long)y * framebuffer.RowBytes);

            if (colorMap is null)
            {
                for (var x = 0; x < width; x++)
                {
                    var v = (uint)src[x];
                    dst[x] = 0xFF000000u | (v << 16) | (v << 8) | v;
                }
            }
            else
            {
                for (var x = 0; x < width; x++)
                {
                    dst[x] = colorMap[src[x]] | 0xFF000000u;
                }
            }
        }
    }

    #endregion
}

/// <summary>
/// The pixels of a <see cref="GrayscaleImage"/> locked for writing, see <see cref="GrayscaleImage.Lock"/>.
/// </summary>
public readonly unsafe struct GrayscaleImageLock : IDisposable
{
    private readonly GrayscaleImage _image;

    internal GrayscaleImageLock(GrayscaleImage image)
    {
        _image = image;
        Address = image.PixelsForLock;
    }

    /// <summary>Gets the address of the first pixel.</summary>
    public IntPtr Address { get; }

    /// <summary>Gets the number of bytes of a row, which is the width of the image.</summary>
    public int RowBytes => _image.Width;

    /// <summary>Gets the width of the image.</summary>
    public int Width => _image.Width;

    /// <summary>Gets the height of the image.</summary>
    public int Height => _image.Height;

    /// <summary>Gets the pixels as a span of <c>RowBytes * Height</c> bytes.</summary>
    public Span<byte> Span => new((void*)Address, checked(_image.Width * _image.Height));

    /// <summary>
    /// Unlocks the pixels and changes the <see cref="GrayscaleImage.Version"/>.
    /// </summary>
    public void Dispose()
    {
        _image.ReleaseWrite();
    }
}

/// <summary>
/// The sampling used to draw a <see cref="GrayscaleImage"/>.
/// </summary>
internal enum GrayscaleSampling
{
    /// <summary>Nearest neighbor.</summary>
    Nearest,

    /// <summary>Bilinear.</summary>
    Linear,

    /// <summary>Bilinear with mipmaps, for minified drawing.</summary>
    LinearMipmap,
}
