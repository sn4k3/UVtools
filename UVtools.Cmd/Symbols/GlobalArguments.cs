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

internal static class GlobalArguments
{
    internal static Argument<FileInfo> InputFileArgument { get; } = new Argument<FileInfo>("input-file", "Input file to open and read").ExistingOnly();
    internal static Argument<FileInfo?> OutputFileArgument { get; } = new("output-file", () => null, "Output file to save");
    internal static Argument<DirectoryInfo?> OutputDirectoryArgument { get; } = new("output-folder", () => null, "Output folder");
}