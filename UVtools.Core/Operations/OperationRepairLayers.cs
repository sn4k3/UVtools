/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using EmguExtensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Managers;
using ZLinq;

namespace UVtools.Core.Operations;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public partial class OperationRepairLayers : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        var removeIslands = RepairIslands && RemoveIslandsBelowEqualPixelCount > 0;
        var attachIslands = RepairIslands && AttachIslandsBelowLayers > 0;
        var repairIssues = removeIslands || attachIslands || RepairResinTraps || RepairSuctionCups;
        List<MainIssue> issues = [];

        bool IsLayerSelected(uint layerIndex) =>
            layerIndex >= LayerIndexStart && layerIndex <= LayerIndexEnd;

        if (DetectIssues && repairIssues)
        {
            var config = IssuesDetectionConfig.Clone();
            config.DisableAll();
            config.IslandConfig.Enabled = removeIslands || attachIslands;
            config.ResinTrapConfig.Enabled = RepairResinTraps || RepairSuctionCups;
            config.ResinTrapConfig.DetectSuctionCups = RepairSuctionCups;

            if (config.IslandConfig.Enabled && LayerRangeCount < SlicerFile.LayerCount)
            {
                var configuredLayers = config.IslandConfig.WhiteListLayers?.ToHashSet();
                var selectedLayers = new List<uint>((int)LayerRangeCount);
                for (var layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
                {
                    if (configuredLayers is null || configuredLayers.Contains(layerIndex))
                        selectedLayers.Add(layerIndex);
                }

                config.IslandConfig.WhiteListLayers = selectedLayers;
            }

            issues = SlicerFile.IssueManager.DetectIssues(config, progress).ToList();
            progress.ThrowIfCancellationRequested();
            issues.RemoveAll(mainIssue => SlicerFile.IssueManager.IgnoredIssues.Contains(mainIssue));
        }
        else if (repairIssues)
        {
            issues = SlicerFile.IssueManager.GetVisible().ToList();
        }

        // Remove islands
        if (removeIslands && RemoveIslandsRecursiveIterations != 1)
        {
            progress.Reset("Removed recursive islands");
            var limit = RemoveIslandsRecursiveIterations == 0
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
                    recursiveIssues = SlicerFile.IssueManager.DetectIssues(config, progress);
                    progress.ThrowIfCancellationRequested();
                    //Debug.WriteLine(i);
                }

                var issuesGroup = IssueManager.GetIssuesBy(recursiveIssues, MainIssue.IssueType.Island)
                    .AsValueEnumerable()
                    .OfType<IssueOfPoints>()
                    .Where(issue =>
                        IsLayerSelected(issue.LayerIndex) &&
                        issue.PixelsCount <= RemoveIslandsBelowEqualPixelCount)
                    .GroupBy(issue => issue.LayerIndex)
                    .ToArray();

                if (issuesGroup.Length == 0) break;

                islandsToRecompute.Clear();
                Parallel.ForEach(issuesGroup, CoreSettings.GetParallelOptions(progress), group =>
                {
                    progress.PauseIfRequested();
                    var layer = SlicerFile[group.Key];
                    using var image = layer.LayerMat;
                    var span = image.GetSpanOfBytes();
                    foreach (var issue in group)
                    {
                        foreach (var issuePixel in issue.Points)
                        {
                            span[image.GetPixelPos(issuePixel)] = 0;
                        }

                        progress.LockAndIncrement();
                    }

                    var nextLayerIndex = group.Key + 1;
                    if (nextLayerIndex < SlicerFile.LayerCount && nextLayerIndex <= LayerIndexEnd)
                        islandsToRecompute.Add(nextLayerIndex);

                    layer.LayerMat = image;
                });

                if (i == 0)
                {
                    var removedIssues = issuesGroup
                        .SelectMany(group => group)
                        .Select(issue => issue.Parent)
                        .Where(parent => parent is not null)
                        .ToHashSet();
                    issues.RemoveAll(removedIssues.Contains);
                }

                if (islandsToRecompute.IsEmpty) break;
            }
        }

        if (attachIslands)
        {
            var issuesGroup = IssueManager.GetIssuesBy(issues, MainIssue.IssueType.Island)
                .AsValueEnumerable()
                .OfType<IssueOfPoints>()
                .Where(issue => IsLayerSelected(issue.LayerIndex) && issue.LayerIndex > LayerIndexStart)
                .GroupBy(issue => issue.LayerIndex)
                .ToArray();
            var issueCount = issuesGroup.Sum(group => group.Count());
            var attachedIssues = new ConcurrentBag<MainIssue>();

            progress.Reset("Attempt to attach islands below", (uint)issueCount);
            Parallel.ForEach(issuesGroup, CoreSettings.GetParallelOptions(progress), group =>
            {
                progress.PauseIfRequested();
                var matCache = new Dictionary<uint, Mat>();
                var modifiedLayers = new HashSet<uint>();
                var lockedLayers = new List<uint>();
                var firstLayerToFill = (long)group.Key - 1;
                var firstLayerToSearch = Math.Max(0L, (long)group.Key - 2);
                var lowestPossibleLayer = Math.Max(
                    LayerIndexStart,
                    group.Key > AttachIslandsBelowLayers + 1u
                        ? group.Key - AttachIslandsBelowLayers - 1u
                        : 0);

                try
                {
                    for (var layerIndex = group.Key;; layerIndex--)
                    {
                        SlicerFile[layerIndex].Mutex.Enter();
                        lockedLayers.Add(layerIndex);
                        matCache.Add(layerIndex, SlicerFile[layerIndex].LayerMat);
                        if (layerIndex == lowestPossibleLayer) break;
                    }

                    var sourceMat = matCache[group.Key];
                    var sourceSpan = sourceMat.GetReadOnlySpanOfBytes();
                    foreach (var issue in group)
                    {
                        var positions = new int[issue.Points.Length];
                        for (var index = 0; index < positions.Length; index++)
                        {
                            positions[index] = sourceMat.GetPixelPos(issue.Points[index]);
                        }

                        var foundAt = firstLayerToSearch == 0 && lowestPossibleLayer == 0 ? 0L : -1L;
                        var requiredSupportingPixels = Math.Max(1,
                            issue.PixelsCount *
                            IssuesDetectionConfig.IslandConfig.RequiredPixelsToSupportMultiplier);

                        for (var layerIndex = firstLayerToSearch;
                             layerIndex >= lowestPossibleLayer && foundAt < 0;
                             layerIndex--)
                        {
                            uint pixelsSupportingIsland = 0;
                            unsafe
                            {
                                var span = matCache[(uint)layerIndex].BytePointer;
                                foreach (var position in positions)
                                {
                                    if (span[position] <
                                        IssuesDetectionConfig.IslandConfig.RequiredPixelBrightnessToSupport)
                                        continue;

                                    pixelsSupportingIsland++;
                                    if (pixelsSupportingIsland < requiredSupportingPixels) continue;

                                    foundAt = layerIndex + 1L;
                                    break;
                                }
                            }
                        }

                        if (foundAt >= 0)
                        {
                            for (var layerIndex = firstLayerToFill; layerIndex >= foundAt; layerIndex--)
                            {
                                modifiedLayers.Add((uint)layerIndex);
                                unsafe
                                {
                                    var span = matCache[(uint)layerIndex].BytePointer;
                                    foreach (var position in positions)
                                    {
                                        span[position] =
                                            (byte)Math.Min(span[position] + sourceSpan[position], byte.MaxValue);
                                    }
                                }
                            }

                            if (issue.Parent is not null)
                                attachedIssues.Add(issue.Parent);
                        }

                        progress.LockAndIncrement();
                    }

                    foreach (var layerIndex in modifiedLayers)
                    {
                        SlicerFile[layerIndex].LayerMat = matCache[layerIndex];
                    }
                }
                finally
                {
                    foreach (var mat in matCache.Values)
                    {
                        mat.Dispose();
                    }

                    for (var index = lockedLayers.Count - 1; index >= 0; index--)
                    {
                        SlicerFile[lockedLayers[index]].Mutex.Exit();
                    }
                }
            });

            var attachedIssueSet = attachedIssues.ToHashSet();
            issues.RemoveAll(attachedIssueSet.Contains);
        }

        var islandsByLayer = removeIslands && RemoveIslandsRecursiveIterations == 1
            ? IssueManager.GetIssuesBy(issues, MainIssue.IssueType.Island)
                .AsValueEnumerable()
                .OfType<IssueOfPoints>()
                .Where(issue =>
                    IsLayerSelected(issue.LayerIndex) &&
                    issue.PixelsCount <= RemoveIslandsBelowEqualPixelCount)
                .GroupBy(issue => issue.LayerIndex)
                .ToDictionary(group => group.Key, group => group.ToArray())
            : [];
        var resinTrapsByLayer = RepairResinTraps
            ? IssueManager.GetIssuesBy(issues, MainIssue.IssueType.ResinTrap)
                .AsValueEnumerable()
                .OfType<IssueOfContours>()
                .Where(issue => IsLayerSelected(issue.LayerIndex))
                .GroupBy(issue => issue.LayerIndex)
                .ToDictionary(group => group.Key, group => group.ToArray())
            : [];
        var applyMorphology = RepairIslands && (GapClosingIterations > 0 || NoiseRemovalIterations > 0);

        if (islandsByLayer.Count > 0 || resinTrapsByLayer.Count > 0 || applyMorphology)
        {
            progress.Reset(ProgressAction, LayerRangeCount);
            Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                progress.PauseIfRequested();
                var layer = SlicerFile[layerIndex];
                Mat? image = null;

                void InitImage()
                {
                    image ??= layer.LayerMat;
                }

                try
                {
                    if (islandsByLayer.TryGetValue((uint)layerIndex, out var layerIslands))
                    {
                        InitImage();
                        var bytes = image!.GetSpanOfBytes();
                        foreach (var issue in layerIslands)
                        {
                            foreach (var issuePixel in issue.Points)
                            {
                                bytes[image!.GetPixelPos(issuePixel)] = 0;
                            }
                        }
                    }

                    if (resinTrapsByLayer.TryGetValue((uint)layerIndex, out var layerResinTraps))
                    {
                        InitImage();
                        foreach (var issue in layerResinTraps)
                        {
                            using var vec = new VectorOfVectorOfPoint(issue.Contours);
                            CvInvoke.DrawContours(image, vec, -1, EmguCvExtensions.WhiteColor, -1);
                            if (ResinTrapsOverlapBy > 0)
                            {
                                CvInvoke.DrawContours(image, vec, -1, EmguCvExtensions.WhiteColor,
                                    ResinTrapsOverlapBy * 2 + 1);
                            }
                        }
                    }

                    if (applyMorphology)
                    {
                        InitImage();

                        if (GapClosingIterations > 0)
                        {
                            CvInvoke.MorphologyEx(image, image, MorphOp.Close,
                                EmguCvExtensions.Kernel3X3Rectangle, EmguCvExtensions.AnchorCenter,
                                (int)GapClosingIterations, BorderType.Default, default);
                        }

                        if (NoiseRemovalIterations > 0)
                        {
                            CvInvoke.MorphologyEx(image, image, MorphOp.Open,
                                EmguCvExtensions.Kernel3X3Rectangle, EmguCvExtensions.AnchorCenter,
                                (int)NoiseRemovalIterations, BorderType.Default, default);
                        }
                    }

                    if (image is not null)
                    {
                        layer.LayerMat = image;
                    }
                }
                finally
                {
                    image?.Dispose();
                }

                progress.LockAndIncrement();
            });
        }

        if (RepairSuctionCups)
        {
            SlicerFile.IssueManager.DrillSuctionCupsForIssues(
                issues.Where(mainIssue =>
                    mainIssue.Type == MainIssue.IssueType.SuctionCup &&
                    IsLayerSelected(mainIssue.StartLayerIndex)),
                SuctionCupsVentHole,
                progress);
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
                if (removeLayers.Count == SlicerFile.LayerCount)
                {
                    removeLayers.RemoveAt(0);
                }

                OperationLayerRemove.RemoveLayers(SlicerFile, removeLayers, progress);
            }
        }

        return !progress.Token.IsCancellationRequested;
    }

    #endregion

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
        var removeIslands = RepairIslands && RemoveIslandsBelowEqualPixelCount > 0;
        var attachIslands = RepairIslands && AttachIslandsBelowLayers > 0;
        var applyMorphology = RepairIslands && (GapClosingIterations > 0 || NoiseRemovalIterations > 0);
        var repairIssues = removeIslands || attachIslands || RepairResinTraps || RepairSuctionCups;
        var repairWithoutIssues = applyMorphology || RemoveEmptyLayers;

        if (!repairIssues && !repairWithoutIssues)
        {
            sb.AppendLine("The selected settings do not perform any repair.");
        }

        if (IssuesDetectionConfig is null &&
            (DetectIssues && repairIssues ||
             removeIslands && RemoveIslandsRecursiveIterations != 1 ||
             attachIslands))
        {
            sb.AppendLine("An issues detection configuration is required for the selected repairs.");
        }

        if (GapClosingIterations > int.MaxValue || NoiseRemovalIterations > int.MaxValue)
        {
            sb.AppendLine($"Gap closing and noise removal iterations cannot exceed {int.MaxValue}.");
        }

        if (RepairSuctionCups && SuctionCupsVentHole < 4)
        {
            sb.AppendLine("The suction cup vent hole diameter must be at least 4 pixels.");
        }

        if (!DetectIssues && repairIssues && !repairWithoutIssues)
        {
            var hasRelevantIssues = SlicerFile.IssueManager.GetVisible().Any(mainIssue =>
                mainIssue.Type switch
                {
                    MainIssue.IssueType.Island when removeIslands || attachIslands =>
                        mainIssue.Any(issue =>
                            issue.LayerIndex >= LayerIndexStart && issue.LayerIndex <= LayerIndexEnd),
                    MainIssue.IssueType.ResinTrap when RepairResinTraps =>
                        mainIssue.Any(issue =>
                            issue.LayerIndex >= LayerIndexStart && issue.LayerIndex <= LayerIndexEnd),
                    MainIssue.IssueType.SuctionCup when RepairSuctionCups =>
                        mainIssue.StartLayerIndex >= LayerIndexStart && mainIssue.StartLayerIndex <= LayerIndexEnd,
                    _ => false
                });

            if (!hasRelevantIssues)
            {
                sb.AppendLine("There are no relevant detected issues in the selected layer range to repair.");
                sb.AppendLine(
                    "Detect issues first or enable \"Re-detect the selected issues before repair\".");
            }
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var repair = new List<string>();
        if (RepairIslands) repair.Add("Islands");
        if (RepairResinTraps) repair.Add("Resin traps");
        if (RepairSuctionCups) repair.Add("Suction cups");
        if (RemoveEmptyLayers) repair.Add("Empty layers");
        var result = $"[Repair: {string.Join('/', repair)}] [Detect: {DetectIssues}] " +
                     $"[Gap closing: {GapClosingIterations}px] " +
                     $"[Noise removal: {NoiseRemovalIterations}px]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Constructor

    public OperationRepairLayers()
    {
    }

    public OperationRepairLayers(FileFormat slicerFile) : base(slicerFile)
    {
    }

    #endregion

    #region Properties

    /// <summary>
    /// IF true it will re-detect the selected issues before repair, otherwise uses and repair the previous detected issues
    /// </summary>
    [ObservableProperty]
    public partial bool DetectIssues { get; set; }

    [ObservableProperty] public partial bool RepairIslands { get; set; } = true;

    [ObservableProperty] public partial bool RepairResinTraps { get; set; } = true;

    [ObservableProperty] public partial bool RepairSuctionCups { get; set; }

    [ObservableProperty] public partial bool RemoveEmptyLayers { get; set; } = true;

    [ObservableProperty] public partial ushort RemoveIslandsBelowEqualPixelCount { get; set; } = 5;

    [ObservableProperty] public partial ushort RemoveIslandsRecursiveIterations { get; set; } = 4;

    [ObservableProperty] public partial ushort AttachIslandsBelowLayers { get; set; } = 2;

    [ObservableProperty] public partial byte ResinTrapsOverlapBy { get; set; } = 5;

    [ObservableProperty] public partial byte SuctionCupsVentHole { get; set; } = 16;

    [ObservableProperty] public partial uint GapClosingIterations { get; set; } = 1;

    [ObservableProperty] public partial uint NoiseRemovalIterations { get; set; }

    public IssuesDetectionConfiguration IssuesDetectionConfig { get; set; } = new();

    #endregion

    #region Equality

    protected bool Equals(OperationRepairLayers other)
    {
        return DetectIssues == other.DetectIssues && RepairIslands == other.RepairIslands &&
               RepairResinTraps == other.RepairResinTraps && RepairSuctionCups == other.RepairSuctionCups &&
               RemoveEmptyLayers == other.RemoveEmptyLayers &&
               RemoveIslandsBelowEqualPixelCount == other.RemoveIslandsBelowEqualPixelCount &&
               RemoveIslandsRecursiveIterations == other.RemoveIslandsRecursiveIterations &&
               AttachIslandsBelowLayers == other.AttachIslandsBelowLayers &&
               ResinTrapsOverlapBy == other.ResinTrapsOverlapBy && SuctionCupsVentHole == other.SuctionCupsVentHole &&
               GapClosingIterations == other.GapClosingIterations &&
               NoiseRemovalIterations == other.NoiseRemovalIterations;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((OperationRepairLayers)obj);
    }

    #endregion
}
