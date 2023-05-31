/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.CommandLine;
using System.IO;

namespace UVtools.Cmd.Symbols;

internal static class SetThumbnailCommand
{
    private const string HeatmapArg = ":heatmap";
    private const string RandomLayerArg = ":random-layer";
    internal static Command CreateCommand()
    {
        var sourceArgument = new Argument<string>($"file path|layer index|{RandomLayerArg}|{HeatmapArg}", () => HeatmapArg, "Choose from a file, layer index, random layer or generate a heatmap");
        var thumbnailIndexesOption = new Option<byte[]>(new[] { "-i", "--indexes" }, "Prints only the matching thumbnail(s) index(es)")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var command = new Command("set-thumbnail", "Sets and replace thumbnail(s) in the file")
        {
            GlobalArguments.InputFileArgument,
            sourceArgument,
            thumbnailIndexesOption,
        };

        command.AddAlias("set-preview");

        command.SetHandler(async (inputFile, source, thumbnailIndexes) =>
            {
                if (string.IsNullOrWhiteSpace(source))
                {
                    Program.WriteLineError("Invalid empty source argument.");
                }

                bool result = false;

                if (string.Equals(source, HeatmapArg, StringComparison.OrdinalIgnoreCase))
                {
                    var slicerFile = Program.OpenInputFile(inputFile);
                    using var mat = await Program.ProgressBarWork($"Generating a heatmap from layers 0 through {slicerFile.LastLayerIndex}",
                        () => slicerFile.GenerateHeatmapAsync(slicerFile.GetBoundingRectangle(50, 100, Program.Progress), Program.Progress));

                    CvInvoke.CvtColor(mat, mat, ColorConversion.Gray2Bgr);


                    if (thumbnailIndexes.Length > 0)
                    {
                        foreach (var thumbnailIndex in thumbnailIndexes)
                        {
                            result = slicerFile.SetThumbnail(thumbnailIndex, mat);
                        }
                    }
                    else
                    {
                        result = slicerFile.SetThumbnails(mat);
                    }

                    if (result) Program.SaveFile(slicerFile);
                    return;
                }

                if (string.Equals(source, RandomLayerArg, StringComparison.OrdinalIgnoreCase))
                {
                    var slicerFile = Program.OpenInputFile(inputFile);
                    if (slicerFile.LayerCount == 0) Program.WriteLineError("The file have no valid layers");


                    using var matRoi = slicerFile[Random.Shared.Next((int)slicerFile.LayerCount)].GetLayerMatBoundingRectangle(50, 100);
                    CvInvoke.CvtColor(matRoi.RoiMat, matRoi.RoiMat, ColorConversion.Gray2Bgr);

                    if (thumbnailIndexes.Length > 0)
                    {
                        foreach (var thumbnailIndex in thumbnailIndexes)
                        {
                            result = slicerFile.SetThumbnail(thumbnailIndex, matRoi.RoiMat);
                        }
                    }
                    else
                    {
                        result = slicerFile.SetThumbnails(matRoi.RoiMat);
                    }

                    if (result) Program.SaveFile(slicerFile);
                    return;
                }

                if (uint.TryParse(source, out var layerIndex))
                {
                    var slicerFile = Program.OpenInputFile(inputFile);
                    if (slicerFile.LayerCount == 0) Program.WriteLineError("The file have no valid layers");
                    slicerFile.SanitizeLayerIndex(ref layerIndex);

                    using var matRoi = slicerFile[layerIndex].GetLayerMatBoundingRectangle(50, 100);
                    CvInvoke.CvtColor(matRoi.RoiMat, matRoi.RoiMat, ColorConversion.Gray2Bgr);

                    if (thumbnailIndexes.Length > 0)
                    {
                        foreach (var thumbnailIndex in thumbnailIndexes)
                        {
                            result = slicerFile.SetThumbnail(thumbnailIndex, matRoi.RoiMat);
                        }
                    }
                    else
                    {
                        result = slicerFile.SetThumbnails(matRoi.RoiMat);
                    }

                    if (result) Program.SaveFile(slicerFile);
                    return;
                }

                if (File.Exists(source))
                {
                    var slicerFile = Program.OpenInputFile(inputFile);

                    if (thumbnailIndexes.Length > 0)
                    {
                        foreach (var thumbnailIndex in thumbnailIndexes)
                        {
                            result = slicerFile.SetThumbnail(thumbnailIndex, source);
                        }
                    }
                    else
                    {
                        result = slicerFile.SetThumbnails(source);
                    }

                    if (result) Program.SaveFile(slicerFile);
                    return;
                }

                Program.WriteLineError($"'{source}' is not a file nor layer index nor {RandomLayerArg} nor {HeatmapArg}");


            }, GlobalArguments.InputFileArgument, sourceArgument, thumbnailIndexesOption);

        return command;
    }
}