/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.CommandLine;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Cmd.Symbols;

public class AvailableFormat
{
    public string ClassName { get; init; } = string.Empty;
    public uint DefaultVersion { get; init; }
    public uint[] Versions { get; init; } = Array.Empty<uint>();
    public FileExtension[] Extensions { get; init; } = Array.Empty<FileExtension>();
    
    public FileFormat.FileFormatType FileType { get; init; }
    public FileFormat.FileImageType LayerImageType { get; init; }
    public Size[] ThumbnailsOriginalSize { get; init; } = Array.Empty<Size>();

    public string[] PrintParameterModifiers { get; init; } = Array.Empty<string>();
    public string[] PrintParameterPerLayerModifiers { get; init; } = Array.Empty<string>();

    public override string ToString()
    {
        return $"{nameof(ClassName)}: {ClassName}, {nameof(DefaultVersion)}: {DefaultVersion}, {nameof(Versions)}: [{string.Join(", ", Versions)}], {nameof(Extensions)}: [{string.Join(", ", Extensions.Select(extension => extension.Extension))}], {nameof(FileType)}: {FileType}, {nameof(LayerImageType)}: {LayerImageType}";
    }
}


internal static class PrintFormatsCommand
{
    internal static Command CreateCommand()
    {
        var jsonOption = new Option<bool>("--json", "Print in json format");
        //var xmlOption = new Option<bool>("--xml", "Print in xml format");

        var command = new Command("print-formats", "Prints the available formats")
        {
            jsonOption,
            //xmlOption
        };

        command.SetHandler((jsonFormat) =>
            {

                var formats = FileFormat.AvailableFormats.Select(slicerFile => new AvailableFormat
                    {
                        ClassName = slicerFile.GetType().Name,
                        DefaultVersion = slicerFile.DefaultVersion,
                        Versions = slicerFile.AvailableVersions,
                        Extensions = slicerFile.FileExtensions,
                        FileType = slicerFile.FileType,
                        LayerImageType = slicerFile.LayerImageType,
                        ThumbnailsOriginalSize = slicerFile.ThumbnailsOriginalSize,
                        PrintParameterModifiers = slicerFile.PrintParameterModifiers.Select(modifier => modifier.Name).ToArray(),
                        PrintParameterPerLayerModifiers = slicerFile.PrintParameterPerLayerModifiers.Select(modifier => modifier.Name).ToArray()
                    });


                if (jsonFormat)
                {
                    Console.WriteLine(JsonSerializer.Serialize(formats, JsonExtensions.SettingsIndent));
                    return;
                }

                foreach (var machine in formats)
                {
                    Console.WriteLine(machine);
                }


            }, jsonOption);

        return command;
    }
}