/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
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


public class OperationRepairLayers : Operation
{
    #region Members
    private bool _detectIssues;
    private bool _repairIslands = true;
    private bool _repairResinTraps = true;
    private bool _repairSuctionCups;
    private bool _removeEmptyLayers = true;
    private ushort _removeIslandsBelowEqualPixelCount = 5;
    private ushort _removeIslandsRecursiveIterations = 4;
    private ushort _attachIslandsBelowLayers = 2;
    private byte _resinTrapsOverlapBy = 5;
    private byte _suctionCupsVentHole = 16;
    private uint _gapClosingIterations = 1;
    private uint _noiseRemovalIterations;

    #endregion

    #region Overrides
    public override bool CanROI => false;
    public override string IconClass => "fa-solid fa-toolbox";
    public override string Title => "Repair layers and issues";
    public override string Description => string.Empty;

    public override string ConfirmationText => "attempt this repair?";

    public override string ProgressTitle =>
        $"Repairing layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressAction => "Repaired layers";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (!_repairIslands && !_repairResinTraps && !_repairSuctionCups && !_removeEmptyLayers)
        {
            sb.AppendLine("You must select at least one repair operation.");
        }

        if (!_detectIssues && SlicerFile.IssueManager.Count == 0)
        {
            sb.AppendLine("There are no present issues on the current session to repair.");
            sb.AppendLine("Please detect issues before run this tool or check the option: \"Re-detect the selected issues before repair\" to force a detect and repair.");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var repair = new List<string>();
        if(_repairIslands) repair.Add("Islands");
        if(_repairResinTraps) repair.Add("Resin traps");
        if(_repairSuctionCups) repair.Add("Suction cups");
        if(_removeEmptyLayers) repair.Add("Empty layers");
        var result = $"[Repair: {string.Join('/', repair)}] [Detect: {_detectIssues}]" +
                     $"[Gap closing: {_gapClosingIterations}px] " +
                     $"[Noise removal: {_noiseRemovalIterations}px]" + LayerRangeString;
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
    public bool DetectIssues
    {
        get => _detectIssues;
        set => RaiseAndSetIfChanged(ref _detectIssues, value);
    }

    public bool RepairIslands
    {
        get => _repairIslands;
        set => RaiseAndSetIfChanged(ref _repairIslands, value);
    }

    public bool RepairResinTraps
    {
        get => _repairResinTraps;
        set => RaiseAndSetIfChanged(ref _repairResinTraps, value);
    }

    public bool RepairSuctionCups
    {
        get => _repairSuctionCups;
        set => RaiseAndSetIfChanged(ref _repairSuctionCups, value);
    }

    public bool RemoveEmptyLayers
    {
        get => _removeEmptyLayers;
        set => RaiseAndSetIfChanged(ref _removeEmptyLayers, value);
    }

    public ushort RemoveIslandsBelowEqualPixelCount
    {
        get => _removeIslandsBelowEqualPixelCount;
        set => RaiseAndSetIfChanged(ref _removeIslandsBelowEqualPixelCount, value);
    }

    public ushort RemoveIslandsRecursiveIterations
    {
        get => _removeIslandsRecursiveIterations;
        set => RaiseAndSetIfChanged(ref _removeIslandsRecursiveIterations, value);
    }

    public ushort AttachIslandsBelowLayers
    {
        get => _attachIslandsBelowLayers;
        set => RaiseAndSetIfChanged(ref _attachIslandsBelowLayers, value);
    }

    public byte ResinTrapsOverlapBy
    {
        get => _resinTrapsOverlapBy;
        set => RaiseAndSetIfChanged(ref _resinTrapsOverlapBy, value);
    }

    public byte SuctionCupsVentHole
    {
        get => _suctionCupsVentHole;
        set => RaiseAndSetIfChanged(ref _suctionCupsVentHole, value);
    }

    public uint GapClosingIterations
    {
        get => _gapClosingIterations;
        set => RaiseAndSetIfChanged(ref _gapClosingIterations, value);
    }

    public uint NoiseRemovalIterations
    {
        get => _noiseRemovalIterations;
        set => RaiseAndSetIfChanged(ref _noiseRemovalIterations, value);
    }

    public IssuesDetectionConfiguration IssuesDetectionConfig { get; set; } = new();

    #endregion

    #region Equality

    protected bool Equals(OperationRepairLayers other)
    {
        return _detectIssues == other._detectIssues && _repairIslands == other._repairIslands && _repairResinTraps == other._repairResinTraps && _repairSuctionCups == other._repairSuctionCups && _removeEmptyLayers == other._removeEmptyLayers && _removeIslandsBelowEqualPixelCount == other._removeIslandsBelowEqualPixelCount && _removeIslandsRecursiveIterations == other._removeIslandsRecursiveIterations && _attachIslandsBelowLayers == other._attachIslandsBelowLayers && _resinTrapsOverlapBy == other._resinTrapsOverlapBy && _suctionCupsVentHole == other._suctionCupsVentHole && _gapClosingIterations == other._gapClosingIterations && _noiseRemovalIterations == other._noiseRemovalIterations;
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

        if (_detectIssues)
        {
            var config = IssuesDetectionConfig.Clone();
            config.DisableAll();
            config.IslandConfig.Enabled =
                (_repairIslands && _removeIslandsBelowEqualPixelCount > 0 && _removeIslandsRecursiveIterations != 1) ||
                _repairIslands && _attachIslandsBelowLayers > 0;
            config.ResinTrapConfig.Enabled = _repairResinTraps;
            config.ResinTrapConfig.DetectSuctionCups = _repairSuctionCups;
            config.EmptyLayerConfig.Enabled = _removeEmptyLayers;

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
            _repairIslands
            && _removeIslandsBelowEqualPixelCount > 0
            && _removeIslandsRecursiveIterations != 1)
        {
            progress.Reset("Removed recursive islands");
            ushort limit = _removeIslandsRecursiveIterations == 0
                ? ushort.MaxValue
                : _removeIslandsRecursiveIterations;

            var recursiveIssues = issues;
            var islandsToRecompute = new ConcurrentBag<uint>();
            var config = IssuesDetectionConfig.Clone();
            config.DisableAll();
            config.IslandConfig.Enable();
            //islandConfig.RequiredAreaToProcessCheck = (ushort)(_removeIslandsBelowEqualPixelCount / 2);

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
                    var span = image.GetDataByteSpan();
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

        if (_repairIslands && _attachIslandsBelowLayers > 0)
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
            var sync = new object();
            Parallel.ForEach(issuesGroup, CoreSettings.GetParallelOptions(progress), group =>
            {
                progress.PauseIfRequested();
                using var mat = SlicerFile[group.Key].LayerMat;
                var matSpan = mat.GetDataByteReadOnlySpan();
                var matCache = new Dictionary<uint, Mat>();
                var matCacheModified = new Dictionary<uint, bool>();
                var startLayer = Math.Max(0, (int)group.Key - 2);
                var lowestPossibleLayer = (uint)Math.Max(0, (int)group.Key - 1 - _attachIslandsBelowLayers);
                    
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
                            var span = matCache[(uint) layerIndex].GetBytePointer();

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
                                var span = matCache[(uint) layerIndex].GetBytePointer();

                                foreach (var point in issue.Points)
                                {
                                    var pos = mat.GetPixelPos(point);
                                    span[pos] = (byte)Math.Min(span[pos] + matSpan[pos], byte.MaxValue);
                                }
                            }
                        }

                        lock (sync)
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
        if (_repairIslands || _repairResinTraps)
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
                    if (_repairIslands && _removeIslandsBelowEqualPixelCount > 0 && _removeIslandsRecursiveIterations == 1)
                    {
                        Span<byte> bytes = null;
                        foreach (IssueOfPoints issue in IssueManager.GetIssuesBy(issues, MainIssue.IssueType.Island, (uint)layerIndex))
                        {
                            if (issue.PixelsCount > _removeIslandsBelowEqualPixelCount) continue;

                            InitImage();
                            if (bytes == null) bytes = image!.GetDataByteSpan();

                            foreach (var issuePixel in issue.Points)
                            {
                                bytes[image!.GetPixelPos(issuePixel)] = 0;
                            }
                        }
                    }

                    if (_repairResinTraps)
                    {
                        foreach (IssueOfContours issue in IssueManager.GetIssuesBy(issues, MainIssue.IssueType.ResinTrap, (uint)layerIndex))
                        {
                            InitImage();
                            using var vec = new VectorOfVectorOfPoint(issue.Contours);
                            CvInvoke.DrawContours(image, vec, -1, EmguExtensions.WhiteColor, -1);
                            if (_resinTrapsOverlapBy > 0)
                            {
                                CvInvoke.DrawContours(image, vec, -1, EmguExtensions.WhiteColor, _resinTrapsOverlapBy * 2 + 1);
                            }
                        }
                    }
                }

                if (_repairIslands && (_gapClosingIterations > 0 || _noiseRemovalIterations > 0))
                {
                    InitImage();

                    if (_gapClosingIterations > 0)
                    {
                        CvInvoke.MorphologyEx(image, image, MorphOp.Close, EmguExtensions.Kernel3x3Rectangle, EmguExtensions.AnchorCenter,
                            (int)_gapClosingIterations, BorderType.Default, default);
                    }

                    if (_noiseRemovalIterations > 0)
                    {
                        CvInvoke.MorphologyEx(image, image, MorphOp.Open, EmguExtensions.Kernel3x3Rectangle, EmguExtensions.AnchorCenter,
                            (int)_noiseRemovalIterations, BorderType.Default, default);
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


        if (_repairSuctionCups && issues.Count > 0)
        {
            SlicerFile.IssueManager.DrillSuctionCupsForIssues(issues.Where(mainIssue => mainIssue.Type == MainIssue.IssueType.SuctionCup), _suctionCupsVentHole, progress);
        }

        if (_removeEmptyLayers)
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