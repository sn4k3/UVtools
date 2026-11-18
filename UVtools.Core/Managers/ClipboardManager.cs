/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using ZLinq;

namespace UVtools.Core.Managers;

public sealed class ClipboardItem : List<Layer>
{
    private Operation _operation = null!;

    #region Properties

    /// <summary>
    /// Gets the LayerCount for this clip
    /// </summary>
    public uint LayerCount { get; }

    /// <summary>
    /// Gets the LayerHeight for this clip
    /// </summary>
    public float LayerHeight { get; }

    /// <summary>
    /// Gets the Resolution for this clip
    /// </summary>
    public Size Resolution { get; }

    /// <summary>
    /// Gets the description of this operation
    /// </summary>
    public string? Description { get; set; }

    public bool IsFullBackup { get; set; }

    /// <summary>
    /// False when compaction removed the exact predecessor required to rerun this operation.
    /// </summary>
    public bool CanRerun { get; internal set; } = true;


    public Operation Operation
    {
        get => _operation;
        set
        {
            _operation = value;
            _operation.ImportedFrom = Operation.OperationImportFrom.Undo;
        }
    }

    #endregion

    #region Constructor
    public ClipboardItem(FileFormat slicerFile, Operation operation, bool isFullBackup = false) : this(slicerFile, string.Empty, isFullBackup)
    {
        Operation = operation;
        string description = operation.ToString();
        if (!description.StartsWith(operation.Title)) description = $"{operation.Title}: {description}";
        Description = description;
    }

    public ClipboardItem(FileFormat slicerFile, string? description = null, bool isFullBackup = false)
    {
        LayerCount = slicerFile.LayerCount;
        LayerHeight = slicerFile.LayerHeight;
        Resolution = slicerFile.Resolution;
        Description = description;
        IsFullBackup = isFullBackup;
    }

    /// <summary>
    /// Creates an empty clip with the same definitions of <paramref name="source"/>.
    /// </summary>
    private ClipboardItem(ClipboardItem source, bool isFullBackup)
    {
        LayerCount = source.LayerCount;
        LayerHeight = source.LayerHeight;
        Resolution = source.Resolution;
        Description = source.Description;
        IsFullBackup = isFullBackup;
        _operation = source._operation;
    }

    /// <summary>
    /// Merges an older clip into a newer one, so the older can be removed from the history while restoring the newer
    /// still gives the same layers.
    /// </summary>
    /// <param name="older">The clip right before <paramref name="newer"/>.</param>
    /// <param name="newer">The clip right after <paramref name="older"/>.</param>
    /// <returns>The clip to replace <paramref name="newer"/> with.</returns>
    internal static ClipboardItem Merge(ClipboardItem older, ClipboardItem newer)
    {
        // A full backup does not depend on the clips before it
        if (newer.IsFullBackup) return newer;

        var layers = new Layer?[newer.LayerCount];
        foreach (var layer in older)
        {
            if (layer.Index < layers.Length) layers[layer.Index] = layer;
        }

        foreach (var layer in newer)
        {
            if (layer.Index < layers.Length) layers[layer.Index] = layer;
        }

        var merged = new ClipboardItem(newer, older.IsFullBackup);
        foreach (var layer in layers)
        {
            if (layer is not null) merged.Add(layer);
        }

        if (merged.Count == merged.LayerCount) merged.IsFullBackup = true;
        return merged;
    }
    #endregion

    #region Methods
    public override string ToString()
    {
        return $"{(IsFullBackup ? "* " : "")}{Description} ({Count})";
    }
    #endregion
}

public sealed partial class ClipboardManager : ObservableObject, IList<ClipboardItem>, IMemoryTrimmable
{
    #region Properties

    public RangeObservableCollection<ClipboardItem> Items { get; } = [];

    public FileFormat SlicerFile { get; set; } = null!;

