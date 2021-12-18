/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Concurrent;
using Emgu.CV;
using UVtools.Core.Extensions;

namespace UVtools.Core.Managers
{
    public class KernelCacheManager : IDisposable
    {
        private readonly ConcurrentDictionary<int, Mat> _kernelCache = new();

        public bool UseDynamicKernel { get; set; }

        private readonly Mat _defaultKernel;

        public KernelCacheManager(bool useDynamicKernel = false, Mat defaultKernel = null)
        {
            UseDynamicKernel = useDynamicKernel;
            _defaultKernel = defaultKernel ?? EmguExtensions.Kernel3x3Rectangle;
        }

        public Mat Get(ref int iterations)
        {
            if (!UseDynamicKernel) return _defaultKernel;
            var mat = _kernelCache.GetOrAdd(iterations, i => EmguExtensions.GetDynamicKernel(ref i));
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
                if(ReferenceEquals(mat, EmguExtensions.Kernel3x3Rectangle)) mat?.Dispose();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Clear();
        }
    }
}
