/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UVtools.Core.FileFormats;
using static UVtools.Core.Extensions.ZipArchiveExtensions;

namespace UVtools.Cmd.Symbols;

internal static class ExtractCommand
{

    internal enum ExtractContentType
    {
        File,
        Thumbnails,
        Layers
    }

    internal static Command CreateCommand()
    {
        var noOverwriteOption = new Option<bool>("--no-overwrite")
        {
            Description = "If the output folder exists do not overwrite"
        };

        var contentTypeOption = new Option<ExtractContentType>("-c", "--content")
        {
            Description = "Set the type of content to extract",
            DefaultValueFactory = _ => ExtractContentType.File
        };

        var indexesOption = new Option<uint[]>("-i", "--index")
        {
            Description = "Sets the thumbnail or layer index to extract"
        };

        var rangeOption = new Option<string>("--range")
        {
            Description = "Sets the layer range to extract"
        };

        var command = new Command("extract", "Extract file contents to a folder")
        {
            GlobalArguments.InputFileArgument,
            GlobalArguments.OutputDirectoryArgument,
            noOverwriteOption,
            contentTypeOption,
            indexesOption,
            rangeOption
        };


        command.SetAction(result =>
        {
            var inputFile = result.GetRequiredValue(GlobalArguments.InputFileArgument);
            var outputDirectory = result.GetValue(GlobalArguments.OutputDirectoryArgument);
            var noOverwrite = result.GetValue(noOverwriteOption);
            var contentType = result.GetValue(contentTypeOption);
            var indexes = result.GetValue(indexesOption) ?? [];
            var range = result.GetValue(rangeOption) ?? string.Empty;

            var path = outputDirectory is null
                    ? Path.Combine(inputFile.DirectoryName!, Path.GetFileNameWithoutExtension(inputFile.Name))
                    : outputDirectory.FullName;

            if (noOverwrite && Directory.Exists(path))
            {
                Program.WriteLineError($"{path} already exits! --no-overwrite is enabled.");
                return;
            }

            var slicerFile = Program.OpenInputFile(inputFile, contentType == ExtractContentType.Thumbnails ? FileFormat.FileDecodeType.Partial : FileFormat.FileDecodeType.Full);

            Program.ProgressBarWork($"Extracting to {Path.GetFileName(path)}",
                () =>
                {
                    if (contentType == ExtractContentType.File)
                    {
                        slicerFile.Extract(path, progress: Program.Progress);
                    }
                    else
                    {
                        var indexesList = new List<uint>();

                        if (contentType == ExtractContentType.Layers)
                        {
                            if (!slicerFile.HaveLayers)
                            {
                                Program.WriteLineWarning("File have no valid layers to extract.");
                                return;
                            }

                            if (!string.IsNullOrWhiteSpace(range))
                            {
                                if (slicerFile.TryParseLayerIndexRange(range, out var layerIndexStart, out var layerIndexEnd))
                                {
                                    for (var layerIndex = layerIndexStart; layerIndex <= layerIndexEnd; layerIndex++)
                                    {
                                        indexesList.Add(layerIndex);
                                    }
                                }
                                else
                                {
                                    Program.WriteLineError($"The specified layer range '{range}' is malformed, use startindex:endindex with positive numbers");
                                }
                            }
                        }
                        else if (contentType == ExtractContentType.Thumbnails)
                        {
                            if (slicerFile.ThumbnailsCount == 0)
                            {
                                Program.WriteLineWarning("File have no valid thumbnails to extract.");
                                return;
                            }
                        }

                        if (indexes.Length == 0 && indexesList.Count == 0)
                        {
                            if (contentType == ExtractContentType.Layers)
                            {
                                for (uint i = 0; i < slicerFile.LayerCount; i++)
                                {
                                    indexesList.Add(slicerFile.SanitizeLayerIndex(i));
                                }
                            }
                            else if (contentType == ExtractContentType.Thumbnails)
                            {
                                for (uint i = 0; i < slicerFile.Thumbnails.Count; i++)
                                {
                                    indexesList.Add(i);
                                }
                            }
                        }
                        else
                        {
                            if (contentType == ExtractContentType.Layers)
                            {
                                foreach (var index in indexes)
                                {
                                    if (index < slicerFile.LayerCount)
                                    {
                                        indexesList.Add(index);
                                    }
                                }
                            }
                            else if (contentType == ExtractContentType.Thumbnails)
                            {
                                foreach (var index in indexes)
                                {
                                    if (index < slicerFile.ThumbnailsCount)
                                    {
                                        indexesList.Add(index);
                                    }
                                }
                            }
                        }

                        indexesList = indexesList.Distinct().OrderBy(layerIndex => layerIndex).ToList();
                        if (indexesList.Count == 0)
                        {
                            Program.WriteLineWarning("No valid indexes to extract.");
                            return;
                        }

                        Directory.CreateDirectory(path);

                        if (contentType == ExtractContentType.Layers)
                        {
                            Parallel.ForEach(indexesList, layerIndex =>
                            {
                                using var mat = slicerFile[layerIndex].LayerMat;
                                mat.Save(Path.Combine(path, slicerFile[layerIndex].Filename));
                            });
                        }
                        else if (contentType == ExtractContentType.Thumbnails)
                        {
                            foreach (var index in indexesList)
                            {
                                var thumbnail = slicerFile.Thumbnails[(int)index];
                                if (thumbnail.IsEmpty)
                                {
                                    continue;
                                }

                                thumbnail.Save(Path.Combine(path, $"Thumbnail{index}.png"));
                            }
                        }
                    }
                });
        });

        return command;
    }
}