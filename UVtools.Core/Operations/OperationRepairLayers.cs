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
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.PixelEditor;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationRepairLayers : Operation
    {
        #region Members
        private bool _repairIslands = true;
        private bool _repairResinTraps = true;
        private bool _removeEmptyLayers = true;
        private bool _repairSuctionCups;
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

            if (!_repairIslands && !_repairResinTraps && !_repairSuctionCups && !_removeEmptyLayers)
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
                        var layer = SlicerFile[group.Key];
                        var image = layer.LayerMat;
                        var bytes = image.GetDataByteSpan();
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
                        initImage();
                        using var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3),
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


            if (_repairSuctionCups && Issues is not null)
            {
                /* build out parent/child relationships between all detected suction cups */

                var bottomSuctionIssues = new ConcurrentBag<LayerIssue>();

                Parallel.ForEach(Issues.Where(issue => issue.Type == LayerIssue.IssueType.SuctionCup), issue => {

                    if (issue.LayerIndex == 0)
                    {
                        bottomSuctionIssues.Add(issue);
                        return;
                    }

                    bool overlapFound = false;
                    
                    foreach(var candidate in Issues.Where(candidate => candidate.LayerIndex == issue.LayerIndex - 1 && candidate.Type  == LayerIssue.IssueType.SuctionCup))
                    {
                        if (EmguContours.ContoursIntersect(new VectorOfVectorOfPoint(issue.Contours), new VectorOfVectorOfPoint(candidate.Contours))) {
                            overlapFound = true;
                            break;
                        }
                    }
                    if (!overlapFound)
                    {
                        bottomSuctionIssues.Add(issue);
                    }
                });

                (bool canDrill, Point location) GetDrillLocation(LayerIssue issue, int diameter)
                {
                    using var vecCentroid = new VectorOfPoint(issue.Contours[0]);
                    var centroid = EmguContour.GetCentroid(vecCentroid);
                    using var circleCheck = EmguExtensions.InitMat(issue.BoundingRectangle.Size);
                    using var contourMat = EmguExtensions.InitMat(issue.BoundingRectangle.Size);
                    using var overlapCheck = EmguExtensions.InitMat(issue.BoundingRectangle.Size);

                    var inverseOffset = new Point(issue.BoundingRectangle.X * -1, issue.BoundingRectangle.Y * -1);
                    using var vec = new VectorOfVectorOfPoint(issue.Contours);
                    CvInvoke.DrawContours(contourMat, vec, -1, EmguExtensions.WhiteColor, -1, LineType.EightConnected, null, int.MaxValue, inverseOffset);

                    CvInvoke.Circle(circleCheck, new(centroid.X + inverseOffset.X, centroid.Y + inverseOffset.Y), diameter, EmguExtensions.WhiteColor, -1);

                    CvInvoke.BitwiseAnd(circleCheck, contourMat, overlapCheck);

                    return CvInvoke.CountNonZero(overlapCheck) > 0 
                        ? (true,centroid)       /* 5px centroid is inside layer! drill baby drill */
                        : (false, new Point()); /* centroid is not inside the actual contour, no drill */
                }

                var drillOps = new List<PixelOperation>();
                //var suctionReliefSize = (ushort)Math.Max(SlicerFile.PpmmMax * 0.8, 17);
                /* for each suction cup issue that is an initial layer */
                foreach (var issue in bottomSuctionIssues)
                {
                    var drillPoint = GetDrillLocation(issue, _suctionCupsVentHole);
                    if (!drillPoint.canDrill) continue;
                    drillOps.Add(new PixelDrainHole(issue.LayerIndex, drillPoint.location, _suctionCupsVentHole));
                }

                SlicerFile.LayerManager.DrawModifications(drillOps, progress);
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
}
