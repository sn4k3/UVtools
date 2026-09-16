/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using Xunit;

namespace UVtools.Tests;

public class OperationCalibrateExposureFinderGammaTests
{
    [Fact]
    public void CalculateBrightnessFactor_CalculatesCorrectly()
    {
        // Full white is always 1.0 regardless of gamma
        Assert.Equal(1.0m, OperationCalibrateExposureFinder.CalculateBrightnessFactor(255, 1.0m));
        Assert.Equal(1.0m, OperationCalibrateExposureFinder.CalculateBrightnessFactor(255, 3.0m));

        // Full black is always 0.0
        Assert.Equal(0.0m, OperationCalibrateExposureFinder.CalculateBrightnessFactor(0, 3.0m));

        // Linear gamma = 1.0: brightness 204 is exactly 80% (204 / 255 = 0.8)
        Assert.Equal(0.80m, Math.Round(OperationCalibrateExposureFinder.CalculateBrightnessFactor(204, 1.0m), 2));

        // Panel gamma = 3.0: brightness 204 is 0.8^3 = 0.512 (51.2% dosage)
        Assert.Equal(0.512m, Math.Round(OperationCalibrateExposureFinder.CalculateBrightnessFactor(204, 3.0m), 3));
    }

    [Fact]
    public void CalculateBrightnessFromFactor_CalculatesCorrectly()
    {
        // Linear: 80% dose corresponds to brightness 204
        Assert.Equal((byte)204, OperationCalibrateExposureFinder.CalculateBrightnessFromFactor(0.80m, 1.0m));

        // Gamma 3.0: 80% dose corresponds to brightness 237 (255 * 0.80^(1/3) ~ 236.72)
        Assert.Equal((byte)237, OperationCalibrateExposureFinder.CalculateBrightnessFromFactor(0.80m, 3.0m));

        // Clamping checks
        Assert.Equal((byte)255, OperationCalibrateExposureFinder.CalculateBrightnessFromFactor(1.5m, 3.0m));
        Assert.Equal((byte)0, OperationCalibrateExposureFinder.CalculateBrightnessFromFactor(-0.5m, 3.0m));
    }

    [Fact]
    public void ExposureItem_BrightnessPercent_ReflectsGamma()
    {
        // Linear gamma 1.0
        var itemLinear = new ExposureItem(0.05m, 20m, 3.0m, 204, 1.0m);
        Assert.Equal(80.00m, itemLinear.BrightnessPercent);

        // Gamma 3.0
        var itemGamma3 = new ExposureItem(0.05m, 20m, 3.0m, 204, 3.0m);
        Assert.Equal(51.20m, itemGamma3.BrightnessPercent);

        // Gamma 3.0 for brightness 237 (~80% dose)
        var itemGamma3Dose80 = new ExposureItem(0.05m, 20m, 3.0m, 237, 3.0m);
        Assert.Equal(80.28m, itemGamma3Dose80.BrightnessPercent);
    }

    [Fact]
    public void GetDefaultBrightnessValues_ProducesAccurateStepValues()
    {
        // Gamma 1.0 produces traditional linear default steps: 100%, 95%, 90%, 85%, 80%, 75%
        Assert.Equal("255, 242, 230, 217, 204, 191", OperationCalibrateExposureFinder.GetDefaultBrightnessValues(1.0m));

        // Gamma 3.0 produces gamma-compensated 100%, 95%, 90%, 85%, 80%, 75% dosage values
        Assert.Equal("255, 251, 246, 242, 237, 232", OperationCalibrateExposureFinder.GetDefaultBrightnessValues(3.0m));
    }

    [Fact]
    public void CalculateBrightnessFromExposureTime_CompensatesForGamma()
    {
        using var slicerFile = CreateFile();
        slicerFile.ExposureTime = 3.0f;
        slicerFile.BottomExposureTime = 20.0f;

        var op = new OperationCalibrateExposureFinder(slicerFile)
        {
            MultipleBrightnessGamma = 3.0m
        };

        // For target 2.40s (80% of 3.00s baseline), gamma 3.0 should yield brightness 237
        Assert.Equal((byte)237, op.CalculateBrightnessFromExposureTime(2.40m));

        // If gamma is set to 1.0, 2.40s should yield brightness 204
        op.MultipleBrightnessGamma = 1.0m;
        Assert.Equal((byte)204, op.CalculateBrightnessFromExposureTime(2.40m));
    }

