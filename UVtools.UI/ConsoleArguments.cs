/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.Generic;
using System.IO;
using UVtools.Core.FileFormats;
using UVtools.Core.MeshFormats;
using UVtools.Core.Operations;
using UVtools.Core.SystemOS;
using ZLinq;

namespace UVtools.UI;

public static class ConsoleArguments
{
    /// <summary>
    /// Parse arguments from command line
    /// </summary>
    /// <param name="args"></param>
    /// <returns>True if is a valid argument, otherwise false</returns>
    public static bool ParseArgs(string[] args)
    {
        if(args.Length == 0) return false;

        if (args[0] is "--cmd" && args.Length > 1)
        {
            var newArgs = string.Join(' ', args[1..]);
            SystemAware.StartProcess(Path.Combine(App.ApplicationPath, SystemAware.GetExecutableName("UVtoolsCmd")), newArgs);
            return true;
        }

        // Convert to other file
        if (args[0] is "-c" or "--convert")
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Invalid syntax: <input_file> <output_file1/ext1> [output_file2/ext2]");
                return true;
            }
            if (!File.Exists(args[1]))
            {
                Console.WriteLine($"Input file does not exists: {args[1]}");
                return true;
            }

            var slicerFile = FileFormat.FindByExtensionOrFilePath(args[1], true);
            if (slicerFile is null)
            {
                Console.WriteLine($"Invalid input file: {args[1]}");
                return true;
            }


            var filenameNoExt = FileFormat.GetFileNameStripExtensions(args[1], out _);

            var filesToConvert = new List<KeyValuePair<FileFormat, string>>();

            for (int i = 2; i < args.Length; i++)
            {
                var outputFile = args[i];
                var targetFormat = FileFormat.FindByExtensionOrFilePath(args[i], true);
                if (targetFormat is null)
                {
                    Console.WriteLine($"Invalid output file/extension: {args[i]}");
                    continue;
                }

                if(targetFormat.IsExtensionValid(outputFile))
                    outputFile = $"{filenameNoExt}.{args[i]}";


                filesToConvert.Add(new KeyValuePair<FileFormat, string>(targetFormat, outputFile));
            }

            if (filesToConvert.Count == 0)
            {
                return true;
            }

            //var workingDir = Path.GetDirectoryName(args[1]);
            //if(!string.IsNullOrWhiteSpace(workingDir)) Directory.SetCurrentDirectory(workingDir);

            Console.WriteLine($"Loading file: {args[1]}");
            slicerFile.Decode(args[1]);

            foreach (var (outputSlicerFile, outputFile) in filesToConvert.AsValueEnumerable().DistinctBy(pair => pair.Value))
            {
                Console.WriteLine($"Converting to: {outputFile}");
                slicerFile.Convert(outputSlicerFile, outputFile);
                Console.WriteLine("Converted");

            }

            Console.WriteLine("OK");

