using System;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
        private bool _buttonOkEnabled = true;
        private string _buttonOkText = "Ok";
        private bool _isROIVisible;
        private Rect _roi = Rect.Empty;
        private bool _clearRoiAfterOperation;

        private IControl _contentControl;

        #region Description

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public double DescriptionMaxWidth
        {
            get => _descriptionMaxWidth;
            set => SetProperty(ref _descriptionMaxWidth, value);
        }

        #endregion

        #region Layer Selector

        public bool LayerRangeVisible
        {
            get => _layerRangeVisible;
            set => SetProperty(ref _layerRangeVisible, value);
        }

        public uint LayerIndexStart
        {
            get => _layerIndexStart;
            set
            {
                if (!(ToolControl?.BaseOperation is null))
                    ToolControl.BaseOperation.LayerIndexStart = value;
                
                if (!SetProperty(ref _layerIndexStart, value)) return;
                OnPropertyChanged(nameof(LayerStartMM));
                OnPropertyChanged(nameof(LayerRangeCountStr));
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

                if (!SetProperty(ref _layerIndexEnd, value)) return;
                OnPropertyChanged(nameof(LayerEndMM));
                OnPropertyChanged(nameof(LayerRangeCountStr));
            }
        }

        public float LayerEndMM => App.SlicerFile.GetHeightFromLayer(_layerIndexEnd);
        
        public string LayerRangeCountStr
        {
            get
            {
                uint layerCount = Math.Max(0, LayerIndexEnd - LayerIndexStart + 1);
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
            get => ToolControl?.BaseOperation.HaveROI ?? _isROIVisible;
            set => SetProperty(ref _isROIVisible, value);
        }

        public Rect ROI
        {
            get => _roi;
            set => SetProperty(ref _roi, value);
        }

        public bool ClearROIAfterOperation
        {
            get => _clearRoiAfterOperation;
            set => SetProperty(ref _clearRoiAfterOperation, value);
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
            set => SetProperty(ref _contentControl, value);
        }

        #endregion

        #region Actions

        public bool ButtonOkEnabled
        {
            get => _buttonOkEnabled;
            set => SetProperty(ref _buttonOkEnabled, value);
        }

        public string ButtonOkText
        {
            get => _buttonOkText;
            set => SetProperty(ref _buttonOkText, value);
        }

        #endregion

        public ToolWindow() 
        {
            InitializeComponent();
            DataContext = this;
            SelectAllLayers();
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
            IsROIVisible = toolControl.BaseOperation.CanROI;
            ContentControl = toolControl;
            ButtonOkText = toolControl.BaseOperation.ButtonOkText;

            OnPropertyChanged(nameof(IsContentVisible));

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

            // Ensure the description don't stretch window
            var timer = new Timer(10)
            {
                AutoReset = true
            };
            timer.Elapsed += (sender, args) =>
            {
                if (Bounds.Width == 0) return;
                DescriptionMaxWidth = Math.Max(Bounds.Width, ToolControl?.Bounds.Width ?? 0)-40;
                Description = toolControl.BaseOperation.Description;
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
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
                if(!ToolControl.ValidateForm()) return;
                if (!string.IsNullOrEmpty(ToolControl.BaseOperation.ConfirmationText))
                {
                    if (await this.MessageBoxQuestion(
                            $"Are you sure you want to {ToolControl.BaseOperation.ConfirmationText}") !=
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