    [Fact]
    public void MultipleBrightnessTable_CalculatesEffectiveExposureAndPercent()
    {
        using var slicerFile = CreateFile();
        slicerFile.ExposureTime = 3.0f;
        slicerFile.BottomExposureTime = 20.0f;

        var op = new OperationCalibrateExposureFinder(slicerFile)
        {
            MultipleBrightnessGamma = 3.0m,
            MultipleBrightnessValues = "255, 237, 204"
        };

        var table = op.MultipleBrightnessTable;
        Assert.Equal(3, table.Count);

        // 255: 100%, 3.00s
        Assert.Equal(255, table[0].Brightness);
        Assert.Equal(100.00m, table[0].BrightnessPercent);
        Assert.Equal(3.00m, table[0].Exposure);

        // 237: ~80.28%, ~2.41s
        Assert.Equal(237, table[1].Brightness);
        Assert.Equal(80.28m, table[1].BrightnessPercent);
        Assert.Equal(2.41m, table[1].Exposure);

        // 204: 51.20%, 1.54s (Math.Round(3.00 * 0.512, 2) = 1.54)
        Assert.Equal(204, table[2].Brightness);
        Assert.Equal(51.20m, table[2].BrightnessPercent);
        Assert.Equal(1.54m, table[2].Exposure);
    }

    [Fact]
    public void GammaChange_AutoUpdatesDefaultValues_PreservesCustomValues()
    {
        using var slicerFile = CreateFile();
        var op = new OperationCalibrateExposureFinder(slicerFile);

        // Initial default with gamma 3.0
        Assert.Equal(3.0m, op.MultipleBrightnessGamma);
        Assert.Equal(OperationCalibrateExposureFinder.GetDefaultBrightnessValues(3.0m), op.MultipleBrightnessValues);

        // Changing gamma to 2.2 updates default values automatically
        op.MultipleBrightnessGamma = 2.2m;
        Assert.Equal(OperationCalibrateExposureFinder.GetDefaultBrightnessValues(2.2m), op.MultipleBrightnessValues);

        // User enters custom values
        op.MultipleBrightnessValues = "255, 180, 120";

        // Changing gamma now should preserve the custom values
        op.MultipleBrightnessGamma = 2.5m;
        Assert.Equal("255, 180, 120", op.MultipleBrightnessValues);

        // Reset restores default for current gamma
        op.ResetMultipleBrightnessValues();
        Assert.Equal(OperationCalibrateExposureFinder.GetDefaultBrightnessValues(2.5m), op.MultipleBrightnessValues);
    }

    [Fact]
    public void InitWithSlicerFile_MigratesLegacyDefaultBrightnessValues()
    {
        using var slicerFile = CreateFile();
        var op = new OperationCalibrateExposureFinder
        {
            MultipleBrightnessValues = "255, 242, 230, 217, 204, 191"
        };

        op.SlicerFile = slicerFile;

        Assert.Equal(OperationCalibrateExposureFinder.GetDefaultBrightnessValues(3.0m), op.MultipleBrightnessValues);
    }

    [Fact]
    public void EmulatedAntiAliasing_UsesSelectedGamma()
    {
        using var slicerFile = CreateFile(ChituboxFile.MAGIC_CBDDLP);
        var op = new OperationCalibrateExposureFinder(slicerFile)
        {
            MultipleBrightnessGamma = 3.0m,
            MultipleBrightnessValues = "204"
        };

        Assert.True(slicerFile.IsAntiAliasingEmulated);
        Assert.Equal(3.0m, op.EffectiveGamma);
        Assert.Equal(51.20m, op.MultipleBrightnessTable[0].BrightnessPercent);
        Assert.Equal(1.54m, op.MultipleBrightnessTable[0].Exposure);

        op.ResetMultipleBrightnessValues();
        Assert.Equal(OperationCalibrateExposureFinder.GetDefaultBrightnessValues(3.0m), op.MultipleBrightnessValues);
    }

    private static ChituboxFile CreateFile(uint magic = ChituboxFile.MAGIC_CTBv4)
    {
        var slicerFile = new ChituboxFile
        {
            Resolution = new Size(100, 100),
            Version = 4,
            BottomLayerCount = 2,
            ExposureTime = 3.0f,
            BottomExposureTime = 20.0f,
            LayerHeight = 0.05f
        };
        slicerFile.HeaderSettings.Magic = magic;

        var layers = new Layer[4];
        for (var i = 0; i < layers.Length; i++)
        {
            using var mat = new Mat(slicerFile.Resolution, DepthType.Cv8U, 1);
            mat.SetTo(new MCvScalar(0));
            layers[i] = new Layer((uint)i, mat, slicerFile);
        }

        slicerFile.Init(layers);
        return slicerFile;
    }
}
