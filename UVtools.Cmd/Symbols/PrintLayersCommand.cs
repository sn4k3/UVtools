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
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using UVtools.Core.FileFormats;

namespace UVtools.Cmd.Symbols;

internal static class PrintLayersCommand
{
    internal static Command CreateCommand()
    {
        var rangeOption = new Option<string>(new[] { "-r", "--range" }, "Prints only the matching layer(s) index(es) in a range")
        {
            ArgumentHelpName = "startindex:endindex"
        };
        var indexesOption = new Option<uint[]>(new[] { "-i", "--indexes" }, "Prints only the matching layer(s) index(es)")
        {
            AllowMultipleArgumentsPerToken = true
        };
        var matchNamesOption = new Option<string[]>(new[] { "-n", "--names" }, "Prints only the name matching properties")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var command = new Command("print-layers", "Prints layer(s) properties")
        {
            GlobalArguments.InputFileArgument,
            rangeOption,
            indexesOption,
            matchNamesOption,
            GlobalOptions.OpenInPartialMode
        };

        command.SetHandler((inputFile, layerRange, layerIndexes, matchNames, partialMode) =>
            {
                var slicerFile = Program.OpenInputFile(inputFile, partialMode ? FileFormat.FileDecodeType.Partial : FileFormat.FileDecodeType.Full);
                var layerIndexesList = new List<uint>();

                if (!string.IsNullOrWhiteSpace(layerRange))
                {
                    if (slicerFile.TryParseLayerIndexRange(layerRange, out var layerIndexStart, out var layerIndexEnd))
                    {
                        for (var layerIndex = layerIndexStart; layerIndex <= layerIndexEnd; layerIndex++)
                        {
                            layerIndexesList.Add(layerIndex);
                        }
                    }
                    else
                    {
                        Program.WriteLineError($"The specified layer range '{layerRange}' is malformed, use startindex:endindex with positive numbers");
                    }
                }

                if (layerIndexes.Length == 0 && layerIndexesList.Count == 0)
                {
                    for (uint i = 0; i < slicerFile.LayerCount; i++)
                    {
                        layerIndexesList.Add(slicerFile.SanitizeLayerIndex(i));
                    }
                }
                else
                {
                    layerIndexesList.AddRange(layerIndexes.Select(layerIndex => slicerFile.SanitizeLayerIndex(layerIndex)));
                }

                layerIndexesList = layerIndexesList.Distinct().OrderBy(layerIndex => layerIndex).ToList();

                foreach (var layerIndex in layerIndexesList)
                {
                    Console.WriteLine($"Layer: {layerIndex}");
                    foreach (var propertyInfo in slicerFile[layerIndex].GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (propertyInfo.Name.Equals("Item")) continue;
                        if (matchNames is not null && matchNames.Length > 0)
                        {
                            if (matchNames.All(s => s != propertyInfo.Name)) continue;
                        }
                        if (propertyInfo.GetCustomAttributes().Any(attribute =>
                            {
                                var type = attribute.GetType();
                                if (type == typeof(XmlIgnoreAttribute)) return true;
                                if (type == typeof(JsonIgnoreAttribute)) return true;
                                return false;
                            })) continue;
                        Console.WriteLine($"{propertyInfo.Name}: {propertyInfo.GetValue(slicerFile[layerIndex])}");
                    }
                }

            }, GlobalArguments.InputFileArgument, rangeOption, indexesOption, matchNamesOption, GlobalOptions.OpenInPartialMode);

        return command;
    }
}