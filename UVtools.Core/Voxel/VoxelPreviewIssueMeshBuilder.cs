/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2020-2026 PTRTECH
 */

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using EmguExtensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.Voxel;

public static class VoxelPreviewIssueMeshBuilder
{
    private static readonly MainIssue.IssueType[] DrawOrder =
    [
        MainIssue.IssueType.EmptyLayer,
        MainIssue.IssueType.PrintHeight,
        MainIssue.IssueType.Debug,
        MainIssue.IssueType.TouchingBound,
        MainIssue.IssueType.SuctionCup,
        MainIssue.IssueType.ResinTrap,
        MainIssue.IssueType.Overhang,
        MainIssue.IssueType.Island
    ];

    public static VoxelPreviewIssueMesh Build(
        FileFormat slicerFile,
        int samplingStride,
        uint maximumTriangleCount,
        OperationProgress? progress = null)
    {
        ArgumentNullException.ThrowIfNull(slicerFile);
        if (samplingStride <= 0) throw new ArgumentOutOfRangeException(nameof(samplingStride));
        if (maximumTriangleCount < 2) throw new ArgumentOutOfRangeException(nameof(maximumTriangleCount));

        var bounds = slicerFile.BoundingRectangle;
        var sourceRevision = slicerFile.ModelGeometryRevision;
        var issueRevision = slicerFile.IssueManager.Revision;
        var issues = slicerFile.IssueManager.GetVisible();
        var stride = samplingStride;
        var maximumDimension = Math.Max(bounds.Width, bounds.Height);

        while (true)
        {
            try
            {
                return BuildAtStride(slicerFile, issues, bounds, stride, maximumTriangleCount,
                    sourceRevision, issueRevision, progress);
            }
            catch (IssueMeshBudgetExceededException)
            {
                if (stride >= maximumDimension) throw;
                stride = Math.Min(maximumDimension, checked(stride * 2));
            }
        }
    }

