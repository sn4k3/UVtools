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
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using UVtools.Core.FileFormats;

namespace UVtools.Cmd.Symbols;

internal static class PrintLayersCommand
{
    internal static Command CreateCommand()
    {
        var rangeOption = new Option<string>(new[] { "-r", "--range" }, "Prints only the matching layer index(es) in a range");
        var indexesOption = new Option<ushort[]>(new[] {"-i", "--indexes"}, "Prints only the matching layer index(es)");
        var matchNamesOption = new Option<string[]>(new[] { "-n", "--names" }, "Prints only the name matching properties");

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
                    var match = Regex.Match(layerRange, @"(\d+)(:|\||-)(\d+)");
                    if (match.Success && match.Groups.Count >= 4)
                    {
                        var startNumberStr = match.Groups[1].Value;
                        var endNumberStr = match.Groups[3].Value;

                        if (uint.TryParse(startNumberStr, out var startLayerIndex) &&
                            uint.TryParse(endNumberStr, out var endLayerIndex))
                        {
                            if (startLayerIndex > endLayerIndex)
                            {
                                (startLayerIndex, endLayerIndex) = (endLayerIndex, startLayerIndex);
                            }

                            for (var layerIndex = startLayerIndex; layerIndex <= endLayerIndex; layerIndex++)
                            {
                                layerIndexesList.Add(layerIndex);
                            }
                        }
                    }
                }

                if (layerIndexes.Length == 0 && layerIndexesList.Count == 0)
                {
                    for (uint i = 0; i < slicerFile.LayerCount; i++)
                    {
                        layerIndexesList.Add(i);
                    }
                }
                else
                {
                    layerIndexesList.AddRange(layerIndexes.Select(layerIndex => (uint) layerIndex));
                }

                layerIndexesList = layerIndexesList.Distinct().OrderBy(layerIndex => layerIndex).ToList();

                foreach (var layerIndex in layerIndexesList)
                {
                    Console.WriteLine($"Layer: {layerIndex}");
                    foreach (var propertyInfo in slicerFile[layerIndex].GetType()
                                 .GetProperties(BindingFlags.Public | BindingFlags.Instance))
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