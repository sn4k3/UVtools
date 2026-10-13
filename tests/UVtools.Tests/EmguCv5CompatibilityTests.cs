using System;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using EmguExtensions;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.Core.PixelEditor;
using Xunit;

namespace UVtools.Tests;

public class EmguCv5CompatibilityTests
{
    [Fact]
    public void NativeRuntimeIsOpenCv5()
    {
        Assert.Contains("OpenCV 5.0.0", CvInvoke.BuildInformation);
    }

    [Fact]
    public void CustomKernelRetainsItsValuesAfterParsingAndFailedReplacement()
    {
        using var configuration = new KernelConfiguration { MatrixText = "0 1 2\n3 4 255" };
        configuration.GenerateKernelFromText();
        var kernel = configuration.KernelMat!;
        Assert.Equal(new byte[] { 0, 1, 2, 3, 4, 255 }, kernel.GetSpan<byte>().ToArray());
        Assert.Equal(DepthType.Cv8U, kernel.Depth);

        configuration.MatrixText = "1 2\n3";
        Assert.Throws<InvalidOperationException>(configuration.GenerateKernelFromText);
        Assert.Same(kernel, configuration.KernelMat);
        Assert.Equal(255, kernel.GetSpan<byte>()[5]);
    }

    [Fact]
    public void CustomKernelRejectsAnAnchorOutsideItsBounds()
    {
        using var configuration = new KernelConfiguration { MatrixText = "1 2\n3 4", AnchorX = 2 };
        Assert.Throws<InvalidOperationException>(configuration.GenerateKernelFromText);
    }

    [Fact]
    public void AffineTranslationPreservesPixelValuesAndClearsUnwrittenMatrixEntries()
    {
        using var image = new Mat(5, 5, DepthType.Cv8U, 1);
        image.SetTo(new MCvScalar(0));
        image.GetSpan<byte>()[6] = 200;

        image.Transform(1, 1, 2, 1, interpolation: Inter.Nearest);

        Assert.Equal(1, CvInvoke.CountNonZero(image));
        Assert.Equal(200, image.GetSpan<byte>()[13]);
    }

    [Fact]
    public void RotationAdjustsBoundsAndPreservesTheTranslationConvention()
    {
        using var image = new Mat(2, 4, DepthType.Cv8U, 1);
        image.SetTo(new MCvScalar(200));
        using var rotated = new Mat();

        image.RotateAdjustBounds(rotated, 90);

        Assert.Equal(new Size(2, 4), rotated.Size);
        Assert.Equal(
            new byte[] { 0, 200, 0, 200, 0, 200, 0, 200 },
            rotated.GetSpan<byte>().ToArray()
        );
    }

    [Theory]
    [InlineData("HersheySimplex", HersheyFonts.Simplex)]
    [InlineData("HersheyDuplex", HersheyFonts.Duplex)]
    [InlineData("HersheyScriptComplex", HersheyFonts.ScriptComplex)]
    [InlineData("Duplex", HersheyFonts.Duplex)]
    public void PixelTextReadsExistingFontProfiles(string name, HersheyFonts expected)
    {
        var serializer = new XmlSerializer(typeof(PixelText));
        using var reader = new StringReader($"<PixelText><Font>{name}</Font></PixelText>");
        var text = Assert.IsType<PixelText>(serializer.Deserialize(reader));
        Assert.Equal(expected, text.Font);

        using var writer = new StringWriter();
        serializer.Serialize(writer, text);
        Assert.Contains($"<Font>Hershey{expected}</Font>", writer.ToString());
    }

    [Fact]
    public void ExposureFinderReadsExistingFontProfiles()
    {
        var serializer = new XmlSerializer(typeof(OperationCalibrateExposureFinder));
        using var reader = new StringReader(
            "<OperationCalibrateExposureFinder><TextFont>HersheyTriplex</TextFont></OperationCalibrateExposureFinder>"
        );
        using var operation = Assert.IsType<OperationCalibrateExposureFinder>(
            serializer.Deserialize(reader)
        );
        Assert.Equal(HersheyFonts.Triplex, operation.TextFont);
        Assert.Equal("HersheyTriplex", operation.SerializedTextFont);
    }

    [Fact]
    public void PixelDimmingParsesRectangularPatternsIntoMats()
    {
        using var operation = new OperationPixelDimming { PatternText = "0 1 2\n3 4 255" };
        Assert.Empty(operation.ValidateInternally()!);
        var pattern = Assert.IsType<Mat>(operation.Pattern);
        Assert.Equal(new Size(3, 2), pattern.Size);
        Assert.Equal(new byte[] { 0, 1, 2, 3, 4, 255 }, pattern.GetSpan<byte>().ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PatternClonesOwnTheirPixelsAndInvalidReplacementPreservesThePattern(bool arithmetic)
    {
        using Operation operation = arithmetic
            ? new OperationPixelArithmetic { UsePattern = true, PatternText = "0 1\n2 3" }
            : new OperationPixelDimming { PatternText = "0 1\n2 3" };
        Assert.Empty(operation.ValidateInternally()!);
        var pattern = Assert.IsType<Mat>(
            arithmetic
                ? ((OperationPixelArithmetic)operation).Pattern
                : ((OperationPixelDimming)operation).Pattern
        );

        using (var clone = operation.Clone())
        {
            var clonedPattern = Assert.IsType<Mat>(
                arithmetic
                    ? ((OperationPixelArithmetic)clone).Pattern
                    : ((OperationPixelDimming)clone).Pattern
            );
            Assert.NotSame(pattern, clonedPattern);
            clonedPattern.GetSpan<byte>()[0] = 88;
        }

        Assert.Equal(0, pattern.GetSpan<byte>()[0]);

        if (arithmetic)
            ((OperationPixelArithmetic)operation).PatternText = "0 1\n2";
        else
            ((OperationPixelDimming)operation).PatternText = "0 1\n2";
        Assert.NotEmpty(operation.ValidateInternally()!);
        Assert.Equal(new byte[] { 0, 1, 2, 3 }, pattern.GetSpan<byte>().ToArray());
    }
}
