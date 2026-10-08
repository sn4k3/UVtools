/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Buffers;

namespace UVtools.Core.Objects;

/// <summary>
/// A directed line segment in pixel space, the coordinates are continuous: the pixel with index <c>i</c> covers the
/// range <c>[i, i + 1)</c> and its center is at <c>i + 0.5</c>.
/// </summary>
/// <param name="StartX">Start X position</param>
/// <param name="StartY">Start Y position</param>
/// <param name="EndX">End X position</param>
/// <param name="EndY">End Y position</param>
public readonly record struct DirectedSegmentF(float StartX, float StartY, float EndX, float EndY);

/// <summary>
/// Rasterizes an unordered set of directed segments into a 8 bit image using the non-zero winding rule.
/// </summary>
/// <remarks>
/// Unlike the even-odd rule, the non-zero rule keeps overlapping or touching shapes of the same orientation filled,
/// while a shape with the opposite orientation inside another still creates a hole.
/// </remarks>
public static class DirectedSegmentFiller
{
    private struct Edge
    {
        /// <summary>First pixel row whose center is crossed by this edge</summary>
        public int RowStart;

        /// <summary>Last pixel row whose center is crossed by this edge, inclusive</summary>
        public int RowEnd;

        /// <summary>X where the edge crosses <see cref="Y0"/></summary>
        public double X0;

        /// <summary>The lowest Y of the edge</summary>
        public double Y0;

        /// <summary>X change for each unit of Y</summary>
        public double DxDy;

        /// <summary>+1 when the segment goes down in Y (to a higher Y), -1 when it goes up</summary>
        public int Direction;
    }

    /// <summary>
    /// Fills the area enclosed by the segments with <paramref name="value"/> using the non-zero winding rule.
    /// </summary>
    /// <remarks>
    /// The image is sampled at the pixel centers: a pixel is filled when its center <c>(x + 0.5, y + 0.5)</c> has a
    /// non-zero winding number. Edges are half open in Y, so an edge lying exactly on a row center belongs to the row
    /// below it and the result is deterministic and independent of the segments order. Horizontal segments and
    /// segments with non finite coordinates do not contribute. Pixels outside the image are clipped.
    /// </remarks>
    /// <param name="segments">The directed segments, in any order</param>
    /// <param name="pixels">The 8 bit image to write into</param>
    /// <param name="width">Image width in pixels</param>
    /// <param name="height">Image height in pixels</param>
    /// <param name="stride">Bytes between the start of two rows</param>
    /// <param name="value">The value to write to the filled pixels</param>
    public static void Fill(ReadOnlySpan<DirectedSegmentF> segments, Span<byte> pixels, int width, int height, int stride, byte value = byte.MaxValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);
        ArgumentOutOfRangeException.ThrowIfLessThan(stride, width);
        if (width == 0 || height == 0 || segments.IsEmpty) return;
        if ((long)(height - 1) * stride + width > pixels.Length)
            throw new ArgumentException("The pixels span is smaller than the stride, width and height requires.", nameof(pixels));

        var edgePool = ArrayPool<Edge>.Shared;
        var intPool = ArrayPool<int>.Shared;
        var doublePool = ArrayPool<double>.Shared;

        var edges = edgePool.Rent(segments.Length);
        var keys = intPool.Rent(segments.Length);
        var active = intPool.Rent(segments.Length);
        int edgeCount = 0;
        double[]? crossX = null;
        int[]? crossDir = null;

        try
        {
            // Build the edge table, skipping horizontal edges and edges that never cross a row center
            int firstRow = int.MaxValue;
            int lastRow = int.MinValue;
            foreach (var segment in segments)
            {
                double y0 = segment.StartY;
                double y1 = segment.EndY;
                double x0 = segment.StartX;
                double x1 = segment.EndX;
                if (!double.IsFinite(x0) || !double.IsFinite(y0) || !double.IsFinite(x1) || !double.IsFinite(y1)) continue;
                if (y0 == y1) continue;

                int direction = 1;
                if (y1 < y0)
                {
                    (y0, y1) = (y1, y0);
                    (x0, x1) = (x1, x0);
                    direction = -1;
                }

                // Rows r where y0 <= r + 0.5 < y1
                var rowStart = Math.Max(Math.Ceiling(y0 - 0.5), 0);
                var rowEnd = Math.Min(Math.Ceiling(y1 - 0.5) - 1, height - 1);
                if (rowEnd < rowStart) continue;

                ref var edge = ref edges[edgeCount];
                edge.RowStart = (int)rowStart;
                edge.RowEnd = (int)rowEnd;
                edge.X0 = x0;
                edge.Y0 = y0;
                edge.DxDy = (x1 - x0) / (y1 - y0);
                edge.Direction = direction;
                keys[edgeCount] = edge.RowStart;
                edgeCount++;

                if (edge.RowStart < firstRow) firstRow = edge.RowStart;
                if (edge.RowEnd > lastRow) lastRow = edge.RowEnd;
            }

            if (edgeCount == 0) return;

            Array.Sort(keys, edges, 0, edgeCount);

            crossX = doublePool.Rent(edgeCount);
            crossDir = intPool.Rent(edgeCount);

            int nextEdge = 0;
            int activeCount = 0;

            for (int row = firstRow; row <= lastRow; row++)
            {
                // Retire the edges that ended before this row
                int kept = 0;
                for (int i = 0; i < activeCount; i++)
                {
                    if (edges[active[i]].RowEnd >= row) active[kept++] = active[i];
                }
                activeCount = kept;

                // Activate the edges that start at this row
                while (nextEdge < edgeCount && edges[nextEdge].RowStart <= row)
                {
                    active[activeCount++] = nextEdge++;
                }

                if (activeCount < 2) continue;

                // Crossings with the row center
                double centerY = row + 0.5;
                for (int i = 0; i < activeCount; i++)
                {
                    ref var edge = ref edges[active[i]];
                    crossX[i] = edge.X0 + (centerY - edge.Y0) * edge.DxDy;
                    crossDir[i] = edge.Direction;
                }

                MemoryExtensions.Sort(crossX.AsSpan(0, activeCount), crossDir.AsSpan(0, activeCount));

                var rowSpan = pixels.Slice(row * stride, width);
                int winding = 0;
                for (int i = 0; i < activeCount - 1; i++)
                {
                    winding += crossDir[i];
                    if (winding == 0) continue;

                    // Pixels x where crossX[i] <= x + 0.5 < crossX[i + 1]
                    var from = Math.Max(Math.Ceiling(crossX[i] - 0.5), 0);
                    var to = Math.Min(Math.Ceiling(crossX[i + 1] - 0.5), width);
                    if (to <= from) continue;

                    rowSpan.Slice((int)from, (int)(to - from)).Fill(value);
                }
            }
        }
        finally
        {
            edgePool.Return(edges);
            intPool.Return(keys);
            intPool.Return(active);
            if (crossX is not null) doublePool.Return(crossX);
            if (crossDir is not null) intPool.Return(crossDir);
        }
    }
}
