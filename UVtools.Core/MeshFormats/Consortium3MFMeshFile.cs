/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.MeshFormats;

public class Consortium3MFMeshFile : MeshFile
{
    #region Members
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
        _triangleStream = new FileStream(PathExtensions.GetTempFilePathWithExtension("trig", $"{About.Software}_"), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.DeleteOnClose);
            
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
        if (!_vertexCache.ContainsKey(p1))
        {
            MeshStream.WriteLineLF($"\t\t\t\t\t<vertex x=\"{p1.X:F6}\" y=\"{p1.Y:F6}\" z=\"{p1.Z:F6}\" />");
            _vertexCache.Add(p1, VertexCount);
            VertexCount++;
        }

        if (!_vertexCache.ContainsKey(p2))
        {
            MeshStream.WriteLineLF($"\t\t\t\t\t<vertex x=\"{p2.X:F6}\" y=\"{p2.Y:F6}\" z=\"{p2.Z:F6}\" />");
            _vertexCache.Add(p2, VertexCount);
            VertexCount++;
        }

        if (!_vertexCache.ContainsKey(p3))
        {
            MeshStream.WriteLineLF($"\t\t\t\t\t<vertex x=\"{p3.X:F6}\" y=\"{p3.Y:F6}\" z=\"{p3.Z:F6}\" />");
            _vertexCache.Add(p3, VertexCount);
            VertexCount++;
        }

        _triangleStream.WriteLineLF($"\t\t\t\t\t<triangle v1=\"{_vertexCache[p1]}\" v2=\"{_vertexCache[p2]}\" v3=\"{_vertexCache[p3]}\" />");

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

        var tmpFile = PathExtensions.GetTempFilePathWithExtension("tmp", $"{About.Software}_");
        if (File.Exists(tmpFile)) File.Delete(tmpFile);
        bool haveThumbnail = SlicerFile is not null && SlicerFile.CreatedThumbnailsCount > 0;
        using (var zip = ZipFile.Open(tmpFile, ZipArchiveMode.Create))
        {
            zip.PutFileContent("[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                                      "\n<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                                                      //"\n\t<Default Extension=\"jpeg\" ContentType=\"image/jpeg\" />" +
                                                      //"\n\t<Default Extension=\"jpg\" ContentType=\"image/jpeg\" />" +
                                                      "\n\t<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" />" +
                                                      "\n\t<Default Extension=\"model\" ContentType=\"application/vnd.ms-package.3dmanufacturing-3dmodel+xml\" />" +
                                                      "\n\t<Default Extension=\"png\" ContentType=\"image/png\" />" +
                                                      //"\n\t<Default Extension=\"texture\" ContentType=\"application/vnd.ms-package.3dmanufacturing-3dmodeltexture\" />" +
                                                      "\n</Types>\n", ZipArchiveMode.Create);
            zip.PutFileContent("_rels/.rels", "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                              "\n<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                                              "\n\t<Relationship Target=\"/3D/3dmodel.model\" Id=\"rel0\" Type=\"http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel\" />" +
                                              (haveThumbnail ? "\n\t<Relationship Target=\"/Metadata/thumbnail.png\" Id=\"rel1\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail\" />" : string.Empty) +
                                              "\n</Relationships>\r\n", ZipArchiveMode.Create);
                
            MeshStream.Seek(0, SeekOrigin.Begin);
            zip.PutFileContent("3D/3dmodel.model", MeshStream, ZipArchiveMode.Create);
                
            if (haveThumbnail)
            {
                var mat = SlicerFile!.GetThumbnail(true);
                zip.PutFileContent("Metadata/thumbnail.png", mat!.GetPngByes(), ZipArchiveMode.Create);
            }
        }
        MeshStream.Dispose();
            
        File.Move(tmpFile, FilePath, true);
    }

    #endregion
}