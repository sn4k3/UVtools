/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using Xunit;

namespace UVtools.Tests;

public class IssueManagerIslandTests
{
    private static FileFormat CreateFile(params Action<Mat>[] drawLayers)
    {
        var slicerFile = PcbFixtures.CreateSlicerFile();
        var layers = new Layer[drawLayers.Length];
        for (var i = 0; i < layers.Length; i++)
        {
            using var mat = new Mat(slicerFile.Resolution, DepthType.Cv8U, 1);
            mat.SetTo(new MCvScalar(0));
            drawLayers[i](mat);
            layers[i] = new Layer((uint)i, mat, slicerFile);
        }

        slicerFile.Init(layers);
        return slicerFile;
    }

    /// <summary>
    /// Fills the rectangles with exactly their size in pixels, OpenCV also includes the last row and column.
    /// </summary>
    private static Action<Mat> Fill(params Rectangle[] rectangles) => mat =>
    {
        foreach (var rectangle in rectangles)
        {
            CvInvoke.Rectangle(mat, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width - 1, rectangle.Height - 1),
                new MCvScalar(255), -1);
        }
    };

    private static IssuesDetectionConfiguration IslandsOnly(Action<IssuesDetectionConfiguration>? configure = null)
    {
        var config = new IssuesDetectionConfiguration();
        config.DisableAll();
        config.IslandConfig.Enabled = true;
        configure?.Invoke(config);
        return config;
    }

    private static List<MainIssue> Detect(FileFormat slicerFile, IssuesDetectionConfiguration config)
    {
        return slicerFile.IssueManager.DetectIssues(config, new OperationProgress());
    }

    private static MainIssue[] Islands(List<MainIssue> issues) =>
        issues.Where(issue => issue.Type == MainIssue.IssueType.Island).ToArray();

    [Fact]
    public void FloatingBlockIsAnIslandAndSupportedBlockIsNot()
    {
        using var slicerFile = CreateFile(
            Fill(new Rectangle(100, 100, 100, 100)),
            Fill(new Rectangle(100, 100, 100, 100), new Rectangle(500, 500, 30, 20)));

        var islands = Islands(Detect(slicerFile, IslandsOnly()));

        var island = Assert.Single(islands);
        Assert.Equal(1u, island.StartLayerIndex);
        Assert.Equal(new Rectangle(500, 500, 30, 20), island.BoundingRectangle);
        Assert.Equal((uint)(30 * 20), island.PixelCount);
    }

    [Fact]
    public void FullySupportedLayersHaveNoIslands()
    {
        using var slicerFile = CreateFile(
            Fill(new Rectangle(100, 100, 300, 300)),
            Fill(new Rectangle(150, 150, 200, 200)),
            Fill(new Rectangle(200, 200, 100, 100)));

        Assert.Empty(Islands(Detect(slicerFile, IslandsOnly())));
    }

    [Theory]
    [InlineData(1, 200)] // Fits in a single vector block
    [InlineData(63, 70)] // Crosses the 64 pixels blocks
    [InlineData(60, 130)]
    [InlineData(64, 64)]
    [InlineData(5, 900)] // Wide, ends before the image border
    public void IslandPixelsAreExactForAnyRunLength(int x, int width)
    {
        var block = new Rectangle(x, 300, width, 7);
        using var slicerFile = CreateFile(
            Fill(new Rectangle(600, 600, 50, 50)),
            Fill(new Rectangle(600, 600, 50, 50), block));

        var island = Assert.Single(Islands(Detect(slicerFile, IslandsOnly())));

        Assert.Equal(block, island.BoundingRectangle);
        var points = Assert.IsType<IssueOfPoints>(Assert.Single(island)).Points;
        Assert.Equal(width * 7, points.Length);
        Assert.All(points, point => Assert.True(block.Contains(point)));
        Assert.Equal(points.Length, points.Distinct().Count());
    }

    [Fact]
    public void DiagonalBondsOnlyConnectComponentsWhenEnabled()
    {
        // The upper block is supported, the lower one only touches it by a corner and has nothing below
        var supported = new Rectangle(100, 100, 50, 50);
        var corner = new Rectangle(150, 150, 50, 50);
        using var slicerFile = CreateFile(
            Fill(supported),
            Fill(supported, corner));

        var withoutDiagonals = Islands(Detect(slicerFile, IslandsOnly(config =>
            config.IslandConfig.AllowDiagonalBonds = false)));
        var withDiagonals = Islands(Detect(slicerFile, IslandsOnly(config =>
            config.IslandConfig.AllowDiagonalBonds = true)));

        Assert.Equal(corner, Assert.Single(withoutDiagonals).BoundingRectangle);

        // Joined to the supported block, both together are supported enough
        Assert.Empty(withDiagonals);
    }

    [Fact]
    public void ComponentsAreSeparatedByASingleBackgroundPixel()
    {
        var supported = new Rectangle(100, 100, 50, 50);
        var neighbor = new Rectangle(151, 100, 50, 50); // One pixel gap
        using var slicerFile = CreateFile(
            Fill(supported),
            Fill(supported, neighbor));

        var island = Assert.Single(Islands(Detect(slicerFile, IslandsOnly(config =>
            config.IslandConfig.AllowDiagonalBonds = true))));

        Assert.Equal(neighbor, island.BoundingRectangle);
    }

    [Fact]
    public void ComponentsWithBranchesAreMerged()
    {
        // A "U" is supported on the left arm only, the arms are connected through the bottom a few rows below
        var base0 = new Rectangle(100, 100, 40, 40);
        var leftArm = new Rectangle(100, 100, 40, 200);
        var rightArm = new Rectangle(300, 100, 40, 200);
        var bottom = new Rectangle(100, 260, 240, 40);
        using var slicerFile = CreateFile(
            Fill(base0),
            Fill(leftArm, rightArm, bottom));

        // Left arm touches the base, so the whole U is one component and is supported by 40x40 of 24000+ pixels
        var islands = Islands(Detect(slicerFile, IslandsOnly(config =>
            config.IslandConfig.EnhancedDetection = false)));

        var island = Assert.Single(islands);
        Assert.Equal(new Rectangle(100, 100, 240, 200), island.BoundingRectangle);
    }

    [Fact]
    public void PartiallySupportedIslandIsKeptWhenOverhangDetectionIsNotComputed()
    {
        // The second layer block barely sits on the first (20% of it) and mostly hangs on the right side
        var first = new Rectangle(100, 100, 50, 50);
        var second = new Rectangle(140, 100, 50, 50);
        using var slicerFile = CreateFile(Fill(first), Fill(second));

        void Configure(IssuesDetectionConfiguration config)
        {
            config.IslandConfig.EnhancedDetection = true;
            config.OverhangConfig.ErodeIterations = 2;
        }

        // Overhang detection disabled: the overhang is still evaluated for the enhanced detection
        var overhangsDisabled = Islands(Detect(slicerFile, IslandsOnly(Configure)));
        Assert.Equal(second, Assert.Single(overhangsDisabled).BoundingRectangle);

        // Overhang detection enabled but not for this layer
        var notWhitelisted = Islands(Detect(slicerFile, IslandsOnly(config =>
        {
            Configure(config);
            config.OverhangConfig.Enabled = true;
            config.OverhangConfig.WhiteListLayers = [];
        })));
        Assert.Equal(second, Assert.Single(notWhitelisted).BoundingRectangle);

        // Overhang detection enabled for it
        var whitelisted = Detect(slicerFile, IslandsOnly(config =>
        {
            Configure(config);
            config.OverhangConfig.Enabled = true;
            config.OverhangConfig.WhiteListLayers = [1];
        }));
        Assert.Equal(second, Assert.Single(Islands(whitelisted)).BoundingRectangle);
        Assert.Single(whitelisted, issue => issue.Type == MainIssue.IssueType.Overhang);
    }

    [Fact]
    public void WhiteListedLayersAreTheOnlyOnesChecked()
    {
        var floating = new Rectangle(500, 500, 20, 20);
        using var slicerFile = CreateFile(
            Fill(new Rectangle(100, 100, 100, 100)),
            Fill(new Rectangle(100, 100, 100, 100), floating),
            Fill(new Rectangle(100, 100, 100, 100), floating, new Rectangle(700, 700, 20, 20)));

        var all = Islands(Detect(slicerFile, IslandsOnly()));
        Assert.Equal(new uint[] { 1, 2 }, all.Select(island => island.StartLayerIndex).Order().ToArray());

        var onlySecond = Islands(Detect(slicerFile, IslandsOnly(config =>
            config.IslandConfig.WhiteListLayers = [2])));
        var island = Assert.Single(onlySecond);
        Assert.Equal(2u, island.StartLayerIndex);
        Assert.Equal(new Rectangle(700, 700, 20, 20), island.BoundingRectangle);
    }

    /// <summary>
    /// A floating part that starts as a small island and grows faster than its island support on each layer: layer 1 is
    /// the island, layer 2 rests on it well enough to not be an island, layer 3 is an island again over layer 2, and
    /// layer 4 covers both and is supported by the block.
    /// </summary>
    private static FileFormat CreateGrowingFloatingPart() => CreateFile(
        Fill(new Rectangle(100, 100, 300, 300)),
        Fill(new Rectangle(100, 100, 300, 300), new Rectangle(500, 500, 10, 10)),
        Fill(new Rectangle(100, 100, 300, 300), new Rectangle(500, 500, 15, 15)),
        Fill(new Rectangle(100, 100, 300, 300), new Rectangle(480, 480, 60, 60)),
        Fill(new Rectangle(100, 100, 440, 440)));

    [Fact]
    public void FloatingPartIsOneIslandPerLayerWhenNotGrouped()
    {
        using var slicerFile = CreateGrowingFloatingPart();

        var islands = Islands(Detect(slicerFile, IslandsOnly(config =>
            config.IslandConfig.EnhancedDetection = false)));

        Assert.Equal(new uint[] { 1, 3 }, islands.Select(island => island.StartLayerIndex).Order().ToArray());
        Assert.All(islands, island => Assert.Single(island));
    }

    [Fact]
    public void FloatingPartIsGroupedFromItsLowestLayerUntilItJoinsTheModel()
    {
        using var slicerFile = CreateGrowingFloatingPart();

        var island = Assert.Single(Islands(Detect(slicerFile, IslandsOnly(config =>
        {
            config.IslandConfig.EnhancedDetection = false;
            config.IslandConfig.GroupFloatingIslands = true;
        }))));

        Assert.Equal(1u, island.StartLayerIndex);
        Assert.Equal(3u, island.EndLayerIndex);
        Assert.Equal(new uint[] { 1, 2, 3 }, island.Select(issue => issue.LayerIndex).ToArray());
        Assert.Equal((uint)(10 * 10 + 15 * 15 + 60 * 60), island.PixelCount);

        var children = island.Cast<IssueOfPoints>().ToArray();
        Assert.False(children[0].RestsOnIsland);
        Assert.True(children[1].RestsOnIsland);
        Assert.True(children[2].RestsOnIsland);
        Assert.All(children, child => Assert.Same(island, child.Parent));
    }

    [Fact]
    public void FloatingPartsThatJoinAreMerged()
    {
        // Two islands on layer 1, both grow on layer 2 and touch each other on layer 3
        using var slicerFile = CreateFile(
            Fill(new Rectangle(100, 100, 100, 100)),
            Fill(new Rectangle(100, 100, 100, 100), new Rectangle(500, 500, 20, 20), new Rectangle(700, 500, 20, 20)),
            Fill(new Rectangle(100, 100, 100, 100), new Rectangle(500, 500, 20, 20), new Rectangle(700, 500, 20, 20)),
            Fill(new Rectangle(100, 100, 100, 100), new Rectangle(500, 500, 220, 20)));

        var island = Assert.Single(Islands(Detect(slicerFile, IslandsOnly(config =>
            config.IslandConfig.GroupFloatingIslands = true))));

        Assert.Equal(1u, island.StartLayerIndex);
        Assert.Equal(3u, island.EndLayerIndex);
        Assert.Equal(5, island.Count);
        Assert.Equal(2, island.Cast<IssueOfPoints>().Count(issue => !issue.RestsOnIsland));
    }

    [Fact]
    public void ModelOnSupportsTouchingAnIslandIsNotFloating()
    {
        // Vertical supports cover 22% of the plate above them, less than the multiplier, but the gaps between them are
        // too narrow for an overhang so the plate is not an island. A single pixel island in a gap touches the plate.
        var supports = Enumerable.Range(0, 8).Select(i => new Rectangle(100 + i * 40, 100, 8, 300)).ToArray();
        var plate = new Rectangle(100, 100, 288, 300);
        using var slicerFile = CreateFile(
            Fill(supports),
            Fill([..supports, new Rectangle(120, 200, 1, 1)]),
            Fill(plate),
            Fill(plate),
            Fill(plate));

        var island = Assert.Single(Islands(Detect(slicerFile, IslandsOnly(config =>
            config.IslandConfig.GroupFloatingIslands = true))));

        Assert.Equal(new Rectangle(120, 200, 1, 1), Assert.Single(island).BoundingRectangle);
    }

    [Fact]
    public void SeparateIslandsAreNotGrouped()
    {
        var floating = new Rectangle(500, 500, 20, 20);
        using var slicerFile = CreateFile(
            Fill(new Rectangle(100, 100, 100, 100)),
            Fill(new Rectangle(100, 100, 100, 100), floating),
            Fill(new Rectangle(100, 100, 100, 100), floating, new Rectangle(700, 700, 20, 20)));

        var islands = Islands(Detect(slicerFile, IslandsOnly(config =>
            config.IslandConfig.GroupFloatingIslands = true)));

        // The first island continues on layer 2 resting on itself, the second one is not touching it
        Assert.Equal(2, islands.Length);
        var first = Assert.Single(islands, island => island.StartLayerIndex == 1);
        Assert.Equal(2u, first.EndLayerIndex);
        var second = Assert.Single(islands, island => island.StartLayerIndex == 2);
        Assert.Equal(new Rectangle(700, 700, 20, 20), Assert.Single(second).BoundingRectangle);
    }

    [Fact]
    public void OverhangsAreFoundOnTheNewArea()
    {
        // The second layer grows to the right, past what the first layer supports
        using var slicerFile = CreateFile(
            Fill(new Rectangle(100, 100, 100, 100)),
            Fill(new Rectangle(100, 100, 300, 100)));

        var config = new IssuesDetectionConfiguration();
        config.DisableAll();
        config.OverhangConfig.Enabled = true;
        config.OverhangConfig.ErodeIterations = 5;

        var overhang = Assert.Single(Detect(slicerFile, config));
        Assert.Equal(MainIssue.IssueType.Overhang, overhang.Type);
        Assert.Equal(1u, overhang.StartLayerIndex);
        // The new area is eroded by the iterations, only on the side facing the previous layer: the other sides touch
        // the borders of the compared area, which are not eroded
        Assert.Equal(new Rectangle(205, 100, 195, 100), overhang.BoundingRectangle);
    }

    [Fact]
    public void TouchingBoundsCoversEveryRowOfTheBlockBesideTheBorder()
    {
        // The block is the topmost content of the layer and touches the left border, its first rows must not be skipped
        var block = new Rectangle(0, 300, 20, 50);
        using var slicerFile = CreateFile(
            Fill(block, new Rectangle(500, 400, 10, 10)));

        var config = new IssuesDetectionConfiguration();
        config.DisableAll();
        config.TouchingBoundConfig.Enabled = true;

        var issue = Assert.Single(Detect(slicerFile, config));
        Assert.Equal(MainIssue.IssueType.TouchingBound, issue.Type);

        // Only the pixels inside of the margin are reported, but over the full height of the block
        Assert.Equal(new Rectangle(0, block.Y, config.TouchingBoundConfig.MarginLeft, block.Height),
            issue.BoundingRectangle);
    }
}
