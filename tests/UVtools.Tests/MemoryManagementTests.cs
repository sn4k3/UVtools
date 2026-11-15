/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.IO;
using System.IO.Compression;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Managers;
using Xunit;

namespace UVtools.Tests;

/// <summary>
/// Shared compressed buffers and the memory governor.
/// </summary>
public sealed class MemoryManagementTests
{
    /// <summary>
    /// Noise compresses poorly, so the layer is large enough to go to the disk cache.
    /// </summary>
    private static Mat CreateNoise(FileFormat slicerFile, int seed)
    {
        var mat = new Mat(slicerFile.Resolution, DepthType.Cv8U, 1);
        var data = new byte[slicerFile.Resolution.Width * slicerFile.Resolution.Height];
        new Random(seed).NextBytes(data);
        mat.SetTo(data);
        return mat;
    }

    private static FileFormat CreateFile(int layerCount, Func<FileFormat, int, Mat> factory)
    {
        var slicerFile = PcbFixtures.CreateSlicerFile();
        var layers = new Layer[layerCount];
        for (var i = 0; i < layers.Length; i++)
        {
            using var mat = factory(slicerFile, i);
            layers[i] = new Layer((uint)i, mat, slicerFile);
        }

        slicerFile.Init(layers);
        return slicerFile;
    }

    private static bool SameImage(Mat expected, Layer layer)
    {
        using var actual = layer.LayerMat;
        using var diff = new Mat();
        CvInvoke.AbsDiff(expected, actual, diff);
        return CvInvoke.CountNonZero(diff) == 0;
    }

    [Fact]
    public void Clone_SharesCompressedBuffer_AndReplacingTheCloneImageKeepsTheOriginal()
    {
        var file = CreateFile(1, CreateNoise);
        var layer = file[0];
        using var original = layer.LayerMat;

        var clone = layer.Clone();
        Assert.Same(layer.CompressedMat.CompressedBytes, clone.CompressedMat.CompressedBytes);
        Assert.True(layer.Equals(clone));

        using (var other = CreateNoise(file, 99))
        {
            clone.LayerMat = other;
        }

        Assert.NotSame(layer.CompressedMat.CompressedBytes, clone.CompressedMat.CompressedBytes);
        Assert.False(layer.Equals(clone));
        Assert.True(SameImage(original, layer));
    }

    [Fact]
    public void GetMaxDegreeOfParallelism_FitsTheWorkersInMemory()
    {
        var reserve = CoreSettings.MemoryReserveBytes;
        try
        {
            // Independent of the memory available on the test machine
            CoreSettings.MemoryReserveBytes = 1;
            MemoryGovernor.Sample();
            Assert.Equal(1, MemoryGovernor.GetMaxDegreeOfParallelism(1, long.MaxValue / 4));

            if (MemoryGovernor.TotalPhysicalBytes > 0 && MemoryGovernor.AvailablePhysicalBytes > 1024 * 1024)
            {
                Assert.Equal(8, MemoryGovernor.GetMaxDegreeOfParallelism(8, 1));
                // A worker that needs all the memory runs alone
                Assert.Equal(1, MemoryGovernor.GetMaxDegreeOfParallelism(8, MemoryGovernor.TotalPhysicalBytes));
                Assert.Equal(1, MemoryGovernor.GetMaxDegreeOfParallelism(-1, MemoryGovernor.TotalPhysicalBytes));
            }

            // Below the reserve a single worker runs
            CoreSettings.MemoryReserveBytes = long.MaxValue / 2;
            MemoryGovernor.Sample();
            if (MemoryGovernor.TotalPhysicalBytes > 0)
            {
                Assert.Equal(MemoryPressureLevel.Critical, MemoryGovernor.Level);
                Assert.Equal(1, MemoryGovernor.GetMaxDegreeOfParallelism(8, 1));
            }
        }
        finally
        {
            CoreSettings.MemoryReserveBytes = reserve;
            MemoryGovernor.Sample();
        }
    }

}
