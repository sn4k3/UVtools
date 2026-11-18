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
using System.Runtime;
using System.Threading;
using StageKit.Primitives.System;

namespace UVtools.Core.Managers;

/// <summary>
/// The memory pressure of the system, from the available physical memory and <see cref="MemoryGovernor.ReserveBytes"/>.
/// </summary>
public enum MemoryPressureLevel : byte
{
    /// <summary>
    /// Available memory is above two times the reserve.
    /// </summary>
    Normal,

    /// <summary>
    /// Available memory is below two times the reserve: caches are trimmed and the undo history moves to the disk.
    /// </summary>
    Elevated,

    /// <summary>
    /// Available memory is below one and a half times the reserve: idle layers move to the disk and the large object heap
    /// is compacted.
    /// </summary>
    High,

    /// <summary>
    /// Available memory is below the reserve: parallel work is throttled until the pressure goes down.
    /// </summary>
    Critical
}

/// <summary>
/// An object holding memory that can be released or moved to the disk under pressure.
/// </summary>
public interface IMemoryTrimmable
{
    /// <summary>
    /// Releases memory according to the <paramref name="level"/>.
    /// </summary>
    /// <param name="level">The current pressure, never <see cref="MemoryPressureLevel.Normal"/>.</param>
    /// <returns>The estimated number of bytes released.</returns>
    long Trim(MemoryPressureLevel level);
}

/// <summary>
/// Watches the system memory and reacts to the pressure in steps, instead of stopping the work when memory runs low:
/// trim caches, move data to the disk cache (<see cref="LayerBlobStore"/>), compact the managed heap, reduce the
/// parallelism, and only then report the condition so the user can act.
/// </summary>
public static class MemoryGovernor
{
    #region Constants

    private const long GigaByte = 1024L * 1024 * 1024;
    private const int SampleIntervalMilliseconds = 500;
    private const int MaximumSampleAgeMilliseconds = 250;

    /// <summary>
    /// While the pressure is critical only the worker holding the lane runs. When it does not come back to
    /// <see cref="Throttle"/> for this long (its loop ended, or one layer takes long) another worker takes the lane.
    /// </summary>
    private const int ThrottleLaneTimeoutMilliseconds = 2000;

    private const int RelieveCooldownMilliseconds = 5000;
    private const int CompactCooldownMilliseconds = 15000;
    private const long MinimumHeapGrowthBytes = 16L * 1024 * 1024;

    /// <summary>
    /// The estimated number of full resolution images each parallel worker holds at a time.
    /// </summary>
    public const int DefaultImagesPerWorker = 3;

    #endregion

    #region Members

    private static readonly Lock Mutex = new();
    private static readonly List<WeakReference<IMemoryTrimmable>> Trimmables = [];
    private static Timer? _timer;
    private static int _level;
    private static long _availableBytes = long.MaxValue;
    private static long _totalBytes;
    private static long _lastSampleTicks = long.MinValue;
    private static int _laneHeld;
    private static long _laneTicks;
    private static readonly Lock LaneMutex = new();
    private static long _laneGeneration;

    [ThreadStatic]
    private static long _threadLaneGeneration;
    private static readonly AsyncLocal<long> OperationLayerPixelCount = new();
    private static readonly AsyncLocal<bool> LayerMemoryScopeActive = new();
    private static long _fileLayerPixelCount;

    [ThreadStatic]
    private static bool _threadHoldsLane;
    private static long _lastRelieveTicks = long.MinValue;
    private static int _lastRelieveLevel;
    private static long _lastCompactTicks = long.MinValue;
    private static long _lastCompactHeapBytes;
    private static long _lastCompactFragmentedBytes;
    private static long _criticalSinceTicks;
    private static int _isRelieving;
    private static long _relievedBytes;

    #endregion

    #region Properties

    /// <summary>
    /// Gets if the monitor is running.
    /// </summary>
    public static bool IsRunning => _timer is not null;

    /// <summary>
    /// Gets if the adaptive management is enabled, see <see cref="CoreSettings.AdaptiveMemoryManagement"/>.
    /// </summary>
    public static bool IsAdaptive => CoreSettings.AdaptiveMemoryManagement;

