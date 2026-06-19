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
    #endregion

    #region Methods
    public override string ToString()
    {
        return $"{(IsFullBackup ? "* " : "")}{Description} ({Count})";
    }
    #endregion
}

public sealed partial class ClipboardManager : ObservableObject, IList<ClipboardItem>
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
            var oldIndex = _currentIndex;
            _currentIndex = value;
            if (value >= 0 && !SuppressRestore)
            {
                ReallocatedLayerCount = false;
                int dir = oldIndex < _currentIndex ? 1 : -1;

                for (int i = oldIndex + dir; i >= 0 && i < Count; i += dir)
                {
                    var clip = this[i];

                    Layer[] layers;
                    if (clip.IsFullBackup)
                    {
                        if(!ReallocatedLayerCount && SlicerFile.LayerCount != clip.Count) ReallocatedLayerCount = true;
                        layers = clip.ToArray();
                    }
                    else
                    {
                        layers = SlicerFile.Layers.AsValueEnumerable().ToArray();

                        if (SlicerFile.LayerCount != clip.LayerCount) // Need resize layer manager
                        {
                            //layers = SlicerFile.LayerManager.ReallocateNew(clip.LayerCount);
                            layers = SlicerFile.ReallocateNew(clip.LayerCount);
                            ReallocatedLayerCount = true;
                        }

                        foreach (var layer in clip)
                        {
                            layers[layer.Index] = layer;
                        }
                    }

                    if (SlicerFile.LayerHeight != clip.LayerHeight)
                    {
                        SlicerFile.LayerHeight = clip.LayerHeight;
                    }

                    if (SlicerFile.Resolution != clip.Resolution)
                    {
                        SlicerFile.Resolution = clip.Resolution;
                    }

                    SlicerFile.SuppressRebuildPropertiesWork(() =>
                    {
                        SlicerFile.Layers = Layer.CloneLayers(layers);
                    });

                    if (i == _currentIndex) break;
                }
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

            CurrentIndex = 0;
        });

        return clip;
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
