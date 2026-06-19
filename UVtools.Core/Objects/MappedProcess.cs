/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Objects;

public partial class MappedProcess : ObservableObject
{
    #region Constants

    public const string DefaultArgument = "\"{0}\"";
    #endregion

    #region Members

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets if this device is enabled
    /// </summary>
    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the full path for the application
    /// </summary>
    [ObservableProperty]
    public partial string ApplicationPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the path name alias
    /// </summary>
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the arguments for the application
    /// </summary>
    [ObservableProperty]
    public partial string Arguments { get; set; } = DefaultArgument;

    /// <summary>
    /// Gets or sets the compatible extensions with this device.
    /// Empty or null to be compatible with everything
    /// </summary>
    [ObservableProperty]
    public partial string? CompatibleExtensions { get; set; }

    [ObservableProperty]
    public partial bool WaitForExit { get; set; }

    #endregion

    #region Constructors

    public MappedProcess() { }

    public MappedProcess(bool isEnabled, string applicationPath, string? name = null, string arguments = DefaultArgument)
    {
        IsEnabled = isEnabled;
        ApplicationPath = applicationPath;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = Path.GetFileNameWithoutExtension(applicationPath);
        }
        Name = name;

        if (string.IsNullOrWhiteSpace(arguments))
        {
            arguments = DefaultArgument;
        }

        Arguments = arguments;
    }

    public MappedProcess(string applicationPath, string? name = null, string arguments = DefaultArgument) : this(true, applicationPath, name, arguments)
    { }

    #endregion

    #region Methods

    public bool IsValid()
    {
        return File.Exists(ApplicationPath);
    }

    public async Task StartProcess(FileFormat slicerFile, CancellationToken cancellationToken = default)
    {
        await StartProcess(slicerFile.FileFullPath!, cancellationToken);
    }

    public async Task StartProcess(string slicerFile, CancellationToken cancellationToken = default)
    {
        var arguments = string.IsNullOrWhiteSpace(Arguments) ? $"\"{slicerFile}\"" : string.Format(Arguments, slicerFile);
        using var process = Process.Start(ApplicationPath, arguments);
        if (process is null) return;
        if (WaitForExit)
        {
            await process.WaitForExitAsync(cancellationToken);
        }
    }

    public override string ToString()
    {
        return $"{ApplicationPath} {Arguments}";
    }

    protected bool Equals(MappedProcess other)
    {
        return ApplicationPath == other.ApplicationPath && Name == other.Name && Arguments == other.Arguments && CompatibleExtensions == other.CompatibleExtensions;
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
        return HashCode.Combine(ApplicationPath, Name, Arguments, CompatibleExtensions);
    }

    public MappedProcess Clone()
    {
        return (MemberwiseClone() as MappedProcess)!;
    }

    #endregion
}
