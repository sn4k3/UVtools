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
        var sourceArgument = new Argument<string>($"file path|layer index|{RandomLayerArg}|{HeatmapArg}")
        {
            Description = "Choose from a file, layer index, random layer or generate a heatmap",
            DefaultValueFactory = _ => HeatmapArg
        };

        var thumbnailIndexesOption = new Option<byte[]>("-i", "--indexes")
        {
            Description = "Replaces only the matching thumbnail(s) index(es)",
            AllowMultipleArgumentsPerToken = true
        };

        var command = new Command("set-thumbnail", "Sets and replace thumbnail(s) in the file")
        {

            GlobalArguments.InputFileArgument,
            sourceArgument,
            thumbnailIndexesOption,
        };

        command.Aliases.Add("set-preview");


        command.SetAction(async result =>
        {
            var inputFile = result.GetRequiredValue(GlobalArguments.InputFileArgument);
            var source = result.GetValue(sourceArgument);
            var thumbnailIndexes = result.GetValue(thumbnailIndexesOption) ?? [];

            if (string.IsNullOrWhiteSpace(source))
            {
                Program.WriteLineError("Invalid empty source argument.");
            }

            bool success = false;

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
                        success = slicerFile.SetThumbnail(thumbnailIndex, mat);
                    }
                }
                else
                {
                    success = slicerFile.SetThumbnails(mat);
                }

                if (success) Program.SaveFile(slicerFile);
                return;
            }

            if (string.Equals(source, RandomLayerArg, StringComparison.OrdinalIgnoreCase))
            {
                var slicerFile = Program.OpenInputFile(inputFile);
                if (!slicerFile.HaveLayers) Program.WriteLineError("The file have no valid layers");


                using var matRoi = slicerFile[Random.Shared.Next((int)slicerFile.LayerCount)].GetLayerMatBoundingRectangle(50, 100);
                CvInvoke.CvtColor(matRoi.RoiMat, matRoi.RoiMat, ColorConversion.Gray2Bgr);

                if (thumbnailIndexes.Length > 0)
                {
                    foreach (var thumbnailIndex in thumbnailIndexes)
                    {
                        success = slicerFile.SetThumbnail(thumbnailIndex, matRoi.RoiMat);
                    }
                }
                else
                {
                    success = slicerFile.SetThumbnails(matRoi.RoiMat);
                }

                if (success) Program.SaveFile(slicerFile);
                return;
            }

            if (uint.TryParse(source, out var layerIndex))
            {
                var slicerFile = Program.OpenInputFile(inputFile);
                if (!slicerFile.HaveLayers) Program.WriteLineError("The file have no valid layers");
                slicerFile.SanitizeLayerIndex(ref layerIndex);

                using var matRoi = slicerFile[layerIndex].GetLayerMatBoundingRectangle(50, 100);
                CvInvoke.CvtColor(matRoi.RoiMat, matRoi.RoiMat, ColorConversion.Gray2Bgr);

                if (thumbnailIndexes.Length > 0)
                {
                    foreach (var thumbnailIndex in thumbnailIndexes)
                    {
                        success = slicerFile.SetThumbnail(thumbnailIndex, matRoi.RoiMat);
                    }
                }
                else
                {
                    success = slicerFile.SetThumbnails(matRoi.RoiMat);
                }

                if (success) Program.SaveFile(slicerFile);
                return;
            }

            if (File.Exists(source))
            {
                var slicerFile = Program.OpenInputFile(inputFile);

                if (thumbnailIndexes.Length > 0)
                {
                    foreach (var thumbnailIndex in thumbnailIndexes)
                    {
                        success = slicerFile.SetThumbnail(thumbnailIndex, source);
                    }
                }
                else
                {
                    success = slicerFile.SetThumbnails(source);
                }

                if (success) Program.SaveFile(slicerFile);
                return;
            }

            Program.WriteLineError($"'{source}' is not a file nor layer index nor {RandomLayerArg} nor {HeatmapArg}");
        });

        return command;
    }
}