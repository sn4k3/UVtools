/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using SixLabors.ImageSharp.Formats.Bmp;
using System;
using System.CommandLine;
using System.Text.Json;
using UVtools.Core.Extensions;
using UVtools.Core.Printer;

namespace UVtools.Cmd.Symbols;

internal static class PrintMachinesCommand
{
    internal static Command CreateCommand()
    {
        var jsonOption = new Option<bool>("--json")
        {
            Description = "Print in json format"
        };
        var xmlOption = new Option<bool>("--xml")
        {
            Description = "Print in xml format"
        };

        var command = new Command("print-machines", "Prints machine settings")
        {
            jsonOption,
            xmlOption
        };


        command.SetAction(result =>
        {
            var jsonFormat = result.GetValue(jsonOption);
            var xmlFormat = result.GetValue(xmlOption);

            if (jsonFormat)
            {
                Console.WriteLine(JsonSerializer.Serialize(Machine.Machines, JsonExtensions.SettingsIndent));
                return;
            }

            if (xmlFormat)
            {
                Console.WriteLine(XmlExtensions.SerializeToString(Machine.Machines, XmlExtensions.SettingsIndent));
                return;
            }

            foreach (var machine in Machine.Machines)
            {
                Console.WriteLine(machine);
            }
        });

        return command;
    }
}