    /// <summary>
    /// Gets the current memory pressure.
    /// </summary>
    public static MemoryPressureLevel Level => (MemoryPressureLevel)Volatile.Read(ref _level);

    /// <summary>
    /// Gets the available physical memory from the last sample.
    /// </summary>
    public static long AvailablePhysicalBytes => Interlocked.Read(ref _availableBytes);

    /// <summary>
    /// Gets the total physical memory from the last sample.
    /// </summary>
    public static long TotalPhysicalBytes => Interlocked.Read(ref _totalBytes);

    /// <summary>
    /// Gets the memory to keep available for the system and other programs.
    /// <see cref="CoreSettings.MemoryReserveBytes"/> when set, otherwise 8% of physical memory clamped to 1-3 GB.
    /// Both modes leave at least 1 GB (or a quarter of smaller systems) for work.
    /// </summary>
    public static long ReserveBytes
    {
        get
        {
            return CalculateReserveBytes(CoreSettings.MemoryReserveBytes, TotalPhysicalBytes);
        }
    }

    /// <summary>
    /// Gets or sets the layer pixel count for the current execution context, used to estimate worker memory.
    /// 0 disables the estimation. Prefer <see cref="BeginLayerMemoryScope"/> for operations so the previous value is restored.
    /// </summary>
    public static long LayerPixelCount
    {
        get => OperationLayerPixelCount.Value;
        set => OperationLayerPixelCount.Value = Math.Max(0, value);
    }

    /// <summary>
    /// Gets the layer pixel count used to size the workers: the one of the current scope, otherwise the one of the
    /// last file that got its layers, so parallel work outside of an operation (issue detection, previews...) is
    /// sized too. 0 when unknown.
    /// </summary>
    public static long EffectiveLayerPixelCount
    {
        get
        {
            var scoped = LayerPixelCount;
            return scoped > 0 ? scoped : Interlocked.Read(ref _fileLayerPixelCount);
        }
    }

    /// <summary>
    /// Informs the size of the layers of a file, so the workers can be sized to it.
    /// </summary>
    /// <param name="pixelCount">The pixels of a layer, ignored when not positive.</param>
    /// <param name="isFileLoaded">True when the file holds its layers, it becomes the size used outside of any scope.
    /// False while the file is still being decoded: only the active scope, if any, is updated.</param>
    public static void NotifyLayerSize(long pixelCount, bool isFileLoaded)
    {
        if (pixelCount <= 0) return;
        if (isFileLoaded) Interlocked.Exchange(ref _fileLayerPixelCount, pixelCount);
        // Updates the scope of the caller, which is restored when the scope ends
        if (LayerMemoryScopeActive.Value) LayerPixelCount = pixelCount;
    }

    /// <summary>
    /// Calculates the effective reserve, leaving at least 1 GB (or a quarter of smaller systems) for work.
    /// </summary>
    public static long CalculateReserveBytes(long configuredBytes, long totalBytes)
    {
        var reserve = configuredBytes > 0 ? configuredBytes
            : totalBytes > 0 ? Math.Clamp(totalBytes * 8 / 100, GigaByte, 3 * GigaByte) : GigaByte;
        return totalBytes > 0 ? Math.Min(reserve, totalBytes - Math.Min(GigaByte, totalBytes / 4)) : reserve;
    }

    /// <summary>
    /// Scopes worker sizing to this operation and the execution contexts dispatched from it.
    /// </summary>
    public static IDisposable BeginLayerMemoryScope(long pixelCount) => new LayerMemoryScope(pixelCount);

    private sealed class LayerMemoryScope : IDisposable
    {
        private readonly long _previous = LayerPixelCount;
        private readonly bool _previousActive = LayerMemoryScopeActive.Value;
        private bool _disposed;

        public LayerMemoryScope(long pixelCount)
        {
            LayerPixelCount = pixelCount;
            LayerMemoryScopeActive.Value = true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            LayerPixelCount = _previous;
            LayerMemoryScopeActive.Value = _previousActive;
            _disposed = true;
        }
    }

