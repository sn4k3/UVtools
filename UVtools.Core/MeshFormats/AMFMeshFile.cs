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
using System.IO.Compression;
using System.Numerics;
using System.Runtime.InteropServices;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.MeshFormats;

public class AMFMeshFile : MeshFile
{
    #region Constants
    private const int VertexRecordBufferSize = 512;
    private const int TriangleRecordBufferSize = 256;
    #endregion

    #region Members
    private static readonly StandardFormat CoordinateFormat = new('F', 6);
    private readonly Dictionary<Vector3, uint> _vertexCache = new(VertexCacheSize);
    private FileStream _triangleStream = null!;
    #endregion

    #region Properties
    public static FileExtension FileExtension => new(typeof(AMFMeshFile), "amf", "Additive Manufacturing Format");
    #endregion

    #region Constructor
    public AMFMeshFile(string filePath, FileMode fileMode, MeshFileFormat fileFormat = MeshFileFormat.ASCII, FileFormat? slicerFile = null) : base(filePath, fileMode, MeshFileFormat.ASCII, slicerFile) { }
        


    #endregion
        
    #region Methods
    public override void BeginWrite()
    {
        /* Create a stream to store the triangles (faces) as they come through */
        _triangleStream = new FileStream(PathExtensions.GetTemporaryFilePathWithExtension("trig", $"{About.Software}_"), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.DeleteOnClose);
            
        MeshStream.WriteLineLF("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        MeshStream.WriteLineLF("<amf unit=\"millimeter\" version=\"1.1\">");
        MeshStream.WriteLineLF($"\t<metadata type=\"name\">{FilenameWithoutExtension}</metadata>");
        MeshStream.WriteLineLF($"\t<metadata type=\"author\">{HeaderComment}</metadata>");
        MeshStream.WriteLineLF("\t<object id=\"0\">");
        MeshStream.WriteLineLF("\t\t<mesh>");
        MeshStream.WriteLineLF("\t\t\t<vertices>");
    }

    public override void WriteTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal)
    {
        var vertex1 = GetOrWriteVertex(p1);
        var vertex2 = GetOrWriteVertex(p2);
        var vertex3 = GetOrWriteVertex(p3);
        WriteTriangle(vertex1, vertex2, vertex3);

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

        MeshStream.WriteLineLF("\t\t\t</vertices>");
        MeshStream.WriteLineLF("\t\t\t<volume>");
        MeshStream.WriteLineLF("\t\t\t\t<metadata type=\"name\">Model</metadata>");

        _triangleStream.Seek(0, SeekOrigin.Begin);
        _triangleStream.CopyTo(MeshStream);
        _triangleStream.Dispose();

        MeshStream.WriteLineLF("\t\t\t</volume>");
        MeshStream.WriteLineLF("\t\t</mesh>");
        MeshStream.WriteLineLF("\t</object>");
        MeshStream.WriteLineLF("</amf>");

        var tmpFile = PathExtensions.GetTemporaryFilePathWithExtension("tmp", $"{About.Software}_");
        if (File.Exists(tmpFile)) File.Delete(tmpFile);
        using (var zip = ZipFile.Open(tmpFile, ZipArchiveMode.Create))
        {
            MeshStream.Seek(0, SeekOrigin.Begin);
            zip.CreateEntryFromContent(Filename, MeshStream, ZipArchiveMode.Create);
        }
        MeshStream.Dispose();
            
        File.Move(tmpFile, FilePath, true);
    }

    private uint GetOrWriteVertex(Vector3 vertex)
    {
        ref var index = ref CollectionsMarshal.GetValueRefOrAddDefault(_vertexCache, vertex, out var exists);
        if (exists)
        {
            return index;
        }

        index = VertexCount++;

        Span<byte> record = stackalloc byte[VertexRecordBufferSize];
        var writer = new MeshTextWriter(record);
        writer.Append("\t\t\t\t<vertex>\n\t\t\t\t\t<coordinates>\n\t\t\t\t\t\t<x>"u8);
        writer.Append(vertex.X, CoordinateFormat);
        writer.Append("</x>\n\t\t\t\t\t\t<y>"u8);
        writer.Append(vertex.Y, CoordinateFormat);
        writer.Append("</y>\n\t\t\t\t\t\t<z>"u8);
        writer.Append(vertex.Z, CoordinateFormat);
        writer.Append("</z>\n\t\t\t\t\t</coordinates>\n\t\t\t\t</vertex>\n"u8);
        writer.CopyTo(MeshStream);

        return index;
    }

    private void WriteTriangle(uint vertex1, uint vertex2, uint vertex3)
    {
        Span<byte> record = stackalloc byte[TriangleRecordBufferSize];
        var writer = new MeshTextWriter(record);
        writer.Append("\t\t\t\t<triangle>\n\t\t\t\t\t<v1>"u8);
        writer.Append(vertex1);
        writer.Append("</v1>\n\t\t\t\t\t<v2>"u8);
        writer.Append(vertex2);
        writer.Append("</v2>\n\t\t\t\t\t<v3>"u8);
        writer.Append(vertex3);
        writer.Append("</v3>\n\t\t\t\t</triangle>\n"u8);
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
