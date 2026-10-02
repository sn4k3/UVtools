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
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using EmguExtensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using Xunit;

namespace UVtools.Tests;

public class OperationRepairLayersTests
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

    private static OperationRepairLayers MorphologyOnly(FileFormat slicerFile, uint gapClosing, uint noiseRemoval)
    {
        return new OperationRepairLayers(slicerFile)
        {
            DetectIssues = false,
            RepairIslands = true,
            RepairResinTraps = false,
            RepairSuctionCups = false,
            RemoveEmptyLayers = false,
            RemoveIslandsBelowEqualPixelCount = 0,
            AttachIslandsBelowLayers = 0,
            GapClosingIterations = gapClosing,
            NoiseRemovalIterations = noiseRemoval
        };
    }

    private static void Solid(Mat mat, Rectangle area) =>
        CvInvoke.Rectangle(mat, new Rectangle(area.X, area.Y, area.Width - 1, area.Height - 1), new MCvScalar(255), -1);

    [Fact]
    public void GapClosingClosesGapsAndOnlyChangesTheLayersThatNeedIt()
    {
        using var slicerFile = CreateFile(
            mat => // Two blocks with a single pixel gap between them
            {
                Solid(mat, new Rectangle(300, 300, 100, 100));
                Solid(mat, new Rectangle(401, 300, 100, 100));
            },
            mat => Solid(mat, new Rectangle(300, 300, 201, 100)), // Already closed
            _ => { }); // Empty

        foreach (var layer in slicerFile)
        {
            layer.IsModified = false;
        }

        Assert.True(MorphologyOnly(slicerFile, 1, 0).Execute());

        using (var mat = slicerFile[0].LayerMat)
        {
            Assert.Equal(255, mat.GetByte(400, 350));
            Assert.Equal(0, mat.GetByte(400, 299));
        }

        Assert.True(slicerFile[0].IsModified);
        Assert.False(slicerFile[1].IsModified);
        Assert.False(slicerFile[2].IsModified);
        Assert.True(slicerFile[2].IsEmpty);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(0, 2)]
    [InlineData(5, 3)]
    public void MorphologyOnTheContentAreaMatchesTheWholeLayer(uint gapClosing, uint noiseRemoval)
    {
        void Draw(Mat mat)
        {
            // Content touching the image borders and corners, plus scattered noise and gaps
            Solid(mat, new Rectangle(0, 0, 40, 40));
            Solid(mat, new Rectangle(42, 0, 30, 40));
            Solid(mat, new Rectangle(960, 960, 40, 40));
            Solid(mat, new Rectangle(500, 0, 60, 8));
            Solid(mat, new Rectangle(500, 12, 60, 8));
            var random = new Random(5);
            for (var i = 0; i < 300; i++)
            {
                mat.SetByte(random.Next(1000), random.Next(1000), (byte)random.Next(1, 256));
            }

            CvInvoke.Circle(mat, new Point(700, 600), 80, new MCvScalar(200), -1, LineType.AntiAlias);
        }

        using var slicerFile = CreateFile(Draw, Draw);

        using var expected = new Mat(slicerFile.Resolution, DepthType.Cv8U, 1);
        expected.SetTo(new MCvScalar(0));
        Draw(expected);
        if (gapClosing > 0)
        {
            CvInvoke.MorphologyEx(expected, expected, MorphOp.Close, EmguCvExtensions.Kernel3X3Rectangle,
                EmguCvExtensions.AnchorCenter, (int)gapClosing, BorderType.Default, default);
        }

        if (noiseRemoval > 0)
        {
            CvInvoke.MorphologyEx(expected, expected, MorphOp.Open, EmguCvExtensions.Kernel3X3Rectangle,
                EmguCvExtensions.AnchorCenter, (int)noiseRemoval, BorderType.Default, default);
        }

        Assert.True(MorphologyOnly(slicerFile, gapClosing, noiseRemoval).Execute());

        foreach (var layer in slicerFile)
        {
            using var mat = layer.LayerMat;
            using var difference = new Mat();
            CvInvoke.AbsDiff(expected, mat, difference);
            Assert.Equal(0, CvInvoke.CountNonZero(difference));
        }
    }
}
