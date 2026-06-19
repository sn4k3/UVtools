/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using UVtools.Core.Objects;

namespace UVtools.Core.Network;

public partial class MappedDevice : ObservableObject
{
    #region Members

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets if this device is enabled
    /// </summary>
    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the full path for the location
    /// </summary>
    [ObservableProperty]
    public partial string Path { get; set; } = null!;

    /// <summary>
    /// Gets or sets the path name alias
    /// </summary>
    [ObservableProperty]
    public partial string? Name { get; set; }

    /// <summary>
    /// Gets or sets the compatible extensions with this device.
    /// Empty or null to be compatible with everything
    /// </summary>
    [ObservableProperty]
    public partial string? CompatibleExtensions { get; set; }

    #endregion

    #region Constructors

    public MappedDevice() { }

    public MappedDevice(string path, string? name = null)
    {
        Path = path;
        Name = name;
    }

    #endregion

    #region Methods

    public override string ToString()
    {
        if (!string.IsNullOrWhiteSpace(Name)) return $"{Path}  ({Name})";
        return Path;
    }

    protected bool Equals(MappedDevice other)
    {
        return Path == other.Path;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MappedDevice)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Path);
    }

    public MappedDevice Clone()
    {
        return (MemberwiseClone() as MappedDevice)!;
    }

    #endregion
}
