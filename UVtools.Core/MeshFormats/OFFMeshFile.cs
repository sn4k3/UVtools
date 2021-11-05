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

namespace UVtools.Core.MeshFormats
{
    public class OFFMeshFile : MeshFile
    {
        #region Members
        private readonly Dictionary<Vector3, uint> _vertexCache = new(VertexCacheSize);
        private FileStream _triangleStream;
        private long _vertexCountWritePosition;
        #endregion

        #region Properties
        public static FileExtension FileExtension => new(typeof(OFFMeshFile), "off", "Object File Format");
        #endregion

        #region Constructor
        public OFFMeshFile(string filePath, FileMode fileMode, MeshFileFormat fileFormat = MeshFileFormat.ASCII, FileFormat slicerFile = null) : base(filePath, fileMode, MeshFileFormat.ASCII, slicerFile) { }
        #endregion
        
        #region Methods
        public override void BeginWrite()
        {
            /* Create a stream to store the triangles (faces) as they come through */
            _triangleStream = new FileStream(PathExtensions.GetTempFilePathWithExtension("trig", $"{About.Software}_"), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.DeleteOnClose);
            
            MeshStream.WriteLineLF("OFF");
            MeshStream.WriteLineLF($"# {HeaderComment}");
            //MeshStream.WriteLineLF("# vertex_count face_count edge_count");
            _vertexCountWritePosition = MeshStream.Position;
            MeshStream.WriteLineLF("0000000000 0000000000 0");
        }

        public override void WriteTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal)
        {
            if (!_vertexCache.ContainsKey(p1))
            {
                MeshStream.WriteLineLF($"{p1.X:F6} {p1.Y:F6} {p1.Z:F6}");
                _vertexCache.Add(p1, VertexCount);
                VertexCount++;
            }

            if (!_vertexCache.ContainsKey(p2))
            {
                MeshStream.WriteLineLF($"{p2.X:F6} {p2.Y:F6} {p2.Z:F6}");
                _vertexCache.Add(p2, VertexCount);
                VertexCount++;
            }

            if (!_vertexCache.ContainsKey(p3))
            {
                MeshStream.WriteLineLF($"{p3.X:F6} {p3.Y:F6} {p3.Z:F6}");
                _vertexCache.Add(p3, VertexCount);
                VertexCount++;
            }

            _triangleStream.WriteLineLF($"3 {_vertexCache[p1]} {_vertexCache[p2]} {_vertexCache[p3]}");

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

            //MeshStream.WriteLineLF("# Polygonal face elements");

            _triangleStream.Seek(0, SeekOrigin.Begin);
            _triangleStream.CopyTo(MeshStream);
            _triangleStream.Dispose();

            MeshStream.Seek(_vertexCountWritePosition + 10 - VertexCount.ToString().Length, SeekOrigin.Begin);
            MeshStream.WriteString(VertexCount.ToString());
            MeshStream.Seek(11 - TriangleCount.ToString().Length, SeekOrigin.Current);
            MeshStream.WriteString(TriangleCount.ToString());
            MeshStream.Seek(0, SeekOrigin.End);
        }

        #endregion
    }
}
