/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Linq;
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

    [Fact]
    public void AttributeValuesAreNotParsedAsCoordinates()
    {
        // #1159: the net names TX0 and Y1 read as X0 and Y1, which drew a track from the end of the real one
        using var mat = DrawPlate(
            """
            %FSLAX46Y46*%
            %MOMM*%
            %LPD*%
            G01*
            %ADD26C,0.800000*%
            D26*
            X50000000Y20000000D02*
            X50000000Y40000000D01*
            %TO.N,/TX0*%
            %TO.C,Y1*%
            M02*
            """, SizeF.Empty);

        var rectangle = CvInvoke.BoundingRectangle(mat);
        Assert.InRange(rectangle.X, 490, 500);
        Assert.InRange(rectangle.Y, 190, 200);
        Assert.InRange(rectangle.Right, 500, 510);
        Assert.InRange(rectangle.Bottom, 400, 410);
    }

    [Fact]
    public void AttributeValuesDoNotChangeTheAperture()
    {
        // #1159: the component name D12 read as selecting the undefined aperture 12, dropping the next pad
        using var mat = DrawPlate(
            """
            %FSLAX46Y46*%
            %MOMM*%
            %ADD10C,2.000000*%
            D10*
            X20000000Y20000000D03*
            %TO.C,D12*%
            X40000000Y20000000D03*
            %TD*%
            M02*
            """, SizeF.Empty);

        Assert.Equal(64, PcbFixtures.CountAt(mat, new PointF(20, 20), SizeF.Empty, flipY: false));
        Assert.Equal(64, PcbFixtures.CountAt(mat, new PointF(40, 20), SizeF.Empty, flipY: false));
    }

    private const string MillimeterHeader = "%FSLAX46Y46*%\n%MOMM*%\n";

    private static byte PixelAt(Mat mat, int x, int y) => mat.GetRawData(y, x)[0];

    private static (Mat Mat, GerberFormat Document) DrawDocument(string gerber, double sizeScale = 1)
    {
        using var file = new TempFile(gerber);
        var mat = new Mat(PcbFixtures.PlatePixels, PcbFixtures.PlatePixels, DepthType.Cv8U, 1);
        mat.SetTo(new MCvScalar(0));
        var document = new GerberFormat
        {
            XYppmm = new SizeF(PcbFixtures.Ppmm, PcbFixtures.Ppmm),
            SizeScale = sizeScale
        };
        GerberFormat.ParseAndDraw(document, file.Path, mat);
        return (mat, document);
    }

    [Fact]
    public void ArcExactToTheFileResolutionIsDrawn()
    {
        // 2.4 inch: the end point of this 0.254mm radius arc is rounded by about 1µm, less than one coordinate step,
        // which was rejected for differing by more than 0.1% of the radius
        using var mat = DrawPlate(
            """
            %FSLAX24Y24*%
            %MOIN*%
            %ADD10C,0.004*%
            D10*
            G75*
            X10100Y10000D02*
            G03X10071Y10071I-100J0D01*
            M02*
            """, SizeF.Empty);

        // The middle of the arc, at 22.5°
        Assert.Equal(255, PixelAt(mat, (int)Math.Round(1.00924 * 254), (int)Math.Round(1.00383 * 254)));
    }

    [Fact]
    public void UnresolvableArcStillMovesToItsEndPoint()
    {
        // The end point is 9mm from the center but the start is 1mm away: drawn as a straight segment, and the next
        // line starts where the arc ends instead of where it started
        using var mat = DrawPlate(MillimeterHeader +
            """
            %ADD10C,0.4*%
            D10*
            G75*
            X10000000Y10000000D02*
            G03X20000000Y10000000I1000000J0D01*
            G01X20000000Y30000000D01*
            M02*
            """, SizeF.Empty);

        Assert.Equal(255, PixelAt(mat, 150, 100)); // the straight segment
        Assert.Equal(255, PixelAt(mat, 200, 200)); // the line from the arc end point
        Assert.Equal(0, PixelAt(mat, 150, 200));   // a line from the arc start point would pass here
    }

    [Theory]
    // Center line 2x0.4 at (1,0), rotated to (0,1)
    [InlineData("21,1,2,0.4,1,0,90", 200, 210, 200, 190)]
    // Vector line (0,0)->(2,0), rotated to (0,0)->(0,2)
    [InlineData("20,1,0.2,0,0,2,0,90", 200, 215, 200, 185)]
    // Outline triangle (0,0),(2,0),(2,1), rotated to (0,0),(0,2),(-1,2)
    [InlineData("4,1,3,0,0,2,0,2,1,0,0,90", 197, 213, 203, 187)]
    // Triangle polygon with a vertex on +X, rotated to +Y
    [InlineData("5,1,3,0,0,2,90", 200, 208, 200, 192)]
    // Center line 4x0.4 at the origin rotated 30°, which isn't a multiple of 90°
    [InlineData("21,1,4,0.4,0,0,30", 217, 210, 217, 190)]
    public void MacroPrimitivesRotateCounterclockwise(string primitive, int litX, int litY, int darkX, int darkY)
    {
        using var mat = DrawPlate(MillimeterHeader +
            $"%AMROT*{primitive}*%\n%ADD11ROT*%\nD11*\nX20000000Y20000000D03*\nM02*\n", SizeF.Empty);

        Assert.Equal(255, PixelAt(mat, litX, litY));
        Assert.Equal(0, PixelAt(mat, darkX, darkY));
    }

    [Fact]
    public void DefiningAnApertureDoesNotSelectIt()
    {
        // D11 is defined between the move and the draw, the line must still use D10
        using var mat = DrawPlate(MillimeterHeader +
            """
            %ADD10C,0.2*%
            D10*
            X10000000Y10000000D02*
            %ADD11C,2*%
            X30000000Y10000000D01*
            M02*
            """, SizeF.Empty);

        Assert.InRange(CvInvoke.BoundingRectangle(mat).Height, 1, 4);
    }

    [Theory]
    // A 2mm pad is 20 pixels, it was one pixel larger on each axis
    [InlineData(1, 190, 20)]
    // Scaled, it grew to the right and down only
    [InlineData(1.5, 185, 30)]
    public void RectangleHasItsSizeAndStaysCentered(double sizeScale, int expectedStart, int expectedSize)
    {
        var (mat, _) = DrawDocument(MillimeterHeader + "%ADD10R,2X2*%\nD10*\nX20000000Y20000000D03*\nM02*\n", sizeScale);
        using (mat)
        {
            var rectangle = CvInvoke.BoundingRectangle(mat);
            Assert.Equal(expectedStart, rectangle.X);
            Assert.Equal(expectedStart, rectangle.Y);
            Assert.Equal(expectedSize, rectangle.Width);
            Assert.Equal(expectedSize, rectangle.Height);
        }
    }

    [Fact]
    public void ClearPolarityMacroFlashClears()
    {
        using var mat = DrawPlate(MillimeterHeader +
            """
            %AMDISC*1,1,2,0,0*%
            %ADD10R,4X4*%
            %ADD11DISC*%
            D10*
            X20000000Y20000000D03*
            %LPC*%
            D11*
            X20000000Y20000000D03*
            M02*
            """, SizeF.Empty);

        Assert.Equal(0, PixelAt(mat, 200, 200));
        Assert.Equal(255, PixelAt(mat, 218, 200)); // the pad outside of the disc
    }

    [Theory]
    // A circle with a hole flashed over a pad
    [InlineData("%ADD11C,2X1*%")]
    // A macro disc with a smaller disc with the exposure off flashed over a pad
    [InlineData("%AMHOLED*1,1,2,0,0*1,0,1,0,0*%\n%ADD11HOLED*%")]
    public void HolesShowWhatIsUnderThem(string aperture)
    {
        using var mat = DrawPlate(MillimeterHeader +
            $"%ADD10R,6X6*%\n{aperture}\nD10*\nX20000000Y20000000D03*\nD11*\nX20000000Y20000000D03*\nM02*\n",
            SizeF.Empty);

        Assert.Equal(255, PixelAt(mat, 200, 200)); // the pad under the hole
        Assert.Equal(255, PixelAt(mat, 208, 200)); // the ring
    }

    [Fact]
    public void HoleOnAnEmptyPlateStaysEmpty()
    {
        using var mat = DrawPlate(MillimeterHeader + "%ADD11C,2X1*%\nD11*\nX20000000Y20000000D03*\nM02*\n", SizeF.Empty);

        Assert.Equal(0, PixelAt(mat, 200, 200));
        Assert.Equal(255, PixelAt(mat, 208, 200));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ClearFlashWithAHoleOnlyClearsTheRing(bool overCopper)
    {
        var pad = overCopper ? "D10*\nX20000000Y20000000D03*\n" : string.Empty;
        using var mat = DrawPlate(MillimeterHeader +
            $"%ADD10R,6X6*%\n%ADD11C,2X1*%\n{pad}%LPC*%\nD11*\nX20000000Y20000000D03*\nM02*\n", SizeF.Empty);

        // The hole keeps what was under it, the clear flash never adds copper
        Assert.Equal(overCopper ? 255 : 0, PixelAt(mat, 200, 200));
        Assert.Equal(0, PixelAt(mat, 208, 200));
    }

    [Fact]
    public void CoordinatesWithAPlusSignAreDrawn()
    {
        using var mat = DrawPlate(MillimeterHeader + "%ADD10C,2*%\nD10*\nX+20000000Y+20000000D03*\nM02*\n", SizeF.Empty);

        Assert.Equal(255, PixelAt(mat, 200, 200));
    }

    [Fact]
    public void SeveralCommandsInOneExtendedBlockAreAllRead()
    {
        // The MO command after FS in the same block was lost and the inch file was read as millimeters
        using var mat = DrawPlate(
            """
            %FSLAX24Y24*MOIN*%
            %ADD10C,0.05*%
            D10*
            X5000Y5000D03*
            M02*
            """, SizeF.Empty);

        Assert.Equal(255, PixelAt(mat, 127, 127)); // 0.5 inch
        Assert.InRange(CvInvoke.BoundingRectangle(mat).Width, 12, 15); // 0.05 inch
    }

    [Fact]
    public void UnsupportedCommandsAreReported()
    {
        var (mat, document) = DrawDocument(MillimeterHeader +
            """
            %ADD10C,1*%
            %SRX2Y2I10J10*%
            D10*
            X20000000Y20000000D03*
            %SR*%
            %LMX*%
            M02*
            """);
        using (mat)
        {
            Assert.Equal(["LM", "SR"], document.UnsupportedCommands.Order());
        }
    }
}
