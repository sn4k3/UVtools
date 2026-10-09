/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using Xunit;

namespace UVtools.Tests;

/// <summary>
/// The print parameters editor only offers the values that the file format is able to store (#1116)
/// </summary>
public class PrintParameterConstraintsTests
{
    [Fact]
    public void CrealityCXDLPSpeedsStepInWholeMillimetersPerSecond()
    {
        var file = new CrealityCXDLPFile();

        var constraints = file.GetPrintParameterModifierConstraints(FileFormat.PrintParameterModifier.LiftSpeed);

        // 5000 mm/min is not a multiple of 60, the largest storable speed is 4980 mm/min
        Assert.Equal(new FileFormat.PrintParameterModifierConstraints(0, 4980, 60, 0, true), constraints);
    }

    [Theory]
    [InlineData(55, 60)] // 0.92 mm/s is stored as 1 mm/s
    [InlineData(25, 0)]
    [InlineData(89, 60)]
    [InlineData(91, 120)]
    [InlineData(5000, 4980)]
    public void CrealityCXDLPSpeedsAreCoercedToTheStoredSpeeds(float speed, float expected)
    {
        var file = new CrealityCXDLPFile();

        Assert.Equal((decimal)expected,
            file.CoercePrintParameterModifierValue(FileFormat.PrintParameterModifier.LiftSpeed, (decimal)speed));
    }

    [Theory]
    [InlineData(3.46f, 3.5f)]
    [InlineData(0.04f, 0.1f)] // below the minimum
    public void CrealityCXDLPExposureTimeIsCoercedToTenthsOfASecond(float exposure, float expected)
    {
        var file = new CrealityCXDLPFile();

        Assert.Equal((decimal)expected,
            file.CoercePrintParameterModifierValue(FileFormat.PrintParameterModifier.ExposureTime, (decimal)exposure));
    }

    [Theory]
    [InlineData(3.6f, 4)]
    [InlineData(0.1f, 1)] // the minimum of 0.1 s is rounded up to a whole second
    public void CrealityCXDLPBottomExposureTimeIsCoercedToWholeSeconds(float exposure, float expected)
    {
        var file = new CrealityCXDLPFile();

        Assert.Equal((decimal)expected,
            file.CoercePrintParameterModifierValue(FileFormat.PrintParameterModifier.BottomExposureTime, (decimal)exposure));
    }

    [Fact]
    public void CrealityCXDLPWaitTimeBeforeCureIsAtLeastOneSecond()
    {
        var file = new CrealityCXDLPFile();

        Assert.Equal(1m,
            file.CoercePrintParameterModifierValue(FileFormat.PrintParameterModifier.WaitTimeBeforeCure, 0m));
    }

    [Fact]
    public void FormatsWithoutStoredLimitsUseTheModifierLimits()
    {
        var file = new ChituboxFile();
        var modifier = FileFormat.PrintParameterModifier.LiftHeight;

        Assert.Equal(new FileFormat.PrintParameterModifierConstraints(0, 1000, 0.5m, 2),
            file.GetPrintParameterModifierConstraints(modifier));
        Assert.Equal(1.23m, file.CoercePrintParameterModifierValue(modifier, 1.234m));
        Assert.Equal(0m, file.CoercePrintParameterModifierValue(modifier, -5m));
        Assert.Equal(1000m, file.CoercePrintParameterModifierValue(modifier, 1005m));
    }

    [Fact]
    public void EditingASpeedStoresTheValueThatIsReported()
    {
        using var file = CreateCrealityCXDLPFile();
        file.LiftSpeed = 120;
        var operation = new OperationEditParameters(file);
        FileFormat.PrintParameterModifier.LiftSpeed.NewValue = 55;

        Assert.Contains("Lift speed: 120mm/min » 60mm/min", operation.ConfirmationText);
        Assert.True(operation.Execute());

        Assert.Equal(60f, file.LiftSpeed);
        Assert.Equal(60m, FileFormat.PrintParameterModifier.LiftSpeed.OldValue);
        Assert.Equal(60m, FileFormat.PrintParameterModifier.LiftSpeed.NewValue);
    }

    [Fact]
    public void ValueThatIsStoredAsTheCurrentValueIsNotAChange()
    {
        using var file = CreateCrealityCXDLPFile();
        file.LiftSpeed = 60;
        var operation = new OperationEditParameters(file);
        FileFormat.PrintParameterModifier.LiftSpeed.NewValue = 55;

        var message = operation.Validate();

        Assert.NotNull(message);
        Assert.Contains("Nothing changed", message);
        Assert.Equal(60m, FileFormat.PrintParameterModifier.LiftSpeed.NewValue);
    }

    private static CrealityCXDLPFile CreateCrealityCXDLPFile()
    {
        var file = new CrealityCXDLPFile
        {
            Resolution = new Size(32, 32),
            DisplayWidth = 50,
            DisplayHeight = 50,
            LayerHeight = 0.05f
        };

        var layers = new Layer[2];
        for (var i = 0; i < layers.Length; i++)
        {
            using var mat = new Mat(file.Resolution, DepthType.Cv8U, 1);
            mat.SetTo(new MCvScalar(0));
            CvInvoke.Rectangle(mat, new Rectangle(0, 0, 4 + i * 2, 4 + i * 2), new MCvScalar(255), -1);
            layers[i] = new Layer((uint)i, mat, file);
        }

        file.Init(layers);
        return file;
    }
}
