/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using EmguExtensions;
using System;
using System.Collections.Concurrent;

namespace UVtools.Core.Managers;

public class KernelCacheManager : IDisposable
{
    private readonly ConcurrentDictionary<int, Mat> _kernelCache = new();

    public bool UseDynamicKernel { get; set; }

    public MorphShapes DynamicKernelShape { get; set; } = MorphShapes.Ellipse;

    private readonly Mat _defaultKernel;

    public KernelCacheManager(bool useDynamicKernel = false, Mat? defaultKernel = null)
    {
        UseDynamicKernel = useDynamicKernel;
        _defaultKernel = defaultKernel ?? EmguCvExtensions.Kernel3X3Rectangle;
    }

    public Mat Get(ref int iterations)
    {
        if (!UseDynamicKernel) return _defaultKernel;
        var mat = _kernelCache.GetOrAdd(iterations, i => EmguCvExtensions.CreateDynamicKernel(ref i, DynamicKernelShape));
        iterations = 1;
        return mat;
    }

    /// <summary>
    /// Clears and dispose all the cache
    /// </summary>
    public void Clear()
    {
        foreach (var (_, mat) in _kernelCache)
        {
            if(ReferenceEquals(mat, EmguCvExtensions.Kernel3X3Rectangle)) mat?.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Clear();
    }
}