/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Reflection;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;

namespace UVtools.Cmd.Symbols;

internal static class RunCommand
{
    internal static Command CreateCommand()
    {
        var filesArgument = new Argument<FileInfo[]>("files", "Operation and script files to run (.uvtop, .cs, .csx)").ExistingOnly();
        var propertiesOption = new Option<string[]>(new []{ "-p" , "--property"}, "Set a property with a new value (Compatible with operations only)")
        {
            AllowMultipleArgumentsPerToken = true,
            ArgumentHelpName = "property=value"
        };

        var command = new Command("run", "Run operations and/or scripts")
        {
            GlobalArguments.InputFileArgument,
            filesArgument,

            propertiesOption,
            GlobalOptions.OutputFile,
            GlobalOptions.OpenInPartialMode
        };

        command.SetHandler((inputFile, files, properties, outputFile, partialMode) =>
            {
                if (files.Length == 0)
                {
                    Program.WriteLineError("No specified files to run");
                    return;
                }

                var slicerFile = Program.OpenInputFile(inputFile, partialMode ? FileFormat.FileDecodeType.Partial : FileFormat.FileDecodeType.Full);
                uint runs = 0;
                uint successfulRuns = 0;

                var parsedProperties = new List<ReflectionPropertyValue>(properties.Length);

                foreach (var property in properties)
                {
                    var split = property.Split(new[] {'=', ':'}, 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if(split.Length < 2) continue;
                    parsedProperties.Add(new (split[0], split[1]));
                }

                foreach (var file in files)
                {
                    var operation = Operation.Deserialize(file.FullName);

                    if (operation is not null)
                    {
                        if (parsedProperties.Count > 0)
                        {
                            foreach (var propertyInfo in operation.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                            {
                                foreach (var property in parsedProperties)
                                {
                                    if (propertyInfo.Name != property.Name) continue;
                                    propertyInfo.SetValueFromString(operation, property.Value);
                                    property.Found = true;
                                }
                            }
                        }

                        operation.SlicerFile = slicerFile;
                        var result = operation.ValidateInternally();
                        if (string.IsNullOrWhiteSpace(result))
                        {
                            Program.ProgressBarWork($"Operation {++runs}: {operation.ProgressTitle}",
                                () =>
                                {
                                    if (!operation.Execute(Program.Progress)) return;
                                    successfulRuns++;
                                    if (!string.IsNullOrWhiteSpace(operation.AfterCompleteReport))
                                    {
                                        Program.WriteLine(operation.AfterCompleteReport);
                                    }
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
                                    if (operationScripting.Execute(Program.Progress)) successfulRuns++;
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

                foreach (var property in parsedProperties)
                {
                    if (property.Found) continue;
                    Program.WriteLineWarning($"Property {property.Name} was defined but not found nor set.");
                }

                if(successfulRuns > 0) Program.SaveFile(slicerFile, outputFile);
            }, GlobalArguments.InputFileArgument, filesArgument, propertiesOption, GlobalOptions.OutputFile, GlobalOptions.OpenInPartialMode);

        return command;
    }
}