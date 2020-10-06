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
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using DynamicData;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using MessageBox.Avalonia.Enums;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;

namespace UVtools.WPF
{
    public partial class MainWindow
    {
        #region Members
        private ObservableCollection<LayerIssue> _issues = new ObservableCollection<LayerIssue>();
        private bool _firstTimeOnIssues = true;
        public DataGrid IssuesGrid;

        private int _issueSelectedIndex = -1;
        #endregion

        #region Properties
        public ObservableCollection<LayerIssue> Issues
        {
            get => _issues;
            private set => RaiseAndSetIfChanged(ref _issues, value);
        }

        public bool IssueCanGoPrevious => Issues.Count > 0 && _issueSelectedIndex > 0;
        public bool IssueCanGoNext => Issues.Count > 0 && _issueSelectedIndex < Issues.Count - 1;
        #endregion

        #region Methods

        public void InitIssues()
        {
            IssuesGrid = this.FindControl<DataGrid>("IssuesGrid");
            IssuesGrid.CellPointerPressed += IssuesGridOnCellPointerPressed;
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
            var task = await Task.Factory.StartNew(() =>
            {
                ShowProgressWindow();
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
                    Dispatcher.UIThread.InvokeAsync(async () =>
                        await this.MessageBoxError(ex.ToString(), "Removal failed"));
                }

                return result;
            });

            IsGUIEnabled = true;

            if (!task) return;

            var whiteListLayers = new List<uint>();

            // Update GUI
            var issueRemoveList = new List<LayerIssue>();
            foreach (LayerIssue issue in IssuesGrid.SelectedItems)
            {
                if (issue.Type != LayerIssue.IssueType.Island &&
                    issue.Type != LayerIssue.IssueType.ResinTrap &&
                    issue.Type != LayerIssue.IssueType.EmptyLayer) continue;

                issueRemoveList.Add(issue);

                if (issue.Type == LayerIssue.IssueType.Island)
                {
                    var nextLayer = issue.Layer.Index + 1;
                    if (nextLayer >= SlicerFile.LayerCount) continue;
                    if (whiteListLayers.Contains(nextLayer)) continue;
                    whiteListLayers.Add(nextLayer);
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
                UpdateIslandsOverhangs(whiteListLayers);
            }

            ShowLayer(); // It will call latter so its a extra call
            CanSave = true;
        }

        private async void UpdateIslandsOverhangs(List<uint> whiteListLayers)
        {
            if (whiteListLayers.Count == 0) return;
            var islandConfig = GetIslandDetectionConfiguration();
            var overhangConfig = GetOverhangDetectionConfiguration();
            var resinTrapConfig = new ResinTrapDetectionConfiguration { Enabled = false };
            var touchingBoundConfig = new TouchingBoundDetectionConfiguration { Enabled = false };
            islandConfig.Enabled = true;
            islandConfig.WhiteListLayers = whiteListLayers;
            overhangConfig.Enabled = true;
            overhangConfig.WhiteListLayers = whiteListLayers;


            IsGUIEnabled = false;
            ProgressWindow.SetTitle("Updating Issues");


            List<LayerIssue> toRemove = new List<LayerIssue>();
            foreach (var layerIndex in islandConfig.WhiteListLayers)
            {
                foreach (var issue in Issues)
                {
                    if (issue.LayerIndex != layerIndex && (issue.Type == LayerIssue.IssueType.Island || issue.Type == LayerIssue.IssueType.Overhang)) continue;
                    toRemove.Add(issue);
                }
            }
            Issues.RemoveMany(toRemove);

            var resultIssues = await Task.Factory.StartNew(() =>
            {
                ShowProgressWindow();
                try
                {
                    var issues = SlicerFile.LayerManager.GetAllIssues(islandConfig, overhangConfig, resinTrapConfig,
                        touchingBoundConfig, false,
                        ProgressWindow.RestartProgress());

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

            if (resultIssues is null || resultIssues.Count == 0) return;

            Issues.AddRange(resultIssues);
            Issues = new ObservableCollection<LayerIssue>(Issues.OrderBy(issue => issue.Type)
                .ThenBy(issue => issue.LayerIndex)
                .ThenBy(issue => issue.PixelsCount).ToList());
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
                IssuesGridOnSelectionChanged();
            }
        }

        public string IssueSelectedIndexStr => (_issueSelectedIndex + 1).ToString().PadLeft(Issues.Count.ToString().Length, '0');

        private void IssuesGridOnSelectionChanged()
        {
            if (IssuesGrid.SelectedItems.Count != 1) return;
            if (!(IssuesGrid.SelectedItem is LayerIssue issue)) return;

            if (issue.Type == LayerIssue.IssueType.TouchingBound || issue.Type == LayerIssue.IssueType.EmptyLayer ||
                (issue.X == -1 && issue.Y == -1))
            {
                ZoomToFit();
            }
            else if (issue.X >= 0 && issue.Y >= 0)
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
            if (IssuesGrid.SelectedItems.Count != 1 || e.PointerPressedEventArgs.ClickCount == 2) return;
            if (!(IssuesGrid.SelectedItem is LayerIssue issue)) return;
            // Double clicking an issue will center and zoom into the 
            // selected issue. Left click on an issue will zoom to fit.

            var pointer = e.PointerPressedEventArgs.GetCurrentPoint(IssuesGrid);
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

            ForceUpdateActualLayer(issue.LayerIndex);

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

        public void OnClickDetectIssues()
        {
            if (!IsFileLoaded) return;
            ComputeIssues(
                GetIslandDetectionConfiguration(),
                GetOverhangDetectionConfiguration(),
                GetResinTrapDetectionConfiguration(),
                GetTouchingBoundsDetectionConfiguration(),
                Settings.Issues.ComputeEmptyLayers);
        }

        private async void ComputeIssues(IslandDetectionConfiguration islandConfig = null,
            OverhangDetectionConfiguration overhangConfig = null,
            ResinTrapDetectionConfiguration resinTrapConfig = null,
            TouchingBoundDetectionConfiguration touchingBoundConfig = null, bool emptyLayersConfig = true)
        {

            Issues.Clear();
            IsGUIEnabled = false;

            ProgressWindow.SetTitle("Computing Issues");

            var resultIssues = await Task.Factory.StartNew(() =>
            {
                ShowProgressWindow();
                try
                {
                    var issues = SlicerFile.LayerManager.GetAllIssues(islandConfig, overhangConfig, resinTrapConfig, touchingBoundConfig,
                        emptyLayersConfig, ProgressWindow.RestartProgress());
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

            if (resultIssues is null) return;
            Issues.AddRange(resultIssues);

            RaisePropertyChanged(nameof(IssueSelectedIndexStr));
            RaisePropertyChanged(nameof(IssueCanGoPrevious));
            RaisePropertyChanged(nameof(IssueCanGoNext));

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

        public OverhangDetectionConfiguration GetOverhangDetectionConfiguration()
        {
            return new OverhangDetectionConfiguration
            {
                Enabled = Settings.Issues.ComputeOverhangs,
                IndependentFromIslands = Settings.Issues.OverhangIndependentFromIslands,
                ErodeIterations = Settings.Issues.OverhangErodeIterations,
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
    }
}
