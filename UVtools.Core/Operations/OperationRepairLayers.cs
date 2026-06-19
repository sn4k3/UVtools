/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using EmguExtensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Managers;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public partial class OperationRepairLayers : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Members

    #endregion

    #region Overrides
    public override bool CanROI => false;
    public override string IconClass => "Toolbox";
    public override string Title => "Repair layers and issues";
    public override string Description => string.Empty;

    public override string ConfirmationText => "attempt this repair?";

    public override string ProgressTitle =>
        $"Repairing layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressAction => "Repaired layers";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (!RepairIslands && !RepairResinTraps && !RepairSuctionCups && !RemoveEmptyLayers)
        {
            sb.AppendLine("You must select at least one repair operation.");
        }

        if (!DetectIssues && SlicerFile.IssueManager.Count == 0)
        {
            sb.AppendLine("There are no present issues on the current session to repair.");
            sb.AppendLine("Please detect issues before run this tool or check the option: \"Re-detect the selected issues before repair\" to force a detect and repair.");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var repair = new List<string>();
        if(RepairIslands) repair.Add("Islands");
        if(RepairResinTraps) repair.Add("Resin traps");
        if(RepairSuctionCups) repair.Add("Suction cups");
        if(RemoveEmptyLayers) repair.Add("Empty layers");
        var result = $"[Repair: {string.Join('/', repair)}] [Detect: {DetectIssues}]" +
                     $"[Gap closing: {GapClosingIterations}px] " +
                     $"[Noise removal: {NoiseRemovalIterations}px]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Constructor

    public OperationRepairLayers() { }

    public OperationRepairLayers(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Properties

    /// <summary>
    /// IF true it will re-detect the selected issues before repair, otherwise uses and repair the previous detected issues
    /// </summary>
    [ObservableProperty]
    public partial bool DetectIssues { get; set; }

    [ObservableProperty]
    public partial bool RepairIslands { get; set; } = true;

    [ObservableProperty]
    public partial bool RepairResinTraps { get; set; } = true;

    [ObservableProperty]
    public partial bool RepairSuctionCups { get; set; }

    [ObservableProperty]
    public partial bool RemoveEmptyLayers { get; set; } = true;

    [ObservableProperty]
    public partial ushort RemoveIslandsBelowEqualPixelCount { get; set; } = 5;

    [ObservableProperty]
    public partial ushort RemoveIslandsRecursiveIterations { get; set; } = 4;

    [ObservableProperty]
    public partial ushort AttachIslandsBelowLayers { get; set; } = 2;

    [ObservableProperty]
    public partial byte ResinTrapsOverlapBy { get; set; } = 5;

    [ObservableProperty]
    public partial byte SuctionCupsVentHole { get; set; } = 16;

    [ObservableProperty]
    public partial uint GapClosingIterations { get; set; } = 1;

    [ObservableProperty]
    public partial uint NoiseRemovalIterations { get; set; }

    public IssuesDetectionConfiguration IssuesDetectionConfig { get; set; } = new();

    #endregion

    #region Equality

    protected bool Equals(OperationRepairLayers other)
    {
        return DetectIssues == other.DetectIssues && RepairIslands == other.RepairIslands && RepairResinTraps == other.RepairResinTraps && RepairSuctionCups == other.RepairSuctionCups && RemoveEmptyLayers == other.RemoveEmptyLayers && RemoveIslandsBelowEqualPixelCount == other.RemoveIslandsBelowEqualPixelCount && RemoveIslandsRecursiveIterations == other.RemoveIslandsRecursiveIterations && AttachIslandsBelowLayers == other.AttachIslandsBelowLayers && ResinTrapsOverlapBy == other.ResinTrapsOverlapBy && SuctionCupsVentHole == other.SuctionCupsVentHole && GapClosingIterations == other.GapClosingIterations && NoiseRemovalIterations == other.NoiseRemovalIterations;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OperationRepairLayers) obj);
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        List<MainIssue> issues;

        if (DetectIssues)
        {
            var config = IssuesDetectionConfig.Clone();
            config.DisableAll();
            config.IslandConfig.Enabled =
                (RepairIslands && RemoveIslandsBelowEqualPixelCount > 0 && RemoveIslandsRecursiveIterations != 1) ||
                RepairIslands && AttachIslandsBelowLayers > 0;
            config.ResinTrapConfig.Enabled = RepairResinTraps;
            config.ResinTrapConfig.DetectSuctionCups = RepairSuctionCups;
            config.EmptyLayerConfig.Enabled = RemoveEmptyLayers;

            issues = SlicerFile.IssueManager.DetectIssues(config, progress).ToList();
            issues.RemoveAll(mainIssue => SlicerFile.IssueManager.IgnoredIssues.Contains(mainIssue));
        }
        else
        {
            issues = SlicerFile.IssueManager.GetVisible().ToList();
        }

        if (issues.Count == 0) return true;

        // Remove islands
        if (//Issues is not null
            //IslandDetectionConfig is not null
            RepairIslands
            && RemoveIslandsBelowEqualPixelCount > 0
            && RemoveIslandsRecursiveIterations != 1)
        {
            progress.Reset("Removed recursive islands");
            ushort limit = RemoveIslandsRecursiveIterations == 0
                ? ushort.MaxValue
                : RemoveIslandsRecursiveIterations;

            var recursiveIssues = issues;
            var islandsToRecompute = new ConcurrentBag<uint>();
            var config = IssuesDetectionConfig.Clone();
            config.DisableAll();
            config.IslandConfig.Enable();
            //islandConfig.RequiredAreaToProcessCheck = (ushort)(RemoveIslandsBelowEqualPixelCount / 2);

            for (uint i = 0; i < limit; i++)
            {
                if (i > 0)
                {
                    /*var whiteList = islandsToRecompute.GroupBy(u => u)
                        .Select(grp => grp.First())
                        .ToList();*/
                    config.IslandConfig.WhiteListLayers = islandsToRecompute.ToList();
                    recursiveIssues = SlicerFile.IssueManager.DetectIssues(config);
                    //Debug.WriteLine(i);
                }

                var issuesGroup = IssueManager.GetIssuesBy(recursiveIssues, MainIssue.IssueType.Island)
                    .Where(issue => issue.PixelsCount <= RemoveIslandsBelowEqualPixelCount)
                    .GroupBy(issue => issue.LayerIndex);


                if (!issuesGroup.Any()) break; // Nothing to process

                islandsToRecompute.Clear();
                Parallel.ForEach(issuesGroup, CoreSettings.GetParallelOptions(progress), group =>
                {
                    progress.PauseIfRequested();
                    var layer = SlicerFile[group.Key];
                    var image = layer.LayerMat;
                    var span = image.GetSpanOfBytes(0, 0);
                    foreach (IssueOfPoints issue in group)
                    {
                        foreach (var issuePixel in issue.Points)
                        {
                            span[image.GetPixelPos(issuePixel)] = 0;
                        }

                        progress.LockAndIncrement();
                    }

                    var nextLayerIndex = group.Key + 1;
                    if (nextLayerIndex < SlicerFile.LayerCount)
                        islandsToRecompute.Add(nextLayerIndex);

                    layer.LayerMat = image;
                });

                // Remove from main list due the replicate below repair
                issues.RemoveAll(mainIssue => mainIssue.Type == MainIssue.IssueType.Island && mainIssue.Area <= RemoveIslandsBelowEqualPixelCount);

                if (islandsToRecompute.IsEmpty) break; // No more leftovers
            }
        }

        if (RepairIslands && AttachIslandsBelowLayers > 0)
        {
            var islandsToProcess = issues;

            /*if (islandsToProcess.Count == 0)
            {
                var config = IssuesDetectionConfig.Clone();
                config.DisableAll();
                config.IslandConfig.Enable();

                islandsToProcess = SlicerFile.IssueManager.DetectIssues(config, progress);
                islandsToProcess.RemoveAll(mainIssue => SlicerFile.IssueManager.IgnoredIssues.Contains(mainIssue));
            }*/

            var issuesGroup = IssueManager.GetIssuesBy(islandsToProcess, MainIssue.IssueType.Island).GroupBy(issue => issue.LayerIndex);

            progress.Reset("Attempt to attach islands below", (uint) islandsToProcess.Count);
            Parallel.ForEach(issuesGroup, CoreSettings.GetParallelOptions(progress), group =>
            {
                progress.PauseIfRequested();
                using var mat = SlicerFile[group.Key].LayerMat;
                var matSpan = mat.GetReadOnlySpanOfBytes();
                var matCache = new Dictionary<uint, Mat>();
                var matCacheModified = new Dictionary<uint, bool>();
                var startLayer = Math.Max(0, (int)group.Key - 2);
                var lowestPossibleLayer = (uint)Math.Max(0, (int)group.Key - 1 - AttachIslandsBelowLayers);

                for (var layerIndex = startLayer+1; layerIndex >= lowestPossibleLayer; layerIndex--)
                {
                    //Debug.WriteLine(layerIndex);
                    Monitor.Enter(SlicerFile[layerIndex].Mutex);
                    matCache.Add((uint) layerIndex, SlicerFile[layerIndex].LayerMat);
                    matCacheModified.Add((uint) layerIndex, false);
                }

                foreach (IssueOfPoints issue in group)
                {
                    int foundAt = startLayer == 0 ? 0 : - 1;
                    var requiredSupportingPixels = Math.Max(1, issue.PixelsCount * IssuesDetectionConfig.IslandConfig.RequiredPixelsToSupportMultiplier);

                    for (var layerIndex = startLayer; layerIndex >= lowestPossibleLayer && foundAt < 0; layerIndex--)
                    {
                        uint pixelsSupportingIsland = 0;

                        unsafe
                        {
                            var span = matCache[(uint) layerIndex].BytePointer;

                            foreach (var point in issue.Points)
                            {
                                if (span[mat.GetPixelPos(point)] < IssuesDetectionConfig.IslandConfig.RequiredPixelBrightnessToSupport)
                                    continue;

                                pixelsSupportingIsland++;

                                if (pixelsSupportingIsland >= requiredSupportingPixels)
                                {
                                    foundAt = layerIndex + 1;
                                    break;
                                }
                            }

                        }
                    }

                    // Copy pixels
                    if (foundAt >= 0)
                    {
                        for (var layerIndex = startLayer + 1; layerIndex >= foundAt; layerIndex--)
                        {
                            matCacheModified[(uint) layerIndex] = true;
                            unsafe
                            {
                                var span = matCache[(uint) layerIndex].BytePointer;

                                foreach (var point in issue.Points)
                                {
                                    var pos = mat.GetPixelPos(point);
                                    span[pos] = (byte)Math.Min(span[pos] + matSpan[pos], byte.MaxValue);
                                }
                            }
                        }

                        lock (progress.Mutex)
                        {
                            // Remove from processed issues
                            issues.Remove(issue.Parent!);
                        }
                    }

                    progress.LockAndIncrement();
                }

                foreach (var dict in matCache)
                {
                    if (matCacheModified[dict.Key])
                    {
                        SlicerFile[dict.Key].LayerMat = dict.Value;
                    }
                    dict.Value.Dispose();
                    Monitor.Exit(SlicerFile[dict.Key].Mutex);
                }
            });
        }

        progress.Reset(ProgressAction, LayerRangeCount);
        if (RepairIslands || RepairResinTraps)
        {
            Parallel.For(LayerIndexStart, LayerIndexEnd, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                progress.PauseIfRequested();
                var layer = SlicerFile[layerIndex];
                Mat? image = null;

                void InitImage()
                {
                    image ??= layer.LayerMat;
                }

                if (issues.Count > 0)
                {
                    if (RepairIslands && RemoveIslandsBelowEqualPixelCount > 0 && RemoveIslandsRecursiveIterations == 1)
                    {
                        var bytes = Span<byte>.Empty;
                        foreach (IssueOfPoints issue in IssueManager.GetIssuesBy(issues, MainIssue.IssueType.Island, (uint)layerIndex))
                        {
                            if (issue.PixelsCount > RemoveIslandsBelowEqualPixelCount) continue;

                            InitImage();
                            if (bytes.IsEmpty) bytes = image!.GetSpanOfBytes(0, 0);

                            foreach (var issuePixel in issue.Points)
                            {
                                bytes[image!.GetPixelPos(issuePixel)] = 0;
                            }
                        }
                    }

                    if (RepairResinTraps)
                    {
                        foreach (IssueOfContours issue in IssueManager.GetIssuesBy(issues, MainIssue.IssueType.ResinTrap, (uint)layerIndex))
                        {
                            InitImage();
                            using var vec = new VectorOfVectorOfPoint(issue.Contours);
                            CvInvoke.DrawContours(image, vec, -1, EmguCvExtensions.WhiteColor, -1);
                            if (ResinTrapsOverlapBy > 0)
                            {
                                CvInvoke.DrawContours(image, vec, -1, EmguCvExtensions.WhiteColor, ResinTrapsOverlapBy * 2 + 1);
                            }
                        }
                    }
                }

                if (RepairIslands && (GapClosingIterations > 0 || NoiseRemovalIterations > 0))
                {
                    InitImage();

                    if (GapClosingIterations > 0)
                    {
                        CvInvoke.MorphologyEx(image, image, MorphOp.Close, EmguCvExtensions.Kernel3X3Rectangle, EmguCvExtensions.AnchorCenter,
                            (int)GapClosingIterations, BorderType.Default, default);
                    }

                    if (NoiseRemovalIterations > 0)
                    {
                        CvInvoke.MorphologyEx(image, image, MorphOp.Open, EmguCvExtensions.Kernel3X3Rectangle, EmguCvExtensions.AnchorCenter,
                            (int)NoiseRemovalIterations, BorderType.Default, default);
                    }
                }

                if (image is not null)
                {
                    layer.LayerMat = image;
                    image.Dispose();
                }

                progress.LockAndIncrement();
            });
        }


        if (RepairSuctionCups && issues.Count > 0)
        {
            SlicerFile.IssueManager.DrillSuctionCupsForIssues(issues.Where(mainIssue => mainIssue.Type == MainIssue.IssueType.SuctionCup), SuctionCupsVentHole, progress);
        }

        if (RemoveEmptyLayers)
        {
            var removeLayers = new List<uint>();
            for (var layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
            {
                if (SlicerFile[layerIndex].NonZeroPixelCount == 0)
                {
                    removeLayers.Add(layerIndex);
                }
            }

            if (removeLayers.Count > 0)
            {
                OperationLayerRemove.RemoveLayers(SlicerFile, removeLayers, progress);
            }
        }

        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}