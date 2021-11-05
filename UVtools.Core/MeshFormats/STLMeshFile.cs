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
        public STLMeshFile(string filePath, FileMode fileMode, MeshFileFormat fileFormat = MeshFileFormat.BINARY, FileFormat slicerFile = null) : base(filePath, fileMode, fileFormat, slicerFile)
        { }

        
        #endregion

        #region Methods
        public override void BeginWrite()
        {
            if (FileFormat == MeshFileFormat.ASCII)
            {
                MeshStream.WriteLineLF($"solid \"{ObjectName}\"");
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
                MeshStream.WriteLineLF($"  facet normal {normal.X} {normal.Y} {normal.Z}");
                MeshStream.WriteLineLF("    outer loop");
                MeshStream.WriteLineLF($"      vertex {p1.X:E11} {p1.Y:E11} {p1.Z:E11}");
                MeshStream.WriteLineLF($"      vertex {p2.X:E11} {p2.Y:E11} {p2.Z:E11}");
                MeshStream.WriteLineLF($"      vertex {p3.X:E11} {p3.Y:E11} {p3.Z:E11}");
                MeshStream.WriteLineLF("    endloop");
                MeshStream.WriteLineLF("  endfacet");
            } 
            else
            {
                MeshStream.WriteFloatLittleEndian(normal.X);
                MeshStream.WriteFloatLittleEndian(normal.Y);
                MeshStream.WriteFloatLittleEndian(normal.Z);

                MeshStream.WriteFloatLittleEndian(p1.X);
                MeshStream.WriteFloatLittleEndian(p1.Y);
                MeshStream.WriteFloatLittleEndian(p1.Z);

                MeshStream.WriteFloatLittleEndian(p2.X);
                MeshStream.WriteFloatLittleEndian(p2.Y);
                MeshStream.WriteFloatLittleEndian(p2.Z);

                MeshStream.WriteFloatLittleEndian(p3.X);
                MeshStream.WriteFloatLittleEndian(p3.Y);
                MeshStream.WriteFloatLittleEndian(p3.Z);
                
                MeshStream.Write(new byte[2]);
            }

            TriangleCount++;
            VertexCount += 3;
        }

        public override void EndWrite()
        {
            if (FileFormat == MeshFileFormat.ASCII)
            {
                MeshStream.WriteLineLF($"endsolid \"{ObjectName}\"");
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
