using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UVtools.Core;

namespace UVtools.Cmd
{
    class Program
    {
        public static async Task<int> Main(params string[] args)
        {
            Stopwatch sw = new Stopwatch();
            uint count = 0;
            var rootCommand = new RootCommand("MSLA/DLP, file analysis, repair, conversion and manipulation")
            {
                new Option(new []{"-f", "--file"}, "Input file to read")
                {
                    Required = true,
                    Argument = new Argument<FileSystemInfo>("filepath").ExistingOnly()
                },
                new Option(new []{"-o", "--output"}, "Output file to save the modifications, if aware, it saves to the same input file")
                {
                    Argument = new Argument<FileSystemInfo>("filepath")
                },
                new Option(new []{"-c", "--convert"}, "Converts input into a output file format by it extension")
                {
                    Argument = new Argument<FileSystemInfo>("filepath")
                },

                new Option(new []{"-p", "--properties"}, "Print a list of all properties/settings"),
                new Option(new []{"-i", "--issues"}, "Compute and print a list of all issues"),
                new Option(new []{"-r", "--repair"}, "Attempt to repair all issues"){
                    Argument = new Argument<int[]>("[start layer index] [end layer index]")
                },

                new Option(new []{"-mr", "--mut-resize"}, "Resizes layer images in a X and/or Y factor, starting from 100% value")
                {
                    Argument = new Argument<int[]>("[x%] [y%] [start layer index] [end layer index] [fade 0/1]")
                },
                new Option(new []{"-ms", "--mut-solidify"}, "Closes all inner holes")
                {
                    Argument = new Argument<int[]>("[start layer index] [end layer index]")
                },
                new Option(new []{"-me", "--mut-erode"}, "Erodes away the boundaries of foreground object")
                {
                    Argument = new Argument<int[]>("[start iterations] [end iterations] [start layer index] [end layer index] [fade 0/1]")
                },
                new Option(new []{"-md", "--mut-dilate"}, "It is just opposite of erosion")
                {
                    Argument = new Argument<int[]>("[start iterations] [end iterations] [start layer index] [end layer index] [fade 0/1]")
                },
                new Option(new []{"-mc", "--mut-close"}, "Dilation followed by Erosion")
                {
                    Argument = new Argument<int[]>("[start iterations] [end iterations] [start layer index] [end layer index] [fade 0/1]")
                },
                new Option(new []{"-mo", "--mut-open"}, "Erosion followed by Dilation")
                {
                    Argument = new Argument<int[]>("[start iterations] [end iterations] [start layer index] [end layer index] [fade 0/1]")
                },
                new Option(new []{"-mg", "--mut-gradient"}, "The difference between dilation and erosion of an image")
                {
                    Argument = new Argument<int[]>("[kernel size] [start layer index] [end layer index] [fade 0/1]")
                },
                
                new Option(new []{"-mpyr", "--mut-pyr"}, "Performs downsampling step of Gaussian pyramid decomposition")
                {
                    Argument = new Argument<int[]>("[start layer index] [end layer index]")
                },
                new Option(new []{"-mgb", "--mut-gaussian-blur"}, "Each pixel is a sum of fractions of each pixel in its neighborhood")
                {
                    Argument = new Argument<int[]>("[aperture] [sigmaX] [sigmaY]")
                },
                new Option(new []{"-mmb", "--mut-median-blur"}, "Each pixel becomes the median of its surrounding pixels")
                {
                    Argument = new Argument<ushort>("[aperture]")
                },

                
                

                /*new Option(new []{"-ls", "--layer-start"}, "Specify a start layer index to use with some operations as a range")
                {
                    Argument = new Argument<uint>("Layer index")
                },

                new Option(new []{"-le", "--layer-end"}, "Specify a end layer index to use with some operations as a range")
                {
                    Argument = new Argument<uint>("Layer index")
                },

                new Option(new []{"-is", "--iteration-start"}, "Specify a start layer index to use with some operations as a range")
                {
                    Argument = new Argument<uint>("Layer index")
                },

                new Option(new []{"-fade"}, "Fade a start value towards a end value to use with some operations")*/

            };

            rootCommand.Handler = CommandHandler.Create(
                (FileSystemInfo file, FileSystemInfo convert, bool properties, bool issues, bool repair, uint layerStartIndex, uint layerEndIndex) =>
            {
                Console.WriteLine($"Reading: {file}");

                var fileFormat = FileFormat.FindByExtension(file.FullName, true, true);
                if (ReferenceEquals(fileFormat, null))
                {
                    Console.WriteLine($"Error: {file.FullName} is not a known nor valid format.");
                }
                else
                {
                    fileFormat.Decode(file.FullName);
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

                if (issues)
                {
                    Console.WriteLine("Computing Issues, please wait.");
                    sw.Restart();
                    var issuesDict = fileFormat.LayerManager.GetAllIssues();
                    sw.Stop();
                    
                    Console.WriteLine("Issues:");
                    Console.WriteLine("----------------------");
                    count = 0;
                    for (uint layerIndex = 0; layerIndex < fileFormat.LayerCount; layerIndex++)
                    {
                        if(!issuesDict.TryGetValue(layerIndex, out var list)) continue;
                        foreach (var issue in list)
                        {
                            Console.WriteLine(issue);
                            count++;
                        }
                    }

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
                            fileFormat.Convert(fileConvert, convert.FullName);
                            sw.Stop();
                            Console.WriteLine($"Convertion done in {sw.ElapsedMilliseconds}ms");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        
                    }
                    
                }

                
            });


            await rootCommand.InvokeAsync(args);
            //await rootCommand.InvokeAsync("-f body_Tough0.1mm_SL1_5h16m_HOLLOW_DRAIN.sl1 -i");

            return 1;

        }
    }
}
