/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.IO;
using UVtools.Core.FileFormats;
using Xunit;

namespace UVtools.Tests;

/// <summary>
/// The Vlare VLR (Peopoly Phenom Forge) layer codec, the hand made streams follow a reverse engineered sample.
/// </summary>
public class VlrRleCodecTests
{
    private static byte[] RoundTrip(byte[] pixels, out uint lines)
    {
        var encoded = VlrRleCodec.Encode(pixels, out lines);
        var decoded = new byte[pixels.Length];
        var end = VlrRleCodec.Decode(encoded, decoded, 0);
        Assert.Equal(pixels.Length, end);
        Assert.Equal(pixels, decoded);
        Assert.Equal(encoded.Length, VlrRleCodec.GetLength(encoded, lines, true));
        return encoded;
    }

    private static byte[] Run(byte gray, int length)
    {
        var result = new byte[length];
        Array.Fill(result, gray);
        return result;
    }

    private static byte[] Concat(params byte[][] parts)
    {
        var result = Array.Empty<byte>();
        foreach (var part in parts) result = [.. result, .. part];
        return result;
    }

    [Fact]
    public void DecodesHandMadeStream()
    {
        // 2 black, 0xA6 white (2 byte length), gray B7 repeated up to 0x7A pixels, a gray single and 0x240 black
        byte[] rle = [0x00, 0x00, 0x02, 0xFF, 0x80, 0xA6, 0xB7, 0x01, 0x7A, 0x9B, 0x00, 0x02, 0x40];
        var pixels = new byte[2 + 0xA6 + 0x7A + 1 + 0x240];
        var end = VlrRleCodec.Decode(rle, pixels, 0);

        Assert.Equal(pixels.Length, end);
        Assert.All(pixels.AsSpan(0, 2).ToArray(), p => Assert.Equal(0, p));
        Assert.All(pixels.AsSpan(2, 0xA6).ToArray(), p => Assert.Equal(255, p));
        Assert.All(pixels.AsSpan(2 + 0xA6, 0x7A).ToArray(), p => Assert.Equal(0xB7, p));
        Assert.Equal(0x9B, pixels[2 + 0xA6 + 0x7A]);
        Assert.All(pixels.AsSpan(2 + 0xA6 + 0x7A + 1).ToArray(), p => Assert.Equal(0, p));
    }

    [Fact]
    public void DecodeStartsAtTheStartPixel()
    {
        byte[] rle = [0x9B, 0x7F, 0xFF, 0x03];
        var pixels = new byte[20];
        var end = VlrRleCodec.Decode(rle, pixels, 10);

        Assert.Equal(15, end);
        Assert.Equal(0x9B, pixels[10]);
        Assert.Equal(0x7F, pixels[11]);
        Assert.Equal([255, 255, 255], pixels[12..15]);
        Assert.Equal(0, pixels[9]);
        Assert.Equal(0, pixels[15]);
    }

    [Fact]
    public void DecodesThreeByteWhiteRun()
    {
        byte[] rle = [0xFF, 0xC0, 0x40, 0x00];
        var pixels = new byte[0x4000];
        Assert.Equal(0x4000, VlrRleCodec.Decode(rle, pixels, 0));
        Assert.All(pixels, p => Assert.Equal(255, p));
    }

    [Fact]
    public void EncodesSingleAndRepeatedGrays()
    {
        Assert.Equal([0x40], VlrRleCodec.Encode(Run(0x40, 1), out var lines));
        Assert.Equal(1u, lines);

        Assert.Equal([0x40, 0x40], VlrRleCodec.Encode(Run(0x40, 2), out lines));
        Assert.Equal(2u, lines);

        Assert.Equal([0x40, 0x01, 0x03], VlrRleCodec.Encode(Run(0x40, 3), out lines));
        Assert.Equal(1u, lines);
    }

    [Fact]
    public void LongGrayRunsAreSplitInChunksThatStartWithASinglePixel()
    {
        var encoded = RoundTrip(Run(0x55, 255 + 255 + 2), out var lines);
        Assert.Equal([0x55, 0x01, 0xFF, 0x55, 0x01, 0xFF, 0x55, 0x55], encoded);
        Assert.Equal(4u, lines);

        encoded = RoundTrip(Run(0x55, 256), out lines);
        Assert.Equal([0x55, 0x01, 0xFF, 0x55], encoded);
        Assert.Equal(2u, lines);
    }

    [Fact]
    public void LongBlackRunsAreSplit()
    {
        var encoded = RoundTrip(Concat(Run(0, 0xFFFE + 10), Run(255, 1)), out var lines);
        Assert.Equal([0x00, 0xFF, 0xFE, 0x00, 0x00, 0x0A, 0xFF, 0x01], encoded);
        Assert.Equal(3u, lines);

        encoded = RoundTrip(Run(0, 0xFFFE), out lines);
        Assert.Equal([0x00, 0xFF, 0xFE], encoded);
        Assert.Equal(1u, lines);
    }

