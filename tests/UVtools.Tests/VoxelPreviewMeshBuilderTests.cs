/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2020-2026 PTRTECH
 */

using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using EmguExtensions;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Voxel;
using Xunit;

namespace UVtools.Tests;

public class VoxelPreviewMeshBuilderTests
{
    [Fact]
    public void SingleVoxelProducesClosedPhysicalCube()
    {
        using var file = CreateFile(1, 1, 1, (_, mat) => mat.SetTo(new MCvScalar(255)));
        using var mesh = VoxelPreviewMeshBuilder.Build(file,
            VoxelPreviewMeshOptions.FromQuality(VoxelPreviewQuality.Detailed));

        Assert.Equal(12u, mesh.TriangleCount);
        Assert.Equal(24, mesh.VertexCount);
        Assert.Equal(0, mesh.MinimumBounds.X, 4);
        Assert.Equal(0, mesh.MinimumBounds.Y, 4);
        Assert.Equal(0, mesh.MinimumBounds.Z, 4);
        Assert.Equal(1, mesh.MaximumBounds.X, 4);
        Assert.Equal(1, mesh.MaximumBounds.Y, 4);
        Assert.Equal(0.05f, mesh.MaximumBounds.Z, 4);
    }

    [Fact]
    public void AdjacentPixelsAndRepeatedLayersAreMergedIntoOnePrism()
    {
        using var file = CreateFile(2, 1, 3, (_, mat) => mat.SetTo(new MCvScalar(255)));
        using var mesh = VoxelPreviewMeshBuilder.Build(file,
            VoxelPreviewMeshOptions.FromQuality(VoxelPreviewQuality.Detailed));

        Assert.Equal(12u, mesh.TriangleCount);
        Assert.Equal(2, mesh.MaximumBounds.X, 4);
        Assert.Equal(1, mesh.MaximumBounds.Y, 4);
        Assert.Equal(0.15f, mesh.MaximumBounds.Z, 4);
    }

    [Fact]
    public void TriangleBudgetRetriesAtCoarserDetailWithoutReturningPartialGeometry()
    {
        using var file = CreateFile(8, 8, 1, (_, mat) =>
        {
            for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++)
            {
                if ((x + y) % 2 == 0) mat.SetByte(x, y, 255);
            }
        });
        var options = new VoxelPreviewMeshOptions(VoxelPreviewQuality.Fast, 8, 12);

        using var mesh = VoxelPreviewMeshBuilder.Build(file, options);

        Assert.True(mesh.SamplingStride > 1);
        Assert.True(mesh.TriangleCount <= options.MaximumTriangleCount);
        Assert.Equal(12u, mesh.TriangleCount);
    }

    [Fact]
    public void GeometryRevisionTracksImagePositionAndScaleChanges()
    {
        using var file = CreateFile(2, 2, 1, (_, mat) => mat.SetTo(new MCvScalar(255)));
        var revision = file.ModelGeometryRevision;

        using (var mat = file[0].LayerMat)
        {
            mat.SetByte(0, 0, 0);
            file[0].LayerMat = mat;
        }

        Assert.True(file.ModelGeometryRevision > revision);
        revision = file.ModelGeometryRevision;
        file[0].PositionZ += 0.01f;
        Assert.True(file.ModelGeometryRevision > revision);
        revision = file.ModelGeometryRevision;
        file.DisplayWidth += 1;
        Assert.True(file.ModelGeometryRevision > revision);
    }

    [Fact]
    public void CacheDetectsFormatPropertiesImplementedOutsideTheBaseSetters()
    {
        using var file = CreateFile(2, 2, 1, (_, mat) => mat.SetTo(new MCvScalar(255)));
        using var mesh = VoxelPreviewMeshBuilder.Build(file,
            VoxelPreviewMeshOptions.FromQuality(VoxelPreviewQuality.Fast));

        Assert.True(mesh.IsCurrentFor(file));

        file.DisplayMirror = FlipDirection.Horizontally;

        Assert.False(mesh.IsCurrentFor(file));
    }

    private static ChituboxFile CreateFile(int width, int height, int layerCount, Action<int, Mat> draw)
    {
        var file = new ChituboxFile
        {
            Resolution = new Size(width, height),
            Display = new SizeF(width, height),
            LayerHeight = 0.05f
        };

        var layers = new Layer[layerCount];
        for (var layerIndex = 0; layerIndex < layerCount; layerIndex++)
        {
            using var mat = new Mat(file.Resolution, DepthType.Cv8U, 1);
            mat.SetTo(new MCvScalar(0));
            draw(layerIndex, mat);
            layers[layerIndex] = new Layer((uint)layerIndex, mat, file);
        }

        file.Init(layers);
        return file;
    }
}
