/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;

namespace UVtools.Core.Layers;

/// <summary>
/// How unusual a layer transition is, relative to the rest of the same file.
/// </summary>
public enum LayerMechanicalRiskLevel
{
    /// <summary>Nothing unusual compared with the rest of the file, shown in green.</summary>
    None,

    /// <summary>A mildly unusual transition, shown in yellow.</summary>
    Low,

    /// <summary>A clearly unusual transition, worth a look, shown in orange.</summary>
    Medium,

    /// <summary>One of the most abrupt transitions of the file, shown in red.</summary>
    High
}

/// <summary>
/// Mechanical analysis of the layer to layer transitions of a file: how much the cross-section area changes, how far
/// its centroid moves, and a combined score of both.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RiskScores"/> are <b>relative to the analysed file</b>: they tell which transitions are unusual
/// compared with the rest of the same model, they are not a prediction of print failure and cannot be compared between
/// different files.
/// </para>
/// <para>
/// The math is exposed as static methods working on plain arrays, so it can be used without decoding a file.
/// </para>
/// </remarks>
public sealed class LayerMechanicalAnalysis
{
    #region Constants

    /// <summary>
    /// The previous layer area, in mm², at or below which a transition is too noisy to be meaningful, like the tip of
    /// a support starting from nothing.
    /// </summary>
    public const float MinPreviousArea = 0.05f;

    /// <summary>
    /// The minimum absolute area change, in mm², for a transition to be significant.
    /// </summary>
    public const float MinAreaDelta = 4.0f;

    /// <summary>
    /// The minimum area growth ratio (0.35 = +35%) of a layer to be a peel spike.
    /// </summary>
    public const float PeelSpikeMinAreaRatio = 0.35f;

    /// <summary>
    /// The percentile of the non-zero values used as the scale of the area change and of the centroid drift.
    /// </summary>
    public const float ScalePercentile = 95f;

    /// <summary>
    /// The maximum value of each normalized component of the risk score.
    /// </summary>
    public const float MaxNormalizedComponent = 5f;

    /// <summary>
    /// The weight of the normalized area change on the risk score.
    /// </summary>
    public const float AreaDeltaWeight = 0.6f;

    /// <summary>
    /// The weight of the normalized centroid drift on the risk score.
    /// </summary>
    public const float CentroidDriftWeight = 0.4f;

    /// <summary>
    /// The risk score from which a layer is <see cref="LayerMechanicalRiskLevel.Low"/>.
    /// </summary>
    public const float LowRiskScore = 1f;

    /// <summary>
    /// The risk score from which a layer is <see cref="LayerMechanicalRiskLevel.Medium"/>.
    /// </summary>
    public const float MediumRiskScore = 2f;

    /// <summary>
    /// The risk score from which a layer is <see cref="LayerMechanicalRiskLevel.High"/>.
    /// </summary>
    public const float HighRiskScore = 3f;

    /// <summary>
    /// The pixel size, in mm, to use when the file does not know its display size.
    /// </summary>
    private const float FallbackPixelSize = 0.035f;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the number of analysed layers.
    /// </summary>
    public int Count => AreaDeltaPercent.Count;

    /// <summary>
    /// Gets the signed area change of each layer relative to the layer below, in percent. It is zero for the first
    /// layer and for the insignificant transitions, see <see cref="CalculateAreaDeltaPercent"/>.
    /// </summary>
    public IReadOnlyList<float> AreaDeltaPercent { get; }

    /// <summary>
    /// Gets the area centroid of each layer in mm, or <see cref="float.NaN"/> components for an empty layer.
    /// </summary>
    public IReadOnlyList<Vector2> Centroids { get; }

    /// <summary>
    /// Gets the distance in mm the centroid of each layer moved relative to the layer below. It is zero for the first
    /// layer and when any of the two layers is empty.
    /// </summary>
    public IReadOnlyList<float> CentroidDrift { get; }

    /// <summary>
    /// Gets the relative risk score of each layer, from 0 to
    /// <see cref="MaxNormalizedComponent"/> × (<see cref="AreaDeltaWeight"/> + <see cref="CentroidDriftWeight"/>).
    /// </summary>
    /// <remarks>It is relative to this file, see the remarks of <see cref="LayerMechanicalAnalysis"/>.</remarks>
    public IReadOnlyList<float> RiskScores { get; }

