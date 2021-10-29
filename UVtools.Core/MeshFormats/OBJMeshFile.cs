/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.MeshFormats
{
    public class OBJMeshFile : MeshFile
    {
        #region Constants
        public const string DefaultObjectName = "Object.1";
        public const int VertexCacheSize = 84000; // About 1MB
        #endregion

        #region Members
        private readonly Dictionary<Vector3, uint> _vertexCache = new(VertexCacheSize);
        private FileStream _triangleStream;
        #endregion

        #region Properties
        public static FileExtension FileExtension => new(typeof(OBJMeshFile), "obj", "Wavefront");

        public string ObjectName { get; } = DefaultObjectName;
        #endregion

        #region Constructor
        public OBJMeshFile(string filePath, FileMode fileMode, MeshFileFormat fileFormat, string name) : base(filePath, fileMode, MeshFileFormat.ASCII)
        {
            ObjectName = name ?? DefaultObjectName;
        }

        public OBJMeshFile(string filePath, FileMode fileMode, MeshFileFormat fileFormat) : this(filePath, fileMode, MeshFileFormat.ASCII, DefaultObjectName) { }
        
        public OBJMeshFile(string filePath, FileMode fileMode) : this(filePath, fileMode, MeshFileFormat.ASCII, DefaultObjectName) { }


        #endregion
        
        #region Methods
        public override void BeginWrite()
        {
            /* Create a stream to store the triangles (faces) as they come through */
            _triangleStream = new FileStream(PathExtensions.GetTempFilePathWithExtension("trig", $"{About.Software}_"), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.DeleteOnClose);
            
            MeshStream.WriteLine($"# {HeaderComment}");
            MeshStream.WriteLine($"o {ObjectName}");
            MeshStream.WriteLine();
            MeshStream.WriteLine("# List of geometric vertices, with (x, y, z [,w]) coordinates, w is optional and defaults to 1.0");
        }

        public override void WriteTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal)
        {
            if (!_vertexCache.ContainsKey(p1))
            {
                MeshStream.WriteLine($"v {p1.X:F6} {p1.Y:F6} {p1.Z:F6}");
                VertexCount++;
                _vertexCache.Add(p1, VertexCount);
            }

            if (!_vertexCache.ContainsKey(p2))
            {
                MeshStream.WriteLine($"v {p2.X:F6} {p2.Y:F6} {p2.Z:F6}");
                VertexCount++;
                _vertexCache.Add(p2, VertexCount);
            }

            if (!_vertexCache.ContainsKey(p3))
            {
                MeshStream.WriteLine($"v {p3.X:F6} {p3.Y:F6} {p3.Z:F6}");
                VertexCount++;
                _vertexCache.Add(p3, VertexCount);
            }

            _triangleStream.WriteLine($"f {_vertexCache[p1]} {_vertexCache[p2]} {_vertexCache[p3]}");

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

            MeshStream.WriteLine();
            MeshStream.WriteLine("# Polygonal face elements");

            _triangleStream.Seek(0, SeekOrigin.Begin);
            _triangleStream.CopyTo(MeshStream);
            _triangleStream.Dispose();

            MeshStream.WriteLine();
            MeshStream.WriteLine($"# Triangles: {TriangleCount}");
            MeshStream.WriteLine($"# Unique Vertexes: {VertexCount}");
        }

        #endregion
    }
}
