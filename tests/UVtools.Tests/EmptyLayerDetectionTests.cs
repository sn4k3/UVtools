/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using Xunit;

namespace UVtools.Tests;

public class EmptyLayerDetectionTests
{
    private static FileFormat CreateFile(params bool[] solidLayers)
    {
        var slicerFile = PcbFixtures.CreateSlicerFile();
        var layers = new Layer[solidLayers.Length];
        for (var i = 0; i < layers.Length; i++)
        {
            using var mat = new Mat(slicerFile.Resolution, DepthType.Cv8U, 1);
            mat.SetTo(new MCvScalar(0));
            if (solidLayers[i])
            {
                CvInvoke.Rectangle(mat, new Rectangle(400, 400, 200, 200), new MCvScalar(255), -1);
            }

            layers[i] = new Layer((uint)i, mat, slicerFile);
        }

        slicerFile.Init(layers);
        return slicerFile;
    }

    private static uint[] DetectEmptyLayers(FileFormat slicerFile, bool ignoreStarting, bool ignoreLoose,
        bool ignoreEnding)
    {
        var config = new IssuesDetectionConfiguration();
        config.DisableAll();
        config.EmptyLayerConfig.Enabled = true;
        config.EmptyLayerConfig.IgnoreStartingEmptyLayers = ignoreStarting;
        config.EmptyLayerConfig.IgnoreLooseEmptyLayers = ignoreLoose;
        config.EmptyLayerConfig.IgnoreEndingEmptyLayers = ignoreEnding;

        var detection = Task.Run(() => slicerFile.IssueManager.DetectIssues(config, new OperationProgress()));
        Assert.True(detection.Wait(TimeSpan.FromSeconds(30)), "Empty-layer detection did not finish.");

        return detection.Result
            .Where(issue => issue.Type == MainIssue.IssueType.EmptyLayer)
            .Select(issue => issue.StartLayerIndex)
            .OrderBy(index => index)
            .ToArray();
    }

    [Fact]
    public void EmptyLayersAreClassifiedAsStartingLooseOrEnding()
    {
        // 0,1 empty (starting) | 2 solid | 3 empty (loose) | 4 solid | 5,6 empty (ending)
        using var slicerFile = CreateFile(false, false, true, false, true, false, false);

        Assert.Equal(new uint[] { 0, 1, 3, 5, 6 }, DetectEmptyLayers(slicerFile, false, false, false));
        Assert.Equal(new uint[] { 3, 5, 6 }, DetectEmptyLayers(slicerFile, true, false, false));
        Assert.Equal(new uint[] { 0, 1, 5, 6 }, DetectEmptyLayers(slicerFile, false, true, false));
        Assert.Equal(new uint[] { 0, 1, 3 }, DetectEmptyLayers(slicerFile, false, false, true));
        Assert.Empty(DetectEmptyLayers(slicerFile, true, true, true));
    }

    [Fact]
    public void AllEmptyLayersAreClassifiedAsStarting()
    {
        using var slicerFile = CreateFile(false, false, false);

        Assert.Equal(new uint[] { 0, 1, 2 }, DetectEmptyLayers(slicerFile, false, true, true));
        Assert.Empty(DetectEmptyLayers(slicerFile, true, false, false));
    }
}