    /// <summary>
    /// Gets the scale used to normalize the area change: the percentile <see cref="ScalePercentile"/> of the
    /// non-zero absolute <see cref="AreaDeltaPercent"/>, or 0 when there is none.
    /// </summary>
    public float AreaDeltaScale { get; }

    /// <summary>
    /// Gets the scale used to normalize the centroid drift: the percentile <see cref="ScalePercentile"/> of the
    /// non-zero <see cref="CentroidDrift"/> in mm, or 0 when there is none.
    /// </summary>
    public float CentroidDriftScale { get; }

    /// <summary>
    /// Gets the highest risk score of the file.
    /// </summary>
    public float MaxRiskScore { get; }

    /// <summary>
    /// Gets the layers, in ascending order, with an <see cref="LayerMechanicalRiskLevel.Medium"/> or
    /// <see cref="LayerMechanicalRiskLevel.High"/> risk level.
    /// </summary>
    public IReadOnlyList<int> HighRiskLayers { get; }

    #endregion

    private LayerMechanicalAnalysis(float[] areaDeltaPercent, Vector2[] centroids, float[] centroidDrift,
        float[] riskScores, float areaDeltaScale, float centroidDriftScale)
    {
        AreaDeltaPercent = areaDeltaPercent;
        Centroids = centroids;
        CentroidDrift = centroidDrift;
        RiskScores = riskScores;
        AreaDeltaScale = areaDeltaScale;
        CentroidDriftScale = centroidDriftScale;

        var highRiskLayers = new List<int>();
        var maxRiskScore = 0f;
        for (var i = 0; i < riskScores.Length; i++)
        {
            var score = riskScores[i];
            if (score > maxRiskScore) maxRiskScore = score;
            if (score >= MediumRiskScore) highRiskLayers.Add(i);
        }

        MaxRiskScore = maxRiskScore;
        HighRiskLayers = highRiskLayers;
    }

    #region Static math

    /// <summary>
    /// Gets whether a layer is a peel spike: its area suddenly grows compared with the layer below, which asks for a
    /// stronger peel.
    /// </summary>
    /// <param name="previousArea">The area of the layer below, in mm².</param>
    /// <param name="area">The area of the layer, in mm².</param>
    public static bool IsPeelSpike(float previousArea, float area)
    {
        if (previousArea <= MinPreviousArea) return false;
        var delta = area - previousArea;
        return delta > MinAreaDelta && delta / previousArea >= PeelSpikeMinAreaRatio;
    }

    /// <summary>
    /// Calculates the signed area change of each layer relative to the layer below, in percent:
    /// <c>(A[n] - A[n-1]) / A[n-1] × 100</c>.
    /// </summary>
    /// <param name="areas">The area of each layer, in mm².</param>
    /// <returns>
    /// The change of each layer. It is zero for the first layer and wherever the transition is insignificant: the layer
    /// below has <see cref="MinPreviousArea"/> or less, or the area changed by <see cref="MinAreaDelta"/> mm² or less,
    /// so the tip of a support does not produce huge noisy percentages.
    /// </returns>
    public static float[] CalculateAreaDeltaPercent(ReadOnlySpan<float> areas)
    {
        var result = new float[areas.Length];
        for (var i = 1; i < areas.Length; i++)
        {
            var previous = areas[i - 1];
            if (previous <= MinPreviousArea) continue;

            var delta = areas[i] - previous;
            if (MathF.Abs(delta) <= MinAreaDelta) continue;

            result[i] = delta / previous * 100f;
        }

        return result;
    }

    /// <summary>
    /// Calculates the distance, in the unit of the centroids, each centroid moved relative to the one of the layer
    /// below.
    /// </summary>
    /// <param name="centroids">The centroid of each layer, with <see cref="float.NaN"/> components when empty.</param>
    /// <returns>The drift of each layer. It is zero for the first layer and when any of the two layers is empty.</returns>
    public static float[] CalculateCentroidDrift(ReadOnlySpan<Vector2> centroids)
    {
        var result = new float[centroids.Length];
        for (var i = 1; i < centroids.Length; i++)
        {
            var current = centroids[i];
            var previous = centroids[i - 1];
            if (!IsValid(current) || !IsValid(previous)) continue;

            result[i] = Vector2.Distance(current, previous);
        }

        return result;
    }

