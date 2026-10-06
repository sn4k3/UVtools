/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.IO;
using System.Numerics;
using DotNext.Buffers;

namespace UVtools.Core.FileFormats;

/// <summary>
/// Helpers for the 1 bit per pixel run length planes used to store anti-aliased layers, one plane per gray threshold.
/// Every byte holds a run of up to 125 pixels in the lower 7 bits and the pixel state (on/off) in the high bit.
/// </summary>
public static class BitPlaneRle
{
    /// <summary>
    /// Gets the length of the run starting at <paramref name="index"/> of pixels that are all at or above
    /// <paramref name="threshold"/> or all below it, depending on the state of the first pixel.
    /// </summary>
    internal static int GetRunLength(ReadOnlySpan<byte> pixels, int index, byte threshold)
    {
        var on = pixels[index] >= threshold;
        if (index + 1 >= pixels.Length || pixels[index + 1] >= threshold != on) return 1;

        var rest = pixels[(index + 2)..];
        var end = on
            ? rest.IndexOfAnyExceptInRange(threshold, byte.MaxValue)
            : rest.IndexOfAnyInRange(threshold, byte.MaxValue);
        return end < 0 ? pixels.Length - index : end + 2;
    }

    /// <summary>
    /// Encodes a plane of the image, pixels at or above <paramref name="threshold"/> are on.
    /// </summary>
    /// <param name="output">The writer to append the plane runs</param>
    /// <param name="pixels">The 8 bit image pixels</param>
    /// <param name="threshold">The gray value from which the pixel is on</param>
    /// <param name="maxRunLength">The maximum pixels a single byte can hold</param>
    internal static void EncodePlane(ref BufferWriterSlim<byte> output, ReadOnlySpan<byte> pixels, byte threshold, int maxRunLength)
    {
        var pixel = 0;
        while (pixel < pixels.Length)
        {
            var on = pixels[pixel] >= threshold;
            var runLength = GetRunLength(pixels, pixel, threshold);
            pixel += runLength;

            var onFlag = on ? 0x80 : 0;
            for (; runLength > 0; runLength -= maxRunLength)
            {
                output.Add((byte)(Math.Min(runLength, maxRunLength) | onFlag));
            }
        }
    }

    /// <summary>
    /// Encodes a plane of the image into a new array, pixels at or above <paramref name="threshold"/> are on.
    /// </summary>
    /// <param name="pixels">The 8 bit image pixels</param>
    /// <param name="threshold">The gray value from which the pixel is on</param>
    /// <param name="maxRunLength">The maximum pixels a single byte can hold</param>
    public static byte[] EncodePlane(ReadOnlySpan<byte> pixels, byte threshold, int maxRunLength)
    {
        var output = new BufferWriterSlim<byte>(FileFormat.GetRleBufferInitialCapacity(pixels.Length));
        try
        {
            EncodePlane(ref output, pixels, threshold, maxRunLength);
            return output.WrittenSpan.ToArray();
        }
        finally
        {
            output.Dispose();
        }
    }

    /// <summary>
    /// Decodes a plane, adding one to every pixel that is on.
    /// </summary>
    /// <param name="rle">The encoded plane, it may contain more data than the plane</param>
    /// <param name="counts">The image pixels, holding the number of planes in which each pixel is on</param>
    /// <returns>The number of bytes consumed from <paramref name="rle"/></returns>
    /// <exception cref="FileLoadException">When the plane does not fit the image</exception>
    internal static int DecodePlane(ReadOnlySpan<byte> rle, Span<byte> counts)
    {
        var pixel = 0;
        for (var index = 0; index < rle.Length; index++)
        {
            // Lower 7 bits is the repeat count for the bit (0..127)
            var reps = rle[index] & 0x7f;
            if (reps > counts.Length - pixel)
                throw new FileLoadException("Error image ran off the end");

            // We only need to set the non-zero pixels
            // High bit is on for white, off for black
            if ((rle[index] & 0x80) != 0)
            {
                Increment(counts.Slice(pixel, reps));
            }

            pixel += reps;

            if (pixel == counts.Length)
            {
                return index + 1;
            }
        }

        return rle.Length;
    }

    /// <summary>
    /// Converts the number of planes in which each pixel is on to an 8 bit gray.
    /// </summary>
    /// <param name="counts">The image pixels with the plane counts, converted in place</param>
    /// <param name="antiAliasing">The number of planes</param>
    internal static void CountsToGray(Span<byte> counts, byte antiAliasing)
    {
        var step = 256 / antiAliasing;
        Span<byte> lut = stackalloc byte[256];
        for (var count = 0; count < lut.Length; count++)
        {
            var gray = count * step;
            lut[count] = (byte)(gray > 0 ? gray - 1 : 0);
        }

        for (var i = 0; i < counts.Length; i++)
        {
            counts[i] = lut[counts[i]];
        }
    }

    private static void Increment(Span<byte> span)
    {
        var i = 0;
        if (Vector.IsHardwareAccelerated)
        {
            for (; i <= span.Length - Vector<byte>.Count; i += Vector<byte>.Count)
            {
                var slice = span.Slice(i, Vector<byte>.Count);
                (new Vector<byte>(slice) + Vector<byte>.One).CopyTo(slice);
            }
        }

        for (; i < span.Length; i++)
        {
            span[i]++;
        }
    }
}
