using System;
using System.Drawing;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MessageBox.Avalonia.Enums;
using UVtools.Core;
using UVtools.WPF.Controls;
using UVtools.WPF.Controls.Tools;
using UVtools.WPF.Extensions;

namespace UVtools.WPF.Windows
{
    public class ToolWindow : WindowEx
    {
        public enum Callbacks : byte
        {
            Init,
            ClearROI,
            Button1, // Reset to defaults
            Checkbox1, // Show Advanced
        }
        public ToolControl ToolControl;
        private string _description;
        private double _descriptionMaxWidth;
        private bool _layerRangeVisible = true;
        private uint _layerIndexStart;
        private uint _layerIndexEnd;
        private bool _isROIVisible;
        private bool _clearRoiAfterOperation;

        private IControl _contentControl;
        private bool _isButton1Visible;
        private string _button1Text = "Reset to defaults";
        private string _checkBox1Text = "Show advanced options";
        private bool _isCheckBox1Checked;
        private bool _isCheckBox1Visible;

        private bool _buttonOkEnabled = true;
        private string _buttonOkText = "Ok";
        private bool _buttonOkVisible = true;

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

        public uint LayerIndexStart
        {
            get => _layerIndexStart;
            set
            {
                if (!(ToolControl?.BaseOperation is null))
                    ToolControl.BaseOperation.LayerIndexStart = value;
                
                if (!RaiseAndSetIfChanged(ref _layerIndexStart, value)) return;
                RaisePropertyChanged(nameof(LayerStartMM));
                RaisePropertyChanged(nameof(LayerRangeCountStr));
            }
        }

        public float LayerStartMM => App.SlicerFile.GetHeightFromLayer(_layerIndexStart);

        public uint LayerIndexEnd
        {
            get => _layerIndexEnd;
            set
            {
                if (!(ToolControl?.BaseOperation is null))
                    ToolControl.BaseOperation.LayerIndexEnd = value;

                if (!RaiseAndSetIfChanged(ref _layerIndexEnd, value)) return;
                RaisePropertyChanged(nameof(LayerEndMM));
                RaisePropertyChanged(nameof(LayerRangeCountStr));
            }
        }

        public float LayerEndMM => App.SlicerFile.GetHeightFromLayer(_layerIndexEnd);
        
        public string LayerRangeCountStr
        {
            get
            {
                uint layerCount = (uint) Math.Max(0, (int)LayerIndexEnd - LayerIndexStart + 1);
                return $"({layerCount} layers / {(decimal)App.SlicerFile.LayerHeight * layerCount}mm)";
            }
            
        }

        public uint MaximumLayerIndex => App.MainWindow?.SliderMaximumValue ?? 0;

        public void SelectAllLayers()
        {
            LayerIndexStart = 0;
            LayerIndexEnd = MaximumLayerIndex;
        }

        public void SelectCurrentLayer()
        {
            LayerIndexStart = LayerIndexEnd = App.MainWindow.ActualLayer;
        }

        public void SelectBottomLayers()
        {
            LayerIndexStart = 0;
            LayerIndexEnd = App.SlicerFile.BottomLayerCount-1u;
        }

        public void SelectNormalLayers()
        {
            LayerIndexStart = App.SlicerFile.BottomLayerCount;
            LayerIndexEnd = MaximumLayerIndex;
        }

        public void SelectFirstLayer()
        {
            LayerIndexStart = LayerIndexEnd = 0;
        }

        public void SelectLastLayer()
        {
            LayerIndexStart = LayerIndexEnd = MaximumLayerIndex;
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

        public ToolWindow() 
        {
            InitializeComponent();
            SelectAllLayers();

            if (ROI != Rectangle.Empty)
            {
                IsROIVisible = true;
            }

            DataContext = this;
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

            Title = toolControl.BaseOperation.Title;
            LayerRangeVisible = toolControl.BaseOperation.LayerRangeSelection != Enumerations.LayerRangeSelection.None;
            //IsROIVisible = toolControl.BaseOperation.CanROI;
            ContentControl = toolControl;
            ButtonOkText = toolControl.BaseOperation.ButtonOkText;
            ButtonOkVisible = ButtonOkEnabled = toolControl.BaseOperation.HaveAction;

            switch (toolControl.BaseOperation.LayerRangeSelection)
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

            RaisePropertyChanged(nameof(IsContentVisible));
            RaisePropertyChanged(nameof(IsROIVisible));

            // Ensure the description don't stretch window
            DispatcherTimer.Run(() =>
            {
                if (Bounds.Width == 0) return true;
                DescriptionMaxWidth = Math.Max(Bounds.Width, ToolControl?.Bounds.Width ?? 0) - 40;
                Description = toolControl.BaseOperation.Description;
                return false;
            }, TimeSpan.FromMilliseconds(1));


            toolControl.Callback(Callbacks.Init);
            toolControl.DataContext = toolControl;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

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
                    if (await this.MessageBoxQuestion($"Are you sure you want to {ToolControl.BaseOperation.ConfirmationText}") !=
                        ButtonResult.Yes) return;
                }
            }

            if (ClearROIAfterOperation)
            {
                App.MainWindow.LayerImageBox.SelectNone();
            }

            DialogResult = DialogResults.OK;
            Close(DialogResult);
        }
    }
}
