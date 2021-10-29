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
        const int VertexCacheSize = 3000;
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

        #region Members
        Dictionary<Vector3, uint> VertexCache = new Dictionary<Vector3, uint>(VertexCacheSize);
        StreamWriter triangleStream = null;
        #endregion

        #region Methods
        public override void BeginWrite()
        {
            /* Create a stream to store the triangles (faces) as they come through */
            triangleStream = File.CreateText(FilePath + ".tris");
            MeshStream.WriteLine($"# {HeaderComment}");
            MeshStream.WriteLine($"o {ObjectName}");
            MeshStream.WriteLine();
            MeshStream.WriteLine("# List of geometric vertices, with (x, y, z [,w]) coordinates, w is optional and defaults to 1.0");
        }

        public override void WriteTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal)
        {
            if (!VertexCache.ContainsKey(p1))
            {
                MeshStream.WriteLine($"v {p1.X:F6} {p1.Y:F6} {p1.Z:F6}");
                VertexCount++;
                VertexCache.Add(p1, VertexCount);
            }
            if (!VertexCache.ContainsKey(p2))
            {
                MeshStream.WriteLine($"v {p2.X:F6} {p2.Y:F6} {p2.Z:F6}");
                VertexCount++;
                VertexCache.Add(p2, VertexCount);
            }
            if (!VertexCache.ContainsKey(p3))
            {
                MeshStream.WriteLine($"v {p3.X:F6} {p3.Y:F6} {p3.Z:F6}");
                VertexCount++;
                VertexCache.Add(p3, VertexCount);
            }

            triangleStream.WriteLine($"f {VertexCache[p1]} {VertexCache[p2]} {VertexCache[p3]}");
            
            /* If we are getting close to the cache size. we do *not* want to go over the capacity as that will trigger
             * an allocation of a bigger buffer and copy of the kvp's */
            if (VertexCache.Count >= VertexCacheSize - 10)
            {
                VertexCache.Clear();
            }
        }

        public override void EndWrite()
        {
            VertexCache.Clear();
            MeshStream.WriteLine();
            MeshStream.WriteLine("# Polygonal face elements");

            /* Make sure all triangles are flushed to disk */
            triangleStream.Flush();
            triangleStream.Close();

            /* copy the triangles to the main OBJ mesh file */
            var fileReader = File.OpenRead(FilePath + ".tris");
            fileReader.CopyTo(MeshStream);
            fileReader.Close();
            fileReader.Dispose();
            File.Delete(FilePath + ".tris");

            MeshStream.WriteLine($"# Triangles: {TriangleCount}");
        }

        #endregion
    }
}
