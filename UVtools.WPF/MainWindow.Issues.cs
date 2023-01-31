/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using Emgu.CV;
using Emgu.CV.Util;
using MessageBox.Avalonia.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;
using Brushes = Avalonia.Media.Brushes;

namespace UVtools.WPF;

public partial class MainWindow
{
    #region Members
    private bool _firstTimeOnIssues = true;
    public DataGrid IssuesGrid;

    private int _issueSelectedIndex = -1;

    public IEnumerable IssuesGridItems 
    {
        get
        {
            if (!IsFileLoaded || DataContext is null) return null;
            if (Settings.Issues.DataGridGroupByType || Settings.Issues.DataGridGroupByLayerIndex)
            {
                var groupView = new DataGridCollectionView(SlicerFile.IssueManager);
                if (Settings.Issues.DataGridGroupByType) groupView.GroupDescriptions.Add(new DataGridPathGroupDescription("Type"));
                if (Settings.Issues.DataGridGroupByLayerIndex) groupView.GroupDescriptions.Add(new DataGridPathGroupDescription("StartLayerIndex"));

                return groupView;
            }

            return SlicerFile.IssueManager;
        }
    }
    #endregion

    #region Properties

    private uint _resinTrapDetectionStartLayer;

    public bool IssueCanGoPrevious => IsFileLoaded && SlicerFile.IssueManager.Count > 0 && _issueSelectedIndex > 0;
    public bool IssueCanGoNext => IsFileLoaded && SlicerFile.IssueManager.Count > 0 && _issueSelectedIndex < SlicerFile.IssueManager.Count - 1;

    public uint ResinTrapDetectionStartLayer
    {
        get => _resinTrapDetectionStartLayer;
        set => RaiseAndSetIfChanged(ref _resinTrapDetectionStartLayer, value);
    }

    public bool SuppressIssueGridSelectionEvent { get; set; }

    #endregion

    #region Methods

