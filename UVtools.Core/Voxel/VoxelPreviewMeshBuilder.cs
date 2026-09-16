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
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using EmguExtensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.Voxel;

public static class VoxelPreviewMeshBuilder
{
    /// <summary>
    /// Once the sequential meshing pass has covered at least this fraction of the layers, exceeding the
    /// triangle budget no longer restarts the build at a coarser stride: decoding is by far the most expensive
    /// part of a build, so throwing away a near-finished, full-detail pass to retry at half the detail would
    /// cost more than it saves. The mesh is instead allowed to finish over budget at the current detail.
    /// </summary>
    private const float KeepDetailAfterProgress = 0.75f;

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

    /// <summary>
    /// Builds the 1-byte-per-voxel occupancy mask for the specified layer, matching the voxel preview mesh's
    /// sampling stride, boundaries, and orientation. The returned array is rented from <see cref="ArrayPool{T}.Shared"/>
    /// and must be returned to the pool by the caller if not empty.
    /// </summary>
    public static byte[] BuildLayerOccupancy(FileFormat slicerFile, Layer layer, VoxelPreviewMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(slicerFile);
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(mesh);

        var gridLength = checked(mesh.GridWidth * mesh.GridHeight);
        if (gridLength == 0) return [];

        return BuildOccupancy(slicerFile, layer, mesh.ModelBounds, mesh.GridWidth, mesh.GridHeight, gridLength,
            mesh.WorkAroundFlip);
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

        /* Work around the mirror effect of the voxel algorithm assuming 0,0 is the bottom left corner. */
        var workAroundFlip = slicerFile.DisplayMirror switch
        {
            FlipDirection.None => FlipDirection.Vertically,
            FlipDirection.Horizontally => FlipDirection.Both,
            FlipDirection.Vertically => FlipDirection.None,
            FlipDirection.Both => FlipDirection.Horizontally,
            _ => throw new ArgumentOutOfRangeException(nameof(slicerFile.DisplayMirror))
        };

        progress?.Reset($"layers (detail 1:{stride})", (uint)layers.Length);
        using var vertices =
            new PooledBuffer<VoxelPreviewVertex>(Math.Min(65_536, (int)options.MaximumTriangleCount * 2));
        using var indices = new PooledBuffer<uint>(Math.Min(98_304, (int)options.MaximumTriangleCount * 3));

        var context = new BuildContext(vertices, indices, options.MaximumTriangleCount, bounds, stride,
            pixelWidth, pixelHeight);
        var activeSides = new Dictionary<SideRunKey, ActiveSide>();
        var nextSides = new Dictionary<SideRunKey, ActiveSide>();

        /* Decoding a layer costs orders of magnitude more than meshing it, so the occupancy grids of a whole
         * chunk are produced in parallel while the meshing itself stays sequential, as it carries state from
         * one layer to the next. The window holds the chunk plus one look ahead grid, which is carried over to
         * the next chunk instead of being decoded twice. */
        var (parallelism, chunkSize) = GetPipelineSizing(slicerFile, gridLength);
        var parallelOptions = CoreSettings.GetParallelOptions(progress?.Token ?? default);
        parallelOptions.MaxDegreeOfParallelism = parallelism;
        var window = new byte[]?[chunkSize + 1];
        byte[]? previousTail = null;

        try
        {
            for (var chunkStart = 0; chunkStart < layers.Length; chunkStart += chunkSize)
            {
                progress?.PauseOrCancelIfRequested();
                var chunkLength = Math.Min(chunkSize, layers.Length - chunkStart);
                var windowLength = chunkLength + (chunkStart + chunkLength < layers.Length ? 1 : 0);

                /* window[0] is already filled when it was the look ahead grid of the previous chunk. */
                var firstToDecode = window[0] is null ? 0 : 1;
                Parallel.For(firstToDecode, windowLength, parallelOptions, offset =>
                {
                    progress?.PauseIfRequested();
                    window[offset] = BuildOccupancy(slicerFile, layers[chunkStart + offset], bounds, gridWidth,
                        gridHeight, gridLength, workAroundFlip);
                    progress?.LockAndIncrement();
                });

                for (var offset = 0; offset < chunkLength; offset++)
                {
                    progress?.PauseOrCancelIfRequested();
                    var layerOffset = chunkStart + offset;
                    var layer = layers[layerOffset];
                    var maximumZ = layer.PositionZ;
                    var minimumZ = layerOffset == 0
                        ? Math.Max(0, maximumZ - Math.Max(layer.LayerHeight, slicerFile.LayerHeight))
                        : layers[layerOffset - 1].PositionZ;
                    if (maximumZ <= minimumZ)
                    {
                        maximumZ = minimumZ + Math.Max(layer.LayerHeight, slicerFile.LayerHeight);
                    }

                    var previous = offset == 0 ? previousTail : window[offset - 1];
                    var current = window[offset]!;
                    var next = offset + 1 < windowLength ? window[offset + 1] : null;

                    context.LayerProgress = (float)(layerOffset + 1) / layers.Length;
                    EmitHorizontalFaces(context, previous, current, next, gridWidth, gridHeight, minimumZ,
                        maximumZ);
                    MergeSideFaces(context, current, gridWidth, gridHeight, minimumZ, maximumZ, activeSides,
                        nextSides);
                    (activeSides, nextSides) = (nextSides, activeSides);

                    /* The grid below the one just meshed is not needed anymore. */
                    if (offset == 0)
                    {
                        Release(ref previousTail);
                    }
                    else
                    {
                        Release(ref window[offset - 1]);
                    }
                }

                /* Carry the last meshed grid as the neighbour below the next chunk, and the look ahead grid as
                 * the first grid of the next chunk. */
                previousTail = window[chunkLength - 1];
                window[chunkLength - 1] = null;
                if (windowLength > chunkLength)
                {
                    window[0] = window[chunkLength];
                    window[chunkLength] = null;
                }
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
                    context.MinimumBounds, context.MaximumBounds, stride, source, options.Quality,
                    bounds, pixelWidth, pixelHeight, gridWidth, gridHeight, workAroundFlip);
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
            Release(ref previousTail);
            for (var index = 0; index < window.Length; index++)
            {
                Release(ref window[index]);
            }
        }

        static void Release(ref byte[]? occupancy)
        {
            if (occupancy is null) return;
            ArrayPool<byte>.Shared.Return(occupancy);
            occupancy = null;
        }
    }

