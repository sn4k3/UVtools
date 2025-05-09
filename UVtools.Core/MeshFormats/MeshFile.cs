/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.IO;
using System.Numerics;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using ZLinq;

namespace UVtools.Core.MeshFormats;

public abstract class MeshFile : IDisposable
{
    #region Constants
    public const int VertexCacheSize = 84000; // About 1MB
    #endregion

    #region Static

    public static readonly FileExtension[] AvailableMeshFiles =
    [
        STLMeshFile.FileExtension,
        Consortium3MFMeshFile.FileExtension,
        AMFMeshFile.FileExtension,
        WRLMeshFile.FileExtension,
        OBJMeshFile.FileExtension,
        PLYMeshFile.FileExtension,
        OFFMeshFile.FileExtension
    ];

    public static string HeaderComment => $"Exported from {About.SoftwareWithVersion} @ {DateTime.UtcNow:u}";

    public static FileExtension? FindFileExtension(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return AvailableMeshFiles.AsValueEnumerable().FirstOrDefault(fileExtension => $".{fileExtension.Extension}" == ext);
    }

    public static MeshFile? CreateInstance(string filePath, FileMode fileMode, MeshFileFormat fileFormat = MeshFileFormat.BINARY, FileFormat? slicerFile = null)
    {
        var fileExtension = FindFileExtension(filePath);
        return fileExtension?.FileFormatType.CreateInstance<MeshFile>(filePath, fileMode, fileFormat, slicerFile!);
    }

    #endregion

    #region Enums

    public enum MeshFileFormat : byte
    {
        [Description("Binary")]
        BINARY,
        [Description("ASCII")]
        ASCII
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the file format for this mesh
    /// </summary>
    public MeshFileFormat FileFormat { get; } = MeshFileFormat.BINARY;

    /// <summary>
    /// Gets the <see cref="FileFormat"/> from model export
    /// </summary>
    public FileFormat? SlicerFile { get; }

    /// <summary>
    /// Gets the file path of the stream
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the file name with extension from <see cref="FilePath"/>
    /// </summary>
    public string Filename => Path.GetFileName(FilePath);
    public string FilenameWithoutExtension => Path.GetFileNameWithoutExtension(FilePath);

    /// <summary>
    /// Gets the current file stream
    /// </summary>
    public FileStream MeshStream { get; }

    /// <summary>
    /// Gets the number of vertexes, this is often triangle count * 3
    /// </summary>
    public uint VertexCount { get; protected set; }

    /// <summary>
    /// Gets the number of triangles
    /// </summary>
    public uint TriangleCount { get; protected set; }
    #endregion

    #region Constructor
    protected MeshFile(string filePath, FileMode fileMode, MeshFileFormat meshFileFormat = MeshFileFormat.BINARY, FileFormat? slicerFile = null)
    {
        FilePath = filePath;
        FileFormat = meshFileFormat;
        SlicerFile = slicerFile;
        MeshStream = new FileStream(filePath, fileMode);
    }
    #endregion

    #region Methods
    /// <summary>
    /// Call once before write content to the file, use this to build up the header if any
    /// </summary>
    public virtual void BeginWrite(){}

    /// <summary>
    /// Writes an triangle to the file
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <param name="normal"></param>
    public abstract void WriteTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal);

    /// <summary>
    /// Call once before close the file, use this to build up the footer if any
    /// </summary>
    public virtual void EndWrite(){}


    /// <inheritdoc />
    public void Dispose()
    {
        MeshStream?.Dispose();
    }
    #endregion
}