using System;
using Avalonia.Markup.Xaml;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolCalculatorControl : ToolControl
    {
        private decimal _lightOffDelayPrintTimeHours;
        public OperationCalculator Operation => BaseOperation as OperationCalculator;

        public decimal LightOffDelayPrintTimeHours
        {
            get => _lightOffDelayPrintTimeHours;
            set => RaiseAndSetIfChanged(ref _lightOffDelayPrintTimeHours, value);
        }

        public ToolCalculatorControl()
        {
            InitializeComponent();
            BaseOperation = new OperationCalculator(SlicerFile);
            Operation.CalcLightOffDelay.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName != nameof(Operation.CalcLightOffDelay.LightOffDelay) &&
                    e.PropertyName != nameof(Operation.CalcLightOffDelay.BottomLightOffDelay)) return;
                LightOffDelayPrintTimeHours = Math.Round(
                                                 (FileFormat.ExtraPrintTime +
                                                 SlicerFile.BottomLayerCount * (Operation.CalcLightOffDelay.BottomLightOffDelay + (decimal) SlicerFile.BottomExposureTime) +
                                                 SlicerFile.NormalLayerCount * (Operation.CalcLightOffDelay.LightOffDelay + (decimal)SlicerFile.ExposureTime))
                                                 / 3600, 2);
            };

            _lightOffDelayPrintTimeHours = (decimal) SlicerFile.PrintTimeHours;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void LightOffDelaySetParameters(byte side)
        {
            if (side == 0) // Bottom layers
            {
                SlicerFile.BottomLiftHeight = (float)Operation.CalcLightOffDelay.BottomLiftHeight;
                SlicerFile.BottomLiftSpeed = (float)Operation.CalcLightOffDelay.BottomLiftSpeed;
                SlicerFile.RetractSpeed = (float)Operation.CalcLightOffDelay.RetractSpeed;
                SlicerFile.BottomLightOffDelay = (float)Operation.CalcLightOffDelay.BottomLightOffDelay;
            }
            else // Normal layers
            {
                SlicerFile.LiftHeight = (float)Operation.CalcLightOffDelay.LiftHeight;
                SlicerFile.LiftSpeed = (float)Operation.CalcLightOffDelay.LiftSpeed;
                SlicerFile.RetractSpeed = (float)Operation.CalcLightOffDelay.RetractSpeed;
                SlicerFile.LightOffDelay = (float)Operation.CalcLightOffDelay.LightOffDelay;
            }

            LightOffDelayPrintTimeHours = (decimal)SlicerFile.PrintTimeHours;

            App.MainWindow.CanSave = true;
        }
    }
}
