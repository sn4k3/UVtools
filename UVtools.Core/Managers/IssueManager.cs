using CommunityToolkit.HighPerformance;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Threading;
using System.Threading.Tasks;
using EmguExtensions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using UVtools.Core.PixelEditor;
using ZLinq;

namespace UVtools.Core.Managers;

public sealed class IssueManager : RangeObservableCollection<MainIssue>
{
    private long _revision;

    public FileFormat SlicerFile { get; }

    /// <summary>
    /// Gets a monotonic revision that changes whenever the visible issue collection changes.
    /// </summary>
    public long Revision => Interlocked.Read(ref _revision);

    public List<MainIssue> IgnoredIssues { get; } = [];

    public bool HaveIssues => Count > 0;

    public IssueManager(FileFormat slicerFile)
    {
        SlicerFile = slicerFile;
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        Interlocked.Increment(ref _revision);
        base.OnCollectionChanged(e);
    }

    /// <summary>
    /// Gets the visible <see cref="MainIssue"/> aka not ignored
    /// </summary>
    /// <returns></returns>
    public MainIssue[] GetVisible()
    {
        return this.AsValueEnumerable().Where(mainIssue => !IgnoredIssues.Contains(mainIssue)).ToArray();
    }

    public static Issue[] GetIssues(IEnumerable<MainIssue> issues)
    {
        var result = new List<Issue>();
        foreach (var mainIssue in issues)
        {
            result.AddRange(mainIssue);
        }

        return result.ToArray();
    }

    public static Issue[] GetIssuesBy(IEnumerable<MainIssue> issues, MainIssue.IssueType type, uint layerIndex)
    {
        var result = new List<Issue>();
        foreach (var mainIssue in issues)
        {
            if (mainIssue.Type != type) continue;
            if (!mainIssue.IsIssueInBetween(layerIndex)) continue;
            foreach (var issue in mainIssue)
            {
                if (issue.LayerIndex != layerIndex) continue;
                result.Add(issue);
            }
        }

        return result.ToArray();
    }

    public static Issue[] GetIssuesBy(IEnumerable<MainIssue> issues, MainIssue.IssueType type)
    {
        var result = new List<Issue>();
        foreach (var mainIssue in issues)
        {
            if (mainIssue.Type != type) continue;
            result.AddRange(mainIssue);
        }

        return result.ToArray();
    }


    public static Issue[] GetIssuesBy(IEnumerable<MainIssue> issues, uint layerIndex)
    {
        var result = new List<Issue>();
        foreach (var mainIssue in issues)
        {
            if (!mainIssue.IsIssueInBetween(layerIndex)) continue;
            foreach (var issue in mainIssue)
            {
                if (issue.LayerIndex != layerIndex) continue;
                result.Add(issue);
            }
        }

        return result.ToArray();
    }

    public Issue[] GetIssues()
    {
        return GetIssues(this);
    }

    public Issue[] GetIssuesBy(MainIssue.IssueType type)
    {
        return GetIssuesBy(this, type);
    }

    public Issue[] GetIssuesBy(MainIssue.IssueType type, uint layerIndex)
    {
        return GetIssuesBy(this, type, layerIndex);
    }

    public Issue[] GetIssuesBy(uint layerIndex)
    {
        return GetIssuesBy(this, layerIndex);
    }

