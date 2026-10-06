using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using EmguExtensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using Xunit;

namespace UVtools.Tests;

/// <summary>
/// Formats that store anti-aliasing as one bit run length plane per gray threshold must quantize the gray image
/// by those thresholds and read back the number of planes lit as the gray level.
/// </summary>
public class AntiAliasPlaneRoundTripTests
{
    private static readonly Size Resolution = new(1440, 810);

    public static IEnumerable<object[]> Cases =>
        from format in new[] { ("cbddlp", true), ("photon", true), ("pws", false) }
        from antiAliasing in new byte[] { 1, 2, 4, 8 }
        select new object[] { format.Item1, format.Item2, antiAliasing };

    private static Mat CreateGrayMat()
    {
        var mat = new Mat(Resolution, DepthType.Cv8U, 1);
        var span = mat.GetSpanOfBytes();
        for (var y = 0; y < Resolution.Height; y++)
        {
            for (var x = 0; x < Resolution.Width; x++)
            {
                // Black margins, a full gradient and long runs of a single gray
                span[y * Resolution.Width + x] = y switch
                {
                    < 100 => 0,
                    < 200 => (byte)(x * 255 / (Resolution.Width - 1)),
                    < 300 => (byte)(255 - x * 255 / (Resolution.Width - 1)),
                    < 400 => (byte)((x / 3 + y) % 256),
                    < 500 => (byte)(x < 700 ? 255 : 200),
                    _ => (byte)(x % 260 < 100 ? 0 : 255)
                };
            }
        }

        return mat;
    }

    private static byte[] Thresholds(bool cbddlp, byte antiAliasing)
    {
        if (cbddlp)
        {
            if (antiAliasing == 1) return [127];
            return Enumerable.Range(0, antiAliasing)
                .Select(bit => unchecked((byte)(256 / antiAliasing * bit - 1)))
                .ToArray();
        }

        return Enumerable.Range(1, antiAliasing)
            .Select(level => (byte)(255 * level / (antiAliasing + 1) + 1))
            .ToArray();
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void GrayIsQuantizedByThresholds(string extension, bool cbddlp, byte antiAliasing)
    {
        var path = Path.Combine(Path.GetTempPath(), $"uvtools-aa-{Guid.NewGuid():N}.{extension}");
        try
        {
            using var source = CreateGrayMat();
            using (var slicerFile = FileFormat.FindByExtensionOrFilePath(extension, true)!)
            {
                slicerFile.Resolution = Resolution;
                slicerFile.DisplayWidth = 144;
                slicerFile.DisplayHeight = 81;
                slicerFile.LayerHeight = 0.05f;
                slicerFile.BottomLayerCount = 1;
                // The anti-aliasing setter of some formats depends on the file type
                slicerFile.FileFullPath = path;
                if (slicerFile is ChituboxFile chitubox && cbddlp) chitubox.HeaderSettings.Magic = ChituboxFile.MAGIC_CBDDLP;
                slicerFile.AntiAliasing = antiAliasing;
                slicerFile.Init([new Layer(0, source, slicerFile), new Layer(1, source, slicerFile)]);
                slicerFile.SetThumbnails(source);
                slicerFile.Encode(path, new OperationProgress());
            }

            using var reopened = FileFormat.Open(path, FileFormat.FileDecodeType.Full, new OperationProgress())!;
            Assert.Equal(antiAliasing, reopened.AntiAliasing);

            var thresholds = Thresholds(cbddlp, antiAliasing);
            var step = 256 / antiAliasing;
            var input = source.GetReadOnlySpanOfBytes();

            for (var layerIndex = 0; layerIndex < 2; layerIndex++)
            {
                using var decoded = reopened[layerIndex].LayerMat;
                var output = decoded.GetReadOnlySpanOfBytes();
                Assert.Equal(input.Length, output.Length);

                for (var i = 0; i < input.Length; i++)
                {
                    var count = 0;
                    foreach (var threshold in thresholds)
                    {
                        if (input[i] >= threshold) count++;
                    }

                    var expected = (byte)(count > 0 ? count * step - 1 : 0);
                    if (output[i] != expected)
                    {
                        Assert.Fail($"{extension} AA {antiAliasing} layer {layerIndex} pixel {i} ({i % Resolution.Width},{i / Resolution.Width}): input {input[i]} expected {expected} got {output[i]}");
                    }
                }
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Formats that keep the 4 high bits of the gray level.
    /// </summary>
    [Theory]
    [InlineData("pwx", 1440, 810)]
    [InlineData("pw0", 1440, 810)]
    [InlineData("lgs", 810, 1440)]
    public void GrayIsQuantizedToHighNibble(string extension, int width, int height)
    {
        var resolution = new Size(width, height);
        var path = Path.Combine(Path.GetTempPath(), $"uvtools-nibble-{Guid.NewGuid():N}.{extension}");
        try
        {
            using var source = new Mat(resolution, DepthType.Cv8U, 1);
            var input = source.GetSpanOfBytes();
            for (var i = 0; i < input.Length; i++)
            {
                var x = i % width;
                var y = i / width;
                input[i] = y switch
                {
                    < 100 => 0,
                    < 200 => (byte)(x * 255 / (width - 1)),
                    < 300 => (byte)((x / 3 + y) % 256),
                    < 400 => (byte)(x < 500 ? 255 : 0xf0 + x % 16),
                    < 500 => (byte)(x < 300 ? 0x0f : 0x10 + x % 16),
                    _ => (byte)(x % 260 < 100 ? 0 : 255)
                };
            }

            using (var slicerFile = FileFormat.FindByExtensionOrFilePath(extension, true)!)
            {
                slicerFile.Resolution = resolution;
                slicerFile.DisplayWidth = width / 10f;
                slicerFile.DisplayHeight = height / 10f;
                slicerFile.LayerHeight = 0.05f;
                slicerFile.BottomLayerCount = 1;
                slicerFile.Init([new Layer(0, source, slicerFile), new Layer(1, source, slicerFile)]);
                slicerFile.SetThumbnails(source);
                slicerFile.Encode(path, new OperationProgress());
            }

            using var reopened = FileFormat.Open(path, FileFormat.FileDecodeType.Full, new OperationProgress())!;
            for (var layerIndex = 0; layerIndex < 2; layerIndex++)
            {
                using var decoded = reopened[layerIndex].LayerMat;
                var output = decoded.GetReadOnlySpanOfBytes();
                Assert.Equal(input.Length, output.Length);

                for (var i = 0; i < input.Length; i++)
                {
                    var expected = (byte)((input[i] & 0xf0) | (input[i] >> 4));
                    if (output[i] != expected)
                    {
                        Assert.Fail($"{extension} layer {layerIndex} pixel {i} ({i % width},{i / width}): input {input[i]} expected {expected} got {output[i]}");
                    }
                }
            }
        }
        finally
        {
            File.Delete(path);
        }
    }
}