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
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

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
            if (!ReferenceEquals(Issues, null)
                && !ReferenceEquals(IslandDetectionConfig, null)
                && RepairIslands
                && RemoveIslandsBelowEqualPixelCount > 0
                && RemoveIslandsRecursiveIterations != 1)
            {
                progress.Reset("Removed recursive islands");
                ushort limit = RemoveIslandsRecursiveIterations == 0
                    ? ushort.MaxValue
                    : RemoveIslandsRecursiveIterations;

                var recursiveIssues = Issues;
                ConcurrentBag<uint> islandsToRecompute = null;

                var islandConfig = IslandDetectionConfig;
                var overhangConfig = new OverhangDetectionConfiguration(false);
                var touchingBoundsConfig = new TouchingBoundDetectionConfiguration(false);
                var printHeightConfig = new PrintHeightDetectionConfiguration(false);
                var resinTrapsConfig = new ResinTrapDetectionConfiguration(false);
                var emptyLayersConfig = false;

                islandConfig.Enabled = true;
                islandConfig.RequiredAreaToProcessCheck = (ushort) Math.Floor(RemoveIslandsBelowEqualPixelCount / 2m);

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
                    islandsToRecompute = new ConcurrentBag<uint>();
                    Parallel.ForEach(issuesGroup, group =>
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

                    if (islandsToRecompute.IsEmpty) break; // No more leftovers
                }
            }

            progress.Reset(ProgressAction, LayerRangeCount);
            if (RepairIslands || RepairResinTraps)
            {
                Parallel.For(LayerIndexStart, LayerIndexEnd, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    Layer layer = SlicerFile[layerIndex];
                    Mat image = null;

                    void initImage()
                    {
                        image ??= layer.LayerMat;
                    }

                    if (Issues is not null)
                    {
                        if (RepairIslands && RemoveIslandsBelowEqualPixelCount > 0 && RemoveIslandsRecursiveIterations == 1)
                        {
                            Span<byte> bytes = null;
                            foreach (var issue in Issues)
                            {
                                if (
                                    issue.LayerIndex != layerIndex ||
                                    issue.Type != LayerIssue.IssueType.Island ||
                                    issue.Pixels.Length > RemoveIslandsBelowEqualPixelCount) continue;

                                initImage();
                                if (bytes == null)
                                    bytes = image.GetDataSpan<byte>();

                                foreach (var issuePixel in issue.Pixels)
                                {
                                    bytes[image.GetPixelPos(issuePixel)] = 0;
                                }
                            }
                            /*if (issues.TryGetValue((uint)layerIndex, out var issueList))
                            {
                                var bytes = image.GetPixelSpan<byte>();
                                foreach (var issue in issueList.Where(issue =>
                                    issue.Type == LayerIssue.IssueType.Island && issue.Pixels.Length <= removeIslandsBelowEqualPixels))
                                {
                                    foreach (var issuePixel in issue.Pixels)
                                    {
                                        bytes[image.GetPixelPos(issuePixel)] = 0;
                                    }
                                }
                            }*/
                        }

                        if (RepairResinTraps)
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

                    if (RepairIslands && (GapClosingIterations > 0 || NoiseRemovalIterations > 0))
                    {
                        initImage();
                        using Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3),
                            new Point(-1, -1));
                        if (GapClosingIterations > 0)
                        {
                            CvInvoke.MorphologyEx(image, image, MorphOp.Close, kernel, new Point(-1, -1),
                                (int)GapClosingIterations, BorderType.Default, default);
                        }

                        if (NoiseRemovalIterations > 0)
                        {
                            CvInvoke.MorphologyEx(image, image, MorphOp.Open, kernel, new Point(-1, -1),
                                (int)NoiseRemovalIterations, BorderType.Default, default);
                        }
                    }

                    if (!ReferenceEquals(image, null))
                    {
                        layer.LayerMat = image;
                        image.Dispose();
                    }

                    progress.LockAndIncrement();
                });
            }

            if (RemoveEmptyLayers)
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