    /// <summary>
    /// Calculates the relative risk score of each layer: the absolute area change and the centroid drift are each
    /// divided by the percentile <see cref="ScalePercentile"/> of their own non-zero values, limited to
    /// <see cref="MaxNormalizedComponent"/>, and combined as
    /// <see cref="AreaDeltaWeight"/> × area + <see cref="CentroidDriftWeight"/> × drift.
    /// </summary>
    /// <param name="areaDeltaPercent">The signed area change of each layer, in percent.</param>
    /// <param name="centroidDrift">The centroid drift of each layer.</param>
    /// <returns>The score of each layer, which is only meaningful compared with the other layers of the same file.</returns>
    public static float[] CalculateRiskScores(ReadOnlySpan<float> areaDeltaPercent, ReadOnlySpan<float> centroidDrift)
    {
        return CalculateRiskScores(areaDeltaPercent, centroidDrift, out _, out _);
    }

    /// <summary>
    /// Gets the risk level of a risk score.
    /// </summary>
    /// <param name="riskScore">The score, see <see cref="RiskScores"/>.</param>
    public static LayerMechanicalRiskLevel GetRiskLevel(float riskScore)
    {
        if (riskScore >= HighRiskScore) return LayerMechanicalRiskLevel.High;
        if (riskScore >= MediumRiskScore) return LayerMechanicalRiskLevel.Medium;
        if (riskScore >= LowRiskScore) return LayerMechanicalRiskLevel.Low;
        return LayerMechanicalRiskLevel.None;
    }

    /// <summary>
    /// Gets a percentile of the values, using linear interpolation between the closest ranks.
    /// </summary>
    /// <param name="values">The values. Non finite values are ignored.</param>
    /// <param name="percentile">The percentile, from 0 to 100.</param>
    /// <param name="useAbsolute">True to take the absolute value of the values.</param>
    /// <param name="ignoreZero">True to ignore the values that are zero.</param>
    /// <returns>The percentile, or 0 when there are no values.</returns>
    public static float GetPercentile(ReadOnlySpan<float> values, float percentile, bool useAbsolute = false,
        bool ignoreZero = false)
    {
        var buffer = ArrayPool<float>.Shared.Rent(Math.Max(1, values.Length));
        try
        {
            var count = 0;
            foreach (var value in values)
            {
                var v = useAbsolute ? MathF.Abs(value) : value;
                if (!float.IsFinite(v) || (ignoreZero && v == 0f)) continue;
                buffer[count++] = v;
            }

            if (count == 0) return 0f;

            Array.Sort(buffer, 0, count);
            var position = Math.Clamp(percentile, 0f, 100f) / 100f * (count - 1);
            var lower = (int)MathF.Floor(position);
            var upper = Math.Min(lower + 1, count - 1);
            return buffer[lower] + (buffer[upper] - buffer[lower]) * (position - lower);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Analyses a file from the area and the centroid of each of its layers.
    /// </summary>
    /// <param name="areas">The area of each layer, in mm².</param>
    /// <param name="centroids">The centroid of each layer, with <see cref="float.NaN"/> components when empty.</param>
    /// <exception cref="ArgumentException">When the number of areas and centroids differ.</exception>
    public static LayerMechanicalAnalysis Analyze(ReadOnlySpan<float> areas, ReadOnlySpan<Vector2> centroids)
    {
        if (areas.Length != centroids.Length)
            throw new ArgumentException("The number of areas and centroids must match.", nameof(centroids));

        var areaDeltaPercent = CalculateAreaDeltaPercent(areas);
        var drift = CalculateCentroidDrift(centroids);
        var risk = CalculateRiskScores(areaDeltaPercent, drift, out var areaScale, out var driftScale);
        return new LayerMechanicalAnalysis(areaDeltaPercent, centroids.ToArray(), drift, risk, areaScale,
            driftScale);
    }

    #endregion

    #region File analysis

    /// <summary>
    /// Calculates the area centroid of each layer of a file, in mm. The layers are decoded in parallel, only their
    /// bounding rectangle.
    /// </summary>
    /// <param name="slicerFile">The fully decoded file.</param>
    /// <param name="progress">Optional progress, also used to pause and cancel.</param>
    /// <param name="cancellationToken">Optional token to cancel, used when <paramref name="progress"/> has none.</param>
    /// <returns>
    /// The centroid of each layer, with <see cref="float.NaN"/> components for the empty layers. The origin is the top
    /// left corner of the layer image, the same as the pixel coordinates.
    /// </returns>
    /// <exception cref="OperationCanceledException">When canceled.</exception>
    public static Vector2[] CalculateCentroids(FileFormat slicerFile, OperationProgress? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slicerFile);

        var layers = slicerFile.Layers;
        var result = new Vector2[layers.Length];
        if (layers.Length == 0) return result;
        if (slicerFile.DecodeType != FileFormat.FileDecodeType.Full)
            throw new InvalidOperationException("The file must be fully decoded to calculate the layer centroids.");

        var pixelSize = slicerFile.PixelSize;
        var pixelWidth = pixelSize.Width > 0 ? pixelSize.Width : FallbackPixelSize;
        var pixelHeight = pixelSize.Height > 0 ? pixelSize.Height : FallbackPixelSize;

        progress?.Reset("layers", (uint)layers.Length);
        var token = cancellationToken.CanBeCanceled ? cancellationToken : progress?.Token ?? default;

        Parallel.For(0, layers.Length, CoreSettings.GetParallelOptions(token), layerIndex =>
        {
            progress?.PauseIfRequested();
            result[layerIndex] = CalculateCentroid(layers[layerIndex], pixelWidth, pixelHeight);
            progress?.LockAndIncrement();
        });

        return result;
    }

    /// <summary>
    /// Analyses the layers of a file: area change, centroid drift and the relative risk score.
    /// </summary>
    /// <param name="slicerFile">The fully decoded file.</param>
    /// <param name="progress">Optional progress, also used to pause and cancel.</param>
    /// <param name="cancellationToken">Optional token to cancel, used when <paramref name="progress"/> has none.</param>
    /// <exception cref="OperationCanceledException">When canceled.</exception>
    public static LayerMechanicalAnalysis Analyze(FileFormat slicerFile, OperationProgress? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slicerFile);

        var layers = slicerFile.Layers;
        var areas = new float[layers.Length];
        for (var i = 0; i < areas.Length; i++)
        {
            areas[i] = layers[i].GetArea();
        }

        return Analyze(areas, CalculateCentroids(slicerFile, progress, cancellationToken));
    }

