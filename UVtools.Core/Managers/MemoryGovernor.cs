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
    /// Available memory is below two times the reserve: caches are trimmed.
    /// </summary>
    Elevated,

    /// <summary>
    /// Available memory is below one and a half times the reserve: the large object heap is compacted.
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
/// trim caches, compact the managed heap, reduce the parallelism, and only then report the condition so the user can act.
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

    [ThreadStatic]
    private static bool _threadHoldsLane;
    private static long _lastRelieveTicks = long.MinValue;
    private static int _lastRelieveLevel;
    private static long _lastCompactTicks = long.MinValue;
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
    /// <see cref="CoreSettings.MemoryReserveBytes"/> when set (up to the physical memory), otherwise 8% of the physical
    /// memory clamped to 1-3 GB.
    /// </summary>
    public static long ReserveBytes
    {
        get
        {
            var reserve = CoreSettings.MemoryReserveBytes;
            var total = TotalPhysicalBytes;
            if (reserve > 0) return total > 0 ? Math.Min(reserve, total) : reserve;
            if (total <= 0) return GigaByte;
            return Math.Clamp(total * 8 / 100, GigaByte, 3 * GigaByte);
        }
    }

    /// <summary>
    /// Gets or sets the pixel count of a layer of the file being processed, used to estimate the memory of each
    /// parallel worker. 0 disables the estimation.
    /// </summary>
    public static long LayerPixelCount { get; set; }

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
        var level = available < reserve ? MemoryPressureLevel.Critical
            : available < reserve * 3 / 2 ? MemoryPressureLevel.High
            : available < reserve * 2 ? MemoryPressureLevel.Elevated
            : MemoryPressureLevel.Normal;

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

    private static void SampleIfStale()
    {
        if (Environment.TickCount64 - Interlocked.Read(ref _lastSampleTicks) < MaximumSampleAgeMilliseconds) return;
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
            if ((int)level <= Volatile.Read(ref _lastRelieveLevel) && now - lastRelieve < RelieveCooldownMilliseconds) return;

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
            if (level >= MemoryPressureLevel.High &&
                now - Interlocked.Read(ref _lastCompactTicks) >= CompactCooldownMilliseconds)
            {
                // The compressed layers live in the large object heap, which is not compacted by default
                Interlocked.Exchange(ref _lastCompactTicks, now);
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(2, GCCollectionMode.Forced, true, true);
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
        if (bytesPerWorker <= 0) bytesPerWorker = LayerPixelCount * DefaultImagesPerWorker;
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

        if (_threadHoldsLane)
        {
            // Done with the previous layer, keep running as the single worker
            Interlocked.Exchange(ref _laneTicks, Environment.TickCount64);
            return;
        }

        while (!token.IsCancellationRequested && Volatile.Read(ref _level) >= (int)MemoryPressureLevel.Critical)
        {
            var now = Environment.TickCount64;
            if (Interlocked.CompareExchange(ref _laneHeld, 1, 0) == 0)
            {
                _threadHoldsLane = true;
                Interlocked.Exchange(ref _laneTicks, now);
                return;
            }

            var laneTicks = Interlocked.Read(ref _laneTicks);
            if (now - laneTicks >= ThrottleLaneTimeoutMilliseconds &&
                Interlocked.CompareExchange(ref _laneTicks, now, laneTicks) == laneTicks)
            {
                // The holder is gone or busy for long, take over so the work never stalls
                _threadHoldsLane = true;
                return;
            }

            if (token.CanBeCanceled) token.WaitHandle.WaitOne(25);
            else Thread.Sleep(25);
        }

        if (_threadHoldsLane) ReleaseLane();
    }

    private static void ReleaseLane()
    {
        _threadHoldsLane = false;
        Volatile.Write(ref _laneHeld, 0);
    }

    /// <summary>
    /// Gets a short description of the memory state.
    /// </summary>
    public static string GetStatusString()
    {
        return $"{Level}, available: {AvailablePhysicalBytes / (double)GigaByte:F2} GB / {TotalPhysicalBytes / (double)GigaByte:F2} GB, reserve: {ReserveBytes / (double)GigaByte:F2} GB";
    }

    #endregion
}
