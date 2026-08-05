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
using UVtools.Core.Excellon;
using UVtools.Core.Operations;
using Xunit;

namespace UVtools.Tests;

public class ExcellonDrillFormatTests
{
    private static ExcellonDrillFormat Parse(string drill)
    {
        using var file = new TempFile(drill, ".drl");
        using var mat = new Mat(1, 1, DepthType.Cv8U, 1);

        // Drawn against a 1x1 Mat: everything is clipped away and only the parsing is under test
        return ExcellonDrillFormat.ParseAndDraw(
            new OperationPCBExposure.PCBExposureFile(file.Path), mat,
            new SizeF(PcbFixtures.Ppmm, PcbFixtures.Ppmm));
    }

    [Fact]
    public void NegativeCoordinatesKeepTheirSign()
    {
        var document = Parse(PcbFixtures.NegativeYDrill);

        Assert.Equal(2, document.Drills.Count);
        Assert.Equal(-22, document.Drills[0].Position.Y, 3);
        Assert.Equal(-38, document.Drills[1].Position.Y, 3);
    }

    [Fact]
    public void BoundsCoverTheDrillRadius()
    {
        // Holes at (12,-22) and (28,-38) with a 1mm tool, so half a millimeter of padding all round
        var document = Parse(PcbFixtures.NegativeYDrill);

        Assert.NotNull(document.BoundsMm);
        var bounds = document.BoundsMm.Value;
        Assert.Equal(11.5, bounds.Left, 3);
        Assert.Equal(28.5, bounds.Right, 3);
        Assert.Equal(-38.5, bounds.Top, 3);
        Assert.Equal(-21.5, bounds.Bottom, 3);
    }

    [Fact]
    public void BoundsAreNullWithoutDrills()
    {
        var document = Parse("M48\nFMAT,2\nMETRIC\n%\nM30\n");

        Assert.Empty(document.Drills);
        Assert.Null(document.BoundsMm);
    }

    [Fact]
    public void ImpliedDecimalHonoursTheFileFormatFractionDigits()
    {
        // ;FILE_FORMAT=4:3 means 3 fraction digits, so X0015000 is 15.000mm.
        // Taking the integer digit count instead would place the point one over, giving 1.5mm.
        var document = Parse(PcbFixtures.ImpliedDecimalDrill);

        var drill = Assert.Single(document.Drills);
        Assert.Equal(15, drill.Position.X, 3);
        Assert.Equal(-25, drill.Position.Y, 3);
    }
}
