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
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Cmd.Symbols;

internal static class SetPropertiesCommand
{
    internal static Command CreateCommand()
    {
        var propertiesArgument = new Argument<string[]>("property=value", "Properties names and values to set")
        {
            Arity = ArgumentArity.OneOrMore
        };
        var layerRangeOption = new Option<string>(new[] { "-r", "--range" }, "Sets properties to the matching layer(s) index(es) in a range")
        {
            ArgumentHelpName = "startindex:endindex"
        };
        var layerIndexesOption = new Option<uint[]>(new[] { "-i", "--indexes" }, "Sets properties to the matching layer(s) index(es)")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var command = new Command("set-properties", "Set properties in a file or to it layers with new values")
        {
            GlobalArguments.InputFileArgument,
            propertiesArgument,

            layerRangeOption,
            layerIndexesOption,
            GlobalOptions.OutputFile,
        };

        command.SetHandler((inputFile, properties, layerRange, layerIndexes, outputFile) =>
            {
                var parsedProperties = new List<ReflectionPropertyValue>(properties.Length);

                foreach (var property in properties)
                {
                    var split = property.Split(new[] { '=', ':' }, 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (split.Length < 2) continue;
                    parsedProperties.Add(new(split[0], split[1]));
                }

                if (parsedProperties.Count == 0)
                {
                    Program.WriteLineError("No properties to set.");
                }

                var slicerFile = Program.OpenInputFile(inputFile, FileFormat.FileDecodeType.Partial);

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
                uint setProperties = 0;

                if (layerIndexesList.Count == 0)
                {
                    foreach (var propertyInfo in slicerFile.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        foreach (var property in parsedProperties)
                        {
                            if (propertyInfo.Name != property.Name) continue;
                            propertyInfo.SetValueFromString(slicerFile, property.Value);
                            property.Found = true;
                            setProperties++;
                        }
                    }
                }
                else
                {
                    foreach (var layerIndex in layerIndexesList)
                    {
                        var layer = slicerFile[layerIndex];
                        foreach (var propertyInfo in layer.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            foreach (var property in parsedProperties)
                            {
                                if (propertyInfo.Name != property.Name) continue;
                                propertyInfo.SetValueFromString(layer, property.Value);
                                property.Found = true;
                                setProperties++;
                            }
                        }
                    }
                }


                foreach (var property in parsedProperties)
                {
                    if (property.Found) continue;
                    Program.WriteLineWarning($"Property {property.Name} was defined but not found nor set.");
                }

                if (setProperties <= 0) return;
                Program.WriteLine($"Properties set: {setProperties}");
                Program.SaveFile(slicerFile, outputFile);
            }, GlobalArguments.InputFileArgument, propertiesArgument, layerRangeOption, layerIndexesOption, GlobalOptions.OutputFile);

        return command;
    }
}