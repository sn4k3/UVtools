/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.CommandLine;
using System.IO;

namespace UVtools.Cmd.Symbols;

internal static class GlobalOptions
{
    internal static Option<decimal> MemoryReserveOption { get; } = CreateMemorySizeOption(
        "--memory-reserve", "RAM to keep available in GB; 0 selects the automatic reserve");

    internal static Option<decimal> CacheMaximumOption { get; } = CreateMemorySizeOption(
        "--cache-max", "Maximum physical disk-cache size in GB; 0 uses available disk space");

    internal static Option<string?> CacheFolderOption { get; } = new("--cache-folder")
    {
        Description = "Base directory for the layer disk cache (default: the platform cache directory)",
        Recursive = true
    };

    internal static Option<bool> NoDiskCacheOption { get; } = new("--no-disk-cache")
    {
        Description = "Keep layer buffers in RAM instead of spilling them to disk",
        Recursive = true
    };

    internal static Option<bool> NoAdaptiveMemoryOption { get; } = new("--no-adaptive-memory")
    {
        Description = "Disable adaptive worker sizing, cache sizing and memory-pressure relief",
        Recursive = true
    };

    private static Option<decimal> CreateMemorySizeOption(string name, string description)
    {
        var option = new Option<decimal>(name) { Description = description, HelpName = "GB", Recursive = true };
        option.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<decimal>();
            if (value < 0 || value > long.MaxValue / (1024m * 1024 * 1024))
                result.AddError($"{name} must be a non-negative size in GB that fits in a 64-bit byte count.");
        });
        return option;
    }

    internal static Option<bool> CoreVersionOption { get; } = new("--core-version")
    {
        Description = "Show the core version information"
    };

    internal static Option<bool> DummyOption { get; } = new("--dummy", "--dummy-mode")
    {
        Description = "Do not save alterations to file"
    };

    internal static Option<bool> QuietOption { get; } = new("-q", "--quiet")
    {
        Description = "Make output silent but exceptions error will still show"
    };

    internal static Option<bool> NoProgressOption { get; } = new("--no-progress")
    {
        Description = "Show no progress"
    };

    internal static Option<FileInfo> OutputFile { get; } = new("-o", "--output")
    {
        Description = "Output file to save"
    };

    internal static Option<bool> OpenInPartialMode { get; } = new("--partial-mode")
    {
        Description = "Fast load the file in partial mode"
    };
    internal static Option<bool> OpenInFullMode { get; } = new("--full-mode")
    {
        Description = "Load the file in full/complete mode"
    };
}
