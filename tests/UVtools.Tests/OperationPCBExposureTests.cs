/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using Emgu.CV;
using UVtools.Core;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;
using Xunit;

namespace UVtools.Tests;

public class OperationPCBExposureTests
{
    private const string CenterPad =
        """
        %FSLAX46Y46*%
        %MOMM*%
        %ADD10C,1.800000*%
        D10*
        X20000000Y-30000000D03*
        M02*
        """;

    private static OperationPCBExposure CreateOperation(FileFormat slicerFile, params string[] filePaths)
    {
        var operation = new OperationPCBExposure(slicerFile)
        {
            Anchor = Anchor.MiddleCenter
        };
        foreach (var path in filePaths) operation.Files.Add(new OperationPCBExposure.PCBExposureFile(path));
        return operation;
    }

    [Fact]
    public void MiddleCenterAnchorPlacesTheArtworkAtThePlateCenter()
    {
        using var slicerFile = PcbFixtures.CreateSlicerFile();
        using var board = new TempFile(PcbFixtures.NegativeYBoard);
        var operation = CreateOperation(slicerFile, board.Path);

        using var mat = operation.GetMat(operation.Files[0]);

        // Allow a pixel either way for the odd sized design
        const int center = PcbFixtures.PlatePixels / 2;
        var rectangle = CvInvoke.BoundingRectangle(mat);
        Assert.InRange(rectangle.X + rectangle.Width / 2, center - 2, center + 2);
        Assert.InRange(rectangle.Y + rectangle.Height / 2, center - 2, center + 2);
    }

    [Theory]
    [InlineData(Anchor.TopLeft, 0, 0)]
    [InlineData(Anchor.TopCenter, 1, 0)]
    [InlineData(Anchor.TopRight, 2, 0)]
    [InlineData(Anchor.MiddleLeft, 0, 1)]
    [InlineData(Anchor.MiddleCenter, 1, 1)]
    [InlineData(Anchor.MiddleRight, 2, 1)]
    [InlineData(Anchor.BottomLeft, 0, 2)]
    [InlineData(Anchor.BottomCenter, 1, 2)]
    [InlineData(Anchor.BottomRight, 2, 2)]
    public void PlacementAnchorsAlignArtworkWithPlate(Anchor anchor, int horizontal, int vertical)
    {
        using var slicerFile = PcbFixtures.CreateSlicerFile();
        using var board = new TempFile(PcbFixtures.NegativeYBoard);
        var operation = CreateOperation(slicerFile, board.Path);
        operation.Anchor = anchor;

        using var mat = operation.GetMat(operation.Files[0]);

        var rectangle = CvInvoke.BoundingRectangle(mat);
        var expectedX = horizontal * (PcbFixtures.PlatePixels - rectangle.Width) / 2;
        var expectedY = vertical * (PcbFixtures.PlatePixels - rectangle.Height) / 2;
        Assert.InRange(rectangle.X, expectedX - 1, expectedX + 1);
        Assert.InRange(rectangle.Y, expectedY - 1, expectedY + 1);
    }

    [Theory]
    [InlineData(Anchor.TopLeft)]
    [InlineData(Anchor.TopCenter)]
    [InlineData(Anchor.TopRight)]
    [InlineData(Anchor.MiddleLeft)]
    [InlineData(Anchor.MiddleRight)]
    [InlineData(Anchor.BottomLeft)]
    [InlineData(Anchor.BottomCenter)]
    [InlineData(Anchor.BottomRight)]
    public void PlacementAnchorsDoNotClipAperturesAtTheArtworkBounds(Anchor anchor)
    {
        const string gerber =
            """
            %FSLAX46Y46*%
            %MOMM*%
            %ADD10C,10.000000*%
            D10*
            X10000000Y20000000D03*
            X30000000Y40000000D03*
            M02*
            """;

        using var slicerFile = PcbFixtures.CreateSlicerFile();
        using var board = new TempFile(gerber);
        var operation = CreateOperation(slicerFile, board.Path);

        using var centered = operation.GetMat(operation.Files[0]);
        var centeredBounds = CvInvoke.BoundingRectangle(centered);
        var centeredPixels = CvInvoke.CountNonZero(centered);

        operation.Anchor = anchor;
        using var anchored = operation.GetMat(operation.Files[0]);

        Assert.Equal(centeredBounds.Size, CvInvoke.BoundingRectangle(anchored).Size);
        Assert.Equal(centeredPixels, CvInvoke.CountNonZero(anchored));
    }