    private int _currentIndex = -1;
    /// <summary>
    /// Gets the index of current item
    /// </summary>
    public int CurrentIndex {
        get => _currentIndex;
        set
        {
            if (_currentIndex == value) return;
            value = Math.Clamp(value, -1, Count - 1);
            _currentIndex = value;
            if (value >= 0 && !SuppressRestore)
            {
                ReallocatedLayerCount = false;
                var targetClip = this[value];

                Layer[] layers;
                if (targetClip.IsFullBackup)
                {
                    if (!ReallocatedLayerCount && SlicerFile.LayerCount != targetClip.Count) ReallocatedLayerCount = true;
                    layers = targetClip.ToArray();
                }
                else
                {
                    // Find the nearest full backup at or above targetIndex (chronologically older)
                    int baseIndex = value + 1;
                    while (baseIndex < Count && !this[baseIndex].IsFullBackup)
                    {
                        baseIndex++;
                    }

                    if (baseIndex >= Count)
                    {
                        baseIndex = Count - 1;
                    }

                    var baseClip = this[baseIndex];
                    layers = baseClip.ToArray();

                    // Replay forward changes from baseIndex - 1 down to targetIndex
                    for (int i = baseIndex - 1; i >= value; i--)
                    {
                        var stepClip = this[i];
                        if (stepClip.IsFullBackup)
                        {
                            layers = stepClip.ToArray();
                            continue;
                        }

                        if (layers.Length != stepClip.LayerCount)
                        {
                            Array.Resize(ref layers, (int)stepClip.LayerCount);
                        }

                        foreach (var layer in stepClip)
                        {
                            if (layer.Index < layers.Length)
                            {
                                layers[layer.Index] = layer;
                            }
                        }
                    }

                    if (!ReallocatedLayerCount && SlicerFile.LayerCount != targetClip.LayerCount)
                    {
                        ReallocatedLayerCount = true;
                    }
                }

                if (SlicerFile.LayerHeight != targetClip.LayerHeight)
                {
                    SlicerFile.LayerHeight = targetClip.LayerHeight;
                }

                if (SlicerFile.Resolution != targetClip.Resolution)
                {
                    SlicerFile.Resolution = targetClip.Resolution;
                }

                SlicerFile.SuppressRebuildPropertiesWork(() =>
                {
                    SlicerFile.Layers = Layer.CloneLayers(layers);
                });
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentIndexCountString));
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            return;
        }
    }

    public string CurrentIndexCountString => string.Format($"{{0:D{Count.DigitCount()}}}", CurrentIndex + 1);

    [ObservableProperty]
    public partial bool SuppressRestore { get; set; }

    [ObservableProperty]
    public partial bool ReallocatedLayerCount { get; set; }

    [ObservableProperty]
    public partial Layer[]? SnapshotLayers { get; private set; }

    public ClipboardItem? CurrentClip => _currentIndex < 0 || _currentIndex >= Count ? null : this[_currentIndex];

    public bool CanUndo => CurrentIndex < Count - 1;
    public bool CanRedo => CurrentIndex > 0;

    #endregion

    #region Singleton
    private static readonly Lazy<ClipboardManager> InstanceHolder =
        new(() => []);

    public static ClipboardManager Instance => InstanceHolder.Value;
    #endregion

    #region Constructor
    private ClipboardManager() { }
    #endregion

    #region List Implementation
    public IEnumerator<ClipboardItem> GetEnumerator() => Items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(ClipboardItem item) => Items.Add(item);

    public void Clear()
    {
        Items.Clear();
        CurrentIndex = -1;
    }

    public void Clear(bool keepOriginal)
    {
        if (!keepOriginal)
        {
            Clear();
            return;
        }

        if (Count == 0) return;

        Init(SlicerFile);
    }

    public bool Contains(ClipboardItem item) => Items.Contains(item);

    public void CopyTo(ClipboardItem[] array, int arrayIndex) => Items.CopyTo(array, arrayIndex);

    public bool Remove(ClipboardItem item) => Items.Remove(item);

    public int Count => Items.Count;
    public bool IsReadOnly => false;
    public int IndexOf(ClipboardItem item) => Items.IndexOf(item);

    public void Insert(int index, ClipboardItem item) => Items.Insert(index, item);

    public void RemoveAt(int index) => Items.RemoveAt(index);

    public ClipboardItem this[int index]
    {
        get => Items[index];
        set => Items[index] = value;
    }
    #endregion

    #region Methods

    public void SuppressRestoreWork(Action action)
    {
        try
        {
            SuppressRestore = true;
            action.Invoke();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            throw;
        }
        finally
        {
            SuppressRestore = false;
        }
    }

    /// <summary>
    /// Clears the manager
    /// </summary>
    public void Reset() => Init(null!);

    /// <summary>
    /// Clears and init clipboard
    /// </summary>
    /// <param name="slicerFile"></param>
    public void Init(FileFormat slicerFile)
    {
        Clear();
        SlicerFile = slicerFile;
        if (slicerFile is null || slicerFile.DecodeType == FileFormat.FileDecodeType.Partial) return;
        MemoryGovernor.Register(this);
        SuppressRestoreWork(() =>
        {
            var clip = new ClipboardItem(SlicerFile, "Original layers", true);
            clip.AddRange(SlicerFile.CloneLayers());
            Add(clip);
            slicerFile.SuppressRebuildPropertiesWork(() => CurrentIndex = 0);
        });
    }

    /// <summary>
    /// Snapshot layers and prepare manager to collect modified layers with <see>
    ///     <cref>Clip</cref>
    /// </see>
    /// </summary>
    public void Snapshot(Layer[]? layers = null)
    {
        SnapshotLayers = layers ?? SlicerFile.CloneLayers();
    }

    public void RestoreSnapshot()
    {
        if (SnapshotLayers is null) return;
        SlicerFile.Layers = SnapshotLayers;
        SnapshotLayers = null;
    }

    /// <summary>
    /// Gets a layer as it was at a clip of the history, without restoring the clip.
    /// </summary>
    /// <remarks>The layer is the one stored in the history, it must not be modified.</remarks>
    /// <param name="clipIndex">The index of the clip, 0 is the newest.</param>
    /// <param name="layerIndex">The index of the layer.</param>
    /// <returns>The layer, or <see langword="null"/> when the clip doesn't exist or didn't have that layer.</returns>
    public Layer? GetLayerAtClip(int clipIndex, uint layerIndex)
    {
        if (clipIndex < 0 || clipIndex >= Count) return null;

        // The clips store the layers that changed, restoring one replays them from the nearest older full backup, so
        // the layer is the one of the newest clip, from the target to older clips, that has it
        for (var i = clipIndex; i < Count; i++)
        {
            var clip = this[i];
            if (clip.IsFullBackup)
            {
                return layerIndex < clip.Count ? clip[(int)layerIndex] : null;
            }

            // A clip with fewer layers truncated the older ones
            if (layerIndex >= clip.LayerCount) return null;

            foreach (var layer in clip)
            {
                if (layer.Index == layerIndex) return layer;
            }
        }

        return null;
    }

    /// <summary>
    /// Collect differences and create a clip
    /// </summary>
    public ClipboardItem? Clip(string? description = null, Layer[]? layers = null, bool doFullBackup = false)
    {
        ClipboardItem? safeClip = null;
        if (!doFullBackup)
        {
            if (layers is not null) Snapshot(layers);
            if (SnapshotLayers is null) throw new InvalidOperationException("A snapshot is required before perform a clip");

            if (SnapshotLayers.Length != SlicerFile.LayerCount)
            {
                doFullBackup = true; // Force full backup when layer count changes
                if (Count > 0 && !this[0].IsFullBackup)
                {
                    safeClip = new ClipboardItem(SlicerFile, "Fail-safe full backup", true);
                    safeClip.AddRange(SnapshotLayers);
                }

                //Insert(0, safeClip);
            }
        }

        var clip = new ClipboardItem(SlicerFile, description, doFullBackup);

        if (doFullBackup)
        {
            clip.AddRange(SlicerFile.CloneLayers());
        }
        else
        {
            int layerIndex = 0;
            for (;
                 layerIndex < SlicerFile.LayerCount
                 && layerIndex < SnapshotLayers!.Length;
                 layerIndex++)
            {
                //if(SnapshotLayers.Count - 1 < layerIndex) break;
                if (SnapshotLayers[layerIndex].Equals(SlicerFile[layerIndex])) continue;
                clip.Add(SlicerFile[layerIndex].Clone());
            }

            // Collect leftovers from snapshot
            // This happens when current state has less layers then the snapshot/previous
            // So we need to preserve them
            for (; layerIndex < SlicerFile.LayerCount; layerIndex++)
            {
                clip.Add(SlicerFile[layerIndex].Clone());
            }

            if (clip.Count == SlicerFile.LayerCount)
            {
                clip.IsFullBackup = true;
            }
        }

        SnapshotLayers = null;

        if (clip.Count == 0)
        {
            if(Count > 0 && this[0].LayerCount == clip.LayerCount)
                return null;
        }

        SuppressRestoreWork(() =>
        {
            var oldCurrentIndex = _currentIndex;
            CurrentIndex = -1;

            // Remove all redo's for integrity
            for (int i = oldCurrentIndex - 1; i >= 0; i--)
            {
                //if(this[i].IsFullBackup) continue; // Full backups can survive
                RemoveAt(i);
            }

            if (safeClip is not null)
            {
                Insert(0, safeClip);
            }
            Insert(0, clip);

            LimitHistory();

            CurrentIndex = 0;
        });

        if (CoreSettings.StoreUndoHistoryOnDisk) QueueMoveHistoryToDisk();

        return clip;
    }

    /// <summary>
    /// Keeps at most <see cref="CoreSettings.MaximumUndoHistory"/> clips plus the original layers, merging the oldest
    /// clips into the ones after them.
    /// </summary>
    private void LimitHistory()
    {
        var maximum = CoreSettings.MaximumUndoHistory;
        if (maximum <= 0) return;

        // The oldest clip holds the original layers and is always kept
        while (Count > maximum + 1 && Count >= 3)
        {
            var olderIndex = Count - 2;
            var newerIndex = olderIndex - 1;
            var merged = ClipboardItem.Merge(this[olderIndex], this[newerIndex]);
            merged.CanRerun = false;
            if (!ReferenceEquals(merged, this[newerIndex])) this[newerIndex] = merged;
            RemoveAt(olderIndex);
        }
    }

    private int _moveHistoryToDiskState; // 0: idle, 1: running, 2: running and requested again

    /// <summary>
    /// Gets a copy of the clips, safe to enumerate from any thread.
    /// </summary>
    private ClipboardItem[] GetClipsCopy()
    {
        try
        {
            var clips = new ClipboardItem[Items.Count];
            Items.CopyTo(clips, 0);
            return clips;
        }
        catch (ArgumentException)
        {
            return []; // Changed meanwhile
        }
    }

    /// <summary>
    /// Moves the layers of the undo history to the disk cache.
    /// </summary>
    /// <param name="includeShared">True to also move the images still in use by the file, they are written once and
    /// only released when the file moves them too.</param>
    /// <returns>The compressed bytes moved to the disk.</returns>
    public long MoveHistoryToDisk(bool includeShared = false)
    {
        return MoveHistoryToDisk(LayerBlobStore.Session, includeShared);
    }

    /// <summary>
    /// Moves the undo history to the specified store, or does nothing when no store is available.
    /// </summary>
    public long MoveHistoryToDisk(LayerBlobStore? store, bool includeShared = false)
    {
        var slicerFile = SlicerFile;
        if (store is null || slicerFile is null) return 0;

        HashSet<byte[]>? inUse = null;
        if (!includeShared)
        {
            var fileLayers = slicerFile.Layers;
            inUse = new HashSet<byte[]>(fileLayers.Length, ReferenceEqualityComparer.Instance);
            foreach (var layer in fileLayers)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (layer is not null) inUse.Add(layer.CompressedArrayInMemory);
            }
        }

        long released = 0;
        void Move(Layer? layer)
        {
            if (layer is null || layer.IsOnDisk) return;
            if (inUse is not null && inUse.Contains(layer.CompressedArrayInMemory)) return;
            released += layer.TryMoveToDisk(store);
        }

        foreach (var clip in GetClipsCopy())
        {
            for (var i = 0; i < clip.Count; i++) Move(clip[i]);
        }

        var snapshot = SnapshotLayers;
        if (snapshot is not null)
        {
            foreach (var layer in snapshot) Move(layer);
        }

        return released;
    }

    /// <summary>
    /// Runs <see cref="MoveHistoryToDisk"/> in background, coalescing requests made while it runs.
    /// </summary>
    private void QueueMoveHistoryToDisk()
    {
        if (LayerBlobStore.Session is null) return;
        while (true)
        {
            var state = Volatile.Read(ref _moveHistoryToDiskState);
            if (state == 2) return;
            if (Interlocked.CompareExchange(ref _moveHistoryToDiskState, state + 1, state) != state) continue;
            if (state == 1) return; // The running task will run again
            break;
        }

        Task.Run(() =>
        {
            do
            {
                Volatile.Write(ref _moveHistoryToDiskState, 1);
                try
                {
                    MoveHistoryToDisk();
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"{nameof(ClipboardManager)}: {e}");
                }
            } while (Interlocked.CompareExchange(ref _moveHistoryToDiskState, 0, 1) != 1);
        });
    }

    /// <summary>
    /// Moves the undo history to the disk cache under memory pressure.
    /// </summary>
    public long Trim(MemoryPressureLevel level)
    {
        return MoveHistoryToDisk(includeShared: level >= MemoryPressureLevel.High);
    }

    /// <summary>
    /// Collect differences and create a clip
    /// </summary>
    public ClipboardItem? Clip(Operation operation, Layer[]? layersSnapshot = null, bool doFullBackup = false)
    {
        string description = operation.ToString();
        if (!description.StartsWith(operation.Title)) description = $"{operation.Title}: {description}";
        var clip = Clip(description, layersSnapshot, doFullBackup);
        if (clip is null) return null;
        clip.Operation = operation;
        return clip;
    }

    public void Undo()
    {
        if (!CanUndo) return;
        CurrentIndex++;
    }

    public void Redo()
    {
        if (!CanRedo) return;
        CurrentIndex--;
    }

    public void SetToOldest() => CurrentIndex = Count-1;
    public void SetToNewest() => CurrentIndex = 0;

    #endregion
}
