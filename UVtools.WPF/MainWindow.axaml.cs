/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.WPF.Controls;
using UVtools.WPF.Extensions;
using UVtools.WPF.Structures;
using UVtools.WPF.Windows;

namespace UVtools.WPF
{
    public class MainWindow : WindowEx, INotifyPropertyChanged
    {
        #region BindableBase
        /// <summary>
        ///     Multicast event for property change notifications.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Checks if a property already matches a desired value.  Sets the property and
        ///     notifies listeners only when necessary.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to a property with both getter and setter.</param>
        /// <param name="value">Desired value for the property.</param>
        /// <param name="propertyName">
        ///     Name of the property used to notify listeners.  This
        ///     value is optional and can be provided automatically when invoked from compilers that
        ///     support CallerMemberName.
        /// </param>
        /// <returns>
        ///     True if the value was changed, false if the existing value matched the
        ///     desired value.
        /// </returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        ///     Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">
        ///     Name of the property used to notify listeners.  This
        ///     value is optional and can be provided automatically when invoked from compilers
        ///     that support <see cref="CallerMemberNameAttribute" />.
        /// </param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = PropertyChanged;
            eventHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Properties
        #region Redirects
        public UserSettings Settings => UserSettings.Instance;
        public FileFormat SlicerFile => App.SlicerFile;
        #endregion

        #region Controls
        public AdvancedImageBox LayerImageBox;
        public SliderEx LayerSlider;
        public Panel LayerNavigationTooltipPanel;
        public Border LayerNavigationTooltipBorder;
        #endregion

        #region Members
        private uint _actualLayer;
        private bool _isGUIEnabled = true;
        private bool _showLayerImageRotated;
        private bool _showLayerImageDifference;
        private bool _showLayerImageIssues = true;
        private bool _showLayerImageCrosshairs = true;
        private bool _isPixelEditorActive;
        private bool _showLayerOutlinePrintVolumeBoundary;
        private bool _showLayerOutlineLayerBoundary;
        private bool _showLayerOutlineHollowAreas;
        private bool _showLayerOutlineEdgeDetection;

        private long _showLayerRenderMs;
        #endregion

        #region  GUI Models
        public bool IsGUIEnabled
        {
            get => _isGUIEnabled;
            set => SetProperty(ref _isGUIEnabled, value);
        }

        public bool IsFileLoaded
        {
            get => !ReferenceEquals(SlicerFile, null);
            set => OnPropertyChanged();
        }

        public bool ShowLayerImageRotated
        {
            get => _showLayerImageRotated;
            set
            {
                if (SetProperty(ref _showLayerImageRotated, value))
                {
                    ShowLayer();
                }
            }
        }

        public bool ShowLayerImageDifference
        {
            get => _showLayerImageDifference;
            set
            {
                if (SetProperty(ref _showLayerImageDifference, value))
                {
                    ShowLayer();
                }
            }
        }

        public bool ShowLayerImageIssues
        {
            get => _showLayerImageIssues;
            set
            {
                if (SetProperty(ref _showLayerImageIssues, value))
                {
                    ShowLayer();
                }
            }
        }

        public bool ShowLayerImageCrosshairs
        {
            get => _showLayerImageCrosshairs;
            set
            {
                if (SetProperty(ref _showLayerImageCrosshairs, value))
                {
                    ShowLayer();
                }
            }
        }

        public bool ShowLayerOutlinePrintVolumeBoundary
        {
            get => _showLayerOutlinePrintVolumeBoundary;
            set
            {
                if (SetProperty(ref _showLayerOutlinePrintVolumeBoundary, value))
                {
                    ShowLayer();
                }
            }
        }

        public bool ShowLayerOutlineLayerBoundary
        {
            get => _showLayerOutlineLayerBoundary;
            set
            {
                if (SetProperty(ref _showLayerOutlineLayerBoundary, value))
                {
                    ShowLayer();
                }
            }
        }