            return true;
        }

        // Extract the file
        if (args[0] is "-e" or "--extract")
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Invalid syntax: <input_file> [output_folder]");
                return true;
            }
            if (!File.Exists(args[1]))
            {
                Console.WriteLine($"Input file does not exists: {args[1]}");
                return true;
            }

            var slicerFile = FileFormat.FindByExtensionOrFilePath(args[1], true);
            if (slicerFile is null)
            {
                Console.WriteLine($"Invalid input file: {args[1]}");
                return true;
            }

            var outputFolder = FileFormat.GetFileNameStripExtensions(args[1], out _);

            if (args.Length >= 3 && !string.IsNullOrWhiteSpace(args[2]))
            {
                try
                {
                    Path.GetFullPath(outputFolder);
                    outputFolder = args[2];
                }
                catch (Exception)
                {
                    Console.WriteLine($"Invalid output directory: {args[2]}");
                    return true;
                }
            }

            //var workingDir = Path.GetDirectoryName(args[1]);
            //if(!string.IsNullOrWhiteSpace(workingDir)) Directory.SetCurrentDirectory(workingDir);

            Console.WriteLine($"Loading file: {args[1]}");
            slicerFile.Decode(args[1]);
            Console.WriteLine($"Extracting to: {outputFolder}");
            slicerFile.Extract(outputFolder);
            Console.WriteLine("Extracted");
            Console.WriteLine("OK");
            return true;
        }

        // Convert to other file
        if (args[0] is "--export-mesh")
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Invalid syntax: <input_file> [output_mesh_file]");
                return true;
            }
            if (!File.Exists(args[1]))
            {
                Console.WriteLine($"Input file does not exists: {args[1]}");
                return true;
            }

            var slicerFile = FileFormat.FindByExtensionOrFilePath(args[1], true);
            if (slicerFile is null)
            {
                Console.WriteLine($"Invalid input file: {args[1]}");
                return true;
            }

            var outputFile = Path.Combine(Path.GetDirectoryName(args[1])!, $"{Path.GetFileNameWithoutExtension(args[1])}.stl");

            if (args.Length >= 3 && !string.IsNullOrWhiteSpace(args[2]))
            {
                outputFile = args[2];
            }

            var outputExtension = MeshFile.FindFileExtension(outputFile);
            if (outputExtension is null)
            {
                Console.WriteLine($"Invalid output file extension: {outputFile}");
                return true;
            }


            Console.WriteLine($"Loading file: {args[1]}");
            slicerFile.Decode(args[1]);

            Console.WriteLine($"Exporting mesh to: {outputFile}");
            var operation = new OperationLayerExportMesh(slicerFile)
            {
                FilePath = outputFile
            };
            operation.Execute();
            Console.WriteLine("Exported");

            Console.WriteLine("OK");

            return true;
        }

        // Run operation and save file
        if (args[0] is "--run-operation")
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Invalid syntax: <input_file> <operation_file.uvtop>");
                return true;
            }

            if (!File.Exists(args[1]))
            {
                Console.WriteLine($"Input file does not exists: {args[1]}");
                return true;
            }

            if (!File.Exists(args[2]))
            {
                Console.WriteLine($"Operation file does not exists: {args[2]}");
                return true;
            }

            var slicerFile = FileFormat.FindByExtensionOrFilePath(args[1], true);
            if (slicerFile is null)
            {
                Console.WriteLine($"Invalid input file: {args[1]}");
                return true;
            }

            var operation = Operation.Deserialize(args[2], slicerFile);

            if (operation is null)
            {
                Console.WriteLine($"Invalid operation file: {args[2]}");
                return true;
            }

            Console.WriteLine($"Loading file: {args[1]}");
            slicerFile.Decode(args[1]);

            Console.WriteLine($"Running operation: {operation.Title}");
            operation.Execute();
            slicerFile.Save();
            Console.WriteLine("Saved");
            Console.WriteLine("OK");

            return true;
        }

        // Run operation and save file
        if (args[0] is "--run-script")
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Invalid syntax: <input_file> <script_file.cs>");
                return true;
            }

            if (!File.Exists(args[1]))
            {
                Console.WriteLine($"Input file does not exists: {args[1]}");
                return true;
            }

            if (!File.Exists(args[2]) || !(args[2].EndsWith(".csx") || args[2].EndsWith(".cs")))
            {
                Console.WriteLine($"Script file does not exists or invalid: {args[2]}");
                return true;
            }

            var slicerFile = FileFormat.FindByExtensionOrFilePath(args[1], true);
            if (slicerFile is null)
            {
                Console.WriteLine($"Invalid input file: {args[1]}");
                return true;
            }

            Console.WriteLine($"Loading file: {args[1]}");
            slicerFile.Decode(args[1]);

            var operation = new OperationScripting(slicerFile);
            operation.ReloadScriptFromFile(args[2]);

            Console.WriteLine($"Running script: {operation.Title}");
            operation.Execute();
            slicerFile.Save();
            Console.WriteLine("Saved");
            Console.WriteLine("OK");

            return true;
        }

        // Run operation and save file
        if (args[0] is "--copy-parameters")
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Invalid syntax: <from_file> <to_file>");
                return true;
            }

            if (args[1] == args[2])
            {
                Console.WriteLine($"Source file must be different from target file");
                return true;
            }

            if (!File.Exists(args[1]))
            {
                Console.WriteLine($"Source file does not exists: {args[1]}");
                return true;
            }

            if (!File.Exists(args[2]))
            {
                Console.WriteLine($"Target file does not exists: {args[1]}");
                return true;
            }

            var fromFile = FileFormat.FindByExtensionOrFilePath(args[1], true);
            if (fromFile is null)
            {
                Console.WriteLine($"Invalid source file: {args[1]}");
                return true;
            }

            var toFile = FileFormat.FindByExtensionOrFilePath(args[2], true);
            if (toFile is null)
            {
                Console.WriteLine($"Invalid target file: {args[2]}");
                return true;
            }

            Console.WriteLine("Loading files");
            fromFile.Decode(args[1], FileFormat.FileDecodeType.Partial);
            toFile.Decode(args[2], FileFormat.FileDecodeType.Partial);

            var count = FileFormat.CopyParameters(fromFile, toFile);
            Console.WriteLine($"Modified parameters: {count}");
            if (count > 0)
            {
                toFile.Save();
                Console.WriteLine("Saved");
            }
            Console.WriteLine("OK");

            return true;
        }

        if (args[0] == "--crypt-ctb")
        {
            if (!File.Exists(args[1]))
            {
                Console.WriteLine($"Input file does not exists: {args[1]}");
                return true;
            }

            CTBEncryptedFile.CryptFile(args[1]);

            return true;
        }

        return false;
    }
}