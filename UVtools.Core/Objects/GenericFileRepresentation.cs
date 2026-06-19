/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UVtools.Core.Objects;

public partial class GenericFileRepresentation : ObservableObject, ICloneable,
    IComparable<GenericFileRepresentation>, IEquatable<GenericFileRepresentation>,
    IComparable<string>, IEquatable<string>
{
    #region Properties

    /// <summary>
    /// Gets or sets the file path
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileName), nameof(Exists), nameof(FileInfo))]
    [field: MaybeNull]
    [field: AllowNull]
    public partial string FilePath { get; set; }

    /// <summary>
    /// Gets the file name with extension
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Gets the file name without extension
    /// </summary>
    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(FilePath);

    /// <summary>
    /// Gets the file extension. The returned value includes the period (".")
    /// </summary>
    public string FileExtension => Path.GetExtension(FilePath);

    /// <summary>
    /// Gets if the file exists
    /// </summary>
    public bool Exists => File.Exists(FilePath);

    /// <summary>
    /// Gets an <see cref="FileInfo"/> instance on the <see cref="FilePath"/>
    /// </summary>
    public FileInfo FileInfo => new(FilePath);

    #endregion

    #region Constructor

    public GenericFileRepresentation()
    {
    }

    public GenericFileRepresentation(string filePath)
    {
        FilePath = filePath;
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
        return FilePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
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
        return FilePath == other.FilePath;
    }


    public bool Equals(string? other)
    {
        return FilePath.Equals(other);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is GenericFileRepresentation other && Equals(other));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FilePath);
    }

    #endregion
}