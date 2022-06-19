/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Collections.Generic;
using System.IO;
using System.Numerics;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.MeshFormats;

public class PLYMeshFile : MeshFile
{
    #region Members
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
        if (!_vertexCache.ContainsKey(p1))
        {
            if (FileFormat == MeshFileFormat.ASCII)
            {
                MeshStream.WriteLineLF($"{p1.X:F6} {p1.Y:F6} {p1.Z:F6}");
            }
            else
            {
                MeshStream.WriteFloatLittleEndian(p1.X);
                MeshStream.WriteFloatLittleEndian(p1.Y);
                MeshStream.WriteFloatLittleEndian(p1.Z);
            }
                
            _vertexCache.Add(p1, VertexCount);
            VertexCount++;
        }

        if (!_vertexCache.ContainsKey(p2))
        {
            if (FileFormat == MeshFileFormat.ASCII)
            {
                MeshStream.WriteLineLF($"{p2.X:F6} {p2.Y:F6} {p2.Z:F6}");
            }
            else
            {
                MeshStream.WriteFloatLittleEndian(p2.X);
                MeshStream.WriteFloatLittleEndian(p2.Y);
                MeshStream.WriteFloatLittleEndian(p2.Z);
            }

            _vertexCache.Add(p2, VertexCount);
            VertexCount++;
        }

        if (!_vertexCache.ContainsKey(p3))
        {
            if (FileFormat == MeshFileFormat.ASCII)
            {
                MeshStream.WriteLineLF($"{p3.X:F6} {p3.Y:F6} {p3.Z:F6}");
            }
            else
            {
                MeshStream.WriteFloatLittleEndian(p3.X);
                MeshStream.WriteFloatLittleEndian(p3.Y);
                MeshStream.WriteFloatLittleEndian(p3.Z);
            }

            _vertexCache.Add(p3, VertexCount);
            VertexCount++;
        }

        if (FileFormat == MeshFileFormat.ASCII)
        {
            _triangleStream.WriteLineLF($"3 {_vertexCache[p1]} {_vertexCache[p2]} {_vertexCache[p3]}");
        }
        else
        {
            _triangleStream.WriteByte(3);
            _triangleStream.WriteUIntLittleEndian(_vertexCache[p1]);
            _triangleStream.WriteUIntLittleEndian(_vertexCache[p2]);
            _triangleStream.WriteUIntLittleEndian(_vertexCache[p3]);
        }

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

        MeshStream.Seek(_vertexCountWritePosition - VertexCount.ToString().Length, SeekOrigin.Begin);
        MeshStream.WriteString(VertexCount.ToString());
        MeshStream.Seek(_faceCountWritePosition - TriangleCount.ToString().Length, SeekOrigin.Begin);
        MeshStream.WriteString(TriangleCount.ToString());
        MeshStream.Seek(0, SeekOrigin.End);
    }

    #endregion
}