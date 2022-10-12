/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using UVtools.Core.Objects;

namespace UVtools.Core.Network;

public class MappedDevice : BindableBase
{
    #region Members

    private bool _isEnabled = true;
    private string _path = null!;
    private string? _name;
    private string? _compatibleExtensions;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets if this device is enabled
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    /// <summary>
    /// Gets or sets the full path for the location
    /// </summary>
    public string Path
    {
        get => _path;
        set => RaiseAndSetIfChanged(ref _path, value);
    }

    /// <summary>
    /// Gets or sets the path name alias
    /// </summary>
    public string? Name
    {
        get => _name;
        set => RaiseAndSetIfChanged(ref _name, value);
    }

    /// <summary>
    /// Gets or sets the compatible extensions with this device.
    /// Empty or null to be compatible with everything
    /// </summary>
    public string? CompatibleExtensions
    {
        get => _compatibleExtensions;
        set => RaiseAndSetIfChanged(ref _compatibleExtensions, value);
    }

    #endregion

    #region Constructors

    public MappedDevice() { }

    public MappedDevice(string path, string? name = null)
    {
        _path = path;
        _name = name;
    }

    #endregion

    #region Methods

    public override string ToString()
    {
        if (!string.IsNullOrWhiteSpace(_name)) return $"{_path}  ({_name})";
        return _path;
    }

    protected bool Equals(MappedDevice other)
    {
        return _path == other._path;
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
        return (_path != null ? _path.GetHashCode() : 0);
    }

    public MappedDevice Clone()
    {
        return (MemberwiseClone() as MappedDevice)!;
    }

    #endregion
}