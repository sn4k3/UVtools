/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UVtools.Core.IO;
using Xunit;

namespace UVtools.Tests;

public class ReverseLineReaderTests
{
    /// <summary>
    /// The reader reads 4096 bytes at a time from the end, the first read starts at this many bytes from the start.
    /// </summary>
    private const int BufferSize = 4096;

    private static string[] ReadReversed(byte[] data, Encoding encoding) =>
        new ReverseLineReader(() => new MemoryStream(data), encoding).ToArray();

    /// <summary>
    /// Reads the lines forward with a <see cref="StreamReader"/>, the reference the reverse reader must match.
    /// </summary>
    private static string[] ReadForward(byte[] data, Encoding encoding)
    {
        using var reader = new StreamReader(new MemoryStream(data), encoding);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line) lines.Add(line);
        lines.Reverse();
        return lines.ToArray();
    }

    [Theory]
    [InlineData("")]
    [InlineData("single")]
    [InlineData("a\nb\nc")]
    [InlineData("a\r\nb\r\nc\r\n")]
    [InlineData("a\rb\n\nc\n\n")]
    [InlineData("\n")]
    public void MatchesForwardReading(string text)
    {
        var data = Encoding.UTF8.GetBytes(text);
        Assert.Equal(ReadForward(data, Encoding.UTF8), ReadReversed(data, Encoding.UTF8));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void FourByteCharacterAcrossTheBufferBoundaryIsRead(int bytesBeforeBoundary)
    {
        // The first read from the end starts inside the emoji, after this many of its 4 bytes: 1 + bytesBeforeBoundary
        // bytes before the last 4096, made of "a" (1), the emoji (4), the b's and "\nlast" (5)
        var text = "a\U0001F600" + new string('b', BufferSize - 9 + bytesBeforeBoundary) + "\nlast";

        var data = Encoding.UTF8.GetBytes(text);
        Assert.Equal(BufferSize + 1 + bytesBeforeBoundary, data.Length);

        Assert.Equal(ReadForward(data, Encoding.UTF8), ReadReversed(data, Encoding.UTF8));
    }

    [Theory]
    [InlineData("first\nsecond")]
    [InlineData("\nsecond")]
    [InlineData("")]
    public void Utf8ByteOrderMarkIsNotPartOfTheFirstLine(string text)
    {
        var data = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();

        Assert.Equal(ReadForward(data, Encoding.UTF8), ReadReversed(data, Encoding.UTF8));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Utf16SurrogatePairAcrossTheBufferBoundaryIsRead(bool bigEndian)
    {
        var encoding = new UnicodeEncoding(bigEndian, false);
        // The first read from the end starts on the low surrogate, the 3rd char: 4 bytes before the last 4096, made of
        // "a" (1 char), the emoji (2 chars), the b's and "\nlast" (5 chars)
        var text = "a\U0001F600" + new string('b', BufferSize / 2 - 6) + "\nlast";

        var data = encoding.GetBytes(text);
        Assert.Equal(BufferSize + 4, data.Length);

        Assert.Equal(ReadForward(data, encoding), ReadReversed(data, encoding));
    }
}