    [Fact]
    public void AnchorNoneLeavesAnOffPlateBoardUnrendered()
    {
        using var slicerFile = PcbFixtures.CreateSlicerFile();
        using var board = new TempFile(PcbFixtures.NegativeYBoard);
        var operation = CreateOperation(slicerFile, board.Path);
        operation.Anchor = Anchor.None;

        using var mat = operation.GetMat(operation.Files[0]);

        Assert.Equal(0, CvInvoke.CountNonZero(mat));
    }

    [Fact]
    public void BoundsSpanEveryFile()
    {
        using var slicerFile = PcbFixtures.CreateSlicerFile();
        using var board = new TempFile(PcbFixtures.NegativeYBoard);
        using var drill = new TempFile(PcbFixtures.NegativeYDrill, ".drl");
        var operation = CreateOperation(slicerFile, board.Path, drill.Path);

        var bounds = operation.GetBoundsMillimeters();

        // The drill sits inside the board, so the union is the board
        Assert.NotNull(bounds);
        Assert.Equal(10, bounds.Value.Left, 3);
        Assert.Equal(30, bounds.Value.Right, 3);
    }

    [Theory]
    [InlineData("hawk-Edge_Cuts.gbr", true)]
    [InlineData("board.gko", true)]
    [InlineData("board-Outline.gbr", true)]
    [InlineData("board-Profile.gbr", true)]
    [InlineData("hawk-F_Cu.gbr", false)]
    public void CommonProfileFileNamesAreDetected(string filePath, bool expected)
    {
        var file = new OperationPCBExposure.PCBExposureFile(filePath);

        Assert.Equal(expected, file.IsBoardOutline);
    }

    [Fact]
    public void BoardOutlineIsUsedInsteadOfArtworkForBounds()
    {
        const string remotePad =
            """
            %FSLAX46Y46*%
            %MOMM*%
            %ADD10C,1.000000*%
            D10*
            X80000000Y-30000000D03*
            M02*
            """;

        using var slicerFile = PcbFixtures.CreateSlicerFile();
        using var outline = new TempFile(PcbFixtures.NegativeYBoard, ".gko");
        using var artwork = new TempFile(remotePad);
        var operation = CreateOperation(slicerFile, outline.Path, artwork.Path);

        var bounds = operation.GetBoundsMillimeters();

        Assert.True(operation.Files[0].IsBoardOutline);
        Assert.NotNull(bounds);
        Assert.Equal(10, bounds.Value.Left, 3);
        Assert.Equal(30, bounds.Value.Right, 3);
    }

    [Fact]
    public void TopLeftAnchorPreservesFeatureMarginsInsideBoardOutline()
    {
        using var slicerFile = PcbFixtures.CreateSlicerFile();
        using var outline = new TempFile(PcbFixtures.NegativeYBoard, ".gko");
        using var artwork = new TempFile(CenterPad);
        var operation = CreateOperation(slicerFile, outline.Path, artwork.Path);
        operation.Anchor = Anchor.TopLeft;

        using var mat = operation.GetMat(operation.Files[1]);
        var feature = CvInvoke.BoundingRectangle(mat);

        // The 1.8mm pad is at the centre of a 20mm board, so anchoring the profile to the corner must retain
        // approximately 10mm (100px) around the feature instead of moving the feature itself to (0, 0).
        Assert.InRange(feature.X + feature.Width / 2, 98, 102);
        Assert.InRange(feature.Y + feature.Height / 2, 98, 102);
    }

    [Fact]
    public void BoardOutlineDoesNotProduceAnExposureLayer()
    {
        using var slicerFile = PcbFixtures.CreateSlicerFile();
        using var outline = new TempFile(PcbFixtures.NegativeYBoard, ".gko");
        using var artwork = new TempFile(CenterPad);
        var operation = CreateOperation(slicerFile, outline.Path, artwork.Path);

        Assert.True(operation.Execute());
        Assert.Equal(1u, slicerFile.LayerCount);
    }