    public void InitIssues()
    {
        IssuesGrid = this.FindControl<DataGrid>("IssuesGrid");
        IssuesGrid.CellPointerPressed += IssuesGridOnCellPointerPressed;
        IssuesGrid.SelectionChanged += IssuesGridOnSelectionChanged;
        IssuesGrid.KeyUp += IssuesGridOnKeyUp;
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

    /*public List<IssueOfContours> GetOverlappingIssues(IssueOfContours targetIssue, int indexOffset)
    {
        var retValue = new List<IssueOfContours>();

        int targetLayerIndex = (int)targetIssue.LayerIndex + indexOffset;
        if (targetLayerIndex > SlicerFile.LayerCount - 1 || targetLayerIndex < 0) return retValue;

        foreach (IssueOfContours candidate in SlicerFile.IssueManager.GetIssuesBy(MainIssue.IssueType.SuctionCup, (uint)targetLayerIndex))
        {
            using var vec1 = new VectorOfVectorOfPoint(targetIssue.Contours);
            using var vec2 = new VectorOfVectorOfPoint(candidate.Contours);
            if (!EmguContours.ContoursIntersect(vec1, vec2)) continue;
            retValue.Add(candidate);
            break;
        }

        return retValue;
    }*/

    public async void RemoveRepairIssues(IEnumerable<MainIssue> issues, bool promptConfirmation = true, bool suctionCupDrill = true)
    {
        if (!issues.Any()) return;

        uint emptyLayers = 0;
        uint islands = 0;
        uint resinTraps = 0;
        uint suctionCups = 0;
        foreach (MainIssue mainIssue in issues)
        {
            switch (mainIssue.Type)
            {
                case MainIssue.IssueType.Island:
                    islands++;
                    break;
                case MainIssue.IssueType.Overhang:
                    // No action
                    break;
                case MainIssue.IssueType.ResinTrap:
                    resinTraps++;
                    break;
                case MainIssue.IssueType.SuctionCup:
                    suctionCups++;
                    break;
                case MainIssue.IssueType.TouchingBound:
                    // No action
                    break;
                case MainIssue.IssueType.PrintHeight:
                    // No action
                    break;
                case MainIssue.IssueType.EmptyLayer:
                    emptyLayers++;
                    break;
                case MainIssue.IssueType.Debug:
                    // No action
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (emptyLayers == 0 && islands == 0 && resinTraps == 0 && suctionCups == 0) return;

        if (promptConfirmation && await this.MessageBoxQuestion(
                $"Are you sure you want to remove and/or repair all selected {issues.Count()} issues?\n\n" +
                "If any, this option will:\n" +
                (emptyLayers > 0 ? $"- Remove {emptyLayers} empty layer(s)\n" : string.Empty) +
                (islands > 0 ? $"- Remove {emptyLayers} island(s)\n" : string.Empty) +
                (resinTraps > 0 ? $"- Fill/solidify {resinTraps} resin trap(s)\n" : string.Empty) +
                (suctionCups > 0 ? $"- Drill {suctionCups} suction cup(s) at it's center\n" : string.Empty) +
                "\nWarning: Removing an island can cause other issues to appear if there is material present in the layers above it.\n" +
                "Always check previous and next layers before performing an island removal.", $"Remove {issues.Count()} Issues?") != ButtonResult.Yes) return;

        var processParallelIssues = new Dictionary<uint, List<Issue>>();
        var processSuctionCups = new List<MainIssue>();
        var layersToRemove = new List<uint>();


        foreach (MainIssue mainIssue in issues)
        {
            switch (mainIssue.Type)
            {
                case MainIssue.IssueType.Island:
                case MainIssue.IssueType.ResinTrap:
                    foreach (var issue in mainIssue)
                    {
                        // Islands and resin traps
                        if (!processParallelIssues.TryGetValue(issue.LayerIndex, out var issueList))
                        {
                            issueList = new List<Issue>();
                            processParallelIssues.Add(issue.LayerIndex, issueList);
                        }

                        issueList.Add(issue);

                    }
                    continue;
                case MainIssue.IssueType.SuctionCup:
                    if (!suctionCupDrill)
                    {
                        foreach (var issue in mainIssue)
                        {
                            // Islands and resin traps
                            if (!processParallelIssues.TryGetValue(issue.LayerIndex, out var issueList))
                            {
                                issueList = new List<Issue>();
                                processParallelIssues.Add(issue.LayerIndex, issueList);
                            }

                            issueList.Add(issue);

                        }
                        continue;
                    }
                    if(mainIssue.StartLayerIndex == 0) continue;
                    processSuctionCups.Add(mainIssue);
                    continue;
                case MainIssue.IssueType.EmptyLayer:
                    layersToRemove.AddRange(mainIssue.Select(issue => issue.LayerIndex));
                    continue;
            }
        }


        var totalIssues = processParallelIssues.Count + processSuctionCups.Count + layersToRemove.Count;
        if (totalIssues == 0) return;

        var issueRemoveList = new List<MainIssue>();

        IsGUIEnabled = false;
        ShowProgressWindow("Removing selected issues", false);

        Clipboard.Snapshot();

        var task = await Task.Run(() =>
        {
            Progress.Reset("Removing selected issues", (uint)processParallelIssues.Count);
            try
            {
                Parallel.ForEach(processParallelIssues, CoreSettings.GetParallelOptions(Progress), layerIssues =>
                {
                    using (var image = SlicerFile[layerIssues.Key].LayerMat)
                    {
                        var bytes = image.GetDataByteSpan();

                        bool edited = false;
                        foreach (var issue in layerIssues.Value)
                        {
                            if (issue.Type == MainIssue.IssueType.Island)
                            {
                                var issueOfPoints = (IssueOfPoints)issue;
                                foreach (var pixel in issueOfPoints.Points)
                                {
                                    bytes[image.GetPixelPos(pixel.X, pixel.Y)] = 0;
                                }

                                edited = true;
                            }
                            else if (issue.Type == MainIssue.IssueType.ResinTrap || (issue.Type == MainIssue.IssueType.SuctionCup && !suctionCupDrill))
                            {
                                var issueOfContours = (IssueOfContours)issue;
                                using var contours = new VectorOfVectorOfPoint(issueOfContours.Contours);
                                CvInvoke.DrawContours(image, contours, -1, EmguExtensions.WhiteColor, -1);
                                if (Settings.LayerRepair.ResinTrapsOverlapBy > 0)
                                {
                                    CvInvoke.DrawContours(image, contours, -1, EmguExtensions.WhiteColor, Settings.LayerRepair.ResinTrapsOverlapBy * 2 + 1);
                                }
                                edited = true;
                            }
                        }

                        if (edited)
                        {
                            SlicerFile[layerIssues.Key].LayerMat = image;
                        }
                    }

                    Progress.LockAndIncrement();
                });

                if (layersToRemove.Count > 0)
                {
                    OperationLayerRemove.RemoveLayers(SlicerFile, layersToRemove);
                }

                if(suctionCupDrill) issueRemoveList.AddRange(SlicerFile.IssueManager.DrillSuctionCupsForIssues(processSuctionCups, UserSettings.Instance.LayerRepair.SuctionCupsVentHole, Progress));

            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                    await this.MessageBoxError(ex.ToString(), "Removal/repair failed"));

                return false;
            }

            return true;
        }, Progress.Token);

        IsGUIEnabled = true;

        if (!task)
        {
            Clipboard.RestoreSnapshot();
            return;
        }

        var whiteListLayers = new List<uint>();

        // Update GUI
            
        foreach (MainIssue issue in issues)
        {
            if (issue.IsSuctionCup && !suctionCupDrill)
            {
                issueRemoveList.Add(issue);
                continue;
            }

            if (issue.Type 
                is not MainIssue.IssueType.Island
                and not MainIssue.IssueType.ResinTrap
                and not MainIssue.IssueType.EmptyLayer) continue;


            issueRemoveList.Add(issue);


            if (issue.IsIsland)
            {
                var nextLayer = issue.StartLayerIndex + 1;
                if (nextLayer >= SlicerFile.LayerCount) continue;
                if (whiteListLayers.Contains(nextLayer)) continue;
                whiteListLayers.Add(nextLayer);
            }
                
            //Issues.Remove(issue);

        }

        if (issueRemoveList.Count == 0) return;

        Clipboard.Clip($"Manually removed {issueRemoveList.Count} issues");
            
        IssuesGrid.SelectedIndex = -1;
        SlicerFile.IssueManager.RemoveRange(issueRemoveList);

        if (layersToRemove.Count > 0)
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

    public void OnClickIssueRemove()
    {
        RemoveRepairIssues(IssuesGrid.SelectedItems.OfType<MainIssue>(), true);
    }

    public void SelectedIssuesIslandRemove()
    {
        if (IssuesGrid.SelectedItem is null) return;
        RemoveRepairIssues(IssuesGrid.SelectedItems.OfType<MainIssue>().Where(mainIssue => mainIssue.IsIsland), false);
    }

    public void SelectedIssuesResinTrapSolidify()
    {
        if (IssuesGrid.SelectedItem is null) return;
        RemoveRepairIssues(IssuesGrid.SelectedItems.OfType<MainIssue>().Where(mainIssue => mainIssue.IsResinTrap), false);
    }

    public void SelectedIssuesSuctionCupDrill()
    {
        if (IssuesGrid.SelectedItem is null) return;
        RemoveRepairIssues(IssuesGrid.SelectedItems.OfType<MainIssue>().Where(mainIssue => mainIssue.IsSuctionCup), false);
    }

    public void SelectedIssuesSuctionCupSolidify()
    {
        if (IssuesGrid.SelectedItem is null) return;
        RemoveRepairIssues(IssuesGrid.SelectedItems.OfType<MainIssue>().Where(mainIssue => mainIssue.IsSuctionCup), false, false);
    }

    public void SelectedIssuesEmptyLayerRemove()
    {
        if (IssuesGrid.SelectedItem is null) return;
        RemoveRepairIssues(IssuesGrid.SelectedItems.OfType<MainIssue>().Where(mainIssue => mainIssue.IsEmptyLayer), false);
    }

    public async void OnClickIssueIgnore()
    {
        if ((_globalModifiers & KeyModifiers.Alt) != 0)
        {
            if(SlicerFile.IssueManager.IgnoredIssues.Count == 0) return;
            if (await this.MessageBoxQuestion(
                    $"Are you sure you want to re-enable {SlicerFile.IssueManager.IgnoredIssues.Count} ignored issues?\n" +
                    "A full re-detect will be required to get the ignored issues.\n", $"Re-enable {SlicerFile.IssueManager.IgnoredIssues.Count} Issues?") !=
                ButtonResult.Yes) return;

            SlicerFile.IssueManager.IgnoredIssues.Clear();

            return;
        }

        if (IssuesGrid.SelectedItems.Count == 0) return;

        if (await this.MessageBoxQuestion(
                $"Are you sure you want to hide and ignore all selected {IssuesGrid.SelectedItems.Count} issues?\n" +
                "The ignored issues won't be re-detected.\n", $"Ignore {IssuesGrid.SelectedItems.Count} Issues?") !=
            ButtonResult.Yes) return;

        var list = IssuesGrid.SelectedItems.Cast<MainIssue>().ToArray();
        SlicerFile.IssueManager.IgnoredIssues.AddRange(list);
        IssuesGrid.SelectedItems.Clear();
        SlicerFile.IssueManager.RemoveRange(list);
        ShowLayer();
    }

    private async Task UpdateIslandsOverhangs(List<uint> whiteListLayers)
    {
        if (whiteListLayers.Count == 0) return;
        var config = GetIssuesDetectionConfiguration(false);
        config.IslandConfig.Enable();
        config.IslandConfig.WhiteListLayers = whiteListLayers;
        config.OverhangConfig.Enable();
        config.OverhangConfig.WhiteListLayers = whiteListLayers;


        IsGUIEnabled = false;
        ShowProgressWindow("Updating Issues");


        var issueList = SlicerFile.IssueManager.ToList();
        issueList.RemoveAll(issue =>
            config.IslandConfig.WhiteListLayers.Contains(issue.StartLayerIndex) && issue.Type is MainIssue.IssueType.Island or MainIssue.IssueType.Overhang);
        /*foreach (var layerIndex in islandConfig.WhiteListLayers)
        {
            issueList.RemoveAll(issue =>
                issue.LayerIndex == layerIndex && (issue.Type == LayerIssue.IssueType.Island ||
                                                   issue.Type == LayerIssue.IssueType.Overhang));
            
        }*/

        var resultIssues = await Task.Run(() =>
        {
            try
            {
                var issues = SlicerFile.IssueManager.DetectIssues(config, Progress);

                issues.RemoveAll(issue => issue.Type is not MainIssue.IssueType.Island and not MainIssue.IssueType.Overhang); // Remove all non islands and overhangs
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
        }, Progress.Token);

        IsGUIEnabled = true;

        if (resultIssues is not null && resultIssues.Count > 0) issueList.AddRange(resultIssues);

        switch (Settings.Issues.DataGridOrderBy)
        {
            case IssuesOrderBy.TypeAscLayerAscAreaDesc:
                issueList = issueList.OrderBy(issue => issue.Type)
                    .ThenBy(issue => issue.StartLayerIndex)
                    .ThenByDescending(issue => issue.Area).ToList();
                break;
            case IssuesOrderBy.TypeAscAreaDescLayerAsc:
                issueList = issueList.OrderBy(issue => issue.Type)
                    .ThenByDescending(issue => issue.Area)
                    .ThenBy(issue => issue.StartLayerIndex).ToList();
                break;
            case IssuesOrderBy.AreaDescLayerIndexAscTypeAsc:
                issueList = issueList.OrderByDescending(issue => issue.Area)
                    .ThenBy(issue => issue.StartLayerIndex)
                    .ThenBy(issue => issue.Type).ToList();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Settings.Issues.DataGridOrderBy));
        }
        
            
        SlicerFile.IssueManager.ReplaceCollection(issueList);
    }

    public int IssueSelectedIndex
    {
        get => _issueSelectedIndex;
        set
        {
            if (!RaiseAndSetIfChanged(ref _issueSelectedIndex, value)) return;
            if(_issueSelectedIndex > 0) IssuesGrid.ScrollIntoView(SlicerFile.IssueManager.FirstOrDefault(issue => ReferenceEquals(issue, IssuesGrid.SelectedItem)), null);
            RaisePropertyChanged(nameof(IssueSelectedIndexStr));
            RaisePropertyChanged(nameof(IssueCanGoPrevious));
            RaisePropertyChanged(nameof(IssueCanGoNext));
        }
    }

    public string IssueSelectedIndexStr => IsFileLoaded 
        ? (_issueSelectedIndex + 1).ToString().PadLeft(SlicerFile.IssueManager.Count.ToString().Length, '0')
        : "0";

    private void IssuesGridOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is null || SuppressIssueGridSelectionEvent) return;

        if (IssuesGrid.SelectedItem is not MainIssue mainIssue)
        {
            ShowLayer();
            return;
        }

        var issue = mainIssue.FirstOrDefault();
        ZoomToIssue(issue, true);
    }

        
    private void IssuesGridOnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        if (e.PointerPressedEventArgs.ClickCount == 2) return;
        if (IssuesGrid.SelectedItem is not MainIssue) return;
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
                var selectedItems = IssuesGrid.SelectedItems.OfType<MainIssue>().ToList();
                IssuesGrid.SelectedItems.Clear();
                foreach (var item in SlicerFile.IssueManager)
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

