/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Numerics;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.Layers;
using Xunit;

namespace UVtools.Tests;

public class LayerMechanicalAnalysisTests
{
    private static readonly Vector2 EmptyCentroid = new(float.NaN);

    [Fact]
    public void AreaDeltaPercent_IgnoresInsignificantTransitions()
    {
        float[] areas = [10f, 20f, 20.5f, 5f, 0.04f, 30f];

        var delta = LayerMechanicalAnalysis.CalculateAreaDeltaPercent(areas);

        Assert.Equal(areas.Length, delta.Length);
        Assert.Equal(0f, delta[0]); // No layer below
        Assert.Equal(100f, delta[1], 0.001f);
        Assert.Equal(0f, delta[2]); // 0.5 mm² is not significant
        Assert.Equal(-75.61f, delta[3], 0.01f); // Signed
        Assert.Equal(-99.2f, delta[4], 0.01f);
        Assert.Equal(0f, delta[5]); // Growing from almost nothing is noise, not +74900%
    }

    [Fact]
    public void PeelSpike_UsesTheSameThresholdsAsTheGuard()
    {
        Assert.True(LayerMechanicalAnalysis.IsPeelSpike(10f, 20f));
        Assert.False(LayerMechanicalAnalysis.IsPeelSpike(10f, 13f)); // Not above 4 mm²
        Assert.False(LayerMechanicalAnalysis.IsPeelSpike(100f, 130f)); // Not +35%
        Assert.False(LayerMechanicalAnalysis.IsPeelSpike(20f, 10f)); // Shrinking
        Assert.False(LayerMechanicalAnalysis.IsPeelSpike(LayerMechanicalAnalysis.MinPreviousArea, 100f));
    }

    [Fact]
    public void CentroidDrift_IsZeroNextToEmptyLayers()
    {
        Vector2[] centroids = [new(0f, 0f), new(3f, 4f), EmptyCentroid, new(3f, 4f), new(3f, 5f)];

        var drift = LayerMechanicalAnalysis.CalculateCentroidDrift(centroids);

        Assert.Equal([0f, 5f, 0f, 0f, 1f], drift);
    }

    [Fact]
    public void RiskScores_AreNormalizedByTheirOwnScale()
    {
        // The only non-zero value of each series is its own scale, so each component normalizes to 1
        var risk = LayerMechanicalAnalysis.CalculateRiskScores([0f, 200f, 0f], [0f, 0f, 2f]);

        Assert.Equal(0f, risk[0]);
        Assert.Equal(0.6f, risk[1], 0.0001f);
        Assert.Equal(0.4f, risk[2], 0.0001f);
    }

    [Fact]
    public void RiskScores_ClampOutliersAndRankThemRed()
    {
        var areaDelta = new float[20];
        Array.Fill(areaDelta, 10f);
        areaDelta[7] = -1000f;

        var risk = LayerMechanicalAnalysis.CalculateRiskScores(areaDelta, new float[20]);

        // 0.6 weight x the maximum of 5, no matter how extreme the outlier is
        Assert.Equal(3f, risk[7], 0.0001f);
        Assert.Equal(LayerMechanicalRiskLevel.Red, LayerMechanicalAnalysis.GetRiskLevel(risk[7]));
        Assert.Equal(LayerMechanicalRiskLevel.Green, LayerMechanicalAnalysis.GetRiskLevel(risk[0]));
    }

    [Fact]
    public void RiskScores_AreZeroWithoutAnyChange()
    {
        var risk = LayerMechanicalAnalysis.CalculateRiskScores(new float[4], new float[4]);

        Assert.All(risk, score => Assert.Equal(0f, score));
    }

    [Theory]
    [InlineData(0f, LayerMechanicalRiskLevel.Green)]
    [InlineData(0.99f, LayerMechanicalRiskLevel.Green)]
    [InlineData(1f, LayerMechanicalRiskLevel.Yellow)]
    [InlineData(1.99f, LayerMechanicalRiskLevel.Yellow)]
    [InlineData(2f, LayerMechanicalRiskLevel.Orange)]
    [InlineData(2.99f, LayerMechanicalRiskLevel.Orange)]
    [InlineData(3f, LayerMechanicalRiskLevel.Red)]
    [InlineData(4.5f, LayerMechanicalRiskLevel.Red)]
    public void RiskLevel_FollowsTheThresholds(float score, LayerMechanicalRiskLevel expected)
    {
        Assert.Equal(expected, LayerMechanicalAnalysis.GetRiskLevel(score));
    }

    [Fact]
    public void Analyze_ListsTheOrangeAndRedLayers()
    {
        // Layers alternating between 100 and 110 mm², with a single layer jumping to 300 mm²
        var areas = new float[31];
        var centroids = new Vector2[31];
        for (var i = 0; i < areas.Length; i++)
        {
            areas[i] = i % 2 == 0 ? 100f : 110f;
            centroids[i] = new Vector2(10f, 10f);
        }

        areas[16] = 300f;

        var analysis = LayerMechanicalAnalysis.Analyze(areas, centroids);

        Assert.Equal(areas.Length, analysis.Count);
        Assert.Equal(172.73f, analysis.AreaDeltaPercent[16], 0.01f);
        Assert.Equal(0f, analysis.CentroidDriftScale); // The centroid never moves, so it has no weight

        // The jump is unusual for this file, its way back is not
        Assert.Equal([16], analysis.HighRiskLayers);
        Assert.Equal(LayerMechanicalRiskLevel.Orange, LayerMechanicalAnalysis.GetRiskLevel(analysis.RiskScores[16]));
        Assert.Equal(analysis.RiskScores[16], analysis.MaxRiskScore);
        Assert.Equal(LayerMechanicalRiskLevel.Green, LayerMechanicalAnalysis.GetRiskLevel(analysis.RiskScores[17]));
    }

    [Fact]
    public void Analyze_RejectsMismatchedLengths()
    {
        Assert.Throws<ArgumentException>(() => LayerMechanicalAnalysis.Analyze(new float[3], new Vector2[2]));
    }

    [Fact]
    public void CalculateCentroids_ReadsTheLayerPixels()
    {
        var slicerFile = PcbFixtures.CreateSlicerFile(); // 0.1 mm pixels
        var layers = new Layer[3];
        for (var i = 0; i < layers.Length; i++)
        {
            using var mat = new Mat(slicerFile.Resolution, DepthType.Cv8U, 1);
            mat.SetTo(new MCvScalar(0));
            if (i > 0)
            {
                // The second layer is 100 px, 10 mm, to the right of the first, a filled OpenCV rectangle includes its far edge
                CvInvoke.Rectangle(mat, new Rectangle(i == 1 ? 400 : 500, 400, 200, 200), new MCvScalar(255), -1);
            }

            layers[i] = new Layer((uint)i, mat, slicerFile);
        }

        slicerFile.Init(layers);

        var centroids = LayerMechanicalAnalysis.CalculateCentroids(slicerFile);

        Assert.True(float.IsNaN(centroids[0].X));
        Assert.Equal(50f, centroids[1].X, 0.01f);
        Assert.Equal(50f, centroids[1].Y, 0.01f);
        Assert.Equal(60f, centroids[2].X, 0.01f);
        Assert.Equal(50f, centroids[2].Y, 0.01f);
        Assert.Equal(10f, LayerMechanicalAnalysis.CalculateCentroidDrift(centroids)[2], 0.01f);
    }
}
