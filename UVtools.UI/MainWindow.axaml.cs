/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Platform.Storage;
using Avalonia.Reactive;
using UVtools.AvaloniaControls;
using UVtools.Core;
using UVtools.Core.Dialogs;
using UVtools.Core.Exceptions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Managers;
using UVtools.Core.Network;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.Core.SystemOS;
using UVtools.UI.Controls;
using UVtools.UI.Controls.Calibrators;
using UVtools.UI.Controls.Tools;
using UVtools.UI.Extensions;
using UVtools.UI.Structures;
using UVtools.UI.Windows;
using Path = System.IO.Path;
using Point = Avalonia.Point;

namespace UVtools.UI;

public partial class MainWindow : WindowEx
{
    #region Redirects

    public AppVersionChecker VersionChecker => App.VersionChecker;
    public static ClipboardManager ClipboardManager => ClipboardManager.Instance;
    #endregion

    #region Controls

    //public ProgressWindow ProgressWindow = new ();

    public static MenuItem[] MenuTools { get; } =
    {
        new() { Tag = new OperationEditParameters()},
        new() { Tag = new OperationRepairLayers()},
        new() { Tag = new OperationMove()},
        new() { Tag = new OperationResize()},
        new() { Tag = new OperationFlip()},
        new() { Tag = new OperationRotate()},
        new() { Tag = new OperationSolidify()},
        new() { Tag = new OperationMorph()},
        new() { Tag = new OperationRaftRelief()},
        new() { Tag = new OperationRedrawModel()},
        //new() { Tag = new OperationThreshold()},
        new() { Tag = new OperationLayerArithmetic()},
        new() { Tag = new OperationPixelArithmetic()},
        new() { Tag = new OperationMask()},
        //new() { Tag = new OperationPixelDimming()},
        new() { Tag = new OperationLightBleedCompensation()},
        new() { Tag = new OperationInfill()},
        new() { Tag = new OperationBlur()},
        new() { Tag = new OperationPattern()},
        new() { Tag = new OperationFadeExposureTime()},
        //new() { Tag = new OperationDoubleExposure()},
        new() { Tag = new OperationPhasedExposure()},
        new() { Tag = new OperationDynamicLifts()},
        new() { Tag = new OperationDynamicLayerHeight()},
        new() { Tag = new OperationLayerReHeight()},
        new() { Tag = new OperationRaiseOnPrintFinish()},
        new() { Tag = new OperationChangeResolution()},
        new() { Tag = new OperationTimelapse()},
        new() { Tag = new OperationLithophane()},
        new() { Tag = new OperationPCBExposure()},
        new() { Tag = new OperationScripting()},
        new() { Tag = new OperationCalculator()},
    };

    public static MenuItem[] MenuCalibration { get; } =
    {
        new() { Tag = new OperationCalibrateExposureFinder()},
        new() { Tag = new OperationCalibrateElephantFoot()},
        new() { Tag = new OperationCalibrateXYZAccuracy()},
        new() { Tag = new OperationCalibrateLiftHeight()},
        new() { Tag = new OperationCalibrateBloomingEffect()},
        new() { Tag = new OperationCalibrateTolerance()},
        new() { Tag = new OperationCalibrateGrayscale()},
        new() { Tag = new OperationCalibrateStressTower()},
        new() { Tag = new OperationCalibrateExternalTests()},
    };

    public static MenuItem[] LayerActionsMenu { get; } =
    {
        new() { Tag = new OperationLayerImport()},
        new() { Tag = new OperationLayerClone()},
        new() { Tag = new OperationLayerRemove()},
        new() { Tag = new OperationLayerExportImage()},
        new() { Tag = new OperationLayerExportGif()},
        new() { Tag = new OperationLayerExportHtml()},
        new() { Tag = new OperationLayerExportSkeleton()},
        new() { Tag = new OperationLayerExportHeatMap()},
        new() { Tag = new OperationLayerExportMesh()},
    };


    #endregion

    #region Members

    public Stopwatch LastStopWatch = new();
        
    private bool _isGUIEnabled = true;
    private uint _savesCount;
    private bool _canSave;
    private IEnumerable<MenuItem> _menuFileOpenRecentItems = Array.Empty<MenuItem>();
    private IEnumerable<MenuItem> _menuFileSendToItems = Array.Empty<MenuItem>();
    private IEnumerable<MenuItem> _menuFileConvertItems = Array.Empty<MenuItem>();

    private PointerEventArgs? _globalPointerEventArgs;
    private PointerPoint _globalPointerPoint;
    private KeyModifiers _globalModifiers = KeyModifiers.None;
    private TabItem _selectedTabItem = null!;
    private TabItem _lastSelectedTabItem = null!;

    private long _loadedFileSize;
    private string _loadedFileSizeRepresentation = string.Empty;

    private readonly StringBuilder _titleStringBuilder = new();

    #endregion

    #region  GUI Models
    public bool IsGUIEnabled
    {
        get => _isGUIEnabled;
        set
        {
            if (!RaiseAndSetIfChanged(ref _isGUIEnabled, value)) return;
            if (!_isGUIEnabled)
            {
                DragDrop.SetAllowDrop(this, false);
                //ProgressWindow = new ProgressWindow();
                return;
            }

            DragDrop.SetAllowDrop(this, true);

            LastStopWatch = Progress.StopWatch;
            ProgressFinish();
            //ProgressWindow.Close(DialogResults.OK);
            //ProgressWindow.Dispose();
            /*if (Dispatcher.UIThread.CheckAccess())
            {
                ProgressWindow.Close();
                ProgressWindow.Dispose();
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ProgressWindow.Close();
                    ProgressWindow.Dispose();
                });
            }*/
        }
    }

    public bool IsFileLoaded
    {
        get => SlicerFile is not null;
        set => RaisePropertyChanged();
    }

    public TabItem SelectedTabItem
    {
        get => _selectedTabItem;
        set
        {
            var lastTab = _selectedTabItem;
            if (!RaiseAndSetIfChanged(ref _selectedTabItem, value)) return;
            LastSelectedTabItem = lastTab;
            if (_firstTimeOnIssues)
            {
                _firstTimeOnIssues = false;
                if (ReferenceEquals(_selectedTabItem, TabIssues) && Settings.Issues.ComputeIssuesOnClickTab)
                {
                    Dispatcher.UIThread.InvokeAsync(async () => await OnClickDetectIssues());

                }
            }
        }
    }

    public TabItem LastSelectedTabItem
    {
        get => _lastSelectedTabItem;
        set => RaiseAndSetIfChanged(ref _lastSelectedTabItem, value);
    }

    #endregion

    public uint SavesCount
    {
        get => _savesCount;
        set => RaiseAndSetIfChanged(ref _savesCount, value);
    }

    public bool CanSave
    {
        get => IsFileLoaded && _canSave;
        set => RaiseAndSetIfChanged(ref _canSave, value);
    }

    public IEnumerable<MenuItem> MenuFileOpenRecentItems
    {
        get => _menuFileOpenRecentItems;
        set => RaiseAndSetIfChanged(ref _menuFileOpenRecentItems, value);
    }

    public IEnumerable<MenuItem> MenuFileSendToItems
    {
        get => _menuFileSendToItems;
        set => RaiseAndSetIfChanged(ref _menuFileSendToItems, value);
    }       
        
    public IEnumerable<MenuItem> MenuFileConvertItems
    {
        get => _menuFileConvertItems;
        set => RaiseAndSetIfChanged(ref _menuFileConvertItems, value);
    }


    #region Constructors

