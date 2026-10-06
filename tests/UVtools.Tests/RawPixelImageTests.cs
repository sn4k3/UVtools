using System;
using Emgu.CV;
using Emgu.CV.CvEnum;
using EmguExtensions;
using UVtools.Core.FileFormats;
using Xunit;

namespace UVtools.Tests;

public class RawPixelImageTests
{
    // Bits kept by each packing per channel (the low bits are lost when expanded back to 8 bits)
    [Theory]
    [InlineData(FileFormat.DATATYPE_RGB555, 0xF8, 0xF8, 0xF8, 2)]
    [InlineData(FileFormat.DATATYPE_RGB555_BE, 0xF8, 0xF8, 0xF8, 2)]
    [InlineData(FileFormat.DATATYPE_BGR555, 0xF8, 0xF8, 0xF8, 2)]
    [InlineData(FileFormat.DATATYPE_BGR555_BE, 0xF8, 0xF8, 0xF8, 2)]
    [InlineData(FileFormat.DATATYPE_RGB565, 0xF8, 0xFC, 0xF8, 2)]
    [InlineData(FileFormat.DATATYPE_RGB565_BE, 0xF8, 0xFC, 0xF8, 2)]
    [InlineData(FileFormat.DATATYPE_BGR565, 0xF8, 0xFC, 0xF8, 2)]
    [InlineData(FileFormat.DATATYPE_BGR565_BE, 0xF8, 0xFC, 0xF8, 2)]
    [InlineData(FileFormat.DATATYPE_RGB888, 0xFF, 0xFF, 0xFF, 3)]
    [InlineData(FileFormat.DATATYPE_BGR888, 0xFF, 0xFF, 0xFF, 3)]
    public void RawPixelsRoundTrip(string dataType, int blueMask, int greenMask, int redMask, int bytesPerPixel)
    {
        using var source = new Mat(13, 17, DepthType.Cv8U, 3);
        var input = source.GetSpanOfBytes();
        new Random(1234).NextBytes(input);

        var encoded = FileFormat.EncodeImage(dataType, source);
        Assert.Equal(source.Width * source.Height * bytesPerPixel, encoded.Length);

        using var decoded = FileFormat.DecodeImage(dataType, encoded, source.Size);
        var output = decoded.GetReadOnlySpanOfBytes();
        Assert.Equal(input.Length, output.Length);

        for (var i = 0; i < input.Length; i += 3)
        {
            Assert.Equal(input[i] & blueMask, output[i]);
            Assert.Equal(input[i + 1] & greenMask, output[i + 1]);
            Assert.Equal(input[i + 2] & redMask, output[i + 2]);
        }
    }

    [Fact]
    public void GrayImageIsExpandedToAllChannels()
    {
        using var source = new Mat(4, 5, DepthType.Cv8U, 1);
        var input = source.GetSpanOfBytes();
        new Random(99).NextBytes(input);

        var encoded = FileFormat.EncodeImage(FileFormat.DATATYPE_RGB565, source);
        using var decoded = FileFormat.DecodeImage(FileFormat.DATATYPE_RGB565, encoded, source.Size);
        var output = decoded.GetReadOnlySpanOfBytes();

        for (var i = 0; i < input.Length; i++)
        {
            Assert.Equal(input[i] & 0xF8, output[i * 3]);
            Assert.Equal(input[i] & 0xFC, output[i * 3 + 1]);
            Assert.Equal(input[i] & 0xF8, output[i * 3 + 2]);
        }
    }

    [Fact]
    public void ChituRgb15RleRoundTrip()
    {
        using var source = new Mat(32, 64, DepthType.Cv8U, 3);
        var input = source.GetSpanOfBytes();
        var random = new Random(7);
        for (var i = 0; i < input.Length; i += 3)
        {
            // Runs of the same color mixed with noise
            if (i % 600 < 450 && i > 0)
            {
                input[i] = input[i - 3];
                input[i + 1] = input[i - 2];
                input[i + 2] = input[i - 1];
            }
            else
            {
                input[i] = (byte)random.Next(256);
                input[i + 1] = (byte)random.Next(256);
                input[i + 2] = (byte)random.Next(256);
            }
        }

        var encoded = FileFormat.EncodeChituImageRGB15Rle(source);
        using var decoded = FileFormat.DecodeChituImageRGB15Rle(encoded, source.Size);
        var output = decoded.GetReadOnlySpanOfBytes();

        for (var i = 0; i < input.Length; i += 3)
        {
            Assert.Equal(input[i] & 0xF8, output[i]);      // blue, 5 bits
            Assert.Equal(input[i + 1] & 0xF8, output[i + 1]); // green, 5 bits (the 6th is the repeat flag)
            Assert.Equal(input[i + 2] & 0xF8, output[i + 2]); // red, 5 bits
        }
    }
}
