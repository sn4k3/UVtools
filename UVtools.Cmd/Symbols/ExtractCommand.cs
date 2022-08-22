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

internal static class ExtractCommand
{
    internal static Command CreateCommand()
    {
        var noOverwriteOption = new Option<bool>("--no-overwrite", "If the output folder exists do not overwrite");

        var command = new Command("extract", "Extract file contents to a folder")
        {
            GlobalArguments.InputFileArgument,
            GlobalArguments.OutputDirectoryArgument,
            noOverwriteOption,
        };

        command.SetHandler((inputFile, outputDirectory, noOverwrite) =>
            {
                var path = outputDirectory is null
                    ? Path.Combine(inputFile.DirectoryName!, Path.GetFileNameWithoutExtension(inputFile.Name))
                    : outputDirectory.FullName;

                if (noOverwrite && Directory.Exists(path))
                {
                    Program.WriteLineError($"{path} already exits! --no-overwrite is enabled.");
                    return;
                }

                var slicerFile = Program.OpenInputFile(inputFile);

                Program.ProgressBarWork($"Extracting to {Path.GetFileName(path)}",
                    () =>
                    {
                        slicerFile.Extract(path);
                    });

            }, GlobalArguments.InputFileArgument, GlobalArguments.OutputDirectoryArgument, noOverwriteOption);

        return command;
    }
}