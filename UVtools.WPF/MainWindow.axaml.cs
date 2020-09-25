/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DynamicData;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using MessageBox.Avalonia.Enums;
using ReactiveUI;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.PixelEditor;
using UVtools.WPF.Controls;
using UVtools.WPF.Extensions;
using UVtools.WPF.Structures;
using UVtools.WPF.Windows;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Color = UVtools.WPF.Structures.Color;
using Helpers = UVtools.WPF.Controls.Helpers;
using Point = Avalonia.Point;

namespace UVtools.WPF
{
    public class MainWindow : WindowEx
    {
        #region Properties
        #region Redirects
        public UserSettings Settings => UserSettings.Instance;
        public FileFormat SlicerFile => App.SlicerFile;
        #endregion

        #region Controls

        public ProgressWindow ProgressWindow = new ProgressWindow();

        //private ProgressWindow _progressWindow;
        public AdvancedImageBox LayerImageBox;
        public SliderEx LayerSlider;
        public Panel LayerNavigationTooltipPanel;
        public Border LayerNavigationTooltipBorder;
           
        public DataGrid IssuesGrid;

        #region DataSets
        public ObservableCollection<SlicerProperty> SlicerProperties { get; } = new ObservableCollection<SlicerProperty>();
        public ObservableCollection<LogItem> Logs { get; } = new ObservableCollection<LogItem>();

        public ObservableCollection<LayerIssue> Issues
        {
            get => _issues;
            private set => SetProperty(ref _issues, value);
        }

        public ObservableCollection<PixelOperation> Drawings { get; } = new ObservableCollection<PixelOperation>();
        #endregion

        #endregion

        #region Members

        public Stopwatch LastStopWatch;
        private uint _actualLayer;
        private bool _isGUIEnabled = true;
        private uint _savesCount;
        private bool _canSave;
        private MenuItem[] _menuFileConvertItems;
        private int _tabSelectedIndex;
        private uint _visibleThumbnailIndex;
        private Bitmap _visibleThumbnailImage;

        private int _issueSelectedIndex = -1;