    [Fact]
    public void FillPlateUsesBoardOutlineForCopySpacing()
    {
        using var slicerFile = PcbFixtures.CreateSlicerFile();
        using var outline = new TempFile(PcbFixtures.NegativeYBoard, ".gko");
        using var artwork = new TempFile(CenterPad);
        var operation = CreateOperation(slicerFile, outline.Path, artwork.Path);
        operation.FillPlate = true;
        operation.FillSpacingX = 5;
        operation.FillSpacingY = 5;

        using var mat = operation.GetMat(operation.Files[1]);
        var bounds = CvInvoke.BoundingRectangle(mat);

        // Four 20mm boards fit per axis with 5mm gaps. The visible pad extent spans the three 25.1mm pitches.
        Assert.InRange(bounds.Width, 770, 774);
        Assert.InRange(bounds.Height, 770, 774);
    }

    [Fact]
    public void FlipYRendersTheArtworkUpright()
    {
        // Gerber Y grows upwards, image rows grow downwards. The board's top edge is its least negative Y,
        // so upright means it lands in the upper half of the drawn area.
        using var slicerFile = PcbFixtures.CreateSlicerFile();
        using var board = new TempFile(PcbFixtures.NegativeYBoardWithPads);
        var operation = CreateOperation(slicerFile, board.Path);
        var offset = operation.GetDrawOffsetMillimeters();

        using var upright = operation.GetMat(operation.Files[0]);
        operation.FlipY = false;
        using var mirrored = operation.GetMat(operation.Files[0]);

        // The round pad is above the square one on the board, at Y -25 against Y -30
        var uprightPad = PcbFixtures.CountAt(upright, new PointF(15, -25), offset);
        var mirroredPad = PcbFixtures.CountAt(mirrored, new PointF(15, -25), offset, flipY: false);
        Assert.Equal(64, uprightPad);
        Assert.Equal(64, mirroredPad);

        // Same pad, opposite halves of the plate
        var uprightY = (int)Math.Round((-25 + offset.Height) * PcbFixtures.Ppmm);
        Assert.True(PcbFixtures.PlatePixels - 1 - uprightY < PcbFixtures.PlatePixels / 2,
            "the upper board edge should render in the upper half of the plate");
    }

