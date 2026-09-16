/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2020-2026 PTRTECH
 */

using System;
using System.Buffers;
using System.Collections.Generic;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Voxel;

public readonly record struct VoxelPreviewIssueDrawRange(
    MainIssue.IssueType Type,
    int IndexOffset,
    int IndexCount);

public sealed class VoxelPreviewIssueMesh : IDisposable
{
    private uint[]? _indices;
    private VoxelPreviewVertex[]? _vertices;

    internal VoxelPreviewIssueMesh(
        VoxelPreviewVertex[] vertices,
        int vertexCount,
        uint[] indices,
        int indexCount,
        VoxelPreviewIssueDrawRange[] drawRanges,
        int samplingStride,
        long sourceRevision,
        long issueRevision)
    {
        _vertices = vertices;
        VertexCount = vertexCount;
        _indices = indices;
        IndexCount = indexCount;
        DrawRanges = drawRanges;
        SamplingStride = samplingStride;
        SourceRevision = sourceRevision;
        IssueRevision = issueRevision;
    }

    public int VertexCount { get; }
    public int IndexCount { get; }
    public uint TriangleCount => (uint)(IndexCount / 3);
    public int SamplingStride { get; }
    public long SourceRevision { get; }
    public long IssueRevision { get; }
    public IReadOnlyList<VoxelPreviewIssueDrawRange> DrawRanges { get; }
    public ReadOnlySpan<VoxelPreviewVertex> Vertices => _vertices.AsSpan(0, VertexCount);
    public ReadOnlySpan<uint> Indices => _indices.AsSpan(0, IndexCount);

    public bool IsCurrentFor(FileFormat sourceFile)
    {
        ArgumentNullException.ThrowIfNull(sourceFile);
        return SourceRevision == sourceFile.ModelGeometryRevision &&
               IssueRevision == sourceFile.IssueManager.Revision;
    }

    public void Dispose()
    {
        var vertices = _vertices;
        _vertices = null;
        if (vertices is not null) ArrayPool<VoxelPreviewVertex>.Shared.Return(vertices);

        var indices = _indices;
        _indices = null;
        if (indices is not null) ArrayPool<uint>.Shared.Return(indices);
    }
}