        public bool ShowLayerOutlineHollowAreas
        {
            get => _showLayerOutlineHollowAreas;
            set
            {
                if (SetProperty(ref _showLayerOutlineHollowAreas, value))
                {
                    ShowLayer();
                }
            }
        }

        public bool ShowLayerOutlineEdgeDetection
        {
            get => _showLayerOutlineEdgeDetection;
            set
            {
                if (SetProperty(ref _showLayerOutlineEdgeDetection, value))
                {
                    ShowLayer();
                }
            }
        }

        public bool IsPixelEditorActive
        {
            get => _isPixelEditorActive;
            set => SetProperty(ref _isPixelEditorActive, value);
        }

        public string MinimumLayerString => SlicerFile is null ? "???" : $"{SlicerFile.LayerHeight}mm\n0";
        public string MaximumLayerString => SlicerFile is null ? "???" : $"{SlicerFile.TotalHeight}mm\n{SlicerFile.LayerCount - 1}";
        public string ActualLayerTooltip => SlicerFile is null ? "???" : $"{SlicerFile.GetHeightFromLayer(ActualLayer):0.00}mm\n{ActualLayer}\n{ActualLayer * 100 / (SlicerFile.LayerCount - 1)}%";

        public uint SliderMaximumValue => SlicerFile?.LayerCount - 1 ?? 0;

        public bool CanGoUp => _actualLayer < SliderMaximumValue;
        public bool CanGoDown => _actualLayer > 0;

        public long ShowLayerRenderMs
        {
            get => _showLayerRenderMs;
            set => SetProperty(ref _showLayerRenderMs, value);
        }
        #endregion

        private uint ActualLayer
        {
            get => _actualLayer;
            set
            {
                if (!SetProperty(ref _actualLayer, value)) return;
                OnPropertyChanged(nameof(CanGoDown));
                OnPropertyChanged(nameof(CanGoUp));
                OnPropertyChanged(nameof(ActualLayerTooltip));
                OnPropertyChanged(nameof(LayerNavigationTooltipMargin));
                ShowLayer();
            }
        }

        public Thickness LayerNavigationTooltipMargin
        {
            get
            {
                double top = 0;
                if (LayerSlider != null)
                {
                    double trackerPos = LayerSlider.Track.Thumb.Bounds.Height / 2 + LayerSlider.Track.Thumb.Bounds.Top;
                    double halfTooltipHeight = LayerNavigationTooltipBorder.Bounds.Height / 2;
                    top = (trackerPos - halfTooltipHeight).Clamp(0,
                        LayerSlider.Bounds.Height - LayerNavigationTooltipBorder.Bounds.Height);
                    
                }
                return new Thickness(
                    0,
                    top,
                    5,
                    0);
            }
        }

        

        public LayerCache LayerCache = new LayerCache();


        #endregion

        #region Constructors

        public MainWindow()
        {
            _showLayerImageDifference = Settings.LayerPreview.ShowLayerDifference;
            _showLayerOutlinePrintVolumeBoundary = Settings.LayerPreview.VolumeBoundsOutline;
            _showLayerOutlineLayerBoundary = Settings.LayerPreview.LayerBoundsOutline;
            _showLayerOutlineHollowAreas = Settings.LayerPreview.HollowOutline;

            DataContext = this;
            InitializeComponent();
#if DEBUG
            //this.AttachDevTools();
#endif
            App.ThemeSelector?.EnableThemes(this);
            LayerImageBox = this.FindControl<AdvancedImageBox>("Layer.Image");
            LayerSlider = this.FindControl<SliderEx>("Layer.Navigation.Slider");
            LayerNavigationTooltipPanel = this.FindControl<Panel>("Layer.Navigation.Tooltip.Panel");
            LayerNavigationTooltipBorder = this.FindControl<Border>("Layer.Navigation.Tooltip.Border");
            /*LayerSlider.PropertyChanged += (sender, args) =>
            {
                Debug.WriteLine(args.Property.Name);
                if (args.Property.Name == nameof(LayerSlider.Value))
                {
                    LayerNavigationTooltipPanel.Margin = LayerNavigationTooltipMargin;
                    return;
                }
            };*/
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine(e.PropertyName);
            /*if (e.PropertyName == nameof(ActualLayer))
            {
                LayerSlider.Value = ActualLayer;
                ShowLayer();
                return;
            }*/
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        #endregion

        #region Events

        public async void MenuFileOpenClicked()
        {
            var dialog = new OpenFileDialog
            {
                AllowMultiple = false,
            };
            var files = await dialog.ShowAsync(this);
            ProcessFiles(files);
        }

        public async void MenuFileSettingsClicked()
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            await settingsWindow.ShowDialog(this);
        }
        #endregion

