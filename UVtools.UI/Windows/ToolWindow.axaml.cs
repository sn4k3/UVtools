/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Avalonia.Input;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using UVtools.Core;
using UVtools.Core.Dialogs;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Managers;
using UVtools.Core.Operations;
using UVtools.UI.Controls;
using UVtools.UI.Controls.Tools;
using UVtools.UI.Extensions;
using UVtools.UI.Structures;
using AvaloniaStatic = UVtools.UI.Controls.AvaloniaStatic;
using Avalonia.Controls;

namespace UVtools.UI.Windows;

public partial class ToolWindow : WindowEx
{
    public enum Callbacks : byte
    {
        Init,
        ClearROI,
        BeforeLoadProfile,
        AfterLoadProfile,
        Button1, // Reset to defaults
        Checkbox1, // Show Advanced
    }
    private KeyModifiers _globalModifiers;
    public ToolControl? ToolControl;
    private string? _description;
    private double _descriptionMaxWidth = 500;
    private double _profileBoxMaxWidth = double.NaN;
    private bool _layerRangeVisible = true;
    private bool _layerRangeSync;
    private uint _layerIndexStart;
    private uint _layerIndexEnd;
    private bool _layerIndexEndEnabled = true;
    private bool _isROIVisible;
    private bool _isMasksVisible;

    private bool _clearRoiAndMaskAfterOperation;

    private bool _isProfilesVisible;
    private RangeObservableCollection<Operation> _profiles = [];
    private Operation? _selectedProfileItem;
    private string? _profileText;

    private ToolBaseControl _contentControl = null!;

    private bool _isButton1Visible;
    private string _button1Text = "Reset to defaults";
    private string _checkBox1Text = "Show advanced options";
    private bool _isCheckBox1Checked;
    private bool _isCheckBox1Visible;

    private bool _buttonOkEnabled = true;
    private string _buttonOkText = "Ok";
    private bool _buttonOkVisible = true;
    private bool _closeWindowAfterProcess = true;


    #region Description

    public string? Description
    {
        get => _description;
        set => RaiseAndSetIfChanged(ref _description, value);
    }

    public double DescriptionMaxWidth
    {
        get => _descriptionMaxWidth;
        set => RaiseAndSetIfChanged(ref _descriptionMaxWidth, value);
    }

    public double ProfileBoxMaxWidth
    {
        get => _profileBoxMaxWidth;
        set => RaiseAndSetIfChanged(ref _profileBoxMaxWidth, value);
    }

    #endregion

    #region Layer Selector

    public bool LayerRangeVisible
    {
        get => _layerRangeVisible;
        set => RaiseAndSetIfChanged(ref _layerRangeVisible, value);
    }

    public bool LayerRangeSync
    {
        get => _layerRangeSync;
        set
        {
            if(!RaiseAndSetIfChanged(ref _layerRangeSync, value)) return;
            if (_layerRangeSync)
            {
                LayerIndexEnd = _layerIndexStart;
            }
        }
    }

    public uint LayerIndexStart
    {
        get => _layerIndexStart;
        set
        {
            SlicerFile?.SanitizeLayerIndex(ref value);

            if (ToolControl?.BaseOperation is not null)
            {
                ToolControl.BaseOperation.LayerRangeSelection = LayerRangeSelection.None;
                ToolControl.BaseOperation.LayerIndexStart = value;
            }

            if (!RaiseAndSetIfChanged(ref _layerIndexStart, value)) return;
            RaisePropertyChanged(nameof(LayerStartMM));
            RaisePropertyChanged(nameof(LayerRangeCountStr));

            if (_layerRangeSync)
            {
                LayerIndexEnd = _layerIndexStart;
            }

            App.MainWindow.ActualLayer = _layerIndexStart;
        }
    }

    public float LayerStartMM => SlicerFile!.ContainsLayer(_layerIndexStart) ? SlicerFile[_layerIndexStart].PositionZ : 0;