    public async void OnClickExportIssues()
    {
        if (!SlicerFile.IssueManager.HaveIssues) return;
        var dialog = new SaveFileDialog
        {
            DefaultExtension = ".uvtissues",
            Filters = new List<FileDialogFilter>
            {
                new()
                {
                    Name = "UVtools issues files",
                    Extensions = new List<string> {"uvtissues"}
                }
            },
            InitialFileName = SlicerFile.FilenameNoExt,
            Directory = SlicerFile.DirectoryPath
        };

        var path = await dialog.ShowAsync(this);
        if (string.IsNullOrWhiteSpace(path)) return;
        
        IsGUIEnabled = false;
        try
        {
            var exportIssues = new SerializableIssuesDocument(SlicerFile);
            exportIssues.SerializeAsync(path);
        }
        catch (Exception e)
        {
            await this.MessageBoxError(e.ToString());
            Debug.WriteLine(e);
            if(File.Exists(path)) File.Delete(path);
        }

        IsGUIEnabled = true;
    }

    public async Task OnClickDetectIssues()
    {
        if (!IsFileLoaded) return;
        if (SlicerFile.DecodeType == FileFormat.FileDecodeType.Partial)
        {
            await this.MessageBoxError("The file was open in partial mode and the detect issues is unable to run in this mode.\n" +
                                       "Please reload the file in full mode in order to use detect issues.", "Unable to run in partial mode");
            return;

        }
        await ComputeIssues(GetIssuesDetectionConfiguration());
    }

