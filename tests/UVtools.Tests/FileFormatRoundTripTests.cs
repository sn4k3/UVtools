using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using EmguExtensions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using UVtools.Core.Printer;
using Xunit;
using Xunit.Abstractions;

namespace UVtools.Tests;

/// <summary>
/// Encodes a small binary model with every format that is able to write files, decodes it back and compares the pixels.
/// </summary>
public class FileFormatRoundTripTests(ITestOutputHelper output)
{
    private static readonly Size DefaultResolution = new(1440, 810);
    private const int Layers = 4;

    /// <summary>
    /// Formats that are not able to store the exact grayscale of the pixels, with the maximum gray difference allowed.
    /// </summary>
    private static readonly Dictionary<string, int> GrayTolerance = new()
    {
        // 7 bit gray limited to 0x7c, white is read back as 248
        [nameof(PHZFile)] = 7,
        [nameof(FDGFile)] = 7,
    };

    /// <summary>
    /// Vector formats that are not able to reproduce every single pixel, with the maximum differing pixels allowed.
    /// </summary>
    private static readonly Dictionary<string, int> PixelTolerance = new()
    {
        [nameof(FlashForgeSVGXFile)] = 16,
    };

    private static Size GetResolution(FileFormat format, string extension)
    {
        if (format is CrealityCXDLPFile)
        {
            var machine = Machine.Machines.First(machine => machine.Brand == PrinterBrand.Creality
                                                           && (machine.Model.StartsWith("CL") || machine.Model.StartsWith("CT")));
            return new Size((int)machine.ResolutionX, (int)machine.ResolutionY);
        }

        if (format is AnetFile) return extension == "n7" ? new Size(2560, 1600) : new Size(1440, 2560);
        // The decoder normalizes the resolution to portrait
        if (format is LGSFile) return new Size(810, 1440);

        return format.Resolution.IsEmpty ? DefaultResolution : format.Resolution;
    }

    public static IEnumerable<object[]> Formats =>
        FileFormat.AvailableFormats
            .Where(format => format is not (ImageFile or GenericZIPFile))
            .SelectMany(format => format.FileExtensions.Select(extension => new object[] { format.GetType().Name, extension.Extension }));

    private static Mat CreateLayerMat(Size resolution, int layerIndex, bool gray = false)
    {
        var mat = new Mat(resolution, DepthType.Cv8U, 1);
        mat.SetTo(new MCvScalar(0));
        CvInvoke.Rectangle(mat, new Rectangle(100 + layerIndex * 10, 80, 600, 400), new MCvScalar(255), -1);
        CvInvoke.Circle(mat, new Point(1000, 400), 150 - layerIndex * 20, new MCvScalar(255), -1);
        // A single pixel row of full white and isolated pixels exercise runs spanning rows
        CvInvoke.Line(mat, new Point(0, 700), new Point(resolution.Width - 1, 700), new MCvScalar(255), 1);
        mat.GetSpanOfBytes()[resolution.Width * 5 + 3 + layerIndex * 7] = 255;
        // The very last pixel differing from the one before it is an edge case of the run length encoders
        mat.GetSpanOfBytes()[^1] = 255;
        if (gray)
        {
            // Gradient band with long and short gray runs plus anti-aliased looking edges
            var span = mat.GetSpanOfBytes();
            for (var y = 600; y < 680; y++)
            {
                for (var x = 0; x < resolution.Width; x++)
                {
                    span[y * resolution.Width + x] = (byte)(x < 700 ? 1 + (x / 7 + y) % 254 : 90 + layerIndex);
                }
            }
        }

        return mat;
    }

    public static IEnumerable<object[]> GrayFormats =>
        FileFormat.AvailableFormats
            .Where(format => format is GooFile or SL1File or ChituboxZipFile or KlipperFile or NanoDLPFile or UVJFile)
            .SelectMany(format => format.FileExtensions.Select(extension => new object[] { format.GetType().Name, extension.Extension }));

    [Theory]
    [MemberData(nameof(Formats))]
    public void BinaryLayersSurviveEncodeAndDecode(string formatTypeName, string extension)
        => RoundTrip(formatTypeName, extension, false);

    [Theory]
    [MemberData(nameof(GrayFormats))]
    public void GrayLayersSurviveEncodeAndDecode(string formatTypeName, string extension)
        => RoundTrip(formatTypeName, extension, true);

    private void RoundTrip(string formatTypeName, string extension, bool gray)
    {
        var format = FileFormat.AvailableFormats.First(f => f.GetType().Name == formatTypeName);
        var path = Path.Combine(Path.GetTempPath(), $"uvtools-roundtrip-{Guid.NewGuid():N}.{extension}");
        try
        {
            using (var slicerFile = (FileFormat)Activator.CreateInstance(format.GetType())!)
            {
                var resolution = GetResolution(slicerFile, extension);
                slicerFile.Resolution = resolution;
                slicerFile.DisplayWidth = resolution.Width / 10f;
                slicerFile.DisplayHeight = resolution.Height / 10f;
                slicerFile.LayerHeight = 0.05f;
                slicerFile.BottomLayerCount = 1;

                var layers = new Layer[Layers];
                for (var i = 0; i < layers.Length; i++)
                {
                    using var mat = CreateLayerMat(slicerFile.Resolution, i, gray);
                    layers[i] = new Layer((uint)i, mat, slicerFile);
                }

                slicerFile.Init(layers);
                slicerFile.SetThumbnails(slicerFile[0].LayerMat);
                slicerFile.Encode(path, new OperationProgress());
            }

            using var reopened = FileFormat.Open(path, FileFormat.FileDecodeType.Full, new OperationProgress());
            Assert.NotNull(reopened);
            output.WriteLine($"{formatTypeName}: {new FileInfo(path).Length} bytes, reopened as {reopened.GetType().Name}");
            Assert.Equal(format.GetType(), reopened.GetType());
            Assert.Equal((uint)Layers, reopened.LayerCount);

            for (var i = 0; i < Layers; i++)
            {
                using var expected = CreateLayerMat(reopened.Resolution, i, gray);
                using var actual = reopened[i].LayerMat;
                Assert.Equal(expected.Size, actual.Size);
                using var diff = new Mat();
                CvInvoke.AbsDiff(expected, actual, diff);
                if (GrayTolerance.TryGetValue(formatTypeName, out var tolerance))
                {
                    CvInvoke.Threshold(diff, diff, tolerance, 255, ThresholdType.Binary);
                }

                var differences = CvInvoke.CountNonZero(diff);
                Assert.True(differences <= PixelTolerance.GetValueOrDefault(formatTypeName),
                    $"{formatTypeName} layer {i} differs on {differences} pixels");
            }
        }
        finally
        {
            File.Delete(path);
        }
    }
}
