/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Buffers;
using System.IO;
using DotNext.Buffers;

namespace UVtools.Core.FileFormats;

/// <summary>
/// The layer run length codec of the Vlare VLR files (Peopoly Phenom Forge).
/// </summary>
/// <remarks>
/// The stream covers whole image rows, from the first pixel of the start row up to the end of the last row with a lit
/// pixel. Tokens are read sequentially:
/// <list type="bullet">
/// <item><c>00 HH LL</c>: a black run, the length is a 16 bit big endian integer.</item>
/// <item><c>FF L...</c>: a white run, the length is 1 byte when below 0x80, 2 bytes (<c>10xxxxxx</c> + 1 byte) up to
/// 0x3FFF, or 3 bytes (<c>110xxxxx</c> + 2 bytes) which is only accepted by the decoder.</item>
/// <item><c>01 N</c>: repeats the previous single gray pixel until its run totals N pixels. It is not counted in the
/// number of lines of the layer.</item>
/// <item>any other byte: a single pixel with that exact 8 bit gray.</item>
/// </list>
/// The number of lines of a layer is the count of the black, white and single pixel tokens, the repeat tokens excluded,
/// so the length of the stream is found by counting tokens, never by searching for the next layer mark.
/// </remarks>
public static class VlrRleCodec
{
    private const byte BlackToken = 0x00;
    private const byte RepeatToken = 0x01;
    private const byte WhiteToken = 0xFF;

    /// <summary>
    /// The longest black run a single token is able to carry, longer runs are split.
    /// </summary>
    public const int MaxBlackRun = 0xFFFE;

    /// <summary>
    /// The longest white run the encoder writes in a single token, longer runs are split.
    /// </summary>
    public const int MaxWhiteRun = 0x3FFF;

    /// <summary>
    /// The longest gray run a single repeat token is able to carry, longer runs are split.
    /// </summary>
    public const int MaxGrayRun = 0xFF;

    private const int ReadChunkSize = 64 * 1024;

    /// <summary>
    /// Decodes <paramref name="rle"/> into <paramref name="pixels"/>.
    /// </summary>
    /// <param name="rle">The encoded data of one layer</param>
    /// <param name="pixels">A zero initialized 8 bit image buffer, black runs only advance the position</param>
    /// <param name="startPixel">The index of the first pixel the data covers, the start of the start row</param>
    /// <returns>The index of the pixel after the last decoded pixel</returns>
    /// <exception cref="FileLoadException">When the data is malformed or the pixels do not fit in the image</exception>
    public static int Decode(ReadOnlySpan<byte> rle, Span<byte> pixels, int startPixel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startPixel);

        var pixel = startPixel;
        var n = 0;
        var previousGray = -1; // The gray of the previous token when it was a single pixel
        while (n < rle.Length)
        {
            var code = rle[n++];
            switch (code)
            {
                case BlackToken:
                {
                    if (n + 2 > rle.Length) throw Truncated();
                    var length = (rle[n] << 8) | rle[n + 1];
                    n += 2;
                    EnsureFits(pixels, pixel, length);
                    pixel += length;
                    previousGray = -1;
                    break;
                }
                case WhiteToken:
                {
                    if (n >= rle.Length) throw Truncated();
                    var size = GetWhiteRunLengthSize(rle[n]);
                    if (n + size > rle.Length) throw Truncated();
                    var length = size switch
                    {
                        1 => rle[n],
                        2 => ((rle[n] & 0x3F) << 8) | rle[n + 1],
                        _ => ((rle[n] & 0x1F) << 16) | (rle[n + 1] << 8) | rle[n + 2]
                    };
                    n += size;
                    EnsureFits(pixels, pixel, length);
                    pixels.Slice(pixel, length).Fill(WhiteToken);
                    pixel += length;
                    previousGray = -1;
                    break;
                }
                case RepeatToken:
                {
                    if (n >= rle.Length) throw Truncated();
                    var total = rle[n++];
                    if (previousGray < 0 || total == 0)
                    {
                        throw new FileLoadException("Corrupted RLE data, a repeat was found without a single pixel to repeat.");
                    }

                    var extra = total - 1; // The single pixel is already written
                    EnsureFits(pixels, pixel, extra);
                    pixels.Slice(pixel, extra).Fill((byte)previousGray);
                    pixel += extra;
                    previousGray = -1;
                    break;
                }
                default:
                    EnsureFits(pixels, pixel, 1);
                    pixels[pixel++] = code;
                    previousGray = code;
                    break;
            }
        }