    /// <summary>
    /// Gets how many layers may be decoded at once and how many of them to hold in the meshing window, scaling
    /// both to the memory that is actually free so that large files do not trade a slow build for swapping.
    /// </summary>
    private static (int Parallelism, int ChunkSize) GetPipelineSizing(FileFormat slicerFile, int gridLength)
    {
        var memoryInfo = GC.GetGCMemoryInfo();
        var freeBytes = Math.Max(0, memoryInfo.TotalAvailableMemoryBytes - memoryInfo.MemoryLoadBytes);
        var budget = Math.Clamp(freeBytes / 4, 256L << 20, 4L << 30);

        var maximumParallelism = CoreSettings.MaxDegreeOfParallelism <= 0
            ? Environment.ProcessorCount
            : CoreSettings.MaxDegreeOfParallelism;

        /* Each worker holds one decoded layer plus the transient allocations of the resize. */
        var layerBytes = Math.Max(1L, (long)slicerFile.ResolutionX * slicerFile.ResolutionY * 2);
        var parallelism = (int)Math.Clamp(budget * 3 / 4 / layerBytes, 1, maximumParallelism);
        var chunkSize = (int)Math.Clamp(budget / 4 / Math.Max(1, gridLength), parallelism, parallelism * 4L);
        return (parallelism, chunkSize);
    }

    private static byte[] BuildOccupancy(
        FileFormat slicerFile,
        Layer layer,
        Rectangle bounds,
        int gridWidth,
        int gridHeight,
        int gridLength,
        FlipDirection flip)
    {
        var occupancy = ArrayPool<byte>.Shared.Rent(gridLength);

        try
        {
            using var mat = layer.LayerMat;
            using var roi = mat.Roi(bounds);
            CvInvoke.Threshold(roi, roi, 127, byte.MaxValue, ThresholdType.Binary);

            /* Layers sharing this Z position follow at consecutive indexes, merge them into this one. */
            for (var layerIndex = layer.Index + 1;
                 layerIndex < slicerFile.LayerCount && slicerFile[layerIndex].PositionZ == layer.PositionZ;
                 layerIndex++)
            {
                using var siblingMat = slicerFile[layerIndex].LayerMat;
                using var siblingRoi = siblingMat.Roi(bounds);
                CvInvoke.Threshold(siblingRoi, siblingRoi, 127, byte.MaxValue, ThresholdType.Binary);
                CvInvoke.Max(roi, siblingRoi, roi);
            }

            if (flip != FlipDirection.None)
            {
                CvInvoke.Flip(roi, roi, (FlipType)flip);
            }

            using var sampled = new Mat();
            CvInvoke.Resize(roi, sampled, new Size(gridWidth, gridHeight), 0, 0, Inter.Area);

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

        /// <summary>Fraction, 0 to 1, of the layers meshed so far. Set by the caller as it walks the layers.</summary>
        public float LayerProgress { get; set; }

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
            if (indices.Count / 3 + 2 > maximumTriangleCount && LayerProgress < KeepDetailAfterProgress)
                throw new MeshBudgetExceededException();
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
