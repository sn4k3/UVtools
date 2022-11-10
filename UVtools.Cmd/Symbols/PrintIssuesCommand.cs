/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.CommandLine;
using System.Linq;
using UVtools.Core.Layers;

namespace UVtools.Cmd.Symbols;

internal static class PrintIssuesCommand
{
    internal static Command CreateCommand()
    {
        var islandsOption = new Option<bool>(new []{ "-i" , "--islands"}, "Enable islands detection");
        var overhangsOption = new Option<bool>(new []{ "-o" , "--overhangs"}, "Enable overhangs detection");
        var resinTrapOption = new Option<bool>(new []{ "-r" , "--resin-traps"}, "Enable resin-traps detection");
        var suctionCupOption = new Option<bool>(new []{ "-s" , "--suction-cups"}, "Enable suction-cups detection");
        var touchingBoundsOption = new Option<bool>(new []{ "-t" , "--touching-bounds"}, "Enable touching bounds detection");
        var printHeightOption = new Option<bool>(new []{ "-p" , "--print-height"}, "Enable print height detection");
        var emptyLayersOption = new Option<bool>(new []{ "-e" , "--empty-layers"}, "Enable empty layer detection");

        var sortByAreaOption = new Option<bool>(new []{ "--sort-area"}, "Sort by area DESC");
        
        var command = new Command("print-issues", "Detect and print issues")
        {
            GlobalArguments.InputFileArgument,

            islandsOption,
            overhangsOption,
            resinTrapOption,
            suctionCupOption,
            touchingBoundsOption,
            printHeightOption,
            emptyLayersOption,

            sortByAreaOption
        };

        command.SetHandler(context =>
        {
            var inputFile = context.ParseResult.GetValueForArgument(GlobalArguments.InputFileArgument);
            var islands = context.ParseResult.GetValueForOption(islandsOption);
            var overhangs = context.ParseResult.GetValueForOption(overhangsOption);
            var resinTraps = context.ParseResult.GetValueForOption(resinTrapOption);
            var suctionCups = context.ParseResult.GetValueForOption(suctionCupOption);
            var touchingBounds = context.ParseResult.GetValueForOption(touchingBoundsOption);
            var printHeight = context.ParseResult.GetValueForOption(printHeightOption);
            var emptyLayers = context.ParseResult.GetValueForOption(emptyLayersOption);
            var sortByArea = context.ParseResult.GetValueForOption(sortByAreaOption);

            var slicerFile = Program.OpenInputFile(inputFile);

            var config = new IssuesDetectionConfiguration();

            if (islands || overhangs || resinTraps || suctionCups || touchingBounds || printHeight || emptyLayers)
            {
                config.DisableAll();
                config.IslandConfig.Enabled = islands;
                config.OverhangConfig.Enabled = overhangs;
                config.ResinTrapConfig.Enabled = resinTraps;
                config.ResinTrapConfig.DetectSuctionCups = suctionCups;
                config.TouchingBoundConfig.Enabled = touchingBounds;
                config.PrintHeightConfig.Enabled = printHeight;
                config.EmptyLayerConfig.Enabled = emptyLayers;
            }

            var issues = Program.ProgressBarWork("Detecting issues", () => slicerFile.IssueManager.DetectIssues(config, Program.Progress));
            if (sortByArea)
            {
                issues = issues.OrderBy(issue => issue.Type)
                    .ThenByDescending(issue => issue.Area)
                    .ThenBy(issue => issue.StartLayerIndex).ToList();
            }

            Console.WriteLine($"Issues: {issues.Count}");

            foreach (var issue in issues)
            {
                Console.WriteLine($"{issue.Type}, {issue.LayerInfoStr}, {issue.Area:F0}px{issue.AreaChar}, {issue.BoundingRectangle}");
            }
        });

        return command;
    }
}