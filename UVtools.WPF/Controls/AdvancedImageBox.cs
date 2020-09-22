using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Skia;
using Avalonia.Styling;
using Avalonia.Visuals.Media.Imaging;
using SkiaSharp;
using UVtools.Core.Extensions;



namespace UVtools.WPF.Controls
{
    public class AdvancedImageBox : ScrollViewer, IStyleable
    {
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
                                         7, 10, 15, 20, 25, 30, 50, 70, 100, 150, 200, 300, 400, 500, 600, 700, 800, 1200, 1600
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
        public enum PanMouseButtons : byte
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
        public enum ImageZoomActions : byte
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

        #endregion

        #region Constants
        public static readonly int MinZoom = 10;
        public static readonly int MaxZoom = 3500;
        #endregion

        public static StyledProperty<byte> GridCellSizeProperty =
            AvaloniaProperty.Register<AdvancedImageBox, byte>(nameof(GridCellSize), 8, false, BindingMode.TwoWay);

        /// <summary>
        /// Gets or sets the basic cell size
        /// </summary>
        public byte GridCellSize
        {
            get => GetValue(GridCellSizeProperty);
            set => SetValue(GridCellSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the color used to create the checkerboard style background
        /// </summary>
        public ISolidColorBrush GridColor { get; set; } = Brushes.Gainsboro;

        /// <summary>
        /// Gets or sets the color used to create the checkerboard style background
        /// </summary>
        public ISolidColorBrush GridColorAlternate { get; set; } = Brushes.White;

        /// <summary>
        /// Gets or sets the image to be displayed
        /// </summary>
        public Bitmap Image
        {
            get => _image;
            set
            {
                _image = value;
                if (_image is null)
                {
                    SizedContainer.Width = 0;
                    SizedContainer.Height = 0;
                }
                else
                {
                    SizedContainer.Width = _image.Size.Width;
                    SizedContainer.Height = _image.Size.Height;
                }

                InvalidateVisual();
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
        public bool ShowGrid { get; set; } = true;

        public bool IsPanning
        {
            get => _isPanning;
            protected set
            {
                if (_isPanning == value) return;
                _isPanning = value;
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

        public Point CenterPoint
        {
            get
            {
                Rect viewport = GetImageViewPort();
                return new Point(viewport.Width / 2, viewport.Height / 2);
            }
        }

        public bool AutoPan { get; set; } = true;

        public PanMouseButtons PanWithMouseButtons { get; set; } = PanMouseButtons.LeftButton | PanMouseButtons.MiddleButton | PanMouseButtons.RightButton;

        public bool PanWithArrows { get; set; } = true;

        public bool InvertMouse { get; set; } = false;

        public bool AutoCenter { get; set; } = true;

        public SizeModes SizeMode { get; set; } = SizeModes.Normal;

        private bool _allowZoom = true;
        public virtual bool AllowZoom
        {
            get => _allowZoom;
            set
            {
                if (AllowZoom != value)
                {
                    _allowZoom = value;

                    //this.OnAllowZoomChanged(EventArgs.Empty);
                }
            }
        }

        ZoomLevelCollection _zoomLevels = ZoomLevelCollection.Default;
        /// <summary>
        ///   Gets or sets the zoom levels.
        /// </summary>
        /// <value>The zoom levels.</value>
        public virtual ZoomLevelCollection ZoomLevels
        {
            get => _zoomLevels;
            set
            {
                if (ZoomLevels != value)
                {
                    _zoomLevels = value;

                    //this.OnZoomLevelsChanged(EventArgs.Empty);
                }
            }
        }

        private int _zoom = 100;
        /// <summary>
        ///   Gets or sets the zoom.
        /// </summary>
        /// <value>The zoom.</value>
        public virtual int Zoom
        {
            get => _zoom;
            set => SetZoom(value, value > Zoom ? ImageZoomActions.ZoomIn : ImageZoomActions.ZoomOut);
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
            set
            {
                if (PixelGridColor != value)
                {
                    _pixelGridColor = value;

                    //this.OnPixelGridColorChanged(EventArgs.Empty);
                }
            }
        }

        private int _pixelGridThreshold = 5;
        /// <summary>
        /// Gets or sets the minimum size of zoomed pixel's before the pixel grid will be drawn
        /// </summary>
        /// <value>The pixel grid threshold.</value>

        public virtual int PixelGridThreshold
        {
            get => _pixelGridThreshold;
            set
            {
                if (PixelGridThreshold != value)
                {
                    _pixelGridThreshold = value;

                    //this.OnPixelGridThresholdChanged(EventArgs.Empty);
                }
            }
        }


        //Our render target we compile everything to and present to the user
        private RenderTargetBitmap RenderTarget;
        private ISkiaDrawingContextImpl SkiaContext;
        private Point _startMousePosition;
        private Vector _startScrollPosition;
        private bool _isPanning;
        private Bitmap _image;

        public ContentControl FillContainer { get; } = new ContentControl
        {
            Background = Brushes.Transparent
        };

        public ContentControl SizedContainer { get; } = new ContentControl
        {
            Background = Brushes.Transparent
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
            
            PropertyChanged += OnPropertyChanged;
        }

        private void FillContainerOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (Image is null) return;
            e.Handled = true;
            if (AllowZoom && SizeMode == SizeModes.Normal)
            {
                // The MouseWheel event can contain multiple "spins" of the wheel so we need to adjust accordingly
                double spins = Math.Abs(e.Delta.Y);
                //Debug.WriteLine(e.GetPosition(this));
                // TODO: Really should update the source method to handle multiple increments rather than calling it multiple times
                for (int i = 0; i < spins; i++)
                {
                   ProcessMouseZoom(e.Delta.Y > 0, e.GetPosition(this));
                }

                //InvalidateVisual();
            }
        }

        private void ProcessMouseZoom(bool isZoomIn, Point cursorPosition)
         =>   PerformZoom(isZoomIn ? ImageZoomActions.ZoomIn : ImageZoomActions.ZoomOut, true, cursorPosition);

        /// <summary>
        /// Returns an appropriate zoom level based on the specified action, relative to the current zoom level.
        /// </summary>
        /// <param name="action">The action to determine the zoom level.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if an unsupported action is specified.</exception>
        private int GetZoomLevel(ImageZoomActions action)
        {
            int result;

            switch (action)
            {
                case ImageZoomActions.None:
                    result = Zoom;
                    break;
                case ImageZoomActions.ZoomIn:
                    result = ZoomLevels.NextZoom(Zoom);
                    break;
                case ImageZoomActions.ZoomOut:
                    result = ZoomLevels.PreviousZoom(Zoom);
                    break;
                case ImageZoomActions.ActualSize:
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

        private void PerformZoom(ImageZoomActions action, bool preservePosition) 
            => PerformZoom(action, preservePosition, CenterPoint);

        private void PerformZoom(ImageZoomActions action, bool preservePosition, Point relativePoint)
        {
            Point currentPixel = PointToImage(relativePoint);
            int currentZoom = Zoom;
            int newZoom = GetZoomLevel(action);

            RestoreSizeMode();
            SetZoom(newZoom, action);

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
        /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
        public Point PointToImage(double x, double y)
            => PointToImage(x, y, false);

        /// <summary>
        ///   Converts the given client size point to represent a coordinate on the source image.
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to convert.</param>
        /// <param name="y">The Y co-ordinate of the point to convert.</param>
        /// <param name="fitToBounds">
        ///   if set to <c>true</c> and the point is outside the bounds of the source image, it will be mapped to the nearest edge.
        /// </param>
        /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
        public Point PointToImage(double x, double y, bool fitToBounds)
            => PointToImage(new Point(x, y), fitToBounds);

        /// <summary>
        ///   Converts the given client size point to represent a coordinate on the source image.
        /// </summary>
        /// <param name="x">The X co-ordinate of the point to convert.</param>
        /// <param name="y">The Y co-ordinate of the point to convert.</param>
        /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
        public Point PointToImage(int x, int y)
        {
            return PointToImage(x, y, false);
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
                x = (point.X + Offset.X - viewport.X) / ZoomFactor;
                y = (point.Y + Offset.Y - viewport.Y) / ZoomFactor;
                
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
            var x = imageLocation.X * ZoomFactor - relativeDisplayPoint.X;
            var y = imageLocation.Y * ZoomFactor - relativeDisplayPoint.Y;
            
            Offset = new Vector(x, y);
            Debug.WriteLine($"{Offset} | " +
                            $"{relativeDisplayPoint} | " +
                            $"{HorizontalScrollBarValue},{VerticalScrollBarValue}");
        }

        /// <summary>
        /// Updates the current zoom.
        /// </summary>
        /// <param name="value">The new zoom value.</param>
        /// <param name="actions">The zoom actions that caused the value to be updated.</param>
        /// <param name="source">The source of the zoom operation.</param>
        private void SetZoom(int value, ImageZoomActions actions)
        {
            var previousZoom = Zoom;
            value = value.Clamp(MinZoom, MaxZoom);
            
            if (_zoom != value)
            {
                _zoom = value;
                if (!UpdateViewPort())
                {
                    InvalidateArrange();
                }


                //this.OnZoomChanged(EventArgs.Empty);
                //this.OnZoomed(new ImageBoxZoomEventArgs(actions, source, previousZoom, this.Zoom));
            }
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
            //this.PerformZoomIn(ImageBoxActionSources.Unknown, preservePosition);
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
           // this.PerformZoomOut(ImageBoxActionSources.Unknown, preservePosition);
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
            Offset = new Point(HorizontalScrollBarMaximum / 2, VerticalScrollBarMaximum / 2);
        }

        private bool UpdateViewPort()
        {
            if (Image is null) return false;

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

            if (changed)
            {
                Debug.WriteLine($"Update ViewPort: {DateTime.Now.Ticks}");
                InvalidateArrange();
            }

            return changed;
        }

        /// <summary>
        /// Resets the zoom to 100%.
        /// </summary>
        /// <param name="source">The source that initiated the action.</param>
        private void PerformActualSize()
        {
            SizeMode = SizeModes.Normal;
            SetZoom(100, ImageZoomActions.ActualSize | (Zoom < 100 ? ImageZoomActions.ZoomIn : ImageZoomActions.ZoomOut));
        }

        private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            //Debug.WriteLine(e.Property.Name);
            if (e.Property.Name == nameof(VerticalScrollBarValue) ||
                e.Property.Name == nameof(HorizontalScrollBarValue))
            {
                InvalidateArrange();
                return;
            }

            if(e.Property.Name.Equals(nameof(Viewport)))
            {
                //if (SupressViewPortPropertyChange) return;
                UpdateViewPort();
                return;
            }
        }

        #region Overrides

        /*public override void EndInit()
        {
            base.EndInit();
            SKPaint SKBrush = new SKPaint { IsAntialias = true, Color = new SKColor(0, 0, 0) };
            SKBrush.Shader = SKShader.CreateColor(SKBrush.Color);
            //RenderTarget = new RenderTargetBitmap(new PixelSize((int)Viewport.Width, (int)Viewport.Height), new Vector(96, 96));
            
            //var context = RenderTarget.CreateDrawingContext(null);
            //SkiaContext = (context as ISkiaDrawingContextImpl);
            //SkiaContext.SkCanvas.Clear(new SKColor(100, 100, 255));
        }*/

        #endregion

        #region Methods



        public void LoadImage(string path)
        {
            Image = new Bitmap(path);
            Image.Save("D:\\test2.png");
            //ImageControl.Source = Image;
            //ImageControl.InvalidateVisual();
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

                    if(firstRowColor == currentColor) currentColor = ReferenceEquals(currentColor, GridColor) ? GridColorAlternate : GridColor;
                }

            }
            else
            {
                context.FillRectangle(Background, new Rect(0, 0, Viewport.Width, Viewport.Height));
                //SkiaContext.SkCanvas.Clear(new SKColor(100, 100, 255));
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
                Rect viewport = GetImageViewPort();
                var offsetX = Offset.X % pixelSize;
                var offsetY = Offset.Y % pixelSize;

                Pen pen = new Pen(PixelGridColor);
                for (double x = viewport.X + pixelSize - offsetX; x < viewport.Right; x += pixelSize)
                {
                    context.DrawLine(pen, new Point(x, viewport.X), new Point(x, viewport.Bottom));
                }

                for (double y = viewport.Y + pixelSize - offsetY; y < viewport.Bottom; y += pixelSize)
                {
                    context.DrawLine(pen, new Point(viewport.Y, y), new Point(viewport.Right, y));
                }

                context.DrawRectangle(pen, viewport);
            }
        }

       /// <summary>
       ///   Gets the source image region.
       /// </summary>
       /// <returns></returns>
       public virtual Rect GetSourceImageRegion()
       {
           if (Image is null) return new Rect(0, 0, 0, 0);

           if (SizeMode != SizeModes.Stretch)
           {
               var viewPort = GetImageViewPort();
               var sourceLeft = Offset.X / ZoomFactor;
               var sourceTop = Offset.Y / ZoomFactor;
               var sourceWidth = viewPort.Width / ZoomFactor;
               var sourceHeight = viewPort.Height / ZoomFactor;

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
                    xOffset = !IsHorizontalBarVisible ? (Viewport.Width - ScaledImageWidth) / 2 : 0;
                    yOffset = !IsVerticalBarVisible ? (Viewport.Height - ScaledImageHeight) / 2 : 0;
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

        public double ZoomFactor => Zoom / 100.0;

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (e.Handled) return;

            var pointer = e.GetCurrentPoint(this);
            if (!(
                    pointer.Properties.IsLeftButtonPressed && (PanWithMouseButtons & PanMouseButtons.LeftButton) != 0 ||
                    pointer.Properties.IsMiddleButtonPressed && (PanWithMouseButtons & PanMouseButtons.MiddleButton) != 0 ||
                    pointer.Properties.IsRightButtonPressed && (PanWithMouseButtons & PanMouseButtons.RightButton) != 0
                    )
                || !AutoPan || IsPanning || Image == null) return;
            var location = pointer.Position;
            
            if (location.X > Viewport.Width) return;
            if (location.Y > Viewport.Height) return;
            _startMousePosition = location;
            IsPanning = true;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (e.Handled) return;

            if (IsPanning) IsPanning = false;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (e.Handled) return;

            if (!IsPanning) return;
            var pointer = e.GetCurrentPoint(this);
            var location = pointer.Position;
            
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

            Offset = new Point(x, y);
            e.Handled = true;
        }
        #endregion
    }
}
