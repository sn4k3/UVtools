using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Managers
{
    public sealed class ClipboardItem : IList<Layer>
    {
        #region Properties
        private readonly List<Layer> _layers = new();

        /// <summary>
        /// Gets the LayerCount for this clip
        /// </summary>
        public uint LayerCount { get; }

        public float LayerHeight { get; }

        /// <summary>
        /// Gets the description of this operation
        /// </summary>
        public string Description { get; set; }

        
        public Operations.Operation Operation { get; set; }

        #endregion

        #region Constructor
        public ClipboardItem(FileFormat slicerFile, Operations.Operation operation) : this(slicerFile)
        {
            Operation = operation;
            string description = operation.ToString();
            if (!description.StartsWith(operation.Title)) description = $"{operation.Title}: {description}";
            Description = description;
        }
        
        public ClipboardItem(FileFormat slicerFile, string description = null)
        {
            LayerCount = slicerFile.LayerCount;
            LayerHeight = slicerFile.LayerHeight;
            Description = description;
        }
        #endregion

        #region List Implementation
        public IEnumerator<Layer> GetEnumerator() => _layers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(Layer item) => _layers.Add(item);

        public void Clear() => _layers.Clear();

        public bool Contains(Layer item) => _layers.Contains(item);

        public void CopyTo(Layer[] array, int arrayIndex) => _layers.CopyTo(array, arrayIndex);

        public bool Remove(Layer item) => _layers.Remove(item);

        public int Count => _layers.Count;
        public bool IsReadOnly => false;
        public int IndexOf(Layer item) => _layers.IndexOf(item);

        public void Insert(int index, Layer item) => _layers.Insert(index, item);

        public void RemoveAt(int index) => _layers.RemoveAt(index);

        public Layer this[int index]
        {
            get => _layers[index];
            set => _layers[index] = value;
        }
        #endregion

        #region Methods
        public override string ToString()
        {
            return $"{Description} ({Count})";
        }
        #endregion
    }

    public sealed class ClipboardManager : BindableBase, IList<ClipboardItem>
    {
        #region Properties

        public ObservableCollection<ClipboardItem> Items { get; } = new ObservableCollection<ClipboardItem>();

        public FileFormat SlicerFile { get; set; }

        private int _currentIndex = -1;
        private LayerManager _snapshotLayerManager;
        private bool _reallocatedLayerCount;
        private bool _suppressRestore;

        /// <summary>
        /// Gets the index of current item
        /// </summary>
        public int CurrentIndex {
            get => _currentIndex;
            set
            {
                if (_currentIndex == value) return;
                if (value < -1) value = -1;
                if (value >= Count) value = Count-1;
                var oldIndex = _currentIndex;
                _currentIndex = value;
                //if (!RaiseAndSetIfChanged(ref _currentIndex, value)) return;

                if (value >= 0 && !SuppressRestore)
                {
                    int dir = oldIndex < _currentIndex ? 1 : -1;

                    for (int i = oldIndex + dir; i >= 0 && i < Count; i += dir)
                    {
                        var layerManager = SlicerFile.LayerManager;
                        var clip = this[i];
                        if (layerManager.Count != clip.LayerCount) // Need resize layer manager
                        {
                            layerManager = layerManager.Reallocate(clip.LayerCount);
                            ReallocatedLayerCount = true;
                        }

                        layerManager.AddLayers(clip);

                        layerManager.BoundingRectangle = Rectangle.Empty;
                        if (SlicerFile.LayerHeight != clip.LayerHeight)
                        {
                            SlicerFile.LayerHeight = clip.LayerHeight;
                        }
                        SlicerFile.LayerManager = layerManager.Clone();
                        if (i == _currentIndex) break;
                    }
                }

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(CurrentIndexCountStr));
                RaisePropertyChanged(nameof(CanUndo));
                RaisePropertyChanged(nameof(CanRedo));
                return;
            }
        }

        public string CurrentIndexCountStr => (CurrentIndex + 1).ToString().PadLeft(Count.ToString().Length, '0');

        public bool SuppressRestore
        {
            get => _suppressRestore;
            set => RaiseAndSetIfChanged(ref _suppressRestore, value);
        }

        public bool ReallocatedLayerCount
        {
            get => _reallocatedLayerCount;
            set => RaiseAndSetIfChanged(ref _reallocatedLayerCount, value);
        }

        public LayerManager SnapshotLayerManager
        {
            get => _snapshotLayerManager;
            private set => RaiseAndSetIfChanged(ref _snapshotLayerManager, value);
        }
        
        public ClipboardItem CurrentClip => _currentIndex < 0 || _currentIndex >= Count ? null : this[_currentIndex];

        public bool CanUndo => CurrentIndex < Count - 1;
        public bool CanRedo => CurrentIndex > 0;

        #endregion

        #region Singleton
        private static readonly Lazy<ClipboardManager> InstanceHolder =
            new Lazy<ClipboardManager>(() => new ClipboardManager());

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

        /// <summary>
        /// Clears the manager
        /// </summary>
        public void Reset() => Init(null);

        /// <summary>
        /// Clears and init clipboard
        /// </summary>
        /// <param name="slicerFile"></param>
        public void Init(FileFormat slicerFile)
        {
            Clear();
            SlicerFile = slicerFile;
            if(slicerFile is null) return;
            var clip = new ClipboardItem(SlicerFile, "Original layers");
            for (int layerIndex = 0; layerIndex < SlicerFile.LayerCount; layerIndex++)
            {
                clip.Add(SlicerFile[layerIndex].Clone());
            }

            Add(clip);
            CurrentIndex = 0;
        }

        /// <summary>
        /// Snapshot layers and prepare manager to collect modified layers with <see cref="Clip"/>
        /// </summary>
        public void Snapshot(LayerManager layerManager = null)
        {
            SnapshotLayerManager = layerManager ?? SlicerFile.LayerManager.Clone();
        }

        /// <summary>
        /// Collect differences and create a clip
        /// </summary>
        public ClipboardItem Clip(string description = null, LayerManager layerManagerSnapshot = null)
        {
            if(!(layerManagerSnapshot is null)) Snapshot(layerManagerSnapshot);
            if (SnapshotLayerManager is null) throw new InvalidOperationException("A snapshot is required before perform a clip");
            var clip = new ClipboardItem(SlicerFile, description);

            int layerIndex = 0;
            for (; 
                    layerIndex < SlicerFile.LayerCount
                    && layerIndex < SnapshotLayerManager.Count
                ; layerIndex++)
            {
                //if(SnapshotLayerManager.Count - 1 < layerIndex) break;
                if(SnapshotLayerManager[layerIndex].Equals(SlicerFile[layerIndex])) continue;
                
                clip.Add(SlicerFile[layerIndex].Clone());
            }

            // Collect leftovers from snapshot
            // This happens when current layer manager has less layers then the snapshot/previous
            // So we need to preserve them
            for (; layerIndex < SlicerFile.LayerCount; layerIndex++)
            {
                clip.Add(SlicerFile[layerIndex].Clone());
            }

            SnapshotLayerManager = null;

            if (clip.Count == 0)
            {
                if(Count > 0 && this[0].LayerCount == clip.LayerCount)
                    return null;
            }

            SuppressRestore = true;
            var oldCurrentIndex = _currentIndex;
            CurrentIndex = -1;

            // Remove all redo's for integrity
            for (int i = oldCurrentIndex - 1; i >= 0; i--)
            {
                RemoveAt(i);
            }

            
            Insert(0, clip);
            
            CurrentIndex = 0;
            SuppressRestore = false;

            return clip;
        }

        /// <summary>
        /// Collect differences and create a clip
        /// </summary>
        public ClipboardItem Clip(Operations.Operation operation, LayerManager layerManagerSnapshot = null)
        {
            string description = operation.ToString();
            if (!description.StartsWith(operation.Title)) description = $"{operation.Title}: {description}";
            var clip = Clip(description, layerManagerSnapshot);
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

        /*public bool SetTo(int index)
        {
            if (index == _currentIndex || index < 0 || index >= Count) return false;
            bool changed = false;
            int dir = _currentIndex < index ? 1 : -1;

            for (int i = _currentIndex+dir; i >= 0 && i < Count; i+=dir)
            {
                var layerManager = SlicerFile.LayerManager;
                var clip = this[i];
                if (layerManager.Count != clip.LayerCount) // Need resize layer manager
                {
                    layerManager = layerManager.Reallocate(clip.LayerCount);
                    ReallocatedLayerCount = true;
                }

                layerManager.AddLayers(clip);

                layerManager.BoundingRectangle = Rectangle.Empty;
                SlicerFile.LayerManager = layerManager.Clone();
                changed = true;
                if (i == index) break;
            }

            if (changed)
            {
                CurrentIndex = index;
            }

            return changed;
        }*/

        #endregion
    }
}
