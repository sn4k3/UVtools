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
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationRepairLayers : Operation
    {
        #region Members
        private bool _repairIslands = true;
        private bool _repairResinTraps = true;
        private bool _removeEmptyLayers = true;
        private ushort _removeIslandsBelowEqualPixelCount = 5;
        private ushort _removeIslandsRecursiveIterations = 4;
        private ushort _attachIslandsBelowLayers = 2;
        private uint _gapClosingIterations = 1;
        private uint _noiseRemovalIterations;

        #endregion

        #region Overrides
        public override bool CanROI => false;
        public override string Title => "Repair layers and issues";
        public override string Description => null;

        public override string ConfirmationText => "attempt  this repair?";

        public override string ProgressTitle =>
            $"Reparing layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Repaired layers";

        public override bool CanHaveProfiles => false;

        public override string ValidateInternally()
        {
            var sb = new StringBuilder();

            if (!RepairIslands && !RemoveEmptyLayers && !RepairResinTraps)
            {
                sb.AppendLine("You must select at least one repair operation.");
            }

            return sb.ToString();
        }
        #endregion

        #region Constructor

        public OperationRepairLayers() { }

        public OperationRepairLayers(FileFormat slicerFile) : base(slicerFile) { }

        #endregion

        #region Properties
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

        [XmlIgnore]
        public IslandDetectionConfiguration IslandDetectionConfig { get; set; }

        [XmlIgnore]
        public List<LayerIssue> Issues { get; set; }
        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            // Remove islands
            if (Issues is not null
                && IslandDetectionConfig is not null
                && _repairIslands
                && _removeIslandsBelowEqualPixelCount > 0
                && _removeIslandsRecursiveIterations != 1)
            {
                progress.Reset("Removed recursive islands");
                ushort limit = _removeIslandsRecursiveIterations == 0
                    ? ushort.MaxValue
                    : _removeIslandsRecursiveIterations;

                var recursiveIssues = Issues;
                var islandsToRecompute = new ConcurrentBag<uint>();
                var islandConfig = IslandDetectionConfig.Clone();
                var overhangConfig = new OverhangDetectionConfiguration(false);
                var touchingBoundsConfig = new TouchingBoundDetectionConfiguration(false);
                var printHeightConfig = new PrintHeightDetectionConfiguration(false);
                var resinTrapsConfig = new ResinTrapDetectionConfiguration(false);
                var emptyLayersConfig = false;

                islandConfig.Enabled = true;
                islandConfig.RequiredAreaToProcessCheck = (ushort)(_removeIslandsBelowEqualPixelCount / 2);

                for (uint i = 0; i < limit; i++)
                {
                    if (i > 0)
                    {
                        /*var whiteList = islandsToRecompute.GroupBy(u => u)
                            .Select(grp => grp.First())
                            .ToList();*/
                        islandConfig.WhiteListLayers = islandsToRecompute.ToList();
                        recursiveIssues = SlicerFile.LayerManager.GetAllIssues(islandConfig, overhangConfig, resinTrapsConfig, touchingBoundsConfig, printHeightConfig, emptyLayersConfig);
                        //Debug.WriteLine(i);
                    }

                    var issuesGroup =
                        recursiveIssues
                        .Where(issue => issue.Type == LayerIssue.IssueType.Island &&
                                        issue.Pixels.Length <= RemoveIslandsBelowEqualPixelCount)
                        .GroupBy(issue => issue.LayerIndex);

                    if (!issuesGroup.Any()) break; // Nothing to process

                    islandsToRecompute.Clear();
                    Parallel.ForEach(issuesGroup, CoreSettings.ParallelOptions, group =>
                    {
                        if (progress.Token.IsCancellationRequested) return;
                        Layer layer = SlicerFile[group.Key];
                        Mat image = layer.LayerMat;
                        Span<byte> bytes = image.GetDataSpan<byte>();
                        foreach (var issue in group)
                        {
                            foreach (var issuePixel in issue.Pixels)
                            {
                                bytes[image.GetPixelPos(issuePixel)] = 0;
                            }

                            progress.LockAndIncrement();
                        }

                        var nextLayerIndex = group.Key + 1;
                        if (nextLayerIndex < SlicerFile.LayerCount)
                            islandsToRecompute.Add(nextLayerIndex);

                        layer.LayerMat = image;
                    });

                    // Remove from main list due the replicate below repair
                    Issues.RemoveAll(issue => issue.Type == LayerIssue.IssueType.Island && issue.Pixels.Length <= RemoveIslandsBelowEqualPixelCount);

                    if (islandsToRecompute.IsEmpty) break; // No more leftovers
                }
            }

            if (_repairIslands && _attachIslandsBelowLayers > 0)
            {
                var islandsToProcess = Issues;

                if (islandsToProcess is null)
                {
                    var islandConfig = IslandDetectionConfig.Clone();
                    var overhangConfig = new OverhangDetectionConfiguration(false);
                    var touchingBoundsConfig = new TouchingBoundDetectionConfiguration(false);
                    var printHeightConfig = new PrintHeightDetectionConfiguration(false);
                    var resinTrapsConfig = new ResinTrapDetectionConfiguration(false);
                    var emptyLayersConfig = false;

                    islandConfig.Enabled = true;

                    islandsToProcess = SlicerFile.LayerManager.GetAllIssues(islandConfig, overhangConfig, resinTrapsConfig, touchingBoundsConfig, printHeightConfig, emptyLayersConfig, null, progress);
                }

                var issuesGroup =
                    islandsToProcess
                        .Where(issue => issue.Type == LayerIssue.IssueType.Island)
                        .GroupBy(issue => issue.LayerIndex);


                progress.Reset("Attempt to attach islands below", (uint) islandsToProcess.Count);
                Parallel.ForEach(issuesGroup, CoreSettings.ParallelOptions, group =>
                {
                    using var mat = SlicerFile[group.Key].LayerMat;
                    var matSpan = mat.GetDataByteSpan();
                    var matCache = new Dictionary<uint, Mat>();
                    var matCacheModified = new Dictionary<uint, bool>();
                    var startLayer = Math.Max(0, (int)group.Key - 2);
                    var lowestPossibleLayer = (uint)Math.Max(0, (int)group.Key - 1 - _attachIslandsBelowLayers);
                    
                    for (var layerIndex = startLayer+1; layerIndex >= lowestPossibleLayer; layerIndex--)
                    {
                        Debug.WriteLine(layerIndex);
                        Monitor.Enter(SlicerFile[layerIndex].Mutex);
                        matCache.Add((uint) layerIndex, SlicerFile[layerIndex].LayerMat);
                        matCacheModified.Add((uint) layerIndex, false);
                    }

                    foreach (var issue in group)
                    {
                        int foundAt = startLayer == 0 ? 0 : - 1;
                        var requiredSupportingPixels = Math.Max(1, issue.PixelsCount * IslandDetectionConfig.RequiredPixelsToSupportMultiplier);

                        for (var layerIndex = startLayer; layerIndex >= lowestPossibleLayer && foundAt < 0; layerIndex--)
                        {
                            uint pixelsSupportingIsland = 0;
                            
                            unsafe
                            {
                                var span = matCache[(uint) layerIndex].GetBytePointer();
                                foreach (var point in issue.Pixels)
                                {
                                    if (span[mat.GetPixelPos(point)] <
                                        IslandDetectionConfig.RequiredPixelBrightnessToSupport)
                                    {
                                        continue;
                                    }

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
                                    foreach (var point in issue.Pixels)
                                    {
                                        var pos = mat.GetPixelPos(point);
                                        span[pos] = (byte) Math.Min(span[pos] + matSpan[pos], byte.MaxValue);
                                    }
                                }
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
                Parallel.For(LayerIndexStart, LayerIndexEnd, CoreSettings.ParallelOptions, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    var layer = SlicerFile[layerIndex];
                    Mat image = null;

                    void initImage()
                    {
                        image ??= layer.LayerMat;
                    }

                    if (Issues is not null)
                    {
                        if (_repairIslands && _removeIslandsBelowEqualPixelCount > 0 && _removeIslandsRecursiveIterations == 1)
                        {
                            Span<byte> bytes = null;
                            foreach (var issue in Issues)
                            {
                                if (
                                    issue.LayerIndex != layerIndex ||
                                    issue.Type != LayerIssue.IssueType.Island ||
                                    issue.Pixels.Length > _removeIslandsBelowEqualPixelCount) continue;

                                initImage();
                                if (bytes == null)
                                    bytes = image.GetDataSpan<byte>();

                                foreach (var issuePixel in issue.Pixels)
                                {
                                    bytes[image.GetPixelPos(issuePixel)] = 0;
                                }
                            }
                        }

                        if (_repairResinTraps)
                        {
                            foreach (var issue in Issues.Where(issue => issue.LayerIndex == layerIndex && issue.Type == LayerIssue.IssueType.ResinTrap))
                            {
                                initImage();
                                using var vec = new VectorOfVectorOfPoint(new VectorOfPoint(issue.Pixels));
                                CvInvoke.DrawContours(image,
                                    vec,
                                    -1,
                                    EmguExtensions.WhiteColor,
                                    -1);
                            }
                        }
                    }

                    if (_repairIslands && (_gapClosingIterations > 0 || _noiseRemovalIterations > 0))
                    {
                        initImage();
                        using Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3),
                            new Point(-1, -1));
                        if (_gapClosingIterations > 0)
                        {
                            CvInvoke.MorphologyEx(image, image, MorphOp.Close, kernel, new Point(-1, -1),
                                (int)_gapClosingIterations, BorderType.Default, default);
                        }

                        if (_noiseRemovalIterations > 0)
                        {
                            CvInvoke.MorphologyEx(image, image, MorphOp.Open, kernel, new Point(-1, -1),
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

            if (_removeEmptyLayers)
            {
                List<uint> removeLayers = new();
                for (uint layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
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
}