    [Fact]
    public void LongWhiteRunsAreSplit()
    {
        var encoded = RoundTrip(Run(255, 0x3FFF + 0x80), out var lines);
        Assert.Equal([0xFF, 0xBF, 0xFF, 0xFF, 0x80, 0x80], encoded);
        Assert.Equal(2u, lines);

        encoded = RoundTrip(Run(255, 0x7F), out lines);
        Assert.Equal([0xFF, 0x7F], encoded);
        Assert.Equal(1u, lines);

        encoded = RoundTrip(Run(255, 0x80), out lines);
        Assert.Equal([0xFF, 0x80, 0x80], encoded);
        Assert.Equal(1u, lines);
    }

    [Fact]
    public void GrayOneIsWrittenAsGrayTwo()
    {
        var encoded = VlrRleCodec.Encode(Concat(Run(1, 5), Run(2, 3)), out var lines);
        Assert.Equal([0x02, 0x01, 0x05, 0x02, 0x01, 0x03], encoded);
        Assert.Equal(2u, lines);

        var decoded = new byte[8];
        VlrRleCodec.Decode(encoded, decoded, 0);
        Assert.Equal(Run(2, 8), decoded);
    }

    [Fact]
    public void MixedRunsRoundTrip()
    {
        var pixels = Concat(
            Run(0, 300), Run(255, 1), Run(0x20, 2), Run(0x21, 1), Run(0, 1), Run(255, 5000), Run(0xFE, 400),
            Run(0xFF, 1), Run(0x0D, 3), Run(0x0A, 1), Run(0, 70000), Run(255, 2));
        RoundTrip(pixels, out _);
    }

    [Fact]
    public void LengthIncludesTheRepeatsAfterTheLastCountedToken()
    {
        // 2 counted tokens (a black run and a gray single) followed by its repeat, then the next layer mark
        byte[] data = [0x00, 0x00, 0x04, 0x9B, 0x01, 0x10, 0x0D, 0x0A, 0x00, 0x00, 0x00];
        Assert.Equal(6, VlrRleCodec.GetLength(data, 2, false));

        // Layer mark like bytes inside the stream must not end it
        byte[] marker = [0x0D, 0xFF, 0x0A, 0x0D, 0x0A];
        Assert.Equal(3, VlrRleCodec.GetLength(marker, 2, false));
    }

    [Fact]
    public void LengthAsksForMoreDataWhenTheRepeatMayFollow()
    {
        byte[] data = [0x00, 0x00, 0x04, 0x9B];
        Assert.Equal(-1, VlrRleCodec.GetLength(data, 2, false));
        Assert.Equal(4, VlrRleCodec.GetLength(data, 2, true));
        Assert.Equal(-1, VlrRleCodec.GetLength(data[..2], 1, false));
    }

    [Fact]
    public void ReadStopsAtTheNextLayer()
    {
        byte[] first = [0x00, 0x00, 0x04, 0x9B, 0x01, 0x10];
        byte[] second = [0x0D, 0x0A, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x05];
        using var stream = new MemoryStream([.. first, .. second]);

        Assert.Equal(first, VlrRleCodec.Read(stream, 2));
        Assert.Equal(first.Length, stream.Position);
        Assert.Empty(VlrRleCodec.Read(stream, 0));
        Assert.Equal(first.Length, stream.Position);
    }

    [Fact]
    public void ReadGrowsTheChunkForLayersLargerThanIt()
    {
        var pixels = new byte[400_000];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = (byte)(i % 7 == 0 ? 0 : 2 + i % 200);

        var encoded = VlrRleCodec.Encode(pixels, out var lines);
        Assert.True(encoded.Length > 128 * 1024);
        using var stream = new MemoryStream([.. encoded, 0x0D, 0x0A]);
        Assert.Equal(encoded, VlrRleCodec.Read(stream, lines));
        Assert.Equal(encoded.Length, stream.Position);
    }

    [Fact]
    public void CorruptedDataThrows()
    {
        var pixels = new byte[4];
        // Runs past the image
        Assert.Throws<FileLoadException>(() => VlrRleCodec.Decode([0xFF, 0x05], pixels, 0));
        Assert.Throws<FileLoadException>(() => VlrRleCodec.Decode([0x00, 0x00, 0x05], pixels, 0));
        Assert.Throws<FileLoadException>(() => VlrRleCodec.Decode([0x40, 0x01, 0x09], pixels, 0));
        // A repeat without a single pixel, and truncated tokens
        Assert.Throws<FileLoadException>(() => VlrRleCodec.Decode([0x01, 0x03], pixels, 0));
        Assert.Throws<FileLoadException>(() => VlrRleCodec.Decode([0xFF, 0x01, 0x01, 0x02], pixels, 0));
        Assert.Throws<FileLoadException>(() => VlrRleCodec.Decode([0x00, 0x00], pixels, 0));
        Assert.Throws<FileLoadException>(() => VlrRleCodec.Decode([0xFF, 0x80], pixels, 0));
        Assert.Throws<FileLoadException>(() => VlrRleCodec.Decode([0xFF, 0xF0], pixels, 0));
        // Not enough tokens for the number of lines
        Assert.Throws<FileLoadException>(() => VlrRleCodec.GetLength([0x40, 0x41], 3, true));
        Assert.Throws<FileLoadException>(() => VlrRleCodec.GetLength([0x00, 0x00], 1, true));
    }
}
