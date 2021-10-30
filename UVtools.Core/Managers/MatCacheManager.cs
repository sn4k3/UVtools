/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Emgu.CV;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;

namespace UVtools.Core.Managers
{
    public class MatCacheManager : IDisposable
    {
        /// <summary>
        /// Gets the slicer file
        /// </summary>
        public FileFormat SlicerFile { get; }

        /// <summary>
        /// Gets or sets the cache count of items to keep in memory
        /// </summary>
        public ushort CacheCount { get; set; }

        /// <summary>
        /// Gets the number of cached elements per a cache entry
        /// </summary>
        public byte ElementsPerCache { get; }

        /// <summary>
        /// Gets the starting layer index range
        /// </summary>
        public uint LayerIndexStart { get; }

        /// <summary>
        /// Gets the ending layer index range
        /// </summary>
        public uint LayerIndexEnd { get; }

        /// <summary>
        /// Gets the size of this collection
        /// </summary>
        public uint CollectionSize => LayerIndexEnd - LayerIndexStart + 1;

        /// <summary>
        /// Gets the image rotation to cache
        /// </summary>
        public Enumerations.RotateDirection Rotate { get; init; } = Enumerations.RotateDirection.None;

        /// <summary>
        /// Gets the image flip to cache
        /// </summary>
        public Enumerations.FlipDirection Flip { get; init; } = Enumerations.FlipDirection.None;

        /// <summary>
        /// Gets or sets the cache direction, false to go backwards, true to go forward
        /// </summary>
        public bool Direction { get; set; } = true;

        /// <summary>
        /// If enabled it will not calculate the cache index given a layer index, set this to true to use your own indexing without pair them with layers
        /// </summary>
        public bool UseRawIndexInsteadOfLayerIndex { get; init; }

        /// <summary>
        /// Gets or sets the auto dispose mode, it will dispose all mat's below of above passed index 
        /// </summary>
        public bool AutoDispose { get; set; } = false;

        /// <summary>
        /// Gets or sets the amount of last mats to keep cached when <see cref="AutoDispose"/> is enabled
        /// </summary>
        public ushort AutoDisposeKeepLast { get; set; } = 0;

        /// <summary>
        /// Gets the cache mat array
        /// </summary>
        private Mat[][] MatCache { get; }

        /// <summary>
        /// Gets or sets the action to trigger after cache the initial <see cref="Mat"/>
        /// </summary>
        public Action<Mat[]> AfterCacheAction { get; set; }

        public MatCacheManager(FileFormat slicerFile, ushort cacheCount = 0, byte elementsPerCache = 1) : this(slicerFile, 0, slicerFile.LastLayerIndex, cacheCount, elementsPerCache)
        { }

        public MatCacheManager(FileFormat slicerFile, uint collectionSize, ushort cacheCount = 0, byte elementsPerCache = 1) : this(slicerFile, 0, collectionSize-1, cacheCount, elementsPerCache)
        { }

        public MatCacheManager(Operation operation, ushort cacheCount = 0, byte elementsPerCache = 1) : this(operation.SlicerFile, operation.LayerIndexStart, operation.LayerIndexEnd, cacheCount, elementsPerCache)
        {}

        public MatCacheManager(FileFormat slicerFile, uint layerIndexStart, uint layerIndexEnd, ushort cacheCount = 0, byte elementsPerCache = 1)
        {
            if (cacheCount == 0) cacheCount = (ushort)(Environment.ProcessorCount * 5);
            if (layerIndexEnd == 0) layerIndexEnd = slicerFile.LayerCount;
            SlicerFile = slicerFile;
            LayerIndexStart = layerIndexStart;
            LayerIndexEnd = layerIndexEnd;
            CacheCount = cacheCount;
            ElementsPerCache = elementsPerCache;
            MatCache = new Mat[CollectionSize][];

            if (layerIndexStart == 0) UseRawIndexInsteadOfLayerIndex = true;
        }

        /// <summary>
        /// Gets the cache index from a layer index, required when layer range is not 0 started
        /// </summary>
        /// <param name="layerIndex"></param>
        /// <returns></returns>
        private uint LayerIndexToCacheIndex(uint layerIndex)
        {
            return UseRawIndexInsteadOfLayerIndex ? layerIndex : layerIndex - LayerIndexStart;
        }

        /// <summary>
        /// Gets the layer index from the cache index, required when layer range is not 0 started
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private uint CacheIndexToLayerIndex(uint index)
        {
            return UseRawIndexInsteadOfLayerIndex ? index : index + LayerIndexStart;
        }

