/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.CommandLine;
using UVtools.Core.FileFormats;

namespace UVtools.Cmd.Symbols;

internal static class PrintGCodeCommand
{
    internal static Command CreateCommand()
    {
        var command = new Command("print-gcode", "Prints the gcode of the file if available")
        {
            GlobalArguments.InputFileArgument,
        };
        
        command.SetAction(result =>
        {
            var inputFile = result.GetRequiredValue(GlobalArguments.InputFileArgument);

            var slicerFile = Program.OpenInputFile(inputFile, FileFormat.FileDecodeType.Partial);
            if (slicerFile.SupportGCode)
            {
                Console.WriteLine(slicerFile.GCodeStr);
            }
            else
            {
                Program.WriteLineWarning($"The file {inputFile.Name} do not support gcode");
            }
        });

        return command;
    }
}