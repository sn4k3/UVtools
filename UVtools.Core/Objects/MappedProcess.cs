/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Objects;

public class MappedProcess : BindableBase
{
    #region Constants

    public const string DefaultArgument = "\"{0}\"";
    #endregion

    #region Members

    private bool _isEnabled = true;
    private string _applicationPath = null!;
    private string _name = string.Empty;
    private string _arguments = DefaultArgument;
    private string? _compatibleExtensions;
    private bool _waitForExit;

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
    /// Gets or sets the full path for the application
    /// </summary>
    public string ApplicationPath
    {
        get => _applicationPath;
        set => RaiseAndSetIfChanged(ref _applicationPath, value);
    }

    /// <summary>
    /// Gets or sets the path name alias
    /// </summary>
    public string Name
    {
        get => _name;
        set => RaiseAndSetIfChanged(ref _name, value);
    }

    /// <summary>
    /// Gets or sets the arguments for the application
    /// </summary>
    public string Arguments
    {
        get => _arguments;
        set => RaiseAndSetIfChanged(ref _arguments, value);
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

    public bool WaitForExit
    {
        get => _waitForExit;
        set => RaiseAndSetIfChanged(ref _waitForExit, value);
    }

    #endregion

    #region Constructors

    public MappedProcess() { }

    public MappedProcess(bool isEnabled, string applicationPath, string? name = null, string arguments = DefaultArgument)
    {
        _isEnabled = isEnabled;
        _applicationPath = applicationPath;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = Path.GetFileNameWithoutExtension(applicationPath);
        }
        _name = name;

        if (string.IsNullOrWhiteSpace(arguments))
        {
            arguments = DefaultArgument;
        }

        _arguments = arguments;
    }

    public MappedProcess(string applicationPath, string? name = null, string arguments = DefaultArgument) : this(true, applicationPath, name, arguments)
    { }

    #endregion

    #region Methods

    public bool IsValid()
    {
        return File.Exists(_applicationPath);
    }

    public async Task StartProcess(FileFormat slicerFile, CancellationToken cancellationToken = default)
    {
        await StartProcess(slicerFile.FileFullPath!, cancellationToken);
    }

    public async Task StartProcess(string slicerFile, CancellationToken cancellationToken = default)
    {
        var arguments = string.IsNullOrWhiteSpace(_arguments) ? $"\"{slicerFile}\"" : string.Format(_arguments, slicerFile);
        using var process = Process.Start(_applicationPath, arguments);
        if (process is null) return;
        if (_waitForExit)
        {
            await process.WaitForExitAsync(cancellationToken);
        }
    }

    public override string ToString()
    {
        return $"{_applicationPath} {Arguments}";
    }

    protected bool Equals(MappedProcess other)
    {
        return _applicationPath == other._applicationPath && _name == other._name && _arguments == other._arguments && _compatibleExtensions == other._compatibleExtensions;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MappedProcess)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_applicationPath, _name, _arguments, _compatibleExtensions);
    }

    public MappedProcess Clone()
    {
        return (MemberwiseClone() as MappedProcess)!;
    }

    #endregion
}