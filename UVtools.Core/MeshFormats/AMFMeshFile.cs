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

public class AMFMeshFile : MeshFile
{
    #region Members
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
        if (!_vertexCache.ContainsKey(p1))
        {
            MeshStream.WriteLineLF($"\t\t\t\t<vertex>");
            MeshStream.WriteLineLF($"\t\t\t\t\t<coordinates>");
            MeshStream.WriteLineLF($"\t\t\t\t\t\t<x>{p1.X:F6}</x>");
            MeshStream.WriteLineLF($"\t\t\t\t\t\t<y>{p1.Y:F6}</y>");
            MeshStream.WriteLineLF($"\t\t\t\t\t\t<z>{p1.Z:F6}</z>");
            MeshStream.WriteLineLF($"\t\t\t\t\t</coordinates>");
            MeshStream.WriteLineLF($"\t\t\t\t</vertex>");
            _vertexCache.Add(p1, VertexCount);
            VertexCount++;
        }

        if (!_vertexCache.ContainsKey(p2))
        {
            MeshStream.WriteLineLF($"\t\t\t\t<vertex>");
            MeshStream.WriteLineLF($"\t\t\t\t\t<coordinates>");
            MeshStream.WriteLineLF($"\t\t\t\t\t\t<x>{p2.X:F6}</x>");
            MeshStream.WriteLineLF($"\t\t\t\t\t\t<y>{p2.Y:F6}</y>");
            MeshStream.WriteLineLF($"\t\t\t\t\t\t<z>{p2.Z:F6}</z>");
            MeshStream.WriteLineLF($"\t\t\t\t\t</coordinates>");
            MeshStream.WriteLineLF($"\t\t\t\t</vertex>");
            _vertexCache.Add(p2, VertexCount);
            VertexCount++;
        }

        if (!_vertexCache.ContainsKey(p3))
        {
            MeshStream.WriteLineLF($"\t\t\t\t<vertex>");
            MeshStream.WriteLineLF($"\t\t\t\t\t<coordinates>");
            MeshStream.WriteLineLF($"\t\t\t\t\t\t<x>{p3.X:F6}</x>");
            MeshStream.WriteLineLF($"\t\t\t\t\t\t<y>{p3.Y:F6}</y>");
            MeshStream.WriteLineLF($"\t\t\t\t\t\t<z>{p3.Z:F6}</z>");
            MeshStream.WriteLineLF($"\t\t\t\t\t</coordinates>");
            MeshStream.WriteLineLF($"\t\t\t\t</vertex>");
            _vertexCache.Add(p3, VertexCount);
            VertexCount++;
        }

        _triangleStream.WriteLineLF($"\t\t\t\t<triangle>");
        _triangleStream.WriteLineLF($"\t\t\t\t\t<v1>{_vertexCache[p1]}</v1>");
        _triangleStream.WriteLineLF($"\t\t\t\t\t<v2>{_vertexCache[p2]}</v2>");
        _triangleStream.WriteLineLF($"\t\t\t\t\t<v3>{_vertexCache[p3]}</v3>");
        _triangleStream.WriteLineLF($"\t\t\t\t</triangle>");

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
            zip.PutFileContent(Filename, MeshStream, ZipArchiveMode.Create);
        }
        MeshStream.Dispose();
            
        File.Move(tmpFile, FilePath, true);
    }

    #endregion
}