/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.CommandLine;
using System.IO;
using UVtools.Core.FileFormats;

namespace UVtools.Cmd.Symbols;

internal static class CompareCommand
{
    internal static Command CreateCommand()
    {
        var fileAArgument = new Argument<FileInfo>("input-file-a")
        {
            Description = "Input file (A) to compare"
        }.AcceptExistingOnly();
        var fileBArgument = new Argument<FileInfo>("input-file-b")
        {
            Description = "Input file (B) to compare"
        }.AcceptExistingOnly();
        var excludeLayersOption = new Option<bool>("--without-layers")
        {
            Description = "Do not compare layers"
        };
        var propertiesOption = new Option<string[]>("-p", "--property")
        {
            Description = "List of strict properties to show if different",
        };

        var command = new Command("compare", "Compare two files and output the differences")
        {
            fileAArgument,
            fileBArgument,

            excludeLayersOption,
            propertiesOption,
            GlobalOptions.OpenInFullMode
        };

        command.SetAction(result =>
        {
            var inputFileA = result.GetRequiredValue(fileAArgument);
            var inputFileB = result.GetRequiredValue(fileBArgument);
            var excludeLayers = result.GetValue(excludeLayersOption);
            var properties = result.GetValue(propertiesOption) ?? [];
            var fullMode = result.GetValue(GlobalOptions.OpenInFullMode);

            var slicerFileA = Program.OpenInputFile(inputFileA, fullMode ? FileFormat.FileDecodeType.Full : FileFormat.FileDecodeType.Partial);
            var slicerFileB = Program.OpenInputFile(inputFileB, fullMode ? FileFormat.FileDecodeType.Full : FileFormat.FileDecodeType.Partial);
            var comparison = FileFormat.Compare(slicerFileA, slicerFileB, !excludeLayers, properties);
            Console.Write(comparison);
        });

        return command;
    }
}