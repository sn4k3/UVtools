/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.MeshFormats;

public class PLYMeshFile : MeshFile
{
    #region Constants
    private const int AsciiVertexLineBufferSize = 160;
    private const int AsciiFaceLineBufferSize = 36;
    private const int BinaryVertexRecordSize = sizeof(float) * 3;
    private const int BinaryFaceRecordSize = sizeof(byte) + sizeof(uint) * 3;
    #endregion

    #region Members
    private static readonly StandardFormat CoordinateFormat = new('F', 6);
    private readonly Dictionary<Vector3, uint> _vertexCache = new(VertexCacheSize);
    private FileStream _triangleStream = null!;
    private long _vertexCountWritePosition;
    private long _faceCountWritePosition;
    #endregion

    #region Properties
    public static FileExtension FileExtension => new(typeof(PLYMeshFile), "ply", "Polygon File Format");
    #endregion

    #region Constructor
    public PLYMeshFile(string filePath, FileMode fileMode, MeshFileFormat fileFormat = MeshFileFormat.BINARY, FileFormat? slicerFile = null) : base(filePath, fileMode, fileFormat, slicerFile) { }
    #endregion
        
    #region Methods
    public override void BeginWrite()
    {
        /* Create a stream to store the triangles (faces) as they come through */
        _triangleStream = new FileStream(PathExtensions.GetTemporaryFilePathWithExtension("trig", $"{About.Software}_"), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.DeleteOnClose);

        MeshStream.WriteLineLF("ply");
        MeshStream.WriteLineLF(FileFormat == MeshFileFormat.ASCII
            ? "format ascii 1.0"
            : "format binary_little_endian 1.0");
        MeshStream.WriteLineLF($"comment {HeaderComment}");
        MeshStream.WriteString("element vertex 0000000000");
        _vertexCountWritePosition = MeshStream.Position;
        MeshStream.WriteLineLF();
        MeshStream.WriteLineLF("property float x");
        MeshStream.WriteLineLF("property float y");
        MeshStream.WriteLineLF("property float z");
        MeshStream.WriteString("element face 0000000000");
        _faceCountWritePosition = MeshStream.Position;
        MeshStream.WriteLineLF();
        MeshStream.WriteLineLF("property list uint8 uint32 vertex_index");
        MeshStream.WriteLineLF("end_header");
    }

    public override void WriteTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal)
    {
        var vertex1 = GetOrWriteVertex(p1);
        var vertex2 = GetOrWriteVertex(p2);
        var vertex3 = GetOrWriteVertex(p3);
        WriteFace(vertex1, vertex2, vertex3);

        TriangleCount++;
            
        /* If we are getting close to the cache size. we do *not* want to go over the capacity as that will trigger
         * an allocation of a bigger buffer and copy of the kvp's */
        if (_vertexCache.Count >= VertexCacheSize - 10)
        {
            _vertexCache.Clear();
        }
    }

    public override void EndWrite()
    {
        _vertexCache.Clear();

        _triangleStream.Seek(0, SeekOrigin.Begin);
        _triangleStream.CopyTo(MeshStream);
        _triangleStream.Dispose();

        MeshStream.Seek(_vertexCountWritePosition - VertexCount.DigitCount(), SeekOrigin.Begin);
        MeshStream.WriteString(VertexCount.ToString());
        MeshStream.Seek(_faceCountWritePosition - TriangleCount.DigitCount(), SeekOrigin.Begin);
        MeshStream.WriteString(TriangleCount.ToString());
        MeshStream.Seek(0, SeekOrigin.End);
    }

    private uint GetOrWriteVertex(Vector3 vertex)
    {
        ref var index = ref CollectionsMarshal.GetValueRefOrAddDefault(_vertexCache, vertex, out var exists);
        if (exists)
        {
            return index;
        }

        index = VertexCount++;
        if (FileFormat == MeshFileFormat.ASCII)
        {
            Span<byte> line = stackalloc byte[AsciiVertexLineBufferSize];
            var writer = new MeshTextWriter(line);
            writer.Append(vertex.X, CoordinateFormat);
            writer.Append((byte)' ');
            writer.Append(vertex.Y, CoordinateFormat);
            writer.Append((byte)' ');
            writer.Append(vertex.Z, CoordinateFormat);
            writer.Append((byte)'\n');
            writer.CopyTo(MeshStream);
        }
        else
        {
            Span<byte> record = stackalloc byte[BinaryVertexRecordSize];
            BinaryPrimitives.WriteSingleLittleEndian(record, vertex.X);
            BinaryPrimitives.WriteSingleLittleEndian(record[sizeof(float)..], vertex.Y);
            BinaryPrimitives.WriteSingleLittleEndian(record[(sizeof(float) * 2)..], vertex.Z);
            MeshStream.Write(record);
        }

        return index;
    }

    private void WriteFace(uint vertex1, uint vertex2, uint vertex3)
    {
        if (FileFormat == MeshFileFormat.ASCII)
        {
            Span<byte> line = stackalloc byte[AsciiFaceLineBufferSize];
            var writer = new MeshTextWriter(line);
            writer.Append("3 "u8);
            writer.Append(vertex1);
            writer.Append((byte)' ');
            writer.Append(vertex2);
            writer.Append((byte)' ');
            writer.Append(vertex3);
            writer.Append((byte)'\n');
            writer.CopyTo(_triangleStream);
        }
        else
        {
            Span<byte> record = stackalloc byte[BinaryFaceRecordSize];
            record[0] = 3;
            BinaryPrimitives.WriteUInt32LittleEndian(record[1..], vertex1);
            BinaryPrimitives.WriteUInt32LittleEndian(record[(1 + sizeof(uint))..], vertex2);
            BinaryPrimitives.WriteUInt32LittleEndian(record[(1 + sizeof(uint) * 2)..], vertex3);
            _triangleStream.Write(record);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _triangleStream?.Dispose();
        }

        base.Dispose(disposing);
    }

    #endregion
}
