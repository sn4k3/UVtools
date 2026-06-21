using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace build;

public record BuildRuntime
{
    public enum BundleTypes
    {
        None,
        Installer,
        AppImage,
        App
    }

    /// <summary>
    /// Runtime Identifier (RID) of the build
    /// </summary>
    public required string Runtime { get; init; } = string.Empty;

    /// <summary>
    /// Is this a bundle?
    /// </summary>
    public bool IsBundle { get; init; }

    /// <summary>
    /// Type of bundle
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public BundleTypes BundleType { get; init; }

    /// <summary>
    /// Build date and time in UTC
    /// </summary>
    public DateTime BuildDateTimeUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Build OS Description
    /// </summary>
    public string BuildOSDescription { get; init; } = RuntimeInformation.OSDescription;

    /// <summary>
    /// Build version
    /// </summary>
    public required string BuildVersion { get; init; } = string.Empty;

    public BuildRuntime()
    {
    }

    [SetsRequiredMembers]
    public BuildRuntime(string runtime, string buildVersion, bool isBundle = false,
        BundleTypes bundleType = BundleTypes.None)
    {
        Runtime = runtime;
        BuildVersion = buildVersion;
        IsBundle = isBundle;
        BundleType = bundleType;
    }
}
