/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using System.Text;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.MeshFormats;

public class STLMeshFile : MeshFile
{
    #region Constants
    public const string DefaultObjectName = "UVTools STL Object";
    private const int AsciiTriangleBufferSize = 512;
    #endregion

    #region Members
    private static readonly StandardFormat ScientificCoordinateFormat = new('E', 11);
    private static readonly StandardFormat GeneralCoordinateFormat = new('G');
    #endregion

    #region Properties
    public static FileExtension FileExtension => new(typeof(STLMeshFile), "stl", "Standard Triangle Language");

    public string ObjectName { get; } = DefaultObjectName;
    #endregion

    #region Constructor
    public STLMeshFile(string filePath, FileMode fileMode, MeshFileFormat fileFormat = MeshFileFormat.BINARY, FileFormat? slicerFile = null) : base(filePath, fileMode, fileFormat, slicerFile)
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
            Span<byte> header = stackalloc byte[80];
            header.Clear();

            Encoding.UTF8.GetBytes(HeaderComment.AsSpan(0, Math.Min(HeaderComment.Length, header.Length)), header);

            MeshStream.Write(header);
            MeshStream.Seek(4, SeekOrigin.Current);
        }
    }

    public override void WriteTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal)
    {
        if (FileFormat == MeshFileFormat.ASCII)
        {
            WriteAsciiTriangle(p1, p2, p3, normal);
        }
        else
        {
            Span<byte> triangle = stackalloc byte[50];
            WriteVector3(triangle, normal);
            WriteVector3(triangle[12..], p1);
            WriteVector3(triangle[24..], p2);
            WriteVector3(triangle[36..], p3);
            triangle[48..].Clear();
            MeshStream.Write(triangle);
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

    private static void WriteVector3(Span<byte> destination, Vector3 vector)
    {
        BinaryPrimitives.WriteSingleLittleEndian(destination, vector.X);
        BinaryPrimitives.WriteSingleLittleEndian(destination[sizeof(float)..], vector.Y);
        BinaryPrimitives.WriteSingleLittleEndian(destination[(sizeof(float) * 2)..], vector.Z);
    }

    private void WriteAsciiTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal)
    {
        Span<byte> record = stackalloc byte[AsciiTriangleBufferSize];
        var writer = new MeshTextWriter(record);
        writer.Append("  facet normal "u8);
        AppendVector3(ref writer, normal, GeneralCoordinateFormat);
        writer.Append("\n    outer loop\n      vertex "u8);
        AppendVector3(ref writer, p1, ScientificCoordinateFormat);
        writer.Append("\n      vertex "u8);
        AppendVector3(ref writer, p2, ScientificCoordinateFormat);
        writer.Append("\n      vertex "u8);
        AppendVector3(ref writer, p3, ScientificCoordinateFormat);
        writer.Append("\n    endloop\n  endfacet\n"u8);
        writer.CopyTo(MeshStream);
    }

    private static void AppendVector3(ref MeshTextWriter writer, Vector3 vector, StandardFormat format)
    {
        writer.Append(vector.X, format);
        writer.Append((byte)' ');
        writer.Append(vector.Y, format);
        writer.Append((byte)' ');
        writer.Append(vector.Z, format);
    }

    #endregion
}
