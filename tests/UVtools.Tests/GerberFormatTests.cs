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
using UVtools.Core.Gerber;
using Xunit;

namespace UVtools.Tests;

public class GerberFormatTests
{
    private static Mat DrawPlate(string gerber, SizeF offsetMm)
    {
        using var file = new TempFile(gerber);
        var mat = new Mat(PcbFixtures.PlatePixels, PcbFixtures.PlatePixels, DepthType.Cv8U, 1);
        mat.SetTo(new MCvScalar(0));
        GerberFormat.ParseAndDraw(file.Path, mat, new SizeF(PcbFixtures.Ppmm, PcbFixtures.Ppmm), offset: offsetMm);
        return mat;
    }

    [Fact]
    public void NegativeCoordinatesKeepTheirSign()
    {
        // Y -20..-40 shifted up by 50mm lands at 10..30mm, ie 100..300px.
        // Dropping the sign would draw it at 70..90mm instead.
        using var mat = DrawPlate(PcbFixtures.NegativeYBoard, new SizeF(0, 50));

        var rectangle = CvInvoke.BoundingRectangle(mat);
        Assert.Equal(100, rectangle.X);
        Assert.Equal(100, rectangle.Y);
    }

    [Fact]
    public void PositiveCoordinatesAreUnaffected()
    {
        using var mat = DrawPlate(PcbFixtures.PositiveYBoard, SizeF.Empty);

        var rectangle = CvInvoke.BoundingRectangle(mat);
        Assert.Equal(100, rectangle.X);
        Assert.Equal(200, rectangle.Y);
    }

    [Fact]
    public void NegativeCoordinatesFallOffTheCanvasWithoutAnOffset()
    {
        // Correctly signed, the board sits at a negative pixel row and OpenCV clips all of it away.
        // Before the sign was honoured this drew the board as if it were at positive Y.
        using var mat = DrawPlate(PcbFixtures.NegativeYBoard, SizeF.Empty);

        Assert.Equal(0, CvInvoke.CountNonZero(mat));
    }

    [Fact]
    public void BoundsReportTheDeclaredCoordinates()
    {
        using var file = new TempFile(PcbFixtures.NegativeYBoard);
        using var mat = new Mat(1, 1, DepthType.Cv8U, 1);

        var document = GerberFormat.ParseAndDraw(file.Path, mat, new SizeF(PcbFixtures.Ppmm, PcbFixtures.Ppmm));

        Assert.NotNull(document.BoundsMm);
        var bounds = document.BoundsMm.Value;
        Assert.Equal(10, bounds.Left, 3);
        Assert.Equal(30, bounds.Right, 3);
        Assert.Equal(-40, bounds.Top, 3);
        Assert.Equal(-20, bounds.Bottom, 3);
    }

    [Fact]
    public void BoundsAreNullWhenNothingIsPlotted()
    {
        using var file = new TempFile("%FSLAX46Y46*%\n%MOMM*%\nM02*\n");
        using var mat = new Mat(1, 1, DepthType.Cv8U, 1);

        var document = GerberFormat.ParseAndDraw(file.Path, mat, new SizeF(PcbFixtures.Ppmm, PcbFixtures.Ppmm));

        Assert.Null(document.BoundsMm);
    }

    [Fact]
    public void RectangleApertureFlashesAtNegativeCoordinates()
    {
        // The corner of a rectangle aperture is derived from the flash centre. Clamping that corner to
        // zero would move this pad to the origin instead of leaving it on the board.
        var offset = new SizeF(0, 50);
        using var mat = DrawPlate(PcbFixtures.NegativeYBoardWithPads, offset);

        var square = PcbFixtures.CountAt(mat, new PointF(20, -30), offset, flipY: false);
        var circle = PcbFixtures.CountAt(mat, new PointF(15, -25), offset, flipY: false);

        Assert.Equal(64, circle);   // control: a circle aperture at the same kind of coordinate
        Assert.Equal(64, square);
    }
}
