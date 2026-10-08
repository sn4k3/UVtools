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
/// The pixel pair run length codec with a 3 bit gray per pixel, used by the layers of the Creality HALOT-X1 CXDLPv4 files.
/// </summary>
/// <remarks>
/// The image is encoded row by row and a run never crosses a row, so the width must be even.
/// A byte with the high bit set is a unit of two horizontally adjacent pixels: the 3 bit gray of the left pixel is in
/// bits 0-2 and the 3 bit gray of the right pixel is in bits 3-5, bit 6 is never set.
/// The bytes that follow a unit with the high bit clear are the big endian 7 bit groups of a repeat value, the unit is
/// written <c>repeat + 1</c> times. A unit without those bytes is written once.
/// </remarks>
public static class PixelPairRleCodec
{
    private const byte UnitFlag = 0x80;
    private const byte ReservedFlag = 0x40;
    private const int GrayMask = 0x7;
    private const int RightGrayShift = 3;
    private const int PixelGrayShift = 5; // 8 bit pixel to 3 bit gray
    private const int RepeatBits = 7;
    private const int RepeatMask = 0x7f;

    /// <summary>
    /// Decodes <paramref name="rle"/> into <paramref name="pixels"/>.
    /// </summary>
    /// <param name="rle">The (already decrypted) encoded data</param>
    /// <param name="pixels">A zero initialized 8 bit image buffer, its length must be a multiple of the width</param>
    /// <param name="width">The image width in pixels, it must be even</param>
    /// <exception cref="ArgumentException">When the width is not even or the buffer length is not a multiple of it</exception>
    /// <exception cref="FileLoadException">When the data is malformed or its runs do not fit in the image</exception>
    public static void Decode(ReadOnlySpan<byte> rle, Span<byte> pixels, int width)
    {
        ValidateWidth(width);
        if (pixels.Length % width != 0)
        {
            throw new ArgumentException("The pixels length must be a multiple of the width.", nameof(pixels));
        }

        var unitsPerRow = width / 2;
        var totalUnits = pixels.Length / 2;
        var unit = 0; // The pixels of a unit start at twice its index, the rows are contiguous
        var n = 0;
        while (n < rle.Length)
        {
            var code = rle[n++];
            if ((code & UnitFlag) == 0)
            {
                throw new FileLoadException("Corrupted RLE data, a repeat was found where a pixel pair was expected.");
            }

            if ((code & ReservedFlag) != 0)
            {
                throw new FileLoadException("Corrupted RLE data, invalid pixel pair.");
            }

            if (unit >= totalUnits)
            {
                throw new FileLoadException("Corrupted RLE data, the image ran off the end.");
            }

            // Units left in the current row, a run can not go past it
            var remaining = unitsPerRow - unit % unitsPerRow;
            var repeat = 0;
            while (n < rle.Length && (rle[n] & UnitFlag) == 0)
            {
                repeat = (repeat << RepeatBits) | rle[n++];
                if (repeat >= remaining)
                {
                    throw new FileLoadException("Corrupted RLE data, a run ran off the end of its row.");
                }
            }

            var count = repeat + 1;
            var left = ExpandGray(code & GrayMask);
            var right = ExpandGray((code >> RightGrayShift) & GrayMask);
            var target = pixels.Slice(unit * 2, count * 2);
            if (left == right)
            {
                // Black is the value of the zero initialized image, there is nothing to write
                if (left != 0)
                {
                    target.Fill(left);
                }
            }
            else
            {
                for (var i = 0; i < target.Length; i += 2)
                {
                    target[i] = left;
                    target[i + 1] = right;
                }
            }

            unit += count;
        }
    }

