/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2020-2026 PTRTECH
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Emgu.CV;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Managers;
using UVtools.Core.Operations;

namespace UVtools.Core.Voxel;

/// <summary>
/// A face-connected group of nonzero pixels in the original slices. Exposures at the same height share a plane.
/// </summary>
public sealed class VoxelObjectSelection
{
    internal readonly record struct PixelRun(int Y, int Start, int Length);
    internal readonly record struct Plane(uint FirstLayer, uint EndLayer, Rectangle Bounds, PixelRun[] Runs);

    private readonly FileFormat _file;
    private readonly VoxelPreviewSourceSnapshot _snapshot;

    internal Plane[] Planes { get; }
    public Rectangle PixelBounds { get; }
    public float MinZ { get; }
    public float MaxZ { get; }

    private VoxelObjectSelection(FileFormat file, VoxelPreviewSourceSnapshot snapshot, Plane[] planes,
        Rectangle pixelBounds, float minZ, float maxZ)
    {
        _file = file;
        _snapshot = snapshot;
        Planes = planes;
        PixelBounds = pixelBounds;
        MinZ = minZ;
        MaxZ = maxZ;
    }

    public bool IsCurrentFor(FileFormat file) => ReferenceEquals(_file, file) && _snapshot.Matches(file);

    /// <summary>
    /// Resolves a preview surface hit to a full-resolution connected group.
    /// </summary>
    public static VoxelObjectSelection? Find(FileFormat file, VoxelPreviewMesh mesh, Vector3 point, Vector3 normal,
        OperationProgress? progress = null)
    {
        if (!file.HaveLayers || !mesh.IsCurrentFor(file) || mesh.VertexCount == 0) return null;

        using var memoryScope = MemoryGovernor.BeginLayerMemoryScope((long)file.ResolutionX * file.ResolutionY);
        var snapshot = VoxelPreviewSourceSnapshot.Capture(file);
        if (snapshot.Revision != mesh.SourceRevision) return null;
        var bounds = mesh.ModelBounds;
        var layers = file.GetDistinctLayersByPositionZ().ToArray();
        /* Move half a cell and half a layer inside the hit surface so that a boundary belongs to its solid cell.
         * A smaller step is lost in the rounding of the ray hit, and the click then lands in the empty cell or
         * layer next to the surface. Coordinates are inside the flipped model area, placed at PreviewPlacement. */
        var inside = mesh.SamplingStride * 0.5f;
        var layerHeight = file.LayerHeight > 0 ? file.LayerHeight : 0.01f;
        var x = point.X / mesh.PixelWidth - mesh.PreviewPlacement.X - normal.X * inside;
        var y = point.Y / mesh.PixelHeight - mesh.PreviewPlacement.Y - normal.Y * inside;
        var z = point.Z - normal.Z * layerHeight * 0.5f;
        var seedPlane = Array.FindIndex(layers, layer => layer.PositionZ >= z);
        if (seedPlane < 0) seedPlane = layers.Length - 1;

        var cellX = Math.Clamp((int)MathF.Floor(x / mesh.SamplingStride), 0, mesh.GridWidth - 1);
        var cellY = Math.Clamp((int)MathF.Floor(y / mesh.SamplingStride), 0, mesh.GridHeight - 1);
        // The preview uses an area resize. Its cells cover the crop proportionally, including any partial edge cell.
        var sample = Rectangle.FromLTRB(
            (int)((long)cellX * bounds.Width / mesh.GridWidth),
            (int)((long)cellY * bounds.Height / mesh.GridHeight),
            DivideRoundUp((cellX + 1L) * bounds.Width, mesh.GridWidth),
            DivideRoundUp((cellY + 1L) * bounds.Height, mesh.GridHeight));
        sample.Offset(bounds.Location);
        sample = FlipBounds(sample, bounds, mesh.WorkAroundFlip);
        // Tolerate a hit on the wrong side of a cell boundary, the closest pixels still win.
        sample.Inflate(mesh.SamplingStride, mesh.SamplingStride);

        if (mesh.WorkAroundFlip is FlipDirection.Horizontally or FlipDirection.Both) x = bounds.Width - x;
        if (mesh.WorkAroundFlip is FlipDirection.Vertically or FlipDirection.Both) y = bounds.Height - y;
        x += bounds.X;
        y += bounds.Y;

        return FindFromSeed(file, mesh, snapshot, layers, seedPlane, sample, x, y, progress);
    }