        /// <summary>
        /// Gets all cached mat's given an index
        /// </summary>
        /// <param name="layerIndex"></param>
        /// <returns></returns>
        public Mat[] Get(uint layerIndex)
        {
            uint index = LayerIndexToCacheIndex(layerIndex);
            if (MatCache[index] is null)
            {
                if (AutoDispose) // Dispose as go
                {
                    ClearButKeep(layerIndex, AutoDisposeKeepLast);
                }
                
                int fromLayerIndex = (int)layerIndex;
                int toLayerIndex = (int)Math.Min(LayerIndexEnd+1, layerIndex + CacheCount);

                if (!Direction)
                {
                    toLayerIndex = fromLayerIndex + 1;
                    fromLayerIndex = Math.Max((int)LayerIndexStart, fromLayerIndex - CacheCount);
                }

                Parallel.For(fromLayerIndex, toLayerIndex, CoreSettings.ParallelOptions,
                    currentLayerIndex =>
                    {
                        var currentCacheIndex = LayerIndexToCacheIndex((uint)currentLayerIndex);
                        if (MatCache[currentCacheIndex] is not null) return; // Already cached
                        MatCache[currentCacheIndex] = new Mat[ElementsPerCache];
                        MatCache[currentCacheIndex][0] = SlicerFile[currentLayerIndex].LayerMat;

                        if (Flip != Enumerations.FlipDirection.None)
                        {
                            CvInvoke.Flip(MatCache[currentCacheIndex][0], MatCache[currentCacheIndex][0], Enumerations.ToOpenCVFlipType(Flip));
                        }

                        if (Rotate != Enumerations.RotateDirection.None)
                        {
                            CvInvoke.Rotate(MatCache[currentCacheIndex][0], MatCache[currentCacheIndex][0], Enumerations.ToOpenCVRotateFlags(Rotate));
                        }

                        AfterCacheAction?.Invoke(MatCache[currentCacheIndex]);
                    });
            }

            return MatCache[index];
        }

        public Mat Get(uint layerIndex, byte elementIndex)
        {
            return Get(layerIndex)[elementIndex];
        }

        public Mat Get1(uint layerIndex)
        {
            return Get(layerIndex)[0];
        }

        public (Mat mat1, Mat mat2) Get2(uint layerIndex)
        {
            var mats = Get(layerIndex);
            return (mats[0], mats[1]);
        }

        public (Mat mat1, Mat mat2, Mat mat3) Get3(uint layerIndex)
        {
            var mats = Get(layerIndex);
            return (mats[0], mats[1], mats[2]);
        }

        
        public void GetAndConsumeWork(uint layerIndex, Action<Mat[]> action)
        {
            try
            {
                action.Invoke(Get(layerIndex));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
            finally
            {
                Consume(layerIndex);
            }
        }

        public void GetAndConsumeWork(uint layerIndex, Action<Mat> action)
        {
            try
            {
                action.Invoke(Get1(layerIndex));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
            finally
            {
                Consume(layerIndex);
            }
        }

        public void GetAndConsumeWork(uint layerIndex, byte elementIndex, Action<Mat> action)
        {
            try
            {
                action.Invoke(Get(layerIndex)[elementIndex]);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
            finally
            {
                Consume(layerIndex);
            }
        }

        /// <summary>
        /// Consume and dispose the cached Mats for given layer index
        /// </summary>
        /// <param name="layerIndex"></param>
        public void Consume(uint layerIndex)
        {
            var index = LayerIndexToCacheIndex(layerIndex);
            if(MatCache[index] is null) return;
            for (int i = 0; i < MatCache[index].Length; i++)
            {
                MatCache[index][i]?.Dispose();
            }
            MatCache[index] = null;
        }

        /// <summary>
        /// Clears and dispose the cache but keep the selected index and optionally the last n indexes
        /// </summary>
        /// <param name="layerIndex"></param>
        /// <param name="keepLast"></param>
        public void ClearButKeep(uint layerIndex, ushort keepLast = 0)
        {
            if (Direction)
            {
                for (int i = (int)layerIndex - 1 - keepLast; i >= LayerIndexStart; i--) Consume((uint)i);
            }
            else
            {
                for (int i = (int)layerIndex + 1 + keepLast; i <= LayerIndexEnd; i++) Consume((uint)i);
            }
        }

        /// <summary>
        /// Clears and dispose all the cache
        /// </summary>
        public void Clear()
        {
            for (uint i = 0; i < MatCache.Length; i++)
            {
                Consume(CacheIndexToLayerIndex(i));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Clear();
        }
    }
}
