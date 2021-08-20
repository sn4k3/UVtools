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
using System.Linq;
using MoreLinq;
using UVtools.Core.FileFormats;

namespace UVtools.WPF
{
    public static class ConsoleArguments
    {
        /// <summary>
        /// Parse arguments from command line
        /// </summary>
        /// <param name="args"></param>
        /// <returns>True if is a valid argument, otherwise false</returns>
        public static bool ParseArgs(string[] args)
        {
            if(args is null || args.Length == 0) return false;

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

                foreach (var (outputSlicerFile, outputFile) in filesToConvert.DistinctBy(pair => pair.Value))
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

            if (args[0] is "--crypt-ctb" or "--encrypt-ctb" or "--decrypt-ctb")
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
}