    /// <summary>
    /// Selects the connected group that contains a pixel, such as the <see cref="Anchor"/> of an edited selection
    /// moved by the edit offset.
    /// </summary>
    /// <param name="file">The file to select in.</param>
    /// <param name="mesh">A preview mesh that is current for <paramref name="file"/>.</param>
    /// <param name="layerIndex">A layer at the height of the pixel.</param>
    /// <param name="pixel">The pixel in slice coordinates.</param>
    /// <param name="progress">Optional progress, also used to cancel.</param>
    /// <returns>The selection, or null when there are no pixels there.</returns>
    public static VoxelObjectSelection? FindAtPixel(FileFormat file, VoxelPreviewMesh mesh, uint layerIndex,
        Point pixel, OperationProgress? progress = null)
    {
        if (!file.HaveLayers || !file.ContainsLayer(layerIndex) || !mesh.IsCurrentFor(file) || mesh.VertexCount == 0)
            return null;

        using var memoryScope = MemoryGovernor.BeginLayerMemoryScope((long)file.ResolutionX * file.ResolutionY);
        var snapshot = VoxelPreviewSourceSnapshot.Capture(file);
        if (snapshot.Revision != mesh.SourceRevision) return null;
        var layers = file.GetDistinctLayersByPositionZ().ToArray();
        var positionZ = file[layerIndex].PositionZ;
        var seedPlane = Array.FindIndex(layers, layer => layer.PositionZ >= positionZ);
        if (seedPlane < 0) return null;

        return FindFromSeed(file, mesh, snapshot, layers, seedPlane, new Rectangle(pixel.X, pixel.Y, 1, 1),
            pixel.X + 0.5f, pixel.Y + 0.5f, progress);
    }

    /// <summary>
    /// Gets a pixel of the selection and a layer at its height. Moving it by an edit offset gives a pixel of the
    /// edited object, see <see cref="FindAtPixel"/>.
    /// </summary>
    public (uint LayerIndex, Point Pixel) Anchor
    {
        get
        {
            var plane = Planes[0];
            var run = plane.Runs[0];
            return (plane.FirstLayer, new Point(run.Start, run.Y));
        }
    }