        #region Methods
        public void ProcessFiles(string[] files, bool openNewWindow = false)
        {
            if (files is null || files.Length == 0) return;

            for (int i = 0; i < files.Length; i++)
            {
                if (i == 0 && !openNewWindow)
                {
                    ProcessFile(files[i]);
                    continue;
                }
            }
        }

        void ReloadFile(uint actualLayer = 0)
        {
            if (App.SlicerFile is null) return;
            ProcessFile(App.SlicerFile.FileFullPath, actualLayer);
        }

        void ProcessFile(string fileName, uint actualLayer = 0)
        {
            var fileNameOnly = Path.GetFileName(fileName);
            App.SlicerFile = FileFormat.FindByExtension(fileName, true, true);
            if (App.SlicerFile is null) return;

            IsGUIEnabled = false;

            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    App.SlicerFile.Decode(fileName);
                }
                catch (OperationCanceledException)
                {
                    App.SlicerFile.Clear();
                }
                finally
                {
                   /* Invoke((MethodInvoker)delegate
                    {
                        // Running on the UI thread
                        EnableGUI(true);
                    });*/
                }
            });

            //ProgressWindow progressWindow = new ProgressWindow();
            //progressWindow.ShowDialog(this);

            task.Wait();

            var mat = App.SlicerFile[0].LayerMat;
            var matRgb = App.SlicerFile[0].BrgMat;

            Debug.WriteLine("4K grayscale - BGRA convertion:");
            var bitmap = mat.ToBitmap();
            Debug.WriteLine("4K BGR - BGRA convertion:");
            var bitmapRgb = matRgb.ToBitmap();
            LayerImageBox.Image = bitmapRgb;
            
            IsGUIEnabled = true;
            ResetDataContext();
        }

        public void GoFirstLayer()
        {
            if (SlicerFile is null) return;
            if (!CanGoDown) return;
            ActualLayer = 0;
        }

        public void GoPreviousLayer()
        {
            if (SlicerFile is null) return;
            if (!CanGoDown) return;
            ActualLayer--;
        }

        public void GoNextLayer()
        {
            if (SlicerFile is null) return;
            if (!CanGoUp) return;
            ActualLayer++;
        }

        public void GoLastLayer()
        {
            if (SlicerFile is null) return;
            if (!CanGoUp) return;
            ActualLayer = SliderMaximumValue;
        }

        /// <summary>
        /// Shows a layer number
        /// </summary>
        void ShowLayer()
        {
            if (SlicerFile is null) return;
            Debug.WriteLine($"Showing layer: {_actualLayer}");

            

            //AddLogVerbose($"Show Layer: {layerNum}");


            Stopwatch watch = Stopwatch.StartNew();
            var layer = SlicerFile[_actualLayer];

            try
            {
                LayerCache.Image = layer.LayerMat;

                //var imageSpan = LayerCache.Image.GetPixelSpan<byte>();
                //var imageBgrSpan = LayerCache.ImageBgr.GetPixelSpan<byte>();

                unsafe
                {
                    var imageSpan = LayerCache.Image.GetBytePointer();
                    var imageBgrSpan = LayerCache.ImageBgr.GetBytePointer();

                    if (_showLayerOutlineEdgeDetection)
                    {
                        using (var canny = new Mat())
                        {
                            CvInvoke.Canny(LayerCache.Image, canny, 80, 40, 3, true);
                            CvInvoke.CvtColor(canny, LayerCache.ImageBgr, ColorConversion.Gray2Bgra);
                        }
                    }
                    else if (_showLayerImageDifference)
                    {
                        if (_actualLayer > 0 && _actualLayer < SlicerFile.LayerCount - 1)
                        {
                            Mat previousImage = null;
                            Mat nextImage = null;

                            // Can improve performance on >4K images?
                            Parallel.Invoke(
                    () => { previousImage = SlicerFile[_actualLayer - 1].LayerMat; },
                                () => { nextImage = SlicerFile[_actualLayer + 1].LayerMat; });

                            /*using (var previousImage = SlicerFile[_actualLayer - 1].LayerMat)
                            using (var nextImage = SlicerFile[_actualLayer + 1].LayerMat)
                            {*/
                            //var previousSpan = previousImage.GetPixelSpan<byte>();
                            //var nextSpan = nextImage.GetPixelSpan<byte>();

                            var previousSpan = previousImage.GetBytePointer();
                            var nextSpan = nextImage.GetBytePointer();

                            int width = LayerCache.Image.Width;
                            int channels = LayerCache.ImageBgr.NumberOfChannels;
                            Parallel.For(0, LayerCache.Image.Height, y =>
                            {
                                for (int x = 0; x < width; x++)
                                {
                                    int pixel = y * width + x;
                                    if (imageSpan[pixel] != 0) continue;
                                    Color color = Color.Empty;
                                    if (previousSpan[pixel] > 0 && nextSpan[pixel] > 0)
                                    {
                                        color = Settings.LayerPreview.NextLayerDifferenceColor;
                                    }
                                    else if (previousSpan[pixel] > 0)
                                    {
                                        color = Settings.LayerPreview.PreviousLayerDifferenceColor;
                                    }
                                    else if (nextSpan[pixel] > 0)
                                    {
                                        color = Settings.LayerPreview.NextLayerDifferenceColor;
                                    }

                                    if (color.IsEmpty) continue;
                                    var bgrPixel = pixel * channels;
                                    imageBgrSpan[bgrPixel] = color.B; // B
                                    imageBgrSpan[++bgrPixel] = color.G; // G
                                    imageBgrSpan[++bgrPixel] = color.R; // R
                                    //imageBgrSpan[++bgrPixel] = color.A; // A
                                }
                            });

                            previousImage.Dispose();
                            nextImage.Dispose();
                        }
                    }

                    /*
                    var selectedIssues = flvIssues.SelectedObjects;
    
                    if (btnLayerImageHighlightIssues.Checked &&
                        !ReferenceEquals(Issues, null))
                    {
                        foreach (var issue in Issues)
                        {
                            if (issue.LayerIndex != ActualLayer) continue;
                            if (!issue.HaveValidPoint) continue;
    
                            Color color = Color.Empty;
    
                            if (issue.Type == LayerIssue.IssueType.ResinTrap)
                            {
                                color = selectedIssues.Contains(issue)
                                    ? Settings.Default.ResinTrapHLColor
                                    : Settings.Default.ResinTrapColor;
    
    
                                using (var vec = new VectorOfVectorOfPoint(new VectorOfPoint(issue.Pixels)))
                                {
                                    CvInvoke.DrawContours(ActualLayerImageBgr, vec, -1,
                                        new MCvScalar(color.B, color.G, color.R), -1);
                                }
    
                                if (btnLayerImageShowCrosshairs.Checked &&
                                    !Settings.Default.CrosshairShowOnlyOnSelectedIssues &&
                                    pbLayer.Zoom <= CrosshairFadeLevel)
                                {
                                    DrawCrosshair(issue.BoundingRectangle);
                                }
    
                                continue;
                            }
    
                            switch (issue.Type)
                            {
                                case LayerIssue.IssueType.Island:
                                    color = selectedIssues.Contains(issue)
                                        ? Settings.Default.IslandHLColor
                                        : Settings.Default.IslandColor;
                                    if (btnLayerImageShowCrosshairs.Checked &&
                                        !Settings.Default.CrosshairShowOnlyOnSelectedIssues &&
                                        pbLayer.Zoom <= CrosshairFadeLevel)
                                    {
                                        DrawCrosshair(issue.BoundingRectangle);
                                    }
    
                                    break;
                                case LayerIssue.IssueType.TouchingBound:
                                    color = Settings.Default.TouchingBoundsColor;
                                    break;
                            }
    
                            foreach (var pixel in issue)
                            {
                                int pixelPos = ActualLayerImage.GetPixelPos(pixel);
                                byte brightness = imageSpan[pixelPos];
                                if (brightness == 0) continue;
    
                                int pixelBgrPos = pixelPos * ActualLayerImageBgr.NumberOfChannels;
    
                                var newColor = color.FactorColor(brightness, 80);
    
                                imageBgrSpan[pixelBgrPos] = newColor.B; // B
                                imageBgrSpan[pixelBgrPos + 1] = newColor.G; // G
                                imageBgrSpan[pixelBgrPos + 2] = newColor.R; // R
                            }
                        }
                    }*/
    
                    if (_showLayerOutlinePrintVolumeBoundary)
                    {
                        CvInvoke.Rectangle(LayerCache.ImageBgr, SlicerFile.LayerManager.BoundingRectangle,
                            new MCvScalar(Settings.LayerPreview.VolumeBoundsOutlineColor.B,
                                Settings.LayerPreview.VolumeBoundsOutlineColor.G,
                                Settings.LayerPreview.VolumeBoundsOutlineColor.R),
                            Settings.LayerPreview.VolumeBoundsOutlineThickness);
                    }
    
                    if (_showLayerOutlineLayerBoundary)
                    {
                        CvInvoke.Rectangle(LayerCache.ImageBgr, SlicerFile[_actualLayer].BoundingRectangle,
                            new MCvScalar(Settings.LayerPreview.LayerBoundsOutlineColor.B,
                                Settings.LayerPreview.LayerBoundsOutlineColor.G, Settings.LayerPreview.LayerBoundsOutlineColor.R),
                            Settings.LayerPreview.LayerBoundsOutlineThickness);
                    }
    
                    if (_showLayerOutlineHollowAreas)
                    {
                        //CvInvoke.Threshold(ActualLayerImage, grayscale, 1, 255, ThresholdType.Binary);
    
                        /*
                         * hierarchy[i][0]: the index of the next contour of the same level
                         * hierarchy[i][1]: the index of the previous contour of the same level
                         * hierarchy[i][2]: the index of the first child
                         * hierarchy[i][3]: the index of the parent
                         */
                        for (int i = 0; i < LayerCache.LayerContours.Size; i++)
                        {
                            if ((int)LayerCache.LayerHierarchyJagged.GetValue(0, i, 2) == -1 &&
                                (int)LayerCache.LayerHierarchyJagged.GetValue(0, i, 3) != -1)
                            {
                                //var r = CvInvoke.BoundingRectangle(contours[i]);
                                //CvInvoke.Rectangle(ActualLayerImageBgr, r, new MCvScalar(0, 0, 255), 2);
                                CvInvoke.DrawContours(LayerCache.ImageBgr, LayerCache.LayerContours, i,
                                    new MCvScalar(Settings.LayerPreview.HollowOutlineColor.B,
                                        Settings.LayerPreview.HollowOutlineColor.G,
                                        Settings.LayerPreview.HollowOutlineColor.R),
                                    Settings.LayerPreview.HollowOutlineLineThickness);
                            }
                        }
                    }
    
                    /*for (var index = 0; index < PixelHistory.Count; index++)
                    {
                        if (PixelHistory[index].LayerIndex != ActualLayer) continue;
                        var operation = PixelHistory[index];
                        if (operation.OperationType == PixelOperation.PixelOperationType.Drawing)
                        {
                            var operationDrawing = (PixelDrawing)operation;
                            var color = operationDrawing.IsAdd
                                ? (flvPixelHistory.SelectedObjects.Contains(operation)
                                    ? Settings.Default.PixelEditorAddPixelHLColor
                                    : Settings.Default.PixelEditorAddPixelColor)
                                : (flvPixelHistory.SelectedObjects.Contains(operation)
                                    ? Settings.Default.PixelEditorRemovePixelHLColor
                                    : Settings.Default.PixelEditorRemovePixelColor);
                            if (operationDrawing.BrushSize == 1)
                            {
                                ActualLayerImageBgr.SetByte(operation.Location.X, operation.Location.Y,
                                    new[] { color.B, color.G, color.R });
                                continue;
                            }
    
                            switch (operationDrawing.BrushShape)
                            {
                                case PixelDrawing.BrushShapeType.Rectangle:
                                    CvInvoke.Rectangle(ActualLayerImageBgr, operationDrawing.Rectangle,
                                        new MCvScalar(color.B, color.G, color.R), operationDrawing.Thickness,
                                        operationDrawing.LineType);
                                    break;
                                case PixelDrawing.BrushShapeType.Circle:
                                    CvInvoke.Circle(ActualLayerImageBgr, operation.Location, operationDrawing.BrushSize / 2,
                                        new MCvScalar(color.B, color.G, color.R), operationDrawing.Thickness,
                                        operationDrawing.LineType);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                        else if (operation.OperationType == PixelOperation.PixelOperationType.Text)
                        {
                            var operationText = (PixelText)operation;
                            var color = operationText.IsAdd
                                ? (flvPixelHistory.SelectedObjects.Contains(operation)
                                    ? Settings.Default.PixelEditorAddPixelHLColor
                                    : Settings.Default.PixelEditorAddPixelColor)
                                : (flvPixelHistory.SelectedObjects.Contains(operation)
                                    ? Settings.Default.PixelEditorRemovePixelHLColor
                                    : Settings.Default.PixelEditorRemovePixelColor);
    
                            CvInvoke.PutText(ActualLayerImageBgr, operationText.Text, operationText.Location,
                                operationText.Font, operationText.FontScale, new MCvScalar(color.B, color.G, color.R),
                                operationText.Thickness, operationText.LineType, operationText.Mirror);
                        }
                        else if (operation.OperationType == PixelOperation.PixelOperationType.Eraser)
                        {
                            if (imageSpan[ActualLayerImage.GetPixelPos(operation.Location)] < 10) continue;
                            var color = flvPixelHistory.SelectedObjects.Contains(operation)
                                ? Settings.Default.PixelEditorRemovePixelHLColor
                                : Settings.Default.PixelEditorRemovePixelColor;
                            for (int i = 0; i < LayerCache.LayerContours.Size; i++)
                            {
                                if (CvInvoke.PointPolygonTest(LayerCache.LayerContours[i], operation.Location, false) >= 0)
                                {
                                    CvInvoke.DrawContours(ActualLayerImageBgr, LayerCache.LayerContours, i,
                                        new MCvScalar(color.B, color.G, color.R), -1);
                                    break;
                                }
                            }
                        }
                        else if (operation.OperationType == PixelOperation.PixelOperationType.Supports)
                        {
                            var operationSupport = (PixelSupport)operation;
                            var color = flvPixelHistory.SelectedObjects.Contains(operation)
                                ? Settings.Default.PixelEditorSupportHLColor
                                : Settings.Default.PixelEditorSupportColor;
    
                            CvInvoke.Circle(ActualLayerImageBgr, operation.Location, operationSupport.TipDiameter / 2,
                                new MCvScalar(color.B, color.G, color.R), -1);
                        }
                        else if (operation.OperationType == PixelOperation.PixelOperationType.DrainHole)
                        {
                            var operationDrainHole = (PixelDrainHole)operation;
                            var color = flvPixelHistory.SelectedObjects.Contains(operation)
                                ? Settings.Default.PixelEditorDrainHoleHLColor
                                : Settings.Default.PixelEditorDrainHoleColor;
    
                            CvInvoke.Circle(ActualLayerImageBgr, operation.Location, operationDrainHole.Diameter / 2,
                                new MCvScalar(color.B, color.G, color.R), -1);
                        }
                    }
    
                    // Show crosshairs for selected issues if crosshair mode is enabled via toolstrip button.
                    // Even when enabled, crosshairs are hidden in pixel edit mode when SHIFT is pressed.
                    if (btnLayerImageShowCrosshairs.Checked &&
                        Settings.Default.CrosshairShowOnlyOnSelectedIssues &&
                        !ReferenceEquals(Issues, null) &&
                        flvIssues.SelectedIndices.Count > 0 &&
                        pbLayer.Zoom <=
                        CrosshairFadeLevel && // Only draw crosshairs when zoom level is below the configurable crosshair fade threshold.
                        !(btnLayerImagePixelEdit.Checked && (ModifierKeys & Keys.Shift) != 0))
                    {
    
    
                        foreach (LayerIssue issue in selectedIssues)
                        {
                            // Don't render crosshairs for selected issue that are not on the current layer, or for 
                            // issue types that don't have a specific location or bounds.
                            if (issue.LayerIndex != ActualLayer || issue.Type == LayerIssue.IssueType.EmptyLayer
                                                                || issue.Type == LayerIssue.IssueType.TouchingBound)
                                continue;
    
                            DrawCrosshair(issue.BoundingRectangle);
                        }
                    }*/

                    if (_showLayerImageRotated)
                    {
                        CvInvoke.Rotate(LayerCache.ImageBgr, LayerCache.ImageBgr, RotateFlags.Rotate90Clockwise);
                    }
                }


                LayerImageBox.Image = LayerCache.ImageBgr.ToBitmap();

                /*byte percent = (byte)((layerNum + 1) * 100 / SlicerFile.LayerCount);

                float pixelPercent =
                    (float)Math.Round(
                        layer.NonZeroPixelCount * 100f / (SlicerFile.ResolutionX * SlicerFile.ResolutionY), 2);
                tsLayerImagePixelCount.Text = $"Pixels: {layer.NonZeroPixelCount} ({pixelPercent}%)";
                btnLayerBounds.Text = $"Bounds: {layer.BoundingRectangle}";
                tsLayerImagePixelCount.Invalidate();
                btnLayerBounds.Invalidate();
                tsLayerInfo.Update();
                tsLayerInfo.Refresh();

                watch.Stop();
                tsLayerPreviewTime.Text = $"{watch.ElapsedMilliseconds}ms";
                //lbLayers.Text = $"{SlicerFile.GetHeightFromLayer(layerNum)} / {SlicerFile.TotalHeight}mm\n{layerNum} / {SlicerFile.LayerCount-1}\n{percent}%";
                lbActualLayer.Text = $"{layer.PositionZ}mm\n{ActualLayer}\n{percent}%";
                lbActualLayer.Location = new Point(lbActualLayer.Location.X,
                    ((int)(tbLayer.Height - (float)tbLayer.Height / tbLayer.Maximum * tbLayer.Value) -
                     lbActualLayer.Height / 2)
                    .Clamp(1, tbLayer.Height - lbActualLayer.Height));

                lbActualLayer.Invalidate();
                lbActualLayer.Update();
                lbActualLayer.Refresh();
                pbLayer.Invalidate();
                pbLayer.Update();
                pbLayer.Refresh();*/

                watch.Stop();
                ShowLayerRenderMs = watch.ElapsedMilliseconds;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        #endregion
    }
}
