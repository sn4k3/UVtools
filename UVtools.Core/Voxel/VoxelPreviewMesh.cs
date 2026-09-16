/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2020-2026 PTRTECH
 */

using System;
using System.Buffers;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Voxel;

public enum VoxelPreviewQuality : byte
{
    Fast,
    Balanced,
    Detailed
}

public enum VoxelPreviewRenderMode : byte
{
    Solid,
    [Description("X-Ray")] XRay,
    Wireframe
}

public enum VoxelPreviewLightingMode : byte
{
    Camera,
    Studio,
    Flat
}

public readonly record struct VoxelPreviewMeshOptions(
    VoxelPreviewQuality Quality,
    int MaximumPlaneDimension,
    uint MaximumTriangleCount)
{
    public static VoxelPreviewMeshOptions FromQuality(VoxelPreviewQuality quality)
    {
        return quality switch
        {
            VoxelPreviewQuality.Fast => new VoxelPreviewMeshOptions(quality, 512, 250_000),
            VoxelPreviewQuality.Balanced => new VoxelPreviewMeshOptions(quality, 1024, 750_000),
            VoxelPreviewQuality.Detailed => new VoxelPreviewMeshOptions(quality, 2048, 1_500_000),
            _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, null)
        };
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct VoxelPreviewVertex(Vector3 position, Vector3 normal)
{
    public readonly Vector3 Position = position;
    public readonly Vector3 Normal = normal;
}

internal readonly record struct VoxelPreviewSourceSnapshot(
    long Revision,
    uint ResolutionX,
    uint ResolutionY,
    float DisplayWidth,
    float DisplayHeight,
    int DisplayMirror,
    uint LayerCount,
    float LayerHeight)
{
    public static VoxelPreviewSourceSnapshot Capture(FileFormat sourceFile)
    {
        return new VoxelPreviewSourceSnapshot(
            sourceFile.ModelGeometryRevision,
            sourceFile.ResolutionX,
            sourceFile.ResolutionY,
            sourceFile.DisplayWidth,
            sourceFile.DisplayHeight,
            (int)sourceFile.DisplayMirror,
            sourceFile.LayerCount,
            sourceFile.LayerHeight);
    }

    public bool Matches(FileFormat sourceFile)
    {
        return Revision == sourceFile.ModelGeometryRevision &&
               ResolutionX == sourceFile.ResolutionX &&
               ResolutionY == sourceFile.ResolutionY &&
               DisplayWidth.Equals(sourceFile.DisplayWidth) &&
               DisplayHeight.Equals(sourceFile.DisplayHeight) &&
               DisplayMirror == (int)sourceFile.DisplayMirror &&
               LayerCount == sourceFile.LayerCount &&
               LayerHeight.Equals(sourceFile.LayerHeight);
    }
}

public sealed class VoxelPreviewMesh : IDisposable
{
    private readonly VoxelPreviewSourceSnapshot _source;
    private uint[]? _indices;
    private VoxelPreviewVertex[]? _vertices;

    internal VoxelPreviewMesh(
        VoxelPreviewVertex[] vertices,
        int vertexCount,
        uint[] indices,
        int indexCount,
        Vector3 minimumBounds,
        Vector3 maximumBounds,
        int samplingStride,
        VoxelPreviewSourceSnapshot source,
        VoxelPreviewQuality quality)
    {
        _vertices = vertices;
        VertexCount = vertexCount;
        _indices = indices;
        IndexCount = indexCount;
        MinimumBounds = vertexCount == 0 ? Vector3.Zero : minimumBounds;
        MaximumBounds = vertexCount == 0 ? Vector3.Zero : maximumBounds;
        SamplingStride = samplingStride;
        SourceRevision = source.Revision;
        Quality = quality;
        _source = source;
    }

    public int VertexCount { get; }
    public int IndexCount { get; }
    public uint TriangleCount => (uint)(IndexCount / 3);
    public Vector3 MinimumBounds { get; }
    public Vector3 MaximumBounds { get; }
    public Vector3 Center => (MinimumBounds + MaximumBounds) / 2;
    public Vector3 Size => MaximumBounds - MinimumBounds;
    public int SamplingStride { get; }
    public long SourceRevision { get; }
    public VoxelPreviewQuality Quality { get; }
    public TimeSpan BuildDuration { get; internal set; }

    public ReadOnlySpan<VoxelPreviewVertex> Vertices => _vertices.AsSpan(0, VertexCount);
    public ReadOnlySpan<uint> Indices => _indices.AsSpan(0, IndexCount);

    public void Dispose()
    {
        var vertices = _vertices;
        _vertices = null;
        if (vertices is not null)
        {
            ArrayPool<VoxelPreviewVertex>.Shared.Return(vertices);
        }

        var indices = _indices;
        _indices = null;
        if (indices is not null)
        {
            ArrayPool<uint>.Shared.Return(indices);
        }
    }

    public bool IsCurrentFor(FileFormat sourceFile)
    {
        ArgumentNullException.ThrowIfNull(sourceFile);
        return _source.Matches(sourceFile);
    }
}

internal sealed class PooledBuffer<T> : IDisposable
{
    private T[]? _buffer;

    public PooledBuffer(int initialCapacity)
    {
        _buffer = ArrayPool<T>.Shared.Rent(Math.Max(initialCapacity, 16));
    }

    public int Count { get; private set; }

    public void Dispose()
    {
        var buffer = _buffer;
        _buffer = null;
        Count = 0;
        if (buffer is not null)
        {
            ArrayPool<T>.Shared.Return(buffer, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
    }

    public void Add(T item)
    {
        EnsureCapacity(Count + 1);
        _buffer![Count++] = item;
    }

    public T[] Detach(out int count)
    {
        var buffer = _buffer ?? throw new ObjectDisposedException(nameof(PooledBuffer<T>));
        count = Count;
        _buffer = null;
        Count = 0;
        return buffer;
    }

    private void EnsureCapacity(int requiredCapacity)
    {
        var buffer = _buffer ?? throw new ObjectDisposedException(nameof(PooledBuffer<T>));
        if (requiredCapacity <= buffer.Length) return;

        var newBuffer = ArrayPool<T>.Shared.Rent(Math.Max(requiredCapacity, buffer.Length * 2));
        buffer.AsSpan(0, Count).CopyTo(newBuffer);
        ArrayPool<T>.Shared.Return(buffer, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        _buffer = newBuffer;
    }
}