    /// <summary>
    /// Encodes an 8 bit image into the pixel pair format, every pixel gray is quantized to 3 bits.
    /// </summary>
    /// <param name="pixels">The 8 bit image pixels, row by row</param>
    /// <param name="width">The image width in pixels, it must be even</param>
    /// <returns>The encoded (not encrypted) data</returns>
    /// <exception cref="ArgumentException">When the width is not even or the pixels length is not a multiple of it</exception>
    public static byte[] Encode(ReadOnlySpan<byte> pixels, int width)
    {
        ValidateWidth(width);
        if (pixels.Length % width != 0)
        {
            throw new ArgumentException("The pixels length must be a multiple of the width.", nameof(pixels));
        }

        var rawData = new BufferWriterSlim<byte>(
            FileFormat.GetRleBufferInitialCapacity(
                pixels.Length,
                estimatedPixelsPerRun: 128,
                encodedBytesPerRun: 2));
        try
        {
            var unitsPerRow = width / 2;
            for (var rowStart = 0; rowStart < pixels.Length; rowStart += width)
            {
                var row = pixels.Slice(rowStart, width);
                var unit = 0;
                while (unit < unitsPerRow)
                {
                    var value = GetUnit(row, unit);
                    var count = 1;
                    while (unit + count < unitsPerRow && GetUnit(row, unit + count) == value)
                    {
                        count++;
                    }

                    AddUnit(ref rawData, value, count);
                    unit += count;
                }
            }

            return rawData.WrittenSpan.ToArray();
        }
        finally
        {
            rawData.Dispose();
        }
    }

    /// <summary>
    /// Checks, without decoding, that <paramref name="rle"/> is a complete pixel pair stream of the given image size.
    /// </summary>
    /// <param name="rle">The (already decrypted) encoded data</param>
    /// <param name="width">The image width in pixels</param>
    /// <param name="height">The image height in pixels</param>
    /// <returns>
    /// True when the data starts with a unit, every run fits in its row, exactly <paramref name="height"/> rows are
    /// produced and no data is left over, otherwise false. Always false when the width is not even.
    /// </returns>
    /// <remarks>Used to detect the codec of a layer, it never throws.</remarks>
    public static bool IsMatch(ReadOnlySpan<byte> rle, int width, int height)
    {
        if (width <= 0 || (width & 1) != 0 || height <= 0)
        {
            return false;
        }

        var unitsPerRow = width / 2;
        var totalUnits = (long)unitsPerRow * height;
        long unit = 0;
        var n = 0;
        while (n < rle.Length)
        {
            var code = rle[n++];
            if ((code & UnitFlag) == 0 || (code & ReservedFlag) != 0 || unit >= totalUnits)
            {
                return false;
            }

            var remaining = unitsPerRow - unit % unitsPerRow;
            long repeat = 0;
            while (n < rle.Length && (rle[n] & UnitFlag) == 0)
            {
                repeat = (repeat << RepeatBits) | rle[n++];
                if (repeat >= remaining)
                {
                    return false;
                }
            }

            unit += repeat + 1;
        }

        return unit == totalUnits;
    }

    private static byte GetUnit(ReadOnlySpan<byte> row, int unit)
    {
        var index = unit * 2;
        return (byte)(UnitFlag |
                      (row[index] >> PixelGrayShift) |
                      ((row[index + 1] >> PixelGrayShift) << RightGrayShift));
    }

    private static void AddUnit(ref BufferWriterSlim<byte> rawData, byte unit, int count)
    {
        rawData.Add(unit);

        var repeat = count - 1;
        if (repeat == 0)
        {
            return;
        }

        // The fewest 7 bit groups, the most significant group first
        var groups = 1;
        for (var rest = repeat >> RepeatBits; rest != 0; rest >>= RepeatBits)
        {
            groups++;
        }

        for (var group = groups - 1; group >= 0; group--)
        {
            rawData.Add((byte)((repeat >> (group * RepeatBits)) & RepeatMask));
        }
    }

    /// <summary>
    /// Expands a 3 bit gray to the 8 bit gray, 0 maps to 0 and 7 maps to 255.
    /// </summary>
    private static byte ExpandGray(int gray3) => (byte)((gray3 << 5) | (gray3 << 2) | (gray3 >> 1));

    private static void ValidateWidth(int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        if ((width & 1) != 0)
        {
            throw new ArgumentException("The pixel pair codec requires an even width.", nameof(width));
        }
    }
}