    private async Task ComputeIssues(IssuesDetectionConfiguration config)
    {
        SlicerFile.IssueManager.Clear();
        IsGUIEnabled = false;
        ShowProgressWindow("Computing Issues");

        var resultIssues = await Task.Run(() =>
        {
            try
            {
                var issues = SlicerFile.IssueManager.DetectIssues(config, Progress);

                switch (Settings.Issues.DataGridOrderBy)
                {
                    case IssuesOrderBy.TypeAscLayerAscAreaDesc:
                        // This order is already made on the detection
                        break;
                    case IssuesOrderBy.TypeAscAreaDescLayerAsc:
                        issues = issues.OrderBy(issue => issue.Type)
                            .ThenByDescending(issue => issue.Area)
                            .ThenBy(issue => issue.StartLayerIndex).ToList();
                        break;
                    case IssuesOrderBy.AreaDescLayerIndexAscTypeAsc:
                        issues = issues.OrderByDescending(issue => issue.Area)
                            .ThenBy(issue => issue.StartLayerIndex)
                            .ThenBy(issue => issue.Type).ToList();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Settings.Issues.DataGridOrderBy));
                }

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
        }, Progress.Token);

        IsGUIEnabled = true;
            
        if (resultIssues is null)
        {
            return;
        }
        SlicerFile.IssueManager.AddRange(resultIssues);
            
