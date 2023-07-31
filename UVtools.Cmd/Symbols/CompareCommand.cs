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
using static UVtools.Core.FileFormats.UVJFile;

namespace UVtools.Cmd.Symbols;

internal static class CompareCommand
{
    internal static Command CreateCommand()
    {
        var fileAArgument = new Argument<FileInfo>("input-file-a", "Input file (A) to compare").ExistingOnly();
        var fileBArgument = new Argument<FileInfo>("input-file-b", "Input file (B) to compare").ExistingOnly();
        var excludeLayersOption = new Option<bool>(new[] { "--without-layers" }, "Do not compare layers");
        var propertiesOption = new Option<string[]>(new[] { "-p", "--property" }, "List of strict properties to show if different");

        var command = new Command("compare", "Compare two files and output the differences")
        {
	        fileAArgument,
	        fileBArgument,

	        excludeLayersOption,
	        propertiesOption,
			GlobalOptions.OpenInFullMode
		};

        command.SetHandler((inputFileA, inputFileB, excludeLayers, properties, fullMode) =>
        {
            var slicerFileA = Program.OpenInputFile(inputFileA, fullMode ? FileFormat.FileDecodeType.Full : FileFormat.FileDecodeType.Partial);
            var slicerFileB = Program.OpenInputFile(inputFileB, fullMode ? FileFormat.FileDecodeType.Full : FileFormat.FileDecodeType.Partial);
            var comparison = FileFormat.Compare(slicerFileA, slicerFileB, !excludeLayers, properties);
            Console.Write(comparison);

        }, fileAArgument, fileBArgument, excludeLayersOption, propertiesOption, GlobalOptions.OpenInFullMode);

        return command;
    }
}