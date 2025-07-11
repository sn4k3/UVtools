﻿/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.CommandLine;
using System.IO;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.Core.Suggestions;

namespace UVtools.Cmd.Symbols;

internal static class RunCommand
{
    internal static Command CreateCommand()
    {
        var classesFilesArgument = new Argument<string[]>("classes/files")
        {
            Description = "Operations, suggestions and script class/files(.uvtop, .uvtsu .cs, .csx) to run",
        };

        var propertiesOption = new Option<string[]>("-p", "--property")
        {
            Description = "Set a property with a new value (Compatible with operations only)",
            HelpName = "property=value",
            AllowMultipleArgumentsPerToken = true,
        };

        var command = new Command("run", "Run operations, suggestions and/or scripts")
        {
            GlobalArguments.InputFileArgument,
            classesFilesArgument,

            propertiesOption,
            GlobalOptions.OutputFile,
            GlobalOptions.OpenInPartialMode
        };

        command.SetAction(result => {
            var inputFile = result.GetRequiredValue(GlobalArguments.InputFileArgument);
            var classesFiles = result.GetRequiredValue(classesFilesArgument);
            var properties = result.GetValue(propertiesOption) ?? [];
            var outputFile = result.GetValue(GlobalOptions.OutputFile);
            var partialMode = result.GetValue(GlobalOptions.OpenInPartialMode);

            if (classesFiles.Length == 0)
            {
                Program.WriteLineError("No specified files to run");
                return;
            }

            var slicerFile = Program.OpenInputFile(inputFile, partialMode ? FileFormat.FileDecodeType.Partial : FileFormat.FileDecodeType.Full);
            uint runs = 0;
            uint successfulRuns = 0;

            var parsedProperties = ReflectionPropertyValue.ParseFromString(properties);

            foreach (var classFile in classesFiles)
            {
                // Operations
                var operation = Operation.CreateInstance(classFile, true, slicerFile);
                if (operation is not null)
                {
                    ReflectionPropertyValue.SetProperties(operation, parsedProperties);
                    var message = operation.ValidateInternally();
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        Program.WriteLineWarning($"Operation '{operation.Title}' can not execute: {message}");
                        continue;
                    }

                    Program.ProgressBarWork($"Operation {++runs}: {operation.ProgressTitle}",
                        () =>
                        {
                            var success = operation.Execute(Program.Progress);
                            if (!string.IsNullOrWhiteSpace(operation.AfterCompleteReport))
                            {
                                Program.WriteLine(operation.AfterCompleteReport);
                            }

                            if (!success) return;
                            successfulRuns++;
                        });

                    continue;
                }

                // Suggestions
                var suggestion = Suggestion.CreateInstance(classFile, true, slicerFile);
                if (suggestion is not null)
                {
                    suggestion.Enabled = true;
                    ReflectionPropertyValue.SetProperties(suggestion, parsedProperties);

                    var message = suggestion.Validate();
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        Program.WriteLineWarning($"Suggestion '{suggestion.Title}' can not execute: {message}");
                        continue;
                    }

                    Program.ProgressBarWork($"Suggestion {++runs}: {suggestion.Title}",
                        () =>
                        {
                            if (!suggestion.Execute(Program.Progress)) return;
                            successfulRuns++;
                        });

                    continue;
                }

                // Script files
                if ((classFile.EndsWith(".cs") || classFile.EndsWith(".csx")) && File.Exists(classFile))
                {
                    var operationScripting = new OperationScripting(slicerFile);
                    operationScripting.ReloadScriptFromFile(classFile);
                    var message = operationScripting.ValidateInternally();
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        Program.ProgressBarWork($"Script {++runs}: {operationScripting.ScriptGlobals?.Script.Name ?? operationScripting.ProgressTitle}",
                            () =>
                            {
                                if (operationScripting.Execute(Program.Progress)) successfulRuns++;
                            });
                    }
                    else
                    {
                        Program.WriteLineWarning($"Script {classFile} can not execute: {message}");
                    }
                    continue;
                }

                Program.WriteLineWarning($"Invalid class or file: {classFile}");
            }

            foreach (var property in parsedProperties)
            {
                if (property.Found) continue;
                Program.WriteLineWarning($"Property {property.Name} was defined but not found nor set.");
            }

            if (successfulRuns > 0) Program.SaveFile(slicerFile, outputFile);
        });

        return command;
    }
}