    public uint LayerIndexEnd
    {
        get => _layerIndexEnd;
        set
        {
            SlicerFile?.SanitizeLayerIndex(ref value);

            if (ToolControl?.BaseOperation is not null)
            {
                ToolControl.BaseOperation.LayerRangeSelection = LayerRangeSelection.None;
                ToolControl.BaseOperation.LayerIndexEnd = value;
            }

            if (!RaiseAndSetIfChanged(ref _layerIndexEnd, value)) return;
            RaisePropertyChanged(nameof(LayerEndMM));
            RaisePropertyChanged(nameof(LayerRangeCountStr));

            //App.MainWindow.ActualLayer = _layerIndexEnd;
        }
    }

    public float LayerEndMM => SlicerFile!.ContainsLayer(_layerIndexEnd) ? SlicerFile[_layerIndexEnd].PositionZ : 0;

    public bool LayerIndexEndEnabled
    {
        get => _layerIndexEndEnabled;
        set => RaiseAndSetIfChanged(ref _layerIndexEndEnabled, value);
    }


    public string LayerRangeCountStr
    {
        get
        {
            uint layerCount = (uint) Math.Max(0, (int)LayerIndexEnd - LayerIndexStart + 1);
            return SlicerFile is null
                ? $"({layerCount} layers"
                : $"({layerCount} layers / {Layer.ShowHeight(SlicerFile.LayerHeight * layerCount)}mm)";
        }

    }

    public uint MaximumLayerIndex => App.MainWindow?.SliderMaximumValue ?? 0;

    public void SelectAllLayers()
    {
        LayerIndexStart = 0;
        LayerIndexEnd = MaximumLayerIndex;
        if(ToolControl?.BaseOperation is not null)
            ToolControl.BaseOperation.LayerRangeSelection = LayerRangeSelection.All;
    }

    public void SelectCurrentLayer()
    {
        LayerIndexStart = LayerIndexEnd = App.MainWindow.ActualLayer;
        if (ToolControl?.BaseOperation is not null)
            ToolControl.BaseOperation.LayerRangeSelection = LayerRangeSelection.Current;
    }

    public void SelectFirstToCurrentLayer()
    {
        LayerIndexEnd = App.MainWindow.ActualLayer;
        LayerIndexStart = 0;
        if (ToolControl?.BaseOperation is not null)
            ToolControl.BaseOperation.LayerRangeSelection = LayerRangeSelection.None;
    }

    public void SelectCurrentToLastLayer()
    {
        if (SlicerFile is null) return;
        LayerIndexStart = App.MainWindow.ActualLayer;
        LayerIndexEnd = SlicerFile.LastLayerIndex;
        if (ToolControl?.BaseOperation is not null)
            ToolControl.BaseOperation.LayerRangeSelection = LayerRangeSelection.None;
    }

    public void SelectBottomLayers()
    {
        if (SlicerFile is null) return;
        LayerIndexStart = 0;
        LayerIndexEnd = Math.Max(1, SlicerFile.FirstNormalLayer?.Index ?? 1) - 1u;
        if (ToolControl?.BaseOperation is not null)
            ToolControl.BaseOperation.LayerRangeSelection = LayerRangeSelection.Bottom;
    }

    public void SelectNormalLayers()
    {
        if (SlicerFile is null) return;
        LayerIndexStart = SlicerFile.FirstNormalLayer?.Index ?? 0;
        LayerIndexEnd = MaximumLayerIndex;
        if (ToolControl?.BaseOperation is not null)
            ToolControl.BaseOperation.LayerRangeSelection = LayerRangeSelection.Normal;
    }

    public void SelectFirstLayer()
    {
        LayerIndexStart = LayerIndexEnd = 0;
        if (ToolControl?.BaseOperation is not null)
            ToolControl.BaseOperation.LayerRangeSelection = LayerRangeSelection.First;
    }

    public void SelectLastLayer()
    {
        LayerIndexStart = LayerIndexEnd = MaximumLayerIndex;
        if (ToolControl?.BaseOperation is not null)
            ToolControl.BaseOperation.LayerRangeSelection = LayerRangeSelection.Last;
    }

