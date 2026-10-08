/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.IO;
using System.Linq;
using UVtools.Core.FileFormats;
using Xunit;

namespace UVtools.Tests;

/// <summary>
/// The Creality HALOT-X1 pixel pair codec, the row fragment is taken from a reverse engineered HALOT-X1 sample.
/// </summary>
public class PixelPairRleCodecTests
{
    private const int HalotX1Width = 15120;
    private const int HalotX1Height = 6230;

    /// <summary>
    /// The 3 bit gray values expanded to 8 bit.
    /// </summary>
    private static readonly byte[] Gray = [0, 36, 73, 109, 146, 182, 219, 255];

    private static readonly byte[] EmptyRow = [0x80, 0x3B, 0x07];

    // Black, a few single pairs, a repeat of (7,7) and black again, it fills exactly one row
    private static readonly byte[] RowFragment =
    [
        0x80, 0x19, 0x1A, 0x88, 0x9A, 0xA3, 0xAD, 0xB6, 0xBF, 0x0B, 0xB6, 0xAD, 0x9C, 0x93, 0x81, 0x80, 0x21, 0x56
    ];

    [Fact]
    public void EmptyRowIsSingleBlackRun()
    {
        var pixels = new byte[HalotX1Width];
        PixelPairRleCodec.Decode(EmptyRow, pixels, HalotX1Width);

        Assert.All(pixels, pixel => Assert.Equal((byte)0, pixel));
        Assert.True(PixelPairRleCodec.IsMatch(EmptyRow, HalotX1Width, 1));
        Assert.Equal(EmptyRow, PixelPairRleCodec.Encode(pixels, HalotX1Width));
    }

    [Fact]
    public void DecodesReverseEngineeredRow()
    {
        // The fragment fills a whole row, the empty row after it checks that the next row starts at its beginning
        var rle = RowFragment.Concat(EmptyRow).ToArray();
        var pixels = new byte[HalotX1Width * 2];
        PixelPairRleCodec.Decode(rle, pixels, HalotX1Width);

        // (left, right, count) as 3 bit gray pairs
        var runs = new (int Left, int Right, int Count)[]
        {
            (0, 0, 3227), (0, 1, 1), (2, 3, 1), (3, 4, 1), (5, 5, 1), (6, 6, 1), (7, 7, 12),
            (6, 6, 1), (5, 5, 1), (4, 3, 1), (3, 2, 1), (1, 0, 1), (0, 0, 4311)
        };
        var expectedRow = new byte[HalotX1Width];
        var index = 0;
        foreach (var (left, right, count) in runs)
        {
            for (var i = 0; i < count; i++)
            {
                expectedRow[index++] = Gray[left];
                expectedRow[index++] = Gray[right];
            }
        }

        Assert.Equal(HalotX1Width, index);
        Assert.Equal(expectedRow, pixels[..HalotX1Width]);
        Assert.All(pixels[HalotX1Width..], pixel => Assert.Equal((byte)0, pixel));
        Assert.True(PixelPairRleCodec.IsMatch(rle, HalotX1Width, 2));
    }

    [Theory]
    [InlineData(2, 40, 1)]
    [InlineData(1002, 12, 2)]
    [InlineData(HalotX1Width, 8, 3)]
    public void RandomImageRoundTrips(int width, int height, int seed)
    {
        var random = new Random(seed);
        var pixels = new byte[width * height];
        for (var rowStart = 0; rowStart < pixels.Length; rowStart += width)
        {
            var pair = 0;
            while (pair < width / 2)
            {
                // Single pairs and runs of the same pair
                var count = Math.Min(random.Next(3) == 0 ? 1 : random.Next(2, 400), width / 2 - pair);
                var left = Gray[random.Next(Gray.Length)];
                var right = Gray[random.Next(Gray.Length)];
                for (var i = 0; i < count; i++)
                {
                    pixels[rowStart + (pair + i) * 2] = left;
                    pixels[rowStart + (pair + i) * 2 + 1] = right;
                }

                pair += count;
            }
        }

        var rle = PixelPairRleCodec.Encode(pixels, width);
        Assert.True(PixelPairRleCodec.IsMatch(rle, width, height));

        var decoded = new byte[pixels.Length];
        PixelPairRleCodec.Decode(rle, decoded, width);
        Assert.Equal(pixels, decoded);
    }

    [Fact]
    public void ClassicRleIsNotMatched()
    {
        // Bands of 500 pixels, each with its own gray, the classic 7 bit codec encodes them as runs
        var pixels = new byte[HalotX1Width * HalotX1Height];
        for (var y = 0; y < HalotX1Height; y++)
        {
            var row = pixels.AsSpan(y * HalotX1Width, HalotX1Width);
            for (var x = 0; x < HalotX1Width; x += 500)
            {
                row.Slice(x, Math.Min(500, HalotX1Width - x)).Fill((byte)((x / 500 * 29 + y / 250 * 11) % 256));
            }
        }

        var classic = CtbRleCodec.Encode(pixels);
        Assert.False(PixelPairRleCodec.IsMatch(classic, HalotX1Width, HalotX1Height));
    }

    [Fact]
    public void EncodeRejectsOddWidth()
    {
        Assert.Throws<ArgumentException>(() => PixelPairRleCodec.Encode(new byte[15], 5));
    }

    [Fact]
    public void DecodeRejectsRunThatOverflowsRow()
    {
        // A 16 px row holds 8 pixel pairs, the second run starts in the second row and asks for 9 of them
        byte[] rle = [0x80, 0x07, 0x81, 0x08];
        var pixels = new byte[16 * 2];
        Assert.Throws<FileLoadException>(() => PixelPairRleCodec.Decode(rle, pixels, 16));
    }

    [Theory]
    [InlineData(new byte[] { 0x3B, 0x80 })] // A repeat where a pixel pair is expected
    [InlineData(new byte[] { 0xC1 })] // Reserved bit set
    [InlineData(new byte[] { 0x80, 0x07, 0x80 })] // Data after the last row
    public void DecodeRejectsCorruptedData(byte[] rle)
    {
        var pixels = new byte[16];
        Assert.Throws<FileLoadException>(() => PixelPairRleCodec.Decode(rle, pixels, 16));
        Assert.False(PixelPairRleCodec.IsMatch(rle, 16, 1));
    }

    [Theory]
    [InlineData(new byte[] { 0x80, 0x3B, 0x07, 0x80 }, 1)] // Data after the last row
    [InlineData(new byte[] { 0x80, 0x3B, 0x07 }, 2)] // Missing row
    [InlineData(new byte[] { }, 1)] // Empty
    public void IsMatchRejectsIncompleteStreams(byte[] rle, int height)
    {
        Assert.False(PixelPairRleCodec.IsMatch(rle, HalotX1Width, height));
    }
}
