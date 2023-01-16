/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.IO;

namespace UVtools.Core.Objects;


public class GenericFileRepresentation : BindableBase, ICloneable, 
    IComparable<GenericFileRepresentation>, IEquatable<GenericFileRepresentation>,
    IComparable<string>, IEquatable<string>
{
    #region Members
    private string _filePath = null!;
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the file path
    /// </summary>
    public string FilePath
    {
        get => _filePath;
        set
        {
            if (RaiseAndSetIfChanged(ref _filePath, value)) return;
            RaisePropertyChanged(nameof(FileName));
            RaisePropertyChanged(nameof(Exists));
            RaisePropertyChanged(nameof(FileInfo));
        }
    }

    /// <summary>
    /// Gets the file name with extension
    /// </summary>
    public string FileName => Path.GetFileName(_filePath);

    /// <summary>
    /// Gets the file name without extension
    /// </summary>
    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(_filePath);

    /// <summary>
    /// Gets the file extension. The returned value includes the period (".")
    /// </summary>
    public string FileExtension => Path.GetExtension(_filePath);

    /// <summary>
    /// Gets if the file exists
    /// </summary>
    public bool Exists => File.Exists(FilePath);

    /// <summary>
    /// Gets an <see cref="FileInfo"/> instance on the <see cref="FilePath"/>
    /// </summary>
    public FileInfo FileInfo => new(_filePath);
    #endregion

    #region Constructor
    public GenericFileRepresentation()
    { }

    public GenericFileRepresentation(string filePath)
    {
        _filePath = filePath;
    }
    #endregion

    #region Methods

    /// <summary>
    /// Checks if the <see cref="FilePath"/> ends with <paramref name="extension"/>
    /// </summary>
    /// <param name="extension">Extension name</param>
    /// <returns>True if found, otherwise false</returns>
    public bool IsExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return false;
        if (extension[0] != '.') extension = $".{extension}";
        return _filePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
        return FileName;
    }

    public object Clone()
    {
        return MemberwiseClone();
    }

    public int CompareTo(GenericFileRepresentation? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return string.Compare(FileName, other.FileName, StringComparison.Ordinal);
    }

    public int CompareTo(string? other)
    {
        return string.Compare(FileName, other, StringComparison.Ordinal);
    }

    public bool Equals(GenericFileRepresentation? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _filePath == other._filePath;
    }
    

    public bool Equals(string? other)
    {
        return _filePath.Equals(other);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is GenericFileRepresentation other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _filePath.GetHashCode();
    }
    #endregion
}