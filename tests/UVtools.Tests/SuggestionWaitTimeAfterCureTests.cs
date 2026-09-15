/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Suggestions;
using Xunit;

namespace UVtools.Tests;

public class SuggestionWaitTimeAfterCureTests
{
    [Fact]
    public void ExecuteUsesBottomHeightAndTransitionsToNormalWaitTime()
    {
        using var file = CreateFile(10);
        var suggestion = new SuggestionWaitTimeAfterCure
        {
            SlicerFile = file,
            BottomHeight = 0.30m,
            FixedBottomWaitTimeAfterCure = 7,
            FixedWaitTimeAfterCure = 1,
            WaitTimeAfterCureTransitionLayerCount = 2,
            MinimumBottomWaitTimeAfterCure = 0,
            MaximumBottomWaitTimeAfterCure = 20,
            MinimumWaitTimeAfterCure = 0,
            MaximumWaitTimeAfterCure = 20
        };

        Assert.True(suggestion.Execute());

        Assert.Equal(7, file.BottomWaitTimeAfterCure);
        Assert.Equal(1, file.WaitTimeAfterCure);
        var expectedWaitTimes = new[] { 7f, 7f, 7f, 7f, 7f, 7f, 5f, 3f, 1f, 1f };
        for (var i = 0; i < expectedWaitTimes.Length; i++)
        {
            Assert.Equal(expectedWaitTimes[i], file[i].WaitTimeAfterCure);
        }
    }

    private static ChituboxFile CreateFile(int layerCount)
    {
        var file = new ChituboxFile
        {
            Resolution = new Size(32, 32),
            Version = 4,
            LayerHeight = 0.05f,
            BottomLayerCount = 2,
            BottomExposureTime = 20,
            ExposureTime = 2
        };
        file.HeaderSettings.Magic = ChituboxFile.MAGIC_CTBv4;

        var layers = new Layer[layerCount];
        for (var i = 0; i < layers.Length; i++)
        {
            using var mat = new Mat(file.Resolution, DepthType.Cv8U, 1);
            layers[i] = new Layer((uint)i, mat, file);
        }

        file.Init(layers);
        return file;
    }
}