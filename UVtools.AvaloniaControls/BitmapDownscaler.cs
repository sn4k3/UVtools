/*
 *                               The MIT License (MIT)
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so.
 */

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Vector = System.Numerics.Vector;

namespace UVtools.AvaloniaControls;

/// <summary>
/// Creates reduced copies of a bitmap, each pixel being the average of a block of the source pixels, which the
/// <see cref="AdvancedImageBox"/> draws when the image is zoomed out, because sampling a huge image with a handful of
/// pixels per screen pixel aliases and shimmers.
/// </summary>
/// <remarks>
/// Letting Avalonia do the same with <c>BitmapInterpolationMode.HighQuality</c> rebuilds the mipmaps of a
/// <see cref="WriteableBitmap"/> on every frame, about 50 ms for a 13312x5120 bitmap, because the bitmap can change
/// at any time. This builds the reduction once for each content of the image instead.
/// </remarks>
internal static unsafe class BitmapDownscaler
{
    /// <summary>The largest reduction level, a block of 64x64 pixels.</summary>
    public const int MaxLevel = 6;

    /// <summary>
    /// Gets if a bitmap has a pixel format which can be reduced: 8 bits per channel, with the alpha last.
    /// </summary>
    public static bool IsSupported(WriteableBitmap bitmap)
    {
        return bitmap.Format == PixelFormat.Bgra8888 || bitmap.Format == PixelFormat.Rgba8888;
    }