    public MainWindow()
    {
        if (Settings.General.RestoreWindowLastPosition)
        {
            Position = new PixelPoint(Settings.General.LastWindowBounds.Location.X, Settings.General.LastWindowBounds.Location.Y);
        }

        if (Settings.General.RestoreWindowLastSize)
        {
            Width = Settings.General.LastWindowBounds.Width;
            Height = Settings.General.LastWindowBounds.Height;
        }

        if (Settings.General.StartMaximized)
        {
            WindowState = WindowState.Maximized;
        }
        else
        {
            var screenSize = this.GetScreenWorkingArea();
            // Use a 20px margin
            if (Width + 20 >= screenSize.Width || Height + 20 >= screenSize.Height)
            {
                WindowState = WindowState.Maximized;
            }
        }

        InitializeComponent();

        //App.ThemeSelector?.EnableThemes(this);
        InitProgress();
        InitInformation();
        InitIssues();
        InitPixelEditor();
        InitClipboardLayers();
        InitLayerPreview();
        InitSuggestions();

        RefreshRecentFiles(true);
            
        foreach (var menuItem in new[] { MenuTools, MenuCalibration, LayerActionsMenu })
        {
            foreach (var menuTool in menuItem)
            {
                if (menuTool.Tag is not Operation operation) continue;
                if (!string.IsNullOrWhiteSpace(operation.IconClass)) menuTool.Icon = new Projektanker.Icons.Avalonia.Icon{Value = operation.IconClass};
                menuTool.Header = operation.Title;
                menuTool.Click += async (sender, args) => await ShowRunOperation(operation.GetType());
            }
        }
            
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

        UpdateTitle();

        DataContext = this;

        MainMenuFile.SubmenuOpened += (sender, e) =>
        {
            if (!IsFileLoaded) return;
                
            var menuItems = new List<MenuItem>();

            var drives = DriveInfo.GetDrives();

            if (drives.Length > 0)
            {
                foreach (var drive in drives)
                {
                    if (drive.DriveType != DriveType.Removable || !drive.IsReady) continue; // Not our target, skip
                    if (SlicerFile!.FileFullPath!.StartsWith(drive.Name))
                        continue; // File already on this device, skip

                    var header = drive.Name;
                    if (!string.IsNullOrWhiteSpace(drive.VolumeLabel))
                    {
                        header += $"  {drive.VolumeLabel}";
                    }

                    header += $"  ({SizeExtensions.SizeSuffix(drive.AvailableFreeSpace)}) [{drive.DriveFormat}]";

                    var menuItem = new MenuItem
                    {
                        Header = header.ReplaceFirst("_", "__"),
                        Tag = drive,
                        Icon = new Projektanker.Icons.Avalonia.Icon{ Value = "fa-brands fa-usb" }
                    };
                    menuItem.Click += FileSendToItemClick;

                    menuItems.Add(menuItem);
                }
            }

            if (Settings.General.SendToCustomLocations.Count > 0)
            {
                foreach (var location in Settings.General.SendToCustomLocations)
                {
                    if(!location.IsEnabled) continue;

                    if (!string.IsNullOrWhiteSpace(location.CompatibleExtensions))
                    {
                        var extensions = location.CompatibleExtensions.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        var found = false;
                        foreach (var ext in extensions)
                        {
                            found = SlicerFile!.FileEndsWith($".{ext}");
                            if (found) break;
                        }
                        if(!found) continue;
                    }

                    var menuItem = new MenuItem
                    {
                        Header = location.ToString().ReplaceFirst("_", "__"),
                        Tag = location,
                        Icon = new Projektanker.Icons.Avalonia.Icon { Value = "fa-solid fa-folder" }
                    };
                    menuItem.Click += FileSendToItemClick;

                    menuItems.Add(menuItem);
                }
            }

            if (Settings.Network.RemotePrinters.Count > 0)
            {
                foreach (var remotePrinter in Settings.Network.RemotePrinters)
                {
                    if(!remotePrinter.IsEnabled || !remotePrinter.IsValid || (!remotePrinter.RequestUploadFile.IsValid && !remotePrinter.RequestPrintFile.IsValid)) continue;

                    if (!string.IsNullOrWhiteSpace(remotePrinter.CompatibleExtensions))
                    {
                        var extensions = remotePrinter.CompatibleExtensions.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        var found = false;
                        foreach (var ext in extensions)
                        {
                            found = SlicerFile!.FileEndsWith($".{ext}");
                            if (found) break;
                        }
                        if (!found) continue;
                    }

                    var menuItem = new MenuItem
                    {
                        Header = remotePrinter.ToString().ReplaceFirst("_", "__"),
                        Tag = remotePrinter,
                        Icon = new Projektanker.Icons.Avalonia.Icon { Value = "fa-solid fa-network-wired" }
                    };
                    menuItem.Click += FileSendToItemClick;

                    menuItems.Add(menuItem);
                }
            }

            if (Settings.General.SendToProcess.Count > 0)
            {
                foreach (var application in Settings.General.SendToProcess)
                {
                    if (!application.IsEnabled ) continue;

                    if (!string.IsNullOrWhiteSpace(application.CompatibleExtensions))
                    {
                        var extensions = application.CompatibleExtensions.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        var found = false;
                        foreach (var ext in extensions)
                        {
                            found = SlicerFile!.FileEndsWith($".{ext}");
                            if (found) break;
                        }
                        if (!found) continue;
                    }

                    var menuItem = new MenuItem
                    {
                        Header = application.Name.ReplaceFirst("_", "__"),
                        Tag = application,
                        Icon = new Projektanker.Icons.Avalonia.Icon { Value = "fa-solid fa-cog" }
                    };
                    menuItem.Click += FileSendToItemClick;

                    menuItems.Add(menuItem);
                }
            }


            MenuFileSendToItems = menuItems;
            MainMenuFileSendTo.IsVisible = MainMenuFileSendTo.IsEnabled = menuItems.Count > 0;
        };

#if DEBUG
        this.AttachDevTools(new KeyGesture(Key.F12, KeyModifiers.Control));
#endif
    }

    private async void FileSendToItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;


        string path;
        if (menuItem.Tag is DriveInfo drive)
        {

            if (!drive.IsReady)
            {
                await this.MessageBoxError($"The device {drive.Name} is not ready/available at this time.",
                    "Unable to copy the file");
                return;
            }

            path = drive.Name;
        }
        else if (menuItem.Tag is MappedDevice device)
        {
            path = device.Path;
        }
        else if (menuItem.Tag is RemotePrinter preRemotePrinter)
        {
            path = preRemotePrinter.HostUrl;
        }
        else if (menuItem.Tag is MappedProcess process)
        {
            path = process.Name;
        }
        else
        {
            return;
        }

        if (CanSave)
        {
            switch (await this.MessageBoxQuestion("There are unsaved changes. Do you want to save the current file before copy it over?\n\n" +
                                                  "Yes: Save the current file and copy it over.\n" +
                                                  "No: Copy the file without current modifications.\n" +
                                                  "Cancel: Abort the operation.", "Send to - Unsaved changes", MessageButtons.YesNoCancel))
            {
                case MessageButtonResult.Yes:
                    await SaveFile(true);
                    break;
                case MessageButtonResult.No:
                    break;
                default:
                    return;
            }
        }