    private static VoxelPreviewIssueMesh BuildAtStride(
        FileFormat slicerFile,
        MainIssue[] mainIssues,
        Rectangle bounds,
        int stride,
        uint maximumTriangleCount,
        long sourceRevision,
        long issueRevision,
        OperationProgress? progress)
    {
        var gridWidth = bounds.IsEmpty ? 0 : DivideRoundUp(bounds.Width, stride);
        var gridHeight = bounds.IsEmpty ? 0 : DivideRoundUp(bounds.Height, stride);
        var gridLength = checked(gridWidth * gridHeight);
        var pixelSize = slicerFile.PixelSize;
        var pixelWidth = pixelSize.Width > 0 ? pixelSize.Width : 0.035f;
        var pixelHeight = pixelSize.Height > 0 ? pixelSize.Height : 0.035f;
        var flip = slicerFile.DisplayMirror switch
        {
            FlipDirection.None => FlipDirection.Vertically,
            FlipDirection.Horizontally => FlipDirection.Both,
            FlipDirection.Vertically => FlipDirection.None,
            FlipDirection.Both => FlipDirection.Horizontally,
            _ => throw new ArgumentOutOfRangeException(nameof(slicerFile.DisplayMirror))
        };

        using var vertices = new PooledBuffer<VoxelPreviewVertex>(1024);
        using var indices = new PooledBuffer<uint>(1536);
        var ranges = new List<VoxelPreviewIssueDrawRange>(DrawOrder.Length);
        var context = new IssueBuildContext(vertices, indices, maximumTriangleCount, bounds, stride,
            pixelWidth, pixelHeight);

        foreach (var type in DrawOrder)
        {
            progress?.PauseOrCancelIfRequested();
            var rangeStart = indices.Count;
            var issuesByZ = new Dictionary<float, List<Issue>>();

            foreach (var mainIssue in mainIssues)
            {
                if (mainIssue.Type != type) continue;
                foreach (var issue in mainIssue)
                {
                    if (!issuesByZ.TryGetValue(issue.Layer.PositionZ, out var layerIssues))
                    {
                        layerIssues = [];
                        issuesByZ.Add(issue.Layer.PositionZ, layerIssues);
                    }

                    layerIssues.Add(issue);
                }
            }

            if (gridLength > 0)
            {
                /* Rasterizing and sampling the issues of a layer is the costly part and is independent between
                 * layers, so the masks of a batch are produced in parallel while the meshing keeps the layer order. */
                var groups = issuesByZ.ToArray();
                var batchSize = Math.Max(1, Environment.ProcessorCount * 2);
                var masks = new byte[]?[batchSize];
                for (var batchStart = 0; batchStart < groups.Length; batchStart += batchSize)
                {
                    progress?.PauseOrCancelIfRequested();
                    var batchLength = Math.Min(batchSize, groups.Length - batchStart);
                    try
                    {
                        Parallel.For(0, batchLength, CoreSettings.GetParallelOptions(progress?.Token ?? default),
                            offset =>
                            {
                                masks[offset] = BuildMask(type, groups[batchStart + offset].Value, bounds, gridWidth,
                                    gridHeight, gridLength, flip);
                            });

                        for (var offset = 0; offset < batchLength; offset++)
                        {
                            EmitGreedyPlane(context, masks[offset]!, gridWidth, gridHeight,
                                groups[batchStart + offset].Key);
                        }
                    }
                    finally
                    {
                        for (var offset = 0; offset < batchSize; offset++)
                        {
                            if (masks[offset] is null) continue;
                            ArrayPool<byte>.Shared.Return(masks[offset]!);
                            masks[offset] = null;
                        }
                    }
                }
            }

            var rangeCount = indices.Count - rangeStart;
            if (rangeCount > 0) ranges.Add(new VoxelPreviewIssueDrawRange(type, rangeStart, rangeCount));
        }

        if (sourceRevision != slicerFile.ModelGeometryRevision || issueRevision != slicerFile.IssueManager.Revision)
            throw new InvalidOperationException("The model or detected issues changed while the 3D issue overlay was being generated.");

        var vertexArray = vertices.Detach(out var vertexCount);
        uint[]? indexArray = null;
        try
        {
            indexArray = indices.Detach(out var indexCount);
            return new VoxelPreviewIssueMesh(vertexArray, vertexCount, indexArray, indexCount, ranges.ToArray(),
                stride, sourceRevision, issueRevision);
        }
        catch
        {
            ArrayPool<VoxelPreviewVertex>.Shared.Return(vertexArray);
            if (indexArray is not null) ArrayPool<uint>.Shared.Return(indexArray);
            throw;
        }
    }

    private static byte[] BuildMask(
        MainIssue.IssueType type,
        List<Issue> issues,
        Rectangle bounds,
        int gridWidth,
        int gridHeight,
        int gridLength,
        FlipDirection flip)
    {
        var result = ArrayPool<byte>.Shared.Rent(gridLength);
        result.AsSpan(0, gridLength).Clear();

        try
        {
            using var source = new Mat(bounds.Size, DepthType.Cv8U, 1);
            source.SetTo(new MCvScalar(0));

            if (type is MainIssue.IssueType.EmptyLayer or MainIssue.IssueType.PrintHeight)
            {
                source.SetTo(EmguCvExtensions.WhiteColor);
            }
            else
            {
                foreach (var issue in issues)
                {
                    switch (issue)
                    {
                        case IssueOfPoints pointsIssue:
                            foreach (var point in pointsIssue.Points)
                            {
                                var x = point.X - bounds.X;
                                var y = point.Y - bounds.Y;
                                if ((uint)x < (uint)bounds.Width && (uint)y < (uint)bounds.Height)
                                    source.SetByte(x, y, byte.MaxValue);
                            }
                            break;
                        case IssueOfContours contoursIssue:
                        {
                            var contours = new Point[contoursIssue.Contours.Length][];
                            for (var contourIndex = 0; contourIndex < contours.Length; contourIndex++)
                            {
                                var contour = contoursIssue.Contours[contourIndex];
                                var translated = new Point[contour.Length];
                                for (var pointIndex = 0; pointIndex < contour.Length; pointIndex++)
                                {
                                    translated[pointIndex] = new Point(
                                        contour[pointIndex].X - bounds.X,
                                        contour[pointIndex].Y - bounds.Y);
                                }

                                contours[contourIndex] = translated;
                            }

                            using var vectors = new VectorOfVectorOfPoint(contours);
                            CvInvoke.DrawContours(source, vectors, -1, EmguCvExtensions.WhiteColor, -1);
                            break;
                        }
                    }
                }
            }

            if (flip != FlipDirection.None) CvInvoke.Flip(source, source, (FlipType)flip);

            /* The area resize can only produce partial values on a coarser grid, anything lit is an issue. */
            if (source.Width == gridWidth && source.Height == gridHeight)
            {
                source.GetReadOnlySpanOfBytes()[..gridLength].CopyTo(result);
                return result;
            }

            using var sampled = new Mat();
            CvInvoke.Resize(source, sampled, new Size(gridWidth, gridHeight), 0, 0, Inter.Area);
            CvInvoke.Threshold(sampled, sampled, 0, byte.MaxValue, ThresholdType.Binary);
            sampled.GetReadOnlySpanOfBytes()[..gridLength].CopyTo(result);

            return result;
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(result);
            throw;
        }
    }

