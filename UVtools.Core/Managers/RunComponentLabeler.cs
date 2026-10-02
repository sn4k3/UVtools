/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Emgu.CV;
using EmguExtensions;

namespace UVtools.Core.Managers;

/// <summary>
/// Labels the connected components of a binary image using horizontal runs of foreground pixels, which makes the cost
/// proportional to the number of runs instead of the number of pixels. Layers are mostly made of large solid areas, so
/// this is a lot faster than a per pixel labeling for them.
/// A pixel is foreground when it is greater than the threshold.
/// </summary>
/// <remarks>
/// Components are numbered in raster order of their first pixel, same as the OpenCV labeling.
/// Instances keep their buffers to be reused, and are not thread safe, use <see cref="ForCurrentThread"/>.
/// </remarks>
internal sealed class RunComponentLabeler
{
    [ThreadStatic] private static RunComponentLabeler? _threadInstance;

    /// <summary>
    /// Above this many runs the buffers are not kept after the labeling, to not hold on memory after a pathological image.
    /// </summary>
    private const int MaximumRetainedRuns = 1 << 22;

    private int[] _rowStart = new int[256];
    private int[] _runY = new int[1024];
    private int[] _runStart = new int[1024];
    private int[] _runEnd = new int[1024];
    private int[] _parent = new int[1024];
    private int[] _runComponent = new int[1024];
    private int[] _runsByComponent = new int[1024];

    private int[] _componentMinX = new int[64];
    private int[] _componentMaxX = new int[64];
    private int[] _componentMinY = new int[64];
    private int[] _componentMaxY = new int[64];
    private int[] _componentArea = new int[64];
    private int[] _componentRunOffset = new int[65];

    /// <summary>
    /// Gets the labeler of the current thread.
    /// </summary>
    public static RunComponentLabeler ForCurrentThread => _threadInstance ??= new RunComponentLabeler();

    /// <summary>
    /// Gets the number of runs found.
    /// </summary>
    public int RunCount { get; private set; }

    /// <summary>
    /// Gets the number of components found.
    /// </summary>
    public int ComponentCount { get; private set; }

    /// <summary>
    /// Gets the row of a run.
    /// </summary>
    public int GetRunY(int run) => _runY[run];

    /// <summary>
    /// Gets the first column of a run.
    /// </summary>
    public int GetRunStart(int run) => _runStart[run];

    /// <summary>
    /// Gets the last column of a run, inclusive.
    /// </summary>
    public int GetRunEnd(int run) => _runEnd[run];

    /// <summary>
    /// Gets the number of foreground pixels of a component.
    /// </summary>
    public int GetArea(int component) => _componentArea[component];

    /// <summary>
    /// Gets the bounding rectangle of a component.
    /// </summary>
    public Rectangle GetBounds(int component)
    {
        return new Rectangle(_componentMinX[component], _componentMinY[component],
            _componentMaxX[component] - _componentMinX[component] + 1,
            _componentMaxY[component] - _componentMinY[component] + 1);
    }

    /// <summary>
    /// Gets the runs of a component, in raster order.
    /// </summary>
    public ReadOnlySpan<int> GetRuns(int component)
    {
        var start = _componentRunOffset[component];
        return _runsByComponent.AsSpan(start, _componentRunOffset[component + 1] - start);
    }

    /// <summary>
    /// Labels the components of an 8-bit single channel image.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <param name="threshold">A pixel is foreground when greater than this value.</param>
    /// <param name="eightConnected">True to also connect diagonal neighbors, otherwise only left, right, top and bottom.</param>
    public void Label(Mat image, byte threshold, bool eightConnected)
    {
        var height = image.Height;
        if (_rowStart.Length < height + 1) _rowStart = new int[height + 1];

        RunCount = 0;
        for (var y = 0; y < height; y++)
        {
            _rowStart[y] = RunCount;
            AddRowRuns(image.GetReadOnlyRowSpanOfBytes(y), y, threshold);
        }

        _rowStart[height] = RunCount;

        for (var run = 0; run < RunCount; run++)
        {
            _parent[run] = run;
        }

        for (var y = 1; y < height; y++)
        {
            LinkRows(_rowStart[y - 1], _rowStart[y], _rowStart[y + 1], eightConnected);
        }

        NumberComponents();
    }

    private void EnsureRunCapacity(int required)
    {
        if (required <= _runStart.Length) return;
        var size = Math.Max(required, _runStart.Length * 2);
        Array.Resize(ref _runY, size);
        Array.Resize(ref _runStart, size);
        Array.Resize(ref _runEnd, size);
        Array.Resize(ref _parent, size);
        Array.Resize(ref _runComponent, size);
        Array.Resize(ref _runsByComponent, size);
    }

    private void AddRun(int y, int start, int end)
    {
        EnsureRunCapacity(RunCount + 1);
        _runY[RunCount] = y;
        _runStart[RunCount] = start;
        _runEnd[RunCount] = end;
        RunCount++;
    }

