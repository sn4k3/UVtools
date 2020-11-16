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
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using MessageBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Managers;
using UVtools.Core.Operations;
using UVtools.WPF.Controls;
using UVtools.WPF.Controls.Tools;
using UVtools.WPF.Extensions;
using UVtools.WPF.Structures;
using UVtools.WPF.Windows;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Helpers = UVtools.WPF.Controls.Helpers;
using Path = System.IO.Path;

namespace UVtools.WPF
{
    public partial class MainWindow : WindowEx
    {
        #region Redirects

        public AppVersionChecker VersionChecker => App.VersionChecker;
        public UserSettings Settings => UserSettings.Instance;
        public FileFormat SlicerFile => App.SlicerFile;
        public ClipboardManager Clipboard => ClipboardManager.Instance;
        #endregion

        #region Controls

        public ProgressWindow ProgressWindow = new ProgressWindow();

        public static MenuItem[] MenuTools { get; } =
        {
            new MenuItem
            {
                Tag = new OperationEditParameters(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/wrench-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationRepairLayers(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/toolbox-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationMove(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/move-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationResize(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/crop-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationFlip(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/flip-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationRotate(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/sync-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationSolidify(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/square-solid-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationMorph(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/geometry-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationThreshold(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/th-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationArithmetic(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/square-root-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationMask(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/mask-16x16.png"))
                    }
            },
            new MenuItem
            {
                Tag = new OperationPixelDimming(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/pixel-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationInfill(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/stroopwafel-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationBlur(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/blur-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationPattern(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/pattern-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationLayerReHeight(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/ladder-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationChangeResolution(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/resize-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationCalculator(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/calculator-16x16.png"))
                }
            },
        };

        public static MenuItem[] LayerActionsMenu { get; } =
        {
            new MenuItem
            {
                Tag = new OperationLayerImport(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/file-import-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationLayerClone(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/copy-16x16.png"))
                }
            },
            new MenuItem
            {
                Tag = new OperationLayerRemove(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/trash-16x16.png"))
                }
            },
        };

        #region DataSets

        

        #endregion

        #endregion

        #region Members

        public Stopwatch LastStopWatch = new Stopwatch();
        
        private bool _isGUIEnabled = true;
        private uint _savesCount;
        private bool _canSave;
        private MenuItem[] _menuFileConvertItems;

        private PointerEventArgs _globalPointerEventArgs;
        private PointerPoint _globalPointerPoint;
        private KeyModifiers _globalModifiers;
        private TabItem _selectedTabItem;
        private TabItem _lastSelectedTab;
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
                    ProgressWindow = new ProgressWindow();
                    return;
                }
                //if (ProgressWindow is null) return;

                LastStopWatch = ProgressWindow.StopWatch;
                ProgressWindow.Close();
                ProgressWindow.Dispose();
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
            get => !(SlicerFile is null);
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
#if DEBUG
            //this.AttachDevTools();
#endif
            App.ThemeSelector?.EnableThemes(this);
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


            foreach (var menuItem in new[] { MenuTools, LayerActionsMenu })
            {
                foreach (var menuTool in menuItem)
                {
                    if (!(menuTool.Tag is Operation operation)) continue;
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
            var clientSizeObs = this.GetObservable(ClientSizeProperty);
            clientSizeObs.Subscribe(size => UpdateLayerTrackerHighlightIssues());
            var windowStateObs = this.GetObservable(WindowStateProperty);
            windowStateObs.Subscribe(size => UpdateLayerTrackerHighlightIssues());
            
            UpdateTitle();

            if (Settings.General.StartMaximized 
                || ClientSize.Width > Screens.Primary.Bounds.Width / Screens.Primary.PixelDensity
                || ClientSize.Height > Screens.Primary.Bounds.Height / Screens.Primary.PixelDensity)
            {
                WindowState = WindowState.Maximized;
            }

            DataContext = this;

            AddHandler(DragDrop.DropEvent, (sender, e) =>
            {
                ProcessFiles(e.Data.GetFileNames().ToArray());
            });
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            Program.ProgramStartupTime.Stop();
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
                    TooltipOverlayText = "Pixel editing is on:\n" +
                                                      "» Click over a pixel to draw\n" +
                                                      "» Hold CTRL to clear pixels";
                    UpdatePixelEditorCursor();
                }
                else
                {
                    LayerImageBox.Cursor = StaticControls.CrossCursor;
                    LayerImageBox.SelectionMode = AdvancedImageBox.SelectionModes.Rectangle;
                    TooltipOverlayText = "ROI selection mode:\n" +
                                         "» Left-click drag to select a fixed region\n" +
                                         "» Left-click + ALT drag to select specific objects\n" +
                                         "» Right click on a specific object to select it\n" +
                                         "Press Esc to clear the ROI";
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
            base.OnKeyUp(e);
            _globalModifiers = e.KeyModifiers;
            if (e.Key == Key.LeftShift ||
                e.Key == Key.RightShift ||
                (e.KeyModifiers & KeyModifiers.Shift) == 0 ||
                (e.KeyModifiers & KeyModifiers.Control) == 0)
            {
                LayerImageBox.TrackerImage = null;
                LayerImageBox.Cursor = StaticControls.ArrowCursor;
                LayerImageBox.AutoPan = true;
                LayerImageBox.SelectionMode = AdvancedImageBox.SelectionModes.None;
                IsTooltipOverlayVisible = false;
                e.Handled = true;
            }
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
            var ext = Path.GetExtension(SlicerFile.FileFullPath);
            var extNoDot = ext.Remove(0, 1);
            var extension = FileExtension.Find(extNoDot);
            if (extension is null)
            {
                await this.MessageBoxError("Unable to find the target extension.", "Invalid extension");
                return;
            }
            SaveFileDialog dialog = new SaveFileDialog
            {
                DefaultExtension = extension.Extension,
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter
                    {
                        Name = extension.Description,
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
            App.SlicerFile = null;

            SlicerProperties.Clear();
            Issues.Clear();
            IgnoredIssues.Clear();
            _issuesSliderCanvas.Children.Clear();
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

            ResetDataContext();
        }

        public void OnMenuFileFullscreen()
        {
            WindowState = WindowState == WindowState.FullScreen ? WindowState.Maximized : WindowState.FullScreen;
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

        public async void MenuHelpBenchmarkClicked()
        {
            await new BenchmarkWindow().ShowDialog(this);
        }

        public void MenuHelpOpenSettingsFolderClicked()
        {
            App.StartProcess(UserSettings.SettingsFolder);
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
            await new PrusaSlicerManager().ShowDialog(this);
        }

        public async void MenuNewVersionClicked()
        {
            var result =
                await this.MessageBoxQuestion(
                    $"Do you like to auto-update {About.Software} v{App.Version} to v{VersionChecker.Version}?\n" +
                    "Yes: Auto update\n" +
                    "No:  Manual update\n" +
                    "Cancel: No action\n\n" +
                    "Changelog:\n" +
                    $"{VersionChecker.Changelog}", $"Update UVtools to v{VersionChecker.Version}?", ButtonEnum.YesNoCancel);

            if (result == ButtonResult.Yes)
            {
                IsGUIEnabled = false;

                var task = await Task.Factory.StartNew(async () =>
                {
                    ShowProgressWindow($"Downloading: {VersionChecker.Filename}");
                    try
                    {
                        VersionChecker.AutoUpgrade(ProgressWindow.RestartProgress(false));
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
            if (result == ButtonResult.No)
            {
                App.OpenBrowser(VersionChecker.UrlLatestRelease);
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

            IsGUIEnabled = false;
            
            var task = await Task.Factory.StartNew( () =>
            {
                ShowProgressWindow($"Opening: {fileNameOnly}");
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
                    Dispatcher.UIThread.InvokeAsync(async () =>
                        await this.MessageBoxError(exception.ToString(), "Error opening the file"));
                }

                return false;
            });

            IsGUIEnabled = true;

            if (!task)
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

            ClipboardManager.Instance.Init(SlicerFile);

            if (!(SlicerFile.ConvertToFormats is null))
            {
                List<MenuItem> menuItems = new List<MenuItem>();
                foreach (var fileFormatType in SlicerFile.ConvertToFormats)
                {
                    FileFormat fileFormat = FileFormat.FindByType(fileFormatType);
                    if(fileFormat.FileExtensions is null) continue;
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

            using Mat mat = SlicerFile[0].LayerMat;

            VisibleThumbnailIndex = 1;

            RefreshProperties();

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

            if (Settings.Issues.ComputeIssuesOnLoad)
            {
                OnClickDetectIssues();
            }

        }

        private async void ShowProgressWindow(string title)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                ProgressWindow.SetTitle(title);
                await ProgressWindow.ShowDialog(this);
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    ProgressWindow.SetTitle(title);
                    await ProgressWindow.ShowDialog(this);
                });
            }
        }

        private void ShowProgressWindowSync(string title)
        {
            ProgressWindow = new ProgressWindow(title);
        }

        private async void ConvertToOnTapped(object? sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem item)) return;
            if (!(item.Tag is FileExtension fileExtension)) return;

            SaveFileDialog dialog = new SaveFileDialog
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

            var task = await Task.Factory.StartNew(() =>
            {
                ShowProgressWindow($"Converting {Path.GetFileName(SlicerFile.FileFullPath)} to {Path.GetExtension(result)}");
                try
                {
                    return SlicerFile.Convert(fileExtension.GetFileFormat(), result, ProgressWindow.RestartProgress());
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

            var task = await Task.Factory.StartNew( () =>
            {
                ShowProgressWindow($"Saving {Path.GetFileName(filepath)}");

                try
                {
                    SlicerFile.SaveAs(tempFile, ProgressWindow.RestartProgress());
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

            IsGUIEnabled = false;

            await Task.Factory.StartNew(() =>
            {
                ShowProgressWindow($"Extracting {Path.GetFileName(SlicerFile.FileFullPath)}");
                try
                {
                    SlicerFile.Extract(finalPath, true, true, ProgressWindow.RestartProgress());
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
        public async Task<Operation> ShowRunOperation(Type type)
        {
            var operation = await ShowOperation(type);
            await RunOperation(operation);
            return operation;
        }

        public async Task<Operation> ShowOperation(Type type)
        {
            var typeBase = typeof(ToolControl);
            var classname = $"{typeBase.Namespace}.Tool{type.Name.Remove(0, Operation.ClassNameLength)}Control";
            var controlType = Type.GetType(classname);
            ToolControl control;

            bool removeContent = false;
            if (controlType is null)
            {
                controlType = typeBase;
                removeContent = true;
                control = new ToolControl(type.CreateInstance<Operation>());
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
            //window.ShowDialog(this);
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
                    /*foreach (var modifier in operation.Modifiers.Where(modifier => modifier.HasChanged))
                    {
                        SlicerFile.SetValueFromPrintParameterModifier(modifier, modifier.NewValue);
                    }*/
                    SlicerFile.EditPrintParameters(operation);
                    RefreshProperties();
                    ResetDataContext();

                    CanSave = true;

                    return false;
                case OperationRepairLayers operation:
                    if (Issues is null)
                    {
                        var islandConfig = GetIslandDetectionConfiguration();
                        islandConfig.Enabled = operation.RepairIslands && operation.RemoveIslandsBelowEqualPixelCount > 0;
                        var overhangConfig = new OverhangDetectionConfiguration { Enabled = false };
                        var resinTrapConfig = GetResinTrapDetectionConfiguration();
                        resinTrapConfig.Enabled = operation.RepairResinTraps;
                        var touchingBoundConfig = new TouchingBoundDetectionConfiguration { Enabled = false };

                        if (islandConfig.Enabled || resinTrapConfig.Enabled)
                        {
                            ComputeIssues(islandConfig, overhangConfig, resinTrapConfig, touchingBoundConfig, Settings.Issues.ComputeEmptyLayers);
                        }
                    }

                    operation.Issues = Issues.ToList();

                    break;
            }

            IsGUIEnabled = false;

            LayerManager backup = null;
            var result = await Task.Factory.StartNew(() =>
            {
                ShowProgressWindow(baseOperation.ProgressTitle);
                backup = SlicerFile.LayerManager.Clone();

                /*var backup = new Layer[baseOperation.LayerRangeCount];
                uint i = 0;
                for (uint layerIndex = baseOperation.LayerIndexStart; layerIndex <= baseOperation.LayerIndexEnd; layerIndex++)
                {
                    backup[i++] = SlicerFile[layerIndex].Clone();
                }*/

                try
                {
                    switch (baseOperation)
                    {
                        // Tools
                        case OperationRepairLayers operation:
                            operation.IslandDetectionConfig = GetIslandDetectionConfiguration();
                            SlicerFile.LayerManager.RepairLayers(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        case OperationMove operation:
                            SlicerFile.LayerManager.Move(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        case OperationResize operation:
                            SlicerFile.LayerManager.Resize(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        case OperationFlip operation:
                            SlicerFile.LayerManager.Flip(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        case OperationRotate operation:
                            SlicerFile.LayerManager.Rotate(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        case OperationSolidify operation:
                            SlicerFile.LayerManager.Solidify(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        case OperationMorph operation:
                            SlicerFile.LayerManager.Morph(operation, BorderType.Default, new MCvScalar(), ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        case OperationThreshold operation:
                            SlicerFile.LayerManager.ThresholdPixels(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        case OperationArithmetic operation:
                            SlicerFile.LayerManager.Arithmetic(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        case OperationMask operation:
                            SlicerFile.LayerManager.Mask(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        case OperationPixelDimming operation:
                            SlicerFile.LayerManager.PixelDimming(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        case OperationInfill operation:
                            SlicerFile.LayerManager.Infill(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        case OperationBlur operation:
                            SlicerFile.LayerManager.Blur(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;

                        case OperationChangeResolution operation:
                            SlicerFile.LayerManager.ChangeResolution(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        case OperationLayerReHeight operation:
                            SlicerFile.LayerManager.ReHeight(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        case OperationPattern operation:
                            SlicerFile.LayerManager.Pattern(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        // Actions
                        case OperationLayerImport operation:
                            SlicerFile.LayerManager.Import(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        case OperationLayerClone operation:
                            SlicerFile.LayerManager.CloneLayer(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;
                        case OperationLayerRemove operation:
                            SlicerFile.LayerManager.RemoveLayers(operation, ProgressWindow.RestartProgress(operation.CanCancel));
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    return true;
                }
                catch (OperationCanceledException)
                {
                    SlicerFile.LayerManager = backup;
                    /*i = 0;
                    for (uint layerIndex = baseOperation.LayerIndexStart; layerIndex <= baseOperation.LayerIndexEnd; layerIndex++)
                    {
                        SlicerFile[layerIndex] = backup[i++];
                    }*/
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
                string description = baseOperation.ToString();
                if (!description.StartsWith(baseOperation.Title)) description = $"{baseOperation.Title}: {description}";
                ClipboardManager.Instance.Clip(description, backup);

                ShowLayer();
                RefreshProperties();
                ResetDataContext();

                CanSave = true;

                switch (baseOperation)
                {
                    // Tools
                    case OperationRepairLayers operation:
                        OnClickDetectIssues();
                        break;
                }
            }

            return true;
        }

        #endregion

        

        #endregion
        }
}
