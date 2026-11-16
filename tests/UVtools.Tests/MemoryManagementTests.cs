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
using System.Runtime.CompilerServices;
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
/// Shared compressed buffers, the layer disk cache, the undo history limit and the memory governor.
/// </summary>
public sealed class MemoryManagementTests : IDisposable
{
    /// <summary>
    /// Keeps the disk cache of the test run out of the user cache folder.
    /// </summary>
    [ModuleInitializer]
    internal static void UseTemporaryDiskCacheFolder()
    {
        CoreSettings.DiskCacheFolder = Path.Combine(Path.GetTempPath(), "UVtoolsTests");
    }

    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"UVtoolsTests-{Guid.NewGuid():N}");
    private readonly LayerBlobStore _store;
    private readonly int _maximumUndoHistory = CoreSettings.MaximumUndoHistory;
    private readonly bool _storeUndoHistoryOnDisk = CoreSettings.StoreUndoHistoryOnDisk;

    public MemoryManagementTests()
    {
        Directory.CreateDirectory(_folder);
        _store = new LayerBlobStore(_folder);
    }

    public void Dispose()
    {
        CoreSettings.MaximumUndoHistory = _maximumUndoHistory;
        CoreSettings.StoreUndoHistoryOnDisk = _storeUndoHistoryOnDisk;
        _store.Dispose();
        try
        {
            Directory.Delete(_folder, true);
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>
    /// Noise compresses poorly, so the layer is large enough to go to the disk cache.
    /// </summary>
    private static Mat CreateNoise(FileFormat slicerFile, int seed)
    {
        var mat = new Mat(slicerFile.Resolution, DepthType.Cv8U, 1);
        var data = new byte[slicerFile.Resolution.Width * slicerFile.Resolution.Height];
        // Deterministic xorshift, the test needs repeatable incompressible pixels and not randomness
        var state = (uint)seed * 2654435761u + 1;
        for (var i = 0; i < data.Length; i++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            data[i] = (byte)(state >> 8);
        }

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
    public void MoveToDisk_ReleasesMemory_AndLoadsTheSameImageBack()
    {
        var file = CreateFile(1, CreateNoise);
        var layer = file[0];
        using var original = layer.LayerMat;
        var hash = layer.Hash;
        var length = layer.CompressedLength;

        var released = layer.TryMoveToDisk(_store);

        Assert.Equal(length, released);
        Assert.True(layer.IsOnDisk);
        Assert.Equal(0, layer.CompressedLengthInMemory);
        Assert.Equal(length, layer.CompressedLength);
        Assert.Equal(length, _store.BytesOnDisk);

        // Metadata and comparisons do not need the image
        var clone = layer.Clone();
        Assert.True(clone.IsOnDisk);
        Assert.Equal(hash, layer.Hash);
        Assert.True(layer.Equals(clone));
        Assert.True(layer.IsOnDisk);

        Assert.True(SameImage(original, layer));
        Assert.False(layer.IsOnDisk);
        Assert.True(SameImage(original, clone));
    }

    [Fact]
    public void MoveToDisk_SharedBuffer_IsWrittenOnce()
    {
        var file = CreateFile(1, CreateNoise);
        var layer = file[0];
        var clone = layer.Clone();

        Assert.True(layer.TryMoveToDisk(_store) > 0);
        Assert.True(clone.TryMoveToDisk(_store) > 0);

        Assert.Equal(layer.CompressedLength, _store.BytesOnDisk);
    }

    [Fact]
    public void MoveToDisk_IdleTime_IsRespected()
    {
        var file = CreateFile(1, CreateNoise);
        var layer = file[0];
        using (layer.LayerMat)
        {
        }

        Assert.Equal(0, layer.TryMoveToDisk(_store, 60_000));
        Assert.False(layer.IsOnDisk);
    }

    [Fact]
    public void ReplacingImage_OfLayerOnDisk_ReleasesTheDiskCopy()
    {
        var file = CreateFile(1, CreateNoise);
        var layer = file[0];
        Assert.True(layer.TryMoveToDisk(_store) > 0);

        using var other = CreateNoise(file, 1234);
        layer.LayerMat = other;

        Assert.False(layer.IsOnDisk);
        Assert.Equal(0, _store.BytesOnDisk);
        Assert.True(SameImage(other, layer));
    }

    [Fact]
    public void ClearingTheFile_ReleasesTheDiskCacheSpace()
    {
        var file = CreateFile(3, CreateNoise);
        foreach (var layer in file) Assert.True(layer.TryMoveToDisk(_store) > 0);
        Assert.True(_store.BytesOnDisk > 0);

        file.Clear();

        Assert.Equal(0, _store.BytesOnDisk);
    }

    [Fact]
    public void DeleteOrphanSessions_DeletesOnlyUnlockedFolders()
    {
        var cacheFolder = Path.Combine(_folder, "orphans");
        var orphan = Directory.CreateDirectory(Path.Combine(cacheFolder, "orphan")).FullName;
        File.WriteAllText(Path.Combine(orphan, "session.lock"), string.Empty);
        File.WriteAllText(Path.Combine(orphan, "00000.pack"), "data");

        var alive = Directory.CreateDirectory(Path.Combine(cacheFolder, "alive")).FullName;
        using (new FileStream(Path.Combine(alive, "session.lock"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Equal(1, LayerBlobStore.DeleteOrphanSessions(cacheFolder));
            Assert.False(Directory.Exists(orphan));
            Assert.True(Directory.Exists(alive));
        }
    }

    [Fact]
    public void UndoHistory_Limit_MergesOldestClipsAndKeepsEveryState()
    {
        CoreSettings.MaximumUndoHistory = 2;
        CoreSettings.StoreUndoHistoryOnDisk = false;
        var file = CreateFile(5, (f, _) =>
        {
            var mat = new Mat(f.Resolution, DepthType.Cv8U, 1);
            mat.SetTo(new MCvScalar(0));
            return mat;
        });

        var clipboard = ClipboardManager.Instance;
        clipboard.Init(file);

        for (var i = 0; i < 4; i++)
        {
            clipboard.Snapshot();
            using var mat = CreateNoise(file, i);
            file[i].LayerMat = mat;
            clipboard.Clip($"Edit {i}");
        }

        // Two operations plus the original layers
        Assert.Equal(3, clipboard.Count);

        // The oldest kept operation holds the merged edits 0, 1 and 2
        clipboard.CurrentIndex = 1;
        for (var i = 0; i < 3; i++)
        {
            using var expected = CreateNoise(file, i);
            Assert.True(SameImage(expected, file[i]));
        }

        Assert.Equal(0u, file[3].NonZeroPixelCount);

        clipboard.SetToOldest();
        for (var i = 0; i < 5; i++) Assert.Equal(0u, file[i].NonZeroPixelCount);

        clipboard.SetToNewest();
        for (var i = 0; i < 4; i++)
        {
            using var expected = CreateNoise(file, i);
            Assert.True(SameImage(expected, file[i]));
        }

        clipboard.Reset();
    }

    [Fact]
    public void UndoHistory_OnDisk_RestoresTheOldImages()
    {
        CoreSettings.StoreUndoHistoryOnDisk = false;
        var file = CreateFile(2, CreateNoise);
        using var original0 = file[0].LayerMat;

        var clipboard = ClipboardManager.Instance;
        clipboard.Init(file);

        clipboard.Snapshot();
        using (var mat = CreateNoise(file, 500))
        {
            file[0].LayerMat = mat;
        }

        clipboard.Clip("Edit");

        // Only the replaced image is no longer in use by the file
        var released = clipboard.MoveHistoryToDisk();
        Assert.Equal(clipboard[1][0].CompressedLength, released);
        Assert.True(clipboard[1][0].IsOnDisk);
        Assert.False(clipboard[1][1].IsOnDisk);

        clipboard.Undo();
        Assert.True(SameImage(original0, file[0]));

        clipboard.Reset();
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

    [Fact]
    public void EncodeLayersInZip_WithCache_WritesEveryLayerInOrder()
    {
        int[] seeds = [1, 2, 1, 3, 2, 2, 4, 1];
        var file = CreateFile(seeds.Length, (f, i) => CreateNoise(f, seeds[i]));

        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            file.EncodeLayersInZip(zip, IndexStartNumber.Zero, useCache: true);
        }

        stream.Position = 0;
        using var read = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.Equal(seeds.Length, read.Entries.Count);
        for (var i = 0; i < seeds.Length; i++)
        {
            var entry = read.Entries[i];
            Assert.Equal(file[i].FormatFileName(string.Empty, 0, IndexStartNumber.Zero), entry.FullName);
            using var entryStream = entry.Open();
            using var memory = new MemoryStream();
            entryStream.CopyTo(memory);
            using var mat = new Mat();
            CvInvoke.Imdecode(memory.ToArray(), ImreadModes.Grayscale, mat);
            using var expected = CreateNoise(file, seeds[i]);
            using var diff = new Mat();
            CvInvoke.AbsDiff(expected, mat, diff);
            Assert.Equal(0, CvInvoke.CountNonZero(diff));
        }
    }
}
