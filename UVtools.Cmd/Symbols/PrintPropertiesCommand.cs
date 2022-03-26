/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.CommandLine;
using System.IO;
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
        var command = new Command("print-properties", "Prints available properties")
        {
            GlobalArguments.InputFileArgument,

            new Option<string[]>(new []{ "-n", "--names"}, "Prints only the name matching properties"),
            new Option<bool>(new []{ "-b", "--base"}, "Prints only the base properties of the file"),
            GlobalOptions.OpenInPartialMode
        };

        command.SetHandler((FileInfo inputFile, string[] matchNames, bool baseOnly, bool partialMode) =>
            {
                var slicerFile = Program.OpenInputFile(inputFile, partialMode ? FileFormat.FileDecodeType.Partial : FileFormat.FileDecodeType.Full);
                uint count = 0;

                Console.WriteLine("Listing properties:");
                Console.WriteLine("----------------------");
                if (!baseOnly)
                {
                    foreach (var config in slicerFile.Configs)
                    {
                        //Program.WriteLine("******************************");
                        //Program.WriteLine($"\t{config.GetType().Name}");
                        //Program.WriteLine("******************************");
                        foreach (var propertyInfo in config.GetType()
                                     .GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            if (propertyInfo.Name.Equals("Item")) continue;
                            if (matchNames.Length > 0)
                            {
                                if(matchNames.All(s => s != propertyInfo.Name)) continue;
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

                var fileFormat = slicerFile as FileFormat;

                foreach (var propertyInfo in fileFormat.GetType()
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
                    count++;
                    Console.WriteLine($"{propertyInfo.Name}: {propertyInfo.GetValue(fileFormat)}");
                }


                Console.WriteLine("----------------------");
                Console.WriteLine($"Total properties: {count}");

            }, command.Arguments[0], command.Options[0], command.Options[1], command.Options[2]);

        return command;
    }
}