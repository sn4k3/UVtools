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
        // Several identical layers so the budget is exceeded on the first layer, well before the
        // 75% mark past which VoxelPreviewMeshBuilder tolerates going over budget instead of retrying.
        using var file = CreateFile(8, 8, 4, (_, mat) =>
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
    public void TriangleBudgetIsToleratedPastSeventyFivePercentProgressInsteadOfRetrying()
    {
        // A solid block for every layer except a fragmented checkerboard on the very last one: the cheap
        // solid layers keep the budget untouched until the last layer's exposed faces blow past it right
        // at 100% progress. VoxelPreviewMeshBuilder must keep this full detail instead of discarding the
        // near-finished pass and restarting at a coarser stride.
        const int layerCount = 40;
        using var file = CreateFile(8, 8, layerCount, (layerIndex, mat) =>
        {
            if (layerIndex < layerCount - 1)
            {
                mat.SetTo(new MCvScalar(255));
                return;
            }

            for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++)
            {
                if ((x + y) % 2 == 0) mat.SetByte(x, y, 255);
            }
        });
        var options = new VoxelPreviewMeshOptions(VoxelPreviewQuality.Detailed, 8, 12);

        using var mesh = VoxelPreviewMeshBuilder.Build(file, options);

        Assert.Equal(1, mesh.SamplingStride);
        Assert.True(mesh.TriangleCount > options.MaximumTriangleCount);
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

    [Theory]
    [InlineData(37, 29, 14, 7)]
    [InlineData(64, 5, 9, 3)] // A single vector wide rows and thin grids
    [InlineData(3, 48, 9, 11)]
    [InlineData(130, 70, 20, 5)]
    public void MeshSurfaceMatchesABruteForceCountOfTheExposedVoxelFaces(int width, int height, int layerCount, int seed)
    {
        var random = new Random(seed);
        var grids = new bool[layerCount][,];
        for (var layer = 0; layer < layerCount; layer++)
        {
            var grid = new bool[width, height];
            if (layer > 0 && random.Next(3) == 0)
            {
                Array.Copy(grids[layer - 1], grid, grid.Length); // Identical layers
            }
            else if (layer % 7 != 3) // Leave some layers empty
            {
                var blobs = random.Next(1, 6);
                for (var blob = 0; blob < blobs; blob++)
                {
                    var x0 = random.Next(width);
                    var y0 = random.Next(height);
                    var w = random.Next(1, Math.Max(2, width / 2));
                    var h = random.Next(1, Math.Max(2, height / 2));
                    for (var y = y0; y < Math.Min(height, y0 + h); y++)
                    for (var x = x0; x < Math.Min(width, x0 + w); x++)
                    {
                        grid[x, y] = (x + y + blob) % 5 != 0; // Holes
                    }
                }
            }

            grids[layer] = grid;
        }

        using var file = CreateFile(width, height, layerCount, (layer, mat) =>
        {
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                if (grids[layer][x, y]) mat.SetByte(x, y, 255);
            }
        });
        using var mesh = VoxelPreviewMeshBuilder.Build(file,
            VoxelPreviewMeshOptions.FromQuality(VoxelPreviewQuality.Detailed));
        Assert.Equal(1, mesh.SamplingStride);

        // The mesh covers the bounding rectangle of the model, mirrored vertically for a file without display mirror
        var bounds = file.BoundingRectangle;
        bool IsOn(int layer, int x, int y)
        {
            if (layer < 0 || layer >= layerCount) return false;
            var gridY = bounds.Y + (bounds.Height - 1 - y);
            return x >= 0 && x < bounds.Width && y >= 0 && y < bounds.Height && grids[layer][bounds.X + x, gridY];
        }

        // Expected area per face direction: +X -X +Y -Y +Z -Z, the horizontal cells are 1x1mm and the layers 0.05mm
        var expected = new double[6];
        for (var layer = 0; layer < layerCount; layer++)
        for (var y = 0; y < bounds.Height; y++)
        for (var x = 0; x < bounds.Width; x++)
        {
            if (!IsOn(layer, x, y)) continue;
            if (!IsOn(layer, x + 1, y)) expected[0] += 0.05;
            if (!IsOn(layer, x - 1, y)) expected[1] += 0.05;
            if (!IsOn(layer, x, y + 1)) expected[2] += 0.05;
            if (!IsOn(layer, x, y - 1)) expected[3] += 0.05;
            if (!IsOn(layer + 1, x, y)) expected[4] += 1;
            if (!IsOn(layer - 1, x, y)) expected[5] += 1;
        }

        var actual = new double[6];
        var vertices = mesh.Vertices;
        var indices = mesh.Indices;
        for (var i = 0; i < indices.Length; i += 3)
        {
            var a = vertices[(int)indices[i]];
            var b = vertices[(int)indices[i + 1]];
            var c = vertices[(int)indices[i + 2]];
            var area = System.Numerics.Vector3.Cross(b.Position - a.Position, c.Position - a.Position).Length() / 2.0;
            var normal = a.Normal;
            var direction = normal.X > 0.5f ? 0 : normal.X < -0.5f ? 1 :
                normal.Y > 0.5f ? 2 : normal.Y < -0.5f ? 3 : normal.Z > 0.5f ? 4 : 5;
            actual[direction] += area;

            // The winding must agree with the normal
            var geometric = System.Numerics.Vector3.Cross(b.Position - a.Position, c.Position - a.Position);
            Assert.True(System.Numerics.Vector3.Dot(geometric, normal) > 0, "Triangle winding disagrees with its normal");
        }

        for (var direction = 0; direction < 6; direction++)
        {
            Assert.Equal(expected[direction], actual[direction], 3);
        }
    }
    [Theory]
    [InlineData(VoxelPreviewCutawayAxis.X, false)]
    [InlineData(VoxelPreviewCutawayAxis.X, true)]
    [InlineData(VoxelPreviewCutawayAxis.Y, false)]
    [InlineData(VoxelPreviewCutawayAxis.Y, true)]
    public void CutawayKeepsTheFacesOnItsSideAndShortensTheOnesCrossingThePlane(VoxelPreviewCutawayAxis axis, bool invert)
    {
        const int width = 40, height = 30, layerCount = 8;
        var random = new Random(31);
        var grids = new bool[layerCount][,];
        for (var layer = 0; layer < layerCount; layer++)
        {
            grids[layer] = new bool[width, height];
            for (var blob = 0; blob < 4; blob++)
            {
                var x0 = random.Next(width);
                var y0 = random.Next(height);
                for (var y = y0; y < Math.Min(height, y0 + random.Next(2, 15)); y++)
                for (var x = x0; x < Math.Min(width, x0 + random.Next(2, 25)); x++)
                {
                    grids[layer][x, y] = true;
                }
            }
        }

        using var file = CreateFile(width, height, layerCount, (layer, mat) =>
        {
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                if (grids[layer][x, y]) mat.SetByte(x, y, 255);
            }
        });
        using var mesh = VoxelPreviewMeshBuilder.Build(file,
            VoxelPreviewMeshOptions.FromQuality(VoxelPreviewQuality.Detailed));

        var bounds = file.BoundingRectangle;
        bool IsOn(int layer, int x, int y)
        {
            if (layer < 0 || layer >= layerCount) return false;
            var gridY = bounds.Y + (bounds.Height - 1 - y);
            return x >= 0 && x < bounds.Width && y >= 0 && y < bounds.Height && grids[layer][bounds.X + x, gridY];
        }

        // The plane goes through the middle of the model, on a voxel boundary
        var cut = (axis == VoxelPreviewCutawayAxis.X ? bounds.Width : bounds.Height) / 2;
        var position = (axis == VoxelPreviewCutawayAxis.X ? bounds.X : bounds.Y) + (float)cut;

        bool KeepParallel(int cell) => invert ? cell >= cut : cell + 1 <= cut; // A face spanning the cell
        bool KeepPerpendicular(int value) => invert ? value >= cut : value <= cut; // A face on a boundary

        var expected = new double[6];
        for (var layer = 0; layer < layerCount; layer++)
        for (var y = 0; y < bounds.Height; y++)
        for (var x = 0; x < bounds.Width; x++)
        {
            if (!IsOn(layer, x, y)) continue;
            var cell = axis == VoxelPreviewCutawayAxis.X ? x : y;

            // Faces perpendicular to the cut axis
            if (axis == VoxelPreviewCutawayAxis.X)
            {
                if (!IsOn(layer, x + 1, y) && KeepPerpendicular(x + 1)) expected[0] += 0.05;
                if (!IsOn(layer, x - 1, y) && KeepPerpendicular(x)) expected[1] += 0.05;
                if (!IsOn(layer, x, y + 1) && KeepParallel(cell)) expected[2] += 0.05;
                if (!IsOn(layer, x, y - 1) && KeepParallel(cell)) expected[3] += 0.05;
            }
            else
            {
                if (!IsOn(layer, x + 1, y) && KeepParallel(cell)) expected[0] += 0.05;
                if (!IsOn(layer, x - 1, y) && KeepParallel(cell)) expected[1] += 0.05;
                if (!IsOn(layer, x, y + 1) && KeepPerpendicular(y + 1)) expected[2] += 0.05;
                if (!IsOn(layer, x, y - 1) && KeepPerpendicular(y)) expected[3] += 0.05;
            }

            if (!IsOn(layer + 1, x, y) && KeepParallel(cell)) expected[4] += 1;
            if (!IsOn(layer - 1, x, y) && KeepParallel(cell)) expected[5] += 1;
        }

        var (vertices, indices) = VoxelPreviewMeshClipper.ApplyCutaway(mesh.Vertices, mesh.Indices, axis, position, invert);
        Assert.Equal(0, vertices.Length % 4);
        Assert.Equal(vertices.Length / 4 * 6, indices.Length);

        var actual = new double[6];
        for (var i = 0; i < indices.Length; i += 3)
        {
            var a = vertices[(int)indices[i]];
            var b = vertices[(int)indices[i + 1]];
            var c = vertices[(int)indices[i + 2]];
            var cross = System.Numerics.Vector3.Cross(b.Position - a.Position, c.Position - a.Position);
            var normal = a.Normal;
            var direction = normal.X > 0.5f ? 0 : normal.X < -0.5f ? 1 :
                normal.Y > 0.5f ? 2 : normal.Y < -0.5f ? 3 : normal.Z > 0.5f ? 4 : 5;
            actual[direction] += cross.Length() / 2.0;
            Assert.True(System.Numerics.Vector3.Dot(cross, normal) > 0, "The winding changed");
        }

        for (var direction = 0; direction < 6; direction++)
        {
            Assert.Equal(expected[direction], actual[direction], 3);
        }

        // Off keeps everything
        var (allVertices, allIndices) = VoxelPreviewMeshClipper.ApplyCutaway(mesh.Vertices, mesh.Indices,
            VoxelPreviewCutawayAxis.Off, 0, false);
        Assert.Equal(mesh.VertexCount, allVertices.Length);
        Assert.Equal(mesh.IndexCount, allIndices.Length);
    }

    [Fact]
    public void BuildingAHeightRangeClosesTheModelOnTheCut()
    {
        using var file = CreateFile(10, 10, 20, (_, mat) => CvInvoke.Rectangle(mat, new Rectangle(2, 2, 5, 5), new MCvScalar(255), -1));
        var options = VoxelPreviewMeshOptions.FromQuality(VoxelPreviewQuality.Detailed);
        var clipZ = file[9].PositionZ;

        using var below = VoxelPreviewMeshBuilder.Build(file, options, null, null, clipZ);
        using var above = VoxelPreviewMeshBuilder.Build(file, options, null, clipZ + 0.001f, null);

        // A closed prism: 12 triangles, and the heights add up to the whole model
        Assert.Equal(12u, below.TriangleCount);
        Assert.Equal(12u, above.TriangleCount);
        Assert.Equal(clipZ, below.MaximumBounds.Z, 4);
        Assert.Equal(clipZ, above.MinimumBounds.Z, 3);
        Assert.Equal(file[19].PositionZ, above.MaximumBounds.Z, 4);
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
    public void IssueOverlayAreaMatchesTheDistinctIssueCellsOfEachLayerAndType()
    {
        const int width = 90, height = 70, layerCount = 30;
        using var file = CreateFile(width, height, layerCount, (_, mat) => mat.SetTo(new MCvScalar(255)));
        var random = new Random(21);
        var cells = new System.Collections.Generic.Dictionary<(MainIssue.IssueType, int), System.Collections.Generic.HashSet<Point>>();
        var types = new[] { MainIssue.IssueType.Island, MainIssue.IssueType.Overhang, MainIssue.IssueType.SuctionCup };

        // Many issues, more than a parallel batch, with overlapping points inside the same layer and type
        for (var n = 0; n < 120; n++)
        {
            var layer = random.Next(layerCount);
            var type = types[random.Next(types.Length)];
            var points = new System.Collections.Generic.List<Point>();
            var cx = random.Next(width - 12);
            var cy = random.Next(height - 12);
            for (var k = 0; k < 25; k++) points.Add(new Point(cx + random.Next(12), cy + random.Next(12)));
            file.IssueManager.Add(new MainIssue(type, new IssueOfPoints(file[layer], points, new Rectangle(cx, cy, 12, 12))));

            if (!cells.TryGetValue((type, layer), out var set)) cells[(type, layer)] = set = [];
            foreach (var point in points) set.Add(point);
        }

        using var overlay = VoxelPreviewIssueMeshBuilder.Build(file, 1, 1_000_000);

        var expected = new System.Collections.Generic.Dictionary<MainIssue.IssueType, double>();
        foreach (var ((type, _), set) in cells)
        {
            expected.TryGetValue(type, out var total);
            expected[type] = total + set.Count;
        }

        Assert.Equal(expected.Count, overlay.DrawRanges.Count);
        foreach (var range in overlay.DrawRanges)
        {
            double area = 0;
            for (var i = range.IndexOffset; i < range.IndexOffset + range.IndexCount; i += 3)
            {
                var a = overlay.Vertices[(int)overlay.Indices[i]].Position;
                var b = overlay.Vertices[(int)overlay.Indices[i + 1]].Position;
                var c = overlay.Vertices[(int)overlay.Indices[i + 2]].Position;
                area += System.Numerics.Vector3.Cross(b - a, c - a).Length() / 2.0;
            }

            Assert.Equal(expected[range.Type], area, 3);
        }
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