    /// <summary>
    /// Finds the runs of foreground pixels of a row, 64 pixels at a time.
    /// </summary>
    private void AddRowRuns(ReadOnlySpan<byte> row, int y, byte threshold)
    {
        var width = row.Length;
        var thresholdVector = Vector128.Create(threshold);
        ref var reference = ref MemoryMarshal.GetReference(row);

        var runStart = -1; // -1 when not inside of a run
        var x = 0;

        for (; x <= width - 64; x += 64)
        {
            var bits =
                (ulong)Vector128.ExtractMostSignificantBits(
                    Vector128.GreaterThan(Vector128.LoadUnsafe(ref reference, (nuint)x), thresholdVector)) |
                (ulong)Vector128.ExtractMostSignificantBits(
                    Vector128.GreaterThan(Vector128.LoadUnsafe(ref reference, (nuint)(x + 16)), thresholdVector)) << 16 |
                (ulong)Vector128.ExtractMostSignificantBits(
                    Vector128.GreaterThan(Vector128.LoadUnsafe(ref reference, (nuint)(x + 32)), thresholdVector)) << 32 |
                (ulong)Vector128.ExtractMostSignificantBits(
                    Vector128.GreaterThan(Vector128.LoadUnsafe(ref reference, (nuint)(x + 48)), thresholdVector)) << 48;

            if (bits == 0)
            {
                if (runStart >= 0)
                {
                    AddRun(y, runStart, x - 1);
                    runStart = -1;
                }

                continue;
            }

            if (bits == ulong.MaxValue)
            {
                if (runStart < 0) runStart = x;
                continue;
            }

            var position = 0;
            while (position < 64)
            {
                if (runStart < 0)
                {
                    var remaining = bits >> position;
                    if (remaining == 0) break;
                    position += BitOperations.TrailingZeroCount(remaining);
                    runStart = x + position;
                }

                var background = ~bits >> position;
                if (background == 0) break; // The run continues in the next block

                position += BitOperations.TrailingZeroCount(background);
                AddRun(y, runStart, x + position - 1);
                runStart = -1;
            }
        }

        for (; x < width; x++)
        {
            if (row[x] > threshold)
            {
                if (runStart < 0) runStart = x;
            }
            else if (runStart >= 0)
            {
                AddRun(y, runStart, x - 1);
                runStart = -1;
            }
        }

        if (runStart >= 0)
        {
            AddRun(y, runStart, width - 1);
        }
    }

    private int Find(int run)
    {
        while (_parent[run] != run)
        {
            _parent[run] = _parent[_parent[run]];
            run = _parent[run];
        }

        return run;
    }

    private void Union(int a, int b)
    {
        var rootA = Find(a);
        var rootB = Find(b);
        if (rootA == rootB) return;

        // The root is always the first run (in raster order) of the component
        if (rootA < rootB) _parent[rootB] = rootA;
        else _parent[rootA] = rootB;
    }

    /// <summary>
    /// Connects the runs of a row with the touching runs of the row above.
    /// </summary>
    private void LinkRows(int aStartIndex, int aEndIndex, int bEndIndex, bool eightConnected)
    {
        var a = aStartIndex; // Row above
        var b = aEndIndex; // Row
        var diagonal = eightConnected ? 1 : 0;

        while (a < aEndIndex && b < bEndIndex)
        {
            var endA = _runEnd[a];
            var endB = _runEnd[b];

            if (_runStart[a] <= endB + diagonal && _runStart[b] <= endA + diagonal)
            {
                Union(a, b);
            }

            // The run that ends first can not touch any further run of the other row
            if (endA < endB) a++;
            else b++;
        }
    }

    private void NumberComponents()
    {
        var componentCount = 0;
        for (var run = 0; run < RunCount; run++)
        {
            if (_parent[run] == run) componentCount++;
        }

        if (_componentMinX.Length < componentCount)
        {
            var size = Math.Max(componentCount, _componentMinX.Length * 2);
            _componentMinX = new int[size];
            _componentMaxX = new int[size];
            _componentMinY = new int[size];
            _componentMaxY = new int[size];
            _componentArea = new int[size];
            _componentRunOffset = new int[size + 1];
        }

        ComponentCount = componentCount;
        Array.Clear(_componentArea, 0, componentCount);
        Array.Clear(_componentRunOffset, 0, componentCount + 1);

        // Roots are the first run of their component, so they are met before any other run of it
        var next = 0;
        for (var run = 0; run < RunCount; run++)
        {
            int component;
            if (_parent[run] == run)
            {
                component = next++;
                _componentMinX[component] = int.MaxValue;
                _componentMaxX[component] = int.MinValue;
                _componentMinY[component] = _runY[run];
            }
            else
            {
                component = _runComponent[Find(run)];
            }

            _runComponent[run] = component;
            _componentMinX[component] = Math.Min(_componentMinX[component], _runStart[run]);
            _componentMaxX[component] = Math.Max(_componentMaxX[component], _runEnd[run]);
            _componentMaxY[component] = _runY[run];
            _componentArea[component] += _runEnd[run] - _runStart[run] + 1;
            _componentRunOffset[component + 1]++;
        }

        for (var component = 0; component < componentCount; component++)
        {
            _componentRunOffset[component + 1] += _componentRunOffset[component];
        }

        // Distribute the runs per component, keeping the raster order
        for (var run = 0; run < RunCount; run++)
        {
            var component = _runComponent[run];
            _runsByComponent[_componentRunOffset[component]++] = run;
        }

        // The offsets were moved to the end of each component, put them back to the start
        for (var component = componentCount; component > 0; component--)
        {
            _componentRunOffset[component] = _componentRunOffset[component - 1];
        }

        _componentRunOffset[0] = 0;
    }

    /// <summary>
    /// Releases the buffers if a pathological image made them grow too much, call when done with the results.
    /// </summary>
    public void TrimExcess()
    {
        if (_runStart.Length <= MaximumRetainedRuns) return;
        _runY = new int[1024];
        _runStart = new int[1024];
        _runEnd = new int[1024];
        _parent = new int[1024];
        _runComponent = new int[1024];
        _runsByComponent = new int[1024];
        RunCount = 0;
        ComponentCount = 0;
    }
}