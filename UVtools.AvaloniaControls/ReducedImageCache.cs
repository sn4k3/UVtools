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
using Avalonia.Media.Imaging;

namespace UVtools.AvaloniaControls;

/// <summary>
/// The reductions by powers of two of one image, which the <see cref="AdvancedImageBox"/> draws when the image is
/// zoomed out. The main image, each composite layer and the minimap have their own, because they reduce by different
/// levels.
/// </summary>
/// <remarks>The reductions are rebuilt only when the content of the image changed, see
/// <see cref="BitmapDownscaler"/>.</remarks>
internal sealed class ReducedImageCache : IDisposable
{
    private WriteableBitmap? _source;
    private int _version;
    private readonly Dictionary<int, WriteableBitmap> _bitmaps = new();
    private readonly Dictionary<int, WriteableBitmap> _spareBitmaps = new();
    private GrayscaleImage? _grayscale;
    private GrayscaleImage? _grayscaleSource;
    private int _grayscaleVersion = -1;
    private int _grayscaleLevel;

    /// <summary>
    /// Gets the reduction by <c>2^level</c> of the bitmap, created when the image changed or the level was not used.
    /// </summary>
    /// <param name="bitmap">The bitmap to reduce.</param>
    /// <param name="level">The level, between 1 and <see cref="BitmapDownscaler.MaxLevel"/>.</param>
    /// <param name="imageVersion">A number which changes when the pixels of the bitmap changed.</param>
    public WriteableBitmap GetBitmap(WriteableBitmap bitmap, int level, int imageVersion)
    {
        if (!ReferenceEquals(_source, bitmap))
        {
            Clear();
            _source = bitmap;
            _version = imageVersion;
        }
        else if (_version != imageVersion)
        {
            // The pixels changed. The bitmaps are kept to be written again, because allocating the pixels of a large
            // one takes as long as reducing the image
            foreach (var (key, stale) in _bitmaps)
            {
                if (_spareBitmaps.Remove(key, out var previous)) previous.Dispose();
                _spareBitmaps[key] = stale;
            }

            _bitmaps.Clear();
            _version = imageVersion;
        }

        if (!_bitmaps.TryGetValue(level, out var reduced))
        {
            _spareBitmaps.Remove(level, out var spare);
            reduced = BitmapDownscaler.Reduce(bitmap, level, spare);
            if (!ReferenceEquals(reduced, spare)) spare?.Dispose();
            _bitmaps[level] = reduced;

            // Only the level in use is worth keeping for the next change
            foreach (var other in _spareBitmaps.Values)
            {
                other.Dispose();
            }

            _spareBitmaps.Clear();
        }

        return reduced;
    }

    /// <summary>
    /// Gets the reduction by <c>2^level</c> of the grayscale image, created when its pixels changed, which is known by
    /// its <see cref="GrayscaleImage.Version"/>, or the level was not used.
    /// </summary>
    public GrayscaleImage? GetGrayscale(GrayscaleImage image, int level)
    {
        var reduced = _grayscale;
        if (reduced is not null && ReferenceEquals(_grayscaleSource, image) &&
            _grayscaleVersion == image.Version && _grayscaleLevel == level)
        {
            return reduced;
        }

        var version = image.Version;
        var newReduced = image.CreateReduced(level, ReferenceEquals(_grayscaleSource, image) ? reduced : null);
        if (!ReferenceEquals(newReduced, reduced)) reduced?.Dispose();
        _grayscale = newReduced;
        _grayscaleSource = newReduced is null ? null : image;
        _grayscaleVersion = version;
        _grayscaleLevel = level;
        return newReduced;
    }

    /// <summary>
    /// Disposes the reductions.
    /// </summary>
    public void Clear()
    {
        _source = null;
        if (_grayscale is not null)
        {
            _grayscale.Dispose();
            _grayscale = null;
            _grayscaleSource = null;
        }

        if (_bitmaps.Count > 0)
        {
            foreach (var reduced in _bitmaps.Values)
            {
                reduced.Dispose();
            }

            _bitmaps.Clear();
        }

        if (_spareBitmaps.Count > 0)
        {
            foreach (var spare in _spareBitmaps.Values)
            {
                spare.Dispose();
            }

            _spareBitmaps.Clear();
        }
    }

    public void Dispose() => Clear();
}
