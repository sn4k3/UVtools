/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2020-2026 PTRTECH
 */

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Emgu.CV;
using Emgu.CV.CvEnum;
using EmguExtensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Managers;
using UVtools.Core.Operations;

namespace UVtools.Core.Voxel;

public static class VoxelPreviewMeshBuilder
{
    public static VoxelPreviewMesh Build(
        FileFormat slicerFile,
        VoxelPreviewMeshOptions options,
        OperationProgress? progress = null)
    {
        ArgumentNullException.ThrowIfNull(slicerFile);
        if (!slicerFile.HaveLayers) throw new InvalidOperationException("The file has no layers to preview.");
        if (slicerFile.DecodeType != FileFormat.FileDecodeType.Full)
            throw new InvalidOperationException("The file must be fully decoded before generating a 3D preview.");
        if (options.MaximumPlaneDimension <= 0) throw new ArgumentOutOfRangeException(nameof(options));
        if (options.MaximumTriangleCount < 12) throw new ArgumentOutOfRangeException(nameof(options));

        var source = VoxelPreviewSourceSnapshot.Capture(slicerFile);
        var bounds = slicerFile.BoundingRectangle;
        if (bounds.IsEmpty) throw new InvalidOperationException("The file has no model pixels to preview.");

        var stride = Math.Max(1,
            Math.Max(DivideRoundUp(bounds.Width, options.MaximumPlaneDimension),
                DivideRoundUp(bounds.Height, options.MaximumPlaneDimension)));
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            progress?.PauseOrCancelIfRequested();
            try
            {
                var mesh = BuildAtStride(slicerFile, options, bounds, stride, source, progress);
                if (!mesh.IsCurrentFor(slicerFile))
                {
                    mesh.Dispose();
                    throw new InvalidOperationException("The layers changed while the 3D preview was being generated.");
                }

                stopwatch.Stop();
                mesh.BuildDuration = stopwatch.Elapsed;
                return mesh;
            }
            catch (MeshBudgetExceededException)
            {
                var maximumDimension = Math.Max(bounds.Width, bounds.Height);
                if (stride >= maximumDimension)
                {
                    throw new InvalidOperationException(
                        $"The model exceeds the {options.MaximumTriangleCount:N0} triangle preview limit even at the lowest detail.");
                }

                stride = Math.Min(maximumDimension, checked(stride * 2));
            }
        }
    }

    private static VoxelPreviewMesh BuildAtStride(
        FileFormat slicerFile,
        VoxelPreviewMeshOptions options,
        Rectangle bounds,
        int stride,
        VoxelPreviewSourceSnapshot source,
        OperationProgress? progress)
    {
        var layers = slicerFile.GetDistinctLayersByPositionZ().ToArray();
        if (layers.Length == 0)
            throw new InvalidOperationException("The file has no distinct layer positions to preview.");

        var gridWidth = DivideRoundUp(bounds.Width, stride);
        var gridHeight = DivideRoundUp(bounds.Height, stride);
        var gridLength = checked(gridWidth * gridHeight);
        var pixelSize = slicerFile.PixelSize;
        var pixelWidth = pixelSize.Width > 0 ? pixelSize.Width : 0.035f;
        var pixelHeight = pixelSize.Height > 0 ? pixelSize.Height : 0.035f;

        progress?.Reset($"layers (detail 1:{stride})", (uint)layers.Length);
        using var vertices =
            new PooledBuffer<VoxelPreviewVertex>(Math.Min(65_536, (int)options.MaximumTriangleCount * 2));
        using var indices = new PooledBuffer<uint>(Math.Min(98_304, (int)options.MaximumTriangleCount * 3));
        using var cacheManager = new MatCacheManager(slicerFile, 3)
        {
            AutoDispose = true,
            AutoDisposeKeepLast = 1,
            StripAntiAliasing = true
        };

        var context = new BuildContext(vertices, indices, options.MaximumTriangleCount, bounds, stride,
            pixelWidth, pixelHeight);
        var activeSides = new Dictionary<SideRunKey, ActiveSide>();
        var nextSides = new Dictionary<SideRunKey, ActiveSide>();

        byte[]? previous = null;
        byte[]? current = null;
        byte[]? next = null;
        try
        {
            current = BuildOccupancy(slicerFile, layers[0], cacheManager, bounds, gridWidth, gridHeight,
                gridLength, progress);
            if (layers.Length > 1)
            {
                next = BuildOccupancy(slicerFile, layers[1], cacheManager, bounds, gridWidth, gridHeight,
                    gridLength, progress);
            }

            for (var layerOffset = 0; layerOffset < layers.Length; layerOffset++)
            {
                progress?.PauseOrCancelIfRequested();
                var layer = layers[layerOffset];
                var maximumZ = layer.PositionZ;
                var minimumZ = layerOffset == 0
                    ? Math.Max(0, maximumZ - Math.Max(layer.LayerHeight, slicerFile.LayerHeight))
                    : layers[layerOffset - 1].PositionZ;
                if (maximumZ <= minimumZ)
                {
                    maximumZ = minimumZ + Math.Max(layer.LayerHeight, slicerFile.LayerHeight);
                }

                EmitHorizontalFaces(context, previous, current!, next, gridWidth, gridHeight, minimumZ, maximumZ);
                MergeSideFaces(context, current!, gridWidth, gridHeight, minimumZ, maximumZ, activeSides, nextSides);
                (activeSides, nextSides) = (nextSides, activeSides);

                progress?.LockAndIncrement();

                if (previous is not null) ArrayPool<byte>.Shared.Return(previous);
                previous = current;
                current = next;
                next = layerOffset + 2 < layers.Length
                    ? BuildOccupancy(slicerFile, layers[layerOffset + 2], cacheManager, bounds, gridWidth,
                        gridHeight, gridLength, progress)
                    : null;
            }

            foreach (var activeSide in activeSides.Values)
            {
                context.EmitSide(activeSide);
            }

            var vertexArray = vertices.Detach(out var vertexCount);
            uint[]? indexArray = null;
            try
            {
                indexArray = indices.Detach(out var indexCount);
                return new VoxelPreviewMesh(vertexArray, vertexCount, indexArray, indexCount,
                    context.MinimumBounds, context.MaximumBounds, stride, source, options.Quality);
            }
            catch
            {
                ArrayPool<VoxelPreviewVertex>.Shared.Return(vertexArray);
                if (indexArray is not null) ArrayPool<uint>.Shared.Return(indexArray);
                throw;
            }
        }
        finally
        {
            if (previous is not null) ArrayPool<byte>.Shared.Return(previous);
            if (current is not null) ArrayPool<byte>.Shared.Return(current);
            if (next is not null) ArrayPool<byte>.Shared.Return(next);
        }
    }

    private static byte[] BuildOccupancy(
        FileFormat slicerFile,
        Layer layer,
        MatCacheManager cacheManager,
        Rectangle bounds,
        int gridWidth,
        int gridHeight,
        int gridLength,
        OperationProgress? progress)
    {
        var occupancy = ArrayPool<byte>.Shared.Rent(gridLength);
        occupancy.AsSpan(0, gridLength).Clear();

        try
        {
            progress?.PauseOrCancelIfRequested();
            using var merged = slicerFile.GetMergedMatForSequentialPositionedLayers(layer.Index, cacheManager);
            using var roi = merged.Roi(bounds);
            using var prepared = roi.Clone();
            var workAroundFlip = slicerFile.DisplayMirror switch
            {
                FlipDirection.None => FlipDirection.Vertically,
                FlipDirection.Horizontally => FlipDirection.Both,
                FlipDirection.Vertically => FlipDirection.None,
                FlipDirection.Both => FlipDirection.Horizontally,
                _ => throw new ArgumentOutOfRangeException(nameof(slicerFile.DisplayMirror))
            };

            if (workAroundFlip != FlipDirection.None)
            {
                CvInvoke.Flip(prepared, prepared, (FlipType)workAroundFlip);
            }

            using var sampled = new Mat();
            CvInvoke.Resize(prepared, sampled, new Size(gridWidth, gridHeight), 0, 0, Inter.Area);
            progress?.PauseOrCancelIfRequested();

            
            var source = sampled.GetReadOnlySpanOfBytes();
            for (var index = 0; index < gridLength; index++)
            {
                occupancy[index] = source[index] == 0 ? (byte)0 : byte.MaxValue;
            }

            return occupancy;
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(occupancy);
            throw;
        }
    }

    private static void EmitHorizontalFaces(
        BuildContext context,
        byte[]? previous,
        byte[] current,
        byte[]? next,
        int width,
        int height,
        float minimumZ,
        float maximumZ)
    {
        var length = checked(width * height);
        var mask = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            for (var index = 0; index < length; index++)
            {
                mask[index] = current[index] != 0 && (previous is null || previous[index] == 0)
                    ? byte.MaxValue
                    : (byte)0;
            }

            EmitGreedyPlane(context, mask, width, height, minimumZ, false);

            for (var index = 0; index < length; index++)
            {
                mask[index] = current[index] != 0 && (next is null || next[index] == 0)
                    ? byte.MaxValue
                    : (byte)0;
            }

            EmitGreedyPlane(context, mask, width, height, maximumZ, true);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(mask);
        }
    }

    private static void EmitGreedyPlane(
        BuildContext context,
        byte[] mask,
        int width,
        int height,
        float z,
        bool positive)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = y * width + x;
                if (mask[index] == 0) continue;

                var rectangleWidth = 1;
                while (x + rectangleWidth < width && mask[index + rectangleWidth] != 0) rectangleWidth++;

                var rectangleHeight = 1;
                while (y + rectangleHeight < height)
                {
                    var row = (y + rectangleHeight) * width + x;
                    var fullRow = true;
                    for (var offset = 0; offset < rectangleWidth; offset++)
                    {
                        if (mask[row + offset] != 0) continue;
                        fullRow = false;
                        break;
                    }

                    if (!fullRow) break;
                    rectangleHeight++;
                }

                for (var clearY = 0; clearY < rectangleHeight; clearY++)
                {
                    mask.AsSpan((y + clearY) * width + x, rectangleWidth).Clear();
                }

                context.EmitHorizontal(x, y, x + rectangleWidth, y + rectangleHeight, z, positive);
            }
        }
    }

    private static void MergeSideFaces(
        BuildContext context,
        byte[] occupancy,
        int width,
        int height,
        float minimumZ,
        float maximumZ,
        Dictionary<SideRunKey, ActiveSide> previousSides,
        Dictionary<SideRunKey, ActiveSide> currentSides)
    {
        currentSides.Clear();

        void AddRun(SideRunKey key)
        {
            if (previousSides.Remove(key, out var activeSide) &&
                Math.Abs(activeSide.MaximumZ - minimumZ) <= Layer.HeightPrecisionIncrementFloat)
            {
                currentSides.Add(key, activeSide with { MaximumZ = maximumZ });
                return;
            }

            if (activeSide.MaximumZ > activeSide.MinimumZ) context.EmitSide(activeSide);
            currentSides.Add(key, new ActiveSide(key, minimumZ, maximumZ));
        }

        for (var x = 0; x < width; x++)
        {
            AddVerticalRuns(SideDirection.NegativeX, x, x, false);
            AddVerticalRuns(SideDirection.PositiveX, x + 1, x, true);
        }

        for (var y = 0; y < height; y++)
        {
            AddHorizontalRuns(SideDirection.NegativeY, y, y, false);
            AddHorizontalRuns(SideDirection.PositiveY, y + 1, y, true);
        }

        foreach (var activeSide in previousSides.Values)
        {
            context.EmitSide(activeSide);
        }

        previousSides.Clear();

        void AddVerticalRuns(SideDirection direction, int fixedCoordinate, int x, bool positive)
        {
            var y = 0;
            while (y < height)
            {
                var solid = occupancy[y * width + x] != 0;
                var neighborX = positive ? x + 1 : x - 1;
                var exposed = solid && (neighborX < 0 || neighborX >= width || occupancy[y * width + neighborX] == 0);
                if (!exposed)
                {
                    y++;
                    continue;
                }

                var start = y++;
                while (y < height)
                {
                    solid = occupancy[y * width + x] != 0;
                    exposed = solid && (neighborX < 0 || neighborX >= width || occupancy[y * width + neighborX] == 0);
                    if (!exposed) break;
                    y++;
                }

                AddRun(new SideRunKey(direction, fixedCoordinate, start, y - start));
            }
        }

        void AddHorizontalRuns(SideDirection direction, int fixedCoordinate, int y, bool positive)
        {
            var x = 0;
            while (x < width)
            {
                var solid = occupancy[y * width + x] != 0;
                var neighborY = positive ? y + 1 : y - 1;
                var exposed = solid && (neighborY < 0 || neighborY >= height || occupancy[neighborY * width + x] == 0);
                if (!exposed)
                {
                    x++;
                    continue;
                }

                var start = x++;
                while (x < width)
                {
                    solid = occupancy[y * width + x] != 0;
                    exposed = solid && (neighborY < 0 || neighborY >= height || occupancy[neighborY * width + x] == 0);
                    if (!exposed) break;
                    x++;
                }

                AddRun(new SideRunKey(direction, fixedCoordinate, start, x - start));
            }
        }
    }

    private static int DivideRoundUp(int value, int divisor)
    {
        return checked((value + divisor - 1) / divisor);
    }

    private enum SideDirection : byte
    {
        NegativeX,
        PositiveX,
        NegativeY,
        PositiveY
    }

    private readonly record struct SideRunKey(SideDirection Direction, int FixedCoordinate, int Start, int Length);

    private readonly record struct ActiveSide(SideRunKey Key, float MinimumZ, float MaximumZ);

    private sealed class MeshBudgetExceededException : Exception;

    private sealed class BuildContext(
        PooledBuffer<VoxelPreviewVertex> vertices,
        PooledBuffer<uint> indices,
        uint maximumTriangleCount,
        Rectangle bounds,
        int stride,
        float pixelWidth,
        float pixelHeight)
    {
        public Vector3 MinimumBounds { get; private set; } = new(float.PositiveInfinity);
        public Vector3 MaximumBounds { get; private set; } = new(float.NegativeInfinity);

        public void EmitHorizontal(int x0, int y0, int x1, int y1, float z, bool positive)
        {
            var minimumX = GetX(x0);
            var maximumX = GetX(x1);
            var minimumY = GetY(y0);
            var maximumY = GetY(y1);

            if (positive)
            {
                AddQuad(
                    new Vector3(minimumX, minimumY, z),
                    new Vector3(maximumX, minimumY, z),
                    new Vector3(maximumX, maximumY, z),
                    new Vector3(minimumX, maximumY, z),
                    Vector3.UnitZ);
            }
            else
            {
                AddQuad(
                    new Vector3(minimumX, maximumY, z),
                    new Vector3(maximumX, maximumY, z),
                    new Vector3(maximumX, minimumY, z),
                    new Vector3(minimumX, minimumY, z),
                    -Vector3.UnitZ);
            }
        }

        public void EmitSide(ActiveSide side)
        {
            var key = side.Key;
            switch (key.Direction)
            {
                case SideDirection.NegativeX:
                {
                    var x = GetX(key.FixedCoordinate);
                    var y0 = GetY(key.Start);
                    var y1 = GetY(key.Start + key.Length);
                    AddQuad(new Vector3(x, y1, side.MinimumZ), new Vector3(x, y0, side.MinimumZ),
                        new Vector3(x, y0, side.MaximumZ), new Vector3(x, y1, side.MaximumZ), -Vector3.UnitX);
                    break;
                }
                case SideDirection.PositiveX:
                {
                    var x = GetX(key.FixedCoordinate);
                    var y0 = GetY(key.Start);
                    var y1 = GetY(key.Start + key.Length);
                    AddQuad(new Vector3(x, y0, side.MinimumZ), new Vector3(x, y1, side.MinimumZ),
                        new Vector3(x, y1, side.MaximumZ), new Vector3(x, y0, side.MaximumZ), Vector3.UnitX);
                    break;
                }
                case SideDirection.NegativeY:
                {
                    var y = GetY(key.FixedCoordinate);
                    var x0 = GetX(key.Start);
                    var x1 = GetX(key.Start + key.Length);
                    AddQuad(new Vector3(x0, y, side.MinimumZ), new Vector3(x1, y, side.MinimumZ),
                        new Vector3(x1, y, side.MaximumZ), new Vector3(x0, y, side.MaximumZ), -Vector3.UnitY);
                    break;
                }
                case SideDirection.PositiveY:
                {
                    var y = GetY(key.FixedCoordinate);
                    var x0 = GetX(key.Start);
                    var x1 = GetX(key.Start + key.Length);
                    AddQuad(new Vector3(x1, y, side.MinimumZ), new Vector3(x0, y, side.MinimumZ),
                        new Vector3(x0, y, side.MaximumZ), new Vector3(x1, y, side.MaximumZ), Vector3.UnitY);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float GetX(int coordinate)
        {
            return (bounds.X + Math.Min(coordinate * stride, bounds.Width)) * pixelWidth;
        }

        private float GetY(int coordinate)
        {
            return (bounds.Y + Math.Min(coordinate * stride, bounds.Height)) * pixelHeight;
        }

        private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            if (indices.Count / 3 + 2 > maximumTriangleCount) throw new MeshBudgetExceededException();
            var start = (uint)vertices.Count;
            vertices.Add(new VoxelPreviewVertex(a, normal));
            vertices.Add(new VoxelPreviewVertex(b, normal));
            vertices.Add(new VoxelPreviewVertex(c, normal));
            vertices.Add(new VoxelPreviewVertex(d, normal));
            indices.Add(start);
            indices.Add(start + 1);
            indices.Add(start + 2);
            indices.Add(start);
            indices.Add(start + 2);
            indices.Add(start + 3);

            MinimumBounds = Vector3.Min(MinimumBounds, Vector3.Min(Vector3.Min(a, b), Vector3.Min(c, d)));
            MaximumBounds = Vector3.Max(MaximumBounds, Vector3.Max(Vector3.Max(a, b), Vector3.Max(c, d)));
        }
    }
}