/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.MeshFormats;

public class WRLMeshFile : MeshFile
{
    #region Constants
    private const int VertexBufferSize = 160;
    private const int FaceBufferSize = 44;
    #endregion

    #region Members
    private static readonly StandardFormat CoordinateFormat = new('F', 6);
    private readonly Dictionary<Vector3, uint> _vertexCache = new(VertexCacheSize);
    private FileStream _triangleStream = null!;
    #endregion

    #region Properties
    public static FileExtension FileExtension => new(typeof(WRLMeshFile), "wrl", "Virtual Reality Modeling Language");
    #endregion

    #region Constructor
    public WRLMeshFile(string filePath, FileMode fileMode, MeshFileFormat fileFormat = MeshFileFormat.ASCII, FileFormat? slicerFile = null) : base(filePath, fileMode, MeshFileFormat.ASCII, slicerFile) { }
    #endregion
        
    #region Methods
    public override void BeginWrite()
    {
        /* Create a stream to store the triangles (faces) as they come through */
        _triangleStream = new FileStream(PathExtensions.GetTemporaryFilePathWithExtension("trig", $"{About.Software}_"), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.DeleteOnClose);
            
        MeshStream.WriteLineLF("#VRML V2.0 utf8");
        MeshStream.WriteLineLF($"WorldInfo {{ info \"{HeaderComment}\" }}");
        MeshStream.WriteLineLF("Shape {");
        MeshStream.WriteLineLF("\tgeometry IndexedFaceSet {");
        MeshStream.WriteLineLF("\t\tcoord Coordinate {");
        MeshStream.WriteString("\t\t\tpoint [");
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

        MeshStream.WriteLineLF(" ]");
        MeshStream.WriteLineLF("\t\t}");
        MeshStream.WriteString("\t\tcoordIndex [");
            
            
        _triangleStream.Seek(0, SeekOrigin.Begin);
        _triangleStream.CopyTo(MeshStream);
        _triangleStream.Dispose();

        MeshStream.WriteLineLF(" ]");

        MeshStream.WriteLineLF("\t}");
        MeshStream.WriteLineLF("}");
    }

    private uint GetOrWriteVertex(Vector3 vertex)
    {
        ref var index = ref CollectionsMarshal.GetValueRefOrAddDefault(_vertexCache, vertex, out var exists);
        if (exists)
        {
            return index;
        }

        index = VertexCount++;

        Span<byte> record = stackalloc byte[VertexBufferSize];
        var writer = new MeshTextWriter(record);
        writer.Append((byte)' ');
        writer.Append(vertex.X, CoordinateFormat);
        writer.Append((byte)' ');
        writer.Append(vertex.Y, CoordinateFormat);
        writer.Append((byte)' ');
        writer.Append(vertex.Z, CoordinateFormat);
        writer.CopyTo(MeshStream);

        return index;
    }

    private void WriteFace(uint vertex1, uint vertex2, uint vertex3)
    {
        Span<byte> record = stackalloc byte[FaceBufferSize];
        var writer = new MeshTextWriter(record);
        writer.Append((byte)' ');
        writer.Append(vertex1);
        writer.Append((byte)' ');
        writer.Append(vertex2);
        writer.Append((byte)' ');
        writer.Append(vertex3);
        writer.Append(" -1"u8);
        writer.CopyTo(_triangleStream);
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
