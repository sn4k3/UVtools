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
        var command = new Command("extract", "Extract file contents to a folder")
        {
            GlobalArguments.InputFileArgument,
            GlobalArguments.OutputDirectoryArgument,
            new Option<bool>("--no-overwrite", "If the output folder exists do not overwrite"),
        };

        command.SetHandler((FileInfo inputFile, DirectoryInfo? outputDirectory, bool noOverwrite) =>
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

            }, command.Arguments[0], command.Arguments[1],
            command.Options[0]);

        return command;
    }
}