    /// <summary>
    /// Gets for how long the pressure is <see cref="MemoryPressureLevel.Critical"/>, or <see cref="TimeSpan.Zero"/>.
    /// </summary>
    public static TimeSpan CriticalDuration
    {
        get
        {
            var since = Interlocked.Read(ref _criticalSinceTicks);
            return since == 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(Environment.TickCount64 - since);
        }
    }

    /// <summary>
    /// Gets if workers are currently being throttled.
    /// </summary>
    public static bool IsThrottling => IsAdaptive && Level == MemoryPressureLevel.Critical;

    /// <summary>
    /// Gets the estimated bytes released by the trimmables since the start.
    /// </summary>
    public static long RelievedBytes => Interlocked.Read(ref _relievedBytes);

    /// <summary>
    /// Occurs when <see cref="Level"/> changes, raised from a thread-pool thread.
    /// </summary>
    public static event EventHandler<MemoryPressureLevel>? PressureChanged;

    #endregion

    #region Methods

    /// <summary>
    /// Starts the monitor, it samples the memory twice per second and relieves the pressure in background.
    /// </summary>
    public static void Start()
    {
        lock (Mutex)
        {
            if (_timer is not null) return;
            Sample();
            _timer = new Timer(_ => OnTimer(), null, SampleIntervalMilliseconds, SampleIntervalMilliseconds);
        }
    }

    /// <summary>
    /// Stops the monitor.
    /// </summary>
    public static void Stop()
    {
        lock (Mutex)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }

    /// <summary>
    /// Registers an object to be trimmed under pressure. It is held by a weak reference.
    /// </summary>
    public static void Register(IMemoryTrimmable trimmable)
    {
        lock (Trimmables)
        {
            for (var i = Trimmables.Count - 1; i >= 0; i--)
            {
                if (!Trimmables[i].TryGetTarget(out var target))
                {
                    Trimmables.RemoveAt(i);
                    continue;
                }

                if (ReferenceEquals(target, trimmable)) return;
            }

            Trimmables.Add(new WeakReference<IMemoryTrimmable>(trimmable));
        }
    }