    private static VoxelObjectSelection? FindFromSeed(FileFormat file, VoxelPreviewMesh mesh,
        VoxelPreviewSourceSnapshot snapshot, Layer[] layers, int seedPlane, Rectangle sample, float x, float y,
        OperationProgress? progress)
    {
        var bounds = mesh.ModelBounds;

        // Keep only component links across the stack and the runs of two neighboring planes.
        var parents = new List<int>(layers.Length);
        var offsets = new int[layers.Length + 1];
        var current = new RunComponentLabeler();
        var previous = new RunComponentLabeler();
        var seed = -1;
        // The plane below and above are only used when the hit plane has no pixels near the hit.
        var seedPriority = int.MaxValue;
        var seedDistance = float.PositiveInfinity;

        progress?.Reset("Labeling slices", (uint)layers.Length);
        for (var planeIndex = 0; planeIndex < layers.Length; planeIndex++)
        {
            progress?.PauseOrCancelIfRequested();
            var first = layers[planeIndex];
            var end = GetPlaneEnd(file, first);
            LabelPlane(file, bounds, first.Index, end, current);
            var offset = parents.Count;
            for (var i = 0; i < current.ComponentCount; i++)
                parents.Add(offset + i);
            offsets[planeIndex + 1] = parents.Count;

            if (Math.Abs(planeIndex - seedPlane) <= 1)
            {
                var priority = planeIndex == seedPlane ? 0 : 1;
                var component = FindSeed(current, bounds, sample, x, y, out var distance);
                if (component >= 0 && (priority < seedPriority || (priority == seedPriority && distance < seedDistance)))
                {
                    seed = offset + component;
                    seedPriority = priority;
                    seedDistance = distance;
                }
            }

            if (planeIndex > 0)
            {
                LinkPlanes(previous, current, offsets[planeIndex - 1], offset, CollectionsMarshal.AsSpan(parents));
            }
            (previous, current) = (current, previous);
            progress?.LockAndIncrement();
        }

        if (seed < 0 || !snapshot.Matches(file)) return null;

        var links = CollectionsMarshal.AsSpan(parents);
        var root = FindRoot(links, seed);
        var planes = new List<Plane>();
        var pixelBounds = Rectangle.Empty;
        var minZ = 0f;
        var maxZ = 0f;

        progress?.Reset("Collecting selected pixels", (uint)layers.Length);
        for (var planeIndex = 0; planeIndex < layers.Length; planeIndex++)
        {
            progress?.PauseOrCancelIfRequested();
            var offset = offsets[planeIndex];
            var endOffset = offsets[planeIndex + 1];
            var firstSelected = offset;
            while (firstSelected < endOffset && FindRoot(links, firstSelected) != root) firstSelected++;
            if (firstSelected < endOffset)
            {
                var first = layers[planeIndex];
                var end = GetPlaneEnd(file, first);
                LabelPlane(file, bounds, first.Index, end, current);
                if (current.ComponentCount != endOffset - offset || !snapshot.Matches(file)) return null;

                var runCount = 0;
                var planeBounds = Rectangle.Empty;
                for (var id = firstSelected; id < endOffset; id++)
                {
                    if (FindRoot(links, id) != root) continue;
                    runCount += current.GetRuns(id - offset).Length;
                    var componentBounds = current.GetBounds(id - offset);
                    planeBounds = planeBounds.IsEmpty ? componentBounds : Rectangle.Union(planeBounds, componentBounds);
                }

                var runs = new PixelRun[runCount];
                var written = 0;
                for (var run = 0; run < current.RunCount; run++)
                {
                    if (FindRoot(links, offset + current.GetRunComponent(run)) != root) continue;
                    var start = current.GetRunStart(run);
                    runs[written++] = new PixelRun(bounds.Y + current.GetRunY(run), bounds.X + start,
                        current.GetRunEnd(run) - start + 1);
                }

                planeBounds.Offset(bounds.Location);
                planes.Add(new Plane(first.Index, end, planeBounds, runs));
                if (pixelBounds.IsEmpty)
                {
                    minZ = planeIndex == 0 ? mesh.MinimumBounds.Z : layers[planeIndex - 1].PositionZ;
                    pixelBounds = planeBounds;
                }
                else
                {
                    pixelBounds = Rectangle.Union(pixelBounds, planeBounds);
                }
                maxZ = first.PositionZ;
            }
            progress?.LockAndIncrement();
        }

        return planes.Count == 0 || !snapshot.Matches(file)
            ? null
            : new VoxelObjectSelection(file, snapshot, planes.ToArray(), pixelBounds, minZ, maxZ);
    }

    public (Vector3 Min, Vector3 Max) GetPreviewBounds(VoxelPreviewMesh mesh)
    {
        var bounds = mesh.MapToPreview(PixelBounds);
        return (new Vector3(bounds.Left * mesh.PixelWidth, bounds.Top * mesh.PixelHeight, MinZ),
            new Vector3(bounds.Right * mesh.PixelWidth, bounds.Bottom * mesh.PixelHeight, MaxZ));
    }

    /// <summary>
    /// Gets the range of preview offsets, in millimeters, that keep the selection inside the build plate.
    /// </summary>
    /// <param name="mesh">The preview mesh the selection was made on.</param>
    /// <returns>The minimum and maximum X/Y offsets in preview coordinates.</returns>
    public (Vector2 Min, Vector2 Max) GetPreviewOffsetLimits(VoxelPreviewMesh mesh)
    {
        // Allowed offsets in slice pixels, mapped to the preview where a work-around flip mirrors the axis.
        var minX = -PixelBounds.Left * mesh.PixelWidth;
        var maxX = ((long)_file.ResolutionX - PixelBounds.Right) * mesh.PixelWidth;
        var minY = -PixelBounds.Top * mesh.PixelHeight;
        var maxY = ((long)_file.ResolutionY - PixelBounds.Bottom) * mesh.PixelHeight;
        if (mesh.WorkAroundFlip is FlipDirection.Horizontally or FlipDirection.Both) (minX, maxX) = (-maxX, -minX);
        if (mesh.WorkAroundFlip is FlipDirection.Vertically or FlipDirection.Both) (minY, maxY) = (-maxY, -minY);
        return (new Vector2(minX, minY), new Vector2(maxX, maxY));
    }

