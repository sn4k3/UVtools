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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
        _store = CreateStore(_folder);
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

    private static LayerBlobStore CreateStore(string folder, Func<string, long>? availableSpace = null,
        long packSize = 256L * 1024 * 1024)
    {
        // Exercise the internal disk policy seam without exposing test hooks in the public API.
        return Assert.IsType<LayerBlobStore>(Activator.CreateInstance(typeof(LayerBlobStore),
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            [folder, availableSpace ?? (_ => long.MaxValue), packSize], null));
    }

    private static object? WriteBlob(LayerBlobStore store, byte[] bytes) =>
        (typeof(LayerBlobStore).GetMethod("Write", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException()).Invoke(store, [bytes, 0UL]);

    private static void ReleaseBlob(object lease) =>
        (lease.GetType().GetMethod("Release") ?? throw new MissingMethodException()).Invoke(lease, null);

    private static FieldInfo GovernorField(string name) =>
        typeof(MemoryGovernor).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(name);

    [Fact]
    public void DirectCompressedMatAccess_RemainsUsableAcrossEvictionAttempts()
    {
        using var file = CreateFile(1, CreateNoise);
        var layer = file[0];
        var exposed = layer.CompressedMat;
        Assert.Equal(0, layer.TryMoveToDisk(_store));
        using var image = exposed.Mat;
        Assert.True(SameImage(image, layer));

        var clone = layer.Clone();
        Assert.True(clone.TryMoveToDisk(_store) > 0);
        Assert.True(SameImage(image, clone));
    }

    [Fact]
    public async Task ConcurrentReadsReplacementsAndCopies_AreSafeDuringEviction()
    {
        using var file = CreateFile(1, CreateNoise);
        using var expected = CreateNoise(file, 0);
        var layer = file[0];
        var target = layer.Clone();
        using var start = new ManualResetEventSlim();
        var reader = Task.Run(() =>
        {
            start.Wait();
            for (var i = 0; i < 64; i++) Assert.True(SameImage(expected, layer));
        });
        var writer = Task.Run(() =>
        {
            start.Wait();
            for (var i = 0; i < 64; i++) layer.LayerMat = expected;
        });
        var copier = Task.Run(() =>
        {
            start.Wait();
            for (var i = 0; i < 64; i++)
            {
                layer.CopyImageTo(target);
                Assert.True(SameImage(expected, target));
            }
        });
        var evictor = Task.Run(() =>
        {
            start.Wait();
            for (var i = 0; i < 128; i++)
            {
                layer.TryMoveToDisk(_store);
                target.TryMoveToDisk(_store);
            }
        });
        start.Set();
        await Task.WhenAll(reader, writer, copier, evictor);
        Assert.True(SameImage(expected, layer));
        Assert.True(SameImage(expected, target));
    }

    [Fact]
    public void DiskBudget_CountsDeadBytesInReferencedPacks()
    {
        using var store = CreateStore(Path.Combine(_folder, "budget"), packSize: 8192);
        store.MaximumBytes = 8192;
        var retained = WriteBlob(store, new byte[4096]);
        Assert.NotNull(retained);
        var released = WriteBlob(store, new byte[4096]);
        Assert.NotNull(released);
        try
        {
            ReleaseBlob(released);
            Assert.Equal(4096, store.BytesOnDisk);
            Assert.Equal(8192, store.FilesLength);
            Assert.Null(WriteBlob(store, new byte[4096]));
        }
        finally
        {
            ReleaseBlob(retained);
        }
        Assert.Equal(0, store.FilesLength);
        var next = WriteBlob(store, new byte[4096]);
        Assert.NotNull(next);
        ReleaseBlob(next);
    }

    [Fact]
    public void DiskHeadroom_IsCheckedForEveryAppend()
    {
        const long reserve = 2L * 1024 * 1024 * 1024;
        var free = reserve + 16384;
        using var store = CreateStore(Path.Combine(_folder, "headroom"), _ => free);
        var first = WriteBlob(store, new byte[4096]);
        Assert.NotNull(first);
        try
        {
            free = reserve + 1024;
            Assert.Null(WriteBlob(store, new byte[4096]));
            Assert.Equal(4096, store.FilesLength);
            Assert.Equal(4096, store.BytesOnDisk);
        }
        finally
        {
            ReleaseBlob(first);
        }
    }

    [Fact]
    public void InsufficientDiskSpace_LeavesTheLayerInMemory()
    {
        using var store = CreateStore(Path.Combine(_folder, "full"), _ => 2L * 1024 * 1024 * 1024);
        using var file = CreateFile(1, CreateNoise);
        using var expected = file[0].LayerMat;
        Assert.Equal(0, file[0].TryMoveToDisk(store));
        Assert.False(file[0].IsOnDisk);
        Assert.True(SameImage(expected, file[0]));
        Assert.Equal(0, store.FilesLength);
    }

    [Fact]
    public void PackCreationFailure_IsARefusalAndPreservesExistingData()
    {
        using var store = CreateStore(Path.Combine(_folder, "pack-error"), packSize: 4096);
        var first = WriteBlob(store, new byte[4096]);
        Assert.NotNull(first);
        try
        {
            Directory.CreateDirectory(Path.Combine(store.Folder, "00001.pack"));
            Assert.Null(WriteBlob(store, new byte[4096]));
            Assert.Equal(4096, store.FilesLength);
            Assert.Equal(4096, store.BytesOnDisk);
        }
        finally
        {
            ReleaseBlob(first);
        }
    }

    [Fact]
    public void Dispose_ClosesAllPacksWithLiveLeases()
    {
        using var store = CreateStore(Path.Combine(_folder, "dispose"), packSize: 4096);
        var first = WriteBlob(store, new byte[4096]);
        var second = WriteBlob(store, new byte[4096]);
        Assert.NotNull(first);
        Assert.NotNull(second);
        store.Dispose();
        try
        {
            Assert.False(Directory.Exists(store.Folder));
            Assert.Equal(0, store.FilesLength);
            Assert.Equal(0, store.BytesOnDisk);
        }
        finally
        {
            ReleaseBlob(first);
            ReleaseBlob(second);
        }
        Assert.Equal(0, store.BytesOnDisk);
    }

    [Fact]
    public void ConcurrentSessionStartupAndCleanup_LeaveEveryLiveSessionIntact()
    {
        var root = Path.Combine(_folder, "startup");
        var stores = new LayerBlobStore[8];
        try
        {
            Parallel.For(0, stores.Length, i => stores[i] = CreateStore(root));
            Assert.Equal(0, LayerBlobStore.DeleteOrphanSessions(Path.Combine(root, LayerBlobStore.CacheFolderName)));
            foreach (var store in stores)
            {
                Assert.True(Directory.Exists(store.Folder));
                var lease = WriteBlob(store, new byte[4096]);
                Assert.NotNull(lease);
                ReleaseBlob(lease);
            }
        }
        finally
        {
            foreach (var store in stores) store?.Dispose();
        }
    }

    [Theory]
    [InlineData(0, 8, 1)]
    [InlineData(2, 8, 2)]
    [InlineData(8, 8, 7)]
    [InlineData(100, 8, 7)]
    [InlineData(0, 1, 0.75)]
    public void EffectiveReserve_LeavesRoomForWork(double configuredGb, double totalGb, double expectedGb)
    {
        const long gb = 1024L * 1024 * 1024;
        Assert.Equal((long)(expectedGb * gb),
            MemoryGovernor.CalculateReserveBytes((long)(configuredGb * gb), (long)(totalGb * gb)));
    }

    [Fact]
    public async Task WorkerMemoryScope_FlowsToWorkersAndRestoresNestedAndConcurrentValues()
    {
        var previous = MemoryGovernor.LayerPixelCount;
        using (MemoryGovernor.BeginLayerMemoryScope(160_000_000))
        {
            var small = Task.Run(async () =>
            {
                using (MemoryGovernor.BeginLayerMemoryScope(4_000_000))
                {
                    await Task.Yield();
                    Assert.Equal(4_000_000, MemoryGovernor.LayerPixelCount);
                }
                Assert.Equal(160_000_000, MemoryGovernor.LayerPixelCount);
            });
            await small;
            Assert.Equal(160_000_000, MemoryGovernor.LayerPixelCount);
            Assert.Equal(160_000_000, await Task.Run(() => MemoryGovernor.LayerPixelCount));
        }
        Assert.Equal(previous, MemoryGovernor.LayerPixelCount);
    }

    [Fact]
    public void FirstHighPressureRelief_CompactsWithoutWaitingForAnotherSample()
    {
        var field = GovernorField("_lastCompactTicks");
        var previous = field.GetValue(null);
        try
        {
            field.SetValue(null, long.MinValue);
            MemoryGovernor.Relieve(MemoryPressureLevel.High);
            Assert.NotEqual(long.MinValue, field.GetValue(null));
        }
        finally
        {
            field.SetValue(null, previous);
        }
    }

    [Fact]
    public void FirstWorkerSizing_SamplesMemoryWithoutStartingTheMonitor()
    {
        var field = GovernorField("_lastSampleTicks");
        var previous = field.GetValue(null);
        try
        {
            field.SetValue(null, long.MinValue);
            using var scope = MemoryGovernor.BeginLayerMemoryScope(1);
            MemoryGovernor.GetMaxDegreeOfParallelism(8);
            Assert.NotEqual(long.MinValue, field.GetValue(null));
        }
        finally
        {
            field.SetValue(null, previous);
        }
    }

    [Fact]
    public async Task ThrottleTakeover_PreventsPreviousOwnerFromRenewingOrReleasingTheLane()
    {
        var level = GovernorField("_level");
        var held = GovernorField("_laneHeld");
        var ticks = GovernorField("_laneTicks");
        var previousLevel = level.GetValue(null);
        var adaptive = CoreSettings.AdaptiveMemoryManagement;
        using var acquiredA = new ManualResetEventSlim();
        using var acquiredB = new ManualResetEventSlim();
        using var returnedA = new ManualResetEventSlim();
        using var releaseB = new ManualResetEventSlim();
        Task? workerA = null;
        Task? workerB = null;
        try
        {
            CoreSettings.AdaptiveMemoryManagement = true;
            level.SetValue(null, (int)MemoryPressureLevel.Critical);
            held.SetValue(null, 0);
            workerA = Task.Factory.StartNew(() =>
            {
                MemoryGovernor.Throttle();
                acquiredA.Set();
                Assert.True(acquiredB.Wait(TimeSpan.FromSeconds(5)));
                using var cancelled = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                MemoryGovernor.Throttle(cancelled.Token);
                Assert.True(cancelled.IsCancellationRequested);
                returnedA.Set();
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            workerB = Task.Factory.StartNew(() =>
            {
                Assert.True(acquiredA.Wait(TimeSpan.FromSeconds(5)));
                ticks.SetValue(null, Environment.TickCount64 - 3000);
                MemoryGovernor.Throttle();
                acquiredB.Set();
                Assert.True(releaseB.Wait(TimeSpan.FromSeconds(5)));
                MemoryGovernor.Throttle();
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Assert.True(returnedA.Wait(TimeSpan.FromSeconds(5)));
            Assert.Equal(1, held.GetValue(null));
        }
        finally
        {
            level.SetValue(null, (int)MemoryPressureLevel.Normal);
            acquiredA.Set();
            acquiredB.Set();
            releaseB.Set();
            try
            {
                if (workerA is not null && workerB is not null) await Task.WhenAll(workerA, workerB);
            }
            finally
            {
                held.SetValue(null, 0);
                level.SetValue(null, previousLevel);
                CoreSettings.AdaptiveMemoryManagement = adaptive;
            }
        }
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
        var orphan = Directory.CreateDirectory(Path.Combine(cacheFolder, $"0-{Guid.NewGuid():N}")).FullName;
        File.WriteAllText(Path.Combine(orphan, "session.lock"), "UVtools.LayerCache.v1");
        File.WriteAllText(Path.Combine(orphan, "00000.pack"), "data");

        var alive = Directory.CreateDirectory(Path.Combine(cacheFolder, $"{Environment.ProcessId}-{Guid.NewGuid():N}")).FullName;
        var unrelated = Directory.CreateDirectory(Path.Combine(cacheFolder, "unrelated")).FullName;
        File.WriteAllText(Path.Combine(unrelated, "keep.txt"), "user data");
        var unmarked = Directory.CreateDirectory(Path.Combine(cacheFolder, $"0-{Guid.NewGuid():N}")).FullName;
        File.WriteAllText(Path.Combine(unmarked, "session.lock"), "not a cache marker");
        using (new FileStream(Path.Combine(alive, "session.lock"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Equal(1, LayerBlobStore.DeleteOrphanSessions(cacheFolder));
            Assert.False(Directory.Exists(orphan));
            Assert.True(Directory.Exists(alive));
            Assert.Equal("user data", File.ReadAllText(Path.Combine(unrelated, "keep.txt")));
            Assert.True(Directory.Exists(unmarked));
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
        Assert.True(clipboard[0].CanRerun);
        Assert.False(clipboard[1].CanRerun);

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
        var released = clipboard.MoveHistoryToDisk(_store);
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

            // An oversized reserve leaves memory available for work rather than making recovery impossible.
            CoreSettings.MemoryReserveBytes = long.MaxValue / 2;
            MemoryGovernor.Sample();
            if (MemoryGovernor.TotalPhysicalBytes > 0)
            {
                Assert.InRange(MemoryGovernor.ReserveBytes, 1, MemoryGovernor.TotalPhysicalBytes - 1);
                Assert.Equal(1, MemoryGovernor.GetMaxDegreeOfParallelism(8, MemoryGovernor.TotalPhysicalBytes));
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
