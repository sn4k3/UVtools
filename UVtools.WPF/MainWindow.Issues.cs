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
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using MessageBox.Avalonia.Enums;
using UVtools.Core;
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;
using Brushes = Avalonia.Media.Brushes;

namespace UVtools.WPF
{
    public partial class MainWindow
    {
        #region Members
        private RangeObservableCollection<LayerIssue> _issues = new();
        private bool _firstTimeOnIssues = true;
        public DataGrid IssuesGrid;

        private int _issueSelectedIndex = -1;
        #endregion

        #region Properties
        public RangeObservableCollection<LayerIssue> Issues
        {
            get => _issues;
            private set => RaiseAndSetIfChanged(ref _issues, value);
        }

        public readonly List<LayerIssue> IgnoredIssues = new();
        private uint _resinTrapDetectionStartLayer;

        public bool IssueCanGoPrevious => Issues.Count > 0 && _issueSelectedIndex > 0;
        public bool IssueCanGoNext => Issues.Count > 0 && _issueSelectedIndex < Issues.Count - 1;

        public uint ResinTrapDetectionStartLayer
        {
            get => _resinTrapDetectionStartLayer;
            set => RaiseAndSetIfChanged(ref _resinTrapDetectionStartLayer, value);
        }

        #endregion

        #region Methods

        public void InitIssues()
        {
            IssuesGrid = this.FindControl<DataGrid>("IssuesGrid");
            IssuesGrid.CellPointerPressed += IssuesGridOnCellPointerPressed;
            IssuesGrid.SelectionChanged += IssuesGridOnSelectionChanged;
            IssuesGrid.KeyUp += IssuesGridOnKeyUp;
            Issues.CollectionChanged += (sender, e) =>
            {
                UpdateLayerTrackerHighlightIssues();
            };
        }

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

        public List<LayerIssue> GetOverlappingIssues(LayerIssue targetIssue, int indexOffset)
        {
            var retValue = new List<LayerIssue>();

            var targetLayerIndex = targetIssue.LayerIndex + indexOffset;
            if (targetLayerIndex > SlicerFile.LayerCount - 1 || targetLayerIndex < 0) return retValue;

            foreach (var candidate in Issues.Where(candidate => candidate.LayerIndex == targetLayerIndex && candidate.Type == LayerIssue.IssueType.SuctionCup))
            {
                using var vec1 = new VectorOfVectorOfPoint(targetIssue.Contours);
                using var vec2 = new VectorOfVectorOfPoint(candidate.Contours);
                if (!EmguContours.ContoursIntersect(vec1, vec2)) continue;
                retValue.Add(candidate);
                break;
            }

            return retValue;
        }

        public async void OnClickIssueRemove()
        {
            if (IssuesGrid.SelectedItems.Count == 0) return;

            if (await this.MessageBoxQuestion($"Are you sure you want to remove all selected {IssuesGrid.SelectedItems.Count} issues?\n\n" +
                                    "Warning: Removing an island can cause other issues to appear if there is material present in the layers above it.\n" +
                                    "Always check previous and next layers before performing an island removal.", $"Remove {IssuesGrid.SelectedItems.Count} Issues?") != ButtonResult.Yes) return;

            Dictionary<uint, List<LayerIssue>> processIssues = new();
            List<uint> layersRemove = new();


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
            ShowProgressWindow("Removing selected issues", false);

            Clipboard.Snapshot();

            var task = await Task.Factory.StartNew(() =>
            {
                Progress.Reset("Removing selected issues", (uint)processIssues.Count);
                bool result = false;
                try
                {
                    Parallel.ForEach(processIssues, CoreSettings.ParallelOptions, layerIssues =>
                    {
                        if (Progress.Token.IsCancellationRequested) return;
                        using (var image = SlicerFile[layerIssues.Key].LayerMat)
                        {
                            var bytes = image.GetDataSpan<byte>();

                            bool edited = false;
                            bool hasSuctionCups = false;
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
                                    using var contours = new VectorOfVectorOfPoint(issue.Contours);
                                    CvInvoke.DrawContours(image, contours, -1, EmguExtensions.WhiteColor, -1);
                                    if (Settings.LayerRepair.ResinTrapsOverlapBy > 0)
                                    {
                                        CvInvoke.DrawContours(image, contours, -1, EmguExtensions.WhiteColor, Settings.LayerRepair.ResinTrapsOverlapBy * 2 + 1);
                                    }
                                    edited = true;
                                }
                                else if (issue.Type == LayerIssue.IssueType.SuctionCup)
                                {
                                    hasSuctionCups = true;
                                }
                            }

                            if (edited)
                            {
                                SlicerFile[layerIssues.Key].LayerMat = image;
                            }

                            if (edited || hasSuctionCups)
                            {
                                result = true;
                            }
                        }

                        Progress.LockAndIncrement();
                    });

                    if (layersRemove.Count > 0)
                    {
                        OperationLayerRemove.RemoveLayers(SlicerFile, layersRemove);
                        result = true;
                    }

                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.InvokeAsync(async () =>
                        await this.MessageBoxError(ex.ToString(), "Removal failed"));
                }

                return result;
            });