    public void SelectLayers(LayerRangeSelection range)
    {
        switch (range)
        {
            case LayerRangeSelection.None:
                break;
            case LayerRangeSelection.All:
                SelectAllLayers();
                break;
            case LayerRangeSelection.Current:
                SelectCurrentLayer();
                break;
            case LayerRangeSelection.Bottom:
                SelectBottomLayers();
                break;
            case LayerRangeSelection.Normal:
                SelectNormalLayers();
                break;
            case LayerRangeSelection.First:
                SelectFirstLayer();
                break;
            case LayerRangeSelection.Last:
                SelectLastLayer();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public bool CloseWindowAfterProcess
    {
        get => _closeWindowAfterProcess;
        set => RaiseAndSetIfChanged(ref _closeWindowAfterProcess, value);
    }

    #endregion

    #region ROI & Masks

    public bool IsROIOrMasksVisible
    {
        get => IsROIVisible || IsMasksVisible;
        set
        {
            IsROIVisible = false;
            IsMasksVisible = false;
        }
    }

    public bool IsROIVisible
    {
        get
        {
            if (ToolControl?.BaseOperation is null) return _isROIVisible;
            return ToolControl.BaseOperation.CanROI && _isROIVisible;
        }
        set
        {
            if(!RaiseAndSetIfChanged(ref _isROIVisible, value)) return;
            RaisePropertyChanged(nameof(IsROIOrMasksVisible));
        }
    }

    public bool IsMasksVisible
    {
        get
        {
            if (ToolControl?.BaseOperation is null) return _isMasksVisible;
            return ToolControl.BaseOperation.CanMask && _isMasksVisible;
        }
        set
        {
            if(!RaiseAndSetIfChanged(ref _isMasksVisible, value)) return;
            RaisePropertyChanged(nameof(IsROIOrMasksVisible));
        }
    }

    public bool CanROI => ToolControl?.BaseOperation?.CanROI ?? false;

    public Rectangle ROI
    {
        get => App.MainWindow.ROI;
        set
        {
            App.MainWindow.ROI = value;
            if (ToolControl?.BaseOperation is not null) ToolControl.BaseOperation.ROI = value;
            IsROIVisible = !value.IsEmpty;
            RaisePropertyChanged();
        }
    }

    public Point[][] Masks => App.MainWindow.MaskPoints.ToArray();

    public bool ClearROIAndMaskAfterOperation
    {
        get => _clearRoiAndMaskAfterOperation;
        set => RaiseAndSetIfChanged(ref _clearRoiAndMaskAfterOperation, value);
    }

    public async Task ClearROI()
    {
        if (await this.MessageBoxQuestion("Are you sure you want to clear the current ROI?\n" +
                                          "This action can not be reverted, to select another ROI you must quit this window and select it on layer preview.",
                "Clear the current ROI?") != MessageButtonResult.Yes) return;

        ROI = Rectangle.Empty;
        ToolControl?.Callback(Callbacks.ClearROI);
    }

    public async Task ClearMasks()
    {
        if (await this.MessageBoxQuestion("Are you sure you want to clear all masks?\n" +
                                          "This action can not be reverted, to select another mask(s) you must quit this window and select it on layer preview.",
                "Clear the all masks?") != MessageButtonResult.Yes) return;
        IsMasksVisible = false;
        App.MainWindow.ClearMask();
        if (ToolControl?.BaseOperation is not null)
        {
            ToolControl.BaseOperation.ClearMasks();
            ToolControl.Callback(Callbacks.ClearROI);
        }

    }

    public void SelectVolumeBoundingRectangle()
    {
        if (SlicerFile is null) return;
        ROI = SlicerFile.BoundingRectangle;
    }

    #endregion

    #region Profiles

    public bool CanHaveProfiles => ToolControl?.BaseOperation?.CanHaveProfiles ?? false;

    public bool IsProfilesVisible
    {
        get => _isProfilesVisible;
        set => RaiseAndSetIfChanged(ref _isProfilesVisible, value);
    }

    public RangeObservableCollection<Operation> Profiles
    {
        get => _profiles;
        set => RaiseAndSetIfChanged(ref _profiles, value);
    }

    public Operation? SelectedProfileItem
    {
        get => _selectedProfileItem;
        set
        {
            if(!RaiseAndSetIfChanged(ref _selectedProfileItem, value) || value is null) return;
            if (ToolControl is null) return;
            var operation = _selectedProfileItem!.Clone();
            operation.ProfileName = null;
            operation.ClearPropertyChangedListeners();
            operation.ImportedFrom = Operation.OperationImportFrom.Profile;
            ToolControl.BaseOperation = operation;
            switch (operation.LayerRangeSelection)
            {
                case LayerRangeSelection.None:
                    LayerIndexStart = operation.LayerIndexStart;
                    LayerIndexEnd = operation.LayerIndexEnd;
                    break;
                default:
                    SelectLayers(operation.LayerRangeSelection);
                    break;
            }

            //ToolControl.Callback(Callbacks.AfterLoadProfile);
            //ToolControl.ResetDataContext();
        }
    }

    public string? ProfileText
    {
        get => _profileText;
        set => RaiseAndSetIfChanged(ref _profileText, value);
    }

    public async Task AddProfile()
    {
        if (ToolControl?.BaseOperation is null) return;
        var name = string.IsNullOrWhiteSpace(_profileText) ? null : _profileText.Trim();
        var operation = OperationProfiles.FindByName(ToolControl.BaseOperation, name);
        if (operation is not null)
        {
            if (await this.MessageBoxQuestion(
                    $"A profile with same name or settings already exists.\nDo you want to overwrite:\n{operation}",
                    "Overwrite profile?") != MessageButtonResult.Yes) return;
            /*var index = OperationProfiles.Instance.IndexOf(operation);
            OperationProfiles.Profiles[index] = ToolControl.BaseOperation;
            index = Profiles.IndexOf(operation);
            Profiles[index] = ToolControl.BaseOperation;*/

            OperationProfiles.RemoveProfile(operation, false);
            Profiles.Remove(operation);
        }

        var toAdd = ToolControl.BaseOperation.Clone();
        toAdd.ProfileName = string.IsNullOrWhiteSpace(_profileText) ? null : _profileText.Trim();
        toAdd.ClearPropertyChangedListeners();
        OperationProfiles.AddProfile(toAdd);
        Profiles.Insert(0, toAdd);

        ProfileText = null;
    }

    public async Task RemoveSelectedProfile()
    {
        if (_selectedProfileItem is null) return;

        if (await this.MessageBoxQuestion(
                $"Are you sure you want to remove the selected profile?\n{_selectedProfileItem}",
                "Remove selected profile?") != MessageButtonResult.Yes) return;

        OperationProfiles.RemoveProfile(_selectedProfileItem);
        Profiles.Remove(_selectedProfileItem);
        SelectedProfileItem = null;

    }

    public async Task ClearProfiles()
    {
        if (Profiles.Count == 0) return;
        if (await this.MessageBoxQuestion(
                $"Are you sure you want to clear all the {Profiles.Count} profiles?",
                "Clear all profiles?") != MessageButtonResult.Yes) return;

        OperationProfiles.ClearProfiles(Profiles[0].GetType());
        Profiles.Clear();
    }

    public void DeselectProfile()
    {
        SelectedProfileItem = null;
    }

    public async Task SetDefaultProfile()
    {
        if (_selectedProfileItem is null) return;

        if ((_globalModifiers & KeyModifiers.Shift) != 0)
        {
            if (await this.MessageBoxQuestion(
                    $"Are you sure you want to clear the selected profile as default settings for this dialog?",
                    "Clear the default profile?") != MessageButtonResult.Yes) return;

            foreach (var operation in Profiles)
            {
                operation.ProfileIsDefault = false;
            }
        }
        else
        {
            if (await this.MessageBoxQuestion(
                    $"Are you sure you want to set the selected profile as default settings for this dialog?\n{_selectedProfileItem}",
                    "Set as default profile?") != MessageButtonResult.Yes) return;

            foreach (var operation in Profiles)
            {
                operation.ProfileIsDefault = false;
            }

            _selectedProfileItem.ProfileIsDefault = true;
        }

        OperationProfiles.Save();

    }

    #endregion

    #region Content

    public bool IsContentVisible => _contentControl.IsVisible;

    public ToolBaseControl ContentControl
    {
        get => _contentControl;
        set => RaiseAndSetIfChanged(ref _contentControl, value);
    }

    #endregion

    #region Actions

    public bool IsButton1Visible
    {
        get => _isButton1Visible;
        set => RaiseAndSetIfChanged(ref _isButton1Visible, value);
    }

    public string Button1Text
    {
        get => _button1Text;
        set => RaiseAndSetIfChanged(ref _button1Text, value);
    }

    public void OnButton1Click() => ToolControl?.Callback(Callbacks.Button1);

    public string CheckBox1Text
    {
        get => _checkBox1Text;
        set => RaiseAndSetIfChanged(ref _checkBox1Text, value);
    }

    public bool IsCheckBox1Checked
    {
        get => _isCheckBox1Checked;
        set
        {
            if(!RaiseAndSetIfChanged(ref _isCheckBox1Checked, value)) return;
            ToolControl?.Callback(Callbacks.Checkbox1);
        }
    }

    public bool IsCheckBox1Visible
    {
        get => _isCheckBox1Visible;
        set => RaiseAndSetIfChanged(ref _isCheckBox1Visible, value);
    }


    public bool ButtonOkEnabled
    {
        get => _buttonOkEnabled;
        set => RaiseAndSetIfChanged(ref _buttonOkEnabled, value);
    }

    public bool ButtonOkVisible
    {
        get => _buttonOkVisible;
        set => RaiseAndSetIfChanged(ref _buttonOkVisible, value);
    }

    public string ButtonOkText
    {
        get => _buttonOkText;
        set => RaiseAndSetIfChanged(ref _buttonOkText, value);
    }

    #endregion

    #region Constructors
    public ToolWindow()
    {
        CanResize = Settings.General.WindowsCanResize;

        SelectAllLayers();

        if (ROI != Rectangle.Empty)
        {
            IsROIVisible = true;
        }

        if (Masks.Length > 0)
        {
            IsMasksVisible = true;
        }

        if (Design.IsDesignMode)
        {
            _contentControl = new ToolBaseControl();
            InitializeComponent();
        }
    }

    public ToolWindow(ToolBaseControl contentControl, string? description = null, bool layerRangeVisible = true, bool layerEndIndexEnabled = true) : this()
    {
        _description = description;
        _layerRangeVisible = layerRangeVisible;
        _layerIndexEndEnabled = layerEndIndexEnabled;
        _contentControl = contentControl;
        _contentControl.ParentWindow = this;
        if (_contentControl is not Controls.Tools.ToolControl) DataContext = this;

        InitializeComponent();
    }

    public ToolWindow(ToolControl toolControl) : this(toolControl, toolControl.BaseOperation!.Description, toolControl.BaseOperation.StartLayerRangeSelection != LayerRangeSelection.None, toolControl.BaseOperation.LayerIndexEndEnabled)
    {
        ToolControl = toolControl;
        toolControl.ParentWindow = this;
        toolControl.BaseOperation.ROI = ROI;
        toolControl.BaseOperation.MaskPoints = Masks;

        Title = toolControl.BaseOperation.Title;
        //LayerRangeVisible = toolControl.BaseOperation.StartLayerRangeSelection != LayerRangeSelection.None;
        //IsROIVisible = toolControl.BaseOperation.CanROI;
        _buttonOkText = toolControl.BaseOperation.ButtonOkText;
        _buttonOkVisible = ButtonOkEnabled = toolControl.BaseOperation.HaveAction;

        bool loadedFromSession = false;
        if (!toolControl.BaseOperation.HaveExecuted
            && toolControl.BaseOperation.ImportedFrom is Operation.OperationImportFrom.None
            && Settings.Tools.RestoreLastUsedSettings)
        {
            var operation = OperationSessionManager.Instance.Find(toolControl.BaseOperation.GetType());
            if (operation is not null)
            {
                toolControl.BaseOperation = operation.Clone();
                loadedFromSession = true;
            }
        }

        if (toolControl.BaseOperation.HaveExecuted
            || toolControl.BaseOperation.ImportedFrom != Operation.OperationImportFrom.None) // Loaded from something
        {
            if (toolControl.BaseOperation.HaveROI)
            {
                ROI = toolControl.BaseOperation.ROI;
            }

            if (toolControl.BaseOperation.HaveMask)
            {
                App.MainWindow.AddMaskPoints(toolControl.BaseOperation.MaskPoints!);
            }

            if (toolControl.BaseOperation.LayerRangeSelection == LayerRangeSelection.None)
            {
                LayerIndexStart = toolControl.BaseOperation.LayerIndexStart;
                LayerIndexEnd = toolControl.BaseOperation.LayerIndexEnd;
            }
            else
            {
                SelectLayers(toolControl.BaseOperation.LayerRangeSelection);
            }
        }
        else
        {
            SelectLayers(toolControl.BaseOperation.StartLayerRangeSelection);
        }


        if (toolControl.BaseOperation.CanHaveProfiles)
        {
            _isProfilesVisible = true;
            var profiles = OperationProfiles.GetOperations(toolControl.BaseOperation.GetType());
            _profiles.AddRange(profiles);

            if (toolControl.BaseOperation.ImportedFrom == Operation.OperationImportFrom.None ||
                (loadedFromSession && !Settings.Tools.LastUsedSettingsPriorityOverDefaultProfile))
            {
                //Operation profile = _profiles.FirstOrDefault(operation => operation.ProfileIsDefault);
                foreach (var operation in Profiles)
                {
                    if (operation.ProfileIsDefault)
                    {
                        SelectedProfileItem = operation;
                        break;
                    }
                }
            }
        }

        //RaisePropertyChanged(nameof(IsContentVisible));
        //RaisePropertyChanged(nameof(IsROIVisible));

        toolControl.Callback(Callbacks.Init);
        toolControl.DataContext = toolControl;
        DataContext = this;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        DescriptionMaxWidth = Math.Max(DesiredSize.Width, _contentControl.DesiredSize.Width) - 20;
        ProfileBoxMaxWidth = DescriptionMaxWidth - 64;
        //Height = MaxHeight;

        /*Dispatcher.UIThread.Post(() =>
        {
            if (Math.Max((int)_contentScrollViewer.Extent.Height - (int)_contentScrollViewer.Viewport.Height, 0) == 0)
            {
                Height = 10;
                SizeToContent = SizeToContent.WidthAndHeight;
            }

            Position = new PixelPoint(
                Math.Max(0, (int)(Math.Max(0, App.MainWindow.Position.X) + App.MainWindow.Width / 2 - Width / 2)),
                Math.Max(0, App.MainWindow.Position.Y) + 20
            );

            CanResize = Settings.General.WindowsCanResize;
        }, DispatcherPriority.Loaded);*/
    }

    /*public void FitToSize()
    {
        SizeToContent = SizeToContent.Manual;
        Height = MaxHeight;
        Dispatcher.UIThread.Post(() =>
        {
            if (Math.Max((int)_contentScrollViewer.Extent.Height - (int)_contentScrollViewer.Viewport.Height, 0) == 0)
            {
                Height = 10;
                SizeToContent = SizeToContent.WidthAndHeight;
            }

            Position = new PixelPoint(
                Math.Max(0, (int)(Math.Max(0, App.MainWindow.Position.X) + App.MainWindow.Width / 2 - Width / 2)),
                Math.Max(0, App.MainWindow.Position.Y) + 20
            );

        }, DispatcherPriority.Loaded);
    }*/

    #endregion

    /*protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (!(ToolControl is null))
        {
            DescriptionMaxWidth = Math.Max(Bounds.Width, ToolControl?.Bounds.Width ?? 0) - 40;
            Description = ToolControl.BaseOperation.Description;
        }
        DataContext = this;
    }*/

    #region Overrides

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _globalModifiers = e.KeyModifiers;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        _globalModifiers = e.KeyModifiers;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        _globalModifiers = e.KeyModifiers;
    }

    #endregion

    public async Task Process()
    {
        if(!await _contentControl.OnBeforeProcess()) return;

        if (LayerIndexStart > LayerIndexEnd)
        {
            await this.MessageBoxError("Layer range start can't be higher than layer end.\nPlease fix and try again.");
            return;
        }

        if (ToolControl?.BaseOperation is not null)
        {
            ToolControl.BaseOperation.LayerIndexStart = LayerIndexStart;
            ToolControl.BaseOperation.LayerIndexEnd = LayerIndexEnd;
            /*if (IsROIVisible && ToolControl.BaseOperation.ROI.IsEmpty)
            {
            }*/
            ToolControl.BaseOperation.SetROIIfEmpty(ROI);
            ToolControl.BaseOperation.SetMasksIfEmpty(Masks);

            if (!await ToolControl.ValidateForm()) return;
            if (Settings.Tools.PromptForConfirmation && !string.IsNullOrEmpty(ToolControl.BaseOperation.ConfirmationText))
            {
                var result = await this.MessageBoxQuestion(
                        $"Are you sure you want to {ToolControl.BaseOperation.ConfirmationText}");
                if (result != MessageButtonResult.Yes) return;
            }
        }
        else
        {
            if (!await _contentControl.ValidateForm()) return;
        }

        if (_clearRoiAndMaskAfterOperation)
        {
            App.MainWindow.ClearROIAndMask();
        }

        if (!await _contentControl.OnAfterProcess()) return;

        DialogResult = DialogResults.OK;
        if (_closeWindowAfterProcess)
        {
            Close(DialogResult);
        }

    }

    public async Task ExportSettings()
    {
        if (ToolControl?.BaseOperation is null) return;

        using var file = await SaveFilePickerAsync(ToolControl.BaseOperation.Id, AvaloniaStatic.OperationSettingFileFilter);

        if (file?.TryGetLocalPath() is not { } filePath) return;

        try
        {
            ToolControl.BaseOperation.Serialize(filePath, true);
        }
        catch (Exception e)
        {
            await this.MessageBoxError(e.ToString(), "Error while trying to export the settings");
        }
    }

    public async Task ImportSettings()
    {
        if (ToolControl?.BaseOperation is null) return;
        var files = await OpenFilePickerAsync(AvaloniaStatic.OperationSettingFileFilter);

        if (files.Count == 0 || files[0].TryGetLocalPath() is not { } filePath) return;

        try
        {
            var operation = Operation.Deserialize(filePath, ToolControl.BaseOperation);
            if (operation is null)
            {
                await this.MessageBoxError("Unable to import settings, file may be malformed.", "Error while trying to import the settings");
                return;
            }

            ToolControl.BaseOperation = operation;
            switch (operation.LayerRangeSelection)
            {
                case LayerRangeSelection.None:
                    LayerIndexStart = operation.LayerIndexStart;
                    LayerIndexEnd = operation.LayerIndexEnd;
                    break;
                default:
                    SelectLayers(operation.LayerRangeSelection);
                    break;
            }


        }
        catch (Exception e)
        {
            await this.MessageBoxError(e.ToString(), "Error while trying to import the settings");
        }
    }

    public void ResetToDefaults()
    {
        if (ToolControl?.BaseOperation is null) return;
        var operation = ToolControl.BaseOperation.GetType().CreateInstance<Operation>(SlicerFile!)!;
        operation.LayerIndexStart = ToolControl.BaseOperation.LayerIndexStart;
        operation.LayerIndexEnd = ToolControl.BaseOperation.LayerIndexEnd;
        operation.LayerRangeSelection = ToolControl.BaseOperation.LayerRangeSelection;
        operation.ROI = ToolControl.BaseOperation.ROI;
        operation.MaskPoints = ToolControl.BaseOperation.MaskPoints;
        ToolControl.BaseOperation = operation;
    }
}