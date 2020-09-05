using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;

namespace UVtools.Cmd
{
    class Program
    {
        public static async Task<int> Main(params string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
            OperationProgress progress = new OperationProgress();
            Stopwatch sw = new Stopwatch();
            uint count = 0;
            var rootCommand = new RootCommand("MSLA/DLP, file analysis, repair, conversion and manipulation")
            {
                new Option(new []{"-f", "--file"}, "Input file to read")
                {
                    IsRequired = true,
                    Argument = new Argument<FileSystemInfo>("filepath").ExistingOnly()
                },
                new Option(new []{"-o", "--output"}, "Output file to save the modifications, if aware, it saves to the same input file")
                {
                    Argument = new Argument<FileSystemInfo>("filepath")
                },
                new Option(new []{"-e", "--extract"}, "Extract file content to a folder")
                {
                    Argument = new Argument<DirectoryInfo>("folder")
                },
                new Option(new []{"-c", "--convert"}, "Converts input into a output file format by it extension")
                {
                    Argument = new Argument<FileSystemInfo>("filepath"),
                },

                new Option(new []{"-p", "--properties"}, "Print a list of all properties/settings"),
                new Option(new []{"-gcode"}, "Print the GCode if available"),
                new Option(new []{"-i", "--issues"}, "Compute and print a list of all issues"),
                new Option(new []{"-r", "--repair"}, "Attempt to repair all issues"){
                    Argument = new Argument<int[]>("[start layer index] [end layer index] [islands 0/1] [remove empty layers 0/1] [resin traps 0/1]"),
                },

                new Option(new []{"-mr", "--mut-resize"}, "Resizes layer images in a X and/or Y factor, starting from 100% value")
                {
                    Argument = new Argument<decimal[]>("[x%] [y%] [start layer index] [end layer index] [fade 0/1]")
                },
                new Option(new []{"-ms", "--mut-solidify"}, "Closes all inner holes")
                {
                    Argument = new Argument<uint[]>("[start layer index] [end layer index]")
                },
                new Option(new []{"-me", "--mut-erode"}, "Erodes away the boundaries of foreground object")
                {
                    Argument = new Argument<uint[]>("[start iterations] [end iterations] [start layer index] [end layer index] [fade 0/1]")
                },
                new Option(new []{"-md", "--mut-dilate"}, "It is just opposite of erosion")
                {
                    Argument = new Argument<uint[]>("[start iterations] [end iterations] [start layer index] [end layer index] [fade 0/1]")
                },
                new Option(new []{"-mc", "--mut-close"}, "Dilation followed by Erosion")
                {
                    Argument = new Argument<uint[]>("[start iterations] [end iterations] [start layer index] [end layer index] [fade 0/1]")
                },
                new Option(new []{"-mo", "--mut-open"}, "Erosion followed by Dilation")
                {
                    Argument = new Argument<uint[]>("[start iterations] [end iterations] [start layer index] [end layer index] [fade 0/1]")
                },
                new Option(new []{"-mg", "--mut-gradient"}, "The difference between dilation and erosion of an image")
                {
                    Argument = new Argument<uint[]>("[kernel size] [start layer index] [end layer index] [fade 0/1]")
                },
                
                new Option(new []{"-mpy", "--mut-py"}, "Performs down-sampling step of Gaussian pyramid decomposition")
                {
                    Argument = new Argument<uint[]>("[start layer index] [end layer index]")
                },
                new Option(new []{"-mgb", "--mut-gaussian-blur"}, "Each pixel is a sum of fractions of each pixel in its neighborhood")
                {
                    Argument = new Argument<ushort[]>("[aperture] [sigmaX] [sigmaY]")
                },
                new Option(new []{"-mmb", "--mut-median-blur"}, "Each pixel becomes the median of its surrounding pixels")
                {
                    Argument = new Argument<ushort>("[aperture]")
                },
            };

            rootCommand.Handler = CommandHandler.Create(
                (
                    FileSystemInfo file,
                    FileSystemInfo convert,
                    DirectoryInfo extract,
                    bool properties,
                    bool gcode,
                    bool issues,
                    int[] repair
                    //decimal[] mutResize
                    ) =>
            {
                var fileFormat = FileFormat.FindByExtension(file.FullName, true, true);
                if (ReferenceEquals(fileFormat, null))
                {
                    Console.WriteLine($"Error: {file.FullName} is not a known nor valid format.");
                }
                else
                {
                    Console.Write($"Reading: {file}");
                    sw.Restart();
                    fileFormat.Decode(file.FullName, progress);
                    sw.Stop();
                    Console.WriteLine($", in {sw.ElapsedMilliseconds}ms");
                    Console.WriteLine("----------------------");
                    Console.WriteLine($"Layers: {fileFormat.LayerCount} x {fileFormat.LayerHeight}mm = {fileFormat.TotalHeight}mm");
                    Console.WriteLine($"Resolution: {new Size((int) fileFormat.ResolutionX, (int) fileFormat.ResolutionY)}");
                    Console.WriteLine($"AntiAlias: {fileFormat.ValidateAntiAliasingLevel()}");
                    
                    Console.WriteLine($"Bottom Layer Count: {fileFormat.InitialLayerCount}");
                    Console.WriteLine($"Bottom Exposure Time: {fileFormat.InitialExposureTime}s");
                    Console.WriteLine($"Layer Exposure Time: {fileFormat.LayerExposureTime}s");
                    Console.WriteLine($"Print Time: {fileFormat.PrintTime}s");
                    Console.WriteLine($"Cost: {fileFormat.MaterialCost}$");
                    Console.WriteLine($"Resin Name: {fileFormat.MaterialName}");
                    Console.WriteLine($"Machine Name: {fileFormat.MachineName}");

                    Console.WriteLine($"Thumbnails: {fileFormat.CreatedThumbnailsCount}");
                    Console.WriteLine("----------------------");
                }

                if (!ReferenceEquals(extract, null))
                {
                    Console.Write($"Extracting to {extract.FullName}");
                    sw.Restart();
                    fileFormat.Extract(extract.FullName, true, true, progress);
                    sw.Stop();
                    Console.WriteLine($", finished in {sw.ElapsedMilliseconds}ms");
                }

                if (properties)
                {
                    count = 0;
                    Console.WriteLine("Listing all properties:");
                    Console.WriteLine("----------------------");
                    foreach (var config in fileFormat.Configs)
                    {
                        Console.WriteLine("******************************");
                        Console.WriteLine($"\t{config.GetType().Name}");
                        Console.WriteLine("******************************");
                        foreach (PropertyInfo propertyInfo in config.GetType()
                            .GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            count++;
                            if (propertyInfo.Name.Equals("Item")) continue;
                            Console.WriteLine($"{propertyInfo.Name}: {propertyInfo.GetValue(config)}");
                        }
                    }

                    Console.WriteLine("----------------------");
                    Console.WriteLine($"Total properties: {count}");
                }

                if (gcode)
                {
                    if (ReferenceEquals(fileFormat.GCode, null))
                    {
                        Console.WriteLine("No GCode available");
                    }
                    else
                    {
                        Console.WriteLine("----------------------");
                        Console.WriteLine(fileFormat.GCode);
                        Console.WriteLine("----------------------");
                        Console.WriteLine($"Total lines: {fileFormat.GCode.Length}");
                    }
                    
                }

                if (issues)
                {
                    Console.WriteLine("Computing Issues, please wait.");
                    sw.Restart();
                    var issueList = fileFormat.LayerManager.GetAllIssues(null, null, null, true, progress);
                    sw.Stop();
                    
                    Console.WriteLine("Issues:");
                    Console.WriteLine("----------------------");
                    count = 0;
                    foreach (var issue in issueList)
                    {
                        Console.WriteLine(issue);
                        count++;
                    }
                    /*for (uint layerIndex = 0; layerIndex < fileFormat.LayerCount; layerIndex++)
                    {
                        if(!issuesDict.TryGetValue(layerIndex, out var list)) continue;
                        foreach (var issue in list)
                        {
                            Console.WriteLine(issue);
                            count++;
                        }
                    }*/

                    Console.WriteLine("----------------------");
                    Console.WriteLine($"Total Issues: {count} in {sw.ElapsedMilliseconds}ms");
                }

                if (!ReferenceEquals(convert, null))
                {
                    var fileConvert = FileFormat.FindByExtension(convert.FullName, true, true);
                    if (ReferenceEquals(fileFormat, null))
                    {
                        Console.WriteLine($"Error: {convert.FullName} is not a known nor valid format.");
                    }
                    else
                    {
                        Console.WriteLine($"Converting {fileFormat.GetType().Name} to {fileConvert.GetType().Name}: {convert.Name}");

                        try
                        {
                            sw.Restart();
                            fileFormat.Convert(fileConvert, convert.FullName, progress);
                            sw.Stop();
                            Console.WriteLine($"Convertion done in {sw.ElapsedMilliseconds}ms");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        
                    }
                    
                }

                if (!ReferenceEquals(repair, null))
                {
                    uint layerStartIndex = (uint) (repair.Length >= 1 ? Math.Max(0, repair[0]) : 0);
                    uint layerEndIndex = repair.Length >= 2 ? (uint) repair[1].Clamp(0, (int) (fileFormat.LayerCount - 1)) : fileFormat.LayerCount-1;
                    bool repairIslands = repair.Length < 3 || repair[2] > 0 || repair[2] < 0;
                    bool removeEmptyLayers = repair.Length < 4 || repair[3] > 0 || repair[3] < 0;
                    bool repairResinTraps = repair.Length < 5 || repair[4] > 0 || repair[4] < 0;

                    fileFormat.LayerManager.RepairLayers(layerStartIndex, layerEndIndex, 2, 1, 4, repairIslands, removeEmptyLayers, repairResinTraps, null, progress);
                }
                
            });


            //await rootCommand.InvokeAsync(args);
            await rootCommand.InvokeAsync("-f body_Tough0.1mm_SL1_5h16m_HOLLOW_DRAIN.sl1 -r -1");

            return 1;

        }
    }
}