            IsGUIEnabled = true;

            if (!task)
            {
                Clipboard.RestoreSnapshot();
                return;
            }

            var whiteListLayers = new List<uint>();

            // Update GUI
            var issueRemoveList = new List<LayerIssue>();
            foreach (LayerIssue issue in IssuesGrid.SelectedItems)
            {
                if (issue.Type != LayerIssue.IssueType.Island &&
                    issue.Type != LayerIssue.IssueType.ResinTrap &&
                    issue.Type != LayerIssue.IssueType.EmptyLayer &&
                    issue.Type != LayerIssue.IssueType.SuctionCup) continue;

                if (issue.Type == LayerIssue.IssueType.Island)
                {
                    var nextLayer = issue.Layer.Index + 1;
                    if (nextLayer >= SlicerFile.LayerCount) continue;
                    if (whiteListLayers.Contains(nextLayer)) continue;
                    whiteListLayers.Add(nextLayer);
                }

                if (issue.Type == LayerIssue.IssueType.SuctionCup)
                {
                    if (issueRemoveList.Contains(issue)) continue;

                    var upDirection = new Stack<LayerIssue>();
                    var downDirection = new Stack<LayerIssue>();
                    upDirection.Push(issue);
                    downDirection.Push(issue);

                    while (upDirection.Count > 0)
                    {
                        var current = upDirection.Pop();
                        foreach(var iss in GetOverlappingIssues(current,1))
                        {
                            upDirection.Push(iss);
                            issueRemoveList.Add(iss);
                        }
                    }
                    while(downDirection.Count > 0)
                    {
                        var current = downDirection.Pop();
                        foreach (var iss in GetOverlappingIssues(current, -1))
                        {
                            downDirection.Push(iss);
                            issueRemoveList.Add(iss);
                        }
                    }

                }

                issueRemoveList.Add(issue);
                //Issues.Remove(issue);

            }

            Clipboard.Clip($"Manually removed {issueRemoveList.Count} issues");
            
            IssuesGrid.SelectedIndex = -1;
            Issues.RemoveRange(issueRemoveList);

            if (layersRemove.Count > 0)
            {
                ResetDataContext();
            }

            if (Settings.PixelEditor.PartialUpdateIslandsOnEditing)
            {
                await UpdateIslandsOverhangs(whiteListLayers);
            }

