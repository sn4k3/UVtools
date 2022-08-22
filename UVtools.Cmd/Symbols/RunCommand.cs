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
using UVtools.Core.Operations;

namespace UVtools.Cmd.Symbols;

internal static class RunCommand
{
    internal static Command CreateCommand()
    {
        var filesArgument = new Argument<FileInfo[]>("files", "Operation and script files to run (.uvtop, .cs, .csx)").ExistingOnly();

        var command = new Command("run", "Run operations and/or scripts")
        {
            GlobalArguments.InputFileArgument,
            filesArgument,

            GlobalOptions.OutputFile,
            GlobalOptions.OpenInPartialMode
        };

        command.SetHandler((inputFile, files, outputFile, partialMode) =>
            {
                if (files.Length == 0)
                {
                    Program.WriteLineError("No files to run");
                    return;
                }

                var slicerFile = Program.OpenInputFile(inputFile, partialMode ? FileFormat.FileDecodeType.Partial : FileFormat.FileDecodeType.Full);
                uint runs = 0;
                uint sucessfullRuns = 0;


                foreach (var file in files)
                {
                    var operation = Operation.Deserialize(file.FullName);

                    if (operation is not null)
                    {
                        operation.SlicerFile = slicerFile;
                        var result = operation.ValidateInternally();
                        if (string.IsNullOrWhiteSpace(result))
                        {
                            Program.ProgressBarWork($"Operation {++runs}: {operation.ProgressTitle}",
                                () =>
                                {
                                    if(operation.Execute(Program.Progress)) sucessfullRuns++;
                                });
                        }
                        else
                        {
                            Program.WriteLineWarning($"Operation {file.Name} can not execute: {result}");
                        }

                        continue;
                    }

                    if (file.Name.EndsWith(".cs") || file.Name.EndsWith(".csx"))
                    {
                        var operationScripting = new OperationScripting(slicerFile);
                        operationScripting.ReloadScriptFromFile(file.FullName);
                        var result = operationScripting.ValidateInternally();
                        if (string.IsNullOrWhiteSpace(result))
                        {
                            Program.ProgressBarWork($"Script {++runs}: {operationScripting.ScriptGlobals?.Script.Name ?? operationScripting.ProgressTitle}",
                                () =>
                                {
                                    if (operationScripting.Execute(Program.Progress)) sucessfullRuns++;
                                });
                        }
                        else
                        {
                            Program.WriteLineWarning($"Script {file.Name} can not execute: {result}");
                        }
                        continue;
                    }

                    Program.WriteLineWarning($"Invalid file: {file.Name}");
                }

                if(sucessfullRuns > 0) Program.SaveFile(slicerFile, outputFile);
            }, GlobalArguments.InputFileArgument, filesArgument, GlobalOptions.OutputFile, GlobalOptions.OpenInPartialMode);

        return command;
    }
}