        ShowLayer();

        RaisePropertyChanged(nameof(IssueSelectedIndexStr));
        RaisePropertyChanged(nameof(IssueCanGoPrevious));
        RaisePropertyChanged(nameof(IssueCanGoNext));

    }

    /*public Dictionary<uint, uint> GetIssuesCountPerLayer()
    {
        if (SlicerFile.IssueManager.Count == 0) return null;
        Dictionary<uint, uint> layerIndexIssueCount = new();
        foreach (var issue in Issues)
        {
            if (!layerIndexIssueCount.ContainsKey(issue.StartLayerIndex))
            {
                layerIndexIssueCount.Add(issue.LayerIndex, 1);
            }
            else
            {
                layerIndexIssueCount[issue.LayerIndex]++;
            }
        }

        return layerIndexIssueCount;
    }*/

    public Dictionary<MainIssue.IssueType, ISolidColorBrush> GetIssueColors(bool highlightColors = false)
    {
        return new Dictionary<MainIssue.IssueType, ISolidColorBrush>
        {
            {MainIssue.IssueType.Island,     highlightColors ? Settings.LayerPreview.IslandHighlightBrush : Settings.LayerPreview.IslandBrush},
            {MainIssue.IssueType.Overhang,   highlightColors ? Settings.LayerPreview.OverhangHighlightBrush : Settings.LayerPreview.OverhangBrush},
            {MainIssue.IssueType.ResinTrap,  highlightColors ? Settings.LayerPreview.ResinTrapHighlightBrush : Settings.LayerPreview.ResinTrapBrush},
            {MainIssue.IssueType.SuctionCup, highlightColors ? Settings.LayerPreview.SuctionCupHighlightBrush : Settings.LayerPreview.SuctionCupBrush},
            {MainIssue.IssueType.TouchingBound, Settings.LayerPreview.TouchingBoundsBrush},
            {MainIssue.IssueType.EmptyLayer, Brushes.Red},
            {MainIssue.IssueType.PrintHeight, Brushes.Red},
            {MainIssue.IssueType.Debug, new ImmutableSolidColorBrush(new Color(255, 15, 112, 16))},
        };
    }

    private void UpdateLayerTrackerHighlightIssues()
    {
        _issuesSliderCanvas.Children.Clear();
        if (!IsFileLoaded || SlicerFile.IssueManager.Count == 0) return;

        var tickFrequencySize = _issuesSliderCanvas.Bounds.Height * LayerSlider.TickFrequency / LayerSlider.Maximum;
        var stroke = (int)Math.Ceiling(tickFrequencySize);

        var colorDictionary = GetIssueColors(true);


        var issues = SlicerFile.IssueManager.GetIssues().OrderBy(issue => issue.Parent.Type).DistinctBy(mainIssue => mainIssue.LayerIndex);

        foreach (var issue in issues)
        {
            var color = Brushes.Red;

            if (Settings.LayerPreview.UseIssueColorOnTracker) colorDictionary.TryGetValue(issue.Parent.Type, out color);

            var yPos = tickFrequencySize * issue.LayerIndex;
            if (issue.LayerIndex == 0 && stroke > 3)
            {
                yPos += tickFrequencySize / 2;
            }
            else if (issue.LayerIndex == SlicerFile.LastLayerIndex && stroke > 3)
            {
                yPos -= tickFrequencySize / 2;
            }
            var line = new Line { StrokeThickness = stroke, Stroke = color, EndPoint = new Avalonia.Point(_issuesSliderCanvas.Width, 0) };
            _issuesSliderCanvas.Children.Add(line);
            Canvas.SetBottom(line, yPos);
        }

        /*for (int layerIndex = 0; layerIndex < SlicerFile.LayerCount; layerIndex++)
        {
            var color = Brushes.Red;

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
        }*/
            
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
        if (!IsFileLoaded) return;
        SlicerFile.IssueManager.Clear();
        if(clearIgnored) SlicerFile.IssueManager.IgnoredIssues.Clear();
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

    public IssuesDetectionConfiguration GetIssuesDetectionConfiguration()
    {
        return new IssuesDetectionConfiguration(
            GetIslandDetectionConfiguration(),
            GetOverhangDetectionConfiguration(),
            GetResinTrapDetectionConfiguration(),
            GetTouchingBoundsDetectionConfiguration(),
            GetPrintHeightDetectionConfiguration(),
            GetEmptyLayerDetectionConfiguration()
        );
    }

    public IssuesDetectionConfiguration GetIssuesDetectionConfiguration(bool enable)
    {
        return new IssuesDetectionConfiguration(
            GetIslandDetectionConfiguration(enable),
            GetOverhangDetectionConfiguration(enable),
            GetResinTrapDetectionConfiguration(enable),
            GetTouchingBoundsDetectionConfiguration(enable),
            GetPrintHeightDetectionConfiguration(enable),
            GetEmptyLayerDetectionConfiguration(enable)
        );
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
            RequiredAreaToConsiderSuctionCup = Settings.Issues.SuctionCupRequiredAreaToConsider,
            RequiredHeightToConsiderSuctionCup = Settings.Issues.SuctionCupRequiredHeightToConsider
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

    public EmptyLayerDetectionConfiguration GetEmptyLayerDetectionConfiguration(bool enable)
    {
        return new()
        {
            Enabled = enable,
        };
    }
    public EmptyLayerDetectionConfiguration GetEmptyLayerDetectionConfiguration() => GetEmptyLayerDetectionConfiguration(Settings.Issues.ComputeEmptyLayers);


    #endregion
}