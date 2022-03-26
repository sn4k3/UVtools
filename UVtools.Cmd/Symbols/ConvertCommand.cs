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

internal static class ConvertCommand
{
    internal static Command CreateCommand()
    {
        var command = new Command("convert", "Convert input file into a output file format by a known type or extension")
        {
            GlobalArguments.InputFileArgument,
            new Argument<string>("target-type/ext", "Target format type or extension"),
            GlobalArguments.OutputFileArgument,

            new Option<ushort>(new[] {"-v", "--version"}, "Sets the file format version"),
            new Option<bool>("--no-overwrite", "If the output file exists do not overwrite"),
        };

        command.SetHandler((FileInfo inputFile, string targetTypeExt, FileInfo? outputFile, ushort version, bool noOverwrite) =>
            {

                var targetType = FileFormat.FindByAnyMeans(targetTypeExt);
                if (targetType is null)
                {
                    Program.WriteLineError($"Unable to find a valid convert type candidate from {targetTypeExt}.");
                    return;
                }

                string? outputFilePath;
                if (outputFile is not null)
                {
                    outputFilePath = outputFile.FullName;
                }
                else
                {
                    outputFilePath = FileFormat.GetFileNameStripExtensions(inputFile.Name)!;
                    if (targetType.FileExtensions.Length == 1)
                    {
                        outputFilePath = Path.Combine(inputFile.DirectoryName!, $"{outputFilePath}.{targetType.FileExtensions[0].Extension}");
                    }
                    else
                    {
                        var ext = FileExtension.Find(targetTypeExt);
                        if (ext is null)
                        {
                            Program.WriteLineError($"Unable to construct the output filename from {targetTypeExt}, there are {targetType.FileExtensions.Length} extensions on this format, please specify an output file.");
                            return;
                        }

                        outputFilePath = Path.Combine(inputFile.DirectoryName!, $"{outputFilePath}.{ext.Extension}");
                    }
                }

                var outputFileName = Path.GetFileName(outputFilePath);

                if (noOverwrite && File.Exists(outputFilePath))
                {
                    Program.WriteLineError($"{outputFileName} already exits! --no-overwrite is enabled.");
                    return;
                }
                
                var slicerFile = Program.OpenInputFile(inputFile);

                Program.ProgressBarWork($"Converting to {outputFileName}",
                    () =>
                    {
                        try
                        {
                            return slicerFile.Convert(targetType, outputFilePath, version, Program.Progress);
                        }
                        catch (Exception)
                        {
                            File.Delete(outputFilePath);
                            throw;
                        }
                    });

            }, command.Arguments[0], command.Arguments[1], command.Arguments[2],
            command.Options[0], command.Options[1]);

        return command;
    }
}