    /// <summary>
    /// Creates a copy of the bitmap reduced by <c>2^level</c> in both axes. The pixel at (x, y) of the result is the
    /// average of the source pixels from (x * 2^level, y * 2^level), the last blocks are smaller when the size of the
    /// source is not a multiple.
    /// </summary>
    /// <param name="source">The bitmap to reduce, with a <see cref="IsSupported">supported format</see>.</param>
    /// <param name="level">The level, between 1 and <see cref="MaxLevel"/>.</param>
    /// <param name="reuse">
    /// A previous result to write into instead of creating a bitmap, which saves the allocation of the pixels, the
    /// larger levels spend as much time on it as on reducing. It is ignored when it doesn't have the right size and
    /// format.
    /// </param>
    /// <returns>A new bitmap with the same format, owned by the caller, or <paramref name="reuse"/>.</returns>
    public static WriteableBitmap Reduce(WriteableBitmap source, int level, WriteableBitmap? reuse = null)
    {
        level = Math.Clamp(level, 1, MaxLevel);
        var factor = 1 << level;
        var sourceSize = source.PixelSize;
        var width = (sourceSize.Width + factor - 1) / factor;
        var height = (sourceSize.Height + factor - 1) / factor;
        var alphaFormat = source.AlphaFormat ?? AlphaFormat.Unpremul;

        var reused = reuse is not null && reuse.PixelSize.Width == width && reuse.PixelSize.Height == height &&
                     reuse.Format == source.Format && reuse.AlphaFormat == alphaFormat;
        var result = reused
            ? reuse!
            : new WriteableBitmap(new PixelSize(width, height), source.Dpi, source.Format!.Value, alphaFormat);
        try
        {
            using var src = source.Lock();
            using var dst = result.Lock();

            var srcAddress = (nint)src.Address;
            var dstAddress = (nint)dst.Address;
            var srcRowBytes = src.RowBytes;
            var dstRowBytes = dst.RowBytes;
            var srcWidth = src.Size.Width;
            var srcHeight = src.Size.Height;
            var mode = alphaFormat switch
            {
                AlphaFormat.Unpremul => AlphaMode.Weighted,
                AlphaFormat.Opaque => AlphaMode.Opaque,
                _ => AlphaMode.Straight
            };

            Parallel.For(0, height, () => new ushort[srcWidth * 4], (y, _, rowSums) =>
            {
                ReduceRow((byte*)srcAddress, srcRowBytes, srcWidth, srcHeight, factor,
                    (uint*)((byte*)dstAddress + (long)y * dstRowBytes), width, y, mode, rowSums);
                return rowSums;
            }, _ => { });

            return result;
        }
        catch
        {
            if (!reused) result.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Reduces 8-bit pixels by <c>2^level</c> in both axes like <see cref="Reduce"/>.
    /// </summary>
    /// <param name="source">The first pixel of the source.</param>
    /// <param name="sourceRowBytes">The bytes between two rows of the source.</param>
    /// <param name="sourceWidth">The width of the source.</param>
    /// <param name="sourceHeight">The height of the source.</param>
    /// <param name="level">The level, between 1 and <see cref="MaxLevel"/>.</param>
    /// <param name="destination">The first pixel of the result, of the size of the reduction.</param>
    /// <param name="destinationRowBytes">The bytes between two rows of the result.</param>
    public static void ReduceGrayscale(byte* source, int sourceRowBytes, int sourceWidth, int sourceHeight, int level,
        byte* destination, int destinationRowBytes)
    {
        level = Math.Clamp(level, 1, MaxLevel);
        var factor = 1 << level;
        var width = (sourceWidth + factor - 1) / factor;
        var height = (sourceHeight + factor - 1) / factor;
        var srcAddress = (nint)source;
        var dstAddress = (nint)destination;

        Parallel.For(0, height, () => new ushort[sourceWidth], (dy, _, sums) =>
        {
            var y0 = dy * factor;
            var y1 = Math.Min(y0 + factor, sourceHeight);
            Array.Clear(sums, 0, sourceWidth);
            fixed (ushort* s = sums)
            {
                for (var y = y0; y < y1; y++)
                {
                    var row = (byte*)srcAddress + (long)y * sourceRowBytes;
                    var i = 0;
                    if (Vector.IsHardwareAccelerated)
                    {
                        var step = Vector<byte>.Count;
                        var half = Vector<ushort>.Count;
                        for (; i <= sourceWidth - step; i += step)
                        {
                            var v = Unsafe.ReadUnaligned<Vector<byte>>(row + i);
                            Unsafe.WriteUnaligned(s + i, Unsafe.ReadUnaligned<Vector<ushort>>(s + i) + Vector.WidenLower(v));
                            Unsafe.WriteUnaligned(s + i + half,
                                Unsafe.ReadUnaligned<Vector<ushort>>(s + i + half) + Vector.WidenUpper(v));
                        }
                    }

                    for (; i < sourceWidth; i++)
                    {
                        s[i] += row[i];
                    }
                }

                var dst = (byte*)dstAddress + (long)dy * destinationRowBytes;
                for (var dx = 0; dx < width; dx++)
                {
                    var x0 = dx * factor;
                    var x1 = Math.Min(x0 + factor, sourceWidth);
                    var count = (uint)((y1 - y0) * (x1 - x0));
                    uint sum = 0;
                    for (var x = x0; x < x1; x++)
                    {
                        sum += s[x];
                    }

                    var rounding = count / 2;
                    dst[dx] = (byte)(BitOperations.IsPow2(count)
                        ? (sum + rounding) >> BitOperations.Log2(count)
                        : (sum + rounding) / count);
                }
            }

            return sums;
        }, _ => { });
    }

    private enum AlphaMode
    {
        /// <summary>The alpha is ignored, the result is opaque.</summary>
        Opaque,

        /// <summary>Premultiplied alpha: all the channels are averaged.</summary>
        Straight,

        /// <summary>Unpremultiplied alpha: the colors are weighted by the alpha.</summary>
        Weighted
    }

    /// <summary>
    /// Creates a row of the reduction. The rows of the block are first added into <paramref name="rowSums"/> with
    /// vectors, a sum of 64 bytes fits a 16-bit lane, then the columns of every block are added.
    /// </summary>
    private static void ReduceRow(byte* source, int sourceRowBytes, int sourceWidth, int sourceHeight, int factor,
        uint* destinationRow, int destinationWidth, int destinationY, AlphaMode mode, ushort[] rowSums)
    {
        var y0 = destinationY * factor;
        var y1 = Math.Min(y0 + factor, sourceHeight);
        var byteCount = sourceWidth * 4;

        Array.Clear(rowSums, 0, byteCount);
        fixed (ushort* sums = rowSums)
        {
            for (var y = y0; y < y1; y++)
            {
                var row = source + (long)y * sourceRowBytes;
                var i = 0;
                if (Vector.IsHardwareAccelerated)
                {
                    var step = Vector<byte>.Count;
                    var half = Vector<ushort>.Count;
                    for (; i <= byteCount - step; i += step)
                    {
                        var v = Unsafe.ReadUnaligned<Vector<byte>>(row + i);
                        var lo = Unsafe.ReadUnaligned<Vector<ushort>>(sums + i) + Vector.WidenLower(v);
                        var hi = Unsafe.ReadUnaligned<Vector<ushort>>(sums + i + half) + Vector.WidenUpper(v);
                        Unsafe.WriteUnaligned(sums + i, lo);
                        Unsafe.WriteUnaligned(sums + i + half, hi);
                    }
                }

                for (; i < byteCount; i++)
                {
                    sums[i] += row[i];
                }
            }

            for (var dx = 0; dx < destinationWidth; dx++)
            {
                var x0 = dx * factor;
                var x1 = Math.Min(x0 + factor, sourceWidth);
                var count = (uint)((y1 - y0) * (x1 - x0));

                uint s0 = 0, s1 = 0, s2 = 0, sa = 0;
                var p = sums + x0 * 4;
                for (var x = x0; x < x1; x++)
                {
                    s0 += p[0];
                    s1 += p[1];
                    s2 += p[2];
                    sa += p[3];
                    p += 4;
                }

                uint a, c0, c1, c2;
                if (mode == AlphaMode.Weighted && sa != 255 * count)
                {
                    // Some pixels are not opaque, the colors of transparent pixels must not count
                    uint w0 = 0, w1 = 0, w2 = 0;
                    for (var y = y0; y < y1; y++)
                    {
                        var q = (uint*)(source + (long)y * sourceRowBytes) + x0;
                        for (var x = x0; x < x1; x++)
                        {
                            var v = *q++;
                            var alpha = v >> 24;
                            w0 += (v & 0xFF) * alpha;
                            w1 += ((v >> 8) & 0xFF) * alpha;
                            w2 += ((v >> 16) & 0xFF) * alpha;
                        }
                    }

                    a = (sa + count / 2) / count;
                    if (sa == 0)
                    {
                        c0 = c1 = c2 = 0;
                    }
                    else
                    {
                        var rounding = sa / 2;
                        c0 = (w0 + rounding) / sa;
                        c1 = (w1 + rounding) / sa;
                        c2 = (w2 + rounding) / sa;
                    }
                }
                else
                {
                    var rounding = count / 2;
                    if (BitOperations.IsPow2(count))
                    {
                        // Every block but the ones at the right and bottom edges, a shift is a lot faster
                        var shift = BitOperations.Log2(count);
                        c0 = (s0 + rounding) >> shift;
                        c1 = (s1 + rounding) >> shift;
                        c2 = (s2 + rounding) >> shift;
                        a = mode == AlphaMode.Opaque ? 255 : (sa + rounding) >> shift;
                    }
                    else
                    {
                        c0 = (s0 + rounding) / count;
                        c1 = (s1 + rounding) / count;
                        c2 = (s2 + rounding) / count;
                        a = mode == AlphaMode.Opaque ? 255 : (sa + rounding) / count;
                    }
                }

                destinationRow[dx] = c0 | (c1 << 8) | (c2 << 16) | (a << 24);
            }
        }
    }
}
