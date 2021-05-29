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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Managers;
using UVtools.Core.Operations;
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
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/crop-16x16.png"))
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
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/square-root-16x16.png"))
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
            new()
            {
                Tag = new OperationPixelDimming(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/pixel-16x16.png"))
                }
            },
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
                Tag = new OperationDynamicLayerHeight(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/dynamic-layers-16x16.png"))
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
                Tag = new OperationLayerReHeight(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/ladder-16x16.png"))
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
        };

        #region DataSets

        

        #endregion

        #endregion

        #region Members

        public Stopwatch LastStopWatch = new();
        
        private bool _isGUIEnabled = true;
        private uint _savesCount;
        private bool _canSave;
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
            if (!IsFileLoaded && Settings.General.LoadDemoFileOnStartup)
            {
                ProcessFile(About.DemoFile);
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
                || !(LayerImageBox.TrackerImage is null)
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

        public async void OpenFile(bool newWindow = false)
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
            ProcessFiles(files, newWindow);
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

            SlicerFile?.Dispose();
            SlicerFile = null;

            SlicerProperties.Clear();
            IssuesClear(true);
            Drawings.Clear();

            SelectedTabItem = TabInformation;
            _firstTimeOnIssues = true;
            IsPixelEditorActive = false;
            CanSave = false;

            _actualLayer = 0;
            LayerCache.Clear();

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
            SettingsWindow settingsWindow = new();
            await settingsWindow.ShowDialog(this);
        }

        public void OpenHomePage()
        {
            App.OpenBrowser(About.Website);
        }

        public void OpenDonateWebsite()
        {
            App.OpenBrowser(About.Donate);
        }

        public void OpenWebsite(string url)
        {
            App.OpenBrowser(url);
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
            App.StartProcess(UserSettings.SettingsFolder);
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
                    "Unable to detect PrusaSlicer") == ButtonResult.Yes) App.OpenBrowser("https://www.prusa3d.com/prusaslicer/");
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


            if (result == ButtonResult.No || OperatingSystem.IsMacOS())
            {
                App.OpenBrowser(VersionChecker.UrlLatestRelease);
                return;
            }
            if (result == ButtonResult.Yes)
            {
                IsGUIEnabled = false;
                ShowProgressWindow($"Downloading: {VersionChecker.Filename}", false);

                var task = await Task.Factory.StartNew( () =>
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

                IsGUIEnabled = true;
                
                return;
            }
            
        } 

        #endregion

        #region Methods

        private void UpdateTitle()
        {
            Title = SlicerFile is null
                    ? $"{About.Software}   Version: {App.VersionStr}"
                    : $"{About.Software}   File: {Path.GetFileName(SlicerFile.FileFullPath)} ({Math.Round(LastStopWatch.ElapsedMilliseconds / 1000m, 2)}s)   Version: {App.VersionStr}"
                    ;

            Title += $"   RAM: {SizeExtensions.SizeSuffix(Environment.WorkingSet)}";

#if DEBUG
            Title += "   [DEBUG]";
#endif
        }

        public async void ProcessFiles(string[] files, bool openNewWindow = false)
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
                        var fileText = File.ReadAllText(files[i]);
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
            SlicerFile = FileFormat.FindByExtension(fileName, true, true);
            if (SlicerFile is null) return;

            IsGUIEnabled = false;
            ShowProgressWindow($"Opening: {fileNameOnly}");

            var task = await Task.Factory.StartNew( () =>
            {
                try
                {
                    SlicerFile.Decode(fileName, Progress);
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

            if (Settings.Automations.AutoConvertFiles)
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
                    var convertToFormat = FileFormat.FindByExtension(convertFileExtension);
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
                        }
                    }
                }
            }

            bool modified = false;
            if (Settings.Automations.LightOffDelaySetMode != Enumerations.LightOffDelaySetMode.NoAction &&
                SlicerFile.CanUseBottomLightOffDelay &&
                (Settings.Automations.ChangeOnlyLightOffDelayIfZero && SlicerFile.BottomLightOffDelay == 0 || !Settings.Automations.ChangeOnlyLightOffDelayIfZero))
            {
                float lightOff = Settings.Automations.LightOffDelaySetMode switch
                {
                    Enumerations.LightOffDelaySetMode.UpdateWithExtraDelay => SlicerFile.CalculateBottomLightOffDelay((float) Settings.Automations.BottomLightOffDelay),
                    Enumerations.LightOffDelaySetMode.UpdateWithoutExtraDelay => SlicerFile.CalculateBottomLightOffDelay(),
                    _ => 0
                };
                if (lightOff != SlicerFile.BottomLightOffDelay)
                {
                    modified = true;
                    SlicerFile.BottomLightOffDelay = lightOff;
                }
            }

            if (Settings.Automations.LightOffDelaySetMode != Enumerations.LightOffDelaySetMode.NoAction &&
                SlicerFile.CanUseLightOffDelay &&
                (Settings.Automations.ChangeOnlyLightOffDelayIfZero && SlicerFile.LightOffDelay == 0 || !Settings.Automations.ChangeOnlyLightOffDelayIfZero))
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

            if (SlicerFile is not ImageFile)
            {
                List<MenuItem> menuItems = new();
                foreach (var fileFormat in FileFormat.AvailableFormats)
                {
                    if(fileFormat is ImageFile) continue;
                    foreach (var fileExtension in fileFormat.FileExtensions)
                    {
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

            using var mat = SlicerFile[0].LayerMat;

            VisibleThumbnailIndex = 1;

            RefreshProperties();

            UpdateTitle();


            if (mat is not null && Settings.LayerPreview.AutoRotateLayerBestView)
            {
                _showLayerImageRotated = mat.Height > mat.Width;
            }

            if (SlicerFile.MirrorDisplay)
            {
                _showLayerImageFlipped = true;
            }

            ResetDataContext();

            ForceUpdateActualLayer(actualLayer.Clamp(actualLayer, SliderMaximumValue));

            if (Settings.LayerPreview.ZoomToFitPrintVolumeBounds)
            {
                ZoomToFit();
            }

            if (mat.Size != SlicerFile.Resolution)
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

            if (Settings.Issues.ComputeIssuesOnLoad)
            {
                _firstTimeOnIssues = false;
                await OnClickDetectIssues();
                if (Issues.Count > 0)
                {
                    SelectedTabItem = TabIssues;
                    if(Settings.Issues.AutoRepairIssuesOnLoad)
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
                if (Issues.Count > 0)
                {
                    SelectedTabItem = TabIssues;
                }
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

            SaveFileDialog dialog = new()
            {
                InitialFileName = Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath),
                Filters = Helpers.ToAvaloniaFilter(fileExtension.Description, fileExtension.Extension),
                Directory = string.IsNullOrEmpty(Settings.General.DefaultDirectoryConvertFile)
                    ? Path.GetDirectoryName(SlicerFile.FileFullPath)
                    : Settings.General.DefaultDirectoryConvertFile
            };

            var result = await dialog.ShowAsync(this);
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
                App.StartProcess(finalPath);
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

            if (removeContent)
            {
                control.IsVisible = false;
            }

            var window = new ToolWindow(control);
            if (loadOperation is not null)
            {
                control.BaseOperation = loadOperation;
            }
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
                    if (Issues is null)
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

                    operation.Issues = Issues.ToList();
                    operation.IslandDetectionConfig = GetIslandDetectionConfiguration();
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

        #endregion

        

        #endregion
        }
}