        if (menuItem.Tag is RemotePrinter remotePrinter)
        {
            var startPrint = (_globalModifiers & KeyModifiers.Shift) != 0 && remotePrinter.RequestPrintFile.IsValid;
            if (!startPrint && !remotePrinter.RequestUploadFile.IsValid) return;
            if (startPrint)
            {
                if (!remotePrinter.RequestUploadFile.IsValid)
                {
                    if (await this.MessageBoxQuestion(
                            $"If supported, you are about to start the print with the filename: {SlicerFile!.Filename}\n" +
                            "This file will not upload, so, it will print the file already present in printer disk.\n" +
                            "Keep in mind there is no guarantee that the file will start to print.\n" +
                            "Are you sure you want to continue?\n\n" +
                            "Yes: Print this file name.\n" +
                            "No: Cancel file print.", "Print the filename?") != MessageButtonResult.Yes) return;
                }
                else
                {
                    if (await this.MessageBoxQuestion(
                            "If supported, you are about to send the file and start the print with it.\n" +
                            "Keep in mind there is no guarantee that the file will start to print.\n" +
                            "Are you sure you want to continue?\n\n" +
                            "Yes: Send file and print it.\n" +
                            "No: Cancel file sending and print.", "Send and print the file?") != MessageButtonResult.Yes) return;
                }
                
            }

            ShowProgressWindow($"Sending: {SlicerFile!.Filename} to {path}");
            Progress.ItemName = "Sending";

            HttpResponseMessage? response = null;
            if(remotePrinter.RequestUploadFile.IsValid)
            {
                try
                {
                    response = await remotePrinter.RequestUploadFile.SendRequest(remotePrinter, Progress, SlicerFile.Filename, SlicerFile.FileFullPath);
                    if (!response.IsSuccessStatusCode)
                    {
                        await this.MessageBoxError(response.ToString(), "Send to printer");
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    await this.MessageBoxError(ex.Message, "Send to printer");
                }
            }
                

            if (startPrint && (!remotePrinter.RequestUploadFile.IsValid || (response is not null && response.IsSuccessStatusCode)))
            {
                response?.Dispose();
                Progress.Title = "Waiting 2 seconds...";
                await Task.Delay(2000);
                try
                {
                    response = await remotePrinter.RequestPrintFile.SendRequest(remotePrinter, Progress, SlicerFile.Filename);
                    if (!response.IsSuccessStatusCode)
                    {
                        await this.MessageBoxError(response.ToString(), "Unable to send the print command");
                    }
                    response.Dispose();
                    /*else
                    {
                        await this.MessageBoxInfo(response.ToString(), "Print send command report");
                    }*/
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    await this.MessageBoxError(ex.Message, "Unable to send the print command");
                }
            }
        }
        else if (menuItem.Tag is MappedProcess process)
        {
            ShowProgressWindow($"Sending: {SlicerFile!.Filename} to {path}");
            Progress.ItemName = "Waiting for completion";
            try
            {
                await process.StartProcess(SlicerFile, Progress.Token);
            }
            catch (OperationCanceledException){}
            catch (Exception ex)
            {
                await this.MessageBoxError(ex.Message, $"Unable to start the process {process.Name}");
            }
                
        }
        else
        {
            ShowProgressWindow($"Sending: {SlicerFile!.Filename} to {path}");
            Progress.ItemName = "Sending";

            bool copyResult = false;
            var fileDest = Path.Combine(path, SlicerFile!.Filename!);
            try
            {
                await using var source = File.OpenRead(SlicerFile!.FileFullPath!);
                await using var dest = new FileStream(fileDest, FileMode.Create, FileAccess.Write);

                Progress.Reset("Megabyte(s)", (uint)(source.Length / 1048576));
                var copyProgress = new Progress<long>(copiedBytes => Progress.ProcessedItems = (uint)(copiedBytes / 1048576));
                await source.CopyToAsync(dest, copyProgress, Progress.Token);

                copyResult = true;
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (File.Exists(fileDest)) File.Delete(fileDest);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                    
            }
            catch (Exception exception)
            {
                await this.MessageBoxError(exception.Message, "Unable to copy the file");
            }

            if(copyResult && menuItem.Tag is DriveInfo removableDrive && OperatingSystem.IsWindows() && Settings.General.SendToPromptForRemovableDeviceEject)
            {
                if (await this.MessageBoxQuestion(
                        $"File '{SlicerFile.Filename}' has copied successfully into {removableDrive.Name}\n" +
                        $"Do you want to eject the {removableDrive.Name} drive now?", "Copied ok, eject the drive?") == MessageButtonResult.Yes)
                {

                    Progress.ResetAll($"Ejecting {removableDrive.Name}");
                    var ejectResult = await Task.Run(() =>
                    {
                        try
                        {
                            return Core.SystemOS.Windows.USB.USBEject(removableDrive.Name);
                        }
                        catch (Exception ex)
                        {
                            HandleException(ex, $"Unable to eject the drive { removableDrive.Name}");
                        }

                        return false;
                    }, Progress.Token);

                    if (!ejectResult)
                    {
                        await this.MessageBoxError($"Unable to eject the drive {removableDrive.Name}\n\n" +
                                                   "Possible causes:\n" +
                                                   "- Drive may be busy or locked\n" +
                                                   "- Drive was already ejected\n" +
                                                   "- No permission to eject the drive\n" +
                                                   "- Another error while trying to eject the drive\n\n" +
                                                   "Please try to eject the drive manually.", $"Unable to eject the drive {removableDrive.Name}");
                    }
                }
            }
        }

            
        IsGUIEnabled = true;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var clientSizeObs = this.GetObservable(ClientSizeProperty);
        clientSizeObs.Subscribe(new AnonymousObserver<Size>(size =>
        {
            Settings.General._lastWindowBounds.Width = (int)size.Width;
            Settings.General._lastWindowBounds.Height = (int)size.Height;
            UpdateLayerTrackerHighlightIssues();
        }));
        var windowStateObs = this.GetObservable(WindowStateProperty);
        windowStateObs.Subscribe(new AnonymousObserver<WindowState>(state => UpdateLayerTrackerHighlightIssues()));
        PositionChanged += (sender, args) =>
        {
            Settings.General._lastWindowBounds.X = Math.Max(0, Position.X);
            Settings.General._lastWindowBounds.Y = Math.Max(0, Position.Y);
        };

        AddHandler(DragDrop.DropEvent, (sender, args) =>
        {
            if (!_isGUIEnabled) return;
            var files = args.Data.GetFiles();
            if (files is null) return;
            ProcessFiles(files.Select(file => file.TryGetLocalPath()).ToArray()!);
        });

        AddLog($"{About.Software} start", Program.ProgramStartupTime.Elapsed.TotalSeconds);

        if (Settings.General.CheckForUpdatesOnStartup)
        {
            Task.Run(() => VersionChecker.Check());
        }

        ProcessFiles(Program.Args);

        if (!IsFileLoaded && Settings.General.LoadLastRecentFileOnStartup)
        {
            RecentFiles.Load();
            if (RecentFiles.Instance.Count > 0)
            {
                ProcessFile(Path.Combine(App.ApplicationPath, RecentFiles.Instance[0]));
            }
        }

        if (!IsFileLoaded && Settings.General.LoadDemoFileOnStartup)
        {
            ProcessFile(Path.Combine(App.ApplicationPath, About.DemoFile));
        }
            
        DispatcherTimer.Run(() =>
        {
            UpdateTitle();
            return true;
        }, TimeSpan.FromSeconds(1));
        Program.ProgramStartupTime.Stop();

        /*if (About.IsBirthday)
        {
            this.MessageBoxInfo($"Age: {About.AgeStr}\n" +
                                $"This message will only show today, see you in next year!\n" +
                                $"Thank you for using {About.Software}.", $"Today it's the {About.Software} birthday!").ConfigureAwait(true);
        }*/
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (!UserSettings.Instance.General.StartMaximized && 
            (UserSettings.Instance.General.RestoreWindowLastPosition ||
            UserSettings.Instance.General.RestoreWindowLastSize))
        {
            UserSettings.Save();
        }
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
    #endregion

    #region Overrides


    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _globalPointerEventArgs = e;
        _globalModifiers = e.KeyModifiers;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _globalPointerPoint = e.GetCurrentPoint(this);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _globalPointerPoint = e.GetCurrentPoint(this);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        _globalModifiers = e.KeyModifiers;
        if (e.Handled
            || !IsFileLoaded
            || LayerImageBox.IsPanning
            || LayerImageBox.TrackerImage is not null
            //|| LayerImageBox.Cursor != Cursor.Default
            || LayerImageBox.Cursor == StaticControls.CrossCursor
            || LayerImageBox.Cursor == StaticControls.HandCursor
            || LayerImageBox.SelectionMode == AdvancedImageBox.SelectionModes.Rectangle
           ) return;

        var imageBoxMousePosition = _globalPointerEventArgs?.GetPosition(LayerImageBox) ?? new Point(-1, -1);
        if (imageBoxMousePosition.X < 0 || imageBoxMousePosition.Y < 0) return;
        
        // Pixel Edit is active, Shift is down, and the cursor is over the image region.
        if (e.KeyModifiers == KeyModifiers.Shift)
        {
            if (IsPixelEditorActive)
            {
                LayerImageBox.AutoPan = false;
                LayerImageBox.Cursor = StaticControls.CrossCursor;
                TooltipOverlayText = "Pixel editing is on (SHIFT):\n" +
                                     "» Click over a pixel to draw\n" +
                                     "» Hold CTRL to clear pixels";
                UpdatePixelEditorCursor();
            }
            else
            {
                LayerImageBox.Cursor = StaticControls.CrossCursor;
                LayerImageBox.SelectionMode = AdvancedImageBox.SelectionModes.Rectangle;
                TooltipOverlayText = "ROI & Mask selection mode (SHIFT):\n" +
                                     "» Left-click drag to select a fixed ROI region\n" +
                                     "» Left-click + ALT drag to select specific objects ROI\n" +
                                     "» Right-click on a specific object to select it ROI\n" +
                                     "» Right-click + ALT on a specific object to select it as a Mask\n" +
                                     "» Right-click + ALT + CTRL on a specific object to select all it enclosing areas as a Mask\n" +
                                     "Press Esc to clear the ROI & Masks";
            }

            IsTooltipOverlayVisible = Settings.LayerPreview.TooltipOverlay;
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            LayerImageBox.Cursor = StaticControls.HandCursor;
            LayerImageBox.AutoPan = false;
            TooltipOverlayText = "Issue selection mode:\n" +
                                 "» Click over an issue to select it";

            IsTooltipOverlayVisible = Settings.LayerPreview.TooltipOverlay;
            e.Handled = true;
            return;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        _globalModifiers = e.KeyModifiers;
        if ((e.Key is Key.LeftShift or Key.RightShift || (e.KeyModifiers & KeyModifiers.Shift) != 0) &&
            (e.KeyModifiers & KeyModifiers.Control) != 0 &&
            e.Key == Key.Z)
        {
            e.Handled = true;
            ClipboardUndoAndRerun(true);
            return;
        }

        if (e.Key is Key.LeftShift or Key.RightShift 
            || (e.KeyModifiers & KeyModifiers.Shift) == 0 || (e.KeyModifiers & KeyModifiers.Control) == 0)
        {
            LayerImageBox.TrackerImage = null;
            LayerImageBox.Cursor = StaticControls.ArrowCursor;
            LayerImageBox.AutoPan = true;
            LayerImageBox.SelectionMode = AdvancedImageBox.SelectionModes.None;
            IsTooltipOverlayVisible = false;
            e.Handled = true;
            return;
        }

            

        base.OnKeyUp(e);
    }
        
    #endregion

    #region Events

    public void MenuFileOpenClicked() => OpenFile();
    public void MenuFileOpenNewWindowClicked() => OpenFile(true);
    public void MenuFileOpenInPartialModeClicked() => OpenFile(false, FileFormat.FileDecodeType.Partial);

    public void MenuFileOpenContainingFolderClicked()
    {
        if (!IsFileLoaded) return;
        SystemAware.SelectFileOnExplorer(SlicerFile!.FileFullPath!);
    }

    public async void MenuFileRenameClicked()
    {
        await RenameCurrentFile();
    }

    public async Task<bool> RenameCurrentFile()
    {
        if (!IsFileLoaded) return false;
        var control = new RenameFileControl();
        var window = new ToolWindow(control, "Rename the current file with a new name", false, false)
        {
            Title = $"Rename \"{SlicerFile!.FilenameNoExt}\" file",
            ButtonOkText = "Rename",
        };

        var result = await window.ShowDialog<DialogResults>(this);

        if (result != DialogResults.OK) return false;

        RemoveRecentFile(control.OldFilePath);
        AddRecentFile(control.NewFilePath);

        return true;
    }

    public async void MenuFileSaveClicked()
    {
        if (!CanSave) return;
        await SaveFile();
    }

    public async void MenuFileSaveAsClicked()
    {
        //await this.MessageBoxInfo(Path.Combine(App.ApplicationPath, "Assets", "Themes"));
        if (!IsFileLoaded) return;
        var filename = FileFormat.GetFileNameStripExtensions(SlicerFile!.FileFullPath!, out var ext);
        //var ext = Path.GetExtension(SlicerFile.FileFullPath);
        //var extNoDot = ext.Remove(0, 1);
        var extension = FileExtension.Find(ext);
        if (extension is null)
        {
            await this.MessageBoxError("Unable to find the target extension.", "Invalid extension");
            return;
        }

        var defaultDirectory = string.IsNullOrWhiteSpace(Settings.General.DefaultDirectorySaveFile)
            ? Path.GetDirectoryName(SlicerFile.FileFullPath)
            : Settings.General.DefaultDirectorySaveFile;
        var defaultFilename = $"{filename}_copy";

        try
        {
            if (!string.IsNullOrWhiteSpace(Settings.General.FileSaveAsDefaultName))
            {
                defaultFilename = Settings.General.FileSaveAsDefaultName;
                var matches = Regex.Matches(defaultFilename, @"{([a-zA-z]+)}");
                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        if (!match.Success) continue;
                        var property = SlicerFile.GetType().GetProperty(match.Groups[1].Value, BindingFlags.Public | BindingFlags.Instance);
                        if (property is null || !property.CanRead || property.GetMethod is null)
                        {
                            defaultFilename = defaultFilename.Replace(match.Value, null);
                            continue;
                        }

                        defaultFilename = defaultFilename.Replace(match.Value, property.GetValue(SlicerFile)?.ToString());
                    }
                }

                if (!string.IsNullOrWhiteSpace(Settings.General.FileSaveAsDefaultNameCleanUpRegex))
                {
                    filename = Regex.Replace(filename, Settings.General.FileSaveAsDefaultNameCleanUpRegex, string.Empty);
                }

                defaultFilename = string.Format(defaultFilename, filename);
            }
        }
        catch
        {
            defaultFilename = $"{filename}_copy";
        }