    [Fact]
    public void EmptyRenderFailsWithAnActionableMessage()
    {
        using var slicerFile = PcbFixtures.CreateSlicerFile();
        using var board = new TempFile(PcbFixtures.NegativeYBoard);
        var operation = CreateOperation(slicerFile, board.Path);
        operation.Anchor = Anchor.None;

        // With original board placement this board is off the plate, so nothing is drawn. The failure must name the
        // cause rather than surface an OpenCV assertion from the thumbnail step.
        var exception = Assert.Throws<InvalidOperationException>(() => operation.Execute());
        Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OnPlateBoardProducesALayer()
    {
        using var slicerFile = PcbFixtures.CreateSlicerFile();
        using var board = new TempFile(PcbFixtures.NegativeYBoard);
        var operation = CreateOperation(slicerFile, board.Path);

        Assert.True(operation.Execute());
        Assert.Equal(1u, slicerFile.LayerCount);
    }

    [Fact]
    public void FillPlateTilesAsManyWholeCopiesAsFit()
    {
        using var slicerFile = PcbFixtures.CreateSlicerFile();
        using var board = new TempFile(PcbFixtures.NegativeYBoard);
        var operation = CreateOperation(slicerFile, board.Path);
        operation.FillPlate = true;
        operation.FillSpacingX = 5;
        operation.FillSpacingY = 5;

        using var mat = operation.GetMat(operation.Files[0]);

        // A 201px design plus a 5mm (50px) gap gives a 251px pitch, so (1000+50)/251 = 4 per axis
        var rectangle = CvInvoke.BoundingRectangle(mat);
        Assert.Equal(4 * 201 + 3 * 50, rectangle.Width);
        Assert.Equal(4 * 201 + 3 * 50, rectangle.Height);
    }

    [Fact]
    public void FillPlateKeepsTheGridCentered()
    {
        using var slicerFile = PcbFixtures.CreateSlicerFile();
        using var board = new TempFile(PcbFixtures.NegativeYBoard);
        var operation = CreateOperation(slicerFile, board.Path);
        operation.FillPlate = true;
        operation.FillSpacingX = 40;
        operation.FillSpacingY = 40;

        using var mat = operation.GetMat(operation.Files[0]);

        // Two 20mm boards either side of a 40mm gap span 80mm and still fit a 100mm plate.
        // Growing the grid outwards from the original instead would fit none.
        var rectangle = CvInvoke.BoundingRectangle(mat);
        Assert.Equal(2 * 201 + 400, rectangle.Width);
        Assert.Equal((PcbFixtures.PlatePixels - rectangle.Width) / 2, rectangle.X);
    }

    [Fact]
    public void FillPlateLeavesTheArtworkAloneWhenNothingElseFits()
    {
        using var slicerFile = PcbFixtures.CreateSlicerFile();
        using var board = new TempFile(PcbFixtures.NegativeYBoard);
        var operation = CreateOperation(slicerFile, board.Path);

        using var single = operation.GetMat(operation.Files[0]);
        var before = CvInvoke.BoundingRectangle(single);

        operation.FillPlate = true;
        operation.FillSpacingX = 90;
        operation.FillSpacingY = 90;
        using var filled = operation.GetMat(operation.Files[0]);

        Assert.Equal(before, CvInvoke.BoundingRectangle(filled));
    }

    [Fact]
    public void DrillHolesAreSubtractedFromTheCopperWhenMerging()
    {
        // Drilled through the pads, so the holes actually land on copper rather than in the empty
        // middle of the board outline
        using var slicerFile = PcbFixtures.CreateSlicerFile();
        using var board = new TempFile(PcbFixtures.NegativeYBoardWithPads);
        using var drill = new TempFile(PcbFixtures.NegativeYPadDrill, ".drl");
        var operation = CreateOperation(slicerFile, board.Path, drill.Path);
        operation.MergeFiles = true;

        var offset = operation.GetDrawOffsetMillimeters();
        using var copperOnly = slicerFile.CreateMat();
        operation.DrawMat(operation.Files[0], copperOnly, false, offset);
        var copperPixels = CvInvoke.CountNonZero(copperOnly);

        Assert.True(operation.Execute());
        using var merged = slicerFile[0].LayerMat;

        Assert.True(CvInvoke.CountNonZero(merged) < copperPixels,
            "merging a drill file must punch its holes out of the copper");
    }

    [Fact]
    public void FilledLayersShareOneGrid()
    {
        // A drill layer covers a smaller area than the copper it belongs to. Measuring the grid cell per
        // layer would give them different pitches and the copies would drift apart across the plate.
        using var board = new TempFile(PcbFixtures.NegativeYBoard);
        using var drill = new TempFile(PcbFixtures.NegativeYDrill, ".drl");

        using var mergedUnion = RenderFilled(board.Path, drill.Path, mergeFiles: true);
        using var separateUnion = RenderFilled(board.Path, drill.Path, mergeFiles: false);

        Assert.Equal(CvInvoke.BoundingRectangle(mergedUnion), CvInvoke.BoundingRectangle(separateUnion));
    }

    /// <summary>
    /// Runs the operation with the plate filled and returns the union of the resulting layers.
    /// </summary>
    private static Mat RenderFilled(string boardPath, string drillPath, bool mergeFiles)
    {
        using var slicerFile = PcbFixtures.CreateSlicerFile();
        var operation = new OperationPCBExposure(slicerFile)
        {
            Anchor = Anchor.MiddleCenter,
            MergeFiles = mergeFiles,
            FillPlate = true,
            FillSpacingX = 5,
            FillSpacingY = 5
        };
        operation.Files.Add(new OperationPCBExposure.PCBExposureFile(boardPath));
        // Inverted so the drill produces a layer of its own when not merging, which is what exposes a
        // grid mismatch between the layers
        operation.Files.Add(new OperationPCBExposure.PCBExposureFile(drillPath, true));
        operation.Execute();

        var union = slicerFile.CreateMat();
        for (var index = 0; index < slicerFile.LayerCount; index++)
        {
            using var layer = slicerFile[index].LayerMat;
            CvInvoke.BitwiseOr(union, layer, union);
        }

        return union;
    }
}
