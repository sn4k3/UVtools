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
    public class STLMeshFile : MeshFile
    {
        #region Constants
        public const string DefaultObjectName = "UVTools STL Object";
        #endregion

        #region Properties
        public static FileExtension FileExtension => new(typeof(STLMeshFile), "stl", "Standard Triangle Language");

        public string ObjectName { get; } = DefaultObjectName;
        #endregion

        #region Constructor
        public STLMeshFile(string filePath, FileMode fileMode, MeshFileFormat fileFormat, string name) : base(filePath, fileMode, fileFormat)
        {
            ObjectName = name ?? DefaultObjectName;
        }

        public STLMeshFile(string filePath, FileMode fileMode, MeshFileFormat fileFormat) : this(filePath, fileMode, fileFormat, DefaultObjectName) { }
        
        public STLMeshFile(string filePath, FileMode fileMode) : this(filePath, fileMode, MeshFileFormat.BINARY, DefaultObjectName) { }

        
        #endregion

        #region Methods
        public override void BeginWrite()
        {
            if (FileFormat == MeshFileFormat.ASCII)
            {
                MeshStream.WriteLine($"solid \"{ObjectName}\"");
            }
            else
            {
                var header = new byte[80];
                var headerText = Encoding.UTF8.GetBytes(HeaderComment);
                Array.Copy(headerText, header, headerText.Length);
                MeshStream.Write(header);
                MeshStream.Seek(4, SeekOrigin.Current);
            }
        }

        public override void WriteTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal)
        {
            if (FileFormat == MeshFileFormat.ASCII)
            {
                MeshStream.WriteLine($"  facet normal {normal.X} {normal.Y} {normal.Z}");
                MeshStream.WriteLine("    outer loop");
                MeshStream.WriteLine($"      vertex {p1.X:E11} {p1.Y:E11} {p1.Z:E11}");
                MeshStream.WriteLine($"      vertex {p2.X:E11} {p2.Y:E11} {p2.Z:E11}");
                MeshStream.WriteLine($"      vertex {p3.X:E11} {p3.Y:E11} {p3.Z:E11}");
                MeshStream.WriteLine("    endloop");
                MeshStream.WriteLine("  endfacet");
            } 
            else
            {
                MeshStream.Write(BitConverter.GetBytes(normal.X));
                MeshStream.Write(BitConverter.GetBytes(normal.Y));
                MeshStream.Write(BitConverter.GetBytes(normal.Z));

                MeshStream.Write(BitConverter.GetBytes(p1.X));
                MeshStream.Write(BitConverter.GetBytes(p1.Y));
                MeshStream.Write(BitConverter.GetBytes(p1.Z));

                MeshStream.Write(BitConverter.GetBytes(p2.X));
                MeshStream.Write(BitConverter.GetBytes(p2.Y));
                MeshStream.Write(BitConverter.GetBytes(p2.Z));

                MeshStream.Write(BitConverter.GetBytes(p3.X));
                MeshStream.Write(BitConverter.GetBytes(p3.Y));
                MeshStream.Write(BitConverter.GetBytes(p3.Z));
                
                MeshStream.Write(new byte[2]);
            }

            TriangleCount++;
            VertexCount += 3;
        }

        public override void EndWrite()
        {
            if (FileFormat == MeshFileFormat.ASCII)
            {
                MeshStream.WriteLine($"endsolid \"{ObjectName}\"");
            } 
            else
            {
                MeshStream.Seek(80, SeekOrigin.Begin);
                MeshStream.WriteUIntLittleEndian(TriangleCount);
            }
            MeshStream.Flush();
        }

        #endregion
    }
}
