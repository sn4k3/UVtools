/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace UVtools.Core.Managers;

/// <summary>
/// The outcome of the most recent disk-cache write attempt.
/// </summary>
public enum LayerCacheWriteStatus
{
    Ready,
    SizeLimit,
    InsufficientDiskSpace,
    IoError,
    Disposed
}

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
    private static ReadOnlySpan<byte> SessionMarker => "UVtools.LayerCache.v1"u8;

    /// <summary>
    /// The disk free space to always leave untouched.
    /// </summary>
    private const long MinimumDiskFreeBytes = 2L * 1024 * 1024 * 1024;

    #endregion

    #region Static

    private static readonly Lock SessionLock = new();
    private static LayerBlobStore? _session;
    private static bool _sessionFailed;
    private static string? _sessionError;

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
                    Volatile.Write(ref _sessionError, e.Message);
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
    /// Describes the session without creating a cache or performing disk I/O.
    /// </summary>
    public static string GetSessionStatusString()
    {
        if (!CoreSettings.EnableDiskCache) return "Disk cache disabled";
        var session = SessionIfCreated;
        if (session is not null) return session.GetStatusString();
        var error = Volatile.Read(ref _sessionError);
        return error is null ? "Disk cache not yet used" : $"Disk cache unavailable: {error}";
    }

    /// <summary>
    /// Deletes the session folders of processes that are not running anymore.
    /// </summary>
    /// <param name="cacheFolder">The folder holding the session folders.</param>
    /// <returns>The number of deleted folders.</returns>
    public static int DeleteOrphanSessions(string cacheFolder)
    {
        if (!Directory.Exists(cacheFolder)) return 0;
        using var coordination = AcquireCacheRoot(cacheFolder);
        try
        {
            return DeleteOrphanSessionsCore(cacheFolder);
        }
        finally
        {
            coordination.ReleaseMutex();
        }
    }

    private static int DeleteOrphanSessionsCore(string cacheFolder)
    {
        var deleted = 0;
        Span<byte> marker = stackalloc byte[SessionMarker.Length];
        foreach (var directory in Directory.EnumerateDirectories(cacheFolder))
        {
            try
            {
                var name = Path.GetFileName(directory).AsSpan();
                var separator = name.IndexOf('-');
                if (separator <= 0 || !uint.TryParse(name[..separator], out _) ||
                    !Guid.TryParseExact(name[(separator + 1)..], "N", out _) ||
                    (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) continue;
                var lockFile = Path.Combine(directory, LockFileName);
                if (!File.Exists(lockFile)) continue;
                using (var stream = new FileStream(lockFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    // Both the session name and its persistent ownership marker must match.
                    if (stream.Length != SessionMarker.Length) continue;
                    stream.ReadExactly(marker);
                    if (!marker.SequenceEqual(SessionMarker)) continue;
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

    private static System.Threading.Mutex AcquireCacheRoot(string cacheFolder)
    {
        var path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(cacheFolder));
        if (OperatingSystem.IsWindows()) path = path.ToUpperInvariant();
        var name = "UVtools.LayerCache." + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path)));
        var coordination = new System.Threading.Mutex(false, name);
        try
        {
            try { coordination.WaitOne(); }
            catch (AbandonedMutexException) { /* Ownership is granted when the previous process crashed. */ }
            return coordination;
        }
        catch
        {
            coordination.Dispose();
            throw;
        }
    }

    #endregion

    #region Members

    private readonly Lock _mutex = new();
    private readonly FileStream _lockStream;
    private readonly ConditionalWeakTable<byte[], LayerBlob> _blobsByArray = new();
    private readonly HashSet<Pack> _packs = [];
    private readonly Func<string, long> _availableDiskSpace;
    private readonly long _packSize;
    private Pack? _currentPack;
    private int _packCount;
    private long _bytesOnDisk;
    private long _filesLength;
    private bool _isDisposed;
    private int _lastWriteStatus;
    private string? _lastError;

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

    /// <summary>Gets the most recent write outcome.</summary>
    public LayerCacheWriteStatus LastWriteStatus => (LayerCacheWriteStatus)Volatile.Read(ref _lastWriteStatus);

    /// <summary>Gets the error of the most recent failed write, if any.</summary>
    public string? LastError => Volatile.Read(ref _lastError);

    /// <summary>Describes physical usage, live data and the most recent write outcome.</summary>
    public string GetStatusString()
    {
        var status = LastWriteStatus switch
        {
            LayerCacheWriteStatus.SizeLimit => "size limit reached",
            LayerCacheWriteStatus.InsufficientDiskSpace => "insufficient free disk space",
            LayerCacheWriteStatus.IoError => "I/O error",
            LayerCacheWriteStatus.Disposed => "closed",
            _ => "ready"
        };
        var maximum = MaximumBytes;
        var limit = maximum > 0 ? $", limit: {maximum / 1048576.0:F0} MB" : string.Empty;
        var error = LastError;
        return $"Disk cache {status}, physical: {FilesLength / 1048576.0:F0} MB, live: {BytesOnDisk / 1048576.0:F0} MB{limit}" +
               (error is null ? string.Empty : $" ({error})");
    }

    private void SetWriteStatus(LayerCacheWriteStatus status, string? error = null)
    {
        Volatile.Write(ref _lastError, error);
        Volatile.Write(ref _lastWriteStatus, (int)status);
    }

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
    public LayerBlobStore(string baseFolder) : this(baseFolder, GetAvailableDiskSpace, PackSize)
    {
    }

    internal LayerBlobStore(string baseFolder, Func<string, long> availableDiskSpace, long packSize)
    {
        ArgumentNullException.ThrowIfNull(availableDiskSpace);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(packSize);
        _availableDiskSpace = availableDiskSpace;
        _packSize = packSize;
        var cacheFolder = Path.Combine(baseFolder, CacheFolderName);
        Directory.CreateDirectory(cacheFolder);
        using var coordination = AcquireCacheRoot(cacheFolder);
        try
        {
            DeleteOrphanSessionsCore(cacheFolder);
            Folder = Path.Combine(cacheFolder, $"{Environment.ProcessId}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Folder);
            _lockStream = new FileStream(Path.Combine(Folder, LockFileName), FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.None, 1);
            try
            {
                _lockStream.Write(SessionMarker);
                _lockStream.Flush(true);
            }
            catch
            {
                _lockStream.Dispose();
                throw;
            }
        }
        finally
        {
            coordination.ReleaseMutex();
        }
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

        lock (_mutex)
        {
            if (_isDisposed) return null;
            if (_blobsByArray.TryGetValue(bytes, out var existing) && existing.TryAddReference())
            {
                return new LayerBlobLease(existing);
            }
            var maximumBytes = MaximumBytes;
            if (maximumBytes > 0 && bytes.Length > maximumBytes - _filesLength)
            {
                SetWriteStatus(LayerCacheWriteStatus.SizeLimit);
                return null;
            }
            // Writes are serialized: no other append in this store can reserve the same free space.
            if (!HasDiskSpaceFor(bytes.Length))
            {
                SetWriteStatus(LayerCacheWriteStatus.InsufficientDiskSpace);
                return null;
            }

            if (_currentPack is null || _currentPack.Length + bytes.Length > _packSize)
            {
                try
                {
                    var newPack = new Pack(Path.Combine(Folder, $"{_packCount++:D5}.pack"));
                    _packs.Add(newPack);
                    _currentPack = newPack;
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    Debug.WriteLine($"{nameof(LayerBlobStore)}: unable to create a pack: {e.Message}");
                    SetWriteStatus(LayerCacheWriteStatus.IoError, e.Message);
                    return null;
                }
            }

            var pack = _currentPack;
            var offset = pack.Length;
            pack.Length += bytes.Length;
            pack.LiveBlobs++;
            _bytesOnDisk += bytes.Length;
            _filesLength += bytes.Length;
            var blob = new LayerBlob(this, pack, offset, bytes.Length, hash);
            try
            {
                RandomAccess.Write(pack.Handle, bytes, offset);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{nameof(LayerBlobStore)}: write failed: {e.Message}");
                SetWriteStatus(LayerCacheWriteStatus.IoError, e.Message);
                blob.ReleaseReference();
                return null;
            }

            blob.SetLoaded(bytes);
            _blobsByArray.AddOrUpdate(bytes, blob);
            SetWriteStatus(LayerCacheWriteStatus.Ready);
            return new LayerBlobLease(blob);
        }
    }

    /// <summary>
    /// Reads the bytes of a blob, returning the array already in memory when another layer has loaded it.
    /// </summary>
    internal byte[] Read(LayerBlob blob)
    {
        var bytes = blob.TryGetLoaded();
        if (bytes is not null) return bytes;

        lock (blob.LoadLock)
        {
            // Different layer clones can reach the same cold blob concurrently.
            bytes = blob.TryGetLoaded();
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
    }

    internal void OnBlobReleased(LayerBlob blob)
    {
        lock (_mutex)
        {
            if (_isDisposed) return;
            _bytesOnDisk -= blob.Length;
            blob.Pack.LiveBlobs--;
            if (blob.Pack.LiveBlobs == 0)
            {
                DeletePack(blob.Pack);
            }
        }
    }

    private void DeletePack(Pack pack)
    {
        if (!_packs.Remove(pack)) return;
        if (ReferenceEquals(pack, _currentPack)) _currentPack = null;
        _filesLength -= pack.Length;
        pack.Dispose();
    }

    private bool HasDiskSpaceFor(long length)
    {
        var available = _availableDiskSpace(Folder);
        return available > MinimumDiskFreeBytes && length < available - MinimumDiskFreeBytes;
    }

    private static long GetAvailableDiskSpace(string folder)
    {
        try
        {
            var drivePath = folder;
            if (OperatingSystem.IsWindows())
            {
                drivePath = Path.GetPathRoot(Path.GetFullPath(folder)) ?? string.Empty;
                // Network shares have no drive letter
                if (drivePath.Length == 0) return 0;
            }

            var drive = new DriveInfo(drivePath);
            return drive.AvailableFreeSpace;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public void Dispose()
    {
        lock (_mutex)
        {
            if (_isDisposed) return;
            _isDisposed = true;
            SetWriteStatus(LayerCacheWriteStatus.Disposed);
            foreach (var pack in _packs) pack.Dispose();
            _packs.Clear();
            _currentPack = null;
            _bytesOnDisk = 0;
            _filesLength = 0;
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
    public readonly Lock LoadLock = new();
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