        private bool _isVerbose;
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
            set
            {
                if (!SetProperty(ref _isGUIEnabled, value)) return;
                if (!_isGUIEnabled)
                {
                    ProgressWindow = new ProgressWindow();
                    return;
                }
                //if (ProgressWindow is null) return;

                LastStopWatch = ProgressWindow.StopWatch;
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ProgressWindow.Close();
                    ProgressWindow.Dispose();
                });


            }
        }

        public bool IsFileLoaded
        {
            get => !(SlicerFile is null);
            set => OnPropertyChanged();
        }

        public int TabSelectedIndex
        {
            get => _tabSelectedIndex;
            set => SetProperty(ref _tabSelectedIndex, value);
        }
        #endregion

        public uint SavesCount
        {
            get => _savesCount;
            set => SetProperty(ref _savesCount, value);
        }

        public bool CanSave
        {
            get => _canSave;
            set => SetProperty(ref _canSave, value);
        }

        public MenuItem[] MenuFileConvertItems
        {
            get => _menuFileConvertItems;
            set => SetProperty(ref _menuFileConvertItems, value);
        }


        #region Thumbnails
        public uint VisibleThumbnailIndex
        {
            get => _visibleThumbnailIndex;
            set
            {
                if (value == 0)
                {
                    SetProperty(ref _visibleThumbnailIndex, value);
                    OnPropertyChanged(nameof(ThumbnailCanGoPrevious));
                    OnPropertyChanged(nameof(ThumbnailCanGoNext));
                    VisibleThumbnailImage = null;
                    return;
                }

                if (!IsFileLoaded) return;
                var index = value-1;
                if (index >= SlicerFile.CreatedThumbnailsCount) return;
                if (SlicerFile.Thumbnails[index] is null) return;
                if (!SetProperty(ref _visibleThumbnailIndex, value)) return;
                
                VisibleThumbnailImage = SlicerFile.Thumbnails[index].ToBitmap();
                OnPropertyChanged(nameof(ThumbnailCanGoPrevious));
                OnPropertyChanged(nameof(ThumbnailCanGoNext));
            }
        }

        public bool ThumbnailCanGoPrevious => SlicerFile is { } && _visibleThumbnailIndex > 1;
        public bool ThumbnailCanGoNext => SlicerFile is { } && _visibleThumbnailIndex < SlicerFile.CreatedThumbnailsCount;

        public void ThumbnailGoPrevious()
        {
            if (!ThumbnailCanGoPrevious) return;
            VisibleThumbnailIndex--;
        }

        public void ThumbnailGoNext()
        {
            if (!ThumbnailCanGoNext) return;
            VisibleThumbnailIndex++;
        }

        public Bitmap VisibleThumbnailImage
        {
            get => _visibleThumbnailImage;
            set
            {
                SetProperty(ref _visibleThumbnailImage, value);
                OnPropertyChanged(nameof(VisibleThumbnailResolution));
            }
        }

        public string VisibleThumbnailResolution => _visibleThumbnailImage is null ? null : $"{{Width: {_visibleThumbnailImage.Size.Width}, Height: {_visibleThumbnailImage.Size.Height}}}";

        public async void OnClickThumbnailSave()
        {
            if (SlicerFile is null) return;
            if (ReferenceEquals(SlicerFile.Thumbnails[_visibleThumbnailIndex-1], null))
            {
                return; // This should never happen!
            }
            var dialog = new SaveFileDialog
            {
                Filters = Helpers.PngFileFilter,
                Directory = Path.GetDirectoryName(SlicerFile.FileFullPath),
                InitialFileName = $"{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}_thumbnail{_visibleThumbnailIndex}.png"
            };

            var filepath = await dialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(filepath))
            {
                SlicerFile.Thumbnails[_visibleThumbnailIndex - 1].Save(filepath);
            }
        }

        public async void OnClickThumbnailImport()
        {
            if (_visibleThumbnailIndex <= 0) return;
            if (ReferenceEquals(SlicerFile.Thumbnails[_visibleThumbnailIndex - 1], null))
            {
                return; // This should never happen!
            }

            var dialog = new OpenFileDialog
            {
                Filters = Helpers.ImagesFileFilter,
                AllowMultiple = false
            };

            var filepath = await dialog.ShowAsync(this);

            if (filepath is null || filepath.Length <= 0) return;
            int i = (int) (_visibleThumbnailIndex - 1);
            SlicerFile.SetThumbnail(i, filepath[0]);
            VisibleThumbnailImage = SlicerFile.Thumbnails[i].ToBitmap();
        }
        #endregion

        #region Slicer Properties

        public async void OnClickPropertiesSaveFile()
        {
            if (SlicerFile?.Configs is null) return;

            var dialog = new SaveFileDialog
            {
                Filters = Helpers.IniFileFilter,
                Directory = Path.GetDirectoryName(SlicerFile.FileFullPath),
                InitialFileName = $"{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}_properties.ini"
            };

            var file = await dialog.ShowAsync(this);

            if (string.IsNullOrEmpty(file)) return;

            try
            {
                using (TextWriter tw = new StreamWriter(file))
                {
                    foreach (var config in SlicerFile.Configs)
                    {
                        var type = config.GetType();
                        tw.WriteLine($"[{type.Name}]");
                        foreach (var property in type.GetProperties())
                        {
                            tw.WriteLine($"{property.Name} = {property.GetValue(config)}");
                        }

                        tw.WriteLine();
                    }

                    tw.Close();
                }
            }
            catch (Exception e)
            {
                await this.MessageBoxError(e.Message, "Error occur while save properties");
                return;
            }

            var result = await this.MessageBoxQuestion(
                "Properties save was successful. Do you want open the file in the default editor?",
                "Properties save complete");
            if (result != ButtonResult.Yes) return;

            App.StartProcess(file);
        }

        public void OnClickPropertiesSaveClipboard()
        {
            if (SlicerFile?.Configs is null) return;
            var sb = new StringBuilder();
            foreach (var config in SlicerFile.Configs)
            {
                var type = config.GetType();
                sb.AppendLine($"[{type.Name}]");
                foreach (var property in type.GetProperties())
                {
                    sb.AppendLine($"{property.Name} = {property.GetValue(config)}");
                }

                sb.AppendLine();
            }

            Application.Current.Clipboard.SetTextAsync(sb.ToString());
        }
        #endregion

        #region GCode
        public bool HaveGCode => IsFileLoaded && SlicerFile.HaveGCode;

        public string GCodeStr => SlicerFile?.GCodeStr;
        public int GCodeLines => !HaveGCode ? 0 : SlicerFile.GCodeStr.Split('\n').Length;

        public void OnClickRebuildGcode()
        {
            if (!HaveGCode) return;
            SlicerFile.RebuildGCode();
            OnPropertyChanged(nameof(GCodeLines));
            OnPropertyChanged(nameof(GCodeStr));
        }

        public async void OnClickGCodeSaveFile()
        {
            if (!HaveGCode) return;

            var dialog = new SaveFileDialog
            {
                Filters = Helpers.IniFileFilter,
                Directory = Path.GetDirectoryName(SlicerFile.FileFullPath),
                InitialFileName = $"{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}_gcode.txt"
            };

            var file = await dialog.ShowAsync(this);

            if (string.IsNullOrEmpty(file)) return;

            try
            {
                using (TextWriter tw = new StreamWriter(file))
                {
                    tw.Write(SlicerFile.GCodeStr);
                    tw.Close();
                }
            }
            catch (Exception e)
            {
                await this.MessageBoxError(e.Message, "Error occur while save gcode");
                return;
            }

            var result = await this.MessageBoxQuestion(
                "GCode save was successful. Do you want open the file in the default editor?",
                "GCode save complete");
            if (result != ButtonResult.Yes) return;

            App.StartProcess(file);
        }

        public void OnClickGCodeSaveClipboard()
        {
            if (!HaveGCode) return;
            Application.Current.Clipboard.SetTextAsync(GCodeStr);
        }

        #endregion

        #region Issues

        public bool IssueCanGoPrevious => Issues.Count > 0 && _issueSelectedIndex > 0;
        public bool IssueCanGoNext => Issues.Count > 0 && _issueSelectedIndex < Issues.Count-1;

        public void IssueGoPrevious()
        {
            if (!IssueCanGoPrevious) return;
            IssueSelectedIndex--;
        }

        public void IssueGoNext()
        {
            if (!IssueCanGoNext) return;
            IssueSelectedIndex++;
        }

        public async void OnClickIssueRemove()
        {
            if (IssuesGrid.SelectedItems.Count == 0) return;

            if (await this.MessageBoxQuestion($"Are you sure you want to remove all selected {IssuesGrid.SelectedItems.Count} issues?\n\n" +
                                    "Warning: Removing an island can cause other issues to appear if there is material present in the layers above it.\n" +
                                    "Always check previous and next layers before performing an island removal.", "Remove Issues?") != ButtonResult.Yes) return;

            Dictionary<uint, List<LayerIssue>> processIssues = new Dictionary<uint, List<LayerIssue>>();
            List<uint> layersRemove = new List<uint>();


            foreach (LayerIssue issue in IssuesGrid.SelectedItems)
            {
                if (issue.Type == LayerIssue.IssueType.TouchingBound) continue;

                if (!processIssues.TryGetValue(issue.Layer.Index, out var issueList))
                {
                    issueList = new List<LayerIssue>();
                    processIssues.Add(issue.Layer.Index, issueList);
                }

                issueList.Add(issue);
                if (issue.Type == LayerIssue.IssueType.EmptyLayer)
                {
                    layersRemove.Add(issue.Layer.Index);
                }
            }


            IsGUIEnabled = false;
            ProgressWindow.SetTitle("Removing selected issues");
            var progress = ProgressWindow.RestartProgress(false);

            progress.Reset("Removing selected issues", (uint)processIssues.Count);
            var task = Task<bool>.Factory.StartNew(() =>
            {
                bool result = false;
                try
                {
                    Parallel.ForEach(processIssues, layerIssues =>
                    {
                        if (progress.Token.IsCancellationRequested) return;
                        using (var image = SlicerFile[layerIssues.Key].LayerMat)
                        {
                            var bytes = image.GetPixelSpan<byte>();

                            bool edited = false;

                            foreach (var issue in layerIssues.Value)
                            {
                                if (issue.Type == LayerIssue.IssueType.Island)
                                {
                                    foreach (var pixel in issue)
                                    {
                                        bytes[image.GetPixelPos(pixel.X, pixel.Y)] = 0;
                                    }

                                    edited = true;
                                }
                                else if (issue.Type == LayerIssue.IssueType.ResinTrap)
                                {
                                    using (var contours =
                                        new VectorOfVectorOfPoint(new VectorOfPoint(issue.Pixels)))
                                    {
                                        CvInvoke.DrawContours(image, contours, -1, new MCvScalar(255), -1);
                                        //CvInvoke.DrawContours(image, contours, -1, new MCvScalar(255), 2);
                                    }

                                    edited = true;
                                }

                            }

                            if (edited)
                            {
                                SlicerFile[layerIssues.Key].LayerMat = image;
                                result = true;
                            }
                        }

                        progress++;
                    });

                    if (layersRemove.Count > 0)
                    {
                        SlicerFile.LayerManager.RemoveLayers(layersRemove);
                        result = true;
                    }

                }
                catch (Exception ex)
                {
                    this.MessageBoxError(ex.Message, "Removal failed");
                }
                finally
                {
                    IsGUIEnabled = true;
                }

                return result;
            });

            await ProgressWindow.ShowDialog(this);

            if (!task.Result) return;

            var whiteListLayers = new List<uint>();

            // Update GUI
            var issueRemoveList = new List<LayerIssue>();
            foreach (LayerIssue issue in IssuesGrid.SelectedItems)
            {
                switch (issue.Type)
                {
                    //if (!issue.HaveValidPoint) continue;
                    case LayerIssue.IssueType.EmptyLayer:
                    case LayerIssue.IssueType.ResinTrap:
                        issueRemoveList.Add(issue);
                        break;
                    case LayerIssue.IssueType.Island:
                    {
                        issueRemoveList.Add(issue);
                        var nextLayer = issue.Layer.Index + 1;
                        if (nextLayer >= SlicerFile.LayerCount) continue;
                        if (whiteListLayers.Contains(nextLayer)) continue;
                        whiteListLayers.Add(nextLayer);
                        break;
                    }
                }

                //Issues.Remove(issue);
                
            }
            
            Issues.RemoveMany(issueRemoveList);

            if (layersRemove.Count > 0)
            {
                ResetDataContext();
            }

            if (Settings.PixelEditor.PartialUpdateIslandsOnEditing)
            { 
                UpdateIslands(whiteListLayers);
            }

            ShowLayer(); // It will call latter so its a extra call
            CanSave = true;
        }

        private async void UpdateIslands(List<uint> whiteListLayers)
        {
            if (whiteListLayers.Count == 0) return;
            var islandConfig = GetIslandDetectionConfiguration();
            var resinTrapConfig = new ResinTrapDetectionConfiguration { Enabled = false };
            var touchingBoundConfig = new TouchingBoundDetectionConfiguration { Enabled = false };
            islandConfig.Enabled = true;
            islandConfig.WhiteListLayers = whiteListLayers;

           
            IsGUIEnabled = false;
            ProgressWindow.SetTitle("Updating Issues");

            
            List<LayerIssue> toRemove = new List<LayerIssue>();
            foreach (var layerIndex in islandConfig.WhiteListLayers)
            {
                foreach (var issue in Issues)
                {
                    if(issue.LayerIndex != layerIndex || issue.Type != LayerIssue.IssueType.Island) continue;
                    toRemove.Add(issue);
                }
            }
            Issues.RemoveMany(toRemove);

            var result = Task<List<LayerIssue>>.Factory.StartNew(() =>
            {
                try
                {
                    var issues = SlicerFile.LayerManager.GetAllIssues(islandConfig, resinTrapConfig,
                        touchingBoundConfig, false,
                        ProgressWindow.RestartProgress());

                    issues.RemoveAll(issue => issue.Type != LayerIssue.IssueType.Island); // Remove all non islands
                    return issues;
                }

                catch (OperationCanceledException)
                {

                }
                catch (Exception ex)
                {
                    this.MessageBoxError(ex.Message, "Error while trying to compute issues");
                }
                finally
                {
                    IsGUIEnabled = true;
                }

                return null;
            });
            
            await ProgressWindow.ShowDialog(this);
            
            if (result.Result is null || result.Result.Count == 0) return;

            Issues.AddRange(result.Result);
            Issues = new ObservableCollection<LayerIssue>(Issues.OrderBy(issue => issue.Type)
                .ThenBy(issue => issue.LayerIndex)
                .ThenBy(issue => issue.PixelsCount).ToList());
        }

        public int IssueSelectedIndex
        {
            get => _issueSelectedIndex;
            set
            {
                if (!SetProperty(ref _issueSelectedIndex, value)) return;
                OnPropertyChanged(nameof(IssueSelectedIndexStr));
                OnPropertyChanged(nameof(IssueCanGoPrevious));
                OnPropertyChanged(nameof(IssueCanGoNext));
            }
        }

        public string IssueSelectedIndexStr => (_issueSelectedIndex+1).ToString().PadLeft(Issues.Count.ToString().Length, '0');

        private void IssuesGridOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            //Debug.WriteLine(IssuesGrid.SelectedIndex);
            //Debug.WriteLine(IssuesGrid.SelectedItems.Count);
            if (IssuesGrid.SelectedItems.Count > 1) return;
            
        }

        private void IssuesGridOnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
        {
            if(!(IssuesGrid.SelectedItem is LayerIssue issue)) return;
            // Double clicking an issue will center and zoom into the 
            // selected issue. Left click on an issue will zoom to fit.

            var pointer = e.PointerPressedEventArgs.GetCurrentPoint(IssuesGrid);

            if (e.PointerPressedEventArgs.ClickCount == 1)
            {
                if (issue.Type == LayerIssue.IssueType.TouchingBound || issue.Type == LayerIssue.IssueType.EmptyLayer ||
                    (issue.X == -1 && issue.Y == -1))
                {
                    ZoomToFit();
                }
                else if (issue.X >= 0 && issue.Y >= 0)
                {
                    if (Settings.LayerPreview.ZoomIssues /*^ (ModifierKeys & Keys.Alt) != 0*/)
                    {
                        ZoomToIssue(issue);
                    }
                    else
                    {
                        //CenterLayerAt(GetTransposedIssueBounds(issue));
                        // If issue is not already visible, center on it and bring it into view.
                        // Issues already in view will not be centered, though their color may
                        // change and the crosshair may move to reflect active selections.

                        if (!LayerImageBox.GetSourceImageRegion().Contains(GetTransposedIssueBounds(issue).ToAvalonia()))
                        {
                            CenterAtIssue(issue);
                        }
                    }
                }

                ForceUpdateActualLayer(issue.LayerIndex);
                return;
            }

            if (e.PointerPressedEventArgs.ClickCount == 2)
            {
                if (pointer.Properties.IsLeftButtonPressed)
                {
                    ZoomToIssue(issue);
                    return;
                }

                if (pointer.Properties.IsRightButtonPressed)
                {
                    ZoomToFit();
                    return;
                }
            }

        }

        public void OnClickDetectIssues()
        {
            if (!IsFileLoaded) return;
            ComputeIssues(
                GetIslandDetectionConfiguration(),
                GetResinTrapDetectionConfiguration(),
                GetTouchingBoundsDetectionConfiguration(),
                Settings.Issues.ComputeEmptyLayers);
        }

        private async void ComputeIssues(IslandDetectionConfiguration islandConfig = null,
            ResinTrapDetectionConfiguration resinTrapConfig = null,
            TouchingBoundDetectionConfiguration touchingBoundConfig = null, bool emptyLayersConfig = true)
        {

            Issues.Clear();
            IsGUIEnabled = false;

            ProgressWindow.SetTitle("Computing Issues");

            var task = Task<List<LayerIssue>>.Factory.StartNew(() =>
            {
                try
                {
                    var issues = SlicerFile.LayerManager.GetAllIssues(islandConfig, resinTrapConfig, touchingBoundConfig,
                        emptyLayersConfig, ProgressWindow.RestartProgress());
                    return issues;
                }
                catch (OperationCanceledException)
                {

                }
                catch (Exception ex)
                {
                    this.MessageBoxError(ex.Message, "Error while trying compute issues");
                }
                finally
                {
                    IsGUIEnabled = true;
                }

                return null;
            });

            await ProgressWindow.ShowDialog(this);
            if (task.Result is null) return;
            Issues.AddRange(task.Result);

            OnPropertyChanged(nameof(IssueSelectedIndexStr));
            OnPropertyChanged(nameof(IssueCanGoPrevious));
            OnPropertyChanged(nameof(IssueCanGoNext));

        }

        public IslandDetectionConfiguration GetIslandDetectionConfiguration()
        {
            return new IslandDetectionConfiguration
            {
                Enabled = Settings.Issues.ComputeIslands,
                AllowDiagonalBonds = Settings.Issues.IslandAllowDiagonalBonds,
                BinaryThreshold = Settings.Issues.IslandBinaryThreshold,
                RequiredAreaToProcessCheck = Settings.Issues.IslandRequiredAreaToProcessCheck,
                RequiredPixelBrightnessToProcessCheck = Settings.Issues.IslandRequiredPixelBrightnessToProcessCheck,
                RequiredPixelsToSupport = Settings.Issues.IslandRequiredPixelsToSupport,
                RequiredPixelBrightnessToSupport = Settings.Issues.IslandRequiredPixelBrightnessToSupport
            };
        }

        public ResinTrapDetectionConfiguration GetResinTrapDetectionConfiguration()
        {
            return new ResinTrapDetectionConfiguration
            {
                Enabled = Settings.Issues.ComputeResinTraps,
                BinaryThreshold = Settings.Issues.ResinTrapBinaryThreshold,
                RequiredAreaToProcessCheck = Settings.Issues.ResinTrapRequiredAreaToProcessCheck,
                RequiredBlackPixelsToDrain = Settings.Issues.ResinTrapRequiredBlackPixelsToDrain,
                MaximumPixelBrightnessToDrain = Settings.Issues.ResinTrapMaximumPixelBrightnessToDrain
            };
        }

        public TouchingBoundDetectionConfiguration GetTouchingBoundsDetectionConfiguration()
        {
            return new TouchingBoundDetectionConfiguration
            {
                Enabled = Settings.Issues.ComputeTouchingBounds,
                //MaximumPixelBrightness = 100
            };
        }

        #endregion

        #region Log
        public bool IsVerbose
        {
            get => _isVerbose;
            set => SetProperty(ref _isVerbose, value);
        }

        public void AddLog(LogItem log)
        {
            log.Index = Logs.Count;
            Logs.Insert(0, log);
        }

        public void AddLog(string description, double elapsedTime = 0) =>
            AddLog(new LogItem(Logs.Count, description, elapsedTime));


        public void AddLogVerbose(string description, double elapsedTime = 0)
        {
            Debug.WriteLine($"{description} ({elapsedTime}s)");
            if (!_isVerbose) return;
            AddLog(description, elapsedTime);
        }
        #endregion

        #region Layer Preview

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
        public string ActualLayerTooltip => SlicerFile is null ? "???" : $"{SlicerFile.GetHeightFromLayer(ActualLayer):0.00}mm\n{ActualLayer}\n{(ActualLayer+1) * 100 / (SlicerFile.LayerCount)}%";

        public uint SliderMaximumValue => SlicerFile?.LayerCount - 1 ?? 0;

        public bool CanGoUp => _actualLayer < SliderMaximumValue;
        public bool CanGoDown => _actualLayer > 0;

        public string LayerPixelCountStr
        {
            get
            {
                if (!LayerCache.IsCached) return "0";
                var pixelPercent =
                    Math.Round(
                        LayerCache.Layer.NonZeroPixelCount * 100.0 / (SlicerFile.ResolutionX * SlicerFile.ResolutionY), 2);
                return $"{LayerCache.Layer.NonZeroPixelCount} ({pixelPercent}%)";
            }
        }

        public string LayerBoundsStr => LayerCache.Layer is null ? "NS" : LayerCache.Layer.BoundingRectangle.ToString();
        public string LayerROIStr => LayerCache.Layer is null ? "NS" : "TODO";

        public long ShowLayerRenderMs
        {
            get => _showLayerRenderMs;
            set => SetProperty(ref _showLayerRenderMs, value);
        }

        public PixelPicker LayerPixelPicker { get; } = new PixelPicker();

        public string LayerZoomStr => $"{LayerImageBox.Zoom / 100}x" +
                                      (AppSettings.LockedZoomLevel == LayerImageBox.Zoom ? "🔒" : string.Empty);
        public string LayerResolutionStr => SlicerFile?.Resolution.ToString() ?? "Unloaded";

        public uint ActualLayer
        {
            get => _actualLayer;
            set
            {
                if (!SetProperty(ref _actualLayer, value)) return;
                ShowLayer();
                InvalidateLayerNavigation();
            }
        }

        public void ForceUpdateActualLayer(uint layerIndex = 0)
        {
            _actualLayer = layerIndex;
            ShowLayer();
            InvalidateLayerNavigation();
            OnPropertyChanged(nameof(ActualLayer));
        }

        public void InvalidateLayerNavigation()
        {
            OnPropertyChanged(nameof(CanGoDown));
            OnPropertyChanged(nameof(CanGoUp));
            OnPropertyChanged(nameof(ActualLayerTooltip));
            OnPropertyChanged(nameof(LayerNavigationTooltipMargin));
            OnPropertyChanged(nameof(LayerPixelCountStr));
            OnPropertyChanged(nameof(LayerBoundsStr));
        }

        public Thickness LayerNavigationTooltipMargin
        {
            get
            {
                double top = 0;
                if (LayerSlider?.Track != null)
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

        #endregion

        public LayerCache LayerCache = new LayerCache();
        private ObservableCollection<LayerIssue> _issues = new ObservableCollection<LayerIssue>();

        #endregion

        #region Constructors

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            //this.AttachDevTools();
#endif
            App.ThemeSelector?.EnableThemes(this);
            LayerImageBox = this.FindControl<AdvancedImageBox>("Layer.Image");
            LayerSlider = this.FindControl<SliderEx>("Layer.Navigation.Slider");
            LayerNavigationTooltipPanel = this.FindControl<Panel>("Layer.Navigation.Tooltip.Panel");
            LayerNavigationTooltipBorder = this.FindControl<Border>("Layer.Navigation.Tooltip.Border");
            IssuesGrid = this.FindControl<DataGrid>("IssuesGrid");
            IssuesGrid.SelectionChanged += IssuesGridOnSelectionChanged;
            IssuesGrid.CellPointerPressed += IssuesGridOnCellPointerPressed;
            
            _showLayerImageDifference = Settings.LayerPreview.ShowLayerDifference;
            _showLayerOutlinePrintVolumeBoundary = Settings.LayerPreview.VolumeBoundsOutline;
            _showLayerOutlineLayerBoundary = Settings.LayerPreview.LayerBoundsOutline;
            _showLayerOutlineHollowAreas = Settings.LayerPreview.HollowOutline;

            /*LayerSlider.PropertyChanged += (sender, args) =>
            {
                Debug.WriteLine(args.Property.Name);
                if (args.Property.Name == nameof(LayerSlider.Value))
                {
                    LayerNavigationTooltipPanel.Margin = LayerNavigationTooltipMargin;
                    return;
                }
            };*/
            //PropertyChanged += OnPropertyChanged;
            DataContext = this;
            
            AddHandler(DragDrop.DropEvent, (sender, e) =>
            {
                ProcessFiles(e.Data.GetFileNames().ToArray());
            });
            
            UpdateTitle();

            AddLog($"{About.Software} start");
            ProcessFiles(Program.Args);
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

        public void MenuFileOpenClicked() => OpenFile();
        public void MenuFileOpenNewWindowClicked() => OpenFile(true);

        public async void MenuFileSaveClicked()
        {
            if (!CanSave) return;
            await SaveFile();
        }

        public async void MenuFileSaveAsClicked()
        {
            if (!IsFileLoaded) return;
            var ext = Path.GetExtension(SlicerFile.FileFullPath);
            var extNoDot = ext.Remove(0, 1);
            SaveFileDialog dialog = new SaveFileDialog
            {
                DefaultExtension = extNoDot,
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter
                    {
                        Name = $"{ext} Files",
                        Extensions = new List<string>
                        {
                            extNoDot
                        }
                    }
                },
                Directory = string.IsNullOrEmpty(Settings.General.DefaultDirectorySaveFile)
                    ? Path.GetDirectoryName(SlicerFile.FileFullPath)
                    : Settings.General.DefaultDirectorySaveFile,
                InitialFileName = $"{Settings.General.FileSaveNamePrefix}{Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath)}{Settings.General.FileSaveNameSuffix}"
            };
            var file = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(file)) return;
            await SaveFile(file);
        }

        public async void OpenFile(bool newWindow = false)
        {
            var dialog = new OpenFileDialog
            {
                AllowMultiple = true,
                Filters = Helpers.ToAvaloniaFileFilter(FileFormat.AllFileFiltersAvalonia),
            };
            var files = await dialog.ShowAsync(this);
            ProcessFiles(files, newWindow);
        }

        public void CloseFile()
        {
            if (SlicerFile is null) return;
            SlicerFile?.Dispose();
            App.SlicerFile = null;
            _actualLayer = 0;
            LayerCache.Clear();
            VisibleThumbnailIndex = 0;
            LayerImageBox.Image = null;
            SlicerProperties.Clear();
            Issues.Clear();
            Drawings.Clear();
            ResetDataContext();
        }

        public async void MenuFileSettingsClicked()
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            await settingsWindow.ShowDialog(this);
        }

        public void OpenWebsite()
        {
            App.OpenBrowser(About.Website);
        }

        public void OpenDonateWebsite()
        {
            App.OpenBrowser(About.Donate);
        }

        public async void MenuHelpAboutClicked()
        {
            await new AboutWindow().ShowDialog(this);
        }

        #endregion

        #region Methods

        private void UpdateTitle()
        {
            Title = SlicerFile is null
                ? $"{About.Software}   Version: {AppSettings.Version}"
                : $"{About.Software}   File: {Path.GetFileName(SlicerFile.FileFullPath)} ({Math.Round(LastStopWatch.ElapsedMilliseconds / 1000m, 2)}s)   Version: {AppSettings.Version}";

#if DEBUG
            Title += "   [DEBUG]";
#endif
        }

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

                App.NewInstance(files[i]);

            }
        }

        void ReloadFile() => ReloadFile(_actualLayer);

        void ReloadFile(uint actualLayer)
        {
            if (App.SlicerFile is null) return;
            ProcessFile(SlicerFile.FileFullPath, _actualLayer);
        }

        async void ProcessFile(string fileName, uint actualLayer = 0)
        {
            if (!File.Exists(fileName)) return;
            CloseFile();
            var fileNameOnly = Path.GetFileName(fileName);
            App.SlicerFile = FileFormat.FindByExtension(fileName, true, true);
            if (SlicerFile is null) return;

            ProgressWindow.SetTitle($"Opening: {fileNameOnly}");
            IsGUIEnabled = false;
            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    SlicerFile.Decode(fileName, ProgressWindow.RestartProgress());
                    return true;
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    this.MessageBoxError(exception.ToString(), "Error opening the file");
                }
                finally
                {
                    IsGUIEnabled = true;
                }

                return false;
            });

            await ProgressWindow.ShowDialog<DialogResults>(this);
            if (!task.Result)
            {
                SlicerFile.Dispose();
                App.SlicerFile = null;
                return;
            }

            if (SlicerFile.LayerCount == 0)
            {
                await this.MessageBoxError("It seems this file has no layers.  Possible causes could be:\n" +
                                "- File is empty\n" +
                                "- File is corrupted\n" +
                                "- File has not been sliced\n" +
                                "- An internal programing error\n\n" +
                                "Please check your file and retry", "Error reading file");
                SlicerFile.Dispose();
                App.SlicerFile = null;
                return;
            }

            if (!(SlicerFile.ConvertToFormats is null))
            {
                List<MenuItem> menuItems = new List<MenuItem>();
                foreach (var fileFormatType in SlicerFile.ConvertToFormats)
                {
                    FileFormat fileFormat = FileFormat.FindByType(fileFormatType);

                    string extensions = fileFormat.FileExtensions.Length > 0
                        ? $" ({fileFormat.GetFileExtensions()})"
                        : string.Empty;

                    var menuItem = new MenuItem
                    {
                        Header = fileFormat.GetType().Name.Replace("File", extensions),
                        Tag = fileFormat
                    };

                    menuItem.Tapped += ConvertToOnTapped;

                    menuItems.Add(menuItem);
                }

                MenuFileConvertItems = menuItems.ToArray();
            }

            using Mat mat = SlicerFile[0].LayerMat;

            VisibleThumbnailIndex = 1;

            if (!(SlicerFile.Configs is null))
            {
                foreach (var config in SlicerFile.Configs)
                {
                    var type = config.GetType();
                    foreach (var property in type.GetProperties())
                    {
                        SlicerProperties.Add(new SlicerProperty(property.Name, property.GetValue(config, null)?.ToString(), type.Name));
                    }
                }
            }
            
            UpdateTitle();

            
            if (!(mat is null) && Settings.LayerPreview.AutoRotateLayerBestView)
            {
               _showLayerImageRotated = mat.Height > mat.Width;
            }

            ResetDataContext();

            ForceUpdateActualLayer(actualLayer.Clamp(actualLayer, SliderMaximumValue));
            
            if (Settings.LayerPreview.ZoomToFitPrintVolumeBounds)
            {
                ZoomToFit();
            }
        }

        private async void ConvertToOnTapped(object? sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem item)) return;
            if (!(item.Tag is FileFormat fileFormat)) return;

            SaveFileDialog dialog = new SaveFileDialog
            {
                InitialFileName = Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath),
                Filters = Helpers.ToAvaloniaFileFilter(fileFormat.FileFilterAvalonia),
                Directory = string.IsNullOrEmpty(Settings.General.DefaultDirectoryConvertFile)
                    ? Path.GetDirectoryName(SlicerFile.FileFullPath)
                    : Settings.General.DefaultDirectoryConvertFile
            };

            var result = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(result)) return;


            IsGUIEnabled = false;
            ProgressWindow.SetTitle(
                $"Converting {Path.GetFileName(SlicerFile.FileFullPath)} to {Path.GetExtension(result)}");

            Task<bool> task = Task<bool>.Factory.StartNew(() =>
            {
                try
                {
                    return SlicerFile.Convert(fileFormat, result, ProgressWindow.RestartProgress());
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    string extraMessage = string.Empty;
                    if (SlicerFile.FileFullPath.EndsWith(".sl1"))
                    {
                        extraMessage = "Note: When converting from SL1 make sure you have the correct printer selected, you MUST use a UVtools base printer.\n" +
                                       "Go to \"Help\" -> \"Install profiles into PrusaSlicer\" to install printers.\n";
                    }

                    this.MessageBoxError($"Convertion was not successful! Maybe not implemented...\n{extraMessage}{ex.Message}", "Convertion unsuccessful");
                }
                finally
                {
                    IsGUIEnabled = true;
                }
                
                return false;
            });

            await ProgressWindow.ShowDialog(this);

            if (task.Result)
            {
                if (await this.MessageBoxQuestion(
                    $"Conversion completed in {LastStopWatch.ElapsedMilliseconds / 1000}s\n\n" +
                    $"Do you want to open {Path.GetFileName(result)} in a new window?",
                    "Conversion complete") == ButtonResult.Yes)
                {
                    App.NewInstance(result);
                }
            }
            else
            {
                try
                {
                    if (File.Exists(result))
                    {
                        File.Delete(result);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
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
        unsafe void ShowLayer()
        {
            if (SlicerFile is null) return;
            
            Stopwatch watch = Stopwatch.StartNew();
            LayerCache.Layer = SlicerFile[_actualLayer];

            try
            {
                //var imageSpan = LayerCache.Image.GetPixelSpan<byte>();
                //var imageBgrSpan = LayerCache.ImageBgr.GetPixelSpan<byte>();

                var imageSpan = LayerCache.ImageSpan;
                var imageBgrSpan = LayerCache.ImageBgrSpan;

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

                
                var selectedIssues = IssuesGrid.SelectedItems;
                
                if (_showLayerImageIssues && Issues.Count > 0)
                {
                    foreach (var issue in Issues)
                    {
                        if (issue.LayerIndex != ActualLayer) continue;
                        if (!issue.HaveValidPoint) continue;

                        Color color = Color.Empty;

                        if (issue.Type == LayerIssue.IssueType.ResinTrap)
                        {
                            color = selectedIssues.Contains(issue)
                                ? Settings.LayerPreview.ResinTrapHighlightColor
                                : Settings.LayerPreview.ResinTrapColor;


                            using (var vec = new VectorOfVectorOfPoint(new VectorOfPoint(issue.Pixels)))
                            {
                                CvInvoke.DrawContours(LayerCache.ImageBgr, vec, -1,
                                    new MCvScalar(color.B, color.G, color.R), -1);
                            }

                            if (_showLayerImageCrosshairs &&
                                !Settings.LayerPreview.CrosshairShowOnlyOnSelectedIssues &&
                                LayerImageBox.Zoom <= AppSettings.CrosshairFadeLevel)
                            {
                                DrawCrosshair(issue.BoundingRectangle);
                            }

                            continue;
                        }

                        switch (issue.Type)
                        {
                            case LayerIssue.IssueType.Island:
                                color = selectedIssues.Contains(issue)
                                    ? Settings.LayerPreview.IslandHighlightColor
                                    : Settings.LayerPreview.IslandColor;
                                if (_showLayerImageCrosshairs &&
                                    !Settings.LayerPreview.CrosshairShowOnlyOnSelectedIssues &&
                                    LayerImageBox.Zoom <= AppSettings.CrosshairFadeLevel)
                                {
                                    DrawCrosshair(issue.BoundingRectangle);
                                }

                                break;
                            case LayerIssue.IssueType.TouchingBound:
                                color = Settings.LayerPreview.TouchingBoundsColor;
                                break;
                        }

                        foreach (var pixel in issue)
                        {
                            int pixelPos = LayerCache.Image.GetPixelPos(pixel);
                            byte brightness = imageSpan[pixelPos];
                            if (brightness == 0) continue;

                            int pixelBgrPos = pixelPos * LayerCache.ImageBgr.NumberOfChannels;

                            var newColor = color.FactorColor(brightness, 80);

                            imageBgrSpan[pixelBgrPos] = newColor.B; // B
                            imageBgrSpan[pixelBgrPos + 1] = newColor.G; // G
                            imageBgrSpan[pixelBgrPos + 2] = newColor.R; // R
                        }
                    }
                }
    
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


                LayerImageBox.Image = LayerCache.ImageBgr.ToBitmap();

                watch.Stop();
                ShowLayerRenderMs = watch.ElapsedMilliseconds;
                AddLogVerbose($"Show Layer: {_actualLayer}", watch.Elapsed.TotalSeconds);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// Draw a crosshair around a rectangle
        /// </summary>
        /// <param name="rect"></param>
        public void DrawCrosshair(Rectangle rect)
        {
            // Gradually increase line thickness from 1 to 3 at the lower-end of the zoom range.
            // This prevents the crosshair lines from disappearing due to being too thin to
            // render at very low zoom factors.
            var lineThickness = (LayerImageBox.Zoom > 100) ? 1 : (LayerImageBox.Zoom < 50) ? 3 : 2;
            var color = new MCvScalar(Settings.LayerPreview.CrosshairColor.B, Settings.LayerPreview.CrosshairColor.G,
                Settings.LayerPreview.CrosshairColor.R);


            // LEFT
            var startPoint = new System.Drawing.Point(Math.Max(0, rect.X - Settings.LayerPreview.CrosshairMargin - 1),
                rect.Y + rect.Height / 2);
            var endPoint =
                new System.Drawing.Point(
                    Settings.LayerPreview.CrosshairLength == 0
                        ? 0
                        : (int)Math.Max(0, startPoint.X - Settings.LayerPreview.CrosshairLength + 1),
                    startPoint.Y);

            CvInvoke.Line(LayerCache.ImageBgr,
                startPoint,
                endPoint,
                color,
                lineThickness);


            // RIGHT
            startPoint.X = Math.Min(LayerCache.ImageBgr.Width,
                rect.Right + Settings.LayerPreview.CrosshairMargin);
            endPoint.X = Settings.LayerPreview.CrosshairLength == 0
                ? LayerCache.ImageBgr.Width
                : (int)Math.Min(LayerCache.ImageBgr.Width, startPoint.X + Settings.LayerPreview.CrosshairLength - 1);

            CvInvoke.Line(LayerCache.ImageBgr,
                startPoint,
                endPoint,
                color,
                lineThickness);

            // TOP
            startPoint = new System.Drawing.Point(rect.X + rect.Width / 2,
                Math.Max(0, rect.Y - Settings.LayerPreview.CrosshairMargin - 1));
            endPoint = new System.Drawing.Point(startPoint.X,
                (int)(Settings.LayerPreview.CrosshairLength == 0
                    ? 0
                    : Math.Max(0, startPoint.Y - Settings.LayerPreview.CrosshairLength + 1)));


            CvInvoke.Line(LayerCache.ImageBgr,
                startPoint,
                endPoint,
                color,
                lineThickness);

            // Bottom
            startPoint.Y = Math.Min(LayerCache.ImageBgr.Height, rect.Bottom + Settings.LayerPreview.CrosshairMargin);
            endPoint.Y = Settings.LayerPreview.CrosshairLength == 0
                ? LayerCache.ImageBgr.Height
                : (int)Math.Min(LayerCache.ImageBgr.Height, startPoint.Y + Settings.LayerPreview.CrosshairLength - 1);

            CvInvoke.Line(LayerCache.ImageBgr,
                startPoint,
                endPoint,
                color,
                lineThickness);
        }

        public async Task<bool> SaveFile(string filepath = null)
        {
            if (filepath is null)
            {
                if (SavesCount == 0 && Settings.General.PromptOverwriteFileSave)
                {
                    var result = await this.MessageBoxQuestion(
                        "Original input file will be overwritten.  Do you wish to proceed?", "Overwrite file?");

                    if(result != ButtonResult.Yes) return false;
                }

                filepath = SlicerFile.FileFullPath;
            }

            var oldFile = SlicerFile.FileFullPath;
            var tempFile = filepath + FileFormat.TemporaryFileAppend;

            IsGUIEnabled = false;
            ProgressWindow.SetTitle($"Saving {Path.GetFileName(filepath)}");

            var task = Task<bool>.Factory.StartNew( () =>
            {
                bool result = false;

                try
                {
                    SlicerFile.SaveAs(tempFile, ProgressWindow.RestartProgress());
                    if (File.Exists(filepath))
                    {
                        File.Delete(filepath);
                    }
                    File.Move(tempFile, filepath);
                    SlicerFile.FileFullPath = filepath;
                    result = true;
                }
                catch (OperationCanceledException)
                {
                    SlicerFile.FileFullPath = oldFile;
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                catch (Exception ex)
                {
                    this.MessageBoxError(ex.Message, "Error while saving the file");
                }
                finally
                {
                    IsGUIEnabled = true;
                }

                return result;
            });

            await ProgressWindow.ShowDialog(this);

            if (task.Result)
            {
                SavesCount++;
                CanSave = false;
                UpdateTitle();
            }

            return task.Result;
        }

        public async void ExtractFile()
        {
            if (!IsFileLoaded) return;
            string fileNameNoExt = Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath);
            OpenFolderDialog dialog = new OpenFolderDialog
            {
                Directory = string.IsNullOrEmpty(Settings.General.DefaultDirectoryExtractFile)
                    ? Path.GetDirectoryName(SlicerFile.FileFullPath)
                    : Settings.General.DefaultDirectoryExtractFile,
                Title =
                    $"A \"{fileNameNoExt}\" folder will be created within your selected folder to dump the contents."
            };

            var result = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(result)) return;

            string finalPath = Path.Combine(result, fileNameNoExt);

            try
            {
                IsGUIEnabled = false;
                ProgressWindow.SetTitle($"Extracting {Path.GetFileName(SlicerFile.FileFullPath)}");

                var task = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        SlicerFile.Extract(finalPath, true, true, ProgressWindow.RestartProgress());
                    }
                    catch (OperationCanceledException)
                    {

                    }
                    finally
                    {
                        IsGUIEnabled = true;
                    }
                });

                await ProgressWindow.ShowDialog(this);
                

                if (await this.MessageBoxQuestion(
                        $"Extraction to {finalPath} completed in ({LastStopWatch.ElapsedMilliseconds / 1000}s)\n\n" +
                        "'Yes' to open target folder, 'No' to continue.",
                        "Extraction complete") == ButtonResult.Yes)
                {
                    App.StartProcess(finalPath);
                }
            }
            catch (Exception ex)
            {
                await this.MessageBoxError(ex.ToString(), "Error while try extracting the file");
            }

        }

        #region Zoom
        public Point GetTransposedPoint(Point point, bool clockWise = true)
        {
            if (!_showLayerImageRotated) return point;
            return clockWise
                ? new Point(point.Y, LayerCache.Image.Height - 1 - point.X)
                : new Point(LayerCache.Image.Height - 1 - point.Y, point.X);
        }

        public Rectangle GetTransposedRectangle(RectangleF rectangleF, bool clockWise = true, bool ignoreLayerRotation = false) =>
            GetTransposedRectangle(Rectangle.Round(rectangleF), clockWise, ignoreLayerRotation);

        public Rectangle GetTransposedRectangle(Rectangle rectangle, bool clockWise = true, bool ignoreLayerRotation = false)
        {
            if (rectangle.IsEmpty || (!ignoreLayerRotation && !_showLayerImageRotated)) return rectangle;
            return clockWise
                ? new Rectangle(LayerCache.Image.Height - rectangle.Bottom,
                    rectangle.Left, rectangle.Height, rectangle.Width)
                //: new Rectangle(ActualLayerImage.Width - rectangle.Bottom, rectangle.Left, rectangle.Width, rectangle.Height);
                //: new Rectangle(ActualLayerImage.Width - rectangle.Bottom, ActualLayerImage.Height-rectangle.Right, rectangle.Width, rectangle.Height); // Rotate90FlipX: // = Rotate270FlipY
                //: new Rectangle(rectangle.Top, rectangle.Left, rectangle.Width, rectangle.Height); // Rotate270FlipX:  // = Rotate90FlipY
                : new Rectangle(rectangle.Top, LayerCache.Image.Height - rectangle.Right, rectangle.Height, rectangle.Width); // Rotate90FlipNone:  // = Rotate270FlipXY
        }

        /// <summary>
        /// Gets the bounding rectangle of the passed issue, automatically adjusting
        /// the coordinates and width/height to account for whether or not the layer
        /// preview image is rotated.  Used to ensure images are properly zoomed or
        /// centered independent of the layer preview rotation.
        /// </summary>
        private Rectangle GetTransposedIssueBounds(LayerIssue issue)
        {
            if (issue.X >= 0 && issue.Y >= 0 && (issue.BoundingRectangle.IsEmpty || issue.Size == 1) &&
                _showLayerImageRotated)
                return new Rectangle(LayerCache.Image.Height - 1 - issue.Y,
                    issue.X, 1, 1);

            return GetTransposedRectangle(issue.BoundingRectangle);
        }

        /// <summary>
        /// Centers layer view on a X,Y coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">X coordinate</param>
        /// <param name="zoomLevel">Zoom level to set, 0 to ignore or negative value to get current locked zoom level</param>
        public void CenterLayerAt(double x, double y, int zoomLevel = 0)
        {
            if (zoomLevel < 0) zoomLevel = AppSettings.LockedZoomLevel;
            if (zoomLevel > 0) LayerImageBox.Zoom = zoomLevel;
            LayerImageBox.CenterAt(x, y);
        }

        /// <summary>
        /// Centers layer view on a X,Y coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">X coordinate</param>
        /// <param name="zoomLevel">Zoom level to set, 0 to ignore or negative value to get current locked zoom level</param>
        public void CenterLayerAt(int x, int y, int zoomLevel = 0)
        {
            if (zoomLevel < 0) zoomLevel = AppSettings.LockedZoomLevel;
            if (zoomLevel > 0) LayerImageBox.Zoom = zoomLevel;
            LayerImageBox.CenterAt(x, y);
        }


        public void CenterLayerAt(Rectangle rectangle, int zoomLevel = 0, bool zoomToRegion = false)
        {
            var viewPort = LayerImageBox.GetSourceImageRegion();
            if (zoomToRegion ||
                rectangle.Width * AppSettings.LockedZoomLevel / LayerImageBox.Zoom > viewPort.Width ||
                rectangle.Height * AppSettings.LockedZoomLevel / LayerImageBox.Zoom > viewPort.Height)
            {
                Debug.WriteLine("zoom to region");
                //SupressLayerZoomEvent = true;
                LayerImageBox.ZoomToRegion(rectangle);
                //SupressLayerZoomEvent = false;
                //pbLayer.ZoomOut(true);
                return;
            }
            Debug.WriteLine($"Center at {zoomLevel}");
            CenterLayerAt(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2, zoomLevel);
        }

        /// <summary>
        /// Centers layer view on a <see cref="Point"/>
        /// </summary>
        /// <param name="point">Point holding X and Y coordinates</param>
        /// <param name="zoomLevel">Zoom level to set, 0 to ignore or negative value to get current locked zoom level</param>
        public void CenterLayerAt(System.Drawing.Point point, int zoomLevel = 0) => CenterLayerAt(point.X, point.Y, zoomLevel);


        /// <summary>
        /// Zoom the layer preview to the passed issue, or if appropriate for issue type,
        /// Zoom to fit the plate or print bounds.
        /// </summary>
        private void ZoomToIssue(LayerIssue issue)
        {
            if (issue.Type == LayerIssue.IssueType.TouchingBound || issue.Type == LayerIssue.IssueType.EmptyLayer ||
                (issue.X == -1 && issue.Y == -1))
            {
                ZoomToFit();
                return;
            }

            if (issue.X >= 0 && issue.Y >= 0)
            {
                // Check to see if this zoom action will cross the crosshair fade threshold
                /*if (tsLayerImageShowCrosshairs.Checked && !ReferenceEquals(Issues, null) && flvIssues.SelectedIndices.Count > 0
                   && pbLayer.Zoom <= CrosshairFadeLevel && LockedZoomLevel > CrosshairFadeLevel)
                {
                    // Refresh the preview without the crosshairs before zooming-in.
                    // Prevents zoomed-in crosshairs from breifly being displayed before
                    // the Layer Preview is refreshed post-zoom.
                    tsLayerImageShowCrosshairs.Checked = false;
                    ShowLayer();
                    tsLayerImageShowCrosshairs.Checked = true;
                }*/
                

                CenterLayerAt(GetTransposedIssueBounds(issue), AppSettings.LockedZoomLevel);

            }
        }

        /// <summary>
        /// Center the layer preview on the passed issue, or if appropriate for issue type,
        /// Zoom to fit the plate or print bounds.
        /// </summary>
        private void CenterAtIssue(LayerIssue issue)
        {
            if (issue.Type == LayerIssue.IssueType.TouchingBound || issue.Type == LayerIssue.IssueType.EmptyLayer ||
                (issue.X == -1 && issue.Y == -1))
            {
                ZoomToFit();
            }

            if (issue.X >= 0 && issue.Y >= 0)
            {
                CenterLayerAt(GetTransposedIssueBounds(issue));
            }
        }

        public void ZoomToFitSimple()
        {
            LayerImageBox.ZoomToFit();
        }

        public void ZoomToFitPrintVolume()
        {
            LayerImageBox.ZoomToRegion(SlicerFile.LayerManager.BoundingRectangle);
        }

        private void ZoomToFit()
        {
            if (ReferenceEquals(SlicerFile, null)) return;

            // If ALT key is pressed when ZoomToFit is performed, the configured option for 
            // zoom to plate vs. zoom to print bounds will be inverted.
            
            if (Settings.LayerPreview.ZoomToFitPrintVolumeBounds /*^ (Application. & KeyModifiers.Alt) != 0*/)
            {
                if (!_showLayerImageRotated)
                {
                    LayerImageBox.ZoomToRegion(SlicerFile.LayerManager.BoundingRectangle);
                }
                else
                {
                    LayerImageBox.ZoomToRegion(LayerCache.Image.Height - 1 - SlicerFile.LayerManager.BoundingRectangle.Bottom,
                        SlicerFile.LayerManager.BoundingRectangle.X,
                        SlicerFile.LayerManager.BoundingRectangle.Height,
                        SlicerFile.LayerManager.BoundingRectangle.Width
                    );
                }
            }
            else
            {
                LayerImageBox.ZoomToFit();
            }
        }

        /// <summary>
        /// If there is an issue under the point location passed, that issue will be selected and
        /// scrolled into view on the IssueList.
        /// </summary>
        private void SelectIssueAtPoint(System.Drawing.Point location)
        {
            //location = GetTransposedPoint(location);
            // If location clicked is within an issue, activate it.
            for (var i = 0; i < Issues.Count; i++)
            {
                LayerIssue issue = Issues[i];

                if (issue.LayerIndex != ActualLayer) continue;
                if (!GetTransposedIssueBounds(issue).Contains(location)) continue;

                IssueSelectedIndex = i;
                break;
            }
        }
        #endregion

        #endregion
    }
}
