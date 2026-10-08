/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using UVtools.Core.FileFormats;
using Xunit;

namespace UVtools.Tests;

/// <summary>
/// The CXDLP format stores the print parameters as integers (speeds in mm/s), the values must be rounded to the
/// nearest stored value instead of truncated (#1116).
/// </summary>
public class CrealityCXDLPPrintParametersTests
{
    [Theory]
    [InlineData(55, 60)] // 0.92 mm/s -> 1 mm/s, was truncated to 0
    [InlineData(100, 120)] // 1.67 mm/s -> 2 mm/s, was truncated to 60
    [InlineData(175, 180)]
    [InlineData(25, 0)] // 0.42 mm/s really rounds to 0
    [InlineData(60, 60)]
    public void SpeedsAreRoundedToTheNearestMillimeterPerSecond(float speed, float expected)
    {
        var file = new CrealityCXDLPFile
        {
            BottomLiftSpeed = speed,
            LiftSpeed = speed,
            RetractSpeed = speed
        };

        Assert.Equal(expected, file.BottomLiftSpeed);
        Assert.Equal(expected, file.LiftSpeed);
        Assert.Equal(expected, file.RetractSpeed);
    }

    [Fact]
    public void IntegerParametersAreRoundedToTheNearestInteger()
    {
        var file = new CrealityCXDLPFile
        {
            BottomExposureTime = 3.6f,
            BottomLiftHeight = 6.5f,
            LiftHeight = 5.4f,
            WaitTimeBeforeCure = 2.7f
        };

        Assert.Equal(4, file.BottomExposureTime);
        Assert.Equal(7, file.BottomLiftHeight);
        Assert.Equal(5, file.LiftHeight);
        Assert.Equal(3, file.WaitTimeBeforeCure);
    }

    [Theory]
    [InlineData(3.3f)] // 3.3 * 10 is 32.999... in float, it was stored as 32
    [InlineData(2.9f)]
    [InlineData(1.5f)]
    public void ExposureTimeKeepsOneDecimal(float exposure)
    {
        var file = new CrealityCXDLPFile { ExposureTime = exposure };

        Assert.Equal(exposure, file.ExposureTime);
    }
}
