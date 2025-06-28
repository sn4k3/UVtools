/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Cmd.Symbols;

internal static class SetPropertiesCommand
{
    internal static Command CreateCommand()
    {
        var propertiesArgument = new Argument<string[]>("property=value")
        {
            Description = "Properties names and values to set",
        };

        var layerRangeOption = new Option<string>("-r", "--range")
        {
            Description = "Sets properties to the matching layer(s) index(es) in a range",
            HelpName = "startindex:endindex"
        };

        var layerIndexesOption = new Option<uint[]>("-i", "--indexes")
        {
            Description = "Sets properties to the matching layer(s) index(es)",
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

        command.SetAction(result =>
        {
            var inputFile = result.GetRequiredValue(GlobalArguments.InputFileArgument);
            var properties = result.GetRequiredValue(propertiesArgument);
            var layerRange = result.GetValue(layerRangeOption);
            var layerIndexes = result.GetValue(layerIndexesOption) ?? [];
            var outputFile = result.GetValue(GlobalOptions.OutputFile);

            var parsedProperties = ReflectionPropertyValue.ParseFromString(properties);

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
                setProperties += ReflectionPropertyValue.SetProperties(slicerFile, parsedProperties);
            }
            else
            {
                foreach (var layerIndex in layerIndexesList)
                {
                    var layer = slicerFile[layerIndex];
                    setProperties += ReflectionPropertyValue.SetProperties(layer, parsedProperties);
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
        });

        return command;
    }
}