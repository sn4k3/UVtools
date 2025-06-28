/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.Linq;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using static Emgu.CV.Dai.OpenVino;
using static UVtools.Core.Extensions.ZipArchiveExtensions;

namespace UVtools.Cmd.Symbols;

internal static class ConvertCommand
{
    internal static Command CreateCommand()
    {
        var targetTypeArgument = new Argument<string>("target-type/ext")
        {
            Description = "Target format type or extension. Use 'auto' for SL1 files with specified FILEFORMAT_xxx"
        };

        var versionOption = new Option<ushort>("-v", "--version")
        {
            Description = "Sets the file format version"
        };

        var noOverwriteOption = new Option<bool>("--no-overwrite")
        {
            Description = "If the output file exists do not overwrite"
        };

        var command = new Command("convert", "Convert input file into a output file format by a known type or extension")
        {
            GlobalArguments.InputFileArgument,
            targetTypeArgument,
            GlobalArguments.OutputFileArgument,

            versionOption,
            noOverwriteOption,
        };

        command.SetAction(result =>
        {
            var inputFile = result.GetRequiredValue(GlobalArguments.InputFileArgument);
            var targetTypeExt = result.GetRequiredValue(targetTypeArgument);
            var outputFile = result.GetValue(GlobalArguments.OutputFileArgument);
            var version = result.GetValue(versionOption);
            var noOverwrite = result.GetValue(noOverwriteOption);

            if (string.Equals(targetTypeExt, "auto", StringComparison.InvariantCultureIgnoreCase))
            {
                using var testFile = Program.OpenInputFile(inputFile, FileFormat.FileDecodeType.Partial);
                string? convertFileExtension;
                switch (testFile)
                {
                    case SL1File sl1File:
                        convertFileExtension = sl1File.LookupCustomValue<string>(SL1File.Keyword_FileFormat, null);
                        break;
                    case VDTFile vdtFile:
                        if (string.IsNullOrWhiteSpace(vdtFile.ManifestFile.Machine.UVToolsConvertTo) || vdtFile.ManifestFile.Machine.UVToolsConvertTo == "None")
                            convertFileExtension = vdtFile.LookupCustomValue<string>(SL1File.Keyword_FileFormat, null);
                        else
                            convertFileExtension = vdtFile.ManifestFile.Machine.UVToolsConvertTo;
                        break;
                    default:
                        Program.WriteLineError($"The file '{testFile.Filename}' is not a valid candidate for auto conversion. Please specify the target format instead.");
                        return;
                }

                if (string.IsNullOrWhiteSpace(convertFileExtension))
                {
                    Program.WriteLineError($"The file '{testFile.Filename}' does not specify a target format, unable to guess. Please specify the target format instead.");
                    return;
                }

                convertFileExtension = convertFileExtension.ToLower(CultureInfo.InvariantCulture);
                var fileExtension = FileFormat.FindExtension(convertFileExtension);
                if (fileExtension is null)
                {
                    Program.WriteLineError($"Unable to find a valid target type from '{convertFileExtension}' extension.");
                }
                targetTypeExt = fileExtension!.GetFileFormat()!.GetType().Name;
                outputFile = new FileInfo(Path.Combine(testFile.DirectoryPath!, $"{testFile.FilenameNoExt}.{convertFileExtension}"));
            }

            var targetType = FileFormat.FindByType(targetTypeExt);
            if (targetType is null)
            {
                targetType = FileFormat.FindByExtensionOrFilePath(targetTypeExt, out var fileFormatsSharingExt);
                if (targetType is not null && fileFormatsSharingExt > 1)
                {
                    Program.WriteLineError($"The extension '{targetTypeExt}' is shared by multiple encoders, use the strict encoder name instead.", false);
                    Program.WriteLineError($"Available {FileFormat.AvailableFormats.Length} encoders:", false, false);
                    foreach (var fileFormat in FileFormat.AvailableFormats)
                    {
                        Program.WriteLineError($"{fileFormat.GetType().Name.RemoveFromEnd("File".Length)} ({string.Join(", ", fileFormat.FileExtensions.Select(extension => extension.Extension))})", false, false);
                    }
                    Environment.Exit(-1);
                    return;
                }
            }

            if (targetType is null)
            {
                Program.WriteLineError($"Unable to find a valid encoder from {targetTypeExt}.", false);
                Program.WriteLineError($"Available {FileFormat.AvailableFormats.Length} encoders:", false, false);
                foreach (var fileFormat in FileFormat.AvailableFormats)
                {
                    Program.WriteLineError($"{fileFormat.GetType().Name.RemoveFromEnd("File".Length)} ({string.Join(", ", fileFormat.FileExtensions.Select(extension => extension.Extension))})", false, false);
                }
                Environment.Exit(-1);
                return;
            }

            string? outputFilePath;
            string inputFileName = FileFormat.GetFileNameStripExtensions(inputFile.Name)!;
            if (outputFile is null)
            {
                outputFilePath = inputFileName;
                if (targetType.FileExtensions.Length == 1)
                {
                    outputFilePath = Path.Combine(inputFile.DirectoryName!, $"{outputFilePath}.{targetType.FileExtensions[0].Extension}");
                }
                else
                {
                    var ext = FileExtension.Find(targetTypeExt);
                    if (ext is null)
                    {
                        Program.WriteLineError($"Unable to construct the output filename and guess the extension from the {targetTypeExt} encoder.", false);
                        Program.WriteLineError($"There are {targetType.FileExtensions.Length} possible extensions on this format ({string.Join(", ", targetType.FileExtensions.Select(extension => extension.Extension))}), please specify an output file.", false, false);
                        return;
                    }

                    outputFilePath = Path.Combine(inputFile.DirectoryName!, $"{outputFilePath}.{ext.Extension}");
                }
            }
            else
            {
                outputFilePath = string.Format(outputFile.FullName, inputFileName);
            }

            var outputFileName = Path.GetFileName(outputFilePath);
            var outputFileDirectory = Path.GetDirectoryName(outputFilePath)!;

            if (outputFileName == string.Empty)
            {
                Program.WriteLineError("No output file was specified.");
                return;
            }

            if (!outputFileName.Contains('.'))
            {
                if (targetType.IsExtensionValid(outputFileName))
                {
                    outputFileName = $"{inputFileName}.{outputFileName}";
                    outputFilePath = Path.Combine(outputFileDirectory, outputFileName);
                }
                else if (targetType.FileExtensions.Length == 1)
                {
                    outputFileName = $"{outputFileName}.{targetType.FileExtensions[0].Extension}";
                    outputFilePath = Path.Combine(outputFileDirectory, outputFileName);
                }
            }

            if (!targetType.IsExtensionValid(outputFileName, true))
            {
                Program.WriteLineError($"The extension on '{outputFileName}' file is not valid for the {targetType.GetType().Name} encoder.", false);
                Program.WriteLineError($"Available {targetType.FileExtensions.Length} extension(s):", false, false);
                foreach (var fileExtension in targetType.FileExtensions)
                {
                    Program.WriteLineError(fileExtension.Extension, false, false);
                }

                Environment.Exit(-1);

                return;
            }

            if (noOverwrite && File.Exists(outputFilePath))
            {
                Program.WriteLineError($"{outputFileName} already exits! --no-overwrite is enabled.");
                return;
            }

            var slicerFile = Program.OpenInputFile(inputFile);

            Program.ProgressBarWork($"Converting to {outputFileName}",
                () =>
                {
                    try
                    {
                        return slicerFile.Convert(targetType, outputFilePath, version, Program.Progress);
                    }
                    catch (Exception)
                    {
                        File.Delete(outputFilePath);
                        throw;
                    }
                });
        });

        return command;
    }
}