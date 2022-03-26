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
using System.Text.Json;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Printer;

namespace UVtools.Cmd.Symbols;

internal static class PrintMachinesCommand
{
    internal static Command CreateCommand()
    {
        var command = new Command("print-machines", "Prints machine settings")
        {
            new Option<bool>("--json", "Print in json format"),
            new Option<bool>("--xml", "Print in xml format")
        };

        command.SetHandler((bool jsonFormat, bool xmlFormat) =>
            {
                if (jsonFormat)
                {
                    Console.WriteLine(JsonSerializer.Serialize(Machine.Machines, JsonExtensions.SettingsIndent));
                    return;
                }

                if (xmlFormat)
                {
                    Console.WriteLine(XmlExtensions.SerializeObject(Machine.Machines, XmlExtensions.SettingsIndent));
                    return;
                }

                foreach (var machine in Machine.Machines)
                {
                    Console.WriteLine(machine);
                }


            }, command.Options[0], command.Options[1]);

        return command;
    }
}