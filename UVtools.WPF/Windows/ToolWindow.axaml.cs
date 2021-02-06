using System;
using System.Collections.ObjectModel;
using System.Drawing;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DynamicData;
using MessageBox.Avalonia.Enums;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;
using UVtools.WPF.Controls;
using UVtools.WPF.Controls.Tools;
using UVtools.WPF.Extensions;
using UVtools.WPF.Structures;

namespace UVtools.WPF.Windows
{
    public class ToolWindow : WindowEx
    {
        public enum Callbacks : byte
        {
            Init,
            ClearROI,
            ProfileLoaded,
            Button1, // Reset to defaults
            Checkbox1, // Show Advanced
        }
        private KeyModifiers _globalModifiers;
        public ToolControl ToolControl;
        private string _description;
        private double _descriptionMaxWidth;
        private bool _layerRangeVisible = true;
        private bool _layerRangeSync;
        private uint _layerIndexStart;
        private uint _layerIndexEnd;
        private bool _isROIVisible;

        private bool _clearRoiAfterOperation;

        private bool _isProfilesVisible;
        private ObservableCollection<Operation> _profiles = new();
        private Operation _selectedProfileItem;
        private string _profileText;

        private IControl _contentControl;
        private bool _isButton1Visible;
        private string _button1Text = "Reset to defaults";
        private string _checkBox1Text = "Show advanced options";
        private bool _isCheckBox1Checked;
        private bool _isCheckBox1Visible;

        private bool _buttonOkEnabled = true;
        private string _buttonOkText = "Ok";
        private bool _buttonOkVisible = true;
        private double _scrollViewerMaxHeight=double.PositiveInfinity;
        


        public double ScrollViewerMaxHeight
        {
            get => _scrollViewerMaxHeight;
            set => RaiseAndSetIfChanged(ref _scrollViewerMaxHeight, value);
        }

        #region Description

        public string Description
        {
            get => _description;
            set => RaiseAndSetIfChanged(ref _description, value);
        }

