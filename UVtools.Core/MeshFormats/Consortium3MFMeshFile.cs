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
using EmguExtensions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.MeshFormats;

public class Consortium3MFMeshFile : MeshFile
{
    #region Constants
    private const int VertexRecordBufferSize = 256;
    private const int TriangleRecordBufferSize = 128;
    #endregion

    #region Members
    private static readonly StandardFormat CoordinateFormat = new('F', 6);
    private readonly Dictionary<Vector3, uint> _vertexCache = new(VertexCacheSize);
    private FileStream _triangleStream = null!;
    #endregion

    #region Properties
    public static FileExtension FileExtension => new(typeof(Consortium3MFMeshFile), "3mf", "3D Manufacturing Format");
    #endregion

    #region Constructor
    public Consortium3MFMeshFile(string filePath, FileMode fileMode, MeshFileFormat fileFormat = MeshFileFormat.ASCII, FileFormat? slicerFile = null) : base(filePath, fileMode, MeshFileFormat.ASCII, slicerFile) { }
        


    #endregion
        
    #region Methods
    public override void BeginWrite()
    {
        /* Create a stream to store the triangles (faces) as they come through */
        _triangleStream = new FileStream(PathExtensions.GetTemporaryFilePathWithExtension("trig", $"{About.Software}_"), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.DeleteOnClose);
            
        MeshStream.WriteLineLF("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        MeshStream.WriteLineLF("<model unit=\"millimeter\" xml:lang=\"en-US\" xmlns:m=\"http://schemas.microsoft.com/3dmanufacturing/material/2015/02\" xmlns=\"http://schemas.microsoft.com/3dmanufacturing/core/2015/02\">");
        MeshStream.WriteLineLF("\t<metadata name=\"Copyright\">");
        MeshStream.WriteLineLF($"\t\t{HeaderComment}");
        MeshStream.WriteLineLF("\t</metadata>");
        MeshStream.WriteLineLF("\t<resources>");
        MeshStream.WriteLineLF("\t\t<object id=\"1\" type=\"model\">");
        MeshStream.WriteLineLF("\t\t\t<mesh>");
        MeshStream.WriteLineLF("\t\t\t\t<vertices>");
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

        MeshStream.WriteLineLF("\t\t\t\t</vertices>");
        MeshStream.WriteLineLF("\t\t\t\t<triangles>");

        _triangleStream.Seek(0, SeekOrigin.Begin);
        _triangleStream.CopyTo(MeshStream);
        _triangleStream.Dispose();

        MeshStream.WriteLineLF("\t\t\t\t</triangles>");
        MeshStream.WriteLineLF("\t\t\t</mesh>");
        MeshStream.WriteLineLF("\t\t</object>");
        MeshStream.WriteLineLF("\t</resources>");
        MeshStream.WriteLineLF("\t<build>");
        MeshStream.WriteLineLF("\t\t<item objectid=\"1\" />");
        MeshStream.WriteLineLF("\t</build>");
        MeshStream.WriteLineLF("</model>");

        var tmpFile = PathExtensions.GetTemporaryFilePathWithExtension("tmp", $"{About.Software}_");
        if (File.Exists(tmpFile)) File.Delete(tmpFile);
        bool haveThumbnail = SlicerFile?.HaveThumbnails ?? false;
        using (var zip = ZipFile.Open(tmpFile, ZipArchiveMode.Create))
        {
            zip.CreateEntryFromContent("[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                                      "\n<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                                                      //"\n\t<Default Extension=\"jpeg\" ContentType=\"image/jpeg\" />" +
                                                      //"\n\t<Default Extension=\"jpg\" ContentType=\"image/jpeg\" />" +
                                                      "\n\t<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" />" +
                                                      "\n\t<Default Extension=\"model\" ContentType=\"application/vnd.ms-package.3dmanufacturing-3dmodel+xml\" />" +
                                                      "\n\t<Default Extension=\"png\" ContentType=\"image/png\" />" +
                                                      //"\n\t<Default Extension=\"texture\" ContentType=\"application/vnd.ms-package.3dmanufacturing-3dmodeltexture\" />" +
                                                      "\n</Types>\n", ZipArchiveMode.Create);
            zip.CreateEntryFromContent("_rels/.rels", "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                              "\n<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                                              "\n\t<Relationship Target=\"/3D/3dmodel.model\" Id=\"rel0\" Type=\"http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel\" />" +
                                              (haveThumbnail ? "\n\t<Relationship Target=\"/Metadata/thumbnail.png\" Id=\"rel1\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail\" />" : string.Empty) +
                                              "\n</Relationships>\r\n", ZipArchiveMode.Create);
                
            MeshStream.Seek(0, SeekOrigin.Begin);
            zip.CreateEntryFromContent("3D/3dmodel.model", MeshStream, ZipArchiveMode.Create);
                
            if (haveThumbnail)
            {
                var mat = SlicerFile!.GetLargestThumbnail();
                zip.CreateEntryFromContent("Metadata/thumbnail.png", mat!.GetPngBytes(), ZipArchiveMode.Create);
            }
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
        writer.Append("\t\t\t\t\t<vertex x=\""u8);
        writer.Append(vertex.X, CoordinateFormat);
        writer.Append("\" y=\""u8);
        writer.Append(vertex.Y, CoordinateFormat);
        writer.Append("\" z=\""u8);
        writer.Append(vertex.Z, CoordinateFormat);
        writer.Append("\" />\n"u8);
        writer.CopyTo(MeshStream);

        return index;
    }

    private void WriteTriangle(uint vertex1, uint vertex2, uint vertex3)
    {
        Span<byte> record = stackalloc byte[TriangleRecordBufferSize];
        var writer = new MeshTextWriter(record);
        writer.Append("\t\t\t\t\t<triangle v1=\""u8);
        writer.Append(vertex1);
        writer.Append("\" v2=\""u8);
        writer.Append(vertex2);
        writer.Append("\" v3=\""u8);
        writer.Append(vertex3);
        writer.Append("\" />\n"u8);
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