        var i = 0;
        var searchFile = Path.Combine(defaultDirectory!, $"{defaultFilename}.{ext}");
        while (File.Exists(searchFile))
        {
            i++;
            searchFile = Path.Combine(defaultDirectory!, $"{defaultFilename}{i}.{ext}");
        }

        if (i > 0)
        {
            defaultFilename = $"{defaultFilename}{i}";
        }

        using var file = await SaveFilePickerAsync(defaultDirectory, defaultFilename,
                AvaloniaStatic.CreateFilePickerFileTypes(extension.Description, ext));

        if (file?.TryGetLocalPath() is not { } filePath) return;

        await SaveFile(filePath);
    }

    public async void OpenFile(bool newWindow = false, FileFormat.FileDecodeType fileDecodeType = FileFormat.FileDecodeType.Full)
    {
        var filters = AvaloniaStatic.ToAvaloniaFileFilter(FileFormat.AllFileFiltersAvalonia);
        var orderedFilters = new List<FilePickerFileType> {filters[Settings.General.DefaultOpenFileExtensionIndex]};
        for (int i = 0; i < filters.Count; i++)
        {
            if(i == Settings.General.DefaultOpenFileExtensionIndex) continue;
            orderedFilters.Add(filters[i]);
        }

        var files = await OpenFilePickerAsync(Settings.General.DefaultDirectoryOpenFile,
            orderedFilters, allowMultiple: true);

        if (files.Count == 0) return;

        ProcessFiles(files.Select(file => file.TryGetLocalPath()!).ToArray(), newWindow, fileDecodeType);
    }

    public async void MenuFileCloseFileClicked()
    {
        if (CanSave && await this.MessageBoxQuestion("There are unsaved changes. Do you want close this file without saving?") !=
            MessageButtonResult.Yes)
        {
            return;
        }

        CloseFile();
    }

    public void CloseFile()
    {
        if (!IsFileLoaded) return;

        MenuFileConvertItems = Array.Empty<MenuItem>();

        ClipboardManager.Instance.Reset();

        //TabGCode.IsVisible = false;

        IssuesClear(true);
        SlicerFile?.Dispose();
        SlicerFile = null;

        SlicerProperties.Clear();
        Drawings.Clear();

        SelectedTabItem = TabInformation;
        _firstTimeOnIssues = true;
        IsPixelEditorActive = false;
        CanSave = false;

        _actualLayer = 0;
        LayerCache.Clear();
        _resinTrapDetectionStartLayer = 0;

        VisibleThumbnailIndex = -1;

        LayerImageBox.Image = null;
        LayerPixelPicker.Reset();

        ClearROIAndMask();

        if(!Settings.Tools.LastUsedSettingsKeepOnCloseFile) OperationSessionManager.Instance.Clear();
        if(_menuFileOpenRecentItems.Any())
        {
            _menuFileOpenRecentItems.First().IsEnabled = true; // Re-enable last file
        }

        UpdateLoadedFileSize();

        ResetDataContext();
    }

    public void MenuFileFullscreenClicked()
    {
        WindowState = WindowState == WindowState.FullScreen ? WindowState.Maximized : WindowState.FullScreen;
    }

    public async void MenuFileSettingsClicked()
    {
        var oldTheme = Settings.General.Theme;
        var oldLayerCompressionCodec = Settings.General.LayerCompressionCodec;
        var settingsWindow = new SettingsWindow();
        await settingsWindow.ShowDialog(this);
        if (settingsWindow.DialogResult == DialogResults.OK)
        {
            if (oldTheme != Settings.General.Theme)
            {
                App.ApplyTheme();
            }

            App._fluentTheme.DensityStyle = Settings.General.ThemeDensity;

            if (oldLayerCompressionCodec != Settings.General.LayerCompressionCodec && IsFileLoaded)
            {
                /*IsGUIEnabled = false;
                ShowProgressWindow($"Changing layers compression codec from {oldLayerCompressionCodec.ToString().ToUpper()} to {Settings.General.LayerCompressionCodec.ToString().ToUpper()}");

                await Task.Run(() =>
                {
                    try
                    {
                        SlicerFile!.ChangeLayersCompressionMethod(Settings.General.LayerCompressionCodec, Progress);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex, "Error while converting layers");
                    }

                    return false;
                });

                IsGUIEnabled = true;*/

                SlicerFile!.ChangeLayersCompressionMethod(Settings.General.LayerCompressionCodec);
            }

            _layerNavigationSliderDebounceTimer.Interval = Settings.LayerPreview.LayerSliderDebounce == 0 ? 1 : Settings.LayerPreview.LayerSliderDebounce;
            RaisePropertyChanged(nameof(IssuesGridItems));
        }
    }

    public async void MenuHelpAboutClicked()
    {
        await new AboutWindow().ShowDialog(this);
    }

    public void MenuHelpFreeUnusedRAMClicked()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    public async void MenuHelpBenchmarkClicked()
    {
        await new BenchmarkWindow().ShowDialog(this);
    }

    public void MenuHelpOpenSettingsFolderClicked()
    {
        SystemAware.StartProcess(UserSettings.SettingsFolder);
    }

    public void MenuHelpReportIssueClicked()
    {
        var system = string.Empty;

        try
        {
            system = AboutWindow.GetEssentialInformationStatic();
        }
        catch
        {
            // ignored
        }

        SystemAware.OpenBrowser($"https://github.com/sn4k3/UVtools/issues/new?template=bug_report_form.yml&title=%5BBug%5D+&system={HttpUtility.UrlEncode(system)}");
    }
    
    public async void MenuHelpMaterialManagerClicked()
    {
        await new MaterialManagerWindow().ShowDialog(this);
    }

    public async void MenuHelpInstallProfilesClicked()
    {
        var PSFolder = App.GetPrusaSlicerDirectory();
        if (string.IsNullOrEmpty(PSFolder) || !Directory.Exists(PSFolder))
        {
            var SSFolder = App.GetPrusaSlicerDirectory(true);
            if (string.IsNullOrEmpty(SSFolder) || !Directory.Exists(SSFolder))
            {
                if (await this.MessageBoxQuestion(
                        "Unable to detect PrusaSlicer nor SuperSlicer on your system, please ensure you have latest version installed.\n" +
                        $"Was looking on: {PSFolder} and {SSFolder}\n\n" +
                        "Click 'Yes' to open the PrusaSlicer webpage for download\n" +
                        "Click 'No' to dismiss",
                        "Unable to detect PrusaSlicer") == MessageButtonResult.Yes) 
                    SystemAware.OpenBrowser("https://www.prusa3d.com/prusaslicer/");
                return;
            }
        }
        await new PrusaSlicerManagerWindow().ShowDialog(this);
    }
    
    public void MenuHelpDebugOpenExecutableDirectoryClicked()
    {
        SystemAware.StartProcess(App.ApplicationPath);
    }

    public void MenuHelpDebugThrowExceptionClicked()
    {
        var i = 1 / new Random().Next(0);
        Debug.WriteLine(i);
    }

    public async void MenuHelpDebugLongMessageBoxClicked()
    {
        await this.MessageBoxError(string.Concat(Enumerable.Repeat("Informative message:\n\nLorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.\n", 100)));
    }

    public void MenuHelpDebugTriggerNewUpdateClicked()
    {
        VersionChecker.Check(true);
    }

    public async void MenuNewVersionClicked()
    {
        var autoUpdateButton = MessageWindow.CreateButton("Auto update", MessageWindow.IconButtonDownload);
        var manualUpdateButton = MessageWindow.CreateButton("Manual update", MessageWindow.IconButtonOpenBrowser);

        var messageBox = new MessageWindow($"Update UVtools to v{VersionChecker.Version}?",
            MessageWindow.IconHeaderQuestion,
            $"Do you like to update {About.Software} from v{About.VersionStr} to v{VersionChecker.Version}?",
            "## Changelog:\n\n" +
            $"{VersionChecker.Changelog}",
            string.IsNullOrWhiteSpace(VersionChecker.DownloadLink) ?
                new[]
                {
                    manualUpdateButton,
                    MessageWindow.CreateCancelButton()
                }
                : new[]
                {
                    autoUpdateButton,
                    manualUpdateButton,
                    MessageWindow.CreateCancelButton()
                },
            true);

        var result = await messageBox.ShowDialog<ButtonWithIcon>(this);

        if (ReferenceEquals(result, autoUpdateButton))
        {
            IsGUIEnabled = false;
            ShowProgressWindow($"Downloading: {VersionChecker.Filename}");
            await VersionChecker.AutoUpgrade(Progress);
            IsGUIEnabled = true;
        }
        else if (ReferenceEquals(result, manualUpdateButton))
        {
            SystemAware.OpenBrowser(VersionChecker.UrlLatestRelease);
        }
    } 

    #endregion

    #region Methods

    private void UpdateLoadedFileSize()
    {
        if (IsFileLoaded)
        {
            _loadedFileSize = SlicerFile!.FileInfo?.Length ?? 0;
            _loadedFileSizeRepresentation = SizeExtensions.SizeSuffix(_loadedFileSize);
        }
        else
        {
            _loadedFileSize = 0;
            _loadedFileSizeRepresentation = "N/A";
        }
        
    }

    private void UpdateTitle()
    {
        _titleStringBuilder.Clear();
        _titleStringBuilder.Append($"{About.Software}   ");
        
        if (IsFileLoaded)
        {
            _titleStringBuilder.Append($"File: {SlicerFile!.Filename} ({_loadedFileSizeRepresentation})");
        }

        _titleStringBuilder.Append($"   Version: {About.VersionStr}   RAM: {SizeExtensions.SizeSuffix(Environment.WorkingSet)}");

        if (IsFileLoaded)
        {
            switch (LastStopWatch.ElapsedMilliseconds)
            {
                case < 1000:
                    _titleStringBuilder.Append($"   LO: {LastStopWatch.ElapsedMilliseconds}ms");
                    break;
                case < 60_000:
                    _titleStringBuilder.Append($"   LO: {LastStopWatch.Elapsed.Seconds}s");
                    break;
                default:
                    _titleStringBuilder.Append($"   LO: {LastStopWatch.Elapsed.Minutes}m {LastStopWatch.Elapsed.Seconds}s");
                    break;
            }

            if (CanSave)
            {
                _titleStringBuilder.Append("   [UNSAVED]");
            }

            if (SlicerFile!.DecodeType == FileFormat.FileDecodeType.Partial)
            {
                _titleStringBuilder.Append("   [PARTIAL MODE]");
            }
        }

        //_titleStringBuilder.Append($"   [{RuntimeInformation.RuntimeIdentifier}]");
#if DEBUG
        _titleStringBuilder.Append("   [DEBUG]");
#endif

        Title = _titleStringBuilder.ToString();
    }

    public async void ProcessFiles(string[] files, bool openNewWindow = false, FileFormat.FileDecodeType fileDecodeType = FileFormat.FileDecodeType.Full)
    {
        if (files.Length == 0) return;

        for (int i = 0; i < files.Length; i++)
        {
            if (!File.Exists(files[i])) continue;

            if (files[i].EndsWith(".uvtop", StringComparison.OrdinalIgnoreCase))
            {
                if(!IsFileLoaded) continue;
                try
                {
                    var operation = Operation.Deserialize(files[i], SlicerFile);
                    if (operation is null) continue;
                    if ((_globalModifiers & KeyModifiers.Shift) != 0) await RunOperation(operation);
                    else await ShowRunOperation(operation);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
                    
                continue;
            }

            if (files[i].EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || files[i].EndsWith(".csx", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsFileLoaded) continue;
                try
                {
                    var operation = new OperationScripting(SlicerFile!);
                    //operation.FilePath = files[i];
                    await Task.Run(() => operation.ReloadScriptFromFile(files[i]));
                    
                    if (operation.CanExecute)
                    {
                        if ((_globalModifiers & KeyModifiers.Shift) != 0) await RunOperation(operation);
                        else await ShowRunOperation(operation);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }

                continue;
            }


            if (i == 0 && !openNewWindow && (_globalModifiers & KeyModifiers.Shift) == 0)
            {
                ProcessFile(files[i], fileDecodeType);
                continue;
            }

            App.NewInstance(files[i]);

        }
    }

    public void ReloadFile() => ReloadFile(_actualLayer);

    public void ReloadFile(uint actualLayer)
    {
        if (!IsFileLoaded) return;
        ProcessFile(SlicerFile!.FileFullPath!, SlicerFile.DecodeType, _actualLayer);
    }

    void ProcessFile(string fileName, uint actualLayer = 0) => ProcessFile(fileName, FileFormat.FileDecodeType.Full, actualLayer);
    async void ProcessFile(string fileName, FileFormat.FileDecodeType fileDecodeType, uint actualLayer = 0)
    {
        if (!File.Exists(fileName)) return;
        CloseFile();
        var fileNameOnly = Path.GetFileName(fileName);
        SlicerFile = FileFormat.FindByExtensionOrFilePath(fileName, true);
        if (SlicerFile is null) return;
        
        IsGUIEnabled = false;
        ShowProgressWindow($"Opening: {fileNameOnly}");

        /*var success = false;
        try
        {
            await SlicerFile.DecodeAsync(fileName, fileDecodeType, Progress);
            success = true;
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            await this.MessageBoxError(exception.ToString(), "Error opening the file");
        }*/

        var task = await Task.Run( () =>
        {
            try
            {
                SlicerFile.Decode(fileName, fileDecodeType, Progress);
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error opening the file");
            }


            return false;
        }, Progress.Token);

        IsGUIEnabled = true;

        if (!task)
        {
            SlicerFile.Dispose();
            SlicerFile = null;
            return;
        }

        AddRecentFile(fileName);

        if (!SlicerFile.HaveLayers)
        {
            await this.MessageBoxError("It seems this file has no layers.  Possible causes could be:\n" +
                                       "- File is empty\n" +
                                       "- File is corrupted\n" +
                                       "- File has not been sliced\n" +
                                       "- An internal programing error\n\n" +
                                       "Please check your file and retry", "Error reading file");
            SlicerFile.Dispose();
            SlicerFile = null;
            return;
        }

        UpdateLoadedFileSize();

        var fileNameNoExt = SlicerFile.FilenameNoExt!;
        if (!FileFormat.IsFileNameValid(fileNameNoExt, out var message, Settings.Automations.FileNameOnlyAsciiCharacters))
        {
            var currentSetting = Settings.Automations.FileNameOnlyAsciiCharacters
                ? "restrict the file name to valid ASCII characters"
                : "allow all valid characters on the file name";
            if (await this.MessageBoxWaring($"{message}\n\n" +
                                            "Some printers can only show and print files with valid ASCII characters.\n" +
                                            "Ensure a proper file name is important, and then is recommended to rename the file with sane characters.\n" +
                                            $"Your current setting is to {currentSetting}. This option can be changed on \"Settings - Automations\".\n\n" +
                                            "Do you want to rename the file now?",
                    $"File \"{fileNameNoExt}\" have invalid characters",
                    MessageButtons.YesNo) ==
                MessageButtonResult.Yes)
            {
                await RenameCurrentFile();
            }
        }

        if (Settings.Automations.AutoConvertFiles && SlicerFile.DecodeType == FileFormat.FileDecodeType.Full)
        {
            string? convertFileExtension = null;
            switch (SlicerFile)
            {
                case SL1File sl1File:
                    convertFileExtension = sl1File.LookupCustomValue<string>(SL1File.Keyword_FileFormat, null);
                    break;
                case VDTFile vdtFile:
                    if (string.IsNullOrWhiteSpace(vdtFile.ManifestFile.Machine.UVToolsConvertTo) || vdtFile.ManifestFile.Machine.UVToolsConvertTo == "None")
                        convertFileExtension = vdtFile.LookupCustomValue<string>(SL1File.Keyword_FileFormat, null);
                    else
                        convertFileExtension = vdtFile.ManifestFile.Machine.UVToolsConvertTo;
                    break;
            }

            if (!string.IsNullOrWhiteSpace(convertFileExtension))
            {
                convertFileExtension = convertFileExtension.ToLower(CultureInfo.InvariantCulture);
                var fileExtension = FileFormat.FindExtension(convertFileExtension);
                //var convertToFormat = FileFormat.FindByExtensionOrFilePath(convertFileExtension);
                var convertToFormat = fileExtension?.GetFileFormat();
                if (fileExtension is not null && convertToFormat is not null)
                {
                    var directory = SlicerFile.DirectoryPath!;
                    var oldFile = SlicerFile.FileFullPath;
                    var oldFileName = SlicerFile.Filename;
                    var filenameNoExt = SlicerFile.FilenameNoExt;
                    var targetFilename = $"{filenameNoExt}.{convertFileExtension}";
                    var outputFile = Path.Combine(directory, targetFilename);
                    FileFormat? convertedFile = null;

                    bool canConvert = true;
                    if (File.Exists(outputFile))
                    {
                        var result = await this.MessageBoxQuestion(
                            $"The file '{SlicerFile.Filename}' is about to get auto-converted to '{targetFilename}'.\n" +
                            $"But a file with same name already exists on the output directory '{directory}'.\n" +
                            "Do you want to overwrite the existing file?\n\n" +
                            "Yes: Overwrite the file.\n" +
                            "No: Choose a location for the file.\n" +
                            "Cancel: Do not auto-convert the file.",

                            $"File '{SlicerFile.Filename}' already exists",
                             MessageButtons.YesNoCancel);

                        if (result is MessageButtonResult.Cancel or MessageButtonResult.Abort)
                        {
                            canConvert = false;
                        }
                        else if (result == MessageButtonResult.No)
                        {
                            using var file = await SaveFilePickerAsync(directory, filenameNoExt,
                                    AvaloniaStatic.CreateFilePickerFileTypes(fileExtension.Description, convertFileExtension));

                            if (file?.TryGetLocalPath() is not { } filePath)
                            {
                                canConvert = false;
                            }
                            else
                            {
                                outputFile = filePath;
                            }
                        }
                    }

                    if (canConvert)
                    {
                        IsGUIEnabled = false;
                        ShowProgressWindow($"Converting {Path.GetFileName(SlicerFile.FileFullPath)} to {convertFileExtension}");

                        task = await Task.Run(() =>
                        {
                            try
                            {
                                convertedFile = SlicerFile.Convert(convertToFormat, outputFile, 0, Progress);
                                return true;
                            }
                            catch (Exception ex)
                            {
                                HandleException(ex, "Error while converting the file");
                            }

                            return false;
                        }, Progress.Token);

                        IsGUIEnabled = true;

                        if (task && convertedFile is not null)
                        {
                            SlicerFile = convertedFile;
                            AddRecentFile(SlicerFile!.FileFullPath!);

                            bool removeSourceFile = false;
                            switch (Settings.Automations.RemoveSourceFileAfterAutoConversion)
                            {
                                case RemoveSourceFileAction.Yes:
                                    removeSourceFile = true;
                                    break;
                                case RemoveSourceFileAction.Prompt:
                                    if (await this.MessageBoxQuestion($"File was successfully converted to: {targetFilename}\n" +
                                                                      $"Do you want to remove the source file: {oldFileName}", $"Remove source file: {oldFileName}") == MessageButtonResult.Yes) removeSourceFile = true;
                                    break;
                            }

                            if (removeSourceFile)
                            {
                                try
                                {
                                    File.Delete(oldFile!);
                                    RemoveRecentFile(oldFile!);
                                }
                                catch (Exception e)
                                {
                                    Debug.WriteLine(e);
                                }
                            }
                        }
                    }
                }
            }
        }

        /*bool modified = false;
        if (modified)
        {
            CanSave = true;
            if (Settings.Automations.SaveFileAfterModifications)
            {
                var saveCount = _savesCount;
                await SaveFile(null, true);
                _savesCount = saveCount;
            }
        }*/

        ClipboardManager.Init(SlicerFile);

        if (SlicerFile is not ImageFile && SlicerFile.DecodeType == FileFormat.FileDecodeType.Full)
        {
            List<MenuItem> menuItems = new();

            var convertMenu = new Dictionary<string, List<MenuItem>>();

            foreach (var fileFormat in FileFormat.AvailableFormats)
            {
                if(fileFormat is ImageFile) continue;

                List<MenuItem>? parentMenu;
                if (string.IsNullOrWhiteSpace(fileFormat.ConvertMenuGroup))
                {
                    parentMenu = menuItems;
                }
                else
                {
                    if (!convertMenu.TryGetValue(fileFormat.ConvertMenuGroup, out parentMenu))
                    {
                        parentMenu = new List<MenuItem>();

                        var subMenuItem = new MenuItem
                        {
                            Header = fileFormat.ConvertMenuGroup,
                            Tag = fileFormat.ConvertMenuGroup,
                        };
                        menuItems.Add(subMenuItem);

                        convertMenu.TryAdd(fileFormat.ConvertMenuGroup, parentMenu);
                    }
                }

                
                foreach (var fileExtension in fileFormat.FileExtensions)
                {
                    if(!fileExtension.IsVisibleOnConvertMenu) continue;

                    var menuItem = new MenuItem
                    {
                        Header = fileExtension.Description,
                        Tag = fileExtension
                    };

                    menuItem.Click += ConvertToOnClick;

                    parentMenu.Add(menuItem);
                }
            }

            foreach (var menuItem in menuItems)
            {
                if (menuItem.Tag is not string group) continue;
                menuItem.ItemsSource = convertMenu[group];
            }

            MenuFileConvertItems = menuItems;
        }

        foreach (var menuItem in new[] { MenuTools, MenuCalibration, LayerActionsMenu })
        {
            foreach (var menuTool in menuItem)
            {
                if (menuTool.Tag is not Operation operation) continue;
                operation.SlicerFile = SlicerFile;
                menuTool.IsEnabled = operation.CanSpawn && (SlicerFile.DecodeType == FileFormat.FileDecodeType.Full || (SlicerFile.DecodeType == FileFormat.FileDecodeType.Partial && operation.CanRunInPartialMode));
            }
        }

        using var mat = SlicerFile.FirstLayer?.LayerMat;

        VisibleThumbnailIndex = 0;

        UpdateTitle();

        if (mat is not null)
        {
            if (Settings.LayerPreview.AutoRotateLayerBestView)
            {
                _showLayerImageRotated = mat.Height > mat.Width;
            }

            if (Settings.LayerPreview.AutoFlipLayerIfMirrored)
            {
                if (SlicerFile.DisplayMirror == FlipDirection.None)
                {
                    _showLayerImageFlipped = false;
                }
                else
                {
                    _showLayerImageFlipped = true;
                    _showLayerImageFlippedHorizontally = SlicerFile.DisplayMirror is FlipDirection.Horizontally or FlipDirection.Both;
                    _showLayerImageFlippedVertically = SlicerFile.DisplayMirror is FlipDirection.Vertically or FlipDirection.Both;
                }
            }

            if (mat.Size != SlicerFile.Resolution)
            {
                var result = await this.MessageBoxWaring($"Layer image resolution of {mat.Size} mismatch with printer resolution of {SlicerFile.Resolution}.\n" +
                                                         "1) Printing this file can lead to problems or malformed model, please verify your slicer printer settings;\n" +
                                                         "2) Processing this file with some of the tools can lead to program crash or dysfunction;\n" +
                                                         "3) If you used PrusaSlicer to slice this file, you must use it with compatible UVtools printer profiles (Help - Install profiles into PrusaSlicer).\n\n" +
                                                         "Click 'Yes' to auto fix and set the file resolution with the layer resolution, but only use this option if you are sure it's ok to!\n" +
                                                         "Click 'No' to continue as it is and ignore this warning, you can still repair issues and use some of the tools.",
                    "File and layer resolution mismatch!", MessageButtons.YesNo);
                if (result == MessageButtonResult.Yes)
                {
                    SlicerFile.Resolution = mat.Size;
                    RaisePropertyChanged(nameof(LayerResolutionStr));
                    CanSave = true;
                }
            }
        }

        if (SlicerFile.LayerHeight <= 0)
        {
            await new MissingInformationWindow().ShowDialog(this);
        }

        if (SlicerFile.LayerHeight <= 0)
        {
            await this.MessageBoxWaring(
                $"This file have a incorrect or not present layer height of {SlicerFile.LayerHeight}mm\n" +
                $"It may not be required for some printers to work properly, but this information is crucial to {About.Software}.\n",
                "Incorrect layer height detected");
        }

        if (SlicerFile.AvailableVersionsCount > 0)
        {
            var extensions = SlicerFile.GetAvailableVersionsForExtension();
            if (extensions.Length > 0)
            {
                if (!extensions.Contains(SlicerFile.Version))
                {
                    var lastVersion = extensions[^1];
                    var result = await this.MessageBoxQuestion(
                        $"Your printer and file format seems only to support the following versions: ({string.Join(", ", extensions)}). However, the file comes with the version {SlicerFile.Version} set, which is outside the supported range.\n" +
                        $"Keeping that version may or not print on your machine, but can mislead {About.Software} to set and use features that your printer firmware doesn't support or vice versa leading to unexpected behaviors.\n\n" +
                        $"Do you want to change the version {SlicerFile.Version} to the latest supported version {lastVersion}?\n" +
                        $"Yes: Change to version {lastVersion}. (Highly recommended!)\n" +
                        $"No: Keep the version {SlicerFile.Version} while editing, but a latter full encode of the file will force the version {lastVersion}.",
                        $"File version {SlicerFile.Version} is outside the supported range for your printer");
                    if (result == MessageButtonResult.Yes)
                    {
                        SlicerFile.Version = lastVersion;
                        CanSave = true;
                    }
                }
            }
        }
        
        var display = SlicerFile.Display;
        if (!display.HaveZero() &&
            //SlicerFile is not LGSFile &&
            ((SlicerFile.ResolutionX > SlicerFile.ResolutionY &&
              SlicerFile.DisplayWidth < SlicerFile.DisplayHeight)
             || (SlicerFile.ResolutionX < SlicerFile.ResolutionY &&
                 SlicerFile.DisplayWidth > SlicerFile.DisplayHeight)))
        {
            var ppm = SlicerFile.Ppmm;
            var ppmMax = ppm.Max();
            var xRatio = Math.Round(ppmMax - ppm.Width + 1);
            var yRatio = Math.Round(ppmMax - ppm.Height + 1);
            await this.MessageBoxWaring(
                "It looks like this file was sliced with an incorrect image ratio.\n" +
                "Printing this file may produce a stretched model with wrong proportions or a failed print.\n" +
                "Please go back to Slicer configuration and validate the printer resolution and display size.\n\n" +
                $"Resolution: {SlicerFile.Resolution}\n" +
                $"Display: {display}\n" +
                $"Ratio: {xRatio}:{yRatio}\n",
                "Incorrect image ratio detected");
        }
        RefreshProperties();
        ResetDataContext();

        ForceUpdateActualLayer(SlicerFile.SanitizeLayerIndex(actualLayer));

        if (Settings.LayerPreview.ZoomToFitPrintVolumeBounds)
        {
            ZoomToFit();
        }

        SlicerFile.IssueManager.CollectionChanged += (sender, e) =>
        {
            UpdateLayerTrackerHighlightIssues();
        };

        if (SlicerFile.DecodeType == FileFormat.FileDecodeType.Full)
        {
            if (Settings.Issues.ComputeIssuesOnFileLoad == UserSettings.IssuesUserSettings.ComputeIssuesOnFileLoadType.EnabledIssues)
            {
                _firstTimeOnIssues = false;
                await OnClickDetectIssues();
                if (SlicerFile.IssueManager.Count > 0)
                {
                    if (Settings.Issues.AutoRepairIssuesOnLoad) await RunOperation(ToolRepairLayersControl.GetOperationRepairLayers());

                    if (SlicerFile.IssueManager.Count > 0)
                    {
                        SelectedTabItem = TabIssues;
                    }
                }
            }
            else if (Settings.Issues.ComputeIssuesOnFileLoad == UserSettings.IssuesUserSettings.ComputeIssuesOnFileLoadType.TimeInexpensiveIssues)
            {
                var config = GetIssuesDetectionConfiguration(false);
                config.PrintHeightConfig.Enable();
                config.EmptyLayerConfig.Enable();
                await ComputeIssues(config);
                if (SlicerFile.IssueManager.Count > 0)
                {
                    var operation = ToolRepairLayersControl.GetOperationDisabledRepair();
                    operation.RemoveEmptyLayers = true;
                    if (Settings.Issues.AutoRepairIssuesOnLoad) await RunOperation(operation);

                    if (SlicerFile.IssueManager.Count > 0)
                    {
                        _firstTimeOnIssues = false;
                        SelectedTabItem = TabIssues;
                    }
                }
            }
        }

        SlicerFile.PropertyChanged += SlicerFileOnPropertyChanged;

        PopulateSuggestions();

        if (SlicerFile is CTBEncryptedFile)
        {
            if (Settings.General.LockedFilesOpenCounter == 0)
            {
                await this.MessageBoxInfo(CTBEncryptedFile.Preamble, "Information");
            }

            Settings.General.LockedFilesOpenCounter++;
            if (Settings.General.LockedFilesOpenCounter >= UserSettings.GeneralUserSettings.LockedFilesMaxOpenCounter)
            {
                Settings.General.LockedFilesOpenCounter = 0;
            }
            UserSettings.Save();
        }

        //TabGCode.IsVisible = HaveGCode;
    }

    private void SlicerFileOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SlicerFile.Thumbnails))
        {
            RefreshThumbnail();
            return;
        }
        if (e.PropertyName is nameof(SlicerFile.Resolution) or nameof(SlicerFile.Display))
        {
            RaisePropertyChanged(nameof(LayerResolutionStr));
            return;
        }
        if (e.PropertyName == nameof(SlicerFile.Ppmm))
        {
            RaisePropertyChanged(nameof(LayerZoomStr));
            return;
        }
    }

    private async void ShowProgressWindow(string title, bool canCancel = true)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            ProgressShow(title, canCancel);

            //ProgressWindow.SetTitle(title);
            //await ProgressWindow.ShowDialog(this);
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressShow(title, canCancel);
                /*try
                {
                    
                    //ProgressWindow.SetTitle(title);
                    //await ProgressWindow.ShowDialog(this);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }*/
                    
            });
        }
    }

    private async void ConvertToOnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem {Tag: FileExtension fileExtension}) return;

        var fileFormat = fileExtension.GetFileFormat();
        if (fileFormat is null) return;
        uint version = fileFormat.DefaultVersion;
        var availableVersions = fileFormat.GetAvailableVersionsForExtension(fileExtension.Extension);

        if (availableVersions.Length > 1)
        {
            var versionSelectorWindow = new VersionSelectorWindow(fileFormat, fileExtension, availableVersions);
            await versionSelectorWindow.ShowDialog(this);
            switch (versionSelectorWindow.DialogResult)
            {
                case DialogResults.OK:
                    version = versionSelectorWindow.Version;
                    break;
                case DialogResults.Cancel:
                    return;
            }
        }

        using var file = await SaveFilePickerAsync(string.IsNullOrEmpty(Settings.General.DefaultDirectoryConvertFile)
                    ? SlicerFile!.DirectoryPath
                    : Settings.General.DefaultDirectoryConvertFile,
                SlicerFile!.FilenameNoExt,
                new List<FilePickerFileType>{AvaloniaStatic.CreateFilePickerFileType(fileExtension.Description, fileExtension.Extension)});

        if (file?.TryGetLocalPath() is not { } filePath) return;

        IsGUIEnabled = false;
        var oldFileName = SlicerFile.Filename!;
        var oldFile = SlicerFile.FileFullPath!;
        ShowProgressWindow($"Converting {oldFileName} to {Path.GetExtension(filePath)}");

        var task = await Task.Run(() =>
        {
            try
            {
                return SlicerFile.Convert(fileFormat, filePath, version, Progress) is not null;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                string extraMessage = string.Empty;
                if (SlicerFile.FileEndsWith(".sl1"))
                {
                    extraMessage = "Note: When converting from SL1 make sure you have the correct printer selected, you MUST use a UVtools base printer.\n" +
                                   "Go to \"Help\" -> \"Install profiles into PrusaSlicer\" to install printers.\n";
                }

                Dispatcher.UIThread.InvokeAsync(async () =>
                    await this.MessageBoxError($"{extraMessage}{ex}", "Conversion unsuccessful"));
            }
                
            return false;
        }, Progress.Token);

        IsGUIEnabled = true;
        
        if (task)
        {
            var question = await this.MessageBoxQuestion(
                $"Conversion completed in {LastStopWatch.ElapsedMilliseconds / 1000}s\n\n" +
                $"Do you want to open '{Path.GetFileName(filePath)}' in a new window?\n" +
                "Yes: Open in a new window.\n" +
                "No: Open in this window.\n" +
                "Cancel: Do not perform any action.\n",
                "Conversion complete",  MessageButtons.YesNoCancel);

            switch (question)
            {
                case MessageButtonResult.No:
                    ProcessFile(filePath, _actualLayer);
                    break;
                case MessageButtonResult.Yes:
                    App.NewInstance(filePath);
                    break;
            }

            bool removeSourceFile = false;
            switch (Settings.Automations.RemoveSourceFileAfterAutoConversion)
            {
                case RemoveSourceFileAction.Yes:
                    removeSourceFile = true;
                    break;
                case RemoveSourceFileAction.Prompt:
                    if (await this.MessageBoxQuestion($"File was successfully converted to: {Path.GetFileName(filePath)}\n" +
                                                      $"Do you want to remove the source file: {oldFileName}", $"Remove source file: {oldFileName}") == MessageButtonResult.Yes) removeSourceFile = true;
                    break;
            }

            if (removeSourceFile)
            {
                try
                {
                    File.Delete(oldFile!);
                    RemoveRecentFile(oldFile);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }
    }


    public async Task<bool> SaveFile(bool ignoreOverwriteWarning) => await SaveFile(null, ignoreOverwriteWarning);

    public async Task<bool> SaveFile(string? filepath = null, bool ignoreOverwriteWarning = false)
    {
        if (filepath is null) // Not save as
        {
            if (!ignoreOverwriteWarning && SavesCount == 0 && Settings.General.FileSavePromptOverwrite)
            {
                var result = await this.MessageBoxQuestion(
                    "Original input file will be overwritten.  Do you wish to proceed?", "Overwrite file?");

                if(result != MessageButtonResult.Yes) return false;
            }
        }

        IsGUIEnabled = false;
        ShowProgressWindow($"Saving {Path.GetFileName(filepath)}");

        var oldFile = SlicerFile!.FileFullPath;

        var task = await Task.Run( () =>
        {
            try
            {
                SlicerFile.SaveAs(filepath, Progress);
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error while saving the file");
            }

            return false;
        }, Progress.Token);
        
        if (task)
        {
            SavesCount++;
            CanSave = false;
            UpdateLoadedFileSize();
            UpdateTitle();
            RefreshProperties(); // Some fields can change after encoding

            if (Settings.General.FileSaveUpdateNameWithNewInformation && oldFile == SlicerFile.FileFullPath)
            {
                try
                {
                    var newFilename = SlicerFile.FilenameStripExtensions!;
                    newFilename = Regex.Replace(newFilename, @"[0-9]+h[0-9]+m([0-9]+s)?", SlicerFile.PrintTimeString);
                    newFilename = Regex.Replace(newFilename, @"(([0-9]*[.])?[0-9]+)ml", $"{SlicerFile.MaterialMillilitersInteger}ml");
                    if (SlicerFile.RenameFile(newFilename)) RemoveRecentFile(oldFile!);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
                
            }

            if (oldFile != SlicerFile.FileFullPath) AddRecentFile(SlicerFile!.FileFullPath!);
        }

        IsGUIEnabled = true;

        return task;
    }

    public async void ResetLayersProperties()
    {
        if (!IsFileLoaded || !SlicerFile!.SupportPerLayerSettings) return;
        if (await this.MessageBoxQuestion(
                "Are you sure you want to reset layers properties with the global properties of the file?\n" +
                "1. The bottom layers will set with the global bottom properties.\n" +
                "2. The normal layers will set with the global normal properties.\n\n" +
                $"This action will undo any modification you or {About.Software} made to layers (properties), reverting its information to the original state.",
                "Reset layers properties with the global properties of the file?") != MessageButtonResult.Yes) return;

        SlicerFile.RebuildLayersProperties(false);
        RefreshCurrentLayerData();
        CanSave = true;
    }

    public async void IPrintedThisFile()
    {
        if (!IsFileLoaded) return;
        await ShowRunOperation(typeof(OperationIPrintedThisFile));
    }

    public async void ExtractFile()
    {
        if (!IsFileLoaded) return;

        var fileNameNoExt = SlicerFile!.FilenameNoExt!;

        var folders = await OpenFolderPickerAsync(string.IsNullOrEmpty(Settings.General.DefaultDirectoryExtractFile)
            ? SlicerFile.DirectoryPath
            : Settings.General.DefaultDirectoryExtractFile,
            $"A \"{fileNameNoExt}\" folder will be created within your selected folder to dump the contents.");
        if (folders.Count == 0) return;
        string finalPath = Path.Combine(folders[0].TryGetLocalPath()!, fileNameNoExt);

        IsGUIEnabled = false;
        ShowProgressWindow($"Extracting {Path.GetFileName(SlicerFile.FileFullPath)}");

        await Task.Factory.StartNew(() =>
        {
            try
            {
                SlicerFile.Extract(finalPath, true, true, Progress);
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error while try extracting the file");
            }
        });
 

        IsGUIEnabled = true;


        if (await this.MessageBoxQuestion(
                $"Extraction to {finalPath} completed in ({LastStopWatch.ElapsedMilliseconds / 1000}s)\n\n" +
                "'Yes' to open target folder, 'No' to continue.",
                "Extraction complete") == MessageButtonResult.Yes)
        {
            SystemAware.StartProcess(finalPath);
        }

    }

    public void OpenTerminal()
    {
        new TerminalWindow().Show(this);
    }

    #region Operations

    public async Task<Operation?> ShowRunOperation(Operation loadOperation) =>
        await ShowRunOperation(loadOperation.GetType(), loadOperation);

    public async Task<Operation?> ShowRunOperation(Type type, Operation? loadOperation = null)
    {
        var operation = await ShowOperation(type, loadOperation);
        await RunOperation(operation);
        return operation;
    }

    public async Task<Operation?> ShowOperation(Type type, Operation? loadOperation = null)
    {
        var toolTypeBase = typeof(ToolControl);
        var calibrateTypeBase = typeof(CalibrateElephantFootControl);
        var classname = type.Name.StartsWith("OperationCalibrate") ?
            $"{calibrateTypeBase.Namespace}.{type.Name.Remove(0, Operation.ClassNameLength)}Control" :
            $"{toolTypeBase.Namespace}.Tool{type.Name.Remove(0, Operation.ClassNameLength)}Control";
        var controlType = Type.GetType(classname);
        ToolControl? control;

        bool removeContent = false;
        if (controlType is null)
        {
            //controlType = toolTypeBase;
            removeContent = true;
            control = new ToolControl(type.CreateInstance<Operation>(SlicerFile!)!);
        }
        else
        {
            control = controlType.CreateInstance<ToolControl>();
            if (control is null) return null;
        }

        if (!control.CanRun)
        {
            return null;
        }

        if (SlicerFile!.DecodeType == FileFormat.FileDecodeType.Partial && !control.BaseOperation!.CanRunInPartialMode)
        {
            await this.MessageBoxError($"The file was open in partial mode and the tool \"{control.BaseOperation.Title}\" is unable to run in this mode.\n" +
                                       "Please reload the file in full mode in order to use this tool.", "Unable to run in partial mode");
            return null;
        }

        if (removeContent)
        {
            control.IsVisible = false;
        }

            
        if (loadOperation is not null)
        {
            control.BaseOperation = loadOperation;
        }
        var window = new ToolWindow(control);
        await window.ShowDialog(this);
        if (window.DialogResult != DialogResults.OK) return null;
        var operation = control.BaseOperation;
        return operation;
    }

    public async Task<bool> RunOperation(Operation? baseOperation)
    {
        if (baseOperation is null) return false;
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        baseOperation.SlicerFile ??= SlicerFile!;

        switch (baseOperation)
        {
            case OperationEditParameters operation:
                //operation.Execute(); Already applied inside control
                RefreshProperties();
                RefreshCurrentLayerData();
                ResetDataContext();

                PopulateSuggestions();

                CanSave = true;
                return true;
            case OperationIPrintedThisFile operation:
                operation.Execute();
                return true;
            case OperationRepairLayers operation:
                if (SlicerFile!.IssueManager.Count == 0)
                {
                    var config = GetIssuesDetectionConfiguration(false);
                    config.IslandConfig.Enabled = operation.RepairIslands && operation.RemoveIslandsBelowEqualPixelCount > 0;
                    config.ResinTrapConfig.Enabled = operation.RepairResinTraps;

                    if (config.IslandConfig.Enabled || config.ResinTrapConfig.Enabled)
                    {
                        await ComputeIssues(config);
                    }
                }

                break;
        }

        IsGUIEnabled = false;
        ShowProgressWindow(baseOperation.ProgressTitle, baseOperation.CanCancel);
        OperationSessionManager.Instance.Add(baseOperation);

        ClipboardManager.Snapshot();

        var result = await Task.Run(() =>
        {
            try
            {
                return baseOperation.Execute(Progress);
            }
            catch (Exception ex)
            {
                HandleException(ex, $"{baseOperation.Title} Error");
            }

            return false;
        }, Progress.Token);

        IsGUIEnabled = true;

        if (result)
        {
            ClipboardManager.Clip(baseOperation);

            ShowLayer();
            RefreshProperties();
            ResetDataContext();

            PopulateSuggestions();

            CanSave = true;

            if(baseOperation.GetType().Name.StartsWith("OperationCalibrate"))
            {
                IssuesClear();
            }

            switch (baseOperation)
            {
                // Tools
                case OperationRepairLayers:
                    await OnClickDetectIssues();
                    break;
                case OperationMove moveOp:
                    if (SlicerFile!.IssueManager.HaveIssues && SlicerFile.BoundingRectangle != moveOp.OriginalBoundingRectangle)
                    {
                        // Recalculate issues due changed position
                        await OnClickDetectIssues();
                    }
                    break;
            }
        }
        else
        {
            ClipboardManager.RestoreSnapshot();
        }

        if (!string.IsNullOrWhiteSpace(baseOperation.AfterCompleteReport))
        {
            await this.MessageBoxInfo(baseOperation.AfterCompleteReport, $"{baseOperation.Title} report ({LastStopWatch.Elapsed.Hours}h{LastStopWatch.Elapsed.Minutes}m{LastStopWatch.Elapsed.Seconds}s)");
        }

        return result;
    }

    private void RefreshRecentFiles(bool reloadFiles = false)
    {
        if(reloadFiles) RecentFiles.Load();

        var items = new List<MenuItem>();

        foreach (var file in RecentFiles.Instance)
        {
            var item = new MenuItem
            {
                Header = Path.GetFileName(file).ReplaceFirst("_", "__"),
                Tag = file,
                IsEnabled = (!IsFileLoaded || SlicerFile!.FileFullPath != file) && File.Exists(file)
            };
            ToolTip.SetTip(item, file);
            ToolTip.SetPlacement(item, PlacementMode.Right);
            ToolTip.SetShowDelay(item, 100);
            items.Add(item);

            item.Click += MenuFileOpenRecentItemOnClick;
        }
        
        MenuFileOpenRecentItems = items;
    }

    private void RemoveRecentFile(string file)
    {
        if (string.IsNullOrWhiteSpace(file)) return;
        RecentFiles.Load();
        RecentFiles.Instance.Remove(file);
        RecentFiles.Save();
        RefreshRecentFiles();
    }

    private void RemoveRecentFile(IEnumerable<string> files)
    {
        RecentFiles.Load();
        foreach (var file in files)
        {
            RecentFiles.Instance.Remove(file);
        }
        RecentFiles.Save();
        RefreshRecentFiles();
    }

    private void AddRecentFile(string file)
    {
        if (string.IsNullOrWhiteSpace(file)) return;
        if (file == Path.Combine(App.ApplicationPath, About.DemoFile)) return;
        RecentFiles.Load();
        RecentFiles.Instance.Insert(0, file);
        RecentFiles.Save();
        RefreshRecentFiles();
    }

    private async void MenuFileOpenRecentItemOnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string file }) return;
        if (IsFileLoaded && SlicerFile!.FileFullPath == file) return;

        if (_globalModifiers == KeyModifiers.Control)
        {
            if (await this.MessageBoxQuestion("Are you sure you want to purge the non-existing files from the recent list?",
                    "Purge the non-existing files?") == MessageButtonResult.Yes)
            {
                /*if (_globalModifiers == KeyModifiers.Shift)
                {
                    RecentFiles.ClearFiles(true);
                    RefreshRecentFiles();
                    return;
                }*/
                if (RecentFiles.PurgenNonExistingFiles(true) > 0) RefreshRecentFiles();
            }

            return;
        }

        if ((_globalModifiers & KeyModifiers.Control) != 0 &&
            (_globalModifiers & KeyModifiers.Shift) != 0)
        {
            if (await this.MessageBoxQuestion($"Are you sure you want to remove the selected file from the recent list?\n{file}",
                    "Remove the file from recent list?") == MessageButtonResult.Yes)
            {
                RemoveRecentFile(file);
            }

            return;
        }

        if (!File.Exists(file))
        {
            if (await this.MessageBoxQuestion($"The file: {file} does not exists anymore.\n" +
                                              "Do you want to remove this file from recent list?",
                    "The file does not exists") == MessageButtonResult.Yes)
            {
                RecentFiles.Load();
                RecentFiles.Instance.Remove(file);
                RecentFiles.Save();

                RefreshRecentFiles();
            }

            return;
        }

        if (_globalModifiers == KeyModifiers.Shift) App.NewInstance(file);
        else ProcessFile(file);
    }

    #endregion

    #region Error Handling
    public void HandleException(Exception ex, string? title = null)
    {
        switch (ex)
        {
            case OperationCanceledException:
                return;
            case MessageException msgEx:
                Dispatcher.UIThread.InvokeAsync(async () => await this.MessageBoxError(msgEx.Message, title));
                return;
            case AggregateException aggEx:
                {
                    var sb = new StringBuilder();

                    if (aggEx.InnerExceptions.Count > 0)
                    {
                        if (aggEx.InnerExceptions.Count == 1)
                        {
                            if (aggEx.InnerExceptions[0] is MessageException messageException)
                            {
                                if (messageException.Title is not null) title = messageException.Title;
                                sb.AppendLine(aggEx.InnerExceptions[0].Message);
                            }
                            else
                            {
                                sb.AppendLine(aggEx.InnerExceptions[0].ToString());
                            }
                        }
                        else
                        {
                            for (var i = 0; i < aggEx.InnerExceptions.Count; i++)
                            {
                                if (aggEx.InnerExceptions[i] is MessageException { Title: { } } messageException)
                                {
                                    title = messageException.Title;
                                }

                                if (i > 0) sb.AppendLine("---------------");
                                sb.AppendLine($"({i + 1}) {(aggEx.InnerExceptions[i] is MessageException ? aggEx.InnerExceptions[i].Message : aggEx.InnerExceptions[i].ToString())}");
                            }
                        }
                    }

                    Dispatcher.UIThread.InvokeAsync(async () => await this.MessageBoxError(sb.ToString(), title));
                    return;
                }
            default:
                Dispatcher.UIThread.InvokeAsync(async () => await this.MessageBoxError(ex.ToString(), title));
                return;
        }
    }
    #endregion

    #endregion
}