using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using UVtools.Core.Extensions;
using UVtools.WPF.Extensions;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Brushes = Avalonia.Media.Brushes;
using Color = Avalonia.Media.Color;
using Pen = Avalonia.Media.Pen;
using Point = Avalonia.Point;
using Size = Avalonia.Size;


namespace UVtools.WPF.Controls
{
    public class AdvancedImageBox : ScrollViewer, IStyleable, INotifyPropertyChanged
    {
        #region Bindable Base
        /// <summary>
        ///     Multicast event for property change notifications.
        /// </summary>
        private PropertyChangedEventHandler _propertyChanged;
        private List<string> events = new List<string>();

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { _propertyChanged += value; events.Add("added"); }
            remove { _propertyChanged -= value; events.Add("removed"); }
        }
        protected bool RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }


        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
        }

        /// <summary>
        ///     Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">
        ///     Name of the property used to notify listeners.  This
        ///     value is optional and can be provided automatically when invoked from compilers
        ///     that support <see cref="CallerMemberNameAttribute" />.
        /// </param>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            var e = new PropertyChangedEventArgs(propertyName);
            OnPropertyChanged(e);
            _propertyChanged?.Invoke(this, e);
        }
        #endregion

        #region Sub Classes

        /// <summary>
        /// Represents available levels of zoom in an <see cref="ImageBox"/> control
        /// </summary>
        public class ZoomLevelCollection : IList<int>
        {
            #region Public Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="ZoomLevelCollection"/> class.
            /// </summary>
            public ZoomLevelCollection()
            {
                List = new SortedList<int, int>();
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ZoomLevelCollection"/> class.
            /// </summary>
            /// <param name="collection">The default values to populate the collection with.</param>
            /// <exception cref="System.ArgumentNullException">Thrown if the <c>collection</c> parameter is null</exception>
            public ZoomLevelCollection(IEnumerable<int> collection)
              : this()
            {
                if (collection == null)
                {
                    throw new ArgumentNullException(nameof(collection));
                }

                AddRange(collection);
            }

            #endregion

            #region Public Class Properties

            /// <summary>
            /// Returns the default zoom levels
            /// </summary>
            public static ZoomLevelCollection Default
            {
                get
                {
                    return new ZoomLevelCollection(new[]
                                                   {
                                         7, 10, 15, 20, 25, 30, 50, 70, 100, 150, 200, 300, 400, 500, 600, 700, 800, 1200, 1600, 3200
                                       });
                }
            }

            #endregion

            #region Public Properties

            /// <summary>
            /// Gets the number of elements contained in the <see cref="ZoomLevelCollection" />.
            /// </summary>
            /// <returns>
            /// The number of elements contained in the <see cref="ZoomLevelCollection" />.
            /// </returns>
            public int Count => List.Count;

            /// <summary>
            /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
            /// </summary>
            /// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
            /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.
            /// </returns>
            public bool IsReadOnly => false;

            /// <summary>
            /// Gets or sets the zoom level at the specified index.
            /// </summary>
            /// <param name="index">The index.</param>
            public int this[int index]
            {
                get => List.Values[index];
                set
                {
                    List.RemoveAt(index);
                    Add(value);
                }
            }

            #endregion

            #region Protected Properties

            /// <summary>
            /// Gets or sets the backing list.
            /// </summary>
            protected SortedList<int, int> List { get; set; }

            #endregion

            #region Public Members

            /// <summary>
            /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
            /// </summary>
            /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
            public void Add(int item)
            {
                List.Add(item, item);
            }

            /// <summary>
            /// Adds a range of items to the <see cref="ZoomLevelCollection"/>.
            /// </summary>
            /// <param name="collection">The items to add to the collection.</param>
            /// <exception cref="System.ArgumentNullException">Thrown if the <c>collection</c> parameter is null.</exception>
            public void AddRange(IEnumerable<int> collection)
            {
                if (collection == null)
                {
                    throw new ArgumentNullException(nameof(collection));
                }

                foreach (int value in collection)
                {
                    Add(value);
                }
            }

            /// <summary>
            /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
            /// </summary>
            public void Clear()
            {
                List.Clear();
            }

            /// <summary>
            /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
            /// </summary>
            /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
            /// <returns>true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.</returns>
            public bool Contains(int item)
            {
                return List.ContainsKey(item);
            }

            /// <summary>
            /// Copies a range of elements this collection into a destination <see cref="Array"/>.
            /// </summary>
            /// <param name="array">The <see cref="Array"/> that receives the data.</param>
            /// <param name="arrayIndex">A 64-bit integer that represents the index in the <see cref="Array"/> at which storing begins.</param>
            public void CopyTo(int[] array, int arrayIndex)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    array[arrayIndex + i] = this.List.Values[i];
                }
            }

            /// <summary>
            /// Finds the index of a zoom level matching or nearest to the specified value.
            /// </summary>
            /// <param name="zoomLevel">The zoom level.</param>
            public int FindNearest(int zoomLevel)
            {
                int nearestValue = this.List.Values[0];
                int nearestDifference = Math.Abs(nearestValue - zoomLevel);
                for (int i = 1; i < Count; i++)
                {
                    int value = List.Values[i];
                    int difference = Math.Abs(value - zoomLevel);
                    if (difference < nearestDifference)
                    {
                        nearestValue = value;
                        nearestDifference = difference;
                    }
                }
                return nearestValue;
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
            public IEnumerator<int> GetEnumerator()
            {
                return List.Values.GetEnumerator();
            }

            /// <summary>
            /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" />.
            /// </summary>
            /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
            /// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
            public int IndexOf(int item)
            {
                return List.IndexOfKey(item);
            }

            /// <summary>
            /// Not implemented.
            /// </summary>
            /// <param name="index">The index.</param>
            /// <param name="item">The item.</param>
            /// <exception cref="System.NotImplementedException">Not implemented</exception>
            public void Insert(int index, int item)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Returns the next increased zoom level for the given current zoom.
            /// </summary>
            /// <param name="zoomLevel">The current zoom level.</param>
            /// <returns>The next matching increased zoom level for the given current zoom if applicable, otherwise the nearest zoom.</returns>
            public int NextZoom(int zoomLevel)
            {
                var index = IndexOf(this.FindNearest(zoomLevel));
                if (index < this.Count - 1)
                {
                    index++;
                }

                return this[index];
            }

            /// <summary>
            /// Returns the next decreased zoom level for the given current zoom.
            /// </summary>
            /// <param name="zoomLevel">The current zoom level.</param>
            /// <returns>The next matching decreased zoom level for the given current zoom if applicable, otherwise the nearest zoom.</returns>
            public int PreviousZoom(int zoomLevel)
            {
                var index = IndexOf(FindNearest(zoomLevel));
                if (index > 0)
                {
                    index--;
                }

                return this[index];
            }

            /// <summary>
            /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
            /// </summary>
            /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
            /// <returns>true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
            public bool Remove(int item)
            {
                return List.Remove(item);
            }

            /// <summary>
            /// Removes the element at the specified index of the <see cref="ZoomLevelCollection"/>.
            /// </summary>
            /// <param name="index">The zero-based index of the element to remove.</param>
            public void RemoveAt(int index)
            {
                List.RemoveAt(index);
            }

            /// <summary>
            /// Copies the elements of the <see cref="ZoomLevelCollection"/> to a new array.
            /// </summary>
            /// <returns>An array containing copies of the elements of the <see cref="ZoomLevelCollection"/>.</returns>
            public int[] ToArray()
            {
                int[] results;

                results = new int[Count];
                CopyTo(results, 0);

                return results;
            }

            #endregion

            #region IList<int> Members

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>An <see cref="ZoomLevelCollection" /> object that can be used to iterate through the collection.</returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        #endregion

        #region Enums

        /// <summary>
        /// Determines the sizing mode of an image hosted in an <see cref="AdvancedImageBox" /> control.
        /// </summary>
        public enum SizeModes : byte
        {
            /// <summary>
            /// The image is disiplayed according to current zoom and scroll properties.
            /// </summary>
            Normal,

            /// <summary>
            /// The image is stretched to fill the client area of the control.
            /// </summary>
            Stretch,

            /// <summary>
            /// The image is stretched to fill as much of the client area of the control as possible, whilst retaining the same aspect ratio for the width and height.
            /// </summary>
            //Fit
        }

        [Flags]
        public enum MouseButtons : byte
        {
            None = 0,
            LeftButton = 1,
            MiddleButton = 2,
            RightButton = 4
        }

        /// <summary>
        /// Describes the zoom action occuring
        /// </summary>
        [Flags]
        public enum ZoomActions : byte
        {
            /// <summary>
            /// No action.
            /// </summary>
            None = 0,

            /// <summary>
            /// The control is increasing the zoom.
            /// </summary>
            ZoomIn = 1,

            /// <summary>
            /// The control is decreasing the zoom.
            /// </summary>
            ZoomOut = 2,

            /// <summary>
            /// The control zoom was reset.
            /// </summary>
            ActualSize = 4
        }

        public enum SelectionModes
        {
            /// <summary>
            ///   No selection.
            /// </summary>
            None,

            /// <summary>
            ///   Rectangle selection.
            /// </summary>
            Rectangle,

            /// <summary>
            ///   Zoom selection.
            /// </summary>
            Zoom
        }

        #endregion

        #region Constants
        public static readonly int MinZoom = 10;
        public static readonly int MaxZoom = 3500;
        #endregion

        public bool CanRender
        {
            get => _canRender;
            set
            {
                if(!RaiseAndSetIfChanged(ref _canRender, value)) return;
                if(_canRender) TriggerRender();
            }
        }

        /// <summary>
        /// Gets or sets the basic cell size
        /// </summary>
        public byte GridCellSize
        {
            get => _gridCellSize;
            set => RaiseAndSetIfChanged(ref _gridCellSize, value);
        }

        /// <summary>
        /// Gets or sets the color used to create the checkerboard style background
        /// </summary>
        public ISolidColorBrush GridColor
        {
            get => _gridColor;
            set => RaiseAndSetIfChanged(ref _gridColor, value);
        }

        /// <summary>
        /// Gets or sets the color used to create the checkerboard style background
        /// </summary>
        public ISolidColorBrush GridColorAlternate
        {
            get => _gridColorAlternate;
            set => RaiseAndSetIfChanged(ref _gridColorAlternate, value);
        }

        /// <summary>
        /// Gets or sets the image to be displayed
        /// </summary>
        public Bitmap Image
        {
            get => _image;
            set
            {
                if (!RaiseAndSetIfChanged(ref _image, value)) return;

                SelectNone();
                UpdateViewPort();
                TriggerRender();
            }
        }

        public bool IsHorizontalBarVisible
        {
            get
            {
                if (Image is null) return false;
                return ScaledImageWidth > Viewport.Width;
            }
        }

        public bool IsVerticalBarVisible
        {
            get
            {
                if (Image is null) return false;
                return ScaledImageHeight > Viewport.Height;
            }
        }

        /// <summary>
        /// Gets or sets if the checkerboard background should be displayed
        /// </summary>
        public bool ShowGrid
        {
            get => _showGrid;
            set => RaiseAndSetIfChanged(ref _showGrid, value);
        }

        public bool IsPanning
        {
            get => _isPanning;
            protected set
            {
                if (!RaiseAndSetIfChanged(ref _isPanning, value)) return;
                _startScrollPosition = Offset;

                if (value)
                {
                    Cursor = new Cursor(StandardCursorType.SizeAll);
                    //this.OnPanStart(EventArgs.Empty);
                }
                else
                {
                    Cursor = Cursor.Default;
                    //this.OnPanEnd(EventArgs.Empty);
                }
            }
        }

        public bool IsSelecting
        {
            get => _isSelecting;
            protected set => RaiseAndSetIfChanged(ref _isSelecting, value);
        }

        public Point CenterPoint
        {
            get
            {
                var viewport = GetImageViewPort();
                return new Point( (viewport.Width / 2), viewport.Height / 2);
            }
        }

        public bool AutoPan
        {
            get => _autoPan;
            set => RaiseAndSetIfChanged(ref _autoPan, value);
        }

        public MouseButtons PanWithMouseButtons
        {
            get => _panWithMouseButtons;
            set => RaiseAndSetIfChanged(ref _panWithMouseButtons, value);
        }

        public bool PanWithArrows
        {
            get => _panWithArrows;
            set => RaiseAndSetIfChanged(ref _panWithArrows, value);
        }

        public MouseButtons SelectWithMouseButtons
        {
            get => _selectWithMouseButtons;
            set => RaiseAndSetIfChanged(ref _selectWithMouseButtons, value);
        }

        public bool InvertMouse
        {
            get => _invertMouse;
            set => RaiseAndSetIfChanged(ref _invertMouse, value);
        }

        public bool AutoCenter
        {
            get => _autoCenter;
            set => RaiseAndSetIfChanged(ref _autoCenter, value);
        }

        public SizeModes SizeMode
        {
            get => _sizeMode;
            set => RaiseAndSetIfChanged(ref _sizeMode, value);
        }

        private bool _allowZoom = true;
        public virtual bool AllowZoom
        {
            get => _allowZoom;
            set => RaiseAndSetIfChanged(ref _allowZoom, value);
        }

        ZoomLevelCollection _zoomLevels = ZoomLevelCollection.Default;
        /// <summary>
        ///   Gets or sets the zoom levels.
        /// </summary>
        /// <value>The zoom levels.</value>
        public virtual ZoomLevelCollection ZoomLevels
        {
            get => _zoomLevels;
            set => RaiseAndSetIfChanged(ref _zoomLevels, value);
        }

        private int _oldZoom = 100;
        private int _zoom = 100;

        /// <summary>
        ///   Gets or sets the zoom.
        /// </summary>
        /// <value>The zoom.</value>
        public virtual int OldZoom
        {
            get => _oldZoom;
            set => RaiseAndSetIfChanged(ref _oldZoom, value);
        }

        /// <summary>
        ///   Gets or sets the zoom.
        /// </summary>
        /// <value>The zoom.</value>
        public virtual int Zoom
        {
            get => _zoom;
            set
            {
                var newZoom = value.Clamp(MinZoom, MaxZoom);

                if (_zoom == newZoom) return;
                var previousZoom = _zoom;
                _zoom = newZoom;
                UpdateViewPort();
                TriggerRender();

                OldZoom = previousZoom;
                RaisePropertyChanged(nameof(Zoom));
                //this.OnZoomChanged(EventArgs.Empty);
                //this.OnZoomed(new ImageBoxZoomEventArgs(actions, source, previousZoom, this.Zoom));
                //SetZoom(value, value > Zoom ? ImageZoomActions.ZoomIn : ImageZoomActions.ZoomOut);
            }
        }

        public virtual bool IsActualSize => Zoom == 100;

        private ISolidColorBrush _pixelGridColor = Brushes.DimGray;
        /// <summary>
        /// Gets or sets the color of the pixel grid.
        /// </summary>
        /// <value>The color of the pixel grid.</value>
        public virtual ISolidColorBrush PixelGridColor
        {
            get => _pixelGridColor;
            set => RaiseAndSetIfChanged(ref _pixelGridColor, value);
        }

        private int _pixelGridThreshold = 5;
        /// <summary>
        /// Gets or sets the minimum size of zoomed pixel's before the pixel grid will be drawn
        /// </summary>
        /// <value>The pixel grid threshold.</value>

        public virtual int PixelGridThreshold
        {
            get => _pixelGridThreshold;
            set => RaiseAndSetIfChanged(ref _pixelGridThreshold, value);
        }

        public SelectionModes SelectionMode
        {
            get => _selectionMode;
            set => RaiseAndSetIfChanged(ref _selectionMode, value);
        }

        public ISolidColorBrush SelectionColor
        {
            get => _selectionColor;
            set => RaiseAndSetIfChanged(ref _selectionColor, value);
        }

        public Rect SelectionRegion
        {
            get => _selectionRegion;
            set
            {
                if(!RaiseAndSetIfChanged(ref _selectionRegion, value)) return;
                TriggerRender();
                RaisePropertyChanged(nameof(HaveSelection));
            }
        }

        public bool HaveSelection => !SelectionRegion.IsEmpty;


        //Our render target we compile everything to and present to the user
        private Point _startMousePosition;
        private Vector _startScrollPosition;
        private bool _isPanning;
        private bool _isSelecting;
        private Bitmap _image;
        private byte _gridCellSize;
        private ISolidColorBrush _gridColor = Brushes.Gainsboro;
        private ISolidColorBrush _gridColorAlternate = Brushes.White;
        private bool _showGrid = true;
        private bool _autoPan = true;
        private MouseButtons _panWithMouseButtons = MouseButtons.LeftButton | MouseButtons.MiddleButton | MouseButtons.RightButton;
        private bool _panWithArrows = true;
        private MouseButtons _selectWithMouseButtons = MouseButtons.LeftButton | MouseButtons.RightButton;
        private bool _invertMouse = false;
        private bool _autoCenter = true;
        private SizeModes _sizeMode = SizeModes.Normal;
        private ISolidColorBrush _selectionColor = new SolidColorBrush(new Color(127, 0, 128, 255));
        private Rect _selectionRegion = Rect.Empty;
        private SelectionModes _selectionMode = SelectionModes.None;
        private bool _canRender = true;


        public ContentControl FillContainer { get; } = new ContentControl
        {
            Background = Brushes.Transparent
        };

        public ContentControl SizedContainer { get; private set; } = new ContentControl
        {
            
        };

        Type IStyleable.StyleKey => typeof(ScrollViewer);

        public AdvancedImageBox()
        {
            Content = FillContainer;
            FillContainer.Content = SizedContainer;
            FillContainer.PointerWheelChanged += FillContainerOnPointerWheelChanged;
            
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            //Container.PointerMoved += ScrollViewerOnPointerMoved;
            //Container.PointerPressed += ScrollViewerOnPointerPressed;
            //Container.PointerReleased += ScrollViewerOnPointerReleased;
        }

        protected override void OnScrollChanged(ScrollChangedEventArgs e)
        {
            Debug.WriteLine($"ViewportDelta: {e.ViewportDelta} | OffsetDelta: {e.OffsetDelta} | ExtentDelta: {e.ExtentDelta}");
            if (!e.ViewportDelta.IsDefault)
            {
                UpdateViewPort();
            }

            TriggerRender();

            base.OnScrollChanged(e);
        }

        private void FillContainerOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            Debug.WriteLine("mouse whell");
            e.Handled = true;
            if (Image is null) return;
            if (AllowZoom && SizeMode == SizeModes.Normal)
            {
                // The MouseWheel event can contain multiple "spins" of the wheel so we need to adjust accordingly
                //double spins = Math.Abs(e.Delta.Y);
                //Debug.WriteLine(e.GetPosition(this));
                // TODO: Really should update the source method to handle multiple increments rather than calling it multiple times
                /*for (int i = 0; i < spins; i++)
                {*/
                ProcessMouseZoom(e.Delta.Y > 0, e.GetPosition(this));
                //}
            }
        }

        public void TriggerRender()
        {
            if (!_canRender) return;
            InvalidateVisual();
        }

        private void ProcessMouseZoom(bool isZoomIn, Point cursorPosition)
         =>   PerformZoom(isZoomIn ? ZoomActions.ZoomIn : ZoomActions.ZoomOut, true, cursorPosition);

        /// <summary>
        /// Returns an appropriate zoom level based on the specified action, relative to the current zoom level.
        /// </summary>
        /// <param name="action">The action to determine the zoom level.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if an unsupported action is specified.</exception>
        private int GetZoomLevel(ZoomActions action)
        {
            int result;

            switch (action)
            {
                case ZoomActions.None:
                    result = Zoom;
                    break;
                case ZoomActions.ZoomIn:
                    result = ZoomLevels.NextZoom(Zoom);
                    break;
                case ZoomActions.ZoomOut:
                    result = ZoomLevels.PreviousZoom(Zoom);
                    break;
                case ZoomActions.ActualSize:
                    result = 100;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }

            return result;
        }

        /// <summary>
        /// Resets the <see cref="SizeModes"/> property whilsts retaining the original <see cref="Zoom"/>.
        /// </summary>
        protected void RestoreSizeMode()
        {
            if (SizeMode != SizeModes.Normal)
            {
                var previousZoom = Zoom;
                SizeMode = SizeModes.Normal;
                Zoom = previousZoom; // Stop the zoom getting reset to 100% before calculating the new zoom
            }
        }

        private void PerformZoom(ZoomActions action, bool preservePosition) 
            => PerformZoom(action, preservePosition, CenterPoint);

        private void PerformZoom(ZoomActions action, bool preservePosition, Point relativePoint)
        {
            Point currentPixel = PointToImage(relativePoint);
            int currentZoom = Zoom;
            int newZoom = GetZoomLevel(action);

            if (preservePosition && Zoom != currentZoom)
                CanRender = false;

            RestoreSizeMode();
            Zoom = newZoom;

            if (preservePosition && Zoom != currentZoom)
            {
                ScrollTo(currentPixel, relativePoint);
            }
        }

        /// <summary>
        ///   Determines whether the specified point is located within the image view port
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>
        ///   <c>true</c> if the specified point is located within the image view port; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsPointInImage(Point point)
            => GetImageViewPort().Contains(point);

        /// <summary>
        ///   Determines whether the specified point is located within the image view port
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to check.</param>
        /// <param name="y">The Y co-ordinate of the point to check.</param>
        /// <returns>
        ///   <c>true</c> if the specified point is located within the image view port; otherwise, <c>false</c>.
        /// </returns>
        public bool IsPointInImage(int x, int y)
            => IsPointInImage(new Point(x, y));

        /// <summary>
        ///   Determines whether the specified point is located within the image view port
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to check.</param>
        /// <param name="y">The Y co-ordinate of the point to check.</param>
        /// <returns>
        ///   <c>true</c> if the specified point is located within the image view port; otherwise, <c>false</c>.
        /// </returns>
        public bool IsPointInImage(double x, double y)
            => IsPointInImage(new Point(x, y));

        /// <summary>
        ///   Converts the given client size point to represent a coordinate on the source image.
        /// </summary>
        /// <param name="point">The source point.</param>
        /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
        public Point PointToImage(Point point)
            => PointToImage(point, false);

        /// <summary>
        ///   Converts the given client size point to represent a coordinate on the source image.
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to convert.</param>
        /// <param name="y">The Y co-ordinate of the point to convert.</param>
        /// <param name="fitToBounds">
        ///   if set to <c>true</c> and the point is outside the bounds of the source image, it will be mapped to the nearest edge.
        /// </param>
        /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
        public Point PointToImage(double x, double y, bool fitToBounds = false)
            => PointToImage(x, y, fitToBounds);

        /// <summary>
        ///   Converts the given client size point to represent a coordinate on the source image.
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to convert.</param>
        /// <param name="y">The Y co-ordinate of the point to convert.</param>
        /// <param name="fitToBounds">
        ///   if set to <c>true</c> and the point is outside the bounds of the source image, it will be mapped to the nearest edge.
        /// </param>
        /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
        public Point PointToImage(int x, int y, bool fitToBounds = false)
        {
            return PointToImage(x, y, fitToBounds);
        }

        /// <summary>
        ///   Converts the given client size point to represent a coordinate on the source image.
        /// </summary>
        /// <param name="point">The source point.</param>
        /// <param name="fitToBounds">
        ///   if set to <c>true</c> and the point is outside the bounds of the source image, it will be mapped to the nearest edge.
        /// </param>
        /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
        public virtual Point PointToImage(Point point, bool fitToBounds)
        {
            double x;
            double y;

            var viewport = GetImageViewPort();

            if (!fitToBounds || viewport.Contains(point))
            {
                x = ((point.X + Offset.X - viewport.X) / ZoomFactor);
                y = ((point.Y + Offset.Y - viewport.Y) / ZoomFactor);
                
                if (fitToBounds)
                {
                    x = x.Clamp(0, Image.Size.Width);
                    y = y.Clamp(0, Image.Size.Height);
                }
            }
            else
            {
                x = 0; // Return Point.Empty if we couldn't match
                y = 0;
            }

            return new Point(x, y);
        }

        /// <summary>
        ///   Scrolls the control to the given point in the image, offset at the specified display point
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to scroll to.</param>
        /// <param name="y">The Y co-ordinate of the point to scroll to.</param>
        /// <param name="relativeX">The X co-ordinate relative to the <c>x</c> parameter.</param>
        /// <param name="relativeY">The Y co-ordinate relative to the <c>y</c> parameter.</param>
        public void ScrollTo(double x, double y, double relativeX, double relativeY)
            => ScrollTo(new Point(x, y), new Point(relativeX, relativeY));

        /// <summary>
        ///   Scrolls the control to the given point in the image, offset at the specified display point
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to scroll to.</param>
        /// <param name="y">The Y co-ordinate of the point to scroll to.</param>
        /// <param name="relativeX">The X co-ordinate relative to the <c>x</c> parameter.</param>
        /// <param name="relativeY">The Y co-ordinate relative to the <c>y</c> parameter.</param>
        public void ScrollTo(int x, int y, int relativeX, int relativeY)
            => ScrollTo(new Point(x, y), new Point(relativeX, relativeY));

        /// <summary>
        ///   Scrolls the control to the given point in the image, offset at the specified display point
        /// </summary>
        /// <param name="imageLocation">The point of the image to attempt to scroll to.</param>
        /// <param name="relativeDisplayPoint">The relative display point to offset scrolling by.</param>
        public virtual void ScrollTo(Point imageLocation, Point relativeDisplayPoint)
        {
            CanRender = false;
            var x = imageLocation.X * ZoomFactor - relativeDisplayPoint.X;
            var y = imageLocation.Y * ZoomFactor - relativeDisplayPoint.Y;

            //Offset = new Vector(x, y);

            _canRender = true;

            DispatcherTimer.RunOnce(() =>
            {
                // TODO: Remove this delay?
                //Debug.WriteLine($"1ms delayed viewport: {Viewport}");
                //CenterAt(new Point(cx, cy));

                Offset = new Vector(x, y);
            }, TimeSpan.FromTicks(1), DispatcherPriority.MaxValue);


            /*Timer timer = new Timer(0.1)
            {
                AutoReset = false,
            };
            timer.Elapsed += (sender, args) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Offset = new Vector(x, y);
                    CanRender = true;
                    timer.Dispose();
                });
            };
            timer.Start();*/


            /*Debug.WriteLine(
                $"X/Y: {x},{y} | \n" +
                $"Offset: {Offset} | \n" +
                $"ZoomFactor: {ZoomFactor} | \n" +
                $"Image Location: {imageLocation}\n" +
                $"MAX: {HorizontalScrollBarMaximum},{VerticalScrollBarMaximum} \n" +
                $"ViewPort: {Viewport.Width},{Viewport.Height} \n" +
                $"Container: {SizedContainer.Width},{SizedContainer.Height} \n" +
                $"Relative: {relativeDisplayPoint}");*/
        }

        /// <summary>
        ///   Zooms into the image
        /// </summary>
        public virtual void ZoomIn()
            => ZoomIn(true);

        /// <summary>
        ///   Zooms into the image
        /// </summary>
        /// <param name="preservePosition"><c>true</c> if the current scrolling position should be preserved relative to the new zoom level, <c>false</c> to reset.</param>
        public virtual void ZoomIn(bool preservePosition)
        {
            PerformZoom(ZoomActions.ZoomIn, preservePosition);
        }

        /// <summary>
        ///   Zooms out of the image
        /// </summary>
        public virtual void ZoomOut()
         => ZoomOut(true);

        /// <summary>
        ///   Zooms out of the image
        /// </summary>
        /// <param name="preservePosition"><c>true</c> if the current scrolling position should be preserved relative to the new zoom level, <c>false</c> to reset.</param>
        public virtual void ZoomOut(bool preservePosition)
        {
           PerformZoom(ZoomActions.ZoomOut, preservePosition);
        }

        /// <summary>
        ///   Zooms to the maximum size for displaying the entire image within the bounds of the control.
        /// </summary>
        public virtual void ZoomToFit()
        {
            if (Image is null) return;

            double zoom;
            double aspectRatio;

            if (Image.Size.Width > Image.Size.Height)
            {
                aspectRatio = Viewport.Width / Image.Size.Width;
                zoom = aspectRatio * 100.0;

                if (Viewport.Height < Image.Size.Height * zoom / 100.0)
                {
                    aspectRatio = Viewport.Height / Image.Size.Height;
                    zoom = aspectRatio * 100.0;
                }
            }
            else
            {
                aspectRatio = Viewport.Height / Image.Size.Height;
                zoom = aspectRatio * 100.0;

                if (Viewport.Width < Image.Size.Width * zoom / 100.0)
                {
                    aspectRatio = Viewport.Width / Image.Size.Width;
                    zoom = aspectRatio * 100.0;
                }
            }

            Zoom = (int)Math.Round(Math.Floor(zoom));
        }

        /// <summary>
        ///   Adjusts the view port to fit the given region
        /// </summary>
        /// <param name="x">The X co-ordinate of the selection region.</param>
        /// <param name="y">The Y co-ordinate of the selection region.</param>
        /// <param name="width">The width of the selection region.</param>
        /// <param name="height">The height of the selection region.</param>
        public void ZoomToRegion(double x, double y, double width, double height)
        {
            ZoomToRegion(new Rect(x, y, width, height));
        }

        /// <summary>
        ///   Adjusts the view port to fit the given region
        /// </summary>
        /// <param name="x">The X co-ordinate of the selection region.</param>
        /// <param name="y">The Y co-ordinate of the selection region.</param>
        /// <param name="width">The width of the selection region.</param>
        /// <param name="height">The height of the selection region.</param>
        public void ZoomToRegion(int x, int y, int width, int height)
        {
            ZoomToRegion(new Rect(x, y, width, height));
        }

        /// <summary>
        ///   Adjusts the view port to fit the given region
        /// </summary>
        /// <param name="rectangle">The rectangle to fit the view port to.</param>
        public virtual void ZoomToRegion(Rectangle rectangle) => ZoomToRegion(rectangle.ToAvalonia());

        /// <summary>
        ///   Adjusts the view port to fit the given region
        /// </summary>
        /// <param name="rectangle">The rectangle to fit the view port to.</param>
        public virtual void ZoomToRegion(Rect rectangle)
        {
            var ratioX = Viewport.Width / rectangle.Width;
            var ratioY = Viewport.Height / rectangle.Height;
            var zoomFactor = Math.Min(ratioX, ratioY);
            var cx = rectangle.X + rectangle.Width / 2;
            var cy = rectangle.Y + rectangle.Height / 2;

            CanRender = false;
            Zoom = (int) (zoomFactor * 100); // This function sets the zoom so viewport will change
            CenterAt(new Point(cx, cy)); // If i call this here, it will move to the wrong position due wrong viewport
        }

        /// <summary>
        /// Zooms to current selection region
        /// </summary>
        public void ZoomToSelectionRegion()
        {
            if (!HaveSelection) return;
            ZoomToRegion(SelectionRegion);
        }

        /// <summary>
        ///   Centers the given point in the image in the center of the control
        /// </summary>
        /// <param name="imageLocation">The point of the image to attempt to center.</param>
        public virtual void CenterAt(System.Drawing.Point imageLocation)
            => ScrollTo(new Point(imageLocation.X, imageLocation.Y), new Point(Viewport.Width / 2, Viewport.Height / 2));

        /// <summary>
        ///   Centers the given point in the image in the center of the control
        /// </summary>
        /// <param name="imageLocation">The point of the image to attempt to center.</param>
        public virtual void CenterAt(Point imageLocation)
         => ScrollTo(imageLocation, new Point(Viewport.Width / 2, Viewport.Height / 2));

        /// <summary>
        ///   Centers the given point in the image in the center of the control
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to center.</param>
        /// <param name="y">The Y co-ordinate of the point to center.</param>
        public void CenterAt(int x, int y)
         => CenterAt(new Point(x, y));

        /// <summary>
        ///   Centers the given point in the image in the center of the control
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to center.</param>
        /// <param name="y">The Y co-ordinate of the point to center.</param>
        public void CenterAt(double x, double y)
         => CenterAt(new Point(x, y));

        /// <summary>
        /// Resets the viewport to show the center of the image.
        /// </summary>
        public virtual void CenterToImage()
        {
            Offset = new Vector(HorizontalScrollBarMaximum / 2, VerticalScrollBarMaximum / 2);
        }

        private bool UpdateViewPort()
        {
            if (Image is null)
            {
                SizedContainer.Width = 0;
                SizedContainer.Height = 0;
                return true;
            }

            var scaledImageWidth = ScaledImageWidth;
            var scaledImageHeight = ScaledImageHeight;
            var width = scaledImageWidth <= Viewport.Width ? Viewport.Width : scaledImageWidth;
            var height = scaledImageHeight <= Viewport.Height ? Viewport.Height : scaledImageHeight;

            

            bool changed = false;
            if (SizedContainer.Width != width)
            {
                SizedContainer.Width = width;
                changed = true;
            }

            if (SizedContainer.Height != height)
            {
                SizedContainer.Height = height;
                changed = true;
            }

            /*if (changed)
            {
                var newContainer = new ContentControl
                {
                    Width = width,
                    Height = height
                };
                FillContainer.Content = SizedContainer = newContainer;
                Debug.WriteLine($"Updated ViewPort: {DateTime.Now.Ticks}");
                //TriggerRender();
            }*/

            return changed;
        }

        /// <summary>
        /// Resets the zoom to 100%.
        /// </summary>
        /// <param name="source">The source that initiated the action.</param>
        private void PerformActualSize()
        {
            SizeMode = SizeModes.Normal;
            //SetZoom(100, ImageZoomActions.ActualSize | (Zoom < 100 ? ImageZoomActions.ZoomIn : ImageZoomActions.ZoomOut));
            Zoom = 100;
        }

        #region Overrides


        #endregion

        #region Methods

        #region Selection

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.Point" /> scaled according to the current zoom level
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to scale.</param>
        /// <param name="y">The Y co-ordinate of the point to scale.</param>
        /// <returns>A <see cref="Point"/> which has been scaled to match the current zoom level</returns>
        public Point GetScaledPoint(int x, int y)
        {
          return GetScaledPoint(new Point(x, y));
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.Point" /> scaled according to the current zoom level
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to scale.</param>
        /// <param name="y">The Y co-ordinate of the point to scale.</param>
        /// <returns>A <see cref="Point"/> which has been scaled to match the current zoom level</returns>
        public PointF GetScaledPoint(float x, float y)
        {
          return GetScaledPoint(new PointF(x, y));
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.Point" /> scaled according to the current zoom level
        /// </summary>
        /// <param name="source">The source <see cref="Point"/> to scale.</param>
        /// <returns>A <see cref="Point"/> which has been scaled to match the current zoom level</returns>
        public virtual Point GetScaledPoint(Point source)
        {
          return new Point(source.X * ZoomFactor, source.Y * ZoomFactor);
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.PointF" /> scaled according to the current zoom level
        /// </summary>
        /// <param name="source">The source <see cref="PointF"/> to scale.</param>
        /// <returns>A <see cref="PointF"/> which has been scaled to match the current zoom level</returns>
        public virtual PointF GetScaledPoint(PointF source)
        {
          return new PointF((float)(source.X * this.ZoomFactor), (float)(source.Y * this.ZoomFactor));
        }

        /// <summary>
        ///   Returns the source rectangle scaled according to the current zoom level
        /// </summary>
        /// <param name="x">The X co-ordinate of the source rectangle.</param>
        /// <param name="y">The Y co-ordinate of the source rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <returns>A <see cref="Rectangle"/> which has been scaled to match the current zoom level</returns>
        public Rect GetScaledRectangle(int x, int y, int width, int height)
        {
          return GetScaledRectangle(new Rect(x, y, width, height));
        }

        /// <summary>
        ///   Returns the source rectangle scaled according to the current zoom level
        /// </summary>
        /// <param name="x">The X co-ordinate of the source rectangle.</param>
        /// <param name="y">The Y co-ordinate of the source rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <returns>A <see cref="RectangleF"/> which has been scaled to match the current zoom level</returns>
        public RectangleF GetScaledRectangle(float x, float y, float width, float height)
        {
          return GetScaledRectangle(new RectangleF(x, y, width, height));
        }

        /// <summary>
        ///   Returns the source rectangle scaled according to the current zoom level
        /// </summary>
        /// <param name="location">The location of the source rectangle.</param>
        /// <param name="size">The size of the source rectangle.</param>
        /// <returns>A <see cref="Rectangle"/> which has been scaled to match the current zoom level</returns>
        public Rect GetScaledRectangle(Point location, Size size)
        {
          return GetScaledRectangle(new Rect(location, size));
        }

        /// <summary>
        ///   Returns the source rectangle scaled according to the current zoom level
        /// </summary>
        /// <param name="location">The location of the source rectangle.</param>
        /// <param name="size">The size of the source rectangle.</param>
        /// <returns>A <see cref="Rectangle"/> which has been scaled to match the current zoom level</returns>
        public RectangleF GetScaledRectangle(PointF location, SizeF size)
        {
          return GetScaledRectangle(new RectangleF(location, size));
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.Rectangle" /> scaled according to the current zoom level
        /// </summary>
        /// <param name="source">The source <see cref="Rectangle"/> to scale.</param>
        /// <returns>A <see cref="Rectangle"/> which has been scaled to match the current zoom level</returns>
        public virtual Rect GetScaledRectangle(Rect source)
        {
          return new Rect(source.Left * ZoomFactor, source.Top * ZoomFactor, source.Width * ZoomFactor, source.Height * ZoomFactor);
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.RectangleF" /> scaled according to the current zoom level
        /// </summary>
        /// <param name="source">The source <see cref="RectangleF"/> to scale.</param>
        /// <returns>A <see cref="RectangleF"/> which has been scaled to match the current zoom level</returns>
        public virtual RectangleF GetScaledRectangle(RectangleF source)
        {
          return new RectangleF((float)(source.Left * ZoomFactor), (float)(source.Top * ZoomFactor), (float)(source.Width * ZoomFactor), (float)(source.Height * ZoomFactor));
        }

        /// <summary>
        ///   Returns the source size scaled according to the current zoom level
        /// </summary>
        /// <param name="width">The width of the size to scale.</param>
        /// <param name="height">The height of the size to scale.</param>
        /// <returns>A <see cref="SizeF"/> which has been resized to match the current zoom level</returns>
        public SizeF GetScaledSize(float width, float height)
        {
          return this.GetScaledSize(new SizeF(width, height));
        }

        /// <summary>
        ///   Returns the source size scaled according to the current zoom level
        /// </summary>
        /// <param name="width">The width of the size to scale.</param>
        /// <param name="height">The height of the size to scale.</param>
        /// <returns>A <see cref="Size"/> which has been resized to match the current zoom level</returns>
        public Size GetScaledSize(int width, int height)
        {
          return this.GetScaledSize(new Size(width, height));
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.SizeF" /> scaled according to the current zoom level
        /// </summary>
        /// <param name="source">The source <see cref="SizeF"/> to scale.</param>
        /// <returns>A <see cref="SizeF"/> which has been resized to match the current zoom level</returns>
        public virtual SizeF GetScaledSize(SizeF source)
        {
          return new SizeF((float)(source.Width * this.ZoomFactor), (float)(source.Height * this.ZoomFactor));
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.Size" /> scaled according to the current zoom level
        /// </summary>
        /// <param name="source">The source <see cref="Size"/> to scale.</param>
        /// <returns>A <see cref="Size"/> which has been resized to match the current zoom level</returns>
        public virtual Size GetScaledSize(Size source)
        {
          return new Size(source.Width * ZoomFactor, source.Height * ZoomFactor);
        }

        /// <summary>
        ///   Creates a selection region which encompasses the entire image
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown if no image is currently set</exception>
        public virtual void SelectAll()
        {
            if (Image is null) return;
            SelectionRegion = new Rect(0, 0, Image.Size.Width, Image.Size.Height);
        }

        /// <summary>
        ///   Clears any existing selection region
        /// </summary>
        public virtual void SelectNone()
        {
            SelectionRegion = Rect.Empty;
        }

        #endregion

        public void LoadImage(string path)
        {
            Image = new Bitmap(path);
        }

        public override void Render(DrawingContext context)
        {
            Debug.WriteLine($"Render: {DateTime.Now.Ticks}");
            //   base.Render(context);

            // Draw Grid
            if (ShowGrid)
            {
                // draw the background
                var currentColor = GridColor;
                for (int y = 0; y < Viewport.Height; y += GridCellSize)
                {
                    var firstRowColor = currentColor;
                    for (int x = 0; x < Viewport.Width; x += GridCellSize)
                    {
                        context.FillRectangle(currentColor, new Rect(x, y, GridCellSize, GridCellSize));
                        currentColor = ReferenceEquals(currentColor, GridColor) ? GridColorAlternate : GridColor;
                    }

                    if (firstRowColor == currentColor)
                        currentColor = ReferenceEquals(currentColor, GridColor) ? GridColorAlternate : GridColor;
                }

            }
            else
            {
                context.FillRectangle(Background, new Rect(0, 0, Viewport.Width, Viewport.Height));
            }

            if (Image is null) return;
            // Draw iamge
            context.DrawImage(Image,
                GetSourceImageRegion(),
                GetImageViewPort()
            );
            //SkiaContext.SkCanvas.dr
            // Draw pixel grid
            var pixelSize = ZoomFactor;
            if (pixelSize > PixelGridThreshold)
            {
                var viewport = GetImageViewPort();
                var offsetX = Offset.X % pixelSize;
                var offsetY = Offset.Y % pixelSize;

                Pen pen = new Pen(PixelGridColor);
                for (double x = viewport.X + pixelSize - offsetX; x < viewport.Right; x += pixelSize)
                {
                    context.DrawLine(pen, new Avalonia.Point(x, viewport.X), new Avalonia.Point(x, viewport.Bottom));
                }

                for (double y = viewport.Y + pixelSize - offsetY; y < viewport.Bottom; y += pixelSize)
                {
                    context.DrawLine(pen, new Avalonia.Point(viewport.Y, y), new Avalonia.Point(viewport.Right, y));
                }

                context.DrawRectangle(pen, viewport);
            }

            if (!SelectionRegion.IsEmpty)
            {
                var rect = GetOffsetRectangle(SelectionRegion);
                context.FillRectangle(SelectionColor, rect);
                Color solidColor = Color.FromArgb(255, SelectionColor.Color.R, SelectionColor.Color.G, SelectionColor.Color.B);
                context.DrawRectangle(new Pen(solidColor.ToUint32()), rect);
            }
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.Point" /> repositioned to include the current image offset and scaled by the current zoom level
        /// </summary>
        /// <param name="source">The source <see cref="Point"/> to offset.</param>
        /// <returns>A <see cref="Point"/> which has been repositioned to match the current zoom level and image offset</returns>
        public virtual Point GetOffsetPoint(System.Drawing.Point source)
        {
            var offset = GetOffsetPoint(new Point(source.X, source.Y));

            return new Point((int)offset.X, (int)offset.Y);
        }

        /// <summary>
        ///   Returns the source co-ordinates repositioned to include the current image offset and scaled by the current zoom level
        /// </summary>
        /// <param name="x">The source X co-ordinate.</param>
        /// <param name="y">The source Y co-ordinate.</param>
        /// <returns>A <see cref="Point"/> which has been repositioned to match the current zoom level and image offset</returns>
        public Point GetOffsetPoint(int x, int y)
        {
            return GetOffsetPoint(new System.Drawing.Point(x, y));
        }

        /// <summary>
        ///   Returns the source co-ordinates repositioned to include the current image offset and scaled by the current zoom level
        /// </summary>
        /// <param name="x">The source X co-ordinate.</param>
        /// <param name="y">The source Y co-ordinate.</param>
        /// <returns>A <see cref="Point"/> which has been repositioned to match the current zoom level and image offset</returns>
        public Point GetOffsetPoint(double x, double y)
        {
            return GetOffsetPoint(new Point(x, y));
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.PointF" /> repositioned to include the current image offset and scaled by the current zoom level
        /// </summary>
        /// <param name="source">The source <see cref="PointF"/> to offset.</param>
        /// <returns>A <see cref="PointF"/> which has been repositioned to match the current zoom level and image offset</returns>
        public virtual Point GetOffsetPoint(Point source)
        {
            Rect viewport = GetImageViewPort();
            var scaled = GetScaledPoint(source);
            var offsetX = viewport.Left + Offset.X;
            var offsetY = viewport.Top + Offset.Y;

            return new Point(scaled.X + offsetX, scaled.Y + offsetY);
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.RectangleF" /> scaled according to the current zoom level and repositioned to include the current image offset
        /// </summary>
        /// <param name="source">The source <see cref="RectangleF"/> to offset.</param>
        /// <returns>A <see cref="RectangleF"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
        public virtual Rect GetOffsetRectangle(Rect source)
        {
            var viewport = GetImageViewPort();
            var scaled = GetScaledRectangle(source);
            var offsetX = viewport.Left - Offset.X;
            var offsetY = viewport.Top - Offset.Y;

            return new Rect(new Point(scaled.Left + offsetX, scaled.Top + offsetY), scaled.Size);
        }

        /// <summary>
        ///   Returns the source rectangle scaled according to the current zoom level and repositioned to include the current image offset
        /// </summary>
        /// <param name="x">The X co-ordinate of the source rectangle.</param>
        /// <param name="y">The Y co-ordinate of the source rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <returns>A <see cref="Rectangle"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
        public Rectangle GetOffsetRectangle(int x, int y, int width, int height)
        {
            return this.GetOffsetRectangle(new Rectangle(x, y, width, height));
        }

        /// <summary>
        ///   Returns the source rectangle scaled according to the current zoom level and repositioned to include the current image offset
        /// </summary>
        /// <param name="x">The X co-ordinate of the source rectangle.</param>
        /// <param name="y">The Y co-ordinate of the source rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <returns>A <see cref="RectangleF"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
        public Rect GetOffsetRectangle(double x, double y, double width, double height)
        {
            return GetOffsetRectangle(new Rect(x, y, width, height));
        }

        /// <summary>
        ///   Returns the source <see cref="T:System.Drawing.Rectangle" /> scaled according to the current zoom level and repositioned to include the current image offset
        /// </summary>
        /// <param name="source">The source <see cref="Rectangle"/> to offset.</param>
        /// <returns>A <see cref="Rectangle"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
        public virtual Rectangle GetOffsetRectangle(Rectangle source)
        {
            var viewport = GetImageViewPort();
            var scaled = GetScaledRectangle(source);
            var offsetX = viewport.Left + Offset.X;
            var offsetY = viewport.Top + Offset.Y;

            return new Rectangle(new System.Drawing.Point((int) (scaled.Left + offsetX), (int) (scaled.Top + offsetY)), 
                new System.Drawing.Size((int) scaled.Size.Width, (int) scaled.Size.Height));
        }

        /// <summary>
        ///   Fits a given <see cref="T:System.Drawing.Rectangle" /> to match image boundaries
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        /// <returns>
        ///   A <see cref="T:System.Drawing.Rectangle" /> structure remapped to fit the image boundaries
        /// </returns>
        public Rectangle FitRectangle(Rectangle rectangle)
       {
           if (Image is null) return Rectangle.Empty;
            var x = rectangle.X;
           var y = rectangle.Y;
           var w = rectangle.Width;
           var h = rectangle.Height;

           if (x < 0)
           {
               x = 0;
           }

           if (y < 0)
           {
               y = 0;
           }

           if (x + w > Image.Size.Width)
           {
               w = (int) (Image.Size.Width - x);
           }

           if (y + h > Image.Size.Height)
           {
               h = (int) (Image.Size.Height - y);
           }

           return new Rectangle(x, y, w, h);
       }

       /// <summary>
       ///   Fits a given <see cref="T:System.Drawing.RectangleF" /> to match image boundaries
       /// </summary>
       /// <param name="rectangle">The rectangle.</param>
       /// <returns>
       ///   A <see cref="T:System.Drawing.RectangleF" /> structure remapped to fit the image boundaries
       /// </returns>
       public Rect FitRectangle(Rect rectangle)
       {
           if(Image is null) return Rect.Empty;
           var x = rectangle.X;
           var y = rectangle.Y;
           var w = rectangle.Width;
           var h = rectangle.Height;

           if (x < 0)
           {
               w -= -x;
               x = 0;
           }

           if (y < 0)
           {
               h -= -y;
               y = 0;
           }

           if (x + w > Image.Size.Width)
           {
               w = Image.Size.Width - x;
           }

           if (y + h > Image.Size.Height)
           {
               h = Image.Size.Height - y;
           }

           return new Rect(x, y, w, h);
       }

        /// <summary>
        ///   Gets the source image region.
        /// </summary>
        /// <returns></returns>
        public virtual Rect GetSourceImageRegion()
       {
           if (Image is null) return Rect.Empty;

           if (SizeMode != SizeModes.Stretch)
           {
               var viewPort = GetImageViewPort();
               double sourceLeft = (Offset.X / ZoomFactor);
               double sourceTop = (Offset.Y / ZoomFactor);
               double sourceWidth = (viewPort.Width / ZoomFactor);
               double sourceHeight = (viewPort.Height / ZoomFactor);

                return new Rect(sourceLeft, sourceTop, sourceWidth, sourceHeight);
           }

           return new Rect(0, 0, Image.Size.Width, Image.Size.Height);
       }

        /// <summary>
        /// Gets the image view port.
        /// </summary>
        /// <returns></returns>
        public virtual Rect GetImageViewPort()
        {
            if (Viewport.Width == 0 && Viewport.Height == 0) return Rect.Empty;

            double xOffset = 0;
            double yOffset = 0;
            double width;
            double height;

            if (SizeMode != SizeModes.Stretch)
            {
                if (AutoCenter)
                {
                    xOffset = (!IsHorizontalBarVisible ? (Viewport.Width - ScaledImageWidth) / 2 : 0);
                    yOffset = (!IsVerticalBarVisible ? (Viewport.Height - ScaledImageHeight) / 2 : 0);
                }

                width = Math.Min(ScaledImageWidth - Math.Abs(Offset.X), Viewport.Width);
                height = Math.Min(ScaledImageHeight - Math.Abs(Offset.Y), Viewport.Height);
            }
            else
            {
                width = Viewport.Width;
                height = Viewport.Height;
            }

            return new Rect(xOffset, yOffset, width, height);
        }

        /// <summary>
        ///   Gets the width of the scaled image.
        /// </summary>
        /// <value>The width of the scaled image.</value>
        protected virtual double ScaledImageWidth => Image.Size.Width * ZoomFactor;

        /// <summary>
        ///   Gets the height of the scaled image.
        /// </summary>
        /// <value>The height of the scaled image.</value>
        protected virtual double ScaledImageHeight => Image.Size.Height * ZoomFactor;

        public double ZoomFactor => _zoom / 100.0;

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (e.Handled
                || IsPanning
                || IsSelecting
                || Image is null) return;

            var pointer = e.GetCurrentPoint(this);

            if (SelectionMode != SelectionModes.None)
            {
                if (!(
                        pointer.Properties.IsLeftButtonPressed && (SelectWithMouseButtons & MouseButtons.LeftButton) != 0 ||
                        pointer.Properties.IsMiddleButtonPressed && (SelectWithMouseButtons & MouseButtons.MiddleButton) != 0 ||
                        pointer.Properties.IsRightButtonPressed && (SelectWithMouseButtons & MouseButtons.RightButton) != 0
                    )
                ) return;
                IsSelecting = true;
            }
            else
            {
                if (!(
                        pointer.Properties.IsLeftButtonPressed && (PanWithMouseButtons & MouseButtons.LeftButton) != 0 ||
                        pointer.Properties.IsMiddleButtonPressed && (PanWithMouseButtons & MouseButtons.MiddleButton) != 0 ||
                        pointer.Properties.IsRightButtonPressed && (PanWithMouseButtons & MouseButtons.RightButton) != 0
                    )
                    || !AutoPan

                ) return;

                IsPanning = true;
            }
            
            var location = pointer.Position;
            
            if (location.X > Viewport.Width) return;
            if (location.Y > Viewport.Height) return;
            _startMousePosition = location;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (e.Handled) return;

            IsPanning = false;
            IsSelecting = false;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (e.Handled) return;

            if (!IsPanning && !IsSelecting) return;
            var pointer = e.GetCurrentPoint(this);
            var location = pointer.Position;


            if (IsPanning)
            {
                double x;
                double y;

                if (!InvertMouse)
                {
                    x = _startScrollPosition.X + (_startMousePosition.X - location.X);
                    y = _startScrollPosition.Y + (_startMousePosition.Y - location.Y);
                }
                else
                {
                    x = (_startScrollPosition.X - (_startMousePosition.X - location.X));
                    y = (_startScrollPosition.Y - (_startMousePosition.Y - location.Y));
                }

                Offset = new Vector(x, y);
            }
            else if (IsSelecting)
            {
                double x;
                double y;
                double w;
                double h;

                var imageOffset = GetImageViewPort().Position;

                if (location.X < _startMousePosition.X)
                {
                    x = location.X;
                    w = _startMousePosition.X - location.X;
                }
                else
                {
                    x = _startMousePosition.X;
                    w = location.X - _startMousePosition.X;
                }

                if (location.Y < _startMousePosition.Y)
                {
                    y = location.Y;
                    h = _startMousePosition.Y - location.Y;
                }
                else
                {
                    y = _startMousePosition.Y;
                    h = location.Y - _startMousePosition.Y;
                }

                x -= imageOffset.X - Offset.X;
                y -= imageOffset.Y - Offset.Y;

                x /= ZoomFactor;
                y /= ZoomFactor;
                w /= ZoomFactor;
                h /= ZoomFactor;

                if (w != 0 && h != 0)
                {
                    SelectionRegion = FitRectangle(new Rect(x, y, w, h));
                }
            }

            e.Handled = true;
        }
        #endregion
    }
}
