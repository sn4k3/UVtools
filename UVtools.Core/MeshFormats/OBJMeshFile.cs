/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
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

        public OBJMeshFile(string filePath, FileMode fileMode, MeshFileFormat fileFormat) : this(filePath, fileMode, fileFormat, DefaultObjectName) { }
        
        public OBJMeshFile(string filePath, FileMode fileMode) : this(filePath, fileMode, MeshFileFormat.ASCII, DefaultObjectName) { }

        
        #endregion

        #region Methods
        public override void BeginWrite()
        {
            MeshStream.WriteLine($"# {HeaderComment}");
            MeshStream.WriteLine($"o {ObjectName}");
            MeshStream.WriteLine();
            MeshStream.WriteLine("# List of geometric vertices, with (x, y, z [,w]) coordinates, w is optional and defaults to 1.0");
        }

        public override void WriteTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal)
        {
            MeshStream.WriteLine($"v {p1.X:F6} {p1.Y:F6} {p1.Z:F6}");
            MeshStream.WriteLine($"v {p2.X:F6} {p2.Y:F6} {p2.Z:F6}");
            MeshStream.WriteLine($"v {p3.X:F6} {p3.Y:F6} {p3.Z:F6}");

            TriangleCount++;
        }

        public override void EndWrite()
        {
            MeshStream.WriteLine();
            MeshStream.WriteLine("# Polygonal face elements");

            uint count = TriangleCount * 3;
            for (var i = 1; i <= count;)
            {
                MeshStream.WriteLine($"f {i++} {i++} {i++}");
            }

            MeshStream.WriteLine();
            MeshStream.WriteLine($"# Triangles: {TriangleCount}");
        }

        #endregion
    }
}