    private static void EmitGreedyPlane(IssueBuildContext context, byte[] mask, int width, int height, float z)
    {
        for (var y = 0; y < height; y++)
        {
            var rowStart = y * width;
            var x = 0;
            while (x < width)
            {
                // Jump to the next cell of the plane, the empty ones are skipped a vector at a time
                var skip = mask.AsSpan(rowStart + x, width - x).IndexOfAnyExcept((byte)0);
                if (skip < 0) break;
                x += skip;

                var index = rowStart + x;
                var rectangleWidth = mask.AsSpan(index, width - x).IndexOf((byte)0);
                if (rectangleWidth < 0) rectangleWidth = width - x;

                var rectangleHeight = 1;
                while (y + rectangleHeight < height &&
                       !mask.AsSpan((y + rectangleHeight) * width + x, rectangleWidth).Contains((byte)0))
                {
                    rectangleHeight++;
                }

                for (var clearY = 0; clearY < rectangleHeight; clearY++)
                    mask.AsSpan((y + clearY) * width + x, rectangleWidth).Clear();

                context.Emit(x, y, x + rectangleWidth, y + rectangleHeight, z);
                x += rectangleWidth;
            }
        }
    }

    private static int DivideRoundUp(int value, int divisor) => checked((value + divisor - 1) / divisor);

    private sealed class IssueMeshBudgetExceededException : Exception;

    private sealed class IssueBuildContext(
        PooledBuffer<VoxelPreviewVertex> vertices,
        PooledBuffer<uint> indices,
        uint maximumTriangleCount,
        Rectangle bounds,
        int stride,
        float pixelWidth,
        float pixelHeight)
    {
        public void Emit(int x0, int y0, int x1, int y1, float z)
        {
            if (indices.Count / 3 + 2 > maximumTriangleCount) throw new IssueMeshBudgetExceededException();

            var minimumX = GetX(x0);
            var maximumX = GetX(x1);
            var minimumY = GetY(y0);
            var maximumY = GetY(y1);
            var start = (uint)vertices.Count;
            var normal = Vector3.UnitZ;
            vertices.Add(new VoxelPreviewVertex(new Vector3(minimumX, minimumY, z), normal));
            vertices.Add(new VoxelPreviewVertex(new Vector3(maximumX, minimumY, z), normal));
            vertices.Add(new VoxelPreviewVertex(new Vector3(maximumX, maximumY, z), normal));
            vertices.Add(new VoxelPreviewVertex(new Vector3(minimumX, maximumY, z), normal));
            indices.Add(start);
            indices.Add(start + 1);
            indices.Add(start + 2);
            indices.Add(start);
            indices.Add(start + 2);
            indices.Add(start + 3);
        }

        private float GetX(int coordinate) =>
            (bounds.X + Math.Min(coordinate * stride, bounds.Width)) * pixelWidth;

        private float GetY(int coordinate) =>
            (bounds.Y + Math.Min(coordinate * stride, bounds.Height)) * pixelHeight;
    }
}