            ShowLayer(); // It will call latter so its a extra call
            CanSave = true;
        }

        public async void OnClickIssueIgnore()
        {
            if ((_globalModifiers & KeyModifiers.Alt) != 0)
            {
                if(IgnoredIssues.Count == 0) return;
                if (await this.MessageBoxQuestion(
                        $"Are you sure you want to re-enable {IgnoredIssues.Count} ignored issues?\n" +
                        "A full re-detect will be required to get the ignored issues.\n", $"Re-enable {IgnoredIssues.Count} Issues?") !=
                    ButtonResult.Yes) return;

                IgnoredIssues.Clear();

                return;
            }

            if (IssuesGrid.SelectedItems.Count == 0) return;

            if (await this.MessageBoxQuestion(
                    $"Are you sure you want to hide and ignore all selected {IssuesGrid.SelectedItems.Count} issues?\n" +
                    "The ignored issues won't be re-detected.\n", $"Ignore {IssuesGrid.SelectedItems.Count} Issues?") !=
                ButtonResult.Yes) return;

            var list = IssuesGrid.SelectedItems.Cast<LayerIssue>().ToArray();
            IgnoredIssues.AddRange(list);
            IssuesGrid.SelectedItems.Clear();
            Issues.RemoveRange(list);
            ShowLayer();
        }

        private async Task UpdateIslandsOverhangs(List<uint> whiteListLayers)
        {
            if (whiteListLayers.Count == 0) return;
            var islandConfig = GetIslandDetectionConfiguration();
            var overhangConfig = GetOverhangDetectionConfiguration();
            var resinTrapConfig = new ResinTrapDetectionConfiguration(false);
            var touchingBoundConfig = new TouchingBoundDetectionConfiguration(false);
            var printHeightConfig = new PrintHeightDetectionConfiguration(false);
            islandConfig.Enabled = true;
            islandConfig.WhiteListLayers = whiteListLayers;
            overhangConfig.Enabled = true;
            overhangConfig.WhiteListLayers = whiteListLayers;


            IsGUIEnabled = false;
            ShowProgressWindow("Updating Issues");


            var issueList = Issues.ToList();
            issueList.RemoveAll(issue =>
                islandConfig.WhiteListLayers.Contains(issue.LayerIndex) && (issue.Type == LayerIssue.IssueType.Island ||
                                                                               issue.Type == LayerIssue.IssueType.Overhang));
            /*foreach (var layerIndex in islandConfig.WhiteListLayers)
            {
                issueList.RemoveAll(issue =>
                    issue.LayerIndex == layerIndex && (issue.Type == LayerIssue.IssueType.Island ||
                                                       issue.Type == LayerIssue.IssueType.Overhang));
                
            }*/

            var resultIssues = await Task.Factory.StartNew(() =>
            {
                try
                {
                    var issues = SlicerFile.LayerManager.GetAllIssues(islandConfig, overhangConfig, resinTrapConfig,
                        touchingBoundConfig, printHeightConfig, false, IgnoredIssues, Progress);

                    issues.RemoveAll(issue => issue.Type != LayerIssue.IssueType.Island && issue.Type != LayerIssue.IssueType.Overhang); // Remove all non islands and overhangs
                    return issues;
                }

                catch (OperationCanceledException)
                {

                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.InvokeAsync(async () =>
                        await this.MessageBoxError(ex.ToString(), "Error while trying to compute issues"));
                }

                return null;
            });

            IsGUIEnabled = true;

            if (resultIssues is not null && resultIssues.Count > 0) issueList.AddRange(resultIssues);

            issueList = issueList.OrderBy(issue => issue.Type)
                .ThenBy(issue => issue.LayerIndex)
                .ThenBy(issue => issue.PixelsCount).ToList();

            Issues.ReplaceCollection(issueList);
        }

        public int IssueSelectedIndex
        {
            get => _issueSelectedIndex;
            set
            {
                if (!RaiseAndSetIfChanged(ref _issueSelectedIndex, value)) return;
                RaisePropertyChanged(nameof(IssueSelectedIndexStr));
                RaisePropertyChanged(nameof(IssueCanGoPrevious));
                RaisePropertyChanged(nameof(IssueCanGoNext));
            }
        }

        public string IssueSelectedIndexStr => (_issueSelectedIndex + 1).ToString().PadLeft(Issues.Count.ToString().Length, '0');

        private void IssuesGridOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DataContext is null) return;

            if (IssuesGrid.SelectedItem is not LayerIssue issue)
            {
                ShowLayer();
                return;
            }

            var firstPoint = issue.FirstPoint;
            if (issue.Type is LayerIssue.IssueType.TouchingBound or LayerIssue.IssueType.EmptyLayer || (firstPoint.X == -1 && firstPoint.Y == -1))
            {
                ZoomToFit();
            }
            else if (firstPoint.X >= 0 && firstPoint.Y >= 0)
            {

                if (Settings.LayerPreview.ZoomIssues ^ (_globalModifiers & KeyModifiers.Alt) != 0)
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
        }

        
        private void IssuesGridOnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
        {
            if (e.PointerPressedEventArgs.ClickCount == 2) return;
            if (IssuesGrid.SelectedItem is not LayerIssue) return;
            // Double clicking an issue will center and zoom into the 
            // selected issue. Left click on an issue will zoom to fit.
            
            var pointer = e.PointerPressedEventArgs.GetCurrentPoint(IssuesGrid);

            if (pointer.Properties.IsRightButtonPressed)
            {
                ZoomToFit();
                return;
            }

            //ForceUpdateActualLayer(issue.LayerIndex);

        }

        private void IssuesGridOnKeyUp(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    IssuesGrid.SelectedItems.Clear();
                    break;
                case Key.Multiply:
                    var selectedItems = IssuesGrid.SelectedItems.OfType<LayerIssue>().ToList();
                    IssuesGrid.SelectedItems.Clear();
                    foreach (LayerIssue item in Issues)
                    {
                        if (!selectedItems.Contains(item))
                            IssuesGrid.SelectedItems.Add(item);
                    }
                    

                    break;
                case Key.Delete:
                    OnClickIssueRemove();
                    break;
            }
        }

        public async void OnClickRepairIssues()
        {
            await ShowRunOperation(typeof(OperationRepairLayers));
        }

        public async Task OnClickDetectIssues()
        {
            if (!IsFileLoaded) return;
            await ComputeIssues(
                GetIslandDetectionConfiguration(),
                GetOverhangDetectionConfiguration(),
                GetResinTrapDetectionConfiguration(),
                GetTouchingBoundsDetectionConfiguration(),
                GetPrintHeightDetectionConfiguration(),
                Settings.Issues.ComputeEmptyLayers);
        }

        private async Task ComputeIssues(IslandDetectionConfiguration islandConfig = null,
            OverhangDetectionConfiguration overhangConfig = null,
            ResinTrapDetectionConfiguration resinTrapConfig = null,
            TouchingBoundDetectionConfiguration touchingBoundConfig = null,
            PrintHeightDetectionConfiguration printHeightConfig = null,
            bool emptyLayersConfig = true)
        {

            Issues.Clear();
            IsGUIEnabled = false;
            ShowProgressWindow("Computing Issues");

            var resultIssues = await Task.Factory.StartNew(() =>
            {
                try
                {
                    var issues = SlicerFile.LayerManager.GetAllIssues(islandConfig, overhangConfig, resinTrapConfig, touchingBoundConfig,
                        printHeightConfig, emptyLayersConfig, IgnoredIssues, Progress);
                    return issues;
                }
                catch (OperationCanceledException)
                {

                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.InvokeAsync(async () =>
                        await this.MessageBoxError(ex.ToString(), "Error while trying compute issues"));
                }

                return null;
            });

            IsGUIEnabled = true;
            
            if (resultIssues is null)
            {
                return;
            }
            Issues.AddRange(resultIssues);
            
            ShowLayer();

            RaisePropertyChanged(nameof(IssueSelectedIndexStr));
            RaisePropertyChanged(nameof(IssueCanGoPrevious));
            RaisePropertyChanged(nameof(IssueCanGoNext));

        }

        public Dictionary<uint, uint> GetIssuesCountPerLayer()
        {
            if (Issues is null || Issues.Count == 0) return null;
            Dictionary<uint, uint> layerIndexIssueCount = new();
            foreach (var issue in Issues)
            {
                if (!layerIndexIssueCount.ContainsKey(issue.LayerIndex))
                {
                    layerIndexIssueCount.Add(issue.LayerIndex, 1);
                }
                else
                {
                    layerIndexIssueCount[issue.LayerIndex]++;
                }
            }

            return layerIndexIssueCount;
        }

        public Dictionary<LayerIssue.IssueType, ISolidColorBrush> GetIssueColors(bool highlightColors = false)
        {
            return new Dictionary<LayerIssue.IssueType, ISolidColorBrush>
            {
                {LayerIssue.IssueType.Island,     highlightColors ? Settings.LayerPreview.IslandHighlightBrush : Settings.LayerPreview.IslandBrush},
                {LayerIssue.IssueType.Overhang,   highlightColors ? Settings.LayerPreview.OverhangHighlightBrush : Settings.LayerPreview.OverhangBrush},
                {LayerIssue.IssueType.ResinTrap,  highlightColors ? Settings.LayerPreview.ResinTrapHighlightBrush : Settings.LayerPreview.ResinTrapBrush},
                {LayerIssue.IssueType.SuctionCup, highlightColors ? Settings.LayerPreview.SuctionCupHighlightBrush : Settings.LayerPreview.SuctionCupBrush},
                {LayerIssue.IssueType.TouchingBound, Settings.LayerPreview.TouchingBoundsBrush},
                {LayerIssue.IssueType.EmptyLayer, Brushes.Red},
                {LayerIssue.IssueType.PrintHeight, Brushes.Red},
                {LayerIssue.IssueType.Debug, new ImmutableSolidColorBrush(new Color(255, 15, 112, 16))},
            };
        }

        private void UpdateLayerTrackerHighlightIssues()
        {
            _issuesSliderCanvas.Children.Clear();
            if (Issues is null || Issues.Count == 0) return;

            var tickFrequencySize = _issuesSliderCanvas.Bounds.Height * LayerSlider.TickFrequency / LayerSlider.Maximum;
            var stroke = (int)Math.Ceiling(tickFrequencySize);

            var colorDictionary = GetIssueColors(true);

            for (int layerIndex = 0; layerIndex < SlicerFile.LayerCount; layerIndex++)
            {
                var color = Brushes.Red;

                var issue = Issues
                    .Where(issue => issue.LayerIndex == layerIndex)
                    .OrderBy(issue => issue.Type)
                    .FirstOrDefault();

                if(issue is null) continue;

                if(Settings.LayerPreview.UseIssueColorOnTracker) colorDictionary.TryGetValue(issue.Type, out color);

                var yPos = tickFrequencySize * layerIndex;
                if (layerIndex == 0 && stroke > 3)
                {
                    yPos += tickFrequencySize / 2;
                }
                else if (layerIndex == SlicerFile.LastLayerIndex && stroke > 3)
                {
                    yPos -= tickFrequencySize / 2;
                }
                var line = new Line { StrokeThickness = stroke, Stroke = color, EndPoint = new Avalonia.Point(_issuesSliderCanvas.Width, 0) };
                _issuesSliderCanvas.Children.Add(line);
                Canvas.SetBottom(line, yPos);
            }
            
            /*var issuesCountPerLayer = GetIssuesCountPerLayer();
            if (issuesCountPerLayer is null)
            {
                return;
            }

            //var tickFrequencySize = LayerSlider.Track.Bounds.Height * LayerSlider.TickFrequency / (LayerSlider.Maximum - LayerSlider.Minimum);
            var tickFrequencySize = _issuesSliderCanvas.Bounds.Height * LayerSlider.TickFrequency / (LayerSlider.Maximum - LayerSlider.Minimum);
            var stroke = (int)Math.Ceiling(tickFrequencySize);
            foreach (var value in issuesCountPerLayer)
            {
                var yPos = tickFrequencySize * value.Key;
                if (value.Key == 0 && stroke > 3)
                {
                    yPos += tickFrequencySize / 2;
                }
                else if(value.Key == SlicerFile.LastLayerIndex && stroke > 3)
                {
                    yPos -= tickFrequencySize / 2;
                }
                var line = new Line{StrokeThickness = stroke, Stroke = Brushes.Red, EndPoint = new Avalonia.Point(_issuesSliderCanvas.Width, 0)};
                _issuesSliderCanvas.Children.Add(line);
                Canvas.SetBottom(line, yPos);
            }*/
        }

        public void IssuesClear(bool clearIgnored = true)
        {
            Issues.Clear();
            if(clearIgnored) IgnoredIssues.Clear();
        }

        public void SetResinTrapDetectionStartLayer(char which)
        {
            switch (which)
            {
                case 'N':
                    ResinTrapDetectionStartLayer = SlicerFile.FirstNormalLayer.Index;
                    break;
                case 'C':
                    ResinTrapDetectionStartLayer = ActualLayer;
                    break;
            }
        }



        public IslandDetectionConfiguration GetIslandDetectionConfiguration(bool enable)
        {
            return new()
            {
                Enabled = enable,
                EnhancedDetection = Settings.Issues.IslandEnhancedDetection,
                AllowDiagonalBonds = Settings.Issues.IslandAllowDiagonalBonds,
                BinaryThreshold = Settings.Issues.IslandBinaryThreshold,
                RequiredAreaToProcessCheck = Settings.Issues.IslandRequiredAreaToProcessCheck,
                RequiredPixelBrightnessToProcessCheck = Settings.Issues.IslandRequiredPixelBrightnessToProcessCheck,
                RequiredPixelsToSupportMultiplier = Settings.Issues.IslandRequiredPixelsToSupportMultiplier,
                RequiredPixelsToSupport = Settings.Issues.IslandRequiredPixelsToSupport,
                RequiredPixelBrightnessToSupport = Settings.Issues.IslandRequiredPixelBrightnessToSupport
            };
        }
        public IslandDetectionConfiguration GetIslandDetectionConfiguration() => GetIslandDetectionConfiguration(Settings.Issues.ComputeIslands);

        public OverhangDetectionConfiguration GetOverhangDetectionConfiguration(bool enable)
        {
            return new()
            {
                Enabled = enable,
                IndependentFromIslands = Settings.Issues.OverhangIndependentFromIslands,
                ErodeIterations = Settings.Issues.OverhangErodeIterations,
            };
        }
        public OverhangDetectionConfiguration GetOverhangDetectionConfiguration() => GetOverhangDetectionConfiguration(Settings.Issues.ComputeOverhangs);


        public ResinTrapDetectionConfiguration GetResinTrapDetectionConfiguration(bool enable)
        {
            return new()
            {
                Enabled = enable,
                StartLayerIndex = _resinTrapDetectionStartLayer,
                BinaryThreshold = Settings.Issues.ResinTrapBinaryThreshold,
                RequiredAreaToProcessCheck = Settings.Issues.ResinTrapRequiredAreaToProcessCheck,
                RequiredBlackPixelsToDrain = Settings.Issues.ResinTrapRequiredBlackPixelsToDrain,
                MaximumPixelBrightnessToDrain = Settings.Issues.ResinTrapMaximumPixelBrightnessToDrain,
                DetectSuctionCups = Settings.Issues.ComputeSuctionCups,
                RequiredAreaToConsiderSuctionCup = Settings.Issues.SuctionCupRequiredAreaToConsider
            };
        }
        public ResinTrapDetectionConfiguration GetResinTrapDetectionConfiguration() => GetResinTrapDetectionConfiguration(Settings.Issues.ComputeResinTraps);

        public TouchingBoundDetectionConfiguration GetTouchingBoundsDetectionConfiguration(bool enable)
        {
            return new()
            {
                Enabled = enable,
                MinimumPixelBrightness = UserSettings.Instance.Issues.TouchingBoundMinimumPixelBrightness,
                MarginLeft = UserSettings.Instance.Issues.TouchingBoundMarginLeft,
                MarginTop = UserSettings.Instance.Issues.TouchingBoundMarginTop,
                MarginRight = UserSettings.Instance.Issues.TouchingBoundMarginRight,
                MarginBottom = UserSettings.Instance.Issues.TouchingBoundMarginBottom,
            };
        }
        public TouchingBoundDetectionConfiguration GetTouchingBoundsDetectionConfiguration() => GetTouchingBoundsDetectionConfiguration(Settings.Issues.ComputeTouchingBounds);


        public PrintHeightDetectionConfiguration GetPrintHeightDetectionConfiguration(bool enable)
        {
            return new ()
            {
                Enabled = enable,
                Offset = (float) Settings.Issues.PrintHeightOffset
            };
        }
        public PrintHeightDetectionConfiguration GetPrintHeightDetectionConfiguration() => GetPrintHeightDetectionConfiguration(Settings.Issues.ComputePrintHeight);


        #endregion
    }
}