        return pixel;
    }

    /// <summary>
    /// Encodes the rows of an 8 bit image.
    /// </summary>
    /// <param name="pixels">The pixels to encode, from the start of the first row to the end of the last row</param>
    /// <param name="numberOfLines">The number of black, white and single pixel tokens written, the repeats excluded</param>
    /// <returns>The encoded data</returns>
    /// <remarks>The gray 1 is not representable as a single pixel, it is written as the gray 2.</remarks>
    public static byte[] Encode(ReadOnlySpan<byte> pixels, out uint numberOfLines)
    {
        var rawData = new BufferWriterSlim<byte>(
            FileFormat.GetRleBufferInitialCapacity(
                pixels.Length,
                estimatedPixelsPerRun: 128,
                encodedBytesPerRun: 2));
        try
        {
            uint lines = 0;
            var pixel = 0;
            while (pixel < pixels.Length)
            {
                var gray = pixels[pixel];
                var runLength = FileFormat.GetRunLength(pixels, pixel);
                pixel += runLength;

                switch (gray)
                {
                    case 0:
                        AddBlack(ref rawData, runLength, ref lines);
                        break;
                    case 255:
                        AddWhite(ref rawData, runLength, ref lines);
                        break;
                    default:
                        AddGray(ref rawData, gray == RepeatToken ? (byte)2 : gray, runLength, ref lines);
                        break;
                }
            }

            numberOfLines = lines;
            return rawData.WrittenSpan.ToArray();
        }
        finally
        {
            rawData.Dispose();
        }
    }

    /// <summary>
    /// Gets the length of the encoded data of a layer by counting its tokens.
    /// </summary>
    /// <param name="data">The data that starts at the first token of the layer</param>
    /// <param name="numberOfLines">The number of lines of the layer</param>
    /// <param name="isEndOfData">True when <paramref name="data"/> reaches the end of the file</param>
    /// <returns>
    /// The length of the encoded data, which includes the repeat tokens that follow the last counted token,
    /// or -1 when <paramref name="data"/> is too short to tell and <paramref name="isEndOfData"/> is false
    /// </returns>
    /// <exception cref="FileLoadException">When the data is malformed or ends before all the lines were found</exception>
    public static int GetLength(ReadOnlySpan<byte> data, uint numberOfLines, bool isEndOfData)
    {
        var n = 0;
        uint tokens = 0;
        while (true)
        {
            if (n >= data.Length)
            {
                if (tokens < numberOfLines && isEndOfData) throw Truncated();
                return isEndOfData ? n : -1;
            }

            var code = data[n];
            if (tokens >= numberOfLines && code != RepeatToken)
            {
                return n; // The next layer starts here
            }

            int size;
            switch (code)
            {
                case BlackToken:
                    size = 3;
                    break;
                case RepeatToken:
                    size = 2;
                    break;
                case WhiteToken:
                    if (n + 1 >= data.Length)
                    {
                        if (isEndOfData) throw Truncated();
                        return -1;
                    }

                    size = 1 + GetWhiteRunLengthSize(data[n + 1]);
                    break;
                default:
                    size = 1;
                    break;
            }

            if (n + size > data.Length)
            {
                if (isEndOfData) throw Truncated();
                return -1;
            }

            n += size;
            if (code != RepeatToken) tokens++;
        }
    }

    /// <summary>
    /// Reads the encoded data of a layer from the stream, which is left right after the data.
    /// </summary>
    /// <param name="stream">A seekable stream positioned at the first token of the layer</param>
    /// <param name="numberOfLines">The number of lines of the layer, 0 for an empty layer</param>
    /// <returns>The encoded data</returns>
    /// <exception cref="FileLoadException">When the data is malformed or the stream ends before all the lines were found</exception>
    public static byte[] Read(Stream stream, uint numberOfLines)
    {
        if (numberOfLines == 0) return [];

        var start = stream.Position;
        var size = ReadChunkSize;
        while (true)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                var read = stream.ReadAtLeast(buffer.AsSpan(0, size), size, throwOnEndOfStream: false);
                var isEndOfData = read < size;
                var length = GetLength(buffer.AsSpan(0, read), numberOfLines, isEndOfData);
                if (length >= 0)
                {
                    stream.Position = start + length;
                    return buffer.AsSpan(0, length).ToArray();
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            stream.Position = start;
            size = checked(size * 2);
        }
    }

    private static void AddBlack(ref BufferWriterSlim<byte> rawData, int length, ref uint lines)
    {
        while (length > 0)
        {
            var chunk = Math.Min(length, MaxBlackRun);
            rawData.Add(BlackToken);
            rawData.Add((byte)(chunk >> 8));
            rawData.Add((byte)chunk);
            lines++;
            length -= chunk;
        }
    }

    private static void AddWhite(ref BufferWriterSlim<byte> rawData, int length, ref uint lines)
    {
        while (length > 0)
        {
            var chunk = Math.Min(length, MaxWhiteRun);
            rawData.Add(WhiteToken);
            if (chunk < 0x80)
            {
                rawData.Add((byte)chunk);
            }
            else
            {
                rawData.Add((byte)(0x80 | (chunk >> 8)));
                rawData.Add((byte)chunk);
            }

            lines++;
            length -= chunk;
        }
    }

    private static void AddGray(ref BufferWriterSlim<byte> rawData, byte gray, int length, ref uint lines)
    {
        while (length > 0)
        {
            // Every chunk starts with a single pixel, one or two pixels do not need the repeat
            var chunk = Math.Min(length, MaxGrayRun);
            if (chunk <= 2)
            {
                for (var i = 0; i < chunk; i++) rawData.Add(gray);
                lines += (uint)chunk;
            }
            else
            {
                rawData.Add(gray);
                rawData.Add(RepeatToken);
                rawData.Add((byte)chunk);
                lines++;
            }

            length -= chunk;
        }
    }

    /// <summary>
    /// Gets how many bytes the length of a white run takes from its first byte.
    /// </summary>
    private static int GetWhiteRunLengthSize(byte first)
    {
        if ((first & 0x80) == 0) return 1;
        if ((first & 0xC0) == 0x80) return 2;
        if ((first & 0xE0) == 0xC0) return 3;
        throw new FileLoadException("Corrupted RLE data, invalid white run length.");
    }

    private static void EnsureFits(Span<byte> pixels, int pixel, int length)
    {
        if (length > pixels.Length - pixel)
        {
            throw new FileLoadException("Corrupted RLE data, the image ran off the end.");
        }
    }

    private static FileLoadException Truncated() => new("Corrupted RLE data, the data ended in the middle of a token.");
}
