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
    public void MeshFacesUseIndependentQuadsForPortableWireframeRendering()
    {
        using var file = CreateFile(3, 2, 2, (_, mat) => mat.SetTo(new MCvScalar(255)));
        using var mesh = VoxelPreviewMeshBuilder.Build(file,
            VoxelPreviewMeshOptions.FromQuality(VoxelPreviewQuality.Detailed));

        Assert.Equal(0, mesh.VertexCount % 4);
        Assert.Equal(mesh.VertexCount / 4 * 6, mesh.IndexCount);

        for (var quadIndex = 0; quadIndex < mesh.VertexCount / 4; quadIndex++)
        {
            var vertex = (uint)(quadIndex * 4);
            var offset = quadIndex * 6;
            Assert.Equal(vertex, mesh.Indices[offset]);
            Assert.Equal(vertex + 1, mesh.Indices[offset + 1]);
            Assert.Equal(vertex + 2, mesh.Indices[offset + 2]);
            Assert.Equal(vertex, mesh.Indices[offset + 3]);
            Assert.Equal(vertex + 2, mesh.Indices[offset + 4]);
            Assert.Equal(vertex + 3, mesh.Indices[offset + 5]);
        }
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

    [Fact]
    public void IssueOverlayMergesAdjacentPointsAndPreservesType()
    {
        using var file = CreateFile(4, 4, 1, (_, mat) => mat.SetTo(new MCvScalar(255)));
        var issue = new IssueOfPoints(file[0], [new Point(1, 1), new Point(2, 1)], new Rectangle(1, 1, 2, 1));
        file.IssueManager.Add(new MainIssue(MainIssue.IssueType.Island, issue));

        using var overlay = VoxelPreviewIssueMeshBuilder.Build(file, 1, 100);

        Assert.Equal(2u, overlay.TriangleCount);
        var range = Assert.Single(overlay.DrawRanges);
        Assert.Equal(MainIssue.IssueType.Island, range.Type);
        Assert.Equal(6, range.IndexCount);
        Assert.All(overlay.Vertices.ToArray(), vertex => Assert.Equal(file[0].PositionZ, vertex.Position.Z, 4));
    }

    [Fact]
    public void LayerOnlyIssueProducesFullFootprintMarker()
    {
        using var file = CreateFile(4, 3, 1, (_, mat) => mat.SetTo(new MCvScalar(255)));
        file.IssueManager.Add(new MainIssue(MainIssue.IssueType.EmptyLayer, new Issue(file[0])));

        using var overlay = VoxelPreviewIssueMeshBuilder.Build(file, 1, 100);

        Assert.Equal(2u, overlay.TriangleCount);
        Assert.Equal(MainIssue.IssueType.EmptyLayer, Assert.Single(overlay.DrawRanges).Type);
        Assert.Equal(0, overlay.Vertices[0].Position.X, 4);
        Assert.Contains(overlay.Vertices.ToArray(), vertex => Math.Abs(vertex.Position.X - 4) < 0.0001f);
        Assert.Contains(overlay.Vertices.ToArray(), vertex => Math.Abs(vertex.Position.Y - 3) < 0.0001f);
    }

    [Fact]
    public void IssueRevisionInvalidatesOverlayWithoutInvalidatingBaseMesh()
    {
        using var file = CreateFile(2, 2, 1, (_, mat) => mat.SetTo(new MCvScalar(255)));
        file.IssueManager.Add(new MainIssue(MainIssue.IssueType.Island,
            new IssueOfPoints(file[0], [new Point(0, 0)], new Rectangle(0, 0, 1, 1))));
        using var baseMesh = VoxelPreviewMeshBuilder.Build(file,
            VoxelPreviewMeshOptions.FromQuality(VoxelPreviewQuality.Fast));
        using var overlay = VoxelPreviewIssueMeshBuilder.Build(file, baseMesh.SamplingStride, 100);

        file.IssueManager.Clear();

        Assert.True(baseMesh.IsCurrentFor(file));
        Assert.False(overlay.IsCurrentFor(file));
    }

    [Fact]
    public void ContourIssuesAreFilledAndHigherPriorityTypesDrawLast()
    {
        using var file = CreateFile(4, 4, 1, (_, mat) => mat.SetTo(new MCvScalar(255)));
        var contour = new[] { new Point(0, 0), new Point(2, 0), new Point(2, 2), new Point(0, 2) };
        file.IssueManager.Add(new MainIssue(MainIssue.IssueType.Island,
            new IssueOfPoints(file[0], [new Point(1, 1)], new Rectangle(1, 1, 1, 1))));
        file.IssueManager.Add(new MainIssue(MainIssue.IssueType.ResinTrap,
            new IssueOfContours(file[0], [contour], new Rectangle(0, 0, 3, 3))));

        using var overlay = VoxelPreviewIssueMeshBuilder.Build(file, 1, 100);

        Assert.Equal(2, overlay.DrawRanges.Count);
        Assert.Equal(MainIssue.IssueType.ResinTrap, overlay.DrawRanges[0].Type);
        Assert.Equal(MainIssue.IssueType.Island, overlay.DrawRanges[1].Type);
        Assert.True(overlay.TriangleCount >= 4);
    }

    [Fact]
    public void IssueOverlayRetriesAtCoarserDetailWhenOverBudget()
    {
        using var file = CreateFile(8, 8, 1, (_, mat) => mat.SetTo(new MCvScalar(255)));
        var points = new System.Collections.Generic.List<Point>();
        for (var y = 0; y < 8; y++)
        for (var x = 0; x < 8; x++)
        {
            if ((x + y) % 2 == 0) points.Add(new Point(x, y));
        }

        file.IssueManager.Add(new MainIssue(MainIssue.IssueType.Island,
            new IssueOfPoints(file[0], points, new Rectangle(0, 0, 8, 8))));

        using var overlay = VoxelPreviewIssueMeshBuilder.Build(file, 1, 2);

        Assert.True(overlay.SamplingStride > 1);
        Assert.True(overlay.TriangleCount <= 2);
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