    /// <summary>
    /// Unregisters an object previously registered with <see cref="Register"/>.
    /// </summary>
    public static void Unregister(IMemoryTrimmable trimmable)
    {
        lock (Trimmables)
        {
            for (var i = Trimmables.Count - 1; i >= 0; i--)
            {
                if (!Trimmables[i].TryGetTarget(out var target) || ReferenceEquals(target, trimmable))
                {
                    Trimmables.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// Samples the memory now and updates <see cref="Level"/>.
    /// </summary>
    /// <returns>The current level.</returns>
    public static MemoryPressureLevel Sample()
    {
        Interlocked.Exchange(ref _lastSampleTicks, Environment.TickCount64);
        if (!HostSystem.TryGetMemoryStatus(out var status) || status.TotalPhysicalBytes <= 0) return Level;

        Interlocked.Exchange(ref _totalBytes, (long)status.TotalPhysicalBytes);
        var available = (long)status.AvailablePhysicalBytes;
        Interlocked.Exchange(ref _availableBytes, available);

        var reserve = ReserveBytes;
        var level = CalculatePressureLevel(available, reserve, Level);

        if (level == MemoryPressureLevel.Critical)
        {
            Interlocked.CompareExchange(ref _criticalSinceTicks, Environment.TickCount64, 0);
        }
        else
        {
            Interlocked.Exchange(ref _criticalSinceTicks, 0);
        }

        var oldLevel = (MemoryPressureLevel)Interlocked.Exchange(ref _level, (int)level);
        if (oldLevel != level)
        {
            Debug.WriteLine($"{nameof(MemoryGovernor)}: {oldLevel} -> {level}, available {available / 1048576} MB, reserve {reserve / 1048576} MB");
            PressureChanged?.Invoke(null, level);
        }

        return level;
    }

    private static MemoryPressureLevel CalculatePressureLevel(long available, long reserve, MemoryPressureLevel previous)
    {
        var level = available < reserve ? MemoryPressureLevel.Critical
            : available < reserve * 3 / 2 ? MemoryPressureLevel.High
            : available < reserve * 2 ? MemoryPressureLevel.Elevated
            : MemoryPressureLevel.Normal;
        if (level >= previous) return level;
        var recoveryThreshold = previous switch
        {
            MemoryPressureLevel.Critical => reserve,
            MemoryPressureLevel.High => reserve * 3 / 2,
            _ => reserve * 2
        };
        var margin = Math.Clamp(reserve / 10, MinimumHeapGrowthBytes, 128L * 1024 * 1024);
        return available - recoveryThreshold < margin ? previous : level;
    }

    /// <summary>Refreshes the memory sample when it is too old for cache or worker sizing.</summary>
    internal static void RefreshIfStale() => SampleIfStale();

    private static void SampleIfStale()
    {
        var lastSample = Interlocked.Read(ref _lastSampleTicks);
        if (lastSample != long.MinValue && Environment.TickCount64 - lastSample < MaximumSampleAgeMilliseconds) return;
        Sample();
    }

    private static void OnTimer()
    {
        try
        {
            var level = Sample();
            if (level == MemoryPressureLevel.Normal || !IsAdaptive) return;

            var now = Environment.TickCount64;
            var lastRelieve = Interlocked.Read(ref _lastRelieveTicks);
            // Escalations are handled immediately, the same level is handled again after the cooldown
            if (lastRelieve != long.MinValue && (int)level <= Volatile.Read(ref _lastRelieveLevel) &&
                now - lastRelieve < RelieveCooldownMilliseconds) return;

            Relieve(level);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"{nameof(MemoryGovernor)}: {e}");
        }
    }

    /// <summary>
    /// Runs the relief steps for <paramref name="level"/>: trims the registered objects and collects the garbage.
    /// Calls while another relief is running are ignored.
    /// </summary>
    /// <returns>The estimated bytes released by the trimmables.</returns>
    public static long Relieve(MemoryPressureLevel level)
    {
        if (level == MemoryPressureLevel.Normal) return 0;
        if (Interlocked.Exchange(ref _isRelieving, 1) != 0) return 0;
        try
        {
            Interlocked.Exchange(ref _lastRelieveTicks, Environment.TickCount64);
            Volatile.Write(ref _lastRelieveLevel, (int)level);

            long released = 0;
            foreach (var trimmable in GetTrimmables())
            {
                try
                {
                    released += trimmable.Trim(level);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"{nameof(MemoryGovernor)}: trim failed: {e}");
                }
            }

            Interlocked.Add(ref _relievedBytes, released);

            var now = Environment.TickCount64;
            var lastCompact = Interlocked.Read(ref _lastCompactTicks);
            var heapBytes = GC.GetTotalMemory(false);
            var fragmentedBytes = GC.GetGCMemoryInfo().FragmentedBytes;
            if (level >= MemoryPressureLevel.High &&
                (lastCompact == long.MinValue || now - lastCompact >= CompactCooldownMilliseconds) &&
                ShouldCompact(released, heapBytes, fragmentedBytes))
            {
                // The compressed layers live in the large object heap, which is not compacted by default
                Interlocked.Exchange(ref _lastCompactTicks, now);
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                Interlocked.Exchange(ref _lastCompactHeapBytes, GC.GetTotalMemory(false));
                Interlocked.Exchange(ref _lastCompactFragmentedBytes, GC.GetGCMemoryInfo().FragmentedBytes);
            }
            else if (released > 0)
            {
                GC.Collect(2, GCCollectionMode.Optimized, false);
            }

            Debug.WriteLine($"{nameof(MemoryGovernor)}: relieved {released / 1048576} MB at {level}");
            return released;
        }
        finally
        {
            Volatile.Write(ref _isRelieving, 0);
        }
    }

    private static bool ShouldCompact(long released, long heapBytes, long fragmentedBytes)
    {
        var previousHeap = Interlocked.Read(ref _lastCompactHeapBytes);
        return Interlocked.Read(ref _lastCompactTicks) == long.MinValue || released > 0 ||
               heapBytes - previousHeap >= Math.Max(MinimumHeapGrowthBytes, previousHeap / 10) ||
               fragmentedBytes - Interlocked.Read(ref _lastCompactFragmentedBytes) >= MinimumHeapGrowthBytes;
    }

    private static List<IMemoryTrimmable> GetTrimmables()
    {
        var list = new List<IMemoryTrimmable>();
        lock (Trimmables)
        {
            for (var i = Trimmables.Count - 1; i >= 0; i--)
            {
                if (Trimmables[i].TryGetTarget(out var target)) list.Add(target);
                else Trimmables.RemoveAt(i);
            }
        }

        return list;
    }

    /// <summary>
    /// Gets the degree of parallelism that fits in the available memory.
    /// </summary>
    /// <param name="requested">The requested degree of parallelism, -1 for the .NET scheduler.</param>
    /// <param name="bytesPerWorker">The estimated bytes each worker holds at a time, 0 to estimate from
    /// <see cref="LayerPixelCount"/> and <see cref="DefaultImagesPerWorker"/>.</param>
    /// <returns>The degree of parallelism, at least 1, or <paramref name="requested"/> when it fits.</returns>
    public static int GetMaxDegreeOfParallelism(int requested, long bytesPerWorker = 0)
    {
        if (requested == 1 || !IsAdaptive) return requested;
        if (bytesPerWorker <= 0) bytesPerWorker = Math.Min(EffectiveLayerPixelCount, long.MaxValue / DefaultImagesPerWorker) * DefaultImagesPerWorker;
        if (bytesPerWorker <= 0) return requested;

        SampleIfStale();
        if (TotalPhysicalBytes <= 0) return requested;

        var free = AvailablePhysicalBytes - ReserveBytes;
        if (free <= 0) return 1;

        // Keep a margin for what is not tracked: the system, the UI and the managed heap growth
        var budget = free / 4 * 3;
        var fit = (int)Math.Clamp(budget / bytesPerWorker, 1, int.MaxValue);
        var maximum = requested <= 0 ? Environment.ProcessorCount : requested;
        return fit >= maximum ? requested : fit;
    }

    /// <summary>
    /// Blocks the calling worker while the pressure is <see cref="MemoryPressureLevel.Critical"/>, letting only one
    /// worker run at a time, at full speed, so the work keeps progressing with less memory instead of stopping.
    /// Called from <see cref="Operations.OperationProgress.PauseIfRequested"/> before each layer is processed, when the
    /// worker holds no image of the previous layer.
    /// </summary>
    /// <param name="token">Stops waiting when cancelled.</param>
    public static void Throttle(CancellationToken token = default)
    {
        if (Volatile.Read(ref _level) < (int)MemoryPressureLevel.Critical || !IsAdaptive)
        {
            if (_threadHoldsLane) ReleaseLane();
            return;
        }

        while (!token.IsCancellationRequested && IsAdaptive &&
               Volatile.Read(ref _level) >= (int)MemoryPressureLevel.Critical)
        {
            var now = Environment.TickCount64;
            lock (LaneMutex)
            {
                if (_threadHoldsLane && _threadLaneGeneration == _laneGeneration && _laneHeld != 0)
                {
                    _laneTicks = now;
                    return;
                }

                _threadHoldsLane = false;
                if (_laneHeld == 0 || now - _laneTicks >= ThrottleLaneTimeoutMilliseconds)
                {
                    _laneHeld = 1;
                    _threadLaneGeneration = ++_laneGeneration;
                    _threadHoldsLane = true;
                    _laneTicks = now;
                    return;
                }
            }

            if (token.CanBeCanceled) token.WaitHandle.WaitOne(25);
            else Thread.Sleep(25);
        }

        if (_threadHoldsLane) ReleaseLane();
    }

    private static void ReleaseLane()
    {
        lock (LaneMutex)
        {
            if (_threadHoldsLane && _threadLaneGeneration == _laneGeneration) _laneHeld = 0;
            _threadHoldsLane = false;
        }
    }

    /// <summary>
    /// Gets a short description of the memory state.
    /// </summary>
    public static string GetStatusString()
    {
        var disk = LayerBlobStore.GetSessionStatusString();
        return $"{Level}, available: {AvailablePhysicalBytes / (double)GigaByte:F2} GB / {TotalPhysicalBytes / (double)GigaByte:F2} GB, reserve: {ReserveBytes / (double)GigaByte:F2} GB, {disk}";
    }

    #endregion
}