    /// <summary>
    /// Converts a preview offset in millimeters to a slice offset in pixels.
    /// </summary>
    public static Point GetPixelOffset(VoxelPreviewMesh mesh, double xMillimeters, double yMillimeters)
    {
        var x = (int)Math.Round(xMillimeters / mesh.PixelWidth);
        var y = (int)Math.Round(yMillimeters / mesh.PixelHeight);
        if (mesh.WorkAroundFlip is FlipDirection.Horizontally or FlipDirection.Both) x = -x;
        if (mesh.WorkAroundFlip is FlipDirection.Vertically or FlipDirection.Both) y = -y;
        return new Point(x, y);
    }

    private static int DivideRoundUp(long value, int divisor) => (int)((value + divisor - 1) / divisor);

    private static Rectangle FlipBounds(Rectangle area, Rectangle bounds, FlipDirection flip)
    {
        if (flip is FlipDirection.Horizontally or FlipDirection.Both) area.X = bounds.Left + (bounds.Right - area.Right);
        if (flip is FlipDirection.Vertically or FlipDirection.Both) area.Y = bounds.Top + (bounds.Bottom - area.Bottom);
        return area;
    }

    private static uint GetPlaneEnd(FileFormat file, Layer first)
    {
        var end = first.Index + 1;
        while (end < file.LayerCount && file[end].PositionZ == first.PositionZ) end++;
        return end;
    }

    private static void LabelPlane(FileFormat file, Rectangle bounds, uint first, uint end, RunComponentLabeler labeler)
    {
        using var image = GetPlaneImage(file, bounds, first, end);
        labeler.Label(image, 0, eightConnected: false);
    }

    internal static Mat GetPlaneImage(FileFormat file, Rectangle bounds, uint first, uint end)
    {
        var image = file[first].GetRoiMat(bounds);
        try
        {
            for (var i = first + 1; i < end; i++)
            {
                if (file[i].IsEmpty) continue;
                using var sibling = file[i].GetRoiMat(bounds);
                CvInvoke.Max(image, sibling, image);
            }
            return image;
        }
        catch
        {
            image.Dispose();
            throw;
        }
    }

    private static int FindSeed(RunComponentLabeler labeler, Rectangle bounds, Rectangle sample, float x, float y,
        out float closest)
    {
        closest = float.PositiveInfinity;
        var component = -1;
        for (var run = 0; run < labeler.RunCount; run++)
        {
            var row = bounds.Top + labeler.GetRunY(run);
            if (row < sample.Top) continue;
            if (row >= sample.Bottom) break;
            var start = Math.Max(sample.Left, bounds.Left + labeler.GetRunStart(run));
            var end = Math.Min(sample.Right - 1, bounds.Left + labeler.GetRunEnd(run));
            if (start > end) continue;
            var dx = Math.Clamp(x, start + 0.5f, end + 0.5f) - x;
            var dy = row + 0.5f - y;
            var distance = dx * dx + dy * dy;
            if (distance >= closest) continue;
            closest = distance;
            component = labeler.GetRunComponent(run);
        }
        return component;
    }

    private static void LinkPlanes(RunComponentLabeler previous, RunComponentLabeler current,
        int previousOffset, int currentOffset, Span<int> parents)
    {
        var a = 0;
        var b = 0;
        while (a < previous.RunCount && b < current.RunCount)
        {
            var previousRow = previous.GetRunY(a);
            var currentRow = current.GetRunY(b);
            if (previousRow < currentRow)
            {
                a++;
                continue;
            }
            if (currentRow < previousRow)
            {
                b++;
                continue;
            }

            var previousEnd = previous.GetRunEnd(a);
            var currentEnd = current.GetRunEnd(b);
            if (previous.GetRunStart(a) <= currentEnd && current.GetRunStart(b) <= previousEnd)
            {
                var previousRoot = FindRoot(parents, previousOffset + previous.GetRunComponent(a));
                var currentRoot = FindRoot(parents, currentOffset + current.GetRunComponent(b));
                parents[Math.Max(previousRoot, currentRoot)] = Math.Min(previousRoot, currentRoot);
            }

            if (previousEnd < currentEnd) a++;
            else b++;
        }
    }

    private static int FindRoot(Span<int> parents, int item)
    {
        while (parents[item] != item)
        {
            parents[item] = parents[parents[item]];
            item = parents[item];
        }
        return item;
    }
}