        public double DescriptionMaxWidth
        {
            get => _descriptionMaxWidth;
            set => RaiseAndSetIfChanged(ref _descriptionMaxWidth, value);
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
                if (!(ToolControl?.BaseOperation is null))
                {
                    ToolControl.BaseOperation.LayerRangeSelection = Enumerations.LayerRangeSelection.None;
                    ToolControl.BaseOperation.LayerIndexStart = value;
                }

                value = value.Clamp(0, App.SlicerFile.LastLayerIndex);
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

        public float LayerStartMM => App.SlicerFile[_layerIndexStart].PositionZ;

        public uint LayerIndexEnd
        {
            get => _layerIndexEnd;
            set
            {
                if (!(ToolControl?.BaseOperation is null))
                {
                    ToolControl.BaseOperation.LayerRangeSelection = Enumerations.LayerRangeSelection.None;
                    ToolControl.BaseOperation.LayerIndexEnd = value;
                }

                value = value.Clamp(0, App.SlicerFile.LastLayerIndex);
                if (!RaiseAndSetIfChanged(ref _layerIndexEnd, value)) return;
                RaisePropertyChanged(nameof(LayerEndMM));
                RaisePropertyChanged(nameof(LayerRangeCountStr));
            }
        }

        public float LayerEndMM => App.SlicerFile[_layerIndexEnd].PositionZ;
        
        public string LayerRangeCountStr
        {
            get
            {
                uint layerCount = (uint) Math.Max(0, (int)LayerIndexEnd - LayerIndexStart + 1);
                return $"({layerCount} layers / {Math.Round(App.SlicerFile.LayerHeight * layerCount, 2)}mm)";
            }
            
        }

        public uint MaximumLayerIndex => App.MainWindow?.SliderMaximumValue ?? 0;

        public void SelectAllLayers()
        {
            LayerIndexStart = 0;
            LayerIndexEnd = MaximumLayerIndex;
            if(!(ToolControl is null))
                ToolControl.BaseOperation.LayerRangeSelection = Enumerations.LayerRangeSelection.All;
        }

        public void SelectCurrentLayer()
        {
            LayerIndexStart = LayerIndexEnd = App.MainWindow.ActualLayer;
            if (!(ToolControl is null))
                ToolControl.BaseOperation.LayerRangeSelection = Enumerations.LayerRangeSelection.Current;
        }

        public void SelectBottomLayers()
        {
            LayerIndexStart = 0;
            LayerIndexEnd = App.SlicerFile.BottomLayerCount-1u;
            if (!(ToolControl is null))
                ToolControl.BaseOperation.LayerRangeSelection = Enumerations.LayerRangeSelection.Bottom;
        }

        public void SelectNormalLayers()
        {
            LayerIndexStart = App.SlicerFile.BottomLayerCount;
            LayerIndexEnd = MaximumLayerIndex;
            if (!(ToolControl is null))
                ToolControl.BaseOperation.LayerRangeSelection = Enumerations.LayerRangeSelection.Normal;
        }

        public void SelectFirstLayer()
        {
            LayerIndexStart = LayerIndexEnd = 0;
            if (!(ToolControl is null))
                ToolControl.BaseOperation.LayerRangeSelection = Enumerations.LayerRangeSelection.First;
        }

        public void SelectLastLayer()
        {
            LayerIndexStart = LayerIndexEnd = MaximumLayerIndex;
            if (!(ToolControl is null))
                ToolControl.BaseOperation.LayerRangeSelection = Enumerations.LayerRangeSelection.Last;
        }

        public void SelectLayers(Enumerations.LayerRangeSelection range)
        {
            switch (range)
            {
                case Enumerations.LayerRangeSelection.None:
                    break;
                case Enumerations.LayerRangeSelection.All:
                    SelectAllLayers();
                    break;
                case Enumerations.LayerRangeSelection.Current:
                    SelectCurrentLayer();
                    break;
                case Enumerations.LayerRangeSelection.Bottom:
                    SelectBottomLayers();
                    break;
                case Enumerations.LayerRangeSelection.Normal:
                    SelectNormalLayers();
                    break;
                case Enumerations.LayerRangeSelection.First:
                    SelectFirstLayer();
                    break;
                case Enumerations.LayerRangeSelection.Last:
                    SelectLastLayer();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion

        #region ROI

        public bool IsROIVisible
        {
            get
            {
                if (ToolControl is null) return _isROIVisible;
                return ToolControl.BaseOperation.CanROI && _isROIVisible;
            }
            set => RaiseAndSetIfChanged(ref _isROIVisible, value);
        }

        public Rectangle ROI => App.MainWindow.ROI;

        public bool ClearROIAfterOperation
        {
            get => _clearRoiAfterOperation;
            set => RaiseAndSetIfChanged(ref _clearRoiAfterOperation, value);
        }

        public async void ClearROI()
        {
            if (await this.MessageBoxQuestion("Are you sure you want to clear the current ROI?\n" +
                                   "This action can not be reverted, to select another ROI you must quit this window and select it on layer preview.",
                "Clear the current ROI?") != ButtonResult.Yes) return;
            IsROIVisible = false;
            App.MainWindow.LayerImageBox.SelectNone();
            ToolControl?.Callback(Callbacks.ClearROI);
        }

        #endregion

        #region Profiles
        public bool IsProfilesVisible
        {
            get => _isProfilesVisible;
            set => RaiseAndSetIfChanged(ref _isProfilesVisible, value);
        }

        public ObservableCollection<Operation> Profiles
        {
            get => _profiles;
            set => RaiseAndSetIfChanged(ref _profiles, value);
        }

        public Operation SelectedProfileItem
        {
            get => _selectedProfileItem;
            set
            {
                if(!RaiseAndSetIfChanged(ref _selectedProfileItem, value) || value is null) return;
                if (ToolControl is null) return;
                var operation = _selectedProfileItem.Clone();
                operation.ProfileName = null;
                operation.SlicerFile = App.SlicerFile;
                ToolControl.BaseOperation = operation;
                SelectLayers(operation.LayerRangeSelection);
                ToolControl.Callback(Callbacks.ProfileLoaded);
                ToolControl.ResetDataContext();
            }
        }

        public string ProfileText
        {
            get => _profileText;
            set => RaiseAndSetIfChanged(ref _profileText, value);
        }

        public async void AddProfile()
        {
            var name = string.IsNullOrWhiteSpace(_profileText) ? null : _profileText.Trim();
            var operation = OperationProfiles.FindByName(ToolControl.BaseOperation, name);
            if (!(operation is null))
            {
                if (await this.MessageBoxQuestion(
                    $"A profile with same name or settings already exists.\nDo you want to overwrite:\n{operation}",
                    "Overwrite profile?") != ButtonResult.Yes) return;
                /*var index = OperationProfiles.Instance.IndexOf(operation);
                OperationProfiles.Profiles[index] = ToolControl.BaseOperation;
                index = Profiles.IndexOf(operation);
                Profiles[index] = ToolControl.BaseOperation;*/
                
                OperationProfiles.RemoveProfile(operation, false);
                Profiles.Remove(operation);
            }

            var toAdd = ToolControl.BaseOperation.Clone();
            toAdd.ProfileName = string.IsNullOrWhiteSpace(_profileText) ? null : _profileText.Trim();
            OperationProfiles.AddProfile(toAdd);
            Profiles.Insert(0, toAdd);

            ProfileText = null;
        }

        public async void RemoveSelectedProfile()
        {
            if (_selectedProfileItem is null) return;
            
            if (await this.MessageBoxQuestion(
                $"Are you sure you want to remove the selected profile?\n{_selectedProfileItem}",
                "Remove selected profile?") != ButtonResult.Yes) return;

            OperationProfiles.RemoveProfile(_selectedProfileItem);
            Profiles.Remove(_selectedProfileItem);
            SelectedProfileItem = null;
            
        }

        public async void ClearProfiles()
        {
            if (Profiles.Count == 0) return;
            if (await this.MessageBoxQuestion(
                $"Are you sure you want to clear all the {Profiles.Count} profiles?",
                "Clear all profiles?") != ButtonResult.Yes) return;

            OperationProfiles.ClearProfiles(Profiles[0].GetType());
            Profiles.Clear();
        }

        public void DeselectProfile()
        {
            SelectedProfileItem = null;
        }

        public async void SetDefaultProfile()
        {
            if (_selectedProfileItem is null) return;

            if ((_globalModifiers & KeyModifiers.Shift) != 0)
            {
                if (await this.MessageBoxQuestion(
                    $"Are you sure you want to clear the selected profile as default settings for this dialog?",
                    "Clear the default profile?") != ButtonResult.Yes) return;

                foreach (var operation in Profiles)
                {
                    operation.ProfileIsDefault = false;
                }
            }
            else
            {
                if (await this.MessageBoxQuestion(
                    $"Are you sure you want to set the selected profile as default settings for this dialog?\n{_selectedProfileItem}",
                    "Set as default profile?") != ButtonResult.Yes) return;

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

        public bool IsContentVisible => ContentControl is null || ContentControl.IsVisible;

        public IControl ContentControl
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
            InitializeComponent();
            SelectAllLayers();

            if (ROI != Rectangle.Empty)
            {
                IsROIVisible = true;
            }
        }

        public ToolWindow(string description = null, bool layerRangeVisible = true) : this()
        {
            Description = description;
            LayerRangeVisible = layerRangeVisible;
        }

        public ToolWindow(ToolControl toolControl) : this()
        {
            ToolControl = toolControl;
            toolControl.ParentWindow = this;
            toolControl.Margin = new Thickness(15);

            Title = toolControl.BaseOperation.Title;
            LayerRangeVisible = toolControl.BaseOperation.StartLayerRangeSelection != Enumerations.LayerRangeSelection.None;
            //IsROIVisible = toolControl.BaseOperation.CanROI;
            ContentControl = toolControl;
            ButtonOkText = toolControl.BaseOperation.ButtonOkText;
            ButtonOkVisible = ButtonOkEnabled = toolControl.BaseOperation.HaveAction;

            if (toolControl.BaseOperation.LayerIndexStart == 0 && toolControl.BaseOperation.LayerIndexEnd == 0)
            {
                SelectLayers(toolControl.BaseOperation.StartLayerRangeSelection);
            }
            else
            {
                LayerIndexStart = toolControl.BaseOperation.LayerIndexStart;
                LayerIndexEnd = toolControl.BaseOperation.LayerIndexEnd;
            }

            //RaisePropertyChanged(nameof(IsContentVisible));
            //RaisePropertyChanged(nameof(IsROIVisible));

            if (ToolControl.BaseOperation.CanHaveProfiles)
            {
                var profiles = OperationProfiles.GetOperations(ToolControl.BaseOperation.GetType());
                Profiles.AddRange(profiles);
                IsProfilesVisible = true;

                foreach (var operation in Profiles)
                {
                    if (operation.ProfileIsDefault)
                    {
                        SelectedProfileItem = operation;
                        break;
                    }
                }
            }

            

            // Ensure the description don't stretch window
            DispatcherTimer.Run(() =>
            {
                if (Bounds.Width == 0) return true;
                ScrollViewerMaxHeight = this.GetScreenWorkingArea().Height - Bounds.Height + ToolControl.Bounds.Height - UserSettings.Instance.General.WindowsVerticalMargin;
                DescriptionMaxWidth = Math.Max(Bounds.Width, ToolControl.Bounds.Width) - 10;
                Description = toolControl.BaseOperation.Description;
                return false;
            }, TimeSpan.FromMilliseconds(1));
            
            toolControl.Callback(Callbacks.Init);
            toolControl.DataContext = toolControl;
            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
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

        public async void Process()
        {
            if (LayerIndexStart > LayerIndexEnd)
            {
                await this.MessageBoxError("Layer range start can't be higher than layer end.\nPlease fix and try again.");
                return;
            }

            if (!ReferenceEquals(ToolControl, null))
            {
                ToolControl.BaseOperation.LayerIndexStart = LayerIndexStart;
                ToolControl.BaseOperation.LayerIndexEnd = LayerIndexEnd;
                if (IsROIVisible && ToolControl.BaseOperation.ROI.IsEmpty)
                {
                    ToolControl.BaseOperation.ROI = App.MainWindow.ROI;
                }

                if (!await ToolControl.ValidateForm()) return;
                if (!string.IsNullOrEmpty(ToolControl.BaseOperation.ConfirmationText))
                {
                    var result =
                        await this.MessageBoxQuestion(
                            $"Are you sure you want to {ToolControl.BaseOperation.ConfirmationText}");
                    if (result != ButtonResult.Yes) return;
                }
            }

            if (ClearROIAfterOperation)
            {
                App.MainWindow.LayerImageBox.SelectNone();
            }

            DialogResult = DialogResults.OK;
            Close(DialogResult);
        }

        public void OpenContextMenu(string name)
        {
            var menu = this.FindControl<ContextMenu>($"{name}ContextMenu");
            if (menu is null) return;
            var parent = this.FindControl<Button>($"{name}Button");
            if (parent is null) return;
            menu.Open(parent);
        }
    }
}
