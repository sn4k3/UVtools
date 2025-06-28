/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.CommandLine;
using System.IO;
using System.Linq;
using UVtools.Core.FileFormats;

namespace UVtools.Cmd.Symbols;

internal static class CopyParametersCommand
{
    internal static Command CreateCommand()
    {
        var targetFilesArgument = new Argument<FileInfo[]>("target-files")
        {
            Description = "Target file(s) to set the parameters",
        }.AcceptExistingOnly();

        var command = new Command("copy-parameters", "Copy print parameters from one file to another")
        {
            GlobalArguments.InputFileArgument,
            targetFilesArgument
        };

        command.SetAction(result =>
        {
            var inputFile = result.GetRequiredValue(GlobalArguments.InputFileArgument);
            var targetFiles = result.GetRequiredValue(targetFilesArgument);


            var slicerFile1 = Program.OpenInputFile(inputFile, FileFormat.FileDecodeType.Partial);

            var distinctFiles = targetFiles.DistinctBy(fi => fi.FullName);

            foreach (var file in distinctFiles)
            {
                if (inputFile.FullName == file.FullName)
                {
                    Program.WriteWarning($"Skipping: {inputFile.Name}, same as input file");
                    continue;
                }
                var slicerFileTarget = Program.OpenInputFile(file, FileFormat.FileDecodeType.Partial);

                var count = FileFormat.CopyParameters(slicerFile1, slicerFileTarget);
                if (count > 0)
                {
                    slicerFileTarget.Save(Program.Progress);
                }

                Program.WriteLine($"{count} properties changed");
            }

        });

        return command;
    }
}