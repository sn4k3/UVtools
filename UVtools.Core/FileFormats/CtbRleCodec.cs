/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.IO;
using DotNext.Buffers;

namespace UVtools.Core.FileFormats;

/// <summary>
/// The 7 bit grayscale run length codec shared by the Chitubox CTB, Creality CXDLP v4 and encrypted CTB formats.
/// </summary>
/// <remarks>
/// Each entry is a byte with the 7 bit gray value, when the high bit is set it is followed by a variable length
/// (1 to 4 bytes) run length, otherwise the entry is a single pixel.
/// </remarks>
public static class CtbRleCodec
{
    private const uint MaxRunLength = 0xfffffff;

    /// <summary>
    /// Decodes <paramref name="rle"/> into <paramref name="pixels"/>.
    /// </summary>
    /// <param name="rle">The (already decrypted) encoded data</param>
    /// <param name="pixels">A zero initialized 8 bit image buffer</param>
    /// <exception cref="FileLoadException">When the data is truncated, malformed or its non-black runs do not fit the image</exception>
    public static void Decode(ReadOnlySpan<byte> rle, Span<byte> pixels)
    {
        long pixel = 0;
        for (var n = 0; n < rle.Length; n++)
        {
            var code = rle[n];
            var stride = 1;

            if ((code & 0x80) == 0x80) // It's a run
            {
                code &= 0x7f; // Get the gray value
                n++;
                if ((uint)n >= (uint)rle.Length) throw new FileLoadException("Corrupted RLE data, truncated run.");

                var slen = rle[n];
                int extraBytes;

                if ((slen & 0x80) == 0)
                {
                    stride = slen;
                    extraBytes = 0;
                }
                else if ((slen & 0xc0) == 0x80)
                {
                    extraBytes = 1;
                    if (n + extraBytes >= rle.Length) throw new FileLoadException("Corrupted RLE data, truncated run.");
                    stride = ((slen & 0x3f) << 8) + rle[n + 1];
                }
                else if ((slen & 0xe0) == 0xc0)
                {
                    extraBytes = 2;
                    if (n + extraBytes >= rle.Length) throw new FileLoadException("Corrupted RLE data, truncated run.");
                    stride = ((slen & 0x1f) << 16) + (rle[n + 1] << 8) + rle[n + 2];
                }
                else if ((slen & 0xf0) == 0xe0)
                {
                    extraBytes = 3;
                    if (n + extraBytes >= rle.Length) throw new FileLoadException("Corrupted RLE data, truncated run.");
                    stride = ((slen & 0xf) << 24) + (rle[n + 1] << 16) + (rle[n + 2] << 8) + rle[n + 3];
                }
                else
                {
                    throw new FileLoadException("Corrupted RLE data");
                }

                n += extraBytes;
            }

            // The image is zero initialized, black runs do not need to be written. They are also tolerated past the
            // end of the image as the encrypted CTB layers can carry padding that decodes as black pixels
            if (code != 0)
            {
                if (stride > pixels.Length - pixel)
                {
                    throw new FileLoadException("Corrupted RLE data, the image ran off the end.");
                }

                // Bit extend from 7-bit to 8-bit greymap
                pixels.Slice((int)pixel, stride).Fill((byte)((code << 1) | 1));
            }

            pixel += stride;
        }
    }

    /// <summary>
    /// Encodes an 8 bit image into the 7 bit run length format.
    /// </summary>
    /// <param name="pixels">The 8 bit image pixels</param>
    /// <returns>The encoded (not encrypted) data</returns>
    public static byte[] Encode(ReadOnlySpan<byte> pixels)
    {
        var rawData = new BufferWriterSlim<byte>(
            FileFormat.GetRleBufferInitialCapacity(
                pixels.Length,
                estimatedPixelsPerRun: 128,
                encodedBytesPerRun: 2));
        try
        {
            var color = (byte)(byte.MaxValue >> 1);
            uint stride = 0;

            var pixel = 0;
            while (pixel < pixels.Length)
            {
                var grey7 = (byte)(pixels[pixel] >> 1);

                // Every pixel with the same 7 bit gray belongs to the run
                var runLength = 1;
                if (pixel + 1 < pixels.Length && pixels[pixel + 1] >> 1 == grey7)
                {
                    var low = (byte)(grey7 << 1);
                    var different = pixels[(pixel + 2)..].IndexOfAnyExcept(low, (byte)(low | 1));
                    runLength = different < 0 ? pixels.Length - pixel : different + 2;
                }

                if (grey7 == color)
                {
                    stride += (uint)runLength;
                }
                else
                {
                    AddRep(ref rawData, stride, color);
                    color = grey7;
                    stride = (uint)runLength;
                }

                pixel += runLength;
            }

            AddRep(ref rawData, stride, color);

            return rawData.WrittenSpan.ToArray();
        }
        finally
        {
            rawData.Dispose();
        }
    }

    private static void AddRep(ref BufferWriterSlim<byte> rawData, uint stride, byte color)
    {
        if (stride == 0)
        {
            return;
        }

        if (stride > MaxRunLength)
        {
            // Split runs longer than what the format is able to represent
            AddRep(ref rawData, MaxRunLength, color);
            AddRep(ref rawData, stride - MaxRunLength, color);
            return;
        }

        if (stride > 1)
        {
            color |= 0x80;
        }

        rawData.Add(color);

        if (stride <= 1)
        {
            // no run needed
            return;
        }

        if (stride <= 0x7f)
        {
            rawData.Add((byte)stride);
            return;
        }

        if (stride <= 0x3fff)
        {
            rawData.Add((byte)((stride >> 8) | 0x80));
            rawData.Add((byte)stride);
            return;
        }

        if (stride <= 0x1fffff)
        {
            rawData.Add((byte)((stride >> 16) | 0xc0));
            rawData.Add((byte)(stride >> 8));
            rawData.Add((byte)stride);
            return;
        }

        rawData.Add((byte)((stride >> 24) | 0xe0));
        rawData.Add((byte)(stride >> 16));
        rawData.Add((byte)(stride >> 8));
        rawData.Add((byte)stride);
    }
}
