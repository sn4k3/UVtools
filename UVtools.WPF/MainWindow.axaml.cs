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
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MessageBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UVtools.AvaloniaControls;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Managers;
using UVtools.Core.Network;
using UVtools.Core.Operations;
using UVtools.Core.SystemOS;
using UVtools.WPF.Controls;
using UVtools.WPF.Controls.Calibrators;
using UVtools.WPF.Controls.Tools;
using UVtools.WPF.Extensions;
using UVtools.WPF.Structures;
using UVtools.WPF.Windows;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Helpers = UVtools.WPF.Controls.Helpers;
using Path = System.IO.Path;
using Point = Avalonia.Point;

namespace UVtools.WPF
{
    public partial class MainWindow : WindowEx
    {
        #region Redirects

        public AppVersionChecker VersionChecker => App.VersionChecker;
        public static ClipboardManager Clipboard => ClipboardManager.Instance;
        #endregion

        #region Controls

        //public ProgressWindow ProgressWindow = new ();

        public static MenuItem[] MenuTools { get; } =
        {
            new()
            {
                Tag = new OperationEditParameters(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/wrench-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationRepairLayers(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/toolbox-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationMove(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/move-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationResize(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/expand-alt-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationFlip(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/flip-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationRotate(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/sync-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationSolidify(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/square-solid-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationMorph(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/geometry-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationRaftRelief(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/cookie-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationRedrawModel(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/code-branch-16x16.png"))
                }
            },
            /*new()
            {
                Tag = new OperationThreshold(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/th-16x16.png"))
                }
            },*/
            new()
            {
                Tag = new OperationLayerArithmetic(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/square-root-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationPixelArithmetic(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/pixel-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationMask(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/mask-16x16.png"))
                    }
            },
            /*new()
            {
                Tag = new OperationPixelDimming(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/pixel-16x16.png"))
                }
            },*/
            new()
            {
                Tag = new OperationLightBleedCompensation(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/lightbulb-solid-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationInfill(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/stroopwafel-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationBlur(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/blur-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationPattern(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/pattern-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationFadeExposureTime(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/history-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationDoubleExposure(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/equals-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationDynamicLifts(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/angle-double-up-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationDynamicLayerHeight(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/dynamic-layers-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationLayerReHeight(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/ladder-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationRaiseOnPrintFinish(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/level-up-alt-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationChangeResolution(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/resize-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationScripting(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/code-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationCalculator(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/calculator-16x16.png"))
                }
            },
        };

        public static MenuItem[] MenuCalibration { get; } =
        {
            new()
            {
                Tag = new OperationCalibrateExposureFinder(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/sun-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationCalibrateElephantFoot(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/elephant-foot-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationCalibrateXYZAccuracy(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/cubes-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationCalibrateLiftHeight(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/long-arrow-alt-up-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationCalibrateTolerance(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/dot-circle-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationCalibrateGrayscale(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/chart-pie-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationCalibrateStressTower(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/chess-rook-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationCalibrateExternalTests(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/bookmark-16x16.png"))
                }
            },
        };

        public static MenuItem[] LayerActionsMenu { get; } =
        {
            new()
            {
                Tag = new OperationLayerImport(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/file-import-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationLayerClone(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/copy-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationLayerRemove(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/trash-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationLayerExportImage(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/file-image-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationLayerExportGif(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/file-gif-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationLayerExportSkeleton(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/file-image-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationLayerExportHeatMap(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/file-image-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationLayerExportMesh(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/cubes-16x16.png"))
                }
            },
        };

        #region DataSets

        

        #endregion

        #endregion

        #region Members

        public Stopwatch LastStopWatch = new();
        
        private bool _isGUIEnabled = true;
        private uint _savesCount;
        private bool _canSave;
        private MenuItem _menuFileSendTo;
        private MenuItem[] _menuFileOpenRecentItems;
        private MenuItem[] _menuFileSendToItems;
        private MenuItem[] _menuFileConvertItems;

        private PointerEventArgs _globalPointerEventArgs;
        private PointerPoint _globalPointerPoint;
        private KeyModifiers _globalModifiers = KeyModifiers.None;
        private TabItem _selectedTabItem;
        private TabItem _lastSelectedTabItem;

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
                    //ProgressWindow = new ProgressWindow();
                    return;
                }

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

        public TabItem TabInformation { get; }
        public TabItem TabGCode { get; }
        public TabItem TabIssues { get; }
        public TabItem TabPixelEditor { get; }
        public TabItem TabLog { get; }

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
                        OnClickDetectIssues();
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

        public MenuItem[] MenuFileOpenRecentItems
        {
            get => _menuFileOpenRecentItems;
            set => RaiseAndSetIfChanged(ref _menuFileOpenRecentItems, value);
        }

        public MenuItem[] MenuFileSendToItems
        {
            get => _menuFileSendToItems;
            set => RaiseAndSetIfChanged(ref _menuFileSendToItems, value);
        }       
        
        public MenuItem[] MenuFileConvertItems
        {
            get => _menuFileConvertItems;
            set => RaiseAndSetIfChanged(ref _menuFileConvertItems, value);
        }


        #region Constructors

        public MainWindow()
        {
            InitializeComponent();

            //App.ThemeSelector?.EnableThemes(this);
            InitProgress();
            InitInformation();
            InitIssues();
            InitPixelEditor();
            InitClipboardLayers();
            InitLayerPreview();

            RefreshRecentFiles(true);
            
            TabInformation = this.FindControl<TabItem>("TabInformation");
            TabGCode = this.FindControl<TabItem>("TabGCode");
            TabIssues = this.FindControl<TabItem>("TabIssues");
            TabPixelEditor = this.FindControl<TabItem>("TabPixelEditor");
            TabLog = this.FindControl<TabItem>("TabLog");


            foreach (var menuItem in new[] { MenuTools, MenuCalibration, LayerActionsMenu })
            {
                foreach (var menuTool in menuItem)
                {
                    if (menuTool.Tag is not Operation operation) continue;
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

            var clientSizeObs = this.GetObservable(ClientSizeProperty);
            clientSizeObs.Subscribe(size => UpdateLayerTrackerHighlightIssues());
            var windowStateObs = this.GetObservable(WindowStateProperty);
            windowStateObs.Subscribe(windowsState => UpdateLayerTrackerHighlightIssues());


            DataContext = this;

            AddHandler(DragDrop.DropEvent, (sender, e) =>
            {
                ProcessFiles(e.Data.GetFileNames().ToArray());
            });

            _menuFileSendTo = this.FindControl<MenuItem>("MainMenu.File.SendTo");
            this.FindControl<MenuItem>("MainMenu.File").SubmenuOpened += (sender, e) =>
            {
                if (!IsFileLoaded) return;
                
                var menuItems = new List<MenuItem>();

                var drives = DriveInfo.GetDrives();

                foreach (var drive in drives)
                {
                    if(drive.DriveType != DriveType.Removable || !drive.IsReady) continue; // Not our target, skip
                    if (SlicerFile.FileFullPath.StartsWith(drive.Name)) continue; // File already on this device, skip
                    
                    var header = drive.Name;
                    if (!string.IsNullOrWhiteSpace(drive.VolumeLabel))
                    {
                        header += $"  {drive.VolumeLabel}";
                    }

                    header += $"  ({SizeExtensions.SizeSuffix(drive.AvailableFreeSpace)}) [{drive.DriveFormat}]";

                    var menuItem = new MenuItem
                    {
                        Header = header,
                        Tag = drive,
                    };
                    menuItem.Click += FileSendToItemClick;

                    menuItems.Add(menuItem);
                }

                if (Settings.General.SendToCustomLocations is not null && Settings.General.SendToCustomLocations.Count > 0)
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
                                found = SlicerFile.FileFullPath.EndsWith($".{ext}");
                                if (found) break;
                            }
                            if(!found) continue;
                        }

                        var menuItem = new MenuItem
                        {
                            Header = location.ToString(),
                            Tag = location,
                        };
                        menuItem.Click += FileSendToItemClick;

                        menuItems.Add(menuItem);
                    }
                }

                if (Settings.Network.RemotePrinters is not null && Settings.Network.RemotePrinters.Count > 0)
                {
                    foreach (var remotePrinter in Settings.Network.RemotePrinters)
                    {
                        if(!remotePrinter.IsEnabled || !remotePrinter.IsValid) continue;

                        if (!string.IsNullOrWhiteSpace(remotePrinter.CompatibleExtensions))
                        {
                            var extensions = remotePrinter.CompatibleExtensions.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                            var found = false;
                            foreach (var ext in extensions)
                            {
                                found = SlicerFile.FileFullPath.EndsWith($".{ext}");
                                if (found) break;
                            }
                            if (!found) continue;
                        }

                        var menuItem = new MenuItem
                        {
                            Header = remotePrinter.ToString(),
                            Tag = remotePrinter,
                        };
                        menuItem.Click += FileSendToItemClick;

                        menuItems.Add(menuItem);
                    }
                }


                MenuFileSendToItems = menuItems.ToArray();
                _menuFileSendTo.IsVisible = _menuFileSendTo.IsEnabled = menuItems.Count > 0;
            };
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
            else
            {
                return;
            }

            if (CanSave)
            {
                switch (await this.MessageBoxQuestion("There are unsaved changes. Do you want to save the current file before copy it over?\n\n" +
                                                      "Yes: Save the current file and copy it over.\n" +
                                                      "No: Copy the file without current modifications.\n" +
                                                      "Cancel: Abort the operation.", "Send to - Unsaved changes", ButtonEnum.YesNoCancel))
                {
                    case ButtonResult.Yes:
                        await SaveFile(true);
                        break;
                    case ButtonResult.No:
                        break;
                    default:
                        return;
                }
            }

            ShowProgressWindow($"Copying: {SlicerFile.Filename} to {path}", true);
            Progress.ItemName = "Copying";


            if (menuItem.Tag is RemotePrinter remotePrinter)
            {
                var startPrint = (_globalModifiers & KeyModifiers.Shift) != 0 && remotePrinter.RequestPrintFile is not null && remotePrinter.RequestPrintFile.IsValid;
                if (startPrint)
                {
                    if (await this.MessageBoxQuestion(
                        "If supported, you are about to send the file and start the print with it.\n" +
                        "Keep in mind there is no guarantee that the file will start to print.\n" +
                        "Are you sure you want to continue?\n\n" +
                        "Yes: Send file and print it.\n" +
                        "No: Cancel file sending and print.", "Send and print the file?") != ButtonResult.Yes) return;
                }

                HttpResponseMessage response = null;
                try
                {
                    await using var stream = File.OpenRead(SlicerFile.FileFullPath);
                    using var httpContent = new StreamContent(stream);


                    Progress.ItemCount = (uint)(stream.Length / 1000000);
                    bool isCopying = true;
                    try
                    {
                        var task = new Task(() =>
                        {
                            while (isCopying)
                            {
                                Progress.ProcessedItems = (uint)(stream.Position / 1000000);
                                Thread.Sleep(200);
                            }
                        });
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    response = await remotePrinter.RequestUploadFile.SendRequest(remotePrinter, Progress, SlicerFile.Filename, httpContent);
                    isCopying = false;
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
                

                if (
                    response is not null && response.IsSuccessStatusCode && 
                    startPrint)
                {
                    response.Dispose();
                    Progress.Title = "Waiting 2 seconds...";
                    await Task.Delay(2000);
                    try
                    {
                        response = await remotePrinter.RequestPrintFile.SendRequest(remotePrinter, Progress, SlicerFile.Filename);
                        if (!response.IsSuccessStatusCode)
                        {
                            await this.MessageBoxError(response.ToString(), "Unable to send the print command");
                        }
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
            else
            {
                /*var copyResult = await Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var fileDest = Path.Combine(path, SlicerFile.Filename);
                        //File.Copy(SlicerFile.FileFullPath, $"{Path.Combine(path, SlicerFile.Filename)}", true);
                        var buffer = new byte[1024 * 1024]; // 1MB buffer

                        using var source = File.OpenRead(SlicerFile.FileFullPath);
                        using var dest = new FileStream(fileDest, FileMode.Create, FileAccess.Write);
                        //long totalBytes = 0;
                        //int currentBlockSize;

                        Progress.Reset("Megabyte(s)", (uint)(source.Length / 1000000));
                        var copyProgress = new Progress<long>(copiedBytes => Progress.ProcessedItems = (uint)(copiedBytes / 1000000));
                        source.CopyToAsync(dest, 0, copyProgress, Progress.Token).ConfigureAwait(false);

                        /*while ((currentBlockSize = source.Read(buffer)) > 0)
                        {
                            totalBytes += currentBlockSize;

                            dest.Write(buffer, 0, currentBlockSize);

                            if (Progress.Token.IsCancellationRequested)
                            {
                                // Delete dest file here
                                dest.Dispose();
                                File.Delete(fileDest);
                                return false;
                            }

                            Progress.ProcessedItems = (uint)(totalBytes / 1000000);
                        }*/

                /*    return true;
                }
                catch (OperationCanceledException) { }
                catch (Exception exception)
                {
                    Dispatcher.UIThread.InvokeAsync(async () =>
                        await this.MessageBoxError(exception.ToString(), "Unable to copy the file"));
                }

                return false;
            });*/

                bool copyResult = false;
                var fileDest = Path.Combine(path, SlicerFile.Filename);
                try
                {
                    await using var source = File.OpenRead(SlicerFile.FileFullPath);
                    await using var dest = new FileStream(fileDest, FileMode.Create, FileAccess.Write);

                    Progress.Reset("Megabyte(s)", (uint)(source.Length / 1000000));
                    var copyProgress = new Progress<long>(copiedBytes => Progress.ProcessedItems = (uint)(copiedBytes / 1000000));
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
                    await this.MessageBoxError(exception.ToString(), "Unable to copy the file");
                }

                if(copyResult && menuItem.Tag is DriveInfo removableDrive && OperatingSystem.IsWindows() && Settings.General.SendToPromptForRemovableDeviceEject)
                {
                    if (await this.MessageBoxQuestion(
                        $"File '{SlicerFile.Filename}' has copied successfully into {removableDrive.Name}\n" +
                        $"Do you want to eject the {removableDrive.Name} drive now?", "Copied ok, eject the drive?") == ButtonResult.Yes)
                    {
                        Progress.ResetAll($"Ejecting {removableDrive.Name}");
                        var ejectResult = await Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                return Core.SystemOS.Windows.USB.USBEject(removableDrive.Name);
                            }
                            catch (OperationCanceledException) { }
                            catch (Exception exception)
                            {
                                Dispatcher.UIThread.InvokeAsync(async () =>
                                    await this.MessageBoxError(exception.ToString(), $"Unable to eject the drive {removableDrive.Name}"));
                            }

                            return false;
                        });

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
            var windowSize = this.GetScreenWorkingArea();

            if (Settings.General.StartMaximized
                || ClientSize.Width > windowSize.Width
                || ClientSize.Height > windowSize.Height)
            {
                WindowState = WindowState.Maximized;
            }

            AddLog($"{About.Software} start", Program.ProgramStartupTime.Elapsed.TotalSeconds);

            if (Settings.General.CheckForUpdatesOnStartup)
            {
                Task.Factory.StartNew(VersionChecker.Check);
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
                ClipboardUndo(true);
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
        
        public void OpenContextMenu(string name)
        {
            var menu = this.FindControl<ContextMenu>($"{name}ContextMenu");
            if (menu is null) return;
            var parent = this.FindControl<Button>($"{name}Button");
            if (parent is null) return;
            menu.Open(parent);
        }



        #endregion

        #region Events

        public void MenuFileOpenClicked() => OpenFile();
        public void MenuFileOpenNewWindowClicked() => OpenFile(true);
        public void MenuFileOpenInPartialModeClicked() => OpenFile(false, FileFormat.FileDecodeType.Partial);

        public void MenuFileOpenCurrentFileFolderClicked()
        {
            if (!IsFileLoaded) return;
            SystemAware.SelectFileOnExplorer(SlicerFile.FileFullPath);
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
            var filename = FileFormat.GetFileNameStripExtensions(SlicerFile.FileFullPath, out var ext);
            //var ext = Path.GetExtension(SlicerFile.FileFullPath);
            //var extNoDot = ext.Remove(0, 1);
            var extension = FileExtension.Find(ext);
            if (extension is null)
            {
                await this.MessageBoxError("Unable to find the target extension.", "Invalid extension");
                return;
            }
            SaveFileDialog dialog = new()
            {
                DefaultExtension = extension.Extension,
                Filters = new List<FileDialogFilter>
                {
                    new()
                    {
                        Name = extension.Description,
                        Extensions = new List<string>
                        {
                            ext
                        }
                    }
                },
                Directory = string.IsNullOrEmpty(Settings.General.DefaultDirectorySaveFile)
                    ? Path.GetDirectoryName(SlicerFile.FileFullPath)
                    : Settings.General.DefaultDirectorySaveFile,
                InitialFileName = $"{Settings.General.FileSaveNamePrefix}{filename}{Settings.General.FileSaveNameSuffix}"
            };
            var file = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(file)) return;
            await SaveFile(file);
        }

        public async void OpenFile(bool newWindow = false, FileFormat.FileDecodeType fileDecodeType = FileFormat.FileDecodeType.Full)
        {
            var filters = Helpers.ToAvaloniaFileFilter(FileFormat.AllFileFiltersAvalonia);
            var orderedFilters = new List<FileDialogFilter> {filters[Settings.General.DefaultOpenFileExtensionIndex]};
            for (int i = 0; i < filters.Count; i++)
            {
                if(i == Settings.General.DefaultOpenFileExtensionIndex) continue;
                orderedFilters.Add(filters[i]);
            }

            var dialog = new OpenFileDialog
            {
                AllowMultiple = true,
                Filters = orderedFilters,
                Directory = Settings.General.DefaultDirectoryOpenFile
            };
            var files = await dialog.ShowAsync(this);
            ProcessFiles(files, newWindow, fileDecodeType);
        }

        public async void OnMenuFileCloseFile()
        {
            if (CanSave && await this.MessageBoxQuestion("There are unsaved changes. Do you want close this file without saving?") !=
                ButtonResult.Yes)
            {
                return;
            }

            CloseFile();
        }

        public void CloseFile()
        {
            if (SlicerFile is null) return;

            MenuFileConvertItems = null;

            ClipboardManager.Instance.Reset();

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

            VisibleThumbnailIndex = 0;

            LayerImageBox.Image = null;
            LayerPixelPicker.Reset();

            ClearROIAndMask();

            if(!Settings.Tools.LastUsedSettingsKeepOnCloseFile)
                OperationSessionManager.Instance.Clear();

            ResetDataContext();
        }

        public void OnMenuFileFullscreen()
        {
            WindowState = WindowState == WindowState.FullScreen ? WindowState.Maximized : WindowState.FullScreen;
        }

        public async void MenuFileSettingsClicked()
        {
            var settingsWindow = new SettingsWindow();
            await settingsWindow.ShowDialog(this);
            if (settingsWindow.DialogResult == DialogResults.OK)
            {
                _layerNavigationSliderDebounceTimer.Interval = Settings.LayerPreview.LayerSliderDebounce == 0 ? 1 : Settings.LayerPreview.LayerSliderDebounce;
                RaisePropertyChanged(nameof(IssuesGridItems));
            }
        }

        public void OpenHomePage()
        {
            SystemAware.OpenBrowser(About.Website);
        }

        public void OpenDonateWebsite()
        {
            SystemAware.OpenBrowser(About.Donate);
        }

        public void OpenWebsite(string url)
        {
            SystemAware.OpenBrowser(url);
        }

        public async void MenuHelpAboutClicked()
        {
            await new AboutWindow().ShowDialog(this);
        }

        public async void MenuHelpBenchmarkClicked()
        {
            await new BenchmarkWindow().ShowDialog(this);
        }

        public void MenuHelpOpenSettingsFolderClicked()
        {
            SystemAware.StartProcess(UserSettings.SettingsFolder);
        }

        private async void MenuHelpMaterialManagerClicked()
        {
            await new MaterialManagerWindow().ShowDialog(this);
        }

        public async void MenuHelpInstallProfilesClicked()
        {
            var PEFolder = App.GetPrusaSlicerDirectory();
            if (string.IsNullOrEmpty(PEFolder) || !Directory.Exists(PEFolder))
            {
                if(await this.MessageBoxQuestion(
                    "Unable to detect PrusaSlicer on your system, please ensure you have latest version installed.\n" +
                    $"Was looking on: {PEFolder}\n\n" +
                    "Click 'Yes' to open the PrusaSlicer webpage for download\n" +
                    "Click 'No' to dismiss",
                    "Unable to detect PrusaSlicer") == ButtonResult.Yes) SystemAware.OpenBrowser("https://www.prusa3d.com/prusaslicer/");
                return;
            }
            await new PrusaSlicerManagerWindow().ShowDialog(this);
        }

        public async void MenuNewVersionClicked()
        {
            var result =
                await this.MessageBoxQuestion(
                    $"Do you like to auto-update {About.Software} v{App.VersionStr} to v{VersionChecker.Version}?\n" +
                    "Yes: Auto update\n" +
                    "No:  Manual update\n" +
                    "Cancel: No action\n\n" +
                    "Changelog:\n" +
                    $"{VersionChecker.Changelog}", $"Update UVtools to v{VersionChecker.Version}?", ButtonEnum.YesNoCancel);


            if (result == ButtonResult.No)
            {
                SystemAware.OpenBrowser(VersionChecker.UrlLatestRelease);
                return;
            }
            if (result == ButtonResult.Yes)
            {
                IsGUIEnabled = false;
                ShowProgressWindow($"Downloading: {VersionChecker.Filename}");

                await VersionChecker.AutoUpgrade(Progress);

                /*var task = await Task.Factory.StartNew( () =>
                {
                    try
                    {
                        VersionChecker.AutoUpgrade(Progress);
                        return true;
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception exception)
                    {
                        Dispatcher.UIThread.InvokeAsync(async () =>
                            await this.MessageBoxError(exception.ToString(), "Error opening the file"));
                    }

                    return false;
                });
                */
                IsGUIEnabled = true;
                
                return;
            }
            
        } 

        #endregion

        #region Methods

        private void UpdateTitle()
        {
            var title = $"{About.Software}   ";

            if (IsFileLoaded)
            {
                title += $"File: {SlicerFile.Filename} ({Math.Round(LastStopWatch.ElapsedMilliseconds / 1000m, 2)}s)   ";
            }

            title += $"Version: {App.VersionStr}   RAM: {SizeExtensions.SizeSuffix(Environment.WorkingSet)}";

            if (IsFileLoaded)
            {
                if (CanSave)
                {
                    title += "   [UNSAVED]";
                }

                if (SlicerFile.DecodeType == FileFormat.FileDecodeType.Partial)
                {
                    title += "   [PARTIAL MODE]";
                }
            }

#if DEBUG
            title += "   [DEBUG]";
#endif

            Title = title;
        }

        public async void ProcessFiles(string[] files, bool openNewWindow = false, FileFormat.FileDecodeType fileDecodeType = FileFormat.FileDecodeType.Full)
        {
            if (files is null || files.Length == 0) return;

            for (int i = 0; i < files.Length; i++)
            {
                if (!File.Exists(files[i])) continue;

                if (files[i].EndsWith(".uvtop"))
                {
                    if(!IsFileLoaded) continue;
                    try
                    {
                        var fileText = await File.ReadAllTextAsync(files[i]);
                        var match = Regex.Match(fileText, @"(?:<\/\s*Operation)([a-zA-Z0-9_]+)(?:\s*>)");
                        if(!match.Success) continue;
                        if(match.Groups.Count < 1) continue;
                        var operationName = match.Groups[1].Value;
                        var baseType = typeof(Operation).FullName;
                        if(string.IsNullOrWhiteSpace(baseType)) continue;
                        var classname = baseType + operationName+", UVtools.Core";
                        var type = Type.GetType(classname);
                        if(type is null) continue;
                        var operation = Operation.Deserialize(files[i], type);
                        await ShowRunOperation(type, operation);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                        throw;
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

        void ReloadFile() => ReloadFile(_actualLayer);

        void ReloadFile(uint actualLayer)
        {
            if (App.SlicerFile is null) return;
            ProcessFile(SlicerFile.FileFullPath, SlicerFile.DecodeType, _actualLayer);
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

            var task = await Task.Factory.StartNew( () =>
            {
                try
                {
                    SlicerFile.Decode(fileName, fileDecodeType, Progress);
                    return true;
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    Dispatcher.UIThread.InvokeAsync(async () =>
                        await this.MessageBoxError(exception.ToString(), "Error opening the file"));
                }

                return false;
            });

            IsGUIEnabled = true;

            if (!task)
            {
                SlicerFile.Dispose();
                SlicerFile = null;
                return;
            }

            AddRecentFile(fileName);

            if (SlicerFile.LayerCount == 0)
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

            if (Settings.Automations.AutoConvertFiles && SlicerFile.DecodeType == FileFormat.FileDecodeType.Full)
            {
                string convertFileExtension = SlicerFile switch
                {
                    SL1File sl1File => sl1File.LookupCustomValue<string>(SL1File.Keyword_FileFormat, null),
                    VDTFile vdtFile => vdtFile.ManifestFile.Machine.UVToolsConvertTo,
                    _ => null
                };

                if (!string.IsNullOrWhiteSpace(convertFileExtension))
                {
                    convertFileExtension = convertFileExtension.ToLower(CultureInfo.InvariantCulture);
                    var convertToFormat = FileFormat.FindByExtensionOrFilePath(convertFileExtension);
                    if (convertToFormat is not null)
                    {
                        var directory = Path.GetDirectoryName(SlicerFile.FileFullPath);
                        var filename = FileFormat.GetFileNameStripExtensions(SlicerFile.FileFullPath);
                        FileFormat convertedFile = null;

                        IsGUIEnabled = false;
                        ShowProgressWindow($"Converting {Path.GetFileName(SlicerFile.FileFullPath)} to {convertFileExtension}");

                        task = await Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                convertedFile = SlicerFile.Convert(convertToFormat,
                                    Path.Combine(directory, $"{filename}.{convertFileExtension}"),
                                    Progress);
                                return true;
                            }
                            catch (OperationCanceledException)
                            { }
                            catch (Exception exception)
                            {
                                Dispatcher.UIThread.InvokeAsync(async () =>
                                    await this.MessageBoxError(exception.ToString(),
                                        "Error while converting the file"));
                            }

                            return false;
                        });

                        IsGUIEnabled = true;

                        if (task && convertedFile is not null)
                        {
                            SlicerFile = convertedFile;
                            AddRecentFile(SlicerFile.FileFullPath);
                        }
                    }
                }
            }

            bool modified = false;
            if (Settings.Automations.LightOffDelaySetMode != Enumerations.LightOffDelaySetMode.NoAction &&
                SlicerFile.CanUseBottomLightOffDelay &&
                (Settings.Automations.ChangeOnlyLightOffDelayIfZero && SlicerFile.BottomLightOffDelay == 0 || !Settings.Automations.ChangeOnlyLightOffDelayIfZero))
            {
                if ((SlicerFile.CanUseAnyWaitTimeBeforeCure || SlicerFile.CanUseAnyWaitTimeAfterCure || SlicerFile.CanUseAnyWaitTimeAfterLift) && 
                    (SlicerFile.BottomWaitTimeBeforeCure > 0 || SlicerFile.BottomWaitTimeAfterCure > 0 || SlicerFile.BottomWaitTimeAfterLift > 0))
                {
                    // Ignore this automation
                }
                else
                {
                    float lightOff = Settings.Automations.LightOffDelaySetMode switch
                    {
                        Enumerations.LightOffDelaySetMode.UpdateWithExtraDelay => SlicerFile.CalculateBottomLightOffDelay((float)Settings.Automations.BottomLightOffDelay),
                        Enumerations.LightOffDelaySetMode.UpdateWithoutExtraDelay => SlicerFile.CalculateBottomLightOffDelay(),
                        _ => 0
                    };
                    if (lightOff != SlicerFile.BottomLightOffDelay)
                    {
                        modified = true;
                        SlicerFile.BottomLightOffDelay = lightOff;
                    }
                }
                
            }

            if (Settings.Automations.LightOffDelaySetMode != Enumerations.LightOffDelaySetMode.NoAction &&
                SlicerFile.CanUseLightOffDelay &&
                (Settings.Automations.ChangeOnlyLightOffDelayIfZero && SlicerFile.LightOffDelay == 0 || !Settings.Automations.ChangeOnlyLightOffDelayIfZero))
            {
                if ((SlicerFile.CanUseAnyWaitTimeBeforeCure || SlicerFile.CanUseAnyWaitTimeAfterCure || SlicerFile.CanUseAnyWaitTimeAfterLift) &&
                    (SlicerFile.WaitTimeBeforeCure > 0 || SlicerFile.WaitTimeAfterCure > 0 || SlicerFile.WaitTimeAfterLift > 0))
                {
                    // Ignore this automation
                }
                else
                {
                    float lightOff = Settings.Automations.LightOffDelaySetMode switch
                    {
                        Enumerations.LightOffDelaySetMode.UpdateWithExtraDelay => SlicerFile.CalculateNormalLightOffDelay((float)Settings.Automations.LightOffDelay),
                        Enumerations.LightOffDelaySetMode.UpdateWithoutExtraDelay => SlicerFile.CalculateNormalLightOffDelay(),
                        _ => 0
                    };
                    if (lightOff != SlicerFile.LightOffDelay)
                    {
                        modified = true;
                        SlicerFile.LightOffDelay = lightOff;
                    }
                }
            }

            if (modified)
            {
                CanSave = true;
                if (Settings.Automations.SaveFileAfterModifications)
                {
                    var saveCount = _savesCount;
                    await SaveFile(null, true);
                    _savesCount = saveCount;
                }
            }

            Clipboard.Init(SlicerFile);

            if (SlicerFile is not ImageFile && SlicerFile.DecodeType == FileFormat.FileDecodeType.Full)
            {
                List<MenuItem> menuItems = new();
                foreach (var fileFormat in FileFormat.AvailableFormats)
                {
                    if(fileFormat is ImageFile) continue;
                    foreach (var fileExtension in fileFormat.FileExtensions)
                    {
                        if(!fileExtension.IsVisibleOnConvertMenu) continue;

                        var menuItem = new MenuItem
                        {
                            Header = fileExtension.Description,
                            Tag = fileExtension
                        };

                        menuItem.Tapped += ConvertToOnTapped;

                        menuItems.Add(menuItem);
                    }
                    /*string extensions = fileFormat.FileExtensions.Length > 0
                        ? $" ({fileFormat.GetFileExtensions()})"
                        : string.Empty;

                    var menuItem = new MenuItem
                    {
                        Header = fileFormat.GetType().Name.Replace("File", extensions),
                        Tag = fileFormat
                    };

                    menuItem.Tapped += ConvertToOnTapped;

                    menuItems.Add(menuItem);*/
                }

                MenuFileConvertItems = menuItems.ToArray();
            }

            using var mat = SlicerFile.FirstLayer?.LayerMat;

            VisibleThumbnailIndex = 1;

            RefreshProperties();

            UpdateTitle();

            if (mat is not null)
            {
                if (Settings.LayerPreview.AutoRotateLayerBestView)
                {
                    _showLayerImageRotated = mat.Height > mat.Width;
                }

                if (Settings.LayerPreview.AutoFlipLayerIfMirrored)
                {
                    if (SlicerFile.DisplayMirror == Enumerations.FlipDirection.None)
                    {
                        _showLayerImageFlipped = false;
                    }
                    else
                    {
                        _showLayerImageFlipped = true;
                        _showLayerImageFlippedHorizontally = SlicerFile.DisplayMirror is Enumerations.FlipDirection.Horizontally or Enumerations.FlipDirection.Both;
                        _showLayerImageFlippedVertically = SlicerFile.DisplayMirror is Enumerations.FlipDirection.Vertically or Enumerations.FlipDirection.Both;
                    }
                }
            }

            ResetDataContext();

            ForceUpdateActualLayer(actualLayer.Clamp(actualLayer, SliderMaximumValue));

            if (Settings.LayerPreview.ZoomToFitPrintVolumeBounds)
            {
                ZoomToFit();
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

            if (mat is not null && mat.Size != SlicerFile.Resolution)
            {
                var result = await this.MessageBoxWaring($"Layer image resolution of {mat.Size} mismatch with printer resolution of {SlicerFile.Resolution}.\n" +
                                            "1) Printing this file can lead to problems or malformed model, please verify your slicer printer settings;\n" +
                                            "2) Processing this file with some of the tools can lead to program crash or misfunction;\n" +
                                            "3) If you used PrusaSlicer to slice this file, you must use it with compatible UVtools printer profiles (Help - Install profiles into PrusaSlicer).\n\n" +
                                            "Click 'Yes' to auto fix and set the file resolution with the layer resolution, but only use this option if you are sure it's ok to!\n" +
                                            "Click 'No' to continue as it is and ignore this warning, you can still repair issues and use some of the tools.",
                    "File and layer resolution mismatch!", ButtonEnum.YesNo);
                if(result == ButtonResult.Yes)
                {
                    SlicerFile.Resolution = mat.Size;
                    RaisePropertyChanged(nameof(LayerResolutionStr));
                }
            }

            SlicerFile.IssueManager.CollectionChanged += (sender, e) =>
            {
                UpdateLayerTrackerHighlightIssues();
            };

            if (SlicerFile.DecodeType == FileFormat.FileDecodeType.Full)
            {
                if (Settings.Issues.ComputeIssuesOnLoad)
                {
                    _firstTimeOnIssues = false;
                    await OnClickDetectIssues();
                    if (SlicerFile.IssueManager.Count > 0)
                    {
                        SelectedTabItem = TabIssues;
                        if (Settings.Issues.AutoRepairIssuesOnLoad)
                            await RunOperation(ToolRepairLayersControl.GetOperationRepairLayers());
                    }
                }
                else
                {
                    await ComputeIssues(
                        GetIslandDetectionConfiguration(false),
                        GetOverhangDetectionConfiguration(false),
                        GetResinTrapDetectionConfiguration(false),
                        GetTouchingBoundsDetectionConfiguration(false),
                        GetPrintHeightDetectionConfiguration(true),
                        true);
                    if (SlicerFile.IssueManager.Count > 0)
                    {
                        SelectedTabItem = TabIssues;
                    }
                }
            }

            SlicerFile.PropertyChanged += SlicerFileOnPropertyChanged;
#if !DEBUG
            if (SlicerFile is CTBEncryptedFile)
            {
                await this.MessageBoxInfo(CTBEncryptedFile.Preamble, "Information");
            }
#endif
        }

        private void SlicerFileOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SlicerFile.Thumbnails))
            {
                RefreshThumbnail();
                return;
            }
            if (e.PropertyName == nameof(SlicerFile.Resolution))
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

        private async void ConvertToOnTapped(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item) return;
            if (item.Tag is not FileExtension fileExtension) return;

            SaveFileDialog saveDialog = new()
            {
                InitialFileName = Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath),
                Filters = Helpers.ToAvaloniaFilter(fileExtension.Description, fileExtension.Extension),
                Directory = string.IsNullOrEmpty(Settings.General.DefaultDirectoryConvertFile)
                    ? Path.GetDirectoryName(SlicerFile.FileFullPath)
                    : Settings.General.DefaultDirectoryConvertFile
            };

            var result = await saveDialog.ShowAsync(this);
            if (string.IsNullOrEmpty(result)) return;


            IsGUIEnabled = false;
            ShowProgressWindow($"Converting {Path.GetFileName(SlicerFile.FileFullPath)} to {Path.GetExtension(result)}");

            var task = await Task.Factory.StartNew(() =>
            {
                try
                {
                    return SlicerFile.Convert(fileExtension.GetFileFormat(), result, Progress) is not null;
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

                    Dispatcher.UIThread.InvokeAsync(async () =>
                        await this.MessageBoxError($"Convertion was not successful! Maybe not implemented...\n{extraMessage}{ex}", "Convertion unsuccessful"));
                }
                
                return false;
            });

            IsGUIEnabled = true;
           
            if (task)
            {
                var question = await this.MessageBoxQuestion(
                    $"Conversion completed in {LastStopWatch.ElapsedMilliseconds / 1000}s\n\n" +
                    $"Do you want to open '{Path.GetFileName(result)}' in a new window?\n" +
                    "Yes: Open in a new window.\n" +
                    "No: Open in this window.\n" +
                    "Cancel: Do not perform any action.\n",
                    "Conversion complete", ButtonEnum.YesNoCancel);
                switch (question)
                {
                    case ButtonResult.No:
                        ProcessFile(result, _actualLayer);
                        break;
                    case ButtonResult.Yes:
                        App.NewInstance(result);
                        break;
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


        public async Task<bool> SaveFile(bool ignoreOverwriteWarning) => await SaveFile(null, ignoreOverwriteWarning);

        public async Task<bool> SaveFile(string filepath = null, bool ignoreOverwriteWarning = false)
        {
            if (filepath is null)
            {
                if (!ignoreOverwriteWarning && SavesCount == 0 && Settings.General.PromptOverwriteFileSave)
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
            ShowProgressWindow($"Saving {Path.GetFileName(filepath)}");
            
            var task = await Task.Factory.StartNew( () =>
            {
                try
                {
                    SlicerFile.SaveAs(tempFile, Progress);
                    if (File.Exists(filepath))
                    {
                        File.Delete(filepath);
                    }
                    File.Move(tempFile, filepath);
                    SlicerFile.FileFullPath = filepath;
                    return true;
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
                    SlicerFile.FileFullPath = oldFile;
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                    Dispatcher.UIThread.InvokeAsync(async () =>
                        await this.MessageBoxError(ex.ToString(), "Error while saving the file"));
                }

                return false;
            });

            IsGUIEnabled = true;

            if (task)
            {
                SavesCount++;
                CanSave = false;
                UpdateTitle();
            }

            return task;
        }

        public async void IPrintedThisFile()
        {
            await ShowRunOperation(typeof(OperationIPrintedThisFile));
        }

        public async void ExtractFile()
        {
            if (!IsFileLoaded) return;
            string fileNameNoExt = Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath);
            OpenFolderDialog dialog = new()
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

            IsGUIEnabled = false;
            ShowProgressWindow($"Extracting {Path.GetFileName(SlicerFile.FileFullPath)}");

            await Task.Factory.StartNew(() =>
            {
                try
                {
                    SlicerFile.Extract(finalPath, true, true, Progress);
                }
                catch (OperationCanceledException)
                {

                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.InvokeAsync(async () =>
                        await this.MessageBoxError(ex.ToString(), "Error while try extracting the file"));
                }
            });
 

            IsGUIEnabled = true;


            if (await this.MessageBoxQuestion(
                $"Extraction to {finalPath} completed in ({LastStopWatch.ElapsedMilliseconds / 1000}s)\n\n" +
                "'Yes' to open target folder, 'No' to continue.",
                "Extraction complete") == ButtonResult.Yes)
            {
                SystemAware.StartProcess(finalPath);
            }

        }

#region Operations
        public async Task<Operation> ShowRunOperation(Type type, Operation loadOperation = null)
        {
            var operation = await ShowOperation(type, loadOperation);
            await RunOperation(operation);
            return operation;
        }

        public async Task<Operation> ShowOperation(Type type, Operation loadOperation = null)
        {
            var toolTypeBase = typeof(ToolControl);
            var calibrateTypeBase = typeof(CalibrateElephantFootControl);
            var classname = type.Name.StartsWith("OperationCalibrate") ?
                $"{calibrateTypeBase.Namespace}.{type.Name.Remove(0, Operation.ClassNameLength)}Control" :
                $"{toolTypeBase.Namespace}.Tool{type.Name.Remove(0, Operation.ClassNameLength)}Control"; ;
            var controlType = Type.GetType(classname);
            ToolControl control;

            bool removeContent = false;
            if (controlType is null)
            {
                //controlType = toolTypeBase;
                removeContent = true;
                control = new ToolControl(type.CreateInstance<Operation>(SlicerFile));
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

            if (SlicerFile.DecodeType == FileFormat.FileDecodeType.Partial && !control.BaseOperation.CanRunInPartialMode)
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

        public async Task<bool> RunOperation(Operation baseOperation)
        {
            if (baseOperation is null) return false;

            switch (baseOperation)
            {
                case OperationEditParameters operation:
                    operation.Execute();
                    RefreshProperties();
                    RefreshCurrentLayerData();
                    ResetDataContext();

                    CanSave = true;
                    return true;
                case OperationIPrintedThisFile operation:
                    operation.Execute();
                    return true;
                case OperationRepairLayers operation:
                    if (SlicerFile.IssueManager.Count == 0)
                    {
                        var islandConfig = GetIslandDetectionConfiguration();
                        islandConfig.Enabled = operation.RepairIslands && operation.RemoveIslandsBelowEqualPixelCount > 0;
                        var overhangConfig = new OverhangDetectionConfiguration(false);
                        var resinTrapConfig = GetResinTrapDetectionConfiguration();
                        resinTrapConfig.Enabled = operation.RepairResinTraps;
                        var touchingBoundConfig = new TouchingBoundDetectionConfiguration(false);
                        var printHeightConfig = new PrintHeightDetectionConfiguration(false);

                        if (islandConfig.Enabled || resinTrapConfig.Enabled)
                        {
                            await ComputeIssues(islandConfig, overhangConfig, resinTrapConfig, touchingBoundConfig, printHeightConfig, Settings.Issues.ComputeEmptyLayers);
                        }
                    }

                    break;
            }

            IsGUIEnabled = false;
            ShowProgressWindow(baseOperation.ProgressTitle, baseOperation.CanCancel);
            OperationSessionManager.Instance.Add(baseOperation);

            Clipboard.Snapshot();

            var result = await Task.Factory.StartNew(() =>
            {
                try
                {
                    return baseOperation.Execute(Progress);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.InvokeAsync(async () =>
                        await this.MessageBoxError(ex.ToString(), $"{baseOperation.Title} Error"));
                }

                return false;
            });

            IsGUIEnabled = true;

            if (result)
            {
                Clipboard.Clip(baseOperation);

                ShowLayer();
                RefreshProperties();
                ResetDataContext();

                CanSave = true;

               if(baseOperation.GetType().Name.StartsWith("OperationCalibrate"))
               {
                   IssuesClear();
               }

                switch (baseOperation)
                {
                    // Tools
                    case OperationRepairLayers operation:
                        await OnClickDetectIssues();
                        break;
                }
            }
            else
            {
                Clipboard.RestoreSnapshot();
            }

            if (baseOperation.Tag is not null)
            {
                var message = baseOperation.Tag.ToString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    //message += $"\nExecution time: ";
                    
                    await this.MessageBoxInfo(message, $"{baseOperation.Title} report ({LastStopWatch.Elapsed.Hours}h{LastStopWatch.Elapsed.Minutes}m{LastStopWatch.Elapsed.Seconds}s)");
                }
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
                    Header = Path.GetFileName(file),
                    Tag = file,
                    IsEnabled = SlicerFile?.FileFullPath != file
                };
                ToolTip.SetTip(item, file);
                ToolTip.SetPlacement(item, PlacementMode.Right);
                ToolTip.SetShowDelay(item, 100);
                items.Add(item);

                item.Click += MenuFileOpenRecentItemOnClick;
            }

            MenuFileOpenRecentItems = items.ToArray();
        }

        private void AddRecentFile(string file)
        {
            if (file == Path.Combine(App.ApplicationPath, About.DemoFile)) return;
            RecentFiles.Load();
            RecentFiles.Instance.Insert(0, file);
            RecentFiles.Save();
            RefreshRecentFiles();
        }

        private async void MenuFileOpenRecentItemOnClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem { Tag: string file }) return;
            if (IsFileLoaded && SlicerFile.FileFullPath == file) return;

            if (_globalModifiers == KeyModifiers.Control)
            {
                if (await this.MessageBoxQuestion("Are you sure you want to purge the non-existing files from the recent list?",
                    "Purge the non-existing files?") == ButtonResult.Yes)
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
                    "Remove the file from recent list?") == ButtonResult.Yes)
                {
                    RecentFiles.Load();
                    RecentFiles.Instance.Remove(file);
                    RecentFiles.Save();

                    RefreshRecentFiles();
                }

                return;
            }

            if (!File.Exists(file))
            {
                if (await this.MessageBoxQuestion($"The file: {file} does not exists anymore.\n" +
                                               "Do you want to remove this file from recent list?",
                    "The file does not exists") == ButtonResult.Yes)
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

            

    #endregion
    }
}
