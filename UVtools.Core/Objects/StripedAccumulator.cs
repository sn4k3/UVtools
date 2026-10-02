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

namespace UVtools.Core.Objects;

/// <summary>
/// Adds many mats into a single target mat from multiple threads.
/// The target is split in horizontal stripes with a lock each, so the threads add to different stripes at the same time
/// instead of waiting for each other to add the whole mat.
/// </summary>
/// <remarks>
/// The result is the same as adding the mats one by one, as long as the addition is commutative and associative,
/// which is true for integer types, including the saturated 8-bit addition.
/// </remarks>
public sealed class StripedAccumulator
{
    private readonly Mat _target;
    private readonly Mat? _mask;
    private readonly Rectangle[] _stripes;
    private readonly object[] _locks;

    /// <summary>
    /// Creates a new accumulator.
    /// </summary>
    /// <param name="target">The mat to accumulate into, it must be initialized with the starting values.</param>
    /// <param name="mask">An optional mask, only the pixels that are not zero in the mask are added.</param>
    /// <param name="stripeCount">The number of stripes, more than the threads is recommended.</param>
    public StripedAccumulator(Mat target, Mat? mask = null, int stripeCount = 0)
    {
        ArgumentNullException.ThrowIfNull(target);
        _target = target;
        _mask = mask;

        if (stripeCount <= 0) stripeCount = Environment.ProcessorCount * 2;
        stripeCount = Math.Max(1, Math.Min(stripeCount, target.Height));

        var stripeHeight = (target.Height + stripeCount - 1) / stripeCount;
        _stripes = new Rectangle[stripeCount];
        _locks = new object[stripeCount];
        for (var i = 0; i < stripeCount; i++)
        {
            var y = i * stripeHeight;
            _stripes[i] = new Rectangle(0, y, target.Width, Math.Max(0, Math.Min(stripeHeight, target.Height - y)));
            _locks[i] = new object();
        }
    }

    /// <summary>
    /// Adds <paramref name="source"/> to the target, it must have the same size and type as the target.
    /// </summary>
    /// <param name="source">The mat to add.</param>
    public void Add(Mat source)
    {
        var count = _stripes.Length;
        var first = Environment.CurrentManagedThreadId % count; // Threads start on different stripes

        for (var i = 0; i < count; i++)
        {
            var index = (first + i) % count;
            var stripe = _stripes[index];
            if (stripe.Height <= 0) continue;

            using var targetStripe = new Mat(_target, stripe);
            using var sourceStripe = new Mat(source, stripe);
            using var maskStripe = _mask is null ? null : new Mat(_mask, stripe);

            lock (_locks[index])
            {
                CvInvoke.Add(targetStripe, sourceStripe, targetStripe, maskStripe);
            }
        }
    }
}