    public List<MainIssue> DetectIssues(IssuesDetectionConfiguration? config = null, OperationProgress? progress = null)
    {
        if (SlicerFile.DecodeType == FileFormat.FileDecodeType.Partial) return [];

        config ??= new IssuesDetectionConfiguration();
        var (
            islandConfig,
            overhangConfig,
            resinTrapConfig,
            touchBoundConfig,
            printHeightConfig,
            emptyLayerConfig
            ) = config;

        progress ??= new OperationProgress();

        var result = new ConcurrentBag<MainIssue>();
        //var layerHollowAreas = new ConcurrentDictionary<uint, List<LayerHollowArea>>();
        var resinTraps = new List<VectorOfVectorOfPoint>?[SlicerFile.LayerCount];
        var suctionCups = new List<VectorOfVectorOfPoint>?[SlicerFile.LayerCount];
        var externalContours = new VectorOfVectorOfPoint?[SlicerFile.LayerCount];
        var hollows = new List<VectorOfVectorOfPoint>?[SlicerFile.LayerCount];
        var airContours = new List<VectorOfVectorOfPoint>?[SlicerFile.LayerCount];
        var resinTrapsContoursArea = new double[SlicerFile.LayerCount][];

        // Whatever way this method exits (including cancellation) the native vectors must be released
        using var cleanup = new ScopeCleanup(() =>
        {
            foreach (var listOfVectors in new[] { resinTraps, suctionCups, hollows, airContours })
            {
                foreach (var vectorArray in listOfVectors)
                {
                    if (vectorArray is null) continue;
                    foreach (var vector in vectorArray)
                    {
                        vector?.Dispose();
                    }
                }
            }

            foreach (var vector in externalContours)
            {
                vector?.Dispose();
            }
        });

        bool IsIgnored(MainIssue issue) => IgnoredIssues.Count > 0 && IgnoredIssues.Contains(issue);

        bool AddIssue(MainIssue issue)
        {
            if (IsIgnored(issue)) return false;
            result.Add(issue);
            return true;
        }

        List<MainIssue> GetResult()
        {
            return result.AsValueEnumerable().OrderBy(mainIssue => mainIssue.Type)
                .ThenBy(issue => issue.StartLayerIndex).ThenByDescending(issue => issue.Area)
                .ThenBy(issue => issue.BoundingRectangle.Y).ThenBy(issue => issue.BoundingRectangle.X).ToList();
        }

        void GenerateAirMap(Mat input, Mat output, VectorOfVectorOfPoint? externals)
        {
            BitwiseNot(input, output);
            if (externals is null || externals.Size == 0) return;
            CvInvoke.DrawContours(output, externals, -1, EmguCvExtensions.BlackColor, -1);
        }

        /* Gets the rectangle that encloses every contour of the group, clamped to bounds.
         * The resin trap passes only ever touch pixels inside this rectangle, so all the per-contour
         * mat work can be confined to it instead of running over the whole layer. */
        static Rectangle GetContourGroupRoi(VectorOfVectorOfPoint group, Size bounds)
        {
            if (group.Size == 0) return Rectangle.Empty;

            var rect = CvInvoke.BoundingRectangle(group[0]);
            for (var i = 1; i < group.Size; i++)
            {
                rect = Rectangle.Union(rect, CvInvoke.BoundingRectangle(group[i]));
            }

            rect.Intersect(new Rectangle(Point.Empty, bounds));
            return rect;
        }

        if (printHeightConfig.Enabled && SlicerFile.MachineZ > 0)
        {
            float printHeightWithOffset = Layer.RoundHeight(SlicerFile.MachineZ + printHeightConfig.Offset);
            if (SlicerFile.PrintHeight > printHeightWithOffset)
            {
                var issues = (from layer in SlicerFile
                    where layer.PositionZ > printHeightWithOffset
                    select new Issue(layer)).ToList();

                if (issues.Count > 0) AddIssue(new MainIssue(MainIssue.IssueType.PrintHeight, issues));
            }
        }

        if (emptyLayerConfig.Enabled)
        {
            var classifyByPosition = emptyLayerConfig.IgnoreStartingEmptyLayers ||
                                     emptyLayerConfig.IgnoreLooseEmptyLayers ||
                                     emptyLayerConfig.IgnoreEndingEmptyLayers;
            var firstNonEmptyLayerIndex = 0;
            var lastNonEmptyLayerIndex = SlicerFile.Count - 1;
            if (classifyByPosition)
            {
                while (firstNonEmptyLayerIndex < SlicerFile.Count && SlicerFile[firstNonEmptyLayerIndex].IsEmpty)
                {
                    firstNonEmptyLayerIndex++;
                }

                while (lastNonEmptyLayerIndex >= firstNonEmptyLayerIndex && SlicerFile[lastNonEmptyLayerIndex].IsEmpty)
                {
                    lastNonEmptyLayerIndex--;
                }
            }

            for (var layerIndex = 0; layerIndex < SlicerFile.Count; layerIndex++)
            {
                var layer = SlicerFile[layerIndex];
                if (!layer.IsEmpty) continue;

                if (!classifyByPosition)
                {
                    AddIssue(new MainIssue(MainIssue.IssueType.EmptyLayer, new Issue(layer)));
                    continue;
                }

                if (layerIndex < firstNonEmptyLayerIndex)
                {
                    if (!emptyLayerConfig.IgnoreStartingEmptyLayers)
                        AddIssue(new MainIssue(MainIssue.IssueType.EmptyLayer, new Issue(layer)));
                }
                else if (layerIndex > lastNonEmptyLayerIndex)
                {
                    if (!emptyLayerConfig.IgnoreEndingEmptyLayers)
                        AddIssue(new MainIssue(MainIssue.IssueType.EmptyLayer, new Issue(layer)));
                }
                else if (!emptyLayerConfig.IgnoreLooseEmptyLayers)
                {
                    AddIssue(new MainIssue(MainIssue.IssueType.EmptyLayer, new Issue(layer)));
                }
            }
        }

        if (islandConfig.Enabled || overhangConfig.Enabled || resinTrapConfig.Enabled || touchBoundConfig.Enabled)
        {
            progress.Reset(OperationProgress.StatusIslands, SlicerFile.LayerCount);

            var firstLayer = SlicerFile.FirstLayer;

            int overhangsIterations = overhangConfig.ErodeIterations;
            using var overhangsKernel =
                EmguCvExtensions.CreateDynamicKernel(ref overhangsIterations, MorphShapes.Cross);

            // Detect contours
            var globalBoundingRectangle = SlicerFile.BoundingRectangle;
            var firstLayerPositionZ = firstLayer?.PositionZ ?? 0;
            var overhangKernelRadius = Math.Max((int)overhangConfig.ErodeIterations, 1);
            var islandWhiteList = islandConfig.WhiteListLayers is null
                ? null
                : new HashSet<uint>(islandConfig.WhiteListLayers);
            var overhangWhiteList = overhangConfig.WhiteListLayers is null
                ? null
                : new HashSet<uint>(overhangConfig.WhiteListLayers);

            Parallel.For(0, SlicerFile.LayerCount, CoreSettings.ParallelOptions, layerIndexInt =>
            {
                progress.PauseIfRequested();
                if (progress.Token.IsCancellationRequested)
                {
                    return;
                }

                uint layerIndex = (uint)layerIndexInt;
                var layer = SlicerFile[layerIndex];

                if (layer.IsEmpty)
                {
                    progress.LockAndIncrement();
                    return;
                }

                // No islands nor overhangs for layer 0 or on plate
                var canHaveIslandsOrOverhangs = layerIndex > 0 && layer.PositionZ > firstLayerPositionZ;
                var checkOverhangs = canHaveIslandsOrOverhangs && overhangConfig.Enabled &&
                                     (overhangWhiteList is null || overhangWhiteList.Contains(layerIndex));
                var checkIslands = canHaveIslandsOrOverhangs && islandConfig.Enabled &&
                                   (islandWhiteList is null || islandWhiteList.Contains(layerIndex));
                var checkResinTraps = resinTrapConfig.Enabled && layerIndex >= resinTrapConfig.StartLayerIndex;

                // Spare a decoding cycle
                if (!touchBoundConfig.Enabled && !checkResinTraps && !checkOverhangs && !checkIslands)
                {
                    progress.LockAndIncrement();
                    return;
                }

                if (touchBoundConfig.Enabled)
                {
                    DetectTouchingBounds(layer, touchBoundConfig, AddIssue);
                }

                if (checkOverhangs || checkIslands || checkResinTraps)
                {
                    var roiRect = layerIndex == 0
                        ? globalBoundingRectangle
                        : Layer.GetBoundingRectangleUnion(SlicerFile[layerIndex - 1], layer);

                    // Resin traps work over the model bounding rectangle, which contains the layer ROI
                    var decodeRect = checkResinTraps ? Rectangle.Union(globalBoundingRectangle, roiRect) : roiRect;

                    using var decodedMat = layer.GetRoiMat(decodeRect);
                    using var roiView = decodeRect == roiRect
                        ? null
                        : new Mat(decodedMat, new Rectangle(roiRect.X - decodeRect.X, roiRect.Y - decodeRect.Y,
                            roiRect.Width, roiRect.Height));
                    var roiMat = roiView ?? decodedMat;

                    if (checkOverhangs || checkIslands)
                    {
                        using var previousMat = SlicerFile[layerIndex - 1].GetRoiMat(roiRect);
                        var overhangs = new List<MainIssue>();

                        // Overhangs
                        // The mask only spans the area where the difference between layers survives, the rest is zeros
                        Mat? overhangMask = null;
                        var overhangMaskRect = Rectangle.Empty;
                        try
                        {
                            if (checkOverhangs)
                            {
                                using var difference = new Mat();
                                CvInvoke.Subtract(roiMat, previousMat, difference);
                                CvInvoke.Threshold(difference, difference, 127, 255, ThresholdType.Binary);

                                var differenceRect = CvInvoke.BoundingRectangle(difference);
                                if (!differenceRect.IsEmpty)
                                {
                                    // Pad enough for the erode to never reach the cropped border
                                    differenceRect.Inflate(overhangKernelRadius + 1, overhangKernelRadius + 1);
                                    differenceRect.Intersect(new Rectangle(Point.Empty, difference.Size));
                                    overhangMaskRect = differenceRect;

                                    using var differenceRoi = new Mat(difference, differenceRect);
                                    overhangMask = new Mat();
                                    CvInvoke.Erode(differenceRoi, overhangMask, overhangsKernel,
                                        EmguCvExtensions.AnchorCenter, overhangsIterations, BorderType.Default,
                                        default);

                                    using var contours = overhangMask.FindContours(out var hierarchy, RetrType.Tree,
                                        ChainApproxMethod.ChainApproxSimple,
                                        new Point(roiRect.X + differenceRect.X, roiRect.Y + differenceRect.Y));
                                    var contoursInGroups =
                                        EmguContours.GetPositiveContoursInGroups(contours, hierarchy);

                                    foreach (var contourGroup in contoursInGroups)
                                    {
                                        if (contourGroup[0].Size < 3) continue; // Single contour, single line, ignore
                                        var area = EmguContours.GetContourArea(contourGroup);
                                        if (area >= overhangConfig.RequiredPixelsToConsider)
                                        {
                                            var rect = CvInvoke.BoundingRectangle(contourGroup[0]);
                                            var overhangIssue = new MainIssue(MainIssue.IssueType.Overhang,
                                                new IssueOfContours(layer, contourGroup.ToArrayOfArray(), rect, area));
                                            overhangs.Add(overhangIssue);
                                            AddIssue(overhangIssue);
                                        }
                                    }
                                }
                            }

                            // Islands
                            // An island needs a component whose pixels are not supported by the previous layer, if all the
                            // pixels are supported there is nothing to find, spare the labeling.
                            if (checkIslands && (islandConfig.RequiredPixelsToSupportMultiplier > 1m ||
                                                 HasUnsupportedPixels(roiMat, previousMat,
                                                     islandConfig.RequiredPixelBrightnessToProcessCheck,
                                                     islandConfig.RequiredPixelBrightnessToSupport)))
                            {
                                var labeler = RunComponentLabeler.ForCurrentThread;
                                labeler.Label(roiMat, islandConfig.BinaryThreshold, islandConfig.AllowDiagonalBonds);

                                for (var component = 0; component < labeler.ComponentCount; component++)
                                {
                                    if (labeler.GetArea(component) < islandConfig.RequiredAreaToProcessCheck)
                                        continue;

                                    var rect = labeler.GetBounds(component);
                                    var runs = labeler.GetRuns(component);

                                    // First pass only counts. The point list is materialized later, and only
                                    // for components that actually turn out to be islands: a large solid
                                    // cross-section would otherwise grow (and immediately discard) a list with
                                    // one entry per pixel.
                                    var pixelCount = 0;
                                    var pixelsSupportingIsland = 0;

                                    foreach (var run in runs)
                                    {
                                        var y = labeler.GetRunY(run);
                                        CountPixels(roiMat.GetReadOnlyRowSpanOfBytes(y),
                                            previousMat.GetReadOnlyRowSpanOfBytes(y),
                                            labeler.GetRunStart(run), labeler.GetRunEnd(run),
                                            islandConfig.RequiredPixelBrightnessToProcessCheck,
                                            islandConfig.RequiredPixelBrightnessToSupport,
                                            ref pixelCount, ref pixelsSupportingIsland);
                                    }

                                    if (pixelCount == 0) continue; // Low brightness, ignore

                                    var requiredSupportingPixels = Math.Max(1,
                                        pixelCount * islandConfig.RequiredPixelsToSupportMultiplier);

                                    if (pixelsSupportingIsland >= requiredSupportingPixels) continue;

                                    var islandBoundingRectangle = rect.OffsetBy(roiRect.Location);

                                    // Check for overhangs in islands
                                    if (islandConfig.EnhancedDetection && pixelsSupportingIsland >= 10 &&
                                        pixelsSupportingIsland >= requiredSupportingPixels / 4)
                                    {
                                        // No overhangs nor intersecting = discard island
                                        // Only when the overhangs were computed for this layer
                                        if (checkOverhangs &&
                                            overhangs.TrueForAll(overhang =>
                                                !overhang.BoundingRectangle.IntersectsWith(islandBoundingRectangle)))
                                        {
                                            continue;
                                        }

                                        var overhangPixels = 0;

                                        if (checkOverhangs)
                                        {
                                            // The layer overhang mask is zero outside of its rectangle
                                            if (overhangMask is not null)
                                            {
                                                foreach (var run in runs)
                                                {
                                                    if (overhangPixels >= overhangConfig.RequiredPixelsToConsider) break;
                                                    var y = labeler.GetRunY(run);
                                                    if (y < overhangMaskRect.Y || y >= overhangMaskRect.Bottom) continue;

                                                    var start = Math.Max(labeler.GetRunStart(run), overhangMaskRect.X);
                                                    var end = Math.Min(labeler.GetRunEnd(run),
                                                        overhangMaskRect.Right - 1);
                                                    if (start > end) continue;

                                                    overhangPixels += CountNonZero(
                                                        overhangMask.GetReadOnlyRowSpanOfBytes(y - overhangMaskRect.Y)
                                                            .Slice(start - overhangMaskRect.X, end - start + 1));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            using var islandRoi = roiMat.Roi(rect);
                                            using var previousIslandRoi = previousMat.Roi(rect);
                                            using var islandOverhangMat = new Mat();
                                            CvInvoke.Subtract(islandRoi, previousIslandRoi, islandOverhangMat);
                                            CvInvoke.Threshold(islandOverhangMat, islandOverhangMat, 127, 255,
                                                ThresholdType.Binary);

                                            CvInvoke.Erode(islandOverhangMat, islandOverhangMat, overhangsKernel,
                                                EmguCvExtensions.AnchorCenter, overhangsIterations,
                                                BorderType.Default, default);

                                            foreach (var run in runs)
                                            {
                                                if (overhangPixels >= overhangConfig.RequiredPixelsToConsider) break;
                                                var y = labeler.GetRunY(run);
                                                overhangPixels += CountNonZero(
                                                    islandOverhangMat.GetReadOnlyRowSpanOfBytes(y - rect.Y).Slice(
                                                        labeler.GetRunStart(run) - rect.X,
                                                        labeler.GetRunEnd(run) - labeler.GetRunStart(run) + 1));
                                            }
                                        }

                                        if (overhangPixels <
                                            overhangConfig.RequiredPixelsToConsider) // No overhang = no island
                                        {
                                            continue;
                                        }
                                    }

                                    // Confirmed island: now collect its pixels.
                                    var points = new List<Point>(pixelCount);
                                    foreach (var run in runs)
                                    {
                                        var y = labeler.GetRunY(run);
                                        var roiRow = roiMat.GetReadOnlyRowSpanOfBytes(y);
                                        for (int x = labeler.GetRunStart(run); x <= labeler.GetRunEnd(run); x++)
                                        {
                                            if (roiRow[x] < islandConfig.RequiredPixelBrightnessToProcessCheck)
                                                continue;

                                            points.Add(new Point(roiRect.X + x, roiRect.Y + y));
                                        }
                                    }

                                    AddIssue(new MainIssue(MainIssue.IssueType.Island,
                                        new IssueOfPoints(layer, points, islandBoundingRectangle)));
                                }

                                labeler.TrimExcess();
                            }                        }
                        finally
                        {
                            overhangMask?.Dispose();
                        }
                    }

                    if (checkResinTraps)
                    {
                        /* this used to calculate all contours for the layers, however new algorithm crops the layers to the overall bounding box
                         * so the contours produced here are not translated properly. We will generate contours during the algorithm itself later */

                        using var modelView = new Mat(decodedMat,
                            new Rectangle(globalBoundingRectangle.X - decodeRect.X,
                                globalBoundingRectangle.Y - decodeRect.Y,
                                globalBoundingRectangle.Width, globalBoundingRectangle.Height));

                        Mat? thresholdedModel = null;
                        try
                        {
                            if (resinTrapConfig.BinaryThreshold > 0)
                            {
                                thresholdedModel = new Mat();
                                CvInvoke.Threshold(modelView, thresholdedModel, resinTrapConfig.BinaryThreshold,
                                    byte.MaxValue, ThresholdType.Binary);
                            }

                            using var contours = (thresholdedModel ?? modelView).FindContours(out var hierarchy,
                                RetrType.Tree);
                            externalContours[layerIndex] = EmguContours.GetExternalContours(contours, hierarchy);
                            hollows[layerIndex] = EmguContours.GetNegativeContoursInGroups(contours, hierarchy);
                            resinTrapsContoursArea[layerIndex] = EmguContours.GetContoursArea(hollows[layerIndex]);
                        }
                        finally
                        {
                            thresholdedModel?.Dispose();
                        }
                    }
                }

                progress.LockAndIncrement();
            }); // Parallel end
        }

        if (progress.Token.IsCancellationRequested) return GetResult();

        if (resinTrapConfig.Enabled)
        {
            //progress.Reset("Detecting Air Boundaries (Resin traps)", LayerCount);
            //if (progress.Token.IsCancellationRequested) return result.OrderBy(issue => issue.Type).ThenBy(issue => issue.LayerIndex).ThenBy(issue => issue.Area).ToList();
            progress.Reset("Detection pass 1 of 2 (Resin traps)", SlicerFile.LayerCount,
                resinTrapConfig.StartLayerIndex);

            var modelBoundingRectangle = SlicerFile.BoundingRectangle;
            var lookahead = Math.Clamp(Environment.ProcessorCount, 4, 16);

            Mat DecodeModelLayer(int layerIndex)
            {
                var mat = SlicerFile[layerIndex].GetRoiMat(modelBoundingRectangle);
                if (resinTrapConfig.MaximumPixelBrightnessToDrain > 0)
                {
                    CvInvoke.Threshold(mat, mat, resinTrapConfig.MaximumPixelBrightnessToDrain, byte.MaxValue,
                        ThresholdType.Binary);
                }

                return mat;
            }

            using var prefetcherPass1 = new LayerMatPrefetcher((int)resinTrapConfig.StartLayerIndex,
                (int)SlicerFile.LastLayerIndex, true, DecodeModelLayer, lookahead);

            /* define all mats up front, reducing allocations */

            using var layerAirMap = new Mat();
            Mat? currentAirMap = null;
            /* the first pass does bottom to top, and tracks anything it thinks is a resin trap */
            for (var layerIndex = resinTrapConfig.StartLayerIndex; layerIndex < SlicerFile.LayerCount; layerIndex++)
            {
                if (progress.Token.IsCancellationRequested) return GetResult();

                using var curLayer = prefetcherPass1.Next();

                //curLayer.Save($"D:\\dump\\{layerIndex}_a.png");

                /* find hollows of current layer */
                GenerateAirMap(curLayer, layerAirMap, externalContours[layerIndex]);

                //layerAirMap.Save($"D:\\dump\\{layerIndex}_b.png");

                if (layerIndex == resinTrapConfig.StartLayerIndex)
                {
                    currentAirMap = layerAirMap.Clone();
                }

                //currentAirMap.Save($"D:\\dump\\{layerIndex}_c.png");

                /* remove solid areas of current layer from the air map, then add in areas of air in current layer to it */
                SubtractAndOr(currentAirMap!, curLayer, layerAirMap);

                //currentAirMap.Save($"D:\\dump\\{layerIndex}_e.png");

                if (hollows[layerIndex] is not null)
                {
                    resinTraps[layerIndex] = [];
                    airContours[layerIndex] = [];
                    Parallel.For(0, hollows[layerIndex].Count, CoreSettings.ParallelOptions, i =>
                    {
                        progress.PauseIfRequested();
                        //for (var i = 0; i < hollows[layerIndex].Count; i++)
                        //{
                        if (progress.Token.IsCancellationRequested) return;
                        if (resinTrapsContoursArea[layerIndex][i] < resinTrapConfig.RequiredAreaToProcessCheck) return;

                        /* intersect current contour, with the current airmap.
                         * Everything is confined to the contour's own bounding box: drawing into a
                         * layer-sized mat here meant allocating + zeroing the full layer and then
                         * scanning the whole image three times (and/count/or) for a contour that
                         * usually covers a tiny fraction of it. */
                        var contourRoi = GetContourGroupRoi(hollows[layerIndex][i], curLayer.Size);
                        if (contourRoi.IsEmpty) return;

                        using var currentContour = EmguCvExtensions.InitMat(contourRoi.Size);
                        using var currentAirMapRoi = new Mat(currentAirMap, contourRoi);
                        using var airOverlap = new Mat();
                        CvInvoke.DrawContours(currentContour, hollows[layerIndex][i], -1, EmguCvExtensions.WhiteColor,
                            -1,
                            LineType.EightConnected, null, int.MaxValue, new Point(-contourRoi.X, -contourRoi.Y));
                        CvInvoke.BitwiseAnd(currentAirMapRoi, currentContour, airOverlap);
                        var overlapCount = CvInvoke.CountNonZero(airOverlap);

                        lock (SlicerFile[layerIndex].Mutex)
                        {
                            if (overlapCount == 0)
                            {
                                /* this countour does *not* overlap known air */

                                /* add a resin trap (for now... will be revisited in part 2) */
                                resinTraps[layerIndex].Add(hollows[layerIndex][i]);
                            }
                            else
                            {
                                if (overlapCount >= resinTrapConfig.RequiredBlackPixelsToDrain)
                                {
                                    /* this contour does overlap air, add it to the current air map and remember this contour was air-connected for 2nd pass */
                                    airContours[layerIndex].Add(hollows[layerIndex][i]);

                                    CvInvoke.BitwiseOr(currentContour, currentAirMapRoi, currentAirMapRoi);
                                }
                                else
                                {
                                    /* it overlapped ,but not by enough, treat as solid */
                                    CvInvoke.Subtract(currentAirMapRoi, currentContour, currentAirMapRoi);
                                }
                            }
                        }
                    });
                }

                progress++;
            }

            if (progress.Token.IsCancellationRequested) return GetResult();
            progress.Reset("Detection pass 2 of 2 (Resin traps)", SlicerFile.LayerCount,
                resinTrapConfig.StartLayerIndex);
            /* starting over again but this time from the top to the bottom */
            if (currentAirMap is not null)
            {
                currentAirMap.Dispose();
                currentAirMap = null;
            }

            var resinTrapGroups = new List<List<(VectorOfVectorOfPoint contour, uint layerIndex)>>();

            using var prefetcherPass2 = new LayerMatPrefetcher((int)SlicerFile.LastLayerIndex,
                (int)resinTrapConfig.StartLayerIndex, false, DecodeModelLayer, lookahead);

            for (int layerIndex = resinTraps.Length - 1; layerIndex >= resinTrapConfig.StartLayerIndex; layerIndex--)
            {
                if (progress.Token.IsCancellationRequested) return GetResult();

                using var curLayer = prefetcherPass2.Next();

                if (layerIndex == resinTraps.Length - 1)
                {
                    /* this is subtly different that for the first pass, we don't use GenerateAirMap for the initial airmap */
                    /* instead we use a bitwise not, this way anything that is open/hollow on the top layer is treated as air */
                    currentAirMap = new Mat();
                    CvInvoke.BitwiseNot(curLayer, currentAirMap);
                }

                /* we still modify the airmap like normal, where we account for the air areas of the layer, and any contours that might overlap...*/
                GenerateAirMap(curLayer, layerAirMap, externalContours[layerIndex]);

                /* Update air map with any hollows that were found to be air-connected during first pass */
                if (airContours[layerIndex] is not null)
                {
                    Parallel.ForEach(airContours[layerIndex], CoreSettings.ParallelOptions, vec =>
                        {
                            progress.PauseIfRequested();
                            CvInvoke.DrawContours(layerAirMap, vec, -1, EmguCvExtensions.WhiteColor, -1);
                        }
                    );
                }

                /* remove solid areas of current layer from the air map, then add in areas of air in current layer to it */
                SubtractAndOr(currentAirMap!, curLayer, layerAirMap);

                if (resinTraps[layerIndex] is not null)
                {
                    suctionCups[layerIndex] = [];
                    /* here we don't worry about finding contours on the layer, the bottom to top pass did that already */
                    /* all we care about is contours the first pass thought were resin traps, since there was no access to air from the bottom */
                    Parallel.For(0, resinTraps[layerIndex].Count, CoreSettings.ParallelOptions, x =>
                    {
                        progress.PauseIfRequested();
                        if (progress.Token.IsCancellationRequested) return;

                        /* check if each contour overlaps known air, confined to the contour's bounding box */
                        var contourRoi = GetContourGroupRoi(resinTraps[layerIndex][x], curLayer.Size);
                        if (contourRoi.IsEmpty) return;

                        using var currentContour = EmguCvExtensions.InitMat(contourRoi.Size);
                        using var currentAirMapRoi = new Mat(currentAirMap, contourRoi);
                        using var airOverlap = new Mat();
                        CvInvoke.DrawContours(currentContour, resinTraps[layerIndex][x], -1,
                            EmguCvExtensions.WhiteColor, -1,
                            LineType.EightConnected, null, int.MaxValue, new Point(-contourRoi.X, -contourRoi.Y));

                        CvInvoke.BitwiseAnd(currentAirMapRoi, currentContour, airOverlap);
                        var overlapCount = CvInvoke.CountNonZero(airOverlap);

                        //lock (SlicerFile[layerIndex].Mutex)
                        //{
                        if (overlapCount >= resinTrapConfig.RequiredBlackPixelsToDrain)
                        {
                            /* this contour does overlap air, add this it our air map */
                            CvInvoke.BitwiseOr(currentContour, currentAirMapRoi, currentAirMapRoi, currentContour);
                            /* Always add the removed contour to suctionTraps (even if we aren't reporting suction traps)
                             * This is because contours that are placed on here get removed from resin traps in the next stage
                             * if you don't put them here, they never get removed even if they should :) */

                            /* if we haven't defined a suctionTrap list for this layer, do so */

                            lock (SlicerFile[layerIndex].Mutex)
                            {
                                /* since we know it isn't a resin trap, it becomes a suction trap */
                                suctionCups[layerIndex].Add(resinTraps[layerIndex][x]);

                                for (var groupIndex = resinTrapGroups.Count - 1; groupIndex >= 0; groupIndex--)
                                {
                                    var group = resinTrapGroups[groupIndex];
                                    if (group[^1].layerIndex > layerIndex + 1)
                                    {
                                        // this group is disconnected from current layer by at least 1 layer, no need to process anything from here anymore
                                        //group.Clear();
                                        //resinTrapGroups.Remove(group);
                                        continue;
                                    }

                                    for (var contourIndex = group.Count - 1; contourIndex >= 0; contourIndex--)
                                    {
                                        if (group[contourIndex].layerIndex > layerIndex + 1) break;
                                        var testContour = group[contourIndex].contour;

                                        if (!EmguContours.ContoursIntersect(testContour, resinTraps[layerIndex][x]))
                                            continue;
                                        // if any contours in this group, that are on the previous layer, overlap the new suction area, they are all suction areas

                                        foreach (var item in group)
                                        {
                                            suctionCups[item.layerIndex].Add(item.contour);
                                            if (item.layerIndex != layerIndex)
                                            {
                                                resinTraps[item.layerIndex].Remove(item.contour);
                                            }
                                        }

                                        group.Clear();
                                        resinTrapGroups.Remove(group);
                                        break;
                                    }
                                }
                            }
                            /* to keep things tidy while we iterate resin traps, it will be left in the list for now, and removed later */
                        }
                        else
                        {
                            /* doesn't overlap by enough, remove from air map */
                            CvInvoke.Subtract(currentAirMapRoi, currentContour, currentAirMapRoi, currentContour);

                            lock (SlicerFile[layerIndex].Mutex)
                            {
                                /* put it in a group of resin traps, used when a subsequent layer becomes a suction cup, it can convert any overlapping groups to suction cup */
                                /* select new LayerIssue(this[layerIndex], LayerIssue.IssueType.ResinTrap, area.Contour, area.BoundingRectangle)) */
                                var overlappingGroupIndexes = new List<int>();
                                for (var groupIndex = 0; groupIndex < resinTrapGroups.Count; groupIndex++)
                                {
                                    if (resinTrapGroups[groupIndex][^1].layerIndex != layerIndex &&
                                        resinTrapGroups[groupIndex][^1].layerIndex != layerIndex + 1) continue;

                                    if (EmguContours.ContoursIntersect(resinTrapGroups[groupIndex][^1].contour,
                                            resinTraps[layerIndex][x]))
                                    {
                                        overlappingGroupIndexes.Add(groupIndex);
                                    }
                                }

                                if (overlappingGroupIndexes.Count == 0)
                                {
                                    // no overlaps, make a single issue
                                    resinTrapGroups.Add([(resinTraps[layerIndex][x], (uint)layerIndex)]);
                                }
                                else if (overlappingGroupIndexes.Count == 1)
                                {
                                    resinTrapGroups[overlappingGroupIndexes[0]]
                                        .Add((resinTraps[layerIndex][x], (uint)layerIndex));
                                }
                                else
                                {
                                    var combinedGroup = new List<(VectorOfVectorOfPoint contour, uint layerIndex)>();
                                    foreach (var index in overlappingGroupIndexes)
                                    {
                                        combinedGroup.AddRange(resinTrapGroups[index]);
                                    }

                                    for (var index = overlappingGroupIndexes.Count - 1; index >= 0; index--)
                                    {
                                        resinTrapGroups[overlappingGroupIndexes[index]].Clear();
                                        resinTrapGroups.RemoveAt(overlappingGroupIndexes[index]);
                                    }

                                    combinedGroup.Add((resinTraps[layerIndex][x], (uint)layerIndex));
                                    resinTrapGroups.Add(combinedGroup);
                                }
                            }
                        }
                        //}
                    });

                    /* anything that converted to a suction trap needs to removed from resinTraps. Loop backwards so indexes don't shift */
                    if (suctionCups[layerIndex] is not null)
                    {
                        for (var i = suctionCups[layerIndex].Count - 1; i >= 0; i--)
                        {
                            resinTraps[layerIndex].Remove(suctionCups[layerIndex][i]);
                            if (resinTraps[layerIndex].Count > 0) continue;
                            resinTraps[layerIndex] = null!;
                            break;
                        }
                    }
                }

                progress++;
            }

            if (currentAirMap is not null)
            {
                currentAirMap.Dispose();
                currentAirMap = null;
            }

            if (progress.Token.IsCancellationRequested) return GetResult();

            /* translate all contour points by ROI x and y */
            var offsetBy = new Point(SlicerFile.BoundingRectangle.X, SlicerFile.BoundingRectangle.Y);
            foreach (var listOfLayers in new[] { resinTraps, suctionCups })
            {
                Parallel.ForEach(listOfLayers.Where(list => list is not null), contoursGroups =>
                {
                    progress.PauseIfRequested();
                    for (var groupIndex = 0; groupIndex < contoursGroups.Count; groupIndex++)
                    {
                        var contours = contoursGroups[groupIndex];

                        var arrayOfArrayOfPoints = contours.ToArrayOfArray();

                        foreach (var pointArray in arrayOfArrayOfPoints)
                            for (var i = 0; i < pointArray.Length; i++)
                                pointArray[i].Offset(offsetBy);

                        contoursGroups[groupIndex].Dispose();
                        contoursGroups[groupIndex] = new VectorOfVectorOfPoint(arrayOfArrayOfPoints);
                    }

                    //progress.LockAndIncrement();
                });
            }

            if (progress.Token.IsCancellationRequested) return GetResult();

            if (resinTrapConfig.DetectSuctionCups)
                progress.Reset("Interpolating areas (Resin traps & suction cups)",
                    (uint)(resinTraps.Count(list => list is not null) + suctionCups.Count(list => list is not null)));
            else
                progress.Reset("Interpolating areas (Resin traps)", (uint)(resinTraps.Count(list => list is not null)));

            Parallel.Invoke(() =>
                {
                    var resinTrapGroups = new List<List<IssueOfContours>>();

                    for (var layerIndex = resinTraps.Length - 1; layerIndex >= 0; layerIndex--)
                    {
                        if (resinTraps[layerIndex] is null) continue;

                        /* select new LayerIssue(this[layerIndex], LayerIssue.IssueType.ResinTrap, area.Contour, area.BoundingRectangle)) */
                        foreach (var trap in resinTraps[layerIndex])
                        {
                            progress.PauseIfRequested();
                            if (progress.Token.IsCancellationRequested) return;

                            var area = EmguContours.GetContourArea(trap);
                            var rect = CvInvoke.BoundingRectangle(trap[0]);
                            var trapIssue = new IssueOfContours(SlicerFile[layerIndex], trap.ToArrayOfArray(), rect,
                                area);

                            var overlappingGroupIndexes = new List<int>();
                            for (var x = 0; x < resinTrapGroups.Count; x++)
                            {
                                if (resinTrapGroups[x][^1].LayerIndex != layerIndex &&
                                    resinTrapGroups[x][^1].LayerIndex != layerIndex + 1) continue;

                                using var vec = new VectorOfVectorOfPoint(resinTrapGroups[x][^1].Contours);
                                if (EmguContours.ContoursIntersect(trap, vec))
                                {
                                    overlappingGroupIndexes.Add(x);
                                }
                            }

                            if (overlappingGroupIndexes.Count == 0)
                            {
                                /* no overlaps, make a single issue */
                                resinTrapGroups.Add([trapIssue]);
                            }
                            else if (overlappingGroupIndexes.Count == 1)
                            {
                                resinTrapGroups[overlappingGroupIndexes[0]].Add(trapIssue);
                            }
                            else
                            {
                                var combinedGroup = new List<IssueOfContours>();
                                foreach (var index in overlappingGroupIndexes)
                                {
                                    combinedGroup.AddRange(resinTrapGroups[index]);
                                }

                                for (var index = overlappingGroupIndexes.Count - 1; index >= 0; index--)
                                {
                                    resinTrapGroups[overlappingGroupIndexes[index]].Clear();
                                    resinTrapGroups.RemoveAt(overlappingGroupIndexes[index]);
                                }

                                combinedGroup.Add(trapIssue);
                                resinTrapGroups.Add(combinedGroup);
                            }
                        }

                        progress.LockAndIncrement();
                    }

                    foreach (var group in resinTrapGroups)
                    {
                        if (group.AsValueEnumerable().Any(issue => issue.LayerIndex == 0))
                            continue; // Not a trap if on plate
                        AddIssue(new MainIssue(MainIssue.IssueType.ResinTrap, group));
                    }
                },
                () =>
                {
                    /* only report suction cup issues if enabled */
                    if (resinTrapConfig.DetectSuctionCups)
                    {
                        var minimumSuctionArea = resinTrapConfig.RequiredAreaToConsiderSuctionCup;
                        var suctionGroups = new List<List<IssueOfContours>>();

                        for (var layerIndex = suctionCups.Length - 1; layerIndex >= 0; layerIndex--)
                        {
                            if (suctionCups[layerIndex] is null) continue;

                            foreach (var trap in suctionCups[layerIndex])
                            {
                                progress.PauseIfRequested();
                                if (progress.Token.IsCancellationRequested) return;

                                var area = EmguContours.GetContourArea(trap);
                                if (area < minimumSuctionArea) continue;
                                var rect = CvInvoke.BoundingRectangle(trap[0]);

                                var trapIssue = new IssueOfContours(SlicerFile[layerIndex], trap.ToArrayOfArray(), rect,
                                    area);

                                var overlappingGroupIndexes = new List<int>();
                                for (var x = 0; x < suctionGroups.Count; x++)
                                {
                                    if (suctionGroups[x][^1].LayerIndex != layerIndex &&
                                        suctionGroups[x][^1].LayerIndex != layerIndex + 1) continue;
                                    using var vec = new VectorOfVectorOfPoint(suctionGroups[x][^1].Contours);
                                    if (EmguContours.ContoursIntersect(trap, vec))
                                    {
                                        overlappingGroupIndexes.Add(x);
                                    }
                                }

                                if (overlappingGroupIndexes.Count == 0)
                                {
                                    /* no overlaps, make a new group */
                                    suctionGroups.Add([trapIssue]);
                                }
                                else if (overlappingGroupIndexes.Count == 1)
                                {
                                    suctionGroups[overlappingGroupIndexes[0]].Add(trapIssue);
                                }
                                else
                                {
                                    var combinedGroup = new List<IssueOfContours>();
                                    /* iterate backwards to not screw up indexes */
                                    for (var i = overlappingGroupIndexes.Count - 1; i >= 0; i--)
                                    {
                                        var index = overlappingGroupIndexes[i];
                                        combinedGroup.AddRange(suctionGroups[index]);
                                        suctionGroups[index].Clear();
                                        suctionGroups.RemoveAt(index);
                                    }

                                    combinedGroup.Add(trapIssue);
                                    suctionGroups.Add(combinedGroup);
                                }
                            }

                            progress.LockAndIncrement();
                        }

                        foreach (var group in suctionGroups)
                        {
                            var mainIssue = new MainIssue(MainIssue.IssueType.SuctionCup, group);
                            if ((decimal)mainIssue.TotalHeight >= resinTrapConfig.RequiredHeightToConsiderSuctionCup)
                            {
                                AddIssue(mainIssue);
                            }
                        }
                    }
                });

        }

        return GetResult();
    }

    /// <summary>
    /// Detects the pixels of a layer that touch the bounds of the image.
    /// </summary>
    private static void DetectTouchingBounds(Layer layer, TouchingBoundDetectionConfiguration config,
        Func<MainIssue, bool> addIssue)
    {
        var bounds = layer.BoundingRectangle;
        var width = (int)layer.ResolutionX;
        var height = (int)layer.ResolutionY;

        bool touchTop = bounds.Top <= config.MarginTop;
        bool touchBottom = bounds.Bottom >= height - config.MarginBottom;
        bool touchLeft = bounds.Left <= config.MarginLeft;
        bool touchRight = bounds.Right >= width - config.MarginRight;

        // The layer pixels are only needed when the layer is actually close to a border
        if (!touchTop && !touchBottom && !touchLeft && !touchRight) return;

        using var sourceMat = layer.LayerMat;
        var sourceSpan = sourceMat.GetReadOnlySpan2DOfBytes();

        List<Point> pixels = [];
        int minx = int.MaxValue;
        int miny = int.MaxValue;
        int maxx = 0;
        int maxy = 0;

        if (touchTop || touchBottom)
        {
            for (int x = bounds.X; x < bounds.Right; x++) // Check Top and Bottom bounds
            {
                if (touchTop)
                {
                    for (int y = bounds.Y; y < config.MarginTop; y++) // Top
                    {
                        if (sourceSpan.DangerousGetReferenceAt(y, x) >= config.MinimumPixelBrightness)
                        {
                            pixels.Add(new Point(x, y));
                            minx = Math.Min(minx, x);
                            miny = Math.Min(miny, y);
                            maxx = Math.Max(maxx, x);
                            maxy = Math.Max(maxy, y);
                        }
                    }
                }

                if (touchBottom)
                {
                    for (int y = height - config.MarginBottom; y < bounds.Bottom; y++) // Bottom
                    {
                        if (sourceSpan.DangerousGetReferenceAt(y, x) >= config.MinimumPixelBrightness)
                        {
                            pixels.Add(new Point(x, y));
                            minx = Math.Min(minx, x);
                            miny = Math.Min(miny, y);
                            maxx = Math.Max(maxx, x);
                            maxy = Math.Max(maxy, y);
                        }
                    }
                }
            }
        }

        if (touchLeft || touchRight)
        {
            // Rows already covered by the top and bottom checks are skipped to not count them twice
            var sideStart = touchTop ? Math.Max(bounds.Y, config.MarginTop) : bounds.Y;
            var sideEnd = touchBottom ? Math.Min(bounds.Bottom, height - config.MarginBottom) : bounds.Bottom;

            for (int y = sideStart; y < sideEnd; y++) // Check Left and Right bounds
            {
                if (touchLeft)
                {
                    for (int x = bounds.X; x < config.MarginLeft; x++) // Left
                    {
                        if (sourceSpan.DangerousGetReferenceAt(y, x) >= config.MinimumPixelBrightness)
                        {
                            pixels.Add(new Point(x, y));
                            minx = Math.Min(minx, x);
                            miny = Math.Min(miny, y);
                            maxx = Math.Max(maxx, x);
                            maxy = Math.Max(maxy, y);
                        }
                    }
                }

                if (touchRight)
                {
                    for (int x = bounds.Right - config.MarginRight; x < bounds.Right; x++) // Right
                    {
                        if (sourceSpan.DangerousGetReferenceAt(y, x) >= config.MinimumPixelBrightness)
                        {
                            pixels.Add(new Point(x, y));
                            minx = Math.Min(minx, x);
                            miny = Math.Min(miny, y);
                            maxx = Math.Max(maxx, x);
                            maxy = Math.Max(maxy, y);
                        }
                    }
                }
            }
        }

        if (pixels.Count > 0)
        {
            addIssue(new MainIssue(MainIssue.IssueType.TouchingBound, new IssueOfPoints(layer, pixels,
                new Rectangle(minx, miny, maxx - minx + 1, maxy - miny + 1))));
        }
    }

    /// <summary>
    /// Checks if any pixel of <paramref name="current"/> bright enough to be considered is not supported by
    /// <paramref name="previous"/> (brightness below <paramref name="supportBrightness"/>).
    /// </summary>
    private static bool HasUnsupportedPixels(Mat current, Mat previous, byte requiredBrightness,
        byte supportBrightness)
    {
        var width = current.Width;
        var vectorSize = Vector<byte>.Count;
        var vectorRequired = new Vector<byte>(requiredBrightness);
        var vectorSupport = new Vector<byte>(supportBrightness);

        for (var y = 0; y < current.Height; y++)
        {
            var currentRow = current.GetReadOnlyRowSpanOfBytes(y);
            var previousRow = previous.GetReadOnlyRowSpanOfBytes(y);

            var x = 0;
            for (; x <= width - vectorSize; x += vectorSize)
            {
                var currentVector = new Vector<byte>(currentRow.Slice(x));
                var previousVector = new Vector<byte>(previousRow.Slice(x));
                if ((Vector.GreaterThanOrEqual(currentVector, vectorRequired) &
                     Vector.LessThan(previousVector, vectorSupport)) != Vector<byte>.Zero)
                {
                    return true;
                }
            }

            for (; x < width; x++)
            {
                if (currentRow[x] >= requiredBrightness && previousRow[x] < supportBrightness) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Counts the pixels of a run that are bright enough to be considered, and from those how many are supported by
    /// the previous layer.
    /// </summary>
    private static void CountPixels(ReadOnlySpan<byte> currentRow, ReadOnlySpan<byte> previousRow, int start, int end,
        byte requiredBrightness, byte supportBrightness, ref int pixelCount, ref int supportedCount)
    {
        var x = start;
        const int vectorSize = 16;

        if (end - start + 1 >= vectorSize)
        {
            var vectorRequired = Vector128.Create(requiredBrightness);
            var vectorSupport = Vector128.Create(supportBrightness);
            ref var currentReference = ref MemoryMarshal.GetReference(currentRow);
            ref var previousReference = ref MemoryMarshal.GetReference(previousRow);

            for (; x + vectorSize <= end + 1; x += vectorSize)
            {
                var considered = Vector128.GreaterThanOrEqual(
                    Vector128.LoadUnsafe(ref currentReference, (nuint)x), vectorRequired);
                var supported = considered & Vector128.GreaterThanOrEqual(
                    Vector128.LoadUnsafe(ref previousReference, (nuint)x), vectorSupport);

                pixelCount += BitOperations.PopCount(Vector128.ExtractMostSignificantBits(considered));
                supportedCount += BitOperations.PopCount(Vector128.ExtractMostSignificantBits(supported));
            }
        }

        for (; x <= end; x++)
        {
            if (currentRow[x] < requiredBrightness) continue;
            pixelCount++;
            if (previousRow[x] >= supportBrightness) supportedCount++;
        }
    }

    /// <summary>
    /// Counts the non-zero bytes.
    /// </summary>
    private static int CountNonZero(ReadOnlySpan<byte> values)
    {
        var count = 0;
        foreach (var value in values)
        {
            if (value != 0) count++;
        }

        return count;
    }
    /// <summary>
    /// Runs <paramref name="body"/> over chunks of rows in parallel.
    /// </summary>
    private static void ForRows(int rows, Action<int, int> body)
    {
        var chunks = Math.Min(rows, Environment.ProcessorCount * 2);
        if (chunks <= 1)
        {
            body(0, rows);
            return;
        }

        var chunkRows = (rows + chunks - 1) / chunks;
        Parallel.For(0, chunks, CoreSettings.ParallelOptions, chunk =>
        {
            var start = chunk * chunkRows;
            var end = Math.Min(rows, start + chunkRows);
            if (start < end) body(start, end);
        });
    }

    /// <summary>
    /// Inverts all the bits of the 8-bit single channel <paramref name="source"/> into <paramref name="destination"/>.
    /// Same result as <see cref="CvInvoke.BitwiseNot(IInputArray, IOutputArray, IInputArray?)"/> but multi-threaded and SIMD.
    /// </summary>
    private static void BitwiseNot(Mat source, Mat destination)
    {
        if (source.Depth != DepthType.Cv8U || source.NumberOfChannels != 1)
        {
            CvInvoke.BitwiseNot(source, destination);
            return;
        }

        destination.Create(source.Rows, source.Cols, DepthType.Cv8U, 1);
        var width = source.Cols;
        var vectorSize = Vector<byte>.Count;

        ForRows(source.Rows, (start, end) =>
        {
            for (var y = start; y < end; y++)
            {
                var sourceRow = source.GetReadOnlyRowSpanOfBytes(y);
                var destinationRow = destination.GetRowSpanOfBytes(y);

                var x = 0;
                for (; x <= width - vectorSize; x += vectorSize)
                {
                    Vector.OnesComplement(new Vector<byte>(sourceRow.Slice(x))).CopyTo(destinationRow.Slice(x));
                }

                for (; x < width; x++)
                {
                    destinationRow[x] = (byte)~sourceRow[x];
                }
            }
        });
    }

    /// <summary>
    /// Updates the air map: <c>airMap = saturate(airMap - solid) | layerAirMap</c> for 8-bit single channel mats.
    /// Same result as a <see cref="CvInvoke.Subtract(IInputArray, IInputArray, IOutputArray, IInputArray?, DepthType)"/>
    /// followed by a <see cref="CvInvoke.BitwiseOr(IInputArray, IInputArray, IOutputArray, IInputArray?)"/>,
    /// but in a single multi-threaded and SIMD pass.
    /// </summary>
    private static void SubtractAndOr(Mat airMap, Mat solid, Mat layerAirMap)
    {
        if (airMap.Depth != DepthType.Cv8U || airMap.NumberOfChannels != 1 ||
            solid.Depth != DepthType.Cv8U || solid.NumberOfChannels != 1 ||
            layerAirMap.Depth != DepthType.Cv8U || layerAirMap.NumberOfChannels != 1)
        {
            CvInvoke.Subtract(airMap, solid, airMap);
            CvInvoke.BitwiseOr(layerAirMap, airMap, airMap);
            return;
        }

        var width = airMap.Cols;
        var vectorSize = Vector<byte>.Count;

        ForRows(airMap.Rows, (start, end) =>
        {
            for (var y = start; y < end; y++)
            {
                var airRow = airMap.GetRowSpanOfBytes(y);
                var solidRow = solid.GetReadOnlyRowSpanOfBytes(y);
                var layerAirRow = layerAirMap.GetReadOnlyRowSpanOfBytes(y);

                var x = 0;
                for (; x <= width - vectorSize; x += vectorSize)
                {
                    var air = new Vector<byte>(airRow.Slice(x));
                    var solidVector = new Vector<byte>(solidRow.Slice(x));
                    var layerAir = new Vector<byte>(layerAirRow.Slice(x));
                    (Vector.SubtractSaturate(air, solidVector) | layerAir).CopyTo(airRow.Slice(x));
                }

                for (; x < width; x++)
                {
                    airRow[x] = (byte)(Math.Max(airRow[x] - solidRow[x], 0) | layerAirRow[x]);
                }
            }
        });
    }

    /// <summary>
    /// Decodes layers ahead, in parallel and in sequence order, while they are being consumed.
    /// </summary>
    private sealed class LayerMatPrefetcher : IDisposable
    {
        private readonly Queue<Task<Mat>> _queue = new();
        private readonly Func<int, Mat> _decode;
        private readonly int _lookahead;
        private readonly int _step;
        private readonly int _last;
        private int _next;

        /// <param name="first">The first layer index to decode.</param>
        /// <param name="last">The last layer index to decode, inclusive.</param>
        /// <param name="ascending">True to go from <paramref name="first"/> up to <paramref name="last"/>, otherwise down.</param>
        /// <param name="decode">The layer decoder.</param>
        /// <param name="lookahead">How many layers to keep decoded ahead.</param>
        public LayerMatPrefetcher(int first, int last, bool ascending, Func<int, Mat> decode, int lookahead)
        {
            _next = first;
            _last = last;
            _step = ascending ? 1 : -1;
            _decode = decode;
            _lookahead = lookahead;
            Fill();
        }

        private bool HasMore => _step > 0 ? _next <= _last : _next >= _last;

        private void Fill()
        {
            while (_queue.Count < _lookahead && HasMore)
            {
                var layerIndex = _next;
                _next += _step;
                _queue.Enqueue(Task.Run(() => _decode(layerIndex)));
            }
        }

        /// <summary>
        /// Gets the next decoded layer, the caller must dispose it.
        /// </summary>
        public Mat Next()
        {
            var task = _queue.Dequeue();
            Fill();
            return task.GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            // Layers decoded but never consumed, for example after a cancellation
            while (_queue.TryDequeue(out var task))
            {
                try
                {
                    task.GetAwaiter().GetResult().Dispose();
                }
                catch
                {
                    // Nothing to release if the decode failed
                }
            }
        }
    }

    private sealed class ScopeCleanup(Action cleanup) : IDisposable
    {
        public void Dispose() => cleanup();
    }
    public MainIssue[] DrillSuctionCupsForIssues(IEnumerable<MainIssue> issues, int ventHoleDiameter,
        OperationProgress progress)
    {
        var drillOps = new List<PixelOperation>();
        var drilledIssues = new List<MainIssue>();
        var radius = SlicerFile.PixelsToNormalizedPitch(ventHoleDiameter / 2);
        //var suctionReliefSize = (ushort)Math.Max(SlicerFile.PpmmMax * 0.8, 17);
        /* for each suction cup issue that is an initial layer */
        foreach (var mainIssue in issues)
        {
            var drillPoint = GetDrillLocation((IssueOfContours)mainIssue[0], radius);
            if (drillPoint.IsAnyNegative()) continue;
            drillOps.Add(new PixelDrainHole(mainIssue.StartLayerIndex, drillPoint, (ushort)ventHoleDiameter));
            drilledIssues.Add(mainIssue);
        }

        SlicerFile.DrawModifications(drillOps, progress);

        return drilledIssues.ToArray();
    }

    public static Point GetDrillLocation(IssueOfContours issue, Size radius)
    {
        using var vecCentroid = new VectorOfPoint(issue.Contours[0]);
        var centroid = EmguContour.GetCentroid(vecCentroid);
        if (centroid.IsAnyNegative()) return centroid;
        using var circleCheck = EmguCvExtensions.InitMat(issue.BoundingRectangle.Size);
        using var contourMat = EmguCvExtensions.InitMat(issue.BoundingRectangle.Size);

        var inverseOffset = new Point(issue.BoundingRectangle.X * -1, issue.BoundingRectangle.Y * -1);
        using var vec = new VectorOfVectorOfPoint(issue.Contours);
        CvInvoke.DrawContours(contourMat, vec, -1, EmguCvExtensions.WhiteColor, -1, LineType.EightConnected, null,
            int.MaxValue, inverseOffset);
        circleCheck.DrawCircle(new(centroid.X + inverseOffset.X, centroid.Y + inverseOffset.Y), radius,
            EmguCvExtensions.WhiteColor, -1);
        CvInvoke.BitwiseAnd(circleCheck, contourMat, circleCheck);

        return CvInvoke.HasNonZero(circleCheck)
            ? centroid /* 5px centroid is inside layer! drill baby drill */
            : new Point(-1, -1); /* centroid is not inside the actual contour, no drill */
    }
}