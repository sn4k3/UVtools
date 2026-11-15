/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace UVtools.Core.Managers;

/// <summary>
/// Disk cache for compressed layer images. Layers that are not being used can move their compressed bytes here to free
/// memory RAM, and read them back on demand.
/// </summary>
/// <remarks>
/// <para>Blobs are appended to fixed size pack files inside a folder unique to this process, a pack file is deleted once
/// every blob written to it is released. Identical arrays (layers sharing their buffer) are written only once.</para>
/// <para>A lock file marks the folder as in use, folders left behind by a crashed process are deleted the next time a
/// store is created.</para>
/// </remarks>
public sealed class LayerBlobStore : IDisposable
{
    #region Constants

    /// <summary>
    /// The name of the folder that holds the session folders, inside <see cref="CoreSettings.DiskCacheFolder"/>.
    /// </summary>
    public const string CacheFolderName = "LayerCache";

    private const string LockFileName = "session.lock";
    private const long PackSize = 256L * 1024 * 1024;

    /// <summary>
    /// The disk free space to always leave untouched.
    /// </summary>
    private const long MinimumDiskFreeBytes = 2L * 1024 * 1024 * 1024;

    #endregion

    #region Static

    private static readonly Lock SessionLock = new();
    private static LayerBlobStore? _session;
    private static bool _sessionFailed;

