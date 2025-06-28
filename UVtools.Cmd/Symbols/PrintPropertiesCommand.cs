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

internal static class PrintPropertiesCommand
{
    internal static Command CreateCommand()
    {
        var matchNamesOption = new Option<string[]>("-n", "--names")
        {
            Description = "Prints only the name matching properties",
            AllowMultipleArgumentsPerToken = true
        };

        var allPropertiesOption = new Option<bool>("-a", "--all")
        {
            Description = "Also prints the sub properties of the file (No effect on layers)"
        };

        var layerRangeOption = new Option<string>("-r", "--range")
        {
            Description = "Prints only the matching layer(s) index(es) in a range",
            HelpName = "startindex:endindex"
        };
        var layerIndexesOption = new Option<uint[]>("-i", "--indexes")
        {
            Description = "Prints only the matching layer(s) index(es)",
            AllowMultipleArgumentsPerToken = true
        };

        var command = new Command("print-properties", "Prints available properties")
        {
            GlobalArguments.InputFileArgument,
            matchNamesOption,
            allPropertiesOption,
            layerRangeOption,
            layerIndexesOption,
            GlobalOptions.OpenInPartialMode
        };

        command.SetAction(result =>
        {
            var inputFile = result.GetRequiredValue(GlobalArguments.InputFileArgument);
            var matchNames = result.GetValue(matchNamesOption) ?? [];
            var allProperties = result.GetValue(allPropertiesOption);
            var layerRange = result.GetValue(layerRangeOption);
            var layerIndexes = result.GetValue(layerIndexesOption) ?? [];
            var partialMode = result.GetValue(GlobalOptions.OpenInPartialMode);

            var slicerFile = Program.OpenInputFile(inputFile, partialMode ? FileFormat.FileDecodeType.Partial : FileFormat.FileDecodeType.Full);
            uint count = 0;

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

            if (layerIndexes.Length > 0)
            {
                layerIndexesList.AddRange(layerIndexes.Select(layerIndex => slicerFile.SanitizeLayerIndex(layerIndex)));
            }

            layerIndexesList = layerIndexesList.Distinct().OrderBy(layerIndex => layerIndex).ToList();
            Console.WriteLine("-------------------------");

            if (layerIndexesList.Count == 0)
            {
                if (allProperties)
                {
                    foreach (var config in slicerFile.Configs)
                    {
                        //Program.WriteLine("******************************");
                        //Program.WriteLine($"\t{config.GetType().Name}");
                        //Program.WriteLine("******************************");
                        foreach (var propertyInfo in config.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            if (propertyInfo.Name.Equals("Item")) continue;
                            if (matchNames.Length > 0)
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
                            count++;
                            Console.WriteLine($"{propertyInfo.Name}: {propertyInfo.GetValue(config)}");
                        }
                    }
                }

                //Program.WriteLine("******************************");
                //Program.WriteLine("\tBase");
                //Program.WriteLine("******************************");


                foreach (var propertyInfo in slicerFile.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
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
                    count++;
                    Console.WriteLine($"{propertyInfo.Name}: {propertyInfo.GetValue(slicerFile)}");
                }
            }
            else
            {
                for (var i = 0; i < layerIndexesList.Count; i++)
                {
                    var layerIndex = layerIndexesList[i];
                    if (i > 0) Console.WriteLine("-------------------------");
                    Console.WriteLine($"# Layer: {layerIndex}");
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
                        count++;
                    }
                }
            }

            Console.WriteLine("-------------------------");
            Console.WriteLine($"Total properties: {count}");
        });

        return command;
    }
}