    private static Vector2 CalculateCentroid(Layer layer, float pixelWidth, float pixelHeight)
    {
        var bounds = layer.BoundingRectangle;
        if (layer.IsEmpty || bounds.IsEmpty) return new Vector2(float.NaN);

        using var roi = layer.GetRoiMat(bounds);
        using var moments = CvInvoke.Moments(roi, true);
        if (moments.M00 <= 0) return new Vector2(float.NaN);

        var x = bounds.X + moments.M10 / moments.M00;
        var y = bounds.Y + moments.M01 / moments.M00;
        return new Vector2((float)(x * pixelWidth), (float)(y * pixelHeight));
    }

    #endregion

    private static float[] CalculateRiskScores(ReadOnlySpan<float> areaDeltaPercent,
        ReadOnlySpan<float> centroidDrift, out float areaScale, out float driftScale)
    {
        if (areaDeltaPercent.Length != centroidDrift.Length)
            throw new ArgumentException("The number of area changes and centroid drifts must match.",
                nameof(centroidDrift));

        areaScale = GetPercentile(areaDeltaPercent, ScalePercentile, true, true);
        driftScale = GetPercentile(centroidDrift, ScalePercentile, true, true);

        var result = new float[areaDeltaPercent.Length];
        for (var i = 0; i < result.Length; i++)
        {
            var normalizedArea = Normalize(areaDeltaPercent[i], areaScale);
            var normalizedDrift = Normalize(centroidDrift[i], driftScale);
            result[i] = AreaDeltaWeight * normalizedArea + CentroidDriftWeight * normalizedDrift;
        }

        return result;
    }

    private static float Normalize(float value, float scale)
    {
        if (scale <= 0f) return 0f;
        return Math.Clamp(MathF.Abs(value) / scale, 0f, MaxNormalizedComponent);
    }

    private static bool IsValid(Vector2 centroid) => !float.IsNaN(centroid.X) && !float.IsNaN(centroid.Y);
}