    /// <summary>
    /// Gets the store of this process, creating it on first use.
    /// Returns <see langword="null"/> when the disk cache is disabled or the folder can't be used.
    /// </summary>
    public static LayerBlobStore? Session
    {
        get
        {
            if (!CoreSettings.EnableDiskCache) return null;
            var session = Volatile.Read(ref _session);
            if (session is not null || _sessionFailed) return session;

            lock (SessionLock)
            {
                if (_session is not null || _sessionFailed) return _session;
                try
                {
                    var store = new LayerBlobStore(CoreSettings.DiskCacheFolderOrDefault);
                    AppDomain.CurrentDomain.ProcessExit += (_, _) => store.Dispose();
                    Volatile.Write(ref _session, store);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"{nameof(LayerBlobStore)}: unable to create the disk cache: {e}");
                    _sessionFailed = true;
                }

                return _session;
            }
        }
    }

    /// <summary>
    /// Gets the store of this process only when it was already created, without creating it.
    /// </summary>
    public static LayerBlobStore? SessionIfCreated => Volatile.Read(ref _session);

    /// <summary>
    /// Deletes the session folders of processes that are not running anymore.
    /// </summary>
    /// <param name="cacheFolder">The folder holding the session folders.</param>
    /// <returns>The number of deleted folders.</returns>
    public static int DeleteOrphanSessions(string cacheFolder)
    {
        if (!Directory.Exists(cacheFolder)) return 0;
        var deleted = 0;
        foreach (var directory in Directory.EnumerateDirectories(cacheFolder))
        {
            try
            {
                var lockFile = Path.Combine(directory, LockFileName);
                if (File.Exists(lockFile))
                {
                    // Opening exclusively only works when the owner process is gone
                    using var stream = new FileStream(lockFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }

                Directory.Delete(directory, true);
                deleted++;
            }
            catch (IOException)
            {
                // In use by another running instance
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return deleted;
    }

    #endregion

    #region Members

    private readonly Lock _mutex = new();
    private readonly FileStream _lockStream;
    private readonly ConditionalWeakTable<byte[], LayerBlob> _blobsByArray = new();
    private Pack? _currentPack;
    private int _packCount;
    private long _bytesOnDisk;
    private long _filesLength;
    private bool _isDisposed;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the folder of this session.
    /// </summary>
    public string Folder { get; }

    /// <summary>
    /// Gets the bytes of the blobs that are still referenced.
    /// </summary>
    public long BytesOnDisk => Interlocked.Read(ref _bytesOnDisk);

    /// <summary>
    /// Gets the total length of the pack files on the disk.
    /// </summary>
    public long FilesLength => Interlocked.Read(ref _filesLength);

    /// <summary>
    /// Gets or sets the maximum bytes the store can hold, 0 for automatic (limited by the disk free space only).
    /// </summary>
    public long MaximumBytes { get; set; }

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a store in a new session folder inside <paramref name="baseFolder"/>/<see cref="CacheFolderName"/>.
    /// </summary>
    /// <param name="baseFolder">The base folder.</param>
    public LayerBlobStore(string baseFolder)
    {
        var cacheFolder = Path.Combine(baseFolder, CacheFolderName);
        Directory.CreateDirectory(cacheFolder);
        DeleteOrphanSessions(cacheFolder);

        Folder = Path.Combine(cacheFolder, $"{Environment.ProcessId}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Folder);
        _lockStream = new FileStream(Path.Combine(Folder, LockFileName), FileMode.CreateNew, FileAccess.ReadWrite,
            FileShare.None, 1, FileOptions.DeleteOnClose);
        MaximumBytes = CoreSettings.DiskCacheMaximumBytes;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Writes a compressed array to the disk, or reuses the blob when the same array instance was already written.
    /// </summary>
    /// <param name="bytes">The compressed bytes, must not be mutated afterwards.</param>
    /// <param name="hash">The hash of <paramref name="bytes"/>, used to validate the blob later.</param>
    /// <returns>A lease to the blob, or <see langword="null"/> when there is no space left.</returns>
    internal LayerBlobLease? Write(byte[] bytes, ulong hash)
    {
        if (_isDisposed || bytes.Length == 0) return null;

        if (_blobsByArray.TryGetValue(bytes, out var existing) && existing.TryAddReference())
        {
            return new LayerBlobLease(existing);
        }

        Pack pack;
        long offset;
        lock (_mutex)
        {
            if (_isDisposed) return null;
            var maximumBytes = MaximumBytes;
            if (maximumBytes > 0 && _bytesOnDisk + bytes.Length > maximumBytes) return null;

            if (_currentPack is null || _currentPack.Length + bytes.Length > PackSize)
            {
                if (!HasDiskSpaceFor(Math.Max(PackSize, bytes.Length))) return null;
                var oldPack = _currentPack;
                _currentPack = new Pack(Path.Combine(Folder, $"{_packCount++:D5}.pack"));
                if (oldPack is not null && oldPack.LiveBlobs == 0) DeletePack(oldPack);
            }

            pack = _currentPack;
            offset = pack.Length;
            pack.Length += bytes.Length;
            pack.LiveBlobs++;
            _bytesOnDisk += bytes.Length;
            _filesLength += bytes.Length;
        }

        var blob = new LayerBlob(this, pack, offset, bytes.Length, hash);
        try
        {
            RandomAccess.Write(pack.Handle, bytes, offset);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"{nameof(LayerBlobStore)}: write failed: {e.Message}");
            blob.ReleaseReference();
            return null;
        }

        blob.SetLoaded(bytes);
        _blobsByArray.AddOrUpdate(bytes, blob);
        return new LayerBlobLease(blob);
    }

    /// <summary>
    /// Reads the bytes of a blob, returning the array already in memory when another layer has loaded it.
    /// </summary>
    internal byte[] Read(LayerBlob blob)
    {
        var bytes = blob.TryGetLoaded();
        if (bytes is not null) return bytes;

        bytes = GC.AllocateUninitializedArray<byte>(blob.Length);
        var read = 0;
        while (read < bytes.Length)
        {
            var count = RandomAccess.Read(blob.Pack.Handle, bytes.AsSpan(read), blob.Offset + read);
            if (count <= 0) throw new EndOfStreamException($"Disk cache blob truncated at {blob.Pack.Path}");
            read += count;
        }

        blob.SetLoaded(bytes);
        _blobsByArray.AddOrUpdate(bytes, blob);
        return bytes;
    }

    internal void OnBlobReleased(LayerBlob blob)
    {
        lock (_mutex)
        {
            _bytesOnDisk -= blob.Length;
            blob.Pack.LiveBlobs--;
            if (blob.Pack.LiveBlobs == 0 && !ReferenceEquals(blob.Pack, _currentPack))
            {
                DeletePack(blob.Pack);
            }
        }
    }

    private void DeletePack(Pack pack)
    {
        _filesLength -= pack.Length;
        pack.Dispose();
    }

    private bool HasDiskSpaceFor(long length)
    {
        try
        {
            var drivePath = Folder;
            if (OperatingSystem.IsWindows())
            {
                drivePath = Path.GetPathRoot(Path.GetFullPath(Folder)) ?? string.Empty;
                // Network shares have no drive letter
                if (drivePath.Length == 0 || drivePath.StartsWith(@"\\")) return true;
            }

            var drive = new DriveInfo(drivePath);
            return drive.AvailableFreeSpace - length > MinimumDiskFreeBytes;
        }
        catch (Exception)
        {
            return true;
        }
    }

    public void Dispose()
    {
        lock (_mutex)
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _currentPack?.Dispose();
            _currentPack = null;
        }

        try
        {
            _lockStream.Dispose();
            Directory.Delete(Folder, true);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"{nameof(LayerBlobStore)}: unable to delete {Folder}: {e.Message}");
        }
    }

    #endregion

    #region Nested types

    internal sealed class Pack : IDisposable
    {
        public string Path { get; }
        public SafeFileHandle Handle { get; }
        public long Length;
        public int LiveBlobs;

        public Pack(string path)
        {
            Path = path;
            Handle = File.OpenHandle(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
                FileOptions.DeleteOnClose | FileOptions.RandomAccess);
        }

        public void Dispose() => Handle.Dispose();
    }

    #endregion
}

/// <summary>
/// A compressed array stored in a <see cref="LayerBlobStore"/>, reference counted by <see cref="LayerBlobLease"/>.
/// </summary>
internal sealed class LayerBlob
{
    private readonly LayerBlobStore _store;
    private readonly WeakReference<byte[]?> _loaded = new(null);
    private int _references = 1;

    public LayerBlobStore.Pack Pack { get; }
    public long Offset { get; }
    public int Length { get; }
    public ulong Hash { get; }

    public LayerBlob(LayerBlobStore store, LayerBlobStore.Pack pack, long offset, int length, ulong hash)
    {
        _store = store;
        Pack = pack;
        Offset = offset;
        Length = length;
        Hash = hash;
    }

    public LayerBlobStore Store => _store;

    public byte[]? TryGetLoaded() => _loaded.TryGetTarget(out var bytes) ? bytes : null;

    public void SetLoaded(byte[] bytes) => _loaded.SetTarget(bytes);

    /// <summary>
    /// Adds a reference, fails when the blob was already released.
    /// </summary>
    public bool TryAddReference()
    {
        var current = Volatile.Read(ref _references);
        while (current > 0)
        {
            var previous = Interlocked.CompareExchange(ref _references, current + 1, current);
            if (previous == current) return true;
            current = previous;
        }

        return false;
    }

    public void ReleaseReference()
    {
        if (Interlocked.Decrement(ref _references) == 0) _store.OnBlobReleased(this);
    }
}

/// <summary>
/// Holds one reference to a <see cref="LayerBlob"/>, released explicitly or when the owner is collected.
/// </summary>
internal sealed class LayerBlobLease
{
    private int _released;

    public LayerBlob Blob { get; }

    /// <summary>
    /// Takes ownership of one reference already added to <paramref name="blob"/>.
    /// </summary>
    public LayerBlobLease(LayerBlob blob)
    {
        Blob = blob;
    }

    /// <summary>
    /// Creates another lease to the same blob, or <see langword="null"/> when it was already released.
    /// </summary>
    public LayerBlobLease? Share() => Blob.TryAddReference() ? new LayerBlobLease(Blob) : null;

    public void Release()
    {
        if (Interlocked.Exchange(ref _released, 1) != 0) return;
        GC.SuppressFinalize(this);
        Blob.ReleaseReference();
    }

    ~LayerBlobLease()
    {
        if (Interlocked.Exchange(ref _released, 1) != 0) return;
        try
        {
            Blob.ReleaseReference();
        }
        catch
        {
            // Never throw from the finalizer
        }
    }
}
